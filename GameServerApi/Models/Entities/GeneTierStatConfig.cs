using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameServerApi.Models
{
    // ----------------------------------------------------------------
    // gene_tier_stat_config
    //   element_type : 'Fire'|'Water'|'Earth'|'Metal'|'Wood'  (PK)
    //   tier_to      : tier đạt được sau khi nâng cấp (2..5)  (PK)
    //   hp_bonus     : max_hp cộng thêm khi đạt tier này
    //   mp_bonus     : max_mp cộng thêm
    //   attack_bonus : attack cộng thêm
    //   defense_bonus: defense cộng thêm
    //
    // Mỗi hệ có thể có stat bonus riêng tại mỗi tier.
    // Server đọc bảng này thay vì dùng hardcode dictionary.
    // ----------------------------------------------------------------
    [Table("gene_tier_stat_config")]
    [PrimaryKey(nameof(ElementType), nameof(TierTo))]
    public class GeneTierStatConfig
    {
        [Column("element_type")]
        [MaxLength(10)]
        public string ElementType { get; set; } = "";

        /// <summary>Tier mà player đạt được sau upgrade thành công (2, 3, 4, 5)</summary>
        [Column("tier_to")]
        public int TierTo { get; set; }

        [Column("hp_bonus")]
        public int HpBonus { get; set; }

        [Column("mp_bonus")]
        public int MpBonus { get; set; }

        [Column("attack_bonus")]
        public int AttackBonus { get; set; }

        [Column("defense_bonus")]
        public int DefenseBonus { get; set; }
    }
}
