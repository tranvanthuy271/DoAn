# Hướng dẫn tạo UI Nâng Cấp Trang Bị trong Unity

## Mục lục
1. [Tổng quan UI & luồng dữ liệu](#1-tổng-quan-ui--luồng-dữ-liệu)
2. [Hierarchy & Layout](#2-hierarchy--layout)
3. [Scripts cần tạo](#3-scripts-cần-tạo)
4. [Script: UpgradePanel.cs](#4-script-upgradepanelcs)
5. [Script: UpgradeStoneSlot.cs](#5-script-upgradestoneslotcs)
6. [Script: UpgradeItemCard.cs](#6-script-upgradeitemcardcs)
7. [API Request / Response DTOs](#7-api-request--response-dtos)
8. [Server validation logic](#8-server-validation-logic)
9. [Inspector config checklist](#9-inspector-config-checklist)
10. [Luồng hoạt động hoàn chỉnh](#10-luồng-hoạt-động-hoàn-chỉnh)

---

## 1. Tổng quan UI & luồng dữ liệu

### Giao diện mục tiêu

```
┌──────────────────────────────────────────────────────────────────┐
│                      NÂNG CẤP TRANG BỊ                          │
├─────────────────────┬──────────┬─────────────────────────────────┤
│   TRANG BỊ HIỆN TẠI│  ──►──  │    TRANG BỊ SAU KHI NÂNG CẤP   │
│                     │         │                                  │
│  [Icon]  Kiếm +3    │         │  [Icon]  Kiếm +4                │
│  ─────────────────  │         │  ───────────────────────         │
│  Tấn công:   +22   │         │  Tấn công:   +28 ▲(+6)  [vàng] │
│  HP tối đa:  +40   │         │  HP tối đa:  +50 ▲(+10) [vàng] │
│  (xám)(+4)Tốc độ+0 │         │  (trắng)(+4)Tốc độ: +3% [mới!] │
│  (xám)(+8)Chí mạng │         │  (xám)(+8) Chí mạng: +0        │
│                     │         │                                  │
├─────────────────────┴──────────┴─────────────────────────────────┤
│  ĐÁ NÂNG CẤP  (16 ô – kéo từ túi đồ hoặc nhấn để chọn)        │
│  ┌───┬───┬───┬───┐  ┌───┬───┬───┬───┐  ┌───┬───┬───┬───┐      │
│  │ Đ │ Đ │ Đ │   │  │ M │ M │   │   │  │ B │   │   │   │      │
│  │ á │ á │ á │   │  │ L │ L │   │   │  │ V │   │   │   │      │
│  └───┴───┴───┴───┘  └───┴───┴───┴───┘  └───┴───┴───┴───┘      │
│       Đá Nâng (3)        Đá May Mắn (2)    Đá Bảo Vệ (1)      │
│                                                                  │
│  Tỉ lệ thành công:  ████████▒▒  87%                             │
│  Bạc cần:  8,000    Bạn có: 15,000                              │
│  ⚠ Thất bại có thể giảm 1 bậc! (từ +7 trở lên)                 │
│                                                                  │
│              [  HỦY  ]      [  NÂNG CẤP  ]                      │
└──────────────────────────────────────────────────────────────────┘
```

### Luồng mở panel

```
Người chơi                     Unity                        Server
    │                             │                             │
    │  Nhấn vào trang bị          │                             │
    │  (slot equip hoặc túi đồ)   │                             │
    │ ──────────────────────────► │                             │
    │                             │  POST /upgrade/config       │
    │                             │  { itemId, upgradeLevel }   │
    │                             │ ──────────────────────────► │
    │                             │  ◄── UpgradeConfigResponse  │
    │                             │  { silverCost, stoneId,     │
    │                             │    stoneNeeded, stoneMin,   │
    │                             │    baseRate, failPolicy }   │
    │  Panel hiện ra với          │                             │
    │  2 card item + 16 ô đá      │ ◄─────────────────────────  │
```

---

## 2. Hierarchy & Layout

Tạo cấu trúc Hierarchy sau trong scene:

```
Canvas
└── UpgradePanel                         (UpgradePanel.cs)
    ├── Background                        (Image – dark overlay)
    ├── Window                            (Image – panel bg)
    │   ├── TitleText                     (TMP_Text "NÂNG CẤP TRANG BỊ")
    │   ├── CloseButton                   (Button)
    │   │
    │   ├── CompareArea                   (HorizontalLayoutGroup)
    │   │   ├── CurrentCard              (UpgradeItemCard.cs)  ← trang bị HIỆN TẠI
    │   │   │   ├── ItemIcon             (Image)
    │   │   │   ├── ItemNameText         (TMP_Text)
    │   │   │   ├── UpgradeLevelText     (TMP_Text)
    │   │   │   └── StatsContainer       (VerticalLayoutGroup)
    │   │   │       └── StatRow (prefab) (TMP_Text + Image)
    │   │   │
    │   │   ├── ArrowIcon                (Image "→")
    │   │   │
    │   │   └── PreviewCard             (UpgradeItemCard.cs)  ← trang bị SAU nâng
    │   │       ├── ItemIcon
    │   │       ├── ItemNameText
    │   │       ├── UpgradeLevelText
    │   │       └── StatsContainer
    │   │           └── StatRow (prefab)
    │   │
    │   ├── StoneMatrix                  (GridLayoutGroup – 4×4 = 16 ô)
    │   │   └── StoneSlot × 16          (UpgradeStoneSlot.cs, prefab)
    │   │       ├── Background           (Image)
    │   │       ├── ItemIcon             (Image)
    │   │       ├── QuantityText         (TMP_Text – hiện số lượng)
    │   │       └── EmptyIcon            (Image – hiển thị khi ô trống)
    │   │
    │   ├── StoneGroupLabels            (3 label nhóm loại đá – optional)
    │   │
    │   ├── RateBar                     (Slider – chỉ đọc)
    │   ├── RateText                    (TMP_Text "87%")
    │   ├── SilverCostText              (TMP_Text)
    │   ├── SilverOwnText               (TMP_Text)
    │   ├── FailWarningText             (TMP_Text "⚠ Thất bại...")
    │   │
    │   ├── CancelButton                (Button)
    │   └── UpgradeButton               (Button)
    │
    └── InventoryPicker                 (script riêng – xem mục 4.5)
        └── ... (panel chọn item từ túi đồ)
```

---

## 3. Scripts cần tạo

| Script | Gắn vào | Nhiệm vụ |
|--------|---------|---------|
| `UpgradePanel.cs` | `UpgradePanel` | Điều phối toàn bộ panel |
| `UpgradeStoneSlot.cs` | Mỗi `StoneSlot` | Quản lý 1 ô đá |
| `UpgradeItemCard.cs` | `CurrentCard` & `PreviewCard` | Hiển thị trang bị + stat |
| `UpgradeConfigDto.cs` | *(Data class)* | DTO config từ server |
| `UpgradeRequestDto.cs` | *(Data class)* | Request gửi lên server |

---

## 4. Script: UpgradePanel.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradePanel : MonoBehaviour
{
    // ── Inspector refs ──────────────────────────────────────────
    [Header("Cards")]
    [SerializeField] UpgradeItemCard currentCard;   // trang bị hiện tại
    [SerializeField] UpgradeItemCard previewCard;   // preview sau nâng

    [Header("Stone Matrix (16 slots)")]
    [SerializeField] UpgradeStoneSlot[] stoneSlots; // kéo 16 slot vào đây

    [Header("Rate & Cost UI")]
    [SerializeField] Slider  rateBar;
    [SerializeField] TMP_Text rateText;
    [SerializeField] TMP_Text silverCostText;
    [SerializeField] TMP_Text silverOwnText;
    [SerializeField] GameObject failWarningObj;

    [Header("Buttons")]
    [SerializeField] Button upgradeButton;
    [SerializeField] Button cancelButton;

    // ── Runtime data ─────────────────────────────────────────────
    EquipmentItemDto   currentItem;     // item đang được chọn để nâng
    string             currentSlotKey;  // "weapon" / "helmet" / hoặc inventorySlotIndex
    bool               isFromInventory; // true = từ túi đồ, false = đang mặc
    UpgradeConfigDto   config;          // config từ server cho bậc target
    List<OptionTemplateDto> optionTemplates; // cache từ GameDataManager

    // ── Mở panel ─────────────────────────────────────────────────

    /// <summary>Gọi khi player nhấn vào trang bị ĐANG MẶC</summary>
    public void OpenForEquipped(EquipmentItemDto item, string slotKey)
    {
        currentItem    = item;
        currentSlotKey = slotKey;
        isFromInventory = false;
        StartCoroutine(LoadConfigThenOpen());
    }

    /// <summary>Gọi khi player nhấn vào trang bị TRONG TÚI ĐỒ</summary>
    public void OpenForInventory(InventorySlotDto slot)
    {
        // Chuyển InventorySlotDto → EquipmentItemDto tạm thời
        currentItem = new EquipmentItemDto {
            id           = slot.id,
            upgradeLevel = slot.upgradeLevel,
            strOptions   = slot.strOptions
        };
        currentSlotKey  = slot.slotIndex.ToString();
        isFromInventory = true;
        StartCoroutine(LoadConfigThenOpen());
    }

    IEnumerator LoadConfigThenOpen()
    {
        // Lấy config bậc target = upgradeLevel + 1
        int targetLevel = currentItem.upgradeLevel + 1;
        yield return ApiManager.Instance.Get(
            $"/upgrade/config?itemId={currentItem.id}&targetLevel={targetLevel}",
            (UpgradeConfigDto cfg) => config = cfg
        );

        // Lấy option templates (thường cache sẵn)
        optionTemplates = GameDataManager.Instance.OptionTemplates;

        // Hiển thị
        ClearStoneSlots();
        currentCard.Display(currentItem, optionTemplates, false);
        previewCard.Display(currentItem, optionTemplates, true);  // preview bậc +1
        RefreshRateAndCost();
        failWarningObj.SetActive(config.failPolicy > 0);
        gameObject.SetActive(true);
    }

    // ── Quản lý ô đá ─────────────────────────────────────────────

    void ClearStoneSlots()
    {
        foreach (var slot in stoneSlots) slot.Clear();
    }

    /// <summary>
    /// Gọi khi player nhấn vào ô đá trống → mở InventoryPicker
    /// lọc chỉ hiện item type=21 (UpgradeStone)
    /// </summary>
    public void OnStoneSlotClicked(UpgradeStoneSlot targetSlot)
    {
        InventoryPicker.Instance.Open(
            filterType: 21,  // chỉ hiện Đá Nâng Cấp
            onPicked: (InventorySlotDto picked) => {
                targetSlot.SetItem(picked);
                RefreshRateAndCost();
            }
        );
    }

    /// <summary>Gọi khi player nhấn vào ô đá đã có đá → tháo ra</summary>
    public void OnStoneSlotRemove(UpgradeStoneSlot targetSlot)
    {
        targetSlot.Clear();
        RefreshRateAndCost();
    }

    // ── Tính tỉ lệ & cập nhật UI ─────────────────────────────────

    void RefreshRateAndCost()
    {
        if (config == null) return;

        // Đếm từng loại đá
        int upgradeStones  = CountStoneType(config.stoneId);   // đá nâng cấp đúng loại
        int luckyStones    = CountStoneType(8);                // id=8 Đá May Mắn
        bool hasProtection = CountStoneType(9) > 0;           // id=9 Đá Bảo Vệ

        // Kiểm tra đủ số tối thiểu
        bool enoughStones  = upgradeStones >= config.stoneMin;
        bool enoughSilver  = PlayerDataManager.Instance.Silver >= config.silverCost;

        // Tính tỉ lệ
        float rate = 0f;
        if (upgradeStones > 0)
        {
            float stoneRatio = Mathf.Min((float)upgradeStones / config.stoneNeeded, 1f);
            rate = config.baseSuccessRate * stoneRatio;
            rate += luckyStones * 0.15f;
            rate  = Mathf.Min(rate, 1f);
        }

        // Cập nhật UI
        rateBar.value  = rate;
        rateText.text  = $"{rate * 100:F0}%";
        silverCostText.text = $"Bạc cần: {config.silverCost:N0}";
        silverOwnText.text  = $"Bạn có: {PlayerDataManager.Instance.Silver:N0}";

        // Màu cảnh báo bạc
        silverCostText.color = enoughSilver ? Color.white : Color.red;

        // Cảnh báo vỡ (ẩn nếu có Đá Bảo Vệ)
        failWarningObj.SetActive(config.failPolicy > 0 && !hasProtection);

        // Nút nâng cấp
        upgradeButton.interactable = enoughStones && enoughSilver && upgradeStones > 0;
    }

    int CountStoneType(int stoneItemId)
    {
        int count = 0;
        foreach (var slot in stoneSlots)
            if (!slot.IsEmpty && slot.ItemData.id == stoneItemId)
                count++;
        return count;
    }

    // ── Nâng cấp ─────────────────────────────────────────────────

    public void OnUpgradeClicked()
    {
        upgradeButton.interactable = false;

        // Build danh sách inventory slot indices của các đá đã đặt
        var stoneSlotIndices = new List<int>();
        foreach (var slot in stoneSlots)
            if (!slot.IsEmpty)
                stoneSlotIndices.Add(slot.InventorySlotIndex);

        var request = new UpgradeRequestDto {
            slotKey         = currentSlotKey,
            isFromInventory = isFromInventory,
            stoneSlotIndices = stoneSlotIndices
        };

        StartCoroutine(ApiManager.Instance.Post(
            "/upgrade/equipment", request,
            (UpgradeResponseDto resp) => HandleUpgradeResponse(resp)
        ));
    }

    void HandleUpgradeResponse(UpgradeResponseDto resp)
    {
        if (resp.success)
        {
            // Cập nhật dữ liệu local
            PlayerDataManager.Instance.UpdateEquipmentOrInventory(resp);

            // Animation thành công (particle, sound, v.v.)
            ShowResultPopup($"✨ Nâng cấp thành công!\n+{resp.newUpgradeLevel}", Color.yellow);

            // Refresh lại 2 card
            currentItem.upgradeLevel = resp.newUpgradeLevel;
            currentItem.strOptions   = resp.updatedStrOptions;
            StartCoroutine(LoadConfigThenOpen());
        }
        else
        {
            string msg = resp.downgraded
                ? $"💔 Thất bại! Trang bị về +{resp.newUpgradeLevel}"
                : "😞 Thất bại! Trang bị giữ nguyên.";
            ShowResultPopup(msg, resp.downgraded ? Color.red : new Color(1f, 0.6f, 0f));

            if (resp.downgraded)
            {
                currentItem.upgradeLevel = resp.newUpgradeLevel;
                currentItem.strOptions   = resp.updatedStrOptions;
                StartCoroutine(LoadConfigThenOpen());
            }
        }

        // Refresh túi đồ (đá đã bị trừ)
        InventoryManager.Instance.Refresh(resp.updatedInventory);
        upgradeButton.interactable = true;
    }

    void ShowResultPopup(string message, Color color)
    {
        // Dùng popup/toast UI có sẵn trong game
        // Ví dụ: PopupManager.Instance.Show(message, color);
        Debug.Log(message);
    }

    public void OnCancelClicked() => gameObject.SetActive(false);
    void OnEnable()  => cancelButton.onClick.AddListener(OnCancelClicked);
    void OnDisable() => cancelButton.onClick.RemoveListener(OnCancelClicked);
}
```

---

## 5. Script: UpgradeStoneSlot.cs

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Quản lý 1 ô trong ma trận 16 ô đá.
/// Click vào ô trống  → gọi UpgradePanel.OnStoneSlotClicked
/// Click vào ô có đá  → gọi UpgradePanel.OnStoneSlotRemove
/// </summary>
public class UpgradeStoneSlot : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] Image    iconImage;
    [SerializeField] TMP_Text quantityText;
    [SerializeField] GameObject emptyIcon;  // icon "+" khi ô trống
    [SerializeField] Image    highlightBorder; // border khi hover

    // ── Trạng thái ──
    public bool             IsEmpty          { get; private set; } = true;
    public InventorySlotDto ItemData         { get; private set; }
    public int              InventorySlotIndex => ItemData?.slotIndex ?? -1;

    UpgradePanel panel;

    void Awake()
    {
        panel = GetComponentInParent<UpgradePanel>();
    }

    public void SetItem(InventorySlotDto slot)
    {
        ItemData  = slot;
        IsEmpty   = false;

        // Hiển thị icon đá
        var tmpl = GameDataManager.Instance.GetItemTemplate(slot.id);
        iconImage.sprite = IconLoader.Load(tmpl.idIcon);
        iconImage.enabled = true;
        quantityText.text = "1"; // mỗi ô = 1 viên
        emptyIcon.SetActive(false);
    }

    public void Clear()
    {
        ItemData  = null;
        IsEmpty   = true;
        iconImage.enabled = false;
        quantityText.text = "";
        emptyIcon.SetActive(true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsEmpty)
            panel.OnStoneSlotClicked(this);
        else
            panel.OnStoneSlotRemove(this);
    }
}
```

---

## 6. Script: UpgradeItemCard.cs

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Hiển thị thông tin 1 trang bị (hiện tại hoặc preview sau nâng cấp).
/// isPreview=true  → hiển thị bậc +N+1, diff stat được tô màu vàng
/// isPreview=false → hiển thị bậc hiện tại, màu trắng/xám
/// </summary>
public class UpgradeItemCard : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] Image    itemIcon;
    [SerializeField] TMP_Text itemNameText;
    [SerializeField] TMP_Text upgradeLevelText;

    [Header("Stats")]
    [SerializeField] Transform    statsContainer;
    [SerializeField] StatRowEntry statRowPrefab;   // prefab 1 dòng stat

    // ── Màu sắc ──
    static readonly Color ColorActive   = Color.white;
    static readonly Color ColorDim      = new Color(0.5f, 0.5f, 0.5f);
    static readonly Color ColorUpgraded = new Color(1f, 0.85f, 0f);   // vàng – stat tăng
    static readonly Color ColorNew      = new Color(0.4f, 1f, 0.4f);  // xanh – mới mở khoá

    public void Display(
        EquipmentItemDto item,
        List<OptionTemplateDto> templates,
        bool isPreview)
    {
        // ── Header ──
        var tmpl = GameDataManager.Instance.GetItemTemplate(item.id);
        itemIcon.sprite = IconLoader.Load(tmpl.idIcon);
        itemNameText.text = tmpl.name;

        int displayLevel = isPreview ? item.upgradeLevel + 1 : item.upgradeLevel;
        upgradeLevelText.text = $"+{displayLevel}";

        // ── Xoá rows cũ ──
        foreach (Transform child in statsContainer) Destroy(child.gameObject);

        // ── Parse options ──
        var equipped = EquippedOptionDisplay.ParseAll(item.strOptions);

        foreach (var opt in equipped)
        {
            var optTmpl = templates.Find(t => t.id == opt.optionId);
            if (optTmpl == null) continue;

            int currentValue = opt.value;                          // giá trị tại bậc hiện tại
            int previewValue = optTmpl.GetValueAt(displayLevel);   // giá trị tại bậc preview

            int showValue = isPreview ? previewValue : currentValue;
            int delta     = previewValue - currentValue;

            // ── Xây label ──
            string label = optTmpl.BuildLabel(showValue);
            if (isPreview && delta > 0)
                label += $"  <color=#AAD4AA>(+{delta})</color>";

            // ── Quyết định màu ──
            Color color;
            if (!isPreview)
            {
                // Card hiện tại: sáng nếu active, xám nếu chưa đạt cấp
                color = optTmpl.IsActive(item.upgradeLevel) ? ColorActive : ColorDim;
            }
            else
            {
                bool wasActive    = optTmpl.IsActive(item.upgradeLevel);
                bool willBeActive = optTmpl.IsActive(displayLevel);

                if (!wasActive && willBeActive)
                    color = ColorNew;       // vừa mở khoá tại bậc này → xanh lá
                else if (delta > 0)
                    color = ColorUpgraded;  // stat tăng → vàng
                else if (willBeActive)
                    color = ColorActive;    // active nhưng không tăng → trắng
                else
                    color = ColorDim;       // vẫn chưa active → xám
            }

            // ── Tạo row ──
            var row = Instantiate(statRowPrefab, statsContainer);
            row.Set(label, color);
        }
    }
}
```

### StatRowEntry.cs (Prefab script đơn giản)

```csharp
using UnityEngine;
using TMPro;

public class StatRowEntry : MonoBehaviour
{
    [SerializeField] TMP_Text labelText;

    public void Set(string text, Color color)
    {
        labelText.text  = text;
        labelText.color = color;
    }
}
```

---

## 7. API Request / Response DTOs

Tạo file `UpgradeDtos.cs` trong `Scripts/Inventory/`:

```csharp
using System;
using System.Collections.Generic;

// ── Config từ server (GET /upgrade/config) ──────────────────────
[Serializable]
public class UpgradeConfigDto
{
    public int   targetLevel;       // bậc muốn đạt
    public int   silverCost;
    public int   stoneId;           // item_template.id của đá cần
    public string stoneName;        // tên đá (để hiển thị)
    public int   stoneNeeded;       // số đá đạt tỉ lệ base
    public int   stoneMin;          // số đá tối thiểu
    public float baseSuccessRate;   // 0.0 ~ 1.0
    public int   failPolicy;        // 0=an toàn 1=-1bậc 2=về+0
}

// ── Request nâng cấp (POST /upgrade/equipment) ──────────────────
[Serializable]
public class UpgradeRequestDto
{
    public string    slotKey;            // "weapon"/"helmet"/... hoặc inventorySlotIndex
    public bool      isFromInventory;    // true = từ túi đồ
    public List<int> stoneSlotIndices;   // index trong inventory của từng viên đá
}

// ── Response sau nâng cấp ────────────────────────────────────────
[Serializable]
public class UpgradeResponseDto
{
    public bool   success;
    public bool   downgraded;          // true = bị giảm bậc khi thất bại
    public int    newUpgradeLevel;
    public string updatedStrOptions;   // strOptions mới của item sau khi nâng cấp
    public string message;

    // Server trả lại toàn bộ inventory sau khi trừ đá
    public List<InventorySlotDto> updatedInventory;
}
```

---

## 8. Server validation logic

Đây là những điều server cần kiểm tra khi nhận `UpgradeRequestDto` (tham khảo để bạn tự implement ở `GameServerApi`):

```
1. Xác thực item tồn tại và thuộc về player (slotKey hợp lệ)

2. Lấy config: SELECT * FROM equipment_upgrade_config
               WHERE upgrade_level = item.upgradeLevel + 1
   → Nếu không có row → trả lỗi "Đã đạt cấp tối đa"

3. Với mỗi index trong stoneSlotIndices:
   a. Kiểm tra index có trong inventory của player không
   b. Kiểm tra item tại index đó có type=21 (UpgradeStone) không
   c. Phân loại: đá nâng cấp đúng loại (id = config.stoneId),
                 Đá May Mắn (id=8), Đá Bảo Vệ (id=9)

4. Kiểm tra số lượng đá nâng cấp đúng loại >= config.stoneMin
   → Nếu không đủ → trả lỗi "Không đủ số lượng đá tối thiểu"

5. Kiểm tra player.silver >= config.silverCost
   → Nếu không đủ → trả lỗi "Không đủ bạc"

6. Tính tỉ lệ (xem công thức ở HUONG_DAN_NANG_CAP_TRANG_BI.md mục 5)

7. Random kết quả → xử lý upgradeLevel → RecalculateStrOptions

8. Trừ bạc + xoá đá đã dùng khỏi inventory

9. Trả UpgradeResponseDto
```

---

## 9. Inspector config checklist

### UpgradePanel (trên GameObject UpgradePanel)

- [ ] **currentCard** → kéo `CurrentCard` GameObject vào
- [ ] **previewCard** → kéo `PreviewCard` GameObject vào
- [ ] **stoneSlots** → kéo đủ 16 `StoneSlot` vào (dùng script để fill tự động hoặc kéo tay)
- [ ] **rateBar** → kéo Slider `RateBar`
- [ ] **rateText** → kéo TMP `RateText`
- [ ] **silverCostText** → kéo TMP `SilverCostText`
- [ ] **silverOwnText** → kéo TMP `SilverOwnText`
- [ ] **failWarningObj** → kéo `FailWarningText` GameObject
- [ ] **upgradeButton** → kéo `UpgradeButton`
- [ ] **cancelButton** → kéo `CancelButton`

### StoneSlot Prefab (trên mỗi ô đá)

- [ ] **iconImage** → Image hiển thị icon đá
- [ ] **quantityText** → TMP hiển thị số lượng
- [ ] **emptyIcon** → GameObject "+" placeholder
- [ ] **highlightBorder** → Image viền (optional)

### UpgradeItemCard (trên CurrentCard và PreviewCard)

- [ ] **itemIcon** → Image icon trang bị
- [ ] **itemNameText** → TMP tên
- [ ] **upgradeLevelText** → TMP bậc (+3 / +4)
- [ ] **statsContainer** → Transform chứa các dòng stat
- [ ] **statRowPrefab** → kéo prefab `StatRowEntry`

### GridLayoutGroup (StoneMatrix)

```
Cell Size:    60 × 60
Spacing:      8 × 8
Constraint:   Fixed Column Count = 4
Padding:      top/bottom/left/right = 10
```

---

## 10. Luồng hoạt động hoàn chỉnh

```
[Player nhấn trang bị]
        │
        ▼
UpgradePanel.OpenForEquipped / OpenForInventory
        │
        ├─ GET /upgrade/config → nhận UpgradeConfigDto
        ├─ currentCard.Display(item, opts, isPreview=false)  → stat hiện tại (trắng/xám)
        └─ previewCard.Display(item, opts, isPreview=true)   → stat preview  (vàng/xanh/xám)

[Player nhấn ô đá trống]
        │
        ▼
InventoryPicker.Open(filterType=21)
        │
        └─ [Player chọn đá từ túi] → StoneSlot.SetItem(slot)
                │
                └─ UpgradePanel.RefreshRateAndCost()
                        ├─ Đếm đá theo id → tính rate
                        ├─ Cập nhật rateBar, rateText, silverCostText
                        └─ upgradeButton.interactable = đủ điều kiện?

[Player nhấn NÂNG CẤP]
        │
        ▼
POST /upgrade/equipment  { slotKey, isFromInventory, stoneSlotIndices:[...] }
        │
Server xử lý → trả UpgradeResponseDto
        │
        ├─ success=true  → animation ✨, refresh card, config lại panel
        └─ success=false → popup thất bại, nếu downgraded thì cập nhật bậc
```

---

## Ghi chú thêm

- **InventoryPicker** nên là 1 panel riêng (có thể dùng lại từ hệ thống túi đồ hiện có), chỉ cần thêm tham số `filterType` để lọc chỉ hiện type=21.
- **GameDataManager.Instance.OptionTemplates** — nên load 1 lần lúc vào game (GET `/data/options`) và cache lại, không gọi API mỗi lần mở panel.
- Mỗi `StoneSlot` chỉ chứa **1 viên đá** (không stack), người chơi muốn đặt nhiều đá thì đặt vào nhiều ô.
- Nếu muốn **drag & drop** từ túi đồ vào ô đá thay vì click: implement `IDropHandler` trên `UpgradeStoneSlot` và `IDragHandler` trên `InventoryCell`.
