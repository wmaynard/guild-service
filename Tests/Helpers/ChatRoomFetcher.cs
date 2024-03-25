using System.Net.NetworkInformation;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Data;

namespace Rumble.Platform.Guilds.Tests.Helpers;

public static class ChatRoomFetcher
{
    public static RumbleJson GetGuildChatRoom(string roomId, out string[] members)
    {
        PlatformService
            .Require<ApiService>()
            .Request($"/chat/admin/rooms")
            .AddParameter("roomId", roomId)
            .AddAuthorization(PlatformService.Require<DynamicConfig>().AdminToken)
            .OnSuccess(_ => { })
            .OnFailure(_ => throw new PingException("Could not find chat room"))
            .Get(out RumbleJson response);

        ChatRoom[] rooms = response.Require<ChatRoom[]>("rooms");

        members = rooms.FirstOrDefault()?.Members;

        return rooms.Length switch
        {
            <1 => throw new PlatformException("No chat room found."),
            1 => response,
            >1 => throw new PlatformException("More than one room found.")
        };
    }

    public static RumbleJson GetGuildChatRoomsForUser(string token, out ChatRoom[] rooms)
    {
        PlatformService
            .Require<ApiService>()
            .Request("chat")
            .AddParameter("lastRead", Timestamp.FifteenMinutesAgo.ToString())
            .AddParameter("detailed", "true")
            .AddAuthorization(token)
            .Get(out RumbleJson chatResponse);

        rooms = chatResponse
            .Require<ChatRoom[]>("roomUpdates")
            .Where(room => room.GuildData?.ContainsKey("guild") ?? false)
            .ToArray();
        return chatResponse;
    }

    public static RumbleJson GetGuildChatRoomsForUser(TokenInfo token, out ChatRoom[] rooms)
    {
        string auth = ((Audience)token.PermissionSet).HasFlag(Audience.ChatService)
            ? token.Authorization
            : null;
        auth ??= PlatformService
            .Require<ApiService>()
            .GenerateToken(token.AccountId, Audience.GuildService | Audience.ChatService);
        
        return GetGuildChatRoomsForUser(auth, out rooms);
    }
}