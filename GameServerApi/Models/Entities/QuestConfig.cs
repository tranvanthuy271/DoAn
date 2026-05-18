using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameServerApi.Models.Entities
{
    /// <summary>
    /// Bảng quest_config — cấu hình nhiệm vụ (1 bảng duy nhất, inspired by LangLa task table).
    /// Tiến trình của người chơi được lưu trong player_data.info_char (không có bảng riêng).
    /// </summary>
    [Table("quest_config")]
    public class QuestConfig
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        [MaxLength(200)]
        public string Name { get; set; } = "";

        [Column("level_need")]
        public int LevelNeed { get; set; } = 1;

        /// <summary>NPC nhận và giao nhiệm vụ (cùng 1 NPC).</summary>
        [Column("npc_id")]
        public int NpcId { get; set; } = 0;

        /// <summary>Hội thoại khi nhận nhiệm vụ.</summary>
        [Column("str1")]
        public string Str1 { get; set; } = "";

        /// <summary>Hội thoại khi nộp/hoàn thành nhiệm vụ.</summary>
        [Column("str2")]
        public string Str2 { get; set; } = "";

        /// <summary>Ghi chú / hướng dẫn.</summary>
        [Column("str3")]
        public string Str3 { get; set; } = "";

        [Column("exp_reward")]
        public int ExpReward { get; set; } = 0;

        [Column("gold_reward")]
        public int GoldReward { get; set; } = 0;

        [Column("silver_reward")]
        public int SilverReward { get; set; } = 0;

        /// <summary>Vật phẩm thưởng. Format: "itemId@quantity,itemId@quantity".</summary>
        [Column("item_reward")]
        [MaxLength(500)]
        public string ItemReward { get; set; } = "";

        /// <summary>
        /// JSON steps: [{id,name,idMob,idNpc,idItem,idMap,x,y,require,STR}]
        /// id type: 0=kill mob, 1=collect item, 5=talk to NPC, 9=reach map
        /// </summary>
        [Column("step")]
        public string StepJson { get; set; } = "[]";

        [Column("sort_order")]
        public int SortOrder { get; set; } = 0;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        // ── Helper (not mapped) ───────────────────────────────────────

        [NotMapped]
        public List<QuestStep> Steps =>
            JsonSerializer.Deserialize<List<QuestStep>>(StepJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? new List<QuestStep>();
    }

    /// <summary>
    /// Bước nhiệm vụ — theo format JSON của LangLa.
    /// id type: 0=kill mob, 1=collect item, 5=talk to NPC, 9=reach map.
    /// </summary>
    public class QuestStep
    {
        /// <summary>Loại bước: 0=kill, 1=collect, 5=talk, 9=reach map.</summary>
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        /// <summary>ID quái cần giết (-1 = không áp dụng).</summary>
        [JsonPropertyName("idMob")]
        public int IdMob { get; set; } = -1;

        /// <summary>ID NPC cần nói chuyện (-1 = không áp dụng).</summary>
        [JsonPropertyName("idNpc")]
        public int IdNpc { get; set; } = -1;

        /// <summary>ID item cần thu thập (-1 = không áp dụng).</summary>
        [JsonPropertyName("idItem")]
        public int IdItem { get; set; } = -1;

        /// <summary>ID map (-1 = bất kỳ map).</summary>
        [JsonPropertyName("idMap")]
        public int IdMap { get; set; } = -1;

        [JsonPropertyName("x")]
        public int X { get; set; } = 0;

        [JsonPropertyName("y")]
        public int Y { get; set; } = 0;

        /// <summary>Số lần / số lượng cần để hoàn thành bước.</summary>
        [JsonPropertyName("require")]
        public int Require { get; set; } = 1;

        /// <summary>Hội thoại phụ (dùng cho bước loại talk=5).</summary>
        [JsonPropertyName("STR")]
        public string Str { get; set; } = "";
    }
}
