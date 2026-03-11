using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// StatsTabUI – Tab "Thông Số" trong CharacterPanel.
///
/// Hiển thị:
///  • Tên nhân vật, Level, Hệ nguyên tố, Gene Tier
///  • Thanh HP live (cập nhật realtime từ PlayerHealth / NetworkPlayerHealth)
///  • Thanh MP (static từ final_stats)
///  • Chỉ số chiến đấu: ATK, Move Speed
///  • Vàng hiện có
///
/// ══════════════════════════════════════════════════════════════
/// SETUP NHANH – kéo đúng thứ tự trong Inspector:
///   1. TxtCharacterName   [TMP_Text]
///   2. TxtLevel           [TMP_Text]
///   3. TxtElement         [TMP_Text]
///   4. HpBar              [Slider, interactable=false]
///   5. TxtHp              [TMP_Text]
///   6. MpBar              [Slider, interactable=false]
///   7. TxtMp              [TMP_Text]
///   8. TxtAttack          [TMP_Text]
///   9. TxtMoveSpeed       [TMP_Text]
///  10. TxtGold            [TMP_Text]
///  11. TxtStatus          [TMP_Text]  (loading / lỗi)
/// ══════════════════════════════════════════════════════════════
/// </summary>
public class StatsTabUI : MonoBehaviour
{
    [Header("Nhân vật")]
    [Tooltip("Tên nhân vật – 'Nguyễn Văn A'")]
    [SerializeField] private TMP_Text txtCharacterName;

    [Tooltip("Level – 'Lv. 25'")]
    [SerializeField] private TMP_Text txtLevel;

    [Tooltip("Hệ + Gene Tier – 'Hệ Fire  ★★  (Gene Tier 2)'")]
    [SerializeField] private TMP_Text txtElement;

    [Header("HP")]
    [Tooltip("Thanh HP (Slider, interactable OFF)")]
    [SerializeField] private Slider   hpBar;

    [Tooltip("Text HP – '2500 / 3000'")]
    [SerializeField] private TMP_Text txtHp;

    [Header("MP")]
    [Tooltip("Thanh MP (Slider, interactable OFF)")]
    [SerializeField] private Slider   mpBar;

    [Tooltip("Text MP – '800 / 1000'")]
    [SerializeField] private TMP_Text txtMp;

    [Header("Chỉ số chiến đấu")]
    [Tooltip("Tấn công – 'ATK: 350'")]
    [SerializeField] private TMP_Text txtAttack;

    [Tooltip("Tốc độ di chuyển – 'Tốc: 5.5'")]
    [SerializeField] private TMP_Text txtMoveSpeed;

    [Header("Kinh tế")]
    [Tooltip("Vàng – 'Vàng: 12,500'")]
    [SerializeField] private TMP_Text txtGold;

    [Header("Trạng thái")]
    [SerializeField] private TMP_Text txtStatus;

    // ── Runtime ────────────────────────────────────────────────────────────
    private PlayerHealth        _localHealth;
    private NetworkPlayerHealth _networkHealth;

    private int _maxHp;
    private int _maxMp;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        if (hpBar != null) hpBar.interactable = false;
        if (mpBar != null) mpBar.interactable = false;
    }

    private void OnEnable()
    {
        // Khi tab được bật lên, cập nhật dữ liệu từ GameManager
        Load();
    }

    private void OnDisable()
    {
        UnsubscribeHealth();
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Đọc dữ liệu từ GameManager và subscribe vào sự kiện HP live.
    /// Gọi khi tab được mở (CharacterPanelController gọi khi switch tab).
    /// </summary>
    public void Load()
    {
        var pd = GameManager.Instance?.GetPlayerData();
        if (pd == null)
        {
            SetStatus("Không có dữ liệu nhân vật.");
            return;
        }

        SetStatus("");

        // ─── DEBUG: kiểm tra giá trị nhận được từ server ───────────────────
        if (pd.final_stats == null)
            Debug.LogWarning($"[StatsTabUI] ⚠ final_stats = NULL → dùng base_stats làm fallback");
        else
            Debug.Log($"[StatsTabUI] ✅ final_stats: hp={pd.final_stats.hp} max_hp={pd.final_stats.max_hp} " +
                      $"mp={pd.final_stats.mp} max_mp={pd.final_stats.max_mp} " +
                      $"attack={pd.final_stats.attack} defense={pd.final_stats.defense} " +
                      $"move_speed={pd.final_stats.move_speed}");

        if (pd.base_stats == null)
            Debug.LogWarning($"[StatsTabUI] ⚠ base_stats = NULL");
        else
            Debug.Log($"[StatsTabUI] base_stats: hp={pd.base_stats.hp} max_hp={pd.base_stats.max_hp} " +
                      $"mp={pd.base_stats.mp} max_mp={pd.base_stats.max_mp} attack={pd.base_stats.attack}");
        // ────────────────────────────────────────────────────────────────────
        if (txtCharacterName != null)
            txtCharacterName.text = string.IsNullOrEmpty(pd.character_name)
                ? "Chưa đặt tên"
                : pd.character_name;

        if (txtLevel != null)
        {
            if (pd.exp_required_for_next_level > 0)
            {
                int expInLevel = pd.experience - pd.exp_at_current_level;
                int expNeeded  = pd.exp_required_for_next_level - pd.exp_at_current_level;
                float pct = expNeeded > 0 ? (float)expInLevel / expNeeded * 100f : 0f;
                txtLevel.text = $"Lv. {pd.level} ({pct:F1}%)";
            }
            else
            {
                txtLevel.text = $"Lv. {pd.level} (MAX)";
            }
        }

        // ── Hệ + Gene Tier ─────────────────────────────────────────────
        if (txtElement != null)
        {
            string stars  = new string('★', pd.gene_tier) + new string('☆', Mathf.Max(0, 5 - pd.gene_tier));
            string hybrid = pd.is_hybrid ? " (Hybrid)" : "";
            txtElement.text = $"Hệ {pd.element_type}{hybrid}  {stars}  (Gene Tier {pd.gene_tier})";
        }

        // ── Lưu maxHp / maxMp ──────────────────────────────────────────
        // Ưu tiên final_stats (đã tính bonus trang bị + tiềm năng)
        bool hasFinal = pd.final_stats != null;
        _maxHp = hasFinal ? pd.final_stats.max_hp : (pd.base_stats?.max_hp ?? 0);
        _maxMp = hasFinal ? pd.final_stats.max_mp : (pd.base_stats?.max_mp ?? 0);

        // ── Stats chiến đấu ────────────────────────────────────────────
        int  atk   = hasFinal ? pd.final_stats.attack     : (pd.base_stats?.attack     ?? 0);
        float spd  = hasFinal ? pd.final_stats.move_speed : 0f;

        Debug.Log($"[StatsTabUI] hasFinal={hasFinal} → maxHp={_maxHp} maxMp={_maxMp} atk={atk} spd={spd}");

        if (txtAttack    != null) txtAttack.text    = $"ATK: {atk}";
        if (txtMoveSpeed != null) txtMoveSpeed.text = $"Tốc: {spd:F1}";
        if (txtGold      != null) txtGold.text      = $"Vàng: {pd.gold:N0}";

        // ── MP (static – chưa có live MP system) ──────────────────────
        UpdateMpBar(_maxMp, _maxMp);   // hiển thị max/max cho đến khi có live MP

        // ── HP live ────────────────────────────────────────────────────
        FindAndSubscribeHealth();
    }

    // ── HP live ────────────────────────────────────────────────────────────

    /// <summary>Tìm PlayerHealth hoặc NetworkPlayerHealth trên Local Player và đăng ký event.</summary>
    private void FindAndSubscribeHealth()
    {
        UnsubscribeHealth();

        // Thử tìm NetworkPlayerHealth của local player trước (multiplayer)
        var allNet = Object.FindObjectsOfType<NetworkPlayerHealth>();
        foreach (var nh in allNet)
        {
            if (nh.IsOwner)
            {
                _networkHealth = nh;
                _networkHealth.OnHealthChanged.AddListener(OnHpChanged);
                // Dùng _maxHp từ final_stats (server data) thay vì nh.GetMaxHealth()
                // vì NetworkPlayerHealth.maxHealth trên client không được sync.
                OnHpChanged(nh.GetCurrentHealth(), _maxHp);
                return;
            }
        }

        // Fallback: single-player PlayerHealth
        _localHealth = Object.FindObjectOfType<PlayerHealth>();
        if (_localHealth != null)
        {
            _localHealth.OnHealthChanged.AddListener(OnHpChanged);
            OnHpChanged(_localHealth.GetCurrentHealth(), _localHealth.GetMaxHealth());
            return;
        }

        // Không tìm thấy → hiển thị max/max từ server data
        UpdateHpBar(_maxHp, _maxHp);
    }

    private void UnsubscribeHealth()
    {
        if (_networkHealth != null)
        {
            _networkHealth.OnHealthChanged.RemoveListener(OnHpChanged);
            _networkHealth = null;
        }
        if (_localHealth != null)
        {
            _localHealth.OnHealthChanged.RemoveListener(OnHpChanged);
            _localHealth = null;
        }
    }

    private void OnHpChanged(int current, int max)
    {
        // Chỉ cập nhật _maxHp nếu giá trị từ event LỚN HƠN giá trị server đã load.
        // NetworkPlayerHealth.maxHealth trên client KHÔNG được sync (plain field, default=100)
        // nên không được dùng nó để ghi đè _maxHp đã lấy từ final_stats.
        if (max > _maxHp) _maxHp = max;
        UpdateHpBar(current, _maxHp);
    }

    // ── UI helpers ─────────────────────────────────────────────────────────

    private void UpdateHpBar(int current, int max)
    {
        if (max <= 0) max = 1;
        if (hpBar != null)
        {
            hpBar.maxValue = max;
            hpBar.value    = Mathf.Clamp(current, 0, max);
        }
        if (txtHp != null)
            txtHp.text = $"{current:N0} / {max:N0}";
    }

    private void UpdateMpBar(int current, int max)
    {
        if (max <= 0) max = 1;
        if (mpBar != null)
        {
            mpBar.maxValue = max;
            mpBar.value    = Mathf.Clamp(current, 0, max);
        }
        if (txtMp != null)
            txtMp.text = $"{current:N0} / {max:N0}";
    }

    private void SetStatus(string msg)
    {
        if (txtStatus == null) return;
        txtStatus.text    = msg;
        txtStatus.enabled = !string.IsNullOrEmpty(msg);
    }
}
