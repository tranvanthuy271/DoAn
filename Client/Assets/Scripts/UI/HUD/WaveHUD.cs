using TMPro;
using UnityEngine;
using UnityEngine.UI;

// WaveHUD — Hiển thị thông tin vòng hiện tại và thời gian còn lại trên client.
// Vị trí: góc trên bên phải màn hình.
// Có nút "Thoát phó bản" bên dưới để exit và đóng dungeon.
public class WaveHUD : MonoBehaviour
{
    [Header("UI Labels (gán trong Inspector hoặc để trống để tự tạo)")]
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Button   exitButton;

    [Header("Hiển thị")]
    [SerializeField] private GameObject hudRoot;
    [SerializeField] private float pollInterval = 0.5f;

    // Layout constants
    private const float PanelW    = 220f;
    private const float LabelH    = 30f;
    private const float BtnH      = 36f;
    private const float Padding   = 10f;
    private const float PanelH    = Padding + LabelH + 6f + LabelH + 8f + BtnH + Padding; // ≈120

    private DungeonManager _dungeonManager;
    private float _pollTimer;
    private int _lastRound     = -1;
    private int _lastRemaining = -1;
    private int _lastMaxRounds = -1;
    private bool _exitRequested;

    // Unity lifecycle

    private void Start()
    {
        _dungeonManager = DungeonManager.Instance;

        if (roundText == null || timerText == null || exitButton == null)
            AutoCreateUI();

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitClicked);

        SetHudVisible(false);
    }

    // AutoCreateUI (programmatic)

    private void AutoCreateUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            var canvasGo = new GameObject("WaveHUD_Canvas");
            if (transform.parent == null)
                UnityEngine.Object.DontDestroyOnLoad(canvasGo);
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
            transform.SetParent(canvasGo.transform, false);
        }

        // Panel — neo góc trên phải
        if (GetComponent<RectTransform>() == null) gameObject.AddComponent<RectTransform>();
        var rect = GetComponent<RectTransform>();
        rect.anchorMin        = new Vector2(1f, 1f);
        rect.anchorMax        = new Vector2(1f, 1f);
        rect.pivot            = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-16f, -16f);
        rect.sizeDelta        = new Vector2(PanelW, PanelH);

        var bg = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.6f);

        float y = -Padding;

        if (roundText == null)
        {
            roundText = CreateLabel("RoundText", new Vector2(-Padding, y), new Vector2(PanelW - Padding * 2, LabelH), 18, "Vòng -/-", TextAlignmentOptions.Center);
            y -= LabelH + 6f;
        }

        if (timerText == null)
        {
            timerText = CreateLabel("TimerText", new Vector2(-Padding, y), new Vector2(PanelW - Padding * 2, LabelH), 16, "00:00", TextAlignmentOptions.Center);
            y -= LabelH + 8f;
        }

        if (exitButton == null)
            exitButton = CreateExitButton(new Vector2(0f, y));

        { /* AutoCreateUI  top-right panel created under '{canvas.gameObject.name}' */ }
    }

    private TMP_Text CreateLabel(string goName, Vector2 anchorPos, Vector2 size, float fontSize, string defaultText, TextAlignmentOptions align)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(transform, false);
        var r = go.AddComponent<RectTransform>();
        // Anchor top-right để cùng hệ tọa độ với panel
        r.anchorMin        = new Vector2(1f, 1f);
        r.anchorMax        = new Vector2(1f, 1f);
        r.pivot            = new Vector2(1f, 1f);
        r.anchoredPosition = anchorPos;
        r.sizeDelta        = size;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text           = defaultText;
        t.fontSize       = fontSize;
        t.color          = Color.white;
        t.alignment      = align;
        t.raycastTarget  = false;
        return t;
    }

    private Button CreateExitButton(Vector2 yOffset)
    {
        // Container
        var btnGo = new GameObject("ExitButton");
        btnGo.transform.SetParent(transform, false);
        var r = btnGo.AddComponent<RectTransform>();
        r.anchorMin        = new Vector2(0f, 1f);
        r.anchorMax        = new Vector2(1f, 1f);
        r.pivot            = new Vector2(0.5f, 1f);
        r.offsetMin        = new Vector2(Padding, 0f);
        r.offsetMax        = new Vector2(-Padding, 0f);
        r.anchoredPosition = new Vector2(0f, yOffset.y);
        r.sizeDelta        = new Vector2(0f, BtnH);

        var img = btnGo.AddComponent<Image>();
        img.color = new Color(0.75f, 0.12f, 0.12f, 0.92f);

        var btn = btnGo.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.9f, 0.2f, 0.2f, 1f);
        colors.pressedColor     = new Color(0.55f, 0.05f, 0.05f, 1f);
        btn.colors = colors;

        // Label
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(btnGo.transform, false);
        var lr = labelGo.AddComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero;
        lr.offsetMax = Vector2.zero;
        var t = labelGo.AddComponent<TextMeshProUGUI>();
        t.text      = "Thoát phó bản";
        t.fontSize  = 15;
        t.color     = Color.white;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;

        return btn;
    }

    // Exit handler

    private void OnExitClicked()
    {
        if (_exitRequested) return;
        _exitRequested = true;

        var dm = _dungeonManager != null ? _dungeonManager : DungeonManager.Instance;
        if (dm == null)
        {
            { /* Cảnh báo: DungeonManager không tìm thấy khi Exit */ }
            _exitRequested = false;
            return;
        }

        // returnMapId = 0 → server tự tìm zone ít người nhất trên map mặc định
        dm.ExitDungeon(0);

        // Tắt button để tránh double-click
        if (exitButton != null) exitButton.interactable = false;
    }

    // Update

    private void Update()
    {
        _pollTimer -= Time.deltaTime;
        if (_pollTimer <= 0f)
        {
            _pollTimer = pollInterval;
            if (_dungeonManager == null)
                _dungeonManager = DungeonManager.Instance;
        }

        if (_dungeonManager == null) { SetLabelsVisible(false); return; }

        if (!_dungeonManager.IsInDungeon)
        {
            SetHudVisible(false);
            _exitRequested = false;
            if (exitButton != null) exitButton.interactable = true;
            return;
        }

        int round     = _dungeonManager.CurrentWaveRound;
        int maxRounds = _dungeonManager.CurrentWaveMaxRounds;
        int remaining = _dungeonManager.CurrentWaveRemainingSeconds;

        if (round != _lastRound || remaining != _lastRemaining || maxRounds != _lastMaxRounds)
        {
            _lastRound     = round;
            _lastRemaining = remaining;
            _lastMaxRounds = maxRounds;

            SetHudVisible(round > 0);

            if (roundText != null)
                roundText.text = $"Vòng {round} / {maxRounds}";

            if (timerText != null)
            {
                int sec = Mathf.Max(0, remaining);
                timerText.text  = $"{sec / 60:00}:{sec % 60:00}";
                timerText.color = sec < 30 ? Color.red : Color.white;
            }
        }
    }

    private void OnDestroy()
    {
        if (exitButton != null) exitButton.onClick.RemoveListener(OnExitClicked);
        _dungeonManager = null;
    }

    // Hàm hỗ trợ dùng nội bộ để tách nhỏ xử lý chính.

    private void SetHudVisible(bool visible)
    {
        if (hudRoot != null && hudRoot != gameObject)
        {
            hudRoot.SetActive(visible);
        }
        else
        {
            SetLabelsVisible(visible);
        }
    }

    private void SetLabelsVisible(bool visible)
    {
        if (roundText  != null) roundText.gameObject.SetActive(visible);
        if (timerText  != null) timerText.gameObject.SetActive(visible);
        if (exitButton != null) exitButton.gameObject.SetActive(visible);
    }
}