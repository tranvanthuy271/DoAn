using UnityEngine;
using Unity.Netcode;

// Client-side: Gá»­i JWT token vÃ  user_id lÃªn server sau khi connect thÃ nh cÃ´ng
// CÃ³ thá»ƒ dÃ¹ng nhÆ° NetworkBehaviour (attach vÃ o NetworkObject) hoáº·c MonoBehaviour (gá»i trá»±c tiáº¿p)
public class ClientAuthHandler : NetworkBehaviour
{
    private bool hasSentAuth = false;
    private static ClientAuthHandler instance;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Chá»‰ client owner má»›i gá»­i auth
        if (IsOwner && IsClient)
        {
            SendAuthToServer();
        }
    }

    // Static method Ä‘á»ƒ gá»­i auth tá»« báº¥t ká»³ Ä‘Ã¢u (khÃ´ng cáº§n NetworkObject)
    public static void SendAuthAfterConnection(ulong clientId)
    {
        if (instance != null && instance.hasSentAuth)
        {
            // Debug.Log("[ClientAuthHandler] Auth already sent, skipping...");
            return;
        }

        string token = AuthHelper.GetToken();
        int userId = PlayerPrefs.GetInt("USER_ID", 0);

        if (string.IsNullOrEmpty(token) || userId == 0)
        {
            // Debug.LogError("[ClientAuthHandler] JWT_TOKEN or USER_ID not found in PlayerPrefs! Cannot authenticate.");
            return;
        }

        // Debug.Log($"[ClientAuthHandler] Sending auth to server via static method: userId={userId}, token length={token.Length}");

        // Táº¡o temporary NetworkObject Ä‘á»ƒ gá»­i ServerRpc
        GameObject tempObj = new GameObject("TempAuthHandler");
        ClientAuthHandler handler = tempObj.AddComponent<ClientAuthHandler>();
        instance = handler;
        
        // Spawn NetworkObject Ä‘á»ƒ cÃ³ thá»ƒ gá»­i ServerRpc
        NetworkObject netObj = tempObj.AddComponent<NetworkObject>();
        netObj.SpawnWithOwnership(clientId);
        
        handler.SendAuthToServer();
    }

    // Gá»­i JWT token vÃ  user_id lÃªn server Ä‘á»ƒ verify vÃ  load player data
    private void SendAuthToServer()
    {
        if (hasSentAuth)
        {
            return;
        }

        string token = AuthHelper.GetToken();
        int userId = PlayerPrefs.GetInt("USER_ID", 0);
        int geneSlot = PlayerPrefs.GetInt("ACTIVE_GENE_SLOT", 1);

        if (string.IsNullOrEmpty(token) || userId == 0)
        {
            // Debug.LogError("[ClientAuthHandler] JWT_TOKEN or USER_ID not found in PlayerPrefs! Cannot authenticate.");
            return;
        }

        { /* ==== [GENE2_DEBUG] ClientAuthHandler.SendAuthToServer: ACTIVE_GENE_SLOT = {geneSlot} ==== */ }
        SendAuthServerRpc(token, userId, geneSlot);
        hasSentAuth = true;
    }

    // ServerRpc: Gá»­i token vÃ  user_id lÃªn server
    [ServerRpc(RequireOwnership = true)]
    private void SendAuthServerRpc(string token, int userId, int geneSlot)
    {
        { /* Server received auth from client {OwnerClientId}: userId={userId} geneSlot={geneSlot} */ }

        // Verify token vÃ  load player data
        if (ServerPlayerDataManager.Instance != null)
        {
            ServerPlayerDataManager.Instance.LoadPlayerDataForClient(
                OwnerClientId,
                userId,
                onSuccess: (playerData) =>
                {
                    { /* âœ“ Player data loaded for client {OwnerClientId}: {playerData.character_name} (slot {geneSlot}) */ }
                },
                onError: (error) =>
                {
                    { /* Lỗi: âœ Failed to load player data for client {OwnerClientId}: {error} */ }
                },
                geneSlot: geneSlot
            );
        }
        else
        {
            { /* Lỗi: ServerPlayerDataManager.Instance is null */ }
        }
    }
}

// FORCE_RECOMPILE_082007
// FORCE_RECOMPILE_082012
