using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


// QuestManager — Singleton quản lý trạng thái nhiệm vụ phía client.
// Giữ ActiveQuest của người chơi và phát sự kiện khi trạng thái thay đổi.
public class QuestManager : MonoBehaviour
{
    // Singleton
    public static QuestManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // DontDestroyOnLoad chỉ hoạt động trên root GameObject — tách khỏi parent nếu có
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    // State
    public QuestStatusDto ActiveQuest   { get; private set; }
    public List<QuestStatusDto> AllQuests { get; private set; } = new();
    // Quest cần hiển thị trên HUD: active nếu có, không thì quest available đầu tiên.
    public QuestStatusDto HintQuest     { get; private set; }

    // Phát khi danh sách / tiến trình nhiệm vụ thay đổi.
    public event Action OnQuestListChanged;

    // API helpers
    private static string ApiUrl(string path)
    {
        var root = ServerAddressConfig.Instance != null
            ? ServerAddressConfig.Instance.ApiRoot
            : "http://localhost:5000";
        return $"{root.TrimEnd('/')}/api/{path}";
    }

    private static string JwtToken => PlayerPrefs.GetString("JWT_TOKEN", "");

    // Hàm public để script hoặc hệ thống khác gọi vào.

    // Tải danh sách quest từ NPC (hoặc trạng thái hiện tại khi npcId=0).
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

    // Tải trạng thái tổng hợp: quest đang active hoặc quest available đầu tiên.
    // Dùng để cập nhật HintQuest cho QuestHudWidget.
    public void RefreshPlayerOverview(Action onDone = null)
    {
        StartCoroutine(LoadPlayerOverviewRoutine(onDone));
    }

    // Coroutines

    private IEnumerator LoadQuestListRoutine(int npcId, Action<List<QuestStatusDto>> onDone)
    {
        string url = npcId > 0 ? ApiUrl($"quest/list?npcId={npcId}") : ApiUrl("quest/list");
        using var req = UnityWebRequest.Get(url);
        req.SetRequestHeader("Authorization", $"Bearer {JwtToken}");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            { /* Cảnh báo: LoadQuests failed: {req.error}  {req.downloadHandler?.text} */ }
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
        string responseText = req.downloadHandler?.text ?? string.Empty;
        string msg = ExtractApiMessage(responseText, req.error);
        if (ok)
        {
            StartCoroutine(LoadQuestListRoutine(0, null)); // background: cập nhật AllQuests
            yield return StartCoroutine(LoadPlayerOverviewRoutine(null)); // chờ HintQuest cập nhật
        }
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
        string responseText = req.downloadHandler?.text ?? string.Empty;
        string msg = ExtractApiMessage(responseText, req.error);
        if (ok)
        {
            ShowQuestCompletionNotification(ParseCompleteResponse(responseText));
            RefreshInventoryAfterQuestComplete();
            StartCoroutine(LoadQuestListRoutine(0, null)); // background: cập nhật AllQuests
            yield return StartCoroutine(LoadPlayerOverviewRoutine(null)); // chờ HintQuest cập nhật
        }
        onDone?.Invoke(ok, msg);
    }

    private IEnumerator LoadPlayerOverviewRoutine(Action onDone)
    {
        using var req = UnityWebRequest.Get(ApiUrl("quest/player-overview"));
        req.SetRequestHeader("Authorization", $"Bearer {JwtToken}");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            { /* Cảnh báo: PlayerOverview failed: {req.error} */ }
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
            { /* PlayerOverview parsed: id={dto?.quest_id} name={dto?.name} status={dto?.status} progress={dto?.quest_progress_json} stepIdx={dto?.current_step_index} */ }
            HintQuest = dto;
            if (dto?.status == "active") ActiveQuest = dto;
        }
        catch (Exception ex)
        {
            { /* Cảnh báo: ParsePlayerOverview error: {ex.Message} */ }
        }

        { /* OnQuestListChanged.Invoke()  subscribers={OnQuestListChanged?.GetInvocationList()?.Length ?? 0} */ }
        OnQuestListChanged?.Invoke();
        onDone?.Invoke();
    }

    // JSON Parsing
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
            { /* Cảnh báo: ParseQuestList error: {ex.Message}\nJson: {json} */ }
            return new List<QuestStatusDto>();
        }
    }

    private static string ExtractApiMessage(string responseText, string fallback = "")
    {
        if (string.IsNullOrWhiteSpace(responseText))
            return string.IsNullOrWhiteSpace(fallback) ? string.Empty : fallback;

        string trimmed = responseText.Trim();
        if (!trimmed.StartsWith("{"))
            return trimmed;

        try
        {
            var envelope = JsonUtility.FromJson<ApiEnvelope>(trimmed);
            if (!string.IsNullOrEmpty(envelope?.error))
                return envelope.error;
            if (!string.IsNullOrEmpty(envelope?.message))
                return envelope.message;
        }
        catch (Exception ex)
        {
            { /* Cảnh báo: ExtractApiMessage parse failed: {ex.Message} */ }
        }

        return trimmed;
    }

    private static QuestCompleteResponse ParseCompleteResponse(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
            return null;

        try
        {
            return JsonUtility.FromJson<QuestCompleteResponse>(responseText);
        }
        catch (Exception ex)
        {
            { /* Cảnh báo: ParseCompleteResponse error: {ex.Message}\nJson: {responseText} */ }
            return null;
        }
    }

    private static void ShowQuestCompletionNotification(QuestCompleteResponse response)
    {
        if (response == null)
            return;

        var lines = new List<string>();
        if (!string.IsNullOrEmpty(response.message))
            lines.Add(response.message);
        if (response.reward_exp > 0)
            lines.Add($"+{response.reward_exp} EXP");
        if (response.reward_gold > 0)
            lines.Add($"+{response.reward_gold} vàng");
        if (response.reward_silver > 0)
            lines.Add($"+{response.reward_silver} bạc");
        if (response.reward_items != null && response.reward_items.Length > 0)
        {
            lines.Add("Vật phẩm nhận được:");
            foreach (var item in response.reward_items)
            {
                if (item == null || item.quantity <= 0) continue;
                string itemName = string.IsNullOrEmpty(item.item_name) ? $"Item #{item.item_template_id}" : item.item_name;
                lines.Add($"- {itemName} x{item.quantity}");
            }
        }

        if (lines.Count > 0)
            GlobalNotificationUI.Show(string.Join("\n", lines), "Nhận thưởng", 4f, "Đóng");
    }

    private static void RefreshInventoryAfterQuestComplete()
    {
        var bridge = FindObjectOfType<InventoryNetworkBridge>(true);
        if (bridge != null)
            bridge.RefreshInventoryFromDB();
    }

    // DTOs
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
        public string npc_map_name;             // tên map của NPC nhận / nộp quest
        public float  npc_pos_x;               // toạ độ X của NPC
        public float  npc_pos_y;               // toạ độ Y của NPC

        // Convenience helpers
        public bool IsActive    => status == "active";
        public bool IsCompleted => status == "completed";
        public bool IsAvailable => status == "available";
    }

    [Serializable]
    private class ApiEnvelope
    {
        public string message;
        public string error;
    }

    [Serializable]
    private class QuestCompleteResponse
    {
        public string message;
        public int reward_exp;
        public int reward_gold;
        public int reward_silver;
        public string item_reward;
        public QuestRewardItemDto[] reward_items;
    }

    [Serializable]
    private class QuestRewardItemDto
    {
        public int item_template_id;
        public string item_name;
        public int quantity;
    }

    [Serializable]
    private class QuestListWrapper { public List<QuestStatusDto> items; }
}
