using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Nút HUD để mở/đóng Chat Panel.
// Gắn script này lên Button trong Canvas HUD.
// Tự tìm ChatPanelUI trong scene, hoặc gán tay trong Inspector.
[RequireComponent(typeof(Button))]
public class ChatToggleButton : MonoBehaviour
{
    [Header("References (tự tìm nếu để trống)")]
    [SerializeField] private ChatPanelUI chatPanel;
    [SerializeField] private FriendListUI friendPanel;

    [Header("Badge (số tin chưa đọc)")]
    [SerializeField] private GameObject  badgeRoot;        // GameObject chứa số đếm
    [SerializeField] private TextMeshProUGUI badgeText;    // text số tin

    // State

    private Button _btn;
    private int    _unreadCount;
    private bool   _chatManagerSubscribed;

    // MonoBehaviour

    private void Awake()
    {
        _btn = GetComponent<Button>();
        _btn.onClick.AddListener(OnClicked);
    }

    private void Start()
    {
        // Auto-find nếu chưa gán
        ResolveChatPanel();
        ResolveFriendPanel();

        EnsureSubscribed();
        UpdateBadge();
    }

    private void Update()
    {
        if (!_chatManagerSubscribed)
            EnsureSubscribed();
    }

    private void OnDestroy()
    {
        if (_chatManagerSubscribed && ChatManager.Instance != null)
            ChatManager.Instance.OnMessageReceived -= OnNewMessage;
    }

    private void EnsureSubscribed()
    {
        if (_chatManagerSubscribed || ChatManager.Instance == null) return;
        ChatManager.Instance.OnMessageReceived += OnNewMessage;
        _chatManagerSubscribed = true;
    }

    // Click Handler

    private void OnClicked()
    {
        ResolveChatPanel();
        if (chatPanel == null) return;
        bool willOpen = !chatPanel.gameObject.activeSelf;
        chatPanel.gameObject.SetActive(willOpen);

        if (willOpen)
        {
            // Reset badge khi mở
            _unreadCount = 0;
            UpdateBadge();
        }
    }

    // Badge

    private void OnNewMessage(ChatMessageDto msg)
    {
        // Chỉ tăng badge khi panel đang đóng
        if (chatPanel != null && chatPanel.gameObject.activeSelf) return;
        _unreadCount++;
        UpdateBadge();
    }

    private void UpdateBadge()
    {
        if (badgeRoot != null)
            badgeRoot.SetActive(_unreadCount > 0);
        if (badgeText != null)
            badgeText.text = _unreadCount > 9 ? "9+" : _unreadCount.ToString();
    }

    // Public

    // Mở thẳng tab tin riêng với một người chơi.
    public void OpenPrivateChat(int targetUserId, string targetUsername)
    {
        ResolveChatPanel();
        chatPanel?.OpenPrivateChat(targetUserId, targetUsername);
        chatPanel?.gameObject.SetActive(true);
        _unreadCount = 0;
        UpdateBadge();
    }

    // Mở/đóng panel bạn bè.
    public void ToggleFriendPanel()
    {
        ResolveFriendPanel();

        if (friendPanel == null)
        {
            { /* Cảnh báo: ToggleFriendPanel failed because FriendListUI is NULL */ }
            return;
        }

        friendPanel.TogglePanel("ChatToggleButton");
    }

    private void ResolveChatPanel()
    {
        if (IsSceneChatPanel(chatPanel))
            return;

        if (chatPanel != null)
            { /* Cảnh báo: Ignoring ChatPanelUI reference that is not part of a loaded scene. Re-resolving scene instance */ }

        chatPanel = FindObjectOfType<ChatPanelUI>(includeInactive: true);
    }

    private void ResolveFriendPanel()
    {
        if (IsSceneFriendPanel(friendPanel))
            return;

        if (friendPanel != null)
            { /* Cảnh báo: Ignoring FriendListUI reference that is not part of a loaded scene. Re-resolving scene instance */ }

        friendPanel = FindObjectOfType<FriendListUI>(includeInactive: true);
    }

    private static bool IsSceneChatPanel(ChatPanelUI panel)
    {
        return panel != null && panel.gameObject.scene.IsValid() && panel.gameObject.scene.isLoaded;
    }

    private static bool IsSceneFriendPanel(FriendListUI panel)
    {
        return panel != null && panel.gameObject.scene.IsValid() && panel.gameObject.scene.isLoaded;
    }
}
