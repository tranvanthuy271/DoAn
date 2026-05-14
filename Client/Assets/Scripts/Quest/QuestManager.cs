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
        if (ok) StartCoroutine(LoadQuestListRoutine(0, null)); // refresh state
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
        if (ok) StartCoroutine(LoadQuestListRoutine(0, null)); // refresh state
        onDone?.Invoke(ok, msg);
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
        public int    id;
        public string name;
        public string description;
        public int    level_need;
        public int    npc_giver_id;
        public int    npc_receiver_id;
        public string status;               // "available","active","completed","locked"
        public int    current_step_index;
        public string progress_json;
        public string steps_json;           // raw JSON from server
        public string rewards_json;         // raw JSON from server

        // Convenience helpers
        public bool IsActive    => status == "active";
        public bool IsCompleted => status == "completed";
    }

    [Serializable]
    private class QuestListWrapper { public List<QuestStatusDto> items; }
}
