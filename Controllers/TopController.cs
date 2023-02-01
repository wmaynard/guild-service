using Microsoft.AspNetCore.Mvc;
using Rumble.Platform.Common.Web;

namespace Rumble.Platform.GuildService.Controllers;

[ApiController, Route(template: "guild")]
public class TopController : PlatformController
{
	// health handled by platform controller
}