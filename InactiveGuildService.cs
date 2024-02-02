using Rumble.Platform.Common.Services;
using Rumble.Platform.Data;
using Rumble.Platform.Guilds.Models;
using Rumble.Platform.Guilds.Services;

namespace Rumble.Platform.Guilds;

public class InactiveGuildService : QueueService<InactiveGuildService.CleanupTask>
{
    private readonly Services.GuildService _guilds;
    private readonly MemberService _members;

    public InactiveGuildService(Services.GuildService guilds, MemberService members) : base("inactive", Common.Utilities.IntervalMs.SixHours, 10, 10)
    {
        _guilds = guilds;
        _members = members;
    }

    protected override void OnTasksCompleted(CleanupTask[] data) { }

    protected override void PrimaryNodeWork()
    {
        GuildMember[] inactives = _members.GetInactiveLeaders();

        if (!inactives.Any())
            return;
        
        List<CleanupTask> tasks = new();
        int index = 0;
        while (index < inactives.Length)
        {
            tasks.Add(new CleanupTask
            {
                InactiveLeaders = inactives
                    .Skip(index)
                    .Take(10)
                    .ToArray()
            });
            index += 10;
        }
        
        CreateUntrackedTasks(tasks.ToArray());
    }

    protected override void ProcessTask(CleanupTask data)
    {
        throw new NotImplementedException();
    }


    public class CleanupTask : PlatformDataModel
    {
        public GuildMember[] InactiveLeaders { get; set; }
    }
}