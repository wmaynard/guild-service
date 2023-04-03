using Microsoft.AspNetCore.Mvc;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Web;
using Rumble.Platform.GuildService.Models;
using Rumble.Platform.GuildService.Services;

namespace Rumble.Platform.GuildService.Controllers;

[ApiController, Route(template: "guild")]
public class TopController : PlatformController
{
	// health handled by platform controller
#pragma warning disable
	private readonly Services.GuildService _guildService;
	private readonly RequestService        _requestService;
	private readonly HistoryService        _historyService;
#pragma warning restore
	
	// Search for player's guild
	[HttpGet, Route("player"), RequireAuth]
	public ActionResult SearchByPlayer()
	{
		string playerId = Token.AccountId;
		
		return Ok(_guildService.SearchByPlayerId(playerId));
	}
	
	// Search guilds by query
	[HttpGet, Route("search"), RequireAuth]
	public ActionResult Search(string query)
	{
		return Ok(_guildService.SearchByQuery(query));
	}
	
	// Get guild info
	[HttpGet, Route("info"), RequireAuth]
	public ActionResult Info(string guildId)
	{
		return Ok(_guildService.SearchById(guildId));
	}
	
	// Update guild info
	[HttpPatch, Route("info"), RequireAuth]
	public ActionResult InfoEdit()
	{
		Guild guild = Require<Guild>(key: "guild");
		
		string requesterId = Token.AccountId;
		Member requester = _guildService.CheckPlayer(requesterId);

		if (requester.Position == Member.Role.Member)
		{
			return Problem($"Requester {requesterId} does not have permissions to guild info for guild {guild.Id}.");
		}

		_guildService.UpdateGuild(guild);
		
		History log = new History(guildId: guild.Id, log: "Guild info was updated.", internalLog: $"Guild {guild.Id} info was updated by player {requesterId}.");
		_historyService.Create(log);

		return Ok(guild);
	}

	// Update member position
	[HttpPatch, Route("position"), RequireAuth]
	public ActionResult PositionUpdate(string playerId, Member.Role position)
	{
		string requesterId = Token.AccountId;
		Member requester = _guildService.CheckPlayer(requesterId);
		
		if (requester == null)
		{
			return Problem($"The requesting player {requesterId} is not in a guild.");
		}

		Guild guild = _guildService.SearchByPlayerId(requesterId);
		Member player = _guildService.CheckPlayer(playerId);
		
		if (requester.Position is Member.Role.Leader or Member.Role.Officer)
		{
			_guildService.UpdatePosition(playerId: playerId, position: position, guildId: guild.Id);
			
			History log = new History(guildId: guild.Id, log: $"{requesterId} changed {player.Name} to a {position}.", internalLog: $"{requesterId} changed {player.Name} to a {position} in guild {guild.Id}.");
			_historyService.Create(log);
			
			return Ok($"Position for player {playerId} has been changed to {position}.");
		}

		return Problem($"The requesting player {requesterId} does not have permissions to change permissions.");
	}
	
	// Change leader
	[HttpPatch, Route("leader"), RequireAuth]
	public ActionResult PositionLeader(string newLeaderId)
	{
		string oldLeaderId = Token.AccountId;
		Member oldLeader = _guildService.CheckPlayer(oldLeaderId);

		if (oldLeader == null)
		{
			return Problem($"The requesting player {oldLeaderId} is not in a guild.");
		}

		Guild guild = _guildService.SearchByPlayerId(oldLeaderId);

		if (oldLeader.Position == Member.Role.Leader)
		{
			_guildService.ChangeLeader(oldLeaderId: oldLeaderId, newLeaderId: newLeaderId, guildId: guild.Id);

			Member newLeader = _guildService.CheckPlayer(newLeaderId);
			
			History log = new History(guildId: guild.Id, log: $"Guild leader has been changed to {newLeader.Name}.", internalLog: $"Guild {guild.Id} leader {oldLeaderId} changed to {newLeaderId}.");
			_historyService.Create(log);

			return Ok($"Leader of guild {guild.Id} has changed from {oldLeaderId} to {newLeaderId}.");
		}

		return Problem($"The player {oldLeaderId} is not a guild leader and thus cannot change guild leader.");
	}
	
	// Leave guild
	[HttpPost, Route("leave"), RequireAuth]
	public ActionResult LeaveGuild()
	{
		string playerId = Token.AccountId;
		
		Member member = _guildService.CheckPlayer(playerId);

		if (member == null)
		{
			return Problem($"The requesting player {playerId} is not in a guild.");
		}
		
		Guild guild = _guildService.SearchByPlayerId(playerId);
		
		bool isLeader = member.Position == Member.Role.Leader;
		
		if (isLeader && guild.Members.Count != 1)
		{
			return Problem($"Player {playerId} cannot leave guild {guild.Id} without designating new leader.");
		}
		
		if (guild.Members.Count == 1)
		{
			DeleteGuild(guild.Id);
			
			History finalLog = new History(guildId: guild.Id, log: $"Guild has been disbanded.", internalLog: $"Guild {guild.Id} has been disbanded.");
			_historyService.Create(finalLog);
			
			return Ok($"Player {playerId} has disbanded guild {guild.Id}.");
		}
		
		_guildService.RemoveMember(playerId: playerId, guildId: guild.Id);
		
		History log = new History(guildId: guild.Id, log: $"{member.Name} has left the guild.", internalLog: $"Player {member.Id} has left guild {guild.Id}.");
		_historyService.Create(log);

		return Ok($"Player {playerId} has left guild {guild.Id}.");
	}
	
	// Create guild
	[HttpPost, Route(""), RequireAuth]
	public ActionResult CreateGuild(string name, string desc, Guild.GuildType type, int level)
	{
		string leaderId = Token.AccountId;
		string leaderName = Token.ScreenName;

		Guild existingGuild = _guildService.SearchByPlayerId(leaderId);

		if (existingGuild != null)
		{
			return Problem($"Player {leaderId} is already in a guild and cannot create a guild.");
		}
		
		Guild guild = new Guild(name: name, description: desc, type: type, levelRequirement: level,
		                        leaderName: leaderName, leaderId: leaderId);
		// TODO check for inappropriate names

		Guild duplicate = _guildService.FindOne(g => g.Name == name);
		if (duplicate != null)
		{
			return Problem($"Guild name {name} already exists.");
		}
			
		_guildService.Create(guild);
		
		History log = new History(guildId: guild.Id, log: $"Guild has been created.", internalLog: $"Player {leaderId} has created guild {guild.Id}.");
		_historyService.Create(log);

		return Ok($"New guild {guild} has been created by player {leaderId}.");
	}
	
	// Delete guild
	[HttpDelete, Route(""), RequireAuth]
	public ActionResult DeleteGuild(string guildId)
	{
		_guildService.Delete(guildId);
		
		History log = new History(guildId: guildId, log: $"Guild has been deleted.", internalLog: $"Guild {guildId} has been deleted.");
		_historyService.Create(log);

		return Ok($"Guild {guildId} has been deleted.");
	}
	
	// Expel member
	[HttpPost, Route("expel"), RequireAuth]
	public ActionResult ExpelGuild(string playerId)
	{
		string requesterId = Token.AccountId;
		Member requester = _guildService.CheckPlayer(requesterId);
		Member player = _guildService.CheckPlayer(playerId);

		if (requester == null)
		{
			return Problem($"The requesting player {playerId} is not in a guild.");
		}

		Guild guild = _guildService.SearchByPlayerId(requesterId);

		if (requester.Position <= player.Position)
		{
			return Problem($"Requester {requesterId} does not have permissions to expel player {playerId}.");
		}
		
		_guildService.RemoveMember(playerId: playerId, guildId: guild.Id);
		
		History log = new History(guildId: guild.Id, log: $"{player.Name} has been expelled from the guild.", internalLog: $"Player {player.Id} has been expelled from guild {guild.Id} by player {requesterId}.");
		_historyService.Create(log);

		return Ok($"Player {playerId} has been expelled from guild {guild.Id}.");
	}

	// Ban member
	[HttpPost, Route("ban"), RequireAuth]
	public ActionResult BanPlayer(string playerId)
	{
		string requesterId = Token.AccountId;
		Member requester = _guildService.CheckPlayer(requesterId);
		Member player = _guildService.CheckPlayer(playerId);
		
		if (requester == null)
		{
			return Problem($"The requesting player {playerId} is not in a guild.");
		}

		Guild guild = _guildService.SearchByPlayerId(requesterId);

		if (requester.Position <= player.Position)
		{
			return Problem($"Requester {requesterId} does not have permissions to ban player {playerId}.");
		}
		
		_guildService.BanMember(playerId: playerId, guildId: guild.Id);
		
		History log = new History(guildId: guild.Id, log: $"{player.Name} has been banned from the guild.", internalLog: $"Player {player.Id} has been banned from guild {guild.Id} by player {requesterId}.");
		_historyService.Create(log);

		return Ok($"Player {playerId} has been banned from guild {guild.Id}.");
	}
	
	// Remove ban
	[HttpPost, Route("unban"), RequireAuth]
	public ActionResult UnbanPlayer(string playerId)
	{
		string requesterId = Token.AccountId;
		Member requester = _guildService.CheckPlayer(requesterId);
		
		if (requester == null)
		{
			return Problem($"The requesting player {playerId} is not in a guild.");
		}
		
		Guild guild = _guildService.SearchByPlayerId(requesterId);

		if (requester.Position == Member.Role.Member)
		{
			return Problem($"Requester {requesterId} does not have permissions to unban player {playerId}.");
		}
		
		_guildService.UnbanMember(playerId: playerId, guildId: guild.Id);
		
		// TODO fetch name to use instead of id from player-service or another means
		History log = new History(guildId: guild.Id, log: $"{playerId} has been banned from the guild.", internalLog: $"Player {playerId} has been banned from guild {guild.Id} by player {requesterId}.");
		_historyService.Create(log);

		return Ok($"Player {playerId} has been unbanned from guild {guild.Id}.");
	}
	
	// TODO inactive leaders
	// TODO recommended guilds
}