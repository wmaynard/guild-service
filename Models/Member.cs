using System.Text.Json.Serialization;
using Rumble.Platform.Common.Utilities;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Data;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ArrangeAttributes

namespace Rumble.Platform.GuildService.Models;

[BsonIgnoreExtraElements]
public class Member : PlatformCollectionDocument
{
	internal const string DB_KEY_NAME        = "name";
	internal const string DB_KEY_PLAYER_ID   = "pid";
	internal const string DB_KEY_POSITION    = "pos";
	internal const string DB_KEY_LAST_ACTIVE = "lastAct";

	public const string FRIENDLY_KEY_NAME        = "name";
	public const string FRIENDLY_KEY_PLAYER_ID   = "playerId";
	public const string FRIENDLY_KEY_POSITION    = "position";
	public const string FRIENDLY_KEY_LAST_ACTIVE = "lastActive";
    
	[BsonElement(DB_KEY_NAME)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_NAME)]
	public string Name { get; set; }
	
	[CompoundIndex(group: "INDEX_GROUP_GUILD", priority: 1)]
	[BsonElement(DB_KEY_PLAYER_ID)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_PLAYER_ID)]
	public string PlayerId { get; set; }
    
	public enum Role
	{
		Member,
		Officer,
		Leader
	}
	[BsonElement(DB_KEY_POSITION)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_POSITION)]
	public Role Position { get; set; }

	[BsonElement(DB_KEY_LAST_ACTIVE)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_LAST_ACTIVE)]
	public long LastActive { get; set; }

	public Member(string name, string playerId, Role position = Role.Member, long lastActive = 0)
	{
		Name = name;
		PlayerId = playerId;
		Position = position;
		if (lastActive == 0)
		{
			LastActive = Timestamp.UnixTime;
		}
		else
		{
			LastActive = lastActive;
		}
	}

	protected override void Validate(out List<string> errors)
	{
		errors = new List<string>();
		if (Name == null)
		{
			errors.Add("Name cannot be null.");
		}
	}
}