using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Quản lý danh sách bạn bè: tải, gửi lời mời, chấp nhận, xóa bạn, tìm kiếm.
/// </summary>
public class FriendManager : MonoBehaviour
{
    public static FriendManager Instance { get; private set; }

    private const string ChatManagerResourcePath = "Prefabs/Chat/ChatManager";

    // ── Events ────────────────────────────────────────────────────────────────

    public event Action<List<FriendEntryDto>> OnFriendListLoaded;
    public event Action<string>               OnError;
    public event Action                       OnRequestSent;

    // ── Cache ─────────────────────────────────────────────────────────────────

    public List<FriendEntryDto> Friends { get; } = new List<FriendEntryDto>();
    public bool HasLoadedFriends { get; private set; }

    // ── MonoBehaviour ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (transform.parent != null)
            transform.SetParent(null, true);

        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log($"[FriendManager] Awake root='{gameObject.name}' active={gameObject.activeInHierarchy} scene='{gameObject.scene.name}'");
    }

    // Store delegate so we can properly unsubscribe later
    private System.Action<PlayerDataResponse> _onPlayerDataSet;

    private void Start()
    {
        _onPlayerDataSet = _ => LoadFriends();
        GameManager.OnPlayerDataSet += _onPlayerDataSet;

        if (HasToken())
        {
            Debug.Log("[FriendManager] Start found JWT token. Priming friend cache.");
            LoadFriends();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        Debug.Log($"[FriendManager] OnDestroy root='{gameObject.name}'");
        GameManager.OnPlayerDataSet -= _onPlayerDataSet;
    }

    public static FriendManager EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        var existing = FindObjectOfType<FriendManager>(includeInactive: true);
        if (existing != null)
        {
            Instance = existing;

            if (existing.transform.parent != null)
                existing.transform.SetParent(null, true);

            if (!existing.gameObject.activeSelf)
                existing.gameObject.SetActive(true);

            DontDestroyOnLoad(existing.gameObject);
            Debug.Log($"[FriendManager] EnsureInstance resolved existing scene object '{existing.gameObject.name}' active={existing.gameObject.activeInHierarchy} scene='{existing.gameObject.scene.name}'");
            return Instance;
        }

        var chatManager = FindObjectOfType<ChatManager>(includeInactive: true);
        if (chatManager != null)
        {
            Instance = chatManager.GetComponent<FriendManager>();
            if (Instance == null)
                Instance = chatManager.gameObject.AddComponent<FriendManager>();

            if (chatManager.transform.parent != null)
                chatManager.transform.SetParent(null, true);

            if (!chatManager.gameObject.activeSelf)
                chatManager.gameObject.SetActive(true);

            DontDestroyOnLoad(chatManager.gameObject);
            Debug.Log($"[FriendManager] EnsureInstance attached to existing ChatManager '{chatManager.gameObject.name}'.");
            return Instance;
        }

        var prefab = Resources.Load<GameObject>(ChatManagerResourcePath);
        if (prefab != null)
        {
            var instanceGo = Instantiate(prefab);
            instanceGo.name = prefab.name;
            Instance = instanceGo.GetComponent<FriendManager>();
            Debug.Log($"[FriendManager] EnsureInstance instantiated prefab '{ChatManagerResourcePath}' -> hasFriendManager={Instance != null}");
            return Instance;
        }

        var go = new GameObject("FriendManager [Auto]");
        Instance = go.AddComponent<FriendManager>();
        Debug.LogWarning("[FriendManager] EnsureInstance created standalone fallback GameObject because ChatManager prefab was not found.");
        return Instance;
    }

    // ── API Calls ─────────────────────────────────────────────────────────────

    public void LoadFriends()
    {
        string token = PlayerPrefs.GetString("JWT_TOKEN", "");
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogWarning("[FriendManager] LoadFriends skipped because JWT_TOKEN is empty.");
            return;
        }

        Debug.Log("[FriendManager] LoadFriends requested.");
        StartCoroutine(LoadFriendsRoutine(token));
    }

    private IEnumerator LoadFriendsRoutine(string token)
    {
        using var req = UnityWebRequest.Get(ApiUrl("friends"));
        req.SetRequestHeader("Authorization", $"Bearer {token}");
        Debug.Log($"[FriendManager] GET {req.url}");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            string err = BuildApiError("LoadFriends", req);
            Debug.LogWarning($"[FriendManager] {err}");
            OnError?.Invoke(err);
            yield break;
        }

        var json = req.downloadHandler.text;
        // Parse array → wrap in object for JsonUtility
        var wrapped = WrapArray(json);
        var response = JsonUtility.FromJson<FriendArrayWrapper>(wrapped);

        Friends.Clear();
        if (response?.items != null) Friends.AddRange(response.items);
        HasLoadedFriends = true;

        int accepted = 0;
        int pendingReceived = 0;
        int pendingSent = 0;
        foreach (var entry in Friends)
        {
            switch (entry.status)
            {
                case "accepted":
                    accepted++;
                    break;
                case "pending_received":
                    pendingReceived++;
                    break;
                case "pending_sent":
                    pendingSent++;
                    break;
            }
        }

        Debug.Log($"[FriendManager] LoadFriends success count={Friends.Count} accepted={accepted} pendingReceived={pendingReceived} pendingSent={pendingSent}");
        OnFriendListLoaded?.Invoke(Friends);
    }

    public void SendFriendRequest(int targetUserId, Action onSuccess = null)
    {
        string token = PlayerPrefs.GetString("JWT_TOKEN", "");
        Debug.Log($"[FriendManager] SendFriendRequest targetUserId={targetUserId}");
        StartCoroutine(SendRequestRoutine(token, targetUserId, onSuccess));
    }

    private IEnumerator SendRequestRoutine(string token, int targetUserId, Action onSuccess)
    {
        var body    = JsonUtility.ToJson(new SendFriendReqPayload { TargetUserId = targetUserId });
        var bodyRaw = System.Text.Encoding.UTF8.GetBytes(body);

        using var req = new UnityWebRequest(ApiUrl("friends/request"), "POST");
        req.uploadHandler   = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type",  "application/json");
        req.SetRequestHeader("Authorization", $"Bearer {token}");
        Debug.Log($"[FriendManager] POST {req.url} body={body}");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            string err = BuildApiError("SendFriendRequest", req);
            Debug.LogWarning($"[FriendManager] {err}");
            OnError?.Invoke(err);
        }
        else
        {
            Debug.Log($"[FriendManager] SendFriendRequest success response={req.downloadHandler.text}");
            OnRequestSent?.Invoke();
            onSuccess?.Invoke();
            LoadFriends(); // refresh
        }
    }

    public void AcceptFriendRequest(int relationId, Action onSuccess = null)
    {
        string token = PlayerPrefs.GetString("JWT_TOKEN", "");
        Debug.Log($"[FriendManager] AcceptFriendRequest relationId={relationId}");
        StartCoroutine(AcceptRoutine(token, relationId, onSuccess));
    }

    private IEnumerator AcceptRoutine(string token, int relationId, Action onSuccess)
    {
        using var req = new UnityWebRequest(ApiUrl($"friends/{relationId}/accept"), "PUT");
        req.downloadHandler = new DownloadHandlerBuffer();
        req.uploadHandler   = new UploadHandlerRaw(Array.Empty<byte>());
        req.SetRequestHeader("Authorization", $"Bearer {token}");
        Debug.Log($"[FriendManager] PUT {req.url}");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            string err = BuildApiError("AcceptFriendRequest", req);
            Debug.LogWarning($"[FriendManager] {err}");
            OnError?.Invoke(err);
        }
        else
        {
            Debug.Log($"[FriendManager] AcceptFriendRequest success relationId={relationId} response={req.downloadHandler.text}");
            onSuccess?.Invoke();
            LoadFriends();
        }
    }

    public void RemoveFriend(int relationId, Action onSuccess = null)
    {
        string token = PlayerPrefs.GetString("JWT_TOKEN", "");
        Debug.Log($"[FriendManager] RemoveFriend relationId={relationId}");
        StartCoroutine(RemoveRoutine(token, relationId, onSuccess));
    }

    private IEnumerator RemoveRoutine(string token, int relationId, Action onSuccess)
    {
        using var req = UnityWebRequest.Delete(ApiUrl($"friends/{relationId}"));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Authorization", $"Bearer {token}");
        Debug.Log($"[FriendManager] DELETE {req.url}");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            string err = BuildApiError("RemoveFriend", req);
            Debug.LogWarning($"[FriendManager] {err}");
            OnError?.Invoke(err);
        }
        else
        {
            Debug.Log($"[FriendManager] RemoveFriend success relationId={relationId} response={req.downloadHandler.text}");
            onSuccess?.Invoke();
            LoadFriends();
        }
    }

    public void SearchUsers(string query, Action<List<UserSearchResult>> onResult)
    {
        string token = PlayerPrefs.GetString("JWT_TOKEN", "");
        Debug.Log($"[FriendManager] SearchUsers query='{query}'");
        StartCoroutine(SearchRoutine(token, query, onResult));
    }

    private IEnumerator SearchRoutine(string token, string q, Action<List<UserSearchResult>> onResult)
    {
        using var req = UnityWebRequest.Get(ApiUrl($"friends/search?q={Uri.EscapeUriString(q)}"));
        req.SetRequestHeader("Authorization", $"Bearer {token}");
        Debug.Log($"[FriendManager] GET {req.url}");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            string err = BuildApiError("SearchUsers", req);
            Debug.LogWarning($"[FriendManager] {err}");
            OnError?.Invoke(err);
            yield break;
        }

        var json     = req.downloadHandler.text;
        var wrapped  = WrapArray(json, "results");
        var response = JsonUtility.FromJson<UserSearchWrapper>(wrapped);
        var list     = new List<UserSearchResult>(response?.results ?? Array.Empty<UserSearchResult>());
        Debug.Log($"[FriendManager] SearchUsers success query='{q}' resultCount={list.Count}");
        onResult?.Invoke(list);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string ApiUrl(string path)
    {
        var root = ServerAddressConfig.Instance != null
            ? ServerAddressConfig.Instance.ApiRoot
            : "http://localhost:5000";
        return $"{root.TrimEnd('/')}/api/{path}";
    }

    private static bool HasToken()
    {
        return !string.IsNullOrWhiteSpace(PlayerPrefs.GetString("JWT_TOKEN", ""));
    }

    /// <summary>Bọc JSON array [..] thành {"key":[..]} để JsonUtility đọc được.</summary>
    private static string WrapArray(string json, string key = "items")
        => $"{{\"{key}\":{json}}}";

    private static string BuildApiError(string operation, UnityWebRequest req)
    {
        string detail = req.downloadHandler?.text;
        if (string.IsNullOrWhiteSpace(detail))
            detail = req.error;

        return $"{operation} failed | code={req.responseCode} result={req.result} detail={detail}";
    }

    // ── Inner DTOs ────────────────────────────────────────────────────────────

    [Serializable]
    private class FriendArrayWrapper
    {
        public FriendEntryDto[] items;
    }

    [Serializable]
    private class UserSearchWrapper
    {
        public UserSearchResult[] results;
    }

    [Serializable]
    private class SendFriendReqPayload
    {
        public int TargetUserId;
    }

    // ── Player Profile ────────────────────────────────────────────────────────

    /// <summary>
    /// Lấy thông tin công khai của nhân vật theo userId (dùng cho Friend Profile).
    /// Gọi GET /api/player/by-user/{userId}
    /// </summary>
    public void GetPlayerProfile(int userId, Action<PlayerProfileDto> onResult)
    {
        string token = PlayerPrefs.GetString("JWT_TOKEN", "");
        Debug.Log($"[FriendManager] GetPlayerProfile userId={userId}");
        StartCoroutine(GetProfileRoutine(token, userId, onResult));
    }

    private IEnumerator GetProfileRoutine(string token, int userId, Action<PlayerProfileDto> onResult)
    {
        using var req = UnityWebRequest.Get(ApiUrl($"player/by-user/{userId}"));
        req.SetRequestHeader("Authorization", $"Bearer {token}");
        Debug.Log($"[FriendManager] GET {req.url}");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            string err = BuildApiError("GetPlayerProfile", req);
            Debug.LogWarning($"[FriendManager] {err}");
            OnError?.Invoke(err);
            onResult?.Invoke(null);
            yield break;
        }

        var dto = JsonUtility.FromJson<PlayerProfileDto>(req.downloadHandler.text);
        Debug.Log($"[FriendManager] GetPlayerProfile success userId={userId} hasDto={dto != null}");
        onResult?.Invoke(dto);
    }
}
