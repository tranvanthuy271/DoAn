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

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Tạo APIClient nếu chưa có (server cần gọi API để load player data)
        if (apiClient == null)
        {
            GameObject apiClientObj = new GameObject("APIClient_Server");
            apiClient = apiClientObj.AddComponent<APIClient>();
            DontDestroyOnLoad(apiClientObj);
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

        // Check cache trước
        if (playerDataCache.ContainsKey(userId))
        {
            PlayerDataResponse cachedData = playerDataCache[userId];
            clientIdToUserId[clientId] = userId;
            clientIdToPlayerData[clientId] = cachedData;
            Debug.Log($"[ServerPlayerDataManager] ✓ Using CACHED player data for userId: {userId}");
            Debug.Log($"[ServerPlayerDataManager] Cached data - Character: {cachedData.character_name}, Element: {cachedData.element_type}, Gender: {cachedData.gender}");
            onSuccess?.Invoke(cachedData);
            return;
        }

        // Load từ API (Query DB)
        if (apiClient == null)
        {
            Debug.LogError("[ServerPlayerDataManager] ✗ APIClient is null! Cannot load player data.");
            onError?.Invoke("APIClient not initialized");
            return;
        }

        Debug.Log($"[ServerPlayerDataManager] Querying ServerAPI for userId: {userId}...");
        Debug.Log($"[ServerPlayerDataManager] API Endpoint: /api/player/{userId}/data");
        
        apiClient.LoadPlayerData(
            userId,
            onSuccess: (playerData) =>
            {
                Debug.Log($"[ServerPlayerDataManager] ===== PLAYER DATA LOADED FROM API =====");
                Debug.Log($"[ServerPlayerDataManager] ✓ API Response received for userId: {userId}");
                Debug.Log($"[ServerPlayerDataManager] Character Name: {playerData.character_name}");
                Debug.Log($"[ServerPlayerDataManager] Element Type: {playerData.element_type}");
                Debug.Log($"[ServerPlayerDataManager] Gender: {playerData.gender}");
                Debug.Log($"[ServerPlayerDataManager] Level: {playerData.level}");
                Debug.Log($"[ServerPlayerDataManager] Map ID: {playerData.map_id}");
                
                // Cache data
                playerDataCache[userId] = playerData;
                clientIdToUserId[clientId] = userId;
                clientIdToPlayerData[clientId] = playerData;

                Debug.Log($"[ServerPlayerDataManager] ✓ Player data cached and mapped to clientId: {clientId}");
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

    /// <summary>
    /// Get player data cho clientId (từ cache)
    /// </summary>
    public PlayerDataResponse GetPlayerDataForClient(ulong clientId)
    {
        if (clientIdToPlayerData.ContainsKey(clientId))
        {
            return clientIdToPlayerData[clientId];
        }
        return null;
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
            Debug.Log($"[ServerPlayerDataManager] Updated player data cache for userId: {userId}");
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
            Debug.Log($"[ServerPlayerDataManager] Removed player data mapping for clientId: {clientId}, userId: {userId}");
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
        Debug.Log("[ServerPlayerDataManager] Cleared all player data");
    }
}
