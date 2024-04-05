using System.Reflection;
using RCL.Logging;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Minq;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Guilds.Exceptions;
using Rumble.Platform.Guilds.Interop;
using Rumble.Platform.Guilds.Models;

namespace Rumble.Platform.Guilds.Services;

public class MemberService : MinqTimerService<GuildMember>
{
    private readonly HistoryService _history;
    
    public MemberService(HistoryService history) : base("members", IntervalMs.SixHours)
    {
        mongo
            .DefineIndex(builder => builder
                .Add(member => member.GuildId)
                .Add(member => member.AccountId)
                .EnforceUniqueConstraint()
            );
        _history = history;
    }

    public GuildMember[] Lookup(string guildId, params string[] accountIds) => mongo
        .Where(query => query
            .EqualTo(member => member.GuildId, guildId)
            .ContainedIn(member => member.AccountId, accountIds)
        )
        .ToArray();

    private void EnsureGuildNotFull(Transaction transaction, string guildId)
    {
        bool isFull = mongo
            .WithTransaction(transaction)
            .Where(query => query
                .EqualTo(member => member.GuildId, guildId)
                .GreaterThan(member => member.Rank, Rank.Applicant)
            )
            .Count() > Guild.CAPACITY;

        if (!isFull)
            return;
        
        Abort(transaction);
        throw new GuildFullException(guildId);
    }

    public GuildMember ApproveApplication(string applicantId, string officerId)
    {
        EnsureSourceOutranksTarget(applicantId, officerId, out GuildMember applicant, out GuildMember officer);
        
        // GuildMember[] lookups = Lookup(guildId, applicantId, officerId);
        // GuildMember officer = lookups.FirstOrDefault(member => member.AccountId == officerId && member.Rank >= Rank.Officer);
        // GuildMember applicant = lookups.FirstOrDefault(member => member.AccountId == applicantId && member.Rank == Rank.Applicant);
        //
        // if (officer == null)
        //     throw new PlatformException("Requesting player is not an officer of the specified guild and can not approve new members.", code: ErrorCode.Unauthorized);
        // if (applicant == null)
        //     throw new PlatformException("No applicant found.  The player may have withdrawn their application or already been actioned on.", code: ErrorCode.MongoRecordNotFound);

        // Find all other guild applications. 
        List<GuildMember> existing = mongo
            .WithTransaction(out Transaction transaction)
            .Where(query => query
                .NotEqualTo(member => member.GuildId, officer.GuildId)
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
        
        EnsureGuildNotFull(transaction, officer.GuildId);
        
        // Approve the existing application and mark the applicant as a full member.
        GuildMember output = mongo
            .WithTransaction(transaction)
            .OnRecordsAffected(_ => Log.Local(Owner.Will, $"Enrolled {applicant.AccountId} into guild {officer.GuildId}"))
            .Where(query => query
                .EqualTo(member => member.GuildId, officer.GuildId)
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

        Optional<GuildService>().PerformGuildUpdateTasks(officer.GuildId);
        return output;
    }

    private void EnsureSourceOutranksTarget(string victimId, string officerId, out GuildMember victim, out GuildMember officer)
    {
        victim = null;
        officer = null;

        if (string.IsNullOrWhiteSpace(victimId) || string.IsNullOrWhiteSpace(officerId))
            return;
        
        GuildMember[] members = mongo
            .Where(query => query.ContainedIn(member => member.AccountId, new[] { victimId, officerId }))
            .Limit(200)
            .ToArray()
            .GroupBy(member => member.GuildId)
            .MaxBy(group => group.Count())
            ?.ToArray()
            ?? Array.Empty<GuildMember>();

        if (members.Length != 2)
            throw new PlatformException("Unable to kick player; the two members are not in the same guild.", code: ErrorCode.Unauthorized);

        victim = members.First(member => member.AccountId == victimId);
        officer = members.First(member => member.AccountId == officerId);

        if (officer.Rank < Rank.Officer)
            throw new PlatformException("Unable to affect player; requester is not an officer.", code: ErrorCode.Unauthorized);
        if (victim.Rank >= officer.Rank)
            throw new PlatformException("Unable to affect player; requester is not high enough rank.", code: ErrorCode.Unauthorized);
    }

    // Scenario 1: Guild member is leaving of their own accord
    // Scenario 2: Guild leader is leaving
    // Scenario 3: Guild leader is leaving and is the only member
    // Scenario 4: Guild officer or leader is kicking someone else out
    // Scenario 5: Guild officer or leader is rejecting an applicant
    public GuildMember Remove(Transaction transaction, string accountId, string kickedBy = null)
    {
        // Ensure that the user making the kick request has permissions to perform the action.
        EnsureSourceOutranksTarget(accountId, kickedBy, out _, out _);

        GuildMember departing = mongo
            .WithTransaction(transaction)
            .Where(query => query.EqualTo(member => member.AccountId, accountId))
            .FirstOrDefault();
            // ?? throw new PlatformException("Account is not a member of a guild.", code: ErrorCode.MongoRecordNotFound);

        if (departing == null)
            return null;
        
        // Promote the next in line if necessary
        GuildMember promoted = !string.IsNullOrWhiteSpace(departing.GuildId) && departing.Rank == Rank.Leader
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
        
        return departing;
    }
    
    public GuildMember Remove(string accountId, string kickedBy = null)
    {
        mongo.WithTransaction(out Transaction transaction);
        
        GuildMember output = Remove(transaction, accountId, kickedBy);
        Commit(transaction);
        
        Optional<GuildService>().PerformGuildUpdateTasks(output.GuildId);
        
        return output;
    }
    
    // Leaders are inactive; promote the next eligible person for that guild.
    public GuildMember AlterRank(string accountId, string officerId, bool upwards = false)
    {
        EnsureSourceOutranksTarget(accountId, officerId, out GuildMember victim, out GuildMember officer);

        if (!upwards && victim.Rank <= Rank.Member)
            throw new PlatformException("Unable to demote member; they're already at the lowest rank.", code: ErrorCode.Unnecessary);

        GuildMember altered = mongo
            .WithTransaction(out Transaction transaction)
            .Where(query => query
                .EqualTo(member => member.GuildId, victim.GuildId)
                .EqualTo(member => member.AccountId, victim.AccountId)
            )
            .UpdateAndReturnOne(update => update
                .Set(member => member.UpdatedBy, officer.AccountId)
                .Set(member => member.Rank, victim.Rank switch
                {
                    Rank.Member when upwards => Rank.Elder,
                    Rank.Elder when upwards => Rank.Officer,
                    Rank.Officer when upwards => Rank.Leader,
                    Rank.Elder => Rank.Member,
                    Rank.Officer => Rank.Elder,
                    _ => throw new PlatformException("Unable to alter rank; invalid rank.", code: ErrorCode.Ineligible)
                })
            );
        
        // Guild leader promoted someone else
        if (altered.Rank == Rank.Leader)
            mongo
                .WithTransaction(transaction)
                .Where(query => query
                    .EqualTo(member => member.GuildId, officer.GuildId)
                    .EqualTo(member => member.AccountId, officer.AccountId)
                )
                .Update(update => update.Set(member => member.Rank, Rank.Officer));
        
        Commit(transaction);
        return altered;
    }

    private GuildMember PromoteNextInLine(Transaction transaction, string guildId, string promotedBy)
    {
        GuildMember supremeLeader = mongo
            .WithTransaction(transaction)
            .OnNoneAffected(_ =>
            {
                Log.Info(Owner.Will, "Leader left guild with no other eligible leadership, it will be deleted.", data: new
                {
                    GuildId = guildId
                });
            })
            .OnRecordsAffected(_ => Log.Info(Owner.Will, "A different member was promoted to leader.", data: new
            {
                GuildId = guildId
            }))
            .Where(query => query
                .EqualTo(member => member.GuildId, guildId)
                .LessThan(member => member.Rank, Rank.Leader)
                .GreaterThan(member => member.Rank, Rank.Applicant)
                .GreaterThanOrEqualTo(member => member.LastActive, Timestamp.OneMonthAgo)
            )
            .Sort(query => query
                .OrderByDescending(member => member.Rank)
                .ThenByDescending(member => member.JoinedOn)
            )
            .UpdateAndReturnOne(update => update
                .Set(member => member.Rank, Rank.Leader)
                .Set(member => member.UpdatedBy, promotedBy)
            );

        if (supremeLeader != null)
            return supremeLeader;
        
        // Promotion was not possible; all remaining members are inactive or otherwise ineligible to become leader.
        mongo
            .WithTransaction(transaction)
            .Where(query => query
                .EqualTo(member => member.GuildId, guildId)
                .LessThan(member => member.Rank, Rank.Leader)
            )
            .Delete();

        Require<GuildService>().Delete(transaction, guildId);
        return null;
    }

    public GuildMember[] GetRoster(string guildId, bool includeApplicants = false) => mongo
        .Where(query => query
            .EqualTo(member => member.GuildId, guildId)
            .GreaterThanOrEqualTo(member => member.Rank, includeApplicants
                ? Rank.Applicant
                : Rank.Member
            )
        )
        .Sort(sort => sort
            .OrderByDescending(member => member.Rank)
            .ThenBy(member => member.JoinedOn)
        )
        .Limit(100)
        .ToArray();

    public GuildMember[] GetInactiveLeaders() => mongo
        .Where(query => query
            .EqualTo(member => member.Rank, Rank.Leader)
            .LessThan(member => member.LastActive, Timestamp.OneMonthAgo)
        )
        .Limit(10_000)
        .ToArray();

    public void Insert(Transaction transaction, GuildMember member) => mongo
        .WithTransaction(transaction)
        .Insert(member);

    public GuildMember GetRegistration(string guildId, string accountId) => mongo
        .Where(query => query
            .EqualTo(member => member.GuildId, guildId)
            .EqualTo(member => member.AccountId, accountId)
        )
        .First();

    public void LoadAccountIds(ref Guild[] guilds)
    {
        string[] guildIds = guilds.Select(guild => guild.Id).ToArray();
        GuildMember[] all = mongo
            .Where(query => query
                .ContainedIn(member => member.GuildId, guildIds)
                .GreaterThan(member => member.Rank, Rank.Applicant)
            )
            .ToArray();

        if (all.Any())
            return;

        foreach (IGrouping<string, GuildMember> group in all.GroupBy(member => member.GuildId))
            guilds.First(guild => guild.Id == group.Key).Members = group.ToArray();
    }


    public string FindGuildIdFromToken(string accountId) => mongo
        .Where(query => query
            .EqualTo(member => member.AccountId, accountId)
            .NotEqualTo(member => member.GuildId, null)
            .NotEqualTo(member => member.Rank, Rank.Applicant)
        )
        .Sort(sort => sort.OrderBy(member => member.CreatedOn))
        .Limit(1)
        .Project(member => member.GuildId)
        .FirstOrDefault();

    public long MarkAccountsActive(Transaction transaction, params string[] accountIds) => mongo
        .WithTransaction(transaction)
        .Where(query => query.ContainedIn(member => member.AccountId, accountIds))
        .OnRecordsAffected(result => Log.Verbose(Owner.Will, $"Marked {result.Affected} accounts as active."))
        .Update(update => update.SetToCurrentTimestamp(member => member.LastActive));

    public string[] GetOutstandingApplications(string accountId) => mongo
        .Where(query => query
            .EqualTo(member => member.AccountId, accountId)
            .EqualTo(member => member.Rank, Rank.Applicant)
        )
        .Project(member => member.GuildId);

    protected override void OnElapsed() => mongo
        .Where(query => query
            .EqualTo(member => member.Rank, Rank.Applicant)
            .LessThanOrEqualTo(member => member.CreatedOn, Timestamp.TwoDaysAgo)
        )
        .Delete();
}