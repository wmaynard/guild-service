using Microsoft.AspNetCore.Mvc;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.Guilds.Models;
using Rumble.Platform.Guilds.Services;

namespace Rumble.Platform.Guilds.Controllers;

[ApiController, Route("guild/admin"), RequireAuth(AuthType.ADMIN_TOKEN)]
public class AdminController : PlatformController
{
    private readonly GuildService _guilds;
    private readonly MemberService _members;
    private readonly QuestService _quests;
    
    [HttpGet]
    public ActionResult GetGuildInformation()
    {
        string guildId = Require<string>("guildId");

        return Ok(_guilds.FromId(guildId));
    }

    [HttpPost, Route("completedQuests")]
    public ActionResult PostGuildBuff()
    {
        Quest[] quests = Require<Quest[]>("quests");
        string accountId = Require<string>(TokenInfo.FRIENDLY_KEY_ACCOUNT_ID);
        string guildId = _members.FindGuildIdFromToken(accountId);

        if (string.IsNullOrWhiteSpace(guildId))
            throw new PlatformException("Player tried to post a completed quest but is not currently in a guild", code: ErrorCode.Unauthorized);

        List<Quest> created = new();
        foreach (Quest quest in quests)
        {
            quest.GuildId = guildId;
            created.Add(_quests.Complete(quest));
        }

        return Ok(created);
    }
}