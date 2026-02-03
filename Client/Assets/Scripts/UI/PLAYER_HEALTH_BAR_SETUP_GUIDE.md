# Hướng Dẫn Setup Health Bar Cho Player

## Bước 1: Tạo PlayerStats ScriptableObject

1. Trong Unity, click chuột phải vào thư mục `Assets/ScriptableObjects` (hoặc tạo mới nếu chưa có)
2. Chọn `Create > Game > Player Stats`
3. Đặt tên file: `PlayerStats` (hoặc tên bạn muốn)
4. Trong Inspector, config các giá trị:
   - **Max Health**: Số máu tối đa (ví dụ: 100)
   - Các thông số khác tùy chỉnh

## Bước 2: Gán PlayerStats vào PlayerController

1. Chọn GameObject Player trong Scene
2. Trong Inspector, tìm component `PlayerController`
3. Kéo thả file `PlayerStats` (ScriptableObject) vào field **Stats**

## Bước 3: Tạo UI Health Bar

### 3.1. Tạo Canvas (nếu chưa có)

1. Click chuột phải trong Hierarchy
2. Chọn `UI > Canvas`
3. Đặt tên: `PlayerHealthCanvas` (hoặc tên khác)

### 3.2. Tạo Health Bar UI

1. Click chuột phải vào Canvas
2. Chọn `UI > Slider`
3. Đặt tên: `PlayerHealthSlider`

### 3.3. Setup Slider

1. Chọn `PlayerHealthSlider`
2. Trong Inspector:
   - **Min Value**: 0
   - **Max Value**: 1
   - **Value**: 1 (máu đầy)
   - **Whole Numbers**: Bỏ tick

### 3.4. Tùy chỉnh giao diện (tùy chọn)

1. Trong Slider, có các child objects:
   - **Background**: Nền của slider
   - **Fill Area > Fill**: Phần fill (màu hiển thị HP)
   - **Handle Slide Area**: Handle (có thể xóa nếu không cần)

2. Tùy chỉnh màu sắc:
   - Chọn `Fill Area > Fill`
   - Trong Image component, đổi màu (ví dụ: màu xanh lá khi đầy máu)

### 3.5. Tạo Text hiển thị số HP (tùy chọn)

**Cách 1: Dùng TextMeshPro (Khuyến nghị)**
1. Click chuột phải vào Canvas
2. Chọn `UI > Text - TextMeshPro`
3. Nếu lần đầu dùng, Unity sẽ import TMP Essentials (chọn Import)
4. Đặt tên: `PlayerHealthText`
5. Đặt vị trí phù hợp (ví dụ: trên health bar)
6. Trong TextMeshPro component:
   - **Text**: "100 / 100" (tạm thời)
   - **Font Size**: Tùy chỉnh
   - **Alignment**: Center

**Cách 2: Dùng Legacy Text**
1. Click chuột phải vào Canvas
2. Chọn `UI > Text` (Legacy)
3. Đặt tên: `PlayerHealthText`
4. Đặt vị trí phù hợp
5. Trong Text component:
   - **Text**: "100 / 100" (tạm thời)
   - **Font Size**: Tùy chỉnh
   - **Alignment**: Center

## Bước 4: Thêm Script HealthBar vào UI

1. Chọn Canvas hoặc tạo một GameObject trống làm container
2. Click `Add Component`
3. Tìm và thêm script `HealthBar`

## Bước 5: Config HealthBar Script

1. Chọn GameObject có component `HealthBar`
2. Trong Inspector, config các field:

### Required Fields:
- **Health Slider**: Kéo thả `PlayerHealthSlider` vào đây
- **Fill Image**: Kéo thả `Fill Area > Fill` vào đây
- **Health Text TMP** (tùy chọn, khuyến nghị): Kéo thả `PlayerHealthText` (TextMeshPro) vào đây
- **Health Text** (tùy chọn, legacy): Kéo thả `PlayerHealthText` (Legacy Text) vào đây nếu không dùng TextMeshPro

### Optional Fields:
- **Player Health** (Single-player): Để trống (script sẽ tự tìm)
- **Network Player Health** (Multiplayer): Để trống (script sẽ tự tìm)
- **Full Health Color**: Màu khi đầy máu (ví dụ: xanh lá)
- **Low Health Color**: Màu khi máu thấp (ví dụ: đỏ)
- **Low Health Threshold**: Ngưỡng máu thấp (0.3 = 30%)

## Bước 6: Kiểm tra Player có Health Component

### Cho Single-Player:
1. Chọn GameObject Player
2. Kiểm tra có component `PlayerHealth` chưa
3. Nếu chưa có, click `Add Component` và thêm `PlayerHealth`

### Cho Multiplayer:
1. Chọn GameObject Player
2. Kiểm tra có component `NetworkPlayerHealth` chưa
3. Nếu chưa có, click `Add Component` và thêm `NetworkPlayerHealth`
4. Đảm bảo Player có `NetworkObject` component

## Bước 7: Test

1. Chạy game
2. Health bar sẽ tự động hiển thị HP của player
3. Khi bị enemy tấn công, HP sẽ giảm và health bar sẽ cập nhật
4. Màu sắc sẽ thay đổi khi HP thấp (dựa vào Low Health Threshold)

## Lưu ý:

- **Single-Player**: Dùng `PlayerHealth` component
- **Multiplayer**: Dùng `NetworkPlayerHealth` component
- Script `HealthBar` sẽ tự động tìm và sử dụng component phù hợp
- Nếu có cả 2 components, sẽ ưu tiên `NetworkPlayerHealth`
- HP được lấy từ `PlayerStats.maxHealth` thông qua `PlayerController.stats`

## Troubleshooting:

### Health bar không hiển thị:
- Kiểm tra Canvas có được enable không
- Kiểm tra Slider có được gán vào HealthBar script không
- Kiểm tra Player có Health component không

### HP không cập nhật:
- Kiểm tra Player có đúng Health component (PlayerHealth hoặc NetworkPlayerHealth)
- Kiểm tra Health component có được gán vào HealthBar script không (hoặc để trống để tự tìm)
- Kiểm tra Console có lỗi gì không

### Màu sắc không thay đổi:
- Kiểm tra Fill Image có được gán vào HealthBar script không
- Kiểm tra Low Health Threshold có được config đúng không
