using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Data;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ArrangeAttributes

namespace Rumble.Platform.GuildService.Models;

[BsonIgnoreExtraElements]
public class Request : PlatformCollectionDocument
{
	internal const string DB_KEY_NAME        = "name";
	internal const string DB_KEY_DESCRIPTION = "desc";
	internal const string DB_KEY_GUILD_ID    = "gId";
	internal const string DB_KEY_PLAYER_ID   = "plId";
	internal const string DB_KEY_LEVEL       = "lvl";
	internal const string DB_KEY_TIMESTAMP   = "ts";

	public const string FRIENDLY_KEY_NAME        = "name";
	public const string FRIENDLY_KEY_DESCRIPTION = "description";
	public const string FRIENDLY_KEY_GUILD_ID    = "guildId";
	public const string FRIENDLY_KEY_PLAYER_ID   = "playerId";
	public const string FRIENDLY_KEY_LEVEL       = "level";
	public const string FRIENDLY_KEY_TIMESTAMP   = "timestamp";
    
	[BsonElement(DB_KEY_NAME)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_NAME)]
	public string Name { get; set; }
    
	[BsonElement(DB_KEY_DESCRIPTION)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_DESCRIPTION)]
	public string Description { get; set; }
	
	[BsonElement(DB_KEY_GUILD_ID)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_GUILD_ID)]
	public string GuildId { get; set; }

	[BsonElement(DB_KEY_PLAYER_ID)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_PLAYER_ID)]
	public string PlayerId { get; set; }
    
	[BsonElement(DB_KEY_LEVEL)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_LEVEL)]
	public int Level { get; set; }
	
	[BsonElement(DB_KEY_TIMESTAMP)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_TIMESTAMP)]
	public long Timestamp { get; set; }

	protected override void Validate(out List<string> errors)
	{
		errors = new List<string>();
		if (Name == null)
		{
			errors.Add("Name cannot be null.");
		}
        
		if (Description == null)
		{
			Description = "";
		}

		if (GuildId == null)
		{
			errors.Add("Guild ID cannot be null.");
		}

		if (PlayerId == null)
		{
			errors.Add("Player ID cannot be null.");
		}
	}
}