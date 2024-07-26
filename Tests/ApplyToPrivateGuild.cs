using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Common.Testing;
using Rumble.Platform.Common.Utilities.JsonTools;
using Rumble.Platform.Guilds.Controllers;
using Rumble.Platform.Guilds.Models;
using Rumble.Platform.Guilds.Services;
using Rumble.Platform.Guilds.Tests.Helpers;

namespace Rumble.Platform.Guilds.Tests;

[TestParameters(tokens: 2)]
[Covers(typeof(TopController), nameof(TopController.Join))]
[DependentOn(typeof(CreatePrivateGuild))]
public class ApplyToPrivateGuild : PlatformUnitTest
{
    private MemberService _members;
    private Guild Guild { get; set; }
    public override void Initialize()
    {
        GetTestResults(typeof(CreatePrivateGuild), out RumbleJson response);

        Guild = response.Require<Guild>("guild");
    }

    public override void Execute()
    {
        Request(Token, new RumbleJson
        {
            { "guildId", Guild.Id }
        }, out RumbleJson response, out int code);
        Assert("First request successful", code.Between(200, 299));
        
        Request(Token2, new RumbleJson
        {
            { "guildId", Guild.Id }
        }, out response, out code);
        Assert("Second request successful", code.Between(200, 299));

        ChatRoomFetcher.GetGuildChatRoomsForUser(Token, out ChatRoom[] rooms);
        Assert("First applicant cannot see any guild chat rooms", rooms.Length == 0);
        ChatRoomFetcher.GetGuildChatRoomsForUser(Token2, out rooms);
        Assert("Second applicant cannot see any guild chat rooms", rooms.Length == 0);
        
        Guild.Members = _members.GetRoster(Guild.Id, includeApplicants: true);
        
        Assert("Guild members contains the first applicant", Guild.Members.Any(member => member.AccountId == Token.AccountId), abortOnFail: true);
        Assert("The first applicant is of Applicant rank", Guild.Members.First(member => member.AccountId == Token.AccountId).Rank == Rank.Applicant);
        Assert("Guild members contains the second applicant", Guild.Members.Any(member => member.AccountId == Token2.AccountId), abortOnFail: true);
        Assert("The second applicant is of Applicant rank", Guild.Members.First(member => member.AccountId == Token2.AccountId).Rank == Rank.Applicant);
    }

    public override void Cleanup() { }
}