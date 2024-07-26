using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Testing;
using Rumble.Platform.Common.Utilities.JsonTools;
using Rumble.Platform.Guilds.Controllers;
using Rumble.Platform.Guilds.Models;
using Rumble.Platform.Guilds.Services;
using Rumble.Platform.Guilds.Tests.Helpers;

namespace Rumble.Platform.Guilds.Tests;

[TestParameters(tokens: 0)]
[Covers(typeof(TopController), nameof(TopController.AlterRank))]
[DependentOn(typeof(CreatePrivateGuild), typeof(AcceptApplicant), typeof(DenyApplicant))]
public class PromoteMember : PlatformUnitTest
{
    private MemberService _members;
    private Guild Guild { get; set; }
    public override void Initialize()
    {
        GetTestResults(typeof(CreatePrivateGuild), out RumbleJson response);

        Guild = response.Require<Guild>("guild");
    }

    public override void Execute()
    {
        string originalLeader = Guild.Leader.AccountId;
        string token = GenerateStandardToken(originalLeader, Audience.GuildService | Audience.ChatService);

        Guild.Members = _members.GetRoster(Guild.Id);
        string toPromote = Guild.Members.FirstOrDefault(member => member.Rank == Rank.Member)?.AccountId;
        Assert("Guild has at least one regular member", !string.IsNullOrWhiteSpace(toPromote));
        
        Request(token, new RumbleJson
        {
            { TokenInfo.FRIENDLY_KEY_ACCOUNT_ID, toPromote },
            { "isPromotion", true }
        }, out RumbleJson response, out int code);
        Assert("Request successful", code.Between(200, 299));
        
        Guild.Members = _members.GetRoster(Guild.Id);
        GuildMember promoted = Guild.Members.FirstOrDefault(member => member.AccountId == toPromote);
        Assert("Promoted member is still a part of the guild", promoted != null, abortOnFail: true);
        Assert("Promoted member is now an Elder.", promoted.Rank == Rank.Elder);
        
        Request(token, new RumbleJson
        {
            { TokenInfo.FRIENDLY_KEY_ACCOUNT_ID, toPromote },
            { "isPromotion", true }
        }, out response, out code);
        Assert("Request successful", code.Between(200, 299));
        
        Guild.Members = _members.GetRoster(Guild.Id);
        promoted = Guild.Members.FirstOrDefault(member => member.AccountId == toPromote);
        Assert("Promoted member is still a part of the guild", promoted != null, abortOnFail: true);
        Assert("Promoted member is now an Officer.", promoted.Rank == Rank.Officer);
        
        Request(token, new RumbleJson
        {
            { TokenInfo.FRIENDLY_KEY_ACCOUNT_ID, toPromote },
            { "isPromotion", true }
        }, out response, out code);
        Assert("Request successful", code.Between(200, 299));
        
        Guild.Members = _members.GetRoster(Guild.Id);
        promoted = Guild.Members.FirstOrDefault(member => member.AccountId == toPromote);
        Assert("Promoted member is still a part of the guild", promoted != null, abortOnFail: true);
        Assert("Promoted member is now the Leader.", promoted.Rank == Rank.Leader);
        Assert("Original leader is now an Officer.", Guild.Members.First(member => member.AccountId == originalLeader).Rank == Rank.Officer);
    }

    public override void Cleanup() { }
}