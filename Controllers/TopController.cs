using System.Net;
using Microsoft.AspNetCore.Mvc;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.GuildService.Models;
using Rumble.Platform.GuildService.Services;

namespace Rumble.Platform.GuildService.Controllers;

[ApiController, Route("guild"), RequireAuth]
public class TopController : PlatformController
{
    #pragma warning disable
    private readonly Services.GuildService _guilds;
    private readonly MemberService _members;
    private readonly HistoryService _history;
    #pragma warning restore
    
    [HttpPost, Route("create")] // TODO: Needs to be admin
    public ActionResult Create()
    {
        Guild guild = Require<Guild>("guild");

        _guilds.Create(guild, Token.AccountId);

        return Ok(guild);
    }
    
    [HttpPost, Route("join")]
    public ActionResult Join()
    {
        string id = Require<string>("guildId");

        GuildMember lastActivity = _history.FindLastActivity(Token.AccountId);
        if ((lastActivity?.CreatedOn ?? 0) > Timestamp.OneDayAgo)
            return new BadRequestObjectResult(lastActivity)
            {
                StatusCode = (int)HttpStatusCode.TooManyRequests
            };

        GuildMember member = _guilds.Join(id, Token.AccountId);

        return Ok(member);

    }

    [HttpPatch, Route("approve")]
    public ActionResult Approve()
    {
        string guildId = Require<string>("guildId");
        string accountId = Require<string>(TokenInfo.FRIENDLY_KEY_ACCOUNT_ID);

        GuildMember output = _members.ApproveApplication(guildId, accountId, Token.AccountId);

        return Ok(output);
    }

    [HttpDelete, Route("leave")]
    public ActionResult Leave()
    {
        GuildMember output = _members.Leave(Token.AccountId);

        return Ok(output);
    }

    [HttpDelete, Route("kick")]
    public ActionResult Kick()
    {
        string accountId = Require<string>(TokenInfo.FRIENDLY_KEY_ACCOUNT_ID);

        GuildMember kicked = _members.Leave(accountId, kickedBy: Token.AccountId);

        return Ok(kicked);
    }

    [HttpGet, Route("search")]
    public ActionResult Search() => Ok(_guilds.Search());

    [HttpGet]
    public ActionResult GetGuildDetails()
    {
        string guildId = Require<string>("guildId");

        Guild info = _guilds.FromId(guildId);

        return Ok(info);
    }
}