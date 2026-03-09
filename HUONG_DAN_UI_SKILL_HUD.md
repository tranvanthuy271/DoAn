# Hướng Dẫn Config Skill HUD (Cooldown + Button)

> **Mục tiêu:** Hiển thị thanh skill dưới màn hình — chỉ hiện những skill đã **unlock**, mỗi slot có:
> - Icon skill
> - Phím tắt (J / K / ...)
> - Overlay đếm ngược cooldown (ảnh tối che đè)
> - Text hiển thị số giây còn lại (ví dụ: `2.3s`)
> - Hiệu ứng "sẵn sàng" khi hết cooldown

---

## Tổng Quan Luồng Dữ Liệu

```
APIClient.GetPlayerData()
  └─ PlayerDataResponse.skills[]  (ApiSkillData: skill_id, skill_name, level, unlocked)
        │
        ▼
  SkillHUDManager.cs  ──────── đọc PlayerSkillManager.skills (List<SkillData>)
        │                       khớp theo skillName ↔ skill_name
        │                       lọc: chỉ hiện slot có unlocked == true
        ▼
  SkillSlotUI.cs (mỗi slot)
        ├─ Image iconImage          → icon skill
        ├─ Image cooldownOverlay    → fillAmount từ GetCooldownPercent()
        ├─ Text  countdownText      → "2.3s" khi đang cooldown
        └─ Text  keyText            → "J", "K", ...
```

---

## Bước 1 — Thêm `skillId` và `GetRemainingCooldown()` vào `SkillData.cs`

Mở file `Client/Assets/Scripts/Player/Skills/SkillData.cs`, thêm 2 thay đổi nhỏ:

```csharp
[Header("Skill Info")]
[Tooltip("Tên skill (để dễ quản lý)")]
public string skillName = "New Skill";

// ★ THÊM DÒNG NÀY — ID khớp với skill_id từ server
[Tooltip("ID skill từ server (khớp với ApiSkillData.skill_id). Đặt 0 nếu không dùng API)")]
public int skillId = 0;

[Tooltip("Phím để kích hoạt skill")]
public KeyCode activationKey = KeyCode.K;
```

Và thêm method `GetRemainingCooldown()` vào cuối class (trước dấu `}`):

```csharp
/// <summary>
/// Lấy số giây cooldown còn lại (0 = sẵn sàng)
/// </summary>
public float GetRemainingCooldown() => canUse ? 0f : cooldownTimer;
```

> `cooldownTimer` trong `SkillData` đếm ngược từ `cooldown → 0`, chính xác là thời gian còn lại.

---

## Bước 2 — Tạo Script `SkillSlotUI.cs`

Tạo file: `Client/Assets/Scripts/UI/HUD/SkillSlotUI.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Một slot skill trên HUD — hiển thị icon, cooldown overlay, countdown text
/// </summary>
public class SkillSlotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] public Image iconImage;           // Icon skill (Image component)
    [SerializeField] public Image cooldownOverlay;     // Image che phủ khi cooldown (Filled, FillMethod = Radial360)
    [SerializeField] public Text  countdownText;       // Text "2.3s"
    [SerializeField] public Text  keyText;             // Text phím tắt "J", "K"...
    [SerializeField] public GameObject readyEffect;    // (Tuỳ chọn) hiệu ứng khi hết cooldown

    // Dữ liệu được gán từ SkillHUDManager
    [HideInInspector] public SkillData skillData;      // SkillData từ PlayerSkillManager

    private bool wasOnCooldown = false;

    private void Update()
    {
        if (skillData == null) return;
        RefreshCooldown();
    }

    private void RefreshCooldown()
    {
        float percent   = skillData.GetCooldownPercent();   // 0 = full cooldown, 1 = ready
        float remaining = skillData.GetRemainingCooldown(); // giây còn lại

        bool onCooldown = percent < 1f;

        // ── Overlay ──────────────────────────────────────────────
        if (cooldownOverlay != null)
        {
            cooldownOverlay.gameObject.SetActive(onCooldown);
            // fillAmount = 0 lúc vừa bắn, tăng dần đến 1 khi xong
            cooldownOverlay.fillAmount = 1f - percent;
        }

        // ── Countdown text ────────────────────────────────────────
        if (countdownText != null)
        {
            if (onCooldown)
            {
                countdownText.gameObject.SetActive(true);
                countdownText.text = remaining < 10f
                    ? remaining.ToString("F1") + "s"   // "2.3s"
                    : Mathf.CeilToInt(remaining) + "s"; // "15s"
            }
            else
            {
                countdownText.gameObject.SetActive(false);
            }
        }

        // ── Ready effect (bật 1 frame khi vừa hết cooldown) ───────
        if (readyEffect != null)
        {
            bool justReady = wasOnCooldown && !onCooldown;
            if (justReady) StartCoroutine(FlashReadyEffect());
        }

        wasOnCooldown = onCooldown;
    }

    private System.Collections.IEnumerator FlashReadyEffect()
    {
        readyEffect.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        readyEffect.SetActive(false);
    }

    /// <summary>
    /// Gán SkillData và cập nhật static info (icon, phím tắt)
    /// </summary>
    public void Setup(SkillData data, Sprite icon = null)
    {
        skillData = data;

        if (keyText != null)
            keyText.text = data.activationKey.ToString().Replace("Alpha", "");

        if (iconImage != null && icon != null)
            iconImage.sprite = icon;

        // Ẩn overlay và text ngay khi setup
        if (cooldownOverlay != null) cooldownOverlay.gameObject.SetActive(false);
        if (countdownText  != null) countdownText.gameObject.SetActive(false);
    }
}
```

---

## Bước 3 — Tạo Script `SkillHUDManager.cs`

Tạo file: `Client/Assets/Scripts/UI/HUD/SkillHUDManager.cs`

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Quản lý thanh skill HUD dưới màn hình.
/// Chỉ hiện skill đã unlock (unlocked == true từ server).
/// Mỗi skill hiện countdown cooldown và phím tắt.
/// </summary>
public class SkillHUDManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Prefab của một slot skill (có SkillSlotUI component)")]
    [SerializeField] private GameObject skillSlotPrefab;

    [Tooltip("Container chứa các slot (HorizontalLayoutGroup)")]
    [SerializeField] private Transform slotContainer;

    [Header("Icon Mapping (Tuỳ chọn)")]
    [Tooltip("Danh sách icon cho từng skill — khớp theo thứ tự với PlayerSkillManager.skills")]
    [SerializeField] private List<Sprite> skillIcons = new List<Sprite>();

    // ── Runtime ──────────────────────────────────────────────────
    private PlayerSkillManager skillManager;
    private List<SkillSlotUI>  spawnedSlots = new List<SkillSlotUI>();

    private void Start()
    {
        StartCoroutine(WaitForPlayerAndBuild());
    }

    /// <summary>
    /// Đợi local player spawn rồi mới build HUD
    /// (PlayerSkillManager chỉ có skills sau khi NetworkSpawn)
    /// </summary>
    private IEnumerator WaitForPlayerAndBuild()
    {
        // Đợi tối đa 10 giây cho player spawn
        float timeout = 10f;
        while (timeout > 0f)
        {
            skillManager = FindLocalPlayerSkillManager();
            if (skillManager != null) break;
            timeout -= 0.2f;
            yield return new WaitForSeconds(0.2f);
        }

        if (skillManager == null)
        {
            Debug.LogWarning("[SkillHUDManager] Không tìm thấy PlayerSkillManager của local player!");
            yield break;
        }

        // Lấy danh sách skill đã unlock từ APIClient cache
        var unlockedIds = GetUnlockedSkillIds();

        BuildHUD(unlockedIds);
    }

    /// <summary>
    /// Tìm PlayerSkillManager của local player (IsOwner)
    /// </summary>
    private PlayerSkillManager FindLocalPlayerSkillManager()
    {
        foreach (var psm in FindObjectsOfType<PlayerSkillManager>())
        {
            // Chỉ lấy owner (local player)
            var netObj = psm.GetComponent<Unity.Netcode.NetworkObject>();
            if (netObj != null && netObj.IsOwner) return psm;
        }
        return null;
    }

    /// <summary>
    /// Lấy HashSet skill_id đã unlock từ dữ liệu player đã load
    /// </summary>
    private HashSet<int> GetUnlockedSkillIds()
    {
        var result = new HashSet<int>();

        // APIClient lưu PlayerDataResponse sau khi login
        // Truy cập qua APIClient.Instance.CachedPlayerData nếu bạn đã thêm
        // Hoặc tìm component PlayerDataHolder (xem Bước 5)
        var holder = FindObjectOfType<PlayerDataHolder>();
        if (holder == null || holder.playerData == null)
        {
            Debug.LogWarning("[SkillHUDManager] Không tìm thấy PlayerDataHolder — hiện tất cả skill.");
            return result; // empty = show all (fallback)
        }

        if (holder.playerData.skills != null)
        {
            foreach (var s in holder.playerData.skills)
            {
                if (s.unlocked) result.Add(s.skill_id);
            }
        }

        return result;
    }

    /// <summary>
    /// Tạo các slot chỉ cho skill đã unlock
    /// </summary>
    private void BuildHUD(HashSet<int> unlockedIds)
    {
        // Xóa slot cũ nếu rebuild
        foreach (var slot in spawnedSlots)
            if (slot != null) Destroy(slot.gameObject);
        spawnedSlots.Clear();

        bool filterByUnlock = unlockedIds.Count > 0;

        for (int i = 0; i < skillManager.skills.Count; i++)
        {
            SkillData data = skillManager.skills[i];
            if (data == null) continue;

            // ── Lọc: chỉ hiện skill đã unlock ────────────────────
            if (filterByUnlock && data.skillId > 0 && !unlockedIds.Contains(data.skillId))
                continue; // skill chưa mở khóa → bỏ qua

            // ── Tạo slot ──────────────────────────────────────────
            GameObject go   = Instantiate(skillSlotPrefab, slotContainer);
            SkillSlotUI slot = go.GetComponent<SkillSlotUI>();

            if (slot == null)
            {
                Debug.LogError("[SkillHUDManager] skillSlotPrefab thiếu component SkillSlotUI!");
                continue;
            }

            Sprite icon = (i < skillIcons.Count) ? skillIcons[i] : null;
            slot.Setup(data, icon);
            spawnedSlots.Add(slot);
        }

        Debug.Log($"[SkillHUDManager] Build xong HUD — {spawnedSlots.Count} slot hiển thị.");
    }

    /// <summary>
    /// Gọi từ ngoài để rebuild HUD (ví dụ: sau khi unlock skill mới)
    /// </summary>
    public void Rebuild()
    {
        if (skillManager == null) return;
        BuildHUD(GetUnlockedSkillIds());
    }
}
```

---

## Bước 4 — Tạo `PlayerDataHolder.cs` (Bridge lưu cache dữ liệu player)

Tạo file: `Client/Assets/Scripts/Services/PlayerDataHolder.cs`

```csharp
using UnityEngine;

/// <summary>
/// Singleton đơn giản lưu PlayerDataResponse sau khi login thành công.
/// SkillHUDManager và các UI khác đọc từ đây.
/// </summary>
public class PlayerDataHolder : MonoBehaviour
{
    public static PlayerDataHolder Instance { get; private set; }

    [HideInInspector] public PlayerDataResponse playerData;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
```

**Gán dữ liệu sau khi login:** Tìm chỗ trong code gọi `APIClient.GetPlayerData()` và thêm:

```csharp
// Sau khi nhận được PlayerDataResponse response:
PlayerDataHolder.Instance.playerData = response;
```

> Nếu bạn đã có singleton khác lưu player data, bỏ qua bước này và sửa `SkillHUDManager.GetUnlockedSkillIds()` để đọc từ đó.

---

## Bước 5 — Tạo Prefab `SkillSlotPrefab` trong Unity Editor

### 5.1 Cấu trúc Hierarchy của 1 slot

```
SkillSlotPrefab (GameObject)
├─ [SkillSlotUI component]
│
├─ Background          (Image — tối, bo tròn, 70×70 px)
├─ IconImage           (Image — icon skill, 54×54 px, centre)
├─ CooldownOverlay     (Image — màu đen alpha 0.7, 54×54 px)
│    ImageType: Filled
│    FillMethod: Radial360
│    FillOrigin: Top
│    FillClockwise: ✓
│    Raycast Target: ✗
├─ CountdownText       (Text — "2.3s", căn giữa, bold, màu trắng)
│    Font Size: 14
│    Alignment: Center/Middle
└─ KeyText             (Text — "J", góc dưới phải, nhỏ hơn)
     Font Size: 11
     Alignment: Right/Lower
```

### 5.2 Gán component `SkillSlotUI`

Chọn root `SkillSlotPrefab` → Add Component → `SkillSlotUI`  
Rồi kéo thả:

| Field trên Inspector | Kéo từ |
|---|---|
| Icon Image | GameObject `IconImage` |
| Cooldown Overlay | GameObject `CooldownOverlay` |
| Countdown Text | GameObject `CountdownText` |
| Key Text | GameObject `KeyText` |
| Ready Effect | (Tuỳ chọn) particle hoặc glow object |

### 5.3 Save thành Prefab

Kéo vào thư mục `Assets/Prefabs/UI/HUD/SkillSlotPrefab.prefab`

---

## Bước 6 — Tạo GameObject `SkillHUD` trong Scene

### 6.1 Tạo trong Canvas HUD

```
Canvas (existing)
└─ HUDPanel (existing)
   └─ SkillHUD                         ← Tạo mới (GameObject rỗng)
        [RectTransform]
          Anchor: Bottom Center
          Pos: (0, 10, 0)
          Size: tuỳ số skill
        [HorizontalLayoutGroup]
          Spacing: 6
          Child Alignment: Middle Center
          Control Width: ✓ / Control Height: ✓ (nếu muốn tự resize)
        [SkillHUDManager component]     ← Add component
```

### 6.2 Gán `SkillHUDManager` Inspector

| Field | Giá trị |
|---|---|
| Skill Slot Prefab | `SkillSlotPrefab.prefab` (kéo vào) |
| Slot Container | chính `SkillHUD` transform (hoặc child `SlotsRow`) |
| Skill Icons | Danh sách Sprite icon theo thứ tự skill |

---

## Bước 7 — Gán `skillId` cho từng SkillData trong PlayerSkillManager

Chọn Player prefab → `PlayerSkillManager` → mở từng `SkillData` trong list `skills`:

| Skill | skillId | skillName |
|---|---|---|
| Fireball | `1` | `"Fireball"` |
| Teleport | `2` | `"Teleport"` |
| ... | ... | ... |

`skillId` phải **khớp với `skill_id`** trong database server (bảng `skill_template`).

> **Lưu ý TeleportSkill:** `TeleportSkill.cs` là component riêng, không nằm trong `PlayerSkillManager.skills`.  
> Nếu muốn hiển thị Teleport trên HUD, xem Bước 8.

---

## Bước 8 — (Tuỳ chọn) Hỗ trợ TeleportSkill trên HUD

`TeleportSkill` có `GetCooldownPercent()` riêng. Cách đơn giản nhất là tạo 1 wrapper `SkillData` giả cho Teleport trong `PlayerSkillManager.skills`, hoặc mở rộng `SkillHUDManager` để scan thêm component `TeleportSkill`:

**Trong `SkillHUDManager.BuildHUD()`**, thêm sau vòng `for` chính:

```csharp
// ── Teleport skill ────────────────────────────────────────────
var teleport = skillManager.GetComponent<TeleportSkill>();
if (teleport != null)
{
    bool teleportUnlocked = unlockedIds.Count == 0 || unlockedIds.Contains(teleport.skillId);
    if (teleportUnlocked)
    {
        GameObject go   = Instantiate(skillSlotPrefab, slotContainer);
        SkillSlotUI slot = go.GetComponent<SkillSlotUI>();
        if (slot != null)
        {
            slot.SetupTeleport(teleport, teleportIcon);
            spawnedSlots.Add(slot);
        }
    }
}
```

Thêm `public int skillId = 0;` và field `[SerializeField] private Sprite teleportIcon;` cho `SkillHUDManager`.

Thêm method `SetupTeleport` vào `SkillSlotUI.cs`:

```csharp
private TeleportSkill teleportRef;

public void SetupTeleport(TeleportSkill teleport, Sprite icon = null)
{
    teleportRef = teleport;
    skillData   = null;

    if (keyText   != null) keyText.text = teleport.teleportKey.ToString();
    if (iconImage != null && icon != null) iconImage.sprite = icon;
    if (cooldownOverlay != null) cooldownOverlay.gameObject.SetActive(false);
    if (countdownText   != null) countdownText.gameObject.SetActive(false);
}

private void Update()
{
    if (skillData != null)  { RefreshCooldown(); return; }
    if (teleportRef != null){ RefreshTeleportCooldown(); }
}

private void RefreshTeleportCooldown()
{
    float percent   = teleportRef.GetCooldownPercent();
    float remaining = teleportRef.cooldown * (1f - percent); // giây còn lại

    bool onCooldown = percent < 1f;

    if (cooldownOverlay != null)
    {
        cooldownOverlay.gameObject.SetActive(onCooldown);
        cooldownOverlay.fillAmount = 1f - percent;
    }

    if (countdownText != null)
    {
        countdownText.gameObject.SetActive(onCooldown);
        if (onCooldown)
            countdownText.text = remaining < 10f
                ? remaining.ToString("F1") + "s"
                : Mathf.CeilToInt(remaining) + "s";
    }
}
```

> **Lưu ý:** `TeleportSkill.cooldown` là `private` — cần đổi thành `public` hoặc thêm property `public float Cooldown => cooldown;` trong `TeleportSkill.cs`.

---

## Bước 9 — Rebuild HUD Sau Khi Unlock Skill Mới

Khi player unlock skill mới (ví dụ qua `UpgradeSkill` API hoặc level up gene):

```csharp
// Sau khi nhận response unlock thành công:
PlayerDataHolder.Instance.playerData = updatedResponse;
FindObjectOfType<SkillHUDManager>()?.Rebuild();
```

---

## Tóm Tắt File Cần Tạo / Sửa

| Hành động | File |
|---|---|
| **Sửa** — thêm `skillId` + `GetRemainingCooldown()` | `Scripts/Player/Skills/SkillData.cs` |
| **Tạo mới** | `Scripts/UI/HUD/SkillSlotUI.cs` |
| **Tạo mới** | `Scripts/UI/HUD/SkillHUDManager.cs` |
| **Tạo mới** | `Scripts/Services/PlayerDataHolder.cs` |
| **Tạo mới (Unity)** | `Prefabs/UI/HUD/SkillSlotPrefab.prefab` |
| **Sửa scene** | Thêm `SkillHUD` GameObject vào HUDPanel trong Canvas |
| **Sửa (Tuỳ chọn)** | `TeleportSkill.cs` — đổi `cooldown` thành `public` |

---

## Checklist Nhanh

- [ ] Thêm `skillId` và `GetRemainingCooldown()` vào `SkillData.cs`
- [ ] Tạo `SkillSlotUI.cs`
- [ ] Tạo `SkillHUDManager.cs`
- [ ] Tạo `PlayerDataHolder.cs` và gán data sau login
- [ ] Tạo prefab `SkillSlotPrefab` (Background + Icon + Overlay + Texts)
- [ ] Cấu hình `CooldownOverlay`: `Image Type = Filled`, `Fill Method = Radial360`, `Fill Origin = Top`
- [ ] Tạo `SkillHUD` trong scene, gán `HorizontalLayoutGroup`
- [ ] Gán `skillId` đúng với DB server cho từng `SkillData` trong Inspector
- [ ] Test: bắn skill → overlay chạy countdown → mất khi hết cooldown
- [ ] Kiểm tra skill chưa unlock bị ẩn đúng
