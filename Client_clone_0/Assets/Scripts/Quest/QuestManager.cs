using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


/// <summary>
/// QuestManager — Singleton quản lý trạng thái nhiệm vụ phía client.
/// Giữ ActiveQuest của người chơi và phát sự kiện khi trạng thái thay đổi.
/// </summary>
public class QuestManager : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static QuestManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ─── State ────────────────────────────────────────────────────────────────
    public QuestStatusDto ActiveQuest   { get; private set; }
    public List<QuestStatusDto> AllQuests { get; private set; } = new();
    /// <summary>Quest cần hiển thị trên HUD: active nếu có, không thì quest available đầu tiên.</summary>
    public QuestStatusDto HintQuest     { get; private set; }

    /// <summary>Phát khi danh sách / tiến trình nhiệm vụ thay đổi.</summary>
    public event Action OnQuestListChanged;

    // ─── API helpers ─────────────────────────────────────────────────────────
    private static string ApiUrl(string path)
    {
        var root = ServerAddressConfig.Instance != null
            ? ServerAddressConfig.Instance.ApiRoot
            : "http://localhost:5000";
        return $"{root.TrimEnd('/')}/api/{path}";
    }

    private static string JwtToken => PlayerPrefs.GetString("JWT_TOKEN", "");

    // ─── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Tải danh sách quest từ NPC (hoặc trạng thái hiện tại khi npcId=0).
    /// </summary>
    public void RefreshFromServer(int npcId = 0, Action<List<QuestStatusDto>> onDone = null)
    {
        StartCoroutine(LoadQuestListRoutine(npcId, onDone));
    }

    public void AcceptQuest(int questId, Action<bool, string> onDone = null)
    {
        StartCoroutine(AcceptQuestRoutine(questId, onDone));
    }

    public void CompleteQuest(int questId, Action<bool, string> onDone = null)
    {
        StartCoroutine(CompleteQuestRoutine(questId, onDone));
    }

    /// <summary>
    /// Tải trạng thái tổng hợp: quest đang active hoặc quest available đầu tiên.
    /// Dùng để cập nhật HintQuest cho QuestHudWidget.
    /// </summary>
    public void RefreshPlayerOverview(Action onDone = null)
    {
        StartCoroutine(LoadPlayerOverviewRoutine(onDone));
    }

    // ─── Coroutines ──────────────────────────────────────────────────────────

    private IEnumerator LoadQuestListRoutine(int npcId, Action<List<QuestStatusDto>> onDone)
    {
        string url = npcId > 0 ? ApiUrl($"quest/list?npcId={npcId}") : ApiUrl("quest/list");
        using var req = UnityWebRequest.Get(url);
        req.SetRequestHeader("Authorization", $"Bearer {JwtToken}");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[QuestManager] LoadQuests failed: {req.error} — {req.downloadHandler?.text}");
            onDone?.Invoke(null);
            yield break;
        }

        var list = ParseQuestList(req.downloadHandler.text);
        AllQuests = list ?? new List<QuestStatusDto>();
        ActiveQuest = AllQuests.Find(q => q.status == "active");
        OnQuestListChanged?.Invoke();
        onDone?.Invoke(AllQuests);
    }

    private IEnumerator AcceptQuestRoutine(int questId, Action<bool, string> onDone)
    {
        var body    = $"{{\"questId\":{questId}}}";
        var bodyRaw = System.Text.Encoding.UTF8.GetBytes(body);

        using var req = new UnityWebRequest(ApiUrl("quest/accept"), "POST");
        req.uploadHandler   = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type",  "application/json");
        req.SetRequestHeader("Authorization", $"Bearer {JwtToken}");
        yield return req.SendWebRequest();

        bool ok = req.result == UnityWebRequest.Result.Success;
        string msg = ok ? "" : (req.downloadHandler?.text ?? req.error);
        if (ok) { StartCoroutine(LoadQuestListRoutine(0, null)); RefreshPlayerOverview(); }
        onDone?.Invoke(ok, msg);
    }

    private IEnumerator CompleteQuestRoutine(int questId, Action<bool, string> onDone)
    {
        var body    = $"{{\"questId\":{questId}}}";
        var bodyRaw = System.Text.Encoding.UTF8.GetBytes(body);

        using var req = new UnityWebRequest(ApiUrl("quest/complete"), "POST");
        req.uploadHandler   = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type",  "application/json");
        req.SetRequestHeader("Authorization", $"Bearer {JwtToken}");
        yield return req.SendWebRequest();

        bool ok = req.result == UnityWebRequest.Result.Success;
        string msg = ok ? "" : (req.downloadHandler?.text ?? req.error);
        if (ok) { StartCoroutine(LoadQuestListRoutine(0, null)); RefreshPlayerOverview(); }
        onDone?.Invoke(ok, msg);
    }

    private IEnumerator LoadPlayerOverviewRoutine(Action onDone)
    {
        using var req = UnityWebRequest.Get(ApiUrl("quest/player-overview"));
        req.SetRequestHeader("Authorization", $"Bearer {JwtToken}");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[QuestManager] PlayerOverview failed: {req.error}");
            onDone?.Invoke();
            yield break;
        }

        string text = req.downloadHandler.text?.Trim();
        if (string.IsNullOrEmpty(text) || text == "null")
        {
            HintQuest   = null;
            ActiveQuest = null;
            OnQuestListChanged?.Invoke();
            onDone?.Invoke();
            yield break;
        }

        try
        {
            var dto = JsonUtility.FromJson<QuestStatusDto>(text);
            HintQuest = dto;
            if (dto?.status == "active") ActiveQuest = dto;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[QuestManager] ParsePlayerOverview error: {ex.Message}");
        }

        OnQuestListChanged?.Invoke();
        onDone?.Invoke();
    }

    // ─── JSON Parsing ─────────────────────────────────────────────────────────
    // JsonUtility doesn't support top-level arrays, so parse manually.
    private static List<QuestStatusDto> ParseQuestList(string json)
    {
        try
        {
            var wrapped = $"{{\"items\":{json}}}";
            var root    = JsonUtility.FromJson<QuestListWrapper>(wrapped);
            return root?.items ?? new List<QuestStatusDto>();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[QuestManager] ParseQuestList error: {ex.Message}\nJson: {json}");
            return new List<QuestStatusDto>();
        }
    }

    // ─── DTOs ─────────────────────────────────────────────────────────────────
    [Serializable]
    public class QuestStatusDto
    {
        public int    quest_id;
        public string name;
        public int    level_need;
        public int    npc_id;
        public string npc_name;                 // tên NPC để hiển thị gợi ý HUD
        public string str1;                     // hội thoại nhận quest
        public string str2;                     // hội thoại nộp quest
        public string str3;                     // ghi chú / hint
        public int    exp_reward;
        public int    gold_reward;
        public int    silver_reward;
        public string item_reward;
        public string status;                   // "available","active","completed","locked"
        public int    current_step_index;
        public string steps_json;               // raw JSON array string
        public string quest_progress_json;      // {"0":3,"1":0} — tiến trình từng bước
        public int    npc_map_id;               // map của NPC nhận thưởng (-1 = không xác định)
        public float  npc_pos_x;               // toạ độ X của NPC
        public float  npc_pos_y;               // toạ độ Y của NPC

        // Convenience helpers
        public bool IsActive    => status == "active";
        public bool IsCompleted => status == "completed";
        public bool IsAvailable => status == "available";
    }

    [Serializable]
    private class QuestListWrapper { public List<QuestStatusDto> items; }
}
