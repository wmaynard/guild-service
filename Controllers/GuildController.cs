using Microsoft.AspNetCore.Mvc;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Web;
using Rumble.Platform.GuildService.Models;
using Rumble.Platform.GuildService.Services;

namespace Rumble.Platform.GuildService.Controllers;

[ApiController, Route(template: "guilds"), RequireAuth]
public class GuildController : PlatformController
{
#pragma warning disable
	private readonly Services.GuildService _guildService;
	private readonly RequestService        _requestService;
	private readonly HistoryService        _historyService;
#pragma warning restore
	
	// Search for player's guild
	[HttpGet, Route("player")]
	public ActionResult SearchByPlayer(string playerId)
	{
		return Ok(_guildService.SearchByPlayerId(playerId));
	}
	
	// Search guilds by query
	[HttpGet, Route("search")]
	public ActionResult Search(string query)
	{
		return Ok(_guildService.SearchByQuery(query));
	}
	
	// Get guild info
	[HttpGet, Route("info")]
	public ActionResult Info(string guildId)
	{
		return Ok(_guildService.SearchById(guildId));
	}
	
	// Update guild info
	[HttpPatch, Route("info")]
	public ActionResult InfoEdit(Guild guild)
	{
		_guildService.UpdateGuild(guild);

		return Ok(guild);
	}
	
	// View guild requests
	[HttpGet, Route("requests")]
	public ActionResult Requests(string guildId)
	{
		return Ok(_requestService.GetRequests(guildId));
	}
	
	// Accept guild request
	[HttpPost, Route("requests/accept")]
	public ActionResult RequestsAccept(string requestId)
	{
		Request request = _requestService.Get(requestId);
		_requestService.AcceptRequest(requestId);
		_guildService.AddMember(playerName: request.Name, playerId: request.PlayerId, guildId: request.GuildId);
		
		return Ok(message: $"Request {requestId} has been accepted.");
	}
	
	// Reject guild request
	[HttpPost, Route("requests/reject")]
	public ActionResult RequestsReject(string requestId)
	{
		_requestService.RejectRequest(requestId);

		return Ok(message: $"Request {requestId} has been rejected.");
	}
	
	// Send guild request
	[HttpPost, Route("requests")]
	public ActionResult RequestsSend(string name, string desc, string guildId, string playerId, int level)
	{
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
	
	// Update member position
	[HttpPatch, Route("position")]
	public ActionResult PositionUpdate(string playerId, Member.Role position, string guildId)
	{
		_guildService.UpdatePosition(playerId: playerId, position: position, guildId: guildId);

		return Ok($"Player {playerId} has been changed to be a {position}.");
	}
	
	// Change leader
	[HttpPatch, Route("leader")]
	public ActionResult PositionLeader(string oldLeaderId, string newLeaderId, string guildId)
	{
		_guildService.ChangeLeader(oldLeaderId: oldLeaderId, newLeaderId: newLeaderId, guildId: guildId);

		return Ok($"Leader of guild {guildId} has changed from {oldLeaderId} to {newLeaderId}.");
	}
	
	// Leave guild
	[HttpPost, Route("leave")]
	public ActionResult LeaveGuild(string playerId, string guildId)
	{
		Guild guild = _guildService.SearchById(guildId);
		Member member = guild.Members.Find(member => member.Id == playerId);
		bool isLeader = member?.Position == Member.Role.Leader;
		
		if (isLeader && guild.Members.Count() != 1)
		{
			return Problem($"Player {playerId} cannot leave guild {guildId} without designating new leader.");
		}
		
		_guildService.RemoveMember(playerId: playerId, guildId: guildId);

		return Ok($"Player {playerId} has left guild {guildId}.");
	}
	
	// Create guild
	[HttpPost, Route("")]
	public ActionResult CreateGuild(string name, string desc, Guild.GuildType type, int level, string leaderName,
	                                string leaderId)
	{
		Guild guild = new Guild(name: name, description: desc, type: type, levelRequirement: level,
		                        leaderName: leaderName, leaderId: leaderId);
		// TODO check for inappropriate names
		
		_guildService.Create(guild);

		return Ok($"New guild {guild} has been created by player {leaderId}.");
	}
	
	// Delete guild
	[HttpDelete, Route("")]
	public ActionResult DeleteGuild(string guildId)
	{
		_guildService.Delete(guildId);

		return Ok($"Guild {guildId} has been deleted.");
	}
	
	// Expel member
	[HttpPost, Route("expel")]
	public ActionResult ExpelGuild(string playerId, string guildId)
	{
		_guildService.RemoveMember(playerId: playerId, guildId: guildId);

		return Ok($"Player {playerId} has been expelled from guild {guildId}.");
	}

	// Ban member
	[HttpPost, Route("ban")]
	public ActionResult BanPlayer(string playerId, string guildId)
	{
		_guildService.BanMember(playerId: playerId, guildId: guildId);

		return Ok($"Player {playerId} has been banned from guild {guildId}.");
	}
	
	// Remove ban
	[HttpPost, Route("unban")]
	public ActionResult UnbanPlayer(string playerId, string guildId)
	{
		_guildService.UnbanMember(playerId: playerId, guildId: guildId);

		return Ok($"Player {playerId} has been unbanned from guild {guildId}.");
	}
	
	// TODO inactive leaders
	// TODO recommended guilds
}