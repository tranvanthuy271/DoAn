using UnityEngine;

/// <summary>
/// Singleton client-side cache cho skill data của player.
///
/// Flow:
///   1. Server gọi PushSkillsToClient() ngay sau khi spawn player.
///   2. Client nhận qua OnInitialSkillsReceived → cache ở đây.
///   3. SkillTabUI đọc từ cache thay vì gọi GetPlayerSkillsServerRpc() mỗi lần mở tab.
///   4. Sau khi nâng skill, SkillTabUI invalidate cache → gọi RPC → OnSkillsReceived cập nhật lại.
/// </summary>
public class PlayerSkillCache : UnityEngine.MonoBehaviour
{
    public static PlayerSkillCache Instance { get; private set; }

    public PlayerSkillsResponse CachedData { get; private set; }
    public bool HasData => CachedData != null && CachedData.skills != null && CachedData.skills.Length > 0;

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoBootstrap()
    {
        if (Instance != null) return;
        var go = new UnityEngine.GameObject("PlayerSkillCache [auto]");
        go.AddComponent<PlayerSkillCache>();
        UnityEngine.Object.DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        GameplayCommandService.OnInitialSkillsReceived += HandleInitialSkills;
        GameplayCommandService.OnSkillsReceived        += HandleSkillsRefresh;
    }

    private void OnDestroy()
    {
        GameplayCommandService.OnInitialSkillsReceived -= HandleInitialSkills;
        GameplayCommandService.OnSkillsReceived        -= HandleSkillsRefresh;
        if (Instance == this) Instance = null;
    }

    private void HandleInitialSkills(string json)
    {
        if (string.IsNullOrEmpty(json) || json.Contains("\"error\""))
        {
            UnityEngine.Debug.LogWarning("[PlayerSkillCache] HandleInitialSkills: nhận json lỗi, bỏ qua.");
            return;
        }
        try
        {
            var data = UnityEngine.JsonUtility.FromJson<PlayerSkillsResponse>(json);
            if (data != null && data.skills != null)
            {
                CachedData = data;
                UnityEngine.Debug.Log($"[PlayerSkillCache] Cached {data.skills.Length} skill(s) từ server push.");
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"[PlayerSkillCache] Parse lỗi OnInitialSkillsReceived: {ex.Message}");
        }
    }

    private void HandleSkillsRefresh(string json)
    {
        if (string.IsNullOrEmpty(json) || json.Contains("\"error\""))
            return;
        try
        {
            var data = UnityEngine.JsonUtility.FromJson<PlayerSkillsResponse>(json);
            if (data != null && data.skills != null)
            {
                CachedData = data;
                UnityEngine.Debug.Log($"[PlayerSkillCache] Cache cập nhật sau OnSkillsReceived: {data.skills.Length} skill(s).");
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"[PlayerSkillCache] Parse lỗi OnSkillsReceived: {ex.Message}");
        }
    }

    /// <summary>Xóa cache — gọi khi logout hoặc cần force-reload.</summary>
    public void Invalidate()
    {
        CachedData = null;
        UnityEngine.Debug.Log("[PlayerSkillCache] Cache đã bị xóa (invalidate).");
    }
}
