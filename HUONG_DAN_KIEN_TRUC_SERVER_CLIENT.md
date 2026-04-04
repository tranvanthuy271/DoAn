# HƯỚNG DẪN KIẾN TRÚC SERVER + CLIENT — LANGLA STYLE

> Phiên bản: 4.0  
> Áp dụng: DoAn — Unity NGO + ASP.NET Core API + MySQL  
> Mục tiêu: bỏ mô hình khai báo từng zone tĩnh, chuyển sang map thường tự sinh zone như LangLa

---

## 1. Nguyên tắc cốt lõi

LangLa quản lý map và zone theo 2 nhóm rõ ràng:

- Map thường: khi server boot sẽ tự tạo sẵn một số zone mặc định cho mỗi map.
- Map đặc biệt hoặc phó bản: không cho người chơi tự đổi khu công khai; server tạo zone riêng khi cần.

DoAn hiện được chỉnh lại theo đúng tinh thần đó:

```
1 Unity NGO server process
1 port duy nhất

ZoneRoomRegistry
  map 0 -> zone 0..N-1  (public zones tự sinh)
  map 1 -> zone 0..N-1
  map 10 -> không auto-create public zone
           custom room -1, -2, -3... khi vào phó bản
```

Hệ quả quan trọng:

- Không còn cần bảng `map_zone_config` để khai báo từng zone thường.
- Không còn tư duy mỗi zone là một process hoặc một port riêng.
- `zone_id` của player vẫn lưu trong `player_data.info_char`.

---

## 2. Mô hình zone của DoAn

### 2.1. Map thường

Map thường dùng `MapDefinition.zoneTopology = SharedPublic`.

Khi server start:

1. `MapWorldBootstrap` khởi tạo `ZoneRoomRegistry`.
2. `ZoneRoomRegistry.Initialize()` duyệt toàn bộ `MapWorldConfig.maps`.
3. Với mỗi map thường, registry tự tạo zone `0..N-1`.

Nếu một map không override số zone thì dùng mặc định toàn cục:

```csharp
sharedMapDefaultZoneCount = 15;
```

Điều này tương đương với `Map.NUM_ZONE = 15` trong LangLa.

### 2.2. Map phó bản hoặc zone riêng

Map phó bản dùng `MapDefinition.zoneTopology = InstanceOnly`.

Loại map này có các đặc tính:

- Không auto-create public zone lúc server boot.
- Không cho client tự gọi RPC để đổi khu công khai.
- Server tạo room runtime bằng `ZoneRoomRegistry.CreateCustomRoom()`.
- Room runtime dùng `zone_id` âm: `-1`, `-2`, `-3`...

Đây là phiên bản tương đương với `listZoneCusTom` và `getIDZoneCustom()` của LangLa.

### 2.3. Fallback khi zone cũ không còn tồn tại

Nếu player đăng nhập mà `map_id` hoặc `zone_id` đang lưu không còn hợp lệ nữa, `ZoneConnectionApprovalV2` sẽ không reject cứng ngay.

Nó sẽ:

1. thử tìm đúng room đã lưu,
2. nếu không thấy và map đó là map thường thì chuyển sang public zone hợp lệ ít tải,
3. nếu vẫn không có thì dùng `fallbackMapId` và `fallbackZoneId` trong `MapWorldConfig`.

Điều này giải quyết trường hợp player từng ở zone riêng nhưng instance đã kết thúc.

---

## 3. Dữ liệu lưu ở đâu

### 3.1. Player position và zone

Thông tin vị trí của player vẫn nằm trong `player_data.info_char` JSON:

```json
{
  "map_id": 1,
  "zone_id": 3,
  "position_x": 12.5,
  "position_y": 4.0
}
```

Không tạo thêm cột SQL riêng cho zone thường.

### 3.2. Không còn `map_zone_config`

`map_zone_config` không còn là nguồn dữ liệu chuẩn nữa.

Lý do:

- Zone thường là dữ liệu runtime được sinh từ chính sách map.
- Zone riêng/phó bản là dữ liệu runtime sống trong memory.
- Nếu vẫn giữ một bảng tĩnh cho từng zone thường thì kiến trúc lại lệch khỏi LangLa.

Migration đi kèm chỉ còn nhiệm vụ dọn bảng cũ nếu DB còn tồn tại.

---

## 4. Thành phần chính trong Unity

### 4.1. `MapWorldConfig`

File: `Client/Assets/Scripts/Network/Shared/MapWorldConfig.cs`

Đây là nơi khai báo:

- network config: listen address, port, API base URL,
- zone defaults: số khu mặc định cho map thường,
- fallback map/zone,
- danh sách map và chính sách zone của từng map.

Mỗi map chỉ cần mô tả chính sách, không cần mô tả từng zone public nữa.

### 4.2. `ZoneRoomRegistry`

File: `Client/Assets/Scripts/Network/Server/ZoneRoomRegistry.cs`

Trách nhiệm:

- tự sinh public zones cho map thường,
- tạo custom room cho phó bản,
- theo dõi player đang ở room nào,
- xóa custom room khi room rỗng.

### 4.3. `ZoneConnectionApprovalV2`

File: `Client/Assets/Scripts/Network/Server/ZoneConnectionApprovalV2.cs`

Trách nhiệm:

- validate JWT,
- resolve room lúc login,
- fallback nếu zone đã lưu không còn hợp lệ,
- assign player vào room ngay từ bước connect.

### 4.4. `ZoneTransitionController`

File: `Client/Assets/Scripts/Network/Server/ZoneTransitionController.cs`

Trách nhiệm:

- xử lý đổi khu cho map thường,
- chặn client request vào private zone,
- hỗ trợ API server-side `ServerTransferClientToCustomRoom()` cho dungeon,
- cập nhật `ZonePlayerSessionManager.UpdateZone()` sau mỗi lần đổi khu,
- lưu vị trí mới về API.

### 4.5. `ZoneServerHeartbeat`

File: `Client/Assets/Scripts/Network/Server/ZoneServerHeartbeat.cs`

Heartbeat bây giờ report từ snapshot room thực tế trong registry, nên sẽ thấy cả room public lẫn room riêng đang còn sống.

---

## 5. Flow runtime chuẩn

### 5.1. Login và connect

```text
Client login API -> nhận JWT
Client connect NGO server -> gửi payload { token, mapId, zoneId }
ZoneConnectionApprovalV2:
  - validate JWT
  - resolve room hợp lệ
  - assign client vào room
  - register session
```

### 5.2. Đổi khu trong map thường

```text
Client yêu cầu đổi khu public
  -> RequestZoneTransferServerRpc(mapId, zoneId, entryPointId)
Server:
  -> kiểm tra map có cho đổi khu không
  -> tìm room public hợp lệ
  -> fallback zone ít tải nếu khu đích đầy
  -> AssignClientToRoom()
  -> UpdateZone() trong session manager
  -> TeleportToZoneClientRpc()
  -> PUT /api/player/{id}/position
```

### 5.3. Vào phó bản

```text
Gameplay logic phía server
  -> ServerTransferClientToCustomRoom(clientId, dungeonMapId, ...)
Registry:
  -> CreateCustomRoom()
  -> sinh zone_id âm mới
Controller:
  -> assign client vào room mới
  -> teleport client
```

Client không được tự gửi `zone_id` âm để vào room riêng.

### 5.4. Disconnect

```text
OnClientDisconnected
  -> save position từ session hiện tại
  -> unregister client khỏi registry
  -> nếu custom room rỗng thì registry xóa room đó
```

---

## 6. Setup `MapWorldConfig.asset`

### 6.1. Root fields

Các field quan trọng:

| Field | Ý nghĩa |
|---|---|
| `sharedMapDefaultZoneCount` | số khu mặc định cho map thường |
| `sharedMapMaxPlayers` | max player cho mỗi public zone |
| `instanceMapMaxPlayers` | max player cho room riêng nếu map không override |
| `fallbackMapId` | map trả về khi zone cũ không còn hợp lệ |
| `fallbackZoneId` | zone trả về trong fallback map |

### 6.2. Mỗi `MapDefinition`

Ví dụ map thường:

```text
Map Id = 0
Map Name = LangKhoiDau
Scene Name = GameScene
Zone Topology = SharedPublic
Public Zone Count Override = 0
Public Zone Max Players Override = 0
Allow Player Zone Switch = true
Entry Points = [ ... ]
```

Ví dụ map phó bản:

```text
Map Id = 20
Map Name = PhoBanLuaTang1
Scene Name = DungeonFire_1
Zone Topology = InstanceOnly
Custom Zone Max Players Override = 4
Allow Player Zone Switch = false
Entry Points = [ ... ]
```

`0` ở các trường override có nghĩa là dùng default toàn cục.

---

## 7. Luồng nào là legacy

Các file sau không còn là kiến trúc chuẩn để mở rộng tiếp:

- `Client/Assets/Scripts/Map/ZoneTrigger.cs`
- `Client/Assets/Scripts/Map/ZoneRoomManager.cs`
- `Client/Assets/Scripts/Map/RoomBroadcast.cs`
- `Client/Assets/Scripts/Player/PlayerZoneHandler.cs`

Nếu scene cũ vẫn đang dùng chúng thì xem như tầng tương thích tạm thời.  
Logic zone mới phải ưu tiên bám theo `ZoneRoomRegistry` và `ZoneTransitionController`.

---

## 8. Checklist triển khai

- `MapWorldConfig.asset` đã được gán vào `MapWorldBootstrap` và `ZonePlayerSessionManager`
- các map thường để `zoneTopology = SharedPublic`
- các map phó bản để `zoneTopology = InstanceOnly`
- `sharedMapDefaultZoneCount` đã set theo số khu mặc định mong muốn
- `ZoneConnectionApprovalV2` đang active trên server bootstrap
- `ZoneTransitionController` đang active trên server bootstrap
- `GameServerApi` đã dùng `zone_id` trong `GetPlayerData` và `UpdatePlayerPosition`
- migration dọn `map_zone_config` đã được cập nhật nếu DB cũ còn bảng này

---

## 9. Ghi chú vận hành

- Zone thường là dữ liệu runtime sinh từ config, không phải dữ liệu SQL tĩnh.
- Zone riêng dùng `zone_id` âm, nhưng chỉ nên tồn tại trong vòng đời instance.
- Khi instance kết thúc, server nên đưa player về public zone hợp lệ trước khi save lâu dài.
- Nếu cần UI chọn khu như LangLa `openTabZone`, dữ liệu nên lấy từ public rooms hiện có trong registry, không lấy từ bảng SQL.
