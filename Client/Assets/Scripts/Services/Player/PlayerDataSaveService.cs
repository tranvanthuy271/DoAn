using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

// Service để batch save player data changes lên API Server
// Lưu theo batch mỗi X giây để tránh quá tải database
public class PlayerDataSaveService : NetworkBehaviour
{
    public static PlayerDataSaveService Instance { get; private set; }

    [Header("Save Settings")]
    [Tooltip("Thời gian giữa mỗi lần batch save (giây)")]
    public float saveInterval = 5f;

    private Queue<PlayerDataChange> saveQueue = new Queue<PlayerDataChange>();
    private float saveTimer = 0f;
    private bool isSaving = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Chỉ server mới save
        if (!IsServer) return;

        saveTimer += Time.deltaTime;
        if (saveTimer >= saveInterval && saveQueue.Count > 0 && !isSaving)
        {
            BatchSaveToAPI();
            saveTimer = 0f;
        }
    }

    // Thêm change vào queue để save sau
    public void QueueSave(PlayerDataChange change)
    {
        if (change == null) return;

        saveQueue.Enqueue(change);
        // Debug.Log($"[PlayerDataSaveService] Queued save: {change.FieldName} = {change.Value} (Type: {change.Type})");
    }

    // Thêm nhiều changes cùng lúc
    public void QueueSaves(List<PlayerDataChange> changes)
    {
        if (changes == null || changes.Count == 0) return;

        foreach (var change in changes)
        {
            QueueSave(change);
        }
    }

    // Batch save tất cả changes trong queue lên API
    private void BatchSaveToAPI()
    {
        if (saveQueue.Count == 0) return;
        if (isSaving) return; // Đang save, bỏ qua

        isSaving = true;

        // Combine all changes thành dictionary
        var combinedChanges = new Dictionary<string, object>();

        int changeCount = saveQueue.Count;
        while (saveQueue.Count > 0)
        {
            var change = saveQueue.Dequeue();
            
            // Merge values nếu cùng field (ưu tiên value mới nhất)
            if (combinedChanges.ContainsKey(change.FieldName))
            {
                combinedChanges[change.FieldName] = change.Value;
            }
            else
            {
                combinedChanges[change.FieldName] = change.Value;
            }
        }

        // Debug.Log($"[PlayerDataSaveService] Batch saving {changeCount} changes...");

        // Lấy playerId từ GameManager hoặc ServerPlayerDataManager
        int playerId = GetPlayerId();
        if (playerId == 0)
        {
            // Debug.LogWarning("[PlayerDataSaveService] Cannot save: PlayerId is 0");
            isSaving = false;
            return;
        }

        // Gửi lên API
        combinedChanges["geneSlot"] = PlayerPrefs.GetInt("ACTIVE_GENE_SLOT", 1) == 2 ? 2 : 1;

        if (true) // Hybrid: always use direct coroutine
        {
            // Tạo JSON từ combinedChanges
            string jsonData = CreateJsonFromChanges(combinedChanges);

            // Gọi API update
            StartCoroutine(UpdatePlayerDataCoroutine(playerId, jsonData));
        }
    }

    // Lấy playerId từ GameManager hoặc ServerPlayerDataManager
    private int GetPlayerId()
    {
        // Ưu tiên: Lấy từ GameManager (local player)
        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
        {
            var playerData = GameManager.Instance.GetPlayerData();
            if (playerData != null && playerData.player_id > 0)
            {
                return playerData.player_id;
            }
        }

        // Fallback: Lấy từ ServerPlayerDataManager (server-side)
        if (ServerPlayerDataManager.Instance != null)
        {
            // Lấy clientId của server (host)
            var networkManager = NetworkManager.Singleton;
            if (networkManager != null && networkManager.IsServer)
            {
                ulong serverClientId = networkManager.LocalClientId;
                int userId = ServerPlayerDataManager.Instance.GetUserIdFromClientId(serverClientId);
                if (userId > 0)
                {
                    return userId;
                }
            }
        }

        return 0;
    }

    // Tạo JSON string từ dictionary changes
    private string CreateJsonFromChanges(Dictionary<string, object> changes)
    {
        // Sử dụng JsonUtility (đơn giản) hoặc Newtonsoft.Json (nếu có)
        System.Text.StringBuilder json = new System.Text.StringBuilder();
        json.Append("{");

        int count = 0;
        foreach (var kvp in changes)
        {
            if (count > 0) json.Append(",");

            json.Append($"\"{kvp.Key}\":");

            // Format value theo type
            if (kvp.Value is string)
            {
                json.Append($"\"{kvp.Value}\"");
            }
            else if (kvp.Value is bool)
            {
                json.Append(kvp.Value.ToString().ToLower());
            }
            else
            {
                json.Append(kvp.Value);
            }

            count++;
        }

        json.Append("}");
        return json.ToString();
    }

    // Coroutine để gửi update request lên API
    private System.Collections.IEnumerator UpdatePlayerDataCoroutine(int playerId, string jsonData)
    {
        string baseUrl = ServerAddressConfig.Instance != null ? ServerAddressConfig.Instance.ApiUrl : "http://localhost:3000/api";
        string url = $"{baseUrl}/player/{playerId}/data";
        
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Put(url, jsonData))
        {
            www.SetRequestHeader("Content-Type", "application/json");
            
            string token = APIClient.Instance != null ? APIClient.Instance.GetToken() : AuthHelper.GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                www.SetRequestHeader("Authorization", $"Bearer {token}");
            }

            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                // Debug.Log($"[PlayerDataSaveService] ✓ Batch save successful for player {playerId}");
            }
            else
            {
                // Debug.LogError($"[PlayerDataSaveService] ✗ Batch save failed: {www.error}");
                // Có thể implement retry logic ở đây
            }
        }

        isSaving = false;
    }

    // Force save ngay lập tức (không đợi interval)
    public void ForceSave()
    {
        if (!IsServer) return;
        if (saveQueue.Count == 0) return;

        saveTimer = saveInterval; // Trigger save ngay
    }

    // Clear queue (khi cần reset)
    public void ClearQueue()
    {
        saveQueue.Clear();
        // Debug.Log("[PlayerDataSaveService] Save queue cleared");
    }

    // Get queue count (để debug)
    public int GetQueueCount()
    {
        return saveQueue.Count;
    }
}

// Class để đại diện cho một thay đổi dữ liệu player
[System.Serializable]
public class PlayerDataChange
{
    public string FieldName;
    public object Value;
    public ChangeType Type;

    public PlayerDataChange(string fieldName, object value, ChangeType type)
    {
        FieldName = fieldName;
        Value = value;
        Type = type;
    }
}

// Loại thay đổi dữ liệu
public enum ChangeType
{
    LevelUp,
    ExperienceGain,
    ItemAcquired,
    ItemLost,
    QuestCompleted,
    CurrencyChange,
    StatChange,
    PositionUpdate,
    Other
}
