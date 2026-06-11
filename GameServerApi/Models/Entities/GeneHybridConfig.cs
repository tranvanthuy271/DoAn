using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerApi.Models
{
    // Entity cho bảng gene_hybrid_config.
    // Lưu config 10 tổ hợp Hybrid Gene (5 hệ chọn 2, không phân biệt thứ tự).
    [Table("gene_hybrid_config")]
    public class GeneHybridConfig
    {
        [Key]
        [Column("hybrid_id")]
        public int HybridId { get; set; }

        // Hệ A — alphabet nhỏ hơn để đảm bảo unique key.
        [Column("element_a")]
        [MaxLength(10)]
        public string ElementA { get; set; } = "";

        // Hệ B — alphabet lớn hơn.
        [Column("element_b")]
        [MaxLength(10)]
        public string ElementB { get; set; } = "";

        [Column("hybrid_name")]
        [MaxLength(100)]
        public string HybridName { get; set; } = "";

        [Column("hybrid_description")]
        [MaxLength(500)]
        public string? HybridDescription { get; set; }

        // CSV danh sách hệ bị sát thương tăng (union của 2 hệ gốc khắc).
        // Ví dụ: "Earth,Fire"
        [Column("bonus_target_elements")]
        [MaxLength(100)]
        public string BonusTargetElements { get; set; } = "";

        // CSV danh sách hệ không còn khắc được player (union của hệ khắc 2 hệ gốc).
        // Ví dụ: "Water,Metal"
        [Column("immune_elements")]
        [MaxLength(100)]
        public string ImmuneElements { get; set; } = "";

        // Vàng tiêu hao khi fusion.
        [Column("fusion_silver_cost")]
        public int FusionGoldCost { get; set; } = 2_000_000;

        // FK → item_template.id (Lõi Đột Biến).
        [Column("fusion_item_id")]
        public int FusionItemId { get; set; }

        // Số lượng item cần dùng để fusion.
        [Column("fusion_item_count")]
        public int FusionItemCount { get; set; } = 5;

        // Phần trăm ATK bonus lên hệ bị khắc kép. Mặc định 0.5 = +50%.
        [Column("atk_bonus_percent")]
        public float AtkBonusPercent { get; set; } = 0.5f;

        [Column("stat_bonus_hp")]
        public int StatBonusHp { get; set; } = 2000;

        [Column("stat_bonus_mp")]
        public int StatBonusMp { get; set; } = 500;

        [Column("stat_bonus_atk")]
        public int StatBonusAtk { get; set; } = 500;

        [Column("stat_bonus_def")]
        public int StatBonusDef { get; set; } = 200;

        // Path dùng với Resources.Load để spawn đúng prefab hybrid.
        // Ví dụ: "Prefabs/Player/Hybrid/Hybrid_Metal_Wood"
        [Column("prefab_path")]
        [MaxLength(200)]
        public string PrefabPath { get; set; } = "";

        // Số skill slot từ hệ chính được giữ lại sau fusion (mặc định 3).
        [Column("primary_skill_keep_count")]
        public int PrimarySkillKeepCount { get; set; } = 3;

        // Hàm hỗ trợ dùng nội bộ để tách nhỏ xử lý chính.
        // Parse BonusTargetElements CSV thành List.
        public List<string> GetBonusTargets() =>
            string.IsNullOrWhiteSpace(BonusTargetElements)
                ? new()
                : [.. BonusTargetElements.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

        // Parse ImmuneElements CSV thành List.
        public List<string> GetImmuneElements() =>
            string.IsNullOrWhiteSpace(ImmuneElements)
                ? new()
                : [.. ImmuneElements.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

        // Trả về key chuẩn hoá (element nhỏ hơn trước) để tra cứu.
        public static (string a, string b) NormalizeKey(string e1, string e2) =>
            string.Compare(e1, e2, StringComparison.OrdinalIgnoreCase) <= 0
                ? (e1, e2)
                : (e2, e1);
    }
}
