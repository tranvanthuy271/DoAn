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
    /// Load player data từ API cho client vừa connect
    /// Được gọi từ ClientAuthSenderComponent khi client gửi userid lên host
    /// Query DB để lấy player data dựa trên userId
    /// </summary>
    public void LoadPlayerDataForClient(ulong clientId, int userId, Action<PlayerDataResponse> onSuccess, Action<string> onError)
    {
        Debug.Log($"[ServerPlayerDataManager] ===== LOADING PLAYER DATA FOR CLIENT =====");
        Debug.Log($"[ServerPlayerDataManager] ClientId: {clientId}");
        Debug.Log($"[ServerPlayerDataManager] UserId: {userId}");
        Debug.Log($"[ServerPlayerDataManager] Current cache state - Total cached users: {playerDataCache.Count}");
        Debug.Log($"[ServerPlayerDataManager] Current clientIdToPlayerData mappings: {clientIdToPlayerData.Count}");

        // Check cache trước
        if (playerDataCache.ContainsKey(userId))
        {
            PlayerDataResponse cachedData = playerDataCache[userId];
            clientIdToUserId[clientId] = userId;
            clientIdToPlayerData[clientId] = cachedData;
            Debug.Log($"[ServerPlayerDataManager] ✓ Using CACHED player data for userId: {userId}");
            Debug.Log($"[ServerPlayerDataManager] ✓ Cached data - Character: {cachedData.character_name}, Element: {cachedData.element_type}, Gender: {cachedData.gender}");
            Debug.Log($"[ServerPlayerDataManager] ✓ Successfully mapped clientId {clientId} -> userId {userId}");
            Debug.Log($"[ServerPlayerDataManager] ✓ Cache verification - clientIdToPlayerData[{clientId}] exists: {clientIdToPlayerData.ContainsKey(clientId)}");
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

        Debug.Log($"[ServerPlayerDataManager] Querying ServerAPI for userId: {userId}...");
        Debug.Log($"[ServerPlayerDataManager] API Endpoint: /api/player/{userId}/data");

        apiClient.LoadPlayerData(
            userId,
            onSuccess: (playerData) =>
            {
                Debug.Log($"[ServerPlayerDataManager] ===== PLAYER DATA LOADED FROM API =====");
                Debug.Log($"[ServerPlayerDataManager] ✓ API Response received for userId: {userId}");
                Debug.Log($"[ServerPlayerDataManager] ✓ ClientId: {clientId}");
                Debug.Log($"[ServerPlayerDataManager] ✓ Character Name: {playerData.character_name}");
                Debug.Log($"[ServerPlayerDataManager] ✓ Element Type: {playerData.element_type}");
                Debug.Log($"[ServerPlayerDataManager] ✓ Gender: {playerData.gender}");
                Debug.Log($"[ServerPlayerDataManager] ✓ Level: {playerData.level}");
                Debug.Log($"[ServerPlayerDataManager] ✓ Map ID: {playerData.map_id}");

                // Cache data
                playerDataCache[userId] = playerData;
                clientIdToUserId[clientId] = userId;
                clientIdToPlayerData[clientId] = playerData;

                Debug.Log($"[ServerPlayerDataManager] ===== CACHING PLAYER DATA =====");
                Debug.Log($"[ServerPlayerDataManager] ✓ playerDataCache[{userId}] = PlayerData ({playerData.character_name})");
                Debug.Log($"[ServerPlayerDataManager] ✓ clientIdToUserId[{clientId}] = {userId}");
                Debug.Log($"[ServerPlayerDataManager] ✓ clientIdToPlayerData[{clientId}] = PlayerData ({playerData.character_name})");
                Debug.Log($"[ServerPlayerDataManager] ===== VERIFY CACHE =====");
                Debug.Log($"[ServerPlayerDataManager] ✓ playerDataCache contains userId {userId}: {playerDataCache.ContainsKey(userId)}");
                Debug.Log($"[ServerPlayerDataManager] ✓ clientIdToPlayerData contains clientId {clientId}: {clientIdToPlayerData.ContainsKey(clientId)}");
                Debug.Log($"[ServerPlayerDataManager] ✓ Total cached users: {playerDataCache.Count}");
                Debug.Log($"[ServerPlayerDataManager] ✓ Total clientId mappings: {clientIdToPlayerData.Count}");
                Debug.Log($"[ServerPlayerDataManager] ✓ Player data successfully cached and mapped to clientId: {clientId}");
                onSuccess?.Invoke(playerData);
            },
            onError: (error) =>
            {
                Debug.LogError($"[ServerPlayerDataManager] ===== FAILED TO LOAD PLAYER DATA FROM API =====");
                Debug.LogError($"[ServerPlayerDataManager] UserId: {userId}");
                Debug.LogError($"[ServerPlayerDataManager] ClientId: {clientId}");
                Debug.LogError($"[ServerPlayerDataManager] Error: {error}");
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
