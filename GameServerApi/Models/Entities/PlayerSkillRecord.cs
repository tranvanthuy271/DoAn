using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerApi.Models.Entities
{
    /// <summary>
    /// Normalized player skill record — replaces skills JSON blob in player_data.
    /// One row per unlocked skill per player.
    /// </summary>
    [Table("player_skill_record")]
    public class PlayerSkillRecord
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("player_id")]
        public int PlayerId { get; set; }

        [Column("skill_id")]
        public int SkillId { get; set; }

        [Column("skill_level")]
        public int SkillLevel { get; set; } = 1;

        [Column("is_equipped")]
        public bool IsEquipped { get; set; } = false;

        /// <summary>Hotbar slot index (0-5), -1 if not on hotbar.</summary>
        [Column("hotbar_slot")]
        public int HotbarSlot { get; set; } = -1;

        // Navigation
        [ForeignKey(nameof(PlayerId))]
        public PlayerData? Player { get; set; }

        [ForeignKey(nameof(SkillId))]
        public SkillTemplate? SkillTemplate { get; set; }
    }
}
