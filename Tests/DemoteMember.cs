using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Testing;
using Rumble.Platform.Common.Utilities.JsonTools;
using Rumble.Platform.Guilds.Controllers;
using Rumble.Platform.Guilds.Models;
using Rumble.Platform.Guilds.Services;

namespace Rumble.Platform.Guilds.Tests;

[TestParameters(tokens: 0)]
[Covers(typeof(TopController), nameof(TopController.AlterRank))]
[DependentOn(typeof(CreatePrivateGuild), typeof(AcceptApplicant), typeof(DenyApplicant), typeof(PromoteMember))]
public class DemoteMember : PlatformUnitTest
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
        Guild.Members = _members.GetRoster(Guild.Id);
        string token = GenerateStandardToken(Guild.Leader.AccountId, Audience.GuildService | Audience.ChatService);
        string toDemote = Guild.Members.FirstOrDefault(member => member.Rank == Rank.Officer)?.AccountId;
        Assert("Guild has at least one officer", !string.IsNullOrWhiteSpace(toDemote));
        
        Request(token, new RumbleJson
        {
            { TokenInfo.FRIENDLY_KEY_ACCOUNT_ID, toDemote },
            { "isPromotion", false }
        }, out RumbleJson response, out int code);
        Assert("Request successful", code.Between(200, 299));
        
        Guild.Members = _members.GetRoster(Guild.Id);
        GuildMember demoted = Guild.Members.FirstOrDefault(member => member.AccountId == toDemote);
        Assert("Promoted member is still a part of the guild", demoted != null, abortOnFail: true);
        Assert("Promoted member is now an Elder.", demoted.Rank == Rank.Elder);
        
        Request(token, new RumbleJson
        {
            { TokenInfo.FRIENDLY_KEY_ACCOUNT_ID, toDemote },
            { "isPromotion", false }
        }, out response, out code);
        Assert("Request successful", code.Between(200, 299));
        
        Guild.Members = _members.GetRoster(Guild.Id);
        demoted = Guild.Members.FirstOrDefault(member => member.AccountId == toDemote);
        Assert("Promoted member is still a part of the guild", demoted != null, abortOnFail: true);
        Assert("Promoted member is now an Elder.", demoted.Rank == Rank.Member);
        
        Request(token, new RumbleJson
        {
            { TokenInfo.FRIENDLY_KEY_ACCOUNT_ID, toDemote },
            { "isPromotion", false }
        }, out response, out code);
        Assert("Request failed because the member is already the lowest rank", code.Between(400, 499));
        
        Guild.Members = _members.GetRoster(Guild.Id);
        demoted = Guild.Members.FirstOrDefault(member => member.AccountId == toDemote);
        Assert("Promoted member is still a part of the guild", demoted != null, abortOnFail: true);
        Assert("Promoted member is still a Member.", demoted.Rank == Rank.Member);
    }

    public override void Cleanup() { }
}