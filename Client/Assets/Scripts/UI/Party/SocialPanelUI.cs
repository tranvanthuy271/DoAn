using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel xã hội bao gồm 4 tab ngoài:
///   0 – Đồng đội  (hiển thị nội dung PartyPanelUI)
///   1 – Bạn bè    (placeholder – có thể gán FriendListUI)
///   2 – Kẻ thù    (placeholder)
///   3 – Tin nhắn  (placeholder – có thể gán ChatPanelUI)
///
/// Nút "Quan hệ" trong CharacterMenuPanelUI.socialPanel trỏ tới GameObject này.
/// </summary>
public class SocialPanelUI : MonoBehaviour
{
    private const string GameplayBlockSource = "SocialPanelUI";
    private const string LogPrefix = "[SocialPanelUI]";

    [Header("Close")]
    [SerializeField] private Button closeButton;

    [Header("Outer Tab Buttons")]
    [SerializeField] private Button tabPartyBtn;    // Đồng đội
    [SerializeField] private Button tabFriendBtn;   // Bạn bè
    [SerializeField] private Button tabEnemyBtn;    // Kẻ thù
    [SerializeField] private Button tabMessageBtn;  // Tin nhắn

    [Header("Outer Tab Panels")]
    [SerializeField] private GameObject panelParty;    // chứa PartyPanelUI
    [SerializeField] private GameObject panelFriend;   // placeholder / FriendListUI root
    [SerializeField] private GameObject panelEnemy;    // placeholder
    [SerializeField] private GameObject panelMessage;  // placeholder / ChatPanelUI root

    [Header("Runtime Party Panel")]
    [Tooltip("Có thể gán prefab asset PartyPanel trực tiếp. Nếu để trống sẽ thử load từ Resources.")]
    [SerializeField] private GameObject partyPanelPrefab;

    [Tooltip("Dùng khi partyPanelPrefab chưa được gán.")]
    [SerializeField] private string partyPanelResourcesPath = "Prefabs/UI/PartyPanel";

    // ── Tab button labels (để đổi màu khi active) ───────────────────────────
    [Header("Tab Label Colors")]
    [SerializeField] private Color colorActiveTab   = new Color(1f,   0.85f, 0.1f, 1f);
    [SerializeField] private Color colorInactiveTab = new Color(0.8f, 0.8f,  0.8f, 1f);

    private int _activeTab = 0;
    private PartyPanelUI _partyPanelInstance;

    private readonly Button[] _tabButtons  = new Button[4];
    private readonly GameObject[] _panels  = new GameObject[4];

    // ─────────────────────────────────────────────────────────────────────────
    #region Unity lifecycle

    private void Awake()
    {
        _tabButtons[0] = tabPartyBtn;
        _tabButtons[1] = tabFriendBtn;
        _tabButtons[2] = tabEnemyBtn;
        _tabButtons[3] = tabMessageBtn;

        _panels[0] = panelParty;
        _panels[1] = panelFriend;
        _panels[2] = panelEnemy;
        _panels[3] = panelMessage;

        closeButton?.onClick.AddListener(Close);

        for (int i = 0; i < _tabButtons.Length; i++)
        {
            int idx = i; // closure capture
            _tabButtons[i]?.onClick.AddListener(() => SelectTab(idx));
        }
    }

    private void OnEnable()
    {
        InputManager.Instance?.SetGameplayInputBlocked(GameplayBlockSource, true);
        InputManager.Instance?.CancelAutoMove();
        EnsurePartyPanelContent();
        SelectTab(_activeTab);
    }

    private void OnDisable()
    {
        InputManager.Instance?.SetGameplayInputBlocked(GameplayBlockSource, false);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Public API

    public void Open(int tabIndex = 0)
    {
        EnsurePartyPanelContent();
        gameObject.SetActive(true);
        SelectTab(tabIndex);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void SelectTab(int tabIndex)
    {
        if (Mathf.Clamp(tabIndex, 0, _panels.Length - 1) == 0)
            EnsurePartyPanelContent();

        _activeTab = Mathf.Clamp(tabIndex, 0, _panels.Length - 1);

        for (int i = 0; i < _panels.Length; i++)
        {
            if (_panels[i] != null)
                _panels[i].SetActive(i == _activeTab);
            else
                Debug.LogWarning($"{LogPrefix} Outer tab index {i} chưa có panel được gán.", this);

            if (_tabButtons[i] != null)
            {
                var lbl = _tabButtons[i].GetComponentInChildren<TMP_Text>();
                if (lbl != null)
                    lbl.color = (i == _activeTab) ? colorActiveTab : colorInactiveTab;

                var img = _tabButtons[i].GetComponent<Image>();
                if (img != null)
                    img.color = (i == _activeTab)
                        ? new Color(0.6f, 0.42f, 0.06f, 1f)
                        : new Color(0.25f, 0.18f, 0.07f, 1f);
            }
        }
    }

    #endregion

    private void EnsurePartyPanelContent()
    {
        if (panelParty == null)
        {
            Debug.LogError($"{LogPrefix} panelParty chưa được gán trong SocialPanelUI.", this);
            return;
        }

        if (_partyPanelInstance != null)
        {
            DisablePlaceholderChildren(_partyPanelInstance.gameObject);
            return;
        }

        _partyPanelInstance = panelParty.GetComponentInChildren<PartyPanelUI>(includeInactive: true);
        if (_partyPanelInstance != null)
        {
            DisablePlaceholderChildren(_partyPanelInstance.gameObject);
            return;
        }

        GameObject prefabToInstantiate = null;
        if (partyPanelPrefab != null)
            prefabToInstantiate = partyPanelPrefab;

        if (prefabToInstantiate == null)
            prefabToInstantiate = Resources.Load<GameObject>(partyPanelResourcesPath);

        if (prefabToInstantiate == null)
        {
            Debug.LogError(
                $"{LogPrefix} Không tìm thấy PartyPanel prefab. Hãy gán partyPanelPrefab hoặc tạo prefab tại Resources/{partyPanelResourcesPath}.",
                this);
            return;
        }

        var instance = Instantiate(prefabToInstantiate, panelParty.transform, false);
        instance.name = prefabToInstantiate.name;

        if (instance.transform is RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;
            rectTransform.anchoredPosition = Vector2.zero;
        }

        _partyPanelInstance = instance.GetComponent<PartyPanelUI>();
        if (_partyPanelInstance == null)
        {
            Debug.LogError($"{LogPrefix} Prefab '{prefabToInstantiate.name}' không có PartyPanelUI.", instance);
            return;
        }

        DisablePlaceholderChildren(instance);
        Debug.Log($"{LogPrefix} Đã instantiate PartyPanel vào tab Đồng đội.", instance);
    }

    private void DisablePlaceholderChildren(GameObject activePartyPanel)
    {
        if (panelParty == null)
            return;

        for (int i = 0; i < panelParty.transform.childCount; i++)
        {
            var child = panelParty.transform.GetChild(i).gameObject;
            if (child == activePartyPanel)
                continue;

            if (child.GetComponent<PartyPanelUI>() != null)
                continue;

            child.SetActive(false);
        }
    }
}
