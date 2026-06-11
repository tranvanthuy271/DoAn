using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerApi.Models.Entities
{
    // Master data cho NPC trong game.
    [Table("npc_config")]
    public class NpcConfig
    {
        [Key]
        [Column("npc_id")]
        public int NpcId { get; set; }

        [Column("npc_name")]
        [MaxLength(100)]
        public string NpcName { get; set; } = "";

        // shop | quest | blacksmith | exchange | event
        [Column("npc_type")]
        [MaxLength(20)]
        public string NpcType { get; set; } = "shop";

        [Column("map_id")]
        public int MapId { get; set; }

        [Column("pos_x")]
        public float PosX { get; set; }

        [Column("pos_y")]
        public float PosY { get; set; }

        // Key khởi đầu trong bảng npc_dialogue.
        [Column("dialogue_key")]
        [MaxLength(50)]
        public string? DialogueKey { get; set; }

        [Column("icon_id")]
        [MaxLength(50)]
        public string? IconId { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        // JSON config shop per NPC (LangLa-style).
        // Format: {"shop_name":"Binh Khí","items":[{"item_template_id":200,"price_silver":1000,"price_gold":0,"stock":-1,"level_need":1}]}
        // null = NPC không có shop.
        // idClass: 0=Tất Cả 1=Hỏa 2=Thủy 3=Thổ 4=Lôi(Kim) 5=Mộc 6=Phong
        [Column("shop_items_json")]
        public string? ShopItemsJson { get; set; }
    }
}
