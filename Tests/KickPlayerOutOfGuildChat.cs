using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Testing;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Data;
using Rumble.Platform.Guilds.Controllers;
using Rumble.Platform.Guilds.Models;
using Rumble.Platform.Guilds.Services;

namespace Rumble.Platform.Guilds.Tests;

[TestParameters(tokens: 0)]
[Covers(typeof(TopController), nameof(TopController.Kick))]
[DependentOn(typeof(JoinGuildTest))]
public class KickPlayerOutOfGuildChat : PlatformUnitTest
{
    private Guild TestGuild { get; set; }
    private DynamicConfig _config;
    private GuildService _guilds;
    private MemberService _members;
    private ApiService _api;
    
    public override void Initialize()
    {
        GetTestResults(typeof(CreatePublicGuildTest), out RumbleJson response);

        TestGuild = response.Require<Guild>("guild");
        _config = PlatformService.Require<DynamicConfig>();
        _guilds = PlatformService.Require<GuildService>();
        _members = PlatformService.Require<MemberService>();
        _api = PlatformService.Require<ApiService>();
    }

    public override void Execute()
    {
        TestGuild.Members = _members.GetRoster(TestGuild.Id);

        int memberCount = TestGuild.Members.Length;
        Assert("Guild has more than the leader in it", memberCount > 1);

        string victimAccountId = TestGuild
            .Members
            .OrderBy(member => member.Rank)
            .First()
            .AccountId;
        string userToken = _api.GenerateToken(
            accountId: victimAccountId,
            screenname: "Doesn't Matter",
            email: "foo@example.com",
            discriminator: 1234,
            audiences: Audience.GuildService | Audience.ChatService
        );

        GetGuildChatRoomsForUser(userToken, out RumbleJson[] rooms);
        Assert("Victim can see guild chat before kick", rooms.Any(room => room.Require<string>("id") == TestGuild.ChatRoomId));

        GetGuildChatRoom(out string[] members);
        Assert("Chat room contains the member we want to kick", members.Contains(victimAccountId));

        _members.Remove(victimAccountId, kickedBy: TestGuild.Leader.AccountId);
        TestGuild.Members = _members.GetRoster(TestGuild.Id);
        Assert("Member has been removed from the guild.", TestGuild.Members.All(member => member.AccountId != victimAccountId));

        GetGuildChatRoom(out members);
        Assert("chat-service has removed the member from the guild chat room.", !members.Contains(victimAccountId));

        GetGuildChatRoomsForUser(userToken, out rooms);
        Assert("Victim can no longer see guild chat", rooms.All(room => room.Require<string>("id") != TestGuild.ChatRoomId));
    }

    public RumbleJson GetGuildChatRoomsForUser(string token, out RumbleJson[] rooms)
    {
        _api
            .Request("chat")
            .AddParameter("lastRead", Timestamp.FifteenMinutesAgo.ToString())
            .AddAuthorization(token)
            .Get(out RumbleJson chatResponse);

        rooms = chatResponse.Require<RumbleJson[]>("roomUpdates");
        return chatResponse;
    }
    
    public RumbleJson GetGuildChatRoom(out string[] members)
    {
        _api
            .Request($"/chat/admin/rooms")
            .AddParameter("roomId", TestGuild.ChatRoomId)
            .AddAuthorization(_config.AdminToken)
            .OnSuccess(_ => { })
            .OnFailure(_ => Fail("Could not find chat room for test guild."))
            .Get(out RumbleJson response);

        RumbleJson[] rooms = response.Require<RumbleJson[]>("rooms");
        Assert("Only one room returned", rooms.Length == 1, abortOnFail: true);

        members = rooms.First().Require<string[]>("members");
        return response;
    }

    public override void Cleanup()
    {
        
    }
}