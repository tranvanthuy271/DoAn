using UnityEngine;

/// <summary>
/// ScriptableObject config cho hiệu ứng skill (buff/debuff).
///
/// === DEBUFF (hiệu ứng bất lợi — gây lên ENEMY hoặc player địch) ===
///   isBuff = false  →  điền các trường ở mục "Debuff Settings"
///   Ví dụ:
///     Slow_Config       : debuffType=Slow,        debuffValue=50,  debuffDuration=3
///     Weaken_Config     : debuffType=Weaken,       debuffValue=20,  debuffDuration=5
///     Burn_Config       : debuffType=Burn,         debuffValue=10,  debuffDuration=4
///     Freeze_Config     : debuffType=Freeze,       debuffValue=0,   debuffDuration=2
///     DefenseDown_Config: debuffType=DefenseDown,  debuffValue=30,  debuffDuration=5
///
/// === BUFF (hiệu ứng có lợi — gây lên ĐỒNG ĐỘI trong cùng tổ đội) ===
///   isBuff = true  →  điền các trường ở mục "Buff Settings"
///   Khi đồng đội nhận buff:
///     • Sprite của họ bị tô màu buffTintColor (fade out theo countdown)
///     • Icon xuất hiện trên đầu (OverheadStatusDisplay) với vòng đếm ngược
///     • Icon xuất hiện trong BuffHudPanel với countdown
///   Ví dụ:
///     Buff_WaterArmor_Config: isBuff=true, buffName="Thủy Giáp Hộ Thể", buffTintColor=cyan
///     Buff_EarthAura_Config : isBuff=true, buffName="Địa Uy Khí",        buffTintColor=gold
///
/// Cách tạo asset:  Assets → Create → DoAn → Skill Effect Config
/// Gắn vào: SkillData.effectConfig trong Inspector của PlayerSkillManager.
/// </summary>
[CreateAssetMenu(fileName = "SkillEffectConfig", menuName = "DoAn/Skill Effect Config")]
public class SkillEffectConfig : ScriptableObject
{
    // ═══════════════════════════════════════════════════════════════════════
    //  DEBUFF (bất lợi — áp lên kẻ địch / player bị trúng skill)
    // ═══════════════════════════════════════════════════════════════════════
    [Header("── DEBUFF (gây lên kẻ địch) ──────────────────────")]
    [Tooltip("Loại hiệu ứng bất lợi.\n  None = không có debuff (chỉ dùng buff).")]
    public SkillDebuffType debuffType = SkillDebuffType.None;

    [Tooltip("Giá trị hiệu ứng:\n  Slow       = % giảm tốc độ di chuyển (50 = chậm 50%)\n  Weaken     = % giảm tấn công\n  Burn       = damage/giây mỗi tick\n  Freeze     = bỏ qua (luôn stun toàn phần)\n  DefenseDown= % giảm phòng thủ")]
    [Range(1, 200)]
    public int debuffValue = 30;

    [Tooltip("Thời gian debuff kéo dài (giây).")]
    [Min(0.5f)]
    public float debuffDuration = 3f;

    [Tooltip("Tên debuff ngắn hiển thị trên đầu nhân vật bị ảnh hưởng.")]
    public string debuffName = "Chậm";

    [Tooltip("Màu sprite tint khi bị debuff này (alpha = cường độ tối đa, fade out theo countdown).")]
    public Color debuffTintColor = new Color(1f, 0.3f, 0.1f, 0.6f);

    // ═══════════════════════════════════════════════════════════════════════
    //  BUFF (có lợi — áp lên ĐỒNG ĐỘI trong cùng tổ đội)
    // ═══════════════════════════════════════════════════════════════════════
    [Header("── BUFF (gây lên đồng đội cùng tổ đội) ───────────")]
    [Tooltip("TRUE = skill này là buff có lợi cho đồng đội.\nKhi bật:\n  • Sprite đồng đội tô màu buffTintColor + fade out theo countdown\n  • Icon xuất hiện trên đầu (OverheadStatusDisplay)\n  • Icon xuất hiện trong BuffHudPanel với countdown")]
    public bool isBuff = false;

    [Tooltip("Tên buff hiển thị trên đầu và trong HUD (chỉ dùng khi isBuff = true).")]
    public string buffName = "";

    [Tooltip("Thời gian buff kéo dài (giây). Nên khớp với buffDuration trong BuffSkill script.")]
    [Min(0.5f)]
    public float buffDuration = 5f;

    [Tooltip("Màu tô sprite đồng đội khi nhận buff (alpha = cường độ tối đa, fade out theo countdown).\nGợi ý: WaterArmor = cyan (0.2, 0.8, 1, 0.6) | EarthAura = vàng (1, 0.85, 0.1, 0.5)")]
    [ColorUsage(showAlpha: true)]
    public Color buffTintColor = new Color(0.2f, 0.8f, 1f, 0.6f);

    // ═══════════════════════════════════════════════════════════════════════
    //  UI CHUNG (icon + outline ring)
    // ═══════════════════════════════════════════════════════════════════════
    [Header("── UI Icon (dùng chung cho buff và debuff) ────────")]
    [Tooltip("ID icon trong Resources/ItemIcons/{iconId}.png\nDebuff thường: 201=Slow, 202=Weaken, 203=Burn, 204=Freeze, 205=DefenseDown\nBuff thường  : 151=WaterArmor, 152=EarthAura")]
    public int iconId = 201;

    [Tooltip("Màu vòng countdown (ring) trên icon trong OverheadStatusDisplay.\nDebuff = đỏ, Buff = vàng.")]
    [ColorUsage(showAlpha: true)]
    public Color ringColor = new Color(1f, 0.2f, 0.2f, 1f);

    // ═══════════════════════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════════════════════
    public string GetDisplayName() => isBuff ? buffName : debuffName;
    public Color  GetTintColor()   => isBuff ? buffTintColor : debuffTintColor;
}
