# CHECKLIST DEBUG – TAB KỸ NĂNG

> Tạo: 2026-03-04  
> Dựa trên log lỗi: `[APIClient] Skills response: {"skill_points_available":0,"player_level":1,"skills":[{"skill_id":5,"skill_code":"DASH",...}]}`

---

## 🔴 A. VẤN ĐỀ DB (Database)

### A1. ❌ DB thực tế KHÁC với `gamedb_v2.sql`
- **Vấn đề:** Log cho thấy `skill_id=5, skill_code="DASH"` — nhưng trong `gamedb_v2.sql` hiện tại, skill_id=5 sẽ là `FIRE_SLASH`. DB đang chạy đã được nạp từ schema cũ hơn, không khớp với file SQL hiện tại.
- **Fix:** Re-run `gamedb_v2.sql` để đồng bộ lại toàn bộ dữ liệu DB.
  ```
  mysql -u root -p gamedb < gamedb_v2.sql
  ```
- **Ảnh hưởng:** Skill name/code/levels_json hiện ra không đúng với code server hiện tại.

---

### A2. ❌ Skill element (Fire/Water/Earth/Wood/Metal) BỊ COMMENT OUT trong SQL
- **Vấn đề:** Trong `gamedb_v2.sql`, toàn bộ khối INSERT skill Fire (15 skills), Water (6), Earth (6), Wood (6), Metal (6) nằm bên trong block:
  ```sql
  /* ================================================================
     ARCHIVED SEED DATA – skill_template have been REMOVED in v2.
     ...
  */
  ```
  → Không có skill element nào được insert vào DB.
- **Fix:** Uncomment (hoặc tạo mới) các INSERT cho element skills, dùng đúng schema hiện tại:
  ```sql
  INSERT INTO `skill_template`
    (`skill_code`,`skill_name`,`description`,`element_type`,`max_level`,`level_to_unlock`,`levels_json`,`icon_id`)
  VALUES (...)
  ```
  > ⚠️ Các INSERT đã archive dùng sai tên cột: `skill_description` → phải là `description`, `level_scale_json` → phải là `levels_json`. Nếu uncomment phải sửa lại tên cột.
- **Ảnh hưởng:** Player hệ Fire vẫn thấy skill Universal nhưng **không thấy skill Fire** của mình.

---

### A3. ⚠️ `player_data.skills` = `[]` cho tất cả player
- **Vấn đề:** Seed data trong SQL đặt cột `skills = '[]'` cho mọi player. `GetPlayerSkills` đọc JSON này để lấy `current_level` của từng skill. Vì `[]` → `playerSkillLevels` rỗng → tất cả skill hiện `current_level = 0`.
- **Hành động cần check:** Sau khi upgrade skill lần đầu, kiểm tra trong MySQL xem cột `skills` có được ghi dữ liệu không:
  ```sql
  SELECT player_id, JSON_LENGTH(skills) FROM player_data;
  ```
- **Trạng thái:** Đây là đúng với player mới — chỉ cần đảm bảo UpgradeSkill ghi đúng.

---

### A4. ⚠️ `skill_points = 0` — Nút Nâng Cấp Luôn Bị Khoá
- **Vấn đề:** Seed data có `"skill_points":0` trong `info_char` JSON. Level 1 players chưa có SP → `can_upgrade = false` cho mọi skill → nút `+` disabled hết.
- **Fix tạm thời để test:** Update thủ công SP trong DB:
  ```sql
  UPDATE player_data
  SET info_char = JSON_SET(info_char, '$.skill_points', 5)
  WHERE player_id = 1;
  ```
- **Trạng thái:** Cần làm trước khi test UI.

---

## 🟡 B. VẤN ĐỀ SERVER (GameServerApi)

### B1. ❌ `GET /api/player/{id}/skills` trả về TẤT CẢ skill cho mọi player
- **File:** `GameServerApi/Controllers/PlayerController.cs` ~line 1038
- **Vấn đề:** Endpoint lấy `_db.SkillTemplates` không filter theo `element_type` của player. Player Metal thấy cả skill Fire/Water/Wood của người khác.
- **Code hiện tại:**
  ```csharp
  var templates = await _db.SkillTemplates
      .OrderBy(s => s.ElementType).ThenBy(s => s.SkillId)
      .ToListAsync();
  ```
- **Fix đề xuất:** Filter chỉ lấy skill Universal (`element_type = NULL`) hoặc khớp element của player:
  ```csharp
  string playerElement = info.ElementType; // lấy từ info_char JSON
  var templates = await _db.SkillTemplates
      .Where(s => s.ElementType == null || s.ElementType == playerElement)
      .OrderBy(s => s.ElementType).ThenBy(s => s.SkillId)
      .ToListAsync();
  ```
  > ⚠️ Cần thêm `ElementType` vào `InfoChar` model nếu chưa có.
- **Ảnh hưởng:** Tab hiển thị quá nhiều skill không thuộc element player.

---

### B2. ⚠️ `UpgradeSkill` — Kiểm tra key lưu trong `skills` JSON
- **File:** `GameServerApi/Controllers/PlayerController.cs` ~line 1100+
- **Vấn đề cần verify:** Sau khi upgrade, server save vào `player_data.skills` với key `"current_level"`. `GetPlayerSkills` khi đọc lại cũng tìm key `"current_level"`. Cần đảm bảo key này nhất quán.
- **Check:** Sau khi upgrade 1 skill, kiểm tra raw JSON trong DB:
  ```sql
  SELECT skills FROM player_data WHERE player_id = 1;
  ```
  Phải thấy: `[{"skill_id":1,"current_level":1}]`

---

### B3. ⚠️ `levels_json` dùng key `"desc"` — Server đọc đúng không?
- **File:** `GameServerApi/Controllers/PlayerController.cs` ~line 1055
- **Code:** `if (nextData.TryGetProperty("desc", out var dc)) nextDesc = dc.GetString() ?? "";`
- **Seed data:** `"desc":"Lướt về phía trước tránh đòn"` → key là `"desc"` ✅ (khớp)
- **Trạng thái:** OK nếu DB dùng đúng schema `gamedb_v2.sql`.

---

## 🔵 C. VẤN ĐỀ UNITY (Client)

### C1. ❌ [INSPECTOR] CharacterPanelController – `contentSkill` chưa gán?
- **File:** `Client/Assets/Scripts/UI/CharacterPanelController.cs`
- **Check trong Unity Editor:** Chọn GameObject `CharacterPanel` → Inspector → kiểm tra slot **Content Skill** có drag đúng GameObject `ContentSkill` (có component `SkillTabUI`) chưa.
- **Triệu chứng nếu NULL:** `contentSkill?.Load()` không được gọi → không có gì hiển thị trong tab Kỹ Năng.

---

### C2. ❌ [INSPECTOR] SkillTabUI – Thiếu references?
- **File:** `Client/Assets/Scripts/Inventory/SkillTabUI.cs`
- **Check:** Chọn `ContentSkill` → Inspector → kiểm tra từng slot:

  | Slot | GameObject cần gán | Triệu chứng nếu NULL |
  |------|--------------------|-----------------------|
  | `Txt Skill Points` | TMP_Text hiển thị "Điểm kỹ năng: X" | Điểm KN không hiện |
  | `Skill List Container` | Transform `Content` trong ScrollView | Skill rows không spawn |
  | `Skill Row Prefab` | Prefab `/Prefabs/SkillRowPrefab` (có SkillRowUI) | Không có row nào |
  | `Txt Status` | TMP_Text hiển thị trạng thái | Lỗi/loading không hiện |

- **Log check:** Nếu thấy log `[SkillTabUI] Thiếu skillRowPrefab hoặc skillListContainer!` → thiếu một trong 2 slot này.

---

### C3. ❌ [PREFAB] SkillRowPrefab – Thiếu references?
- **File:** `Client/Assets/Scripts/Inventory/SkillRowUI.cs`
- **Check:** Mở Prefab `SkillRowPrefab` → Inspector → kiểm tra:

  | Slot | Loại | Triệu chứng nếu NULL |
  |------|------|-----------------------|
  | `Txt Skill Name` | TMP_Text | Tên skill trống |
  | `Txt Level` | TMP_Text | Lv. không hiển thị |
  | `Txt Require` | TMP_Text | Yêu cầu / trạng thái không hiển thị |
  | `Txt Desc` | TMP_Text | Mô tả effect không hiển thị |
  | `Btn Upgrade` | Button | Nút + không hoạt động |
  | `Icon Image` | Image | (Optional) không ảnh hưởng logic |

---

### C4. ⚠️ `JsonUtility.FromJson` – `element_type: null` trong JSON
- **File:** `Client/Assets/Scripts/API/APIClient.cs` line ~993
- **Vấn đề:** Server trả `"element_type":null`. Unity's `JsonUtility.FromJson` set field này thành `null` hoặc `""` (empty string).
- **Code `SkillRowUI`:** `string.IsNullOrEmpty(_info.element_type)` → hiện `"[Universal]"` ✅
- **Trạng thái:** OK — đã xử lý đúng, không cần sửa.

---

### C5. ⚠️ `JsonUtility` – Không deserialize được nếu JSON thừa field?
- **Vấn đề:** `JsonUtility` bỏ qua field không có trong class → an toàn. Nhưng nếu bất kỳ field trong `PlayerSkillInfo` là kiểu `int` mà JSON trả `null` → JsonUtility có thể crash.
- **Check:** Đảm bảo tất cả `int` fields trong `PlayerSkillInfo` đều có giá trị số trong JSON (không phải `null`). Hiện tại server trả đúng kiểu số.
- **Trạng thái:** Cần theo dõi nếu thêm field mới.

---

### C6. ⚠️ ScrollView Content – Thiếu VerticalLayoutGroup + ContentSizeFitter?
- **Vấn đề:** Skill rows được `Instantiate` vào `skillListContainer` (Content transform). Nếu object Content thiếu:
  - `VerticalLayoutGroup` → các row chồng lên nhau tại (0,0)
  - `ContentSizeFitter` (`Vertical Fit = Preferred Size`) → scroll không hoạt động
- **Check:** Chọn `Content` object trong ScrollView hierarchy → Inspector → xác nhận có 2 component trên.

---

### C7. ⚠️ Tab không gọi `Load()` khi `playerId` chưa được set
- **File:** `Client/Assets/Scripts/UI/CharacterPanelController.cs` line ~157
- **Code:** `if (sk && playerId > 0) contentSkill?.Load();`
- **Vấn đề:** Nếu `playerId <= 0` (chưa login xong), tab Kỹ Năng không load data. `Start()` tự đọc từ `PlayerPrefs["USER_ID"]`.
- **Check:** Thêm log xác nhận `playerId` được set trước khi mở tab:
  ```csharp
  Debug.Log($"[CharacterPanel] SwitchTab skill – playerId={playerId}");
  ```

---

## ✅ THỨ TỰ FIX ĐỀ XUẤT

```
[ ] 1. A4 – Update thủ công skill_points = 5 cho player test (để test nút upgrade)
[ ] 2. C1 – Kiểm tra contentSkill đã gán trong Inspector chưa
[ ] 3. C2 – Kiểm tra tất cả slot của SkillTabUI trong Inspector
[ ] 4. C3 – Kiểm tra tất cả slot của SkillRowPrefab trong Inspector
[ ] 5. C6 – Kiểm tra Content có VerticalLayoutGroup + ContentSizeFitter
[ ] 6. B1 – Sửa GetPlayerSkills để filter theo element_type của player
[ ] 7. A2 – Uncomment + sửa tên cột cho element skill INSERTs trong gamedb_v2.sql
[ ] 8. A1 – Re-run gamedb_v2.sql để đồng bộ DB
[ ] 9. B2 – Verify UpgradeSkill ghi đúng key "current_level" vào skills JSON
```

---

## 📋 QUICK TEST CHECKLIST (sau khi fix)

```
[ ] Mở CharacterPanel → Click tab "Kỹ Năng"
[ ] Console KHÔNG thấy lỗi NullReferenceException
[ ] Hiển thị text "Điểm kỹ năng: X"
[ ] Danh sách scroll tối thiểu 3 skill rows
[ ] Mỗi row hiển thị: tên skill, Lv.0/5, mô tả, nút +
[ ] Nút + disabled khi skill_points = 0
[ ] Nút + enabled khi skill_points >= sp_cost (sau khi update DB)
[ ] Click nút + → level tăng lên 1, điểm SP giảm → list refresh
[ ] Skill Universal (element_type=null) hiển thị tag "[Universal]"
[ ] Skill Fire của player Fire hiển thị tag "[Fire]"
[ ] Player Metal KHÔNG thấy skill Fire (sau khi fix B1)
```
