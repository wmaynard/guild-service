using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using RCL.Logging;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Common.Interfaces;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Data;

namespace Rumble.Platform.Guilds.Models;

public class Guild : PlatformCollectionDocument, ISearchable<Guild>
{
    #region ignore
    public static int CAPACITY => DynamicConfig.Instance.Optional("capacity", 20);
    public static int DESCRIPTION_LENGTH => DynamicConfig.Instance.Optional("maxDescriptionLength", 500);
    public static int NAME_LENGTH => DynamicConfig.Instance.Optional("maxNameLength", 50);
    
    [BsonElement("name")]
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [BsonElement("lang")]
    [JsonPropertyName("language")]
    public string Language { get; set; }
    
    [BsonElement("loc")]
    [JsonPropertyName("region")]
    public string Region { get; set; }
    
    [BsonElement("viz")]
    [JsonPropertyName("access")]
    public AccessLevel Access { get; set; }
    
    [BsonElement("minLv")]
    [JsonPropertyName("requiredLevel")]
    public int RequiredLevel { get; set; }
    
    [BsonElement("desc")]
    [JsonPropertyName("description")]
    public string Description { get; set; }
    
    [BsonElement("icon")]
    [JsonPropertyName("iconData")]
    public RumbleJson IconData { get; set; }
    
    [BsonElement("room")]
    [JsonPropertyName("chatRoomId")]
    public string ChatRoomId { get; set; }
    
    [BsonElement("sync")]
    [JsonIgnore]
    public long LastChatSync { get; set; }
    
    [BsonIgnore]
    [JsonPropertyName("members")]
    public GuildMember[] Members { get; set; }

    [BsonIgnore]
    [JsonPropertyName("capacity")]
    public int Capacity => CAPACITY;
    
    [BsonIgnore, JsonIgnore]
    public bool IsFull => Members.Count(member => member.Rank > Rank.Applicant) > CAPACITY;
    
    [BsonIgnore, JsonIgnore]
    public GuildMember Leader => Members.First(member => member.Rank == Rank.Leader);

    protected override void Validate(out List<string> errors)
    {
        errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("A guild must have a name.");
        if (string.IsNullOrWhiteSpace(Description))
            errors.Add("A guild must have a description");
        if (Name != null && Name.Length > NAME_LENGTH)
            errors.Add("Guild name is too long.");
        if (Description != null && Description.Length > DESCRIPTION_LENGTH)
            errors.Add("Guild description is too long.");

        // TODO: less brittle check here
        Access = (AccessLevel)Math.Max(Math.Min((int)Access, (int)AccessLevel.Private), (int)AccessLevel.Public);
    }
    #endregion

    public long SearchWeight { get; set; }
    public double SearchConfidence { get; set; }
    public Dictionary<Expression<Func<Guild, object>>, int> DefineSearchWeights() => new()
    {
        { guild => guild.Name, 100 },
        { guild => guild.Description, 10 },
        { guild => guild.Region, 1 },
        { guild => guild.Language, 1 }
    };
}

public enum AccessLevel
{
    Public = 0,
    Closed = 1,
    Private = 2
}

