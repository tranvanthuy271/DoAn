# HƯỚNG DẪN CONFIG HỆ THỐNG CHAT & BẠN BÈ TRONG UNITY

> **Stack:** Unity 2D · TextMeshPro · Netcode for GameObjects · REST API (SignalR WebSocket)  
> **Scene mục tiêu:** GameScene (scene gameplay chính)

---

## MỤC LỤC

1. [Tổng quan Layout](#1-tổng-quan-layout)
2. [Bước 1 – Tạo Prefab tự động](#2-bước-1--tạo-prefab-tự-động)
3. [Bước 2 – Thêm ChatManager & FriendManager vào Scene](#3-bước-2--thêm-chatmanager--friendmanager-vào-scene)
4. [Bước 3 – Đặt ChatPanel vào Canvas HUD](#4-bước-3--đặt-chatpanel-vào-canvas-hud)
5. [Bước 4 – Đặt FriendListPanel vào Canvas HUD](#5-bước-4--đặt-friendlistpanel-vào-canvas-hud)
6. [Bước 5 – Thêm nút HUD (Chat + Bạn bè)](#6-bước-5--thêm-nút-hud-chat--bạn-bè)
7. [Bước 6 – Thêm ProximityChatBubble vào Player Prefab](#7-bước-6--thêm-proximitychatbubble-vào-player-prefab)
8. [Bước 7 – Gán MessageEntry Prefab cho ChatPanel](#8-bước-7--gán-messageentry-prefab-cho-chatpanel)
9. [Bước 8 – Cách ChannelIconButton hoạt động](#9-bước-8--cách-channeliconbutton-hoạt-động)
10. [Bước 9 – Chạy migration SQL](#10-bước-9--chạy-migration-sql)
11. [Kiểm tra chạy thử](#11-kiểm-tra-chạy-thử)
12. [Sơ đồ Hierarchy](#12-sơ-đồ-hierarchy)

---

## 1. Tổng quan Layout

```
GameScene
├── Canvas (Screen Space - Overlay)
│   ├── HUDPanel
│   │   ├── ChatHudButton        ← nút mở/đóng Chat, hiện badge tin chưa đọc
│   │   └── FriendHudButton      ← nút mở/đóng Bạn bè, hiện badge lời mời
│   ├── ChatPanel                ← panel chat chính (ẩn mặc định)
│   └── FriendListPanel          ← panel bạn bè (ẩn mặc định)
└── ChatManager (DontDestroyOnLoad)
    ├── ChatManager.cs
    └── FriendManager.cs

Player Prefab
└── ProximityChatBubble.cs       ← bubble nổi trên đầu nhân vật
```

---

## 2. Bước 1 – Tạo Prefab tự động

1. Mở Unity Editor
2. Trên thanh menu chọn **GameTools → Chat → Create Chat Prefabs**
3. Đợi vài giây → sẽ thấy log `✓ Đã tạo tất cả prefab` trong Console
4. Kiểm tra thư mục `Assets/Resources/Prefabs/Chat/`:

| Prefab | Mô tả |
|--------|-------|
| `ChatMessageEntry.prefab` | Một dòng tin nhắn trong ScrollView |
| `ChatPanel.prefab` | Panel chat chính (500×300 px) |
| `FriendListPanel.prefab` | Panel bạn bè (360×450 px) |
| `ChatManager.prefab` | Singleton quản lý chat + bạn bè |
| `ChatHudButton.prefab` | Nút HUD mở chat (có badge) |
| `FriendHudButton.prefab` | Nút HUD mở bạn bè (có badge) |

> **Lưu ý:** Sau khi tạo, bạn có thể thay đổi hình ảnh/màu sắc tùy ý — các script chỉ cần các tên GameObject con đúng.

---

## 3. Bước 2 – Thêm ChatManager & FriendManager vào Scene

**Cách 1 – Dùng prefab:**
1. Kéo `ChatManager.prefab` từ `Assets/Resources/Prefabs/Chat/` vào **gốc Hierarchy** (không phải vào Canvas)
2. Prefab đã có sẵn `ChatManager` + `FriendManager` script

**Cách 2 – Tạo tay:**
1. Tạo Empty GameObject đặt tên `ChatManager`
2. `Add Component` → `ChatManager`
3. `Add Component` → `FriendManager`

> **ChatManager** tự kết nối SignalR Hub sau khi player login xong (lắng nghe `GameManager.OnPlayerDataSet`). Không cần gọi gì thêm.

---

## 4. Bước 3 – Đặt ChatPanel vào Canvas HUD

1. Kéo `ChatPanel.prefab` vào **Canvas** trong scene
2. Chỉnh **RectTransform**:
   - Anchor: `Bottom-Left`
   - Pos X: `260`, Pos Y: `160`
   - Width: `500`, Height: `300`
3. Trong Inspector của `ChatPanel`, tìm component `ChatPanelUI`:
   - Tất cả trường đã được tự động gán từ prefab
   - Trường `messageEntryPrefab` → kéo `ChatMessageEntry.prefab` vào
4. **Tắt** ChatPanel lúc đầu: bỏ dấu tick ở góc trên Inspector

> **Tip:** Đặt ChatPanel ở góc dưới-trái màn hình, giống style MMORPG truyền thống.

---

## 5. Bước 4 – Đặt FriendListPanel vào Canvas HUD

1. Kéo `FriendListPanel.prefab` vào Canvas
2. Chỉnh **RectTransform**:
   - Anchor: `Bottom-Right`
   - Pos X: `-190`, Pos Y: `230`
   - Width: `360`, Height: `450`
3. **Tắt** FriendListPanel lúc đầu
4. Quay lại `ChatPanel` → tìm trường `Friend List Panel` trong `ChatPanelUI` → kéo `FriendListPanel` vào

---

## 6. Bước 5 – Thêm nút HUD (Chat + Bạn bè)

### 6.1 – Nút Chat (ChatHudButton)

1. Kéo `ChatHudButton.prefab` vào `HUDPanel` trong Canvas
2. Chỉnh vị trí: góc **dưới-trái** màn hình (ví dụ Pos X: `30`, Pos Y: `30`)
3. Trong Inspector, component `ChatToggleButton`:
   - Trường `Chat Panel` → kéo `ChatPanel` vào
   - Trường `Friend Panel` → kéo `FriendListPanel` vào *(tùy chọn)*
4. **Thay ảnh nền:** click vào `ChatHudButton` → component `Image` → thay Source Image bằng icon chat của bạn

```
ChatHudButton
├── Image (màu nền — thay bằng icon của bạn)
├── Button
├── ChatToggleButton.cs
├── IconLabel (TextMeshPro "Chat" — có thể xóa nếu dùng icon ảnh)
└── BadgeRoot (ẩn mặc định — hiện khi có tin chưa đọc)
    └── BadgeText (số tin)
```

### 6.2 – Nút Bạn bè (FriendHudButton)

1. Kéo `FriendHudButton.prefab` vào `HUDPanel`
2. Chỉnh vị trí cạnh nút Chat (Pos X: `80`, Pos Y: `30`)
3. Trong Inspector, component `FriendToggleButton`:
   - Trường `Friend Panel` → kéo `FriendListPanel` vào
4. **Thay ảnh:** thay Image → icon hình người/bạn bè của bạn

---

## 7. Bước 6 – Thêm ProximityChatBubble vào Player Prefab

> Bubble tin lân cận hiển thị phía trên đầu nhân vật — tự động ẩn sau 5 giây.

### Các bước thực hiện:

**Bước 7.1 – Mở Player Prefab**
1. Trong `Project` window, mở thư mục `Assets/Prefabs/Player/He/` (hoặc `Fusion/`)
2. Double-click prefab `Hoa.prefab` (hoặc bất kỳ nhân vật nào)
3. Unity sẽ mở **Prefab Edit Mode** (thanh breadcrumb phía trên)

**Bước 7.2 – Thêm Component**
1. Chọn **root GameObject** của prefab (tên trùng với tên prefab)
2. Trong Inspector → `Add Component`
3. Gõ **`ProximityChatBubble`** → chọn script

**Bước 7.3 – Cấu hình Component**
```
ProximityChatBubble
├── Offset:       (0, 2.2, 0)   ← bubble hiện trên đầu 2.2 unit
├── Canvas Scale: 0.01          ← tỉ lệ World Canvas
├── Canvas Size:  (340, 60)     ← kích thước vùng text
├── Font Size:    18
├── Text Color:   White
├── Bg Color:     (0,0,0, 0.6)  ← nền đen trong suốt
└── Display Time: 5             ← ẩn sau 5 giây
```

**Bước 7.4 – Lưu Prefab**
1. Nhấn `Ctrl+S` hoặc click **Save** trên thanh breadcrumb
2. Lặp lại cho **tất cả** các Player Prefab (He/Kim, He/Hoa, ... và Fusion/)

> **Lưu ý quan trọng:** `ProximityChatBubble` chỉ hiển thị bubble cho chính người chơi đó (kiểm tra `senderId == USER_ID`). Bubble cho người chơi khác cần thêm logic vào `NetworkPlayerDataSync` — xem mục bên dưới.

### 7.5 – Hiển thị bubble của người chơi khác (nâng cao)

Trong script `NetworkPlayerDataSync.cs`, thêm event handler khi nhận proximity message từ server:

```csharp
// Trong NetworkPlayerDataSync.cs – thêm vào Start():
void Start()
{
    // ... code hiện tại ...
    
    if (IsLocalPlayer && ChatManager.Instance != null)
        ChatManager.Instance.OnMessageReceived += OnChatMessage;
}

void OnDestroy()
{
    if (ChatManager.Instance != null)
        ChatManager.Instance.OnMessageReceived -= OnChatMessage;
}

// ServerRpc để broadcast proximity msg lên tất cả client
[ServerRpc(RequireOwnership = true)]
void ShowProximityBubbleServerRpc(string senderName, string message)
{
    ShowProximityBubbleClientRpc(senderName, message);
}

[ClientRpc]
void ShowProximityBubbleClientRpc(string senderName, string message)
{
    GetComponentInChildren<ProximityChatBubble>()?.ShowMessage(senderName, message);
}

private void OnChatMessage(ChatMessageDto msg)
{
    if (msg.GetChannel() != ChatChannel.Proximity) return;
    var myId = PlayerPrefs.GetInt("USER_ID", 0).ToString();
    if (msg.senderId == myId)
        ShowProximityBubbleServerRpc(msg.senderName, msg.message);
}
```

---

## 8. Bước 7 – Gán MessageEntry Prefab cho ChatPanel

1. Chọn `ChatPanel` trong Hierarchy
2. Tìm component `ChatPanelUI` trong Inspector
3. Trường **Message Entry Prefab** → kéo `ChatMessageEntry.prefab` vào
4. (Nếu bạn muốn tạo prefab entry đẹp hơn, chỉnh sửa `ChatMessageEntry` trong Project rồi gán lại)

---

## 9. Bước 8 – Cách ChannelIconButton hoạt động

### Sơ đồ hoạt động:

```
[Nhấn nút LC/TG/GT...]
        ↓
ChatChannelDropdownUI.Toggle()
        ↓
Hiển thị listbox dropdown
├── LC – Tin lân cận   (xanh dương)
├── TG – Tin thế giới  (vàng)
├── LO – Tin lớp       (xanh lá)
├── GT – Tin gia tộc   (tím)
├── N  – Tin nhóm      (cam)
└── R  – Tin riêng     (hồng)
        ↓
Chọn một kênh
        ↓
ChatPanelUI.OnChannelDropdownSelected(ch)
├── ChatManager.CurrentSendChannel = ch
├── Cập nhật icon button → hiện mã viết tắt (LC/TG...)
├── Cập nhật nhãn kênh (Lân cận/Thế giới...)
└── Tab tương ứng được highlight
```

### Màu sắc icon theo kênh:

| Kênh | Mã | Màu badge |
|------|----|-----------|
| Lân cận | LC | Xanh dương `#3399FF` |
| Thế giới | TG | Vàng `#FFCC33` |
| Lớp | LO | Xanh lá `#66CC66` |
| Gia tộc | GT | Tím `#CC66FF` |
| Nhóm | N | Cam `#FF9933` |
| Riêng | R | Hồng `#FF6699` |

> **Custom icon:** Để thay màu/sprite cho từng kênh, mở `ChatChannelDropdownUI` trong Inspector → sửa mảng `Channel Items`.

---

## 10. Bước 9 – Chạy migration SQL

Trước khi chạy server, thực thi file SQL để tạo bảng bạn bè:

```sql
-- Chạy file này trong MySQL/MariaDB
source /path/to/GameServerApi/sql/020_chat_friends.sql;
```

Hoặc nếu dùng EnsureCreated (mặc định), bảng sẽ tự tạo khi server khởi động.

---

## 11. Kiểm tra chạy thử

### Checklist:

- [ ] `ChatManager` prefab đã có trong scene (DontDestroyOnLoad)
- [ ] `ChatPanel` đã có trong Canvas, **tắt** lúc đầu
- [ ] `FriendListPanel` đã có trong Canvas, **tắt** lúc đầu
- [ ] `ChatHudButton` đã có trong HUDPanel, đã gán `ChatPanel` reference
- [ ] `FriendHudButton` đã có trong HUDPanel, đã gán `FriendListPanel` reference
- [ ] `ChatMessageEntry.prefab` đã gán vào trường `messageEntryPrefab` của `ChatPanelUI`
- [ ] `ProximityChatBubble` đã thêm vào tất cả Player Prefab
- [ ] Server đang chạy và `/chathub` SignalR endpoint hoạt động
- [ ] JWT token được lưu trong `PlayerPrefs["JWT_TOKEN"]` sau khi login

### Test flow:

1. Chạy game → đăng nhập → vào GameScene
2. Trong Console Unity sẽ thấy `[Chat] Đã kết nối ChatHub`
3. Nhấn nút **Chat** (ChatHudButton) → ChatPanel hiện ra
4. Gõ tin nhắn → nhấn Enter hoặc nút **Gửi**
5. Nhấn icon kênh **TG/LC** → dropdown hiển thị danh sách kênh
6. Chọn **Lân cận** → gõ tin → bubble hiện trên đầu nhân vật
7. Nhấn nút **Bạn bè** → FriendListPanel hiện → tìm người chơi → gửi lời mời

---

## 12. Sơ đồ Hierarchy

```
GameScene
│
├── [Canvas] (Screen Space Overlay, Sort Order 0)
│   │
│   ├── HUDPanel
│   │   ├── ChatHudButton          (ChatToggleButton.cs)
│   │   │   ├── Image (icon chat)
│   │   │   ├── IconLabel (TMP)
│   │   │   └── BadgeRoot
│   │   │       └── BadgeText (TMP)
│   │   │
│   │   └── FriendHudButton        (FriendToggleButton.cs)
│   │       ├── Image (icon friend)
│   │       ├── IconLabel (TMP)
│   │       └── BadgeRoot
│   │           └── BadgeText (TMP)
│   │
│   ├── ChatPanel                  (ChatPanelUI.cs) — ẩn mặc định
│   │   ├── Header
│   │   │   ├── TitleText "Tin nhắn"
│   │   │   └── CloseButton
│   │   ├── MessageScrollView
│   │   │   └── Viewport/Content   ← ChatMessageEntry spawn ở đây
│   │   ├── TabBar                 (ChatTabUI.cs)
│   │   │   ├── Tab_Chung
│   │   │   ├── Tab_Riêng
│   │   │   ├── Tab_GiaToc
│   │   │   ├── Tab_Nhom
│   │   │   └── Tab_Lop
│   │   ├── InputBar
│   │   │   ├── ChannelIconButton  ← icon LC/TG/... nhấn mở dropdown
│   │   │   ├── ChannelNameLabel   ← "Lân cận"
│   │   │   ├── ChatInputField
│   │   │   └── SendButton
│   │   └── ChannelDropdown        (ChatChannelDropdownUI.cs) — ẩn mặc định
│   │       ├── Row: LC Tin lân cận
│   │       ├── Row: TG Tin thế giới
│   │       ├── Row: LO Tin lớp
│   │       ├── Row: GT Tin gia tộc
│   │       ├── Row: N  Tin nhóm
│   │       └── Row: R  Tin riêng
│   │
│   └── FriendListPanel            (FriendListUI.cs) — ẩn mặc định
│       ├── Header / CloseButton
│       ├── SearchBar
│       │   ├── SearchInput
│       │   └── SearchButton
│       └── FriendScrollView
│           └── Viewport/Content   ← FriendRow entries
│
└── ChatManager (DontDestroyOnLoad)
    ├── ChatManager.cs             ← kết nối SignalR /chathub
    └── FriendManager.cs           ← REST API bạn bè

Player Prefab (Assets/Prefabs/Player/He/Hoa.prefab, ...)
└── ProximityChatBubble.cs         ← bubble World Space trên đầu nhân vật
```

---

## Câu hỏi thường gặp

**Q: Chat không kết nối được?**
> Kiểm tra `ServerAddressConfig` trong Inspector — `Api Root` phải trỏ đúng địa chỉ server. Đảm bảo server đã `app.MapHub<ChatHub>("/chathub")`.

**Q: Bubble không hiện?**
> Kiểm tra `ProximityChatBubble` đã add vào Player Prefab chưa. Kênh gửi phải là **Lân cận** và player phải đã `JoinMap(mapId)`.

**Q: Tin lân cận cần `JoinMap` khi nào?**
> Gọi `ChatManager.Instance.JoinMap(mapId.ToString())` trong sự kiện chuyển map (ví dụ trong `MapPortalTrigger` hoặc `MapTransitionButton`).

**Q: Tab "Riêng" mở nhưng không có target?**
> FriendListPanel tự động hiện. Chọn bạn bè → nhấn icon chat → OpenPrivateChat tự động chuyển tab và gán target.
