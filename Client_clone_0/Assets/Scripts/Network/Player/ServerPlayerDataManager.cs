using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;

/// <summary>
/// Server-side manager: Load player data từ API cho mỗi client khi connect
/// Map clientId -> userId và lưu player data trong memory
/// Query DB khi nhận userid từ client
/// </summary>
public class ServerPlayerDataManager : NetworkBehaviour
{
    public static ServerPlayerDataManager Instance { get; private set; }

    [Header("API Client (Server-side)")]
    private APIClient apiClient;

    // Dictionary để map clientId -> userId
    private Dictionary<ulong, int> clientIdToUserId = new Dictionary<ulong, int>();

    // Dictionary để cache player data theo userId
    private Dictionary<int, PlayerDataResponse> playerDataCache = new Dictionary<int, PlayerDataResponse>();

    // Dictionary để map clientId -> PlayerDataResponse (để truy cập nhanh)
    private Dictionary<ulong, PlayerDataResponse> clientIdToPlayerData = new Dictionary<ulong, PlayerDataResponse>();

    // Cache riêng cho gene slot 2
    private Dictionary<int, PlayerDataResponse> playerData2Cache = new Dictionary<int, PlayerDataResponse>();

    // Ghi nhớ gene slot cuối cùng đã load thành công cho mỗi client (để tránh slot 1 overwrite slot 2)
    private Dictionary<ulong, int> clientIdToGeneSlot = new Dictionary<ulong, int>();

    // Dictionary để map clientId -> JWT token (để dùng khi sync DB cho client đúng token)
    private Dictionary<ulong, string> clientIdToJwt = new Dictionary<ulong, string>();

    /// <summary>Lưu JWT token của client khi họ gửi auth</summary>
    public void StoreClientJwt(ulong clientId, string jwt)
    {
        if (!string.IsNullOrEmpty(jwt))
            clientIdToJwt[clientId] = jwt;
    }

    /// <summary>Lấy JWT token đã lưu của client. Trả về chuỗi rỗng nếu chưa có.</summary>
    public string GetClientJwt(ulong clientId)
    {
        return clientIdToJwt.TryGetValue(clientId, out var jwt) ? jwt : "";
    }

    private void Awake()
    {
        if (FindObjectOfType<MapWorldBootstrap>() != null)
        {
            Debug.Log("[ServerPlayerDataManager] MapWorldBootstrap detected — disabling legacy player data manager.");
            enabled = false;
            return;
        }

        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            Debug.Log("[ServerPlayerDataManager] Creating new instance with DontDestroyOnLoad");
            
            // QUAN TRỌNG: Tạo APIClient NGAY trong Awake() 
            // Vì OnClientConnected có thể được trigger trước Start()
            InitializeAPIClient();
        }
        else
        {
            Debug.Log("[ServerPlayerDataManager] Instance already exists, destroying duplicate");
            
            // QUAN TRỌNG: Trước khi destroy, đảm bảo Instance có APIClient
            if (Instance.apiClient == null)
            {
                Debug.LogWarning("[ServerPlayerDataManager] Existing instance has null APIClient, initializing now");
                Instance.InitializeAPIClient();
            }
            
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Khởi tạo APIClient để query player data từ server API
    /// </summary>
    private void InitializeAPIClient()
    {
        if (apiClient == null)
        {
            Debug.Log("[ServerPlayerDataManager] Initializing APIClient...");
            
            // QUAN TRỌNG: Ưu tiên dùng APIClient.Instance đã có (đã có token từ login)
            if (APIClient.Instance != null)
            {
                Debug.Log("[ServerPlayerDataManager] Using existing APIClient.Instance (has token from login)");
                apiClient = APIClient.Instance;
                
                string token = apiClient.GetToken();
                Debug.Log($"[ServerPlayerDataManager] ✓ APIClient has token: {(!string.IsNullOrEmpty(token) ? "YES" : "NO")}, length: {token?.Length ?? 0}");
            }
            else
            {
                Debug.Log("[ServerPlayerDataManager] No existing APIClient.Instance, creating new one...");
                GameObject apiClientObj = new GameObject("APIClient_Server");
                apiClient = apiClientObj.AddComponent<APIClient>();
                DontDestroyOnLoad(apiClientObj);
                Debug.Log("[ServerPlayerDataManager] ✓ New APIClient created");
            }
        }
        else
        {
            Debug.Log("[ServerPlayerDataManager] APIClient already exists, skipping initialization");
        }
    }

    private void Start()
    {
        // Verify APIClient is ready after scene load
        if (apiClient == null)
        {
            Debug.LogWarning("[ServerPlayerDataManager] APIClient is null in Start(), re-initializing...");
            InitializeAPIClient();
        }
        else
        {
            Debug.Log("[ServerPlayerDataManager] ✓ APIClient verified in Start()");
        }
    }

    /// <summary>
    /// Load player data từ API cho client vừa connect.
    /// geneSlot=1 → player_data, geneSlot=2 → player2_data.
    /// </summary>
    public void LoadPlayerDataForClient(ulong clientId, int userId, Action<PlayerDataResponse> onSuccess, Action<string> onError, int geneSlot = 1)
    {
        Debug.Log($"[ServerPlayerDataManager] ===== LOADING PLAYER DATA FOR CLIENT =====");
        Debug.Log($"[ServerPlayerDataManager] ClientId: {clientId}, UserId: {userId}, GeneSlot: {geneSlot}");

        // Chọn cache theo gene slot
        var cache = geneSlot == 2 ? playerData2Cache : playerDataCache;

        // Check cache trước
        if (cache.ContainsKey(userId))
        {
            PlayerDataResponse cachedData = cache[userId];
            clientIdToUserId[clientId] = userId;
            // Priority guard trong cache path
            int existingSlot = clientIdToGeneSlot.GetValueOrDefault(clientId, 0);
            if (geneSlot >= existingSlot)
            {
                clientIdToGeneSlot[clientId] = geneSlot;
                clientIdToPlayerData[clientId] = cachedData;
            }
            Debug.Log($"[ServerPlayerDataManager] ✓ Using CACHED player data (slot {geneSlot}) for userId: {userId}");
            onSuccess?.Invoke(cachedData);
            return;
        }

        // Load từ API (Query DB)
        if (apiClient == null)
        {
            Debug.LogWarning("[ServerPlayerDataManager] ⚠️ APIClient is null, initializing now...");
            InitializeAPIClient();
            
            if (apiClient == null)
            {
                Debug.LogError("[ServerPlayerDataManager] ✗ Failed to initialize APIClient! Cannot load player data.");
                onError?.Invoke("APIClient initialization failed");
                return;
            }
        }

        string endpoint = geneSlot == 2 ? $"/api/player/{userId}/data2" : $"/api/player/{userId}/data";
        Debug.Log($"[ServerPlayerDataManager] Querying API (slot {geneSlot}): {endpoint}");

        Action<int, Action<PlayerDataResponse>, Action<string>> loadFunc =
            geneSlot == 2
                ? apiClient.LoadPlayer2Data
                : apiClient.LoadPlayerData;

        var cacheToUse = geneSlot == 2 ? playerData2Cache : playerDataCache;

        loadFunc(
            userId,
            (playerData) =>
            {
                Debug.Log($"[ServerPlayerDataManager] ✓ Loaded slot {geneSlot} data for userId {userId}: {playerData.character_name} ({playerData.element_type})");

                // Cache data
                cacheToUse[userId] = playerData;
                clientIdToUserId[clientId] = userId;

                // Priority guard: slot 2 không bao giờ bị overwrite bởi slot 1
                int existingSlot = clientIdToGeneSlot.GetValueOrDefault(clientId, 0);
                if (geneSlot >= existingSlot)
                {
                    clientIdToGeneSlot[clientId] = geneSlot;
                    clientIdToPlayerData[clientId] = playerData;
                    Debug.Log($"[ServerPlayerDataManager] ✓ clientIdToPlayerData updated: clientId={clientId} slot={geneSlot} element={playerData.element_type}");
                }
                else
                {
                    Debug.LogWarning($"[ServerPlayerDataManager] ⚠ Skipped overwrite: slot {geneSlot} tried to overwrite existing slot {existingSlot} for clientId={clientId}");
                }

                onSuccess?.Invoke(playerData);
            },
            (error) =>
            {
                Debug.LogError($"[ServerPlayerDataManager] ✗ Failed to load slot {geneSlot} data for userId {userId}: {error}");
                onError?.Invoke(error);
            }
        );
    }


    [ContextMenu("Log all player data")]
    public void LogAllPlayerData()
    {
        Debug.Log($"[ServerPlayerDataManager] ===== LOGGING ALL PLAYER DATA =====");
        foreach (var kvp in clientIdToPlayerData)
        {
            ulong clientId = kvp.Key;
            PlayerDataResponse data = kvp.Value;
            Debug.Log($"ClientId: {clientId} => Character: {data.character_name}, Element: {data.element_type}, Gender: {data.gender}");
        }
    }


    /// <summary>
    /// Get player data cho clientId (từ cache)
    /// </summary>
    public PlayerDataResponse GetPlayerDataForClient(ulong clientId)
    {
        Debug.Log($"[ServerPlayerDataManager] GetPlayerDataForClient called for clientId: {clientId}");
        
        // Log full cache state for debugging
        Debug.Log($"[ServerPlayerDataManager] Current clientIdToPlayerData cache:");
        foreach (var kvp in clientIdToPlayerData)
        {
            Debug.Log($"  ClientId: {kvp.Key} => PlayerData: {(kvp.Value != null ? kvp.Value.character_name : "null")}");
        }
        
        if (clientIdToPlayerData.ContainsKey(clientId))
        {
            Debug.Log($"[ServerPlayerDataManager] ✓ Found player data for clientId {clientId}");
            return clientIdToPlayerData[clientId];
        }
        
        Debug.Log($"[ServerPlayerDataManager] ✗ No player data found for clientId {clientId}");
        return null;
    }

    /// <summary>
    /// Alias for GetPlayerDataForClient (for better naming consistency)
    /// </summary>
    public PlayerDataResponse GetPlayerDataByClientId(ulong clientId)
    {
        return GetPlayerDataForClient(clientId);
    }

    /// <summary>
    /// Get userId từ clientId
    /// </summary>
    public int GetUserIdFromClientId(ulong clientId)
    {
        if (clientIdToUserId.ContainsKey(clientId))
        {
            return clientIdToUserId[clientId];
        }
        return 0;
    }

    /// <summary>
    /// Update player data cache (khi có thay đổi như level up, equip item, etc.)
    /// </summary>
    public void UpdatePlayerDataCache(int userId, PlayerDataResponse newData)
    {
        if (playerDataCache.ContainsKey(userId))
        {
            playerDataCache[userId] = newData;
            // Debug.Log($"[ServerPlayerDataManager] Updated player data cache for userId: {userId}");
        }

        // Update trong clientIdToPlayerData
        foreach (var kvp in clientIdToUserId)
        {
            if (kvp.Value == userId)
            {
                clientIdToPlayerData[kvp.Key] = newData;
            }
        }
    }

    /// <summary>
    /// Remove player data khi client disconnect
    /// </summary>
    public void RemovePlayerData(ulong clientId)
    {
        if (clientIdToUserId.ContainsKey(clientId))
        {
            int userId = clientIdToUserId[clientId];
            clientIdToUserId.Remove(clientId);
            clientIdToPlayerData.Remove(clientId);

            // Không xóa cache (có thể dùng lại nếu reconnect)
            // Debug.Log($"[ServerPlayerDataManager] Removed player data mapping for clientId: {clientId}, userId: {userId}");
        }
    }

    /// <summary>
    /// Clear all data (khi server shutdown)
    /// </summary>
    public void ClearAllData()
    {
        clientIdToUserId.Clear();
        clientIdToPlayerData.Clear();
        playerDataCache.Clear();
        // Debug.Log("[ServerPlayerDataManager] Cleared all player data");
    }
}
