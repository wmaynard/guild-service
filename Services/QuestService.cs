using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Minq;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Guilds.Models;

namespace Rumble.Platform.Guilds.Services;

public class QuestService : MinqTimerService<Quest>
{
    public QuestService() : base("quests", interval: IntervalMs.TwelveHours) { }

    public Quest Complete(Quest quest) => string.IsNullOrWhiteSpace(quest?.GuildId)
        ? throw new PlatformException("Invalid quest; cannot mark as complete.", code: ErrorCode.InvalidParameter)
        : mongo
            .Where(query => query
                .EqualTo(db => db.GuildId, quest.GuildId)
                .EqualTo(db => db.Type, quest.Type)
            )
            .Upsert(update => update.Set(db => db.EndsOn, quest.EndsOn));

    public void LoadCompletedQuests(ref Guild guild)
    {
        string guildId = guild.Id;
        guild.Quests = mongo
            .Where(query => query
                .EqualTo(quest => quest.GuildId, guildId)
                .GreaterThan(quest => quest.EndsOn, Timestamp.Now)
            )
            .Limit(100)
            .ToArray();
    }

    protected override void OnElapsed() => mongo
        .Where(query => query.LessThanOrEqualTo(quest => quest.EndsOn, Timestamp.Now))
        .OnRecordsAffected(result => Log.Info(Owner.Will, "Old completed quest data deleted.", data: new
        {
            Count = result.Affected
        }))
        .Delete();
}