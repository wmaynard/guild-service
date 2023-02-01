using Microsoft.AspNetCore.Mvc;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;

namespace Rumble.Platform.GuildService.Controllers;

[ApiController, Route(template: "guild/admin"), RequireAuth(AuthType.ADMIN_TOKEN)]
public class AdminController : PlatformController
{
#pragma warning disable
	private readonly Services.GuildService _guildService;
#pragma warning restore
	
	// TODO future admin functionality
	
}