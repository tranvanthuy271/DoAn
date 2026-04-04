using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BuffHUDPanel – Container HUD để hiển thị tất cả buff active của player.
///
/// Setup trong Unity:
///   1. Tạo GameObject "BuffHUDPanel" trong Canvas > HUD layer.
///   2. Gắn script này + HorizontalLayoutGroup (spacing=4) trên iconContainer.
///   3. Tạo Prefab "BuffIconPrefab" từ BuffIconUI.cs và gắn vào trường buffIconPrefab.
///   4. Kích thước mỗi icon gợi ý: 40×40 px.
///   5. Panel sẽ tự cập nhật khi ActiveBuffManager.OnBuffListChanged fire.
///
/// Tình huống hiển thị:
///   - Buff mới → tạo icon từ pool → Setup(buff)
///   - Buff hết hạn → icon tự fade → trả về pool
///   - Hover icon → tooltip hiển thị tên + chỉ số
/// </summary>
public class BuffHUDPanel : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Prefab chứa BuffIconUI component")]
    [SerializeField] private BuffIconUI buffIconPrefab;

    [Tooltip("Transform parent để spawn icon vào (thường là HorizontalLayoutGroup)")]
    [SerializeField] private Transform iconContainer;

    // ── Pool ──────────────────────────────────────────────────────────────
    private readonly List<BuffIconUI> _pool   = new List<BuffIconUI>();
    private readonly List<BuffIconUI> _active = new List<BuffIconUI>();

    // Container ẩn để đặt icon đang trong pool, tránh ảnh hưởng đến layout.
    private Transform _poolContainer;

    // Đánh dấu đã subscribe ActiveBuffManager chưa (xử lý trường hợp Instance null lúc OnEnable).
    private bool _subscribed;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (iconContainer == null) iconContainer = transform;

        // Tạo container ẩn để giữ icon không dùng; không ảnh hưởng HorizontalLayoutGroup.
        var poolGo = new GameObject("_BuffIconPool");
        _poolContainer = poolGo.transform;
        _poolContainer.SetParent(transform, false);
        poolGo.SetActive(false);

        // Đảm bảo iconContainer có HorizontalLayoutGroup để icon hiển thị sát nhau.
        EnsureHorizontalLayout();
    }

    private void OnEnable()
    {
        TrySubscribeBuffManager();

        // Lắng nghe player data set để load lại buff sau login / scene change.
        GameManager.OnPlayerDataSet += OnPlayerDataReady;

        // Hiển thị buff đang có ngay (nếu đã load từ trước).
        RefreshIcons(ActiveBuffManager.Instance?.GetActiveBuffs()
                     ?? new System.Collections.Generic.List<ActiveBuffDto>());
    }

    private void OnDisable()
    {
        if (ActiveBuffManager.Instance != null)
            ActiveBuffManager.Instance.OnBuffListChanged -= RefreshIcons;
        _subscribed = false;

        GameManager.OnPlayerDataSet -= OnPlayerDataReady;
    }

    private void Start()
    {
        // Trường hợp ActiveBuffManager.Instance chưa có lúc OnEnable → thử lại ở Start.
        TrySubscribeBuffManager();

        // Kích hoạt load buff từ server (có player data hay chưa đều thử).
        ActiveBuffManager.Instance?.LoadFromServer();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private void TrySubscribeBuffManager()
    {
        if (_subscribed || ActiveBuffManager.Instance == null) return;
        ActiveBuffManager.Instance.OnBuffListChanged += RefreshIcons;
        _subscribed = true;
    }

    private void OnPlayerDataReady(PlayerDataResponse _)
    {
        TrySubscribeBuffManager();
        ActiveBuffManager.Instance?.LoadFromServer();
    }

    /// <summary>Đảm bảo iconContainer có HorizontalLayoutGroup với spacing 4.</summary>
    private void EnsureHorizontalLayout()
    {
        var hlg = iconContainer.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null) return; // đã có, giữ nguyên cấu hình từ Inspector

        hlg = iconContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing              = 4f;
        hlg.childAlignment       = TextAnchor.MiddleLeft;
        hlg.childControlWidth    = false;
        hlg.childControlHeight   = false;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        hlg.padding = new RectOffset(4, 4, 2, 2);
    }

    // ── Core ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Được gọi khi danh sách buff thay đổi.
    /// Sync danh sách icon với danh sách buff hiện tại.
    /// </summary>
    private void RefreshIcons(System.Collections.Generic.List<ActiveBuffDto> buffs)
    {
        // Trả tất cả icon đang active về pool
        foreach (var icon in _active)
            ReturnToPool(icon);
        _active.Clear();

        if (buffs == null) return;

        foreach (var buff in buffs)
        {
            var icon = GetFromPool();
            icon.Setup(buff);
            icon.OnExpired -= OnIconExpired;
            icon.OnExpired += OnIconExpired;
            _active.Add(icon);
        }
    }

    private void OnIconExpired(BuffIconUI icon)
    {
        if (_active.Remove(icon))
            ReturnToPool(icon);
    }

    // ── Pool helpers ──────────────────────────────────────────────────────

    private BuffIconUI GetFromPool()
    {
        BuffIconUI icon = _pool.Count > 0 ? _pool[0] : null;
        if (icon != null)
        {
            _pool.RemoveAt(0);
        }
        else
        {
            if (buffIconPrefab == null)
            {
                Debug.LogError("[BuffHUDPanel] buffIconPrefab chưa được gán!");
                return null;
            }
            icon = Instantiate(buffIconPrefab, iconContainer);
        }
        // Chuyển icon từ pool container sang iconContainer để layout group nhận ra.
        icon.transform.SetParent(iconContainer, false);
        icon.gameObject.SetActive(true);
        return icon;
    }

    private void ReturnToPool(BuffIconUI icon)
    {
        if (icon == null) return;
        icon.Clear(); // gameObject.SetActive(false)
        // Chuyển sang _poolContainer ẩn → iconContainer (HorizontalLayoutGroup) sẽ chỉ
        // chứa các icon đang active, tránh khoảng trống/sai spacing.
        icon.transform.SetParent(_poolContainer, false);
        _pool.Add(icon);
    }
}
