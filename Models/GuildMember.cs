using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Data;

namespace Rumble.Platform.Guilds.Models;

public class GuildMember : PlatformCollectionDocument
{
    public string AccountId { get; set; }
    public string ApprovedBy { get; set; }
    public string UpdatedBy { get; set; }
    public string KickedBy { get; set; }
    public string GuildId { get; set; }
    public long JoinedOn { get; set; }
    public long LeftOn { get; set; }
    public long LastActive { get; set; }
    public Rank Rank { get; set; }
    [BsonIgnore]
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