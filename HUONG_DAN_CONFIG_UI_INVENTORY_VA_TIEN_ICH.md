# Huong dan config Inventory Toggle va box tien ich

Tai lieu nay gom 2 phan:

1. Cau hinh `InventoryToggleBtn` de mo dung `CharacterPanel` + `InventoryPanel`
2. Cau hinh box tien ich co nut mui ten dong/mo theo nhu cau

## 1. InventoryToggleBtn

Code lien quan:

- `Client/Assets/Scripts/Inventory/UI/InventoryToggleButton.cs`
- `Client/Assets/Scripts/UI/Character/InformationPanelController.cs`
- `Client/Assets/Scripts/UI/Character/CharacterPanelController.cs`

Hanh vi sau khi sua:

- Bam `InventoryToggleBtn` se luon mo tab tui do
- `CharacterPanel` shell van active
- `Window` ben trong `CharacterPanel` se inactive
- `InventoryPanel` se active
- Khong tu dong toggle-off khi bam lai nut shortcut

### Inspector can gan

Gan script `InventoryToggleButton` vao button shortcut `InventoryToggleBtn`.

Trong Inspector:

- `Information Panel`: keo `InformationPanelController`
- `Character Panel`: keo `CharacterPanelController` neu muon co fallback
- `Inventory UI`: keo `InventoryUI` neu muon co fallback

Trong `InformationPanelController`:

- `BtnThongTin` -> nut thong tin
- `BtnTuiDo` -> nut tui do tab goc
- `BtnCloseAll` -> nut close tong
- `Character Panel` -> `CharacterPanelController`
- `Inventory UI` -> `InventoryUI`

Trong `CharacterPanelController`:

- `Panel Root` phai la object goc `CharacterPanel`
- `Content Root` phai la object `Window` ben trong `CharacterPanel`

Muc tieu dung:

- `Panel Root` active de 2 nut tab van hien
- `Content Root` inactive khi mo tui do tu shortcut

## 2. Box tien ich co nut mui ten

Code moi:

- `Client/Assets/Scripts/UI/Menu/UtilityDrawerController.cs`
- `Client/Assets/Scripts/UI/Menu/UtilityDrawerAutoInstaller.cs`

Mac dinh he thong se tu tao 1 box tien ich runtime trong `GameScene` neu scene chua co `UtilityRoot`.
Neu ban da co UI thu cong san, script auto-installer se bo qua va khong tao trung.

Script nay ho tro 2 kieu:

- Kieu A: box van ton tai khi collapse, chi an noi dung, nut mui ten nhay len dau box
- Kieu B: collapse xong an ca box, dung them 1 `ShowButton` de mo lai

### Hierarchy goi y

```text
UtilityRoot
|- UtilityBox
|  |- UtilityContent
|  |- ToggleArrowButton
|  |- AnchorExpanded
|  |- AnchorCollapsed
|- ShowUtilityButton
```

### Cach gan Inspector

Gan `UtilityDrawerController` vao `UtilityRoot` hoac `UtilityBox` deu duoc.

Gan cac field:

- `Box Root`: object goc cua box tien ich
- `Content Root`: object chua toan bo icon tien ich
- `Toggle Button`: nut mui ten dong/mo
- `Show Button`: nut mo box tu ben ngoai, co the bo trong neu khong dung
- `Toggle Button Rect`: `RectTransform` cua nut mui ten
- `Toggle Graphic`: image/icon mui ten can quay 180 do
- `Expanded Button Anchor`: empty rect dat ben duoi listbox
- `Collapsed Button Anchor`: empty rect dat o dau box
- `Box Rect`: rect cua frame box neu muon co height expand/collapse ro rang

### Gia tri nen dung

- `Start Expanded`: `true` neu muon vao scene la mo san
- `Hide Box When Collapsed`: `false` neu muon box van con frame khi collapse
- `Expanded Arrow Rotation Z`: `0`
- `Collapsed Arrow Rotation Z`: `180`
- `Expanded Box Height`: vi du `170`
- `Collapsed Box Height`: vi du `44`

### Hanh vi sau khi config

- Bam `ShowButton` -> hien toan bo tien ich
- Bam `ToggleButton` khi dang mo -> an `UtilityContent`, quay mui ten 180 do, nut nhay len `AnchorCollapsed`
- Bam `ToggleButton` khi dang dong -> mo lai `UtilityContent`, dua nut xuong `AnchorExpanded`

## 3. Khuyen nghi setup theo anh mau

Neu muon giong anh user gui:

- Dat `AnchorExpanded` o ngay duoi cum icon tien ich
- Dat `AnchorCollapsed` o mep tren cua box
- `UtilityContent` chua toan bo button: Qua tang, Kho bau, Phuc loi, Thu, Hoat dong, Vong xo may man, Uu dai, BXH, Cho, Shop

## 4. Neu scene hien tai chua co object

Can tao them:

- 1 object `UtilityBox`
- 1 object `UtilityContent`
- 2 empty `RectTransform`: `AnchorExpanded`, `AnchorCollapsed`
- 1 nut `ToggleArrowButton`
- 1 nut `ShowUtilityButton` neu muon an ca box khi collapse

## 5. File clone

Neu dang test bang `Client_clone_0` thi script utility moi cung da duoc mirror sang clone.