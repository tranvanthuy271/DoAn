using UnityEngine;

/// <summary>
/// EnemyClickHandler — Xử lý click chọn enemy trên client.
/// Gắn vào root GameObject của enemy prefab.
///
/// Yêu cầu:
///   - Collider2D (non-trigger) trên root enemy để OnMouseDown hoạt động.
///   - Child GameObject "SelectionIndicator" (sprite mũi tên, mặc định ẩn).
///   - EnemyInfoPanel tồn tại trong scene (singleton).
///
/// Khi click:
///   1. Bỏ chọn enemy đang được chọn trước đó.
///   2. Hiển thị SelectionIndicator (mũi tên bên dưới chân quái).
///   3. Mở EnemyInfoPanel với thông số của enemy này.
///
/// Tự động bỏ chọn và đóng panel khi enemy bị destroy.
/// </summary>
public class EnemyClickHandler : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Child GameObject mũi tên/indicator, mặc định ẩn trong prefab")]
    public GameObject selectionIndicator;

    // ─── Static: chỉ một enemy được chọn tại một thời điểm ───────────
    private static EnemyClickHandler _currentSelected;

    // ─── Cached components ────────────────────────────────────────────
    private NetworkEnemyHealth _netHealth;
    private EnemySkillSet _skillSet;          // Chỉ có trên server/host
    private EnemyStatOverride _statOverride;  // Chỉ có trên server/host

    private void Awake()
    {
        _netHealth    = GetComponent<NetworkEnemyHealth>();
        _skillSet     = GetComponent<EnemySkillSet>();
        // _statOverride được AddComponent tại runtime bởi HostSpawnConfigLoader.
        // KHÔNG cache trong Awake() vì lúc đó chưa tồn tại — tự tìm lại khi cần.

        if (selectionIndicator != null)
            selectionIndicator.SetActive(false);
    }

    // Unity gọi khi click chuột trên Collider2D (cần Collider2D non-trigger trên root)
    private void OnMouseDown()
    {
        Select();
    }

    // ─────────────────────────────────────────────────────────────────

    /// <summary>Chọn enemy này, bỏ chọn enemy trước đó.</summary>
    public void Select()
    {
        // Bỏ chọn enemy cũ
        if (_currentSelected != null && _currentSelected != this)
            _currentSelected.Deselect();

        _currentSelected = this;

        if (selectionIndicator != null)
            selectionIndicator.SetActive(true);

        EnemyInfoPanel.Instance?.Show(BuildStats());
    }

    /// <summary>Bỏ chọn enemy này (ẩn indicator).</summary>
    public void Deselect()
    {
        if (selectionIndicator != null)
            selectionIndicator.SetActive(false);
    }

    /// <summary>
    /// Cập nhật lại HP trên panel nếu enemy này đang được chọn.
    /// Gọi từ NetworkEnemyHealth.OnHealthChanged nếu cần real-time update.
    /// </summary>
    public void RefreshPanelIfSelected()
    {
        if (_currentSelected == this)
            EnemyInfoPanel.Instance?.UpdateHP(
                _netHealth != null ? _netHealth.GetCurrentHealth() : 0,
                _netHealth != null ? _netHealth.GetMaxHealth()     : 0);
    }

    // ─────────────────────────────────────────────────────────────────

    private EnemyStats BuildStats()
    {
        // Lazy-load vì _statOverride được AddComponent runtime sau khi NetworkObject.Spawn()
        if (_statOverride == null)
            _statOverride = GetComponent<EnemyStatOverride>();

        // Tên quái: ưu tiên từ EnemyStatOverride (lấy từ DB), fallback tên GameObject
        string name = (_statOverride != null && !string.IsNullOrEmpty(_statOverride.EnemyName))
            ? _statOverride.EnemyName
            : gameObject.name.Replace("(Clone)", "").Trim();

        return new EnemyStats
        {
            enemyName   = name,
            currentHp   = _netHealth    != null ? _netHealth.GetCurrentHealth() : 0,
            maxHp       = _netHealth    != null ? _netHealth.GetMaxHealth()     : 0,
            elementType = _skillSet     != null ? _skillSet.ElementType         : "None",
            level       = _statOverride != null ? _statOverride.Level           : 1,
            expReward   = _statOverride != null ? _statOverride.OverrideExp     : 0
        };
    }

    private void OnDestroy()
    {
        if (_currentSelected == this)
        {
            _currentSelected = null;
            EnemyInfoPanel.Instance?.Hide();
        }
    }
}
