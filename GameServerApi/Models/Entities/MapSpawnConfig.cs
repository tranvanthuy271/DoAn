using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerApi.Models.Entities
{
    /// <summary>
    /// Bảng map_spawn_config — Cấu hình spawn enemy + tỉ lệ drop item theo từng map.
    ///
    /// Unity host đọc endpoint GET /api/map/{mapId}/spawn-config khi scene load,
    /// validate toàn bộ entries, rồi spawn enemy với thông số ghi đè.
    ///
    /// spawn_json format:
    ///   [{enemy_id, hp, exp, cx, cy, is_boss, count, respawn_time}, ...]
    ///   Lặp lại nhiều entry cho cùng enemy_id để tạo nhiều vị trí spawn khác nhau.
    ///   hp=0 hoặc exp=0 → Unity host fallback về base_hp/exp_reward trong bảng enemy.
    ///
    /// drop_json format:
    ///   [{enemy_id, items:[{item_id, rate, qty_min, qty_max}]}, ...]
    ///   rate: 0.0–1.0 (0.25 = 25%). Một entry per enemy_id duy nhất.
    /// </summary>
    public class MapSpawnConfig
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>FK → map_config.map_id — một map chỉ có một dòng config.</summary>
        [Column("map_id")]
        [Required]
        public int MapId { get; set; }

        /// <summary>
        /// JSON array danh sách vị trí spawn.
        /// Ví dụ một entry:
        /// {"enemy_id":1,"hp":200,"exp":50,"cx":100.5,"cy":60.0,"is_boss":false,"count":2,"respawn_time":30}
        /// </summary>
        [Column("spawn_json", TypeName = "longtext")]
        public string SpawnJson { get; set; } = "[]";

        /// <summary>
        /// JSON array tỉ lệ drop per enemy_id.
        /// Ví dụ:
        /// [{"enemy_id":1,"items":[{"item_id":10,"rate":0.25,"qty_min":1,"qty_max":1}]}]
        /// </summary>
        [Column("drop_json", TypeName = "longtext")]
        public string DropJson { get; set; } = "[]";

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
