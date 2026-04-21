# Hướng Dẫn Cấu Hình Wave HUD trong Unity

Wave HUD hiển thị **số vòng hiện tại** và **thời gian còn lại** của mỗi wave trên màn hình client.  
Dữ liệu được đồng bộ từ server qua `NetworkVariable` của `WaveDungeonRuntime`.

---

## 1. Kiến Trúc

```
WaveDungeonRuntime (NetworkBehaviour — server-authoritative)
  └── NetworkVariable<int> _currentRound     → CurrentRound   (public property)
  └── NetworkVariable<int> _remainingSeconds → RemainingSeconds (public property)
  └── NetworkVariable<int> _maxRounds        → MaxRounds      (public property)
  └── ShowTimeUpClientRpc()         → GlobalNotificationUI.Show("Hết Thời Gian")
  └── ShowWaveCompleteClientRpc()   → GlobalNotificationUI.Show("Vòng Hoàn Thành")

WaveHUD (MonoBehaviour — client only)
  └── Tự tìm WaveDungeonRuntime mỗi 0.5s
  └── Cập nhật roundText + timerText khi giá trị thay đổi
  └── Tô đỏ timer khi < 30 giây còn lại
```

---

## 2. Tự Động (không cần thêm gì)

`WaveDungeonRuntime` đã tự tạo HUD panel góc trên-trái khi client vào dungeon scene.  
Nếu bạn chỉ muốn HUD cơ bản này, **không cần làm thêm gì**.

---

## 3. Cấu Hình WaveHUD Tùy Chỉnh (nếu muốn HUD riêng)

### Bước 1 — Tạo UI trong scene GameScene (hoặc DungeonWaveScene)

1. Trong Hierarchy, tạo `GameObject` → đặt tên `WaveHUD`
2. Thêm `Panel` con chứa 2 `TextMeshPro - Text (UI)`:
   - `RoundText` — hiển thị `"Vòng 1 / 20"`
   - `TimerText` — hiển thị `"04:55"` (đổi màu đỏ khi < 30s)

Ví dụ cấu trúc:
```
Canvas (Screen Space Overlay)
└── WaveHUDPanel (Image, màu nền nửa trong suốt)
    ├── RoundText  (TMP_Text, anchor top-left)
    └── TimerText  (TMP_Text, anchor top-left, bên dưới RoundText)
```

### Bước 2 — Gắn Component

1. Chọn `WaveHUDPanel` trong Hierarchy
2. **Add Component → WaveHUD**
3. Trong Inspector:

| Field | Giá trị |
|-------|---------|
| **Round Text** | Kéo `RoundText` TMP_Text vào |
| **Timer Text** | Kéo `TimerText` TMP_Text vào |
| **Hud Root** | Kéo `WaveHUDPanel` vào (để ẩn/hiện panel tự động) |
| **Poll Interval** | `0.5` (giây — bao lâu tìm WaveDungeonRuntime một lần) |

> **Lưu ý:** Nếu bỏ trống `Hud Root`, script sẽ ẩn/hiện từng label riêng lẻ.

### Bước 3 — Đặt WaveHUD trong scene đúng

| Phương án | Ưu điểm |
|-----------|---------|
| Đặt trong **GameScene** (scene chính) | Luôn tồn tại, tự ẩn khi không ở dungeon |
| Đặt trong **DungeonWaveScene** | Chỉ hiện khi vào dungeon, không cần poll |

**Khuyến nghị**: Đặt trong `GameScene` để dùng chung HUD cho tất cả dungeon loại wave.

---

## 4. Thông Báo Hết Thời Gian

Khi timer về 0, server tự động:
1. Gửi `ShowTimeUpClientRpc()` → **GlobalNotificationUI** hiện popup "Hết Thời Gian"
2. Sau 5 giây countdown → tự động đẩy player về `map_id = 0`

Bạn **không cần cấu hình gì thêm** cho tính năng này.  
Chỉ cần đảm bảo `GlobalNotificationPanel` prefab tồn tại trong `Assets/Resources/Prefabs/UI/`.

---

## 5. Thông Báo Hoàn Thành Vòng

Khi boss bị tiêu diệt, server gửi `ShowWaveCompleteClientRpc()`:
- Popup hiện trong 2.5 giây: **"Hoàn thành vòng X! Chuẩn bị vòng Y..."**
- Sau 3 giây, wave mới bắt đầu tự động

---

## 6. Kiểm Tra

Sau khi cấu hình xong:

1. Vào Play Mode, kết nối client vào server
2. Vào dungeon wave (map_id = 110)
3. Kiểm tra:
   - [ ] Panel WaveHUD hiện ra sau khi vào dungeon
   - [ ] `RoundText` hiển thị `"Vòng 1 / 20"`
   - [ ] `TimerText` đếm ngược từ `05:00`
   - [ ] TimerText đổi màu **đỏ** khi còn < 30 giây
   - [ ] Sau khi tiêu diệt tất cả quái thường → boss spawn, popup "Boss vòng 1 đã xuất hiện"
   - [ ] Sau khi kill boss → popup "Hoàn thành vòng 1! Chuẩn bị vòng 2..."
   - [ ] Wave 2 bắt đầu sau 3 giây
   - [ ] Khi hết time → popup "Hết Thời Gian" → về map chính sau 5 giây

---

## 7. Tham Khảo

- `Client/Assets/Scripts/UI/HUD/WaveHUD.cs` — Script WaveHUD
- `Client/Assets/Scripts/Dungeon/Runtime/WaveDungeonRuntime.cs` — Server runtime
- `Client/Assets/Scripts/UI/GlobalNotificationUI.cs` — Popup thông báo
- `HUONG_DAN_CONFIG_DUNGEON_WAVE_ENEMY.md` — Cấu hình enemy cho wave dungeon
