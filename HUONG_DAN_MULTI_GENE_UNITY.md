# Hướng Dẫn Config Unity — Hệ Thống Multi-Gene & Hybrid Fusion

---

## Tổng Quan Luồng Chơi

```
[Chọn hệ chính]  →  [Nâng cấp hệ chính Tier 1→5]
                 →  [Chọn hệ phụ] (mở khi hệ chính ≥ Tier 1)
                 →  [Nâng cấp hệ phụ Tier 1→5 — chi phí x1.2]
                 →  [Hybrid Fusion] (chỉ khi CẢ 2 hệ đều Tier 5)
                 →  [HYBRID GENE: skill giữ nguyên, +50% ATK lên hệ bị khắc, miễn nhiễm hệ khắc]
```

---

## PHẦN 1: API Endpoints Mới

| Method | Endpoint | Mục đích |
|--------|----------|----------|
| `GET`  | `/api/gene/list?playerId=X` | Lấy trạng thái tất cả gene |
| `POST` | `/api/gene/secondary/select` | Chọn hệ phụ lần đầu |
| `GET`  | `/api/gene/multi/config?elementType=X&tier=Y` | Config nâng cấp hệ phụ |
| `POST` | `/api/gene/secondary/upgrade` | Nâng cấp hệ phụ |
| `GET`  | `/api/gene/hybrid/config?playerId=X` | Config + điều kiện Hybrid Fusion |
| `POST` | `/api/gene/hybrid/fuse` | Thực hiện Hybrid Fusion |

---

## PHẦN 2: Cập Nhật Model Trong Unity

Trong `Assets/Scripts/Models/PlayerInfo.cs` (hoặc file tương đương chứa class `InfoChar`), **thêm các trường sau**:

```csharp
[JsonProperty("secondary_element")]
public string secondaryElement;

[JsonProperty("secondary_gene_tier")]
public int secondaryGeneTier;

[JsonProperty("secondary_gene_exp")]
public int secondaryGeneExp;

[JsonProperty("is_hybrid")]
public bool isHybrid;

[JsonProperty("hybrid_element_a")]
public string hybridElementA;

[JsonProperty("hybrid_element_b")]
public string hybridElementB;

// CSV: "Earth,Fire" — các hệ bị +50% sát thương
[JsonProperty("hybrid_bonus_targets")]
public string hybridBonusTargets;

// CSV: "Water,Metal" — các hệ không còn +25% ATK lên player
[JsonProperty("hybrid_immune_elements")]
public string hybridImmuneElements;

[JsonProperty("hybrid_atk_bonus_pct")]
public float hybridAtkBonusPct;
```

---

## PHẦN 3: Tạo Panel Chọn Hệ Phụ (SecondaryGeneSelectPanel)

### 3.1 Hierarchy

```
Canvas
└── SecondaryGeneSelectPanel
    ├── TitleText              [TMP — "Chọn Hệ Phụ"]
    ├── PrimaryGeneInfo        [TMP — "Hệ chính: Fire Tier 3"]
    ├── WarningText            [TMP — "⚠ Chỉ được chọn 1 lần!"]
    ├── ElementGrid            [GridLayoutGroup — 5 cột]
    │   ├── BtnFire            [Button + Image icon lửa]
    │   ├── BtnWater           [Button + Image icon nước]
    │   ├── BtnEarth           [Button + Image icon đất]
    │   ├── BtnMetal           [Button + Image icon kim]
    │   └── BtnWood            [Button + Image icon mộc]
    ├── SelectedPreview        [TMP — "Đã chọn: Water"]
    ├── CounterInfo            [TMP — "Hỏa khắc Thổ / Thủy khắc Hỏa"]
    ├── ConfirmButton          [Button — "Xác Nhận"]
    └── CloseButton            [Button]
```

### 3.2 Script: SecondaryGeneSelectPanel.cs

Tạo file `Assets/Scripts/UI/Gene/SecondaryGeneSelectPanel.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;

public class SecondaryGeneSelectPanel : MonoBehaviour
{
    public static SecondaryGeneSelectPanel Instance;

    [Header("UI")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI primaryGeneInfoText;
    public TextMeshProUGUI selectedPreviewText;
    public TextMeshProUGUI counterInfoText;
    public Button confirmButton;
    public Button closeButton;

    [Header("Element Buttons")]
    public Button btnFire, btnWater, btnEarth, btnMetal, btnWood;

    private string _selectedElement = "";
    private string _serverUrl = "http://localhost:5000";

    // Ngũ hành tương khắc (để hiển thị info)
    private static readonly System.Collections.Generic.Dictionary<string, string> Counters = new()
    {
        ["Fire"] = "Thổ", ["Earth"] = "Kim", ["Metal"] = "Mộc",
        ["Wood"] = "Thủy", ["Water"] = "Hỏa"
    };

    void Awake() => Instance = this;

    void Start()
    {
        btnFire.onClick.AddListener(() => SelectElement("Fire"));
        btnWater.onClick.AddListener(() => SelectElement("Water"));
        btnEarth.onClick.AddListener(() => SelectElement("Earth"));
        btnMetal.onClick.AddListener(() => SelectElement("Metal"));
        btnWood.onClick.AddListener(() => SelectElement("Wood"));
        confirmButton.onClick.AddListener(OnConfirm);
        closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        confirmButton.interactable = false;
    }

    public void Open()
    {
        gameObject.SetActive(true);
        _selectedElement = "";
        RefreshUI();
    }

    void RefreshUI()
    {
        var info = GameManager.Instance.PlayerInfo; // thay bằng cách lấy info của bạn
        primaryGeneInfoText.text = $"Hệ chính: {info.elementType} Tier {info.geneTier}";
        // Disable button trùng hệ chính
        SetButtonInteractable(info.elementType, false);
        selectedPreviewText.text = "Chưa chọn hệ phụ";
        counterInfoText.text = "";
        confirmButton.interactable = false;
    }

    void SetButtonInteractable(string element, bool interactable)
    {
        switch (element)
        {
            case "Fire":  btnFire.interactable  = interactable; break;
            case "Water": btnWater.interactable = interactable; break;
            case "Earth": btnEarth.interactable = interactable; break;
            case "Metal": btnMetal.interactable = interactable; break;
            case "Wood":  btnWood.interactable  = interactable; break;
        }
    }

    void SelectElement(string element)
    {
        _selectedElement = element;
        selectedPreviewText.text = $"✓ Đã chọn: {element}";
        string counters = Counters.TryGetValue(element, out var c) ? c : "?";
        counterInfoText.text = $"{element} khắc {counters}";
        confirmButton.interactable = true;
    }

    void OnConfirm()
    {
        if (string.IsNullOrEmpty(_selectedElement)) return;
        StartCoroutine(DoSelect());
    }

    IEnumerator DoSelect()
    {
        confirmButton.interactable = false;
        var json = $"{{\"playerId\":{GameManager.Instance.PlayerId},\"secondaryElement\":\"{_selectedElement}\"}}";
        var req = new UnityWebRequest($"{_serverUrl}/api/gene/secondary/select", "POST");
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            // Cập nhật GameManager / PlayerInfo
            // GameManager.Instance.PlayerInfo.secondaryElement = _selectedElement;
            Debug.Log($"✓ Đã chọn hệ phụ: {_selectedElement}");
            gameObject.SetActive(false);
            // Mở SecondaryGeneUpgradePanel nếu cần
        }
        else
        {
            Debug.LogError($"Lỗi chọn hệ phụ: {req.downloadHandler.text}");
            confirmButton.interactable = true;
        }
    }
}
```

### 3.3 Kéo References vào Inspector

| Slot Inspector | GameObject cần kéo |
|---|---|
| **Primary Gene Info Text** | `PrimaryGeneInfo` |
| **Selected Preview Text** | `SelectedPreview` |
| **Counter Info Text** | `CounterInfo` |
| **Btn Fire** | `BtnFire` |
| **Btn Water** | `BtnWater` |
| **Btn Earth** | `BtnEarth` |
| **Btn Metal** | `BtnMetal` |
| **Btn Wood** | `BtnWood` |
| **Confirm Button** | `ConfirmButton` |
| **Close Button** | `CloseButton` |

---

## PHẦN 4: Tạo Panel Nâng Cấp Hệ Phụ (SecondaryGeneUpgradePanel)

Panel này **giống hệt GeneUpgradePanel** hiện tại — copy nguyên không cần thay đổi nhiều.

### 4.1 Thay đổi duy nhất trong script

Thay endpoint từ `/api/gene/upgrade` → `/api/gene/secondary/upgrade`  
Thay endpoint lấy config từ `/api/gene/config` → `/api/gene/multi/config`

Trong API call để lấy config:
```csharp
// Thay:
string url = $"{serverUrl}/api/gene/config?elementType={secondaryElement}&tier={secondaryTier}";
// thành:
string url = $"{serverUrl}/api/gene/multi/config?elementType={secondaryElement}&tier={secondaryTier}";
```

Trong API call để nâng cấp:
```csharp
// Thay:
string url = $"{serverUrl}/api/gene/upgrade";
// thành:
string url = $"{serverUrl}/api/gene/secondary/upgrade";
```

> **Lưu ý**: HUD hiển thị **2 progress bar gene**: 1 cho hệ chính, 1 cho hệ phụ.

---

## PHẦN 5: Tạo Panel Hybrid Fusion (HybridFusionPanel)

### 5.1 Hierarchy

```
Canvas
└── HybridFusionPanel
    ├── TitleText              [TMP — "⚡ HYBRID GENE FUSION"]
    ├── ElementASection
    │   ├── ElementAIcon       [Image — icon hệ A]
    │   └── ElementATierText   [TMP — "Hỏa Tier 5"]
    ├── PlusSymbol             [TMP — "+"]
    ├── ElementBSection
    │   ├── ElementBIcon       [Image — icon hệ B]
    │   └── ElementBTierText   [TMP — "Thủy Tier 5"]
    ├── ArrowDown              [Image — mũi tên xuống]
    ├── HybridNameText         [TMP — "Hỏa Thủy Long" — màu vàng/gradient]
    ├── HybridDescText         [TMP — mô tả hybrid]
    ├── BonusSection
    │   ├── BonusTitle         [TMP — "🗡 Sát thương tăng 50% lên:"]
    │   └── BonusTargetsText   [TMP — "Thổ, Hỏa"]
    ├── ImmuneSection
    │   ├── ImmuneTitle        [TMP — "🛡 Miễn nhiễm với:"]
    │   └── ImmuneElementsText [TMP — "Thủy, Kim"]
    ├── StatBonusSection
    │   ├── StatHpText         [TMP — "+2000 HP"]
    │   ├── StatMpText         [TMP — "+500 MP"]
    │   ├── StatAtkText        [TMP — "+500 ATK"]
    │   └── StatDefText        [TMP — "+200 DEF"]
    ├── CostSection
    │   ├── GoldCostText       [TMP — "2,000,000 Vàng"]
    │   ├── ItemIcon           [Image — icon Lõi Đột Biến]
    │   └── ItemCostText       [TMP — "x5 Lõi Đột Biến"]
    ├── ItemCountText          [TMP — "Bạn có: 3/5 Lõi Đột Biến"]
    ├── StatusText             [TMP — thông báo lỗi/thành công]
    ├── FuseButton             [Button — "⚡ FUSION" — chỉ enable đủ điều kiện]
    └── CloseButton            [Button]
```

### 5.2 Script: HybridFusionPanel.cs

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Collections.Generic;

public class HybridFusionPanel : MonoBehaviour
{
    public static HybridFusionPanel Instance;

    [Header("Element Display")]
    public Image elementAIcon;
    public TextMeshProUGUI elementATierText;
    public Image elementBIcon;
    public TextMeshProUGUI elementBTierText;

    [Header("Hybrid Info")]
    public TextMeshProUGUI hybridNameText;
    public TextMeshProUGUI hybridDescText;
    public TextMeshProUGUI bonusTargetsText;
    public TextMeshProUGUI immuneElementsText;

    [Header("Stats")]
    public TextMeshProUGUI statHpText, statMpText, statAtkText, statDefText;

    [Header("Cost")]
    public TextMeshProUGUI goldCostText;
    public TextMeshProUGUI itemCostText;
    public TextMeshProUGUI itemCountText;

    [Header("Buttons")]
    public Button fuseButton;
    public Button closeButton;
    public TextMeshProUGUI statusText;

    [Header("Element Icons")]
    public Sprite fireSprite, waterSprite, earthSprite, metalSprite, woodSprite;

    private string _serverUrl = "http://localhost:5000";
    private HybridConfigResponse _config;

    void Awake() => Instance = this;

    void Start()
    {
        fuseButton.onClick.AddListener(OnFuse);
        closeButton.onClick.AddListener(() => gameObject.SetActive(false));
    }

    public void Open()
    {
        gameObject.SetActive(true);
        fuseButton.interactable = false;
        statusText.text = "";
        StartCoroutine(LoadConfig());
    }

    IEnumerator LoadConfig()
    {
        int pid = GameManager.Instance.PlayerId;
        var req = UnityWebRequest.Get($"{_serverUrl}/api/gene/hybrid/config?playerId={pid}");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            statusText.text = $"⚠ {req.downloadHandler.text}";
            return;
        }

        _config = JsonConvert.DeserializeObject<HybridConfigResponse>(req.downloadHandler.text);
        RefreshUI();
    }

    void RefreshUI()
    {
        var info = GameManager.Instance.PlayerInfo;

        elementAIcon.sprite = GetSprite(_config.elementA);
        elementATierText.text = $"{_config.elementA} Tier 5";
        elementBIcon.sprite = GetSprite(_config.elementB);
        elementBTierText.text = $"{_config.elementB} Tier 5";

        hybridNameText.text = _config.hybridName;
        hybridDescText.text = _config.hybridDescription;

        bonusTargetsText.text = string.Join(", ", _config.bonusTargets);
        immuneElementsText.text = string.Join(", ", _config.immuneElements);

        statHpText.text  = $"+{_config.statBonus.hp} HP";
        statMpText.text  = $"+{_config.statBonus.mp} MP";
        statAtkText.text = $"+{_config.statBonus.attack} ATK";
        statDefText.text = $"+{_config.statBonus.defense} DEF";

        goldCostText.text = $"{_config.fusionGoldCost:N0} Vàng";
        itemCostText.text = $"x{_config.fusionItemCount} {_config.fusionItemName}";
        itemCountText.text = $"Bạn có: {_config.availableItems}/{_config.fusionItemCount}";

        fuseButton.interactable = _config.canFuse;
        if (!_config.canFuse)
            statusText.text = $"Cần thêm {_config.fusionItemCount - _config.availableItems} Lõi Đột Biến";
    }

    void OnFuse() => StartCoroutine(DoFuse());

    IEnumerator DoFuse()
    {
        fuseButton.interactable = false;
        var json = $"{{\"playerId\":{GameManager.Instance.PlayerId},\"itemCount\":{_config.fusionItemCount}}}";
        var req = new UnityWebRequest($"{_serverUrl}/api/gene/hybrid/fuse", "POST");
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonConvert.DeserializeObject<FuseResponse>(req.downloadHandler.text);
            statusText.text = resp.message;
            // Cập nhật PlayerInfo
            // GameManager.Instance.PlayerInfo.isHybrid = true;
            // ... cập nhật các trường khác
            Debug.Log($"✅ {resp.message}");
        }
        else
        {
            statusText.text = $"❌ {req.downloadHandler.text}";
            fuseButton.interactable = _config?.canFuse ?? false;
        }
    }

    Sprite GetSprite(string element) => element switch
    {
        "Fire"  => fireSprite,
        "Water" => waterSprite,
        "Earth" => earthSprite,
        "Metal" => metalSprite,
        "Wood"  => woodSprite,
        _       => null
    };

    // Response classes
    [System.Serializable]
    class HybridConfigResponse
    {
        public string hybridName, hybridDescription;
        public string elementA, elementB;
        public List<string> bonusTargets, immuneElements;
        public float atkBonusPercent;
        public int fusionGoldCost, fusionItemId, fusionItemCount, availableItems;
        public string fusionItemName;
        public bool canFuse;
        public StatBonus statBonus;
    }
    [System.Serializable]
    class StatBonus { public int hp, mp, attack, defense; }
    [System.Serializable]
    class FuseResponse { public bool success; public string message; }
}
```

---

## PHẦN 6: Cập Nhật CombatManager (Damage Bonus + Immunity)

Trong script combat của bạn (ví dụ `PlayerAttack.cs` hoặc `CombatCalculator.cs`), cập nhật hàm tính sát thương:

```csharp
// === Ngũ Hành tương khắc ===
private static readonly Dictionary<string, string> CounterMap = new()
{
    ["Metal"] = "Wood",  ["Wood"]  = "Water",
    ["Water"] = "Fire",  ["Fire"]  = "Earth",
    ["Earth"] = "Metal"
};

/// <summary>
/// Tính damage multiplier dựa trên hệ tương khắc và trạng thái Hybrid.
/// </summary>
public static float GetDamageMultiplier(
    string attackerSkillElement,  // hệ của skill đang dùng
    InfoChar attackerInfo,         // info người tấn công
    string targetElement)          // hệ của mục tiêu
{
    // Nếu player là HYBRID:
    if (attackerInfo.isHybrid && !string.IsNullOrEmpty(attackerInfo.hybridBonusTargets))
    {
        var bonusTargets = attackerInfo.hybridBonusTargets.Split(',');
        foreach (var t in bonusTargets)
        {
            if (t.Trim().Equals(targetElement, System.StringComparison.OrdinalIgnoreCase))
                return 1f + attackerInfo.hybridAtkBonusPct; // x1.5 mặc định
        }
    }

    // Tương khắc thường
    if (CounterMap.TryGetValue(attackerSkillElement, out var dominated) &&
        dominated.Equals(targetElement, System.StringComparison.OrdinalIgnoreCase))
        return 1.5f;   // khắc hệ → +50%

    if (CounterMap.TryGetValue(targetElement, out var dominates) &&
        dominates.Equals(attackerSkillElement, System.StringComparison.OrdinalIgnoreCase))
        return 0.75f;  // bị khắc → -25%

    return 1.0f;
}

/// <summary>
/// Khi kẻ địch tấn công player Hybrid,
/// kiểm tra xem hệ đó có thuộc immune_elements không.
/// Nếu có → giảm sát thương còn 50% thay vì 0.75.
/// </summary>
public static float GetReceivedDamageMultiplier(string attackerElement, InfoChar playerInfo)
{
    if (playerInfo.isHybrid && !string.IsNullOrEmpty(playerInfo.hybridImmuneElements))
    {
        var immune = playerInfo.hybridImmuneElements.Split(',');
        foreach (var e in immune)
        {
            if (e.Trim().Equals(attackerElement, System.StringComparison.OrdinalIgnoreCase))
                return 0.5f; // giảm sát thương còn 50%
        }
    }

    // Tương khắc thường ngược chiều
    if (CounterMap.TryGetValue(attackerElement, out var dom) &&
        dom.Equals(playerInfo.elementType, System.StringComparison.OrdinalIgnoreCase))
        return 0.75f;  // hệ kẻ địch khắc hệ player → tăng sát thương nhận

    return 1.0f;
}
```

> **Cách dùng trong combat loop:**
> ```csharp
> float mult = GetDamageMultiplier(skill.elementType, attackerInfo, target.elementType);
> int damage = Mathf.RoundToInt(baseDamage * mult);
>
> // Khi nhận damage:
> float receiveMult = GetReceivedDamageMultiplier(enemy.elementType, playerInfo);
> int received = Mathf.RoundToInt(enemyDamage * receiveMult);
> ```

---

## PHẦN 7: Cập Nhật HUD

### Hiển thị gene trên HUD

Trong `PlayerHUD.cs`, cập nhật hàm `UpdateGeneDisplay()`:

```csharp
void UpdateGeneDisplay(InfoChar info)
{
    // Hệ chính
    primaryGeneIcon.sprite = GetElementSprite(info.elementType);
    primaryGeneTierText.text = $"Tier {info.geneTier}";

    // Hệ phụ (nếu có)
    bool hasSecondary = !string.IsNullOrEmpty(info.secondaryElement);
    secondaryGeneSection.SetActive(hasSecondary);
    if (hasSecondary)
    {
        secondaryGeneIcon.sprite = GetElementSprite(info.secondaryElement);
        secondaryGeneTierText.text = $"Tier {info.secondaryGeneTier}";
    }

    // Hybrid badge
    hybridBadge.SetActive(info.isHybrid);
    if (info.isHybrid)
    {
        // Hiển thị màu gradient blend 2 hệ
        hybridBadgeText.text = "HYBRID";
    }
}
```

### Hierarchy HUD bổ sung

```
HUD
└── GeneDisplay
    ├── PrimaryGeneIcon    [Image]
    ├── PrimaryGeneTier    [TMP]
    ├── SecondaryGeneSection  [ẩn/hiện theo code]
    │   ├── SecondaryGeneIcon  [Image]
    │   └── SecondaryGeneTier  [TMP]
    └── HybridBadge        [Image + TMP "HYBRID" — ẩn khi chưa fusion]
```

---

## PHẦN 8: Kiểm Tra Nhanh

```bash
# 1. Chọn hệ phụ Water cho player 1
curl -X POST http://localhost:5000/api/gene/secondary/select \
  -H "Content-Type: application/json" \
  -d '{"playerId":1,"secondaryElement":"Water"}'

# 2. Xem config nâng cấp hệ phụ Water tier 1
curl "http://localhost:5000/api/gene/multi/config?elementType=Water&tier=1"

# 3. Nâng cấp hệ phụ (dùng 3 item)
curl -X POST http://localhost:5000/api/gene/secondary/upgrade \
  -H "Content-Type: application/json" \
  -d '{"playerId":1,"itemCount":3}'

# 4. Xem config hybrid (khi cả 2 hệ đều tier 5)
curl "http://localhost:5000/api/gene/hybrid/config?playerId=1"

# 5. Fuse thành Hybrid
curl -X POST http://localhost:5000/api/gene/hybrid/fuse \
  -H "Content-Type: application/json" \
  -d '{"playerId":1,"itemCount":5}'

# 6. Xem trạng thái gene hiện tại
curl "http://localhost:5000/api/gene/list?playerId=1"
```

---

## PHẦN 9: Lỗi Thường Gặp

| Lỗi | Nguyên nhân | Cách fix |
|-----|----------|---------|
| `Đã chọn hệ phụ: Water. Không thể thay đổi.` | Mỗi player chỉ được chọn 1 lần | Hiển thị popup thông báo |
| `Hệ phụ không được trùng với hệ chính.` | Player chọn trùng | Disable button hệ chính trong UI |
| `Hệ chính X cần đạt Tier 5` | Chưa đủ điều kiện | Disable FuseButton, hiện progress |
| `Cần thêm N Lõi Đột Biến` | Không đủ item | Hiển thị số thiếu |
| `Player đã là Hybrid Gene rồi.` | Đã fusion rồi | Ẩn FuseButton vĩnh viễn |

---

## Bước Triển Khai Đề Xuất

| # | Việc | Ghi chú |
|---|------|---------|
| 1 | Chạy `migration_multigene.sql` trên DB | Chạy 1 lần duy nhất |
| 2 | Khởi động lại server | EF Core nhận bảng mới |
| 3 | Thêm fields vào Unity `InfoChar` model | Copy từ Phần 2 |
| 4 | Tạo `SecondaryGeneSelectPanel` trong Hierarchy | Theo Phần 3 |
| 5 | Tạo `SecondaryGeneUpgradePanel` (copy GeneUpgradePanel) | Đổi endpoint |
| 6 | Tạo `HybridFusionPanel` | Theo Phần 5 |
| 7 | Update `CombatManager` | Theo Phần 6 |
| 8 | Update `PlayerHUD` | Theo Phần 7 |
