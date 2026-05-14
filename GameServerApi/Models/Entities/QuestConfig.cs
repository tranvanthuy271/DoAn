using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameServerApi.Models.Entities
{
    [Table("quest_config")]
    public class QuestConfig
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        [MaxLength(100)]
        public string Name { get; set; } = "";

        [Column("description")]
        [MaxLength(500)]
        public string Description { get; set; } = "";

        [Column("level_need")]
        public int LevelNeed { get; set; } = 1;

        [Column("npc_giver_id")]
        public int NpcGiverId { get; set; }

        [Column("npc_receiver_id")]
        public int NpcReceiverId { get; set; }

        /// <summary>
        /// JSON array: [{"type":"kill","target_id":1,"target_name":"Goblin","required_count":5}]
        /// type values: kill | collect | talk
        /// </summary>
        [Column("steps_json")]
        public string StepsJson { get; set; } = "[]";

        /// <summary>
        /// JSON: {"exp":500,"gold":50,"silver":0}
        /// </summary>
        [Column("rewards_json")]
        public string RewardsJson { get; set; } = "{}";

        [Column("sort_order")]
        public int SortOrder { get; set; } = 0;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        // ── Helpers (not mapped) ──────────────────────────────────────

        [NotMapped]
        public List<QuestStep> Steps =>
            JsonSerializer.Deserialize<List<QuestStep>>(StepsJson) ?? new List<QuestStep>();

        [NotMapped]
        public QuestReward Reward =>
            JsonSerializer.Deserialize<QuestReward>(RewardsJson) ?? new QuestReward();
    }

    public class QuestStep
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "kill"; // kill | collect | talk

        [JsonPropertyName("target_id")]
        public int TargetId { get; set; }

        [JsonPropertyName("target_name")]
        public string TargetName { get; set; } = "";

        [JsonPropertyName("required_count")]
        public int RequiredCount { get; set; } = 1;
    }

    public class QuestReward
    {
        [JsonPropertyName("exp")]
        public int Exp { get; set; }

        [JsonPropertyName("gold")]
        public int Gold { get; set; }

        [JsonPropertyName("silver")]
        public int Silver { get; set; }
    }
}
