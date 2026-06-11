using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerApi.Models
{
    // Entity cho bảng gene_hybrid_skill.
    // Mỗi hybrid combination có đúng 1 combo skill đặc biệt (slot 3).
    [Table("gene_hybrid_skill")]
    public class GeneHybridSkill
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        // FK → gene_hybrid_config.hybrid_id
        [Column("hybrid_id")]
        public int HybridId { get; set; }

        // Skill code khớp với skill_template.skill_code — ví dụ: HYBRID_KIM_MOC_SLASH
        [Column("skill_code")]
        [MaxLength(50)]
        public string SkillCode { get; set; } = "";

        // Slot index trong hotbar 0-based. Hybrid skill luôn để vào slot 3 (4th slot).
        [Column("slot_priority")]
        public int SlotPriority { get; set; } = 3;

        // Navigation property
        [ForeignKey(nameof(HybridId))]
        public GeneHybridConfig? HybridConfig { get; set; }
    }
}
