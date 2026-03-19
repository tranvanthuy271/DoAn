using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerApi.Models.Entities
{
    /// <summary>
    /// Cây hội thoại cho NPC. Mỗi row là một node trong cây.
    /// </summary>
    [Table("npc_dialogue")]
    public class NpcDialogue
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("npc_id")]
        public int NpcId { get; set; }

        /// <summary>Key định danh node, VD: "greet", "quest_intro", "shop_open".</summary>
        [Column("dialogue_key")]
        [MaxLength(50)]
        public string DialogueKey { get; set; } = "";

        [Column("text_vi")]
        [MaxLength(1000)]
        public string TextVi { get; set; } = "";

        /// <summary>Key của node tiếp theo (null = kết thúc hội thoại).</summary>
        [Column("next_key")]
        [MaxLength(50)]
        public string? NextKey { get; set; }

        /// <summary>none | open_shop | give_quest | teleport</summary>
        [Column("action_type")]
        [MaxLength(20)]
        public string ActionType { get; set; } = "none";

        [ForeignKey(nameof(NpcId))]
        public NpcConfig? Npc { get; set; }
    }
}
