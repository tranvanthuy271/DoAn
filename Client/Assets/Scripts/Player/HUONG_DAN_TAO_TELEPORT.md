# Hướng Dẫn Tạo Skill Dịch Chuyển Tức Thời (Teleport/Blink)

## Tổng Quan

Hướng dẫn này sẽ giúp bạn tạo skill dịch chuyển tức thời cho player, cho phép player dịch chuyển một khoảng cách theo hướng đang nhìn.

## Yêu Cầu

- Unity Editor đã mở project
- Đã có Player object với PlayerController
- Đã có script `TeleportSkill.cs`

---

## Bước 1: Thêm TeleportSkill vào Player

### 1.1. Thêm Component

1. Trong **Hierarchy**, tìm và chọn **Player** object
2. Trong **Inspector**, click **Add Component**
3. Gõ `TeleportSkill` vào ô tìm kiếm
4. Click vào **Teleport Skill** trong danh sách kết quả
5. Component `TeleportSkill` sẽ được thêm vào player

### 1.2. Kiểm Tra Components Cần Thiết

Component `TeleportSkill` sẽ tự động tìm các component sau:
- ✅ **PlayerController**: Để xác định hướng player
- ✅ **CharacterController** hoặc **Rigidbody2D**: Để di chuyển player
- ✅ **NetworkObject**: Để hoạt động trong multiplayer

Nếu thiếu, hãy thêm vào player.

---

## Bước 2: Cấu Hình Teleport Skill

Với **Player** object được chọn, trong Inspector tìm component **Teleport Skill** và cấu hình:

### 2.1. Cấu Hình Cơ Bản

#### Teleport Settings:
- **Teleport Key**: Chọn phím để kích hoạt skill (ví dụ: `T`)
- **Cooldown**: Thời gian chờ giữa các lần sử dụng (ví dụ: `3` giây)
- **Teleport Distance**: Khoảng cách dịch chuyển (ví dụ: `5` units)
- **Teleport Duration**: 
  - Đặt `0` để teleport **tức thời** (khuyến nghị)
  - Đặt giá trị > 0 để teleport **smooth** (di chuyển mượt trong thời gian đó)
- **Check Collision**: 
  - ✅ Tick nếu muốn kiểm tra vật cản trước khi teleport (tránh teleport vào tường)
  - ❌ Bỏ tick nếu muốn teleport tự do (có thể teleport vào tường)
- **Obstacle Layer Mask**: Chọn layer chứa vật cản (ví dụ: Wall, Obstacle)
  - Nếu `Check Collision` = true, cần set layer này

### 2.2. Cấu Hình Animation (Tùy chọn)

#### Animation Settings:
- **Animation Trigger Name**: Tên Trigger trong Animator (ví dụ: `"Teleport"`)
  - Để trống nếu không muốn animation
  - Phải giống với tên parameter trong Animator Controller
- **Skill Effect Object**: Object SkillEffect để hiển thị animation
  - Để trống nếu không cần animation trên player
  - Hoặc kéo SkillEffect object từ player vào đây

### 2.3. Cấu Hình Hiệu Ứng (Tùy chọn)

#### Visual Effects:
- **Teleport Start Effect**: Prefab hiệu ứng khi bắt đầu teleport
  - Ví dụ: Particle System với hiệu ứng biến mất
  - Để trống nếu không cần
- **Teleport End Effect**: Prefab hiệu ứng khi kết thúc teleport
  - Ví dụ: Particle System với hiệu ứng xuất hiện
  - Để trống nếu không cần

---

## Bước 3: Setup Animation (Nếu có)

### 3.1. Tạo Parameter Trong Animator Controller

1. Mở Animator Controller (ví dụ: `SkillEffect.controller`)
2. Click tab **Parameters**
3. Click nút **+** → **Trigger**
4. Đặt tên: `Teleport` (hoặc tên bạn muốn)
5. Đảm bảo tên này giống với **Animation Trigger Name** trong TeleportSkill

### 3.2. Tạo State Và Transition

1. Trong Animator window, click chuột phải → **Create State** → **Empty**
2. Đặt tên state: `teleport` (hoặc tên tương tự)
3. Kéo animation clip vào state này (nếu có)
4. Tạo transition từ **Empty** → **teleport**:
   - Click vào state "Empty"
   - Click chuột phải → **Make Transition** → chọn state "teleport"
   - Click vào transition vừa tạo
   - Trong Inspector, thêm condition: **Teleport** (Trigger)
5. Tạo transition từ **teleport** → **Empty**:
   - Click vào state "teleport"
   - Click chuột phải → **Make Transition** → chọn state "Empty"
   - Click vào transition vừa tạo
   - Trong Inspector, tick **Has Exit Time**

### 3.3. Gán Animation Trigger Name

Trong **Teleport Skill** component, gán **Animation Trigger Name** = `"Teleport"` (hoặc tên parameter bạn đã tạo)

---

## Bước 4: Tạo Hiệu Ứng (Tùy chọn)

### 4.1. Tạo Teleport Start Effect

1. Tạo GameObject mới trong Hierarchy
2. Đặt tên: `TeleportStartEffect`
3. Thêm **Particle System** component:
   - Click **Add Component** → **Effects** → **Particle System**
   - Cấu hình particle:
     - **Start Color**: Màu bạn muốn (ví dụ: xanh dương)
     - **Start Size**: Kích thước particle
     - **Start Lifetime**: Thời gian sống
     - **Rate over Time**: Số lượng particle/giây
     - **Shape**: Hình dạng (ví dụ: Circle)
4. Lưu thành Prefab: `TeleportStartEffect.prefab`
5. Gán vào **Teleport Start Effect** trong TeleportSkill

### 4.2. Tạo Teleport End Effect

1. Tương tự như trên, tạo `TeleportEndEffect`
2. Cấu hình particle khác một chút (ví dụ: màu khác, hiệu ứng khác)
3. Lưu thành Prefab: `TeleportEndEffect.prefab`
4. Gán vào **Teleport End Effect** trong TeleportSkill

---

## Bước 5: Setup Collision Detection (Nếu cần)

### 5.1. Tạo Layer Cho Vật Cản

1. Mở **Edit** → **Project Settings** → **Tags and Layers**
2. Trong **Layers**, tìm layer trống (ví dụ: Layer 8)
3. Đặt tên: `Wall` hoặc `Obstacle`
4. Lưu lại

### 5.2. Gán Layer Cho Vật Cản

1. Chọn các GameObject là vật cản (tường, đá, v.v.)
2. Trong Inspector, tìm **Layer**
3. Chọn layer `Wall` hoặc `Obstacle` vừa tạo

### 5.3. Cấu Hình Obstacle Layer Mask

1. Chọn **Player** object
2. Trong **Teleport Skill** component, tìm **Obstacle Layer Mask**
3. Click vào dropdown
4. Tick vào layer `Wall` hoặc `Obstacle` (và các layer vật cản khác)
5. Bỏ tick các layer không phải vật cản

---

## Bước 6: Test Skill Teleport

### 6.1. Play Game

1. Click nút **Play** (▶) trong Unity Editor
2. Game sẽ chạy

### 6.2. Test Teleport

1. Trong game, điều khiển player
2. Nhấn phím **T** (hoặc phím bạn đã set)
3. Kiểm tra:
   - ✅ Player dịch chuyển một khoảng cách theo hướng đang nhìn
   - ✅ Nếu có animation, animation chạy
   - ✅ Nếu có hiệu ứng, hiệu ứng xuất hiện
   - ✅ Nếu có check collision, player không teleport vào tường
   - ✅ Cooldown hoạt động (không thể bấm liên tục)

### 6.3. Kiểm Tra Console

1. Mở **Console** window (Window → General → Console)
2. Kiểm tra xem có lỗi không
3. Nếu có lỗi, xem phần Troubleshooting

---

## Bước 7: Tùy Chỉnh (Nâng cao)

### 7.1. Thay Đổi Khoảng Cách

1. Chọn **Player** object
2. Trong **Teleport Skill**, thay đổi **Teleport Distance**:
   - Tăng lên (ví dụ: `10`) để teleport xa hơn
   - Giảm xuống (ví dụ: `3`) để teleport gần hơn

### 7.2. Thay Đổi Cooldown

1. Thay đổi **Cooldown**:
   - Giảm xuống (ví dụ: `1`) để teleport thường xuyên hơn
   - Tăng lên (ví dụ: `5`) để teleport ít hơn

### 7.3. Thay Đổi Tốc Độ Teleport

1. Thay đổi **Teleport Duration**:
   - Đặt `0` để teleport tức thời (mặc định)
   - Đặt giá trị > 0 (ví dụ: `0.5`) để teleport smooth (di chuyển mượt trong 0.5 giây)

### 7.4. Bật/Tắt Collision Check

1. Tick/bỏ tick **Check Collision**:
   - ✅ Tick: Player không thể teleport vào tường
   - ❌ Bỏ tick: Player có thể teleport tự do (có thể teleport vào tường)

---

## Troubleshooting

### Vấn đề: Teleport không hoạt động khi nhấn phím

**Giải pháp:**
- ✅ Kiểm tra **Teleport Key** đã được set đúng chưa
- ✅ Kiểm tra player có phải là Owner không (trong multiplayer)
- ✅ Kiểm tra Console để xem có lỗi không
- ✅ Kiểm tra **Cooldown** đã hết chưa (đợi vài giây rồi thử lại)

### Vấn đề: Player teleport vào tường

**Giải pháp:**
- ✅ Bật **Check Collision** = true
- ✅ Kiểm tra **Obstacle Layer Mask** đã được set đúng chưa
- ✅ Kiểm tra vật cản đã được gán đúng layer chưa
- ✅ Kiểm tra vật cản có Collider không

### Vấn đề: Teleport quá xa hoặc quá gần

**Giải pháp:**
- ✅ Điều chỉnh **Teleport Distance**:
  - Giảm nếu teleport quá xa
  - Tăng nếu teleport quá gần

### Vấn đề: Animation không chạy

**Giải pháp:**
- ✅ Kiểm tra **Animation Trigger Name** đã được gán đúng chưa
- ✅ Kiểm tra Animator Controller có parameter tương ứng không
- ✅ Kiểm tra SkillEffect object đã được gán chưa

### Vấn đề: Player bị kẹt sau khi teleport

**Giải pháp:**
- ✅ Kiểm tra có vật cản ở vị trí teleport không
- ✅ Tăng khoảng cách an toàn trong code (hiện tại là 0.5 units)
- ✅ Kiểm tra CharacterController hoặc Rigidbody2D có bị disable không

---

## Checklist Hoàn Thành

Trước khi test, đảm bảo:

- [ ] TeleportSkill component đã được thêm vào Player
- [ ] Teleport Key đã được set
- [ ] Teleport Distance đã được set (> 0)
- [ ] Cooldown đã được set (> 0)
- [ ] Check Collision đã được cấu hình (nếu cần)
- [ ] Obstacle Layer Mask đã được set (nếu Check Collision = true)
- [ ] Animation Trigger Name đã được gán (nếu có animation)
- [ ] Hiệu ứng đã được tạo và gán (nếu có)
- [ ] Đã test và teleport hoạt động đúng

---

## Ví Dụ Cấu Hình Hoàn Chỉnh

```
Teleport Settings:
├── Teleport Key: T
├── Cooldown: 3
├── Teleport Distance: 5
├── Teleport Duration: 0 (tức thời)
├── Check Collision: true
└── Obstacle Layer Mask: Wall, Obstacle

Animation Settings:
├── Animation Trigger Name: "Teleport"
└── Skill Effect Object: SkillEffect (từ player)

Visual Effects:
├── Teleport Start Effect: TeleportStartEffect.prefab
└── Teleport End Effect: TeleportEndEffect.prefab
```

---

## Lưu Ý Quan Trọng

1. **Teleport Duration = 0**: Teleport tức thời (khuyến nghị)
2. **Check Collision**: Nên bật để tránh teleport vào tường
3. **Obstacle Layer Mask**: Phải set đúng layer vật cản
4. **Cooldown**: Nên đặt > 0 để tránh spam teleport
5. **Network**: Skill chỉ hoạt động cho Owner (trong multiplayer)

---

## Kết Luận

Sau khi hoàn thành các bước trên, bạn sẽ có:
- ✅ Skill teleport hoàn chỉnh
- ✅ Có thể dịch chuyển bằng phím T
- ✅ Có cooldown system
- ✅ Có thể kiểm tra collision
- ✅ Có animation và hiệu ứng (nếu đã setup)

Chúc bạn thành công! ✨
