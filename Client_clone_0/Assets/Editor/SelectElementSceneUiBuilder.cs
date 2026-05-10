#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tools → DoAn → Rebuild SelectElement Scene UI
///
/// Dựng lại layout UI cho scene chọn nhân vật theo bố cục:
///   - Trái  : Grid 2×3 gồm 6 nút chân dung nhân vật (6 hệ)
///   - Giữa  : Ảnh preview nhân vật đã chọn
///   - Phải  : Panel gỗ hiển thị thông tin nhân vật được chọn
///   - Dưới  : "Trở về" | Input tên nhân vật | "Tạo mới / Xác nhận"
/// </summary>
public static class SelectElementSceneUiBuilder
{
    private const string ScenePath          = "Assets/Scenes/SelectElement.unity";
    private const string BackgroundSpritePath = "Assets/Resources/Loading/bg.jpg";
    private const string WoodPanelSpritePath  = "Assets/Resources/UINew/a662941f-2e34-4ff5-8bdc-14068425bbdb-removebg-preview.png";
    private const string WoodButtonSpritePath = "Assets/Resources/UINew/0a503eb3-84a7-4f23-bf73-c0e32d09bf60_rm_bg.png";
    private const string ElementIconConfigPath = "Assets/Resources/ScriptableObjects/ElementIconConfig.asset";
    private const string LoadingPanelPrefabPath = "Assets/Prefabs/UI/LoadingPanel.prefab";

    // Thứ tự elementId cho 6 nút theo bố cục grid 2×3 (hàng trên-trái → phải, rồi xuống):
    // [0]=Kim(0), [1]=Mộc(1), [2]=Thủy(2), [3]=Thổ(4), [4]=Phong(5), [5]=Hỏa(3)
    private static readonly int[] ElementIds  = { 0, 1, 2, 4, 5, 3 };
    private static readonly string[] ElementNames = { "Kim", "Mộc", "Thủy", "Thổ", "Phong", "Hỏa" };

    private static readonly Color BtnNormal     = new Color(0.15f, 0.08f, 0.03f, 0.85f);
    private static readonly Color BtnGoldBorder = new Color(1f, 0.78f, 0.20f, 1f);
    private static readonly Color BtnHighlight  = new Color(0.95f, 0.60f, 0.10f, 1f);
    private static readonly Color BtnSelected   = new Color(0.15f, 0.60f, 0.15f, 1f);
    private static readonly Color PanelBg       = new Color(0.46f, 0.22f, 0.07f, 0.94f);
    private static readonly Color GoldText      = new Color(1f, 0.93f, 0.40f, 1f);
    private static readonly Color BrownDark     = new Color(0.36f, 0.15f, 0.05f, 1f);
    private static readonly Color InputColor    = new Color(0.72f, 0.34f, 0.08f, 0.96f);
    private static readonly Color InputOutline  = new Color(1f, 0.67f, 0.18f, 1f);
    private static readonly Color ButtonColor   = new Color(0.77f, 0.33f, 0.08f, 1f);
    private static readonly Color ButtonHL      = new Color(1f, 0.60f, 0.14f, 1f);

    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("Tools/DoAn/Rebuild SelectElement Scene UI")]
    public static void RebuildSelectElementSceneUi()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        SE_DestroyRoot("Canvas");
        SE_DestroyRoot("SelectElementCanvas");
        SE_DestroyRoot("SelectElementController");
        SE_DestroyRoot("APIClient");
        SE_DestroyRoot("LoginLoadingManager");

        Sprite bgSprite    = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundSpritePath);
        Sprite woodPanel   = AssetDatabase.LoadAssetAtPath<Sprite>(WoodPanelSpritePath);
        Sprite woodButton  = AssetDatabase.LoadAssetAtPath<Sprite>(WoodButtonSpritePath);
        var    iconConfig  = AssetDatabase.LoadAssetAtPath<ElementIconConfig>(ElementIconConfigPath);

        SE_EnsureEventSystem();

        // ── Canvas ────────────────────────────────────────────────
        var canvasGo = SE_CreateCanvas();

        // ── Background ────────────────────────────────────────────
        SE_BuildBackground(canvasGo.transform, bgSprite);

        // ── Header (title + version, NO big logo) ─────────────────
        SE_BuildHeader(canvasGo.transform);

        // ── Controller ────────────────────────────────────────────
        var ctrlGo     = new GameObject("SelectElementController");
        var controller = ctrlGo.AddComponent<SelectElementController>();

        // ── Left grid: 6 portrait buttons ────────────────────────
        var buttons = SE_BuildCharacterGrid(canvasGo.transform);

        // ── Center: character preview image ───────────────────────
        var previewImage = SE_BuildPreview(canvasGo.transform);

        // ── Right: info panel  ────────────────────────────────────
        SE_BuildInfoPanel(canvasGo.transform, woodPanel,
            out TMP_Text instructionText);

        // ── Bottom bar ────────────────────────────────────────────
        SE_BuildBottomBar(canvasGo.transform, woodButton,
            out TMP_InputField nameInput,
            out Button confirmButton,
            out Button backButton,
            out TMP_Text errorText);

        // ── Wire controller ───────────────────────────────────────
        SE_WireController(controller, buttons, nameInput,
            errorText, instructionText, confirmButton, backButton,
            previewImage, iconConfig);

        // ── Utilities ─────────────────────────────────────────────
        SE_EnsureApiClient();
        SE_EnsureLoadingManager();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "SelectElement UI",
            "Đã dựng lại scene SelectElement.\n" +
            "• Grid 2×3 nhân vật bên trái\n" +
            "• Preview giữa màn hình\n" +
            "• Panel thông tin bên phải\n" +
            "• Thanh nút dưới cùng",
            "OK");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Canvas
    // ─────────────────────────────────────────────────────────────────────────
    private static GameObject SE_CreateCanvas()
    {
        var go = new GameObject("SelectElementCanvas",
            typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        SE_SetUiLayer(go);

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = false;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1024f, 640f);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight   = 0.5f;

        SE_Stretch(go.GetComponent<RectTransform>());
        return go;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Background
    // ─────────────────────────────────────────────────────────────────────────
    private static void SE_BuildBackground(Transform parent, Sprite bg)
    {
        var bgGo = SE_MakeImage(parent, "Background", new Color(0.33f,0.63f,0.93f,1f), bg);
        SE_Stretch(bgGo.GetComponent<RectTransform>());
        if (bg != null)
            bgGo.GetComponent<Image>().color = Color.white;

        // Light dim overlay so UI elements pop
        var dim = SE_MakeImage(parent, "BackgroundDim", new Color(0f,0f,0f,0.12f), null);
        SE_Stretch(dim.GetComponent<RectTransform>());
        dim.GetComponent<Image>().raycastTarget = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Header
    // ─────────────────────────────────────────────────────────────────────────
    private static void SE_BuildHeader(Transform parent)
    {
        var title = SE_MakeText(parent, "TopLeftTitle", "Làng Lá Base", 18f, Color.white, FontStyles.Bold);
        SE_Anchor(title.rectTransform, new Vector2(0f,1f), new Vector2(0f,1f), new Vector2(220f,30f), new Vector2(16f,-16f));
        title.alignment = TextAlignmentOptions.Left;
        SE_AddShadow(title.gameObject, new Vector2(1.5f,-1.5f));

        var version = SE_MakeText(parent, "VersionText", "Phiên bản: 1.3.4", 18f, Color.white, FontStyles.Bold);
        SE_Anchor(version.rectTransform, new Vector2(1f,1f), new Vector2(1f,1f), new Vector2(230f,30f), new Vector2(-16f,-16f));
        version.alignment = TextAlignmentOptions.Right;
        SE_AddShadow(version.gameObject, new Vector2(1.5f,-1.5f));

        var sceneTitle = SE_MakeText(parent, "SceneTitle", "CHỌN NHÂN VẬT", 28f, GoldText, FontStyles.Bold);
        SE_Anchor(sceneTitle.rectTransform, new Vector2(0.5f,1f), new Vector2(0.5f,1f), new Vector2(400f,40f), new Vector2(0f,-28f));
        SE_AddShadow(sceneTitle.gameObject, new Vector2(2f,-2f), BrownDark);
        SE_AddOutline(sceneTitle.gameObject, new Color(0.5f,0.25f,0.05f,1f), new Vector2(2f,-2f));
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Left – 6 portrait buttons in 2×3 GridLayoutGroup
    // ─────────────────────────────────────────────────────────────────────────
    private static Button[] SE_BuildCharacterGrid(Transform parent)
    {
        // Container anchored to the left of the canvas
        var gridContainer = new GameObject("CharacterButtonGrid", typeof(RectTransform));
        gridContainer.transform.SetParent(parent, false);
        SE_SetUiLayer(gridContainer);
        // Position: center of grid is about 400 left of canvas center, slightly above center
        // Grid: 2 cols × 3 rows of 90×90 cells with 10px spacing → 190×290
        SE_Anchor(gridContainer.GetComponent<RectTransform>(),
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            new Vector2(195f, 295f), new Vector2(-390f, 40f));

        var grid = gridContainer.AddComponent<GridLayoutGroup>();
        grid.cellSize        = new Vector2(90f, 90f);
        grid.spacing         = new Vector2(10f, 10f);
        grid.startCorner     = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis       = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment  = TextAnchor.UpperLeft;
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;

        var buttons = new Button[6];

        for (int i = 0; i < 6; i++)
        {
            string elementName = ElementNames[i];

            // Outer frame (gold border)
            var frame = SE_MakeImage(gridContainer.transform, $"CharBtn_{elementName}", BtnNormal, null);
            SE_AddOutline(frame, BtnGoldBorder, new Vector2(2.5f,-2.5f));

            var btn = frame.AddComponent<Button>();
            btn.targetGraphic = frame.GetComponent<Image>();
            var cb = btn.colors;
            cb.normalColor      = BtnNormal;
            cb.highlightedColor = BtnHighlight;
            cb.pressedColor     = new Color(0.10f,0.45f,0.10f,0.95f);
            cb.selectedColor    = BtnSelected;
            cb.disabledColor    = new Color(0.30f,0.30f,0.30f,0.60f);
            btn.colors = cb;

            // Portrait image (fills the button, controller will set sprite at runtime)
            var portrait = SE_MakeImage(frame.transform, "Portrait", new Color(1f,1f,1f,0f), null);
            SE_Stretch(portrait.GetComponent<RectTransform>());
            portrait.GetComponent<Image>().raycastTarget = false;
            portrait.GetComponent<Image>().preserveAspect = true;

            // Element name label (bottom strip)
            var nameLabel = SE_MakeText(frame.transform, "ElementLabel", elementName, 13f, GoldText, FontStyles.Bold);
            var nameLabelRt = nameLabel.rectTransform;
            nameLabelRt.anchorMin = new Vector2(0f,0f);
            nameLabelRt.anchorMax = new Vector2(1f,0f);
            nameLabelRt.pivot     = new Vector2(0.5f,0f);
            nameLabelRt.offsetMin = new Vector2(0f,0f);
            nameLabelRt.offsetMax = new Vector2(0f,22f);
            nameLabel.alignment   = TextAlignmentOptions.Center;
            SE_AddShadow(nameLabel.gameObject, new Vector2(1f,-1f), BrownDark);

            buttons[i] = btn;
        }

        return buttons;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Center – character preview image
    // ─────────────────────────────────────────────────────────────────────────
    private static Image SE_BuildPreview(Transform parent)
    {
        // Platform / base (decorative circle)
        var platformGo = SE_MakeImage(parent, "PreviewPlatform",
            new Color(0.60f, 0.40f, 0.15f, 0.55f), null);
        SE_Anchor(platformGo.GetComponent<RectTransform>(),
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            new Vector2(180f, 40f), new Vector2(-70f, -145f));
        SE_AddOutline(platformGo, new Color(1f,0.75f,0.20f,0.8f), new Vector2(2f,-2f));

        // Preview image for the character sprite
        var previewGo = SE_MakeImage(parent, "CharacterPreview",
            new Color(1f,1f,1f,0f), null);
        SE_Anchor(previewGo.GetComponent<RectTransform>(),
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            new Vector2(220f, 320f), new Vector2(-70f, 30f));
        var previewImg = previewGo.GetComponent<Image>();
        previewImg.preserveAspect = true;
        previewImg.raycastTarget  = false;

        return previewImg;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Right – info panel
    // ─────────────────────────────────────────────────────────────────────────
    private static void SE_BuildInfoPanel(Transform parent, Sprite woodSprite,
        out TMP_Text instructionText)
    {
        // Outer panel
        var panel = SE_MakeImage(parent, "InfoPanel", PanelBg, woodSprite);
        if (woodSprite != null) panel.GetComponent<Image>().color = Color.white;
        SE_Anchor(panel.GetComponent<RectTransform>(),
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            new Vector2(230f, 310f), new Vector2(390f, 25f));
        SE_AddOutline(panel, new Color(1f,0.72f,0.20f,0.90f), new Vector2(2f,-2f));

        // Title bar (darker strip at top)
        var titleBar = SE_MakeImage(panel.transform, "TitleBar",
            new Color(0.25f, 0.10f, 0.03f, 0.85f), null);
        var tbRt = titleBar.GetComponent<RectTransform>();
        tbRt.anchorMin = new Vector2(0f,1f);
        tbRt.anchorMax = new Vector2(1f,1f);
        tbRt.pivot     = new Vector2(0.5f,1f);
        tbRt.offsetMin = new Vector2(0f,-50f);
        tbRt.offsetMax = Vector2.zero;

        // Panel title text
        var panelTitle = SE_MakeText(titleBar.transform, "PanelTitle",
            "Thông tin nhân vật", 16f, GoldText, FontStyles.Bold);
        SE_Stretch(panelTitle.rectTransform);
        panelTitle.alignment = TextAlignmentOptions.Center;
        SE_AddShadow(panelTitle.gameObject, new Vector2(1.5f,-1.5f), BrownDark);

        // Divider
        var divider = SE_MakeImage(panel.transform, "Divider",
            new Color(1f, 0.75f, 0.20f, 0.60f), null);
        var divRt = divider.GetComponent<RectTransform>();
        divRt.anchorMin = new Vector2(0.05f,1f);
        divRt.anchorMax = new Vector2(0.95f,1f);
        divRt.pivot     = new Vector2(0.5f,1f);
        divRt.offsetMin = new Vector2(0f,-52f);
        divRt.offsetMax = new Vector2(0f,-50f);
        divider.GetComponent<Image>().raycastTarget = false;

        // Instruction / info content text
        instructionText = SE_MakeText(panel.transform, "InstructionText",
            "Chọn nhân vật của bạn", 15f, Color.white, FontStyles.Normal);
        var itRt = instructionText.rectTransform;
        itRt.anchorMin = new Vector2(0f,0f);
        itRt.anchorMax = new Vector2(1f,1f);
        itRt.pivot     = new Vector2(0.5f,0.5f);
        itRt.offsetMin = new Vector2(12f,10f);
        itRt.offsetMax = new Vector2(-12f,-56f);
        instructionText.alignment      = TextAlignmentOptions.TopLeft;
        instructionText.enableWordWrapping = true;
        instructionText.overflowMode   = TextOverflowModes.Overflow;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Bottom bar
    // ─────────────────────────────────────────────────────────────────────────
    private static void SE_BuildBottomBar(Transform parent, Sprite woodButton,
        out TMP_InputField nameInput,
        out Button confirmButton,
        out Button backButton,
        out TMP_Text errorText)
    {
        // ── Error / status text (just above the bar) ──────────────
        errorText = SE_MakeText(parent, "ErrorText", string.Empty, 15f, Color.red, FontStyles.Bold);
        SE_Anchor(errorText.rectTransform,
            new Vector2(0.5f,0f), new Vector2(0.5f,0f),
            new Vector2(480f,30f), new Vector2(0f,80f));
        errorText.alignment = TextAlignmentOptions.Center;
        errorText.enableWordWrapping = false;

        // ── "Trở về" button ───────────────────────────────────────
        var backGo = SE_MakeButton(parent, "BackButton", "Trở về",
            ButtonColor, Color.white, 20f, woodButton);
        SE_Anchor(backGo.GetComponent<RectTransform>(),
            new Vector2(0.5f,0f), new Vector2(0.5f,0f),
            new Vector2(130f, 52f), new Vector2(-390f, 32f));
        backButton = backGo.GetComponent<Button>();

        // ── Character name input (hidden by default; controller shows it) ──
        var inputContainer = SE_MakeImage(parent, "CharacterNameInput", InputColor, null);
        SE_Anchor(inputContainer.GetComponent<RectTransform>(),
            new Vector2(0.5f,0f), new Vector2(0.5f,0f),
            new Vector2(280f, 46f), new Vector2(0f, 32f));
        SE_AddOutline(inputContainer, InputOutline, new Vector2(2f,-2f));
        inputContainer.SetActive(false);   // hidden until "Tạo mới" clicked

        nameInput = inputContainer.AddComponent<TMP_InputField>();
        nameInput.targetGraphic = inputContainer.GetComponent<Image>();
        nameInput.contentType   = TMP_InputField.ContentType.Standard;
        nameInput.lineType      = TMP_InputField.LineType.SingleLine;
        nameInput.caretColor    = Color.white;
        nameInput.selectionColor = new Color(0.95f,0.76f,0.20f,0.45f);
        nameInput.characterLimit = 20;

        var textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(inputContainer.transform, false);
        SE_SetUiLayer(textArea);
        SE_StretchWithPadding(textArea.GetComponent<RectTransform>(), 12f,5f,12f,5f);

        var ph = SE_MakeText(textArea.transform, "Placeholder",
            "Tên nhân vật (3-20 ký tự)", 15f, new Color(0.86f,0.78f,0.66f,0.84f), FontStyles.Bold);
        SE_Stretch(ph.rectTransform);
        ph.alignment = TextAlignmentOptions.Left;

        var inputText = SE_MakeText(textArea.transform, "Text", string.Empty, 16f, Color.white, FontStyles.Bold);
        SE_Stretch(inputText.rectTransform);
        inputText.alignment = TextAlignmentOptions.Left;

        nameInput.textViewport  = textArea.GetComponent<RectTransform>();
        nameInput.textComponent = inputText;
        nameInput.placeholder   = ph;

        // ── "Tạo mới / Xác nhận" button ─────────────────────────
        var confirmGo = SE_MakeButton(parent, "ConfirmButton", "Tạo mới",
            ButtonColor, Color.white, 20f, woodButton);
        SE_Anchor(confirmGo.GetComponent<RectTransform>(),
            new Vector2(0.5f,0f), new Vector2(0.5f,0f),
            new Vector2(155f, 52f), new Vector2(390f, 32f));
        confirmButton = confirmGo.GetComponent<Button>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Wire SelectElementController
    // ─────────────────────────────────────────────────────────────────────────
    private static void SE_WireController(
        SelectElementController controller,
        Button[] buttons,
        TMP_InputField nameInput,
        TMP_Text errorText,
        TMP_Text instructionText,
        Button confirmButton,
        Button backButton,
        Image previewImage,
        ElementIconConfig iconConfig)
    {
        var so = new SerializedObject(controller);

        // characterButtons array
        var buttonsArray = so.FindProperty("characterButtons");
        if (buttonsArray != null)
        {
            buttonsArray.arraySize = buttons.Length;
            for (int i = 0; i < buttons.Length; i++)
            {
                var elem     = buttonsArray.GetArrayElementAtIndex(i);
                var btnProp  = elem.FindPropertyRelative("button");
                var idProp   = elem.FindPropertyRelative("elementId");
                if (btnProp != null)  btnProp.objectReferenceValue = buttons[i];
                if (idProp  != null)  idProp.intValue              = ElementIds[i];
            }
        }

        SE_SetRef(so, "characterNameInput", nameInput);
        SE_SetRef(so, "errorText",          errorText);
        SE_SetRef(so, "instructionText",    instructionText);
        SE_SetRef(so, "confirmButton",      confirmButton);
        SE_SetRef(so, "backButton",         backButton);
        SE_SetRef(so, "previewImage",       previewImage);

        if (iconConfig != null)
            SE_SetRef(so, "elementIconConfig", iconConfig);

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Utilities
    // ─────────────────────────────────────────────────────────────────────────
    private static void SE_EnsureApiClient()
    {
        if (Object.FindObjectOfType<APIClient>() == null)
            new GameObject("APIClient").AddComponent<APIClient>();
    }

    private static void SE_EnsureLoadingManager()
    {
        var mgr = Object.FindObjectOfType<LoginLoadingManager>();
        if (mgr == null)
            mgr = new GameObject("LoginLoadingManager").AddComponent<LoginLoadingManager>();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LoadingPanelPrefabPath);
        if (prefab == null) return;

        var so = new SerializedObject(mgr);
        SE_SetRef(so, "loadingPanelPrefab", prefab);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SE_EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        SE_SetUiLayer(go);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  UI helpers
    // ─────────────────────────────────────────────────────────────────────────
    private static GameObject SE_MakeButton(Transform parent, string name, string label,
        Color color, Color textColor, float fontSize, Sprite sprite)
    {
        var go  = SE_MakeImage(parent, name, color, sprite);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();
        var cb = btn.colors;
        cb.normalColor      = color;
        cb.highlightedColor = ButtonHL;
        cb.pressedColor     = new Color(0.50f,0.17f,0.04f,1f);
        cb.disabledColor    = new Color(0.35f,0.35f,0.35f,0.55f);
        btn.colors = cb;
        var txt = SE_MakeText(go.transform, "Label", label, fontSize, textColor, FontStyles.Bold);
        SE_Stretch(txt.rectTransform);
        SE_AddShadow(txt.gameObject, new Vector2(1.5f,-1.5f), BrownDark);
        return go;
    }

    private static GameObject SE_MakeImage(Transform parent, string name, Color color, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        SE_SetUiLayer(go);
        var img = go.GetComponent<Image>();
        img.color  = color;
        img.sprite = sprite;
        img.type   = Image.Type.Simple;
        img.raycastTarget = true;
        return go;
    }

    private static TextMeshProUGUI SE_MakeText(Transform parent, string name, string text,
        float size, Color color, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        SE_SetUiLayer(go);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text         = text;
        tmp.fontSize     = size;
        tmp.color        = color;
        tmp.fontStyle    = style;
        tmp.alignment    = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        UIRuntimeAssetHelper.ApplyNotoSans(tmp);
        return tmp;
    }

    private static void SE_AddShadow(GameObject go, Vector2 dist)                => SE_AddShadow(go, dist, new Color(0f,0f,0f,0.78f));
    private static void SE_AddShadow(GameObject go, Vector2 dist, Color col)     { var s = go.AddComponent<Shadow>();  s.effectColor = col; s.effectDistance = dist; }
    private static void SE_AddOutline(GameObject go, Color col, Vector2 dist)    { var o = go.AddComponent<Outline>(); o.effectColor = col; o.effectDistance = dist; }

    private static void SE_Anchor(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 size, Vector2 pos)
    {
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = pivot; rt.sizeDelta = size; rt.anchoredPosition = pos;
    }
    private static void SE_Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f,0.5f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
    private static void SE_StretchWithPadding(RectTransform rt, float l, float b, float r, float t)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(l,b); rt.offsetMax = new Vector2(-r,-t);
    }
    private static void SE_SetUiLayer(GameObject go)
    {
        int layer = LayerMask.NameToLayer("UI");
        if (layer >= 0) go.layer = layer;
    }
    private static void SE_SetRef(SerializedObject so, string prop, Object val)
    {
        var p = so.FindProperty(prop);
        if (p != null) p.objectReferenceValue = val;
    }
    private static void SE_DestroyRoot(string n)
    {
        var t = GameObject.Find(n);
        if (t != null && t.transform.parent == null) Object.DestroyImmediate(t);
    }
}
#endif
