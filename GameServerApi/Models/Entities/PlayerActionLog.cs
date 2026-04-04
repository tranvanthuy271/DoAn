using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerApi.Models.Entities
{
    /// <summary>
    /// Audit trail for important player actions — used for fraud detection and game economy monitoring.
    /// </summary>
    [Table("player_action_log")]
    public class PlayerActionLog
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("player_id")]
        public int PlayerId { get; set; }

        /// <summary>Categorises the logged event.</summary>
        [Column("action_type")]
        [MaxLength(50)]
        public string ActionType { get; set; } = "";

        /// <summary>JSON payload with full before/after context.</summary>
        [Column("detail_json")]
        public string DetailJson { get; set; } = "{}";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey(nameof(PlayerId))]
        public PlayerData? Player { get; set; }
    }

    /// <summary>Well-known action type constants — avoids magic strings in controllers/services.</summary>
    public static class ActionTypes
    {
        public const string Login          = "login";
        public const string LevelUp        = "level_up";
        public const string EquipUpgrade   = "equip_upgrade";
        public const string GeneUpgrade    = "gene_upgrade";
        public const string Fusion         = "fusion";
        public const string ItemConsume    = "item_consume";
        public const string SkillUpgrade   = "skill_upgrade";
    }
}
