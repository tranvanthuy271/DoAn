using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// StatsTabUI – Tab "Nhân vật" (tab 0 trong CharacterPanel).
///
/// Hiển thị:
///  • Tên / Level / Element / Gene Tier
///  • Thanh HP live (realtime) + Thanh MP
///  • ATK, Move Speed, Vàng
///  • Danh sách trang bị đang mặc (weapon/armor/pants/boots + helmet/accessory nếu có)
///    mỗi slot có tên, level nâng cấp, nút "Nâng cấp"
///
/// Setup Inspector:
///   1-7.  Các TMP_Text + Slider như cũ.
///   8.    equipListContainer  – Transform cha chứa các dòng trang bị (VLG)
///   9.    equipRowPrefab      – Prefab EquipRowUI
/// </summary>
public class StatsTabUI : MonoBehaviour
{
    [Header("Nhân vật")]
    [SerializeField] private TMP_Text txtCharacterName;
    [SerializeField] private TMP_Text txtLevel;
    [SerializeField] private TMP_Text txtElement;

    [Header("HP")]
    [SerializeField] private Slider   hpBar;
    [SerializeField] private TMP_Text txtHp;

    [Header("MP")]
    [SerializeField] private Slider   mpBar;
    [SerializeField] private TMP_Text txtMp;

    [Header("Chỉ số chiến đấu")]
    [SerializeField] private TMP_Text txtAttack;
    [SerializeField] private TMP_Text txtMoveSpeed;

    [Header("Kinh tế")]
    [SerializeField] private TMP_Text txtGold;

    [Header("Trang bị đang mặc")]
    [Tooltip("Transform chứa các dòng EquipRowUI (dùng VerticalLayoutGroup)")]
    [SerializeField] private Transform equipListContainer;
    [Tooltip("Prefab mỗi dòng trang bị (phải có EquipRowUI)")]
    [SerializeField] private EquipRowUI equipRowPrefab;

    [Header("Trạng thái")]
    [SerializeField] private TMP_Text txtStatus;

    // ── Runtime ──────────────────────────────────────────────
    private int   _playerId = -1;
    private int   _maxHp;
    private int   _maxMp;

    private PlayerHealth        _localHealth;
    private NetworkPlayerHealth _networkHealth;

    private readonly List<EquipRowUI> _equipRows = new List<EquipRowUI>();

    // ── Lifecycle ─────────────────────────────────────────────

    private void Awake()
    {
        if (hpBar != null) hpBar.interactable = false;
        if (mpBar != null) mpBar.interactable = false;
    }

    private void OnEnable()  => Load();
    private void OnDisable() => UnsubscribeHealth();

    // ── Public API ────────────────────────────────────────────

    public void SetPlayerId(int id) => _playerId = id;

    public void Load()
    {
        var pd = GameManager.Instance?.GetPlayerData();
        if (pd == null) { SetStatus("Không có dữ liệu nhân vật."); return; }

        SetStatus("");

        // ─── Tên / Level ─────────────────────────────────────
        if (txtCharacterName != null)
            txtCharacterName.text = string.IsNullOrEmpty(pd.character_name) ? "Chưa đặt tên" : pd.character_name;

        if (txtLevel != null)
        {
            if (pd.exp_required_for_next_level > 0)
            {
                int expIn    = pd.experience - pd.exp_at_current_level;
                int expNeed  = pd.exp_required_for_next_level - pd.exp_at_current_level;
                float pct    = expNeed > 0 ? (float)expIn / expNeed * 100f : 0f;
                txtLevel.text = $"Lv. {pd.level} ({pct:F1}%)";
            }
            else
                txtLevel.text = $"Lv. {pd.level} (MAX)";
        }

        // ─── Element / Gene ────────────────────────────────────
        if (txtElement != null)
        {
            string stars  = new string('★', pd.gene_tier) + new string('☆', Mathf.Max(0, 5 - pd.gene_tier));
            string hybrid = pd.is_hybrid ? " (Hybrid)" : "";
            txtElement.text = $"Hệ {pd.element_type}{hybrid}  {stars}  (Gene Tier {pd.gene_tier})";
        }

        // ─── Stats ────────────────────────────────────────────
        bool hasFinal = pd.final_stats != null;
        _maxHp = hasFinal ? pd.final_stats.max_hp : (pd.base_stats?.max_hp ?? 0);
        _maxMp = hasFinal ? pd.final_stats.max_mp : (pd.base_stats?.max_mp ?? 0);

        int   atk  = hasFinal ? pd.final_stats.attack     : (pd.base_stats?.attack ?? 0);
        float spd  = hasFinal ? pd.final_stats.move_speed : 0f;

        if (txtAttack    != null) txtAttack.text    = $"ATK: {atk}";
        if (txtMoveSpeed != null) txtMoveSpeed.text = $"Tốc: {spd:F1}";
        if (txtGold      != null) txtGold.text      = $"Vàng: {pd.gold:N0}";

        UpdateMpBar(_maxMp, _maxMp);
        FindAndSubscribeHealth();

        // ─── Trang bị ─────────────────────────────────────────
        LoadEquipmentSection(pd.equipment);
    }

    // ── Equipment section ─────────────────────────────────────

    private void LoadEquipmentSection(EquipmentData eq)
    {
        ClearEquipRows();

        if (equipListContainer == null || equipRowPrefab == null) return;

        if (eq == null)
        {
            // Try to fetch from API using the full equipment DTO
            if (_playerId > 0 && APIClient.Instance != null)
            {
                APIClient.Instance.GetPlayerEquipment(_playerId, onSuccess: PopulateEquipFromDto);
            }
            return;
        }

        // Build rows from the basic EquipmentData that comes with PlayerData
        var slots = new (string label, string name, int attack, int hp)[]
        {
            ("Vũ khí",  eq.weapon?.name, eq.weapon?.attack ?? 0, eq.weapon?.hp ?? 0),
            ("Giáp",    eq.armor?.name,  eq.armor?.attack  ?? 0, eq.armor?.hp  ?? 0),
            ("Quần",    eq.pants?.name,  eq.pants?.attack  ?? 0, eq.pants?.hp  ?? 0),
            ("Giày",    eq.boots?.name,  eq.boots?.attack  ?? 0, eq.boots?.hp  ?? 0),
        };

        foreach (var slot in slots)
        {
            var row = Instantiate(equipRowPrefab, equipListContainer);
            row.SetData(slot.label, slot.name, 0, _playerId, onUpgraded: Load);
            _equipRows.Add(row);
        }

        // Also try to get full data (including upgrade levels)
        if (_playerId > 0 && APIClient.Instance != null)
            APIClient.Instance.GetPlayerEquipment(_playerId, onSuccess: PopulateEquipFromDto);
    }

    private void PopulateEquipFromDto(PlayerEquipmentDto dto)
    {
        if (dto == null) return;
        ClearEquipRows();

        if (equipListContainer == null || equipRowPrefab == null) return;

        var slots = new (string key, string label, EquipmentItemDto item)[]
        {
            ("weapon",    "Vũ khí",   dto.weapon),
            ("helmet",    "Mũ",       dto.helmet),
            ("armor",     "Giáp",     dto.armor),
            ("pants",     "Quần",     dto.pants),
            ("boots",     "Giày",     dto.boots),
            ("accessory", "Phụ kiện", dto.accessory),
        };

        foreach (var s in slots)
        {
            var row = Instantiate(equipRowPrefab, equipListContainer);
            string name  = s.item?.itemName ?? s.item?.itemCode ?? "";
            int    level = s.item?.upgradeLevel ?? 0;
            row.SetData(s.label, name, level, _playerId, s.key, s.item, onUpgraded: Load);
            _equipRows.Add(row);
        }
    }

    private void ClearEquipRows()
    {
        foreach (var r in _equipRows)
            if (r != null) Destroy(r.gameObject);
        _equipRows.Clear();
    }

    // ── HP live ───────────────────────────────────────────────

    private void FindAndSubscribeHealth()
    {
        UnsubscribeHealth();

        var allNet = Object.FindObjectsOfType<NetworkPlayerHealth>();
        foreach (var nh in allNet)
        {
            if (nh.IsOwner)
            {
                _networkHealth = nh;
                _networkHealth.OnHealthChanged.AddListener(OnHpChanged);
                OnHpChanged(nh.GetCurrentHealth(), _maxHp);
                return;
            }
        }

        _localHealth = Object.FindObjectOfType<PlayerHealth>();
        if (_localHealth != null)
        {
            _localHealth.OnHealthChanged.AddListener(OnHpChanged);
            OnHpChanged(_localHealth.GetCurrentHealth(), _localHealth.GetMaxHealth());
            return;
        }

        UpdateHpBar(_maxHp, _maxHp);
    }

    private void UnsubscribeHealth()
    {
        if (_networkHealth != null) { _networkHealth.OnHealthChanged.RemoveListener(OnHpChanged); _networkHealth = null; }
        if (_localHealth   != null) { _localHealth.OnHealthChanged.RemoveListener(OnHpChanged);   _localHealth   = null; }
    }

    private void OnHpChanged(int current, int max)
    {
        if (max > _maxHp) _maxHp = max;
        UpdateHpBar(current, _maxHp);
    }

    // ── UI helpers ────────────────────────────────────────────

    private void UpdateHpBar(int current, int max)
    {
        if (max <= 0) max = 1;
        if (hpBar != null) { hpBar.maxValue = max; hpBar.value = Mathf.Clamp(current, 0, max); }
        if (txtHp != null)  txtHp.text = $"{current:N0} / {max:N0}";
    }

    private void UpdateMpBar(int current, int max)
    {
        if (max <= 0) max = 1;
        if (mpBar != null) { mpBar.maxValue = max; mpBar.value = Mathf.Clamp(current, 0, max); }
        if (txtMp != null)  txtMp.text = $"{current:N0} / {max:N0}";
    }

    private void SetStatus(string msg)
    {
        if (txtStatus == null) return;
        txtStatus.text    = msg;
        txtStatus.enabled = !string.IsNullOrEmpty(msg);
    }
}
