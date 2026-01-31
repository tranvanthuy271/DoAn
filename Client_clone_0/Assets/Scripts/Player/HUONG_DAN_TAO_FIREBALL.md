# Hướng Dẫn Tạo Skill Fireball Từ Đầu Đến Cuối

## Tổng Quan

Hướng dẫn này sẽ giúp bạn tạo skill Fireball hoàn chỉnh từ đầu, bao gồm:
- Tạo Projectile Prefab
- Setup Animation (nếu có)
- Thêm Skill vào PlayerSkillManager
- Test và Debug

---

## Bước 1: Tạo Fireball Projectile Prefab

### 1.1. Tạo GameObject cho Fireball

1. Trong Unity Editor, mở **Hierarchy** window
2. Click chuột phải trong Hierarchy → **2D Object** → **Sprite** (hoặc **Create Empty** nếu muốn tạo GameObject trống)
3. Đặt tên GameObject là: `FireballProjectile`
4. Chọn GameObject `FireballProjectile` trong Hierarchy

### 1.2. Thêm Sprite (Nếu có)

1. Với `FireballProjectile` được chọn, trong **Inspector** tìm component **Sprite Renderer**
2. Nếu chưa có sprite:
   - Tìm sprite Fireball trong **Project** window (hoặc import sprite mới)
   - Kéo sprite vào field **Sprite** trong Sprite Renderer
3. Nếu chưa có Sprite Renderer:
   - Click **Add Component** → **Sprite Renderer**
   - Gán sprite vào

### 1.3. Thêm Rigidbody2D (BẮT BUỘC)

1. Với `FireballProjectile` được chọn, click **Add Component**
2. Tìm và chọn **Rigidbody 2D**
3. Cấu hình Rigidbody2D:
   - **Body Type**: Chọn **Dynamic**
   - **Gravity Scale**: Đặt **0** (quan trọng! Để projectile bay ngang, không rơi)
   - **Constraints**: 
     - ✅ Tick **Freeze Rotation Z** (nếu muốn projectile không xoay)
     - ❌ **KHÔNG** tick **Freeze Position X** (phải để trống để projectile di chuyển được)
     - ❌ **KHÔNG** tick **Freeze Position Y** (hoặc tick nếu muốn bay hoàn toàn ngang)

### 1.4. Thêm Collider2D (Tùy chọn - Nếu cần collision)

1. Click **Add Component**
2. Chọn **Circle Collider 2D** (hoặc **Box Collider 2D** tùy hình dạng)
3. Điều chỉnh kích thước collider cho phù hợp với sprite
4. Nếu không cần collision, có thể bỏ qua bước này

### 1.5. Thêm Animator (Nếu muốn có animation trên projectile)

#### Bước 1.5.1: Thêm Component Animator

1. Với GameObject `FireballProjectile` được chọn trong Hierarchy
2. Trong **Inspector** window (thường ở bên phải), cuộn xuống dưới cùng
3. Tìm và click nút **Add Component** (màu xanh, ở dưới cùng của Inspector)
4. Một menu sẽ hiện ra, gõ `Animator` vào ô tìm kiếm
5. Click vào **Animator** (không phải Animation!) trong danh sách kết quả
6. Component **Animator** sẽ được thêm vào GameObject

#### Bước 1.5.2: Tìm Animator Controller

Trước khi gán Controller, bạn cần tìm file Animator Controller:

1. Mở **Project** window (thường ở dưới cùng hoặc bên trái)
2. Tìm file Animator Controller:
   - Thường nằm trong: `Assets/Animations/Skills/SkillEffect.controller`
   - Hoặc tìm file có đuôi `.controller` trong project
3. Nếu chưa có Animator Controller:
   - Click chuột phải trong Project window → **Create** → **Animator Controller**
   - Đặt tên: `SkillEffect` (hoặc tên bạn muốn)
   - Lưu vào thư mục: `Assets/Animations/Skills/`

#### Bước 1.5.3: Gán Animator Controller

1. Với GameObject `FireballProjectile` được chọn, trong Inspector tìm component **Animator** vừa thêm
2. Tìm field **Controller** (field đầu tiên trong Animator component)
3. Có 2 cách để gán Controller:

   **Cách 1: Kéo thả (Khuyến nghị)**
   - Mở **Project** window
   - Tìm file `SkillEffect.controller` (hoặc Animator Controller bạn muốn dùng)
   - Kéo file đó từ Project window vào field **Controller** trong Inspector
   - Field **Controller** sẽ hiển thị tên controller thay vì "None (Animator Controller)"

   **Cách 2: Chọn từ menu**
   - Click vào field **Controller** (có icon hình tròn bên cạnh)
   - Một menu sẽ hiện ra với danh sách các Animator Controller có sẵn
   - Click chọn controller bạn muốn (ví dụ: `SkillEffect`)

4. Sau khi gán, field **Controller** sẽ hiển thị tên controller (ví dụ: `SkillEffect`)

#### Bước 1.5.4: Cấu Hình Apply Root Motion (QUAN TRỌNG!)

1. Với component **Animator** được chọn trong Inspector
2. Tìm checkbox **Apply Root Motion** (thường ở dưới field Controller)
3. **ĐẢM BẢO checkbox này KHÔNG được tick** (unchecked/trống)
   - Nếu checkbox có dấu tick (✓), click vào để bỏ tick
   - Phải để trống hoàn toàn

**Tại sao quan trọng?**
- Nếu **Apply Root Motion** = BẬT (ticked): Animation sẽ điều khiển vị trí của GameObject
- Điều này sẽ **override** movement của projectile → Projectile sẽ không di chuyển được
- Nếu **Apply Root Motion** = TẮT (unchecked): Animation chỉ ảnh hưởng đến sprite/visual, không ảnh hưởng đến vị trí
- Projectile sẽ di chuyển bình thường nhờ Rigidbody2D và ProjectileMovement script

**Kiểm tra:**
- ✅ **Apply Root Motion** = Trống/Không tick = ĐÚNG
- ❌ **Apply Root Motion** = Có tick = SAI → Click để bỏ tick

#### Bước 1.5.5: Cấu Hình Update Mode

1. Tìm field **Update Mode** trong Animator component
2. Có 3 lựa chọn:
   - **Normal**: Animation chạy theo Time.timeScale (mặc định) - **Khuyến nghị dùng cái này**
   - **Unscaled Time**: Animation chạy độc lập với Time.timeScale (dùng khi game bị pause)
   - **Animate Physics**: Animation sync với physics update (ít dùng)

3. Chọn **Normal** (mặc định) - không cần thay đổi

#### Bước 1.5.6: Cấu Hình Culling Mode

1. Tìm field **Culling Mode** trong Animator component
2. Có 3 lựa chọn:
   - **Always Animate**: Animation luôn chạy dù GameObject có trong camera view hay không - **Khuyến nghị cho projectile**
   - **Cull Update Transforms**: Chỉ update transform khi trong camera view
   - **Cull Completely**: Tắt hoàn toàn khi không trong camera view

3. Chọn **Always Animate** - đảm bảo animation luôn chạy trên projectile

#### Bước 1.5.7: Kiểm Tra Cấu Hình

Sau khi hoàn thành, Animator component sẽ có cấu hình như sau:

```
Animator Component:
├── Controller: SkillEffect (hoặc tên controller của bạn)
├── Avatar: None
├── Apply Root Motion: [ ] (KHÔNG tick)
├── Update Mode: Normal
└── Culling Mode: Always Animate
```

**Checklist:**
- [ ] Controller đã được gán (không còn "None")
- [ ] Apply Root Motion = TẮT (không tick)
- [ ] Update Mode = Normal
- [ ] Culling Mode = Always Animate

#### Bước 1.5.8: Lưu Ý Quan Trọng

1. **Nếu chưa có Animator Controller:**
   - Bạn có thể bỏ qua bước gán Controller
   - Code sẽ vẫn hoạt động, chỉ không có animation
   - Có thể thêm Controller sau

2. **Nếu Controller chưa có parameter "Fireball":**
   - Bạn vẫn có thể gán Controller
   - Nhưng animation sẽ không trigger được
   - Cần thêm parameter "Fireball" (Trigger) vào Controller (xem Bước 2)

3. **Nếu không muốn animation trên projectile:**
   - Có thể bỏ qua bước này hoàn toàn
   - Projectile vẫn hoạt động bình thường
   - Chỉ không có animation visual

#### Bước 1.5.9: Test Nhanh (Tùy chọn)

1. Với `FireballProjectile` được chọn, trong Inspector tìm Animator component
2. Nếu Controller đã được gán, bạn sẽ thấy:
   - Field **Controller** có tên controller (không còn "None")
   - Có thể click vào controller để mở Animator window
3. Nếu muốn xem Animator Controller:
   - Double-click vào Controller trong field
   - Animator window sẽ mở ra (Window → Animation → Animator)
   - Bạn sẽ thấy các states và transitions

---

**Lưu ý:** Nếu gặp khó khăn ở bước nào, hãy kiểm tra lại từng bước hoặc xem phần Troubleshooting ở cuối hướng dẫn.

### 1.6. Điều Chỉnh Kích Thước (Nếu cần)

1. Với `FireballProjectile` được chọn, trong Inspector tìm **Transform**
2. Điều chỉnh **Scale** cho phù hợp (ví dụ: `(1, 1, 1)` hoặc `(0.5, 0.5, 1)`)
3. Điều chỉnh **Rotation** nếu cần (thường là `(0, 0, 0)`)

### 1.7. Lưu Thành Prefab

1. Trong **Project** window, tạo hoặc mở thư mục `Assets/Prefabs/` (hoặc thư mục bạn muốn)
2. Kéo GameObject `FireballProjectile` từ **Hierarchy** vào thư mục trong **Project** window
3. Prefab sẽ được tạo: `FireballProjectile.prefab`
4. Xóa GameObject `FireballProjectile` trong Hierarchy (giữ lại prefab trong Project)

**Lưu ý:** Nếu muốn chỉnh sửa prefab sau này:
- Double-click vào prefab trong Project window
- Chỉnh sửa trong Prefab Mode
- Click **<** ở trên cùng để thoát Prefab Mode

---

## Bước 2: Setup Animation (Tùy chọn - Nếu muốn có animation)

### 2.1. Kiểm Tra Animator Controller

1. Mở Animator Controller (ví dụ: `Assets/Animations/Skills/SkillEffect.controller`)
2. Kiểm tra xem đã có:
   - ✅ Parameter **"Fireball"** (kiểu Trigger) - nếu chưa có, tạo mới:
     - Click **Parameters** tab
     - Click **+** → **Trigger**
     - Đặt tên: `Fireball`
   - ✅ State **"fireball"** (hoặc tên tương tự) - nếu chưa có, tạo mới:
     - Click chuột phải trong Animator window → **Create State** → **Empty**
     - Đặt tên: `fireball`
     - Kéo animation clip vào state này
   - ✅ Transition từ **"Empty"** → **"fireball"** khi trigger "Fireball"
   - ✅ Transition từ **"fireball"** → **"Empty"** khi animation kết thúc

### 2.2. Tạo Animation Clip (Nếu chưa có)

1. Trong **Project** window, tìm hoặc tạo thư mục `Assets/Animations/Skills/`
2. Click chuột phải → **Create** → **Animation**
3. Đặt tên: `FireballAnimation`
4. Chọn GameObject có sprite Fireball (hoặc tạo GameObject mới)
5. Mở **Animation** window (Window → Animation → Animation)
6. Click **Create** để tạo animation
7. Tạo keyframes cho animation (ví dụ: scale, rotation, color, v.v.)
8. Lưu animation

### 2.3. Gán Animation Vào Animator Controller

1. Mở Animator Controller
2. Chọn state **"fireball"**
3. Trong Inspector, kéo animation clip vào field **Motion**

---

## Bước 3: Thêm Skill Fireball Vào PlayerSkillManager

### 3.1. Mở Player Object

1. Trong **Hierarchy**, tìm và chọn **Player** object
2. Trong **Inspector**, tìm component **Player Skill Manager**
   - Nếu chưa có, click **Add Component** → **Player Skill Manager**

### 3.2. Thêm Skill Mới

1. Với **Player Skill Manager** được chọn, tìm field **Skills List**
2. Click nút **+** (hoặc thay đổi **Size** từ 0 lên 1) để thêm skill mới
3. Mở rộng skill vừa thêm (click mũi tên bên trái)

### 3.3. Cấu Hình Thông Tin Skill

Điền các thông tin sau:

#### Thông Tin Cơ Bản:
- **Skill Name**: `Fireball` (hoặc tên bạn muốn)
- **Activation Key**: Chọn `K` (hoặc phím khác bạn muốn)
- **Cooldown**: `2` (giây) - thời gian chờ giữa các lần bắn

#### Cấu Hình Projectile:
- **Projectile Prefab**: Kéo `FireballProjectile.prefab` từ Project window vào đây
- **Projectile Speed**: `15` (units/second) - tốc độ bay của fireball
- **Spawn Offset**: `0.5` (units) - khoảng cách spawn từ vị trí player
- **Projectile Lifetime**: `5` (giây) - thời gian sống của fireball trước khi tự hủy

#### Cấu Hình Animation (Nếu có):
- **Animation Trigger Name**: `Fireball` (phải giống với tên parameter trong Animator Controller)
- **Player Skill Effect Object**: 
  - Để trống nếu không muốn animation trên player
  - Hoặc kéo SkillEffect object từ player vào đây
- **Projectile Skill Effect Prefab**: 
  - Để trống nếu đã gắn Animator trực tiếp vào projectile
  - Hoặc kéo SkillEffect prefab vào đây nếu muốn animation riêng
- **Disable Player Skill Effect Animation**: 
  - Tick nếu chỉ muốn animation trên projectile
  - Bỏ tick nếu muốn animation cả trên player và projectile

### 3.4. Lưu Cấu Hình

1. Các thay đổi sẽ tự động được lưu
2. Không cần làm gì thêm

---

## Bước 4: Test Skill Fireball

### 4.1. Play Game

1. Click nút **Play** (▶) ở trên cùng Unity Editor
2. Game sẽ chạy trong Scene view hoặc Game view

### 4.2. Test Skill

1. Trong game, điều khiển player
2. Nhấn phím **K** (hoặc phím bạn đã set trong Activation Key)
3. Kiểm tra:
   - ✅ Fireball được spawn từ vị trí player
   - ✅ Fireball bay ngang theo hướng player đang nhìn
   - ✅ Animation chạy trên fireball (nếu có)
   - ✅ Fireball tự hủy sau 5 giây (hoặc khi va chạm nếu có collision)

### 4.3. Kiểm Tra Console

1. Mở **Console** window (Window → General → Console)
2. Kiểm tra xem có log nào không:
   - ✅ `[PlayerSkillManager] Đã khởi tạo X skill(s)` - skill đã được load
   - ✅ `[PlayerSkillManager] Đã trigger animation 'Fireball' trên projectile!` - animation đã chạy
   - ❌ Nếu có warning/error, xem phần Troubleshooting bên dưới

---

## Bước 5: Tùy Chỉnh Fireball (Nâng cao)

### 5.1. Thay Đổi Tốc Độ

1. Chọn **Player** object
2. Trong **Player Skill Manager**, tìm skill Fireball
3. Thay đổi **Projectile Speed**:
   - Tăng lên (ví dụ: `20`) để fireball bay nhanh hơn
   - Giảm xuống (ví dụ: `10`) để fireball bay chậm hơn

### 5.2. Thay Đổi Khoảng Cách Bay

1. Thay đổi **Projectile Lifetime**:
   - Tăng lên (ví dụ: `10`) để fireball bay xa hơn
   - Giảm xuống (ví dụ: `3`) để fireball bay gần hơn

### 5.3. Thay Đổi Cooldown

1. Thay đổi **Cooldown**:
   - Giảm xuống (ví dụ: `1`) để bắn nhanh hơn
   - Tăng lên (ví dụ: `5`) để bắn chậm hơn

### 5.4. Thêm Hiệu Ứng

1. Mở **FireballProjectile** prefab
2. Thêm các component:
   - **Particle System** (cho hiệu ứng lửa)
   - **Light 2D** (cho ánh sáng)
   - **Trail Renderer** (cho vệt đuôi)
3. Lưu prefab

---

## Troubleshooting

### Vấn đề: Fireball không xuất hiện khi nhấn K

**Giải pháp:**
- ✅ Kiểm tra **Activation Key** đã được set đúng chưa
- ✅ Kiểm tra **Projectile Prefab** đã được gán vào skill chưa
- ✅ Kiểm tra Console để xem có lỗi không
- ✅ Kiểm tra player có phải là Owner không (trong multiplayer)

### Vấn đề: Fireball xuất hiện nhưng không di chuyển

**Giải pháp:**
- ✅ Kiểm tra **Rigidbody2D**:
  - Body Type = **Dynamic**
  - Gravity Scale = **0**
  - Không freeze **Position X**
- ✅ Kiểm tra **Animator**:
  - **Apply Root Motion** = **TẮT**
- ✅ Kiểm tra **Projectile Speed** > 0
- ✅ Kiểm tra Console để xem có warning về Rigidbody2D không

### Vấn đề: Animation không chạy

**Giải pháp:**
- ✅ Kiểm tra **Animation Trigger Name** đã được gán đúng chưa
- ✅ Kiểm tra Animator Controller có parameter tên "Fireball" không
- ✅ Kiểm tra Animator Controller đã được gán vào Animator chưa
- ✅ Kiểm tra Console để xem có log trigger animation không

### Vấn đề: Fireball rơi xuống thay vì bay ngang

**Giải pháp:**
- ✅ Kiểm tra **Gravity Scale** = **0** trong Rigidbody2D
- ✅ Kiểm tra code có set `rb.gravityScale = 0f` không (code tự động làm)

### Vấn đề: Fireball biến mất ngay lập tức

**Giải pháp:**
- ✅ Kiểm tra **Projectile Lifetime** > 0
- ✅ Kiểm tra có collision nào destroy fireball không
- ✅ Kiểm tra có script nào destroy fireball không

---

## Checklist Hoàn Thành

Trước khi test, đảm bảo:

- [ ] FireballProjectile prefab đã được tạo
- [ ] Rigidbody2D đã được thêm và cấu hình đúng (Gravity Scale = 0)
- [ ] Animator đã được thêm (nếu có animation) và Apply Root Motion = TẮT
- [ ] Skill Fireball đã được thêm vào PlayerSkillManager
- [ ] Projectile Prefab đã được gán vào skill
- [ ] Activation Key đã được set
- [ ] Animation Trigger Name đã được gán (nếu có animation)
- [ ] Đã test và fireball hoạt động đúng

---

## Kết Luận

Sau khi hoàn thành các bước trên, bạn sẽ có:
- ✅ Skill Fireball hoàn chỉnh
- ✅ Có thể bắn bằng phím K
- ✅ Có animation (nếu đã setup)
- ✅ Có cooldown system
- ✅ Dễ dàng tùy chỉnh

Bây giờ bạn có thể tạo thêm các skill khác (Ice Shard, Lightning Bolt, v.v.) bằng cách lặp lại các bước trên với prefab và cấu hình khác!

---

## Ví Dụ Cấu Hình Hoàn Chỉnh

```
Skill Name: Fireball
Activation Key: K
Cooldown: 2
Projectile Prefab: FireballProjectile
Projectile Speed: 15
Spawn Offset: 0.5
Projectile Lifetime: 5
Animation Trigger Name: Fireball
Player Skill Effect Object: (để trống)
Projectile Skill Effect Prefab: (để trống)
Disable Player Skill Effect Animation: false
```

Chúc bạn thành công! 🔥
