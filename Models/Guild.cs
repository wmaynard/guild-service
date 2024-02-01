using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Data;

namespace Rumble.Platform.GuildService.Models;

public class Guild : PlatformCollectionDocument
{
    public string Name { get; set; }
    public string Language { get; set; }
    public string Region { get; set; }
    public GuildType Type { get; set; }
    public int RequiredLevel { get; set; }
    public string Description { get; set; }
    public RumbleJson IconData { get; set; }
    
    [BsonIgnore]
    public GuildMember[] Members { get; set; }


}

public enum GuildType
{
    Open = 0,
    Closed = 1,
    Private = 2
}