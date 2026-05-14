using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Fire-and-forget helper: báo cáo tiến trình nhiệm vụ lên REST API.
/// Gọi từ server-side Unity code (NetworkEnemyHealth, NetworkInventory).
///
/// Dùng ZoneApiKey để xác thực (giống DungeonRewardGrantService).
/// Gọi QuestProgressReporter.Report(monoBehaviour, ...) — cần 1 MonoBehaviour
/// để StartCoroutine (vì UnityWebRequest cần coroutine context).
/// </summary>
public static class QuestProgressReporter
{
    public enum ProgressType { Kill, Collect, Talk }

    /// <summary>
    /// Báo cáo sự kiện lên /api/quest/progress.
    /// type: "kill" | "collect" | "talk"
    /// targetId: enemy_id, item_template_id, hoặc npc_id tương ứng
    /// </summary>
    public static void Report(MonoBehaviour host, int playerId, ProgressType type, int targetId, int delta = 1)
    {
        if (host == null || playerId <= 0) return;
        host.StartCoroutine(ReportCoroutine(playerId, type.ToString().ToLower(), targetId, delta));
    }

    private static IEnumerator ReportCoroutine(int playerId, string type, int targetId, int delta)
    {
        string baseUrl = ServerAddressConfig.Instance != null
            ? ServerAddressConfig.Instance.ApiUrl
            : "http://localhost:5000/api";

        string url = $"{baseUrl}/quest/progress-by-event";

        var body = new ProgressEventBody
        {
            playerId = playerId,
            type     = type,
            targetId = targetId,
            delta    = delta,
        };
        string json    = JsonUtility.ToJson(body);
        byte[] payload = Encoding.UTF8.GetBytes(json);

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler   = new UploadHandlerRaw(payload);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        // Zone API key (server-to-server auth)
        string apiKey = ZoneRoomRegistry.Instance?.Config?.GetZoneApiKey();
        if (!string.IsNullOrWhiteSpace(apiKey))
            req.SetRequestHeader("X-Zone-Api-Key", apiKey);

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
            Debug.Log($"[QuestProgress] OK playerId={playerId} type={type} targetId={targetId}: {req.downloadHandler.text}");
        else
            Debug.LogWarning($"[QuestProgress] FAIL playerId={playerId} type={type} targetId={targetId}: {req.error} {req.downloadHandler?.text}");
    }

    [System.Serializable]
    private class ProgressEventBody
    {
        public int    playerId;
        public string type;
        public int    targetId;
        public int    delta;
    }
}
