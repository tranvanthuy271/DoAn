# Hướng Dẫn Tạo Mũi Tên Chỉ Chọn Item (Selection Arrow)

Khi người chơi **click** hoặc **đi ngang qua** item drop, một mũi tên sẽ hiện ra phía trên item để biết đã chọn.

---

## Bước 1 — Mở prefab ItemPickup

1. **Project** window → `Assets/Prefabs/` → tìm prefab `ItemPickup`
2. **Double-click** để mở Prefab Mode (isolated)

---

## Bước 2 — Tạo child object mũi tên

Trong **Hierarchy** của Prefab Mode:

1. Right-click vào **ItemPickup** → **Create Empty** → đặt tên `SelectionArrow`
2. Chọn `SelectionArrow` → thiết lập **Transform**:
   - Position: `X = 0`, `Y = 0.8`, `Z = 0` *(trên đầu item)*
   - Scale: `X = 0.4`, `Y = 0.4`, `Z = 1`

---

## Bước 3 — Thêm sprite mũi tên

### Cách A — Dùng sprite có sẵn (khuyến nghị)

1. Chọn `SelectionArrow` → **Add Component** → **Sprite Renderer**
2. Kéo một sprite hình mũi tên (↓ hoặc ▼) từ Project vào ô **Sprite**
3. Có thể dùng sprite nằm trong `Assets/Art/` hoặc tải từ Unity Asset Store

### Cách B — Dùng Text (không cần ảnh)

1. Chọn `SelectionArrow` → **Add Component** → **TextMeshPro - Text (UI)**
   > Nếu dùng Text cần thêm Canvas riêng → xem mục bên dưới
2. Hoặc **Add Component** → tìm `TextMesh` (3D Text) → nhập `▼`
   - Font Size: `3`
   - Color: Vàng `(255, 235, 0, 255)`
   - Alignment: Center

---

## Bước 4 — Thêm Animation bob lên xuống (tùy chọn)

Để mũi tên nhấp nhô liên tục:

1. Chọn `SelectionArrow`
2. **Window** → **Animation** → **Animation** → **Create** → lưu file `SelectionArrowBob.anim`
3. Nhấn **Record** → di chuyển `SelectionArrow.transform.localPosition.y`:
   - Frame 0: Y = 0.8
   - Frame 15: Y = 1.0
   - Frame 30: Y = 0.8
4. Trong **Animator** window → đặt clip loop: chọn clip → Inspector → tích **Loop Time**

---

## Bước 5 — Gán vào script ItemPickup

1. Chọn **root GameObject** `ItemPickup` trong Prefab Mode
2. Trong Inspector, tìm component **Item Pickup**
3. Ô **Selection Indicator** → kéo `SelectionArrow` object từ Hierarchy vào

   ```
   [Item Pickup (Script)]
   ▾ Selection Effect
     Selection Indicator  [SelectionArrow]  ← kéo vào đây
   ```

---

## Bước 6 — Lưu và kiểm tra

1. **Ctrl+S** để lưu prefab
2. Quay về Play Mode → đứng gần item hoặc **click vào item** → mũi tên xuất hiện 3 giây

---

## Kết quả mong đợi

| Hành động | Hiệu ứng |
|-----------|----------|
| Click chuột vào item | Mũi tên hiện 3 giây |
| Đi ngang qua item | Mũi tên hiện 3 giây |
| Item được nhặt | Mũi tên ẩn ngay |

---

## Lưu ý kỹ thuật

- `selectionIndicator.SetActive(false)` khi spawn → mũi tên ẩn mặc định
- `ShowSelectionIndicator()` trong code sẽ `SetActive(true)` và auto-hide sau 3 giây
- Nếu item bị despawn (nhặt rồi), `HideSelectionIndicator()` gọi ngay trong `DespawnItemClientRpc`

---

## Cấu hình Physics (quan trọng)

Để item **không đẩy player** khi đi qua:
- Code đã xử lý tự động qua `OnCollisionEnter2D` → `Physics2D.IgnoreCollision`
- Nếu vẫn bị đẩy nhẹ: **Edit → Project Settings → Physics 2D → Layer Collision Matrix**
  - Tạo Layer mới tên `Item` (Layer 8 hoặc layer còn trống)
  - Bỏ tích ô giao giữa `Item` × `Player`
  - Gán item prefab: Inspector → Layer → `Item`



