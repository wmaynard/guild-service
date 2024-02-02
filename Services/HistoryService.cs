using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Minq;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Guilds.Models;

namespace Rumble.Platform.Guilds.Services;

public class HistoryService : MinqTimerService<GuildMember>
{
    public HistoryService() : base("history", IntervalMs.OneDay)
    {
        
    }

    private static void Clean(ref GuildMember[] members)
    {
        members = members.Where(member => member != null).ToArray();
        foreach (GuildMember member in members)
        {
            member.ChangeId();
            member.CreatedOn = default;
        }
    }

    public void Insert(Transaction transaction, params GuildMember[] members)
    {
        Clean(ref members);
        mongo.WithTransaction(transaction).Insert(members);
    }

    public override void Insert(params GuildMember[] members)
    {
        Clean(ref members);
        base.Insert(members);
    }
    
    public GuildMember FindLastActivity(string accountId) => mongo
        .Where(query => query.EqualTo(member => member.AccountId, accountId))
        .Limit(1)
        .Sort(sort => sort.OrderByDescending(member => member.CreatedOn))
        .FirstOrDefault();

    protected override void OnElapsed() => mongo
        .Where(query => query.LessThanOrEqualTo(member => member.CreatedOn, Timestamp.SixMonthsAgo))
        .Delete();
}