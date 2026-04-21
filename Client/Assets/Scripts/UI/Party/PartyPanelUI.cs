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

        Debug.Log($"{LogPrefix} OnEnable | currentTab={_activeTabIndex}", this);

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
            Debug.LogError($"{LogPrefix} PartyManager.EnsureInstance returned null khi mở PartyPanel.", this);
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

        Debug.Log($"{LogPrefix} SelectTab -> {_activeTabIndex}", this);

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
        Debug.Log(
            $"{LogPrefix} HandlePartyStateChanged | partyId={state?.partyId} members={state?.memberCount ?? 0}/{state?.maxMembers ?? 0} leader={PartyManager.Instance?.IsLeader}",
            this);

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
        Debug.Log(
            $"{LogPrefix} HandleSearchResultsUpdated | count={_latestSearchResults.parties?.Length ?? 0} leaders={string.Join(", ", (_latestSearchResults.parties ?? Array.Empty<PartySearchEntryDto>()).Select(p => p.leaderName))}",
            this);

        if (_activeTabIndex == 1)
            RenderSearchResults(_latestSearchResults, "HubEvent");
    }

    private void HandleNearbyPlayersUpdated(NearbyPlayersPayload payload)
    {
        _latestNearbyPlayers = NormalizeNearbyPayload(payload);
        Debug.Log(
            $"{LogPrefix} HandleNearbyPlayersUpdated | count={_latestNearbyPlayers.players?.Length ?? 0} names={string.Join(", ", (_latestNearbyPlayers.players ?? Array.Empty<NearbyPlayerDto>()).Select(p => p.characterName))}",
            this);

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
            Debug.LogWarning($"{LogPrefix} Ignored join request because local player is not party leader. requester={payload.requesterName} userId={payload.requesterUserId}", this);
            return;
        }

        bool requesterAlreadyInParty = _latestPartyState?.members != null
            && _latestPartyState.members.Any(member => string.Equals(member.userId, payload.requesterUserId, StringComparison.Ordinal));
        if (requesterAlreadyInParty)
        {
            Debug.LogWarning($"{LogPrefix} Ignored join request because requester is already in the party. requester={payload.requesterName} userId={payload.requesterUserId}", this);
            return;
        }

        Debug.Log($"{LogPrefix} Join request received | requester={payload.requesterName} userId={payload.requesterUserId} level={payload.requesterLevel} element={payload.requesterElementType} | joinRequestPopup={(joinRequestPopup == null ? "NULL" : joinRequestPopup.gameObject.name)}", this);

        // Ưu tiên dùng panel thông báo riêng nếu đã ghi
        if (joinRequestPopup != null)
        {
            joinRequestPopup.ShowRequest(
                payload,
                onAccept:  (partyId, userId) =>
                {
                    Debug.Log($"{LogPrefix} NotificationPanel Accept | partyId={partyId} userId={userId}", this);
                    ResolvePartyManager()?.AcceptJoinRequest(partyId, userId);
                },
                onDecline: (partyId, userId) =>
                {
                    Debug.Log($"{LogPrefix} NotificationPanel Decline | partyId={partyId} userId={userId}", this);
                    ResolvePartyManager()?.RejectJoinRequest(partyId, userId);
                }
            );
            return;
        }

        // Fallback: xếp hàng cho nút hành động
        _pendingJoinRequests.Enqueue(payload);
        Debug.Log($"{LogPrefix} Join request queued for action button | requester={payload.requesterName} userId={payload.requesterUserId}", this);
        SetStatusText($"{payload.requesterName} xin vào tổ đội. Nhấn nút hành động để chấp nhận nhanh.");
    }

    private void HandlePartyError(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        Debug.LogWarning($"{LogPrefix} Party error: {message}", this);
        SetStatusText(message);
    }

    private void BuildMemberList(PartyStatePayload state)
    {
        ClearList(memberListRoot);
        if (state?.members == null || memberListRoot == null || memberEntryPrefab == null)
        {
            Debug.Log($"{LogPrefix} BuildMemberList skipped | hasState={state != null} root={memberListRoot != null} prefab={memberEntryPrefab != null}", this);
            return;
        }

        Debug.Log($"{LogPrefix} BuildMemberList | count={state.members.Length}", this);

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
        Debug.Log(
            $"{LogPrefix} ActionButton clicked | hasParty={partyManager?.HasParty} leader={partyManager?.IsLeader} connected={partyManager?.IsConnected} pendingJoinRequests={_pendingJoinRequests.Count}",
            this);

        if (partyManager == null)
        {
            Debug.LogError($"{LogPrefix} ActionButton failed because PartyManager is null.", this);
            SetStatusText("PartyManager chưa sẵn sàng.");
            return;
        }

        if (!partyManager.IsConnected)
        {
            Debug.LogWarning($"{LogPrefix} ActionButton blocked because PartyManager is not connected.", this);
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
        Debug.Log($"{LogPrefix} ChatGroupButton clicked.", this);

        if (partyManager == null || !partyManager.HasParty)
        {
            Debug.LogWarning($"{LogPrefix} Không thể mở chat nhóm vì hiện chưa có tổ đội.", this);
            SetStatusText("Bạn chưa có nhóm để chat.");
            return;
        }

        var chatPanel = ResolveChatPanel();
        if (chatPanel == null)
        {
            Debug.LogError($"{LogPrefix} Không resolve được ChatPanelUI để mở chat nhóm.", this);
            SetStatusText("Không tìm thấy ChatPanel.");
            return;
        }

        if (ChatManager.Instance != null && string.IsNullOrWhiteSpace(ChatManager.Instance.CurrentGroupId))
        {
            ChatManager.Instance.JoinGroup(partyManager.CurrentParty.partyId);
            ChatManager.Instance.CurrentSendChannel = ChatChannel.Group;
            Debug.Log($"{LogPrefix} Forced ChatManager.JoinGroup for partyId={partyManager.CurrentParty.partyId}", this);
        }

        chatPanel.OpenOnGroupTab();
        gameObject.SetActive(false);
        Debug.Log($"{LogPrefix} PartyPanel closed after opening group chat.", this);
    }

    private void BuildList<T>(Transform root, GameObject prefab, IReadOnlyList<T> items, Action<GameObject, T> binder, string listName)
    {
        ClearList(root);
        if (root == null || prefab == null || items == null)
        {
            Debug.Log($"{LogPrefix} BuildList skipped | list={listName} root={root != null} prefab={prefab != null} itemsNull={items == null}", this);
            return;
        }

        Debug.Log($"{LogPrefix} BuildList | list={listName} count={items.Count}", this);

        foreach (T item in items)
        {
            GameObject row = Instantiate(prefab, root);
            binder?.Invoke(row, item);
        }
    }

    private void ClosePanel()
    {
        Debug.Log($"{LogPrefix} Close button clicked.", this);
        gameObject.SetActive(false);
    }

    private void OnRefreshSearchClicked()
    {
        Debug.Log($"{LogPrefix} Refresh search button clicked.", this);
        RequestSearchRefresh("RefreshButton");
    }

    private void OnRefreshNearbyClicked()
    {
        Debug.Log($"{LogPrefix} Refresh nearby button clicked.", this);
        RequestNearbyRefresh("RefreshButton");
    }

    private void OnLockToggleChanged(bool isOn)
    {
        var partyManager = ResolvePartyManager();
        UpdateToggleIndicators();
        Debug.Log($"{LogPrefix} Lock toggle changed -> {isOn}", this);

        if (partyManager == null)
        {
            Debug.LogWarning($"{LogPrefix} Lock toggle ignored because PartyManager is null.", this);
            if (lockToggle != null)
                lockToggle.SetIsOnWithoutNotify(_latestPartyState != null && _latestPartyState.isLocked);
            UpdateToggleIndicators();
            return;
        }

        if (!partyManager.HasParty || !partyManager.IsLeader)
        {
            Debug.LogWarning($"{LogPrefix} Lock toggle ignored because local player is not party leader.", this);
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
        Debug.Log($"{LogPrefix} AutoAccept toggle changed -> {isOn}", this);

        if (partyManager == null)
        {
            Debug.LogWarning($"{LogPrefix} AutoAccept toggle ignored because PartyManager is null.", this);
            if (autoAcceptToggle != null)
                autoAcceptToggle.SetIsOnWithoutNotify(_latestPartyState != null && _latestPartyState.autoAccept);
            UpdateToggleIndicators();
            return;
        }

        if (!partyManager.HasParty || !partyManager.IsLeader)
        {
            Debug.LogWarning($"{LogPrefix} AutoAccept toggle ignored because local player is not party leader.", this);
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

        Debug.Log($"{LogPrefix} RenderNearbyPlayers | source={source} population={population}", this);
    }

    private void RequestSearchRefresh(string reason)
    {
        Debug.Log($"{LogPrefix} RequestSearchRefresh | reason={reason}", this);
        ResolvePartyManager()?.RefreshPartiesInCurrentZone();
    }

    private void RequestNearbyRefresh(string reason)
    {
        Debug.Log($"{LogPrefix} RequestNearbyRefresh | reason={reason}", this);
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
                Debug.Log($"{LogPrefix} Injected local player into nearby list | name={localPlayer.characterName}", this);
            }
        }

        return new NearbyPlayersPayload { players = players.ToArray() };
    }

    private NearbyPlayerDto CreateLocalNearbyPlayer(string localUserId)
    {
        var playerData = GameManager.Instance?.GetPlayerData() ?? GameManager.Instance?.currentPlayerData;
        if (playerData == null)
        {
            Debug.LogWarning($"{LogPrefix} Cannot inject local nearby player because PlayerData is null.", this);
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
            Debug.Log($"{LogPrefix} Resolved existing ChatPanelUI from scene.", _resolvedChatPanel);
            return _resolvedChatPanel;
        }

        var chatPrefab = Resources.Load<GameObject>(chatPanelResourcesPath);
        if (chatPrefab == null)
        {
            Debug.LogError($"{LogPrefix} ChatPanel prefab not found at Resources/{chatPanelResourcesPath}.", this);
            return null;
        }

        var uiParent = ResolveUiParent();
        if (uiParent == null)
        {
            Debug.LogError($"{LogPrefix} No Canvas/UI root found to instantiate ChatPanel.", this);
            return null;
        }

        var chatInstance = Instantiate(chatPrefab, uiParent, false);
        chatInstance.name = chatPrefab.name;
        chatInstance.SetActive(false);
        _resolvedChatPanel = chatInstance.GetComponent<ChatPanelUI>();

        Debug.Log($"{LogPrefix} Instantiated ChatPanel from Resources/{chatPanelResourcesPath}.", chatInstance);
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
                Debug.Log($"{LogPrefix} Instantiated join request popup from serialized prefab reference.", popupInstance);
                return joinRequestPopup;
            }
        }

        joinRequestPopup = GetComponentInChildren<PartyJoinRequestPopupUI>(true);
        if (IsSceneJoinRequestPopup(joinRequestPopup))
            return joinRequestPopup;

        joinRequestPopup = FindObjectOfType<PartyJoinRequestPopupUI>(includeInactive: true);
        if (IsSceneJoinRequestPopup(joinRequestPopup))
        {
            Debug.Log($"{LogPrefix} Resolved existing join request popup from scene.", joinRequestPopup);
            return joinRequestPopup;
        }

        var popupPrefab = Resources.Load<GameObject>(joinRequestPopupResourcesPath);
        if (popupPrefab == null)
        {
            Debug.LogWarning($"{LogPrefix} Join request popup prefab not found at Resources/{joinRequestPopupResourcesPath}.", this);
            return null;
        }

        var parent = ResolveUiParent();
        if (parent == null)
        {
            Debug.LogWarning($"{LogPrefix} Could not resolve UI parent for join request popup.", this);
            return null;
        }

        var instance = Instantiate(popupPrefab, parent, false);
        instance.name = popupPrefab.name;
        instance.SetActive(false);
        instance.transform.SetAsLastSibling();
        joinRequestPopup = instance.GetComponent<PartyJoinRequestPopupUI>();

        if (joinRequestPopup == null)
        {
            Debug.LogWarning($"{LogPrefix} Instantiated popup prefab does not contain PartyJoinRequestPopupUI.", instance);
            Destroy(instance);
            return null;
        }

        Debug.Log($"{LogPrefix} Instantiated join request popup from Resources/{joinRequestPopupResourcesPath}.", instance);
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