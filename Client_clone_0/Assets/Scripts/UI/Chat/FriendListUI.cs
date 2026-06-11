using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Panel quản lý bạn bè với 3 tab:
// 0 = Bạn Bè  |  1 = Kết Bạn Mới  |  2 = Lời Mời
public class FriendListUI : MonoBehaviour
{
    private const string GameplayBlockSource = "FriendListUI";
    private const string FriendRowEntryResourcePath = "Prefabs/Chat/FriendRowEntry";

    // Inspector Refs

    [Header("Header")]
    [SerializeField] private Button            closeButton;
    [SerializeField] private TextMeshProUGUI   titleLabel;

    [Header("Tab Buttons (0=Bạn bè, 1=Kết bạn mới, 2=Lời mời)")]
    [SerializeField] private Button            tabFriendsBtn;
    [SerializeField] private Button            tabAddBtn;
    [SerializeField] private Button            tabPendingBtn;
    [SerializeField] private TextMeshProUGUI   tabPendingBadge;

    [Header("Tab Panels")]
    [SerializeField] private GameObject        panelFriends;
    [SerializeField] private GameObject        panelAdd;
    [SerializeField] private GameObject        panelPending;

    [Header("Tab Bạn Bè")]
    [SerializeField] private Transform         friendListContent;
    [SerializeField] private GameObject        friendEntryPrefab;
    [SerializeField] private TextMeshProUGUI   emptyFriendLabel;

    [Header("Tab Kết Bạn Mới")]
    [SerializeField] private TMP_InputField    searchInput;
    [SerializeField] private Button            searchButton;
    [SerializeField] private Transform         searchResultContent;
    [SerializeField] private GameObject        searchResultEntryPrefab;
    [SerializeField] private TextMeshProUGUI   searchHintLabel;

    [Header("Tab Lời Mời")]
    [SerializeField] private Transform         pendingContent;
    [SerializeField] private GameObject        pendingEntryPrefab;
    [SerializeField] private TextMeshProUGUI   emptyPendingLabel;

    [Header("Player Profile Panel (tuỳ chọn)")]
    [SerializeField] private PlayerProfilePanelUI profilePanel;

    // State

    private int                          _activeTab = 0;
    private readonly List<GameObject>    _friendRows  = new();
    private readonly List<GameObject>    _searchRows  = new();
    private readonly List<GameObject>    _pendingRows = new();
    private RectTransform                _rectTransform;
    private Image                        _backdropImage;
    private Button                       _backdropButton;
    private Color                        _searchHintDefaultColor = Color.white;
    private Coroutine                    _searchFeedbackCoroutine;
    private GameObject                   _fallbackRowEntryPrefab;

    // MonoBehaviour

    private void Awake()
    {
        _rectTransform = transform as RectTransform;
        UIPanelManager.Register(gameObject, () => HidePanel("UIPanelManager"));
        UIDraggablePanel.Ensure(gameObject);

        closeButton?.onClick.AddListener(() => HidePanel("CloseButton"));

        tabFriendsBtn?.onClick.AddListener(() => SwitchTab(0));
        tabAddBtn    ?.onClick.AddListener(() => SwitchTab(1));
        tabPendingBtn?.onClick.AddListener(() => SwitchTab(2));

        searchButton?.onClick.AddListener(HandleSearchButtonClicked);

        ConfigureNonBlockingText(searchHintLabel, nameof(searchHintLabel));
        ConfigureNonBlockingText(emptyFriendLabel, nameof(emptyFriendLabel));
        ConfigureNonBlockingText(emptyPendingLabel, nameof(emptyPendingLabel));
        ConfigureSearchResultsRaycast();
        ConfigureStatusLabels();

        if (searchHintLabel != null)
            _searchHintDefaultColor = searchHintLabel.color;

        if (searchInput != null)
        {
            searchInput.readOnly = false;
            searchInput.interactable = true;
        }

        // Block game input khi gõ vào ô tìm kiếm
        searchInput?.onSelect.AddListener(_ =>
        {
            LogSearchUiEvent("SearchInput", "Selected");
            InputManager.Instance?.SetInputEnabled(false);
        });
        searchInput?.onDeselect.AddListener(_ =>
        {
            LogSearchUiEvent("SearchInput", "Deselected");
            InputManager.Instance?.SetInputEnabled(true);
        });
        searchInput?.onSubmit.AddListener(_ =>
        {
            LogSearchUiEvent("SearchInput", "Submit");
            OnSearchRequested("InputSubmit");
        });
        searchInput?.onEndEdit.AddListener(t =>
        {
            { /* SearchInput EndEdit text='{t}' returnPressed={Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)} */ }
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                OnSearchRequested("EndEditReturn");
        });

        AttachSearchDebugTriggers();

        { /* Awake refs: close={closeButton != null} tabs={tabFriendsBtn != null}/{tabAddBtn != null}/{tabPendingBtn != null} searchInput={searchInput != null} searchButton={searchButton != null} searchHint={searchHintLabel != null} friendContent={friendListContent != null} pendingContent={pendingContent != null} friendEntryPrefab={DescribeObject(friendEntryPrefab)} searchResultEntryPrefab={DescribeObject(searchResultEntryPrefab)} pendingEntryPrefab={DescribeObject(pendingEntryPrefab)} */ }
    }

    private void OnEnable()
    {
        { /* OnEnable activeTab={_activeTab} activeSelf={gameObject.activeSelf} */ }
        EnsureBackdrop();
        BringToFront();
        EnsurePanelVisible();
        SetBackdropActive(true);

        InputManager.Instance?.SetGameplayInputBlocked(GameplayBlockSource, true);
        InputManager.Instance?.CancelAutoMove();

        TargetSelector.ClearTarget();
        EnemyClickHandler.DeselectCurrent();
        NpcInteraction.DeselectCurrent();
        EnemyInfoPanel.Instance?.Hide();
        EventSystem.current?.SetSelectedGameObject(null);

        SwitchTab(_activeTab);
        var friendManager = FriendManager.EnsureInstance();
        if (friendManager != null)
        {
            friendManager.OnFriendListLoaded += RefreshAllTabs;
            friendManager.OnRequestSent      += OnRequestSent;
            friendManager.OnError            += OnFriendError;

            if (friendManager.HasLoadedFriends)
                RefreshAllTabs(friendManager.Friends);

            friendManager.LoadFriends();
        }
        else
        {
            { /* Cảnh báo: FriendManager.EnsureInstance returned NULL when panel opened */ }
        }
    }

    private void OnDisable()
    {
        { /* OnDisable */ }
        SetBackdropActive(false);
        InputManager.Instance?.SetGameplayInputBlocked(GameplayBlockSource, false);
        InputManager.Instance?.SetInputEnabled(true);  // đảm bảo input được mở lại
        EventSystem.current?.SetSelectedGameObject(null);
        var friendManager = FriendManager.Instance;
        if (friendManager != null)
        {
            friendManager.OnFriendListLoaded -= RefreshAllTabs;
            friendManager.OnRequestSent      -= OnRequestSent;
            friendManager.OnError            -= OnFriendError;
        }
    }

    public void TogglePanel(string source = "Unknown") => SetPanelVisible(!gameObject.activeSelf, source);

    public void ShowPanel(string source = "Unknown") => SetPanelVisible(true, source);

    public void HidePanel(string source = "Unknown") => SetPanelVisible(false, source);

    private void SetPanelVisible(bool visible, string source)
    {
        { /* SetPanelVisible source={source} visible={visible} currentActive={gameObject.activeSelf} */ }

        if (visible)
        {
            if (!gameObject.activeSelf)
            {
                UIPanelManager.CloseOthers(gameObject);
                gameObject.SetActive(true);
                UIPanelManager.NotifyOpened(gameObject);
            }
            else
            {
                BringToFront();
                EnsurePanelVisible();
                SetBackdropActive(true);
            }

            return;
        }

        if (gameObject.activeSelf)
            gameObject.SetActive(false);
        UIPanelManager.NotifyClosed(gameObject);
    }

    // Tab switching

    private void SwitchTab(int tab)
    {
        _activeTab = tab;
        if (panelFriends != null) panelFriends.SetActive(tab == 0);
        if (panelAdd     != null) panelAdd    .SetActive(tab == 1);
        if (panelPending != null) panelPending.SetActive(tab == 2);

        // Highlight tab
        SetTabHighlight(tabFriendsBtn, tab == 0);
        SetTabHighlight(tabAddBtn,     tab == 1);
        SetTabHighlight(tabPendingBtn, tab == 2);

        if (tab == 1 && searchHintLabel != null)
            searchHintLabel.gameObject.SetActive(_searchRows.Count == 0);

        if (tab == 1)
            FocusSearchInput();

        { /* Switched tab -> {tab} friendsActive={panelFriends != null && panelFriends.activeSelf} addActive={panelAdd != null && panelAdd.activeSelf} pendingActive={panelPending != null && panelPending.activeSelf} hintActive={searchHintLabel != null && searchHintLabel.gameObject.activeSelf} */ }
    }

    private static void SetTabHighlight(Button btn, bool active)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null)
            img.color = active
                ? new Color(0.7f, 0.5f, 0.1f, 1f)
                : new Color(0.25f, 0.18f, 0.07f, 1f);
    }

    // Refresh tất cả tabs

    private void RefreshAllTabs(List<FriendEntryDto> friends)
    {
        ClearRows(_friendRows);
        ClearRows(_pendingRows);

        int pendingReceivedCount = 0;
        int pendingSentCount = 0;
        foreach (var f in friends)
        {
            switch (f.status)
            {
                case "accepted":
                    var row = BuildFriendRow(f, friendListContent ?? pendingContent);
                    if (row != null) _friendRows.Add(row);
                    break;
                case "pending_received":
                    pendingReceivedCount++;
                    var preq = BuildPendingRow(f, pendingContent ?? friendListContent, true);
                    if (preq != null) _pendingRows.Add(preq);
                    break;
                case "pending_sent":
                    pendingSentCount++;
                    var sent = BuildPendingRow(f, pendingContent ?? friendListContent, false);
                    if (sent != null) _pendingRows.Add(sent);
                    break;
                default:
                    { /* Cảnh báo: Unknown friend status '{f.status}' for user '{f.username}' relationId={f.relationId} */ }
                    break;
            }
        }

        if (emptyFriendLabel != null)
            emptyFriendLabel.gameObject.SetActive(_friendRows.Count == 0);
        if (emptyPendingLabel != null)
            emptyPendingLabel.gameObject.SetActive(_pendingRows.Count == 0);

        RefreshListLayout(friendListContent, "Friends");
        RefreshListLayout(pendingContent, "Pending");

        // Badge
        if (tabPendingBadge != null)
        {
            tabPendingBadge.gameObject.SetActive(pendingReceivedCount > 0);
            tabPendingBadge.text = pendingReceivedCount > 9 ? "9+" : pendingReceivedCount.ToString();
        }

        { /* RefreshAllTabs friends={_friendRows.Count} pendingRows={_pendingRows.Count} pendingReceived={pendingReceivedCount} pendingSent={pendingSentCount} pendingContentChildren={pendingContent?.childCount ?? -1} emptyPendingActive={emptyPendingLabel != null && emptyPendingLabel.gameObject.activeSelf} */ }
    }

    // Search

    private void HandleSearchButtonClicked()
    {
        LogSearchUiEvent("SearchButton", "OnClick");
        OnSearchRequested("SearchButton");
    }

    private void OnSearchRequested(string source)
    {
        var q = searchInput?.text?.Trim();
        { /* Search requested source={source} query='{q}' activeTab={_activeTab} panelAddActive={panelAdd != null && panelAdd.activeSelf} buttonActive={searchButton != null && searchButton.gameObject.activeInHierarchy} buttonInteractable={searchButton != null && searchButton.interactable} resultContent={searchResultContent != null} */ }

        if (string.IsNullOrEmpty(q) || q.Length < 2)
        {
            SetSearchFeedback("Nhập ít nhất 2 ký tự để tìm người chơi.", new Color(1f, 0.85f, 0.35f));
            { /* Cảnh báo: Search aborted source={source} because query is too short */ }
            return;
        }

        SetSearchFeedback($"Đang tìm '{q}'...", new Color(1f, 0.95f, 0.5f));

        var friendManager = FriendManager.EnsureInstance();
        if (friendManager == null)
        {
            { /* Cảnh báo: Search aborted source={source} because FriendManager.EnsureInstance returned NULL */ }
            SetSearchFeedback("Hệ thống bạn bè chưa sẵn sàng. Vui lòng thử lại sau.", new Color(1f, 0.45f, 0.45f));
            return;
        }

        { /* Dispatching SearchUsers query='{q}' source={source} */ }
        friendManager.SearchUsers(q, results =>
        {
            { /* Search result count={results.Count} query='{q}' source={source} */ }
            ClearRows(_searchRows);

            if (results.Count == 0)
                SetSearchFeedback($"Không tìm thấy người chơi nào khớp '{q}'.", _searchHintDefaultColor);

            foreach (var r in results)
            {
                var row = BuildSearchRow(r, searchResultContent);
                if (row != null) _searchRows.Add(row);
            }

            RefreshSearchResultsLayout();

            if (results.Count > 0)
                SetSearchFeedback($"Tìm thấy {results.Count} người chơi cho '{q}'.", new Color(0.55f, 1f, 0.55f), 2.5f);
        });
    }

    // Row Builders

    private GameObject BuildFriendRow(FriendEntryDto f, Transform parent)
    {
        if (parent == null) return null;
        var go = InstantiateRowPrefab(friendEntryPrefab, parent, nameof(friendEntryPrefab))
            ?? MakeDefaultRow(parent);

        string displayName = ResolveFriendDisplayName(f);

        SetText(go, "NameText", displayName);
        SetButtonLabel(go, "ChatButton", "Chat");
        SetButtonLabel(go, "ProfileButton", "Xem");
        SetButtonLabel(go, "DeleteButton", "Xoa");

        // Chat
        BindButton(go, "ChatButton", () => OnChatWithFriend(f.friendUserId, displayName));

        // Xem thông tin
        BindButton(go, "ProfileButton", () => OpenProfile(f.friendUserId, displayName));

        // Xóa bạn
        BindButton(go, "DeleteButton", () =>
            FriendManager.Instance?.RemoveFriend(f.relationId, () => FriendManager.Instance.LoadFriends()));

        // Ẩn các btn không thuộc tab bạn bè
        SetChildActive(go, "AcceptButton", false);
        SetChildActive(go, "AddButton",    false);
        ApplyRowPresentation(go);

        return go;
    }

    private GameObject BuildPendingRow(FriendEntryDto f, Transform parent, bool isIncoming)
    {
        if (parent == null) return null;
        var go = InstantiateRowPrefab(pendingEntryPrefab, parent, nameof(pendingEntryPrefab))
            ?? MakeDefaultRow(parent);

        string displayName = ResolveFriendDisplayName(f);

        SetText(go, "NameText", isIncoming ? displayName : $"{displayName} (đã gửi)");

        if (isIncoming)
        {
            SetButtonLabel(go, "AcceptButton", "Nhan");
            BindButton(go, "AcceptButton", () =>
            {
                { /* Accept friend request relationId={f.relationId} username={f.username} displayName={displayName} */ }
                FriendManager.Instance?.AcceptFriendRequest(f.relationId, () => FriendManager.Instance.LoadFriends());
            });

            BindButton(go, "DeleteButton", () =>
            {
                { /* Reject friend request relationId={f.relationId} username={f.username} displayName={displayName} */ }
                FriendManager.Instance?.RemoveFriend(f.relationId, () => FriendManager.Instance.LoadFriends());
            });
            SetButtonLabel(go, "DeleteButton", "Tu choi");
        }
        else
        {
            BindButton(go, "DeleteButton", () =>
            {
                { /* Cancel sent friend request relationId={f.relationId} username={f.username} displayName={displayName} */ }
                FriendManager.Instance?.RemoveFriend(f.relationId, () => FriendManager.Instance.LoadFriends());
            });
            SetButtonLabel(go, "DeleteButton", "Huy");
        }

        SetChildActive(go, "ChatButton",    false);
        SetChildActive(go, "ProfileButton", false);
        SetChildActive(go, "AcceptButton",  isIncoming);
        SetChildActive(go, "AddButton",     false);
        ApplyRowPresentation(go);

        { /* Built pending row username='{f.username}' displayName='{displayName}' relationId={f.relationId} isIncoming={isIncoming} rowName={go.name} parentChildCount={parent.childCount} activeInHierarchy={go.activeInHierarchy} */ }

        return go;
    }

    private GameObject BuildSearchRow(UserSearchResult r, Transform parent)
    {
        if (parent == null) return null;
        var go = InstantiateSearchRowPrefab(parent)
            ?? MakeDefaultRow(parent);

        string displayName = ResolveSearchDisplayName(r);

        go.name = $"SearchResult_{r.userId}";

        SetText(go, "NameText", displayName);
        SetButtonLabel(go, "AddButton", "Kết bạn");

        // "Kết Bạn" — bấm sẽ gửi yêu cầu rồi disable btn
        var addBtn = go.transform.Find("AddButton")?.GetComponent<Button>();
        if (addBtn != null)
        {
            int uid = r.userId;
            addBtn.onClick.AddListener(() =>
            {
                { /* Send friend request button clicked targetUserId={uid} username={r.username} displayName={displayName} */ }
                addBtn.interactable = false;
                SetText(go, "AddButton/Label", "Đã gửi");
                ApplyRowPresentation(go);
                FriendManager.Instance?.SendFriendRequest(uid, () =>
                {
                    { /* Send friend request success targetUserId={uid} username={r.username} displayName={displayName} */ }
                    SetSearchFeedback($"Đã gửi lời mời kết bạn tới {displayName}.", new Color(0.55f, 1f, 0.55f), 2.5f);
                });
            });
        }

        { /* Built search row username='{r.username}' displayName='{displayName}' userId={r.userId} rowName={go.name} parentChildCount={parent.childCount} activeInHierarchy={go.activeInHierarchy} */ }

        SetChildActive(go, "ChatButton",    false);
        SetChildActive(go, "ProfileButton", false);
        SetChildActive(go, "AcceptButton",  false);
        SetChildActive(go, "DeleteButton",  false);
        SetChildActive(go, "AddButton",     true);
        ApplyRowPresentation(go);

        return go;
    }

    // Actions

    private void OnChatWithFriend(int userId, string username)
    {
        var chatPanel = FindObjectOfType<ChatPanelUI>(includeInactive: true);
        if (chatPanel != null)
        {
            chatPanel.gameObject.SetActive(true);
            chatPanel.OpenPrivateChat(userId, username);
        }
    }

    private void OpenProfile(int userId, string username)
    {
        { /* OpenProfile userId={userId} username='{username}' */ }
        if (profilePanel == null)
            profilePanel = FindObjectOfType<PlayerProfilePanelUI>(includeInactive: true);
        if (profilePanel == null)
        {
            { /* Cảnh báo: PlayerProfilePanelUI bridge not found in scene */ }
            return;
        }

        HidePanel("FriendListUI.OpenProfile");
        profilePanel.LoadProfile(userId, username);
    }

    // Hàm hỗ trợ dùng nội bộ để tách nhỏ xử lý chính.

    private void OnRequestSent()
    {
        { /* OnRequestSent fired */ }
        if (_activeTab == 1 && searchHintLabel != null)
            SetSearchFeedback("Đã gửi lời mời kết bạn.", new Color(0.55f, 1f, 0.55f), 2.5f);
    }

    private void OnFriendError(string err)
    {
        { /* Cảnh báo: {err} */ }
        if (_activeTab == 1 && searchHintLabel != null)
            SetSearchFeedback(BuildUserFacingFriendError(err), new Color(1f, 0.45f, 0.45f));
    }

    private void EnsureBackdrop()
    {
        if (_backdropImage != null) return;

        var parent = transform.parent as RectTransform;
        if (parent == null)
        {
            { /* Cảnh báo: Cannot create backdrop because panel has no RectTransform parent */ }
            return;
        }

        var backdropName = gameObject.name + "_Backdrop";
        var existing = parent.Find(backdropName);
        GameObject backdropGo;
        if (existing != null)
        {
            backdropGo = existing.gameObject;
        }
        else
        {
            backdropGo = new GameObject(backdropName, typeof(RectTransform), typeof(Image), typeof(Button));
            backdropGo.transform.SetParent(parent, false);
        }

        var backdropRect = backdropGo.GetComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;

        _backdropImage = backdropGo.GetComponent<Image>();
        _backdropImage.color = new Color(0f, 0f, 0f, 0.08f);
        _backdropImage.raycastTarget = true;

        _backdropButton = backdropGo.GetComponent<Button>();
        _backdropButton.transition = Selectable.Transition.None;
        _backdropButton.onClick.RemoveAllListeners();
        _backdropButton.onClick.AddListener(() => HidePanel("Backdrop"));

        backdropGo.SetActive(false);
    }

    private void SetBackdropActive(bool active)
    {
        if (_backdropImage == null) return;
        _backdropImage.gameObject.SetActive(active);
        if (active)
            BringToFront();
    }

    private void BringToFront()
    {
        transform.SetAsLastSibling();
        if (_backdropImage != null)
        {
            _backdropImage.transform.SetAsLastSibling();
            _backdropImage.transform.SetSiblingIndex(Mathf.Max(0, transform.GetSiblingIndex() - 1));
        }
    }

    private void EnsurePanelVisible()
    {
        if (_rectTransform == null)
            return;

        if (UIDraggablePanel.ClampToRootCanvas(_rectTransform))
            { /* Cảnh báo: Panel position was adjusted to stay inside the root canvas */ }
    }

    private void FocusSearchInput()
    {
        if (searchInput == null)
        {
            { /* Cảnh báo: FocusSearchInput skipped because searchInput is NULL */ }
            return;
        }

        if (!searchInput.gameObject.activeInHierarchy)
        {
            { /* Cảnh báo: FocusSearchInput skipped because searchInput is not active in hierarchy */ }
            return;
        }

        searchInput.readOnly = false;
        searchInput.interactable = true;

        Canvas.ForceUpdateCanvases();
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(searchInput.gameObject);

        searchInput.Select();
        searchInput.ActivateInputField();
        LogSearchUiEvent("SearchInput", "FocusedProgrammatically");
    }

    private void ConfigureNonBlockingText(TextMeshProUGUI text, string labelName)
    {
        if (text == null)
            return;

        if (!text.raycastTarget)
            return;

        { /* Cảnh báo: {labelName} had raycastTarget=true and could block clicks. Forcing raycastTarget=false */ }
        text.raycastTarget = false;
    }

    private void AttachSearchDebugTriggers()
    {
        AttachPointerDebug(searchInput != null ? searchInput.gameObject : null, "SearchInput");
        AttachPointerDebug(searchButton != null ? searchButton.gameObject : null, "SearchButton");
        AttachPointerDebug(searchHintLabel != null ? searchHintLabel.gameObject : null, "SearchHintLabel");

        var searchScrollRect = searchResultContent != null ? searchResultContent.GetComponentInParent<ScrollRect>() : null;
        if (searchScrollRect != null)
        {
            AttachPointerDebug(searchScrollRect.gameObject, "SearchResultScrollView");
            if (searchScrollRect.viewport != null)
                AttachPointerDebug(searchScrollRect.viewport.gameObject, "SearchResultViewport");
        }
    }

    private void ConfigureSearchResultsRaycast()
    {
        var searchScrollRect = searchResultContent != null ? searchResultContent.GetComponentInParent<ScrollRect>() : null;
        if (searchScrollRect == null)
        {
            { /* Cảnh báo: Could not resolve SearchResultScrollView from searchResultContent */ }
            return;
        }

        var rootGraphic = searchScrollRect.GetComponent<Graphic>();
        if (rootGraphic == null)
        {
            { /* Cảnh báo: SearchResultScrollView has no root Graphic to configure */ }
            return;
        }

        if (!rootGraphic.raycastTarget)
            return;

        { /* Cảnh báo: SearchResultScrollView root graphic '{rootGraphic.GetType().Name}' had raycastTarget=true and was covering SearchBar/SearchButton. Forcing raycastTarget=false */ }
        rootGraphic.raycastTarget = false;
    }

    private void ConfigureStatusLabels()
    {
        ConfigureStatusLabel(searchHintLabel, anchorBottom: true);
        ConfigureStatusLabel(emptyFriendLabel, anchorBottom: false);
        ConfigureStatusLabel(emptyPendingLabel, anchorBottom: false);
    }

    private void RefreshSearchResultsLayout()
    {
        if (searchResultContent == null)
            return;

        Canvas.ForceUpdateCanvases();

        if (searchResultContent is RectTransform contentRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        var searchScrollRect = searchResultContent.GetComponentInParent<ScrollRect>();
        if (searchScrollRect != null)
        {
            if (searchScrollRect.viewport != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(searchScrollRect.viewport);

            searchScrollRect.normalizedPosition = new Vector2(0f, 1f);
        }

        var contentSize = searchResultContent is RectTransform rect ? rect.rect.size.ToString() : "n/a";
        { /* RefreshSearchResultsLayout childCount={searchResultContent.childCount} contentSize={contentSize} */ }
    }

    private void RefreshListLayout(Transform content, string listName)
    {
        if (content == null)
            return;

        Canvas.ForceUpdateCanvases();

        if (content is RectTransform contentRect)
        {
            if (contentRect.sizeDelta.y < 0f)
                contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, 0f);

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }

        var scrollRect = content.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
        {
            if (scrollRect.viewport != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.viewport);

            scrollRect.normalizedPosition = new Vector2(0f, 1f);
        }

        var size = content is RectTransform rect ? rect.rect.size.ToString() : "n/a";
        { /* RefreshListLayout list={listName} childCount={content.childCount} contentSize={size} */ }
    }

    private void SetSearchFeedback(string message, Color color, float autoHideAfterSeconds = -1f)
    {
        if (searchHintLabel == null)
            return;

        if (_searchFeedbackCoroutine != null)
        {
            StopCoroutine(_searchFeedbackCoroutine);
            _searchFeedbackCoroutine = null;
        }

        searchHintLabel.gameObject.SetActive(true);
        searchHintLabel.color = color;
        searchHintLabel.text = message;

        if (autoHideAfterSeconds > 0f)
            _searchFeedbackCoroutine = StartCoroutine(HideSearchFeedbackAfterDelay(autoHideAfterSeconds));
    }

    private static void ConfigureStatusLabel(TextMeshProUGUI label, bool anchorBottom)
    {
        if (label == null)
            return;

        label.fontSize = Mathf.Max(label.fontSize * 2f, 24f);
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.margin = Vector4.zero;
        label.raycastTarget = false;

        if (label.transform is not RectTransform rect)
            return;

        rect.pivot = new Vector2(0.5f, 0.5f);

        if (anchorBottom)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(0f, 26f);
            rect.sizeDelta = new Vector2(-48f, 52f);
            return;
        }

        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(-40f, 52f);
    }

    private System.Collections.IEnumerator HideSearchFeedbackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        _searchFeedbackCoroutine = null;

        if (searchHintLabel == null)
            yield break;

        searchHintLabel.color = _searchHintDefaultColor;
        searchHintLabel.gameObject.SetActive(_searchRows.Count == 0);
    }

    private static void SetSearchRowButtonWidth(GameObject row, string buttonName, float minWidth)
    {
        var layoutElement = row.transform.Find(buttonName)?.GetComponent<LayoutElement>();
        if (layoutElement != null)
            layoutElement.minWidth = minWidth;
    }

    private static string BuildUserFacingFriendError(string rawError)
    {
        if (string.IsNullOrWhiteSpace(rawError))
            return "Đã có lỗi xảy ra. Vui lòng thử lại.";

        var lower = rawError.ToLowerInvariant();
        if (lower.Contains("401") || lower.Contains("unauthorized") || lower.Contains("jwt"))
            return "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
        if (lower.Contains("404"))
            return "Không tìm thấy dữ liệu phù hợp trên máy chủ.";
        if (lower.Contains("500") || lower.Contains("internal server error"))
            return "Máy chủ đang bận. Vui lòng thử lại sau.";

        const string detailMarker = "detail=";
        var detailIndex = lower.IndexOf(detailMarker);
        if (detailIndex >= 0)
            return rawError.Substring(detailIndex + detailMarker.Length).Trim();

        return rawError;
    }

    private void AttachPointerDebug(GameObject target, string elementName)
    {
        if (target == null)
            return;

        var trigger = target.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = target.AddComponent<EventTrigger>();

        trigger.triggers ??= new List<EventTrigger.Entry>();

        AddTrigger(trigger, EventTriggerType.PointerDown, eventData => LogSearchUiEvent(elementName, "PointerDown", eventData));
        AddTrigger(trigger, EventTriggerType.PointerUp, eventData => LogSearchUiEvent(elementName, "PointerUp", eventData));
        AddTrigger(trigger, EventTriggerType.PointerClick, eventData => LogSearchUiEvent(elementName, "PointerClick", eventData));
    }

    private static void AddTrigger(EventTrigger trigger, EventTriggerType eventType, System.Action<BaseEventData> callback)
    {
        var entry = new EventTrigger.Entry { eventID = eventType };
        entry.callback.AddListener(eventData => callback(eventData));
        trigger.triggers.Add(entry);
    }

    private void LogSearchUiEvent(string elementName, string eventName, BaseEventData eventData = null)
    {
        var pointerEvent = eventData as PointerEventData;
        var pointerPos = pointerEvent != null ? pointerEvent.position.ToString() : "n/a";
        var selectedName = EventSystem.current?.currentSelectedGameObject != null
            ? EventSystem.current.currentSelectedGameObject.name
            : "NULL";

        { /* {elementName} {eventName} pointer={pointerPos} selected={selectedName} activeTab={_activeTab} panelAddActive={panelAdd != null && panelAdd.activeSelf} */ }
    }

    private static void ClearRows(List<GameObject> list)
    {
        foreach (var go in list) if (go != null) Destroy(go);
        list.Clear();
    }

    private static void SetText(GameObject go, string path, string text)
    {
        var t = go.transform.Find(path);
        if (t == null) return;
        var tmp = t.GetComponent<TextMeshProUGUI>();
        if (tmp != null) tmp.text = text;
    }

    private static void SetButtonLabel(GameObject go, string buttonName, string text)
    {
        SetText(go, buttonName + "/Label", text);
    }

    private static void BindButton(GameObject go, string childName, System.Action action)
    {
        var btn = go.transform.Find(childName)?.GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(() => action());
    }

    private static void SetChildActive(GameObject go, string childName, bool active)
    {
        var t = go.transform.Find(childName);
        if (t != null) t.gameObject.SetActive(active);
    }

    private void ApplyRowPresentation(GameObject row)
    {
        if (row == null)
            return;

        if (row.TryGetComponent<HorizontalLayoutGroup>(out var rowLayout))
        {
            rowLayout.padding = new RectOffset(6, 6, 4, 4);
            rowLayout.spacing = 4f;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childControlWidth = true;
        }

        if (row.TryGetComponent<LayoutElement>(out var rowElement))
        {
            rowElement.minHeight = Mathf.Max(rowElement.minHeight, 50f);
            rowElement.preferredHeight = Mathf.Max(rowElement.preferredHeight, 50f);
        }

        ConfigureNameLabel(row.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>());
        ConfigureButtonVisual(row, "ChatButton");
        ConfigureButtonVisual(row, "ProfileButton");
        ConfigureButtonVisual(row, "AcceptButton");
        ConfigureButtonVisual(row, "AddButton");
        ConfigureButtonVisual(row, "DeleteButton");
    }

    private static void ConfigureNameLabel(TextMeshProUGUI label)
    {
        if (label == null)
            return;

        label.fontSize = 22f;
        label.enableAutoSizing = true;
        label.fontSizeMin = 15f;
        label.fontSizeMax = 22f;
        label.alignment = TextAlignmentOptions.Left;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.margin = new Vector4(4f, 0f, 8f, 0f);
        label.raycastTarget = false;

        var layoutElement = label.GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            layoutElement.flexibleWidth = 1f;
            layoutElement.minWidth = 120f;
        }
    }

    private static void ConfigureButtonVisual(GameObject row, string buttonName)
    {
        var buttonTransform = row.transform.Find(buttonName);
        if (buttonTransform == null)
            return;

        var label = buttonTransform.Find("Label")?.GetComponent<TextMeshProUGUI>();
        if (label != null)
        {
            label.fontSize = 15f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 10f;
            label.fontSizeMax = 15f;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.margin = Vector4.zero;
            label.raycastTarget = false;
        }

        var layoutElement = buttonTransform.GetComponent<LayoutElement>();
        if (layoutElement == null)
            return;

        if (!buttonTransform.gameObject.activeSelf)
        {
            layoutElement.minWidth = 0f;
            layoutElement.preferredWidth = 0f;
            return;
        }

        string labelText = label != null ? label.text : buttonName;
        float width = Mathf.Clamp(20f + (labelText?.Length ?? 0) * 10f, 44f, 92f);
        layoutElement.minWidth = width;
        layoutElement.preferredWidth = width;
    }

    private static string ResolveFriendDisplayName(FriendEntryDto entry)
    {
        if (entry == null)
            return string.Empty;

        return string.IsNullOrWhiteSpace(entry.characterName) ? entry.username : entry.characterName;
    }

    private static string ResolveSearchDisplayName(UserSearchResult result)
    {
        if (result == null)
            return string.Empty;

        return string.IsNullOrWhiteSpace(result.characterName) ? result.username : result.characterName;
    }

    private GameObject InstantiateSearchRowPrefab(Transform parent)
    {
        var row = InstantiateRowPrefab(searchResultEntryPrefab, parent, nameof(searchResultEntryPrefab));
        if (row != null)
            return row;

        row = InstantiateRowPrefab(friendEntryPrefab, parent, nameof(friendEntryPrefab) + " fallback");
        if (row != null)
            return row;

        row = InstantiateRowPrefab(pendingEntryPrefab, parent, nameof(pendingEntryPrefab) + " fallback");
        if (row != null)
            return row;

        var resourcesPrefab = Resources.Load<GameObject>("Prefabs/Chat/FriendRowEntry");
        row = InstantiateRowPrefab(resourcesPrefab, parent, "Resources/Prefabs/Chat/FriendRowEntry");
        if (row != null)
            return row;

        { /* Cảnh báo: Could not resolve any row prefab for search results. Falling back to MakeDefaultRow */ }
        return null;
    }

    private GameObject InstantiateRowPrefab(GameObject configuredPrefab, Transform parent, string source)
    {
        if (TryInstantiateConfiguredRowPrefab(configuredPrefab, parent, source, out var instance))
            return instance;

        var fallbackPrefab = GetFallbackRowEntryPrefab();
        if (fallbackPrefab != null && !ReferenceEquals(fallbackPrefab, configuredPrefab))
        {
            OverrideBrokenRowPrefabReference(source, fallbackPrefab);

            if (TryInstantiateConfiguredRowPrefab(fallbackPrefab, parent, source + " -> fallbackResource", out instance))
                return instance;
        }

        return null;
    }

    private bool TryInstantiateConfiguredRowPrefab(GameObject configuredPrefab, Transform parent, string source, out GameObject instance)
    {
        instance = null;

        if (configuredPrefab == null)
        {
            { /* Cảnh báo: Row prefab source='{source}' is NULL */ }
            return false;
        }

        try
        {
            instance = Instantiate(configuredPrefab, parent, false);
            return instance != null;
        }
        catch (System.Exception ex)
        {
            { /* Cảnh báo: Failed to instantiate row prefab source='{source}' prefab={DescribeObject(configuredPrefab)} error={ex.GetType().Name}: {ex.Message} */ }
            return false;
        }
    }

    private GameObject GetFallbackRowEntryPrefab()
    {
        if (_fallbackRowEntryPrefab != null)
            return _fallbackRowEntryPrefab;

        _fallbackRowEntryPrefab = Resources.Load<GameObject>(FriendRowEntryResourcePath);
        { /* Loaded fallback row prefab path='{FriendRowEntryResourcePath}' result={DescribeObject(_fallbackRowEntryPrefab)} */ }
        return _fallbackRowEntryPrefab;
    }

    private void OverrideBrokenRowPrefabReference(string source, GameObject fallbackPrefab)
    {
        if (fallbackPrefab == null)
            return;

        if (source.StartsWith(nameof(pendingEntryPrefab)) && !ReferenceEquals(pendingEntryPrefab, fallbackPrefab))
        {
            pendingEntryPrefab = fallbackPrefab;
            { /* Overrode broken pendingEntryPrefab with fallback prefab {DescribeObject(fallbackPrefab)} */ }
            return;
        }

        if (source.StartsWith(nameof(friendEntryPrefab)) && !ReferenceEquals(friendEntryPrefab, fallbackPrefab))
        {
            friendEntryPrefab = fallbackPrefab;
            { /* Overrode broken friendEntryPrefab with fallback prefab {DescribeObject(fallbackPrefab)} */ }
            return;
        }

        if (source.StartsWith(nameof(searchResultEntryPrefab)) && !ReferenceEquals(searchResultEntryPrefab, fallbackPrefab))
        {
            searchResultEntryPrefab = fallbackPrefab;
            { /* Overrode broken searchResultEntryPrefab with fallback prefab {DescribeObject(fallbackPrefab)} */ }
        }
    }

    private static string DescribeObject(UnityEngine.Object obj)
    {
        if (obj == null)
            return "null";

        return $"{obj.name}<{obj.GetType().Name}>";
    }

    // Mở chat riêng từ ChatPanelUI

    // Gọi từ ChatPanelUI khi tab "Riêng" được chọn mà chưa có target.
    public void SetPrivateChatTarget(int userId, string username)
    {
        if (ChatManager.Instance != null)
        {
            ChatManager.Instance.PrivateChatTargetId   = userId.ToString();
            ChatManager.Instance.PrivateChatTargetName = username;
            ChatManager.Instance.CurrentSendChannel    = ChatChannel.Private;
        }
        HidePanel("SetPrivateChatTarget");
    }

    // Default Row Builder (fallback khi chưa gán prefab)

    private static GameObject MakeDefaultRow(Transform parent)
    {
        var go  = new GameObject("Row", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt  = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 42);
        go.GetComponent<Image>().color = new Color(0.14f, 0.1f, 0.05f, 0.9f);

        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment       = TextAnchor.MiddleLeft;
        hlg.padding              = new RectOffset(8, 8, 4, 4);
        hlg.spacing              = 6;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;

        go.AddComponent<LayoutElement>().minHeight = 42;

        MakeLabel(go.transform, "NameText", "",  14, Color.white).AddComponent<LayoutElement>().flexibleWidth = 1;
        MakeIconBtn(go.transform, "ChatButton",    "💬", new Color(0.18f, 0.45f, 0.9f));
        MakeIconBtn(go.transform, "ProfileButton", "👁",  new Color(0.35f, 0.35f, 0.6f));
        MakeIconBtn(go.transform, "AcceptButton",  "✓",  new Color(0.2f,  0.65f, 0.2f));
        MakeIconBtn(go.transform, "AddButton",     "➕", new Color(0.3f,  0.6f,  0.15f));
        MakeIconBtn(go.transform, "DeleteButton",  "✕",  new Color(0.7f,  0.15f, 0.1f));

        return go;
    }

    private static GameObject MakeLabel(Transform parent, string name, string text, float size, Color color)
    {
        var go  = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        return go;
    }

    private static void MakeIconBtn(Transform parent, string name, string icon, Color bgColor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = bgColor;
        go.AddComponent<LayoutElement>().minWidth = 32;

        var lbl = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        lbl.transform.SetParent(go.transform, false);
        var lblRt = lbl.GetComponent<RectTransform>();
        lblRt.anchorMin = Vector2.zero;
        lblRt.anchorMax = Vector2.one;
        lblRt.offsetMin = Vector2.zero;
        lblRt.offsetMax = Vector2.zero;
        var tmp = lbl.GetComponent<TextMeshProUGUI>();
        tmp.text      = icon;
        tmp.fontSize  = 14;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
    }
}
