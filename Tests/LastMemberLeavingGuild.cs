using System.Net.NetworkInformation;
using System.Text.Json.Serialization;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Testing;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Data;
using Rumble.Platform.Guilds.Controllers;
using Rumble.Platform.Guilds.Models;
using Rumble.Platform.Guilds.Services;
using Rumble.Platform.Guilds.Tests.Helpers;
using ThirdParty.Json.LitJson;

namespace Rumble.Platform.Guilds.Tests;

[TestParameters(tokens: 0)]
[Covers(typeof(TopController), nameof(TopController.Leave))]
[DependentOn(typeof(CreateSecondPrivateGuild))]
public class LastMemberLeavingGuild : PlatformUnitTest
{
    private MemberService _members;
    private GuildService _guilds;
    private Guild Guild { get; set; }
    public override void Initialize()
    {
        GetTestResults(typeof(CreateSecondPrivateGuild), out RumbleJson response);

        Guild = response.Require<Guild>("guild");
    }

    public override void Execute()
    {
        string token = GenerateStandardToken(Guild.Leader.AccountId, Audience.GuildService | Audience.ChatService);

        ChatRoomFetcher.GetGuildChatRoomsForUser(token, out ChatRoom[] rooms);
        Assert("Only one guild chat room for user", rooms.Length == 1);
        
        Guild.Members = _members.GetRoster(Guild.Id);
        Assert("Guild only has one user in it", Guild.Members.Length == 1);
        
        Request(token, out RumbleJson response, out int code);
        Assert("Request successful", code.Between(200, 299));

        Guild[] searchResults = _guilds.Search(Guild.Id);
        Assert("Guild has been deleted", searchResults.Length == 0);

        ChatRoomFetcher.GetGuildChatRoomsForUser(token, out rooms);
        Assert("User cannot see a guild chat room anymore", rooms.Length == 0);
    }

    public override void Cleanup() { }
}