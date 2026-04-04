# HƯỚNG DẪN TRIỂN KHAI BUFF HUD — ITEM SỬ DỤNG CÓ EFFECT

> **Tham khảo từ:** LangLaServer (`Effect.java`, `Char.java`) + Client_base (`StatusEffect.java`, `GameHUD.java`, `BuffTooltip.java`)  
> **Áp dụng cho:** DoAn project (ASP.NET Core API + Unity client)

---

## Tổng quan hệ thống

```
[Người chơi nhấn Sử dụng item]
        │
        ▼
[ItemUseHandler.DoUseConsumableItem()]
        │  gọi API
        ▼
[POST /api/player/{id}/inventory/use-item]   ← PlayerController
        │  đọc item_effect_template
        │  tạo ActiveBuff với ExpireAt
        │  lưu vào player_data.active_buffs
        │  trả về active_buffs[] trong response
        ▼
[ActiveBuffManager.OnBuffsReceived()]
        │  lưu List<ActiveBuffDto>
        │  bắn sự kiện OnBuffListChanged
        ▼
[BuffHudPanel]   ← subscribe OnBuffListChanged
        │  tạo / update BuffIconEntry × N
        │  mỗi 1s: cập nhật countdown ring
        ▼
[Khi click icon] → BuffDetailTooltip (tên, mô tả, thời gian còn lại)

[PlayerCombat.Attack()] → nhân damage × (1 + AttackBuff%)
[NetworkPlayerHealth.TakeDamage()] → nhân defense × (1 + DefenseBuff%)
```

---

## Phần 1 — Server (ASP.NET Core)

### 1.1 Kiểm tra DB đã có migration chưa

Chạy lệnh kiểm tra trong MySQL:

```sql
SHOW TABLES LIKE 'item_effect_template';
SHOW COLUMNS FROM player_data LIKE 'active_buffs';
```

Nếu **chưa có**, chạy file migration:

```bash
mysql -u root -p gamedb < migration_item_buff_system.sql
```

File migration nằm tại: `GameServerApi/migration_item_buff_system.sql`  
Nó tạo:
- Bảng `item_effect_template` (định nghĩa effect của mỗi item)
- Cột `active_buffs LONGTEXT` trong `player_data` (JSON array buff đang active)
- Dữ liệu mẫu: HP Potion (id 101–104), MP Potion (id 111–113), Buff Food (id 121–152)

### 1.2 Verify server endpoint đã có

Các endpoint cần thiết đã được triển khai đầy đủ trong `PlayerController.cs`:

| Endpoint | Mô tả |
|---|---|
| `POST /api/player/{id}/inventory/use-item` | Dùng item, áp buff timed, trả `active_buffs[]` |
| `GET /api/player/{id}/active-buffs` | Lấy toàn bộ buff đang active (dùng khi load game) |

**Logic use-item đã xử lý:**
- `HpRestore` / `MpRestore` → heal ngay lập tức
- Buff có `duration_sec > 0` → tạo `ActiveBuff` với `ExpireAt = UTC.Now + duration`, lưu vào `active_buffs`
- Stacking cùng `effectType` → reset `expireAt` (không chồng chất)
- Response trả về cả `active_buffs` (toàn bộ) và `new_buffs` (vừa thêm)

### 1.3 Model `PlayerData.GetActiveBuffs()` / `SetActiveBuffs()`

File: `GameServerApi/Models/Entities/PlayerData.cs`

Kiểm tra class `ActiveBuff` đã có đủ các field sau (đã có):

```csharp
public class ActiveBuff {
    public string EffectType;   // "AttackBuff", "DefenseBuff", "GeneExpBuff", "ExpBuff", "PhucBuff"
    public int Value;           // % (ví dụ 20 = +20%)
    public int IconId;          // ID icon trong Resources/BuffIcons/
    public string Name;         // "EXP Gene +20%"
    public string Detail;       // "+20% EXP Gene (30 phút)"
    public DateTime? ExpireAt;  // UTC thời điểm hết hạn
}
```

---

## Phần 2 — Client Unity (C# Scripts)

### 2.1 Thư mục cần tạo

```
Assets/
  Resources/
    BuffIcons/          ← đặt sprite PNG icon của từng buff
      buff_gene_exp.png
      buff_exp.png
      buff_phuc.png
      buff_attack.png
      buff_defense.png
      buff_hp_restore.png     (dùng chung cho HP potion, instant không hiển thị)
  Scripts/
    Buffs/
      ActiveBuffManager.cs    ← đã có
      ActiveBuffDto.cs        ← đã có
      BuffHudPanel.cs         ← CẦN TẠO
      BuffIconEntry.cs        ← CẦN TẠO
      BuffDetailTooltip.cs    ← CẦN TẠO
  Prefabs/
    UI/
      BuffIconEntry.prefab    ← CẦN TẠO trong Unity Editor
      BuffDetailTooltip.prefab← CẦN TẠO trong Unity Editor
```

---

### 2.2 Script `BuffIconEntry.cs`

> Tương đương `StatusEffect.renderHudIcon()` trong Client_base LangLa.

Tạo file: `Assets/Scripts/Buffs/BuffIconEntry.cs`

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// BuffIconEntry — 1 ô buff icon trong HUD bar.
/// Hiển thị icon, countdown ring (radial fill), khi click mở BuffDetailTooltip.
/// 
/// Cấu trúc Prefab:
///   BuffIconEntry (RectTransform 48×48)
///   ├── Background (Image – nền tối)
///   ├── Icon (Image – sprite buff)
///   ├── CountdownRing (Image – Image.Type = Filled, FillMethod = Radial360)
///   └── TimeLabel (TMP_Text – nhỏ, góc dưới phải, hiển thị "30s")
/// </summary>
public class BuffIconEntry : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image countdownRing;   // Image.Type = Filled, FillMethod = Radial360
    [SerializeField] private TMP_Text timeLabel;

    [Header("Settings")]
    [SerializeField] private string buffIconsFolder = "BuffIcons";

    // Buff data
    private ActiveBuffDto _buffData;
    private float _totalDuration; // giây

    // Callback khi click → BuffHudPanel tạo tooltip
    public System.Action<ActiveBuffDto, Vector2> OnClicked;

    // ── Public API ──────────────────────────────────────────────────────

    /// <summary>Gán dữ liệu buff và cập nhật hiển thị.</summary>
    public void Bind(ActiveBuffDto buff)
    {
        _buffData = buff;

        // Tính tổng duration từ expireAt (không có server-side duration_sec ở đây)
        float remaining = buff.GetRemainingSeconds();
        _totalDuration = remaining > 0 ? remaining : 1f; // fallback tránh chia 0

        // Load icon
        LoadIcon(buff.iconId);

        // Cập nhật ngay lập tức
        UpdateVisuals();

        // Bắt đầu coroutine tự cập nhật
        StopAllCoroutines();
        StartCoroutine(UpdateLoop());
    }

    // ── Internal ────────────────────────────────────────────────────────

    private void LoadIcon(int iconId)
    {
        if (iconImage == null) return;
        var sprite = Resources.Load<Sprite>($"{buffIconsFolder}/buff_{iconId}");
        if (sprite == null) sprite = Resources.Load<Sprite>($"{buffIconsFolder}/{iconId}");
        if (sprite != null)
            iconImage.sprite = sprite;
    }

    private IEnumerator UpdateLoop()
    {
        while (true)
        {
            UpdateVisuals();
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void UpdateVisuals()
    {
        if (_buffData == null) return;

        float remaining = _buffData.GetRemainingSeconds();

        // Countdown ring — giống clock-wipe trong LangLa (StatusEffect.renderHudIcon)
        if (countdownRing != null)
        {
            if (remaining < 0)
            {
                countdownRing.fillAmount = 1f; // permanent buff — ring đầy
                countdownRing.gameObject.SetActive(false);
            }
            else
            {
                countdownRing.gameObject.SetActive(true);
                countdownRing.fillAmount = Mathf.Clamp01(remaining / _totalDuration);
            }
        }

        // Time label
        if (timeLabel != null)
        {
            if (remaining < 0)
            {
                timeLabel.text = "";
            }
            else if (remaining >= 3600)
            {
                timeLabel.text = $"{(int)(remaining / 3600)}h";
            }
            else if (remaining >= 60)
            {
                timeLabel.text = $"{(int)(remaining / 60)}m";
            }
            else
            {
                timeLabel.text = $"{(int)remaining}s";
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Gọi callback, truyền vị trí màn hình để tooltip xuất hiện đúng chỗ
        OnClicked?.Invoke(_buffData, eventData.position);
    }
}
```

---

### 2.3 Script `BuffDetailTooltip.cs`

> Tương đương `BuffTooltip.java` trong Client_base LangLa.

Tạo file: `Assets/Scripts/Buffs/BuffDetailTooltip.cs`

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// BuffDetailTooltip — panel popup hiện khi click vào buff icon trong HUD.
/// Hiển thị: tên buff, mô tả, thời gian còn lại (live countdown).
/// Tự đóng sau 5 giây.
/// 
/// Cấu trúc Prefab:
///   BuffDetailTooltip (Canvas overrideSorting=250, RectTransform 220×120)
///   ├── Background (Image – dark panel)
///   ├── NameText    (TMP_Text – tên buff, bold)
///   ├── DetailText  (TMP_Text – mô tả)
///   ├── TimeText    (TMP_Text – "Còn lại: 29:42")
///   └── CloseButton (Button – optional, X nhỏ góc phải)
/// </summary>
public class BuffDetailTooltip : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text detailText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private Button closeButton;

    [Header("Settings")]
    [SerializeField] private float autoCloseSeconds = 5f;

    private ActiveBuffDto _buff;
    private Coroutine _autoCloseCoroutine;
    private Coroutine _updateCoroutine;

    private void Awake()
    {
        // Render trên tất cả UI khác
        var canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 250;

        if (GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    /// <summary>Hiển thị tooltip tại vị trí màn hình screenPos.</summary>
    public void Show(ActiveBuffDto buff, Vector2 screenPos)
    {
        _buff = buff;

        if (nameText  != null) nameText.text  = buff.name;
        if (detailText != null) detailText.text = buff.detail;

        // Đặt vị trí tooltip ngay trên icon
        var rt = GetComponent<RectTransform>();
        if (rt != null) rt.position = new Vector3(screenPos.x, screenPos.y + 70f, 0f);

        gameObject.SetActive(true);

        StopAllCoroutines();
        _updateCoroutine    = StartCoroutine(UpdateTimeLoop());
        _autoCloseCoroutine = StartCoroutine(AutoCloseAfter(autoCloseSeconds));
    }

    public void Close()
    {
        StopAllCoroutines();
        gameObject.SetActive(false);
    }

    private IEnumerator UpdateTimeLoop()
    {
        while (_buff != null)
        {
            float remaining = _buff.GetRemainingSeconds();
            if (timeText != null)
            {
                if (remaining < 0)
                    timeText.text = "Vĩnh viễn";
                else if (remaining <= 0)
                {
                    timeText.text = "Hết hạn";
                    Close();
                    yield break;
                }
                else
                {
                    int minutes = (int)(remaining / 60);
                    int seconds = (int)(remaining % 60);
                    timeText.text = $"Còn lại: {minutes:D2}:{seconds:D2}";
                }
            }
            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator AutoCloseAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Close();
    }
}
```

---

### 2.4 Script `BuffHudPanel.cs`

> Tương đương hàm `GameHUD.c(Graphics var0)` trong Client_base LangLa — vẽ hàng icon.

Tạo file: `Assets/Scripts/Buffs/BuffHudPanel.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// BuffHudPanel — thanh HUD hiển thị tất cả buff icon đang active.
/// Subscribe vào ActiveBuffManager.OnBuffListChanged, tạo/xóa BuffIconEntry.
/// 
/// Setup trong Unity:
///   1. Tạo GameObject "BuffHudPanel" trong HUD Canvas
///   2. Thêm HorizontalLayoutGroup (spacing=4, childAlignment=MiddleLeft)
///   3. Gắn script này, kéo buffIconEntryPrefab và tooltipPrefab vào Inspector
///   4. Gắn BuffHudPanel vào Canvas gốc (dưới HP bar, giống LangLa y=47)
///
/// Cấu trúc Scene:
///   HUD Canvas
///   ├── HealthBar
///   ├── MpBar
///   └── BuffHudPanel ← script này, HorizontalLayoutGroup
///       ├── BuffIconEntry (0)
///       ├── BuffIconEntry (1)
///       └── ...
/// </summary>
public class BuffHudPanel : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("Prefab BuffIconEntry (48×48)")]
    [SerializeField] private BuffIconEntry buffIconEntryPrefab;

    [Tooltip("Prefab BuffDetailTooltip")]
    [SerializeField] private BuffDetailTooltip tooltipPrefab;

    [Header("References")]
    [Tooltip("Canvas gốc để đặt tooltip (để tooltip render đúng layer)")]
    [SerializeField] private Canvas rootCanvas;

    // Pool đơn giản: reuse entries thay vì Instantiate liên tục
    private readonly List<BuffIconEntry> _entries = new List<BuffIconEntry>();
    private BuffDetailTooltip _activeTooltip;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    private void Start()
    {
        // Subscribe sự kiện từ ActiveBuffManager
        if (ActiveBuffManager.Instance != null)
            ActiveBuffManager.Instance.OnBuffListChanged += OnBuffListChanged;

        // Load buff ngay khi vào game
        if (ActiveBuffManager.Instance != null)
            ActiveBuffManager.Instance.LoadFromServer();
    }

    private void OnDestroy()
    {
        if (ActiveBuffManager.Instance != null)
            ActiveBuffManager.Instance.OnBuffListChanged -= OnBuffListChanged;
    }

    // ── Internal ──────────────────────────────────────────────────────────

    private void OnBuffListChanged(List<ActiveBuffDto> buffs)
    {
        // Ẩn entry dư
        for (int i = buffs.Count; i < _entries.Count; i++)
            _entries[i].gameObject.SetActive(false);

        // Bind / tạo entry mới
        for (int i = 0; i < buffs.Count; i++)
        {
            BuffIconEntry entry;
            if (i < _entries.Count)
            {
                entry = _entries[i];
                entry.gameObject.SetActive(true);
            }
            else
            {
                entry = Instantiate(buffIconEntryPrefab, transform);
                int capturedIndex = i;
                entry.OnClicked = (buff, screenPos) => ShowTooltip(buff, screenPos);
                _entries.Add(entry);
            }
            entry.Bind(buffs[i]);
        }
    }

    private void ShowTooltip(ActiveBuffDto buff, Vector2 screenPos)
    {
        // Đóng tooltip cũ
        if (_activeTooltip != null)
            _activeTooltip.Close();

        // Tạo tooltip mới (nếu cần)
        if (_activeTooltip == null)
        {
            var parent = rootCanvas != null ? rootCanvas.transform : transform.root;
            _activeTooltip = Instantiate(tooltipPrefab, parent);
        }

        _activeTooltip.Show(buff, screenPos);
    }
}
```

---

### 2.5 Tích hợp Buff % vào Combat

#### 2.5.1 `PlayerCombat.cs` — Áp dụng AttackBuff

Mở file: `Assets/Scripts/Player/Combat/PlayerCombat.cs`

Tìm hàm `Attack()`, tại dòng `int damage = stats.baseDamage;` thay bằng:

```csharp
private void Attack()
{
    if (!canAttack) return;
    PlayerStats stats = controller.stats;
    if (stats == null) return;

    // === BUFF INTEGRATION ===
    int damage = stats.baseDamage;
    if (ActiveBuffManager.Instance != null)
    {
        float attackBonusPct = ActiveBuffManager.Instance.GetBonusPct("AttackBuff");
        damage = Mathf.RoundToInt(damage * (1f + attackBonusPct));
    }
    // ========================

    // ... (code còn lại giữ nguyên)
```

#### 2.5.2 Defense Buff — Áp dụng trong `NetworkPlayerHealth.cs`

Tìm file `NetworkPlayerHealth.cs` (hoặc `EnemyHealth.cs`), tại hàm `TakeDamage(int damage)`:

```csharp
public void TakeDamage(int rawDamage)
{
    // Server-authoritative check
    if (!IsServer) return;

    // --- Defense Buff reduction ---
    int finalDamage = rawDamage;
    // Gọi qua ClientRpc hoặc lưu bonus trên NetworkVariable nếu multiplayer
    // (đơn giản nhất: lấy từ static instance nếu là local player)
    // finalDamage = Mathf.RoundToInt(rawDamage / (1f + defenseBonusPct));
    // --- End ---

    currentHp.Value -= finalDamage;
    // ...
}
```

> **Lưu ý multiplayer:** `ActiveBuffManager` chỉ biết buff của local player.  
> Nếu muốn defense buff hoạt động server-side đúng, cần sync bonus lên `NetworkVariable<float>`.  
> `InventoryNetworkBridge.RequestSyncBuffBonuses()` đã có sẵn để làm điều này.

#### 2.5.3 Buff EXP & Gene EXP — đã có sẵn

Trong hàm nhận EXP kill (tìm `gain-exp` hoặc `GiveExp`):

```csharp
// Apply ExpBuff
float expMultiplier = 1f + (ActiveBuffManager.Instance?.GetBonusPct("ExpBuff") ?? 0f)
                         + (ActiveBuffManager.Instance?.GetBonusPct("PhucBuff") ?? 0f);
int finalExp = Mathf.RoundToInt(baseExp * expMultiplier);

// Apply GeneExpBuff (khi nạp EXP vào Gene)
float geneMult = 1f + (ActiveBuffManager.Instance?.GetBonusPct("GeneExpBuff") ?? 0f);
int finalGeneExp = Mathf.RoundToInt(baseGeneExp * geneMult);
```

---

## Phần 3 — Setup Prefab trong Unity Editor

### 3.1 Tạo Prefab `BuffIconEntry`

1. Trong Hierarchy, click chuột phải → **UI → Image** → đặt tên `BuffIconEntry`
2. Set `RectTransform`: Width=48, Height=48
3. Tạo các con:

```
BuffIconEntry (RectTransform 48×48)
├── Background
│     Image – Color (0,0,0,0.6)
│     RectTransform: Stretch All (offsetMin=0, offsetMax=0)
├── Icon
│     Image – Source Image = <sprite mặc định>
│     RectTransform: Stretch All với padding 4px
│     Preserve Aspect = true
├── CountdownRing
│     Image – Source Image = <ring sprite hoặc dùng default circle>
│     Image.Type = Filled
│     Fill Method = Radial360
│     Fill Origin = Top
│     Color = (1, 0.8, 0, 0.9) [màu vàng]
│     RectTransform: Stretch All
├── TimeLabel
│     TMP_Text – Font Size 10, Anchor BottomRight
│     Alignment: Center
│     RectTransform: Width=48, Height=16, AnchorMin=(0,0), AnchorMax=(1,0), pivot=(0.5,0)
```

4. Gắn script `BuffIconEntry.cs` vào root `BuffIconEntry`
5. Kéo các child vào các slot trong Inspector
6. Kéo ra `Assets/Prefabs/UI/BuffIconEntry.prefab`

### 3.2 Tạo Prefab `BuffDetailTooltip`

1. Hierarchy → **UI → Panel** → đặt tên `BuffDetailTooltip`
2. RectTransform: Width=220, Height=110
3. Thêm Image background (dark, rounded nếu có)

```
BuffDetailTooltip (Panel)
├── Background (Image – dark gray, alpha 0.9)
├── NameText    (TMP_Text, FontSize=14, Bold)
│     Anchor: TopLeft với padding 8px
├── DetailText  (TMP_Text, FontSize=11)
│     Dưới NameText, WordWrapping=On
├── TimeText    (TMP_Text, FontSize=11, Color=yellow)
│     Anchor: BottomLeft với padding 8px
└── CloseBtn    (Button, optional, X nhỏ góc phải trên)
```

4. Gắn script `BuffDetailTooltip.cs`
5. Kéo ra `Assets/Prefabs/UI/BuffDetailTooltip.prefab`

### 3.3 Tạo `BuffHudPanel` trong Scene

1. Trong HUD Canvas, tạo GameObject trống → đặt tên `BuffHudPanel`
2. Add Component:
   - `RectTransform`: Anchor=Bottom-Left, Pos=(10, 60, 0), Width=300, Height=52
   - `HorizontalLayoutGroup`: Spacing=4, Child Alignment=Middle Left, Control Child Size=false
   - `ContentSizeFitter`: Horizontal Fit=Preferred Size (optional)
3. Gắn script `BuffHudPanel.cs`
4. Kéo prefab vào Inspector:
   - `buffIconEntryPrefab` ← `Assets/Prefabs/UI/BuffIconEntry.prefab`
   - `tooltipPrefab` ← `Assets/Prefabs/UI/BuffDetailTooltip.prefab`
   - `rootCanvas` ← drag Canvas gốc của scene

### 3.4 Vị trí trong HUD (tham khảo LangLa)

Trong LangLa client, buff bar nằm tại:
- `x = 5 + AppListener.o` (trái màn hình, cạnh HP bar)
- `y = 47 + AppListener.o/2` (ngay dưới HP/MP bar)
- Mỗi icon spacing 18px, kích thước 20px

DoAn nên đặt tương tự:

```
HUD Canvas
├── HealthBar   (top-left)
├── MpBar       (dưới HealthBar)
└── BuffHudPanel (dưới MpBar, left-aligned)
```

---

## Phần 4 — Thêm Sprite Icon cho Buff

### 4.1 Đặt sprite vào `Resources/BuffIcons/`

Tên file phải khớp với `iconId` trong DB. Ví dụ `item_effect_template` có `icon_id = 121`:
- Đặt file: `Assets/Resources/BuffIcons/buff_121.png`
- Hoặc: `Assets/Resources/BuffIcons/121.png`

`BuffIconEntry.LoadIcon()` sẽ thử theo thứ tự đó.

### 4.2 Bảng iconId mẫu (từ migration_item_buff_system.sql)

| iconId | Buff | Item mẫu |
|---|---|---|
| 101–104 | HP Potion icons | Thuốc Hồi Máu |
| 111–113 | MP Potion icons | Thuốc Hồi Linh |
| 121 | Gene EXP +20% | Nhân Sâm Tâm Linh |
| 122 | Gene EXP +50% | Nhân Sâm Thần Thánh |
| 123 | Gene EXP +100% | Nhân Sâm Thiên Hạ |
| 131 | EXP +25% | Nén Hương Kinh Nghiệm |
| 132 | EXP +50% | Nén Hương Thần Thánh |
| 141 | Phúc +10% | Bùa Phúc Nhỏ |
| 142 | Phúc +25% | Bùa Phúc Lớn |
| 151 | Attack +15% | Bùa Tăng Công Nhỏ |
| 152 | Defense +15% | Bùa Phòng Thủ Nhỏ |

> **Tạm thời nếu chưa có sprite:** dùng Unity built-in sprites, hoặc tạo script fallback tô màu theo `effectType`.

---

## Phần 5 — Luồng dữ liệu đầy đủ (Flow Diagram)

```
[DB: item_effect_template]
  item 121: GeneExpBuff, value=20, duration=1800s, icon=121

[Người chơi nhấn Sử dụng item 121]
        │
        ▼
ItemUseHandler.DoUseConsumableItem(slot)
        │
        ▼
APIClient.UseInventoryItem(playerId, slotIndex)
  → POST /api/player/{id}/inventory/use-item { "slotIndex": N }
        │
        ▼
PlayerController.UseInventoryItem()
  1. Đọc item_effect_template WHERE item_template_id=121
  2. Tạo ActiveBuff { EffectType="GeneExpBuff", Value=20, ExpireAt=+30min }
  3. player_data.active_buffs = JSON([...newBuff])
  4. DB.SaveChanges()
  5. Return { active_buffs: [...], new_buffs: [...] }
        │
        ▼
ItemUseHandler (response.active_buffs)
  → ActiveBuffManager.Instance.OnBuffsReceived(active_buffs)
        │
        ▼
ActiveBuffManager
  - _activeBuffs = [ActiveBuffDto { effectType="GeneExpBuff", expireAt=... }]
  - OnBuffListChanged?.Invoke(list)
        │
        ▼
BuffHudPanel.OnBuffListChanged(list)
  - Bind/tạo BuffIconEntry cho mỗi buff
        │
        ▼
BuffIconEntry.Bind(buff)
  - Load sprite "buff_121" từ Resources/BuffIcons/
  - Hiển thị icon + countdown ring
  - Coroutine cập nhật ring mỗi 0.5s
        │
[Người chơi click vào icon]
        ▼
BuffHudPanel.ShowTooltip(buff, screenPos)
  → buffDetailTooltip.Show(buff, screenPos)
        │
        ▼
BuffDetailTooltip
  - nameText = "EXP Gene +20%"
  - detailText = "+20% EXP Gene (30 phút)"
  - timeText = "Còn lại: 29:42"  (live update mỗi giây)
  - Tự đóng sau 5 giây
```

---

## Phần 6 — Tóm tắt checklist triển khai

### Server (đã có sẵn ✅)
- [x] Bảng `item_effect_template` + entity + DbContext
- [x] Cột `active_buffs` trong `player_data`
- [x] `POST /api/player/{id}/inventory/use-item` — xử lý timed buff
- [x] `GET /api/player/{id}/active-buffs` — load lại khi vào game

### Client Scripts (cần tạo)
- [ ] `BuffIconEntry.cs` — icon đơn lẻ với countdown ring
- [ ] `BuffDetailTooltip.cs` — popup chi tiết khi click
- [ ] `BuffHudPanel.cs` — thanh HUD subscribe `OnBuffListChanged`
- [ ] Tích hợp `AttackBuff` vào `PlayerCombat.Attack()`

### Scene Setup
- [ ] Tạo Prefab `BuffIconEntry.prefab` (đúng cấu trúc UI)
- [ ] Tạo Prefab `BuffDetailTooltip.prefab`
- [ ] Tạo `BuffHudPanel` GameObject trong HUD Canvas
- [ ] Kéo prefabs vào Inspector của `BuffHudPanel`

### Assets
- [ ] Đặt sprite PNG vào `Resources/BuffIcons/` với tên `buff_{iconId}.png`

### DB
- [ ] Chạy `migration_item_buff_system.sql` nếu chưa chạy
- [ ] Chạy `migration_item_buff_system.sql` để chèn dữ liệu mẫu item 101–152

---

## Phần 7 — Tham khảo LangLa vs DoAn

| LangLa (Client_base) | DoAn (Unity) | Ghi chú |
|---|---|---|
| `StatusEffect` class | `ActiveBuffDto` class | Dữ liệu 1 buff |
| `Char.vEffect` Vector | `ActiveBuffManager._activeBuffs` List | Danh sách buff active |
| `GameHUD.c()` — vẽ row icons | `BuffHudPanel.OnBuffListChanged()` | Render hàng icon |
| `StatusEffect.renderHudIcon()` | `BuffIconEntry.UpdateVisuals()` | Vẽ icon + countdown |
| 4-quadrant clock wipe (icon 315) | `Image.fillAmount` Radial360 | Hiệu ứng đồng hồ |
| `BuffTooltip.java` | `BuffDetailTooltip.cs` | Popup chi tiết buff |
| `UIEvent(6000+index)` | `IPointerClickHandler` | Bắt click icon |
| `Effect.setEff()` stat change | `ActiveBuffManager.GetBonusPct()` | Lấy % bonus |
| `Char.listEffect` serialize | `player_data.active_buffs` JSON | Lưu DB |
| `cmd 50` (add effect packet) | `UseInventoryItem` response | Nhận buff |
| `cmd 51` (remove effect) | `TrimExpiredBuffsLoop()` | Xóa buff hết hạn |

---

## Phần 8 — Các loại Buff và API dùng ở đâu

| effectType | `GetBonusPct()` dùng ở đâu | Giá trị |
|---|---|---|
| `AttackBuff` | `PlayerCombat.Attack()` → nhân damage | % |
| `DefenseBuff` | `NetworkPlayerHealth.TakeDamage()` → giảm damage nhận | % |
| `ExpBuff` | Server `gain-exp` endpoint + client EXP gain | % |
| `PhucBuff` | Server drop gold/EXP + client | % |
| `GeneExpBuff` | Gene absorption calculation | % |
| `HpBuff` | MaxHP stat recalc (nếu muốn triển khai thêm) | flat/% |
| `MpBuff` | MaxMP stat recalc (nếu muốn triển khai thêm) | flat/% |

---

## Phần 9 — Thêm item buff mới vào DB

Để thêm 1 loại buff item mới, chỉ cần:

```sql
-- 1. Thêm item template
INSERT INTO item_template (id, name, detail, isXepChong, gioiTinh, type, idClass, idIcon, levelNeed, ...)
VALUES (160, 'Bùa Gia Tốc', 'Tăng 20% tốc độ di chuyển trong 20 phút.', 'True', 2, 24, 0, 160, 10, ...);

-- 2. Thêm effect template
INSERT INTO item_effect_template (item_template_id, effect_type, value, duration_sec, icon_id, display_name, detail)
VALUES (160, 'SpeedBuff', 20, 1200, 160, 'Tốc độ +20%', '+20% tốc độ di chuyển (20 phút)');
```

Và thêm handle trong Unity:
```csharp
// Trong PlayerController.cs (Unity), thêm vào Update() hoặc movement code:
float speedBonus = 1f + (ActiveBuffManager.Instance?.GetBonusPct("SpeedBuff") ?? 0f);
float finalSpeed = baseSpeed * speedBonus;
```

Không cần thay đổi gì trên server — hệ thống tự động đọc `item_effect_template` và lưu buff.
