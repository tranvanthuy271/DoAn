#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tools → DoAn → Rebuild Register Scene UI
/// Dựng lại toàn bộ UI cho scene Register với phong cách giống Login.
/// </summary>
public static class RegisterSceneUiBuilder
{
    private const string RegisterScenePath = "Assets/Scenes/Register.unity";
    private const string BackgroundSpritePath = "Assets/Resources/Loading/bg.jpg";
    private const string WoodButtonSpritePath = "Assets/Resources/UINew/0a503eb3-84a7-4f23-bf73-c0e32d09bf60_rm_bg.png";

    private static readonly Color LabelColor        = new Color(1f,   0.93f, 0.46f, 1f);
    private static readonly Color InputColor        = new Color(0.72f, 0.34f, 0.08f, 0.96f);
    private static readonly Color InputOutlineColor = new Color(1f,   0.67f, 0.18f, 1f);
    private static readonly Color ButtonColor       = new Color(0.77f, 0.33f, 0.08f, 1f);
    private static readonly Color ButtonHighlight   = new Color(1f,   0.60f, 0.14f, 1f);
    private static readonly Color BrownDark         = new Color(0.36f, 0.15f, 0.05f, 1f);

    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("Tools/DoAn/Rebuild Register Scene UI")]
    public static void RebuildRegisterSceneUi()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.OpenScene(RegisterScenePath, OpenSceneMode.Single);

        DestroyRootByName("Canvas");
        DestroyRootByName("RegisterCanvas");
        DestroyRootByName("RegisterController");
        DestroyRootByName("APIClient");

        Sprite bgSprite     = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundSpritePath);
        Sprite btnSprite    = AssetDatabase.LoadAssetAtPath<Sprite>(WoodButtonSpritePath);

        REG_EnsureEventSystem();

        GameObject canvasGo = REG_CreateCanvas();
        REG_BuildBackground(canvasGo.transform, bgSprite);
        REG_BuildHeader(canvasGo.transform);

        GameObject ctrlGo = new GameObject("RegisterController");
        RegisterController controller = ctrlGo.AddComponent<RegisterController>();

        REG_BuildForm(
            canvasGo.transform, btnSprite,
            out TMP_InputField usernameInput,
            out TMP_InputField emailInput,
            out TMP_InputField passwordInput,
            out TMP_InputField confirmPasswordInput,
            out Button registerButton,
            out Button backButton,
            out TMP_Text errorText,
            out TMP_Text successText);

        // Direct-assign (all fields are public on RegisterController)
        controller.usernameInput        = usernameInput;
        controller.emailInput           = emailInput;
        controller.passwordInput        = passwordInput;
        controller.confirmPasswordInput = confirmPasswordInput;
        controller.registerButton       = registerButton;
        controller.backButton           = backButton;
        controller.errorText            = errorText;
        controller.successText          = successText;

        // Ensure APIClient exists
        if (Object.FindObjectOfType<APIClient>() == null)
            new GameObject("APIClient").AddComponent<APIClient>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Register UI",
            "Đã dựng lại scene Register với phong cách Login.\n\nMở Play Mode để kiểm tra.",
            "OK");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Canvas
    // ─────────────────────────────────────────────────────────────────────────
    private static GameObject REG_CreateCanvas()
    {
        var go = new GameObject("RegisterCanvas",
            typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        REG_SetUiLayer(go);

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = false;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode    = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;

        REG_Stretch(go.GetComponent<RectTransform>());
        return go;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Background + clouds
    // ─────────────────────────────────────────────────────────────────────────
    private static void REG_BuildBackground(Transform parent, Sprite bg)
    {
        var background = REG_MakeImage(parent, "Background", new Color(0.33f, 0.63f, 0.93f, 1f), bg);
        REG_Stretch(background.GetComponent<RectTransform>());
        if (bg != null)
            background.GetComponent<Image>().color = Color.white;

        REG_AddCloud(parent, "CloudsRightA",  new Vector2( 360f, -125f), new Vector2(360f, 120f), new Color(1f,1f,1f,0.42f));
        REG_AddCloud(parent, "CloudsRightB",  new Vector2( 460f, -250f), new Vector2(520f, 180f), new Color(1f,1f,1f,0.72f));
        REG_AddCloud(parent, "CloudsBottomA", new Vector2(-280f, -270f), new Vector2(520f, 155f), new Color(1f,1f,1f,0.40f));
        REG_AddCloud(parent, "CloudsBottomB", new Vector2(  90f, -285f), new Vector2(580f, 180f), new Color(1f,1f,1f,0.56f));
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Header – identical to Login
    // ─────────────────────────────────────────────────────────────────────────
    private static void REG_BuildHeader(Transform parent)
    {
        var title = REG_MakeText(parent, "TopLeftTitle", "Làng Lá Base", 18f, Color.white, FontStyles.Bold);
        REG_Anchor(title.rectTransform, new Vector2(0f,1f), new Vector2(0f,1f), new Vector2(220f,30f), new Vector2(16f,-16f));
        title.alignment = TextAlignmentOptions.Left;
        REG_AddShadow(title.gameObject, new Vector2(1.5f,-1.5f));

        var version = REG_MakeText(parent, "VersionText", "Phiên bản: 1.3.4", 18f, Color.white, FontStyles.Bold);
        REG_Anchor(version.rectTransform, new Vector2(1f,1f), new Vector2(1f,1f), new Vector2(230f,30f), new Vector2(-16f,-16f));
        version.alignment = TextAlignmentOptions.Right;
        REG_AddShadow(version.gameObject, new Vector2(1.5f,-1.5f));

        var logoRoot = new GameObject("LogoRoot", typeof(RectTransform));
        logoRoot.transform.SetParent(parent, false);
        REG_SetUiLayer(logoRoot);
        REG_Anchor(logoRoot.GetComponent<RectTransform>(), new Vector2(0.5f,1f), new Vector2(0.5f,1f), new Vector2(520f,150f), new Vector2(0f,-92f));

        var redEye = REG_MakeText(logoRoot.transform, "LogoLeftMark", "\u25c9", 42f, new Color(0.94f,0.04f,0.03f,1f), FontStyles.Bold);
        REG_Anchor(redEye.rectTransform, new Vector2(0.36f,0.75f), new Vector2(0.5f,0.5f), new Vector2(70f,60f), Vector2.zero);
        REG_AddShadow(redEye.gameObject, new Vector2(2f,-2f));

        var greyEye = REG_MakeText(logoRoot.transform, "LogoRightMark", "\u25c9", 42f, new Color(0.72f,0.72f,0.76f,1f), FontStyles.Bold);
        REG_Anchor(greyEye.rectTransform, new Vector2(0.64f,0.75f), new Vector2(0.5f,0.5f), new Vector2(70f,60f), Vector2.zero);
        REG_AddShadow(greyEye.gameObject, new Vector2(2f,-2f));

        var logo = REG_MakeText(logoRoot.transform, "LogoText", "LANG LA BASE", 54f, new Color(1f,0.76f,0.18f,1f), FontStyles.Bold);
        REG_Anchor(logo.rectTransform, new Vector2(0.5f,0.34f), new Vector2(0.5f,0.5f), new Vector2(520f,82f), Vector2.zero);
        REG_AddShadow(logo.gameObject, new Vector2(4f,-4f), new Color(0.23f,0.10f,0.02f,0.95f));
        REG_AddOutline(logo.gameObject, new Color(0.46f,0.19f,0.03f,1f), new Vector2(3f,-3f));
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Register form (4 inputs + 2 buttons + error/success)
    // ─────────────────────────────────────────────────────────────────────────
    private static void REG_BuildForm(
        Transform parent,
        Sprite btnSprite,
        out TMP_InputField usernameInput,
        out TMP_InputField emailInput,
        out TMP_InputField passwordInput,
        out TMP_InputField confirmPasswordInput,
        out Button registerButton,
        out Button backButton,
        out TMP_Text errorText,
        out TMP_Text successText)
    {
        var formRoot = new GameObject("RegisterForm", typeof(RectTransform));
        formRoot.transform.SetParent(parent, false);
        REG_SetUiLayer(formRoot);
        // Form is wider and taller than Login form (4 inputs)
        REG_Anchor(formRoot.GetComponent<RectTransform>(),
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            new Vector2(580f, 380f), new Vector2(0f, -70f));

        // ── Username ──────────────────────────────────────────────
        REG_MakeLabel(formRoot.transform, "UsernameLabel", "Tài khoản:", new Vector2(-192f, 148f));
        usernameInput = REG_CreateInput(formRoot.transform, "UsernameInput", "Nhập tên tài khoản", false, 12f);
        REG_Anchor(usernameInput.GetComponent<RectTransform>(),
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            new Vector2(295f,42f), new Vector2(70f,148f));

        // ── Email ─────────────────────────────────────────────────
        REG_MakeLabel(formRoot.transform, "EmailLabel", "Email:", new Vector2(-192f, 92f));
        emailInput = REG_CreateInput(formRoot.transform, "EmailInput", "Nhập địa chỉ email", false, 12f);
        REG_Anchor(emailInput.GetComponent<RectTransform>(),
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            new Vector2(295f,42f), new Vector2(70f,92f));

        // ── Password ──────────────────────────────────────────────
        REG_MakeLabel(formRoot.transform, "PasswordLabel", "Mật khẩu:", new Vector2(-192f, 36f));
        passwordInput = REG_CreateInput(formRoot.transform, "PasswordInput", "Nhập mật khẩu (≥ 6 ký tự)", true, 50f);
        REG_Anchor(passwordInput.GetComponent<RectTransform>(),
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            new Vector2(295f,42f), new Vector2(70f,36f));

        // toggle eye button on password
        var toggleGo = REG_MakeButton(passwordInput.transform, "TogglePasswordButton",
            "Hiện", new Color(0.22f,0.06f,0.02f,0.58f), Color.white, 14f, null);
        REG_Anchor(toggleGo.GetComponent<RectTransform>(),
            new Vector2(1f,0.5f), new Vector2(1f,0.5f),
            new Vector2(45f,34f), new Vector2(-25f,0f));

        // ── Confirm password ──────────────────────────────────────
        REG_MakeLabel(formRoot.transform, "ConfirmLabel", "Xác nhận:", new Vector2(-192f, -20f));
        confirmPasswordInput = REG_CreateInput(formRoot.transform, "ConfirmPasswordInput", "Nhập lại mật khẩu", true, 12f);
        REG_Anchor(confirmPasswordInput.GetComponent<RectTransform>(),
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            new Vector2(295f,42f), new Vector2(70f,-20f));

        // ── Back button (text-link style, left) ───────────────────
        var backGo = REG_MakeButton(formRoot.transform, "BackButton",
            "← Đăng nhập", new Color(0f,0f,0f,0f), Color.white, 18f, null);
        REG_Anchor(backGo.GetComponent<RectTransform>(),
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            new Vector2(180f,38f), new Vector2(-140f,-82f));
        REG_AddUnderline(backGo.transform);
        backButton = backGo.GetComponent<Button>();

        // ── Register button (primary) ─────────────────────────────
        var regGo = REG_MakeButton(formRoot.transform, "RegisterButton",
            "Đăng ký", ButtonColor, Color.white, 24f, btnSprite);
        REG_Anchor(regGo.GetComponent<RectTransform>(),
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            new Vector2(240f,58f), new Vector2(80f,-82f));
        registerButton = regGo.GetComponent<Button>();

        // ── Error text ────────────────────────────────────────────
        errorText = REG_MakeText(formRoot.transform, "ErrorText", string.Empty, 16f, Color.red, FontStyles.Bold);
        REG_Anchor(errorText.rectTransform,
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            new Vector2(540f,36f), new Vector2(0f,-138f));
        errorText.enableWordWrapping = true;
        errorText.alignment = TextAlignmentOptions.Center;

        // ── Success text ──────────────────────────────────────────
        successText = REG_MakeText(formRoot.transform, "SuccessText", string.Empty, 16f, new Color(0.2f,0.9f,0.3f,1f), FontStyles.Bold);
        REG_Anchor(successText.rectTransform,
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            new Vector2(540f,36f), new Vector2(0f,-164f));
        successText.enableWordWrapping = true;
        successText.alignment = TextAlignmentOptions.Center;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Shared helpers (prefix REG_ to avoid name clash in same assembly)
    // ─────────────────────────────────────────────────────────────────────────
    private static TMP_InputField REG_CreateInput(Transform parent, string name, string placeholder, bool password, float rightPad)
    {
        var go = REG_MakeImage(parent, name, InputColor, null);
        REG_AddOutline(go, InputOutlineColor, new Vector2(2f,-2f));

        var input = go.AddComponent<TMP_InputField>();
        input.targetGraphic = go.GetComponent<Image>();
        input.contentType   = password ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
        input.lineType       = TMP_InputField.LineType.SingleLine;
        input.caretColor     = Color.white;
        input.selectionColor = new Color(0.95f,0.76f,0.20f,0.45f);

        var textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(go.transform, false);
        REG_SetUiLayer(textArea);
        REG_StretchWithPadding(textArea.GetComponent<RectTransform>(), 12f,5f,rightPad,5f);

        var ph = REG_MakeText(textArea.transform, "Placeholder", placeholder, 16f, new Color(0.86f,0.78f,0.66f,0.84f), FontStyles.Bold);
        REG_Stretch(ph.rectTransform);
        ph.alignment = TextAlignmentOptions.Left;

        var inputText = REG_MakeText(textArea.transform, "Text", string.Empty, 17f, Color.white, FontStyles.Bold);
        REG_Stretch(inputText.rectTransform);
        inputText.alignment = TextAlignmentOptions.Left;

        input.textViewport  = textArea.GetComponent<RectTransform>();
        input.textComponent = inputText;
        input.placeholder   = ph;
        return input;
    }

    private static void REG_MakeLabel(Transform parent, string name, string text, Vector2 pos)
    {
        var label = REG_MakeText(parent, name, text, 18f, LabelColor, FontStyles.Bold);
        REG_Anchor(label.rectTransform, new Vector2(0.5f,0.5f), new Vector2(1f,0.5f), new Vector2(130f,36f), pos);
        label.alignment = TextAlignmentOptions.Right;
        REG_AddShadow(label.gameObject, new Vector2(1.5f,-1.5f), BrownDark);
    }

    private static GameObject REG_MakeButton(Transform parent, string name, string label, Color color, Color textColor, float fontSize, Sprite sprite)
    {
        var go     = REG_MakeImage(parent, name, color, sprite);
        var button = go.AddComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        var cb = button.colors;
        cb.normalColor      = color.a <= 0.01f ? new Color(1f,1f,1f,0f) : color;
        cb.highlightedColor = color.a <= 0.01f ? new Color(1f,1f,1f,0.18f) : ButtonHighlight;
        cb.pressedColor     = color.a <= 0.01f ? new Color(1f,1f,1f,0.28f) : new Color(0.50f,0.17f,0.04f,1f);
        cb.disabledColor    = new Color(0.35f,0.35f,0.35f,0.55f);
        button.colors = cb;
        var txt = REG_MakeText(go.transform, "Label", label, fontSize, textColor, FontStyles.Bold);
        REG_Stretch(txt.rectTransform);
        REG_AddShadow(txt.gameObject, new Vector2(1.5f,-1.5f), BrownDark);
        return go;
    }

    private static GameObject REG_MakeImage(Transform parent, string name, Color color, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        REG_SetUiLayer(go);
        var img = go.GetComponent<Image>();
        img.color  = color;
        img.sprite = sprite;
        img.type   = Image.Type.Simple;
        img.raycastTarget = true;
        return go;
    }

    private static TextMeshProUGUI REG_MakeText(Transform parent, string name, string text, float size, Color color, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        REG_SetUiLayer(go);
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

    private static void REG_AddCloud(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        var cloud = REG_MakeImage(parent, name, color, null);
        REG_Anchor(cloud.GetComponent<RectTransform>(), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), size, pos);
        cloud.GetComponent<Image>().raycastTarget = false;
    }

    private static void REG_AddUnderline(Transform parent)
    {
        var line = REG_MakeImage(parent, "Underline", Color.white, null);
        var rt   = line.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.08f, 0f);
        rt.anchorMax = new Vector2(0.92f, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = new Vector2(0f, 2f);
        line.GetComponent<Image>().raycastTarget = false;
    }

    private static void REG_AddShadow(GameObject go, Vector2 dist)            => REG_AddShadow(go, dist, new Color(0f,0f,0f,0.78f));
    private static void REG_AddShadow(GameObject go, Vector2 dist, Color col) { var s = go.AddComponent<Shadow>(); s.effectColor = col; s.effectDistance = dist; }
    private static void REG_AddOutline(GameObject go, Color col, Vector2 dist){ var o = go.AddComponent<Outline>(); o.effectColor = col; o.effectDistance = dist; }

    private static void REG_Anchor(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 size, Vector2 pos)
    {
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = pivot; rt.sizeDelta = size; rt.anchoredPosition = pos;
    }
    private static void REG_Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f,0.5f); rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
    private static void REG_StretchWithPadding(RectTransform rt, float l, float b, float r, float t)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = new Vector2(l,b); rt.offsetMax = new Vector2(-r,-t);
    }

    private static void REG_EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        REG_SetUiLayer(go);
    }

    private static void REG_SetUiLayer(GameObject go)
    {
        int layer = LayerMask.NameToLayer("UI");
        if (layer >= 0) go.layer = layer;
    }

    private static void DestroyRootByName(string n)
    {
        var t = GameObject.Find(n);
        if (t != null && t.transform.parent == null) Object.DestroyImmediate(t);
    }
}
#endif
