using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Data;

namespace Rumble.Platform.GuildService.Models;

public class History : PlatformCollectionDocument
{
	internal const string DB_KEY_GUILD_ID     = "gid";
	internal const string DB_KEY_LOG          = "log";
	internal const string DB_KEY_INTERNAL_LOG = "int";
	internal const string DB_KEY_TIMESTAMP    = "ts";

	public const string FRIENDLY_KEY_GUILD_ID     = "guildId";
	public const string FRIENDLY_KEY_LOG          = "log";
	public const string FRIENDLY_KEY_INTERNAL_LOG = "internalLog";
	public const string FRIENDLY_KEY_TIMESTAMP    = "timestamp";
	
	[SimpleIndex]
	[BsonElement(DB_KEY_GUILD_ID)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_GUILD_ID)]
	public string GuildId { get; set; }
	
	[BsonElement(DB_KEY_LOG)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_LOG)]
	public string Log { get; set; }
	
	[BsonElement(DB_KEY_INTERNAL_LOG)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_INTERNAL_LOG)]
	public string InternalLog { get; set; }
	
	[BsonElement(DB_KEY_TIMESTAMP)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_TIMESTAMP)]
	public long Timestamp { get; set; }

	public History(string guildId, string log, string internalLog)
	{
		GuildId = guildId;
		Log = log;
		InternalLog = internalLog;
		Timestamp = Common.Utilities.Timestamp.UnixTime;
	}
}