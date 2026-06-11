using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Tạo prefab BlacksmithFunctionMenuCanvas.prefab.
// Menu: Tools → Blacksmith → Create BlacksmithFunctionMenu Prefab
public static class CreateBlacksmithFunctionMenuPrefab
{
    private const string PREFAB_PATH       = "Assets/Resources/UI/BlacksmithFunctionMenuCanvas.prefab";
    private const string NOTO_SANS_PATH    = "Assets/TextMesh Pro/Resources/Fonts & Materials/NotoSans-Regular SDF.asset";

    // Màu
    private static readonly Color BackdropColor   = new Color(0.03f, 0.04f, 0.07f, 0.78f);
    private static readonly Color CardColor       = new Color(0.08f, 0.09f, 0.12f, 0.96f);
    private static readonly Color TitleColor      = new Color(1.00f, 0.92f, 0.72f, 1.00f);
    private static readonly Color BodyColor       = new Color(0.92f, 0.95f, 1.00f, 1.00f);
    private static readonly Color StatusColor     = new Color(0.76f, 0.83f, 0.95f, 1.00f);
    private static readonly Color BtnEquipment    = new Color(0.72f, 0.42f, 0.14f, 0.94f);
    private static readonly Color BtnGeneMain     = new Color(0.58f, 0.24f, 0.10f, 0.94f);
    private static readonly Color BtnSecSelect    = new Color(0.14f, 0.39f, 0.52f, 0.94f);
    private static readonly Color BtnSecUpgrade   = new Color(0.15f, 0.46f, 0.28f, 0.94f);
    private static readonly Color BtnClose        = new Color(0.26f, 0.28f, 0.34f, 0.96f);

    [MenuItem("Tools/Blacksmith/Create BlacksmithFunctionMenu Prefab")]
    public static void Create()
    {
        Directory.CreateDirectory(Application.dataPath + "/Resources/UI");
        AssetDatabase.Refresh();

        TMP_FontAsset notoSans = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NOTO_SANS_PATH);
        if (notoSans == null)
            Debug.LogWarning("[CreateBlacksmithFunctionMenuPrefab] NotoSans font asset không tìm thấy tại " + NOTO_SANS_PATH + ". Text sẽ dùng font mặc định.");

        // Canvas root
        var root = new GameObject("BlacksmithFunctionMenuCanvas");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        var rootRT = root.GetComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        // BlacksmithFunctionMenuPanel component (trên root)
        var panel = root.AddComponent<BlacksmithFunctionMenuPanel>();

        // Backdrop (fullscreen dim)
        var backdropGO = MakeRT("Backdrop", root.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
        backdropGO.offsetMin = Vector2.zero;
        backdropGO.offsetMax = Vector2.zero;
        var backdropImg = backdropGO.gameObject.AddComponent<Image>();
        backdropImg.color = BackdropColor;

        // Card (nền hộp thoại 900×640)
        var card = MakeRT("BlacksmithFunctionCard", root.transform,
            new Vector2(900f, 640f), Vector2.zero,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var cardImg = card.gameObject.AddComponent<Image>();
        cardImg.color = CardColor;

        var outline = card.gameObject.AddComponent<Outline>();
        outline.effectColor    = new Color(0.95f, 0.76f, 0.32f, 0.45f);
        outline.effectDistance = new Vector2(2f, -2f);

        // Title
        var titleTMP = MakeTMP("Title", card,
            "Thợ Rèn Hắc Long", 44, FontStyles.Bold, TitleColor,
            new Vector2(0f, 245f), new Vector2(720f, 60f), notoSans);

        // Subtitle
        var subtitleTMP = MakeTMP("Subtitle", card,
            "Chọn chức năng muốn dùng khi nói chuyện với thợ rèn.", 25, FontStyles.Normal, BodyColor,
            new Vector2(0f, 185f), new Vector2(760f, 72f), notoSans);

        // Buttons
        Button btnEquipment   = MakeButton("EquipmentUpgradeButton",   card, new Vector2(0f,   78f), BtnEquipment,  "Cường Hóa Trang Bị",         new Vector2(650f, 68f), 30, notoSans);
        Button btnGeneMain    = MakeButton("PrimaryGeneUpgradeButton", card, new Vector2(0f,   -4f), BtnGeneMain,   "Nâng Tier Gene Chính",        new Vector2(650f, 68f), 30, notoSans);
        Button btnSecSelect   = MakeButton("SecondarySelectButton",    card, new Vector2(0f,  -86f), BtnSecSelect,  "Chọn Hệ Thứ 2",              new Vector2(650f, 68f), 30, notoSans);
        Button btnSecUpgrade  = MakeButton("SecondaryUpgradeButton",   card, new Vector2(0f, -168f), BtnSecUpgrade, "Cường Hóa Tier Hệ Thứ 2",   new Vector2(650f, 68f), 30, notoSans);

        // Status text
        var statusTMP = MakeTMP("Status", card,
            "Mang gene và nguyên liệu tới đây, ta lo phần còn lại.", 24, FontStyles.Normal, StatusColor,
            new Vector2(0f, -252f), new Vector2(780f, 72f), notoSans);

        // Close button (X, góc trên phải)
        Button btnClose = MakeButton("CloseButton", card,
            new Vector2(395f, 264f), BtnClose, "X",
            new Vector2(58f, 58f), 26, notoSans);

        // Gán SerializeField qua SerializedObject
        var so = new SerializedObject(panel);
        so.FindProperty("cardTransform")              .objectReferenceValue = card;
        so.FindProperty("titleText")                  .objectReferenceValue = titleTMP;
        so.FindProperty("subtitleText")               .objectReferenceValue = subtitleTMP;
        so.FindProperty("statusText")                 .objectReferenceValue = statusTMP;
        so.FindProperty("equipmentUpgradeButton")     .objectReferenceValue = btnEquipment;
        so.FindProperty("primaryGeneUpgradeButton")   .objectReferenceValue = btnGeneMain;
        so.FindProperty("secondarySelectButton")      .objectReferenceValue = btnSecSelect;
        so.FindProperty("secondaryUpgradeButton")     .objectReferenceValue = btnSecUpgrade;
        so.FindProperty("closeButton")                .objectReferenceValue = btnClose;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Lưu prefab
        bool overwrite = true;
        if (File.Exists(Application.dataPath + "/../" + PREFAB_PATH))
        {
            overwrite = EditorUtility.DisplayDialog(
                "Prefab đã tồn tại",
                $"Prefab tại {PREFAB_PATH} đã tồn tại. Ghi đè?\n(Prefab cũ tại Assets/Prefabs/UI/ không còn được dùng)",
                "Ghi Đè", "Hủy");
        }

        if (overwrite)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            Debug.Log($"[CreateBlacksmithFunctionMenuPrefab] Đã tạo: {PREFAB_PATH}\nRuntime sẽ tự load từ Resources/UI/BlacksmithFunctionMenuCanvas");
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }

        Object.DestroyImmediate(root);
    }

    // Hàm hỗ trợ dùng nội bộ để tách nhỏ xử lý chính.

    private static RectTransform MakeRT(string name, Transform parent,
        Vector2 sizeDelta, Vector2 anchoredPos, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = sizeDelta;
        rt.localScale       = Vector3.one;
        return rt;
    }

    private static TextMeshProUGUI MakeTMP(string name, RectTransform parent,
        string text, float fontSize, FontStyles style, Color color,
        Vector2 anchoredPos, Vector2 size, TMP_FontAsset font)
    {
        var rt  = MakeRT(name, parent, size, anchoredPos, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text               = text;
        tmp.fontSize           = fontSize;
        tmp.fontStyle          = style;
        tmp.color              = color;
        tmp.alignment          = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
        if (font != null) { tmp.font = font; tmp.fontSharedMaterial = font.material; }
        return tmp;
    }

    private static Button MakeButton(string name, RectTransform parent,
        Vector2 anchoredPos, Color baseColor, string label,
        Vector2 size, int fontSize, TMP_FontAsset font)
    {
        var rt  = MakeRT(name, parent, size, anchoredPos, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var img = rt.gameObject.AddComponent<Image>();
        img.color = baseColor;

        var btn = rt.gameObject.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor      = baseColor;
        cb.highlightedColor = baseColor * 1.08f;
        cb.pressedColor     = baseColor * 0.90f;
        cb.selectedColor    = cb.highlightedColor;
        cb.disabledColor    = new Color(0.28f, 0.30f, 0.34f, 0.85f);
        cb.colorMultiplier  = 1f;
        btn.colors          = cb;
        btn.targetGraphic   = img;

        // Label text trong button
        var labelRT = MakeRT("Label", rt, size - new Vector2(30f, 0f), Vector2.zero,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var tmp = labelRT.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text               = label;
        tmp.fontSize           = fontSize;
        tmp.fontStyle          = FontStyles.Bold;
        tmp.color              = Color.white;
        tmp.alignment          = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        if (font != null) { tmp.font = font; tmp.fontSharedMaterial = font.material; }

        return btn;
    }
}
