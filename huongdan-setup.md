# 📖 HƯỚNG DẪN LOGIN / LOGOUT + LƯU DB (LOCALHOST, ĐÃ CÓ NETCODE)

## 🎯 Bối cảnh hiện tại
- Bạn đã có project Unity Client ở thư mục `Client/` và đã setup **Netcode** + đồng bộ Client/Server (movement, spawn, v.v.).  
- Giờ chỉ còn thiếu **luồng Login / Logout + lưu dữ liệu lên SQL** và **cách chạy API + chạy nhiều client (ParrelSync)** để test.

Mục tiêu của file này:
- **(1)** Tạo và chạy SQL Database đúng schema.
- **(2)** Tạo và chạy **Game Server API (.NET)** ở localhost.
- **(3)** Kết nối Unity Client (Login, Logout) với API đó.
- **(4)** Dùng **ParrelSync** để chạy nhiều client, test login + save DB.

---

## 1️⃣ SETUP SQL DATABASE

### 1.1. Chọn loại DB
- Bạn có thể dùng **SQL Server** hoặc **MySQL**.  
Để dễ cho .NET + Windows, gợi ý dùng **SQL Server** (SQL Server Express + SQL Server Management Studio).

### 1.2. Tạo Database
1. Mở **SSMS** → Connect đến SQL Server.
2. Right-click `Databases` → **New Database…**
3. Đặt tên ví dụ: `GameDB` → OK.

### 1.3. Tạo các bảng chính (rút gọn theo `luuthongtin.md`)
> Bạn có thể chạy script SQL này trong SSMS (New Query → Paste → Execute).

```sql
-- Bảng users
CREATE TABLE users (
    user_id INT IDENTITY(1,1) PRIMARY KEY,
    username VARCHAR(50) UNIQUE NOT NULL,
    email VARCHAR(100) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    last_login DATETIME NULL
);

-- Bảng player_data (1-1 với users)
CREATE TABLE player_data (
    player_id INT PRIMARY KEY FOREIGN KEY REFERENCES users(user_id),
    level INT NOT NULL DEFAULT 1,
    experience INT NOT NULL DEFAULT 0,
    gold INT NOT NULL DEFAULT 0,

    map_id INT NOT NULL DEFAULT 1,
    position_x FLOAT NOT NULL DEFAULT 0,
    position_y FLOAT NOT NULL DEFAULT 0,
    position_z FLOAT NOT NULL DEFAULT 0,

    hp INT NOT NULL DEFAULT 100,
    max_hp INT NOT NULL DEFAULT 100,
    mp INT NOT NULL DEFAULT 50,
    max_mp INT NOT NULL DEFAULT 50,
    attack INT NOT NULL DEFAULT 10,

    element_type VARCHAR(10) NOT NULL,      -- Metal / Wood / Water / Fire / Earth
    gene_tier TINYINT NOT NULL DEFAULT 1,
    is_hybrid BIT NOT NULL DEFAULT 0,
    secondary_element VARCHAR(10) NULL,

    equipment NVARCHAR(MAX) NULL,      -- JSON
    skills NVARCHAR(MAX) NULL,         -- JSON
    inventory NVARCHAR(MAX) NULL,      -- JSON
    potential_stats NVARCHAR(MAX) NULL,-- JSON

    updated_at DATETIME NOT NULL DEFAULT GETDATE()
);

-- Bảng exp_requirements (EXP để lên level)
CREATE TABLE exp_requirements (
    level INT PRIMARY KEY,
    exp_required INT NOT NULL,
    base_stat_increase NVARCHAR(MAX) NULL, -- JSON: {\"hp\":50,\"mp\":30,\"attack\":10}
    skill_points INT NOT NULL DEFAULT 0,
    potential_points INT NOT NULL DEFAULT 0,
    created_at DATETIME NOT NULL DEFAULT GETDATE()
);

-- Seed ví dụ level 1-3
INSERT INTO exp_requirements (level, exp_required, base_stat_increase, skill_points, potential_points)
VALUES
(1, 0,   '{\"hp\":0,\"mp\":0,\"attack\":0}', 0, 0),
(2, 100, '{\"hp\":50,\"mp\":20,\"attack\":5}', 1, 2),
(3, 300, '{\"hp\":80,\"mp\":30,\"attack\":10}', 1, 2);
```

> Sau này bạn có thể thêm `maps`, `enemy_spawns`, `loot_table`… theo `luuthongtin.md`, nhưng để login / lưu player thì 3 bảng trên là đủ.

---

## 2️⃣ TẠO GAME SERVER API (.NET) – LOGIN / REGISTER / PLAYER DATA

### 2.1. Tạo project ASP.NET Core Web API
Trong thư mục server (không phải Client), chạy:

```bash
dotnet new webapi -n GameServerApi
cd GameServerApi
```

### 2.2. Thêm Entity Framework Core + SQL Server
```bash
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

### 2.3. Cấu hình connection string
Mở `appsettings.json` và thêm:

```json
\"ConnectionStrings\": {
  \"GameDB\": \"Server=localhost;Database=GameDB;Trusted_Connection=True;TrustServerCertificate=True\"
}
```

> Nếu dùng SQL Auth:  
`\"Server=localhost;Database=GameDB;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True\"`

### 2.4. Tạo DbContext + Models (ý tưởng)
- `Models/User.cs` – map bảng `users`
- `Models/PlayerData.cs` – map bảng `player_data`
- `Models/ExpRequirement.cs` – map bảng `exp_requirements`
- `Data/GameDbContext.cs` – kế thừa `DbContext`, dùng connection string `GameDB`.

### 2.5. Tạo AuthController & PlayerController (theo `luuthongtin.md`)
**Bắt buộc có các endpoint:**
- `POST /api/auth/register` – tạo users + player_data trống hoặc chờ chọn hệ.
- `POST /api/auth/login` – verify account → trả JWT + user_id.
- `POST /api/player/create` – chọn hệ lần đầu → tạo record `player_data` với element_type, stats base.
- `GET  /api/player/{playerId}/data` – trả full player_data (stats + equipment + inventory + skills).

> Toàn bộ logic đã được mô tả chi tiết trong `luuthongtin.md` (phần Backend API). Ở đây bạn chỉ cần implement giống y như mô tả đó.

### 2.6. Chạy API server
Trong thư mục `GameServerApi`:

```bash
dotnet run
```

Mặc định API sẽ chạy ở `http://localhost:5000` (hoặc  http://localhost:5xxx – xem log console).  

> Đảm bảo base URL trong `APIClient.cs` của Unity là `http://localhost:5000/api`.

---

## 3️⃣ KẾT NỐI UNITY CLIENT VỚI API (LOGIN / LOGOUT)

Project Unity Client của bạn đã có:
- `APIClient.cs`
- `LoginController.cs`
- `RegisterController.cs`
- `MainMenuController.cs`
- `SelectElementController.cs`
- `GameManager.cs`

Giờ chỉ cần **gắn script đúng chỗ + set Base URL + test**.

### 3.1. Cấu hình APIClient trong Client/Assets
1. Mở project `Client` bằng Unity.
2. Mở **scene đầu tiên** (thường là `Login` hoặc `Bootstrap`).
3. Tạo GameObject `APIClient` (nếu chưa có):
   - `Create Empty` → đặt tên `APIClient`.
   - Add Component: `APIClient`.
4. Trong Inspector của `APIClient`:
   - **Base URL**: `http://localhost:5000/api`

### 3.2. Gắn LoginController
Trong scene `Login` (bạn có thể đã có):
- Đảm bảo có:
  - `UsernameInput` (InputField / TMP_InputField)
  - `PasswordInput`
  - `LoginButton`
  - `RegisterButton`
  - `ErrorText`
- Tạo GameObject `LoginController`:
  - Add Component: `LoginController`.
  - Kéo các UI vào field tương ứng trong Inspector.

**Luồng:**  
- Khi bấm **Login**:
  - `LoginController` → gọi `APIClient.Login()`
  - Nếu thành công: lưu `USER_ID` + token → gọi `APIClient.LoadPlayerData()`
  - Nếu có player_data → lưu vào `GameManager` → `SceneManager.LoadScene("MainMenu")`
  - Nếu chưa có player_data → `SceneManager.LoadScene("SelectElement")`

### 3.3. Gắn RegisterController
Trong scene `Register`:
- Các Input: Username, Email, Password, ConfirmPassword.
- GameObject `RegisterController`:
  - Add Component `RegisterController`.
  - Kéo UI vào fields.

**Luồng:**  
- Bấm **Register** → `APIClient.Register()` → nếu OK → chờ 2s → quay lại `Login`.

### 3.4. Gắn SelectElementController (chọn hệ)
Trong scene `SelectElement`:
- 5 button: Metal, Wood, Water, Fire, Earth.
- Text hiển thị lỗi.
- GameObject `SelectElementController`:
  - Add Component.
  - Gán các Button vào fields.

**Luồng:**  
- Bấm button hệ → `APIClient.CreatePlayer(elementType)` → server tạo `player_data` → trả `PlayerDataResponse` → lưu vào `GameManager` → `LoadScene("MainMenu")`.

### 3.5. Gắn MainMenuController (Join Game + Logout)
Trong scene `MainMenu`:
- Button `JoinGameButton`, `LogoutButton`.
- Text `PlayerInfoText`.
- GameObject `MainMenuController`:
  - Add Component `MainMenuController`.
  - Gán các UI vào fields.

**Luồng Login / Logout:**
- **Sau Login thành công** → luôn đi qua `MainMenu`.
- Button **Join Game**:
  - Gọi `NetworkManagerCustom.ConnectToServer()` (localhost:2003).
  - Sau 1s `Invoke` → `SceneManager.LoadScene("GameScene")`.
- Button **Logout**:
  - Gọi `APIClient.ClearToken()`.
  - Xóa `USER_ID`, `USERNAME` trong PlayerPrefs.
  - `SceneManager.LoadScene("Login")`.

> Như vậy: **Login → LoadData → MainMenu → JoinGame → GameScene** là 1 flow hoàn chỉnh.

---

## 4️⃣ CHẠY GAME SERVER LOCALHOST (NETCODE PORT 2003)

Bạn đã có Netcode setup trong `Client/Assets`. Chỉ nhắc lại ngắn gọn:

### 4.1. Cấu hình NetworkManager + UnityTransport
Trong scene `GameScene` của Client:
1. GameObject `NetworkManager`:
   - Component `NetworkManager` (từ Netcode).
   - Component `UnityTransport`.
   - Component `NetworkManagerCustom` (script của bạn).
2. Trong `NetworkManagerCustom`:
   - **Server IP**: `127.0.0.1`.
   - **Server Port**: `2003`.
3. NetworkManager → **Network Prefabs**:
   - Add `Player.prefab` (có `NetworkObject` + `PlayerNetwork`). 

### 4.2. Chạy server trong Editor (Host hoặc Dedicated)
**Cách đơn giản để test:**
- Mở scene `GameScene`.
- Tạo 1 UI button (chỉ để dev) gọi `NetworkManagerCustom.StartServer()` hoặc `StartHost()`.
- Hoặc viết một script nhỏ chạy `StartServer()` ở `Start()` khi build server.

> Đối với localhost: bạn có thể dùng **Host** (vừa Server vừa Client trong 1 instance) để test nhanh, hoặc Dedicated Server + Client riêng.

---

## 5️⃣ CHẠY NHIỀU CLIENT BẰNG PARRELSYNC (DUPLICATE PROJECT)

Bạn đã có `ParrelSync.csproj` trong `Client`, nên gần như chắc chắn đã cài **ParrelSync**.

### 5.1. Mở ParrelSync
Trong Unity (Client):
- **ParrelSync → Clones Manager** (menu bar trên cùng).

### 5.2. Tạo Clone
1. Trong cửa sổ Clones Manager, click **Add new clone**.
2. Đợi tool tạo 1 clone của project (ví dụ: `Client_Clone`).
3. Bấm **Open in New Editor** để mở clone bằng 1 Unity Editor thứ 2.

### 5.3. Cách test
**Editor 1 (Project gốc):**
- Mở scene `GameScene` (hoặc `MainMenu` → JoinGame).
- Chạy **Host** hoặc **Server** (port 2003).

**Editor 2 (Clone):**
- Mở scene `Login` → Login bằng tài khoản khác.
- Đi qua flow: Login → LoadData → MainMenu → JoinGame.
- Client sẽ connect đến Host/Server ở Editor 1 qua **127.0.0.1:2003**.

Kết quả:
- 2 cửa sổ Unity, mỗi cửa sổ là 1 client (hoặc 1 client + 1 host).
- Mỗi client login bằng account riêng, data đọc/ghi trên cùng DB `GameDB`.

---

## 6️⃣ DÒNG DỮ LIỆU: PLAYER LƯU LÊN DB NHƯ THẾ NÀO?

**Login → Load → Gameplay → Save**:
1. **Đăng ký**: `POST /api/auth/register` → tạo `users`.
2. **Chọn hệ**: `POST /api/player/create` → tạo `player_data` với element_type, stats base, map_id, position.
3. **Login**: `POST /api/auth/login` → trả JWT + user_id.
4. **Load player**: `GET /api/player/{playerId}/data` → trả full player_data (+ final stats).
5. **Trong game**:  
   - Khi **level up / equip / cộng stat / unlock skill / nhặt item / đổi map**, Server API (hoặc GameServer) sẽ:
     - Update record `player_data` tương ứng (JSON fields + stats + map_id + position).
     - Save vào SQL (`UPDATE player_data SET ... WHERE player_id = @id`).
6. **Logout / Disconnect**:  
   - Trước khi disconnect, Server nên **save lại 1 lần nữa** (auto-save) để đảm bảo không mất dữ liệu.

> Toàn bộ logic save đã được mô tả chi tiết ở `luuthongtin.md` (phần **Periodic Save**, **EXP & Level Up**, **Equip/Unequip**, **Potential**, **Skills**).  
> Ở Client, bạn **chỉ cần gọi đúng API** và để Server lo phần DB.

---

## 7️⃣ CHECKLIST RIÊNG CHO LOGIN / LOGOUT + DB

- [ ] Tạo database `GameDB` và chạy script tạo `users`, `player_data`, `exp_requirements`.
- [ ] Project .NET API (`GameServerApi`) build và chạy được (`dotnet run`).
- [ ] Kết nối DB OK (không lỗi connection string).
- [ ] Implement xong 4 endpoint tối thiểu:
  - [ ] `POST /api/auth/register`
  - [ ] `POST /api/auth/login`
  - [ ] `POST /api/player/create`
  - [ ] `GET /api/player/{playerId}/data`
- [ ] Base URL trong `APIClient.cs` = `http://localhost:5000/api`.
- [ ] Scene `Login`, `Register`, `SelectElement`, `MainMenu`, `GameScene` đã gắn đúng script.
- [ ] NetworkManager + NetworkManagerCustom + UnityTransport trong `GameScene` đã cấu hình port **2003**.
- [ ] Test: Register → Login → SelectElement → MainMenu → JoinGame → GameScene OK.
- [ ] ParrelSync clone chạy được 2 Editor → 2 client login khác nhau → đều lưu dữ liệu lên DB.

---

## 8️⃣ QUICK START – GẮN SCRIPT VÀO SCENE (CLIENT ĐANG CÓ)

### 8.1. Login Scene

- **Scene:** `Login`  
- **Cần có trong Hierarchy:**
  - `Canvas` chứa:
    - `UsernameInput` (InputField hoặc TMP_InputField)
    - `PasswordInput`
    - `LoginButton`
    - `RegisterButton`
    - `ErrorText`
  - `APIClient` (GameObject + Component `APIClient` – nếu bạn không đặt prefab riêng, có thể để ở scene Bootstrap khác).
  - `GameManager` (nên đặt ở scene Bootstrap hoặc Main, có `GameManager` trong `Core`).
  - `LoginController` (Empty GameObject + Component `LoginController`)

**Gán reference trong `LoginController`:**
- `usernameInput` → drag `UsernameInput`
- `passwordInput` → drag `PasswordInput`
- `loginButton` → drag `LoginButton`
- `registerButton` → drag `RegisterButton`
- `errorText` → drag `ErrorText`

### 8.2. Register Scene

- **Scene:** `Register`  
- **Cần có trong Hierarchy:**
  - `Canvas` chứa:
    - `UsernameInput`
    - `EmailInput`
    - `PasswordInput`
    - `ConfirmPasswordInput`
    - `RegisterButton`
    - `BackButton`
    - `ErrorText`
    - `SuccessText`
  - `APIClient` (nếu không dùng scene Bootstrap chung)
  - `RegisterController` (Empty + Component)

**Gán reference trong `RegisterController`:**
- Map lần lượt các Input/Buttons/Text vào đúng field trong Inspector.

### 8.3. SelectElement Scene (chọn hệ lần đầu)

- **Scene:** `SelectElement`  
- **Cần có:**
  - 5 Button: `MetalButton`, `WoodButton`, `WaterButton`, `FireButton`, `EarthButton`
  - `ErrorText`
  - `APIClient` + `GameManager` (có thể là DontDestroyOnLoad từ scene trước)
  - `SelectElementController` (Empty + Component)

**Gán reference trong `SelectElementController`:**
- `metalButton` → MetalButton  
- `woodButton` → WoodButton  
- `waterButton` → WaterButton  
- `fireButton` → FireButton  
- `earthButton` → EarthButton  
- `errorText` → ErrorText

### 8.4. MainMenu Scene

- **Scene:** `MainMenu`  
- **Cần có:**
  - `JoinGameButton`
  - `LogoutButton`
  - `PlayerInfoText`
  - `MainMenuController` (Empty + Component)
  - `GameManager` (DontDestroyOnLoad)
  - `NetworkManager` (nếu bạn muốn connect ngay tại MainMenu – hoặc để trong `GameScene` cũng được)

**Gán reference trong `MainMenuController`:**
- `joinGameButton` → JoinGameButton  
- `logoutButton` → LogoutButton  
- `playerInfoText` → PlayerInfoText

### 8.5. GameScene (Netcode)

- **Scene:** `GameScene` (bạn hiện đang dùng `MainGame.unity` – có thể dùng lại, chỉ cần đảm bảo cấu hình đúng):
  - GameObject `NetworkManager`:
    - Component `NetworkManager` (Unity Netcode)
    - Component `UnityTransport`
    - Component `NetworkManagerController` (đang có)
    - (Tùy bạn: nếu muốn dùng `NetworkManagerCustom` thì thêm Component này và bỏ `NetworkManagerController`)
  - GameObject `NetworkPlayerSpawner` (đang có) – spawn player theo Netcode.
  - Prefab `Player` / `NetworkPlayer` có:
    - `NetworkObject`
    - `NetworkPlayerController` hoặc `PlayerNetwork` (chọn 1 hệ thống netcode, tránh trùng)

> Lời khuyên: hiện bạn đã có `NetworkManagerController`, `NetworkPlayerController`, `NetworkPlayerSpawner` hoạt động ổn → giữ nguyên để quản lý spawn/movement, và dùng `NetworkManagerCustom` **chỉ như helper** nếu cần connect theo IP/Port custom. Đừng gắn 2 flow song song nếu chưa thật sự cần.

---

## 9️⃣ NẾU MUỐN LÀM SERVER TÁCH RIÊNG (DEDICATED)

Sau khi mọi thứ chạy ổn ở localhost (Host + API), bước tiếp theo:
- Tách **GameServer (Netcode)** ra project Unity riêng (Server build).
- Tách **GameServerApi (.NET)** ra máy khác / VPS.
- Client chỉ cần đổi:
  - Base URL (API) → IP VPS.
  - `serverIP` trong `NetworkManagerCustom` → IP VPS (port 2003).

Luồng vẫn giữ nguyên, chỉ đổi **địa chỉ**.

---

**Tóm lại:**  
- Bạn đã có Netcode trong `Client` → chỉ cần: **DB + API + gắn đúng scripts UI** là có full login/logout + lưu player lên DB.  
- Toàn bộ kiến trúc script giờ nằm trong `Client/Assets/Scripts` với các nhóm: `Core`, `API`, `UI`, `Network`, `Inventory`, `Combat`, `Enemy`, `Player`, `Data`, `Gene`.  
- Làm lần lượt theo các bước trên, nếu bị kẹt ở bước nào, gửi log / lỗi, mình sẽ chỉnh lại flow hoặc code mẫu cho đúng với project của bạn. 💻🎮
