using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace GameServerApi.Hubs
{
    public sealed class GameUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirst("user_id")?.Value
                ?? connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? connection.User?.FindFirst("sub")?.Value;
        }
    }
}