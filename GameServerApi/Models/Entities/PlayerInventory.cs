using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerApi.Models.Entities
{
    // Normalized player inventory — replaces inventory JSON blob in player_data.
    // One row per item slot per player.
    [Table("player_inventory")]
    public class PlayerInventory
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("player_id")]
        public int PlayerId { get; set; }

        [Column("item_template_id")]
        public int ItemTemplateId { get; set; }

        [Column("quantity")]
        public int Quantity { get; set; } = 1;

        [Column("slot_index")]
        public int SlotIndex { get; set; } = 0;

        [Column("upgrade_level")]
        public int UpgradeLevel { get; set; } = 0;

        [Column("str_options")]
        [MaxLength(500)]
        public string StrOptions { get; set; } = "";

        [Column("is_locked")]
        public bool IsLocked { get; set; } = false;

        [Column("acquired_at")]
        public DateTime AcquiredAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey(nameof(PlayerId))]
        public PlayerData? Player { get; set; }

        [ForeignKey(nameof(ItemTemplateId))]
        public ItemTemplate? ItemTemplate { get; set; }
    }
}
