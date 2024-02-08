using MongoDB.Bson.Serialization.Attributes;
using RCL.Logging;
using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Common.Minq;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Data;
using Rumble.Platform.Guilds.Models;
using Rumble.Platform.Guilds.Services;

namespace Rumble.Platform.Guilds.Interop;

public class ChatService : MinqTimerService<ChatService.UpdateRetry>
{
    private readonly GuildService _guilds;
    
    public ChatService(GuildService guilds) : base("chat_retries", IntervalMs.OneHour) => _guilds = guilds;

    public class UpdateRetry : PlatformCollectionDocument
    {
        public string GuildId { get; set; }
        public string RoomId { get; set; }
        public long Attempts { get; set; }
        
        [BsonIgnore]
        public bool ShouldDelete { get; set; }
        
        [BsonIgnore]
        public bool ShouldIncrement { get; set; }
    }

    protected override void OnElapsed()
    {
        UpdateRetry[] retries = mongo
            .Where(query => query.LessThan(db => db.Attempts, 5))
            .Sort(sort => sort.OrderBy(retry => retry.CreatedOn))
            .Limit(100)
            .ToArray();

        foreach (UpdateRetry retry in retries)
        {
            try
            {
                bool success = string.IsNullOrWhiteSpace(retry.GuildId) 
                    ? Delete(new Guild { ChatRoomId = retry.RoomId }) 
                    : Update(_guilds.FromId(retry.GuildId), out _);

                if (success)
                    retry.ShouldDelete = true;
                else
                    retry.ShouldIncrement = true;
            }
            catch (Exception e)
            {
                Log.Error(Owner.Will, "Unexpected problem encountered when retrying a chat-service update", exception: e);
            }
        }

        mongo
            .Where(query => query.ContainedIn(retry => retry.Id, retries.Where(retry => retry.ShouldDelete).Select(retry => retry.Id)))
            .Delete();
        mongo
            .Where(query => query.ContainedIn(retry => retry.Id, retries.Where(retry => retry.ShouldIncrement).Select(retry => retry.Id)))
            .Update(update => update.Increment(retry => retry.Attempts));
    }
    
    private static ApiRequest Request(string url) => ApiService.Instance?
        .Request(url)
        .AddAuthorization(DynamicConfig.Instance.AdminToken);
    
    private bool CreatePrivateRoom(Guild guild, out ChatRoom room)
    {
        string[] accountIds = guild.Members.Select(member => member.AccountId).ToArray();
        guild.Members = null;
        ChatRoom output = null;
        Request("/chat/admin/rooms/new")
            .SetPayload(new RumbleJson
            {
                { "accountIds", accountIds },
                { "data", new RumbleJson
                {
                    { "guild", guild }
                }}
            })
            .OnFailure(response => Log.Error(Owner.Will, "Unable to create guild room.", data: new
            {
                Response = response
            }))
            .OnSuccess(response =>
            {
                output = response.Require<ChatRoom>("room");
            })
            .Post(out _, out int code);

        room = output;
        return code.Between(200, 299);
    }
    private bool UpdatePrivateRoom(Guild guild, out ChatRoom room)
    {
        string[] accountIds = guild
            .Members
            .Where(member => member.Rank > Rank.Applicant)
            .Select(member => member.AccountId)
            .ToArray();
        guild.Members = null;
        ChatRoom output = null;
        Request("/chat/admin/rooms/update")
            .SetPayload(new RumbleJson
            {
                { "roomId", guild.ChatRoomId },
                { "accountIds", accountIds }
                // { "data", new RumbleJson
                // {
                //     { "guild", guild }
                // }}
            })
            .OnFailure(response =>
            {
                Log.Error(Owner.Will, "Unable to modify guild room.", data: new
                {
                    Response = response
                });
                mongo.Insert(new UpdateRetry
                {
                    GuildId = guild.Id,
                    RoomId = guild.ChatRoomId
                });
            })
            .OnSuccess(response =>
            {
                Log.Local(Owner.Will, "Updated guild room");
                output = response.Require<ChatRoom>("room");
            })
            .Patch(out _, out int code);

        room = output;
        return code.Between(200, 299);
    }

    private bool DeletePrivateRoom(Guild guild)
    {
        guild.Members = null;
        
        Request("/chat/admin/rooms/new")
            .SetPayload(new RumbleJson
            {
                { "roomId", guild.ChatRoomId },
                { "accountIds", Array.Empty<string>() },
                { "data", new RumbleJson
                {
                    { "guild", guild }
                }},
                { "channel", 2 } // 0 is None, 1 is Global, 2 is Guild
            })
            .OnFailure(response => Log.Error(Owner.Will, "Unable to delete guild room.", data: new
            {
                Response = response
            }))
            .OnSuccess(_ => Log.Info(Owner.Will, "Chat room deleted."))
            .Post(out _, out int code);
        
        return code.Between(200, 299);
    }

    public class ChatRoom : PlatformDataModel
    {
        public RumbleJson Data { get; set; }
        public string[] Members { get; set; }
        public string Type { get; set; }
        public string Id { get; set; }
        public long CreatedOn { get; set; }
    }

    public static bool Create(Guild guild, out ChatRoom room) => Require<ChatService>().CreatePrivateRoom(guild, out room);
    public static bool Update(Guild guild, out ChatRoom room) => Require<ChatService>().UpdatePrivateRoom(guild, out room);
    public static void TryUpdateRoom(string guildId) => Task.Run(() =>
    {
         try
         {
             Update(Require<GuildService>().FromId(guildId), out _);
         }
         catch (Exception e)
         {
             Log.Error(Owner.Will, "Unable to update guild room asynchronously.", data: new
             {
                 GuildId = guildId
             }, exception: e);
         }
    });
    public static void TryUpdateRoom(Guild guild) => Task.Run(() =>
    {
        try
        {
            Update(guild, out _);
        }
        catch (Exception e)
        {
            Log.Error(Owner.Will, "Unable to update guild room asynchronously.", data: new
            {
                GuildId = guild.Id
            }, exception: e);
        }
    });
    public static bool Delete(Guild guild) => Require<ChatService>().DeletePrivateRoom(guild);
}