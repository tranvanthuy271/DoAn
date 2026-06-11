using UnityEngine;
using UnityEngine.UI;
using TMPro;

// DungeonButtonItem — Component gắn lên mỗi nút phó bản trong DungeonListUI.
// Prefab cần có cấu trúc:
// DungeonButtonItem (Button + DungeonButtonItem)
// ├─ Icon          (Image)
// ├─ NameText      (Text)
// ├─ TypeBadge     (Text) — "THỬ THÁCH" hoặc "NHIỀU NGƯỜI"
// ├─ LevelText     (Text) — "Yêu cầu Lv.X"
// ├─ DescText      (Text)
// ├─ SlotText      (Text) — "1/1" hoặc "0/4" (multi only)
// └─ LockOverlay   (GameObject) — hiện khi player chưa đủ level
public class DungeonButtonItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button      button;
    [SerializeField] private TMPro.TextMeshProUGUI        nameText;
    [SerializeField] private TMPro.TextMeshProUGUI        typeBadgeText;
    [SerializeField] private TMPro.TextMeshProUGUI        levelText;
    [SerializeField] private TMPro.TextMeshProUGUI        descText;
    [SerializeField] private TMPro.TextMeshProUGUI        slotText;
    [SerializeField] private GameObject  lockOverlay;   // Hiện khi bị khoá (chưa đủ level)
    [SerializeField] private Image       typeBadgeBg;   // Background badge để tô màu

    // Màu badge
    [Header("Badge Colors")]
    [SerializeField] private Color soloColor  = new Color(0.9f, 0.4f, 0.1f); // cam
    [SerializeField] private Color multiColor = new Color(0.1f, 0.5f, 0.9f); // xanh

    private DungeonConfigData _config;
    private int               _playerLevel;
    private DungeonSessionData _liveSession; // null nếu chưa tải

    private void Reset()
    {
        button       = GetComponent<Button>();
    }

    // Khởi tạo dữ liệu cho nút này.
    public void Setup(DungeonConfigData config, int playerLevel, DungeonSessionData liveSession = null)
    {
        _config      = config;
        _playerLevel = playerLevel;
        _liveSession = liveSession;
        Refresh();
    }

    // Cập nhật số người online cho phó bản multi (gọi định kỳ).
    public void UpdateSession(DungeonSessionData session)
    {
        _liveSession = session;
        RefreshSlot();
    }

    private void Refresh()
    {
        if (_config == null) return;

        bool isSolo  = _config.dungeon_type == "solo";
        bool locked  = _playerLevel < _config.min_level_required;

        // Tên
        if (nameText)     nameText.text     = _config.dungeon_name;

        // Mô tả
        if (descText)     descText.text     = _config.description;

        // Level yêu cầu
        if (levelText)
            levelText.text = _config.min_level_required <= 1
                ? "Không giới hạn level"
                : $"Yêu cầu Lv.{_config.min_level_required}";

        // Badge type
        if (typeBadgeText)
            typeBadgeText.text = isSolo ? "THỬ THÁCH" : "NHIỀU NGƯỜI";
        if (typeBadgeBg)
            typeBadgeBg.color = isSolo ? soloColor : multiColor;

        // Slot (chỉ hiện đầy đủ cho multi)
        RefreshSlot();

        // Lock overlay
        if (lockOverlay) lockOverlay.SetActive(locked);
        if (button)      button.interactable = !locked;

        // Listener
        button?.onClick.RemoveAllListeners();
        button?.onClick.AddListener(OnClick);
    }

    private void RefreshSlot()
    {
        if (slotText == null) return;

        if (_config.dungeon_type == "solo")
        {
            slotText.text = "1 / 1";
        }
        else if (_liveSession != null && _liveSession.status != "ended")
        {
            slotText.text = $"{_liveSession.current_players} / {_liveSession.max_players}";
        }
        else
        {
            slotText.text = $"0 / {_config.max_players}";
        }
    }

    private void OnClick()
    {
        if (_config == null) return;
        DungeonListUI.Instance?.OnDungeonSelected(_config);
    }
}
