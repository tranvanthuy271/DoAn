using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Netcode;

// NPC Action handler — chạy trên SERVER, xử lý từng action_type khi người chơi chọn menu.
// Luồng:
// NpcInteraction.SelectMenuItemServerRpc → NpcAction.Execute(action_type, clientId, ...)
// → kiểm tra điều kiện (items, gold) → gọi API → lưu DB → gửi ActionResultClientRpc về client
// Giống LangLa NPC_Action.java: mỗi action kiểm tra điều kiện rồi gọi API tương ứng.
public static class NpcAction
{
    private const string LogPrefix = "[NpcAction]";

    // Entry point

    // Gọi từ NpcInteraction.SelectMenuItemServerRpc.
    // owner = MonoBehaviour dùng để StartCoroutine (NpcInteraction instance).
    public static void Execute(
        string actionType,
        NpcData npcData,
        ulong clientId,
        NpcInteraction owner)
    {
        switch (actionType.ToLowerInvariant())
        {
            // Các action này đã có pipeline riêng trong NpcInteraction:
            case "open_shop":
            case "open_blacksmith":
            case "open_dungeon":
            case "close":
                // Không xử lý ở đây — NpcInteraction.SelectMenuItemServerRpc xử lý
                Debug.LogWarning($"{LogPrefix} Execute: '{actionType}' should be handled by NpcInteraction, not NpcAction.", owner);
                break;

            case "reset_potential":
                owner.StartCoroutine(CallApiAction(clientId, npcData.npc_id, "reset-potential", owner));
                break;

            case "reset_skill":
                owner.StartCoroutine(CallApiAction(clientId, npcData.npc_id, "reset-skill", owner));
                break;

            case "learn_skill":
                owner.StartCoroutine(CallApiAction(clientId, npcData.npc_id, "learn-skill", owner));
                break;

            case "exchange_skill":
                owner.StartCoroutine(CallApiAction(clientId, npcData.npc_id, "exchange-skill", owner));
                break;

            case "exchange_charm":
                owner.StartCoroutine(CallApiAction(clientId, npcData.npc_id, "exchange-charm", owner));
                break;

            case "lock_level":
                owner.StartCoroutine(CallApiAction(clientId, npcData.npc_id, "lock-level", owner));
                break;

            default:
                Debug.LogWarning($"{LogPrefix} Unknown action_type='{actionType}' for npcId={npcData.npc_id}");
                owner.SendActionResultRpc(clientId, false, $"Chức năng '{actionType}' chưa được hỗ trợ.", null);
                break;
        }
    }

    // API call

    private static IEnumerator CallApiAction(
        ulong clientId,
        int npcId,
        string apiAction,
        NpcInteraction owner)
    {
        int    playerId = NpcInteraction.ResolveClientUserIdStatic(clientId);
        string jwt      = NpcInteraction.ResolveClientJwtStatic(clientId);
        string apiBase  = NpcServerManager.Instance?.ApiBase ?? ServerAddressConfig.Instance.ApiRoot;
        string url      = $"{apiBase}/api/npc/action/{apiAction}";

        string bodyJson = $"{{\"playerId\":{playerId},\"npcId\":{npcId}}}";
        Debug.Log($"{LogPrefix} POST {url}  body={bodyJson}  client={clientId}");

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(bodyJson));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.timeout         = 10;
        req.SetRequestHeader("Content-Type", "application/json");
        if (!string.IsNullOrEmpty(jwt))
            req.SetRequestHeader("Authorization", $"Bearer {jwt}");

        yield return req.SendWebRequest();

        string responseText = req.downloadHandler?.text ?? "";
        Debug.Log($"{LogPrefix} Response [{req.responseCode}]: {responseText}");

        NpcActionResponse resp = null;
        try
        {
            resp = JsonUtility.FromJson<NpcActionResponse>(responseText);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"{LogPrefix} Parse response failed: {ex.Message}");
        }

        if (resp == null)
            resp = new NpcActionResponse { success = false, message = req.downloadHandler?.text ?? "Lỗi kết nối." };

        // success=false nếu request thất bại
        if (req.result != UnityWebRequest.Result.Success && resp.success)
            resp.success = false;

        owner.SendActionResultRpc(clientId, resp.success, resp.message, JsonUtility.ToJson(resp.playerData));
    }

    // DTO

    [Serializable]
    public class NpcActionResponse
    {
        public bool   success;
        public string message;
        public NpcActionPlayerData playerData;
    }

    [Serializable]
    public class NpcActionPlayerData
    {
        public int gold;
        public int silver;
        public int skillPoints;
        public int potentialPoints;
        public int level;
    }
}
