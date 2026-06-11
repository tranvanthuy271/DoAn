# 🔄 Kiến Trúc Tổng Thể Hệ Thống Server

Tài liệu này trình bày chi tiết về luồng kiến trúc tổng thể, sự phối hợp vận hành giữa các thành phần Unity Client, Dedicated Server (chạy Unity NGO), API Backend (ASP.NET Core) và Hệ thống Realtime Hubs (SignalR).

---

## 1. Sơ đồ Kiến trúc Hệ thống (System Architecture)

Sơ đồ dưới đây thể hiện sự tương tác và các giao thức kết nối giữa các thành phần chính trong hệ thống:

```mermaid
graph TD
    %% Clients
    subgraph Client ["Unity Client (Gameplay & UI)"]
        UI["UI & Core Logic<br/>(Login, Inventory, Quest)"]
        NGOClient["NGO Netcode Client<br/>(Player Object Sync)"]
        SigClient["SignalR Client<br/>(Chat / Party Presence)"]
    end

    %% Dedicated Server
    subgraph DedicatedServer ["Unity Dedicated Game Server (NGO Host)"]
        Approval["ZoneConnectionApprovalV2<br/>(JWT Auth & Room Assign)"]
        Registry["ZoneRoomRegistry<br/>(Quản lý Map & Zone/Room)"]
        Transition["ZoneTransitionController<br/>(Chuyển Map/Teleport/Phó bản)"]
        DungeonRun["Dungeon Runtime<br/>(Wave & Party Dungeon)"]
        SessionMgr["ZonePlayerSessionManager<br/>(Quản lý Session Player)"]
        SpawnConfig["HostSpawnConfigLoader<br/>(Spawn Enemy/Boss)"]
    end

    %% Web API Server
    subgraph ApiServer ["ASP.NET Core API Server"]
        AuthCtrl["AuthController<br/>(Đăng nhập / JWT / Điểm danh)"]
        PlayerCtrl["PlayerController<br/>(Data Nhân vật, Vị trí, Inventory)"]
        DungeonCtrl["DungeonController<br/>(Config Wave / Boss / Ải)"]
        ChatHub["ChatHub (SignalR)<br/>(Kênh Chat Thế giới/Nhóm/Bang)"]
        PartyHub["PartyHub (SignalR)<br/>(Trạng thái Tổ đội thời gian thực)"]
    end

    %% Database
    subgraph Database ["Database Layer"]
        MySQL[("MySQL Database<br/>(gamedb)")]
    end

    %% Interactions
    UI -->|REST API (HTTPS/JWT)| AuthCtrl
    UI -->|REST API (HTTPS/JWT)| PlayerCtrl
    UI -->|REST API (HTTPS/JWT)| DungeonCtrl
    SigClient -->|Websocket| ChatHub
    SigClient -->|Websocket| PartyHub
    
    NGOClient <-->|Netcode (UDP/Transport)| DedicatedServer
    
    DedicatedServer -->|Internal REST Call| PlayerCtrl
    DedicatedServer -->|Internal REST Call| DungeonCtrl
    
    AuthCtrl --> MySQL
    PlayerCtrl --> MySQL
    DungeonCtrl --> MySQL
```

---

## 2. Các Thành Phần Chính & Vai Trò

Hệ thống được thiết kế theo mô hình lai (Hybrid) kết hợp giữa Web REST API cho các tác vụ lưu trữ dữ liệu bền vững và Dedicated Realtime Server cho các tác vụ đồng bộ hóa vật lý, di chuyển:

| Thành phần | Công nghệ | Vai trò / Trách nhiệm |
| :--- | :--- | :--- |
| **Unity Client** | Unity 2D / C# | Hiển thị giao diện (UI), điều khiển nhân vật, vẽ đồ họa, xử lý va chạm cục bộ phía client, gửi và nhận đồng bộ qua Netcode và API. |
| **Dedicated Server** | Unity NGO Host / Headless | Thực hiện tính toán vật lý chuẩn (Authority), spawn quái vật/boss, chạy tiến trình phó bản, đồng bộ hóa vị trí của mọi thực thể qua Netcode. |
| **API Server** | ASP.NET Core | Cung cấp các Endpoint phục vụ xác thực JWT, thao tác dữ liệu cơ sở dữ liệu, quản lý nâng cấp Gene, kỹ năng, trang bị và lưu trữ vị trí lâu dài. |
| **Realtime Hub** | SignalR (Websocket) | Truyền tải thông điệp chat thời gian thực và quản lý phòng chờ tổ đội mà không cần đi qua băng thông UDP của Game Server. |
| **Database** | MySQL | Lưu trữ thông tin tài khoản, nhân vật, cấu hình quái vật, danh sách phó bản và trang bị. |

---

## 3. Luồng Chạy Chi Tiết Của Các Tính Năng Hệ Thống

### 3.1. Luồng Đăng nhập & Xác thực (Authentication)
1. **Client** gửi tài khoản và mật khẩu thông qua REST API `/api/auth/login`.
2. **API Server** xác thực mật khẩu bằng Bcrypt. Nếu thành công, tiến hành ghi điểm danh ngày (`RecordDailyAttendanceAsync`), tạo mã JWT Token chứa thông tin `userId`, `username` và gửi lại Client.
3. **Client** lưu mã JWT Token vào `PlayerPrefs` để đính kèm vào header cho tất cả các yêu cầu HTTP sau này.

> [!IMPORTANT]
> Mã JWT Token này cũng chính là payload được gửi kèm khi kết nối bắt tay với Unity Dedicated Game Server để xác thực danh tính người chơi trực tiếp từ mạng Netcode.

---

### 3.2. Luồng Bắt tay kết nối & Phân bổ Zone (NGO Connection)
1. Client yêu cầu kết nối với Dedicated Server qua cổng mạng UDP của Netcode.
2. `ZoneConnectionApprovalV2` trên Server nhận yêu cầu kết nối, thực hiện:
   * Kiểm tra tính hợp lệ của mã JWT bằng `JwtValidator`.
   * Tìm vị trí lưu gần nhất của nhân vật (`map_id`, `zone_id` lấy từ DB).
   * Phân bổ người chơi vào phòng (Room/Zone) phù hợp.
3. `ZoneRoomRegistry` quản lý các loại phòng:
   * **SharedPublic (Map thường)**: Tự động khởi tạo `0..N-1` zone (mặc định là 15 zone). Cho phép người chơi tự do đổi khu qua RPC.
   * **InstanceOnly (Map phó bản)**: Được tạo động lúc runtime với `zone_id` âm (ví dụ: `-1`, `-2`...). Client không thể tự động kết nối vào nếu không được chuyển bởi hệ thống Server.

---

### 3.3. Luồng Đồng bộ Nhân vật & Spawn (Character Spawning)
1. Sau khi kết nối thành công, `ClientAuthHandler` phía Client gửi yêu cầu RPC xác thực `SendAuthServerRpc()`.
2. `ServerPlayerDataManager` trên Server nhận yêu cầu, tiến hành tải dữ liệu nhân vật thông qua API `/api/player/{userId}/data` và cache vào RAM server.
3. `NetworkPlayerSpawner` dựa trên hệ phái (`element_type`) và giới tính (`gender`) từ dữ liệu nhân vật để sinh đúng Prefab nhân vật tương ứng tại vị trí tọa độ đã lưu.
4. Quyền sở hữu (Ownership) của vật thể nhân vật được gán cho `clientId` tương ứng để Client có thể trực tiếp điều khiển di chuyển. Các chỉ số được đồng bộ liên tục qua `NetworkPlayerDataSync`.

> [!NOTE]
> Hệ thống lọc hiển thị `NetworkVisibilityZoneFilter` giúp tối ưu băng thông bằng cách chỉ đồng bộ hóa thông tin của những người chơi ở **cùng một zone** với nhau.

---

### 3.4. Luồng Chuyển Map / Teleport
1. Người chơi va chạm vào các điểm dịch chuyển (Portal) hoặc chọn bản đồ khác.
2. Client gửi yêu cầu chuyển map lên Server.
3. `ZoneTransitionController` kiểm tra điều kiện chuyển cảnh:
   * Thay đổi trạng thái session của người chơi trong `ZonePlayerSessionManager`.
   * Gửi lệnh ClientRpc yêu cầu Client tải Scene mới qua `ClientSceneController`.
   * Cập nhật tọa độ mới của người chơi về cơ sở dữ liệu qua API REST `PUT /api/player/{id}/position`.

---

### 3.5. Luồng Phó bản & Tổ đội (Dungeon Run)
* **Đối với phó bản Solo (Wave Dungeon)**:
  1. Client gọi yêu cầu đi ải solo.
  2. Server tạo một Custom Room động với `zone_id` âm.
  3. Kích hoạt `WaveDungeonRuntime` để load cấu hình ải từ API `/api/dungeon/wave/{dungeonId}/config`.
  4. Triệu hồi quái theo đợt (Wave), hết quái nhỏ sẽ triệu hồi Boss, hoàn thành ải sẽ nhận thưởng và chuyển người chơi về map thường.
* **Đối với phó bản Tổ đội (Party Dungeon)**:
  1. Trưởng nhóm đăng ký đi ải tổ đội tại NPC.
  2. Server kiểm tra danh sách thành viên nhóm (thông qua SignalR `PartyHub` và session trên Server).
  3. `ZoneTransitionController` tạo một Custom Room chung duy nhất cho cả nhóm, dịch chuyển toàn bộ thành viên vào room này.
  4. Triệu hồi quái vật và Boss theo cấu hình tổ đội (`PartyDungeonRuntime`).

---

### 3.6. Luồng AI Boss Nhiều Pha (Multi-phase Boss AI)
1. Khi Boss được spawn trong phó bản, hệ thống tải cấu hình pha từ API `/api/dungeon/boss/config` (dữ liệu `phases_json` trong DB). Nếu DB trống, hệ thống sẽ sử dụng cơ chế dự phòng mặc định (60% HP và 30% HP).
2. Khi Boss chiến đấu, lớp `BossAI` liên tục theo dõi lượng máu.
3. Khi lượng máu xuống dưới các ngưỡng quy định:
   * Kích hoạt phase mới thông qua `ExecutePhase()`.
   * Kích hoạt các hiệu ứng đặc biệt: `enrage` (tăng sát thương và tốc độ), `berserk` (tăng mạnh sát thương và giảm cooldown kỹ năng), `summon` (triệu hồi thêm quái nhỏ trợ chiến), hoặc `heal` (hồi phục máu dựa trên % máu tối đa).

---

## 4. Bản Đồ Ánh Xạ File Nguồn Quan Trọng

Dưới đây là các lớp C# cốt lõi đảm nhận vai trò trong luồng kiến trúc trên:

```
📁 Client/Assets/Scripts/ Network/
├── 📁 Server/
│   ├── 📄 ZoneConnectionApprovalV2.cs   <-- Tiếp nhận kết nối, validate JWT
│   ├── 📄 ZoneRoomRegistry.cs           <-- Quản lý danh sách room (public & custom)
│   ├── 📄 ZoneTransitionController.cs   <-- Xử lý chuyển dịch giữa các map/zone/phó bản
│   └── 📄 ZonePlayerSessionManager.cs   <-- Quản lý trạng thái phiên chơi của player
├── 📁 Client/
│   └── 📄 ClientSceneController.cs      <-- Điều khiển tải màn chơi phía Client
└── 📁 Shared/
    ├── 📄 MapWorldConfig.cs             <-- Cấu hình toàn bộ danh sách map, zone mặc định
    └── 📄 JwtValidator.cs               <-- Giải mã và xác thực token JWT

📁 GameServerApi/
├── 📁 Controllers/
│   ├── 📄 AuthController.cs             <-- REST API xác thực tài khoản, cấp JWT
│   ├── 📄 PlayerController.cs           <-- REST API đọc ghi chỉ số nhân vật, vị trí, hòm đồ
│   └── 📄 DungeonController.cs          <-- REST API cấu hình phó bản, đợt quái, Boss AI
└── 📁 Hubs/
    ├── 📄 ChatHub.cs                    <-- SignalR truyền tin chat thời gian thực
    └── 📄 PartyHub.cs                   <-- SignalR đồng bộ tổ đội thời gian thực
```
