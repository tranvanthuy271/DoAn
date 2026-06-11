#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Editor tool — tạo prefab UtilityDrawerBox (box tiện ích có nút mũi tên thu gọn/mở rộng).
// Chạy từ menu Unity:
// Tools ▸ Create Utility Drawer Prefab
// Sau khi tạo:
// 1. Kéo prefab vào Canvas trong GameScene.
// 2. Gán các icon tiện ích vào bên trong UtilityContent.
// 3. Điều chỉnh vị trí AnchorExpanded / AnchorCollapsed cho vừa layout.
public static class CreateUtilityDrawerPrefab
{
    private const string PrefabPath = "Assets/Resources/Prefabs/UI/UtilityDrawerBox.prefab";

    // Màu sắc
    private static readonly Color BoxBg          = new Color(0.10f, 0.08f, 0.05f, 0.92f); // nền hộp tối
    private static readonly Color BoxBorder       = new Color(0.60f, 0.45f, 0.10f, 1.00f); // viền vàng nâu
    private static readonly Color ArrowBtnBg      = new Color(0.20f, 0.14f, 0.04f, 1.00f); // nền nút mũi tên
    private static readonly Color ArrowBtnHover   = new Color(0.34f, 0.24f, 0.07f, 1.00f);
    private static readonly Color ShowBtnBg       = new Color(0.18f, 0.13f, 0.04f, 0.95f); // nền nút show
    private static readonly Color ShowBtnHover    = new Color(0.30f, 0.22f, 0.07f, 1.00f);
    private static readonly Color IconPlaceholder = new Color(0.25f, 0.20f, 0.08f, 0.85f); // icon placeholder
    private static readonly Color Gold            = new Color(1.00f, 0.90f, 0.40f, 1.00f);
    private static readonly Color White           = Color.white;

    // Kích thước cố định
    private const float BoxWidth        = 52f;   // chiều rộng box
    private const float BoxHeight       = 176f;  // chiều cao khi mở (tương đương 170 + padding)
    private const float CollapsedHeight = 44f;   // chiều cao khi thu gọn
    private const float IconSize        = 36f;   // kích thước mỗi icon
    private const float IconSpacing     = 4f;    // khoảng cách giữa icon
    private const float ArrowBtnSize    = 28f;   // kích thước nút mũi tên

    [MenuItem("Tools/Create Utility Drawer Prefab")]
    public static void CreatePrefab()
    {
        EnsureFolder("Assets/Resources/Prefabs/UI");

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Utility Drawer Prefab",
                "Prefab đã tồn tại tại:\n" + PrefabPath + "\n\nGhi đè?",
                "Ghi đè", "Hủy");
            if (!overwrite) return;
        }

        var root = BuildHierarchy();

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool ok);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Tạo Utility Drawer Prefab",
            ok ? $"Hoàn tất!\n{PrefabPath}\n\nKéo prefab vào Canvas trong scene."
               : "Lưu prefab thất bại — kiểm tra đường dẫn.",
            "OK");

        if (ok)
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
    }

    // Xây dựng hierarchy
    private static GameObject BuildHierarchy()
    {
        // UtilityRoot
        var root = new GameObject("UtilityRoot");
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(BoxWidth + 8f, BoxHeight + ArrowBtnSize + 8f);

        var controller = root.AddComponent<UtilityDrawerController>();

        // ShowUtilityButton (ẩn khi expanded)
        var showBtn = MakeButton(root.transform, "ShowUtilityButton",
            "▶", 14f, ShowBtnBg, ShowBtnHover, ArrowBtnSize, ArrowBtnSize);
        PinToCorner(showBtn, 0f, 0f); // góc dưới-trái root, sẽ di chuyển trong scene

        // UtilityBox
        var box = new GameObject("UtilityBox");
        box.transform.SetParent(root.transform, false);
        var boxRect = box.AddComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0f, 0f);
        boxRect.anchorMax = new Vector2(0f, 0f);
        boxRect.pivot     = new Vector2(0f, 0f);
        boxRect.sizeDelta = new Vector2(BoxWidth, BoxHeight);
        boxRect.anchoredPosition = new Vector2(4f, 4f);

        var boxImg = box.AddComponent<Image>();
        boxImg.color = BoxBg;
        var boxOutline = box.AddComponent<Outline>();
        boxOutline.effectColor    = BoxBorder;
        boxOutline.effectDistance = new Vector2(2, -2);

        // UtilityContent (danh sách icon)
        var content = new GameObject("UtilityContent");
        content.transform.SetParent(box.transform, false);
        var contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 0f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.offsetMin = new Vector2(4f, ArrowBtnSize + 4f); // dành chỗ cho nút mũi tên bên dưới
        contentRect.offsetMax = new Vector2(-4f, -4f);

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing             = IconSpacing;
        vlg.padding             = new RectOffset(0, 0, 4, 4);
        vlg.childAlignment      = TextAnchor.UpperCenter;
        vlg.childControlWidth   = false;
        vlg.childControlHeight  = false;
        vlg.childForceExpandWidth  = false;
        vlg.childForceExpandHeight = false;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Tạo icon placeholder cho các nút tiện ích
        string[] iconLabels = new[]
        {
            "Quà", "Kho", "Phúc", "Thư", "HĐ", "Vòng", "ƯĐ", "BXH", "Chợ", "Shop"
        };
        foreach (var label in iconLabels)
            AddIconButton(content.transform, label);

        // AnchorExpanded (empty — bên dưới content, trên nút mũi tên)
        var anchorExp = new GameObject("AnchorExpanded");
        anchorExp.transform.SetParent(box.transform, false);
        var ancExpRect = anchorExp.AddComponent<RectTransform>();
        ancExpRect.anchorMin        = new Vector2(0.5f, 0f);
        ancExpRect.anchorMax        = new Vector2(0.5f, 0f);
        ancExpRect.pivot            = new Vector2(0.5f, 1f);
        ancExpRect.sizeDelta        = new Vector2(ArrowBtnSize, 0f);
        ancExpRect.anchoredPosition = new Vector2(0f, ArrowBtnSize + 2f);

        // AnchorCollapsed (empty — mép trên box)
        var anchorCol = new GameObject("AnchorCollapsed");
        anchorCol.transform.SetParent(box.transform, false);
        var ancColRect = anchorCol.AddComponent<RectTransform>();
        ancColRect.anchorMin        = new Vector2(0.5f, 1f);
        ancColRect.anchorMax        = new Vector2(0.5f, 1f);
        ancColRect.pivot            = new Vector2(0.5f, 1f);
        ancColRect.sizeDelta        = new Vector2(ArrowBtnSize, 0f);
        ancColRect.anchoredPosition = new Vector2(0f, 0f);

        // ToggleArrowButton
        var arrowBtnGO = MakeButton(box.transform, "ToggleArrowButton",
            "▼", 14f, ArrowBtnBg, ArrowBtnHover, ArrowBtnSize, ArrowBtnSize);
        var arrowRect = arrowBtnGO.GetComponent<RectTransform>();
        arrowRect.anchorMin        = new Vector2(0.5f, 0f);
        arrowRect.anchorMax        = new Vector2(0.5f, 0f);
        arrowRect.pivot            = new Vector2(0.5f, 0f);
        arrowRect.sizeDelta        = new Vector2(ArrowBtnSize, ArrowBtnSize);
        arrowRect.anchoredPosition = new Vector2(0f, 2f);

        // Viền mũi tên bo góc nhỏ
        var arrowOutline = arrowBtnGO.AddComponent<Outline>();
        arrowOutline.effectColor    = BoxBorder;
        arrowOutline.effectDistance = new Vector2(1, -1);

        // Gán SerializedObject references vào controller
        var so = new SerializedObject(controller);
        SetObjectRef(so, "boxRoot",                box);
        SetObjectRef(so, "contentRoot",            content);
        SetObjectRef(so, "boxRect",                boxRect);
        SetObjectRef(so, "toggleButton",           arrowBtnGO.GetComponent<Button>());
        SetObjectRef(so, "showButton",             showBtn.GetComponent<Button>());
        SetObjectRef(so, "toggleButtonRect",       arrowRect);
        SetObjectRef(so, "toggleGraphic",          arrowRect);
        SetObjectRef(so, "expandedButtonAnchor",   ancExpRect);
        SetObjectRef(so, "collapsedButtonAnchor",  ancColRect);

        so.FindProperty("hideBoxWhenCollapsed").boolValue    = false;
        so.FindProperty("bringToggleButtonToFront").boolValue = true;
        so.FindProperty("startExpanded").boolValue           = true;
        so.FindProperty("expandedArrowRotationZ").floatValue  = 0f;
        so.FindProperty("collapsedArrowRotationZ").floatValue = 180f;
        so.FindProperty("expandedBoxHeight").floatValue       = BoxHeight;
        so.FindProperty("collapsedBoxHeight").floatValue      = CollapsedHeight;
        so.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    // Hàm hỗ trợ dùng nội bộ để tách nhỏ xử lý chính.

    private static void AddIconButton(Transform parent, string label)
    {
        var go = new GameObject($"Icon_{label}");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(IconSize, IconSize);

        var img = go.AddComponent<Image>();
        img.color = IconPlaceholder;

        var btn = go.AddComponent<Button>();
        var bc  = btn.colors;
        bc.normalColor      = IconPlaceholder;
        bc.highlightedColor = new Color(0.40f, 0.32f, 0.12f, 1f);
        bc.pressedColor     = new Color(0.16f, 0.12f, 0.04f, 1f);
        btn.colors = bc;

        var outline = go.AddComponent<Outline>();
        outline.effectColor    = BoxBorder;
        outline.effectDistance = new Vector2(1, -1);

        // Label nhỏ
        var lbl = new GameObject("Label");
        lbl.transform.SetParent(go.transform, false);
        var lblRect = lbl.AddComponent<RectTransform>();
        lblRect.anchorMin = Vector2.zero;
        lblRect.anchorMax = Vector2.one;
        lblRect.offsetMin = Vector2.zero;
        lblRect.offsetMax = Vector2.zero;
        var tmp = lbl.AddComponent<TextMeshProUGUI>();
        tmp.text          = label;
        tmp.fontSize      = 8f;
        tmp.color         = Gold;
        tmp.alignment     = TextAlignmentOptions.Bottom;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
    }

    // Tạo Button với chữ ký hiệu (mũi tên / show)
    private static GameObject MakeButton(Transform parent, string name,
        string symbol, float fontSize,
        Color normalColor, Color hoverColor,
        float width, float height)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);

        var img = go.AddComponent<Image>();
        img.color = normalColor;

        var btn = go.AddComponent<Button>();
        var bc  = btn.colors;
        bc.normalColor      = normalColor;
        bc.highlightedColor = hoverColor;
        bc.pressedColor     = new Color(normalColor.r * 0.7f, normalColor.g * 0.7f, normalColor.b * 0.7f, 1f);
        btn.colors = bc;

        // Graphic mũi tên (child riêng để xoay độc lập)
        var arrow = new GameObject("ArrowGraphic");
        arrow.transform.SetParent(go.transform, false);
        var arrowRect = arrow.AddComponent<RectTransform>();
        arrowRect.anchorMin = Vector2.zero;
        arrowRect.anchorMax = Vector2.one;
        arrowRect.offsetMin = Vector2.zero;
        arrowRect.offsetMax = Vector2.zero;
        var arrowTMP = arrow.AddComponent<TextMeshProUGUI>();
        arrowTMP.text          = symbol;
        arrowTMP.fontSize      = fontSize;
        arrowTMP.fontStyle     = FontStyles.Bold;
        arrowTMP.color         = Gold;
        arrowTMP.alignment     = TextAlignmentOptions.Center;
        arrowTMP.raycastTarget = false;

        return go;
    }

    private static void PinToCorner(GameObject go, float x, float y)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin        = new Vector2(0f, 0f);
        rt.anchorMax        = new Vector2(0f, 0f);
        rt.pivot            = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(x, y);
    }

    private static void SetObjectRef(SerializedObject so, string propName, Object obj)
    {
        var prop = so.FindProperty(propName);
        if (prop != null)
            prop.objectReferenceValue = obj;
        else
            { /* Cảnh báo: Không tìm thấy property '{propName}' trong UtilityDrawerController. Kiểm tra tên field */ }
    }

    private static void EnsureFolder(string path)
    {
        var parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
