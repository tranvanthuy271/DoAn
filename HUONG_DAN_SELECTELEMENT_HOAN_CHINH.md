# 📖 HƯỚNG DẪN HOÀN CHỈNH SCENE SELECTELEMENT

## 🎯 Tổng quan

Scene `SelectElement` cho phép người chơi:
- ✅ **Chọn nhân vật từ 9 button** (mỗi button = 1 nhân vật cụ thể: hệ + giới tính)
  - Metal - Nam
  - Metal - Nữ
  - Wood - Nam
  - Wood - Nữ
  - Water - Nam
  - Water - Nữ
  - Fire - Nam
  - Fire - Nữ
  - Earth - Nam (chỉ có Nam)
- ✅ **Nhập tên nhân vật** (3-20 ký tự)
- ✅ **Xem preview nhân vật** khi chọn button
- ✅ **Button "Về"** quay lại scene Login
- ✅ **Button "Go"** vào MainMenu và load toàn bộ dữ liệu từ server

---

## 📋 1. THAY ĐỔI DATABASE

### 1.1. Thêm cột `character_name` vào bảng `player_data`

Mở **phpMyAdmin** → chọn database `gamedb` → bảng `player_data` → tab **SQL**:

```sql
ALTER TABLE `player_data` 
ADD COLUMN `character_name` VARCHAR(50) NOT NULL DEFAULT '' 
AFTER `gender`;
```

**Hoặc** chạy script SQL đầy đủ:

```sql
-- Thêm cột character_name vào bảng player_data
ALTER TABLE `player_data` 
ADD COLUMN `character_name` VARCHAR(50) NOT NULL DEFAULT '' 
COMMENT 'Tên nhân vật (3-20 ký tự)' 
AFTER `gender`;

-- Cập nhật các bản ghi hiện có (nếu có) - có thể để trống hoặc set giá trị mặc định
UPDATE `player_data` 
SET `character_name` = CONCAT('Player_', player_id) 
WHERE `character_name` IS NULL OR `character_name` = '';
```

### 1.2. Kiểm tra

Sau khi chạy SQL, kiểm tra:
- Vào tab **Cấu trúc** của bảng `player_data`
- Xác nhận có cột `character_name` với kiểu `varchar(50)`, NOT NULL

---

## 🎮 2. CẬP NHẬT UNITY SCENE: SELECTELEMENT

### 2.1. Thêm UI Elements vào Scene

Trong scene `SelectElement`, đảm bảo có các UI sau:

#### **A. 9 Button chọn nhân vật:**

1. **Tạo 9 Button** cho 9 nhân vật:
   - `Button_Metal_Male` - Metal - Nam
   - `Button_Metal_Female` - Metal - Nữ
   - `Button_Wood_Male` - Wood - Nam
   - `Button_Wood_Female` - Wood - Nữ
   - `Button_Water_Male` - Water - Nam
   - `Button_Water_Female` - Water - Nữ
   - `Button_Fire_Male` - Fire - Nam
   - `Button_Fire_Female` - Fire - Nữ
   - `Button_Earth_Male` - Earth - Nam (chỉ có Nam)
   
   **Layout gợi ý**: Sắp xếp theo grid 3x3 hoặc 2 cột (Nam bên trái, Nữ bên phải)

#### **B. Input Field nhập tên nhân vật:**

2. **TMP_InputField CharacterNameInput**:
   - Tạo **TMP_InputField** mới: `CharacterNameInput`
   - Placeholder: "Nhập tên nhân vật (3-20 ký tự)"
   - Character Limit: 20
   - Position: Bên dưới các button chọn nhân vật

#### **C. Button Navigation:**

3. **Button "Về" (BackButton)**:
   - Tạo Button mới: `BackButton`
   - Text: "Về" hoặc "← Quay lại"
   - Position: Góc trên trái hoặc dưới cùng

4. **Button "Xác nhận" (ConfirmButton)**:
   - Tạo Button mới: `ConfirmButton`
   - Text: "Xác nhận" hoặc "Tạo nhân vật"
   - Position: Bên cạnh button "Go"

5. **Button "Go" (GoButton)**:
   - Tạo Button mới: `GoButton`
   - Text: "Go" hoặc "Vào Game"
   - Position: Bên cạnh button "Xác nhận" hoặc dưới cùng
   - **Ban đầu disable** (chỉ enable khi đã tạo nhân vật thành công)

#### **D. Character Preview Image:**

6. **Image Component để hiển thị preview**:
   - Trong Canvas, tạo GameObject mới (hoặc dùng Image có sẵn)
   - Add Component → **Image**
   - Đặt vị trí muốn hiển thị preview (ví dụ: giữa màn hình, bên phải)
   - Đặt size phù hợp (ví dụ: Width=300, Height=300)
   - **Mục đích**: Hiển thị sprite preview của nhân vật được chọn

#### **E. Sprites cho từng nhân vật:**

7. **Chuẩn bị 9 Sprites** (hoặc lấy từ prefabs có sẵn):
   - `MetalMaleSprite` - Sprite nhân vật Metal Nam
   - `MetalFemaleSprite` - Sprite nhân vật Metal Nữ
   - `WoodMaleSprite` - Sprite nhân vật Wood Nam
   - `WoodFemaleSprite` - Sprite nhân vật Wood Nữ
   - `WaterMaleSprite` - Sprite nhân vật Water Nam
   - `WaterFemaleSprite` - Sprite nhân vật Water Nữ
   - `FireMaleSprite` - Sprite nhân vật Fire Nam
   - `FireFemaleSprite` - Sprite nhân vật Fire Nữ
   - `EarthMaleSprite` - Sprite nhân vật Earth Nam
   
   **Cách lấy sprite từ prefab có sẵn**:
   - Mở prefab trong Project window
   - Nếu prefab có Image component → lấy sprite từ Image
   - Nếu prefab có SpriteRenderer → lấy sprite từ SpriteRenderer
   - Hoặc import sprites trực tiếp vào project
   
   **Lưu ý**: 
   - Sprites nên có kích thước phù hợp (ví dụ: 300x300 pixels)
   - Có thể dùng cùng sprite cho cả Nam và Nữ nếu muốn

### 2.2. Cấu hình SelectElementController

1. **Chọn GameObject `SelectElementController`** trong Hierarchy

2. **Trong Inspector**, gán các UI vào các fields:

   - **Character Buttons** (Array size = 9):
     - **Element 0** (Metal - Nam):
       - `Button` → kéo `Button_Metal_Male` vào
       - `Element Type` → chọn "Metal"
       - `Gender` → chọn "Male"
       - `Preview Sprite` → kéo sprite `MetalMaleSprite` vào (từ Project window)
     
     - **Element 1** (Metal - Nữ):
       - `Button` → kéo `Button_Metal_Female` vào
       - `Element Type` → chọn "Metal"
       - `Gender` → chọn ""
       - `Preview Sprite` → kéo sprite `MetalFemaleSprite` vào
     
     - **Element 2** (Wood - Nam):
       - `Button` → kéo `Button_Wood_Male` vào
       - `Element Type` → chọn "Wood"
       - `Gender` → chọn "Male"
       - `Preview Sprite` → kéo sprite `WoodMaleSprite` vào
     
     - **Element 3** (Wood - Nữ):
       - `Button` → kéo `Button_Wood_Female` vào
       - `Element Type` → chọn "Wood"
       - `Gender` → chọn "Female"
       - `Preview Sprite` → kéo sprite `WoodFemaleSprite` vào
     
     - **Element 4** (Water - Nam):
       - `Button` → kéo `Button_Water_Male` vào
       - `Element Type` → chọn "Water"
       - `Gender` → chọn "Male"
       - `Preview Sprite` → kéo sprite `WaterMaleSprite` vào
     
     - **Element 5** (Water - Nữ):
       - `Button` → kéo `Button_Water_Female` vào
       - `Element Type` → chọn "Water"
       - `Gender` → chọn "Female"
       - `Preview Sprite` → kéo sprite `WaterFemaleSprite` vào
     
     - **Element 6** (Fire - Nam):
       - `Button` → kéo `Button_Fire_Male` vào
       - `Element Type` → chọn "Fire"
       - `Gender` → chọn "Male"
       - `Preview Sprite` → kéo sprite `FireMaleSprite` vào
     
     - **Element 7** (Fire - Nữ):
       - `Button` → kéo `Button_Fire_Female` vào
       - `Element Type` → chọn "Fire"
       - `Gender` → chọn "Female"
       - `Preview Sprite` → kéo sprite `FireFemaleSprite` vào
     
     - **Element 8** (Earth - Nam):
       - `Button` → kéo `Button_Earth_Male` vào
       - `Element Type` → chọn "Earth"
       - `Gender` → chọn "Male"
       - `Preview Sprite` → kéo sprite `EarthMaleSprite` vào

   - **UI References**:
     - `Character Name Input` → kéo TMP_InputField nhập tên vào
     - `Error Text` → kéo Text hiển thị lỗi vào
     - `Instruction Text` → kéo Text hướng dẫn vào
     - `Confirm Button` → kéo button Xác nhận vào
     - `Back Button` → kéo button Về vào
     - `Go Button` → kéo button Go vào

   - **Character Preview**:
     - `Preview Image` → kéo Image component (đã tạo ở bước 7) vào

3. **Gắn sự kiện cho InputField**:
   - Chọn `CharacterNameInput` trong Hierarchy
   - Trong Inspector → **On Value Changed (String)** → kéo `SelectElementController` vào
   - Chọn method: `SelectElementController.OnCharacterNameChanged()`

4. **Gắn sự kiện cho các Button**:
   - `ConfirmButton` → `OnConfirmButtonClicked()` (đã có sẵn)
   - `BackButton` → `OnBackButtonClicked()` (đã có sẵn)
   - `GoButton` → `OnGoButtonClicked()` (đã có sẵn)
   
   **Lưu ý**: Các button chọn nhân vật sẽ tự động được gán sự kiện trong code `InitializeCharacterButtons()`, không cần gán thủ công.

---

## 🔄 3. LUỒNG HOẠT ĐỘNG

### 3.1. Khi người chơi vào scene SelectElement:

1. **Ban đầu**:
   - Tất cả 9 button chọn nhân vật đều **enable**
   - InputField tên nhân vật **enable**
   - Button "Xác nhận" **enable**
   - Button "Go" **disable** (chưa tạo nhân vật)
   - Button "Về" **enable**
   - Text hướng dẫn: "Chọn nhân vật của bạn"
   - Không có preview nhân vật

2. **Khi chọn button nhân vật** (ví dụ: Metal - Nam):
   - Button được chọn được **highlight** (màu xanh)
   - **Sprite preview nhân vật tương ứng được hiển thị** trong `PreviewImage`
   - Text hướng dẫn: "Đã chọn: Metal - Nam"
   - `selectedElement` = "Metal", `selectedGender` = "Male"

3. **Khi chọn button nhân vật khác**:
   - Button cũ mất highlight, button mới được highlight
   - Preview image cập nhật sprite mới (không cần spawn/destroy)
   - Text hướng dẫn cập nhật theo lựa chọn mới

4. **Khi nhập tên nhân vật**:
   - InputField cho phép nhập 3-20 ký tự
   - Button "Go" được **enable** khi: đã chọn nhân vật + đã nhập tên (≥3 ký tự)

5. **Khi bấm "Xác nhận"**:
   - Validate: đã chọn nhân vật, đã nhập tên (3-20 ký tự)
   - Gọi API `/api/player/create` với `element_type`, `gender`, `character_name`
   - Nếu thành công:
     - Button "Go" được **enable**
     - Text hiển thị: "Tạo nhân vật thành công! Nhấn 'Go' để vào game." (màu xanh)
   - Nếu lỗi → hiển thị lỗi và enable lại các button

6. **Khi bấm "Go"**:
   - Gọi API `/api/player/{playerId}/data` để **load toàn bộ dữ liệu từ server**
   - Lưu vào `GameManager.Instance.SetPlayerData()`
   - Chuyển sang scene `MainMenu`

7. **Khi bấm "Về"**:
   - Quay lại scene `Login`

---

## 🔧 4. THAY ĐỔI CODE ĐÃ THỰC HIỆN

### 4.1. Server (GameServerApi)

#### **A. Model: PlayerData.cs**
- ✅ Đã thêm property: `public string CharacterName { get; set; } = "";`

#### **B. DbContext: GameDbContext.cs**
- ✅ Đã map column: `entity.Property(p => p.CharacterName).HasColumnName("character_name");`

#### **C. Controller: PlayerController.cs**
- ✅ API `/api/player/create` đã nhận thêm parameter `character_name`
- ✅ Validate: `character_name` không được rỗng
- ✅ Validate: `character_name` phải có từ 3 đến 20 ký tự
- ✅ API `/api/player/{playerId}/data` đã trả về field `character_name`

### 4.2. Client (Unity)

#### **A. APIClient.cs**
- ✅ Method `CreatePlayer()` đã nhận thêm parameter `characterName`
- ✅ Request body đã gửi cả `element_type`, `gender`, `character_name`
- ✅ `PlayerDataResponse` đã có field `character_name`

#### **B. SelectElementController.cs**
- ✅ Đã thay đổi từ 5 button hệ + 2 button giới tính → **9 button nhân vật riêng biệt**
- ✅ Class `CharacterButtonData` để lưu thông tin mỗi button (button, elementType, gender, **previewSprite**)
- ✅ Array `characterButtons[9]` chứa 9 button nhân vật
- ✅ Method `InitializeCharacterButtons()` để gán sự kiện cho tất cả button
- ✅ Method `OnCharacterButtonClicked(int index)` để xử lý khi chọn nhân vật
- ✅ Logic hiển thị preview nhân vật bằng **Image component** (set sprite trực tiếp, không spawn prefab)
- ✅ Method `ShowCharacterPreview()` lấy sprite từ `CharacterButtonData.previewSprite` và set vào `previewImage`
- ✅ Method `UpdateButtonVisuals()` để highlight button được chọn
- ✅ Method `OnCharacterNameChanged()` để update Go button
- ✅ Method `OnGoButtonClicked()` để load data và vào MainMenu
- ✅ Method `OnBackButtonClicked()` để quay lại Login
- ✅ Validate tên nhân vật (3-20 ký tự)
- ✅ Button "Go" chỉ enable khi đã tạo nhân vật thành công

---

## 📝 5. KIỂM TRA

### 5.1. Test các trường hợp:

1. **Chọn button Metal - Nam + nhập tên "Test123" + Xác nhận** → ✅ Tạo thành công → Button Go enable
2. **Chọn button Metal - Nữ + nhập tên "TestGirl" + Xác nhận** → ✅ Tạo thành công
3. **Chọn button Earth - Nam + nhập tên "EarthPlayer" + Xác nhận** → ✅ Tạo thành công
4. **Chọn button nhưng không nhập tên, bấm Xác nhận** → ❌ Hiển thị: "Vui lòng nhập tên nhân vật!"
5. **Không chọn button, bấm Xác nhận** → ❌ Hiển thị: "Vui lòng chọn nhân vật trước!"
6. **Nhập tên < 3 ký tự** → ❌ Hiển thị: "Tên nhân vật phải có từ 3 đến 20 ký tự!"
7. **Bấm "Go" sau khi tạo nhân vật** → ✅ Load data từ server → Vào MainMenu
8. **Bấm "Về"** → ✅ Quay lại scene Login
9. **Chọn button nhân vật khác nhau** → ✅ Preview image thay đổi sprite tương ứng, button được highlight
10. **Chọn button khác sau khi đã chọn** → ✅ Button cũ mất highlight, button mới được highlight, preview image cập nhật sprite mới

### 5.2. Kiểm tra Database:

Sau khi tạo nhân vật thành công:
- Vào phpMyAdmin → bảng `player_data`
- Kiểm tra cột `character_name` có giá trị đúng không

### 5.3. Kiểm tra GameManager:

Sau khi bấm "Go" và vào MainMenu:
- Kiểm tra `GameManager.Instance.GetPlayerData()` có đầy đủ dữ liệu không
- Kiểm tra các field: `character_name`, `level`, `element_type`, `gender`, `base_stats`, etc.

---

## ⚠️ 6. LƯU Ý

1. **Database Migration**:
   - Nếu database đã có dữ liệu, cần chạy SQL để thêm cột `character_name`
   - Có thể set giá trị mặc định cho các nhân vật cũ

2. **Prefabs Preview**:
   - Nếu chưa có prefab cho từng hệ, có thể:
     - Dùng prefab chung và thay đổi material/texture theo hệ
     - Hoặc tạo prefab mới cho từng hệ
   - Prefab preview có thể là model 3D hoặc sprite 2D tùy game

3. **UI/UX**:
   - Button "Go" chỉ enable khi đã tạo nhân vật thành công
   - Preview nhân vật tự động thay đổi khi chọn hệ khác
   - Text hướng dẫn luôn cập nhật theo lựa chọn hiện tại
   - InputField tên có giới hạn 20 ký tự

4. **Error Handling**:
   - Validate tên nhân vật ở cả client và server
   - Hiển thị message lỗi rõ ràng cho người chơi
   - Enable lại các button khi có lỗi

---

## 🎉 7. HOÀN TẤT

Sau khi làm theo hướng dẫn:
- ✅ Database đã có cột `character_name`
- ✅ Scene SelectElement đã có đầy đủ UI
- ✅ Code đã được cập nhật đầy đủ
- ✅ Logic validate đã hoạt động
- ✅ Preview nhân vật hiển thị khi chọn hệ
- ✅ Button "Go" load data từ server trước khi vào MainMenu
- ✅ Button "Về" quay lại Login

**Bạn có thể test ngay trong Unity Editor!**

---

## 📌 8. CHECKLIST TỔNG HỢP

### Database:
- [ ] Đã chạy SQL thêm cột `character_name`
- [ ] Kiểm tra cột đã tồn tại trong phpMyAdmin

### Unity Scene:
- [ ] Đã tạo 9 button chọn nhân vật (Metal-Male, Metal-Female, Wood-Male, Wood-Female, Water-Male, Water-Female, Fire-Male, Fire-Female, Earth-Male)
- [ ] Đã thêm TMP_InputField cho tên nhân vật
- [ ] Đã thêm Button "Về"
- [ ] Đã thêm Button "Xác nhận"
- [ ] Đã thêm Button "Go"
- [ ] Đã tạo Image component để hiển thị preview
- [ ] Đã chuẩn bị 9 sprites cho 9 nhân vật (hoặc lấy từ prefabs có sẵn)
- [ ] Đã gán tất cả UI vào `SelectElementController` trong Inspector:
  - [ ] Array `Character Buttons` size = 9, gán đầy đủ button, elementType, gender, **previewSprite** cho từng element
  - [ ] Gán CharacterNameInput, ErrorText, InstructionText
  - [ ] Gán ConfirmButton, BackButton, GoButton
  - [ ] Gán **PreviewImage** (Image component)
- [ ] Đã gắn sự kiện cho InputField (OnCharacterNameChanged)
- [ ] Đã gắn sự kiện cho ConfirmButton, BackButton, GoButton

### Code:
- [ ] Server đã nhận và validate `character_name`
- [ ] Client đã gửi `character_name` khi tạo nhân vật
- [ ] `SelectElementController` đã có logic preview nhân vật
- [ ] `SelectElementController` đã có logic load data khi bấm "Go"
- [ ] `GameManager` đã lưu player data đầy đủ

### Test:
- [ ] Test tạo nhân vật với tên hợp lệ
- [ ] Test validate tên không hợp lệ
- [ ] Test preview nhân vật khi chọn hệ
- [ ] Test button "Go" load data và vào MainMenu
- [ ] Test button "Về" quay lại Login
- [ ] Test với hệ Earth (chỉ có Nam)
