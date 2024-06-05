using System.Net;
using Microsoft.AspNetCore.Mvc;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.Data;
using Rumble.Platform.Guilds.Models;
using Rumble.Platform.Guilds.Services;

namespace Rumble.Platform.Guilds.Controllers;

[ApiController, Route("guild"), RequireAuth]
public class TopController : PlatformController
{
    #pragma warning disable
    private readonly Services.GuildService _guilds;
    private readonly MemberService _members;
    private readonly HistoryService _history;
    #pragma warning restore
    
    [HttpPost, Route("create"), RequireAuth(AuthType.ADMIN_TOKEN)]
    public ActionResult Create()
    {
        Guild guild = Require<Guild>("guild");
        string accountId = Require<string>(TokenInfo.FRIENDLY_KEY_ACCOUNT_ID);
        
        guild.Members = new[] 
        {
            new GuildMember
            {
                AccountId = accountId,
                JoinedOn = Timestamp.Now,
                Rank = Rank.Leader
            }
        };
        
        _guilds.Create(guild);

        return Ok(guild);
    }
    
    [HttpPost, Route("join")]
    public ActionResult Join()
    {
        string id = Require<string>("guildId");

        // Cooldown disabled for MVP
        // GuildMember lastActivity = _history.FindLastActivity(Token.AccountId);
        // if ((lastActivity?.CreatedOn ?? 0) > Timestamp.OneDayAgo)
        //     return new BadRequestObjectResult(lastActivity)
        //     {
        //         StatusCode = (int)HttpStatusCode.TooManyRequests
        //     };

        Guild guild = _guilds.Join(id, Token.AccountId);

        return Ok(guild);
    }

    [HttpPatch, Route("approve")]
    public ActionResult Approve()
    {
        string accountId = Require<string>(TokenInfo.FRIENDLY_KEY_ACCOUNT_ID);

        GuildMember output = _members.ApproveApplication(accountId, Token.AccountId);

        return Ok(output);
    }

    [HttpDelete, Route("leave")]
    public ActionResult Leave()
    {
        _members.Remove(Token.AccountId);

        return Ok();
    }

    // TODO: Guild chat mute?
    // TODO: Add a ban system
    [HttpDelete, Route("kick")]
    public ActionResult Kick()
    {
        string accountId = Require<string>(TokenInfo.FRIENDLY_KEY_ACCOUNT_ID);

        _members.Remove(accountId, kickedBy: Token.AccountId);

        return Ok();
    }

    [HttpPatch, Route("rank")]
    public ActionResult AlterRank()
    {
        string accountId = Require<string>(TokenInfo.FRIENDLY_KEY_ACCOUNT_ID);
        bool isPromotion = Require<bool>("isPromotion");

        GuildMember demoted = _members.AlterRank(accountId, Token.AccountId, isPromotion);

        return Ok(demoted);
    }

    [HttpGet, Route("search")]
    public ActionResult Search()
    {
        string terms = Optional<string>("terms");

        Guild[] results = string.IsNullOrWhiteSpace(terms)
            ? _guilds.Browse()
            : _guilds.Search(terms);

        string[] guildsAppliedTo = _members.GetOutstandingApplications(Token.AccountId);
        
        if (guildsAppliedTo.Any())
            foreach (Guild guild in results.Where(result => guildsAppliedTo.Contains(result.Id)))
                guild.TokenIsOutstandingApplicant = true;
        
        return Ok(new RumbleJson
        {
            { "guilds", results },
            { "guildsAppliedTo", guildsAppliedTo }
        });
    } 

    [HttpGet]
    public ActionResult GetGuildDetails()
    {
        string guildId = Optional<string>("guildId");
        
        guildId ??= _members.FindGuildIdFromToken(Token.AccountId)
            ?? throw new PlatformException("No guildId specified or player not in a guild; cannot retrieve guild details.", code: ErrorCode.MongoRecordNotFound);

        Guild output = _guilds.FromId(guildId);
        bool isGuildMember = output.Members.Any(member => member.AccountId == Token.AccountId && member.Rank > Rank.Applicant);
        
        if (!isGuildMember || output.IsFull)
            output.Members = output.Members
                .Where(member => member.Rank > Rank.Applicant)
                .ToArray();

        return Ok(output);
    }

    [HttpPatch, Route("update")]
    public ActionResult EditGuild()
    {
        Guild guild = Require<Guild>("guild");
        
        Guild updated = _guilds.ModifyDetails(guild, Token.AccountId);
        
        return Ok(updated);
    }

    [HttpPatch, Route("account")]
    public ActionResult UpdatePlayerPowerScore()
    {
        Require<long>("totalHeroScore");
        
        return Ok();
    }
}