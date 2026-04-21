using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;

public static class DungeonRewardGrantService
{
    public static IEnumerator GrantRewardsToClient(ulong clientId, IReadOnlyList<DungeonRewardItemConfig> rewards)
    {
        if (rewards == null || rewards.Count == 0)
            yield break;

        int targetPlayerId = ResolveTargetPlayerId(clientId);
        if (targetPlayerId <= 0)
        {
            Debug.LogWarning($"[DungeonRewardGrantService] Không resolve được playerId cho client {clientId}.");
            yield break;
        }

        string apiRoot = ServerAddressConfig.Instance.ApiRoot.TrimEnd('/');
        string zoneApiKey = ZoneRoomRegistry.Instance?.Config?.GetZoneApiKey();
        string jwt = ResolveJwt(clientId);

        string bodyJson;
        string url;

        using var req = new UnityWebRequest();
        req.method = UnityWebRequest.kHttpVerbPOST;
        req.downloadHandler = new DownloadHandlerBuffer();

        if (!string.IsNullOrWhiteSpace(zoneApiKey))
        {
            url = apiRoot + "/api/dungeonreward/grant";
            bodyJson = BuildZoneGrantBody(targetPlayerId, rewards);
            req.SetRequestHeader("X-Zone-Api-Key", zoneApiKey);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(jwt))
            {
                Debug.LogWarning($"[DungeonRewardGrantService] Thiếu JWT để phát thưởng cho client {clientId}.");
                yield break;
            }

            url = $"{apiRoot}/api/player/{targetPlayerId}/inventory/add";
            bodyJson = BuildInventoryAddBody(rewards);
            req.SetRequestHeader("Authorization", $"Bearer {jwt}");
        }

        req.url = url;
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(bodyJson));
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            Debug.LogError($"[DungeonRewardGrantService] Phát thưởng thất bại cho playerId={targetPlayerId}: {req.error} | {req.downloadHandler.text}");
    }

    private static int ResolveTargetPlayerId(ulong clientId)
    {
        string sessionPlayerId = ZonePlayerSessionManager.Instance?.GetPlayerId(clientId);
        if (int.TryParse(sessionPlayerId, out int parsed) && parsed > 0)
            return parsed;

        if (NetworkManager.Singleton != null && clientId == NetworkManager.Singleton.LocalClientId)
        {
            if (GameManager.Instance?.currentPlayerData != null && GameManager.Instance.currentPlayerData.user_id > 0)
                return GameManager.Instance.currentPlayerData.user_id;

            return PlayerPrefs.GetInt("USER_ID", 0);
        }

        return 0;
    }

    private static string ResolveJwt(ulong clientId)
    {
        string sessionJwt = ZonePlayerSessionManager.Instance?.GetClientJwt(clientId);
        if (!string.IsNullOrWhiteSpace(sessionJwt))
            return sessionJwt;

        if (NetworkManager.Singleton != null && clientId == NetworkManager.Singleton.LocalClientId)
            return APIClient.Instance != null ? APIClient.Instance.GetToken() : PlayerPrefs.GetString("JWT_TOKEN", string.Empty);

        return string.Empty;
    }

    private static string BuildZoneGrantBody(int playerId, IReadOnlyList<DungeonRewardItemConfig> rewards)
    {
        return $"{{\"targetPlayerId\":{playerId},\"items\":{BuildItemsJson(rewards)}}}";
    }

    private static string BuildInventoryAddBody(IReadOnlyList<DungeonRewardItemConfig> rewards)
    {
        return $"{{\"items\":{BuildItemsJson(rewards)}}}";
    }

    private static string BuildItemsJson(IReadOnlyList<DungeonRewardItemConfig> rewards)
    {
        var builder = new StringBuilder();
        builder.Append('[');

        for (int i = 0; i < rewards.Count; i++)
        {
            DungeonRewardItemConfig reward = rewards[i];
            if (reward == null || reward.itemTemplateId <= 0 || reward.quantity <= 0)
                continue;

            if (builder.Length > 1)
                builder.Append(',');

            builder.Append('{')
                .Append("\"itemTemplateId\":").Append(reward.itemTemplateId)
                .Append(',')
                .Append("\"quantity\":").Append(reward.quantity);

            if (reward.upgradeLevel > 0)
                builder.Append(',').Append("\"upgradeLevel\":").Append(reward.upgradeLevel);

            if (!string.IsNullOrWhiteSpace(reward.strOptions))
                builder.Append(',').Append("\"strOptions\":\"").Append(EscapeJson(reward.strOptions)).Append("\"");

            builder.Append('}');
        }

        builder.Append(']');
        return builder.ToString();
    }

    private static string EscapeJson(string value)
    {
        return string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}