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
        /// <summary>world_travel | enter_dungeon | exit_dungeon</summary>
        public string PortalType { get; set; } = "world_travel";

        /// <summary>left | right | none — hướng trên UI nút chuyển map</summary>
        public string PortalDirection { get; set; } = "none";

        /// <summary>Cần item này trong túi đồ (NULL = không cần)</summary>
        public int? RequiredItemId { get; set; }

        public int? RequiredLevel { get; set; }

        public int? RequiredQuestId { get; set; }

        /// <summary>Phó bản sở hữu cổng này (NULL = open world)</summary>
        public int? DungeonId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
