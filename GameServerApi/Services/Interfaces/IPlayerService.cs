using GameServerApi.Models;
using GameServerApi.Models.Entities;
using GameServerApi.Models.Services;

namespace GameServerApi.Services.Interfaces
{
    public interface IPlayerService
    {
        Task<PlayerData?> GetPlayerAsync(int playerId);
        Task<FinalStats> GetFinalStatsAsync(int playerId);
    }
}
