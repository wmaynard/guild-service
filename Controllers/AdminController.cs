using Microsoft.AspNetCore.Mvc;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.Guilds.Services;

namespace Rumble.Platform.Guilds.Controllers;

[ApiController, Route("guild/admin"), RequireAuth(AuthType.ADMIN_TOKEN)]
public class AdminController : PlatformController
{
    private readonly GuildService _guilds;
    
    [HttpGet]
    public ActionResult GetGuildInformation()
    {
        string guildId = Require<string>("guildId");

        return Ok(_guilds.FromId(guildId));
    }
}