# Hướng Dẫn Hệ Thống Bạn Bè (Friend System)

## Tổng Quan

Hệ thống bạn bè bao gồm:
- **FriendListPanel** — Panel 3 tab: Bạn Bè / Kết Bạn Mới / Lời Mời
- **PlayerProfilePanel** — Panel xem thông tin nhân vật bạn bè
- **FriendManager** — Singleton xử lý REST API
- **FriendToggleButton** — Nút HUD để mở/đóng panel
- **Backend** — `GET /api/player/by-user/{userId}` endpoint mới

---

## 1. Tạo Prefabs (Unity Editor)

Mở menu **GameTools → Friends → Create Friend Prefabs**.

Script sẽ tự tạo 3 prefab trong `Assets/Resources/Prefabs/Chat/`:
- `FriendListPanel.prefab`
- `FriendRowEntry.prefab`
- `PlayerProfilePanel.prefab`

> Nếu muốn tự tạo tay, xem cấu trúc dưới đây.

---

## 2. Cấu Trúc FriendListPanel

```
FriendListPanel (FriendListUI)
├── Header
│   ├── TitleText (TMP)
│   └── CloseButton (Button)
├── TabBar (HLG)
│   ├── TabFriendsBtn (Button) ← Tab 0
│   ├── TabAddBtn (Button) ← Tab 1
│   └── TabPendingBtn (Button) ← Tab 2
│       └── TabPendingBadge (TMP — badge đỏ)
├── PanelFriends (active: true)
│   ├── EmptyFriendLabel (TMP)
│   └── FriendScrollView (ScrollRect)
│       └── Viewport → Content (VLG)
├── PanelAdd (active: false)
│   ├── SearchBar (HLG)
│   │   ├── SearchInput (TMP_InputField)
│   │   └── SearchButton (Button)
│   ├── SearchHintLabel (TMP)
│   └── SearchResultScrollView
│       └── Viewport → Content (VLG)
└── PanelPending (active: false)
    ├── EmptyPendingLabel (TMP)
    └── PendingScrollView
        └── Viewport → Content (VLG)
```

### Inspector FriendListUI — Gán các trường:

| Field | GameObject |
|---|---|
| Close Button | Header/CloseButton |
| Title Label | Header/TitleText |
| Tab Friends Btn | TabBar/TabFriendsBtn |
| Tab Add Btn | TabBar/TabAddBtn |
| Tab Pending Btn | TabBar/TabPendingBtn |
| Tab Pending Badge | TabBar/TabPendingBtn/TabPendingBadge |
| Panel Friends | PanelFriends |
| Panel Add | PanelAdd |
| Panel Pending | PanelPending |
| Friend List Content | PanelFriends/.../Content |
| Empty Friend Label | PanelFriends/EmptyFriendLabel |
| Search Input | PanelAdd/SearchBar/SearchInput |
| Search Button | PanelAdd/SearchBar/SearchButton |
| Search Result Content | PanelAdd/.../Content |
| Search Hint Label | PanelAdd/SearchHintLabel |
| Pending Content | PanelPending/.../Content |
| Empty Pending Label | PanelPending/EmptyPendingLabel |
| Friend Entry Prefab | FriendRowEntry.prefab |
| Search Result Entry Prefab | FriendRowEntry.prefab |
| Pending Entry Prefab | FriendRowEntry.prefab |
| Profile Panel | PlayerProfilePanel (trong scene) |

---

## 3. Cấu Trúc FriendRowEntry

```
FriendRowEntry (HLG, LayoutElement minHeight=44)
├── NameText (TMP, flexibleWidth=1)
├── ChatButton (Button, 34px) — chỉ dùng cho tab Bạn Bè
├── ProfileButton (Button, 34px) — chỉ dùng cho tab Bạn Bè
├── AcceptButton (Button, 34px) — chỉ dùng cho tab Lời Mời
├── AddButton (Button, 34px) — chỉ dùng cho tab Kết Bạn Mới
└── DeleteButton (Button, 34px) — Bạn Bè + Lời Mời
```

> `FriendListUI` tự ẩn/hiện các button theo context (BuildFriendRow / BuildPendingRow / BuildSearchRow).

---

## 4. Cấu Trúc PlayerProfilePanel

```
PlayerProfilePanel (PlayerProfilePanelUI)
├── Header
│   ├── NameLabel (TMP)
│   ├── ElementLabel (TMP)
│   ├── LevelLabel (TMP)
│   └── CloseButton (Button)
├── TabBar (HLG)
│   ├── TabEquipBtn
│   ├── TabSkillBtn
│   └── TabPotentialBtn
├── ContentArea
│   ├── PanelEquip (ScrollView → Content)
│   ├── PanelSkill (ScrollView → Content, inactive)
│   └── PanelPotential (ScrollView → Content, inactive)
└── LoadingOverlay (inactive)
```

---

## 5. Thêm vào Scene

1. Kéo `FriendListPanel.prefab` vào Canvas HUD.
2. **Đảm bảo GameObject FriendListPanel ở trạng thái inactive** (bỏ tick trong Inspector).
3. Kéo `PlayerProfilePanel.prefab` vào Canvas HUD, cũng để **inactive**.
4. Gán `PlayerProfilePanel` vào field **Profile Panel** của `FriendListUI`.
5. Đặt `FriendListPanel` ở vị trí giữa màn hình (Anchor Center, AnchoredPosition 0,0).

---

## 6. Gán FriendToggleButton

1. Tạo Button trong HUD Canvas.
2. Gắn script `FriendToggleButton` lên Button.
3. (Tuỳ chọn) Gán field **Friend Panel** → FriendListPanel trong scene.
4. (Tuỳ chọn) Gán **Badge Root** / **Badge Text** nếu muốn hiện số lời mời.

---

## 7. Luồng Kết Bạn

```
Player A tìm kiếm → nhấn "Kết Bạn"
    → POST /api/friends/request { targetUserId }
    
Player B thấy badge trên tab Lời Mời
    → Nhấn "✓" Accept
    → PUT /api/friends/{relationId}/accept

Cả hai thấy nhau trong tab Bạn Bè
    → Có thể chat riêng, xem thông tin
```

---

## 8. Chat Riêng

Nhấn nút 💬 trên FriendRowEntry:
- Mở `ChatPanelUI` (nếu đang đóng)
- Gọi `chatPanel.OpenPrivateChat(userId, username)`
- ChatPanel chuyển sang channel **Riêng** với target là người bạn đó

---

## 9. Xem Thông Tin Nhân Vật

Nhấn nút 👁 trên FriendRowEntry:
1. `FriendListUI.OpenProfile(userId, username)` được gọi
2. `PlayerProfilePanelUI.LoadProfile(userId, username)` fetch `GET /api/player/by-user/{userId}`
3. Panel hiển thị: tên, nguyên tố, level, trang bị, kỹ năng, tiềm năng của người đó

> API trả về dữ liệu **chỉ đọc** — không thể sửa của người khác.

---

## 10. Backend — Endpoint Mới

### GET /api/player/by-user/{userId}

Trả về thông tin nhân vật theo `userId` (PK bảng Users):

```json
{
  "player_id": 5,
  "user_id": 12,
  "character_name": "Kiếm Thủ",
  "element_type": "Metal",
  "gender": "male",
  "level": 30,
  "equipment": { "weapon": "...", "armor": "..." },
  "skills": [{ "skillId": 1, "level": 3 }],
  "potential_stats": { "attack": 5, "hp": 3, "mp": 0, "defense": 2, "gene": 0 }
}
```

Cần `Authorization: Bearer <token>`.

---

## 11. Input Blocking

Khi người chơi click vào ô **SearchInput** để tìm bạn:
- `InputManager.SetInputEnabled(false)` được gọi → chặn skill/di chuyển
- Khi click ra ngoài hoặc đóng panel → `SetInputEnabled(true)` được khôi phục

---

## 12. Kiểm Tra Nhanh

| Bước | Kết quả mong đợi |
|---|---|
| Mở game, nhấn nút bạn bè | FriendListPanel mở, hiện tab Bạn Bè |
| Nhấn lại nút | Panel đóng |
| Tab "Kết Bạn Mới" → gõ tên → Tìm | Kết quả xuất hiện với nút Kết Bạn |
| Nhấn Kết Bạn | Nút disabled, text "Đã gửi" |
| Đăng nhập tk khác, tab Lời Mời | Badge đỏ hiện số, thấy lời mời |
| Nhấn ✓ Accept | Cả hai tab Bạn Bè cập nhật |
| Nhấn 💬 | ChatPanel mở, gửi tin nhắn riêng được |
| Nhấn 👁 | PlayerProfilePanel mở với data đúng người |
| Nhấn ✕ Xóa bạn | Người đó biến khỏi danh sách |
