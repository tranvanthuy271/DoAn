using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

// Gọi REST /api/leaderboard/* trực tiếp từ client (không qua ServerRpc).
// Mỗi category BXH có một ID cố định (khớp với leaderboard_cache trong DB):
// 1 = Cấp Độ | 2 = Nhiệm Vụ | 3 = Chuyên Cần | 4 = Phó Bản | 5 = Vàng
public class LeaderboardService : MonoBehaviour
{
    public static LeaderboardService Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // Category IDs
    public const int CatLevel      = 1;
    public const int CatQuest      = 2;
    public const int CatAttendance = 3;
    public const int CatDungeon    = 4;
    public const int CatGold       = 5;

    // Hàm public để script hoặc hệ thống khác gọi vào.

    public void FetchLevel(int _, Action<LeaderboardEntryDto[]> onDone, Action<string> onError)
        => StartCoroutine(FetchCategoryRoutine(CatLevel, onDone, onError));

    public void FetchQuests(int _, Action<LeaderboardEntryDto[]> onDone, Action<string> onError)
        => StartCoroutine(FetchCategoryRoutine(CatQuest, onDone, onError));

    public void FetchAttendance(int _, Action<LeaderboardEntryDto[]> onDone, Action<string> onError)
        => StartCoroutine(FetchCategoryRoutine(CatAttendance, onDone, onError));

    public void FetchDungeon(int _, int __, Action<LeaderboardEntryDto[]> onDone, Action<string> onError)
        => StartCoroutine(FetchCategoryRoutine(CatDungeon, onDone, onError));

    public void FetchGold(int _, Action<LeaderboardEntryDto[]> onDone, Action<string> onError)
        => StartCoroutine(FetchCategoryRoutine(CatGold, onDone, onError));

    // Lấy bảng xếp hạng theo ID danh mục (1-5).
    public void FetchCategory(int categoryId, Action<LeaderboardEntryDto[]> onDone, Action<string> onError)
        => StartCoroutine(FetchCategoryRoutine(categoryId, onDone, onError));

    // Xử lý nội bộ phục vụ các hàm public.

    private IEnumerator FetchCategoryRoutine(int categoryId,
        Action<LeaderboardEntryDto[]> onDone, Action<string> onError)
    {
        string url = ApiUrl($"leaderboard/{categoryId}");
        { /* Gọi API: {url} */ }

        using var req = UnityWebRequest.Get(url);
        AuthHelper.AddAuthHeader(req);

        yield return req.SendWebRequest();

        { /* Response code={req.responseCode} result={req.result} */ }

        if (req.result != UnityWebRequest.Result.Success)
        {
            { /* Cảnh báo: Lỗi mạng: {req.responseCode}  {req.error} */ }
            onError?.Invoke($"Lỗi mạng: {req.responseCode} – {req.error}");
            yield break;
        }

        string raw = req.downloadHandler.text;
        { /* Raw response ({raw.Length} chars): {raw.Substring(0, Mathf.Min(300, raw.Length))} */ }

        // Server trả về { id, name, list: "[ ... ]" }
        // list là một string JSON lồng bên trong
        var wrapper = JsonUtility.FromJson<LeaderboardCategoryDto>(raw);
        if (wrapper == null)
        {
            { /* Cảnh báo: Parse LeaderboardCategoryDto thất bại (null). Raw: {raw} */ }
            onDone?.Invoke(Array.Empty<LeaderboardEntryDto>());
            yield break;
        }

        { /* wrapper.id={wrapper.id} wrapper.name={wrapper.name} list='{(wrapper.list?.Length > 0 ? wrapper.list.Substring(0, Mathf.Min(100, wrapper.list.Length)) */ }

        if (string.IsNullOrEmpty(wrapper.list) || wrapper.list == "[]")
        {
            { /* list rỗng cho catId={categoryId} */ }
            onDone?.Invoke(Array.Empty<LeaderboardEntryDto>());
            yield break;
        }

        // Parse mảng entries từ list string
        string wrapped = $"{{\"items\":{wrapper.list}}}";
        { /* Wrapped JSON: {wrapped.Substring(0, Mathf.Min(200, wrapped.Length))} */ }

        var result = JsonUtility.FromJson<LeaderboardResponseWrapper>(wrapped);
        int count = result?.items?.Length ?? 0;
        { /* Parse xong: {count} entries cho catId={categoryId} */ }
        onDone?.Invoke(result?.items ?? Array.Empty<LeaderboardEntryDto>());
    }

    private static string ApiUrl(string path)
    {
        var root = ServerAddressConfig.Instance != null
            ? ServerAddressConfig.Instance.ApiRoot
            : "http://localhost:5000";
        return $"{root.TrimEnd('/')}/api/{path}";
    }
}

[System.Serializable]
public class LeaderboardCategoryDto
{
    public int    id;
    public string name;
    public string list;
}
