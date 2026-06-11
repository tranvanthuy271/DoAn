using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartyPanelUI : MonoBehaviour
{
    private const string GameplayBlockSource = "PartyPanelUI";
    private const string LogPrefix = "[PartyPanelUI]";

    [Header("Header")]
    [SerializeField] private Button closeButton;

    [Header("Tabs")]
    [SerializeField] private Button tabPartyButton;
    [SerializeField] private Button tabSearchButton;
    [SerializeField] private Button tabNearbyButton;
    [SerializeField] private GameObject partyTabPanel;
    [SerializeField] private GameObject searchTabPanel;
    [SerializeField] private GameObject nearbyTabPanel;

    [Header("Tab Tổ Đội")]
    [SerializeField] private Transform memberListRoot;
    [SerializeField] private GameObject memberEntryPrefab;
    [SerializeField] private Toggle lockToggle;
    [SerializeField] private Toggle autoAcceptToggle;
    [SerializeField] private Image lockToggleIndicatorImage;
    [SerializeField] private Image autoAcceptToggleIndicatorImage;
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionButtonLabel;
    [SerializeField] private Button chatGroupButton;
    [SerializeField] private TMP_Text statusText;

    [Header("Thông báo Xin vào nhóm")]
    [SerializeField] private PartyJoinRequestPopupUI joinRequestPopup;
    [SerializeField] private string joinRequestPopupResourcesPath = "Prefabs/UI/Party/PartyJoinRequestPopup";

    [Header("Tab Tìm Nhóm")]
    [SerializeField] private Transform searchListRoot;
    [SerializeField] private GameObject searchEntryPrefab;
    [SerializeField] private Button refreshSearchButton;

    [Header("Tab Gần Đây")]
    [SerializeField] private Transform nearbyListRoot;
    [SerializeField] private GameObject nearbyEntryPrefab;
    [SerializeField] private Button refreshNearbyButton;
    [SerializeField] private TMP_Text nearbyPopulationText;

    [Header("Linked Chat")]
    [SerializeField] private string chatPanelResourcesPath = "Prefabs/Chat/ChatPanel";

    private readonly Queue<PartyJoinRequestPayload> _pendingJoinRequests = new();
    private int _activeTabIndex;
    private PartyStatePayload _latestPartyState;
    private PartySearchResultPayload _latestSearchResults = new PartySearchResultPayload();
    private NearbyPlayersPayload _latestNearbyPlayers = new NearbyPlayersPayload();
    private ChatPanelUI _resolvedChatPanel;
    private PartyManager _partyManager;

    private void Awake()
    {
        closeButton?.onClick.AddListener(ClosePanel);
        tabPartyButton?.onClick.AddListener(() => SelectTab(0));
        tabSearchButton?.onClick.AddListener(() => SelectTab(1));
        tabNearbyButton?.onClick.AddListener(() => SelectTab(2));
        actionButton?.onClick.AddListener(OnActionClicked);
        chatGroupButton?.onClick.AddListener(OpenGroupChat);
        refreshSearchButton?.onClick.AddListener(OnRefreshSearchClicked);
        refreshNearbyButton?.onClick.AddListener(OnRefreshNearbyClicked);
        lockToggle?.onValueChanged.AddListener(OnLockToggleChanged);
        autoAcceptToggle?.onValueChanged.AddListener(OnAutoAcceptToggleChanged);

        UpdateToggleIndicators();
    }

    private void OnEnable()
    {
        InputManager.Instance?.SetGameplayInputBlocked(GameplayBlockSource, true);
        InputManager.Instance?.CancelAutoMove();

        { /* {LogPrefix} OnEnable | currentTab={_activeTabIndex} */ }

        _partyManager = PartyManager.EnsureInstance();

        joinRequestPopup = ResolveJoinRequestPopup();

        if (_partyManager != null)
        {
            _partyManager.OnPartyStateChanged += HandlePartyStateChanged;
            _partyManager.OnSearchResultsUpdated += HandleSearchResultsUpdated;
            _partyManager.OnNearbyPlayersUpdated += HandleNearbyPlayersUpdated;
            _partyManager.OnJoinRequestReceived += HandleJoinRequestReceived;
            _partyManager.OnError += HandlePartyError;

            _latestPartyState = _partyManager.CurrentParty;
            _latestSearchResults = _partyManager.LatestSearchResults ?? new PartySearchResultPayload();
            _latestNearbyPlayers = NormalizeNearbyPayload(_partyManager.LatestNearbyPlayers);
        }
        else
        {
            { /* Lỗi: {LogPrefix} PartyManager.EnsureInstance returned null khi mở PartyPanel */ }
        }

        SelectTab(_activeTabIndex);
        HandlePartyStateChanged(_latestPartyState);
        RequestSearchRefresh("OnEnable");
        RequestNearbyRefresh("OnEnable");
    }

    private void OnDisable()
    {
        InputManager.Instance?.SetGameplayInputBlocked(GameplayBlockSource, false);

        if (_partyManager != null)
        {
            _partyManager.OnPartyStateChanged -= HandlePartyStateChanged;
            _partyManager.OnSearchResultsUpdated -= HandleSearchResultsUpdated;
            _partyManager.OnNearbyPlayersUpdated -= HandleNearbyPlayersUpdated;
            _partyManager.OnJoinRequestReceived -= HandleJoinRequestReceived;
            _partyManager.OnError -= HandlePartyError;
        }

        _partyManager = null;
    }

    public void SelectTab(int tabIndex)
    {
        _activeTabIndex = Mathf.Clamp(tabIndex, 0, 2);

        { /* {LogPrefix} SelectTab -> {_activeTabIndex} */ }

        if (partyTabPanel != null) partyTabPanel.SetActive(_activeTabIndex == 0);
        if (searchTabPanel != null) searchTabPanel.SetActive(_activeTabIndex == 1);
        if (nearbyTabPanel != null) nearbyTabPanel.SetActive(_activeTabIndex == 2);

        SetButtonState(tabPartyButton, _activeTabIndex == 0);
        SetButtonState(tabSearchButton, _activeTabIndex == 1);
        SetButtonState(tabNearbyButton, _activeTabIndex == 2);

        switch (_activeTabIndex)
        {
            case 0:
                BuildMemberList(_latestPartyState);
                break;
            case 1:
                RenderSearchResults(_latestSearchResults, "SelectTab");
                RequestSearchRefresh("SelectTab");
                break;
            case 2:
                RenderNearbyPlayers(_latestNearbyPlayers, "SelectTab");
                RequestNearbyRefresh("SelectTab");
                break;
        }
    }

    private void HandlePartyStateChanged(PartyStatePayload state)
    {
        _latestPartyState = state;
        { /* {LogPrefix} HandlePartyStateChanged | partyId={state?.partyId} members={state?.memberCount ?? 0}/{state?.maxMembers ?? 0} leader={PartyManager.Instance?.IsLeader} */ }

        BuildMemberList(state);

        if (lockToggle != null)
        {
            lockToggle.SetIsOnWithoutNotify(state != null && state.isLocked);
            // Selalu interactable – handler sẽ kiểm tra có nhóm + là leader không
            lockToggle.interactable = true;
        }

        if (autoAcceptToggle != null)
        {
            autoAcceptToggle.SetIsOnWithoutNotify(state != null && state.autoAccept);
            autoAcceptToggle.interactable = true;
        }

        UpdateToggleIndicators();

        // Rebuild nearby list khi party state đổi (invite button visibility phụ thuộc vào IsLeader)
        if (_activeTabIndex == 2)
            RenderNearbyPlayers(_latestNearbyPlayers, "PartyStateChange");

        if (_partyManager != null && _partyManager.IsConnected)
        {
            RequestSearchRefresh("PartyStateChange");
            RequestNearbyRefresh("PartyStateChange");
        }

        if (actionButtonLabel != null)
        {
            if (state == null)
                actionButtonLabel.text = "Tạo nhóm";
            else if (PartyManager.Instance != null && PartyManager.Instance.IsLeader)
                actionButtonLabel.text = "Giải tán";
            else
                actionButtonLabel.text = "Rời nhóm";
        }

        if (statusText != null)
        {
            statusText.text = state == null
                ? "Chưa có tổ đội."
                : $"Thành viên: {state.memberCount}/{state.maxMembers}";
        }

        SetStatusText(
            state == null
                ? "Chưa có tổ đội."
                : $"Thành viên: {state.memberCount}/{state.maxMembers}",
            false);
    }

    private void HandleSearchResultsUpdated(PartySearchResultPayload payload)
    {
        _latestSearchResults = payload ?? new PartySearchResultPayload();
        { /* {LogPrefix} HandleSearchResultsUpdated | count={_latestSearchResults.parties?.Length ?? 0} leaders={string.Join( */ }

        if (_activeTabIndex == 1)
            RenderSearchResults(_latestSearchResults, "HubEvent");
    }

    private void HandleNearbyPlayersUpdated(NearbyPlayersPayload payload)
    {
        _latestNearbyPlayers = NormalizeNearbyPayload(payload);
        { /* {LogPrefix} HandleNearbyPlayersUpdated | count={_latestNearbyPlayers.players?.Length ?? 0} names={string.Join( */ }

        if (_activeTabIndex == 2)
            RenderNearbyPlayers(_latestNearbyPlayers, "HubEvent");
    }

    private void HandleJoinRequestReceived(PartyJoinRequestPayload payload)
    {
        if (payload == null)
            return;

        if (joinRequestPopup == null)
            joinRequestPopup = ResolveJoinRequestPopup();

        var partyManager = ResolvePartyManager(ensure: false);
        if (partyManager == null || !partyManager.IsLeader)
        {
            { /* Cảnh báo: {LogPrefix} Ignored join request because local player is not party leader. requester={payload.requesterName} userId={payload.requesterUserId} */ }
            return;
        }

        bool requesterAlreadyInParty = _latestPartyState?.members != null
            && _latestPartyState.members.Any(member => string.Equals(member.userId, payload.requesterUserId, StringComparison.Ordinal));
        if (requesterAlreadyInParty)
        {
            { /* Cảnh báo: {LogPrefix} Ignored join request because requester is already in the party. requester={payload.requesterName} userId={payload.requesterUserId} */ }
            return;
        }

        { /* {LogPrefix} Join request received | requester={payload.requesterName} userId={payload.requesterUserId} level={payload.requesterLevel} element={payload.requesterElementType} | joinRequestPopup={(joinRequestPopup == null ? */ }

        // Ưu tiên dùng panel thông báo riêng nếu đã ghi
        if (joinRequestPopup != null)
        {
            joinRequestPopup.ShowRequest(
                payload,
                onAccept:  (partyId, userId) =>
                {
                    { /* {LogPrefix} NotificationPanel Accept | partyId={partyId} userId={userId} */ }
                    ResolvePartyManager()?.AcceptJoinRequest(partyId, userId);
                },
                onDecline: (partyId, userId) =>
                {
                    { /* {LogPrefix} NotificationPanel Decline | partyId={partyId} userId={userId} */ }
                    ResolvePartyManager()?.RejectJoinRequest(partyId, userId);
                }
            );
            return;
        }

        // Fallback: xếp hàng cho nút hành động
        _pendingJoinRequests.Enqueue(payload);
        { /* {LogPrefix} Join request queued for action button | requester={payload.requesterName} userId={payload.requesterUserId} */ }
        SetStatusText($"{payload.requesterName} xin vào tổ đội. Nhấn nút hành động để chấp nhận nhanh.");
    }

    private void HandlePartyError(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        { /* Cảnh báo: {LogPrefix} Party error: {message} */ }
        SetStatusText(message);
    }

    private void BuildMemberList(PartyStatePayload state)
    {
        ClearList(memberListRoot);
        if (state?.members == null || memberListRoot == null || memberEntryPrefab == null)
        {
            { /* {LogPrefix} BuildMemberList skipped | hasState={state != null} root={memberListRoot != null} prefab={memberEntryPrefab != null} */ }
            return;
        }

        { /* {LogPrefix} BuildMemberList | count={state.members.Length} */ }

        foreach (PartyMemberDto member in state.members
                     .OrderByDescending(m => string.Equals(m.userId, state.leaderUserId, StringComparison.Ordinal))
                     .ThenBy(m => m.characterName))
        {
            GameObject row = Instantiate(memberEntryPrefab, memberListRoot);
            row.GetComponent<PartyMemberEntryUI>()?.Setup(member, string.Equals(member.userId, state.leaderUserId, StringComparison.Ordinal));
        }
    }

    private void OnActionClicked()
    {
        var partyManager = ResolvePartyManager();
        { /* {LogPrefix} ActionButton clicked | hasParty={partyManager?.HasParty} leader={partyManager?.IsLeader} connected={partyManager?.IsConnected} pendingJoinRequests={_pendingJoinRequests.Count} */ }

        if (partyManager == null)
        {
            { /* Lỗi: {LogPrefix} ActionButton failed because PartyManager is null */ }
            SetStatusText("PartyManager chưa sẵn sàng.");
            return;
        }

        if (!partyManager.IsConnected)
        {
            { /* Cảnh báo: {LogPrefix} ActionButton blocked because PartyManager is not connected */ }
            SetStatusText("PartyManager chưa kết nối. Đang thử kết nối lại, xem Console để biết chi tiết.", false);
        }

        if (!partyManager.HasParty)
        {
            SetStatusText("Đang gửi yêu cầu tạo nhóm...", false);
            partyManager.CreateParty();
            return;
        }

        if (_pendingJoinRequests.Count > 0 && partyManager.IsLeader)
        {
            PartyJoinRequestPayload req = _pendingJoinRequests.Dequeue();
            SetStatusText($"Đang chấp nhận {req.requesterName} vào nhóm...", false);
            partyManager.AcceptJoinRequest(req.partyId, req.requesterUserId);
            return;
        }

        if (partyManager.IsLeader)
        {
            SetStatusText("Đang giải tán nhóm...", false);
            partyManager.DisbandParty();
        }
        else
        {
            SetStatusText("Đang rời nhóm...", false);
            partyManager.LeaveParty();
        }
    }

    private void OpenGroupChat()
    {
        var partyManager = ResolvePartyManager();
        { /* {LogPrefix} ChatGroupButton clicked */ }

        if (partyManager == null || !partyManager.HasParty)
        {
            { /* Cảnh báo: {LogPrefix} Không thể mở chat nhóm vì hiện chưa có tổ đội */ }
            SetStatusText("Bạn chưa có nhóm để chat.");
            return;
        }

        var chatPanel = ResolveChatPanel();
        if (chatPanel == null)
        {
            { /* Lỗi: {LogPrefix} Không resolve được ChatPanelUI để mở chat nhóm */ }
            SetStatusText("Không tìm thấy ChatPanel.");
            return;
        }

        if (ChatManager.Instance != null && string.IsNullOrWhiteSpace(ChatManager.Instance.CurrentGroupId))
        {
            ChatManager.Instance.JoinGroup(partyManager.CurrentParty.partyId);
            ChatManager.Instance.CurrentSendChannel = ChatChannel.Group;
            { /* {LogPrefix} Forced ChatManager.JoinGroup for partyId={partyManager.CurrentParty.partyId} */ }
        }

        chatPanel.OpenOnGroupTab();
        gameObject.SetActive(false);
        { /* {LogPrefix} PartyPanel closed after opening group chat */ }
    }

    private void BuildList<T>(Transform root, GameObject prefab, IReadOnlyList<T> items, Action<GameObject, T> binder, string listName)
    {
        ClearList(root);
        if (root == null || prefab == null || items == null)
        {
            { /* {LogPrefix} BuildList skipped | list={listName} root={root != null} prefab={prefab != null} itemsNull={items == null} */ }
            return;
        }

        { /* {LogPrefix} BuildList | list={listName} count={items.Count} */ }

        foreach (T item in items)
        {
            GameObject row = Instantiate(prefab, root);
            binder?.Invoke(row, item);
        }
    }

    private void ClosePanel()
    {
        { /* {LogPrefix} Close button clicked */ }
        gameObject.SetActive(false);
    }

    private void OnRefreshSearchClicked()
    {
        { /* {LogPrefix} Refresh search button clicked */ }
        RequestSearchRefresh("RefreshButton");
    }

    private void OnRefreshNearbyClicked()
    {
        { /* {LogPrefix} Refresh nearby button clicked */ }
        RequestNearbyRefresh("RefreshButton");
    }

    private void OnLockToggleChanged(bool isOn)
    {
        var partyManager = ResolvePartyManager();
        UpdateToggleIndicators();
        { /* {LogPrefix} Lock toggle changed -> {isOn} */ }

        if (partyManager == null)
        {
            { /* Cảnh báo: {LogPrefix} Lock toggle ignored because PartyManager is null */ }
            if (lockToggle != null)
                lockToggle.SetIsOnWithoutNotify(_latestPartyState != null && _latestPartyState.isLocked);
            UpdateToggleIndicators();
            return;
        }

        if (!partyManager.HasParty || !partyManager.IsLeader)
        {
            { /* Cảnh báo: {LogPrefix} Lock toggle ignored because local player is not party leader */ }
            if (lockToggle != null)
                lockToggle.SetIsOnWithoutNotify(_latestPartyState != null && _latestPartyState.isLocked);
            UpdateToggleIndicators();
            return;
        }

        partyManager.SetLock(isOn);
    }

    private void OnAutoAcceptToggleChanged(bool isOn)
    {
        var partyManager = ResolvePartyManager();
        UpdateToggleIndicators();
        { /* {LogPrefix} AutoAccept toggle changed -> {isOn} */ }

        if (partyManager == null)
        {
            { /* Cảnh báo: {LogPrefix} AutoAccept toggle ignored because PartyManager is null */ }
            if (autoAcceptToggle != null)
                autoAcceptToggle.SetIsOnWithoutNotify(_latestPartyState != null && _latestPartyState.autoAccept);
            UpdateToggleIndicators();
            return;
        }

        if (!partyManager.HasParty || !partyManager.IsLeader)
        {
            { /* Cảnh báo: {LogPrefix} AutoAccept toggle ignored because local player is not party leader */ }
            if (autoAcceptToggle != null)
                autoAcceptToggle.SetIsOnWithoutNotify(_latestPartyState != null && _latestPartyState.autoAccept);
            UpdateToggleIndicators();
            return;
        }

        partyManager.SetAutoAccept(isOn);
    }

    private void UpdateToggleIndicators()
    {
        lockToggleIndicatorImage = ResolveToggleIndicatorImage(lockToggle, lockToggleIndicatorImage);
        autoAcceptToggleIndicatorImage = ResolveToggleIndicatorImage(autoAcceptToggle, autoAcceptToggleIndicatorImage);

        SetToggleIndicatorVisible(lockToggleIndicatorImage, lockToggle != null && lockToggle.isOn);
        SetToggleIndicatorVisible(autoAcceptToggleIndicatorImage, autoAcceptToggle != null && autoAcceptToggle.isOn);
    }

    private static Image ResolveToggleIndicatorImage(Toggle toggle, Image current)
    {
        if (current != null)
            return current;

        if (toggle == null)
            return null;

        current = toggle.transform.Find("Checkmark")?.GetComponent<Image>();
        if (current != null)
            return current;

        var backgroundImage = toggle.targetGraphic as Image;
        foreach (Image image in toggle.GetComponentsInChildren<Image>(true))
        {
            if (image != null && image != backgroundImage)
                return image;
        }

        return null;
    }

    private static void SetToggleIndicatorVisible(Image indicator, bool isVisible)
    {
        if (indicator == null)
            return;

        if (indicator.gameObject.activeSelf != isVisible)
            indicator.gameObject.SetActive(isVisible);

        indicator.enabled = isVisible;
    }

    private void RenderSearchResults(PartySearchResultPayload payload, string source)
    {
        BuildList(searchListRoot, searchEntryPrefab, payload?.parties, (go, data) =>
        {
            go.GetComponent<PartySearchEntryUI>()?.Setup(data);
        }, $"Search:{source}");
    }

    private void RenderNearbyPlayers(NearbyPlayersPayload payload, string source)
    {
        BuildList(nearbyListRoot, nearbyEntryPrefab, payload?.players, (go, data) =>
        {
            go.GetComponent<PartyNearbyEntryUI>()?.Setup(data);
        }, $"Nearby:{source}");

        int population = payload?.players?.Length ?? 0;
        if (nearbyPopulationText != null)
            nearbyPopulationText.text = $"Dân số: {population}";

        { /* {LogPrefix} RenderNearbyPlayers | source={source} population={population} */ }
    }

    private void RequestSearchRefresh(string reason)
    {
        { /* {LogPrefix} RequestSearchRefresh | reason={reason} */ }
        ResolvePartyManager()?.RefreshPartiesInCurrentZone();
    }

    private void RequestNearbyRefresh(string reason)
    {
        { /* {LogPrefix} RequestNearbyRefresh | reason={reason} */ }
        ResolvePartyManager()?.RefreshNearbyPlayers();
    }

    private NearbyPlayersPayload NormalizeNearbyPayload(NearbyPlayersPayload payload)
    {
        var players = new List<NearbyPlayerDto>(payload?.players ?? Array.Empty<NearbyPlayerDto>());
        string localUserId = ResolveLocalUserId();

        if (!string.IsNullOrWhiteSpace(localUserId)
            && players.All(player => !string.Equals(player.userId, localUserId, StringComparison.Ordinal)))
        {
            var localPlayer = CreateLocalNearbyPlayer(localUserId);
            if (localPlayer != null)
            {
                players.Add(localPlayer);
                { /* {LogPrefix} Injected local player into nearby list | name={localPlayer.characterName} */ }
            }
        }

        return new NearbyPlayersPayload { players = players.ToArray() };
    }

    private NearbyPlayerDto CreateLocalNearbyPlayer(string localUserId)
    {
        var playerData = GameManager.Instance?.GetPlayerData() ?? GameManager.Instance?.currentPlayerData;
        if (playerData == null)
        {
            { /* Cảnh báo: {LogPrefix} Cannot inject local nearby player because PlayerData is null */ }
            return null;
        }

        return new NearbyPlayerDto
        {
            userId = localUserId,
            characterName = string.IsNullOrWhiteSpace(playerData.character_name) ? ResolveLocalCharacterName() : playerData.character_name,
            level = Mathf.Max(1, playerData.level),
            className = ResolveLocalClassName(playerData),
            elementType = playerData.element_type ?? string.Empty,
            mapId = playerData.map_id,
            zoneId = ResolveCurrentZoneId(),
            inParty = PartyManager.Instance != null && PartyManager.Instance.HasParty,
            isPartyLeader = PartyManager.Instance != null && PartyManager.Instance.IsLeader,
        };
    }

    private ChatPanelUI ResolveChatPanel()
    {
        if (IsSceneChatPanel(_resolvedChatPanel))
            return _resolvedChatPanel;

        _resolvedChatPanel = ChatPanelUI.Instance;
        if (!IsSceneChatPanel(_resolvedChatPanel))
            _resolvedChatPanel = FindObjectOfType<ChatPanelUI>(includeInactive: true);

        if (IsSceneChatPanel(_resolvedChatPanel))
        {
            { /* {LogPrefix} Resolved existing ChatPanelUI from scene */ }
            return _resolvedChatPanel;
        }

        var chatPrefab = Resources.Load<GameObject>(chatPanelResourcesPath);
        if (chatPrefab == null)
        {
            { /* Lỗi: {LogPrefix} ChatPanel prefab not found at Resources/{chatPanelResourcesPath} */ }
            return null;
        }

        var uiParent = ResolveUiParent();
        if (uiParent == null)
        {
            { /* Lỗi: {LogPrefix} No Canvas/UI root found to instantiate ChatPanel */ }
            return null;
        }

        var chatInstance = Instantiate(chatPrefab, uiParent, false);
        chatInstance.name = chatPrefab.name;
        chatInstance.SetActive(false);
        _resolvedChatPanel = chatInstance.GetComponent<ChatPanelUI>();

        { /* {LogPrefix} Instantiated ChatPanel from Resources/{chatPanelResourcesPath} */ }
        return _resolvedChatPanel;
    }

    private PartyManager ResolvePartyManager(bool ensure = true)
    {
        if (_partyManager != null)
            return _partyManager;

        _partyManager = ensure ? PartyManager.EnsureInstance() : PartyManager.Instance;
        return _partyManager;
    }

    private Transform ResolveUiParent()
    {
        var currentCanvas = GetComponentInParent<Canvas>(includeInactive: true);
        if (currentCanvas != null)
            return currentCanvas.transform;

        var anyCanvas = FindObjectOfType<Canvas>(includeInactive: true);
        if (anyCanvas != null)
            return anyCanvas.transform;

        return null;
    }

    private PartyJoinRequestPopupUI ResolveJoinRequestPopup()
    {
        if (IsSceneJoinRequestPopup(joinRequestPopup))
            return joinRequestPopup;

        if (joinRequestPopup != null && !joinRequestPopup.gameObject.scene.IsValid())
        {
            var uiParent = ResolveUiParent();
            if (uiParent != null)
            {
                var popupInstance = Instantiate(joinRequestPopup.gameObject, uiParent, false);
                popupInstance.name = joinRequestPopup.gameObject.name;
                popupInstance.SetActive(false);
                popupInstance.transform.SetAsLastSibling();
                joinRequestPopup = popupInstance.GetComponent<PartyJoinRequestPopupUI>();
                { /* {LogPrefix} Instantiated join request popup from serialized prefab reference */ }
                return joinRequestPopup;
            }
        }

        joinRequestPopup = GetComponentInChildren<PartyJoinRequestPopupUI>(true);
        if (IsSceneJoinRequestPopup(joinRequestPopup))
            return joinRequestPopup;

        joinRequestPopup = FindObjectOfType<PartyJoinRequestPopupUI>(includeInactive: true);
        if (IsSceneJoinRequestPopup(joinRequestPopup))
        {
            { /* {LogPrefix} Resolved existing join request popup from scene */ }
            return joinRequestPopup;
        }

        var popupPrefab = Resources.Load<GameObject>(joinRequestPopupResourcesPath);
        if (popupPrefab == null)
        {
            { /* Cảnh báo: {LogPrefix} Join request popup prefab not found at Resources/{joinRequestPopupResourcesPath} */ }
            return null;
        }

        var parent = ResolveUiParent();
        if (parent == null)
        {
            { /* Cảnh báo: {LogPrefix} Could not resolve UI parent for join request popup */ }
            return null;
        }

        var instance = Instantiate(popupPrefab, parent, false);
        instance.name = popupPrefab.name;
        instance.SetActive(false);
        instance.transform.SetAsLastSibling();
        joinRequestPopup = instance.GetComponent<PartyJoinRequestPopupUI>();

        if (joinRequestPopup == null)
        {
            { /* Cảnh báo: {LogPrefix} Instantiated popup prefab does not contain PartyJoinRequestPopupUI */ }
            Destroy(instance);
            return null;
        }

        { /* {LogPrefix} Instantiated join request popup from Resources/{joinRequestPopupResourcesPath} */ }
        return joinRequestPopup;
    }

    private void SetStatusText(string message, bool isError = true)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = isError ? new Color(1f, 0.55f, 0.55f, 1f) : Color.white;
        }
    }

    private static bool IsSceneChatPanel(ChatPanelUI panel)
    {
        return panel != null && panel.gameObject.scene.IsValid() && panel.gameObject.scene.isLoaded;
    }

    private static bool IsSceneJoinRequestPopup(PartyJoinRequestPopupUI popup)
    {
        return popup != null && popup.gameObject.scene.IsValid() && popup.gameObject.scene.isLoaded;
    }

    private static string ResolveLocalUserId()
    {
        if (GameManager.Instance?.currentPlayerData != null && GameManager.Instance.currentPlayerData.user_id > 0)
            return GameManager.Instance.currentPlayerData.user_id.ToString();

        int userId = PlayerPrefs.GetInt("USER_ID", 0);

        return userId > 0 ? userId.ToString() : string.Empty;
    }

    private static string ResolveLocalCharacterName()
    {
        if (!string.IsNullOrWhiteSpace(GameManager.Instance?.currentPlayerData?.character_name))
            return GameManager.Instance.currentPlayerData.character_name;

        return PlayerPrefs.GetString("USERNAME", "Người chơi");
    }

    private static string ResolveLocalClassName(PlayerDataResponse data)
    {
        if (data == null || string.IsNullOrWhiteSpace(data.element_type))
            return "Khác";

        return ElementHelper.ToVietnamese(data.element_type);
    }

    private static int ResolveCurrentZoneId()
    {
        if (ClientSceneController.Instance != null && ClientSceneController.Instance.CurrentZoneId >= 0)
            return ClientSceneController.Instance.CurrentZoneId;

        return 0;
    }

    private static void ClearList(Transform root)
    {
        if (root == null)
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }

    private static void SetButtonState(Button button, bool active)
    {
        if (button != null)
            button.interactable = !active;
    }
}