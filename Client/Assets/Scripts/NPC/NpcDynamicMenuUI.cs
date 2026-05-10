using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// NPC Dynamic Menu UI — hiển thị danh sách menu do server gửi về.
///
/// Luồng (giống LangLa):
///   Server gửi menu_items "Mua đồ;Tẩy tiềm năng;Cáo từ" → OpenMenuClientRpc →
///   NpcDynamicMenuUI.Open() hiện panel + populate list →
///   Player click item → SelectMenuItemServerRpc(index) → server execute action.
///
/// Inspector setup:
///   mainPanel       — root panel (ảnh nền gỗ, v.v.)
///   titleText       — TMP_Text "Xin chào {playerName}"
///   menuListContent — Transform (Content trong ScrollRect)
///   menuItemRowPrefab — prefab NpcMenuItemRow
///   btnClose        — Button "Cáo từ"
///
/// Nếu chưa có prefab trong scene, dùng GetOrFind() hoặc GetOrCreate() để load từ Resources.
/// </summary>
public class NpcDynamicMenuUI : MonoBehaviour
{
    private const string LogPrefix   = "[NpcDynamicMenuUI]";
    private const string ResourcePath = "Prefabs/UI/NPC/NpcDynamicMenuPanel";
    private const string GameplayBlockSource = "NpcDynamicMenu";

    public static NpcDynamicMenuUI Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────
    [Header("Panel")]
    [SerializeField] private GameObject mainPanel;

    [Header("Tiêu đề")]
    [SerializeField] private TMP_Text titleText;

    [Header("Danh sách menu")]
    [SerializeField] private Transform     menuListContent;   // Content trong ScrollRect
    [SerializeField] private GameObject    menuItemRowPrefab; // Prefab NpcMenuItemRow

    [Header("Nút đóng")]
    [SerializeField] private Button btnClose;

    // ── Runtime ────────────────────────────────────────────────────────
    private NpcInteraction          _currentInteraction;
    private readonly List<GameObject> _rows = new();

    /// <summary>NpcData dùng để mở panel lần cuối — dùng bởi NpcInteraction.ExecuteMenuActionClientRpc.</summary>
    public NpcData LastOpenedNpcData { get; private set; }

    public bool IsOpen => mainPanel != null && mainPanel.activeSelf;

    // ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (mainPanel) mainPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        // Chỉ đăng ký listener nếu chưa có (tránh gọi Close() 2 lần nếu prefab đã wired trong Inspector)
        if (btnClose)
        {
            btnClose.onClick.RemoveListener(Close);
            btnClose.onClick.AddListener(Close);
        }
        // KHÔNG gọi mainPanel.SetActive(false) ở đây —
        // Awake() đã xử lý trạng thái ẩn ban đầu.
        // Gọi thêm ở Start() sẽ ẩn panel sau khi Open() đã show nó
        // (Start chạy trễ hơn — sau SetActive(true) trong Open).
    }

    // ── Singleton helpers ──────────────────────────────────────────────

    public static NpcDynamicMenuUI GetOrFind()
    {
        if (Instance != null) return Instance;
        Instance = FindObjectOfType<NpcDynamicMenuUI>(includeInactive: true);
        return Instance;
    }

    public static NpcDynamicMenuUI GetOrCreate()
    {
        var found = GetOrFind();
        if (found != null) return found;

        // Thử load từ Resources
        var prefab = Resources.Load<GameObject>(ResourcePath);
        if (prefab != null)
        {
            Transform parent = FindScreenSpaceCanvas();
            var go = Instantiate(prefab, parent, false);
            go.name = prefab.name;

            // Force RT về center (prefab cũ đôi khi được save với anchor (0,0) + sizeDelta (0,0))
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin        = new Vector2(0.5f, 0.5f);
                rt.anchorMax        = new Vector2(0.5f, 0.5f);
                rt.pivot            = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                if (rt.sizeDelta.sqrMagnitude < 1f)   // (0,0) → chưa set
                    rt.sizeDelta = new Vector2(300f, 430f);
            }

            go.SetActive(false);
            Instance = go.GetComponent<NpcDynamicMenuUI>();
            if (Instance != null)
            {
                // Fix Viewport RT — nếu prefab cũ lưu anchor (0,0)/(0,0) → mask = 0px → block tất cả click
                FixViewportRt(go);
                Debug.Log($"{LogPrefix} Instantiated from Resources/{ResourcePath}.");
                return Instance;
            }
        }

        // Fallback: tạo runtime (không có ảnh nền, chỉ functional)
        Instance = CreateRuntime();
        return Instance;
    }

    // ── Open / Close ───────────────────────────────────────────────────

    /// <summary>
    /// Mở dynamic menu với danh sách item từ server.
    /// <param name="data">NpcData với menu_items = "Mua đồ;Nâng cấp;Cáo từ" (labels only).</param>
    /// <param name="interaction">NpcInteraction để gọi SelectMenuItemServerRpc sau khi chọn.</param>
    /// </summary>
    public void Open(NpcData data, NpcInteraction interaction)
    {
        if (data == null) { Debug.LogWarning($"{LogPrefix} Open: data null."); return; }

        _currentInteraction = interaction;
        LastOpenedNpcData   = data;

        // Tiêu đề: "Xin chào {tên nhân vật}"
        string playerName = ResolveLocalPlayerName();
        if (titleText != null)
            titleText.text = $"Xin chào {playerName}";

        // Populate rows
        ClearRows();
        if (!string.IsNullOrWhiteSpace(data.menu_items))
        {
            string[] labels = data.menu_items.Split(';');
            for (int i = 0; i < labels.Length; i++)
            {
                string label = labels[i].Trim();
                if (string.IsNullOrEmpty(label)) continue;
                SpawnRow(label, i);
            }
        }

        // Hiện panel
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (mainPanel) mainPanel.SetActive(true);

        // Diagnostics: log trạng thái các component quan trọng sau khi open
        var scrollRect = GetComponentInChildren<ScrollRect>(true);
        var viewport   = scrollRect != null ? scrollRect.viewport : null;
        var content    = scrollRect != null ? scrollRect.content  : null;
        var cgRoot     = GetComponent<CanvasGroup>();
        Debug.Log($"{LogPrefix} Open diagnostics | " +
            $"mainPanel={(mainPanel!=null?mainPanel.name:"NULL")} active={mainPanel?.activeSelf} " +
            $"menuListContent={(menuListContent!=null?menuListContent.name:"NULL")} " +
            $"ScrollRect={(scrollRect!=null?"OK":"NULL")} " +
            $"Viewport={(viewport != null ? $"{viewport.name} anchorMax={viewport.anchorMax} size={viewport.rect.size}" : "NULL")} " +
            $"Content={(content != null ? $"{content.name} childCount={content.childCount}" : "NULL")} " +
            $"CanvasGroup={(cgRoot != null ? $"blocksRaycasts={cgRoot.blocksRaycasts} interactable={cgRoot.interactable}" : "none")}");

        InputManager.Instance?.SetGameplayInputBlocked(GameplayBlockSource, true);
        InputManager.Instance?.CancelAutoMove();

        // Force layout rebuild — đảm bảo ContentSizeFitter tính đúng sau khi spawn rows
        if (menuListContent != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(menuListContent as RectTransform
                ?? menuListContent.GetComponent<RectTransform>());

        Debug.Log($"{LogPrefix} Open | npcId={data.npc_id} name='{data.npc_name}' items='{data.menu_items}'");
    }

    public void Close()
    {
        if (mainPanel) mainPanel.SetActive(false);
        ClearRows();
        _currentInteraction = null;
        InputManager.Instance?.SetGameplayInputBlocked(GameplayBlockSource, false);
        Debug.Log($"{LogPrefix} Close.");
    }

    // ── Rows ───────────────────────────────────────────────────────────

    private void SpawnRow(string label, int index)
    {
        GameObject rowGo;

        if (menuItemRowPrefab != null && menuListContent != null)
        {
            rowGo = Instantiate(menuItemRowPrefab, menuListContent);
            Debug.Log($"{LogPrefix} SpawnRow [{index}] '{label}' | prefab='{menuItemRowPrefab.name}' parent='{menuListContent.name}' rowActive={rowGo.activeSelf}");
        }
        else
        {
            Debug.LogWarning($"{LogPrefix} SpawnRow [{index}] fallback | prefab={(menuItemRowPrefab==null?"NULL":menuItemRowPrefab.name)} content={(menuListContent==null?"NULL":menuListContent.name)}");
            rowGo = CreateFallbackRow(label, index);
            if (rowGo == null) return;
        }

        var row = rowGo.GetComponent<NpcMenuItemRow>();
        if (row != null)
        {
            int captured = index;
            row.Init(label, () => OnRowSelected(captured));
            // ─ Diagnostics: check button + canvas group state
            var btn = rowGo.GetComponent<Button>() ?? rowGo.GetComponentInChildren<Button>(true);
            var cg  = rowGo.GetComponentInParent<CanvasGroup>();
            var scrollSr = menuListContent?.GetComponentInParent<ScrollRect>();
            bool raycaster = FindObjectOfType<GraphicRaycaster>() != null;
            Debug.Log($"{LogPrefix} SpawnRow [{index}] '{label}' | NpcMenuItemRow=OK btn={(btn!=null?"OK btn.interactable="+btn.interactable+" btn.enabled="+btn.enabled:"NULL")} " +
                      $"CanvasGroup={(cg!=null?"blocksRaycasts="+cg.blocksRaycasts+" interactable="+cg.interactable:"none")} " +
                      $"ScrollRect={(scrollSr!=null?"OK":"NULL")} GraphicRaycaster={raycaster}");
        }
        else
        {
            // Prefab không có NpcMenuItemRow → tự bind
            Debug.LogWarning($"{LogPrefix} SpawnRow [{index}] '{label}' | NO NpcMenuItemRow component — manual bind");
            var btn = rowGo.GetComponentInChildren<Button>(true);
            var txt = rowGo.GetComponentInChildren<TMP_Text>(true);
            if (txt) txt.text = label;
            if (btn) { int captured = index; btn.onClick.AddListener(() => OnRowSelected(captured)); }
            Debug.Log($"{LogPrefix} SpawnRow [{index}] manual bind | btn={(btn!=null?"OK":"NULL")} txt={(txt!=null?"OK":"NULL")}");
        }

        _rows.Add(rowGo);
    }

    private void OnRowSelected(int index)
    {
        Debug.Log($"{LogPrefix} Row selected | index={index}");
        if (_currentInteraction == null)
        {
            Debug.LogWarning($"{LogPrefix} OnRowSelected: _currentInteraction is null.");
            Close();
            return;
        }
        _currentInteraction.SelectMenuItemServerRpc(index);
    }

    private void ClearRows()
    {
        foreach (var go in _rows)
            if (go != null) Destroy(go);
        _rows.Clear();
    }

    // ── Helpers ────────────────────────────────────────────────────────

    private static string ResolveLocalPlayerName()
    {
        string name = GameManager.Instance?.currentPlayerData?.character_name ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
            name = PlayerPrefs.GetString("USERNAME", "Người chơi");
        return name;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Fix Viewport RectTransform về anchor (0,0)-(1,1) nếu prefab cũ lưu sai.
    /// Viewport có Mask component — nếu RT = 0×0 thì mask block tất cả click trong ScrollRect.
    /// </summary>
    private static void FixViewportRt(GameObject panelRoot)
    {
        // Tìm ScrollRect → lấy .viewport trực tiếp (chính xác nhất)
        var sr = panelRoot.GetComponentInChildren<ScrollRect>(true);
        if (sr != null && sr.viewport != null)
        {
            var vp = sr.viewport;
            // Chỉ fix nếu anchor vẫn là (0,0)/(0,0) — dấu hiệu prefab cũ bị sai
            if (vp.anchorMax.x < 0.5f && vp.anchorMax.y < 0.5f)
            {
                vp.anchorMin        = Vector2.zero;
                vp.anchorMax        = Vector2.one;
                vp.offsetMin        = Vector2.zero;
                vp.offsetMax        = Vector2.zero;
                vp.pivot            = new Vector2(0f, 1f);
                Debug.Log($"{LogPrefix} FixViewportRt: corrected Viewport anchor from (0,0) to (1,1).");
            }
        }
    }

    /// <summary>
    /// Tìm Screen Space (Overlay hoặc Camera) canvas gốc để làm parent.
    private static Transform FindScreenSpaceCanvas()
    {
        Canvas best = null;
        int bestOrder = int.MinValue;
        foreach (var c in FindObjectsOfType<Canvas>(includeInactive: false))
        {
            // Chỉ chấp nhận Screen Space canvas (Overlay hoặc Camera)
            if (c.renderMode == RenderMode.WorldSpace) continue;
            // Ưu tiên root canvas (không phải nested)
            if (c.transform.parent != null && c.transform.parent.GetComponentInParent<Canvas>() != null) continue;
            if (best == null || c.sortingOrder > bestOrder)
            {
                best = c;
                bestOrder = c.sortingOrder;
            }
        }
        if (best != null) return best.transform;
        Debug.LogWarning($"{LogPrefix} Không tìm thấy Screen Space Canvas.");
        return null;
    }

    // ── Runtime creation (khi chưa có prefab trong scene/Resources) ──────

    private static NpcDynamicMenuUI CreateRuntime()
    {
        var parent = FindScreenSpaceCanvas();
        if (parent == null)
        {
            Debug.LogWarning($"{LogPrefix} No Screen Space Canvas found — cannot create runtime NpcDynamicMenuUI.");
            return null;
        }

        // Root GO
        var root = new GameObject("NpcDynamicMenuPanel");
        root.transform.SetParent(parent, false);
        root.SetActive(false);

        var rt = root.AddComponent<RectTransform>();
        rt.anchorMin  = new Vector2(0.5f, 0.5f);
        rt.anchorMax  = new Vector2(0.5f, 0.5f);
        rt.pivot      = new Vector2(0.5f, 0.5f);
        rt.sizeDelta  = new Vector2(340f, 480f);

        // Background
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.55f, 0.38f, 0.22f, 0.97f); // wood-ish brown

        // Canvas group for raycasting
        root.AddComponent<CanvasGroup>();

        var comp = root.AddComponent<NpcDynamicMenuUI>();
        comp.mainPanel = root;

        // Title
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(root.transform, false);
        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot     = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -12f);
        titleRt.sizeDelta = new Vector2(-20f, 36f);
        var titleTxt = titleGo.AddComponent<TextMeshProUGUI>();
        titleTxt.text      = "Xin chào Người chơi";
        titleTxt.fontSize  = 18f;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.color     = Color.white;
        titleTxt.alignment = TextAlignmentOptions.Center;
        comp.titleText = titleTxt;
        UIRuntimeAssetHelper.ApplyNotoSans(new[] { titleTxt });

        // Separator line
        var sepGo = new GameObject("Separator");
        sepGo.transform.SetParent(root.transform, false);
        var sepRt = sepGo.AddComponent<RectTransform>();
        sepRt.anchorMin = new Vector2(0.05f, 1f);
        sepRt.anchorMax = new Vector2(0.95f, 1f);
        sepRt.pivot     = new Vector2(0.5f, 1f);
        sepRt.anchoredPosition = new Vector2(0f, -50f);
        sepRt.sizeDelta = new Vector2(0f, 2f);
        var sepImg = sepGo.AddComponent<Image>();
        sepImg.color = new Color(1f, 1f, 1f, 0.3f);

        // Scroll view
        var scrollGo = new GameObject("ScrollView");
        scrollGo.transform.SetParent(root.transform, false);
        var scrollRt = scrollGo.AddComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0f, 0f);
        scrollRt.anchorMax = new Vector2(1f, 1f);
        scrollRt.offsetMin = new Vector2(10f, 56f);
        scrollRt.offsetMax = new Vector2(-10f, -56f);

        var scrollRect = scrollGo.AddComponent<ScrollRect>();

        // Viewport
        var viewportGo = new GameObject("Viewport");
        viewportGo.transform.SetParent(scrollGo.transform, false);
        var vpRt = viewportGo.AddComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = Vector2.zero;
        vpRt.offsetMax = Vector2.zero;
        viewportGo.AddComponent<Image>().color = Color.clear;
        viewportGo.AddComponent<Mask>().showMaskGraphic = false;

        // Content
        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(viewportGo.transform, false);
        var contentRt = contentGo.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot     = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 0f);
        var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment         = TextAnchor.UpperCenter;
        vlg.spacing                = 4f;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.padding                = new RectOffset(4, 4, 4, 4);
        var csf = contentGo.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        comp.menuListContent = contentRt;

        scrollRect.content   = contentRt;
        scrollRect.viewport  = vpRt;
        scrollRect.horizontal = false;
        scrollRect.vertical   = true;

        // Row prefab (runtime — created once, stored as inactive template)
        comp.menuItemRowPrefab = CreateRowPrefab(root.transform);

        // Close button "Cáo từ"
        var closeGo = new GameObject("BtnClose");
        closeGo.transform.SetParent(root.transform, false);
        var closeRt = closeGo.AddComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(0.5f, 0f);
        closeRt.anchorMax = new Vector2(0.5f, 0f);
        closeRt.pivot     = new Vector2(0.5f, 0f);
        closeRt.anchoredPosition = new Vector2(0f, 10f);
        closeRt.sizeDelta = new Vector2(160f, 40f);
        var closeImg = closeGo.AddComponent<Image>();
        closeImg.color = new Color(0.60f, 0.38f, 0.10f, 1f);
        var closeBtn = closeGo.AddComponent<Button>();
        var closeLblGo = new GameObject("Label");
        closeLblGo.transform.SetParent(closeGo.transform, false);
        var closeLblRt = closeLblGo.AddComponent<RectTransform>();
        closeLblRt.anchorMin = Vector2.zero;
        closeLblRt.anchorMax = Vector2.one;
        closeLblRt.offsetMin = Vector2.zero;
        closeLblRt.offsetMax = Vector2.zero;
        var closeTxt = closeLblGo.AddComponent<TextMeshProUGUI>();
        closeTxt.text      = "Cáo từ";
        closeTxt.fontSize  = 16f;
        closeTxt.fontStyle = FontStyles.Bold;
        closeTxt.color     = Color.white;
        closeTxt.alignment = TextAlignmentOptions.Center;
        UIRuntimeAssetHelper.ApplyNotoSans(new[] { closeTxt });
        comp.btnClose = closeBtn;
        closeBtn.onClick.AddListener(comp.Close);

        Debug.Log($"{LogPrefix} Created runtime NpcDynamicMenuUI.");
        return comp;
    }

    private static GameObject CreateRowPrefab(Transform hideParent)
    {
        var rowGo = new GameObject("NpcMenuItemRow_Template");
        rowGo.transform.SetParent(hideParent, false);
        rowGo.SetActive(false);

        var rowRt = rowGo.AddComponent<RectTransform>();
        rowRt.sizeDelta = new Vector2(0f, 44f);

        var rowImg = rowGo.AddComponent<Image>();
        rowImg.color = new Color(0f, 0f, 0f, 0.25f);

        var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment        = TextAnchor.MiddleLeft;
        hlg.spacing               = 8f;
        hlg.padding               = new RectOffset(10, 10, 4, 4);
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // Chat bubble icon
        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(rowGo.transform, false);
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.color = Color.white;
        var iconLe = iconGo.AddComponent<LayoutElement>();
        iconLe.minWidth  = 28f;
        iconLe.minHeight = 28f;
        iconLe.flexibleWidth = 0f;
        // Load chat bubble sprite if available
        var chatSprite = Resources.Load<Sprite>("Icons/chat_bubble");
        if (chatSprite != null) iconImg.sprite = chatSprite;
        else iconImg.color = new Color(1f, 1f, 1f, 0.6f);

        // Label
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(rowGo.transform, false);
        var labelTxt = labelGo.AddComponent<TextMeshProUGUI>();
        labelTxt.text      = "Menu Item";
        labelTxt.fontSize  = 16f;
        labelTxt.color     = Color.white;
        labelTxt.alignment = TextAlignmentOptions.MidlineLeft;
        var labelLe = labelGo.AddComponent<LayoutElement>();
        labelLe.flexibleWidth = 1f;
        UIRuntimeAssetHelper.ApplyNotoSans(new[] { labelTxt });

        // Button component on root
        var btn = rowGo.AddComponent<Button>();

        // NpcMenuItemRow component
        rowGo.AddComponent<NpcMenuItemRow>();

        return rowGo;
    }

    private GameObject CreateFallbackRow(string label, int index)
    {
        if (menuListContent == null) return null;
        var rowGo = new GameObject($"Row_{index}");
        rowGo.transform.SetParent(menuListContent, false);
        var rowRt = rowGo.AddComponent<RectTransform>();
        rowRt.sizeDelta = new Vector2(0f, 44f);
        rowGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);
        var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment        = TextAnchor.MiddleLeft;
        hlg.spacing               = 8f;
        hlg.padding               = new RectOffset(10, 10, 4, 4);
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(rowGo.transform, false);
        var txt = labelGo.AddComponent<TextMeshProUGUI>();
        txt.text      = label;
        txt.fontSize  = 16f;
        txt.color     = Color.white;
        txt.alignment = TextAlignmentOptions.MidlineLeft;
        var labelLe = labelGo.AddComponent<LayoutElement>();
        labelLe.flexibleWidth = 1f;
        UIRuntimeAssetHelper.ApplyNotoSans(new[] { txt });
        var btn = rowGo.AddComponent<Button>();
        int captured = index;
        btn.onClick.AddListener(() => OnRowSelected(captured));
        return rowGo;
    }
}
