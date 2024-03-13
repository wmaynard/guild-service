using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc.Filters;
using Rumble.Platform.Common.Filters;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Data;
using Rumble.Platform.Guilds.Services;

namespace Rumble.Platform.Guilds.Filters;

public class ActivityFilter : PlatformFilter, IAuthorizationFilter
{
    private static ConcurrentStack<string> _activePlayers = new();
    private static long _flushed = Timestamp.Now;
    
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (Token == null || Token.IsAdmin)
            return;
        
        _activePlayers.Push(Token.AccountId);

        #if RELEASE
        if (_flushed > Timestamp.FiveMinutesAgo)
            return;
        #endif

        long affected = PlatformService
            .Optional<MemberService>()
            ?.MarkAccountsActive(null, _activePlayers.ToArray())
            ?? 0;
        
        if (affected > 0)
            _activePlayers.Clear();
    }
}