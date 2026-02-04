# Kiến Trúc Hệ Thống Game Unity Multiplayer

```
┌──────────────────────┐
│     Unity Client     │
│  (PC / Android)      │
│                      │
│ - UI Login/Register  │
│ - Gameplay           │
│ - Hiển thị nhân vật  │
└─────────┬────────────┘
          │
          │ HTTP / TCP (Login, Register)
          ▼
┌──────────────────────┐
│   Game Server (C#)   │
│  (Server Authoritative)
│                      │
│ - Login / Register   │
│ - Verify Account     │
│ - Load Player Data   │
│ - Save Player Data   │
│ - Netcode Server     │
└─────────┬────────────┘
          │
          │ SQL Query
          ▼
┌──────────────────────┐
│      SQL Database    │
│ (MySQL / SQL Server) │
│                      │
│ - users              │
│ - player_data        │
│ - inventory          │
│ - skills             │
└──────────────────────┘
```

---

# Kế Hoạch Triển Khai Chi Tiết

## 📋 GIAI ĐOẠN 1: Thiết Lập Cơ Sở Hạ Tầng (Tuần 1-2)

### 1.1. Database Setup
- [ ] **Cài đặt Database Server**
  - Chọn MySQL hoặc SQL Server
  - Cấu hình server, tạo database mới
  
- [ ] **Thiết kế Schema Database**
  - Tạo bảng `users`:
    - `user_id` (PRIMARY KEY, AUTO_INCREMENT)
    - `username` (UNIQUE, VARCHAR)
    - `email` (UNIQUE, VARCHAR)
    - `password_hash` (VARCHAR)
    - `created_at` (DATETIME)
    - `last_login` (DATETIME)
  
  - Tạo bảng `player_data` (bảng duy nhất lưu toàn bộ thông tin nhân vật):
    - `player_id` (PRIMARY KEY, FOREIGN KEY -> users.user_id)
    
    **Thông tin cơ bản:**
    - `level` (INT)
    - `experience` (INT)
    - `gold` (INT)
    - `map_id` (INT, FOREIGN KEY -> maps.map_id) - Map hiện tại player đang ở
    - `position_x`, `position_y`, `position_z` (FLOAT) - Vị trí trong map
    - `updated_at` (DATETIME)
    
    **Chỉ số cơ bản:**
    - `hp` (INT) - HP hiện tại
    - `max_hp` (INT) - HP tối đa
    - `mp` (INT) - MP hiện tại
    - `max_mp` (INT) - MP tối đa
    - `attack` (INT) - Tấn công
    
    **Chỉ số phòng thủ & sát thương:**
    - `damage_reduction` (FLOAT) - Giảm sát thương (%)
    - `critical_rate` (FLOAT) - Tỉ lệ chí mạng (%)
    - `critical_damage` (FLOAT) - Sát thương chí mạng (%)
    - `dodge_rate` (FLOAT) - Né đòn (%)
    - `block_rate` (FLOAT) - Block (%)
    - `lifesteal` (FLOAT) - Hút máu (%)
    
    **Chỉ số chiến đấu:**
    - `attack_speed` (FLOAT) - Tốc độ đánh
    - `cooldown_reduction` (FLOAT) - Thời gian hồi chiêu (giảm %)
    - `move_speed` (FLOAT) - Tốc độ di chuyển
    - `jump_height` (FLOAT) - Độ cao nhảy
    - `dash_speed` (FLOAT) - Tốc độ lướt
    
    **Hệ / Gene (Ngũ hành) - BẮT BUỘC:**
    - `element_type` (ENUM: 'Metal','Wood','Water','Fire','Earth') - Hệ chính (Kim, Mộc, Thủy, Hỏa, Thổ)
    - `gene_tier` (TINYINT, DEFAULT 1) - Tier của Gene (1, 2, 3...)
    - `is_hybrid` (BOOLEAN, DEFAULT FALSE) - Có phải Fusion Gene không
    - `secondary_element` (ENUM: 'Metal','Wood','Water','Fire','Earth', NULL) - Hệ phụ (chỉ có khi Fusion)
    
    **Chỉ số nguyên tố (Elemental Damage - từ vũ khí/items):**
    - `fire_damage` (INT) - Sát thương lửa (từ items, không phải hệ)
    - `ice_damage` (INT) - Sát thương băng
    - `lightning_damage` (INT) - Sát thương sét
    - `poison_damage` (INT) - Sát thương độc
    - `bleed_damage` (INT) - Chảy máu
    
    **Dữ liệu JSON (lưu dạng TEXT/JSON):**
    - `equipment` (TEXT/JSON) - Trang bị đang mặc (JSON object)
      - Ví dụ: `{"weapon": {"item_id": 101, "name": "Flame Sword", "attack": 50, "fire_damage": 20}, "armor": {"item_id": 201, "defense": 30, "hp": 200}, "pants": null, "boots": {"item_id": 301, "move_speed": 1.2}}`
      - Slots: weapon, armor, pants, boots (có thể mở rộng: ring, necklace...)
    - `skills` (TEXT/JSON) - Danh sách skills của player (JSON array)
      - Ví dụ: `[{"skill_id": 1, "skill_name": "Fireball", "level": 5, "unlocked": true}, ...]`
    - `inventory` (TEXT/JSON) - Danh sách items trong inventory (JSON array)
      - Ví dụ: `[{"item_id": 1, "item_name": "Sword", "quantity": 1, "slot_index": 0}, ...]`
    - `potential_stats` (TEXT/JSON) - Mảng tiềm năng để nâng chỉ số (JSON array)
      - Ví dụ: `[{"stat_name": "attack", "points": 10}, {"stat_name": "max_hp", "points": 5}, ...]`

  - Tạo bảng `maps`:
    - `map_id` (PRIMARY KEY, AUTO_INCREMENT)
    - `map_name` (VARCHAR) - Tên map
    - `map_type` (VARCHAR) - Loại map (field, dungeon, raid, arena...)
    - `min_level` (INT) - Level tối thiểu để vào
    - `max_level` (INT) - Level tối đa
    - `spawn_x`, `spawn_y`, `spawn_z` (FLOAT) - Vị trí spawn mặc định
    - `created_at` (DATETIME)

  - Tạo bảng `enemy_spawns`:
    - `spawn_id` (PRIMARY KEY, AUTO_INCREMENT)
    - `map_id` (FOREIGN KEY -> maps.map_id)
    - `enemy_type` (VARCHAR) - Loại enemy (goblin, orc, boss...)
    - `enemy_level` (INT) - Level của enemy
    - `spawn_x`, `spawn_y`, `spawn_z` (FLOAT) - Vị trí spawn
    - `spawn_radius` (FLOAT) - Bán kính spawn (nếu spawn random trong vùng)
    - `respawn_time` (INT) - Thời gian respawn (giây)
    - `max_spawn_count` (INT) - Số lượng tối đa cùng lúc
    - `is_active` (BOOLEAN, DEFAULT TRUE) - Có đang spawn không
    - `created_at` (DATETIME)

  - Tạo bảng `loot_table`:
    - `loot_id` (PRIMARY KEY, AUTO_INCREMENT)
    - `enemy_type` (VARCHAR) - Loại enemy (khớp với enemy_spawns.enemy_type)
    - `item_id` (INT) - Item sẽ rơi
    - `item_name` (VARCHAR) - Tên item
    - `drop_rate` (FLOAT) - Tỉ lệ rơi (0.0 - 1.0, ví dụ: 0.1 = 10%)
    - `min_quantity` (INT, DEFAULT 1) - Số lượng tối thiểu
    - `max_quantity` (INT, DEFAULT 1) - Số lượng tối đa
    - `is_guaranteed` (BOOLEAN, DEFAULT FALSE) - Bảo đảm rơi (100%)
    - `gold_min` (INT, DEFAULT 0) - Gold tối thiểu
    - `gold_max` (INT, DEFAULT 0) - Gold tối đa
    - `exp_reward` (INT, DEFAULT 0) - EXP khi giết enemy này
    - `created_at` (DATETIME)

  - Tạo bảng `exp_requirements`:
    - `level` (PRIMARY KEY, INT) - Level cần đạt
    - `exp_required` (INT) - EXP cần để lên level này
    - `base_stat_increase` (JSON) - Chỉ số tăng khi lên level
      - Ví dụ: `{"hp": 50, "mp": 30, "attack": 10}`
    - `skill_points` (INT, DEFAULT 0) - Số điểm skill nhận được khi lên level
    - `potential_points` (INT, DEFAULT 0) - Số điểm tiềm năng nhận được khi lên level
    - `created_at` (DATETIME)

### 1.2. Game Server Setup (C#)
- [ ] **Tạo Project Game Server**
  - Tạo solution C# (.NET 6/7/8)
  - Cấu hình project structure:
    ```
    GameServer/
    ├── Controllers/      (API Controllers)
    ├── Services/         (Business Logic)
    ├── Models/           (Data Models)
    ├── Data/             (Database Context)
    ├── Network/          (Netcode for GameObjects)
    └── Program.cs
    ```

- [ ] **Cài đặt Dependencies**
  - Entity Framework Core (cho database)
  - MySQL.Data hoặc Microsoft.Data.SqlClient
  - ASP.NET Core (cho HTTP API)
  - Unity Netcode for GameObjects (cho multiplayer)
  - JWT Authentication (cho security)

- [ ] **Cấu hình Database Connection**
  - Tạo DbContext
  - Setup connection string
  - Test kết nối database

---

## 📋 GIAI ĐOẠN 2: Backend API - Authentication (Tuần 2-3)

### 2.1. Authentication System
- [ ] **Register API**
  - Endpoint: `POST /api/auth/register`
  - Validate input (username, email, password)
  - Hash password (bcrypt/Argon2)
  - Kiểm tra username/email đã tồn tại
  - Tạo user mới trong database
  - Trả về JWT token

- [ ] **Login API**
  - Endpoint: `POST /api/auth/login`
  - Verify username/password
  - Tạo JWT token
  - Update `last_login` trong database
  - Trả về token + player data cơ bản

- [ ] **Verify Token API**
  - Endpoint: `GET /api/auth/verify`
  - Validate JWT token
  - Trả về user info nếu hợp lệ

### 2.2. Player Data APIs
- [ ] **Load Player Data (Khi Login)**
  - Endpoint: `GET /api/player/{playerId}/data`
  - Lấy toàn bộ thông tin từ bảng `player_data`:
    - Thông tin cơ bản: level, exp, gold, map_id, position
    - Tất cả chỉ số base: HP, MP, Attack, và các chỉ số khác
    - Hệ/Gene: element_type, gene_tier, is_hybrid, secondary_element
    - Parse JSON columns: equipment, skills, inventory, potential_stats
    - Load map info từ bảng `maps` dựa trên `map_id`
  - **Server tính FINAL STATS:**
    - Base stats + Equipment stats + Potential stats
    - Áp dụng Gene bonuses
    - Trả về final stats đã tính sẵn
  - **Server tính Skill Points & Potential Points:**
    - Query `exp_requirements` để tính tổng skill_points và potential_points từ level 1 đến level hiện tại
    - Trừ đi số điểm đã dùng (từ skills và potential_stats)
    - Tính points available
  - **Server load EXP requirements:**
    - Query `exp_requirements` WHERE `level = current_level + 1`
    - Lấy `exp_required` cho level tiếp theo
  - Trả về JSON response đầy đủ:
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
      "potential_points_available": 20
    }
    ```

- [ ] **Save Player Data**
  - Endpoint: `POST /api/player/{playerId}/save`
  - Validate data từ client (tất cả chỉ số)
  - Serialize equipment, skills, inventory, potential_stats thành JSON
  - Update database (1 bảng duy nhất)
  - Xử lý transaction để đảm bảo data consistency
  - Trả về success/error status

### 2.3. Equipment APIs
- [ ] **Equip Item**
  - Endpoint: `POST /api/player/{playerId}/equip`
  - Request body: `{"item_id": 101, "slot": "weapon"}`
  - Server validate:
    - Item có trong inventory không?
    - Slot hợp lệ không? (weapon, armor, pants, boots)
    - Item có thể equip vào slot này không?
  - Server update `equipment` JSON trong database
  - Server tính lại final stats (base + equipment)
  - Server save database
  - Trả về: updated equipment + final stats

- [ ] **Unequip Item**
  - Endpoint: `POST /api/player/{playerId}/unequip`
  - Request body: `{"slot": "weapon"}`
  - Server validate slot hợp lệ
  - Server remove item khỏi equipment JSON
  - Server đưa item về inventory
  - Server tính lại final stats
  - Server save database
  - Trả về: updated equipment + final stats

- [ ] **Get Equipment**
  - Endpoint: `GET /api/player/{playerId}/equipment`
  - Lấy equipment JSON từ database
  - Trả về equipment hiện tại đang mặc

- [ ] **Get Inventory**
  - Endpoint: `GET /api/player/{playerId}/inventory`
  - Lấy inventory JSON từ database
  - Trả về danh sách items trong túi

### 2.4. Map APIs
- [ ] **Change Map**
  - Endpoint: `POST /api/player/{playerId}/change-map`
  - Request body: `{"map_id": 1, "position_x": 0, "position_y": 0, "position_z": 0}`
  - Server validate:
    - Map có tồn tại không?
    - Player đủ level không? (check min_level, max_level)
  - Server update `map_id`, `position_x, position_y, position_z` trong database
  - Server trả về map info + enemy spawns

- [ ] **Get Map Info**
  - Endpoint: `GET /api/map/{mapId}`
  - Lấy thông tin map từ database
  - Trả về: map_name, map_type, min_level, max_level, spawn position, enemy_spawns

- [ ] **Get Current Map**
  - Endpoint: `GET /api/player/{playerId}/current-map`
  - Lấy map_id hiện tại của player
  - Trả về map info

### 2.5. Loot & Item APIs
- [ ] **Pickup Item**
  - Endpoint: `POST /api/player/{playerId}/pickup-item`
  - Request body: `{"item_id": 101, "position_x": 10, "position_y": 0, "position_z": 5}`
  - Server validate:
    - Item có tồn tại tại vị trí đó không?
    - Player có đủ chỗ trong inventory không?
    - Check distance (player phải gần item, ví dụ: < 2m)
  - Server xử lý:
    - Thêm item vào inventory JSON
    - Despawn item (nếu đang trong game)
    - Update database
  - Trả về: updated inventory

- [ ] **Get Loot Table**
  - Endpoint: `GET /api/enemy/{enemyType}/loot-table`
  - Lấy danh sách items có thể rơi từ enemy type
  - Trả về loot_table cho enemy đó

### 2.6. Potential Stats APIs
- [ ] **Add Potential Stat**
  - Endpoint: `POST /api/player/{playerId}/add-potential-stat`
  - Request body: `{"stat_name": "attack", "points": 5}`
  - Server validate: đủ potential points, stat name hợp lệ
  - Server update potential_stats JSON
  - Server tính lại final stats
  - Trả về: updated potential_stats + final_stats + potential_points_remaining

- [ ] **Reset Potential Stats**
  - Endpoint: `POST /api/player/{playerId}/reset-potential-stats`
  - Server reset potential_stats JSON
  - Hoàn lại potential points
  - Trả về: reset stats + potential_points_available

- [ ] **Get Potential Stats**
  - Endpoint: `GET /api/player/{playerId}/potential-stats`
  - Lấy potential_stats JSON từ database
  - Trả về potential stats hiện tại

### 2.7. Skills APIs
- [ ] **Unlock Skill**
  - Endpoint: `POST /api/player/{playerId}/unlock-skill`
  - Request body: `{"skill_id": 101}`
  - Server validate: đủ skill points, đủ level, skill chưa unlock
  - Server thêm skill vào skills JSON
  - Trả về: updated skills + skill_points_remaining

- [ ] **Upgrade Skill**
  - Endpoint: `POST /api/player/{playerId}/upgrade-skill`
  - Request body: `{"skill_id": 101}`
  - Server validate: skill đã unlock, chưa max level, đủ skill points
  - Server tăng skill level trong skills JSON
  - Trả về: updated skills + skill_points_remaining

- [ ] **Get Skills**
  - Endpoint: `GET /api/player/{playerId}/skills`
  - Lấy skills JSON từ database
  - Trả về danh sách skills đã unlock

- [ ] **Get Available Skills**
  - Endpoint: `GET /api/skills/available?level={level}`
  - Lấy danh sách skills có thể unlock ở level hiện tại
  - Trả về danh sách skills với requirements

---

## 📋 GIAI ĐOẠN 3: Unity Client - UI & Authentication (Tuần 3-4)

### 3.1. Unity Project Setup
- [ ] **Tạo Unity Project**
  - Unity version: 2021.3 LTS hoặc 2022.3 LTS
  - Import Unity Netcode for GameObjects package
  - Cấu hình project settings

- [ ] **Project Structure**
  ```
  Assets/
  ├── Scripts/
  │   ├── Network/        (Netcode scripts)
  │   ├── UI/             (UI controllers)
  │   ├── Player/         (Player scripts)
  │   ├── API/            (HTTP client)
  │   └── Managers/       (Game managers)
  ├── Scenes/
  │   ├── LoginScene
  │   ├── GameScene
  │   └── RegisterScene
  └── Prefabs/
      ├── Player
      └── UI/
  ```

### 3.2. Login/Register UI
- [ ] **Login Scene**
  - Tạo UI Canvas với:
    - Input field: Username
    - Input field: Password
    - Button: Login
    - Button: Register (chuyển sang Register Scene)
    - Text: Error message display
  
  - Script `LoginController.cs`:
    - Gửi HTTP POST request đến `/api/auth/login`
    - Lưu JWT token vào PlayerPrefs hoặc secure storage
    - Load player data sau khi login thành công
    - Chuyển sang Game Scene

- [ ] **Register Scene**
  - Tạo UI Canvas với:
    - Input field: Username
    - Input field: Email
    - Input field: Password
    - Input field: Confirm Password
    - Button: Register
    - Button: Back to Login
  
  - Script `RegisterController.cs`:
    - Validate input (password match, email format)
    - Gửi HTTP POST request đến `/api/auth/register`
    - Hiển thị success/error message
    - Tự động chuyển về Login Scene sau khi đăng ký thành công

### 3.3. HTTP Client Service
- [ ] **Tạo API Client Script**
  - `APIClient.cs` - Singleton service:
    - Base URL configuration
    - Methods: `Login()`, `Register()`, `LoadPlayerData()`, `SavePlayerData()`
    - Xử lý HTTP requests với UnityWebRequest
    - Xử lý errors và timeouts
    - JWT token management (lưu, gửi trong headers)

---

## 📋 GIAI ĐOẠN 4: Unity Client - Gameplay & Network (Tuần 4-5)

### 4.1. Player Character
- [ ] **Player Prefab Setup**
  - Tạo Player prefab với:
    - NetworkObject component (Netcode)
    - CharacterController hoặc Rigidbody
    - Animator (nếu có animation)
    - PlayerController script
  
- [ ] **PlayerController Script**
  - Xử lý movement (WASD/Arrow keys)
  - Xử lý camera follow
  - Sync position qua network
  - Xử lý input cho mobile (touch controls)

### 4.2. Network Setup - Client-Server Connection qua IP:Port

#### 4.2.1. Kiến Trúc Tổng Thể
```
CLIENT (Unity Player - PC/Android)
        |
        | Internet (IP:Port)
        |
SERVER (Unity Server Build trên Linux VPS)
        |
     SQL Database
```

**Ví dụ cấu hình:**
- VPS IP: `123.45.67.89`
- Game Port: `7777`
- Client connect tới: `123.45.67.89:7777`

#### 4.2.2. Cấu Trúc Project Unity (Network)
- [ ] **Tạo cấu trúc thư mục:**
  ```
  Assets/
   ├── Scripts/
   │   ├── Network/
   │   │   ├── NetworkManagerCustom.cs    (Quản lý kết nối)
   │   │   ├── PlayerNetwork.cs           (NetworkBehaviour cho player)
   │   │   ├── GameServerManager.cs       (Server logic)
   │   │   └── NetworkConfig.cs           (Config IP/Port)
   │   ├── UI/
   │   ├── Player/
   │   └── Managers/
   │
   ├── Prefabs/
   │   ├── NetworkManager.prefab          (NetworkManager component)
   │   └── Player.prefab                   (Player với NetworkObject)
   │
   └── Scenes/
       ├── MainMenu.unity                  (Menu chọn server)
       ├── LoginScene.unity
       └── GameScene.unity                 (Game scene với network)
  ```

#### 4.2.3. NetworkManager Custom Script
- [ ] **Tạo `NetworkManagerCustom.cs`:**
  ```csharp
  using Unity.Netcode;
  using UnityEngine;
  using Unity.Netcode.Transports.UTP;

  public class NetworkManagerCustom : MonoBehaviour
  {
      [Header("Server Config")]
      public string serverIP = "123.45.67.89";
      public ushort serverPort = 7777;

      public void ConnectToServer()
      {
          var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
          transport.ConnectionData.Address = serverIP;
          transport.ConnectionData.Port = serverPort;
          
          if (NetworkManager.Singleton.StartClient())
          {
              Debug.Log($"Connecting to {serverIP}:{serverPort}");
          }
          else
          {
              Debug.LogError("Failed to start client!");
          }
      }

      public void StartHost()
      {
          var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
          transport.ConnectionData.Address = "0.0.0.0";
          transport.ConnectionData.Port = serverPort;
          
          NetworkManager.Singleton.StartHost();
      }

      public void StartServer()
      {
          var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
          transport.ConnectionData.Address = "0.0.0.0";
          transport.ConnectionData.Port = serverPort;
          
          NetworkManager.Singleton.StartServer();
      }
  }
  ```

#### 4.2.4. Player Network Script
- [ ] **Tạo `PlayerNetwork.cs`:**
  ```csharp
  using Unity.Netcode;
  using UnityEngine;

  public class PlayerNetwork : NetworkBehaviour
  {
      // Network Variables - tự động sync
      public NetworkVariable<int> playerID = new NetworkVariable<int>();
      public NetworkVariable<string> playerName = new NetworkVariable<string>();
      public NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();

      public override void OnNetworkSpawn()
      {
          if (IsServer)
          {
              // Server set player ID dựa trên OwnerClientId
              playerID.Value = (int)OwnerClientId;
              
              // Load player name từ database dựa trên user_id
              // playerName.Value = LoadPlayerNameFromDB(userId);
          }

          if (IsOwner)
          {
              // Chỉ owner mới thấy camera này
              SetupLocalPlayer();
          }
      }

      private void SetupLocalPlayer()
      {
          // Setup camera, input, UI cho local player
      }
  }
  ```

#### 4.2.5. Luồng Đăng Nhập & Load Dữ Liệu Chi Tiết

- [ ] **BƯỚC 1: Login (HTTP API)**
  1. Client gửi username/password → `POST /api/auth/login`
  2. Server verify credentials
  3. Server tạo JWT token
  4. Server update `last_login` trong database
  5. Server trả về: `{token, user_id, username}`

- [ ] **BƯỚC 2: Load Player Data (HTTP API)**
  1. Client gửi request với JWT token → `GET /api/player/{playerId}/data`
  2. Server validate JWT token
  3. Server load từ database:
     - Base stats (hp, mp, attack, level, exp, gold...)
     - Hệ/Gene (element_type, gene_tier, is_hybrid, secondary_element)
     - Equipment JSON (trang bị đang mặc)
     - Inventory JSON (túi đồ)
     - Skills JSON
     - Potential stats JSON
  4. **Server tính FINAL STATS:**
     ```
     finalAttack = baseAttack
     finalMaxHp = baseMaxHp
     finalMoveSpeed = baseMoveSpeed
     
     // Cộng stats từ equipment
     foreach (item in equipment) {
         if (item.weapon != null) {
             finalAttack += item.weapon.attack
             finalAttack += item.weapon.fire_damage (nếu có)
         }
         if (item.armor != null) {
             finalMaxHp += item.armor.hp
         }
         if (item.boots != null) {
             finalMoveSpeed += item.boots.move_speed
         }
     }
     
     // Cộng potential stats
     foreach (stat in potential_stats) {
         finalAttack += stat.points (nếu là attack)
     }
     
     // Áp dụng Gene bonuses
     if (gene_tier == 2) finalAttack *= 1.2
     if (gene_tier == 3) finalAttack *= 1.5
     if (is_hybrid) finalAttack *= 1.1
     ```
  5. Server trả về response:
     ```json
     {
       "player_id": 1,
       "base_stats": {
         "hp": 1000,
         "max_hp": 1000,
         "attack": 100
       },
       "equipment": {
         "weapon": {"item_id": 101, "name": "Flame Sword", "attack": 50, "fire_damage": 20},
         "armor": {"item_id": 201, "defense": 30, "hp": 200},
         "pants": null,
         "boots": {"item_id": 301, "move_speed": 1.2}
       },
       "final_stats": {
         "hp": 1200,
         "max_hp": 1200,
         "attack": 180,
         "fire_damage": 20,
         "move_speed": 6.2
       },
       "inventory": [
         {"item_id": 102, "name": "Iron Sword", "quantity": 1, "slot_index": 0},
         {"item_id": 202, "name": "Leather Armor", "quantity": 1, "slot_index": 1}
       ],
       "skills": [...],
       "element_type": "Fire",
       "gene_tier": 2,
       "is_hybrid": false
     }
     ```
  6. Client lưu data vào memory/local storage

- [ ] **BƯỚC 3: Hiển thị UI (Client)**
  1. Client hiển thị Main Menu
  2. Client hiển thị equipment slots (weapon, armor, pants, boots)
  3. Client load model/sprite cho từng item đang mặc:
     - Weapon model → gắn vào player hand
     - Armor sprite → thay đổi player appearance
     - Boots sprite → thay đổi player appearance
  4. Client hiển thị inventory UI với items từ response
  5. Client hiển thị final stats (KHÔNG tự tính, dùng stats từ server)

- [ ] **BƯỚC 4: Connect to Game Server (Netcode)**
  1. User click "Join Game" → Gọi `ConnectToServer()`
  2. Client connect tới `serverIP:serverPort` qua Unity Netcode
  3. Server nhận connection → `OnClientConnected(ulong clientId)`
  4. Server map `clientId` với `user_id` (từ JWT token hoặc session)
  5. Server load player data từ database:
     - Load `map_id` từ player_data
     - Load map info từ bảng `maps`
     - Load enemy_spawns từ bảng `enemy_spawns` WHERE `map_id = X`
  6. Server load map scene (nếu chưa load):
     - Load map prefab/scene
     - Spawn map objects
  7. Server spawn enemies trong map:
     - Với mỗi spawn_id trong enemy_spawns:
       - Check `max_spawn_count` và số lượng hiện tại
       - Spawn enemy tại `spawn_x, spawn_y, spawn_z`
       - Sync enemy qua network
  8. Server spawn player prefab:
     - Tại vị trí `position_x, position_y, position_z` (hoặc spawn point của map nếu position = 0)
  9. Server apply player data:
     - Position (position_x, position_y, position_z)
     - Final stats (đã tính sẵn)
     - Equipment (để sync model)
     - Map ID
  10. Server sync data qua NetworkVariable:
      - Player position, stats, equipment
      - Enemy positions, stats
  11. Client nhận data:
      - Load map scene
      - Hiển thị character với equipment đã mặc
      - Hiển thị enemies đã spawn
  12. Client apply equipment models:
      - Gắn weapon model vào player
      - Thay đổi armor/boots appearance
      - Update UI equipment slots

#### 4.2.6. Server Config File
- [ ] **Tạo `NetworkConfig.cs` hoặc JSON config:**
  ```csharp
  [System.Serializable]
  public class ServerConfig
  {
      public string serverIP = "123.45.67.89";
      public ushort serverPort = 7777;
      public int maxPlayers = 100;
  }
  ```
  
  - Load config từ file JSON hoặc từ API
  - Cho phép thay đổi server IP/Port dễ dàng

### 4.3. Gameplay Features

#### 4.3.1. Equipment System (Equip/Unequip)

- [ ] **Luồng LẮP TRANG BỊ (Equip):**
  1. Player mở Inventory UI
  2. Player click vào item muốn equip
  3. Client gửi request → `POST /api/player/{playerId}/equip`
     ```json
     {
       "item_id": 101,
       "slot": "weapon"
     }
     ```
  4. Server validate:
     - Item có trong inventory không?
     - Slot hợp lệ không? (weapon, armor, pants, boots)
     - Item có thể equip vào slot này không? (check item type)
  5. Server xử lý:
     - Nếu slot đã có item → đưa item cũ về inventory
     - Remove item mới khỏi inventory
     - Update `equipment` JSON trong database
     - Tính lại final stats (base + equipment mới)
     - Save database
  6. Server trả về response:
     ```json
     {
       "success": true,
       "equipment": {...},
       "final_stats": {...},
       "inventory": [...]
     }
     ```
  7. Client nhận response:
     - Update equipment UI (hiển thị item mới trong slot)
     - Update inventory UI (remove item khỏi túi)
     - Update stats UI (hiển thị final stats mới)
     - Gắn weapon model vào player (nếu là weapon)
     - Thay đổi armor/boots appearance (nếu là armor/boots)
     - Sync model qua network (nếu đang trong game)

- [ ] **Luồng THÁO TRANG BỊ (Unequip):**
  1. Player click vào equipment slot đang mặc
  2. Client gửi request → `POST /api/player/{playerId}/unequip`
     ```json
     {
       "slot": "weapon"
     }
     ```
  3. Server validate:
     - Slot có item không?
     - Slot hợp lệ không?
  4. Server xử lý:
     - Remove item khỏi `equipment` JSON
     - Đưa item về `inventory` JSON
     - Tính lại final stats (base stats, không có equipment)
     - Save database
  5. Server trả về response:
     ```json
     {
       "success": true,
       "equipment": {...},
       "final_stats": {...},
       "inventory": [...]
     }
     ```
  6. Client nhận response:
     - Update equipment UI (slot trống)
     - Update inventory UI (thêm item vào túi)
     - Update stats UI (hiển thị final stats mới)
     - Gỡ weapon model khỏi player
     - Reset armor/boots appearance về default

- [ ] **Sync Equipment Model qua Network:**
  - Khi player equip/unequip trong game:
    1. Client gửi request lên server (HTTP API)
    2. Server update database và tính lại stats
    3. Server sync equipment qua NetworkVariable
    4. Tất cả clients nhận equipment update
    5. Tất cả clients update model/appearance của player đó

#### 4.3.2. Hiển thị Nhân Vật
- [ ] **Load Player Model:**
  - Load player model từ Resources hoặc Addressables
  - Apply equipment models:
    - Weapon model → gắn vào player hand/bone
    - Armor sprite/model → thay đổi player appearance
    - Boots sprite/model → thay đổi player appearance
  - Sync player appearance qua network
  - Display player name above character

#### 4.3.3. Gameplay Mechanics
- [ ] **Movement system**
- [ ] **Combat system** (sử dụng final stats đã tính từ server)
- [ ] **Inventory UI** (hiển thị items từ inventory JSON)
- [ ] **Equipment UI** (hiển thị equipment slots)
- [ ] **Skills system**
- [ ] **Level/Experience system**

---

## 📋 GIAI ĐOẠN 5: Game Server - Netcode Integration (Tuần 5-6)

### 5.1. Build Unity Server cho Linux VPS

#### 5.1.1. Cách 1: Dedicated Server Build (Khuyến nghị)
- [ ] **Chuẩn bị Unity Project:**
  - Đảm bảo có scene riêng cho Server (hoặc dùng GameScene)
  - Scene phải có NetworkManager prefab
  - Tắt tất cả UI không cần thiết (server không cần UI)

- [ ] **Build Settings:**
  1. File → Build Settings
  2. Chọn platform: **Linux**
  3. Architecture: **x86_64** (64-bit)
  4. Server Build: ✅ **Bật "Server Build"**
  5. Development Build: Tắt (trừ khi debug)
  6. Click "Build" → Chọn thư mục output
  7. Output: `GameServer.x86_64` + `GameServer_Data/`

- [ ] **Script tự động start server:**
  - Tạo script `ServerBootstrap.cs`:
  ```csharp
  using Unity.Netcode;
  using UnityEngine;

  public class ServerBootstrap : MonoBehaviour
  {
      void Start()
      {
          #if UNITY_SERVER
          // Tự động start server khi build chạy
          var networkManager = NetworkManager.Singleton;
          if (!networkManager.IsServer && !networkManager.IsClient)
          {
              networkManager.StartServer();
              Debug.Log("Server started on port 7777");
          }
          #endif
      }
  }
  ```

#### 5.1.2. Deploy lên Linux VPS
- [ ] **Upload files lên VPS:**
  ```bash
  # Sử dụng SCP hoặc SFTP
  scp -r GameServer.x86_64 GameServer_Data/ user@123.45.67.89:/home/user/gameserver/
  ```

- [ ] **Cấu hình trên VPS:**
  ```bash
  # SSH vào VPS
  ssh user@123.45.67.89

  # Tạo thư mục
  mkdir -p /home/user/gameserver
  cd /home/user/gameserver

  # Set quyền thực thi
  chmod +x GameServer.x86_64

  # Mở firewall port
  sudo ufw allow 7777/tcp
  sudo ufw allow 7777/udp

  # Chạy server
  ./GameServer.x86_64
  ```

- [ ] **Chạy server như service (systemd):**
  - Tạo file `/etc/systemd/system/gameserver.service`:
  ```ini
  [Unit]
  Description=Game Server
  After=network.target

  [Service]
  Type=simple
  User=gameserver
  WorkingDirectory=/home/user/gameserver
  ExecStart=/home/user/gameserver/GameServer.x86_64
  Restart=always
  RestartSec=10

  [Install]
  WantedBy=multi-user.target
  ```

  - Enable và start service:
  ```bash
  sudo systemctl enable gameserver
  sudo systemctl start gameserver
  sudo systemctl status gameserver
  ```

#### 5.1.3. Cách 2: Host từ Unity Editor (Chỉ để test)
- [ ] **Test local:**
  - Trong Unity Editor: NetworkManager → Start Host
  - Chỉ dùng để test, KHÔNG dùng cho production
  - Client có thể connect tới `localhost:7777` hoặc `127.0.0.1:7777`

### 5.2. Server Authoritative Logic

#### 5.2.1. Player Identification
- [ ] **Nhận diện Player:**
  - Server sử dụng `OwnerClientId` để nhận diện client
  - Map `OwnerClientId` với `user_id` từ database
  - Lưu mapping trong Dictionary: `Dictionary<ulong, int>` (ClientId → user_id)

- [ ] **Script `GameServerManager.cs`:**
  ```csharp
  using Unity.Netcode;
  using System.Collections.Generic;
  using UnityEngine;

  public class GameServerManager : NetworkBehaviour
  {
      // Map ClientId -> user_id từ database
      private Dictionary<ulong, int> clientToUserId = new Dictionary<ulong, int>();

      public override void OnNetworkSpawn()
      {
          if (IsServer)
          {
              NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
              NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
          }
      }

      private void OnClientConnected(ulong clientId)
      {
          Debug.Log($"Client {clientId} connected");
          
          // TODO: Authenticate client với JWT token
          // Load user_id từ database
          // clientToUserId[clientId] = userId;
          
          // Spawn player và load data
          SpawnPlayerForClient(clientId);
      }

      private void OnClientDisconnected(ulong clientId)
      {
          Debug.Log($"Client {clientId} disconnected");
          
          // Save player data trước khi disconnect
          if (clientToUserId.ContainsKey(clientId))
          {
              SavePlayerData(clientToUserId[clientId]);
          }
          
          clientToUserId.Remove(clientId);
      }

      private void SpawnPlayerForClient(ulong clientId)
      {
          // Spawn player prefab cho client này
          // Load position, stats từ database
      }
  }
  ```

#### 5.2.2. Server Tính Final Stats (QUAN TRỌNG)

- [ ] **Quy tắc VÀNG: Client KHÔNG được tự tính stats**
  - Client chỉ hiển thị stats từ server
  - Server là source of truth cho tất cả calculations

- [ ] **Luồng tính Final Stats trên Server:**
  1. Load base stats từ database (hp, mp, attack, move_speed...)
  2. Load equipment JSON từ database
  3. Load potential_stats JSON từ database
  4. Tính final stats:
     ```
     // Bước 1: Base stats
     finalAttack = baseAttack
     finalMaxHp = baseMaxHp
     finalMaxMp = baseMaxMp
     finalMoveSpeed = baseMoveSpeed
     finalFireDamage = 0
     finalIceDamage = 0
     // ... các stats khác
     
     // Bước 2: Cộng stats từ Equipment
     if (equipment.weapon != null) {
         finalAttack += equipment.weapon.attack
         finalFireDamage += equipment.weapon.fire_damage (nếu có)
         finalIceDamage += equipment.weapon.ice_damage (nếu có)
     }
     if (equipment.armor != null) {
         finalMaxHp += equipment.armor.hp
         finalMaxMp += equipment.armor.mp (nếu có)
     }
     if (equipment.pants != null) {
         finalMaxHp += equipment.pants.hp (nếu có)
     }
     if (equipment.boots != null) {
         finalMoveSpeed += equipment.boots.move_speed
     }
     
     // Bước 3: Cộng Potential Stats
     foreach (stat in potential_stats) {
         if (stat.stat_name == "attack") {
             finalAttack += stat.points
         }
         if (stat.stat_name == "max_hp") {
             finalMaxHp += stat.points
         }
         // ... các stats khác
     }
     
     // Bước 4: Áp dụng Gene Bonuses
     if (gene_tier == 2) {
         finalAttack *= 1.2  // +20% attack
         finalMaxHp *= 1.15   // +15% HP
     }
     if (gene_tier == 3) {
         finalAttack *= 1.5  // +50% attack
         finalMaxHp *= 1.3   // +30% HP
     }
     if (is_hybrid) {
         finalAttack *= 1.1  // +10% attack bonus
         finalMaxHp *= 1.05  // +5% HP bonus
     }
     
     // Bước 5: Cộng Skill Bonuses (nếu skills có passive tăng stats)
     Load skills JSON từ database
     foreach (skill in skills) {
         if (skill.unlocked == true) {
             // Ví dụ: Skill "Power Boost" level 5 → +10% attack
             if (skill.skill_id == 101 && skill.level >= 5) {
                 finalAttack *= 1.1
             }
             // Ví dụ: Skill "Health Mastery" level 3 → +15% HP
             if (skill.skill_id == 102 && skill.level >= 3) {
                 finalMaxHp *= 1.15
             }
             // ... các skill bonuses khác
         }
     }
     
     // Bước 6: Final stats đã tính xong
     // Trả về cho client hoặc lưu vào memory
     ```
  5. Server trả về final stats cho client
  6. Client chỉ hiển thị, KHÔNG tự tính lại

- [ ] **Khi nào cần tính lại stats:**
  - Khi player login → Tính 1 lần và trả về
  - Khi player equip item → Tính lại và sync
  - Khi player unequip item → Tính lại và sync
  - Khi player level up → Tính lại (base stats thay đổi)
  - Khi player thay đổi potential stats → Tính lại
  - Khi player unlock skill → Tính lại (nếu skill có passive bonus)
  - Khi player upgrade skill → Tính lại (nếu skill level ảnh hưởng stats)
  - Khi player đổi hệ/gene → Tính lại (gene bonuses thay đổi)

#### 5.2.3. Server RPC Pattern
- [ ] **Ví dụ: Take Damage (Server Authoritative):**
  ```csharp
  using Unity.Netcode;
  using UnityEngine;

  public class PlayerCombat : NetworkBehaviour
  {
      public NetworkVariable<int> hp = new NetworkVariable<int>(100);
      public NetworkVariable<int> maxHp = new NetworkVariable<int>(100);

      // Client gọi Server RPC
      [ServerRpc]
      public void TakeDamageServerRpc(int damage)
      {
          if (!IsServer) return;

          // Chỉ server mới được thay đổi HP
          hp.Value = Mathf.Max(0, hp.Value - damage);
          
          Debug.Log($"Player {OwnerClientId} took {damage} damage. HP: {hp.Value}");

          // Nếu HP = 0, xử lý death
          if (hp.Value <= 0)
          {
              OnDeath();
          }
      }

      // Client gọi để heal
      [ServerRpc]
      public void HealServerRpc(int amount)
      {
          if (!IsServer) return;
          
          hp.Value = Mathf.Min(maxHp.Value, hp.Value + amount);
      }

      private void OnDeath()
      {
          // Xử lý khi player chết
          // Respawn, save stats, etc.
      }
  }
  ```

#### 5.2.3. Movement Validation
- [ ] **Validate player movement trên server:**
  ```csharp
  public class PlayerMovement : NetworkBehaviour
  {
      public NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();
      
      private float maxMoveSpeed = 10f;
      private float lastPositionTime;
      private Vector3 lastPosition;

      [ServerRpc]
      public void MoveServerRpc(Vector3 newPosition)
      {
          if (!IsServer) return;

          // Validate movement
          float distance = Vector3.Distance(lastPosition, newPosition);
          float timeDelta = Time.time - lastPositionTime;
          
          // Check speed hack
          float speed = distance / timeDelta;
          if (speed > maxMoveSpeed * 1.5f) // Cho phép 50% tolerance
          {
              Debug.LogWarning($"Player {OwnerClientId} moving too fast! Speed: {speed}");
              // Reject movement hoặc teleport về lastPosition
              return;
          }

          // Accept movement
          networkPosition.Value = newPosition;
          lastPosition = newPosition;
          lastPositionTime = Time.time;
      }
      
      void Update()
      {
          if (IsOwner)
          {
              // Local player: gửi position lên server
              Vector3 currentPos = transform.position;
              MoveServerRpc(currentPos);
          }
          else
          {
              // Remote players: sync từ networkPosition
              transform.position = networkPosition.Value;
          }
      }
  }
  ```

#### 5.2.4. Combat System với HỆ/GENE (Server Authoritative)

- [ ] **Luồng tính sát thương với HỆ (Sử dụng Final Stats):**
  1. Client gửi attack request lên Server (chỉ gửi input, không tính damage)
  2. Server load player data từ database hoặc memory:
     - Final stats (đã tính sẵn từ base + equipment + potential + gene)
     - `element_type`, `gene_tier`, `is_hybrid`, `secondary_element`
  3. Server tính base damage từ `finalAttack` (đã bao gồm equipment, potential, gene bonuses)
  4. Server check tương khắc dựa trên `element_type`:
     - Kim khắc Mộc → x1.5 damage
     - Mộc khắc Thủy → x1.5 damage
     - Thủy khắc Hỏa → x1.5 damage
     - Hỏa khắc Kim → x1.5 damage
     - Thổ khắc tất cả → x1.2 damage
     - Bị khắc → x0.7 damage
  5. Nếu `is_hybrid = true` → thêm bonus 10% damage (bonus này đã được tính trong final stats, nhưng có thể thêm bonus riêng cho combat)
  6. Cộng thêm elemental damage từ equipment (finalFireDamage, finalIceDamage...)
  7. Server tính final damage và apply vào target
  8. Server sync HP về client

- [ ] **Lưu ý quan trọng:**
  - Server sử dụng `finalAttack` (đã tính sẵn), KHÔNG tính lại từ base
  - Equipment stats đã được cộng vào final stats rồi
  - Gene bonuses đã được áp dụng trong final stats rồi
  - Chỉ cần check tương khắc và cộng elemental damage từ equipment

- [ ] **Validation & Anti-cheat:**
  - Server KHÔNG tin client về hệ
  - Server đọc `element_type` từ database mỗi lần combat
  - Client không thể fake hệ vì server là source of truth
  - Validate gene_tier hợp lệ (1-3)

- [ ] **Fusion Gene Logic:**
  - Khi `is_hybrid = true` và có `secondary_element`
  - Server tính damage dựa trên cả 2 hệ
  - Áp dụng bonus hybrid (10-20% tùy tier)
  - Có thể có passive riêng cho hybrid

- [ ] **Arena/Raid/Boss đổi hệ:**
  - Server lưu hệ hiện tại của player trong memory
  - Khi vào Arena/Raid, server check `element_type` từ DB
  - Boss có thể đổi hệ → server tính lại tương khắc
  - Client chỉ hiển thị, không quyết định

#### 5.2.5. Data Synchronization
- [ ] **Sync Player Stats:**
  - Sử dụng `NetworkVariable<T>` cho các giá trị cần sync
  - Server là source of truth
  - Client chỉ đọc, không được ghi trực tiếp
  - `element_type`, `gene_tier` được sync từ DB → Server → Client

- [ ] **Periodic Save:**
  - Auto-save mỗi 5 phút
  - Save khi player disconnect
  - Save khi có thay đổi quan trọng (level up, item acquired, đổi hệ)
  - Save khi player đổi map (update map_id, position)

#### 5.2.6. Map System (Server Authoritative)

- [ ] **Luồng Player vào Map:**
  1. Client gửi request → `POST /api/player/{playerId}/change-map`
     ```json
     {
       "map_id": 1,
       "position_x": 0,
       "position_y": 0,
       "position_z": 0
     }
     ```
  2. Server validate:
     - Map có tồn tại không?
     - Player đủ level để vào map không? (check min_level, max_level)
     - Player có quyền vào map không? (nếu có điều kiện đặc biệt)
  3. Server update database:
     - Update `map_id` trong player_data
     - Update `position_x, position_y, position_z`
     - Save database
  4. Server trả về map info:
     ```json
     {
       "map_id": 1,
       "map_name": "Forest Field",
       "spawn_x": 0,
       "spawn_y": 0,
       "spawn_z": 0,
       "enemy_spawns": [...]
     }
     ```
  5. Client load map scene và spawn player tại vị trí

- [ ] **Server quản lý Map:**
  - Server load danh sách enemy_spawns khi map được load
  - Server spawn enemies dựa trên spawn_id
  - Server track số lượng enemies đang sống trong map
  - Server quản lý respawn timer

#### 5.2.7. Enemy Spawn System (Server Authoritative)

- [ ] **Luồng Server Spawn Enemy:**
  1. Khi map được load hoặc player vào map:
     - Server query `enemy_spawns` WHERE `map_id = X` AND `is_active = true`
     - Server lấy danh sách spawn points
  2. Server spawn enemy:
     - Với mỗi spawn_id:
       - Check `max_spawn_count` (số lượng tối đa)
       - Check số lượng enemy hiện tại trong map
       - Nếu chưa đạt max → Spawn enemy
       - Tạo enemy với:
         - `enemy_type` từ database
         - `enemy_level` từ database
         - Position: `spawn_x, spawn_y, spawn_z` (hoặc random trong `spawn_radius`)
         - Gán `spawn_id` để track
  3. Server sync enemy qua network:
     - Spawn enemy prefab với NetworkObject
     - Sync position, rotation
     - Sync enemy stats (HP, level, type)
  4. Client nhận enemy spawn → Hiển thị enemy model

- [ ] **Enemy Respawn Logic:**
  1. Khi enemy chết:
     - Server mark enemy là "dead"
     - Server lưu thời gian chết
     - Server despawn enemy (destroy NetworkObject)
  2. Server check respawn timer:
     - Mỗi frame/update, server check `respawn_time`
     - Nếu đã đủ thời gian → Spawn lại enemy tại spawn_id đó
     - Check `max_spawn_count` trước khi spawn

- [ ] **Validation & Anti-cheat:**
  - Client KHÔNG được spawn enemy
  - Client KHÔNG được thay đổi enemy stats
  - Server là source of truth cho tất cả enemy data
  - Server validate enemy spawn dựa trên database

#### 5.2.8. Loot Drop System (Server Authoritative)

- [ ] **Luồng Enemy chết và rơi đồ:**
  1. Client gửi attack → Server tính damage
  2. Server check enemy HP:
     - Nếu HP <= 0 → Enemy chết
  3. Server xử lý loot drop:
     - Query `loot_table` WHERE `enemy_type = X`
     - Với mỗi item trong loot_table:
       - Nếu `is_guaranteed = true` → Rơi 100%
       - Nếu không → Random với `drop_rate`
       - Nếu rơi → Random `quantity` từ `min_quantity` đến `max_quantity`
     - Tính gold: Random từ `gold_min` đến `gold_max`
     - Tính EXP: `exp_reward`
  4. Server tạo loot items:
     - Tạo item objects trên map tại vị trí enemy chết
     - Mỗi item có NetworkObject để sync
     - Item có `item_id`, `quantity`, `position`
  5. Server sync loot qua network:
     - Spawn item prefab với NetworkObject
     - Client nhận → Hiển thị item trên ground
  6. Server xử lý khi player nhặt item:
     - Client gửi request → `POST /api/player/{playerId}/pickup-item`
       ```json
       {
         "item_id": 101,
         "position_x": 10,
         "position_y": 0,
         "position_z": 5
       }
     ```
     - Server validate:
       - Item có tồn tại tại vị trí đó không?
       - Player có đủ chỗ trong inventory không?
       - Check distance (player phải gần item mới nhặt được)
     - Server xử lý:
       - Thêm item vào `inventory` JSON
       - Despawn item (destroy NetworkObject)
       - Update database
     - Server trả về updated inventory

#### 5.2.9. EXP & Level Up System (Server Authoritative)

- [ ] **Luồng Cộng EXP khi đánh quái:**
  1. Khi enemy chết (HP <= 0):
     - Server query `loot_table` WHERE `enemy_type = X`
     - Server lấy `exp_reward` từ loot_table
     - Server xác định player nào giết enemy (dựa trên last damage dealer)
  2. Server cộng EXP:
     - Cộng `exp_reward` vào `experience` của player
     - Update `experience` trong database
  3. Server check level up:
     - Query `exp_requirements` WHERE `level = current_level + 1`
     - Lấy `exp_required` cho level tiếp theo
     - Nếu `experience >= exp_required` → Gọi hàm Level Up
  4. Server sync EXP qua network:
     - Update NetworkVariable cho `experience`
     - Client nhận → Update EXP bar UI

- [ ] **Luồng Level Up (Chi tiết):**
  1. Server xác định level mới:
     - `new_level = current_level + 1`
     - `remaining_exp = experience - exp_required`
  2. Server load exp_requirements cho level mới:
     - Lấy `base_stat_increase` (JSON)
     - Lấy `skill_points`
     - Lấy `potential_points`
  3. Server cập nhật base stats:
     - `baseMaxHp += base_stat_increase.hp`
     - `baseMaxMp += base_stat_increase.mp`
     - `baseAttack += base_stat_increase.attack`
     - Update các stats khác nếu có
  4. Server cập nhật level và EXP:
     - `level = new_level`
     - `experience = remaining_exp` (EXP dư chuyển sang level mới)
  5. Server cộng skill points và potential points:
     - Cộng `skill_points` vào skill points available (có thể lưu trong player_data hoặc tính từ level)
     - Cộng `potential_points` vào potential points available
  6. Server tính lại final stats:
     - Tính lại từ base stats mới + equipment + potential + gene
  7. Server save database:
     - Update `level`, `experience`, base stats
     - Save database
  8. Server sync qua network:
     - Update NetworkVariable cho `level`, `experience`, stats
     - Gửi notification "Level Up!" cho client
  9. Client nhận:
     - Hiển thị level up animation/UI
     - Update level, EXP bar
     - Update stats UI
     - Hiển thị skill points và potential points mới

- [ ] **Multi-level Up (Nếu EXP đủ lên nhiều level):**
  1. Server check level up liên tục:
     - Sau khi level up, check lại EXP có đủ lên level tiếp theo không
     - Nếu có → Level up tiếp
     - Lặp lại cho đến khi EXP không đủ
  2. Server tính tổng stats increase:
     - Cộng tất cả `base_stat_increase` từ các level đã lên
     - Cộng tất cả `skill_points` và `potential_points`
  3. Server apply một lần:
     - Update base stats với tổng increase
     - Update level với level cuối cùng
     - Update EXP với EXP còn lại

#### 5.2.10. Potential Stats System (Cộng Chỉ Số - Server Authoritative)

- [ ] **Luồng Cộng Chỉ Số cho Player:**
  1. Player có potential points (từ level up hoặc items)
  2. Client gửi request → `POST /api/player/{playerId}/add-potential-stat`
     ```json
     {
       "stat_name": "attack",
       "points": 5
     }
     ```
  3. Server validate:
     - Player có đủ potential points không?
     - Stat name hợp lệ không? (attack, max_hp, max_mp, move_speed...)
     - Points > 0 và <= potential points available
  4. Server xử lý:
     - Load `potential_stats` JSON từ database
     - Tìm stat trong potential_stats:
       - Nếu đã có → Cộng thêm points
       - Nếu chưa có → Thêm mới vào array
     - Trừ potential points available
     - Update `potential_stats` JSON trong database
  5. Server tính lại final stats:
     - Tính lại từ base + equipment + potential (mới) + gene
  6. Server save database:
     - Update `potential_stats` JSON
     - Save database
  7. Server trả về:
     ```json
     {
       "success": true,
       "potential_stats": [...],
       "final_stats": {...},
       "potential_points_remaining": 10
     }
     ```
  8. Client nhận:
     - Update potential stats UI
     - Update final stats UI
     - Update potential points UI

- [ ] **Luồng Reset Potential Stats (Nếu có):**
  1. Client gửi request → `POST /api/player/{playerId}/reset-potential-stats`
  2. Server validate:
     - Player có đủ item/currency để reset không?
  3. Server xử lý:
     - Reset `potential_stats` JSON về rỗng
     - Hoàn lại tất cả potential points đã dùng
     - Tính lại final stats
     - Save database
  4. Server trả về updated stats

#### 5.2.11. Skills System (Unlock & Upgrade Skills - Server Authoritative)

- [ ] **Luồng Unlock Skill:**
  1. Player có skill points (từ level up)
  2. Client gửi request → `POST /api/player/{playerId}/unlock-skill`
     ```json
     {
       "skill_id": 101
     }
     ```
  3. Server validate:
     - Skill có tồn tại không?
     - Player đủ level để unlock skill không?
     - Player có đủ skill points không?
     - Skill đã được unlock chưa?
  4. Server xử lý:
     - Load `skills` JSON từ database
     - Thêm skill mới vào array:
       ```json
       {
         "skill_id": 101,
         "skill_name": "Fireball",
         "level": 1,
         "unlocked": true
       }
       ```
     - Trừ skill points
     - Update `skills` JSON trong database
  5. Server save database:
     - Update `skills` JSON
     - Save database
  6. Server trả về:
     ```json
     {
       "success": true,
       "skills": [...],
       "skill_points_remaining": 5
     }
     ```
  7. Client nhận:
     - Update skills UI (unlock skill)
     - Update skill points UI
     - Hiển thị skill trong skill bar

- [ ] **Luồng Upgrade Skill (Nâng cấp skill):**
  1. Client gửi request → `POST /api/player/{playerId}/upgrade-skill`
     ```json
     {
       "skill_id": 101
     }
     ```
  2. Server validate:
     - Skill đã được unlock chưa?
     - Skill chưa đạt level max chưa?
     - Player có đủ skill points không?
  3. Server xử lý:
     - Load `skills` JSON
     - Tìm skill trong array
     - Tăng `level` lên 1
     - Trừ skill points
     - Update `skills` JSON
  4. Server save database:
     - Update `skills` JSON
     - Save database
  5. Server trả về updated skills
  6. Client nhận:
     - Update skill level trong UI
     - Update skill damage/effect (nếu có)

- [ ] **Validation & Anti-cheat:**
  - Client KHÔNG được tự unlock/upgrade skill
  - Client KHÔNG được tự cộng potential stats
  - Server validate tất cả requests
  - Server check skill points và potential points từ database
  - Server là source of truth cho tất cả calculations

- [ ] **Validation & Anti-cheat:**
  - Client KHÔNG được tự tạo item
  - Client KHÔNG được tự cộng gold/EXP
  - Server validate tất cả loot drops
  - Server check distance khi player nhặt item
  - Server validate item có thực sự rơi từ enemy không

### 5.3. Testing & Troubleshooting

#### 5.3.1. Test Connection Local
- [ ] **Test trên cùng máy:**
  - Start server: `NetworkManager.Singleton.StartHost()`
  - Client connect tới: `localhost:7777` hoặc `127.0.0.1:7777`
  - Kiểm tra log xem có kết nối thành công không

#### 5.3.2. Test Connection từ xa
- [ ] **Test từ máy khác:**
  - Đảm bảo VPS đã mở port 7777 (TCP + UDP)
  - Client connect tới IP public của VPS
  - Kiểm tra firewall rules trên VPS

#### 5.3.3. Debug Tools
- [ ] **Network Debug UI:**
  - Enable NetworkManager → Enable Runtime Network Statistics
  - Hiển thị ping, packet loss, RTT
  - Monitor số lượng clients connected

- [ ] **Logging:**
  ```csharp
  // Client side
  NetworkManager.Singleton.OnClientConnectedCallback += (clientId) => {
      Debug.Log($"Connected to server! ClientId: {clientId}");
  };
  
  NetworkManager.Singleton.OnClientDisconnectCallback += (clientId) => {
      Debug.Log($"Disconnected from server! ClientId: {clientId}");
  };
  
  // Server side
  NetworkManager.Singleton.OnClientConnectedCallback += (clientId) => {
      Debug.Log($"Client {clientId} connected to server");
  };
  ```

#### 5.3.4. Common Issues & Solutions
- [ ] **Không connect được:**
  - Kiểm tra firewall: `sudo ufw status`
  - Kiểm tra port có đang listen: `netstat -tulpn | grep 7777`
  - Kiểm tra IP address có đúng không
  - Kiểm tra Unity Transport settings

- [ ] **Connection timeout:**
  - Tăng timeout trong Unity Transport
  - Kiểm tra network latency
  - Kiểm tra server có đang chạy không

- [ ] **Player không spawn:**
  - Kiểm tra Network Prefabs đã được add vào NetworkManager chưa
  - Kiểm tra Player Prefab có NetworkObject component chưa
  - Kiểm tra spawn logic trên server

---

## 📋 GIAI ĐOẠN 6: Testing & Optimization (Tuần 6-7)

### 6.1. Testing
- [ ] **Unit Tests**
  - Test authentication APIs
  - Test database operations
  - Test data validation

- [ ] **Integration Tests**
  - Test full login flow
  - Test save/load player data
  - Test network connection

- [ ] **Load Testing**
  - Test với nhiều players đồng thời
  - Test server performance
  - Optimize database queries

### 6.2. Security
- [ ] **Security Measures**
  - Validate tất cả inputs từ client
  - Rate limiting cho APIs
  - SQL injection prevention
  - XSS prevention
  - Secure password storage
  - HTTPS cho HTTP APIs

### 6.3. Optimization
- [ ] **Performance**
  - Optimize database queries (indexes)
  - Connection pooling
  - Caching frequently accessed data
  - Network message optimization
  - Client-side prediction (nếu cần)

---

## 📋 GIAI ĐOẠN 7: Deployment (Tuần 7-8)

### 7.1. Server Deployment
- [ ] **Server Setup**
  - Deploy Game Server lên cloud (AWS, Azure, DigitalOcean)
  - Setup database server
  - Configure firewall rules
  - Setup SSL certificates

### 7.2. Client Build
- [ ] **Build Unity Client**
  - Build PC version (Windows/Mac/Linux)
  - Build Android APK
  - Test builds trên các platforms
  - Setup build pipeline (CI/CD nếu có)

### 7.3. Monitoring & Logging
- [ ] **Monitoring Setup**
  - Setup logging system
  - Monitor server performance
  - Monitor database performance
  - Error tracking (Sentry, etc.)

---

## 🛠️ Công Cụ & Technologies

### Backend
- **.NET 6/7/8** - Server framework
- **Entity Framework Core** - ORM
- **MySQL/SQL Server** - Database
- **ASP.NET Core** - HTTP API
- **Unity Netcode for GameObjects** - Multiplayer networking
- **JWT** - Authentication

### Frontend (Unity)
- **Unity 2021.3+ LTS**
- **Unity Netcode for GameObjects**
- **UnityWebRequest** - HTTP client
- **UI Toolkit hoặc uGUI** - UI system

### DevOps
- **Git** - Version control
- **Docker** (optional) - Containerization
- **Cloud Platform** (AWS/Azure/GCP) - Hosting

---

## 📝 Notes

- **Server Authoritative**: Tất cả game logic quan trọng phải được xử lý trên server để tránh cheating
- **HỆ/GENE phải lưu trong DB**: BẮT BUỘC phải có `element_type`, `gene_tier`, `is_hybrid` trong database. Server đọc từ DB để tính sát thương và tương khắc, KHÔNG tin client. Chỉ lưu `fire_damage`, `ice_damage`... là KHÔNG ĐỦ vì đó chỉ là stat từ items, không nói được player thuộc hệ nào.
- **Security First**: Luôn validate data từ client, không tin tưởng client
- **Scalability**: Thiết kế hệ thống có thể scale khi số lượng players tăng
- **Error Handling**: Xử lý lỗi một cách graceful, có error messages rõ ràng
- **Documentation**: Document APIs và code để dễ maintain

---

## ✅ Checklist Tổng Quan

- [ ] Database schema hoàn chỉnh
- [ ] Authentication system hoạt động
- [ ] Player data save/load hoạt động
- [ ] Unity client có thể login/register
- [ ] Network connection ổn định
- [ ] Player spawn và sync hoạt động
- [ ] Gameplay cơ bản hoạt động
- [ ] Security measures được implement
- [ ] Testing hoàn tất
- [ ] Deployment thành công