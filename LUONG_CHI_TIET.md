GAME ARCHITECTURE FLOW DOCUMENTATION
Kiến trúc tổng quan

Client: Ứng dụng game người chơi
API Server: Server xử lý authentication, lưu trữ dữ liệu persistent
Host-Client Server: Server game sử dụng Netcode để đồng bộ real-time


FLOW 1: CLIENT START & LOGIN (Bước 1-5)
Mô tả
Client khởi động và đăng nhập vào hệ thống
Chi tiết

Client Application Start: Người chơi mở game
Login Request: Client gửi username/password tới API Server
Validate Credentials: Authentication Service kiểm tra thông tin với Database
Return Auth Token: Database trả về token xác thực
Token + PlayerID: Client nhận token và PlayerID để sử dụng cho các request sau

Công nghệ đề xuất

JWT Token cho authentication
HTTPS cho bảo mật
BCrypt cho mã hóa password


FLOW 2: LOAD PLAYER DATA FROM API (Bước 6-10)
Mô tả
Sau khi login, client tải toàn bộ dữ liệu người chơi từ API Server
Chi tiết

Request Player Data: Client gửi PlayerID + Token yêu cầu dữ liệu
Query Player Data: Player Data Service truy vấn database
Return Full Data: Database trả về:

Stats (level, exp, health, mana...)
Inventory (items, equipment...)
Progress (quests, achievements...)
Settings (preferences, key bindings...)


Player Data Package: Dữ liệu được đóng gói gửi về client
Store Local Cache: Client lưu vào bộ nhớ cache local

Data Structure Example
json{
  "playerId": "12345",
  "stats": {
    "level": 25,
    "exp": 45000,
    "health": 1200,
    "mana": 800
  },
  "inventory": [
    {"itemId": "sword_001", "quantity": 1},
    {"itemId": "potion_hp", "quantity": 15}
  ],
  "progress": {
    "questsCompleted": ["quest_1", "quest_5"],
    "achievements": ["first_kill", "level_10"]
  }
}

FLOW 3: CONNECT TO HOST-CLIENT SERVER (Bước 11-16)
Mô tả
Client kết nối tới game server (Netcode) để tham gia gameplay
Chi tiết

Connect to Game Server: Client gửi connection request với PlayerID + Token
Verify Token: Network Manager xác thực token với API Server
Token Valid: API Server xác nhận token hợp lệ
Create Player Instance: Host-Client tạo instance cho người chơi trong game world
Request Initial Data: Server yêu cầu dữ liệu ban đầu
Send Cached Data: Client gửi dữ liệu đã cache từ API Server

Netcode Setup
csharp// Unity Netcode for GameObjects example
NetworkManager.Singleton.StartClient();
NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

FLOW 4: INITIAL SYNC (Bước 17-20)
Mô tả
Đồng bộ trạng thái ban đầu giữa server và client
Chi tiết

Initialize Player State: Server khởi tạo trạng thái người chơi
Broadcast to Other Clients: Thông báo người chơi mới join
Sync World State: Đồng bộ:

Vị trí các người chơi khác
Trạng thái môi trường
Entities đang hoạt động


Update UI & Game State: Client cập nhật giao diện và bắt đầu gameplay


FLOW 5: GAMEPLAY & REAL-TIME SYNC (Bước 21-27)
Mô tả
Vòng lặp chính của gameplay với đồng bộ real-time
Chi tiết

Player Action: Người chơi thực hiện hành động (di chuyển, tấn công...)
Send Action via Netcode: Client gửi action qua Netcode RPC/NetworkVariable
Receive & Validate: Server nhận và validate action
Apply to Server State: Server cập nhật trạng thái authoritative
Broadcast Changes: Server phát tán thay đổi
Sync to All Clients: Tất cả client nhận update
Update Game State: Mỗi client cập nhật local state

Client-Side Prediction
csharp// Client tự predict trước, sau đó reconcile với server
void Move(Vector3 direction) {
    // Local prediction
    transform.position += direction;
    
    // Send to server
    MoveServerRpc(direction);
}

[ServerRpc]
void MoveServerRpc(Vector3 direction) {
    // Server validates and broadcasts
    transform.position += direction;
}

FLOW 6: DATA CHANGE & PERSISTENCE (Bước 28-32)
Mô tả
Lưu trữ các thay đổi dữ liệu quan trọng lên database
Chi tiết

Detect Data Change: Server phát hiện thay đổi quan trọng:

Level up
Item acquired/lost
Quest completion
Currency changes


Queue Save Request: Thêm vào hàng đợi lưu
Batch Save: Lưu theo batch mỗi X giây (tránh quá tải DB)
Confirm Save: Database xác nhận đã lưu thành công
ACK to Server: Server nhận acknowledgment

Save Strategy
csharp// Batch saving every 5 seconds
private Queue<PlayerDataChange> saveQueue;
private float saveInterval = 5f;

void Update() {
    saveTimer += Time.deltaTime;
    if (saveTimer >= saveInterval && saveQueue.Count > 0) {
        BatchSaveToAPI();
        saveTimer = 0;
    }
}
Optimizations

Dirty Flag: Chỉ lưu field đã thay đổi
Compression: Nén dữ liệu trước khi gửi
Priority Queue: Ưu tiên dữ liệu quan trọng


FLOW 7: PERIODIC SYNC (Bước 33-34)
Mô tả
Checkpoint định kỳ để đảm bảo data consistency
Chi tiết

Periodic Checkpoint: Mỗi 30s-1 phút, server gửi checkpoint
Save Critical Data: Lưu dữ liệu quan trọng:

Player position
Health/Mana
Critical items



Backup Strategy

Incremental Save: Chỉ lưu delta changes
Full Save: Mỗi 5-10 phút lưu toàn bộ state
Redis Cache: Cache intermediate state để giảm tải DB


FLOW 8: DISCONNECT HANDLING (Bước 35-39)
Mô tả
Xử lý khi người chơi disconnect
Chi tiết

Disconnect Event: Client ngắt kết nối (tự nguyện hoặc timeout)
Save Final State: Server lưu trạng thái cuối cùng
Final Write: Ghi vào database
Remove Player Instance: Xóa instance khỏi game world
Notify Other Clients: Thông báo người chơi khác

Graceful Shutdown
csharpvoid OnApplicationQuit() {
    // Send final state to server
    SavePlayerDataServerRpc();
    
    // Wait for confirmation
    await WaitForSaveConfirmation();
    
    // Disconnect
    NetworkManager.Singleton.Shutdown();
}

CONSIDERATIONS & BEST PRACTICES
1. Network Optimization

Client-Side Prediction: Giảm lag cảm nhận
Server Reconciliation: Đảm bảo consistency
Interpolation: Smooth movement của other players
Delta Compression: Chỉ gửi thay đổi

2. Data Consistency

Server Authority: Server là source of truth
Optimistic Locking: Tránh race conditions
Transaction: Đảm bảo atomicity khi save
Idempotency: API calls có thể retry an toàn

3. Security

Token Expiration: JWT token có thời hạn
Rate Limiting: Giới hạn request/second
Input Validation: Validate mọi input từ client
Anti-Cheat: Server-side validation cho critical actions

4. Scalability

Load Balancing: Phân tải nhiều game servers
Database Sharding: Chia database theo PlayerID
Caching Layer: Redis cho hot data
Message Queue: RabbitMQ/Kafka cho async processing

5. Error Handling

Retry Logic: Tự động retry khi network fail
Fallback: Graceful degradation
Logging: Chi tiết error cho debugging
Monitoring: Real-time alerts cho issues


TECHNOLOGY STACK RECOMMENDATIONS
API Server

Backend: Node.js (Express) / ASP.NET Core / Go
Database: PostgreSQL / MySQL / MongoDB
Cache: Redis
Authentication: JWT / OAuth2

Host-Client Server

Engine: Unity với Netcode for GameObjects
Alternative: Unreal Engine với Replication System
Protocol: UDP với reliability layer

Client

Engine: Unity / Unreal
State Management: Redux pattern cho complex state
Networking: Netcode SDK

Infrastructure

Cloud: AWS / Google Cloud / Azure
Container: Docker + Kubernetes
CDN: CloudFlare cho static assets
Monitoring: Prometheus + Grafana


SEQUENCE EXAMPLE
Player opens game
  → Login to API Server
  → Receive auth token + PlayerID
  → Load player data (stats, inventory, progress)
  → Cache data locally
  → Connect to Game Server with token
  → Server validates token
  → Server creates player instance
  → Initial sync with other players
  → Gameplay starts
  
During Gameplay (loop):
  → Player action
  → Client predicts locally
  → Send to server via Netcode
  → Server validates
  → Server updates authoritative state
  → Broadcast to all clients
  → Clients reconcile with server state
  
Every 5 seconds:
  → Batch save changes to API Server
  → API writes to Database
  
Every 30-60 seconds:
  → Full checkpoint save
  
On Disconnect:
  → Final state save
  → Cleanup server instance
  → Notify other players