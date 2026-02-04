# 🔁 LUỒNG HOẠT ĐỘNG GAME (CHI TIẾT - SERVER AUTHORITATIVE)

## 📋 TỔNG QUAN KIẾN TRÚC

```
Unity Client (PC/Android)
        │
        │ HTTP API (Login, Register, Player Data)
        │ Unity Netcode (Gameplay, Combat)
        ▼
Game Server (C# - Server Authoritative)
        │
        │ SQL Query
        ▼
SQL Database (MySQL/SQL Server)
```

**Kết nối:**
- HTTP API: `https://server.com/api/...`
- Game Server: `123.45.67.89:7777` (Unity Netcode)

---

## 1️⃣ REGISTER (Đăng Ký)

### Client:
- Hiển thị UI Register (Username, Email, Password, Confirm Password)
- Validate input (password match, email format)
- Gửi request → `POST /api/auth/register`

### Server:
1. Validate input (username, email, password)
2. Hash password (bcrypt/Argon2)
3. Kiểm tra username/email đã tồn tại
4. Tạo record trong bảng `users`:
   - `user_id` (AUTO_INCREMENT)
   - `username` (UNIQUE)
   - `email` (UNIQUE)
   - `password_hash`
   - `created_at`
5. Trả về JWT token

### Client:
- Lưu token vào PlayerPrefs hoặc SecureStorage
- Chuyển sang Login Scene

**❗ Lưu ý:** Chưa tạo nhân vật (player_data) ở bước này

---

## 2️⃣ LOGIN (Đăng Nhập)

### Client:
- Hiển thị UI Login (Username, Password)
- Gửi request → `POST /api/auth/login`

### Server:
1. Verify username/password
2. Tạo JWT token
3. Update `last_login` trong database
4. Trả về: `{token, user_id, username}`

### Client:
- Lưu token vào PlayerPrefs/SecureStorage
- Chuyển sang Main Menu

---

## 3️⃣ CHỌN HỆ BAN ĐẦU (One-time - Chỉ 1 lần)

### Client:
- Hiển thị UI chọn hệ:
  - Kim (Metal)
  - Mộc (Wood)
  - Thủy (Water)
  - Hỏa (Fire)
  - Thổ (Earth)
- Gửi request → `POST /api/player/create`
  ```json
  {
    "element_type": "Fire"
  }
  ```

### Server:
1. Validate: Player chưa có player_data
2. Tạo record trong bảng `player_data`:
   - `player_id` = `user_id`
   - `element_type` = hệ đã chọn
   - `gene_tier` = 1
   - `is_hybrid` = false
   - `secondary_element` = NULL
   - Set stats mặc định theo hệ:
     - Level = 1
     - Experience = 0
     - Gold = 0
     - Base stats (HP, MP, Attack...) theo hệ
   - `map_id` = 1 (map khởi đầu)
   - `position_x, position_y, position_z` = spawn point của map
   - `equipment` = {} (JSON rỗng)
   - `inventory` = [] (JSON rỗng)
   - `skills` = [] (JSON rỗng)
   - `potential_stats` = [] (JSON rỗng)
3. Save database
4. Trả về: `{success: true, player_id}`

### Client:
- Hiển thị thông báo "Tạo nhân vật thành công"
- Chuyển sang Main Menu

**❗ Bước này chỉ xảy ra 1 lần duy nhất cho mỗi account**

---

## 4️⃣ LOAD PLAYER DATA (Sau Login)

### Client:
- Gửi request với JWT token → `GET /api/player/{playerId}/data`

### Server:
1. Validate JWT token
2. Load từ database:
   - Base stats (hp, mp, attack, level, exp, gold...)
   - Hệ/Gene (element_type, gene_tier, is_hybrid, secondary_element)
   - Equipment JSON (trang bị đang mặc)
   - Inventory JSON (túi đồ)
   - Skills JSON (kỹ năng đã unlock)
   - Potential stats JSON (chỉ số đã cộng)
   - Map info (map_id, position)
3. **Server tính FINAL STATS:**
   ```
   Final Stats = Base Stats 
                + Equipment Stats 
                + Potential Stats 
                + Gene Bonuses (tier, hybrid)
                + Skill Bonuses (passive)
   ```
4. **Server tính Skill Points & Potential Points:**
   - Query `exp_requirements` từ level 1 → level hiện tại
   - Tính tổng skill_points và potential_points
   - Trừ đi số điểm đã dùng
5. **Server load EXP requirements:**
   - Query `exp_requirements` WHERE `level = current_level + 1`
   - Lấy `exp_required` cho level tiếp theo
6. Trả về JSON response:
   ```json
   {
     "player_id": 1,
     "level": 10,
     "experience": 5000,
     "exp_required_for_next_level": 8000,
     "base_stats": {...},
     "equipment": {...},
     "potential_stats": [...],
     "final_stats": {...},
     "inventory": [...],
     "skills": [...],
     "skill_points_available": 15,
     "potential_points_available": 20,
     "map_id": 1,
     "position": {...}
   }
   ```

### Client:
- Lưu data vào memory/local storage
- Hiển thị Main Menu với:
  - Equipment slots (weapon, armor, pants, boots)
  - Inventory UI
  - Stats UI (final stats từ server)
  - Skills UI
  - Potential stats UI

---

## 5️⃣ VÀO GAME (Join Game - Unity Netcode)

### Client:
1. User click "Join Game"
2. Gọi `ConnectToServer()` với IP:Port (ví dụ: `123.45.67.89:7777`)
3. Connect tới Game Server qua Unity Netcode

### Server:
1. Nhận connection → `OnClientConnected(ulong clientId)`
2. Xác thực JWT token (từ client hoặc session)
3. Map `clientId` → `user_id`
4. Load player data từ database:
   - Load `map_id` từ player_data
   - Load map info từ bảng `maps`
   - Load enemy_spawns từ bảng `enemy_spawns` WHERE `map_id = X`
5. Load map scene (nếu chưa load):
   - Load map prefab/scene
   - Spawn map objects
6. **Server spawn enemies trong map:**
   - Với mỗi spawn_id trong enemy_spawns:
     - Check `max_spawn_count` và số lượng hiện tại
     - Spawn enemy tại `spawn_x, spawn_y, spawn_z`
     - Sync enemy qua network (NetworkObject)
7. **Server spawn player prefab:**
   - Tại vị trí `position_x, position_y, position_z` (hoặc spawn point của map)
8. **Server apply player data:**
   - Position
   - Final stats (đã tính sẵn)
   - Equipment (để sync model)
   - Map ID
9. **Server sync data qua NetworkVariable:**
   - Player position, stats, equipment
   - Enemy positions, stats

### Client:
1. Nhận data từ server
2. Load map scene
3. Hiển thị character với equipment đã mặc:
   - Gắn weapon model vào player
   - Thay đổi armor/boots appearance
4. Hiển thị enemies đã spawn
5. Update UI (equipment slots, stats, inventory)

---

## 6️⃣ GAMEPLAY (Trong Game - Server Authoritative)

### 6.1. Movement

**Client:**
- Gửi input (WASD/Arrow keys) → Server

**Server:**
- Validate movement
- Check speed hack (max_move_speed)
- Update position
- Sync position qua NetworkVariable

**Client:**
- Nhận position → Update character position

---

### 6.2. Combat (Đánh Quái)

**Client:**
- Gửi attack request → Server (chỉ gửi input, KHÔNG tính damage)

**Server:**
1. Load player data:
   - Final stats (đã tính sẵn)
   - `element_type`, `gene_tier`, `is_hybrid`
2. Tính base damage từ `finalAttack`
3. Check tương khắc:
   - Kim khắc Mộc → x1.5 damage
   - Mộc khắc Thủy → x1.5 damage
   - Thủy khắc Hỏa → x1.5 damage
   - Hỏa khắc Kim → x1.5 damage
   - Thổ khắc tất cả → x1.2 damage
   - Bị khắc → x0.7 damage
4. Nếu `is_hybrid = true` → bonus 10% damage
5. Cộng elemental damage từ equipment (fire_damage, ice_damage...)
6. Tính final damage và apply vào enemy
7. Check enemy HP:
   - Nếu HP <= 0 → Enemy chết → Xử lý loot drop

**Client:**
- Nhận damage result → Hiển thị damage numbers
- Nhận enemy HP update → Update enemy HP bar

**❗ Client KHÔNG tính damage, chỉ hiển thị**

---

### 6.3. Enemy Chết & Loot Drop

**Server:**
1. Khi enemy HP <= 0:
   - Xác định player nào giết enemy (last damage dealer)
   - Query `loot_table` WHERE `enemy_type = X`
2. **Tính loot drop:**
   - Với mỗi item trong loot_table:
     - Nếu `is_guaranteed = true` → Rơi 100%
     - Nếu không → Random với `drop_rate`
     - Nếu rơi → Random `quantity` từ `min_quantity` đến `max_quantity`
   - Random gold từ `gold_min` đến `gold_max`
   - Lấy `exp_reward`
3. **Cộng Gold và EXP:**
   - Cộng gold vào `gold` của player
   - Cộng EXP vào `experience` của player
   - Update database
4. **Check Level Up:**
   - Query `exp_requirements` WHERE `level = current_level + 1`
   - Nếu `experience >= exp_required` → Gọi hàm Level Up
5. **Tạo loot items trên map:**
   - Tạo item objects tại vị trí enemy chết
   - Mỗi item có NetworkObject để sync
6. **Sync qua network:**
   - Update gold, EXP, level (nếu level up)
   - Spawn item prefabs

**Client:**
- Nhận gold/EXP update → Update UI
- Nhận level up notification → Hiển thị level up animation
- Nhận items trên ground → Hiển thị item models

---

### 6.4. Level Up

**Server:**
1. Xác định level mới:
   - `new_level = current_level + 1`
   - `remaining_exp = experience - exp_required`
2. Load `exp_requirements` cho level mới:
   - `base_stat_increase` (JSON)
   - `skill_points`
   - `potential_points`
3. Cập nhật base stats:
   - `baseMaxHp += base_stat_increase.hp`
   - `baseMaxMp += base_stat_increase.mp`
   - `baseAttack += base_stat_increase.attack`
4. Cập nhật level và EXP:
   - `level = new_level`
   - `experience = remaining_exp`
5. Cộng skill points và potential points
6. Tính lại final stats
7. Save database
8. Sync qua network

**Client:**
- Nhận level up notification
- Hiển thị level up animation/UI
- Update level, EXP bar, stats UI
- Hiển thị skill points và potential points mới

**Multi-level Up:** Server check và level up liên tục nếu EXP đủ lên nhiều level

---

### 6.5. Nhặt Item

**Client:**
- Player đi đến item
- Gửi request → `POST /api/player/{playerId}/pickup-item`
  ```json
  {
    "item_id": 101,
    "position_x": 10,
    "position_y": 0,
    "position_z": 5
  }
  ```

**Server:**
1. Validate:
   - Item có tồn tại tại vị trí đó không?
   - Player có đủ chỗ trong inventory không?
   - Check distance (player phải gần item, ví dụ: < 2m)
2. Xử lý:
   - Thêm item vào `inventory` JSON
   - Despawn item (destroy NetworkObject)
   - Update database
3. Trả về: updated inventory

**Client:**
- Nhận updated inventory → Update inventory UI

---

### 6.6. Equip/Unequip Item

**Client:**
- Click item trong inventory → Click "Equip"
- Gửi request → `POST /api/player/{playerId}/equip`
  ```json
  {
    "item_id": 101,
    "slot": "weapon"
  }
  ```

**Server:**
1. Validate:
   - Item có trong inventory không?
   - Slot hợp lệ không? (weapon, armor, pants, boots)
   - Item có thể equip vào slot này không?
2. Xử lý:
   - Nếu slot đã có item → đưa item cũ về inventory
   - Remove item mới khỏi inventory
   - Update `equipment` JSON trong database
   - Tính lại final stats (base + equipment mới + potential + gene)
3. Save database
4. Trả về: updated equipment + final stats

**Client:**
- Nhận response:
  - Update equipment UI
  - Update inventory UI
  - Update stats UI
  - Gắn weapon model vào player (nếu là weapon)
  - Thay đổi armor/boots appearance

---

### 6.7. Cộng Chỉ Số (Potential Stats)

**Client:**
- Mở Potential Stats UI
- Chọn stat muốn cộng (attack, max_hp, max_mp...)
- Gửi request → `POST /api/player/{playerId}/add-potential-stat`
  ```json
  {
    "stat_name": "attack",
    "points": 5
  }
  ```

**Server:**
1. Validate:
   - Player có đủ potential points không?
   - Stat name hợp lệ không?
   - Points > 0 và <= potential points available
2. Xử lý:
   - Load `potential_stats` JSON
   - Tìm stat trong array:
     - Nếu đã có → Cộng thêm points
     - Nếu chưa có → Thêm mới
   - Trừ potential points available
   - Update `potential_stats` JSON
   - Tính lại final stats
3. Save database
4. Trả về: updated potential_stats + final_stats + potential_points_remaining

**Client:**
- Nhận response:
  - Update potential stats UI
  - Update final stats UI
  - Update potential points UI

---

### 6.8. Unlock/Upgrade Skill

**Client:**
- Mở Skills UI
- Click "Unlock" hoặc "Upgrade" skill
- Gửi request → `POST /api/player/{playerId}/unlock-skill` hoặc `upgrade-skill`
  ```json
  {
    "skill_id": 101
  }
  ```

**Server:**
1. Validate:
   - Skill có tồn tại không?
   - Player đủ level để unlock không?
   - Player có đủ skill points không?
   - Skill đã được unlock chưa? (nếu unlock)
   - Skill chưa max level chưa? (nếu upgrade)
2. Xử lý:
   - Load `skills` JSON
   - Thêm skill mới (unlock) hoặc tăng level (upgrade)
   - Trừ skill points
   - Update `skills` JSON
   - Tính lại final stats (nếu skill có passive bonus)
3. Save database
4. Trả về: updated skills + skill_points_remaining

**Client:**
- Nhận response:
  - Update skills UI
  - Update skill points UI
  - Hiển thị skill trong skill bar (nếu unlock)

---

### 6.9. Đổi Map

**Client:**
- Player đi đến portal/exit
- Gửi request → `POST /api/player/{playerId}/change-map`
  ```json
  {
    "map_id": 2,
    "position_x": 0,
    "position_y": 0,
    "position_z": 0
  }
  ```

**Server:**
1. Validate:
   - Map có tồn tại không?
   - Player đủ level không? (check min_level, max_level)
2. Xử lý:
   - Update `map_id`, `position_x, position_y, position_z` trong database
   - Load enemy_spawns cho map mới
   - Spawn enemies trong map mới
3. Save database
4. Trả về: map info + enemy spawns

**Client:**
- Nhận response:
  - Load map scene mới
  - Spawn player tại vị trí mới
  - Hiển thị enemies mới

---

## 7️⃣ AUTO SAVE / SAVE QUAN TRỌNG

**Server tự động save `player_data` khi:**

1. **Level up** → Save level, experience, base stats
2. **Equip/Unequip item** → Save equipment JSON, final stats
3. **Cộng potential stats** → Save potential_stats JSON, final stats
4. **Unlock/Upgrade skill** → Save skills JSON, final stats
5. **Nhặt item** → Save inventory JSON
6. **Đổi map** → Save map_id, position
7. **Mỗi 5 phút** → Auto-save toàn bộ data
8. **Player disconnect** → Save toàn bộ data trước khi disconnect

**Server KHÔNG save:**
- Movement (chỉ sync qua network, không save liên tục)
- Combat damage (chỉ sync HP, không save mỗi hit)

---

## 8️⃣ THOÁT GAME / DISCONNECT

**Client:**
- User click "Quit" hoặc đóng game
- Disconnect khỏi Game Server

**Server:**
1. Nhận disconnect → `OnClientDisconnected(ulong clientId)`
2. Save toàn bộ player data:
   - Level, experience, gold
   - Position, map_id
   - Equipment, inventory, skills, potential_stats
   - Tất cả stats
3. Remove mapping `clientId → user_id`
4. Despawn player prefab
5. Log disconnect

**Client:**
- Cleanup network objects
- Quay về Main Menu hoặc đóng game

**Player đăng nhập lại → Quay về bước 4 (Load Player Data)**

---

## 🧠 TÓM TẮT LUỒNG (1 DÒNG)

```
Register 
→ Login 
→ Chọn hệ ban đầu (1 lần) 
→ Load Player Data (HTTP API)
→ Join Game (Unity Netcode)
→ Server load data + spawn player + spawn enemies
→ Gameplay (Server Authoritative: movement, combat, loot, level up, equip, skills, maps)
→ Auto save (mỗi 5 phút + khi có thay đổi quan trọng)
→ Disconnect → Save data
```

---

## ⚠️ QUY TẮC VÀNG (SERVER AUTHORITATIVE)

1. **Client KHÔNG tính damage** → Chỉ server tính
2. **Client KHÔNG tự cộng EXP/Gold** → Chỉ server cộng
3. **Client KHÔNG tự unlock skill** → Chỉ server unlock
4. **Client KHÔNG tự cộng stats** → Chỉ server cộng
5. **Client KHÔNG spawn enemy** → Chỉ server spawn
6. **Client KHÔNG tự tạo item** → Chỉ server tạo từ loot table
7. **Server là source of truth** cho tất cả calculations và validations

---

## 📊 DATABASE TABLES SỬ DỤNG

- `users` - Thông tin account
- `player_data` - Toàn bộ thông tin nhân vật (1 bảng duy nhất)
- `maps` - Thông tin maps
- `enemy_spawns` - Vị trí spawn enemies
- `loot_table` - Items và rewards từ enemies
- `exp_requirements` - EXP cần và rewards khi level up
