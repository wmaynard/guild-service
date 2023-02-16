using MongoDB.Driver;
using Rumble.Platform.Common.Services;
using Rumble.Platform.GuildService.Models;

namespace Rumble.Platform.GuildService.Services;

public class RequestService : PlatformMongoService<Request>
{
#pragma warning disable
	private readonly GuildService _guildService;
#pragma warning restore
	
	public RequestService() : base(collection: "requests") {  }

	// Fetch requests for a guild
	public List<Request> GetRequests(string guildId)
	{
		return _collection.Find(request => request.GuildId == guildId).ToList();
	}
	
	// Fetch requests for a player
	public List<Request> GetPlayerRequests(string playerId)
	{
		return _collection.Find(request => request.PlayerId == playerId).ToList();
	}

	// Accept request
	public void AcceptRequest(string requestId)
	{
		Request request = _collection.Find(request => request.Id == requestId).FirstOrDefault();

		_guildService.AddMember(playerName: request.Name, playerId: request.PlayerId, guildId: request.GuildId);
	}
	
	// Reject request
	public void RejectRequest(string requestId)
	{
		_collection.DeleteOne(request => request.Id == requestId);
	}
}