using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameServerApi.Controllers
{
    internal static class ZoneServerRegistry
    {
        internal sealed record ZoneStatSnapshot(int MapId, int ZoneId, int Players, int MaxPlayers);

        internal sealed class ZoneServerEntry
        {
            public string Ip { get; init; } = "127.0.0.1";
            public int Port { get; init; }
            public int MapCount { get; init; }
            public int PlayerCount { get; init; }
            public IReadOnlyList<ZoneStatSnapshot> ZoneStats { get; init; } = Array.Empty<ZoneStatSnapshot>();
            public DateTime RegisteredAtUtc { get; init; } = DateTime.UtcNow;
            public DateTime LastHeartbeatUtc { get; init; } = DateTime.UtcNow;
        }

        private static readonly ConcurrentDictionary<int, ZoneServerEntry> Servers = new();

        public static ZoneServerEntry Register(string ip, int port, int mapCount)
        {
            var now = DateTime.UtcNow;
            return Servers.AddOrUpdate(
                port,
                _ => new ZoneServerEntry
                {
                    Ip = ip,
                    Port = port,
                    MapCount = mapCount,
                    RegisteredAtUtc = now,
                    LastHeartbeatUtc = now
                },
                (_, existing) => new ZoneServerEntry
                {
                    Ip = ip,
                    Port = port,
                    MapCount = mapCount,
                    PlayerCount = existing.PlayerCount,
                    ZoneStats = existing.ZoneStats,
                    RegisteredAtUtc = now,
                    LastHeartbeatUtc = now
                });
        }

        public static ZoneServerEntry UpsertHeartbeat(string ip, int port, int playerCount, IReadOnlyList<ZoneStatSnapshot> zoneStats)
        {
            var now = DateTime.UtcNow;
            return Servers.AddOrUpdate(
                port,
                _ => new ZoneServerEntry
                {
                    Ip = ip,
                    Port = port,
                    MapCount = zoneStats.Select(stat => stat.MapId).Distinct().Count(),
                    PlayerCount = playerCount,
                    ZoneStats = zoneStats,
                    RegisteredAtUtc = now,
                    LastHeartbeatUtc = now
                },
                (_, existing) => new ZoneServerEntry
                {
                    Ip = string.IsNullOrWhiteSpace(ip) ? existing.Ip : ip,
                    Port = port,
                    MapCount = existing.MapCount,
                    PlayerCount = playerCount,
                    ZoneStats = zoneStats,
                    RegisteredAtUtc = existing.RegisteredAtUtc,
                    LastHeartbeatUtc = now
                });
        }

        public static bool Deregister(int port) => Servers.TryRemove(port, out _);
    }

    [ApiController]
    [Route("api/zone/server")]
    [Authorize(Roles = "GameServer")]
    public class ZoneServerController : ControllerBase
    {
        [HttpPost("register")]
        public IActionResult Register([FromBody] ZoneServerRegisterRequest request)
        {
            if (request.Port <= 0)
                return BadRequest(new { message = "port phải lớn hơn 0." });

            if (string.IsNullOrWhiteSpace(request.Ip))
                return BadRequest(new { message = "ip không được để trống." });

            var entry = ZoneServerRegistry.Register(request.Ip, request.Port, Math.Max(0, request.MapCount));
            return Ok(new
            {
                success = true,
                ip = entry.Ip,
                port = entry.Port,
                map_count = entry.MapCount,
                registered_at = entry.RegisteredAtUtc
            });
        }

        [HttpPut("heartbeat")]
        public IActionResult Heartbeat([FromBody] ZoneServerHeartbeatRequest request)
        {
            if (request.Port <= 0)
                return BadRequest(new { message = "port phải lớn hơn 0." });

            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
            var zoneStats = (request.ZoneStats ?? Array.Empty<ZoneServerZoneStatDto>())
                .Select(stat => new ZoneServerRegistry.ZoneStatSnapshot(
                    stat.MapId,
                    stat.ZoneId,
                    Math.Max(0, stat.Players),
                    Math.Max(0, stat.Max)))
                .ToArray();

            var entry = ZoneServerRegistry.UpsertHeartbeat(
                remoteIp,
                request.Port,
                Math.Max(0, request.PlayerCount),
                zoneStats);

            return Ok(new
            {
                success = true,
                player_count = entry.PlayerCount,
                zones = entry.ZoneStats.Count,
                updated_at = entry.LastHeartbeatUtc
            });
        }

        [HttpDelete("deregister")]
        public IActionResult Deregister([FromQuery] int port)
        {
            if (port <= 0)
                return BadRequest(new { message = "port phải lớn hơn 0." });

            bool removed = ZoneServerRegistry.Deregister(port);
            return Ok(new { success = true, removed });
        }
    }

    public sealed class ZoneServerRegisterRequest
    {
        public string Ip { get; set; } = string.Empty;
        public int Port { get; set; }
        public int MapCount { get; set; }
    }

    public sealed class ZoneServerHeartbeatRequest
    {
        public int Port { get; set; }
        public int PlayerCount { get; set; }
        public ZoneServerZoneStatDto[] ZoneStats { get; set; } = Array.Empty<ZoneServerZoneStatDto>();
    }

    public sealed class ZoneServerZoneStatDto
    {
        public int MapId { get; set; }
        public int ZoneId { get; set; }
        public int Players { get; set; }
        public int Max { get; set; }
    }
}