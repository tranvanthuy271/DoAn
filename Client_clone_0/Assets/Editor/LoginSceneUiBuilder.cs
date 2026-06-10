#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class LoginSceneUiBuilder
{
    private const string LoginScenePath = "Assets/Scenes/Login.unity";
    private const string BackgroundSpritePath = "Assets/Resources/Loading/bg.jpg";
    private const string WoodPanelSpritePath = "Assets/Resources/UINew/a662941f-2e34-4ff5-8bdc-14068425bbdb-removebg-preview.png";
    private const string WoodButtonSpritePath = "Assets/Resources/UINew/0a503eb3-84a7-4f23-bf73-c0e32d09bf60_rm_bg.png";
    private const string LoadingPanelPrefabPath = "Assets/Prefabs/UI/LoadingPanel.prefab";

    private static readonly Color LabelColor = new Color(1f, 0.93f, 0.46f, 1f);
    private static readonly Color InputColor = new Color(0.72f, 0.34f, 0.08f, 0.96f);
    private static readonly Color InputOutlineColor = new Color(1f, 0.67f, 0.18f, 1f);
    private static readonly Color ButtonColor = new Color(0.77f, 0.33f, 0.08f, 1f);
    private static readonly Color ButtonHighlightColor = new Color(1f, 0.60f, 0.14f, 1f);
    private static readonly Color BrownDark = new Color(0.36f, 0.15f, 0.05f, 1f);

    [MenuItem("Tools/DoAn/Rebuild Login Scene UI")]
    public static void RebuildLoginSceneUi()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(LoginScenePath, OpenSceneMode.Single);
        DestroyRootByName("Canvas");
        DestroyRootByName("LoginCanvas");
        DestroyRootByName("LoginController");
        DestroyRootByName("LoginLoadingManager");

        Sprite backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundSpritePath);
        Sprite woodPanelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(WoodPanelSpritePath);
        Sprite woodButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(WoodButtonSpritePath);

        EnsureEventSystem();

        GameObject canvasGo = CreateCanvas();
        BuildBackground(canvasGo.transform, backgroundSprite);
        BuildHeader(canvasGo.transform);

        GameObject controllerGo = new GameObject("LoginController");
        LoginController controller = controllerGo.AddComponent<LoginController>();

        BuildMainLoginUi(
            canvasGo.transform,
            woodButtonSprite,
            out TMP_InputField usernameInput,
            out TMP_InputField passwordInput,
            out Button loginButton,
            out Button registerButton,
            out Button togglePasswordButton,
            out TMP_Text togglePasswordLabel,
            out Button accountListButton,
            out TMP_Text errorText);

        BuildSavedAccountPopup(
            canvasGo.transform,
            woodPanelSprite,
            out GameObject accountPanel,
            out Button closeAccountPanelButton,
            out Transform accountContent,
            out GameObject accountRowTemplate,
            out TMP_Text emptyAccountText);

        AssignLoginController(
            controller,
            usernameInput,
            passwordInput,
            loginButton,
            registerButton,
            togglePasswordButton,
            togglePasswordLabel,
            accountListButton,
            accountPanel,
            closeAccountPanelButton,
            accountContent,
            accountRowTemplate,
            emptyAccountText,
            errorText);

        EnsureApiClient();
        EnsureLoadingManager();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Login UI",
            "Da dung lai scene Login.\n\nVao Play Mode de test: login thu mot tai khoan, sau do bam nut Tai khoan de mo danh sach da luu.",
            "OK");
    }

    private static GameObject CreateCanvas()
    {
        GameObject canvasGo = new GameObject("LoginCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SetUiLayer(canvasGo);

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = false;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Stretch(canvasGo.GetComponent<RectTransform>());
        return canvasGo;
    }

    private static void BuildBackground(Transform parent, Sprite backgroundSprite)
    {
        GameObject background = MakeImage(parent, "Background", new Color(0.33f, 0.63f, 0.93f, 1f), backgroundSprite);
        Stretch(background.GetComponent<RectTransform>());

        if (backgroundSprite != null)
        {
            Image image = background.GetComponent<Image>();
            image.color = Color.white;
            image.preserveAspect = false;
        }

        AddCloud(parent, "CloudsRightA", new Vector2(360f, -125f), new Vector2(360f, 120f), new Color(1f, 1f, 1f, 0.42f));
        AddCloud(parent, "CloudsRightB", new Vector2(460f, -250f), new Vector2(520f, 180f), new Color(1f, 1f, 1f, 0.72f));
        AddCloud(parent, "CloudsBottomA", new Vector2(-280f, -270f), new Vector2(520f, 155f), new Color(1f, 1f, 1f, 0.40f));
        AddCloud(parent, "CloudsBottomB", new Vector2(90f, -285f), new Vector2(580f, 180f), new Color(1f, 1f, 1f, 0.56f));
    }

    private static void BuildHeader(Transform parent)
    {
        TextMeshProUGUI title = MakeText(parent, "TopLeftTitle", "L\u00e0ng L\u00e1 Base", 18f, Color.white, FontStyles.Bold);
        Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(220f, 30f), new Vector2(16f, -16f));
        title.alignment = TextAlignmentOptions.Left;
        AddShadow(title.gameObject, new Vector2(1.5f, -1.5f));

        TextMeshProUGUI version = MakeText(parent, "VersionText", "Phi\u00ean b\u1ea3n: 1.3.4", 18f, Color.white, FontStyles.Bold);
        Anchor(version.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(230f, 30f), new Vector2(-16f, -16f));
        version.alignment = TextAlignmentOptions.Right;
        AddShadow(version.gameObject, new Vector2(1.5f, -1.5f));

        GameObject logoRoot = new GameObject("LogoRoot", typeof(RectTransform));
        logoRoot.transform.SetParent(parent, false);
        SetUiLayer(logoRoot);
        Anchor(logoRoot.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(520f, 150f), new Vector2(0f, -92f));

        TextMeshProUGUI redEye = MakeText(logoRoot.transform, "LogoLeftMark", "\u25c9", 42f, new Color(0.94f, 0.04f, 0.03f, 1f), FontStyles.Bold);
        Anchor(redEye.rectTransform, new Vector2(0.36f, 0.75f), new Vector2(0.5f, 0.5f), new Vector2(70f, 60f), Vector2.zero);
        AddShadow(redEye.gameObject, new Vector2(2f, -2f));

        TextMeshProUGUI greyEye = MakeText(logoRoot.transform, "LogoRightMark", "\u25c9", 42f, new Color(0.72f, 0.72f, 0.76f, 1f), FontStyles.Bold);
        Anchor(greyEye.rectTransform, new Vector2(0.64f, 0.75f), new Vector2(0.5f, 0.5f), new Vector2(70f, 60f), Vector2.zero);
        AddShadow(greyEye.gameObject, new Vector2(2f, -2f));

        TextMeshProUGUI logo = MakeText(logoRoot.transform, "LogoText", "LANG LA BASE", 54f, new Color(1f, 0.76f, 0.18f, 1f), FontStyles.Bold);
        Anchor(logo.rectTransform, new Vector2(0.5f, 0.34f), new Vector2(0.5f, 0.5f), new Vector2(520f, 82f), Vector2.zero);
        AddShadow(logo.gameObject, new Vector2(4f, -4f), new Color(0.23f, 0.10f, 0.02f, 0.95f));
        AddOutline(logo.gameObject, new Color(0.46f, 0.19f, 0.03f, 1f), new Vector2(3f, -3f));
    }

    private static void BuildMainLoginUi(
        Transform parent,
        Sprite woodButtonSprite,
        out TMP_InputField usernameInput,
        out TMP_InputField passwordInput,
        out Button loginButton,
        out Button registerButton,
        out Button togglePasswordButton,
        out TMP_Text togglePasswordLabel,
        out Button accountListButton,
        out TMP_Text errorText)
    {
        GameObject formRoot = new GameObject("LoginForm", typeof(RectTransform));
        formRoot.transform.SetParent(parent, false);
        SetUiLayer(formRoot);
        Anchor(formRoot.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(560f, 300f), new Vector2(0f, -78f));

        MakeLabel(formRoot.transform, "ServerLabel", "M\u00e1y ch\u1ee7:", new Vector2(-185f, 100f));
        GameObject serverBox = MakeImage(formRoot.transform, "ServerNameBox", InputColor, null);
        Anchor(serverBox.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(282f, 42f), new Vector2(60f, 100f));
        AddOutline(serverBox, InputOutlineColor, new Vector2(2f, -2f));
        TextMeshProUGUI serverText = MakeText(serverBox.transform, "ServerNameText", "L\u00e0ng L\u00e1 Base (T\u1ed1t)", 18f, Color.white, FontStyles.Bold);
        StretchWithPadding(serverText.rectTransform, 14f, 5f, 42f, 5f);
        serverText.alignment = TextAlignmentOptions.Left;
        TextMeshProUGUI serverArrow = MakeText(serverBox.transform, "ServerArrow", "\u25be", 23f, new Color(1f, 0.79f, 0.33f, 1f), FontStyles.Bold);
        Anchor(serverArrow.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(42f, 34f), new Vector2(-22f, 0f));

        MakeLabel(formRoot.transform, "UsernameLabel", "T\u00e0i kho\u1ea3n:", new Vector2(-185f, 48f));
        usernameInput = CreateInput(formRoot.transform, "UsernameInput", "Nh\u1eadp Email/S\u1ed1 \u0111i\u1ec7n tho\u1ea1i", false, 12f);
        Anchor(usernameInput.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(282f, 42f), new Vector2(60f, 48f));

        MakeLabel(formRoot.transform, "PasswordLabel", "M\u1eadt kh\u1ea9u:", new Vector2(-185f, -4f));
        passwordInput = CreateInput(formRoot.transform, "PasswordInput", "Nh\u1eadp m\u1eadt kh\u1ea9u", true, 50f);
        Anchor(passwordInput.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(282f, 42f), new Vector2(60f, -4f));

        GameObject toggleGo = MakeButton(passwordInput.transform, "TogglePasswordButton", "Hi\u1ec7n", new Color(0.22f, 0.06f, 0.02f, 0.58f), Color.white, 14f, null);
        Anchor(toggleGo.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(45f, 34f), new Vector2(-25f, 0f));
        togglePasswordButton = toggleGo.GetComponent<Button>();
        togglePasswordLabel = toggleGo.GetComponentInChildren<TMP_Text>(true);

        accountListButton = MakeButton(formRoot.transform, "AccountListButton", "T\u00e0i kho\u1ea3n", new Color(0f, 0f, 0f, 0f), Color.white, 18f, null).GetComponent<Button>();
        Anchor(accountListButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(150f, 38f), new Vector2(-110f, -70f));
        AddUnderline(accountListButton.transform);

        registerButton = MakeButton(formRoot.transform, "RegisterButton", "\u0110\u0103ng k\u00fd", new Color(0f, 0f, 0f, 0f), Color.white, 18f, null).GetComponent<Button>();
        Anchor(registerButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(150f, 38f), new Vector2(110f, -70f));
        AddUnderline(registerButton.transform);

        loginButton = MakeButton(formRoot.transform, "LoginButton", "V\u00e0o Game", ButtonColor, Color.white, 24f, woodButtonSprite).GetComponent<Button>();
        Anchor(loginButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(230f, 58f), new Vector2(0f, -124f));

        errorText = MakeText(formRoot.transform, "ErrorText", string.Empty, 16f, Color.red, FontStyles.Bold);
        Anchor(errorText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(520f, 40f), new Vector2(0f, -176f));
        errorText.enableWordWrapping = true;
    }

    private static void BuildSavedAccountPopup(
        Transform parent,
        Sprite woodPanelSprite,
        out GameObject accountPanel,
        out Button closeButton,
        out Transform content,
        out GameObject rowTemplate,
        out TMP_Text emptyText)
    {
        accountPanel = new GameObject("SavedAccountPanel", typeof(RectTransform));
        accountPanel.transform.SetParent(parent, false);
        SetUiLayer(accountPanel);
        Stretch(accountPanel.GetComponent<RectTransform>());

        GameObject dim = MakeImage(accountPanel.transform, "Dim", new Color(0f, 0f, 0f, 0.18f), null);
        Stretch(dim.GetComponent<RectTransform>());

        GameObject window = MakeImage(accountPanel.transform, "Window", new Color(0.67f, 0.34f, 0.11f, 0.98f), woodPanelSprite);
        Anchor(window.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(456f, 500f), new Vector2(0f, 0f));
        if (woodPanelSprite != null)
        {
            window.GetComponent<Image>().color = Color.white;
        }

        TextMeshProUGUI title = MakeText(window.transform, "TitleText", "T\u00e0i kho\u1ea3n", 24f, new Color(1f, 0.95f, 0.20f, 1f), FontStyles.Bold);
        Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(280f, 46f), new Vector2(0f, -28f));
        AddShadow(title.gameObject, new Vector2(2f, -2f), BrownDark);

        GameObject closeGo = MakeButton(window.transform, "CloseButton", "X", new Color(0.70f, 0.20f, 0.06f, 1f), Color.white, 20f, null);
        Anchor(closeGo.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(44f, 44f), new Vector2(-16f, -16f));
        closeButton = closeGo.GetComponent<Button>();

        GameObject scrollGo = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
        scrollGo.transform.SetParent(window.transform, false);
        SetUiLayer(scrollGo);
        Anchor(scrollGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(402f, 365f), new Vector2(0f, -38f));

        GameObject viewport = MakeImage(scrollGo.transform, "Viewport", new Color(1f, 1f, 1f, 0.02f), null);
        Stretch(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        GameObject contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGo.transform.SetParent(viewport.transform, false);
        SetUiLayer(contentGo);
        RectTransform contentRt = contentGo.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.offsetMin = new Vector2(8f, 0f);
        contentRt.offsetMax = new Vector2(-8f, 0f);
        contentRt.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup layout = contentGo.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 8, 8);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentGo.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = scrollGo.GetComponent<ScrollRect>();
        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content = contentRt;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        emptyText = MakeText(window.transform, "EmptyAccountText", "Ch\u01b0a c\u00f3 t\u00e0i kho\u1ea3n \u0111\u00e3 l\u01b0u", 18f, Color.white, FontStyles.Bold);
        Anchor(emptyText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(380f, 50f), new Vector2(0f, -40f));
        AddShadow(emptyText.gameObject, new Vector2(1.5f, -1.5f), BrownDark);

        rowTemplate = BuildSavedAccountRowTemplate(contentGo.transform);
        rowTemplate.SetActive(false);
        content = contentGo.transform;
        accountPanel.SetActive(false);
    }

    private static GameObject BuildSavedAccountRowTemplate(Transform parent)
    {
        GameObject row = MakeImage(parent, "SavedAccountRowTemplate", new Color(0.90f, 0.55f, 0.21f, 0.55f), null);
        row.AddComponent<Button>();
        row.AddComponent<LoginSavedAccountRow>();

        LayoutElement layout = row.AddComponent<LayoutElement>();
        layout.preferredHeight = 50f;
        layout.minHeight = 50f;

        RectTransform rowRt = row.GetComponent<RectTransform>();
        rowRt.sizeDelta = new Vector2(386f, 50f);
        AddOutline(row, new Color(1f, 0.83f, 0.40f, 1f), new Vector2(1.5f, -1.5f));

        TextMeshProUGUI username = MakeText(row.transform, "UsernameText", "username", 18f, Color.white, FontStyles.Bold);
        StretchWithPadding(username.rectTransform, 16f, 4f, 90f, 4f);
        username.alignment = TextAlignmentOptions.Left;
        AddShadow(username.gameObject, new Vector2(1.5f, -1.5f), BrownDark);

        GameObject deleteGo = MakeButton(row.transform, "DeleteButton", "X\u00f3a", new Color(0.74f, 0.22f, 0.07f, 1f), Color.white, 17f, null);
        Anchor(deleteGo.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(62f, 36f), new Vector2(-38f, 0f));

        LoginSavedAccountRow rowUi = row.GetComponent<LoginSavedAccountRow>();
        SerializedObject so = new SerializedObject(rowUi);
        SetObjectRef(so, "usernameText", username);
        SetObjectRef(so, "selectButton", row.GetComponent<Button>());
        SetObjectRef(so, "deleteButton", deleteGo.GetComponent<Button>());
        so.ApplyModifiedPropertiesWithoutUndo();

        return row;
    }

    private static TMP_InputField CreateInput(Transform parent, string name, string placeholder, bool password, float rightPadding)
    {
        GameObject go = MakeImage(parent, name, InputColor, null);
        AddOutline(go, InputOutlineColor, new Vector2(2f, -2f));

        TMP_InputField input = go.AddComponent<TMP_InputField>();
        input.targetGraphic = go.GetComponent<Image>();
        input.contentType = password ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.caretColor = Color.white;
        input.selectionColor = new Color(0.95f, 0.76f, 0.20f, 0.45f);

        GameObject textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(go.transform, false);
        SetUiLayer(textArea);
        StretchWithPadding(textArea.GetComponent<RectTransform>(), 12f, 5f, rightPadding, 5f);

        TextMeshProUGUI placeholderText = MakeText(textArea.transform, "Placeholder", placeholder, 16f, new Color(0.86f, 0.78f, 0.66f, 0.84f), FontStyles.Bold);
        Stretch(placeholderText.rectTransform);
        placeholderText.alignment = TextAlignmentOptions.Left;

        TextMeshProUGUI inputText = MakeText(textArea.transform, "Text", string.Empty, 17f, Color.white, FontStyles.Bold);
        Stretch(inputText.rectTransform);
        inputText.alignment = TextAlignmentOptions.Left;

        input.textViewport = textArea.GetComponent<RectTransform>();
        input.textComponent = inputText;
        input.placeholder = placeholderText;
        return input;
    }

    private static void MakeLabel(Transform parent, string name, string text, Vector2 position)
    {
        TextMeshProUGUI label = MakeText(parent, name, text, 18f, LabelColor, FontStyles.Bold);
        Anchor(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1f, 0.5f), new Vector2(130f, 36f), position);
        label.alignment = TextAlignmentOptions.Right;
        AddShadow(label.gameObject, new Vector2(1.5f, -1.5f), BrownDark);
    }

    private static GameObject MakeButton(Transform parent, string name, string label, Color color, Color textColor, float fontSize, Sprite sprite)
    {
        GameObject go = MakeImage(parent, name, color, sprite);
        Button button = go.AddComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        ColorBlock colors = button.colors;
        colors.normalColor = color.a <= 0.01f ? new Color(1f, 1f, 1f, 0f) : color;
        colors.highlightedColor = color.a <= 0.01f ? new Color(1f, 1f, 1f, 0.18f) : ButtonHighlightColor;
        colors.pressedColor = color.a <= 0.01f ? new Color(1f, 1f, 1f, 0.28f) : new Color(0.50f, 0.17f, 0.04f, 1f);
        colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.55f);
        button.colors = colors;

        TextMeshProUGUI text = MakeText(go.transform, "Label", label, fontSize, textColor, FontStyles.Bold);
        Stretch(text.rectTransform);
        AddShadow(text.gameObject, new Vector2(1.5f, -1.5f), BrownDark);
        return go;
    }

    private static GameObject MakeImage(Transform parent, string name, Color color, Sprite sprite)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        SetUiLayer(go);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.raycastTarget = true;
        return go;
    }

    private static TextMeshProUGUI MakeText(Transform parent, string name, string text, float fontSize, Color color, FontStyles style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        SetUiLayer(go);

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        UIRuntimeAssetHelper.ApplyNotoSans(tmp);
        return tmp;
    }

    private static void AddCloud(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        GameObject cloud = MakeImage(parent, name, color, null);
        Anchor(cloud.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size, position);
        cloud.GetComponent<Image>().raycastTarget = false;
    }

    private static void AddUnderline(Transform parent)
    {
        GameObject line = MakeImage(parent, "Underline", Color.white, null);
        RectTransform rt = line.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.08f, 0f);
        rt.anchorMax = new Vector2(0.92f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.offsetMin = new Vector2(0f, 0f);
        rt.offsetMax = new Vector2(0f, 2f);
        line.GetComponent<Image>().raycastTarget = false;
    }

    private static void AddShadow(GameObject go, Vector2 distance)
    {
        AddShadow(go, distance, new Color(0f, 0f, 0f, 0.78f));
    }

    private static void AddShadow(GameObject go, Vector2 distance, Color color)
    {
        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
    }

    private static void AddOutline(GameObject go, Color color, Vector2 distance)
    {
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
    }

    private static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 size, Vector2 anchoredPosition)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPosition;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void StretchWithPadding(RectTransform rt, float left, float bottom, float right, float top)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    private static void AssignLoginController(
        LoginController controller,
        TMP_InputField usernameInput,
        TMP_InputField passwordInput,
        Button loginButton,
        Button registerButton,
        Button togglePasswordButton,
        TMP_Text togglePasswordLabel,
        Button accountListButton,
        GameObject accountPanel,
        Button closeAccountPanelButton,
        Transform accountContent,
        GameObject accountRowTemplate,
        TMP_Text emptyAccountText,
        TMP_Text errorText)
    {
        SerializedObject so = new SerializedObject(controller);
        SetObjectRef(so, "usernameInput", usernameInput);
        SetObjectRef(so, "passwordInput", passwordInput);
        SetObjectRef(so, "loginButton", loginButton);
        SetObjectRef(so, "registerButton", registerButton);
        SetObjectRef(so, "togglePasswordButton", togglePasswordButton);
        SetObjectRef(so, "togglePasswordLabel", togglePasswordLabel);
        SetObjectRef(so, "accountListButton", accountListButton);
        SetObjectRef(so, "accountListPanel", accountPanel);
        SetObjectRef(so, "closeAccountListButton", closeAccountPanelButton);
        SetObjectRef(so, "savedAccountContent", accountContent);
        SetObjectRef(so, "savedAccountRowPrefab", accountRowTemplate);
        SetObjectRef(so, "emptySavedAccountText", emptyAccountText);
        SetObjectRef(so, "errorText", errorText);

        SerializedProperty autoLogin = so.FindProperty("autoLoginSavedAccount");
        if (autoLogin != null)
        {
            autoLogin.boolValue = false;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureApiClient()
    {
        if (Object.FindObjectOfType<APIClient>() != null)
        {
            return;
        }

        new GameObject("APIClient").AddComponent<APIClient>();
    }

    private static void EnsureLoadingManager()
    {
        LoginLoadingManager manager = Object.FindObjectOfType<LoginLoadingManager>();
        if (manager == null)
        {
            manager = new GameObject("LoginLoadingManager").AddComponent<LoginLoadingManager>();
        }

        GameObject loadingPanelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LoadingPanelPrefabPath);
        if (loadingPanelPrefab == null)
        {
            return;
        }

        SerializedObject so = new SerializedObject(manager);
        SetObjectRef(so, "loadingPanelPrefab", loadingPanelPrefab);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        SetUiLayer(eventSystem);
    }

    private static void DestroyRootByName(string rootName)
    {
        GameObject target = GameObject.Find(rootName);
        if (target != null && target.transform.parent == null)
        {
            Object.DestroyImmediate(target);
        }
    }

    private static void SetObjectRef(SerializedObject so, string propertyName, Object value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetUiLayer(GameObject go)
    {
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer >= 0)
        {
            go.layer = uiLayer;
        }
    }
}
#endif
