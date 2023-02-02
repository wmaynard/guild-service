using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Data;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ArrangeAttributes

namespace Rumble.Platform.GuildService.Models;

[BsonIgnoreExtraElements]
public class Guild : PlatformCollectionDocument
{
    internal const string DB_KEY_NAME              = "name";
    internal const string DB_KEY_DESCRIPTION       = "desc";
    internal const string DB_KEY_TYPE              = "type";
    internal const string DB_KEY_LEVEL_REQUIREMENT = "lvlreq";
    internal const string DB_KEY_ART               = "art";
    internal const string DB_KEY_LEADER            = "ldr";
    internal const string DB_KEY_MEMBERS           = "mbrs";
    internal const string DB_KEY_BANS              = "bans";
    internal const string DB_KEY_CHAT_ROOM         = "chatrm";

    public const string FRIENDLY_KEY_NAME              = "name";
    public const string FRIENDLY_KEY_DESCRIPTION       = "description";
    public const string FRIENDLY_KEY_TYPE              = "type";
    public const string FRIENDLY_KEY_LEVEL_REQUIREMENT = "levelRequirement";
    public const string FRIENDLY_KEY_ART               = "art";
    public const string FRIENDLY_KEY_LEADER            = "leader";
    public const string FRIENDLY_KEY_MEMBERS           = "members";
    public const string FRIENDLY_KEY_BANS              = "bans";
    public const string FRIENDLY_KEY_CHAT_ROOM         = "chatroom";
    
    [BsonElement(DB_KEY_NAME)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_NAME)]
    public string Name { get; set; }
    
    [BsonElement(DB_KEY_DESCRIPTION)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_DESCRIPTION)]
    public string Description { get; set; }

    public enum GuildType
    {
        Public,
        Private,
        Closed
    }
    [BsonElement(DB_KEY_TYPE)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_TYPE)]
    public GuildType Type { get; set; }
    
    [BsonElement(DB_KEY_LEVEL_REQUIREMENT)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_LEVEL_REQUIREMENT)]
    public int LevelRequirement { get; set; }
    
    [BsonElement(DB_KEY_ART)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_ART), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Art { get; set; } // potentially an asset link or uploaded link by the players
    
    [BsonElement(DB_KEY_LEADER)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_LEADER)]
    public string Leader { get; set; }
    
    [BsonElement(DB_KEY_MEMBERS)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_MEMBERS)]
    public List<Member> Members { get; set; } // TODO member limit?
    
    [BsonElement(DB_KEY_BANS)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_BANS)]
    public List<string> Bans { get; set; } // player IDs
    
    [BsonElement(DB_KEY_CHAT_ROOM)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_CHAT_ROOM)]
    public string ChatRoom { get; set; }

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
    }
}