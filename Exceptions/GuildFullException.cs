using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;

namespace Rumble.Platform.Guilds.Exceptions;

public class GuildFullException : PlatformException
{
    public GuildFullException(string guildId) : base("Unable to perform update; guild is full.", code: ErrorCode.Ineligible)
    {
        
    }
}