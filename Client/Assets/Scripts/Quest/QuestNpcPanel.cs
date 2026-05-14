using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// QuestNpcPanel — Panel nhiệm vụ khi người chơi nói chuyện với NPC loại "quest".
/// Hiển thị danh sách nhiệm vụ từ NPC: nhận mới / xem tiến trình / nộp nhiệm vụ.
///
/// Prefab: Assets/Resources/UI/QuestNpcPanel.prefab
/// Gọi QuestNpcPanel.GetOrCreate() từ NpcMenuUI để mở.
/// </summary>
public class QuestNpcPanel : MonoBehaviour
{
    private const string LogPrefix = "[QuestNpcPanel]";
    private const string ResourcesPath = "UI/QuestNpcPanel";

    public static QuestNpcPanel Instance { get; private set; }

    // ─── Inspector references ────────────────────────────────────────────────
    [Header("Root")]
    [SerializeField] private GameObject rootPanel;

    [Header("Header")]
    [SerializeField] private TMP_Text npcNameText;

    [Header("Quest Detail")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private TMP_Text   questNameText;
    [SerializeField] private TMP_Text   questDescText;
    [SerializeField] private TMP_Text   questProgressText;
    [SerializeField] private TMP_Text   questRewardText;

    [Header("Buttons")]
    [SerializeField] private Button     btnAccept;
    [SerializeField] private Button     btnComplete;
    [SerializeField] private Button     btnClose;

    [Header("Quest List (for NPCs with multiple quests)")]
    [SerializeField] private Transform  questListRoot;
    [SerializeField] private GameObject questListItemPrefab; // Text + Button

    // ─── Runtime state ───────────────────────────────────────────────────────
    private NpcData _currentNpc;
    private QuestManager.QuestStatusDto _selectedQuest;
    private bool _initialized;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        if (Instance == null) Instance = this;
        EnsureInit();
    }

    private void EnsureInit()
    {
        if (_initialized) return;
        _initialized = true;

        // Auto-wire buttons by common name conventions
        if (btnClose   == null) btnClose   = FindChildButton("BtnClose");
        if (btnAccept  == null) btnAccept  = FindChildButton("BtnAccept");
        if (btnComplete == null) btnComplete = FindChildButton("BtnComplete");

        btnClose?.onClick.AddListener(Close);
        btnAccept?.onClick.AddListener(OnAcceptClicked);
        btnComplete?.onClick.AddListener(OnCompleteClicked);

        if (rootPanel) rootPanel.SetActive(false);
    }

    // ─── Public API ──────────────────────────────────────────────────────────

    public static QuestNpcPanel GetOrCreate()
    {
        if (Instance != null) return Instance;
        Instance = FindObjectOfType<QuestNpcPanel>(true);

        if (Instance == null)
        {
            var prefabGO = Resources.Load<GameObject>(ResourcesPath);
            if (prefabGO != null)
            {
                var go = Instantiate(prefabGO);
                go.name = "QuestNpcPanel";
                Instance = go.GetComponent<QuestNpcPanel>();
            }
        }

        if (Instance == null)
            Debug.LogWarning($"{LogPrefix} Không tìm thấy QuestNpcPanel trong scene hoặc Resources/{ResourcesPath}.");

        return Instance;
    }

    public void Open(NpcData npc)
    {
        _currentNpc = npc;
        EnsureInit();

        if (rootPanel) rootPanel.SetActive(true);
        else gameObject.SetActive(true);

        if (npcNameText) npcNameText.text = npc?.npc_name ?? "";

        // Tải danh sách quest từ NPC này
        QuestManager.Instance?.RefreshFromServer(npc?.npc_id ?? 0, OnQuestListLoaded);
    }

    public void Close()
    {
        _currentNpc    = null;
        _selectedQuest = null;

        if (rootPanel) rootPanel.SetActive(false);
        else gameObject.SetActive(false);
    }

    // ─── Internal ────────────────────────────────────────────────────────────

    private void OnQuestListLoaded(List<QuestManager.QuestStatusDto> quests)
    {
        if (quests == null || quests.Count == 0)
        {
            ShowDetail(null, "NPC này hiện không có nhiệm vụ nào.");
            return;
        }

        // Chọn quest ưu tiên: active > available > completed
        var active    = quests.Find(q => q.status == "active");
        var available = quests.Find(q => q.status == "available");
        var pick      = active ?? available ?? quests[0];

        ShowDetail(pick, null);
        BuildQuestList(quests);
    }

    private void ShowDetail(QuestManager.QuestStatusDto quest, string fallbackMsg)
    {
        _selectedQuest = quest;

        if (quest == null)
        {
            if (questNameText)     questNameText.text     = fallbackMsg ?? "";
            if (questDescText)     questDescText.text     = "";
            if (questProgressText) questProgressText.text = "";
            if (questRewardText)   questRewardText.text   = "";
            btnAccept?.gameObject.SetActive(false);
            btnComplete?.gameObject.SetActive(false);
            return;
        }

        if (questNameText) questNameText.text = quest.name ?? "";
        if (questDescText) questDescText.text = quest.description ?? "";

        // Tiến trình
        if (questProgressText)
        {
            string prog = BuildProgressText(quest);
            questProgressText.text = prog;
        }

        // Phần thưởng
        if (questRewardText)
        {
            string reward = BuildRewardText(quest.rewards_json);
            questRewardText.text = reward;
        }

        bool canAccept   = quest.status == "available";
        bool canComplete = quest.status == "active" && IsAllStepsDone(quest);

        btnAccept?.gameObject.SetActive(canAccept);
        btnComplete?.gameObject.SetActive(canComplete);
    }

    private void BuildQuestList(List<QuestManager.QuestStatusDto> quests)
    {
        if (questListRoot == null) return;

        // Clear old entries
        for (int i = questListRoot.childCount - 1; i >= 0; i--)
            Destroy(questListRoot.GetChild(i).gameObject);

        foreach (var q in quests)
        {
            if (questListItemPrefab != null)
            {
                var item = Instantiate(questListItemPrefab, questListRoot);
                var label = item.GetComponentInChildren<TMP_Text>();
                if (label) label.text = $"{q.name}  [{LocalizeStatus(q.status)}]";
                var btn = item.GetComponentInChildren<Button>();
                var captured = q;
                btn?.onClick.AddListener(() => ShowDetail(captured, null));
            }
        }
    }

    private void OnAcceptClicked()
    {
        if (_selectedQuest == null) return;
        btnAccept.interactable = false;

        QuestManager.Instance?.AcceptQuest(_selectedQuest.id, (ok, msg) =>
        {
            btnAccept.interactable = true;
            if (ok)
            {
                Debug.Log($"{LogPrefix} Nhận nhiệm vụ '{_selectedQuest.name}' thành công.");
                QuestManager.Instance.RefreshFromServer(_currentNpc?.npc_id ?? 0, OnQuestListLoaded);
            }
            else
            {
                Debug.LogWarning($"{LogPrefix} Nhận nhiệm vụ thất bại: {msg}");
                if (questProgressText) questProgressText.text = $"Lỗi: {msg}";
            }
        });
    }

    private void OnCompleteClicked()
    {
        if (_selectedQuest == null) return;
        btnComplete.interactable = false;

        QuestManager.Instance?.CompleteQuest(_selectedQuest.id, (ok, msg) =>
        {
            btnComplete.interactable = true;
            if (ok)
            {
                Debug.Log($"{LogPrefix} Nộp nhiệm vụ '{_selectedQuest.name}' thành công.");
                QuestManager.Instance.RefreshFromServer(_currentNpc?.npc_id ?? 0, OnQuestListLoaded);
            }
            else
            {
                Debug.LogWarning($"{LogPrefix} Nộp nhiệm vụ thất bại: {msg}");
                if (questProgressText) questProgressText.text = $"Lỗi: {msg}";
            }
        });
    }

    // ─── Text helpers ─────────────────────────────────────────────────────────

    private static string BuildProgressText(QuestManager.QuestStatusDto q)
    {
        if (q.status == "available") return "Chưa nhận nhiệm vụ.";
        if (q.status == "completed") return "Đã hoàn thành.";
        if (string.IsNullOrEmpty(q.steps_json)) return "Đang thực hiện...";

        try
        {
            // Minimal parse: get step names from steps_json array
            // Format: [{"type":"kill","target_name":"Goblin","required_count":5}, ...]
            var wrapped  = $"{{\"items\":{q.steps_json}}}";
            var stepRoot = JsonUtility.FromJson<StepArrayWrapper>(wrapped);
            if (stepRoot?.items == null || stepRoot.items.Count == 0) return "Đang thực hiện...";

            // Parse progress
            var prog = ParseProgressJson(q.progress_json);

            var lines = new System.Text.StringBuilder();
            for (int i = 0; i < stepRoot.items.Count; i++)
            {
                var step  = stepRoot.items[i];
                prog.TryGetValue(i.ToString(), out int done);
                bool isDone = done >= step.required_count;
                string checkmark = isDone ? "✓ " : (i == q.current_step_index ? "▶ " : "   ");
                lines.AppendLine($"{checkmark}{step.target_name}: {done}/{step.required_count}");
            }
            return lines.ToString().TrimEnd();
        }
        catch { return "Đang thực hiện..."; }
    }

    private static string BuildRewardText(string rewardsJson)
    {
        if (string.IsNullOrEmpty(rewardsJson)) return "";
        try
        {
            var r = JsonUtility.FromJson<RewardDto>(rewardsJson);
            var parts = new List<string>();
            if (r.exp  > 0) parts.Add($"EXP: {r.exp}");
            if (r.gold > 0) parts.Add($"Gold: {r.gold}");
            return string.Join("  |  ", parts);
        }
        catch { return ""; }
    }

    private static bool IsAllStepsDone(QuestManager.QuestStatusDto q)
    {
        if (string.IsNullOrEmpty(q.steps_json)) return false;
        try
        {
            var wrapped  = $"{{\"items\":{q.steps_json}}}";
            var stepRoot = JsonUtility.FromJson<StepArrayWrapper>(wrapped);
            if (stepRoot?.items == null) return false;
            var prog = ParseProgressJson(q.progress_json);
            foreach (var step in stepRoot.items)
            {
                prog.TryGetValue(stepRoot.items.IndexOf(step).ToString(), out int done);
                if (done < step.required_count) return false;
            }
            return true;
        }
        catch { return false; }
    }

    private static Dictionary<string, int> ParseProgressJson(string json)
    {
        var dict = new Dictionary<string, int>();
        if (string.IsNullOrEmpty(json) || json == "{}") return dict;
        try
        {
            // Minimal parsing for {"0":3,"1":0} style
            json = json.Trim('{', '}');
            foreach (var pair in json.Split(','))
            {
                var kv = pair.Split(':');
                if (kv.Length == 2)
                {
                    string key = kv[0].Trim('"', ' ');
                    if (int.TryParse(kv[1].Trim(), out int val))
                        dict[key] = val;
                }
            }
        }
        catch { }
        return dict;
    }

    private static string LocalizeStatus(string status) => status switch
    {
        "available" => "Có thể nhận",
        "active"    => "Đang làm",
        "completed" => "Đã xong",
        "locked"    => "Khóa",
        _           => status
    };

    private Button FindChildButton(string childName)
    {
        var t = transform.Find(childName) ?? transform.Find($"rootPanel/{childName}");
        return t?.GetComponent<Button>();
    }

    // ─── Sub-DTOs for local parsing ───────────────────────────────────────────
    [Serializable]
    private class StepDto
    {
        public string type;
        public string target_name;
        public int    required_count;
    }

    [Serializable]
    private class StepArrayWrapper { public List<StepDto> items; }

    [Serializable]
    private class RewardDto { public int exp; public int gold; public int silver; }
}
