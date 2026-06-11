using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameServerApi.Models
{
    // gene_upgrade_config
    //   tier_from        : gene tier hiện tại (PK, 1~4)
    //   element_type     : 'Fire'|'Water'|'Earth'|'Metal'|'Wood' (PK)
    //   gene_exp_required: gene_exp cần có trước khi nâng cấp
    //   gold_cost        : vàng tiêu hao (cột DB là silver_cost – dùng làm gold)
    //   item_id          : item_template.id cần dùng (cột DB là stone_id)
    //   items_needed     : số item để đạt base_success_rate tối đa
    //   items_min        : số item tối thiểu để thực hiện nâng cấp
    //   base_success_rate: tỉ lệ thành công khi dùng đủ items_needed item
    [Table("gene_upgrade_config")]
    [PrimaryKey(nameof(TierFrom), nameof(ElementType))]
    public class GeneUpgradeConfig
    {
        [Column("tier_from")]
        public int TierFrom { get; set; }

        [Column("element_type")]
        [MaxLength(10)]
        public string ElementType { get; set; } = "";

        [Column("gene_exp_required")]
        public int GeneExpRequired { get; set; }

        // Cột DB: silver_cost – dùng làm gold_cost
        [Column("silver_cost")]
        public int GoldCost { get; set; }

        // Cột DB: stone_id – id của item_template cần dùng
        [Column("stone_id")]
        public int ItemId { get; set; }

        // Cột DB: stone_needed – số item để đạt tỉ lệ thành công tối đa
        [Column("stone_needed")]
        public int ItemsNeeded { get; set; }

        // Cột DB: stone_min – số item tối thiểu
        [Column("stone_min")]
        public int ItemsMin { get; set; }

        [Column("base_success_rate")]
        public float BaseSuccessRate { get; set; }
    }
}
