# HƯỚNG DẪN: HỆ THỐNG GHÉP ĐÁ (GEM FUSION)

## 1. TỔNG QUAN

Khi nhấn NPC Thợ Rèn → mở panel có **3 tab**:
- **Cường Hóa** — nâng cấp trang bị
- **Ghép Đá** — fusion stone (mô tả trong doc này)
- **Túi Đồ** — hiển thị inventory

---

## 2. LOGIC GHÉP ĐÁ

### Công thức giá trị đá
Mỗi cấp đá có giá trị tăng **×4** so với cấp trước:

| Cấp đá | Giá trị |
|--------|---------|
| Đá 1   | 1       |
| Đá 2   | 4       |
| Đá 3   | 16      |
| Đá N   | 4^(N-1) |

### Tính tỉ lệ thành công
1. Tính **tổng giá trị** = sum(value(đá[i]) cho mỗi đá trong 16 slot)
2. Tính **ngưỡng cần** = value(đá mục tiêu) = value(đá input) * 4
3. Tỉ lệ = `tổng giá trị / ngưỡng cần * 100%`
4. Giới hạn tỉ lệ trong **[0%, 140%]**

**Ví dụ — ghép Đá 1 thành Đá 2 (ngưỡng = 4):**

| Số viên Đá 1 | Tổng giá trị | Tỉ lệ |
|-------------|-------------|-------|
| 1 viên      | 1           | 25%   |
| 2 viên      | 2           | 50%   |
| 3 viên      | 3           | 75%   |
| 4 viên      | 4           | 100%  |
| 8 viên      | 8           | 140%  (capped, ≡ ghép thẳng lên Đá 3) |

> **Lưu ý:** Khi tỉ lệ **>= 140%**, đầu ra nâng lên **1 cấp bổ sung**.  
> Ví dụ: 8 Đá 1 → tỉ lệ 200% → nhưng cap 140% → kết quả là **Đá 3** (thay vì Đá 2).

### Điều kiện ghép
- **Tối thiểu 2 viên** (tỉ lệ ≥ 50%) mới cho ghép.
- Chỉ ghép các đá **cùng loại** (cùng item type "Đá" – type = 21 trong item_template).
- Slot hiển thị tối đa **16 đá** trong panel.

---

## 3. UI LAYOUT (theo ảnh)

```
┌────────────────────────────────────────────────────────┐
│  [ Cường Hóa ]  [ Ghép Đá ]  [ Túi Đồ ]              │
├────────────────────────────────────────────────────────┤
│  Đặt vào đá cần ghép                                   │
│  ┌──┬──┬──┬──┐          ──►   ┌─────────┐            │
│  │  │  │  │  │                │  Xem thử│            │
│  ├──┼──┼──┼──┤                └─────────┘            │
│  │  │  │  │  │                                        │
│  ├──┼──┼──┼──┤                                        │
│  │  │  │  │  │                                        │
│  ├──┼──┼──┼──┤                                        │
│  │  │  │  │  │                                        │
│  └──┴──┴──┴──┘                                        │
│  [Đá dưới cấp 12 ▲]  [ Tự chọn ]       [ Ghép ]     │
└────────────────────────────────────────────────────────┘
```

### Các thành phần UI cần tạo trong Unity

| GameObject              | Type                 | Ghi chú                              |
|------------------------|----------------------|--------------------------------------|
| `GemFusionPanel`       | Panel (Canvas)       | Root panel                           |
| `TabBar`               | HorizontalLayoutGroup| 3 btn: Cường Hóa / Ghép Đá / Túi Đồ|
| `GemSlotGrid`          | GridLayoutGroup 4×4  | 16 slot đặt đá vào                   |
| `ArrowImage`           | Image                | Mũi tên ➔ chỉ hướng kết quả        |
| `PreviewResultImage`   | Image                | Icon đá có thể ghép ra               |
| `PreviewSuccessText`   | TMP_Text             | "XX% thành công"                     |
| `BtnPreview`           | Button               | "Xem thử"                            |
| `BtnPickFromBag`       | Button               | Mở túi đồ lọc đá                    |
| `BtnFuse`              | Button               | "Ghép" — gửi lên host                |
| `ResultText`           | TMP_Text             | Hiển thị kết quả sau ghép            |

---

## 4. DATA MODEL

### item_template — Nhận diện đá
Đá ghép có `type = 21` trong bảng `item_template`.

```sql
-- Ví dụ: Đá cấp 1, 2, 3
SELECT id, name, type FROM item_template WHERE type = 21;
-- id=1  "Đá Nâng Cấp Cấp 1"  type=21
-- id=2  "Đá Nâng Cấp Cấp 2"  type=21
-- id=3  "Đá Nâng Cấp Cấp 3"  type=21
```

### Cấu hình quan hệ cấp đá
Thêm cột `upgradeTarget` vào `item_template` (hoặc dùng hardcode theo `id`):
```
Đá 1 (id=1) → ghép ra Đá 2 (id=2)
Đá 2 (id=2) → ghép ra Đá 3 (id=3)
```

---

## 5. API SERVER

### `POST /api/player/{playerId}/gem/fuse`

**Request body:**
```json
{
  "gemSlots": [1, 1, 1, 1],
  "sourceItemTemplateId": 1
}
```
- `gemSlots`: mảng `itemTemplateId` từng slot (tối đa 16, không kể slot trống)
- `sourceItemTemplateId`: loại đá muốn ghép

**Server xử lý:**
1. Xác thực JWT (kiểm tra player_id)
2. Kiểm tra player có đủ đá trong `inventory_json`
3. Tính tổng giá trị → tỉ lệ
4. Nếu `tỉ lệ < 50%`: trả về 400 `"Không đủ đá để ghép (tối thiểu 50%)"`
5. `Random.NextDouble() * 100 <= tỉ lệ` → SUCCESS → trừ đá, cộng đá kết quả
6. Nếu `tỉ lệ >= 140%` → cộng đá cấp N+2 (bỏ qua 1 cấp)
7. Trả về kết quả + inventory mới

**Response (success):**
```json
{
  "success": true,
  "resultItemTemplateId": 2,
  "resultItemName": "Đá Nâng Cấp Cấp 2",
  "successRate": 75.0,
  "inventory": [...]
}
```

**Response (fail):**
```json
{
  "success": false,
  "successRate": 75.0,
  "message": "Ghép thất bại"
}
```

---

## 6. CLIENT — SCRIPT `GemFusionPanel.cs`

### Các field Inspector cần gán

```csharp
[Header("Gem Slots")]
[SerializeField] private Image[] gemSlotImages;          // 16 images
[SerializeField] private Sprite emptyGemSlotSprite;

[Header("Result Preview")]
[SerializeField] private Image previewResultImage;
[SerializeField] private TMP_Text previewSuccessText;    // "75% thành công"

[Header("Buttons")]
[SerializeField] private Button btnPreview;
[SerializeField] private Button btnPickFromBag;
[SerializeField] private Button btnFuse;

[Header("Result")]
[SerializeField] private TMP_Text resultText;
[SerializeField] private GameObject resultPanel;
```

### Flow chính

```
1. Player nhấn [Tự chọn] / [BtnPickFromBag]
   → Mở InventoryPanel, lọc items có type=21
   → Player click đá → AddGemToSlot(itemTemplateId)

2. Sau khi thêm đá:
   → CalculatePreview() → hiển thị icon đá kết quả + tỉ lệ %

3. Player nhấn [Xem thử]
   → Chỉ hiển thị preview mà không ghép

4. Player nhấn [Ghép]
   → Gửi ServerRpc hoặc gọi API trực tiếp (host-validated)
   → Server trả về kết quả
   → Client hiển thị animation + kết quả

5. Refresh inventory sau ghép
```

### Hàm tính preview (client-side)

```csharp
private void CalculatePreview()
{
    int totalValue = 0;
    foreach (var slotId in gemSlots)
    {
        if (slotId > 0)
            totalValue += GetGemValue(slotId); // 4^(level-1)
    }

    if (sourceGemTemplateId <= 0 || totalValue == 0)
    {
        previewSuccessText.text = "";
        return;
    }

    int targetValue = GetGemValue(sourceGemTemplateId) * 4; // cấp kế tiếp
    float successRate = Mathf.Clamp((float)totalValue / targetValue * 100f, 0f, 140f);

    // Xác định đá kết quả
    int resultTemplateId = GetNextGemTemplateId(sourceGemTemplateId);
    if (successRate >= 140f)
        resultTemplateId = GetNextGemTemplateId(resultTemplateId); // lên 2 cấp

    previewSuccessText.text = $"{successRate:F0}% thành công";
    previewResultImage.sprite = GetGemSprite(resultTemplateId);
}

private int GetGemValue(int templateId)
{
    // Dựa vào mapping cấp đá → giá trị 4^(level-1)
    // Ví dụ: id=1 → cấp 1 → value=1, id=2 → cấp 2 → value=4
    int level = GetGemLevel(templateId);
    return (int)Mathf.Pow(4, level - 1);
}
```

---

## 7. NETWORK FLOW (host-validated)

```
Client (người chơi)
  │
  │  [Nhấn Ghép] → GemFuseServerRpc(playerId, sourceTemplateId, gemSlots[])
  ▼
Host (server)
  ├─ Xác thực player có đủ đá trong DB?
  ├─ Tính tỉ lệ → random kết quả
  ├─ Cập nhật inventory_json trong DB
  ├─ Cập nhật networkInventoryData (NetworkVariable)
  └─ GemFuseResultClientRpc(success, resultItemTemplateId)
  │
  ▼
Client (nhận kết quả)
  └─ Hiển thị animation + popup kết quả
```

---

## 8. SETUP TRONG UNITY

### Bước 1: Tạo UI

1. Trong Canvas → tạo `GemFusionPanel` (ẩn mặc định)
2. Thêm `TabBar` với 3 Button: "Cường Hóa", "Ghép Đá", "Túi Đồ"
3. GridLayoutGroup 4×4 gồm 16 `GemSlot` Image
4. Thêm `ArrowImage`, `PreviewResultImage`, `PreviewSuccessText`
5. Thêm 3 Button: `BtnPickFromBag`, `BtnPreview`, `BtnFuse`

### Bước 2: Gắn Script

- Gắn `GemFusionPanel.cs` vào root panel
- Kéo các references vào Inspector

### Bước 3: NPC Thợ Rèn mở panel

```csharp
// Trong NpcMenuUI.cs hoặc BlacksmithPanel.cs
public void OpenBlacksmithPanel()
{
    tabCuongHoa.SetActive(true);   // mặc định tab đầu
    tabGhepDa.SetActive(false);
    tabTuiDo.SetActive(false);
}
```

### Bước 4: API Server

Thêm endpoint `POST /api/player/{playerId}/gem/fuse` vào `PlayerController.cs` (xem mục 5 ở trên).

### Bước 5: Kiểm tra item_template

```sql
-- Đảm bảo các đá cấp 1-N có type=21 và isXepChong='True'
SELECT id, name, type, isXepChong FROM item_template WHERE type = 21;
```

---

## 9. LƯU Ý QUAN TRỌNG

- **Client chỉ hiển thị preview** — kết quả thực do **host quyết định** (random server-side)  
- **Không dùng client-side random** để tránh gian lận  
- **`isXepChong = 'True'`** cho đá để inventory stacking hoạt động đúng  
- Sau khi ghép, gọi `networkInventory.networkInventoryData` update → tự sync về tất cả client qua NGO NetworkVariable  
