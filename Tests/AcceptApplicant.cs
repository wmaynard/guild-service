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
[Covers(typeof(TopController), nameof(TopController.Approve))]
[DependentOn(typeof(CreatePrivateGuild), typeof(ApplyToPrivateGuild))]
public class AcceptApplicant : PlatformUnitTest
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
        Guild.Members = _members.GetRoster(Guild.Id, includeApplicants: true);
        int applicants = Guild.Members.Count(member => member.Rank == Rank.Applicant);
        Assert("Guild has at least one applicant", applicants > 0, abortOnFail: true);
        
        string token = GenerateStandardToken(Guild.Leader.AccountId, Audience.GuildService | Audience.ChatService);
        string toAccept = Guild.Members.First(member => member.Rank == Rank.Applicant).AccountId;
        Request(token, new RumbleJson
        {
            { TokenInfo.FRIENDLY_KEY_ACCOUNT_ID, toAccept }
        }, out RumbleJson response, out int code);
        Assert("Request successful", code.Between(200, 299));
        
        Guild.Members = _members.GetRoster(Guild.Id, includeApplicants: true);
        Assert("Guild members contains the applicant", Guild.Members.Any(member => member.AccountId == toAccept), abortOnFail: true);
        Assert("The applicant is now a Member", Guild.Members.First(member => member.AccountId == toAccept).Rank == Rank.Member);
        Assert("Applicant count has been reduced", Guild.Members.Count(member => member.Rank == Rank.Applicant) < applicants);
    }

    public override void Cleanup() { }
}