#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

/// <summary>
/// Editor tool — tạo tự động scene SelectGene với đầy đủ UI hierarchy.
/// Chạy từ menu Unity: Tools ▸ Game ▸ Create SelectGene Scene
///
/// Scene hierarchy được tạo:
///   SelectGeneRoot (SelectGeneController)
///   Canvas (Screen Space - Overlay)
///     Background (Image – nền tối)
///     Header
///       Title ("Chọn Hệ Gene" – TMP)
///     SlotsContainer (HorizontalLayoutGroup)
///       Slot1 (GeneSlotUI)
///         SlotTitle, ExistingPanel (Name/Level/Element/PlayBtn), EmptyPanel (CreateBtn), LockedPanel
///       Slot2 (GeneSlotUI)
///         SlotTitle, ExistingPanel, EmptyPanel, LockedPanel
///     ErrorText (TMP)
///     LoadingOverlay
///       LoadingText (TMP)
///     ExitButton
///     CreateGene2Panel (tạo nhân vật mới)
/// </summary>
public static class SelectGeneSceneSetup
{
    private const string ScenePath    = "Assets/Scenes/SelectGene.unity";
    private const string ScenesFolder  = "Assets/Scenes";
    private const string NotoSansPath  = "Assets/TextMesh Pro/Resources/Fonts & Materials/NotoSans-Regular SDF.asset";

    // Palette
    private static readonly Color BgColor      = new Color(0.08f, 0.06f, 0.04f, 1f);
    private static readonly Color PanelColor   = new Color(0.18f, 0.11f, 0.04f, 0.96f);
    private static readonly Color AccentGold   = new Color(0.95f, 0.80f, 0.30f, 1f);
    private static readonly Color ButtonOrange = new Color(0.75f, 0.35f, 0.05f, 1f);
    private static readonly Color ButtonGreen  = new Color(0.15f, 0.55f, 0.20f, 1f);
    private static readonly Color ButtonRed    = new Color(0.65f, 0.10f, 0.10f, 1f);
    private static readonly Color White        = Color.white;
    private static readonly Color LightGray    = new Color(0.85f, 0.85f, 0.85f, 1f);

    [MenuItem("Tools/Game/Create SelectGene Scene")]
    public static void CreateSelectGeneScene()
    {
        // Tạo thư mục Scenes nếu chưa có
        if (!Directory.Exists(ScenesFolder))
            Directory.CreateDirectory(ScenesFolder);

        // Tạo scene mới
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Xoá Main Camera mặc định để tránh xung đột
        foreach (var go in scene.GetRootGameObjects())
        {
            if (go.name == "Main Camera" || go.name == "Directional Light")
                Object.DestroyImmediate(go);
        }

        // ── Root Controller ──────────────────────────────────────────────
        var root = new GameObject("SelectGeneRoot");
        var controller = root.AddComponent<SelectGeneController>();

        // ── Canvas ──────────────────────────────────────────────────────
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        canvasScaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Background ──────────────────────────────────────────────────
        var bg = CreateUIImage(canvasGO.transform, "Background", BgColor);
        SetStretch(bg.GetComponent<RectTransform>());

        // ── Header ──────────────────────────────────────────────────────
        var header = new GameObject("Header");
        header.transform.SetParent(canvasGO.transform, false);
        var headerRect = header.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 0.85f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.offsetMin = headerRect.offsetMax = Vector2.zero;

        var titleText = CreateText(header.transform, "TitleText", "Chọn Hệ Gene",
            fontSize: 64, color: AccentGold, bold: true);
        var titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = titleRect.offsetMax = Vector2.zero;
        titleText.alignment = TextAlignmentOptions.Center;

        // ── Slots Container ──────────────────────────────────────────────
        var slotsContainerGO = new GameObject("SlotsContainer");
        slotsContainerGO.transform.SetParent(canvasGO.transform, false);
        var slotsRect = slotsContainerGO.AddComponent<RectTransform>();
        slotsRect.anchorMin = new Vector2(0.05f, 0.1f);
        slotsRect.anchorMax = new Vector2(0.95f, 0.85f);
        slotsRect.offsetMin = slotsRect.offsetMax = Vector2.zero;

        var hlg = slotsContainerGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 40;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth  = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        hlg.padding = new RectOffset(20, 20, 20, 20);

        // ── Slot 1 ────────────────────────────────────────────────────────
        var slot1GO = BuildSlotUI(slotsContainerGO.transform, "Slot1", slotIndex: 1);
        var slot1UI = slot1GO.GetComponent<GeneSlotUI>();

        // ── Slot 2 ────────────────────────────────────────────────────────
        var slot2GO = BuildSlotUI(slotsContainerGO.transform, "Slot2", slotIndex: 2);
        var slot2UI = slot2GO.GetComponent<GeneSlotUI>();

        // ── Error Text ───────────────────────────────────────────────────
        var errorText = CreateText(canvasGO.transform, "ErrorText", "", fontSize: 28, color: new Color(1f, 0.4f, 0.4f));
        var errorRect = errorText.GetComponent<RectTransform>();
        errorRect.anchorMin = new Vector2(0.1f, 0.04f);
        errorRect.anchorMax = new Vector2(0.9f, 0.1f);
        errorRect.offsetMin = errorRect.offsetMax = Vector2.zero;
        errorText.alignment = TextAlignmentOptions.Center;
        errorText.gameObject.SetActive(false);

        // ── Loading Overlay ──────────────────────────────────────────────
        var loadingOverlay = CreateUIImage(canvasGO.transform, "LoadingOverlay", new Color(0, 0, 0, 0.7f));
        SetStretch(loadingOverlay.GetComponent<RectTransform>());
        loadingOverlay.GetComponent<RectTransform>().SetAsLastSibling();

        var loadingText = CreateText(loadingOverlay.transform, "LoadingText",
            "Đang tải...", fontSize: 44, color: White);
        var loadingTRect = loadingText.GetComponent<RectTransform>();
        loadingTRect.anchorMin = new Vector2(0.2f, 0.4f);
        loadingTRect.anchorMax = new Vector2(0.8f, 0.6f);
        loadingTRect.offsetMin = loadingTRect.offsetMax = Vector2.zero;
        loadingText.alignment = TextAlignmentOptions.Center;
        loadingOverlay.SetActive(false);

        // ── Exit Button ──────────────────────────────────────────────────
        var exitBtn = CreateButton(canvasGO.transform, "ExitButton", "Thoát", ButtonRed,
            new Vector2(0.02f, 0.02f), new Vector2(0.12f, 0.10f));

        // ── Create Gene2 Panel ────────────────────────────────────────────
        var createPanel = BuildCreateGene2Panel(canvasGO.transform);
        createPanel.SetActive(false);

        // ── Wire SelectGeneController ─────────────────────────────────────
        controller.slot1UI         = slot1UI;
        controller.slot2UI         = slot2UI;
        controller.titleText       = titleText;
        controller.exitButton      = exitBtn.GetComponent<Button>();
        controller.loadingOverlay  = loadingOverlay;
        controller.loadingText     = loadingText;
        controller.errorText       = errorText;
        controller.createGene2Panel = createPanel;
        controller.createNameInput  = createPanel.GetComponentInChildren<TMP_InputField>();
        // Buttons are nested inside "ButtonRow" — use path or recursive helper
        controller.confirmCreateButton = createPanel.transform.Find("ButtonRow/ConfirmCreate")?.GetComponent<Button>();
        controller.cancelCreateButton  = createPanel.transform.Find("ButtonRow/CancelCreate")?.GetComponent<Button>();
        controller.createErrorText     = createPanel.transform.Find("CreateError")?.GetComponent<TMP_Text>();

        // ── Apply NotoSans font (supports Vietnamese) to all TMP_Text ────
        var notoSans = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NotoSansPath);
        if (notoSans != null)
        {
            foreach (var tmp in canvasGO.GetComponentsInChildren<TMP_Text>(true))
                tmp.font = notoSans;
        }
        else
        {
            Debug.LogWarning($"[SelectGeneSceneSetup] Không tìm thấy font tại: {NotoSansPath}");
        }

        // ── EventSystem: NOT created here — GameUIPersist already provides
        //    a DontDestroyOnLoad EventSystem from the Login/GameScene. ────

        // ── Save scene ───────────────────────────────────────────────────
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();

        // ── Add to Build Settings ─────────────────────────────────────────
        AddSceneToBuildSettings(ScenePath);

        EditorUtility.DisplayDialog(
            "SelectGene Scene",
            $"Scene đã được tạo tại:\n{ScenePath}\n\nĐã thêm vào Build Settings.\n\nHãy gán GeneSlotUI references (ExistingPanel, EmptyPanel...) trong Inspector nếu cần.",
            "OK");
    }

    // ─── Slot UI Builder ──────────────────────────────────────────────────
    private static GameObject BuildSlotUI(Transform parent, string name, int slotIndex)
    {
        var slotGO = CreateUIImage(parent, name, PanelColor);
        var slotUI = slotGO.AddComponent<GeneSlotUI>();
        slotUI.slotIndex = slotIndex;

        var layout = slotGO.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 12;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth  = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth  = true;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(16, 16, 16, 16);

        // Slot Title
        var slotTitle = CreateText(slotGO.transform, "SlotTitle",
            $"Hệ Gene {slotIndex}", fontSize: 36, color: AccentGold, bold: true);
        slotTitle.alignment = TextAlignmentOptions.Center;
        AddLayoutElement(slotTitle.gameObject, minHeight: 50, flexH: 0);
        slotUI.slotTitleText = slotTitle;

        // ExistingCharacterPanel
        var existingPanel = new GameObject("ExistingCharacterPanel");
        existingPanel.transform.SetParent(slotGO.transform, false);
        existingPanel.AddComponent<RectTransform>();
        var existVLayout = existingPanel.AddComponent<VerticalLayoutGroup>();
        existVLayout.spacing = 8;
        existVLayout.childAlignment = TextAnchor.UpperCenter;
        existVLayout.childControlWidth  = true;
        existVLayout.childControlHeight = false;
        existVLayout.childForceExpandWidth  = true;
        existVLayout.childForceExpandHeight = false;
        AddLayoutElement(existingPanel, minHeight: 300, flexH: 1);

        var nameText  = CreateText(existingPanel.transform, "CharacterNameText", "Tên nhân vật", 32, White, bold: true);
        var levelText = CreateText(existingPanel.transform, "LevelText",         "Cấp 1",        28, LightGray);
        var elemText  = CreateText(existingPanel.transform, "ElementText",       "Hỏa",          28, AccentGold);
        nameText.alignment  = TextAlignmentOptions.Center;
        levelText.alignment = TextAlignmentOptions.Center;
        elemText.alignment  = TextAlignmentOptions.Center;
        AddLayoutElement(nameText.gameObject,  minHeight: 40, flexH: 0);
        AddLayoutElement(levelText.gameObject, minHeight: 36, flexH: 0);
        AddLayoutElement(elemText.gameObject,  minHeight: 36, flexH: 0);

        var playBtn = CreateButton(existingPanel.transform, "PlayButton", "▶ Vào Game", ButtonGreen,
            Vector2.zero, Vector2.zero);
        AddLayoutElement(playBtn, minHeight: 60, flexH: 0);

        slotUI.existingCharacterPanel = existingPanel;
        slotUI.characterNameText      = nameText;
        slotUI.levelText              = levelText;
        slotUI.elementText            = elemText;
        slotUI.playButton             = playBtn.GetComponent<Button>();

        // EmptySlotPanel
        var emptyPanel = new GameObject("EmptySlotPanel");
        emptyPanel.transform.SetParent(slotGO.transform, false);
        emptyPanel.AddComponent<RectTransform>();
        var emptyVLayout = emptyPanel.AddComponent<VerticalLayoutGroup>();
        emptyVLayout.spacing = 12;
        emptyVLayout.childAlignment = TextAnchor.UpperCenter;
        emptyVLayout.childControlWidth  = true;
        emptyVLayout.childControlHeight = false;
        emptyVLayout.childForceExpandWidth  = true;
        emptyVLayout.childForceExpandHeight = false;
        AddLayoutElement(emptyPanel, minHeight: 300, flexH: 1);

        var emptyLabel = CreateText(emptyPanel.transform, "EmptyLabel", "Chưa có nhân vật", 28, LightGray);
        emptyLabel.alignment = TextAlignmentOptions.Center;
        AddLayoutElement(emptyLabel.gameObject, minHeight: 40, flexH: 0);

        if (slotIndex == 2)
        {
            var createBtn = CreateButton(emptyPanel.transform, "CreateCharacterButton", "+ Tạo nhân vật", ButtonOrange,
                Vector2.zero, Vector2.zero);
            AddLayoutElement(createBtn, minHeight: 60, flexH: 0);
            slotUI.createCharacterButton = createBtn.GetComponent<Button>();
        }
        slotUI.emptySlotPanel = emptyPanel;
        slotUI.emptySlotLabel = emptyLabel;

        // LockedPanel
        var lockedPanel = new GameObject("LockedPanel");
        lockedPanel.transform.SetParent(slotGO.transform, false);
        lockedPanel.AddComponent<RectTransform>();
        AddLayoutElement(lockedPanel, minHeight: 300, flexH: 1);

        var lockLabel = CreateText(lockedPanel.transform, "LockedLabel",
            "Chua mo khoa\nCan dat dieu kien mo\nhe gene 2", 26, new Color(0.7f, 0.7f, 0.7f));
        lockLabel.alignment = TextAlignmentOptions.Center;
        SetStretch(lockLabel.GetComponent<RectTransform>());
        slotUI.lockedPanel  = lockedPanel;
        slotUI.lockedLabel  = lockLabel;

        // Default visibility
        existingPanel.SetActive(false);
        emptyPanel.SetActive(slotIndex == 1);
        lockedPanel.SetActive(slotIndex == 2);

        return slotGO;
    }

    // ─── Create Gene2 Panel ────────────────────────────────────────────────
    private static GameObject BuildCreateGene2Panel(Transform parent)
    {
        var panel = CreateUIImage(parent, "CreateGene2Panel", new Color(0.12f, 0.08f, 0.03f, 0.97f));
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.25f, 0.3f);
        rect.anchorMax = new Vector2(0.75f, 0.75f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 16;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth  = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(24, 24, 24, 24);

        var panelTitle = CreateText(panel.transform, "PanelTitle", "Tạo Nhân Vật Hệ Gene 2",
            36, AccentGold, bold: true);
        panelTitle.alignment = TextAlignmentOptions.Center;
        AddLayoutElement(panelTitle.gameObject, minHeight: 50, flexH: 0);

        // Name Input
        var inputGO = new GameObject("NameInput");
        inputGO.transform.SetParent(panel.transform, false);
        inputGO.AddComponent<RectTransform>();
        AddLayoutElement(inputGO, minHeight: 52, flexH: 0);

        var inputBg = inputGO.AddComponent<Image>();
        inputBg.color = new Color(0.05f, 0.03f, 0.01f, 1f);

        var inputField = inputGO.AddComponent<TMP_InputField>();

        var textAreaGO = new GameObject("Text Area");
        textAreaGO.transform.SetParent(inputGO.transform, false);
        var taRect = textAreaGO.AddComponent<RectTransform>();
        taRect.anchorMin = Vector2.zero; taRect.anchorMax = Vector2.one;
        taRect.offsetMin = new Vector2(8, 4); taRect.offsetMax = new Vector2(-8, -4);
        textAreaGO.AddComponent<RectMask2D>();

        var inputTextGO = new GameObject("Text");
        inputTextGO.transform.SetParent(textAreaGO.transform, false);
        var inputText = inputTextGO.AddComponent<TextMeshProUGUI>();
        inputText.color = White;
        inputText.fontSize = 28;
        var inputTextRect = inputTextGO.GetComponent<RectTransform>();
        inputTextRect.anchorMin = Vector2.zero; inputTextRect.anchorMax = Vector2.one;
        inputTextRect.offsetMin = inputTextRect.offsetMax = Vector2.zero;

        var placeholderGO = new GameObject("Placeholder");
        placeholderGO.transform.SetParent(textAreaGO.transform, false);
        var placeholder = placeholderGO.AddComponent<TextMeshProUGUI>();
        placeholder.text = "Nhập tên nhân vật (3-20 ký tự)...";
        placeholder.color = new Color(0.6f, 0.6f, 0.6f);
        placeholder.fontSize = 28;
        placeholder.fontStyle = FontStyles.Italic;
        var phRect = placeholderGO.GetComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero; phRect.anchorMax = Vector2.one;
        phRect.offsetMin = phRect.offsetMax = Vector2.zero;

        inputField.textComponent     = inputText;
        inputField.placeholder        = placeholder;
        inputField.characterLimit     = 20;

        // Create Error Text
        var createErr = CreateText(panel.transform, "CreateError", "", 24, new Color(1f, 0.4f, 0.4f));
        createErr.alignment = TextAlignmentOptions.Center;
        createErr.gameObject.SetActive(false);
        AddLayoutElement(createErr.gameObject, minHeight: 36, flexH: 0);

        // Buttons Row
        var btnRow = new GameObject("ButtonRow");
        btnRow.transform.SetParent(panel.transform, false);
        btnRow.AddComponent<RectTransform>();
        var hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth  = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = false;
        AddLayoutElement(btnRow, minHeight: 60, flexH: 0);

        var cancelBtn = CreateButton(btnRow.transform, "CancelCreate", "Huỷ", ButtonRed, Vector2.zero, Vector2.zero);
        var confirmBtn = CreateButton(btnRow.transform, "ConfirmCreate", "Tạo nhân vật", ButtonGreen, Vector2.zero, Vector2.zero);

        return panel;
    }

    // ─── UI Helpers ────────────────────────────────────────────────────────

    private static GameObject CreateUIImage(Transform parent, string name, Color color)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var img  = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string text,
        int fontSize, Color color, bool bold = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text       = text;
        tmp.fontSize   = fontSize;
        tmp.color      = color;
        tmp.fontStyle  = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.enableWordWrapping = true;
        return tmp;
    }

    private static GameObject CreateButton(Transform parent, string name, string label,
        Color bgColor, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        if (anchorMin != Vector2.zero || anchorMax != Vector2.zero)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        var img  = go.AddComponent<Image>();
        img.color = bgColor;
        var btn  = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = bgColor * 1.2f;
        colors.pressedColor     = bgColor * 0.8f;
        btn.colors = colors;

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(go.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 28;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;

        return go;
    }

    private static void AddLayoutElement(GameObject go, float minHeight, float flexH)
    {
        var le = go.AddComponent<LayoutElement>();
        le.minHeight     = minHeight;
        le.flexibleHeight = flexH;
    }

    private static void AddLayoutElement(Transform t, float minHeight, float flexH)
        => AddLayoutElement(t.gameObject, minHeight, flexH);

    private static void SetStretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool alreadyAdded = scenes.Exists(s => s.path == scenePath);
        if (!alreadyAdded)
        {
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
