using RCL.Logging;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Interop;
using Rumble.Platform.Common.Minq;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.Guilds.Filters;
using Rumble.Platform.Guilds.Services;
using Rumble.Platform.Guilds.Models;

namespace Rumble.Platform.Guilds;

public class Startup : PlatformStartup
{
    protected override PlatformOptions ConfigureOptions(PlatformOptions options) => options
        .SetProjectOwner(Owner.Will)
        .SetTokenAudience(Audience.GuildService)
        .SetRegistrationName("Guild Service")
        .DisableFeatures(CommonFeature.ConsoleObjectPrinting)
        .SetPerformanceThresholds(warnMS: 5_000, errorMS: 20_000, criticalMS: 300_000)
        .AddFilter<ActivityFilter>()
        .WipeLocalDatabasesOnStartup()
        .OnReady(_ => { });
}