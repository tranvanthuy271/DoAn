using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerApi.Models
{
    // item_template  (DB v3.0 – LangLa schema)
    //
    // type: 0=Helmet 1=Weapon 2=Armor 3=Pants 4=Boots 5=Ring
    //       21=UpgradeStone 22=HPPotion 23=MPPotion 25=GeneStone 30=Material
    // gioiTinh : 0=Male 1=Female 2=All
    // idClass  : 0=All 1=Fire 2=Water 3=Earth 4=Metal 5=Wood
    [Table("item_template")]
    public class ItemTemplate
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("detail")]
        public string? Detail { get; set; }

        // 'True' / 'False' string (LangLa convention)
        [Column("isXepChong")]
        public string IsXepChong { get; set; } = "False";

        // 0=Male 1=Female 2=All
        [Column("gioiTinh")]
        public int GioiTinh { get; set; } = 2;

        // 0=Helmet 1=Weapon 2=Armor 3=Pants 4=Boots 5=Ring 21=UpgStone …
        [Column("type")]
        public int Type { get; set; }

        // 0=All 1=Fire 2=Water 3=Earth 4=Metal 5=Wood
        [Column("idClass")]
        public int IdClass { get; set; } = 0;

        [Column("idIcon")]
        public int IdIcon { get; set; } = 0;

        [Column("levelNeed")]
        public int LevelNeed { get; set; } = 1;

        [Column("taiPhuNeed")]
        public int TaiPhuNeed { get; set; } = 0;

        [Column("idMob")]
        public int IdMob { get; set; } = -1;

        [Column("idChar")]
        public int IdChar { get; set; } = 0;

        // Item bị khóa (không bán/bỏ được) — đây là thuộc tính của loại item, không phải instance. VD: 'bạc khóa'
        [Column("isLock")]
        public bool IsLock { get; set; } = false;

        // Giá bán lại cho NPC (đơn vị bạc)
        [Column("sellPrice")]
        public int SellPrice { get; set; } = 0;
    }
}
