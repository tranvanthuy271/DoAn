using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Panel phó bản của NPC (npc_type == "dungeon").
/// Màn hình 1 (listPanel): hiển thị dialogue + danh sách phó bản.
/// Màn hình 2 (confirmPanel): xác nhận tham gia, hỗ trợ cả tổ đội.
///
/// Prefab: Assets/Prefabs/UI/DungeonNpcMenuPanel.prefab
/// Gắn vào cùng Canvas với NpcMenuUI. Gọi DungeonNpcMenuUI.GetOrCreate() để mở.
/// </summary>
public class DungeonNpcMenuUI : MonoBehaviour
{
    private const string InputBlockSource = "DungeonNpcMenuUI";
    private const string LogPrefix = "[DungeonNpcMenuUI]";
    private const string EntryPrefabResourcesPath = "UI/DungeonNpcMenuEntryPrefab";

    public static DungeonNpcMenuUI Instance { get; private set; }

    // ── List panel (màn hình 1) ───────────────────────────────
    [Header("List Panel")]
    [SerializeField] private GameObject listPanel;
    [SerializeField] private TMP_Text   greetingText;       // "Xin chào {tên nhân vật}"
    [SerializeField] private Transform  dungeonListRoot;    // Content của ScrollView
    [SerializeField] private GameObject dungeonEntryPrefab; // DungeonNpcMenuEntryPrefab
    [SerializeField] private Button     btnCloseList;       // "Cáo từ"

    // ── Confirm panel (màn hình 2) ────────────────────────────
    [Header("Confirm Panel")]
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private TMP_Text   confirmInfoText;    // "Hãy tập hợp tất cả đồng đội..."
    [SerializeField] private Transform  confirmOptionRoot;  // Content cho nút "Tham gia"
    [SerializeField] private GameObject confirmOptionPrefab; // prefab 1 nút option (icon + text)
    [SerializeField] private Button     btnConfirmJoin;     // nút "Tham gia" trực tiếp (tuỳ chọn)
    [SerializeField] private Button     btnBackToList;      // "Cáo từ" (quay lại list)

    public bool IsOpen => listPanel != null && listPanel.activeSelf
                       || confirmPanel != null && confirmPanel.activeSelf;

    private DungeonConfigData _pendingDungeon;
    private Coroutine _loadCoroutine;
    private bool _initialized;
    private bool _loggedRuntimeEntryFallback;

    // ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        if (Instance == null)
            Instance = this;

        AutoWireReferences();
    }

    private void EnsureInit()
    {
        AutoWireReferences();
        if (_initialized) return;

        _initialized = true;

        if (listPanel)    listPanel.SetActive(false);
        if (confirmPanel) confirmPanel.SetActive(false);

        RegisterButton(btnCloseList, Close);
        RegisterButton(btnBackToList, BackToList);
        RegisterButton(btnConfirmJoin, OnConfirmJoinClicked);

        // Auto-wire X buttons (created by editor tool as "BtnClose_X" in each panel)
        WireXClose(listPanel,    Close);
        WireXClose(confirmPanel, BackToList);

        ApplyTheme();
    }

    private static void WireXClose(GameObject panel, UnityAction action)
    {
        if (panel == null) return;
        var xBtn = panel.transform.Find("BtnClose_X");
        if (xBtn != null)
        {
            Button button = xBtn.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveListener(action);
                button.onClick.AddListener(action);
            }
        }
    }

    // ── Public API ────────────────────────────────────────────

    /// <summary>Mở danh sách phó bản từ NPC. Gọi từ NpcMenuUI khi npc_type=="dungeon".</summary>
    public void Open(NpcData npc)
    {
        EnsureInit();
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        ClientSceneController.Instance?.EnsureZoneStateFromRuntimeData();

        Debug.Log(
            $"{LogPrefix} Open | npcId={npc?.npc_id ?? -1} name='{npc?.npc_name}' type='{npc?.npc_type}' scene={SceneManager.GetActiveScene().name} map={ClientSceneController.Instance?.CurrentMapId ?? -1} zone={ClientSceneController.Instance?.CurrentZoneId ?? -1}",
            this);

        string playerName = GameManager.Instance?.GetPlayerData()?.character_name ?? "Người chơi";
        if (greetingText != null)
            greetingText.text = !string.IsNullOrEmpty(npc?.dialogue_text)
                ? npc.dialogue_text
                : $"Xin chào {playerName}";

        if (listPanel == null)
        {
            Debug.LogError($"{LogPrefix} listPanel chưa được resolve. Kiểm tra prefab/scene.", this);
            return;
        }

        listPanel.SetActive(true);
        confirmPanel?.SetActive(false);

        InputManager.Instance?.SetGameplayInputBlocked(InputBlockSource, true);
        InputManager.Instance?.CancelAutoMove();

        if (_loadCoroutine != null)
            StopCoroutine(_loadCoroutine);

        _loadCoroutine = StartCoroutine(LoadAndRenderDungeons());
    }

    /// <summary>Hiển thị màn hình xác nhận cho dungeon đã chọn.</summary>
    public void ShowConfirm(DungeonConfigData config)
    {
        EnsureInit();
        _pendingDungeon = config;
        listPanel?.SetActive(false);
        confirmPanel?.SetActive(true);

        var partyManager = PartyManager.EnsureInstance();
        bool hasParty = partyManager != null && partyManager.HasParty;

        Debug.Log(
            $"{LogPrefix} ShowConfirm | dungeonId={config?.dungeon_id ?? -1} name='{config?.dungeon_name}' type='{config?.dungeon_type}' hasParty={hasParty}",
            this);

        SetConfirmInfoMessage(hasParty
            ? "Hãy tập hợp tất cả đồng đội trong nhóm tại đây"
            : "Bạn muốn tham gia phó bản này?");

        // Xây tuỳ chọn động
        if (confirmOptionRoot != null && confirmOptionPrefab != null)
        {
            foreach (Transform t in confirmOptionRoot) Destroy(t.gameObject);
            var go  = Instantiate(confirmOptionPrefab, confirmOptionRoot);
            var txt = go.GetComponentInChildren<TMP_Text>();
            if (txt != null) txt.text = "Tham gia";
            var btn = go.GetComponent<Button>() ?? go.GetComponentInChildren<Button>();
            btn?.onClick.AddListener(OnConfirmJoinClicked);
        }
        else if (btnConfirmJoin == null)
        {
            Debug.LogWarning($"{LogPrefix} Không có confirmOptionPrefab và cũng không có btnConfirmJoin.", this);
        }
    }

    public void Close()
    {
        Debug.Log($"{LogPrefix} Close | pendingDungeon={_pendingDungeon?.dungeon_id ?? -1}", this);
        if (_loadCoroutine != null)
        {
            StopCoroutine(_loadCoroutine);
            _loadCoroutine = null;
        }

        listPanel?.SetActive(false);
        confirmPanel?.SetActive(false);
        _pendingDungeon = null;
        InputManager.Instance?.SetGameplayInputBlocked(InputBlockSource, false);
    }

    // ── Internal ──────────────────────────────────────────────

    private void BackToList()
    {
        Debug.Log($"{LogPrefix} BackToList | pendingDungeon={_pendingDungeon?.dungeon_id ?? -1}", this);
        confirmPanel?.SetActive(false);
        listPanel?.SetActive(true);
        _pendingDungeon = null;
    }

    private void OnConfirmJoinClicked()
    {
        if (_pendingDungeon == null)
        {
            Debug.LogWarning($"{LogPrefix} OnConfirmJoinClicked nhưng chưa có _pendingDungeon.", this);
            return;
        }

        bool isMulti = string.Equals(_pendingDungeon.dungeon_type, "multi",
                                     System.StringComparison.OrdinalIgnoreCase);

        var partyManager = PartyManager.EnsureInstance();

        Debug.Log(
            $"{LogPrefix} ConfirmJoin | dungeonId={_pendingDungeon.dungeon_id} type='{_pendingDungeon.dungeon_type}' hasParty={partyManager != null && partyManager.HasParty} isLeader={partyManager != null && partyManager.IsLeader}",
            this);

        if (isMulti)
        {
            // Phó bản tổ đội: bắt buộc phải có nhóm
            if (partyManager == null || !partyManager.HasParty)
            {
                Debug.LogWarning($"{LogPrefix} Reject join: chưa có tổ đội.", this);
                SetConfirmInfoMessage("Cần phải có nhóm mới có thể tham gia phó bản này.");
                return;
            }

            // Chỉ nhóm trưởng mới được khởi động
            if (!partyManager.IsLeader)
            {
                Debug.LogWarning($"{LogPrefix} Reject join: người chơi không phải trưởng nhóm.", this);
                SetConfirmInfoMessage("Chỉ nhóm trưởng mới có thể khởi động phó bản.\nHãy yêu cầu nhóm trưởng nhấn Tham gia.");
                return;
            }

            // Kiểm tra tất cả thành viên cùng zone + map
            string notReadyName = FindMemberNotInSameZone(partyManager);
            if (notReadyName != null)
            {
                Debug.LogWarning($"{LogPrefix} Reject join: thành viên '{notReadyName}' chưa cùng map/zone.", this);
                SetConfirmInfoMessage($"Thành viên \"{notReadyName}\" chưa ở cùng khu vực.\nHãy tập hợp đầy đủ trước khi vào phó bản!");
                return;
            }

            Debug.Log($"{LogPrefix} StartPartyDungeon | dungeonId={_pendingDungeon.dungeon_id} mapId={_pendingDungeon.map_id} type='{_pendingDungeon.dungeon_type}'", this);

            // Thu thập userIds của tất cả thành viên party
            var members = partyManager.CurrentParty?.members;
            string[] memberUserIds = null;
            if (members != null && members.Length > 0)
            {
                memberUserIds = new string[members.Length];
                for (int i = 0; i < members.Length; i++)
                    memberUserIds[i] = members[i].userId;
            }

            DungeonManager.Instance?.EnterPartyDungeon(_pendingDungeon, memberUserIds);
        }
        else
        {
            // Phó bản solo
            Debug.Log($"{LogPrefix} Enter solo dungeon directly | dungeonId={_pendingDungeon.dungeon_id} scene='{_pendingDungeon.scene_name}'", this);
            DungeonManager.Instance?.EnterDungeon(_pendingDungeon);
        }

        Close();
    }

    /// <summary>
    /// Kiểm tra tất cả thành viên online trong tổ đội có cùng mapId và zoneId với local player.
    /// Trả về tên thành viên đầu tiên không cùng zone, hoặc null nếu tất cả OK.
    /// </summary>
    private static string FindMemberNotInSameZone(PartyManager partyManager)
    {
        var partyMembers  = partyManager.CurrentParty?.members;
        if (partyMembers == null || partyMembers.Length <= 1) return null; // solo hoặc không có data → bỏ qua

        var nearbyPlayers = partyManager.LatestNearbyPlayers?.players;
        if (nearbyPlayers == null || nearbyPlayers.Length == 0) return null; // chưa có data → server sẽ validate

        ClientSceneController.Instance?.EnsureZoneStateFromRuntimeData();

        int localMap  = ClientSceneController.Instance?.CurrentMapId  ?? -1;
        int localZone = ClientSceneController.Instance?.CurrentZoneId ?? -1;

        // Tập hợp userId của tất cả người đang cùng zone/map với local player
        var usersInSameZone = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
        foreach (var np in nearbyPlayers)
            if (np.mapId == localMap && np.zoneId == localZone)
                usersInSameZone.Add(np.userId);

        var pd = GameManager.Instance?.GetPlayerData();
        string myUserId = pd != null ? pd.user_id.ToString() : string.Empty;

        foreach (var member in partyMembers)
        {
            if (string.Equals(member.userId, myUserId, System.StringComparison.Ordinal)) continue;
            if (!member.online) continue; // thành viên offline → bỏ qua
            if (!usersInSameZone.Contains(member.userId))
                return member.characterName;
        }

        return null;
    }

    private IEnumerator LoadAndRenderDungeons()
    {
        Debug.Log(
            $"{LogPrefix} LoadAndRenderDungeons start | root={(dungeonListRoot != null)} prefab={(dungeonEntryPrefab != null ? dungeonEntryPrefab.name : "<runtime>")} scene={SceneManager.GetActiveScene().name}",
            this);

        // Xoá list cũ
        if (dungeonListRoot != null)
            foreach (Transform t in dungeonListRoot) Destroy(t.gameObject);

        if (dungeonListRoot == null)
        {
            Debug.LogError($"{LogPrefix} dungeonListRoot chưa được resolve. Không thể render list.", this);
            _loadCoroutine = null;
            yield break;
        }

        // Gọi trực tiếp REST API (dungeon/list không cần auth) thay vì đi qua ServerRpc
        // để tránh dependency vào JWT và GameplayCommandService
        string apiUrl = ServerAddressConfig.Instance.ApiUrl + "/dungeon/list";
        DungeonConfigData[] dungeons = null;

        Debug.Log($"{LogPrefix} Fetching dungeon list directly | url={apiUrl}", this);
        using (var req = UnityWebRequest.Get(apiUrl))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string json = req.downloadHandler.text;
                var resp = JsonUtility.FromJson<DungeonListResponse>(json);
                dungeons = resp?.dungeons;
                Debug.Log($"{LogPrefix} Dungeon list received | count={(dungeons != null ? dungeons.Length : 0)}", this);
            }
            else
            {
                Debug.LogWarning($"{LogPrefix} Dungeon list API error: {req.error} | response={req.downloadHandler?.text}", this);
            }
        }

        _loadCoroutine = null;

        if (dungeons == null || dungeons.Length == 0)
        {
            Debug.LogWarning($"{LogPrefix} Không có dungeon nào để render.", this);
            yield break;
        }

        foreach (var cfg in dungeons)
        {
            if (dungeonListRoot == null) break;

            var go    = CreateEntryInstance(dungeonListRoot);
            var entry = go.GetComponent<DungeonNpcMenuEntryUI>();
            Debug.Log($"{LogPrefix} Render dungeon entry | dungeonId={cfg.dungeon_id} name='{cfg.dungeon_name}' type='{cfg.dungeon_type}' runtimeEntry={dungeonEntryPrefab == null}", this);
            entry?.Setup(cfg, this);
        }
    }

    // ── Static factory (fallback tìm trong scene) ─────────────

    public static DungeonNpcMenuUI GetOrCreate()
    {
        if (Instance != null) return Instance;
        Instance = FindObjectOfType<DungeonNpcMenuUI>(true);

        if (Instance == null)
        {
            var prefabGO = Resources.Load<GameObject>("UI/DungeonNpcMenuPanel");
            if (prefabGO != null)
            {
                var go = Instantiate(prefabGO);
                go.name = "DungeonNpcMenuPanel";
                Instance = go.GetComponent<DungeonNpcMenuUI>();
            }
        }

        if (Instance == null)
        {
            Debug.LogWarning($"{LogPrefix} Không tìm thấy DungeonNpcMenuUI trong scene hoặc Resources/UI/DungeonNpcMenuPanel.");
        }

        return Instance;
    }

    private void ApplyTheme()
    {
        if (listPanel != null)
            UIRuntimeAssetHelper.ApplyNotoSans(listPanel.GetComponentsInChildren<TMP_Text>(true));

        if (confirmPanel != null)
            UIRuntimeAssetHelper.ApplyNotoSans(confirmPanel.GetComponentsInChildren<TMP_Text>(true));
    }

    private void AutoWireReferences()
    {
        if (listPanel == null)
            listPanel = FindChildByName("ListPanel")?.gameObject;

        if (greetingText == null)
            greetingText = FindChildComponent<TMP_Text>("GreetingText");

        if (dungeonListRoot == null)
            dungeonListRoot = FindChildByPath("ListPanel/DungeonScrollView/Viewport/Content")
                           ?? FindChildByPath("DungeonScrollView/Viewport/Content")
                           ?? FindChildByName("Content");

        if (dungeonEntryPrefab == null)
            dungeonEntryPrefab = Resources.Load<GameObject>(EntryPrefabResourcesPath);

        if (btnCloseList == null)
            btnCloseList = FindChildComponent<Button>("BtnCloseList");

        if (confirmPanel == null)
            confirmPanel = FindChildByName("ConfirmPanel")?.gameObject;

        if (confirmInfoText == null)
            confirmInfoText = FindChildComponent<TMP_Text>("ConfirmInfoText");

        if (confirmOptionRoot == null)
            confirmOptionRoot = FindChildByName("ConfirmOptionRoot");

        if (btnConfirmJoin == null)
            btnConfirmJoin = FindChildComponent<Button>("BtnConfirmJoin");

        if (btnBackToList == null)
            btnBackToList = FindChildComponent<Button>("BtnBackToList");
    }

    private Transform FindChildByPath(string path)
    {
        return string.IsNullOrWhiteSpace(path) ? null : transform.Find(path);
    }

    private Transform FindChildByName(string childName)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
                return child;
        }

        return null;
    }

    private T FindChildComponent<T>(string childName) where T : Component
    {
        Transform child = FindChildByName(childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private static void RegisterButton(Button button, UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void SetConfirmInfoMessage(string message)
    {
        if (confirmInfoText == null)
            return;

        confirmInfoText.text = message ?? string.Empty;
        Debug.Log($"{LogPrefix} ConfirmInfo='{confirmInfoText.text}'", this);
    }

    private GameObject CreateEntryInstance(Transform parent)
    {
        if (dungeonEntryPrefab != null)
        {
            return Instantiate(dungeonEntryPrefab, parent);
        }

        if (!_loggedRuntimeEntryFallback)
        {
            _loggedRuntimeEntryFallback = true;
            Debug.LogWarning($"{LogPrefix} dungeonEntryPrefab chưa được gán. Dùng runtime fallback row.", this);
        }

        return CreateRuntimeEntry(parent);
    }

    private GameObject CreateRuntimeEntry(Transform parent)
    {
        var root = new GameObject(
            "DungeonNpcMenuEntryRuntime",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement),
            typeof(DungeonNpcMenuEntryUI));
        root.transform.SetParent(parent, false);

        var rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(310f, 46f);

        var layoutElement = root.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = 46f;
        layoutElement.minHeight = 46f;

        var image = root.GetComponent<Image>();
        image.color = new Color(0.50f, 0.30f, 0.10f, 0f);

        var button = root.GetComponent<Button>();
        var colors = button.colors;
        colors.normalColor = new Color(0.50f, 0.30f, 0.10f, 0f);
        colors.highlightedColor = new Color(0.70f, 0.45f, 0.15f, 0.6f);
        colors.pressedColor = new Color(0.85f, 0.55f, 0.18f, 0.8f);
        colors.selectedColor = new Color(0.70f, 0.45f, 0.15f, 0.6f);
        button.colors = colors;

        var iconGO = new GameObject("ChatBubbleIcon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(root.transform, false);
        var iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.1f);
        iconRect.anchorMax = new Vector2(0f, 0.9f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.offsetMin = new Vector2(10f, 0f);
        iconRect.offsetMax = new Vector2(46f, 0f);
        var iconImage = iconGO.GetComponent<Image>();
        iconImage.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        iconImage.raycastTarget = false;

        var textGO = new GameObject("DungeonNameText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(root.transform, false);
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(54f, 4f);
        textRect.offsetMax = new Vector2(-6f, -4f);

        var tmp = textGO.GetComponent<TextMeshProUGUI>();
        tmp.text = "Tên Phó Bản";
        tmp.fontSize = 16f;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;
        UIRuntimeAssetHelper.ApplyNotoSans(tmp);

        return root;
    }
}
