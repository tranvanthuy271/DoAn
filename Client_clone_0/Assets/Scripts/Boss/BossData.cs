using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
//  BossData  —  ScriptableObject config cho từng con Boss
//  Tạo asset: Assets → Create → Game/Boss/Boss Data
// ─────────────────────────────────────────────────────────────────────────────

[CreateAssetMenu(fileName = "BossData", menuName = "Game/Boss/Boss Data")]
public class BossData : ScriptableObject
{
    // ── Thông tin cơ bản ────────────────────────────────────────────────────
    [Header("Thông Tin Cơ Bản")]
    public string bossName      = "Boss";
    public int    maxHealth     = 1000;
    public int    level         = 10;
    public int    expReward     = 500;

    // ── Di chuyển ───────────────────────────────────────────────────────────
    [Header("Di Chuyển")]
    public float moveSpeed        = 2f;
    public float chaseSpeed       = 3.5f;
    public float detectionRange   = 12f;
    public float meleeAttackRange = 1.8f;

    [Header("Nhảy (tương tự người chơi)")]
    public bool  canJump     = false;
    public float jumpForce   = 8f;
    public int   maxJumps    = 1;   // 1 = nhảy đơn, 2 = double jump

    [Header("Bay Lượn")]
    public bool  canFly       = false;
    [Tooltip("Chiều cao Y so với vị trí ban đầu khi bay")]
    public float flyHeight    = 3f;
    public float flySpeed     = 4f;

    // ── Né tránh ────────────────────────────────────────────────────────────
    [Header("Né Tránh Skill Người Chơi")]
    [Range(0f, 100f)]
    [Tooltip("Xác suất né (%). 0 = không né, 100 = luôn né")]
    public float dodgeChance    = 20f;
    [Tooltip("Thời gian hồi chiêu né (giây)")]
    public float dodgeCooldown  = 3f;
    [Tooltip("Khoảng cách dịch chuyển khi né")]
    public float dodgeDistance  = 2f;

    // ── Sát thương phản lại ─────────────────────────────────────────────────
    [Header("Sát Thương Cố Định Khi Bị Đánh")]
    [Tooltip("Kích hoạt: mỗi lần bị đánh, boss trả lại một lượng damage cố định cho kẻ tấn công")]
    public bool returnDamageEnabled = false;
    [Min(0)]
    public int  returnDamageAmount  = 10;

    // ── Tự hồi HP ───────────────────────────────────────────────────────────
    [Header("Tự Hồi HP")]
    public bool  hpRegenEnabled      = false;
    [Range(0f, 100f)]
    [Tooltip("Bắt đầu hồi khi HP ≤ ngưỡng này (%)")]
    public float regenThresholdPct   = 50f;
    [Tooltip("HP hồi mỗi giây")]
    public float regenPerSec         = 5f;

    // ── Kháng nguyên tố ─────────────────────────────────────────────────────
    [Header("Kháng Nguyên Tố (0–100 %)")]
    [Range(0, 100)] public int khangHoa   = 0;
    [Range(0, 100)] public int khangThuy  = 0;
    [Range(0, 100)] public int khangTho   = 0;
    [Range(0, 100)] public int khangMoc   = 0;
    [Range(0, 100)] public int khangKim   = 0;
    [Range(0, 100)] public int khangPhong = 0;

    // ── Kỹ năng ─────────────────────────────────────────────────────────────
    [Header("Kỹ Năng Đánh Thường")]
    public BossNormalAttackConfig normalAttack = new BossNormalAttackConfig();

    [Header("Kỹ Năng Hỏa Cầu Mưa")]
    public BossFireballConfig fireballRain = new BossFireballConfig();

    [Header("Kỹ Năng Sét Liên Tiếp")]
    public BossLightningConfig lightning = new BossLightningConfig();

    [Header("Kỹ Năng Ẩn Thân")]
    public BossStealthConfig stealth = new BossStealthConfig();
}

// ── Nested configs (Serializable để hiện trong Inspector) ────────────────────

[System.Serializable]
public class BossNormalAttackConfig
{
    public bool  enabled     = true;
    [Min(0)] public int   damage      = 20;
    public float range      = 1.8f;
    public float cooldown   = 1.5f;
    [Tooltip("Lực đẩy lùi kẻ địch")]
    public float knockback  = 3f;
    [Tooltip("Layer mask của người chơi")]
    public LayerMask playerLayer = 0;
}

[System.Serializable]
public class BossFireballConfig
{
    public bool  enabled     = true;
    [Tooltip("Prefab hỏa cầu — gán trong Inspector của BossController")]
    public GameObject fireballPrefab;
    [Min(0)] public int   damage       = 30;
    [Tooltip("Số hỏa cầu mỗi lần cast")]
    [Range(1, 10)] public int count    = 3;
    [Tooltip("Chiều cao spawn so với vị trí người chơi")]
    public float spawnHeight  = 8f;
    [Tooltip("Phạm vi ngẫu nhiên theo trục X khi spawn")]
    public float spreadRadius = 3f;
    [Tooltip("Tốc độ rơi xuống")]
    public float fallSpeed    = 5f;
    public float cooldown     = 8f;
}

[System.Serializable]
public class BossLightningConfig
{
    public bool  enabled      = true;
    [Tooltip("Prefab tia sét — gán trong Inspector của BossController")]
    public GameObject lightningPrefab;
    [Min(0)] public int   damage       = 15;
    [Tooltip("Số tia sét liên tiếp (4–5)")]
    [Range(2, 8)] public int boltCount  = 5;
    [Tooltip("Khoảng cách giữa các tia (theo trục X)")]
    public float boltSpacing  = 0.9f;
    [Tooltip("Thời gian mỗi tia tồn tại")]
    public float boltDuration = 2f;
    [Tooltip("Độ trễ giữa các tia liên tiếp")]
    public float boltDelay    = 0.15f;
    [Tooltip("Thời gian stun người chơi khi trúng")]
    public float stunDuration = 2f;
    public float cooldown     = 10f;
}

[System.Serializable]
public class BossStealthConfig
{
    public bool  enabled   = true;
    [Tooltip("Thời gian ẩn thân (giây)")]
    public float duration  = 4f;
    public float cooldown  = 12f;
    [Tooltip("Alpha của sprite khi đang ẩn thân (0 = vô hình hoàn toàn)")]
    [Range(0f, 0.3f)]
    public float stealthAlpha = 0.1f;
}
