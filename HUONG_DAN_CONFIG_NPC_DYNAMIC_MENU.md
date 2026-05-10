# Hướng Dẫn Cấu Hình NPC Dynamic Menu (Unity)

## Tổng quan luồng

```
Click NPC → InteractServerRpc (server)
          → NpcMenuConfig.GetMenuItems(npcId, npcType)
          → OpenMenuClientRpc → NpcDynamicMenuUI.Open(npcData)
          → Người chơi chọn item → SelectMenuItemServerRpc(index)
          → NpcAction.Execute(actionType, ...) hoặc FetchShopAndSend
          → ShowActionResultClientRpc → GlobalNotificationUI.Show(msg)
```

---

## 1. Tạo Prefab `NpcDynamicMenuPanel`

### Vị trí lưu
```
Assets/Resources/Prefabs/UI/NPC/NpcDynamicMenuPanel.prefab
Assets/Resources/Prefabs/UI/NPC/NpcMenuItemRow.prefab
```

### Cấu trúc Hierarchy

```
Canvas (scene gốc)
└── NpcDynamicMenuPanel          [Image, NpcDynamicMenuUI component]
    ├── TitleText                [TextMeshProUGUI]
    ├── Separator                [Image, màu nâu đậm, height=2]
    ├── ScrollView               [ScrollRect, horizontal=false]
    │   └── Viewport             [Mask, Image]
    │       └── Content          [VerticalLayoutGroup, ContentSizeFitter]
    │           └── (rows spawn here at runtime)
    └── BtnClose                 [Button + TextMeshProUGUI "Cáo từ"]
```

### Bước tạo trong Unity Editor

1. **GameObject > UI > Panel** → đặt tên `NpcDynamicMenuPanel`
   - Width: 280, Height: 400 (hoặc tự điều chỉnh)
   - Image: màu `#5C3A1E` (nâu gỗ) hoặc dùng sprite `bg_wood`
   - Anchor: giữa màn hình

2. **Add component** `NpcDynamicMenuUI` vào `NpcDynamicMenuPanel`

3. **Tạo con `TitleText`** (TextMeshProUGUI)
   - Nội dung mẫu: `Xin chào Anh Hùng`
   - Font size: 18, Bold, màu trắng hoặc vàng nhạt
   - Padding top: 10

4. **Tạo con `Separator`** (Image)
   - Height: 2, màu `#3B2409`

5. **Tạo `ScrollView`** (GameObject > UI > Scroll View)
   - Xoá mặc định `Scrollbar Horizontal`
   - `ScrollRect.horizontal = false`
   - `Viewport > Mask` component: giữ mặc định

6. **Trong `Content` (child của Viewport)**:
   - Add `VerticalLayoutGroup`:
     - Spacing: 4
     - Padding: Left=8, Right=8, Top=4, Bottom=4
     - ChildControlWidth: ✓, ChildForceExpandWidth: ✓
   - Add `ContentSizeFitter`:
     - Vertical Fit: **Preferred Size**

7. **Tạo `BtnClose`** (Button - TextMeshPro)
   - Label: "Cáo từ"
   - Màu nền: `#8B4513`
   - Height: 36, margin bottom: 8

8. **Đặt inactive**: Object `NpcDynamicMenuPanel` → bỏ tick Active (script sẽ gọi `SetActive(true)` khi mở)

---

## 2. Tạo Prefab `NpcMenuItemRow`

### Cấu trúc

```
NpcMenuItemRow               [HorizontalLayoutGroup, NpcMenuItemRow component, Button]
├── IconImage                [Image, 28x28px, màu trắng]
└── LabelText                [TextMeshProUGUI, flexible width]
```

### Bước tạo

1. **Tạo Empty GameObject** → đặt tên `NpcMenuItemRow`
   - Height: 40
   - Add `HorizontalLayoutGroup`:
     - Spacing: 8
     - Padding: Left=8, Right=8
     - ChildControlHeight: ✓
   - Add `Button` (lấy tất cả làm clickable)
     - Transition: Color Tint
     - Normal Color: `#00000000` (trong suốt), Highlighted: `#FFFFFF40`

2. **Add component** `NpcMenuItemRow` vào root

3. **Tạo con `IconImage`** (Image)
   - Width: 28, Height: 28
   - Sprite: `icon_chat_bubble` hoặc bất kỳ icon nào phù hợp
   - Preserve Aspect: ✓

4. **Tạo con `LabelText`** (TextMeshProUGUI)
   - LayoutElement: Flexible Width = 1
   - Font size: 15, màu trắng
   - Alignment: Middle Left

---

## 3. Wiring trong Inspector

### `NpcDynamicMenuUI` component

| Field | Gán |
|-------|-----|
| `mainPanel` | `NpcDynamicMenuPanel` (self) |
| `titleText` | `TitleText` (TMP) |
| `menuListContent` | `Content` transform bên trong ScrollView |
| `menuItemRowPrefab` | `NpcMenuItemRow.prefab` |
| `btnClose` | `BtnClose` (Button) |

### `NpcMenuItemRow` component

| Field | Gán |
|-------|-----|
| `labelText` | `LabelText` (TMP) |
| `iconImage` | `IconImage` (Image) |
| `rowButton` | Button trên root |

---

## 4. Thêm vào Scene

1. Kéo prefab `NpcDynamicMenuPanel` vào **Canvas** trong scene (scene Game)
2. Để **inactive** (không tick Active)
3. NpcDynamicMenuUI.GetOrFind() sẽ tự tìm thấy nó trong scene

---

## 5. Config NPC mới

Khi thêm NPC mới vào DB (`npc_config`), thêm case tương ứng vào:
```
Client/Assets/Scripts/NPC/NpcMenuConfig.cs → GetByNpcId(int npcId)
```

Ví dụ:
```csharp
case 20:   // NPC mới: "Bộ Hành Giả" (exchange NPC)
    return "Đổi trang bị:open_shop;" +
           "Tẩy tiềm năng:reset_potential;" +
           "Cáo từ:close";
```

---

## 6. Thêm action_type mới

Nếu cần action_type mới (ví dụ: `open_quest`, `teleport`):

**Bước 1:** Thêm case vào `NpcInteraction.SelectMenuItemServerRpc`:
```csharp
case "open_quest":
    ExecuteMenuActionClientRpc("open_quest", TargetClient(clientId));
    break;
```

**Bước 2:** Thêm case vào `NpcInteraction.ExecuteMenuActionClientRpc`:
```csharp
case "open_quest":
    QuestUI.GetOrFind()?.Open(lastData);
    break;
```

**Nếu action cần gọi API:** Thêm vào `NpcAction.Execute` + `NpcActionController` (API).

---

## 7. Chi phí các chức năng NPC (thay đổi trong `NpcActionController.cs`)

| Action | Chi phí mặc định |
|--------|------------------|
| Tẩy tiềm năng | 250,000 bạc |
| Tẩy kỹ năng | 250,000 bạc |
| Học bí kíp | 100,000 bạc + 1 điểm kỹ năng |
| Đổi bí kíp | 500,000 bạc |
| Đổi bùa nổ | 300,000 bạc |
| Khoá/mở cấp | 250,000 bạc |

Chỉnh giá trong các hằng số `const int Cost*` đầu file `NpcActionController.cs`:
```csharp
private const int CostResetPotential = 250_000;  // ← sửa ở đây
```

---

## 8. Test nhanh

1. Chạy Unity Editor ở mode Host
2. Click NPC npc_id=8 (Tiên Dược)
3. Menu hiện: `Mua tiên dược | Tẩy tiềm năng | Tẩy kỹ năng | Học bí kíp | Đổi bí kíp | Đổi bùa nổ | Khoá cấp | Cáo từ`
4. Chọn "Tẩy tiềm năng" → thông báo "Không đủ bạc. Cần 250,000 bạc." (nếu silver=0)
5. Chọn "Mua tiên dược" → shop mở với danh sách items của npc_id=8
