using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Nút HUD để mở/đóng Friend List Panel.
/// Gắn script này lên Button trong Canvas HUD.
/// Hiển thị badge khi có lời mời kết bạn đang chờ.
/// </summary>
[RequireComponent(typeof(Button))]
public class FriendToggleButton : MonoBehaviour
{
    [Header("References (tự tìm nếu để trống)")]
    [SerializeField] private FriendListUI friendPanel;

    [Header("Badge (lời mời đang chờ)")]
    [SerializeField] private GameObject      badgeRoot;
    [SerializeField] private TextMeshProUGUI badgeText;

    // ── MonoBehaviour ─────────────────────────────────────────────────────────

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnClicked);
    }

    private void Start()
    {
        ResolveFriendPanel();

        Debug.Log($"[FriendToggleButton] Start resolvedPanel={(friendPanel != null ? friendPanel.name : "NULL")} active={friendPanel != null && friendPanel.gameObject.activeSelf} scene={(IsSceneFriendPanel(friendPanel) ? friendPanel.gameObject.scene.name : "INVALID")}");

        var friendManager = FriendManager.EnsureInstance();
        if (friendManager != null)
        {
            friendManager.OnFriendListLoaded += OnFriendListLoaded;

            if (friendManager.HasLoadedFriends)
            {
                OnFriendListLoaded(friendManager.Friends);
            }
            else
            {
                Debug.Log("[FriendToggleButton] Friend cache not loaded yet. Requesting initial friend list.");
                friendManager.LoadFriends();
            }
        }
    }

    private void OnDestroy()
    {
        if (FriendManager.Instance != null)
            FriendManager.Instance.OnFriendListLoaded -= OnFriendListLoaded;
    }

    // ── Click ─────────────────────────────────────────────────────────────────

    private void OnClicked()
    {
        ResolveFriendPanel();

        if (friendPanel == null)
        {
            Debug.LogError("[FriendToggleButton] Clicked but no FriendListUI could be resolved.");
            return;
        }

        Debug.Log($"[FriendToggleButton] Clicked currentActive={friendPanel.gameObject.activeSelf} activeInHierarchy={friendPanel.gameObject.activeInHierarchy} panelPos={(friendPanel.transform as RectTransform)?.anchoredPosition}");
        friendPanel.TogglePanel("FriendToggleButton");
    }

    private void ResolveFriendPanel()
    {
        if (IsSceneFriendPanel(friendPanel))
            return;

        if (friendPanel != null)
            Debug.LogWarning("[FriendToggleButton] Ignoring FriendListUI reference that is not part of a loaded scene. Re-resolving scene instance.", this);

        friendPanel = FindObjectOfType<FriendListUI>(includeInactive: true);
    }

    private static bool IsSceneFriendPanel(FriendListUI panel)
    {
        return panel != null && panel.gameObject.scene.IsValid() && panel.gameObject.scene.isLoaded;
    }

    // ── Badge ─────────────────────────────────────────────────────────────────

    private void OnFriendListLoaded(System.Collections.Generic.List<FriendEntryDto> friends)
    {
        int pending = 0;
        foreach (var f in friends)
            if (f.status == "pending_received") pending++;

        Debug.Log($"[FriendToggleButton] Friend list loaded count={friends?.Count ?? 0} pendingReceived={pending}");

        if (badgeRoot != null) badgeRoot.SetActive(pending > 0);
        if (badgeText != null) badgeText.text = pending > 9 ? "9+" : pending.ToString();
    }
}
