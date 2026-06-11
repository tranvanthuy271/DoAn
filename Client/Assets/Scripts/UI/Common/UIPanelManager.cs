using System;
using System.Collections.Generic;
using UnityEngine;

// Mutual-exclusion manager cho các panel lớn trong game.
// Quy tắc:
// - Khi 1 panel mở → tất cả panel khác đã đăng ký bị đóng tự động.
// - QuestHudWidget.rootPanel được đăng ký qua RegisterHud() →
// tự ẩn khi có panel mở, hiện lại khi hết panel.
// Pattern dùng trong mỗi panel:
// Awake()   → Register(gameObject, Close)
// Open()    → CloseOthers(gameObject)  ...show...  NotifyOpened(gameObject)
// Close()   → ...hide...               NotifyClosed(gameObject)
// OnDestroy → Unregister(gameObject)
// QuestHudWidget:
// Awake() → RegisterHud(rootWidget)
// OnDestroy → UnregisterHud(rootWidget)
public static class UIPanelManager
{
    private struct PanelEntry
    {
        public GameObject Go;
        public Action     CloseAction;
    }

    private static readonly List<PanelEntry> _panels     = new();
    private static readonly HashSet<GameObject> _open    = new();   // panels currently open
    private static readonly List<GameObject>    _huds    = new();   // hidden while any panel is open

    // Prevents NotifyClosed (called inside close-actions) from prematurely showing HUDs
    private static bool _isBatchClosing;
    private static bool _hudsHiddenByPanel;

    // Panel registration

    // Đăng ký panel. Gọi trong Awake(). An toàn khi gọi nhiều lần.
    public static void Register(GameObject go, Action closeAction)
    {
        if (go == null) return;
        foreach (var e in _panels)
            if (e.Go == go) return;
        _panels.Add(new PanelEntry { Go = go, CloseAction = closeAction });
    }

    // Huỷ đăng ký. Gọi trong OnDestroy().
    public static void Unregister(GameObject go)
    {
        if (go == null) return;
        for (int i = _panels.Count - 1; i >= 0; i--)
            if (_panels[i].Go == go) { _panels.RemoveAt(i); break; }
        if (_open.Remove(go))
            RefreshHudVisibilityFromOpenPanels();
    }

    // HUD registration

    // Đăng ký HUD element (ẩn khi có panel, hiện khi hết panel).
    public static void RegisterHud(GameObject go)
    {
        if (go == null || _huds.Contains(go)) return;
        _huds.Add(go);
        ApplyHudVisibility(go);
    }

    public static void UnregisterHud(GameObject go) => _huds.Remove(go);

    public static void ApplyHudVisibility(GameObject go)
    {
        if (go != null)
            go.SetActive(!_hudsHiddenByPanel);
    }

    private static void RefreshHudVisibility()
    {
        foreach (var h in _huds)
            ApplyHudVisibility(h);
    }

    private static void RefreshHudVisibilityFromOpenPanels()
    {
        if (_isBatchClosing) return;

        _hudsHiddenByPanel = _open.Count > 0;
        RefreshHudVisibility();
    }

    // Panel lifecycle

    // Gọi ở ĐẦU mỗi Open(), TRƯỚC khi show panel.
    // Đóng tất cả panel đang mở + ẩn HUD.
    public static void CloseOthers(GameObject exceptGo)
    {
        _isBatchClosing = true;
        try
        {
            foreach (var e in _panels.ToArray())
            {
                if (e.Go == null || e.Go == exceptGo) continue;
                if (_open.Contains(e.Go))
                    e.CloseAction?.Invoke();
            }
        }
        finally
        {
            _isBatchClosing = false;
        }

        // Ẩn HUD sau khi panel khác đã đóng
        _hudsHiddenByPanel = true;
        RefreshHudVisibility();
    }

    // Gọi SAU KHI panel đã hiển thị (sau SetActive(true) / Show).
    // Đánh dấu panel là đang mở.
    public static void NotifyOpened(GameObject go)
    {
        if (go != null) _open.Add(go);
        _hudsHiddenByPanel = true;
        RefreshHudVisibility();
    }

    // Gọi SAU KHI panel đã ẩn (sau SetActive(false) / Hide).
    // Hiện lại HUD nếu không còn panel nào đang mở.
    public static void NotifyClosed(GameObject go)
    {
        _open.Remove(go);
        RefreshHudVisibilityFromOpenPanels();

        // Không còn panel nào → hiện lại HUD
    }
}
