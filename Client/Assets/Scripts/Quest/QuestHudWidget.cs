using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// QuestHudWidget — Panel nhá» ở góc màn hình hiển thị nhiệm vụ hiện tại.
///
/// Hiển thị:
///   - "Chính: [tên quest]"
///   - "- Tìm [npc_name] để nhận nhiệm vụ"   (nếu quest chưa nhận)
///   - "- [tên bước]: done/require"             (nếu đang làm)
///   - "- ✓ Tìm [npc_name] để nộp nhiệm vụ"   (nếu hoàn thành)
///
/// Nhấn nút "→" để tự động di chuyển đến mục tiêu.
///
/// Cấu trúc GameObject khuyến nghị:
///   QuestHudWidget (MonoBehaviour)
///   └── Panel (Image – ná»n má»)
///       ├── QuestName  (TMP_Text – "Chính: ...")
///       ├── QuestStep  (TMP_Text – "- ...")
///       └── BtnNavigate (Button)
///           └── Label (TMP_Text – "→")
/// </summary>
public class QuestHudWidget : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject rootWidget;
    [SerializeField] private TMP_Text   questNameText;
    [SerializeField] private TMP_Text   questStepText;
    [SerializeField] private Button     btnNavigate;
    [SerializeField] private TMP_Text   btnNavigateLabel;

    // ─── Auto-move state ──────────────────────────────────────────────────────
    private bool  _autoMoving;
    private float _autoMoveTargetX;
    private int   _autoMoveTargetMapId = -1;

    private const float ARRIVE_THRESHOLD = 0.8f;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Tách khỏi parent Canvas nếu có (giống QuestNpcPanel) để sortOrder=30 hoạt động đúng
        if (transform.parent != null)
            transform.SetParent(null, false);
        DontDestroyOnLoad(gameObject);
        AutoWire();
        // Đăng ký rootWidget làm HUD: ẩn khi có panel mở, hiện khi hết panel
        UIPanelManager.RegisterHud(rootWidget != null ? rootWidget : gameObject);
    }

    private void OnDestroy()
    {
        UIPanelManager.UnregisterHud(rootWidget != null ? rootWidget : gameObject);
    }

    private void OnEnable()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestListChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestListChanged -= Refresh;
        StopAutoMove();
    }

    private void Start()
    {
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
        var quest = QuestManager.Instance?.HintQuest
                 ?? QuestManager.Instance?.ActiveQuest;

        Debug.Log($"[QuestHudWidget] Refresh() — QuestManager={QuestManager.Instance != null} HintQuest={QuestManager.Instance?.HintQuest?.name} ActiveQuest={QuestManager.Instance?.ActiveQuest?.name} rootWidget={rootWidget?.name} active={rootWidget?.activeSelf}");

        // Luôn hiện widget (ẩn chỉ khi rootWidget = null)
        if (rootWidget) rootWidget.SetActive(true);

        if (quest == null)
        {
            StopAutoMove();
            if (questNameText) questNameText.text = "Nhiệm vụ: Chưa có";
            if (questStepText)  questStepText.text  = "- Tìm NPC nhiệm vụ để bắt đầu";
            if (btnNavigate)    btnNavigate.gameObject.SetActive(false);
            Debug.Log("[QuestHudWidget] quest=null → hiển thị 'Chưa có'");
            return;
        }

        Debug.Log($"[QuestHudWidget] quest={quest.name} status={quest.status} progress={quest.quest_progress_json} stepIdx={quest.current_step_index}");

        StopAutoMove();

        // "Chính: [name]"
        if (questNameText) questNameText.text = $"Chính: {quest.name}";

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

    // ─── Step line builder ────────────────────────────────────────────────────

    private static string BuildStepLine(QuestManager.QuestStatusDto q)
    {
        if (q.status == "available")
        {
            return $"- Tìm {BuildNpcTargetLabel(q)} để nhận nhiệm vụ";
        }

        var steps = ParseSteps(q.steps_json);
        if (steps == null || steps.Count == 0) return "- Đang thực hiện...";

        bool allDone = AreAllDone(q, steps);
        if (allDone)
        {
            return $"- ✓ Tìm {BuildNpcTargetLabel(q)} để nộp nhiệm vụ";
        }

        int idx = Mathf.Clamp(q.current_step_index, 0, steps.Count - 1);
        var step = steps[idx];
        int done = GetProgress(q, idx);

        int remaining = 0;
        for (int i = 0; i < steps.Count; i++)
            if (GetProgress(q, i) < steps[i].require) remaining++;

        string extra = remaining > 1 ? $" (còn {remaining} việc)" : "";
        return $"- {step.name}: {done}/{step.require}{extra}";
    }

    private static string BuildNpcTargetLabel(QuestManager.QuestStatusDto q)
    {
        string npcName = string.IsNullOrEmpty(q.npc_name) ? "NPC" : q.npc_name;
        if (!string.IsNullOrEmpty(q.npc_map_name))
            return $"{npcName} ở {q.npc_map_name}";
        if (q.npc_map_id >= 0)
            return $"{npcName} ở map {q.npc_map_id}";
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

        if (btnNavigate != null) btnNavigate.onClick.AddListener(OnNavigateClicked);
    }

    // ─── DTOs ─────────────────────────────────────────────────────────────────

    [System.Serializable]
    private class StepDto { public string name; public int require; public int idMap = -1; public int x; public int y; }

    [System.Serializable]
    private class StepArrayWrapper { public List<StepDto> items; }
}

