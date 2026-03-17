using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerApi.Models
{
    /// <summary>
    /// Entity cho bảng gene_multi_config.
    /// Lưu config nâng cấp hệ gene THỨ 2 (secondary element) với chi phí cao hơn ~20%.
    /// </summary>
    [Table("gene_multi_config")]
    public class GeneMultiConfig
    {
        [Column("tier_from")]
        public int TierFrom { get; set; }

        [Column("element_type")]
        [MaxLength(10)]
        public string ElementType { get; set; } = "";

        /// <summary>Gene exp cần tích luỹ trước khi được phép nâng cấp.</summary>
        [Column("gene_exp_required")]
        public int GeneExpRequired { get; set; }

        /// <summary>Vàng (gold) tiêu hao khi thực hiện nâng cấp.</summary>
        [Column("silver_cost")]
        public int GoldCost { get; set; }

        [Column("stone_id")]
        public int ItemId { get; set; }

        [Column("stone_needed")]
        public int ItemsNeeded { get; set; }

        [Column("stone_min")]
        public int ItemsMin { get; set; }

        /// <summary>Tỉ lệ thành công khi dùng đủ ItemsNeeded item. 0.0 – 1.0.</summary>
        [Column("base_success_rate")]
        public float BaseSuccessRate { get; set; }
    }
}
