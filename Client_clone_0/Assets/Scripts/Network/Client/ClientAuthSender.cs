using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Client-side: Gửi JWT token và user_id lên server sau khi connect thành công
/// Script này chạy trên NetworkObject do server spawn (AuthSenderNetworkObjectPrefab)
/// </summary>
public class ClientAuthSender : NetworkBehaviour
{
    private static bool hasSentAuth = false;

    /// <summary>
    /// Gửi auth sau khi client connect thành công
    /// Tạo một NetworkObject tạm thời để gửi ServerRpc ngay lập tức
    /// </summary>
    public static void SendAuthAfterConnection(ulong clientId)
    {
        if (hasSentAuth)
        {
            Debug.Log("[ClientAuthSender] Auth already sent, skipping...");
            return;
        }

        string token = PlayerPrefs.GetString("JWT_TOKEN", "");
        int userId = PlayerPrefs.GetInt("USER_ID", 0);

        Debug.Log($"[ClientAuthSender] ===== CLIENT SENDING AUTH =====");
        Debug.Log($"[ClientAuthSender] ClientId: {clientId}");
        Debug.Log($"[ClientAuthSender] UserId: {userId}");
        Debug.Log($"[ClientAuthSender] Token length: {token?.Length ?? 0}");
        Debug.Log($"[ClientAuthSender] Token (first 50 chars): {(token?.Length > 50 ? token.Substring(0, 50) + "..." : token)}");

        if (string.IsNullOrEmpty(token) || userId == 0)
        {
            Debug.LogError("[ClientAuthSender] ✗ JWT_TOKEN or USER_ID not found in PlayerPrefs! Cannot authenticate.");
            Debug.LogError($"[ClientAuthSender] Token empty: {string.IsNullOrEmpty(token)}, UserId: {userId}");
            return;
        }

        // Tìm một ClientAuthSender trên NetworkObject đã spawn sẵn trong scene (do server spawn) để gửi ServerRpc
        Debug.Log("[ClientAuthSender] Looking for existing spawned AuthSenderNetworkObject in scene...");
        ClientAuthSender senderInstance = FindAuthSenderInstance();
        
        if (senderInstance != null)
        {
            Debug.Log($"[ClientAuthSender] ✓ Found existing AuthSenderNetworkObject: {senderInstance.gameObject.name}");
            senderInstance.SendAuthInstance(token, userId);
            hasSentAuth = true;
        }
        else
        {
            // Không tìm thấy AuthSenderNetworkObject đã spawn, đợi và retry
            // Host sẽ spawn AuthSenderNetworkObject khi server start
            Debug.LogWarning("[ClientAuthSender] ✗ Cannot find any AuthSenderNetworkObject to send auth! Waiting and retrying...");
            Debug.LogWarning("[ClientAuthSender] ⚠️ Make sure AuthSenderNetworkObjectPrefab has NetworkObject + ClientAuthSenderComponent and is assigned to authSenderPrefab.");
            
        // Tạo một MonoBehaviour tạm để chạy coroutine retry
        GameObject tempObj = new GameObject("ClientAuthSender_RetryHelper");
        ClientAuthRetryHelper retryHelper = tempObj.AddComponent<ClientAuthRetryHelper>();
        retryHelper.StartRetry(token, userId, clientId);
        // KHÔNG set hasSentAuth = true ở đây, để lần retry thực sự gửi được auth
        }
    }

    /// <summary>
    /// Tìm ClientAuthSender (NetworkBehaviour) trên một NetworkObject đã spawn sẵn
    /// </summary>
    public static ClientAuthSender FindAuthSenderInstance()
    {
        // Tìm tất cả ClientAuthSender trong scene, ưu tiên cái nào NetworkObject đã spawn
        ClientAuthSender[] allSenders = FindObjectsOfType<ClientAuthSender>();
        foreach (var sender in allSenders)
        {
            if (sender == null) continue;

            NetworkObject netObj = sender.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                Debug.Log($"[ClientAuthSender] ✓ Found spawned AuthSenderNetworkObject: {netObj.name}");
                return sender;
            }
        }

        // Chẩn đoán: có thể AuthSenderNetworkObject đã spawn nhưng prefab lại thiếu ClientAuthSender (NetworkBehaviour)
        // => log ra các NetworkObject đang spawn để check nhanh
        NetworkObject[] allNetObjs = FindObjectsOfType<NetworkObject>();
        int spawnedCount = 0;
        for (int i = 0; i < allNetObjs.Length; i++)
        {
            var netObj = allNetObjs[i];
            if (netObj == null || !netObj.IsSpawned) continue;
            spawnedCount++;

            if (netObj.name.Contains("AuthSenderNetworkObject"))
            {
                Debug.LogWarning($"[ClientAuthSender] Found spawned NetworkObject '{netObj.name}' BUT it has NO ClientAuthSender component. Fix prefab: add ClientAuthSender (NetworkBehaviour) to AuthSenderNetworkObjectPrefab.");

                var comps = netObj.GetComponents<Component>();
                if (comps != null)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.Append("[ClientAuthSender] Components on spawned AuthSenderNetworkObject: ");
                    for (int c = 0; c < comps.Length; c++)
                    {
                        if (comps[c] == null) continue;
                        sb.Append(comps[c].GetType().Name);
                        if (c < comps.Length - 1) sb.Append(", ");
                    }
                    Debug.LogWarning(sb.ToString());
                }
            }
        }

        Debug.LogWarning($"[ClientAuthSender] No spawned AuthSenderNetworkObject with ClientAuthSender component found in scene yet. Total spawned NetworkObjects: {spawnedCount}");
        return null;
    }

    /// <summary>
    /// Reset flag (để test lại)
    /// </summary>
    public static void Reset()
    {
        hasSentAuth = false;
    }

    /// <summary>
    /// Instance method để gửi auth (được gọi từ static SendAuthAfterConnection)
    /// </summary>
    public void SendAuthInstance(string token, int userId)
    {
        Debug.Log($"[ClientAuthSender] ===== CLIENT SENDING SERVERRPC =====");
        Debug.Log($"[ClientAuthSender] NetworkObject is spawned: {IsSpawned}");
        Debug.Log($"[ClientAuthSender] OwnerClientId: {OwnerClientId}");
        Debug.Log($"[ClientAuthSender] UserId: {userId}");
        Debug.Log($"[ClientAuthSender] Token length: {token?.Length ?? 0}");

        try
        {
            SendAuthServerRpc(token, userId);
            Debug.Log("[ClientAuthSender] ✓ ServerRpc sent successfully");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ClientAuthSender] ✗ Failed to send ServerRpc: {ex.Message}");
        }
    }

    // RequireOwnership = false để client có thể gọi ServerRpc trên object do server spawn (server-owned).
    // Quan trọng: phải dùng SenderClientId thay vì OwnerClientId để map đúng clientId.
    [ServerRpc(RequireOwnership = false)]
    private void SendAuthServerRpc(string token, int userId, ServerRpcParams rpcParams = default)
    {
        var senderClientId = rpcParams.Receive.SenderClientId;

        Debug.Log($"[ClientAuthSender] ===== SERVER RECEIVED AUTH FROM CLIENT =====");
        Debug.Log($"[ClientAuthSender] SenderClientId: {senderClientId}");
        Debug.Log($"[ClientAuthSender] OwnerClientId (of this NetworkObject): {OwnerClientId}");
        Debug.Log($"[ClientAuthSender] UserId: {userId}");
        Debug.Log($"[ClientAuthSender] Token length: {token?.Length ?? 0}");
        Debug.Log($"[ClientAuthSender] Token (first 50 chars): {(token?.Length > 50 ? token.Substring(0, 50) + "..." : token)}");

        // Verify token và load player data
        if (ServerPlayerDataManager.Instance != null)
        {
            Debug.Log("[ClientAuthSender] ServerPlayerDataManager.Instance found, calling LoadPlayerDataForClient...");
            ServerPlayerDataManager.Instance.LoadPlayerDataForClient(
                senderClientId,
                userId,
                onSuccess: (playerData) =>
                {
                    Debug.Log("[ClientAuthSender] ===== PLAYER DATA LOADED SUCCESSFULLY =====");
                    Debug.Log($"[ClientAuthSender] ClientId: {senderClientId}");
                    Debug.Log($"[ClientAuthSender] UserId: {userId}");
                    Debug.Log($"[ClientAuthSender] Character Name: {playerData.character_name}");
                    Debug.Log($"[ClientAuthSender] Element Type: {playerData.element_type}");
                    Debug.Log($"[ClientAuthSender] Gender: {playerData.gender}");
                    Debug.Log($"[ClientAuthSender] Level: {playerData.level}");
                    Debug.Log($"[ClientAuthSender] Map ID: {playerData.map_id}");
                },
                onError: (error) =>
                {
                    Debug.LogError("[ClientAuthSender] ===== FAILED TO LOAD PLAYER DATA =====");
                    Debug.LogError($"[ClientAuthSender] ClientId: {senderClientId}");
                    Debug.LogError($"[ClientAuthSender] UserId: {userId}");
                    Debug.LogError($"[ClientAuthSender] Error: {error}");
                }
            );
        }
        else
        {
            Debug.LogError("[ClientAuthSender] ✗ ServerPlayerDataManager.Instance is null!");
        }
    }
}

/// <summary>
/// Helper class để retry tìm NetworkObject và gửi auth
/// </summary>
public class ClientAuthRetryHelper : MonoBehaviour
{
    private string token;
    private int userId;
    private ulong clientId;
    private int retryCount = 0;
    private const int maxRetries = 30; // 3 giây
    
    public void StartRetry(string token, int userId, ulong clientId)
    {
        this.token = token;
        this.userId = userId;
        this.clientId = clientId;
        
        // Mỗi lần StartRetry coi như một attempt mới, không phụ thuộc hasSentAuth
        ClientAuthSender.Reset();
        StartCoroutine(RetryFindNetworkObject());
    }

    private System.Collections.IEnumerator RetryFindNetworkObject()
    {
        while (retryCount < maxRetries)
        {
            retryCount++;
            Debug.Log($"[ClientAuthRetryHelper] Retry {retryCount}/{maxRetries}: Looking for spawned NetworkObject...");
            
            ClientAuthSender sender = ClientAuthSender.FindAuthSenderInstance();
            if (sender != null)
            {
                Debug.Log($"[ClientAuthRetryHelper] ✓ Found AuthSenderNetworkObject: {sender.gameObject.name}, sending auth...");
                sender.SendAuthInstance(token, userId);
                Destroy(gameObject); // Xóa helper sau khi xong
                yield break;
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        
        Debug.LogError($"[ClientAuthRetryHelper] ✗ Failed to find NetworkObject after {maxRetries} attempts!");
        Destroy(gameObject);
    }
}
