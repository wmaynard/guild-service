using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Utilities.JsonTools;

namespace Rumble.Platform.Guilds.Models;

public class Quest : PlatformCollectionDocument
{
    [BsonElement("t")]
    [JsonPropertyName("type")]
    public string Type { get; set; }
    
    [BsonElement("end")]
    [JsonPropertyName("endsOn")]
    public long EndsOn { get; set; }
    
    [BsonElement("guild")]
    [JsonIgnore]
    public string GuildId { get; set; }

    protected override void Validate(out List<string> errors)
    {
        errors = new();
        
        if (string.IsNullOrWhiteSpace(Type))
            errors.Add("The type must not be null or whitespace.");
        if (EndsOn <= Timestamp.Now)
            errors.Add("The endsOn field must be later than the current time.");
    }
}