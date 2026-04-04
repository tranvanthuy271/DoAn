using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BuffHudPanel — thanh HUD hiển thị tất cả buff icon đang active trên người chơi.
/// Subscribe vào ActiveBuffManager.OnBuffListChanged, tạo/cập nhật/ẩn BuffIconEntry.
///
/// Setup trong Unity Editor:
///   1. Tạo GameObject "BuffHudPanel" trong HUD Canvas (con của Canvas)
///   2. Add Component: HorizontalLayoutGroup (spacing=4, childAlignment=MiddleLeft)
///      - Control Child Size: Width=false, Height=false
///      - Child Force Expand: Width=false, Height=false
///   3. RectTransform: Anchor=BottomLeft, Pos=(10, 60, 0), Width=300, Height=52
///   4. Gắn script này vào GameObject
///   5. Kéo prefabs vào Inspector:
///      - buffIconEntryPrefab ← Assets/Prefabs/UI/BuffIconEntry.prefab
///      - tooltipPrefab       ← Assets/Prefabs/UI/BuffDetailTooltip.prefab
///      - tooltipParent       ← Transform/Panel cố định trong Canvas — tooltip sẽ spawn tại đây
///
/// Tham khảo: GameHUD.c(Graphics) trong LangLa Client_base
/// </summary>
public class BuffHudPanel : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("Prefab BuffIconEntry (48×48) — hiển thị 1 buff icon + countdown ring")]
    [SerializeField] private BuffIconEntry buffIconEntryPrefab;

    [Tooltip("Prefab BuffDetailTooltip — popup khi click buff icon")]
    [SerializeField] private BuffDetailTooltip tooltipPrefab;

    [Header("References")]
    [Tooltip("Transform (Panel/GO cố định trong Canvas) nơi tooltip sẽ được spawn. "
           + "Tooltip hiện ngay tại vị trí của object này.")]
    [SerializeField] private Transform tooltipParent;

    // ── Private state ─────────────────────────────────────────────────────

    /// <summary>Pool các entry đã tạo (reuse thay vì Instantiate liên tục).</summary>
    private readonly List<BuffIconEntry> _entries = new List<BuffIconEntry>();

    /// <summary>Tooltip đang hiển thị hiện tại (null nếu không có).</summary>
    private BuffDetailTooltip _activeTooltip;

    // Đánh dấu đã subscribe ActiveBuffManager (Instance có thể null lúc OnEnable)
    private bool _buffManagerSubscribed;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    private void OnEnable()
    {
        TrySubscribeBuffManager();

        // Load lại khi panel được bật (vd: vào scene mới)
        if (ActiveBuffManager.Instance != null)
            ActiveBuffManager.Instance.LoadFromServer();

        // Lắng nghe event player data set để load buff ngay khi login/reconnect
        GameManager.OnPlayerDataSet += OnPlayerDataReady;
    }

    private void Start()
    {
        // Thử subscribe lần nữa phòng trường hợp Instance null lúc OnEnable
        TrySubscribeBuffManager();

        if (ActiveBuffManager.Instance != null)
            ActiveBuffManager.Instance.LoadFromServer();
        else
            Debug.LogWarning("[BuffHudPanel] ActiveBuffManager.Instance is null. " +
                             "Đảm bảo ActiveBuffManager đã có trong scene và là DontDestroyOnLoad.");
    }

    private void OnDisable()
    {
        GameManager.OnPlayerDataSet -= OnPlayerDataReady;
    }

    private void OnDestroy()
    {
        if (ActiveBuffManager.Instance != null)
            ActiveBuffManager.Instance.OnBuffListChanged -= OnBuffListChanged;
        _buffManagerSubscribed = false;
    }

    private void TrySubscribeBuffManager()
    {
        if (_buffManagerSubscribed || ActiveBuffManager.Instance == null) return;
        ActiveBuffManager.Instance.OnBuffListChanged += OnBuffListChanged;
        _buffManagerSubscribed = true;
    }

    private void OnPlayerDataReady(PlayerDataResponse _)
    {
        TrySubscribeBuffManager();
        ActiveBuffManager.Instance?.LoadFromServer();
    }

    // ── Internal ──────────────────────────────────────────────────────────

    /// <summary>
    /// Được gọi khi danh sách buff thay đổi (item buff thêm/hết hạn/bị xóa).
    /// Cập nhật tất cả entry — tương đương vòng for trong GameHUD.c() của LangLa.
    /// </summary>
    private void OnBuffListChanged(List<ActiveBuffDto> buffs)
    {
        // Ẩn các entry dư (pool reuse)
        for (int i = buffs.Count; i < _entries.Count; i++)
            _entries[i].gameObject.SetActive(false);

        // Bind dữ liệu vào từng entry (tạo mới nếu chưa đủ)
        for (int i = 0; i < buffs.Count; i++)
        {
            BuffIconEntry entry;

            if (i < _entries.Count)
            {
                // Reuse entry đã có
                entry = _entries[i];
                entry.gameObject.SetActive(true);
            }
            else
            {
                // Tạo entry mới — KHÔNG ghi đè tooltipParent/OnClicked:
                // mỗi entry dùng tooltipParent đã config sẵn trong prefab
                entry = Instantiate(buffIconEntryPrefab, transform);
                _entries.Add(entry);
            }

            entry.Bind(buffs[i]);
        }
    }

    /// <summary>
    /// Tạo/cập nhật BuffDetailTooltip đặt sang phải của icon được click.
    /// Tương đương GameSrc.onUIEvent() → new BuffTooltip(...) trong LangLa.
    /// </summary>
    private void ShowTooltip(ActiveBuffDto buff)
    {
        // Đóng tooltip cũ nếu đang mở
        if (_activeTooltip != null)
            _activeTooltip.Close();

        // Tạo tooltip nếu chưa có (lazy init); parent = tooltipParent (vị trí cố định)
        if (_activeTooltip == null && tooltipPrefab != null)
        {
            Transform parent = tooltipParent != null ? tooltipParent : transform.root;
            _activeTooltip = Instantiate(tooltipPrefab, parent);
        }
        else if (_activeTooltip != null && tooltipParent != null
                 && _activeTooltip.transform.parent != tooltipParent)
        {
            _activeTooltip.transform.SetParent(tooltipParent, false);
        }

        // Hiển thị nội dung tại vị trí tooltipParent — không cần tính canvas-space
        if (_activeTooltip != null)
            _activeTooltip.Show(buff);
    }
}
