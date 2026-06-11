using System;
using UnityEngine;
using Unity.Netcode;

// Client-side: Gửi JWT token và user_id lên server sau khi connect thành công
// Script này chạy trên NetworkObject do server spawn (AuthSenderNetworkObjectPrefab)
public class ClientAuthSender : NetworkBehaviour
{
    private static bool hasSentAuth = false;
    
    // DEPRECATED: These fields were used for Update() polling approach which failed
    // Kept for backward compatibility with retry helper
    internal static bool shouldSendAuth = false;
    internal static string pendingAuthToken = "";
    internal static int pendingAuthUserId = 0;
    internal static int pendingAuthGeneSlot = 1;
    internal static float authScheduledTime = 0f;
    internal static ClientAuthSender pendingAuthInstance = null;
    
    // Debug: Frame counter to track Update() calls (no longer actively used)
    private static int updateFrameCount = 0;

    // Gửi auth sau khi client connect thành công
    // Tạo một NetworkObject tạm thời để gửi ServerRpc ngay lập tức
    public static void SendAuthAfterConnection(ulong clientId)
    {
        if (hasSentAuth)
        {
            { /* Auth already sent, skipping */ }
            return;
        }

        string token = PlayerPrefs.GetString("JWT_TOKEN", "");
        int userId = PlayerPrefs.GetInt("USER_ID", 0);
        int geneSlot = PlayerPrefs.GetInt("ACTIVE_GENE_SLOT", 1);

        // Use LocalClientId for accurate client ID (the callback parameter 'clientId' may be 0 on client side)
        ulong actualClientId = NetworkManager.Singleton?.LocalClientId ?? clientId;

        { /* ===== CLIENT SENDING AUTH ===== */ }
        { /* ==== [GENE2_DEBUG] ClientAuthSender: ACTIVE_GENE_SLOT = {geneSlot} ==== */ }
        { /* Callback ClientId param: {clientId} */ }
        { /* LocalClientId (actual): {actualClientId} */ }
        { /* UserId: {userId} */ }
        { /* GeneSlot: {geneSlot} */ }
        { /* Token length: {token?.Length ?? 0} */ }
        { /* Token (first 50 chars): {(token?.Length > 50 ? token.Substring(0, 50) + */ }

        if (string.IsNullOrEmpty(token) || userId == 0)
        {
            { /* Lỗi: ✗ JWT_TOKEN or USER_ID not found in PlayerPrefs! Cannot authenticate */ }
            { /* Lỗi: Token empty: {string.IsNullOrEmpty(token)}, UserId: {userId} */ }
            return;
        }

        // Tìm một ClientAuthSender trên NetworkObject đã spawn sẵn trong scene (do server spawn) để gửi ServerRpc
        { /* Looking for existing spawned AuthSenderNetworkObject in scene */ }
        ClientAuthSender senderInstance = FindAuthSenderInstance();
        
        if (senderInstance != null)
        {
            { /* ✓ Found existing AuthSenderNetworkObject: {senderInstance.gameObject.name} */ }
            { /* Component enabled: {senderInstance.enabled}, GameObject active: {senderInstance.gameObject.activeInHierarchy} */ }
            { /* Component IsSpawned: {senderInstance.IsSpawned}, NetworkObject: {senderInstance.NetworkObject} */ }
            
            // CRITICAL FIX: Call SendAuthNow() IMMEDIATELY - Update() doesn't work during connection phase
            { /* 🚀 Calling SendAuthNow() IMMEDIATELY (NO DELAY) */ }
            { /* Reason: Update() only runs once during Netcode connection handshake */ }
            
            senderInstance.SendAuthNow(token, userId, geneSlot);
            hasSentAuth = true;
        }
        else
        {
            // Không tìm thấy AuthSenderNetworkObject đã spawn, đợi và retry
            // Host sẽ spawn AuthSenderNetworkObject khi server start
            { /* Cảnh báo: ✗ Cannot find any AuthSenderNetworkObject to send auth! Waiting and retrying */ }
            { /* Cảnh báo: ⚠️ Make sure AuthSenderNetworkObjectPrefab has NetworkObject + ClientAuthSenderComponent and is assigned to authSenderPrefab */ }
            
        // Tạo một MonoBehaviour tạm để chạy coroutine retry
        GameObject tempObj = new GameObject("ClientAuthSender_RetryHelper");
        ClientAuthRetryHelper retryHelper = tempObj.AddComponent<ClientAuthRetryHelper>();
        retryHelper.StartRetry(token, userId, clientId);
        // KHÔNG set hasSentAuth = true ở đây, để lần retry thực sự gửi được auth
        }
    }

    // Tìm ClientAuthSender (NetworkBehaviour) trên một NetworkObject đã spawn sẵn
    public static ClientAuthSender FindAuthSenderInstance()
    {
        // Tìm tất cả ClientAuthSender trong scene, ưu tiên cái nào NetworkObject đã spawn
        ClientAuthSender[] allSenders = FindObjectsOfType<ClientAuthSender>();
        { /* Found {allSenders.Length} ClientAuthSender components in scene */ }
        
        foreach (var sender in allSenders)
        {
            if (sender == null) continue;

            NetworkObject netObj = sender.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                { /* ✓ Found spawned AuthSenderNetworkObject: {netObj.name} */ }
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
                { /* Cảnh báo: Found spawned NetworkObject '{netObj.name}' BUT it has NO ClientAuthSender component. Fix prefab: add ClientAuthSender (NetworkBehaviour) to AuthSenderNetworkObjectPrefab */ }

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
                    { /* Cảnh báo: Ghi nhận: sb.ToString() */ }
                }
            }
        }

        { /* Cảnh báo: No spawned AuthSenderNetworkObject with ClientAuthSender component found in scene yet. Total spawned NetworkObjects: {spawnedCount} */ }
        return null;
    }

    // Reset flag (để test lại)
    public static void Reset()
    {
        hasSentAuth = false;
    }

    // Update() loop - checks for pending auth and sends when time is reached
    // This works where Coroutines/Invoke fail because Update() runs normally during network init
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
                { /* Update() Frame #{updateFrameCount}, Time: {Time.time:F3}, Scheduled: {authScheduledTime:F3}, Remaining: {authScheduledTime - Time.time:F3}s */ }
                { /* enabled: {enabled}, active: {gameObject.activeInHierarchy}, shouldSendAuth: {shouldSendAuth} */ }
            }
        }
        else
        {
            // Log when Update() runs but shouldSendAuth is false
            if (updateFrameCount <= 5)
            {
                { /* Update() Frame #{updateFrameCount} - shouldSendAuth is FALSE (already sent or cleared) */ }
            }
        }
        
        // Debug: Log when Update() is called with pending auth
        if (shouldSendAuth && pendingAuthInstance != null)
        {
            if (updateFrameCount % 10 == 1 || updateFrameCount <= 5)
            {
                { /* This==Pending: {this == pendingAuthInstance}, This ID: {GetInstanceID()}, Pending ID: {pendingAuthInstance.GetInstanceID()} */ }
            }
        }
        
        // Only process on this specific instance
        if (this != pendingAuthInstance)
        {
            // Debug: Log why we're skipping (only first few times)
            if (shouldSendAuth && updateFrameCount <= 5)
            {
                { /* Cảnh báo: ⚠️ Update() SKIPPING - this != pendingAuthInstance! Frame #{updateFrameCount} */ }
                { /* Cảnh báo: this InstanceID={GetInstanceID()}, pendingAuthInstance InstanceID={pendingAuthInstance?.GetInstanceID()} */ }
            }
            return;
        }
        
        if (shouldSendAuth && Time.time >= authScheduledTime)
        {
            { /* \n======================================== */ }
            { /* UPDATE() TRIGGERED - TIME TO SEND AUTH */ }
            { /* Frame #{updateFrameCount}, Time: {Time.time:F3}, Scheduled: {authScheduledTime:F3} */ }
            { /* ========================================\n */ }
            
            // Send immediately
            SendAuthNow(pendingAuthToken, pendingAuthUserId, pendingAuthGeneSlot);
            
            // Clear flags
            shouldSendAuth = false;
            pendingAuthInstance = null;
            hasSentAuth = true;
        }
        else if (shouldSendAuth && updateFrameCount % 10 == 0)
        {
            // Debug: Log every 10 frames to show we're still waiting
            { /* Still waiting... Frame #{updateFrameCount}, Time: {Time.time:F3}, Need: {authScheduledTime:F3}, Remaining: {authScheduledTime - Time.time:F3}s */ }
        }
    }

    // Actually send the ServerRpc - now called IMMEDIATELY (no delay)
    public void SendAuthNow(string token, int userId, int geneSlot = 1)
    {
        { /* ===== SENDING SERVERRPC NOW ===== */ }
        { /* NetworkObject is spawned: {IsSpawned} */ }
        { /* OwnerClientId: {OwnerClientId} */ }
        { /* UserId: {userId} */ }
        { /* Token length: {token.Length} */ }
        { /* GameObject.activeInHierarchy: {gameObject.activeInHierarchy} */ }
        { /* Component enabled: {enabled} */ }
        { /* IsClient: {IsClient}, IsServer: {IsServer} */ }
        { /* LocalClientId: {NetworkManager.Singleton.LocalClientId} */ }

        // Validate state before sending
        if (!IsSpawned)
        {
            { /* Lỗi: NetworkObject no longer spawned */ }
            return;
        }

        if (string.IsNullOrEmpty(token))
        {
            { /* Lỗi: Token is null/empty */ }
            return;
        }

        { /* All validation passed - calling ServerRpc */ }
        { /* Token: {token.Substring(0, Math.Min(30, token.Length))}..., UserId: {userId} */ }

        try
        {
            { /* 📤📤📤 CALLING SendAuthServerRpc() 📤📤📤 */ }
            
            SendAuthServerRpc(token, userId, geneSlot);
            
            { /* ✓✓✓ SendAuthServerRpc() CALLED SUCCESSFULLY ✓✓✓ */ }
            { /* ⚠️ Now check HOST console for 'SERVERRPC RECEIVED ON HOST' message */ }
        }
        catch (System.Exception ex)
        {
            { /* Lỗi: EXCEPTION: {ex.Message} */ }
            { /* Lỗi: Stack trace: {ex.StackTrace} */ }
        }
    }

    // EVOLUTION OF SOLUTIONS:
    // 1. REMOVED: SendAuthWithDelay coroutine - failed to resume after yield during Netcode callbacks
    // 2. REMOVED: Update() loop polling - only executed ONCE then stopped during connection phase
    // 3. CURRENT: SendAuthNow() called IMMEDIATELY without any delay - works reliably!

    // RequireOwnership = false để client có thể gọi ServerRpc trên object do server spawn (server-owned).
    // Quan trọng: phải dùng SenderClientId thay vì OwnerClientId để map đúng clientId.
    [ServerRpc(RequireOwnership = false)]
    private void SendAuthServerRpc(string token, int userId, int geneSlot, ServerRpcParams rpcParams = default)
    {
        { /* \n\n\n */ }
        { /* █████████████████████████████████████████████████████ */ }
        { /* █████████████████████████████████████████████████████ */ }
        { /* ███ 🎯 SERVERRPC RECEIVED ON HOST!!! 🎯 ███ */ }
        { /* █████████████████████████████████████████████████████ */ }
        { /* █████████████████████████████████████████████████████ */ }
        { /* Time: {Time.time} */ }
        { /* Frame: {Time.frameCount} */ }
        { /* Thread: {System.Threading.Thread.CurrentThread.ManagedThreadId} */ }
        { /* IsServer: {IsServer} */ }
        { /* IsClient: {IsClient} */ }
        
        var senderClientId = rpcParams.Receive.SenderClientId;

        { /* \n[HOST/SERVER] ===== PARSING RPC PARAMETERS ===== */ }
        { /* 👤 SenderClientId: {senderClientId} */ }
        { /* 🏠 OwnerClientId (of this NetworkObject): {OwnerClientId} */ }
        { /* 🆔 UserId: {userId} */ }
        { /* 🔑 Token length: {token?.Length ?? 0} */ }
        { /* Token (first 50 chars): {(token?.Length > 50 ? token.Substring(0, 50) + */ }

        // Verify token và load player data
        if (ServerPlayerDataManager.Instance != null)
        {
            { /* ===== CALLING SERVERPLAYERDATAMANAGER ===== */ }
            { /* ServerPlayerDataManager.Instance found, calling LoadPlayerDataForClient */ }
            { /* Parameters - ClientId: {senderClientId}, UserId: {userId} */ }
            
            ServerPlayerDataManager.Instance.LoadPlayerDataForClient(
                senderClientId,
                userId,
                onSuccess: (playerData) =>
                {
                    { /* ===== PLAYER DATA LOADED SUCCESSFULLY (CALLBACK) ===== */ }
                    { /* ✓ SUCCESS CALLBACK TRIGGERED */ }
                    { /* ✓ ClientId: {senderClientId} */ }
                    { /* ✓ UserId: {userId} */ }
                    { /* ✓ Character Name: {playerData.character_name} */ }
                    { /* ✓ Element Type: {playerData.element_type} */ }
                    { /* ✓ Gender: {playerData.gender} */ }
                    { /* ✓ Level: {playerData.level} */ }
                    { /* ✓ Map ID: {playerData.map_id} */ }
                    { /* ===== VERIFYING CACHE AFTER CALLBACK ===== */ }
                    
                    // Verify cache ngay sau khi success callback
                    if (ServerPlayerDataManager.Instance != null)
                    {
                        var verifyData = ServerPlayerDataManager.Instance.GetPlayerDataForClient(senderClientId);
                        if (verifyData != null)
                        {
                            { /* ✓✓✓ CACHE VERIFIED - Data exists for clientId {senderClientId} */ }
                            { /* ✓ Cached Character: {verifyData.character_name} */ }
                        }
                        else
                        {
                            { /* Lỗi: ✗✗✗ CACHE VERIFICATION FAILED - No data found for clientId {senderClientId} after success callback */ }
                        }
                    }
                },
                onError: (error) =>
                {
                    { /* Lỗi: ===== FAILED TO LOAD PLAYER DATA (ERROR CALLBACK) ===== */ }
                    { /* Lỗi: ✗ ERROR CALLBACK TRIGGERED */ }
                    { /* Lỗi: ✗ ClientId: {senderClientId} */ }
                    { /* Lỗi: ✗ UserId: {userId} */ }
                    { /* Lỗi: ✗ Error: {error} */ }
                },
                geneSlot: geneSlot
            );
        }
        else
        {
            { /* Lỗi: ===== SERVERPLAYERDATAMANAGER IS NULL ===== */ }
            { /* Lỗi: ✗ ServerPlayerDataManager.Instance is null */ }
            { /* Lỗi: ✗ Cannot load player data for clientId: {senderClientId}, userId: {userId} */ }
        }
    }
}

// Helper class để retry tìm NetworkObject và gửi auth
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
        // Preserve gene slot that was already stored by SendAuthAfterConnection
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
