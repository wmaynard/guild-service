using RCL.Logging;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Common.Interfaces;
using Rumble.Platform.Common.Minq;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Guilds.Exceptions;
using Rumble.Platform.Guilds.Interop;
using Rumble.Platform.Guilds.Models;

namespace Rumble.Platform.Guilds.Services;

public class GuildService : MinqService<Guild>
{
    private readonly MemberService _members;
    private EventHandler _onGuildUpdate;
    
    public GuildService(MemberService members) : base("guilds")
    {
        mongo
            .DefineIndex(builder => builder
                .Add(guild => guild.Name)
                .SetName("uniqueName")
                .EnforceUniqueConstraint()
            );
        
        mongo
            .DefineIndex(builder => builder
                .Add(guild => guild.Id)
                .Add(guild => guild.ChatRoomId)
                .SetName("uniqueChatRoom")
                .EnforceUniqueConstraint()
            );

        _members = members;
    }

    public void PerformGuildUpdateTasks(string guildId)
    {
        Guild updated = FromId(guildId);
        mongo
            .ExactId(updated.Id)
            .Update(update => update.Set(guild => guild.MemberCount, updated.Members.Length));
        
        // Has to happen last because this prunes the member count
        ChatService.TryUpdateRoom(updated);
    }

    public Guild Join(string id, string accountId)
    {
        Guild desired = FromId(id);
        if (desired.IsFull)
            throw new GuildFullException(id);

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

        PerformGuildUpdateTasks(desired.Id);

        return desired;
    }

    // TODO
    public Guild[] Browse() => mongo.All().ToArray();

    public Guild[] Search(params string[] terms)
    {
        Guild[] output = mongo
            .Where(query => query.LessThan(guild => guild.Access, AccessLevel.Private))
            .Search(terms);

        if (!output.Any())
            return output;
        
        // _members.LoadAccountIds(ref output);

        return output;
    } 

    public override Guild FromId(string id)
    {
        Guild output = base.FromId(id)
            ?? throw new PlatformException("Guild not found.", code: ErrorCode.MongoRecordNotFound);
        output.Members = _members.GetRoster(id, true);
        return output;
    }

    public Guild Create(Guild guild)
    {

        Transaction transaction = null;
        try
        {
            mongo
                .WithTransaction(out transaction)
                .Insert(guild);
            guild.Leader.GuildId = guild.Id;

            _members.Remove(transaction, guild.Leader.AccountId);
            _members.Insert(transaction, guild.Leader);
            
            
            // Copy() here is a kluge to get around the fact that this wipes out the roster list
            ChatService.Create(guild.Copy(), out ChatService.ChatRoom room);
            guild.ChatRoomId = room.Id;

            mongo
                .WithTransaction(transaction)
                .Update(guild);
        
            Commit(transaction);
        }
        catch
        {
            Log.Error(Owner.Will, "Failed to create guild; attempting rollback of chat room.");
            ChatService.Delete(guild);
            Abort(transaction);
            throw;
        }
        
        return guild;
    }

    public void Delete(Transaction transaction, string guildId) => mongo
        .WithTransaction(transaction)
        .ExactId(guildId)
        .Delete();

    public Guild ModifyDetails(Guild guild, string officerId)
    {
        GuildMember officer = _members.GetRegistration(guild.Id, officerId);

        if (officer.Rank < Rank.Officer)
            throw new PlatformException("Member is not an officer.", code: ErrorCode.Unauthorized);

        return mongo
            .ExactId(guild.Id)
            .Limit(1)
            .UpdateAndReturnOne(update => update
                .Set(db => db.Name, guild.Name)
                .Set(db => db.Language, guild.Language)
                .Set(db => db.Region, guild.Region)
                .Set(db => db.Access, guild.Access)
                .Set(db => db.RequiredLevel, guild.RequiredLevel)
                .Set(db => db.Description, guild.Description)
                .Set(db => db.IconData, guild.IconData)
            )
            ?? throw new PlatformException("Unable to update guild details.");
    }

    public string[] FindGuildsInNeedOfSync(int limit) => mongo
        .Where(query => query.LessThanOrEqualTo(guild => guild.LastChatSync, Timestamp.OneHourAgo))
        .Limit(limit)
        .UpdateAndReturn(update => update.SetToCurrentTimestamp(guild => guild.LastChatSync))
        .Select(guild => guild.Id)
        .ToArray();
}