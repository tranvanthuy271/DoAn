using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerApi.Models.Entities
{
    /// <summary>
    /// Item bán trong shop của NPC.
    /// </summary>
    [Table("npc_shop_item")]
    public class NpcShopItem
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("npc_id")]
        public int NpcId { get; set; }

        [Column("item_template_id")]
        public int ItemTemplateId { get; set; }

        /// <summary>Giá bạc (0 = miễn phí).</summary>
        [Column("price_silver")]
        public int PriceSilver { get; set; }

        /// <summary>Giá vàng (0 = dùng bạc).</summary>
        [Column("price_gold")]
        public int PriceGold { get; set; }

        /// <summary>-1 = không giới hạn.</summary>
        [Column("stock")]
        public int Stock { get; set; } = -1;

        [Column("required_level")]
        public int RequiredLevel { get; set; } = 1;

        [ForeignKey(nameof(NpcId))]
        public NpcConfig? Npc { get; set; }

        [ForeignKey(nameof(ItemTemplateId))]
        public ItemTemplate? ItemTemplate { get; set; }
    }
}
