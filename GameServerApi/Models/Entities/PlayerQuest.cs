using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerApi.Models.Entities
{
    [Table("player_quest")]
    public class PlayerQuest
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("player_id")]
        public int PlayerId { get; set; }

        [Column("quest_config_id")]
        public int QuestConfigId { get; set; }

        /// <summary>active | completed</summary>
        [Column("status")]
        [MaxLength(20)]
        public string Status { get; set; } = "active";

        [Column("current_step_index")]
        public int CurrentStepIndex { get; set; } = 0;

        /// <summary>
        /// JSON: {"0":3} = bước 0 đã đạt 3 lần tiến trình
        /// </summary>
        [Column("progress_json")]
        public string ProgressJson { get; set; } = "{}";

        [Column("accepted_at")]
        public DateTime AcceptedAt { get; set; } = DateTime.UtcNow;

        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }

        // ── Navigation ─────────────────────────────────────────────────
        [ForeignKey("QuestConfigId")]
        public QuestConfig? Quest { get; set; }
    }
}
