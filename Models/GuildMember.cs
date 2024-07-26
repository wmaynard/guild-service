using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Utilities.JsonTools;

namespace Rumble.Platform.Guilds.Models;

public class GuildMember : PlatformCollectionDocument
{
    [BsonElement(TokenInfo.DB_KEY_ACCOUNT_ID)]
    [JsonPropertyName(TokenInfo.FRIENDLY_KEY_ACCOUNT_ID)]
    public string AccountId { get; set; }
    
    [BsonElement("approvedBy")]
    [JsonIgnore]
    public string ApprovedBy { get; set; }
    
    [BsonElement("updatedBy")]
    [JsonIgnore]
    public string UpdatedBy { get; set; }
    
    [BsonElement("kickedBy")]
    [JsonIgnore]
    public string KickedBy { get; set; }
    
    [BsonElement("guild")]
    [JsonPropertyName("guildId")]
    public string GuildId { get; set; }
    
    [BsonElement("joined")]
    [JsonPropertyName("joinedOn"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public long JoinedOn { get; set; }
    
    [BsonElement("left")]
    [JsonPropertyName("leftOn"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public long LeftOn { get; set; }
    
    [BsonElement("active")]
    [JsonPropertyName("lastActive")]
    public long LastActive { get; set; }
    
    [BsonElement("rank")]
    [JsonPropertyName("rank")]
    public Rank Rank { get; set; }
    
    [BsonIgnore]
    [JsonPropertyName("rankVerbose")]
    public string RankVerbose => Rank.GetDisplayName();
}

public enum Rank
{
    Applicant = 0,
    Member = 1,
    Elder = 2,
    Officer = 5,
    Leader = 10
}