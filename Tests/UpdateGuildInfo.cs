using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Testing;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Data;
using Rumble.Platform.Guilds.Controllers;
using Rumble.Platform.Guilds.Models;
using Rumble.Platform.Guilds.Services;

namespace Rumble.Platform.Guilds.Tests;

[TestParameters(tokens: 0)]
[Covers(typeof(TopController), nameof(TopController.EditGuild))]
[DependentOn(typeof(CreatePublicGuild), typeof(CreatePrivateGuild), typeof(Search))]
public class UpdateGuildInfo : PlatformUnitTest
{
    private GuildService _guilds;
    private Guild Guild { get; set; }
    public override void Initialize()
    {
        GetTestResults(typeof(CreatePublicGuild), out RumbleJson response);

        Guild = response.Require<Guild>("guild");
    }

    public override void Execute()
    {
        Guild.Name = "Something Inappropriate";
        Guild.Description = $"Hello, Midgar!  I'm updating this at Unix Time {Timestamp.Now}.";
        Guild.Region = "Another Region";
        Guild.Access = AccessLevel.Private;
        Guild.RequiredLevel = new Random().Next(0, 100);
        Guild.IconData = new RumbleJson
        {
            { "foo", "bar" }
        };

        string token = GenerateStandardToken(Guild.Leader.AccountId);
        Request(token, new RumbleJson
        {
            { "guild", Guild }
        }, out RumbleJson response, out int code);
        Assert("JSON returned", response != null);
        Assert("Request successful", code.Between(200, 299));

        Guild fromDb = _guilds.FromId(Guild.Id);
        Assert("Guild name not updated", fromDb.Name != Guild.Name);
        Assert("Guild description updated", fromDb.Description == Guild.Description);
        Assert("Guild region updated", fromDb.Region == Guild.Region);
        Assert("Guild access updated", fromDb.Access == Guild.Access);
        Assert("Guild required level updated", fromDb.RequiredLevel == Guild.RequiredLevel);
        Assert("Guild icon updated", fromDb.IconData == Guild.IconData);
    }

    public override void Cleanup() { }
}