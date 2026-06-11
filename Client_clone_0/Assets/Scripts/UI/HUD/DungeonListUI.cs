using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// DungeonListUI — Panel hiển thị danh sách phó bản để người chơi tham gia.
// SETUP TRONG SCENE:
// 1. Tạo Canvas > Panel gọi là "DungeonPanel", gắn DungeonListUI.
// 2. Trong Panel:
// ├─ ScrollView > Content  ← dùng làm dungeonListContent
// ├─ StatusText (Text)     ← thông báo trạng thái
// ├─ CloseButton (Button)
// └─ DungeonEntryButton (Button) — nút mở panel (đặt ở HUD chính)
// 3. Tạo Prefab "DungeonButtonItemPrefab" (có DungeonButtonItem component)
// và assign vào dungeonItemPrefab.
public class DungeonListUI : MonoBehaviour
{
    public static DungeonListUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject  dungeonPanel;       // Panel cha chứa toàn bộ UI
    [SerializeField] private Transform   dungeonListContent; // ScrollView Content
    [SerializeField] private GameObject  dungeonItemPrefab;  // Prefab DungeonButtonItem

    [Header("Buttons")]
    [SerializeField] private Button      openDungeonBtn;     // Nút mở panel (trên HUD)
    [SerializeField] private Button      closeBtn;           // Nút đóng panel

    [Header("Status")]
    [SerializeField] private TextMeshProUGUI statusText;         // Thông báo trạng thái (loading, error...)
    [SerializeField] private GameObject  loadingIndicator;   // Spinner hoặc "Loading..." object

    [Header("Confirm Dialog (tuỳ chọn)")]
    [SerializeField] private GameObject  confirmDialog;
    [SerializeField] private TextMeshProUGUI confirmDungeonName;
    [SerializeField] private TextMeshProUGUI confirmDesc;
    [SerializeField] private Button      confirmYesBtn;
    [SerializeField] private Button      confirmNoBtn;

    private DungeonConfigData[]              _cachedDungeons;
    private Dictionary<int, DungeonSessionData> _sessionCache = new();
    private DungeonConfigData                _selectedDungeon;

    private int _playerLevel = 1; // Lấy từ PlayerDataManager nếu có

    //  UNITY LIFECYCLE

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Đóng panel ban đầu
        if (dungeonPanel)  dungeonPanel.SetActive(false);
        if (confirmDialog) confirmDialog.SetActive(false);
        if (loadingIndicator) loadingIndicator.SetActive(false);

        // Nút mở/đóng
        openDungeonBtn?.onClick.AddListener(OpenPanel);
        closeBtn?.onClick.AddListener(ClosePanel);

        // Confirm dialog
        confirmYesBtn?.onClick.AddListener(ConfirmEnter);
        confirmNoBtn?.onClick.AddListener(() => confirmDialog?.SetActive(false));

        // Lắng nghe trạng thái từ DungeonManager
        if (DungeonManager.Instance != null)
        {
            DungeonManager.Instance.OnDungeonStatusMessage += ShowStatus;
            DungeonManager.Instance.OnDungeonEntered        += ClosePanel;
        }

        // Cập nhật level của player nếu có PlayerDataManager
        TryGetPlayerLevel();
    }

    private void OnDestroy()
    {
        if (DungeonManager.Instance != null)
        {
            DungeonManager.Instance.OnDungeonStatusMessage -= ShowStatus;
            DungeonManager.Instance.OnDungeonEntered        -= ClosePanel;
        }
    }

    //  OPEN / CLOSE

    public void OpenPanel()
    {
        if (dungeonPanel == null) return;
        dungeonPanel.SetActive(true);
        TryGetPlayerLevel();
        StartCoroutine(LoadAndRenderDungeons());
    }

    public void ClosePanel()
    {
        dungeonPanel?.SetActive(false);
        confirmDialog?.SetActive(false);
    }

    //  LOAD & RENDER DUNGEON LIST

    private IEnumerator LoadAndRenderDungeons()
    {
        SetLoading(true);
        ClearList();
        { /* Bắt đầu tải danh sách phó bản */ }

        if (GameplayCommandService.Instance == null)
        {
            ShowStatus("Lỗi: Server chưa sẵn sàng.");
            SetLoading(false);
            yield break;
        }

        bool done = false;
        DungeonConfigData[] dungeons = null;

        GameplayCommandService.OnDungeonListReceived -= HandleDungeonList;
        GameplayCommandService.OnDungeonListReceived += HandleDungeonList;
        GameplayCommandService.Instance.GetDungeonListServerRpc();

        void HandleDungeonList(string json)
        {
            GameplayCommandService.OnDungeonListReceived -= HandleDungeonList;
            if (!json.Contains("\"error\""))
            {
                var resp = JsonUtility.FromJson<DungeonListResponse>(json);
                dungeons = resp?.dungeons;
            }
            else
                ShowStatus($"Lỗi tải danh sách: {json}");
            done = true;
        }

        yield return new WaitUntil(() => done);
        SetLoading(false);

        if (dungeons == null || dungeons.Length == 0)
        {
            { /* Cảnh báo: API trả về 0 phó bản hoặc null */ }
            ShowStatus("Chưa có phó bản nào.");
            yield break;
        }

        { /* Nhận được {dungeons.Length} phó bản */ }
        foreach (var d in dungeons)
            { /* #{d.dungeon_id} '{d.dungeon_name}' type={d.dungeon_type} map_id={d.map_id} scene={d.scene_name} minLv={d.min_level_required} maxP={d.max_players} */ }

        _cachedDungeons = dungeons;
        ShowStatus("");

        // Render
        foreach (var config in dungeons)
        {
            var go   = Instantiate(dungeonItemPrefab, dungeonListContent);
            var item = go.GetComponent<DungeonButtonItem>();
            _sessionCache.TryGetValue(config.dungeon_id, out var session);
            { /* Render item #{config.dungeon_id} '{config.dungeon_name}' prefab={go != null} item={item != null} session={session != null} */ }
            item?.Setup(config, _playerLevel, session);
        }
    }

    private void ClearList()
    {
        foreach (Transform child in dungeonListContent)
            Destroy(child.gameObject);
    }

    //  SELECTION & CONFIRM

    // Gọi bởi DungeonButtonItem khi người chơi click.
    public void OnDungeonSelected(DungeonConfigData config)
    {
        _selectedDungeon = config;

        if (confirmDialog != null)
        {
            // Hiện hộp thoại xác nhận
            if (confirmDungeonName) confirmDungeonName.text = config.dungeon_name;
            if (confirmDesc)
            {
                bool isSolo = config.dungeon_type == "solo";
                string typeLabel = isSolo ? "Thử thách 1 mình" : $"Nhiều người ({config.max_players})";
                string timeLabel = config.time_limit_seconds > 0
                    ? $"⏱ {config.time_limit_seconds / 60} phút"
                    : "Không giới hạn thời gian";
                confirmDesc.text = $"{config.description}\n\n{typeLabel}  |  {timeLabel}";
            }
            confirmDialog.SetActive(true);
        }
        else
        {
            // Không có confirm → vào thẳng
            ConfirmEnter();
        }
    }

    private void ConfirmEnter()
    {
        confirmDialog?.SetActive(false);
        if (_selectedDungeon == null) return;
        DungeonManager.Instance?.EnterDungeon(_selectedDungeon);
    }

    // Hàm hỗ trợ dùng nội bộ để tách nhỏ xử lý chính.

    private void ShowStatus(string msg)
    {
        if (statusText) statusText.text = msg;
    }

    private void SetLoading(bool loading)
    {
        if (loadingIndicator) loadingIndicator.SetActive(loading);
        if (statusText && loading) statusText.text = "Đang tải danh sách phó bản...";
    }

    private void TryGetPlayerLevel()
    {
        // Lấy level từ PlayerPrefs (được lưu khi load PlayerData)
        // Thay bằng PlayerDataManager.Instance nếu project có
        _playerLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
    }
}
