using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Data;

namespace Rumble.Platform.GuildService.Models;

public class History : PlatformCollectionDocument
{
	internal const string DB_KEY_GUILD_ID = "gid";
	internal const string DB_KEY_LOG      = "log";

	public const string FRIENDLY_KEY_GUILD_ID = "guildId";
	public const string FRIENDLY_KEY_LOG      = "log";
	
	[BsonElement(DB_KEY_GUILD_ID)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_GUILD_ID)]
	public string GuildId { get; set; }
	
	[BsonElement(DB_KEY_LOG)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_LOG)]
	public string Log { get; set; }
}