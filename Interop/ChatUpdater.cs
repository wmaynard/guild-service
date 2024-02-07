using Rumble.Platform.Common.Services;
using Rumble.Platform.Data;
using Rumble.Platform.Guilds.Models;
using Rumble.Platform.Guilds.Services;

namespace Rumble.Platform.Guilds.Interop;

public class ChatUpdater : QueueService<ChatUpdater.ChatUpdateTask>
{
    private readonly GuildService _guilds;

    public ChatUpdater(GuildService guilds, MemberService members) : base("chat_updates", Common.Utilities.IntervalMs.FiveMinutes, 10, 0)
        => _guilds = guilds;
    
    protected override void OnTasksCompleted(ChatUpdateTask[] data) { }

    protected override void PrimaryNodeWork()
    {
        string[] guildIds = _guilds.FindGuildsInNeedOfSync(limit: 100);
        if (!guildIds.Any())
            return;
        
        ChatUpdateTask[] tasks = guildIds.Select(id => new ChatUpdateTask
        {
            GuildId = id
        })
        .ToArray();
        
        CreateUntrackedTasks(tasks);
    }

    protected override void ProcessTask(ChatUpdateTask data) => ChatService.TryUpdateRoom(data.GuildId);
    
    public class ChatUpdateTask : PlatformDataModel
    {
        public string GuildId { get; set; }
    }
}