using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Minq;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Guilds.Models;

namespace Rumble.Platform.Guilds.Services;

public class GuildService : MinqService<Guild>
{
    private readonly MemberService _members;
    
    public GuildService(MemberService members) : base("guilds")
    {
        mongo
            .DefineIndex(builder => builder
                .Add(guild => guild.Name)
                .EnforceUniqueConstraint()
            );

        _members = members;
    }

    public Guild Join(string id, string accountId)
    {
        Guild desired = FromId(id);

        GuildMember registrant = new()
        {
            AccountId = accountId,
            GuildId = desired.Id,
            JoinedOn = Timestamp.Now,
            Rank = desired.Access switch
            {
                AccessLevel.Public => Rank.Member,
                AccessLevel.Closed => Rank.Applicant,
                AccessLevel.Private => throw new PlatformException("Joining this guild requires an invitation.", code: ErrorCode.Unauthorized),
                _ => throw new PlatformException("Invalid guild type.", code: ErrorCode.InvalidRequestData)
            }
        };
        
        _members.Insert(registrant);

        desired.Members = desired
            .Members
            .Union(new[] { registrant })
            .ToArray();

        return desired;
    }

    public Guild[] Search(params string[] terms) => mongo
        .All()
        .Limit(100)
        .ToArray();

    public override Guild FromId(string id)
    {
        Guild output = base.FromId(id)
            ?? throw new PlatformException("Guild not found.", code: ErrorCode.MongoRecordNotFound);
        output.Members = _members.GetRoster(id, true);
        return output;
    }

    public Guild Create(Guild guild, string leaderId)
    {
        Insert(guild);
        // TODO: Transactions
        try
        {
            _members.Leave(leaderId);
        }
        catch (PlatformException e) // TODO: Custom exceptions
        {
            if (e.Code != ErrorCode.MongoRecordNotFound)
                throw;
        }

        GuildMember leader = new()
        {
            AccountId = leaderId,
            GuildId = guild.Id,
            JoinedOn = Timestamp.Now,
            Rank = Rank.Leader
        };
        _members.Insert(leader);
        
        guild.Members = new [] { leader };
        return guild;
    }

    public void Delete(Transaction transaction, string guildId) => mongo
        .WithTransaction(transaction)
        .ExactId(guildId)
        .Delete();
}