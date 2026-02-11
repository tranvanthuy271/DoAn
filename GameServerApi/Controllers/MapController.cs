using System.Text.Json;
using GameServerApi.Data;
using GameServerApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameServerApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MapController : ControllerBase
    {
        private readonly GameDbContext _db;

        public MapController(GameDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// GET /api/map/{mapId}/config
        /// Lấy spawn points cho map
        /// </summary>
        [HttpGet("{mapId}/config")]
        public async Task<IActionResult> GetMapConfig(int mapId)
        {
            var mapConfig = await _db.MapConfigs.FirstOrDefaultAsync(m => m.MapId == mapId);
            
            if (mapConfig == null)
            {
                // Trả về default spawn points nếu map không tồn tại (Game 2D chỉ cần x và y)
                var defaultSpawnPoints = new[]
                {
                    new { x = 0f, y = 0f },
                    new { x = 5f, y = 0f },
                    new { x = -5f, y = 0f }
                };
                
                return Ok(new
                {
                    map_id = mapId,
                    map_name = "Default Map",
                    spawn_points = defaultSpawnPoints
                });
            }

            // Parse spawn points JSON
            try
            {
                var spawnPoints = JsonSerializer.Deserialize<object[]>(mapConfig.SpawnPointsJson) ?? new object[0];

                return Ok(new
                {
                    map_id = mapConfig.MapId,
                    map_name = mapConfig.MapName,
                    spawn_points = spawnPoints
                });
            }
            catch (JsonException ex)
            {
                // Nếu JSON không hợp lệ, trả về default (Game 2D chỉ cần x và y)
                var defaultSpawnPoints = new[]
                {
                    new { x = 0f, y = 0f }
                };
                
                return Ok(new
                {
                    map_id = mapId,
                    map_name = mapConfig.MapName,
                    spawn_points = defaultSpawnPoints
                });
            }
        }
    }
}
