using Microsoft.AspNetCore.Mvc;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Web;
using Rumble.Platform.GuildService.Services;

namespace Rumble.Platform.GuildService.Controllers;

[ApiController, Route(template: "guild"), RequireAuth]
public class GuildController : PlatformController
{
#pragma warning disable
	private readonly Services.GuildService _guildService;
	private readonly RequestService        _requestService;
	private readonly HistoryService        _historyService;
#pragma warning restore
	
	// Search guilds by query
	
	// Get guild info
	
	// Update guild info
	
	// View guild requests
	
	// Accept guild request
	
	// Reject guild request
	
	// Send guild request
	
	// Update member position
	
	// Leave guild
	
	// Create guild
	
	// Delete guild
	
	// Expel member
	
	// Ban member
	
	// Remove ban
	
	// TODO inactive leaders
	// TODO recommended guilds
}