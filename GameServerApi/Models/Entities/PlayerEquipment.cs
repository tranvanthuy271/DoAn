using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerApi.Models.Entities
{
    /// <summary>
    /// Normalized player equipment — replaces equipment JSON blob in player_data.
    /// One row per equipped slot per player.
    /// </summary>
    [Table("player_equipment")]
    public class PlayerEquipment
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("player_id")]
        public int PlayerId { get; set; }

        /// <summary>Slot name: helmet, weapon, armor, pants, boots, ring</summary>
        [Column("slot")]
        [MaxLength(20)]
        public string Slot { get; set; } = "";

        [Column("item_template_id")]
        public int ItemTemplateId { get; set; }

        [Column("upgrade_level")]
        public int UpgradeLevel { get; set; } = 0;

        /// <summary>Format: "optId,tierVal;optId,tierVal" — same as legacy strOptions.</summary>
        [Column("str_options")]
        [MaxLength(500)]
        public string StrOptions { get; set; } = "";

        [Column("equipped_at")]
        public DateTime EquippedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey(nameof(PlayerId))]
        public PlayerData? Player { get; set; }

        [ForeignKey(nameof(ItemTemplateId))]
        public ItemTemplate? ItemTemplate { get; set; }
    }
}
