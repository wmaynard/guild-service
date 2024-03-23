using System.Text.Json.Serialization;
using Rumble.Platform.Data;

namespace Rumble.Platform.Guilds.Tests.Helpers;

public class ChatRoom : PlatformDataModel
{
    [JsonPropertyName("data")]
    public RumbleJson GuildData { get; set; }
    public string[] Members { get; set; }
    public string Type { get; set; }
    public int Channel { get; set; }
    public string Id { get; set; }
    public long CreatedOn { get; set; }
}