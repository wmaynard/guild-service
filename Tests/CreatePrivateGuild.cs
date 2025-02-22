using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Testing;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Utilities.JsonTools;
using Rumble.Platform.Guilds.Controllers;
using Rumble.Platform.Guilds.Models;

namespace Rumble.Platform.Guilds.Tests;

[TestParameters(tokens: 1, repetitions: 10, timeout: 30_000, abortOnFailedAssert: false)]
[Covers(typeof(TopController), nameof(TopController.Create))]
public class CreatePrivateGuild : PlatformUnitTest
{
    public override void Initialize() { }

    public override void Execute()
    {
        Request(DynamicConfig.Instance.AdminToken, new RumbleJson
        {
            { TokenInfo.FRIENDLY_KEY_ACCOUNT_ID, Token.AccountId },
            { "guild", new Guild
            {
                Name = $"TestPrivateGuild-{TimestampMs.Now}",
                Language = "en-US",
                Region = "us",
                Access = AccessLevel.Private,
                RequiredLevel = 50,
                Description = "This is a test guild and should be ignored.",
            
            }}
        }, out RumbleJson response, out int code);
    
        Assert("JSON returned", response != null);
        Assert("Request successful", code.Between(200, 299));

        Guild guild = response.Require<Guild>("guild");
        Assert("Guild not null", guild != null, abortOnFail: true);
        Assert("Guild has members", guild.Members.Any(member => member.AccountId == Token.AccountId && member.Rank == Rank.Leader));
        Assert("Guild only has one member", guild.MemberCount == 1);
        Assert("Guild has an assigned chat room", !string.IsNullOrWhiteSpace(guild.ChatRoomId));
        Log.Local(Owner.Will, $"{guild.Id} {Token.AccountId}");
    }

    public override void Cleanup() { }
}