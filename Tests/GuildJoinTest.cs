using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Testing;
using Rumble.Platform.Data;
using Rumble.Platform.Guilds.Controllers;
using Rumble.Platform.Guilds.Models;

namespace Rumble.Platform.Guilds.Tests;

[TestParameters(tokens: 19, timeout: 60_000)]
[Covers(typeof(TopController), nameof(TopController.Join))]
[DependentOn(typeof(CreatePublicGuildTest))]
public class JoinGuildTest : PlatformUnitTest
{
    private Guild GuildToJoin { get; set; }
    public override void Initialize()
    {
        GetTestResults(typeof(CreatePublicGuildTest), out RumbleJson response);

        GuildToJoin = response.Require<Guild>("guild");
    }

    public override void Execute()
    {
        Guild guild = null;
        
        for (int i = 0; i < 19; i++)
        {
            TokenInfo token = TryGetToken(i);
            Assert("Token not null", token != null);
            Request(token, new RumbleJson
            {
                { "guildId", GuildToJoin.Id }
            }, out RumbleJson response, out int code);
        
            Assert("JSON returned", response != null);
            Assert("Request successful", code.Between(200, 299));
        
            guild = response.Require<Guild>("guild");
            Assert("Response has a guild in it", guild != null, abortOnFail: true);
            Assert("Guild has members", guild.Members.Any(member => member.AccountId == Token.AccountId));
        }

        Assert("Guild is full of distinct members", guild.Members.Distinct().Count() == 20);
    }

    public override void Cleanup() { }
}
