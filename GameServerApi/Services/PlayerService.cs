using GameServerApi.Data;
using GameServerApi.Models;
using GameServerApi.Models.Entities;
using GameServerApi.Models.Services;
using GameServerApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GameServerApi.Services
{
    public class PlayerService : IPlayerService
    {
        private readonly GameDbContext _db;

        public PlayerService(GameDbContext db)
        {
            _db = db;
        }

        public async Task<PlayerData?> GetPlayerAsync(int playerId) =>
            await _db.PlayerData.FirstOrDefaultAsync(p => p.PlayerId == playerId);

        public async Task<FinalStats> GetFinalStatsAsync(int playerId)
        {
            var player = await GetPlayerAsync(playerId);
            if (player == null) return new FinalStats();
            var info = player.GetInfoChar();
            return StatCalculator.Compute(info, player.EquipmentJson, player.PotentialStatsJson);
        }
    }
}
