using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Testing;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Data;
using Rumble.Platform.Guilds.Controllers;
using Rumble.Platform.Guilds.Models;

namespace Rumble.Platform.Guilds.Tests;

[TestParameters(tokens: 1, repetitions: 0, timeout: 30_000, abortOnFailedAssert: false)]
[Covers(typeof(TopController), nameof(TopController.Search))]
[DependentOn(typeof(CreatePublicGuild), typeof(CreatePrivateGuild))]
public class Search : PlatformUnitTest
{
    public override void Initialize() { }

    public override void Execute()
    {
        Request(Token, null, out RumbleJson response, out int code);
        Assert("JSON returned", response != null, abortOnFail: true);
        Assert("Request successful", code.Between(200, 299));

        Guild[] results = response.Require<Guild[]>("guilds");
        Assert("At least 2 Guilds returned", results.Length > 1);
        Assert("At least one public guild returned", results.Any(guild => guild.Access == AccessLevel.Public));
        Assert("At least one private guild returned", results.Any(guild => guild.Access == AccessLevel.Private));
        Assert("No invite only guilds returned", results.All(guild => guild.Access != AccessLevel.InviteOnly));
        Assert("All guilds have a member count greater than 0", results.All(guild => guild.MemberCount > 0));

        string[] guildsAppliedTo = response.Optional<string[]>("guildsAppliedTo");
        Assert("Response contains an array of guilds applied to", guildsAppliedTo != null);
    }

    public override void Cleanup() { }
}