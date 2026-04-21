#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.IO;

/// <summary>
/// Editor utility — tạo 2 prefab UI:
///   Assets/Prefabs/UI/LoadingPanel.prefab
///   Assets/Prefabs/UI/ErrorNotifyPanel.prefab
///
/// Cách dùng: Menu  Tools ▸ DoAn ▸ Create Login UI Prefabs
/// Sau khi tạo xong mở prefab, thay các Image sprite bằng art của game.
/// </summary>
public static class LoginPanelPrefabCreator
{
    private const string PrefabDir = "Assets/Prefabs/UI";

    // ── Màu tạm (thay bằng sprite thật trong Inspector) ──────────────────
    // Màu nền overlay
    private static readonly Color OverlayColor        = new Color(0f, 0f, 0f, 0.65f);
    // Khung dialog ngoài (nâu gỗ)
    private static readonly Color DialogBg            = new Color(0.42f, 0.24f, 0.10f);
    // Header bar (nâu đậm hơn)
    private static readonly Color HeaderBg            = new Color(0.30f, 0.16f, 0.06f);
    // Inner content box
    private static readonly Color InnerBg             = new Color(0.23f, 0.13f, 0.05f);
    // Nút (vàng/cam)
    private static readonly Color ButtonBg            = new Color(0.85f, 0.50f, 0.08f);
    // Nút đậm hơn khi hover
    private static readonly Color ButtonHighlight     = new Color(1.00f, 0.68f, 0.18f);
    // Chữ vàng title
    private static readonly Color TitleColor          = new Color(1.00f, 0.90f, 0.30f);
    // Chữ trắng nội dung
    private static readonly Color ContentColor        = Color.white;
    // Progress bar bg
    private static readonly Color ProgressBg          = new Color(0.15f, 0.08f, 0.03f);
    // Progress bar fill (vàng sáng)
    private static readonly Color ProgressFill        = new Color(0.95f, 0.78f, 0.18f);
    // Close button
    private static readonly Color CloseBg             = new Color(0.65f, 0.10f, 0.10f);

    // ── Menu Entry ───────────────────────────────────────────────────────

    [MenuItem("Tools/DoAn/Create Login UI Prefabs")]
    public static void CreateAll()
    {
        Directory.CreateDirectory(Application.dataPath + "/" + PrefabDir.Replace("Assets/", ""));

        CreateLoadingPanel();
        CreateErrorNotifyPanel();

        AssetDatabase.Refresh();
        Debug.Log("[LoginPanelPrefabCreator] ✓ Prefabs created in " + PrefabDir);
        EditorUtility.DisplayDialog("Done",
            "Đã tạo 2 prefab trong " + PrefabDir +
            "\n\nLoadingPanel.prefab\nErrorNotifyPanel.prefab\n\n" +
            "Mở prefab, chọn Image và thay Source Image bằng sprite của game.",
            "OK");
    }

    // ════════════════════════════════════════════════════════════════════
    //  LOADING PANEL
    // ════════════════════════════════════════════════════════════════════

    private static void CreateLoadingPanel()
    {
        // Root — cần RectTransform để stretch đúng trong Canvas runtime
        var root = new GameObject("LoadingPanel");
        root.AddComponent<RectTransform>();
        root.AddComponent<CanvasRenderer>(); // giúp Unity nhận dạng đây là UI element
        Stretch(root);

        // Overlay (dim background)
        var overlay = MakePanel(root.transform, "Overlay", OverlayColor);
        Stretch(overlay);

        // Dialog box  (width 520, height 300)
        var dialog = MakePanel(root.transform, "DialogBox", DialogBg);
        var dialogRt = dialog.GetComponent<RectTransform>();
        dialogRt.anchorMin = dialogRt.anchorMax = new Vector2(0.5f, 0.5f);
        dialogRt.sizeDelta = new Vector2(520, 300);
        dialogRt.anchoredPosition = Vector2.zero;

        // ── Header bar ────────────────────────────────────────────────
        var header = MakePanel(dialog.transform, "HeaderBg", HeaderBg);
        var hrt = header.GetComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0f, 1f);
        hrt.anchorMax = new Vector2(1f, 1f);
        hrt.pivot     = new Vector2(0.5f, 1f);
        hrt.sizeDelta = new Vector2(0, 56);
        hrt.anchoredPosition = Vector2.zero;

        // Title in header
        var title = MakeTmpText(header.transform, "TitleText", "Đang tải", TitleColor, 26, TMPro.FontStyles.Bold);
        Stretch(title);

        // ── Inner content area ────────────────────────────────────────
        var inner = MakePanel(dialog.transform, "ContentArea", InnerBg);
        var irt = inner.GetComponent<RectTransform>();
        irt.anchorMin = new Vector2(0.04f, 0.10f);
        irt.anchorMax = new Vector2(0.96f, 0.76f);
        irt.offsetMin = irt.offsetMax = Vector2.zero;

        // Status message text
        var status = MakeTmpText(inner.transform, "StatusText", "Đang kết nối đến máy chủ...", ContentColor, 17);
        var srt = status.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.02f, 0.52f);
        srt.anchorMax = new Vector2(0.98f, 0.98f);
        srt.offsetMin = srt.offsetMax = Vector2.zero;

        // Progress bar background
        var pBarBg = MakePanel(inner.transform, "ProgressBarBg", ProgressBg);
        var pBgRt = pBarBg.GetComponent<RectTransform>();
        pBgRt.anchorMin = new Vector2(0.02f, 0.10f);
        pBgRt.anchorMax = new Vector2(0.98f, 0.44f);
        pBgRt.offsetMin = pBgRt.offsetMax = Vector2.zero;

        // Progress bar fill  (Image.Type.Filled, Horizontal)
        var pFill = MakePanel(pBarBg.transform, "ProgressBarFill", ProgressFill);
        var pFillImg = pFill.GetComponent<Image>();
        pFillImg.type    = Image.Type.Filled;
        pFillImg.fillMethod    = Image.FillMethod.Horizontal;
        pFillImg.fillOrigin    = (int)Image.OriginHorizontal.Left;
        pFillImg.fillAmount = 0f;
        Stretch(pFill);

        // Percentage text  (centered over bar)
        var pctText = MakeTmpText(pBarBg.transform, "PercentText", "0%", ContentColor, 15, TMPro.FontStyles.Bold);
        var pctTmp = pctText.GetComponent<TMPro.TextMeshProUGUI>();
        // Ensure percent text renders above the progress fill by using a Canvas with higher sorting order
        var pctCanvas = pctTmp.gameObject.AddComponent<Canvas>();
        pctCanvas.overrideSorting = true;
        pctCanvas.sortingOrder = 1;
        Stretch(pctText);

        SavePrefab(root, "LoadingPanel");
    }

    // ════════════════════════════════════════════════════════════════════
    //  ERROR / NOTIFY PANEL
    // ════════════════════════════════════════════════════════════════════

    private static void CreateErrorNotifyPanel()
    {
        var root = new GameObject("ErrorNotifyPanel");
        root.AddComponent<RectTransform>();
        root.AddComponent<CanvasRenderer>();
        Stretch(root);

        // Overlay (dim background)
        var overlay = MakePanel(root.transform, "Overlay", OverlayColor);
        Stretch(overlay);

        // Dialog box (width 480, height 290)
        var dialog = MakePanel(root.transform, "DialogBox", DialogBg);
        var dialogRt = dialog.GetComponent<RectTransform>();
        dialogRt.anchorMin = dialogRt.anchorMax = new Vector2(0.5f, 0.5f);
        dialogRt.sizeDelta = new Vector2(480, 290);
        dialogRt.anchoredPosition = Vector2.zero;

        // ── Close (X) button — top-right ─────────────────────────────
        var closeBtn = MakeButton(dialog.transform, "CloseButton", "✕", CloseBg, ContentColor, 18);
        var crt = closeBtn.GetComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot     = new Vector2(1f, 1f);
        crt.sizeDelta = new Vector2(40, 40);
        crt.anchoredPosition = new Vector2(6, 6);

        // ── Header bar ────────────────────────────────────────────────
        var header = MakePanel(dialog.transform, "HeaderBg", HeaderBg);
        var hrt = header.GetComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0f, 1f);
        hrt.anchorMax = new Vector2(1f, 1f);
        hrt.pivot     = new Vector2(0.5f, 1f);
        hrt.sizeDelta = new Vector2(0, 54);
        hrt.anchoredPosition = Vector2.zero;

        // Title
        var title = MakeTmpText(header.transform, "TitleText", "Nhắc nhở", TitleColor, 26, TMPro.FontStyles.Bold);
        Stretch(title);

        // ── Inner content box ─────────────────────────────────────────
        var inner = MakePanel(dialog.transform, "ContentArea", InnerBg);
        var irt = inner.GetComponent<RectTransform>();
        irt.anchorMin = new Vector2(0.06f, 0.24f);
        irt.anchorMax = new Vector2(0.94f, 0.76f);
        irt.offsetMin = irt.offsetMax = Vector2.zero;

        // Message text
        var msg = MakeTmpText(inner.transform, "MessageText",
            "Không thể kết nối đến máy chủ.\nĐường truyền Internet có vấn đề hoặc\nmáy chủ đang bảo trì.",
            ContentColor, 17);
        var msgTmp = msg.GetComponent<TMPro.TextMeshProUGUI>();
        msgTmp.enableWordWrapping = true;
        Stretch(msg);

        // ── Confirm button ────────────────────────────────────────────
        var confirmBtn = MakeButton(dialog.transform, "ConfirmButton", "Xác nhận", ButtonBg, TitleColor, 20, TMPro.FontStyles.Bold);
        var brt = confirmBtn.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 0f);
        brt.anchorMax = new Vector2(0.5f, 0f);
        brt.pivot     = new Vector2(0.5f, 0f);
        brt.sizeDelta = new Vector2(180, 46);
        brt.anchoredPosition = new Vector2(0, 14);

        // Button color transition to golden highlight
        var colors = confirmBtn.GetComponent<Button>().colors;
        colors.normalColor      = ButtonBg;
        colors.highlightedColor = ButtonHighlight;
        colors.pressedColor     = new Color(0.60f, 0.35f, 0.04f);
        confirmBtn.GetComponent<Button>().colors = colors;

        SavePrefab(root, "ErrorNotifyPanel");
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static GameObject MakePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = color;
        go.layer = LayerMask.NameToLayer("UI");
        return go;
    }

    private static GameObject MakeTmpText(Transform parent, string name, string text,
        Color color, float fontSize,
        TMPro.FontStyles style = TMPro.FontStyles.Normal)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text      = text;
        tmp.color     = color;
        tmp.fontSize  = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        go.layer = LayerMask.NameToLayer("UI");
        return go;
    }

    private static GameObject MakeButton(Transform parent, string name,
        string label, Color bgColor, Color textColor,
        float fontSize = 18, TMPro.FontStyles fontStyle = TMPro.FontStyles.Normal)
    {
        var go = MakePanel(parent, name, bgColor);
        go.AddComponent<Button>();

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(go.transform, false);
        textGO.AddComponent<RectTransform>();
        var t = textGO.AddComponent<TMPro.TextMeshProUGUI>();
        t.text      = label;
        t.color     = textColor;
        t.fontSize  = fontSize;
        t.fontStyle = fontStyle;
        t.alignment = TMPro.TextAlignmentOptions.Center;
        textGO.layer = LayerMask.NameToLayer("UI");
        Stretch(textGO);
        return go;
    }

    private static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = rt.offsetMax = Vector2.zero;
    }

    private static void SavePrefab(GameObject root, string prefabName)
    {
        string path = PrefabDir + "/" + prefabName + ".prefab";
        bool success;
        PrefabUtility.SaveAsPrefabAsset(root, path, out success);
        Object.DestroyImmediate(root);

        if (success)
            Debug.Log($"[LoginPanelPrefabCreator] ✓ Saved {path}");
        else
            Debug.LogError($"[LoginPanelPrefabCreator] ✗ Failed to save {path}");
    }
}
#endif
