using MongoDB.Driver.Linq;
using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Common.Testing;
using Rumble.Platform.Common.Utilities.JsonTools;
using Rumble.Platform.Guilds.Controllers;
using Rumble.Platform.Guilds.Models;
using Rumble.Platform.Guilds.Services;

namespace Rumble.Platform.Guilds.Tests;

[TestParameters(tokens: 1)]
[Covers(typeof(TopController), nameof(TopController.Search))]
[DependentOn(typeof(CreatePrivateGuild))]
public class ApplyToMultipleGuilds : PlatformUnitTest
{
    public GuildService _guilds;
    
    public override void Initialize() { }

    public override void Execute()
    {
        Guild[] guilds = _guilds
            .Browse()
            .Where(guild => guild.Access == AccessLevel.Private)
            .ToArray();
        Assert("There is more than 1 private guild available from browse", guilds.Length > 1);

        foreach (Guild locked in guilds)
            _guilds.Join(locked.Id, Token.AccountId);
        
        Request(Token, null, out RumbleJson response, out int code);
        Assert("JSON returned", response != null);
        Assert("Request successful", code.Between(200, 299));
        
        string[] guildIds = response.Require<string[]>("guildsAppliedTo");
        Assert($"Player has {guilds.Length} outstanding applications.", guildIds.Length == guilds.Length, abortOnFail: true);
        Assert("All outstanding applications match applications", guilds.All(guild => guildIds.Contains(guild.Id)));
    }

    public override void Cleanup() { }
}