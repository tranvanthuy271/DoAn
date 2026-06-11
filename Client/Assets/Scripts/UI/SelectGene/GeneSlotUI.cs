using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// GeneSlotUI: Component cho mỗi ô nhân vật trên màn SelectGene.
// Hiển thị thông tin nhân vật đã tạo HOẶC nút "Tạo nhân vật" nếu chưa có.
public class GeneSlotUI : MonoBehaviour
{
    [Header("Slot Info")]
    public int slotIndex = 1; // 1 hoặc 2

    [Header("Panel: Nhân vật đã có")]
    public GameObject existingCharacterPanel;
    public TMP_Text   characterNameText;
    public TMP_Text   levelText;
    public TMP_Text   elementText;
    public Image      genderIcon;
    public Button     playButton;

    [Header("Panel: Chưa có nhân vật")]
    public GameObject emptySlotPanel;
    public Button     createCharacterButton;
    public TMP_Text   emptySlotLabel;

    [Header("Panel: Khoá (chưa mở)")]
    public GameObject lockedPanel;
    public TMP_Text   lockedLabel;

    [Header("Shared")]
    public TMP_Text slotTitleText;

    // Đăng ký và xử lý sự kiện phát sinh trong runtime.
    public event Action<int> OnPlayClicked;
    public event Action<int> OnCreateClicked;

    private void Awake()
    {
        if (playButton != null)
            playButton.onClick.AddListener(() => OnPlayClicked?.Invoke(slotIndex));

        if (createCharacterButton != null)
            createCharacterButton.onClick.AddListener(() => OnCreateClicked?.Invoke(slotIndex));
    }

    // Public Setup Methods

    // Thiết lập slot với dữ liệu nhân vật đã tồn tại.
    public void SetupExistingCharacter(GeneSlotInfo data)
    {
        SetAllPanels(showExisting: true, showEmpty: false, showLocked: false);

        if (slotTitleText != null)
            slotTitleText.text = $"Hệ Gene {data.slot}";

        if (characterNameText != null)
            characterNameText.text = data.character_name;

        if (levelText != null)
            levelText.text = $"Cấp {data.level}";

        if (elementText != null)
            elementText.text = GetElementDisplayName(data.element_type);

        if (genderIcon != null)
            genderIcon.gameObject.SetActive(!string.IsNullOrEmpty(data.gender));
    }

    // Thiết lập slot trống — hoặc có thể tạo, hoặc bị khoá.
    public void SetupEmpty(bool isUnlocked, int slot, string defaultElement = null)
    {
        SetAllPanels(showExisting: false, showEmpty: isUnlocked, showLocked: !isUnlocked);

        if (slotTitleText != null)
            slotTitleText.text = $"Hệ Gene {slot}";

        if (emptySlotLabel != null)
            emptySlotLabel.text = isUnlocked ? "Chưa có nhân vật" : "Chưa mở khoá";

        if (lockedLabel != null)
            lockedLabel.text = "Cần mở khoá hệ gene 2";

        // Explicitly show/hide create button (in case it was deactivated separately)
        if (createCharacterButton != null)
            createCharacterButton.gameObject.SetActive(isUnlocked);
    }

    // Private Helpers

    private void SetAllPanels(bool showExisting, bool showEmpty, bool showLocked)
    {
        if (existingCharacterPanel != null) existingCharacterPanel.SetActive(showExisting);
        if (emptySlotPanel != null)         emptySlotPanel.SetActive(showEmpty);
        if (lockedPanel != null)            lockedPanel.SetActive(showLocked);
    }

    private string GetElementDisplayName(string elementType)
    {
        return elementType?.ToLower() switch
        {
            "fire"  => "Hỏa",
            "water" => "Thủy",
            "earth" => "Thổ",
            "wood"  => "Mộc",
            "metal" => "Kim",
            "wind"  => "Phong",
            _       => elementType ?? "?"
        };
    }
}
