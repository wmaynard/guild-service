using MongoDB.Driver;
using Rumble.Platform.Common.Services;
using Rumble.Platform.GuildService.Models;

namespace Rumble.Platform.GuildService.Services;

public class HistoryService : PlatformMongoService<History>
{
	public HistoryService() : base(collection: "history") {  }
	
	// Fetch history for a guild
	public List<History> GetHistory(string guildId)
	{
		return _collection.Find(history => history.GuildId == guildId).ToList();
	}
}