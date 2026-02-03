# HƯỚNG DẪN CHI TIẾT: SETUP UI HP CHO PLAYER VÀ ENEMY (PvE)

## 📋 MỤC LỤC
1. [Tổng quan](#tổng-quan)
2. [Setup UI HP cho Player](#setup-ui-hp-cho-player)
3. [Setup UI HP cho Enemy](#setup-ui-hp-cho-enemy)
4. [Tích hợp Combat System](#tích-hợp-combat-system)
5. [Test và Troubleshooting](#test-và-troubleshooting)

---

## 🎯 TỔNG QUAN

Hệ thống PvE (Player vs Enemy) bao gồm:
- **Player đánh Enemy** → Enemy mất HP → UI Enemy HP bar cập nhật
- **Enemy đánh Player** → Player mất HP → UI Player HP bar cập nhật

### Các thành phần cần thiết:

1. **PlayerHealthBar**: Hiển thị HP của player (đã có sẵn)
2. **EnemyHealthBar**: Hiển thị HP của enemy trên đầu enemy (mới tạo)
3. **PlayerCombat**: Xử lý player đánh enemy (đã có, cần cập nhật damage)
4. **EnemyAI**: Xử lý enemy đánh player (đã có, đã cập nhật)

---

## 👤 SETUP UI HP CHO PLAYER

### Bước 1: Tạo Canvas cho Player UI

1. Trong Scene, tạo **Canvas** mới: `PlayerUICanvas`
2. Setup Canvas:
   - **Render Mode**: Screen Space - Overlay (hoặc Screen Space - Camera)
   - **Canvas Scaler**: Scale With Screen Size
   - **Reference Resolution**: 1920 x 1080

### Bước 2: Tạo Player Health Bar UI

1. Trong `PlayerUICanvas`, tạo **Panel** mới: `PlayerHealthPanel`
2. Trong `PlayerHealthPanel`, tạo các UI elements:

#### **Health Slider:**
- Tạo **Slider**: `PlayerHealthSlider`
- **Rect Transform**:
  - Anchor: Top-Left
  - Position: (50, -50, 0) - Điều chỉnh theo ý bạn
  - Size: (200, 20)
- **Slider Settings**:
  - Min Value: 0
  - Max Value: 1
  - Value: 1
- **Fill Area** → **Fill**: Màu xanh lá (green)

#### **Health Text (Optional):**
- Tạo **Text** (hoặc **TextMeshPro - Text (UI)**): `PlayerHealthText`
- **Rect Transform**: Đặt trên hoặc bên cạnh slider
- **Text**: "100 / 100"
- **Font Size**: 14-16

### Bước 3: Setup HealthBar Component

1. Add Component → **HealthBar** vào `PlayerHealthPanel`
2. Kéo các UI elements vào Inspector:
   - **Health Slider** → `healthSlider`
   - **Fill Image** (từ Slider) → `fillImage`
   - **Health Text** → `healthText` (nếu có)
3. Cấu hình Colors:
   - **Full Health Color**: Xanh lá (0, 255, 0)
   - **Low Health Color**: Đỏ (255, 0, 0)
   - **Low Health Threshold**: 0.3 (30% HP)

### Bước 4: Kết nối với Player

**HealthBar sẽ tự động tìm PlayerHealth hoặc NetworkPlayerHealth**, không cần gán thủ công.

Nếu muốn gán thủ công:
- Kéo Player GameObject vào **Player Health** field (cho single-player)
- Hoặc kéo Player GameObject vào **Network Player Health** field (cho multiplayer)

---

## 👹 SETUP UI HP CHO ENEMY

### Bước 1: Tạo Canvas cho Enemy UI (World Space)

1. Tạo **Canvas** mới: `EnemyUICanvas`
2. Setup Canvas:
   - **Render Mode**: World Space
   - **Event Camera**: Main Camera
   - **Canvas Scaler**: Disabled (không cần scale)

### Bước 2: Tạo Enemy Health Bar Prefab

1. Trong `EnemyUICanvas`, tạo **Panel**: `EnemyHealthPanel`
2. Setup Panel:
   - **Rect Transform**:
     - Width: 100
     - Height: 15
     - Scale: (0.01, 0.01, 1) - Để nhỏ lại cho world space

3. Trong `EnemyHealthPanel`, tạo các UI elements:

#### **Health Slider:**
- Tạo **Slider**: `EnemyHealthSlider`
- **Rect Transform**:
  - Anchor: Center
  - Position: (0, 0, 0)
  - Size: (100, 10)
- **Slider Settings**:
  - Min Value: 0
  - Max Value: 1
  - Value: 1
- **Fill Area** → **Fill**: Màu đỏ (red) - Enemy thường dùng màu đỏ

#### **Health Text (Optional):**
- Tạo **TextMeshPro - Text**: `EnemyHealthText` (3D text)
- Hoặc **TextMeshPro - Text (UI)**: `EnemyHealthTextUI` (2D text)
- **Text**: "10 / 10"
- **Font Size**: 8-10

### Bước 3: Setup EnemyHealthBar Component

1. Add Component → **EnemyHealthBar** vào `EnemyHealthPanel`
2. Kéo các UI elements vào Inspector:
   - **Health Slider** → `healthSlider`
   - **Fill Image** (từ Slider) → `fillImage`
   - **Health Text** → `healthText` (UI Canvas)
   - **Health Text 3D** → `healthText3D` (3D Text trên enemy, optional)
3. Cấu hình:
   - **Offset**: (0, 1.5, 0) - Vị trí trên đầu enemy
   - **Always Face Camera**: ✅ (nếu dùng 3D text)
   - **Hide When Full**: ❌ (hiển thị luôn)
   - **Hide When Dead**: ✅ (ẩn khi chết)

### Bước 4: Tạo Enemy Health Bar Prefab

1. Chọn `EnemyHealthPanel`
2. Drag vào **Prefabs** folder để tạo prefab: `EnemyHealthBarPrefab`
3. Xóa `EnemyHealthPanel` khỏi Scene (chỉ giữ prefab)

### Bước 5: Gắn Health Bar vào Enemy Prefab

**Cách 1: Tự động (Recommended)**

1. Mở Enemy Prefab
2. Tạo GameObject con: `HealthBarContainer`
3. Instantiate `EnemyHealthBarPrefab` vào `HealthBarContainer`
4. Add Component → **EnemyHealthBar** vào health bar instance
5. Kéo `EnemyHealth` component vào **Enemy Health** field
6. Kéo Enemy Transform vào **Enemy Transform** field

**Cách 2: Tự động tìm (Nếu không gán)**

- `EnemyHealthBar` sẽ tự động tìm `EnemyHealth` trong parent
- Tự động lấy `enemyTransform` từ `EnemyHealth.transform`

**Cách 3: Setup trong code (Dynamic)**

```csharp
// Trong script spawn enemy
GameObject enemy = Instantiate(enemyPrefab);
EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();

// Spawn health bar
GameObject healthBar = Instantiate(enemyHealthBarPrefab, enemy.transform);
EnemyHealthBar healthBarComponent = healthBar.GetComponent<EnemyHealthBar>();
healthBarComponent.Setup(enemyHealth, enemy.transform);
```

---

## ⚔️ TÍCH HỢP COMBAT SYSTEM

### 1. Player đánh Enemy

#### **PlayerCombat đã được cập nhật:**
- ✅ Tính damage từ `PlayerStats.baseDamage`
- ✅ Gọi `EnemyHealth.TakeDamage(damage)`
- ✅ Enemy HP tự động cập nhật → UI tự động update

#### **Cách hoạt động:**
```
Player nhấn N → PlayerCombat.Attack() 
    → Detect enemies in range 
    → enemyHealth.TakeDamage(stats.baseDamage) 
    → EnemyHealth.OnHealthChanged event 
    → EnemyHealthBar.UpdateHealthBar() 
    → UI cập nhật
```

#### **Cấu hình Damage:**
1. Mở **PlayerStats** ScriptableObject
2. Điều chỉnh **Base Damage**: 10 (mặc định)
3. Player sẽ gây damage = baseDamage mỗi lần đánh

### 2. Enemy đánh Player

#### **EnemyAI đã được cập nhật:**
- ✅ Hỗ trợ cả `NetworkPlayerHealth` (multiplayer) và `PlayerHealth` (single-player)
- ✅ Gây damage từ `EnemyAI.damage` (mặc định: 2)
- ✅ Player HP tự động cập nhật → UI tự động update

#### **Cách hoạt động:**
```
EnemyAI.Update() 
    → Check distance to player 
    → StartMeleeAttack() 
    → Animation Event: OnAttackHit() 
    → playerHealth.TakeDamage(enemyAI.damage) 
    → PlayerHealth.OnHealthChanged event 
    → HealthBar.UpdateHealthBar() 
    → UI cập nhật
```

#### **Cấu hình Enemy Damage:**
1. Chọn Enemy Prefab
2. Trong **EnemyAI** component:
   - **Damage**: 2 (mặc định) - Điều chỉnh theo ý bạn
   - **Attack Cooldown**: 1.0 giây
   - **Melee Attack Range**: 1.2 (khoảng cách đánh)

---

## 🧪 TEST VÀ TROUBLESHOOTING

### Test Checklist:

#### **Test Player HP UI:**
- [ ] Player HP bar hiển thị đúng (100/100)
- [ ] Khi enemy đánh player, HP bar giảm
- [ ] Màu sắc thay đổi khi HP thấp (< 30%)
- [ ] Text cập nhật đúng (current/max)

#### **Test Enemy HP UI:**
- [ ] Enemy HP bar hiển thị trên đầu enemy
- [ ] HP bar follow enemy khi di chuyển
- [ ] Khi player đánh enemy, HP bar giảm
- [ ] HP bar ẩn khi enemy chết (nếu `hideWhenDead = true`)

#### **Test Combat:**
- [ ] Player nhấn N → Enemy mất HP
- [ ] Enemy đánh player → Player mất HP
- [ ] Damage đúng với cấu hình (Player: baseDamage, Enemy: damage)

### Troubleshooting:

#### **Vấn đề 1: Player HP bar không hiển thị**

**Nguyên nhân:**
- HealthBar không tìm thấy PlayerHealth
- Canvas chưa được setup đúng

**Giải pháp:**
1. Kiểm tra Console: Xem có log "[HealthBar] Using PlayerHealth" không
2. Kiểm tra Player có `PlayerHealth` hoặc `NetworkPlayerHealth` component không
3. Kiểm tra Canvas Render Mode: Screen Space - Overlay

#### **Vấn đề 2: Enemy HP bar không hiển thị**

**Nguyên nhân:**
- EnemyHealthBar không tìm thấy EnemyHealth
- Canvas World Space chưa setup đúng
- Offset quá xa

**Giải pháp:**
1. Kiểm tra Enemy có `EnemyHealth` component không
2. Kiểm tra Canvas Render Mode: World Space
3. Kiểm tra Event Camera được gán chưa
4. Điều chỉnh **Offset** trong EnemyHealthBar (ví dụ: (0, 1.5, 0))

#### **Vấn đề 3: Enemy HP bar không follow enemy**

**Nguyên nhân:**
- Enemy Transform chưa được gán
- Canvas Camera chưa được setup

**Giải pháp:**
1. Kiểm tra **Enemy Transform** được gán trong Inspector
2. Kiểm tra Canvas → Event Camera được gán Main Camera
3. Kiểm tra `UpdatePosition()` được gọi trong `Update()`

#### **Vấn đề 4: Damage không hoạt động**

**Nguyên nhân:**
- PlayerStats.baseDamage = 0
- EnemyAI.damage = 0
- Layer mask không đúng

**Giải pháp:**
1. Kiểm tra **PlayerStats.baseDamage** > 0
2. Kiểm tra **EnemyAI.damage** > 0
3. Kiểm tra **PlayerCombat → Enemy Layers** có chọn đúng layer của enemy không

#### **Vấn đề 5: HP bar không cập nhật**

**Nguyên nhân:**
- Event chưa được subscribe
- UI elements chưa được gán

**Giải pháp:**
1. Kiểm tra `OnHealthChanged` event có được subscribe không
2. Kiểm tra HealthBar/EnemyHealthBar có gán đúng UI elements không
3. Debug: Thêm log trong `UpdateHealthBar()` để xem có được gọi không

---

## 📝 TÓM TẮT QUY TRÌNH

### Setup Player HP UI:
1. ✅ Tạo Canvas (Screen Space - Overlay)
2. ✅ Tạo Slider + Text
3. ✅ Add HealthBar component
4. ✅ Tự động tìm PlayerHealth

### Setup Enemy HP UI:
1. ✅ Tạo Canvas (World Space)
2. ✅ Tạo EnemyHealthBar prefab
3. ✅ Gắn vào Enemy prefab
4. ✅ Tự động follow enemy

### Combat Flow:
1. ✅ Player đánh → Enemy mất HP → UI update
2. ✅ Enemy đánh → Player mất HP → UI update

---

## 🎨 TIPS VÀ BEST PRACTICES

### 1. UI Design:
- **Player HP**: Đặt ở góc trên trái, dễ nhìn
- **Enemy HP**: Nhỏ gọn, không che khuất enemy
- **Colors**: Player = Xanh, Enemy = Đỏ (theo convention)

### 2. Performance:
- Sử dụng Object Pooling cho EnemyHealthBar nếu có nhiều enemy
- Disable health bar khi enemy chết để tiết kiệm performance

### 3. UX:
- Hiển thị HP text để người chơi biết chính xác số HP
- Màu sắc thay đổi khi HP thấp để cảnh báo
- Smooth animation khi HP thay đổi (có thể thêm tween)

---

## 📚 CODE EXAMPLES

### Tự động spawn Enemy Health Bar:

```csharp
// Trong script spawn enemy
public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public GameObject enemyHealthBarPrefab;

    void SpawnEnemy(Vector3 position)
    {
        // Spawn enemy
        GameObject enemy = Instantiate(enemyPrefab, position, Quaternion.identity);
        EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();

        // Spawn health bar
        GameObject healthBar = Instantiate(enemyHealthBarPrefab, enemy.transform);
        EnemyHealthBar healthBarComponent = healthBar.GetComponent<EnemyHealthBar>();
        
        // Setup
        healthBarComponent.Setup(enemyHealth, enemy.transform);
    }
}
```

### Customize Health Bar Colors:

```csharp
// Trong HealthBar hoặc EnemyHealthBar
[Header("Custom Colors")]
public Color[] healthColors = new Color[] { 
    Color.red,      // 0-30%
    Color.yellow,   // 30-60%
    Color.green     // 60-100%
};

private void UpdateHealthBar(int current, int max)
{
    float percent = (float)current / max;
    
    // Chọn màu dựa trên percent
    Color targetColor;
    if (percent <= 0.3f) targetColor = healthColors[0];
    else if (percent <= 0.6f) targetColor = healthColors[1];
    else targetColor = healthColors[2];
    
    fillImage.color = targetColor;
}
```

---

**Tác giả**: Auto (AI Assistant)  
**Ngày tạo**: 2026  
**Phiên bản**: 1.0
