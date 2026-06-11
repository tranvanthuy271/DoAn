#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Tạo nhanh các prefab UI cho login:
// - LoadingPanel.prefab: spinner overlay mới, không còn progress %
// - ErrorNotifyPanel.prefab: giữ lại để tương thích scene cũ nếu cần
public static class LoginPanelPrefabCreator
{
    private const string PrefabDir = "Assets/Prefabs/UI";

    private static readonly Color OverlayColor = new Color(0f, 0f, 0f, 0.55f);
    private static readonly Color TitleColor = new Color(1f, 0.9f, 0.3f, 1f);
    private static readonly Color DialogBg = new Color(0.42f, 0.24f, 0.10f, 0.96f);
    private static readonly Color HeaderBg = new Color(0.30f, 0.16f, 0.06f, 1f);
    private static readonly Color InnerBg = new Color(0.23f, 0.13f, 0.05f, 0.98f);
    private static readonly Color ButtonBg = new Color(0.85f, 0.50f, 0.08f, 1f);
    private static readonly Color ButtonHighlight = new Color(1.00f, 0.68f, 0.18f, 1f);
    private static readonly Color CloseBg = new Color(0.65f, 0.10f, 0.10f, 1f);

    [MenuItem("Tools/DoAn/Create Login UI Prefabs")]
    public static void CreateAll()
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Prefabs/UI"));

        CreateLoadingPanel();
        CreateErrorNotifyPanel();

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog(
            "Done",
            "Đã tạo:\n- LoadingPanel.prefab (spinner overlay mới)\n- ErrorNotifyPanel.prefab (legacy)\n\n" +
            "Mở LoadingPanel.prefab để thay art/spinner nếu muốn.",
            "OK");
    }

    private static void CreateLoadingPanel()
    {
        GameObject root = new GameObject("LoadingPanel", typeof(RectTransform), typeof(LoadingOverlayView));
        Stretch(root);

        GameObject overlay = MakePanel(root.transform, "Overlay", OverlayColor);
        Stretch(overlay);

        GameObject spinnerRoot = new GameObject("SpinnerRoot", typeof(RectTransform));
        spinnerRoot.transform.SetParent(root.transform, false);
        SetUiLayer(spinnerRoot);
        RectTransform spinnerRootRt = spinnerRoot.GetComponent<RectTransform>();
        spinnerRootRt.anchorMin = new Vector2(0.5f, 0.5f);
        spinnerRootRt.anchorMax = new Vector2(0.5f, 0.5f);
        spinnerRootRt.pivot = new Vector2(0.5f, 0.5f);
        spinnerRootRt.anchoredPosition = new Vector2(0f, 40f);
        spinnerRootRt.sizeDelta = new Vector2(180f, 180f);

        GameObject spinnerImage = new GameObject("SpinnerImage", typeof(RectTransform), typeof(Image), typeof(LoadingSpinnerAnimator));
        spinnerImage.transform.SetParent(spinnerRoot.transform, false);
        SetUiLayer(spinnerImage);
        RectTransform spinnerRt = spinnerImage.GetComponent<RectTransform>();
        spinnerRt.anchorMin = new Vector2(0.5f, 0.5f);
        spinnerRt.anchorMax = new Vector2(0.5f, 0.5f);
        spinnerRt.pivot = new Vector2(0.5f, 0.5f);
        spinnerRt.anchoredPosition = Vector2.zero;
        spinnerRt.sizeDelta = new Vector2(150f, 150f);

        Image spinner = spinnerImage.GetComponent<Image>();
        Sprite[] frames = Resources.LoadAll<Sprite>("Loading");
        if (frames != null && frames.Length > 0)
        {
            spinner.sprite = frames[0];
            spinner.preserveAspect = true;
        }
        spinner.color = Color.white;

        GameObject statusGo = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        statusGo.transform.SetParent(root.transform, false);
        SetUiLayer(statusGo);
        RectTransform statusRt = statusGo.GetComponent<RectTransform>();
        statusRt.anchorMin = new Vector2(0.5f, 0.5f);
        statusRt.anchorMax = new Vector2(0.5f, 0.5f);
        statusRt.pivot = new Vector2(0.5f, 0.5f);
        statusRt.anchoredPosition = new Vector2(0f, -75f);
        statusRt.sizeDelta = new Vector2(720f, 70f);

        TextMeshProUGUI statusText = statusGo.GetComponent<TextMeshProUGUI>();
        statusText.text = "Đang tải...";
        statusText.fontSize = 24f;
        statusText.color = Color.white;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.enableWordWrapping = true;
        statusText.raycastTarget = false;

        UIRuntimeAssetHelper.ApplyNotoSans(statusText);
        root.GetComponent<LoadingOverlayView>().ResolveReferences();

        SavePrefab(root, "LoadingPanel");
    }

    private static void CreateErrorNotifyPanel()
    {
        GameObject root = new GameObject("ErrorNotifyPanel");
        root.AddComponent<RectTransform>();
        root.AddComponent<CanvasRenderer>();
        Stretch(root);

        GameObject overlay = MakePanel(root.transform, "Overlay", OverlayColor);
        Stretch(overlay);

        GameObject dialog = MakePanel(root.transform, "DialogBox", DialogBg);
        RectTransform dialogRt = dialog.GetComponent<RectTransform>();
        dialogRt.anchorMin = dialogRt.anchorMax = new Vector2(0.5f, 0.5f);
        dialogRt.sizeDelta = new Vector2(480, 290);
        dialogRt.anchoredPosition = Vector2.zero;

        GameObject closeBtn = MakeButton(dialog.transform, "CloseButton", "✕", CloseBg, Color.white, 18);
        RectTransform closeRt = closeBtn.GetComponent<RectTransform>();
        closeRt.anchorMin = closeRt.anchorMax = new Vector2(1f, 1f);
        closeRt.pivot = new Vector2(1f, 1f);
        closeRt.sizeDelta = new Vector2(40f, 40f);
        closeRt.anchoredPosition = new Vector2(6f, 6f);

        GameObject header = MakePanel(dialog.transform, "HeaderBg", HeaderBg);
        RectTransform headerRt = header.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0f, 1f);
        headerRt.anchorMax = new Vector2(1f, 1f);
        headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.sizeDelta = new Vector2(0f, 54f);
        headerRt.anchoredPosition = Vector2.zero;
        StretchText(header.transform, "TitleText", "Nhắc nhở", TitleColor, 26f, FontStyles.Bold);

        GameObject inner = MakePanel(dialog.transform, "ContentArea", InnerBg);
        RectTransform innerRt = inner.GetComponent<RectTransform>();
        innerRt.anchorMin = new Vector2(0.06f, 0.24f);
        innerRt.anchorMax = new Vector2(0.94f, 0.76f);
        innerRt.offsetMin = Vector2.zero;
        innerRt.offsetMax = Vector2.zero;

        GameObject messageGo = StretchText(
            inner.transform,
            "MessageText",
            "Không thể kết nối đến máy chủ.",
            Color.white,
            17f,
            FontStyles.Normal);
        TextMeshProUGUI messageText = messageGo.GetComponent<TextMeshProUGUI>();
        messageText.enableWordWrapping = true;

        GameObject confirmBtn = MakeButton(dialog.transform, "ConfirmButton", "Xác nhận", ButtonBg, Color.white, 20f, FontStyles.Bold);
        RectTransform confirmRt = confirmBtn.GetComponent<RectTransform>();
        confirmRt.anchorMin = new Vector2(0.5f, 0f);
        confirmRt.anchorMax = new Vector2(0.5f, 0f);
        confirmRt.pivot = new Vector2(0.5f, 0f);
        confirmRt.sizeDelta = new Vector2(180f, 46f);
        confirmRt.anchoredPosition = new Vector2(0f, 14f);

        ColorBlock colors = confirmBtn.GetComponent<Button>().colors;
        colors.normalColor = ButtonBg;
        colors.highlightedColor = ButtonHighlight;
        colors.pressedColor = new Color(0.60f, 0.35f, 0.04f);
        confirmBtn.GetComponent<Button>().colors = colors;

        SavePrefab(root, "ErrorNotifyPanel");
    }

    private static GameObject MakePanel(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = color;
        SetUiLayer(go);
        return go;
    }

    private static GameObject MakeButton(
        Transform parent,
        string name,
        string label,
        Color bgColor,
        Color textColor,
        float fontSize,
        FontStyles fontStyle = FontStyles.Normal)
    {
        GameObject go = MakePanel(parent, name, bgColor);
        go.AddComponent<Button>();

        GameObject textGo = StretchText(go.transform, "Label", label, textColor, fontSize, fontStyle);
        UIRuntimeAssetHelper.ApplyNotoSans(textGo.GetComponent<TextMeshProUGUI>());
        return go;
    }

    private static GameObject StretchText(
        Transform parent,
        string name,
        string text,
        Color color,
        float fontSize,
        FontStyles fontStyle)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = color;
        tmp.fontSize = fontSize;
        tmp.fontStyle = fontStyle;
        tmp.alignment = TextAlignmentOptions.Center;
        go.layer = LayerMask.NameToLayer("UI");
        Stretch(go);
        return go;
    }

    private static void Stretch(GameObject go)
    {
        RectTransform rectTransform = go.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = go.AddComponent<RectTransform>();
        }

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static void SetUiLayer(GameObject go)
    {
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer >= 0)
        {
            go.layer = uiLayer;
        }
    }

    private static void SavePrefab(GameObject root, string prefabName)
    {
        string path = $"{PrefabDir}/{prefabName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        { /* Saved {path} */ }
    }
}
#endif
