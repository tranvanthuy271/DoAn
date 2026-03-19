using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerApi.Models.Entities
{
    /// <summary>
    /// Master data cho NPC trong game.
    /// </summary>
    [Table("npc_config")]
    public class NpcConfig
    {
        [Key]
        [Column("npc_id")]
        public int NpcId { get; set; }

        [Column("npc_name")]
        [MaxLength(100)]
        public string NpcName { get; set; } = "";

        /// <summary>shop | quest | blacksmith | exchange | event</summary>
        [Column("npc_type")]
        [MaxLength(20)]
        public string NpcType { get; set; } = "shop";

        [Column("map_id")]
        public int MapId { get; set; }

        [Column("pos_x")]
        public float PosX { get; set; }

        [Column("pos_y")]
        public float PosY { get; set; }

        /// <summary>Key khởi đầu trong bảng npc_dialogue.</summary>
        [Column("dialogue_key")]
        [MaxLength(50)]
        public string? DialogueKey { get; set; }

        [Column("icon_id")]
        [MaxLength(50)]
        public string? IconId { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;
    }
}
