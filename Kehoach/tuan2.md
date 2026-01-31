## Tuần 2 (27/01–02/02): NGO baseline (spawn/ownership/movement sync) + test 2 instance

### 1. Mục tiêu tuần 2
- **Đầu ra bắt buộc**:
  - 2 người chơi join local (Editor + 1 build `.exe`).
  - Cả hai **spawn đúng vị trí**, mỗi người điều khiển **đúng nhân vật của mình** (owner-only input).
  - **Movement sync mượt** giữa 2 máy (chạy/nhảy/đứng yên nhìn thấy nhau đúng).
- **Ưu tiên tuần 2**: chỉ tập trung vào **Player + movement/transform**, chưa động vào farm/đồ/upgrade.

---

### 2. Điều kiện tiên quyết (check nhanh trước khi làm)
- Đã cài **Netcode for GameObjects** (NGO) theo `SERVER_CLIENT_SETUP_GUIDE.md`.
- Trong scene chơi chính (vd: `MainGame.unity`) đã có:
  - `NetworkManager` GameObject với:
    - `NetworkManager` component.
    - `UnityTransport` component.
  - Hệ thống UI hoặc nút để **Start Host / Start Client** (có thể dùng `ConnectionUI` như trong guide).
- Prefab Player đã có variant mạng, ví dụ: `NetworkPlayer`:
  - Có component `NetworkObject`.
  - Có component `NetworkTransform` (hoặc `ClientNetworkTransform` nếu dùng).
  - Có các script điều khiển chuyển động (`PlayerMovement`, `PlayerController`, v.v.).

Nếu thiếu chỗ nào, xem lại `SERVER_CLIENT_SETUP_GUIDE.md` và bổ sung trước rồi mới làm tiếp.

---

### 3. Kế hoạch theo ngày (tuần 2)

#### Ngày 1: Chuẩn hoá NetworkPlayer prefab + Network Prefabs
- **Mục tiêu**: Có 1 prefab player mạng chuẩn, để server spawn cho từng client.
- **Bước làm**:
  1. Trong `Assets/Prefabs/Player/`, duplicate prefab Player hiện có → đặt tên `NetworkPlayer` (nếu chưa có).
  2. Mở prefab `NetworkPlayer`:
     - Thêm component `NetworkObject`.
     - Thêm component `NetworkTransform`.
       - Bật sync Position, Rotation (nếu cần), tắt Scale nếu không dùng.
       - Bật Interpolate để di chuyển mượt.
  3. Mở `NetworkManager` trong scene:
     - Trong phần `Network Prefabs`, thêm `NetworkPlayer` vào danh sách.
  4. Đảm bảo các script điều khiển (`PlayerMovement`, `PlayerController`, Animator, Rigidbody2D…) vẫn nằm trên prefab.
- **Definition of Done**:
  - Chạy scene, không báo lỗi missing component ở `NetworkPlayer`.
  - Trong `NetworkManager` → `Network Prefabs` thấy `NetworkPlayer` đã được đăng ký.

#### Ngày 2: Làm hệ thống spawn player (NetworkPlayerSpawner)
- **Mục tiêu**: Khi client kết nối, server tự động spawn 1 `NetworkPlayer` cho từng client.
- **Bước làm**:
  1. Mở script `NetworkPlayerSpawner` (hoặc tạo nếu chưa có) theo guide:
     - Bên server (IsServer/IsHost) lắng nghe `OnClientConnectedCallback` của `NetworkManager`.
     - Khi có client join:
       - Chọn 1 spawn point (mảng `spawnPoints`).
       - `Instantiate` prefab `NetworkPlayer` tại vị trí đó.
       - Gọi `networkObject.SpawnWithOwnership(clientId)`.
  2. Trong scene game:
     - Tạo GameObject `NetworkPlayerSpawner`.
     - Gán script `NetworkPlayerSpawner` vào.
     - Gán prefab `NetworkPlayer` và các `spawnPoints` trong Inspector.
  3. Chạy **một instance** trong Editor:
     - Bấm `Start Host` → kiểm tra đã spawn 1 player.
- **Definition of Done**:
  - Khi Start Host trong Editor, player spawn đúng vị trí spawn point.
  - Không lỗi null khi client connect/disconnect.

#### Ngày 3: Áp dụng owner-only input cho PlayerMovement
- **Mục tiêu**: Mỗi client chỉ điều khiển **nhân vật của chính mình**, không điều khiển player của người khác.
- **Ý tưởng**:
  - Dùng `NetworkBehaviour` hoặc `NetworkObject.IsOwner` để phân biệt **local player** và **remote player**.
- **Bước làm**:
  1. Ở script điều khiển (ví dụ `PlayerMovement` hoặc `PlayerController`):
     - Thêm `using Unity.Netcode;` ở đầu file.
     - Thêm field tham chiếu `NetworkObject networkObject;` hoặc kế thừa `NetworkBehaviour` (tuỳ kiến trúc bạn đang dùng).
  2. Trong `Awake`/`Start`:
     - Lấy `networkObject = GetComponent<NetworkObject>();`.
  3. Trong hàm xử lý input (vd `HandleInput()` hoặc `Update()`):
     - Thêm check:
       - Nếu có `networkObject` **và** `NetworkManager.Singleton` đang chạy **và** `!networkObject.IsOwner` thì **return** sớm, không xử lý input.
  4. Đảm bảo **remote player** vẫn update animation/transform bằng `NetworkTransform`, nhưng **không đọc phím**.
- **Definition of Done**:
  - Khi chạy **2 client** (Editor + build):
    - Ở mỗi máy, bạn chỉ điều khiển được 1 nhân vật (local).
    - Nhân vật của người kia chỉ chạy theo sync từ network, không phản ứng với input của bạn.

#### Ngày 4: Đồng bộ movement giữa 2 người chơi (primary focus tuần 2)
- **Mục tiêu**: Khi 1 người di chuyển, người kia nhìn thấy nhân vật đó di chuyển mượt và chính xác.
- **Bước làm**:
  1. Kiểm tra `NetworkTransform` trên `NetworkPlayer`:
     - Đặt **Send Rate** ở mức hợp lý (vd 20 gửi/giây).
     - Bật Interpolate.
  2. Đảm bảo Rigidbody2D/Physics chỉ được **owner** điều khiển (ở phía local):
     - Lực/velocity chỉ set trong code nếu `IsOwner == true`.
  3. Chạy test:
     - Build 1 bản `.exe`.
     - Mở Editor (Host) + chạy `.exe` (Client).
     - Mỗi bên di chuyển, quan sát bên còn lại:
       - Có lag/giật không?
       - Có hiện tượng “teleport” không?
  4. Nếu bị giật mạnh:
     - Tăng Interpolate, giảm send rate 1 chút (thử 15–20).
     - Hạn chế logic nặng trong `Update`/`FixedUpdate` của player.
- **Definition of Done**:
  - Ở cả hai máy, nhìn thấy player kia di chuyển **đúng hướng**, không bị delay lớn, không bị “double control”.

#### Ngày 5: Camera follow local player
- **Mục tiêu**: Mỗi client có camera riêng **chỉ follow local player** của họ.
- **Bước làm**:
  1. Mở script camera (vd `CameraFollow`):
     - Thay vì `FindGameObjectWithTag("Player")`, hãy subscribe hoặc set target từ `OnNetworkSpawn` của player owner.
  2. Cách đơn giản:
     - Trong `NetworkPlayerController` (hoặc script player chính):
       - Trong `OnNetworkSpawn()`, nếu `IsOwner == true`, tìm `CameraFollow` trong scene và gán target = transform của player này.
  3. Chạy 2 instance:
     - Trên mỗi máy, camera follow **đúng player local**.
- **Definition of Done**:
  - Editor: camera follow player host.
  - Build: camera follow player client.

#### Ngày 6: Thêm debug HUD (clientId, role, ping, fps/tick)
- **Mục tiêu**: Có overlay đơn giản để bạn dễ debug network về sau.
- **Bước làm**:
  1. Tạo Canvas đơn giản trong scene:
     - Thêm `Text` (UGUI) hoặc TMP_Text để hiển thị thông tin.
  2. Viết script `DebugNetworkHUD`:
     - Lấy `NetworkManager.Singleton`:
       - `LocalClientId`.
       - `IsHost`, `IsServer`, `IsClient`.
     - Ping: có thể dùng `NetworkManager.Singleton.LocalTime` hoặc tự ước lượng (nếu cần chi tiết thì để tuần sau).
     - FPS: đo bằng `1 / Time.deltaTime` trung bình.
  3. Cập nhật text mỗi `Update()`.
- **Definition of Done**:
  - Khi chạy game, góc màn hình hiển thị, ví dụ:
    - `ClientId: 0 | Role: Host`
    - `ClientId: 1 | Role: Client`

#### Ngày 7: Tổng hợp + test lại “2 instance local”
- **Mục tiêu**: Đảm bảo toàn bộ mục tiêu tuần 2 được đáp ứng trước khi chuyển sang tuần 3.
- **Checklist test**:
  - [ ] Editor Start Host → spawn 1 player host.
  - [ ] Build Start Client → join vào host, spawn 1 player client.
  - [ ] Mỗi bên điều khiển **đúng player của mình**, không điều khiển player của người khác.
  - [ ] Movement sync: chạy/nhảy/đứng yên thấy đúng trên cả 2 bên.
  - [ ] Camera follow đúng local player ở mỗi máy.
  - [ ] Debug HUD hiển thị đúng clientId + role.
- **Definition of Done (tuần 2)**:
  - Bạn có thể quay 1 video ngắn: Editor + build cùng chạy, 2 nhân vật chạy qua lại và nhìn thấy nhau sync ổn, dùng video đó sau này đưa vào báo cáo/demo.








