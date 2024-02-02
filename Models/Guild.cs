using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Data;

namespace Rumble.Platform.Guilds.Models;

public class Guild : PlatformCollectionDocument
{
    public string Name { get; set; }
    public string Language { get; set; }
    public string Region { get; set; }
    public AccessLevel Access { get; set; }
    public int RequiredLevel { get; set; }
    public string Description { get; set; }
    public RumbleJson IconData { get; set; }
    
    [BsonIgnore]
    public GuildMember[] Members { get; set; }


}

public enum AccessLevel
{
    Public = 0,
    Closed = 1,
    Private = 2
}