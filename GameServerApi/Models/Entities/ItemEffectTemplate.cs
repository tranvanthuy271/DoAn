using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerApi.Models.Entities
{
    // item_effect_template – định nghĩa effect/buff của 1 item tiêu thụ.
    // Mỗi item có thể có nhiều effect (row per effect, cùng item_template_id).
    // effectType:
    // "HpRestore"   – hồi HP ngay lập tức (duration_sec = 0)
    // "MpRestore"   – hồi MP ngay lập tức (duration_sec = 0)
    // "HpBuff"      – tăng max HP (timed)
    // "MpBuff"      – tăng max MP (timed)
    // "AttackBuff"  – tăng % sát thương (timed)
    // "DefenseBuff" – tăng % phòng thủ (timed)
    // "GeneExpBuff" – tăng % EXP gene nạp vào (timed)
    // "ExpBuff"     – tăng % EXP nhận khi kill (timed)
    // "PhucBuff"    – phúc: +% vàng + % EXP drop (timed)
    [Table("item_effect_template")]
    public class ItemEffectTemplate
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        // FK → item_template.id
        [Column("item_template_id")]
        public int ItemTemplateId { get; set; }

        // Effect type string (see summary above).
        [Required]
        [MaxLength(50)]
        [Column("effect_type")]
        public string EffectType { get; set; } = string.Empty;

        // Giá trị: số HP/MP hồi, hoặc % tăng stat.
        [Column("value")]
        public int Value { get; set; } = 0;

        // 0 = instant; >0 = timed buff (giây).
        [Column("duration_sec")]
        public int DurationSec { get; set; } = 0;

        // Icon ID hiển thị trong HUD buff bar (0 = dùng icon của item).
        [Column("icon_id")]
        public int IconId { get; set; } = 0;

        // Tên hiển thị ngắn trong buff tooltip.
        [MaxLength(200)]
        [Column("display_name")]
        public string DisplayName { get; set; } = string.Empty;

        // Mô tả chi tiết chỉ số được áp dụng.
        [MaxLength(500)]
        [Column("detail")]
        public string Detail { get; set; } = string.Empty;

        // Thứ tự hiển thị khi item có nhiều effect.
        [Column("sort_order")]
        public int SortOrder { get; set; } = 0;
    }
}
