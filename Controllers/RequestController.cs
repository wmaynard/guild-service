using Microsoft.AspNetCore.Mvc;
using RCL.Logging;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.GuildService.Models;
using Rumble.Platform.GuildService.Services;

namespace Rumble.Platform.GuildService.Controllers;

[ApiController, Route(template: "guild/requests"), RequireAuth]
public class RequestController : PlatformController
{
#pragma warning disable
	private readonly Services.GuildService _guildService;
	private readonly RequestService        _requestService;
	private readonly HistoryService        _historyService;
#pragma warning restore
	
	// View guild requests
	[HttpGet, Route("")]
	public ActionResult Requests()
	{
		string playerId = Token.AccountId;
		Member player = _guildService.CheckPlayer(playerId);
		
		if (player == null) // not in guild, view own requests
		{
			return Ok(_requestService.GetPlayerRequests(playerId));
		}
		
		Guild guild = _guildService.SearchByPlayerId(playerId);

		if (player.Position == Member.Role.Member)
		{
			return Problem($"Player {playerId} does not have permissions to view requests.");
		}
		
		return Ok(_requestService.GetRequests(guild.Id));
	}
	
	// Accept guild request
	[HttpPost, Route("accept")]
	public ActionResult RequestsAccept(string requestId)
	{
		string playerId = Token.AccountId;
		Member player = _guildService.CheckPlayer(playerId);
		
		if (player.Position == Member.Role.Member)
		{
			return Problem($"Player {playerId} does not have permissions to accept requests.");
		}
		
		Guild guild = _guildService.SearchByPlayerId(playerId);

		Request request = _requestService.Get(requestId);

		if (guild.Id != request.GuildId) // mismatched guild
		{
			Log.Error(owner: Owner.Nathan, message: "A player attempted to accept a request from a different guild.", data: $"Player ID: {playerId}. Guild ID: {guild.Id}. Request ID: {requestId}.");
			return Problem($"Player {playerId} attempted to accept a request {requestId} from a different guild.");
		}
		
		_requestService.AcceptRequest(requestId);
		_guildService.AddMember(playerName: request.Name, playerId: request.PlayerId, guildId: request.GuildId);
		
		History log = new History(guildId: guild.Id, log: $"{request.Name} has been accepted into the guild.", internalLog: $"Player {playerId} has accepted {request.PlayerId} into guild {guild.Id}.");
		_historyService.Create(log);
		
		return Ok(message: $"Request {requestId} has been accepted.");
	}
	
	// Reject guild request
	[HttpPost, Route("reject")]
	public ActionResult RequestsReject(string requestId)
	{
		string playerId = Token.AccountId;
		Member player = _guildService.CheckPlayer(playerId);
		
		if (player.Position == Member.Role.Member)
		{
			return Problem($"Player {playerId} does not have permissions to reject requests.");
		}
		
		Guild guild = _guildService.SearchByPlayerId(playerId);

		Request request = _requestService.Get(requestId);

		if (guild.Id != request.GuildId) // mismatched guild
		{
			Log.Error(owner: Owner.Nathan, message: "A player attempted to reject a request from a different guild.", data: $"Player ID: {playerId}. Guild ID: {guild.Id}. Request ID: {requestId}.");
			return Problem($"Player {playerId} attempted to reject a request {requestId} from a different guild.");
		}
		
		_requestService.RejectRequest(requestId);

		return Ok(message: $"Request {requestId} has been rejected.");
	}
	
	// Send guild request
	[HttpPost, Route("")]
	public ActionResult RequestsSend(string name, string desc, string guildId, int level)
	{
		string playerId = Token.AccountId;
		Guild existingGuild = _guildService.SearchByPlayerId(playerId);
		if (existingGuild != null)
		{
			return Problem($"Player {playerId} is already part of a guild {existingGuild.Id}.");
		}

		bool banned = _guildService.CheckBan(playerId: playerId, guildId: guildId);

		if (banned)
		{
			return Problem($"Player {playerId} is banned from guild {guildId}.");
		}

		Guild guild = _guildService.SearchById(guildId);

		if (guild.Type == Guild.GuildType.Public)
		{
			_guildService.AddMember(playerName: name, playerId: playerId, guildId: guildId);
			// TODO after adding member limits, change message if full
			return Ok($"Member has been accepted into guild {guildId}.");
		}

		if (guild.Type == Guild.GuildType.Private)
		{
			Request request = new Request(name: name, description: desc, guildId: guildId, playerId: playerId, level: level);
			_requestService.Create(request);

			return Ok($"Request created for guild {guildId}");
		}
		
		return Problem($"Request was not created for guild {guildId} as guild is closed.");
	}
}