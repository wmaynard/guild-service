using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Minq;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.GuildService.Models;

namespace Rumble.Platform.GuildService.Services;

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

    public GuildMember Join(string id, string accountId)
    {
        Guild desired = mongo.ExactId(id).First();

        return _members.Register(new GuildMember
        {
            AccountId = accountId,
            GuildId = desired.Id,
            Rank = desired.Type switch
            {
                GuildType.Open => Rank.Member,
                GuildType.Closed => Rank.Applicant,
                GuildType.Private => throw new PlatformException("Joining this guild requires an invitation.", code: ErrorCode.Unauthorized),
                _ => throw new PlatformException("Invalid guild type.", code: ErrorCode.InvalidRequestData)
            }
        });
    }

    public Guild[] Search(params string[] terms) => mongo
        .All()
        .Limit(100)
        .ToArray();

    public override Guild FromId(string id)
    {
        Guild output = base.FromId(id);
        output.Members = _members.GetRoster(id);
        return output;
    }

    public Guild Create(Guild guild, string leaderId)
    {
        Insert(guild);
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
}