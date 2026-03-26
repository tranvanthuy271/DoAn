using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerApi.Models.Entities
{
    /// <summary>
    /// Bảng map_zone_config — Phân vùng zone trong map.
    /// Kiến trúc 1 port: tất cả zone dùng cùng host_ip và 1 port NGO duy nhất.
    /// Phân biệt zone bằng room_id logic (không cần disconnect/reconnect).
    /// </summary>
    [Table("map_zone_config")]
    public class MapZoneConfig
    {
        [Key]
        [Column("zone_id")]
        public int ZoneId { get; set; }

        [Column("map_id")]
        public int MapId { get; set; }

        /// <summary>Thứ tự zone trong map: 0, 1, 2, ...</summary>
        [Column("zone_index")]
        public int ZoneIndex { get; set; }

        [Column("zone_name")]
        public string ZoneName { get; set; } = "";

        /// <summary>
        /// Định danh logic của zone — không liên quan đến port.
        /// VD: "map1_zone0", "map1_zone1".
        /// Server dùng để nhóm client và lọc broadcast.
        /// </summary>
        [Column("room_id")]
        public string RoomId { get; set; } = "";

        /// <summary>IP của NGO server duy nhất (tất cả zone dùng chung)</summary>
        [Column("host_ip")]
        public string HostIp { get; set; } = "localhost";

        // Vùng trigger trong Unity (khớp với BoxCollider2D của ZoneTrigger)
        [Column("trigger_x_min")] public float TriggerXMin { get; set; }
        [Column("trigger_x_max")] public float TriggerXMax { get; set; }
        [Column("trigger_y_min")] public float TriggerYMin { get; set; }
        [Column("trigger_y_max")] public float TriggerYMax { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;
    }
}

