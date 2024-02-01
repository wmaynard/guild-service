using System.Reflection;
using RCL.Logging;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Minq;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.GuildService.Models;

namespace Rumble.Platform.GuildService.Services;

public class MemberService : MinqService<GuildMember>
{
    private readonly HistoryService _history;
    
    public MemberService(HistoryService history) : base("members")
    {
        mongo
            .DefineIndex(builder => builder
                .Add(member => member.GuildId)
                .Add(member => member.AccountId)
                .EnforceUniqueConstraint()
            );
        _history = history;
    }

    public GuildMember Register(GuildMember member) => mongo
        .Where(query => query
            .EqualTo(db => db.GuildId, member.GuildId)
            .EqualTo(db => db.AccountId, member.AccountId)
        )
        .Upsert(member);

    public GuildMember[] Lookup(string guildId, params string[] accountIds) => mongo
        .Where(query => query
            .EqualTo(member => member.GuildId, guildId)
            .ContainedIn(member => member.AccountId, accountIds)
        )
        .ToArray();

    public GuildMember ApproveApplication(string guildId, string applicantId, string officerId)
    {
        GuildMember[] lookups = Lookup(guildId, applicantId, officerId);
        GuildMember officer = lookups.FirstOrDefault(member => member.AccountId == officerId && member.Rank >= Rank.Officer);
        GuildMember applicant = lookups.FirstOrDefault(member => member.AccountId == applicantId && member.Rank == Rank.Applicant);

        if (officer == null)
            throw new PlatformException("Requesting player is not an officer of the specified guild and can not approve new members.", code: ErrorCode.Unauthorized);
        if (applicant == null)
            throw new PlatformException("No applicant found.  The player may have withdrawn their application or already been actioned on.", code: ErrorCode.MongoRecordNotFound);

        // Find all other guild applications. 
        List<GuildMember> existing = mongo
            .WithTransaction(out Transaction transaction)
            .Where(query => query
                .NotEqualTo(member => member.GuildId, guildId)
                .EqualTo(member => member.AccountId, applicant.AccountId)
            )
            .ToList();

        // If the user left a guild as part of this, track the time this happened at.
        foreach (GuildMember other in existing.Where(member => member.Rank != Rank.Applicant))
            other.LeftOn = Timestamp.Now;
        
        // Delete all other guild applications.
        mongo
            .WithTransaction(transaction)
            .OnRecordsAffected(result => Log.Local(Owner.Will, $"Deleted enrollment information for applicant from {result.Affected} other guilds."))
            .Where(query => query.ContainedIn(member => member.Id, existing.Select(other => other.Id)))
            .Delete();
        
        // Approve the existing application and mark the applicant as a full member.
        GuildMember output = mongo
            .WithTransaction(transaction)
            .OnRecordsAffected(_ => Log.Local(Owner.Will, $"Enrolled {applicant.AccountId} into guild {guildId}"))
            .Where(query => query
                .EqualTo(member => member.GuildId, guildId)
                .EqualTo(member => member.AccountId, applicant.AccountId)
                .EqualTo(member => member.Rank, applicant.Rank)
            )
            .UpdateAndReturnOne(update => update
                .Set(member => member.Rank, Rank.Member)
                .Set(member => member.JoinedOn, Timestamp.Now)
                .Set(member => member.ApprovedBy, officerId)
            );
        
        existing.Add(output);
        
        // Track all changes in the activity log.
        _history.Insert(transaction, existing.ToArray());
        

        if (output == null)
        {
            Abort(transaction);
            throw new PlatformException("Unable to approve guild applicant; transaction aborted.");
        }
        
        Commit(transaction);
        return output;
    }

    // Scenario 1: Guild member is leaving of their own accord
    // Scenario 2: Guild leader is leaving
    // Scenario 3: Guild leader is leaving and is the only member
    // Scenario 4: Guild officer or leader is kicking someone else out
    // Scenario 5: Guild officer or leader is rejecting an applicant
    public GuildMember Leave(string accountId, string kickedBy = null)
    {
        // Ensure that the user making the kick request has permissions to perform the action.
        if (!string.IsNullOrWhiteSpace(kickedBy))
        {
            GuildMember[] members = mongo
                .Where(query => query.ContainedIn(member => member.AccountId, new[] {accountId, kickedBy}))
                .Limit(200)
                .ToArray()
                .GroupBy(member => member.GuildId)
                .MaxBy(group => group.Count())
                ?.ToArray()
                ?? Array.Empty<GuildMember>();

            if (members.Length != 2)
                throw new PlatformException("Unable to kick player; the two members are not in the same guild.", code: ErrorCode.Unauthorized);

            GuildMember kicker = members.First(member => member.AccountId == kickedBy);
            GuildMember recipient = members.First(member => member.AccountId == accountId);

            if (kicker.Rank < Rank.Officer)
                throw new PlatformException("Unable to kick player; requester is not an officer.", code: ErrorCode.Unauthorized);
            if (recipient.Rank >= kicker.Rank)
                throw new PlatformException("Unable to kick player; requester is not high enough rank.", code: ErrorCode.Unauthorized);
        }
        
        GuildMember departing = mongo
            .WithTransaction(out Transaction transaction)
            .Where(query => query.EqualTo(member => member.AccountId, accountId))
            .FirstOrDefault()
            ?? throw new PlatformException("Account is not a member of a guild.", code: ErrorCode.MongoRecordNotFound);
        
        // Promote the next in line if necessary
        GuildMember promoted = string.IsNullOrWhiteSpace(departing.GuildId) && departing.Rank == Rank.Leader
            ? PromoteNextInLine(transaction, departing.GuildId, departing.AccountId)
            : null;

        departing.LeftOn = Timestamp.Now;
        departing.KickedBy = kickedBy;
        _history.Insert(transaction, departing, promoted);

        // Delete all of the departing player's entries in guild membership.
        mongo
            .WithTransaction(transaction)
            .Where(query => query.EqualTo(member => member.AccountId, accountId))
            .Delete();
        
        Commit(transaction);
        return departing;
    }

    private GuildMember PromoteNextInLine(Transaction transaction, string guildId, string promotedBy) => mongo
        .WithTransaction(transaction)
        .OnNoneAffected(_ =>
        {
            Log.Info(Owner.Will, "Leader left guild with no other members, it will be deleted.", data: new
            {
                GuildId = guildId
            });
            // TODO: Delete guild
        })
        .OnRecordsAffected(_ => Log.Info(Owner.Will, "A different member was promoted to leader.", data: new
        {
            GuildId = guildId
        }))
        .Where(query => query
            .EqualTo(member => member.GuildId, guildId)
            .LessThan(member => member.Rank, Rank.Leader)
            .GreaterThan(member => member.Rank, Rank.Applicant)
        )
        .Sort(query => query
            .OrderByDescending(member => member.Rank)
            .OrderByDescending(member => member.JoinedOn)
        )
        .UpdateAndReturnOne(update => update
            .Set(member => member.Rank, Rank.Leader)
            .Set(member => member.PromotedBy, promotedBy)
        );

    public GuildMember[] GetRoster(string guildId, bool includeApplicants = false) => mongo
        .Where(query => query
            .EqualTo(member => member.GuildId, guildId)
            .GreaterThanOrEqualTo(member => member.Rank, includeApplicants
                ? Rank.Applicant
                : Rank.Member
            )
        )
        .Sort(sort => sort.OrderByDescending(member => member.Rank))
        .Limit(100)
        .ToArray();
}