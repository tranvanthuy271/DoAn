using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Client-side: Gửi JWT token và user_id lên server sau khi connect thành công
/// Có thể dùng như NetworkBehaviour (attach vào NetworkObject) hoặc MonoBehaviour (gọi trực tiếp)
/// </summary>
public class ClientAuthHandler : NetworkBehaviour
{
    private bool hasSentAuth = false;
    private static ClientAuthHandler instance;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Chỉ client owner mới gửi auth
        if (IsOwner && IsClient)
        {
            SendAuthToServer();
        }
    }

    /// <summary>
    /// Static method để gửi auth từ bất kỳ đâu (không cần NetworkObject)
    /// </summary>
    public static void SendAuthAfterConnection(ulong clientId)
    {
        if (instance != null && instance.hasSentAuth)
        {
            Debug.Log("[ClientAuthHandler] Auth already sent, skipping...");
            return;
        }

        string token = PlayerPrefs.GetString("JWT_TOKEN", "");
        int userId = PlayerPrefs.GetInt("USER_ID", 0);

        if (string.IsNullOrEmpty(token) || userId == 0)
        {
            Debug.LogError("[ClientAuthHandler] JWT_TOKEN or USER_ID not found in PlayerPrefs! Cannot authenticate.");
            return;
        }

        Debug.Log($"[ClientAuthHandler] Sending auth to server via static method: userId={userId}, token length={token.Length}");

        // Tạo temporary NetworkObject để gửi ServerRpc
        GameObject tempObj = new GameObject("TempAuthHandler");
        ClientAuthHandler handler = tempObj.AddComponent<ClientAuthHandler>();
        instance = handler;
        
        // Spawn NetworkObject để có thể gửi ServerRpc
        NetworkObject netObj = tempObj.AddComponent<NetworkObject>();
        netObj.SpawnWithOwnership(clientId);
        
        handler.SendAuthToServer();
    }

    /// <summary>
    /// Gửi JWT token và user_id lên server để verify và load player data
    /// </summary>
    private void SendAuthToServer()
    {
        if (hasSentAuth)
        {
            return;
        }

        string token = PlayerPrefs.GetString("JWT_TOKEN", "");
        int userId = PlayerPrefs.GetInt("USER_ID", 0);

        if (string.IsNullOrEmpty(token) || userId == 0)
        {
            Debug.LogError("[ClientAuthHandler] JWT_TOKEN or USER_ID not found in PlayerPrefs! Cannot authenticate.");
            return;
        }

        Debug.Log($"[ClientAuthHandler] Sending auth to server: userId={userId}, token length={token.Length}");
        SendAuthServerRpc(token, userId);
        hasSentAuth = true;
    }

    /// <summary>
    /// ServerRpc: Gửi token và user_id lên server
    /// </summary>
    [ServerRpc(RequireOwnership = true)]
    private void SendAuthServerRpc(string token, int userId)
    {
        Debug.Log($"[ClientAuthHandler] Server received auth from client {OwnerClientId}: userId={userId}");

        // Verify token và load player data
        if (ServerPlayerDataManager.Instance != null)
        {
            ServerPlayerDataManager.Instance.LoadPlayerDataForClient(
                OwnerClientId,
                userId,
                onSuccess: (playerData) =>
                {
                    Debug.Log($"[ClientAuthHandler] ✓ Player data loaded for client {OwnerClientId}: {playerData.character_name}");
                },
                onError: (error) =>
                {
                    Debug.LogError($"[ClientAuthHandler] ✗ Failed to load player data for client {OwnerClientId}: {error}");
                }
            );
        }
        else
        {
            Debug.LogError("[ClientAuthHandler] ServerPlayerDataManager.Instance is null!");
        }
    }
}
