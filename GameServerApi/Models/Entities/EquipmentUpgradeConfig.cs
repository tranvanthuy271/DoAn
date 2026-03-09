using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerApi.Models
{
    // ----------------------------------------------------------------
    // equipment_upgrade_config
    //   upgrade_level     : +1 ~ +20
    //   stone_id          : FK → item_template.id  (đá nâng cấp đúng bậc)
    //   stone_needed      : số đá để đạt base_success_rate
    //   stone_min         : số đá tối thiểu được phép dùng
    //   base_success_rate : tỉ lệ khi dùng đúng stone_needed viên
    //   fail_policy       : 0=an toàn  1=-1 bậc  2=về+0
    // ----------------------------------------------------------------
    [Table("equipment_upgrade_config")]
    public class EquipmentUpgradeConfig
    {
        [Key]
        [Column("upgrade_level")]
        public int UpgradeLevel { get; set; }

        [Column("silver_cost")]
        public int SilverCost { get; set; }

        [Column("stone_id")]
        public int StoneId { get; set; }

        [Column("stone_needed")]
        public int StoneNeeded { get; set; }

        [Column("stone_min")]
        public int StoneMin { get; set; }

        [Column("base_success_rate")]
        public float BaseSuccessRate { get; set; }

        [Column("fail_policy")]
        public int FailPolicy { get; set; }
    }
}
