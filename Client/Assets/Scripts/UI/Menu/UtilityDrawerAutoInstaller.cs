using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class UtilityDrawerAutoInstaller
{
    private static readonly string[] UtilityLabels =
    {
        "Qua Tang",
        "Kho Bau",
        "Phuc Loi",
        "Thu",
        "Hoat Dong",
        "VXMM",
        "Uu Dai",
        "BXH",
        "Cho",
        "Shop"
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TryInstall(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryInstall(scene);
    }

    private static void TryInstall(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        RectTransform preferredParent = FindRectTransform(scene, "HUD/Btn");
        RectTransform fallbackParent = FindRectTransform(scene, "HUD");
        RectTransform parent = preferredParent != null ? preferredParent : fallbackParent;
        if (parent == null)
            return;

        if (FindDirectChild(parent, "UtilityRoot") != null)
            return;

        BuildRuntimeUtilityMenu(parent);
    }

    private static void BuildRuntimeUtilityMenu(RectTransform parent)
    {
        Font defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        RectTransform root = CreateRect("UtilityRoot", parent, new Vector2(320f, 188f));
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.anchoredPosition = new Vector2(150f, -8f);

        RectTransform box = CreateRect("UtilityBox", root, new Vector2(320f, 170f));
        box.anchorMin = new Vector2(0f, 1f);
        box.anchorMax = new Vector2(0f, 1f);
        box.pivot = new Vector2(0f, 1f);
        box.anchoredPosition = Vector2.zero;
        Image boxImage = box.gameObject.AddComponent<Image>();
        boxImage.color = new Color(0.37f, 0.21f, 0.10f, 0.92f);
        Outline boxOutline = box.gameObject.AddComponent<Outline>();
        boxOutline.effectColor = new Color(0.84f, 0.67f, 0.28f, 1f);
        boxOutline.effectDistance = new Vector2(2f, -2f);

        RectTransform content = CreateRect("UtilityContent", box, new Vector2(292f, 122f));
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(0f, 1f);
        content.pivot = new Vector2(0f, 1f);
        content.anchoredPosition = new Vector2(14f, -12f);

        GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(52f, 54f);
        grid.spacing = new Vector2(6f, 6f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        grid.childAlignment = TextAnchor.UpperLeft;

        for (int i = 0; i < UtilityLabels.Length; i++)
        {
            CreateUtilityButton(content, defaultFont, UtilityLabels[i]);
        }

        RectTransform collapsedAnchor = CreateRect("AnchorCollapsed", box, new Vector2(36f, 28f));
        collapsedAnchor.anchorMin = new Vector2(1f, 1f);
        collapsedAnchor.anchorMax = new Vector2(1f, 1f);
        collapsedAnchor.pivot = new Vector2(1f, 1f);
        collapsedAnchor.anchoredPosition = new Vector2(-10f, -8f);

        RectTransform expandedAnchor = CreateRect("AnchorExpanded", box, new Vector2(36f, 28f));
        expandedAnchor.anchorMin = new Vector2(0.5f, 0f);
        expandedAnchor.anchorMax = new Vector2(0.5f, 0f);
        expandedAnchor.pivot = new Vector2(0.5f, 0f);
        expandedAnchor.anchoredPosition = new Vector2(0f, 8f);

        RectTransform toggleButtonRect = CreateRect("ToggleArrowButton", box, new Vector2(36f, 28f));
        Image toggleImage = toggleButtonRect.gameObject.AddComponent<Image>();
        toggleImage.color = new Color(0.90f, 0.58f, 0.18f, 1f);
        Button toggleButton = toggleButtonRect.gameObject.AddComponent<Button>();

        RectTransform arrowRect = CreateRect("ArrowGraphic", toggleButtonRect, new Vector2(24f, 24f));
        arrowRect.anchorMin = new Vector2(0.5f, 0.5f);
        arrowRect.anchorMax = new Vector2(0.5f, 0.5f);
        arrowRect.pivot = new Vector2(0.5f, 0.5f);
        arrowRect.anchoredPosition = Vector2.zero;
        Text arrowText = arrowRect.gameObject.AddComponent<Text>();
        arrowText.text = "^";
        arrowText.font = defaultFont;
        arrowText.fontSize = 20;
        arrowText.alignment = TextAnchor.MiddleCenter;
        arrowText.color = Color.white;

        UtilityDrawerController controller = root.gameObject.AddComponent<UtilityDrawerController>();
        controller.ConfigureRuntime(
            box.gameObject,
            content.gameObject,
            toggleButton,
            null,
            toggleButtonRect,
            arrowRect,
            expandedAnchor,
            collapsedAnchor,
            box,
            false,
            170f,
            44f,
            true);

        Debug.Log("[UtilityDrawerAutoInstaller] Created default utility drawer under HUD.");
    }

    private static void CreateUtilityButton(RectTransform parent, Font font, string label)
    {
        RectTransform buttonRect = CreateRect(label.Replace(" ", string.Empty) + "Button", parent, new Vector2(52f, 54f));
        Image image = buttonRect.gameObject.AddComponent<Image>();
        image.color = new Color(0.98f, 0.83f, 0.24f, 1f);
        Button button = buttonRect.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(1f, 0.90f, 0.38f, 1f);
        colors.pressedColor = new Color(0.85f, 0.67f, 0.16f, 1f);
        button.colors = colors;
        button.onClick.AddListener(() => Debug.Log($"[UtilityDrawerAutoInstaller] Clicked utility '{label}'."));

        RectTransform textRect = CreateRect("Label", buttonRect, new Vector2(46f, 46f));
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(3f, 3f);
        textRect.offsetMax = new Vector2(-3f, -3f);
        Text text = textRect.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = 10;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.32f, 0.12f, 0.02f, 1f);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.text = label;
    }

    private static RectTransform FindRectTransform(Scene scene, string path)
    {
        string[] segments = path.Split('/');
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root.name != segments[0])
                continue;

            Transform current = root.transform;
            for (int j = 1; j < segments.Length && current != null; j++)
                current = FindDirectChild(current, segments[j]);

            if (current is RectTransform rect)
                return rect;
        }

        return null;
    }

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;
        }

        return null;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        return rect;
    }
}