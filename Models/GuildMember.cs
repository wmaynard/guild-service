using Rumble.Platform.Data;

namespace Rumble.Platform.GuildService.Models;

public class GuildMember : PlatformCollectionDocument
{
    public string AccountId { get; set; }
    public string ApprovedBy { get; set; }
    public string PromotedBy { get; set; }
    public string KickedBy { get; set; }
    public string GuildId { get; set; }
    public long JoinedOn { get; set; }
    public long LeftOn { get; set; }
    public long LastActive { get; set; }
    public Rank Rank { get; set; }


}

public enum Rank
{
    Applicant = 0,
    Member = 1,
    Officer = 2,
    Leader = 3
}