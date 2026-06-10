using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.SceneManagement;

/// <summary>
/// HUD nhiem vu nho o goc man hinh.
///
/// Hien thi:
///   - "Chinh: [ten quest]"
///   - "- Tim [npc_name] de nhan nhiem vu" khi quest chua nhan.
///   - "- [ten buoc]: done/require" khi quest dang lam.
///   - "- [x] Tim [npc_name] de nop nhiem vu" khi da hoan thanh.
///
/// Nut dieu huong tu dong dua player toi NPC hoac muc tieu cua buoc hien tai.
/// </summary>
public class QuestHudWidget : MonoBehaviour
{
    private static QuestHudWidget _instance;

    private static readonly Vector2 HudPanelSize = new Vector2(360f, 104f);
    private static readonly Vector2 HudPanelPosition = new Vector2(12f, -336.2f);
    private static readonly Vector2 NavigateButtonSize = new Vector2(44f, 86f);
    private static readonly Vector2 PerfStatsSize = new Vector2(320f, 30f);
    private static readonly Vector2 PerfStatsPosition = new Vector2(-12f, HudPanelPosition.y);
    private static readonly Vector4 HudTextMargin = new Vector4(2f, 0f, 2f, 0f);
    private const float PerfStatsUpdateInterval = 5f;

    [Header("UI References")]
    [SerializeField] private GameObject rootWidget;
    [SerializeField] private TMP_Text   questNameText;
    [SerializeField] private TMP_Text   questStepText;
    [SerializeField] private Button     btnNavigate;
    [SerializeField] private TMP_Text   btnNavigateLabel;
    [SerializeField] private TMP_Text   perfStatsText;

    // ─── Auto-move state ──────────────────────────────────────────────────────
    private bool  _autoMoving;
    private float _autoMoveTargetX;
    private int   _autoMoveTargetMapId = -1;
    private float _perfTimer;
    private float _perfElapsed;
    private int   _perfFrames;
    private float _rttTotalMs;
    private int   _rttSamples;
    private bool  _isGameplayScene;

    private const float ARRIVE_THRESHOLD = 0.8f;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        // Tách khỏi parent Canvas nếu có (giống QuestNpcPanel) để sortOrder=30 hoạt động đúng
        if (transform.parent != null)
            transform.SetParent(null, false);
        DontDestroyOnLoad(gameObject);
        AutoWire();
        _isGameplayScene = IsGameplayScene(SceneManager.GetActiveScene().name);
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureHudLayout();
        ApplySceneVisibility();
        // Đăng ký rootWidget làm HUD: ẩn khi có panel mở, hiện khi hết panel
        UIPanelManager.RegisterHud(rootWidget != null ? rootWidget : gameObject);
        if (perfStatsText != null)
            UIPanelManager.RegisterHud(perfStatsText.gameObject);
        ApplySceneVisibility();
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        UIPanelManager.UnregisterHud(rootWidget != null ? rootWidget : gameObject);
        if (perfStatsText != null)
            UIPanelManager.UnregisterHud(perfStatsText.gameObject);
    }

    private void OnEnable()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestListChanged += Refresh;
        if (_isGameplayScene)
            Refresh();
        else
            ApplySceneVisibility();
    }

    private void OnDisable()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestListChanged -= Refresh;
        StopAutoMove();
    }

    private void Start()
    {
        if (!_isGameplayScene)
        {
            ApplySceneVisibility();
            return;
        }

        // Tải trạng thái HUD ngay khi vào scene
        if (QuestManager.Instance != null)
        {
            // Subscribe ở đây phòng trường hợp OnEnable() chạy trước QuestManager.Awake()
            // (khi đó Instance=null → OnEnable không subscribe được → subscribers=0)
            QuestManager.Instance.OnQuestListChanged -= Refresh; // tránh double nếu đã subscribe
            QuestManager.Instance.OnQuestListChanged += Refresh;
            QuestManager.Instance.RefreshPlayerOverview(Refresh);
        }
        else
            Refresh();
    }

    private void Update()
    {
        if (!_isGameplayScene)
        {
            ApplySceneVisibility();
            return;
        }

        UpdatePerfStats();

        if (!_autoMoving) return;

        if (Input.GetKeyDown(KeyCode.Escape)) { StopAutoMove(); return; }

        int curMap = MapManager.Instance != null ? MapManager.Instance.mapId : -1;

        // Äang chá» chuyển sang map khác — không inject input
        if (_autoMoveTargetMapId >= 0 && curMap >= 0 && curMap != _autoMoveTargetMapId) return;

        var player = GetLocalPlayer();
        if (player == null) { StopAutoMove(); return; }

        float diff = _autoMoveTargetX - player.transform.position.x;
        if (Mathf.Abs(diff) <= ARRIVE_THRESHOLD) { StopAutoMove(); return; }

        InputManager.Instance?.SetAutoMoveInput(diff > 0 ? 1f : -1f);
    }

    // ─── Refresh ──────────────────────────────────────────────────────────────

    public void Refresh()
    {
        if (!_isGameplayScene)
        {
            ApplySceneVisibility();
            return;
        }

        var quest = QuestManager.Instance?.HintQuest
                 ?? QuestManager.Instance?.ActiveQuest;

        Debug.Log($"[QuestHudWidget] Refresh() — QuestManager={QuestManager.Instance != null} HintQuest={QuestManager.Instance?.HintQuest?.name} ActiveQuest={QuestManager.Instance?.ActiveQuest?.name} rootWidget={rootWidget?.name} active={rootWidget?.activeSelf}");

        // Luôn hiện widget (ẩn chỉ khi rootWidget = null)
        EnsureHudLayout();
        if (rootWidget) UIPanelManager.ApplyHudVisibility(rootWidget);
        if (perfStatsText != null) UIPanelManager.ApplyHudVisibility(perfStatsText.gameObject);

        if (quest == null)
        {
            StopAutoMove();
            if (questNameText) questNameText.text = "Nhiem vu: Chua co";
            if (questStepText)  questStepText.text  = "- Tim NPC nhiem vu de bat dau";
            if (btnNavigate)    btnNavigate.gameObject.SetActive(false);
            Debug.Log("[QuestHudWidget] quest=null -> hien thi Chua co");
            return;
        }

        Debug.Log($"[QuestHudWidget] quest={quest.name} status={quest.status} progress={quest.quest_progress_json} stepIdx={quest.current_step_index}");

        StopAutoMove();

        // "Chinh: [name]"
        if (questNameText) questNameText.text = $"Chinh: {quest.name}";

        // Step line
        if (questStepText) questStepText.text = BuildStepLine(quest);

        if (btnNavigate) btnNavigate.gameObject.SetActive(true);
        if (btnNavigateLabel) btnNavigateLabel.text = "->";  // mui ten
    }

    // ─── Navigation ───────────────────────────────────────────────────────────

    private void OnNavigateClicked()
    {
        var quest = QuestManager.Instance?.HintQuest
                 ?? QuestManager.Instance?.ActiveQuest;
        if (quest == null) return;

        var steps  = ParseSteps(quest.steps_json);
        bool allDone = AreAllDone(quest, steps);

        if (quest.status == "available" || allDone)
        {
            // Äến NPC nhận / nộp quest
            NavigateTo(quest.npc_map_id, quest.npc_pos_x);
        }
        else if (steps != null && quest.current_step_index < steps.Count)
        {
            var step = steps[quest.current_step_index];
            NavigateTo(step.idMap, step.x);
        }
    }

    private void NavigateTo(int targetMapId, float targetX)
    {
        int curMap = MapManager.Instance != null ? MapManager.Instance.mapId : -1;

        if (targetMapId < 0 || targetMapId == curMap)
        {
            _autoMoveTargetX     = targetX;
            _autoMoveTargetMapId = curMap;
            _autoMoving          = true;
        }
        else
        {
            bool goRight = targetMapId > curMap;
            TriggerMapTransitionButton(goRight, targetMapId, targetX);
        }
    }

    private void TriggerMapTransitionButton(bool goRight, int targetMapId, float targetX)
    {
        var player = GetLocalPlayer();
        if (player == null) return;

        var allBtns = FindObjectsByType<MapTransitionButton>(FindObjectsSortMode.None);
        MapTransitionButton best = null;
        float bestDist = float.MaxValue;

        foreach (var b in allBtns)
        {
            bool btnRight = b.transform.position.x > player.transform.position.x;
            if (btnRight != goRight) continue;
            float d = Mathf.Abs(b.transform.position.x - player.transform.position.x);
            if (d < bestDist) { bestDist = d; best = b; }
        }

        if (best != null)
        {
            _autoMoveTargetX     = targetX;
            _autoMoveTargetMapId = targetMapId;
            _autoMoving          = true;
            best.GetComponent<Button>()?.onClick.Invoke();
        }
    }

    private void StopAutoMove()
    {
        if (_autoMoving) { InputManager.Instance?.CancelAutoMove(); _autoMoving = false; }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _isGameplayScene = IsGameplayScene(scene.name);
        ApplySceneVisibility();

        if (_isGameplayScene)
            Refresh();
        else
            StopAutoMove();
    }

    private static bool IsGameplayScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return false;

        return sceneName == "GameScene"
            || sceneName.StartsWith("Map")
            || sceneName.StartsWith("Dungeon");
    }

    private void ApplySceneVisibility()
    {
        bool visible = _isGameplayScene;
        if (rootWidget != null && rootWidget.activeSelf != visible)
            rootWidget.SetActive(visible);
        if (perfStatsText != null && perfStatsText.gameObject.activeSelf != visible)
            perfStatsText.gameObject.SetActive(visible);
    }

    // ─── Step line builder ────────────────────────────────────────────────────

    private static string BuildStepLine(QuestManager.QuestStatusDto q)
    {
        if (q.status == "available")
        {
            return $"- Tim {BuildNpcTargetLabel(q)} de nhan nhiem vu";
        }

        var steps = ParseSteps(q.steps_json);
        if (steps == null || steps.Count == 0) return "- Đang thực hiện...";

        bool allDone = AreAllDone(q, steps);
        if (allDone)
        {
            return $"- [x] Tim {BuildNpcTargetLabel(q)} de nop nhiem vu";
        }

        int idx = Mathf.Clamp(q.current_step_index, 0, steps.Count - 1);
        var step = steps[idx];
        int done = GetProgress(q, idx);

        int remaining = 0;
        for (int i = 0; i < steps.Count; i++)
            if (GetProgress(q, i) < steps[i].require) remaining++;

        string extra = remaining > 1 ? $" (con {remaining} viec)" : "";
        return $"- {step.name}: {done}/{step.require}{extra}";
    }

    private static string BuildNpcTargetLabel(QuestManager.QuestStatusDto q)
    {
        string npcName = string.IsNullOrEmpty(q.npc_name) ? "NPC" : q.npc_name;
        if (!string.IsNullOrEmpty(q.npc_map_name))
            return $"{npcName} o {q.npc_map_name}";
        if (q.npc_map_id >= 0)
            return $"{npcName} o map {q.npc_map_id}";
        return npcName;
    }

    private static bool AreAllDone(QuestManager.QuestStatusDto q, List<StepDto> steps)
    {
        if (steps == null || steps.Count == 0) return false;
        for (int i = 0; i < steps.Count; i++)
            if (GetProgress(q, i) < steps[i].require) return false;
        return true;
    }

    private static int GetProgress(QuestManager.QuestStatusDto q, int idx)
    {
        if (string.IsNullOrEmpty(q.quest_progress_json)) return 0;
        try
        {
            foreach (var pair in q.quest_progress_json.Trim('{', '}').Split(','))
            {
                var kv = pair.Trim().Split(':');
                if (kv.Length == 2 && kv[0].Trim('"', ' ') == idx.ToString()
                    && int.TryParse(kv[1].Trim(), out int v)) return v;
            }
        }
        catch { }
        return 0;
    }

    private static List<StepDto> ParseSteps(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonUtility.FromJson<StepArrayWrapper>($"{{\"items\":{json}}}")?.items; }
        catch { return null; }
    }

    private static GameObject GetLocalPlayer()
    {
        foreach (var go in GameObject.FindGameObjectsWithTag("Player"))
        {
            var net = go.GetComponent<Unity.Netcode.NetworkObject>();
            if (net != null && net.IsLocalPlayer) return go;
        }
        return null;
    }

    private void AutoWire()
    {
        // rootWidget PHẢI là child panel, không phải gameObject.
        // Nếu rootWidget == gameObject thì Refresh() gọi SetActive(false) trong OnEnable
        // sẽ tắt toàn bộ component trước khi Start() chạy → RefreshPlayerOverview không bao giờ được gọi
        // → HUD bị ẩn vĩnh viễn.
        if (rootWidget == null)
        {
            rootWidget = transform.Find("QuestHudPanel")?.gameObject;
            if (rootWidget == null && transform.childCount > 0)
                rootWidget = transform.GetChild(0).gameObject;
            // KHÔNG fallback về gameObject!
        }

        var root = rootWidget != null ? rootWidget.transform : transform;
        if (questNameText    == null) questNameText    = root.Find("QuestName")?.GetComponent<TMP_Text>();
        if (questStepText    == null) questStepText    = root.Find("QuestStep")?.GetComponent<TMP_Text>();
        if (btnNavigate      == null) btnNavigate      = root.Find("BtnNavigate")?.GetComponent<Button>()
                                                        ?? root.Find("BtnOpen")?.GetComponent<Button>();
        if (btnNavigateLabel == null && btnNavigate != null)
            btnNavigateLabel = btnNavigate.GetComponentInChildren<TMP_Text>();
        if (perfStatsText == null)
            perfStatsText = transform.Find("PerfStatsText")?.GetComponent<TMP_Text>();

        if (btnNavigate != null) btnNavigate.onClick.AddListener(OnNavigateClicked);
    }

    // ─── DTOs ─────────────────────────────────────────────────────────────────

    private void EnsureHudLayout()
    {
        if (rootWidget != null && rootWidget.TryGetComponent(out RectTransform panelRect))
        {
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = HudPanelPosition;
            panelRect.sizeDelta = HudPanelSize;
        }

        ApplyQuestTextLayout(questNameText, 0.52f, 1f, 14f, 4f, -56f, -6f, 18f, FontStyles.Bold);
        ApplyQuestTextLayout(questStepText, 0f, 0.52f, 14f, 8f, -56f, -2f, 16f, FontStyles.Normal);

        if (btnNavigate != null && btnNavigate.TryGetComponent(out RectTransform navRect))
        {
            navRect.anchorMin = new Vector2(1f, 0.5f);
            navRect.anchorMax = new Vector2(1f, 0.5f);
            navRect.pivot = new Vector2(1f, 0.5f);
            navRect.anchoredPosition = new Vector2(-4f, 0f);
            navRect.sizeDelta = NavigateButtonSize;
        }

        if (btnNavigateLabel != null)
        {
            btnNavigateLabel.fontSize = 22f;
            btnNavigateLabel.enableWordWrapping = false;
            btnNavigateLabel.overflowMode = TextOverflowModes.Overflow;
            btnNavigateLabel.alignment = TextAlignmentOptions.Center;
        }

        EnsurePerfStatsText();
    }

    private void EnsurePerfStatsText()
    {
        if (perfStatsText == null)
        {
            var go = new GameObject("PerfStatsText", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            perfStatsText = go.GetComponent<TextMeshProUGUI>();
            perfStatsText.text = "FPS -- | Ping --ms";
            UIRuntimeAssetHelper.ApplyNotoSans(perfStatsText);
            UIPanelManager.RegisterHud(go);
        }

        perfStatsText.fontSize = 16f;
        perfStatsText.fontStyle = FontStyles.Normal;
        perfStatsText.color = Color.white;
        perfStatsText.enableWordWrapping = false;
        perfStatsText.overflowMode = TextOverflowModes.Overflow;
        perfStatsText.alignment = TextAlignmentOptions.MidlineRight;
        perfStatsText.raycastTarget = false;
        perfStatsText.fontMaterial.DisableKeyword("OUTLINE_ON");

        var outline = perfStatsText.GetComponent<Outline>();
        if (outline != null)
        {
            if (Application.isPlaying)
                Destroy(outline);
            else
                DestroyImmediate(outline);
        }

        RectTransform rect = perfStatsText.rectTransform;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = PerfStatsPosition;
        rect.sizeDelta = PerfStatsSize;
        rect.localScale = Vector3.one;
    }

    private void UpdatePerfStats()
    {
        if (perfStatsText == null)
            EnsurePerfStatsText();

        float delta = Time.unscaledDeltaTime;
        if (delta <= 0f)
            return;

        _perfTimer += delta;
        _perfElapsed += delta;
        _perfFrames++;

        float rttMs = GetCurrentRttMs();
        if (rttMs >= 0f)
        {
            _rttTotalMs += rttMs;
            _rttSamples++;
        }

        if (_perfTimer < PerfStatsUpdateInterval)
            return;

        float fps = _perfElapsed > 0f ? Mathf.Min(60f, _perfFrames / _perfElapsed) : 0f;
        string pingText = _rttSamples > 0 ? $"{Mathf.RoundToInt(_rttTotalMs / _rttSamples)}ms" : "--ms";
        if (perfStatsText != null)
            perfStatsText.text = $"FPS {Mathf.RoundToInt(fps)} | Ping {pingText}";

        _perfTimer = 0f;
        _perfElapsed = 0f;
        _perfFrames = 0;
        _rttTotalMs = 0f;
        _rttSamples = 0;
    }

    private static float GetCurrentRttMs()
    {
        var networkManager = NetworkManager.Singleton;
        var transport = networkManager != null ? networkManager.NetworkConfig?.NetworkTransport : null;
        if (networkManager == null || transport == null || !networkManager.IsListening)
            return -1f;

        if (networkManager.IsClient)
            return transport.GetCurrentRtt(NetworkManager.ServerClientId);

        if (networkManager.IsServer && networkManager.ConnectedClientsIds != null)
        {
            float total = 0f;
            int count = 0;
            foreach (ulong clientId in networkManager.ConnectedClientsIds)
            {
                if (clientId == NetworkManager.ServerClientId)
                    continue;

                total += transport.GetCurrentRtt(clientId);
                count++;
            }

            return count > 0 ? total / count : 0f;
        }

        return -1f;
    }

    private static void ApplyQuestTextLayout(
        TMP_Text text,
        float minY,
        float maxY,
        float left,
        float bottom,
        float right,
        float top,
        float fontSize,
        FontStyles fontStyle)
    {
        if (text == null) return;

        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.margin = HudTextMargin;
        text.alignment = TextAlignmentOptions.TopLeft;

        if (!text.TryGetComponent(out RectTransform rect)) return;

        rect.anchorMin = new Vector2(0f, minY);
        rect.anchorMax = new Vector2(1f, maxY);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, top);
    }

    [System.Serializable]
    private class StepDto { public string name; public int require; public int idMap = -1; public int x; public int y; }

    [System.Serializable]
    private class StepArrayWrapper { public List<StepDto> items; }
}

