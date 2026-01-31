# Hướng Dẫn Sử Dụng Hệ Thống Skill Projectile

## Tổng Quan

Hệ thống skill này cho phép bạn dễ dàng thêm skill projectile mới cho player. Chỉ cần thêm thông tin skill vào danh sách và skill sẽ tự động hoạt động!

## Yêu Cầu

- Unity Editor đã mở project
- Đã có script `PlayerSkillManager.cs` được gắn vào Player object
- Đã có Projectile Prefab cho skill mới

---

## Bước 1: Thêm PlayerSkillManager vào Player

1. Trong Hierarchy, chọn **Player** object
2. Trong Inspector, click **Add Component**
3. Tìm và chọn **Player Skill Manager**
4. Component `PlayerSkillManager` sẽ được thêm vào player

---

## Bước 2: Thêm Skill Mới

### Cách 1: Thêm Skill Trong Inspector (Khuyến nghị)

1. Với **Player** object được chọn, tìm component **Player Skill Manager** trong Inspector
2. Tìm field **Skills List** (danh sách các skill)
3. Click nút **+** để thêm skill mới vào danh sách
4. Cấu hình các thông tin sau cho skill:

#### Thông Tin Cơ Bản:
- **Skill Name**: Tên skill (ví dụ: "Fireball", "Ice Shard", "Lightning Bolt")
- **Activation Key**: Phím để kích hoạt skill (ví dụ: `K`, `J`, `L`)
- **Cooldown**: Thời gian cooldown giữa các lần sử dụng (giây)

#### Cấu Hình Projectile:
- **Projectile Prefab**: Kéo Projectile Prefab vào đây
- **Projectile Speed**: Tốc độ bay của projectile (units/second)
- **Spawn Offset**: Khoảng cách spawn từ vị trí player
- **Projectile Lifetime**: Thời gian sống của projectile (giây). Đặt 0 để không tự hủy

#### Cấu Hình Animation (Tùy chọn):
- **Animation Trigger Name**: Tên Trigger trong Animator (ví dụ: "Skill2", "Fireball", "IceShard")
  - Để trống nếu không muốn trigger animation
- **Player Skill Effect Object**: Object SkillEffect để hiển thị animation trên player (tùy chọn)
- **Projectile Skill Effect Prefab**: Prefab SkillEffect để gắn vào projectile (tùy chọn)
- **Disable Player Skill Effect Animation**: Tick nếu không muốn animation hiển thị trên player

5. Lặp lại để thêm skill khác nếu cần

### Cách 2: Thêm Skill Bằng Code (Nâng cao)

```csharp
// Trong script của bạn
PlayerSkillManager skillManager = GetComponent<PlayerSkillManager>();
SkillData newSkill = new SkillData
{
    skillName = "MyNewSkill",
    activationKey = KeyCode.T,
    cooldown = 3f,
    projectilePrefab = myProjectilePrefab,
    projectileSpeed = 15f,
    spawnOffset = 0.5f,
    projectileLifetime = 5f,
    animationTriggerName = "MySkill"
};
skillManager.skills.Add(newSkill);
```

---

## Bước 3: Setup Projectile Prefab

Mỗi skill cần một Projectile Prefab riêng. Setup như sau:

### 3.1. Tạo/Cấu Hình Projectile Prefab

1. Tạo hoặc mở Projectile Prefab trong Unity
2. Đảm bảo Prefab có các component sau:
   - ✅ **SpriteRenderer** (nếu có sprite)
   - ✅ **Rigidbody2D** (BẮT BUỘC)
   - ✅ **Animator** (nếu muốn animation trên projectile)
   - ✅ **Collider2D** (nếu cần collision)

### 3.2. Cấu Hình Rigidbody2D

1. Chọn **Rigidbody2D** component
2. Cấu hình:
   - **Body Type**: **Dynamic**
   - **Gravity Scale**: **0** (để bay ngang)
   - **Constraints**: Không freeze **Position X**

### 3.3. Cấu Hình Animator (Nếu có)

1. Chọn **Animator** component
2. Cấu hình:
   - **Controller**: Gán Animator Controller (ví dụ: SkillEffect.controller)
   - **Apply Root Motion**: **TẮT** (quan trọng!)

### 3.4. Lưu Prefab

1. Click **Overrides** → **Apply All** (nếu có)
2. Hoặc đóng Prefab Mode

---

## Bước 4: Setup Animation (Tùy chọn)

Nếu skill có animation:

### 4.1. Setup Animator Controller

1. Mở Animator Controller (ví dụ: `SkillEffect.controller`)
2. Đảm bảo có:
   - ✅ Parameter với tên giống **Animation Trigger Name** (kiểu Trigger)
   - ✅ State với animation tương ứng
   - ✅ Transition từ Empty → State khi trigger
   - ✅ Transition từ State → Empty khi animation kết thúc

### 4.2. Gán Animation Trigger Name

Trong **Skill Data**, gán **Animation Trigger Name** giống với tên parameter trong Animator Controller.

---

## Ví Dụ: Thêm Skill "Fireball"

1. **Tạo Projectile Prefab**:
   - Tạo GameObject mới
   - Thêm SpriteRenderer với sprite lửa
   - Thêm Rigidbody2D (Gravity Scale = 0)
   - Thêm Animator (Apply Root Motion = TẮT)
   - Lưu thành Prefab: `FireballProjectile`

2. **Thêm Skill vào PlayerSkillManager**:
   - Skill Name: `"Fireball"`
   - Activation Key: `K`
   - Cooldown: `2`
   - Projectile Prefab: `FireballProjectile`
   - Projectile Speed: `15`
   - Spawn Offset: `0.5`
   - Projectile Lifetime: `5`
   - Animation Trigger Name: `"Fireball"` (nếu có)

3. **Test**: Play game và nhấn `K` để bắn Fireball!

---

## Quản Lý Nhiều Skill

Bạn có thể thêm nhiều skill vào cùng một player:

```
Skills List:
├── Skill 0: Fireball (Key: K)
├── Skill 1: Ice Shard (Key: J)
├── Skill 2: Lightning Bolt (Key: L)
└── Skill 3: Poison Dart (Key: U)
```

Mỗi skill hoạt động độc lập với:
- Phím riêng
- Projectile riêng
- Cooldown riêng
- Animation riêng (nếu có)

---

## API Sử Dụng Trong Code

### Kiểm Tra Trạng Thái Skill

```csharp
PlayerSkillManager skillManager = GetComponent<PlayerSkillManager>();

// Kiểm tra skill đang được sử dụng
bool isUsing = skillManager.IsUsingSkill("Fireball");

// Kiểm tra skill có thể sử dụng
bool canUse = skillManager.CanUseSkill("Fireball");

// Lấy phần trăm cooldown (0 = đang cooldown, 1 = sẵn sàng)
float cooldownPercent = skillManager.GetSkillCooldownPercent("Fireball");
```

---

## Troubleshooting

### Vấn đề: Skill không hoạt động khi nhấn phím

**Giải pháp:**
- ✅ Kiểm tra PlayerSkillManager đã được gắn vào Player chưa
- ✅ Kiểm tra Activation Key đã được gán đúng chưa
- ✅ Kiểm tra Projectile Prefab đã được gán chưa
- ✅ Kiểm tra Console để xem có lỗi không

### Vấn đề: Projectile không di chuyển

**Giải pháp:**
- ✅ Kiểm tra Rigidbody2D có Gravity Scale = 0 không
- ✅ Kiểm tra Rigidbody2D không bị freeze Position X
- ✅ Kiểm tra Projectile Speed > 0
- ✅ Kiểm tra Apply Root Motion = TẮT trên Animator

### Vấn đề: Animation không chạy

**Giải pháp:**
- ✅ Kiểm tra Animation Trigger Name đã được gán đúng chưa
- ✅ Kiểm tra Animator Controller có parameter tương ứng không
- ✅ Kiểm tra Animator Controller đã được gán vào Animator chưa

### Vấn đề: Nhiều skill dùng cùng một phím

**Giải pháp:**
- ✅ Mỗi skill phải có Activation Key riêng
- ✅ Nếu 2 skill dùng cùng phím, chỉ skill đầu tiên trong list sẽ hoạt động

---

## Lưu Ý Quan Trọng

1. **Mỗi skill cần Projectile Prefab riêng**: Không dùng chung prefab cho nhiều skill
2. **Apply Root Motion phải TẮT**: Nếu bật, projectile sẽ không di chuyển được
3. **Rigidbody2D là bắt buộc**: Projectile cần Rigidbody2D để di chuyển
4. **Gravity Scale = 0**: Để projectile bay ngang, không rơi
5. **Tên skill phải unique**: Mỗi skill nên có tên riêng để dễ quản lý

---

## Kết Luận

Với hệ thống này, bạn có thể dễ dàng thêm skill mới chỉ bằng cách:
1. Tạo Projectile Prefab
2. Thêm Skill Data vào PlayerSkillManager
3. Done! Skill tự động hoạt động

Không cần viết code mới cho mỗi skill!
