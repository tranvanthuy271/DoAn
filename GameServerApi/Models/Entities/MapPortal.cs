using System;

namespace GameServerApi.Models.Entities
{
    /// <summary>
    /// Bảng map_portal — Cổng dịch chuyển giữa các map/phòng trong phó bản.
    ///
    /// Pattern từ LangLa: WayPoint (dataWayPoint)
    ///   mapHere  → SourceMapId
    ///   mapNext  → DestMapId
    ///   l,m,n,o  → SrcX, SrcY + SrcRadius (vùng trigger)
    ///   p,q      → DestX, DestY (điểm đến)
    /// </summary>
    public class MapPortal
    {
        public int PortalId { get; set; }
        public string PortalName { get; set; } = "";

        // ── Nguồn ──
        public int SourceMapId { get; set; }
        public float SrcX { get; set; } = 0f;
        public float SrcY { get; set; } = 0f;
        /// <summary>Bán kính vùng trigger (server dùng để validate vị trí player)</summary>
        public float SrcRadius { get; set; } = 2.0f;

        // ── Đích ──
        public int DestMapId { get; set; }
        public string DestSceneName { get; set; } = "";
        public float DestX { get; set; } = 0f;
        public float DestY { get; set; } = 0f;

        // ── Loại cổng ──
        /// <summary>enter_dungeon | room_transition | exit_dungeon | world_travel</summary>
        public string PortalType { get; set; } = "room_transition";

        /// <summary>Cần item này trong túi đồ (NULL = không cần)</summary>
        public int? RequiredItemId { get; set; }

        /// <summary>Phó bản sở hữu cổng này (NULL = open world)</summary>
        public int? DungeonId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
