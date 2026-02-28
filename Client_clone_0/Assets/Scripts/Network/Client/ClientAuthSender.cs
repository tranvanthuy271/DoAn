using System;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Client-side: Gửi JWT token và user_id lên server sau khi connect thành công
/// Script này chạy trên NetworkObject do server spawn (AuthSenderNetworkObjectPrefab)
/// </summary>
public class ClientAuthSender : NetworkBehaviour
{
    private static bool hasSentAuth = false;
    
    // DEPRECATED: These fields were used for Update() polling approach which failed
    // Kept for backward compatibility with retry helper
    internal static bool shouldSendAuth = false;
    internal static string pendingAuthToken = "";
    internal static int pendingAuthUserId = 0;
    internal static float authScheduledTime = 0f;
    internal static ClientAuthSender pendingAuthInstance = null;
    
    // Debug: Frame counter to track Update() calls (no longer actively used)
    private static int updateFrameCount = 0;

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

        // Use LocalClientId for accurate client ID (the callback parameter 'clientId' may be 0 on client side)
        ulong actualClientId = NetworkManager.Singleton?.LocalClientId ?? clientId;

        Debug.Log($"[ClientAuthSender] ===== CLIENT SENDING AUTH =====");
        Debug.Log($"[ClientAuthSender] Callback ClientId param: {clientId}");
        Debug.Log($"[ClientAuthSender] LocalClientId (actual): {actualClientId}");
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
            Debug.Log($"[ClientAuthSender] Component enabled: {senderInstance.enabled}, GameObject active: {senderInstance.gameObject.activeInHierarchy}");
            Debug.Log($"[ClientAuthSender] Component IsSpawned: {senderInstance.IsSpawned}, NetworkObject: {senderInstance.NetworkObject}");
            
            // CRITICAL FIX: Call SendAuthNow() IMMEDIATELY - Update() doesn't work during connection phase
            Debug.Log("[ClientAuthSender] 🚀 Calling SendAuthNow() IMMEDIATELY (NO DELAY)");
            Debug.Log($"[ClientAuthSender] Reason: Update() only runs once during Netcode connection handshake");
            
            senderInstance.SendAuthNow(token, userId);
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
        Debug.Log($"[ClientAuthSender] Found {allSenders.Length} ClientAuthSender components in scene");
        
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
    /// Update() loop - checks for pending auth and sends when time is reached
    /// This works where Coroutines/Invoke fail because Update() runs normally during network init
    /// </summary>
    // NOTE: This Update() method is NO LONGER USED as of the latest fix.
    // Reason: Update() only executed ONCE during Netcode connection handshake, then stopped completely.
    // Solution: SendAuthNow() is now called IMMEDIATELY without delay in SendAuthAfterConnection().
    // This code is kept for historical reference and debugging purposes.
    private void Update()
    {
        // Debug: ALWAYS increment to track if Update() is being called
        updateFrameCount++;
        
        // Debug: First check if Update() is even being called
        if (shouldSendAuth)
        {
            // Log every 10 frames to see progression
            if (updateFrameCount % 10 == 1 || updateFrameCount <= 5)
            {
                Debug.Log($"[ClientAuthSender] Update() Frame #{updateFrameCount}, Time: {Time.time:F3}, Scheduled: {authScheduledTime:F3}, Remaining: {authScheduledTime - Time.time:F3}s");
                Debug.Log($"[ClientAuthSender] enabled: {enabled}, active: {gameObject.activeInHierarchy}, shouldSendAuth: {shouldSendAuth}");
            }
        }
        else
        {
            // Log when Update() runs but shouldSendAuth is false
            if (updateFrameCount <= 5)
            {
                Debug.Log($"[ClientAuthSender] Update() Frame #{updateFrameCount} - shouldSendAuth is FALSE (already sent or cleared)");
            }
        }
        
        // Debug: Log when Update() is called with pending auth
        if (shouldSendAuth && pendingAuthInstance != null)
        {
            if (updateFrameCount % 10 == 1 || updateFrameCount <= 5)
            {
                Debug.Log($"[ClientAuthSender] This==Pending: {this == pendingAuthInstance}, This ID: {GetInstanceID()}, Pending ID: {pendingAuthInstance.GetInstanceID()}");
            }
        }
        
        // Only process on this specific instance
        if (this != pendingAuthInstance)
        {
            // Debug: Log why we're skipping (only first few times)
            if (shouldSendAuth && updateFrameCount <= 5)
            {
                Debug.LogWarning($"[ClientAuthSender] ⚠️ Update() SKIPPING - this != pendingAuthInstance! Frame #{updateFrameCount}");
                Debug.LogWarning($"[ClientAuthSender] this InstanceID={GetInstanceID()}, pendingAuthInstance InstanceID={pendingAuthInstance?.GetInstanceID()}");
            }
            return;
        }
        
        if (shouldSendAuth && Time.time >= authScheduledTime)
        {
            Debug.Log("\n========================================");
            Debug.Log("🔥🔥🔥 UPDATE() TRIGGERED - TIME TO SEND AUTH! 🔥🔥🔥");
            Debug.Log($"Frame #{updateFrameCount}, Time: {Time.time:F3}, Scheduled: {authScheduledTime:F3}");
            Debug.Log("========================================\n");
            
            // Send immediately
            SendAuthNow(pendingAuthToken, pendingAuthUserId);
            
            // Clear flags
            shouldSendAuth = false;
            pendingAuthInstance = null;
            hasSentAuth = true;
        }
        else if (shouldSendAuth && updateFrameCount % 10 == 0)
        {
            // Debug: Log every 10 frames to show we're still waiting
            Debug.Log($"[ClientAuthSender] Still waiting... Frame #{updateFrameCount}, Time: {Time.time:F3}, Need: {authScheduledTime:F3}, Remaining: {authScheduledTime - Time.time:F3}s");
        }
    }

    /// <summary>
    /// Actually send the ServerRpc - now called IMMEDIATELY (no delay)
    /// </summary>
    public void SendAuthNow(string token, int userId)
    {
        Debug.Log("[ClientAuthSender] ===== SENDING SERVERRPC NOW =====");
        Debug.Log($"[ClientAuthSender] NetworkObject is spawned: {IsSpawned}");
        Debug.Log($"[ClientAuthSender] OwnerClientId: {OwnerClientId}");
        Debug.Log($"[ClientAuthSender] UserId: {userId}");
        Debug.Log($"[ClientAuthSender] Token length: {token.Length}");
        Debug.Log($"[ClientAuthSender] GameObject.activeInHierarchy: {gameObject.activeInHierarchy}");
        Debug.Log($"[ClientAuthSender] Component enabled: {enabled}");
        Debug.Log($"[ClientAuthSender] IsClient: {IsClient}, IsServer: {IsServer}");
        Debug.Log($"[ClientAuthSender] LocalClientId: {NetworkManager.Singleton.LocalClientId}");

        // Validate state before sending
        if (!IsSpawned)
        {
            Debug.LogError("[ClientAuthSender] ❌ NetworkObject no longer spawned!");
            return;
        }

        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("[ClientAuthSender] ❌ Token is null/empty!");
            return;
        }

        Debug.Log($"[ClientAuthSender] ✅ All validation passed - calling ServerRpc");
        Debug.Log($"[ClientAuthSender] Token: {token.Substring(0, Math.Min(30, token.Length))}..., UserId: {userId}");

        try
        {
            Debug.Log("[ClientAuthSender] 📤📤📤 CALLING SendAuthServerRpc() 📤📤📤");
            
            SendAuthServerRpc(token, userId);
            
            Debug.Log("[ClientAuthSender] ✓✓✓ SendAuthServerRpc() CALLED SUCCESSFULLY ✓✓✓");
            Debug.Log("[ClientAuthSender] ⚠️ Now check HOST console for 'SERVERRPC RECEIVED ON HOST' message!");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ClientAuthSender] ❌❌❌ EXCEPTION: {ex.Message}");
            Debug.LogError($"[ClientAuthSender] Stack trace: {ex.StackTrace}");
        }
    }

    // EVOLUTION OF SOLUTIONS:
    // 1. REMOVED: SendAuthWithDelay coroutine - failed to resume after yield during Netcode callbacks
    // 2. REMOVED: Update() loop polling - only executed ONCE then stopped during connection phase  
    // 3. CURRENT: SendAuthNow() called IMMEDIATELY without any delay - works reliably!

    // RequireOwnership = false để client có thể gọi ServerRpc trên object do server spawn (server-owned).
    // Quan trọng: phải dùng SenderClientId thay vì OwnerClientId để map đúng clientId.
    [ServerRpc(RequireOwnership = false)]
    private void SendAuthServerRpc(string token, int userId, ServerRpcParams rpcParams = default)
    {
        Debug.Log("\n\n\n");
        Debug.Log("█████████████████████████████████████████████████████");
        Debug.Log("█████████████████████████████████████████████████████");
        Debug.Log("███ 🎯 SERVERRPC RECEIVED ON HOST!!! 🎯 ███");
        Debug.Log("█████████████████████████████████████████████████████");
        Debug.Log("█████████████████████████████████████████████████████");
        Debug.Log($"[HOST/SERVER] Time: {Time.time}");
        Debug.Log($"[HOST/SERVER] Frame: {Time.frameCount}");
        Debug.Log($"[HOST/SERVER] Thread: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
        Debug.Log($"[HOST/SERVER] IsServer: {IsServer}");
        Debug.Log($"[HOST/SERVER] IsClient: {IsClient}");
        
        var senderClientId = rpcParams.Receive.SenderClientId;

        Debug.Log($"\n[HOST/SERVER] ===== PARSING RPC PARAMETERS =====");
        Debug.Log($"[HOST/SERVER] 👤 SenderClientId: {senderClientId}");
        Debug.Log($"[HOST/SERVER] 🏠 OwnerClientId (of this NetworkObject): {OwnerClientId}");
        Debug.Log($"[HOST/SERVER] 🆔 UserId: {userId}");
        Debug.Log($"[HOST/SERVER] 🔑 Token length: {token?.Length ?? 0}");
        Debug.Log($"[ClientAuthSender] Token (first 50 chars): {(token?.Length > 50 ? token.Substring(0, 50) + "..." : token)}");

        // Verify token và load player data
        if (ServerPlayerDataManager.Instance != null)
        {
            Debug.Log("[ClientAuthSender] ===== CALLING SERVERPLAYERDATAMANAGER =====");
            Debug.Log("[ClientAuthSender] ServerPlayerDataManager.Instance found, calling LoadPlayerDataForClient...");
            Debug.Log($"[ClientAuthSender] Parameters - ClientId: {senderClientId}, UserId: {userId}");
            
            ServerPlayerDataManager.Instance.LoadPlayerDataForClient(
                senderClientId,
                userId,
                onSuccess: (playerData) =>
                {
                    Debug.Log("[ClientAuthSender] ===== PLAYER DATA LOADED SUCCESSFULLY (CALLBACK) =====");
                    Debug.Log($"[ClientAuthSender] ✓ SUCCESS CALLBACK TRIGGERED");
                    Debug.Log($"[ClientAuthSender] ✓ ClientId: {senderClientId}");
                    Debug.Log($"[ClientAuthSender] ✓ UserId: {userId}");
                    Debug.Log($"[ClientAuthSender] ✓ Character Name: {playerData.character_name}");
                    Debug.Log($"[ClientAuthSender] ✓ Element Type: {playerData.element_type}");
                    Debug.Log($"[ClientAuthSender] ✓ Gender: {playerData.gender}");
                    Debug.Log($"[ClientAuthSender] ✓ Level: {playerData.level}");
                    Debug.Log($"[ClientAuthSender] ✓ Map ID: {playerData.map_id}");
                    Debug.Log($"[ClientAuthSender] ===== VERIFYING CACHE AFTER CALLBACK =====");
                    
                    // Verify cache ngay sau khi success callback
                    if (ServerPlayerDataManager.Instance != null)
                    {
                        var verifyData = ServerPlayerDataManager.Instance.GetPlayerDataForClient(senderClientId);
                        if (verifyData != null)
                        {
                            Debug.Log($"[ClientAuthSender] ✓✓✓ CACHE VERIFIED - Data exists for clientId {senderClientId}");
                            Debug.Log($"[ClientAuthSender] ✓ Cached Character: {verifyData.character_name}");
                        }
                        else
                        {
                            Debug.LogError($"[ClientAuthSender] ✗✗✗ CACHE VERIFICATION FAILED - No data found for clientId {senderClientId} after success callback!");
                        }
                    }
                },
                onError: (error) =>
                {
                    Debug.LogError("[ClientAuthSender] ===== FAILED TO LOAD PLAYER DATA (ERROR CALLBACK) =====");
                    Debug.LogError($"[ClientAuthSender] ✗ ERROR CALLBACK TRIGGERED");
                    Debug.LogError($"[ClientAuthSender] ✗ ClientId: {senderClientId}");
                    Debug.LogError($"[ClientAuthSender] ✗ UserId: {userId}");
                    Debug.LogError($"[ClientAuthSender] ✗ Error: {error}");
                }
            );
        }
        else
        {
            Debug.LogError("[ClientAuthSender] ===== SERVERPLAYERDATAMANAGER IS NULL =====");
            Debug.LogError("[ClientAuthSender] ✗ ServerPlayerDataManager.Instance is null!");
            Debug.LogError($"[ClientAuthSender] ✗ Cannot load player data for clientId: {senderClientId}, userId: {userId}");
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
            // Debug.Log($"[ClientAuthRetryHelper] Retry {retryCount}/{maxRetries}: Looking for spawned NetworkObject...");
            
            ClientAuthSender sender = ClientAuthSender.FindAuthSenderInstance();
            if (sender != null)
            {
                // Debug.Log($"[ClientAuthRetryHelper] ✓ Found AuthSenderNetworkObject: {sender.gameObject.name}, sending auth...");
                
                // Schedule auth send via Update() loop instead of calling removed method
                ClientAuthSender.pendingAuthToken = token;
                ClientAuthSender.pendingAuthUserId = userId;
                ClientAuthSender.pendingAuthInstance = sender;
                ClientAuthSender.authScheduledTime = Time.time + 0.1f; // 0.1s delay
                ClientAuthSender.shouldSendAuth = true;
                
                Destroy(gameObject); // Xóa helper sau khi xong
                yield break;
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        
        // Debug.LogError($"[ClientAuthRetryHelper] ✗ Failed to find NetworkObject after {maxRetries} attempts!");
        Destroy(gameObject);
    }
}
