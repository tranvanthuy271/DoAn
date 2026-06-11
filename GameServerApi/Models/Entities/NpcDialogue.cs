using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerApi.Models.Entities
{
    // Cây hội thoại cho NPC. Mỗi row là một node trong cây.
    [Table("npc_dialogue")]
    public class NpcDialogue
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("npc_id")]
        public int NpcId { get; set; }

        // Key định danh node, VD: "greet", "quest_intro", "shop_open".
        [Column("dialogue_key")]
        [MaxLength(50)]
        public string DialogueKey { get; set; } = "";

        [Column("text_vi")]
        [MaxLength(1000)]
        public string TextVi { get; set; } = "";

        // Key của node tiếp theo (null = kết thúc hội thoại).
        [Column("next_key")]
        [MaxLength(50)]
        public string? NextKey { get; set; }

        // none | open_shop | give_quest | teleport
        [Column("action_type")]
        [MaxLength(20)]
        public string ActionType { get; set; } = "none";

        [ForeignKey(nameof(NpcId))]
        public NpcConfig? Npc { get; set; }
    }
}
