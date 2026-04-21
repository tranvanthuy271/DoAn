# ServerRpc Item Use Setup

Mục tiêu: dùng item trong gameplay chỉ đi qua một luồng duy nhất.

Luồng đúng:

`GameScene client -> ItemDetailPanel -> ItemUseHandler -> GameplayCommandService.UseInventoryItemServerRpc(slotIndex) -> server gọi GameServerApi -> UseItemResultClientRpc -> client refresh HP/MP, buff, inventory`

Không dùng REST trực tiếp từ client cho gameplay item use.

## 1. Unity scene và prefab phải có gì

### ServerScene

Mở `Assets/Scenes/ServerScene.unity`.

Chọn object `ServerBootstrap` và kiểm tra:

- Có component `MapWorldBootstrap`
- Field `_networkManagersPrefab` đang trỏ tới `Assets/Prefabs/NetworkManagers.prefab`
- Field `_config` đang trỏ tới `Assets/ScriptableObjects/MapWorldConfig.asset`

### NetworkManagers prefab

Mở `Assets/Prefabs/NetworkManagers.prefab`.

Prefab này phải có các component sau trên cùng root object:

- `NetworkObject`
- `ZoneTransitionController`
- `ZonePlayerSessionManager`
- `GameplayCommandService`

`GameplayCommandService` phải nằm trên chính network prefab được spawn từ server. Nếu không, client sẽ không có singleton `GameplayCommandService.Instance` và các lệnh `ServerRpc` như dùng item, skill, inventory sẽ fail ở client.

### Network prefab list

Kiểm tra `Assets/ScriptableObjects/DefaultNetworkPrefabs - Copy.asset` hoặc list prefab mà `NetworkManager` đang dùng.

`Assets/Prefabs/NetworkManagers.prefab` phải nằm trong danh sách network prefabs.

## 2. GameScene client cần gì

Mở `Assets/Scenes/GameScene.unity`.

Đảm bảo có:

- `InventoryNetworkBridge`
- `ItemUseHandler`
- `InventoryUI`
- `ItemDetailPanel` prefab đã được gán vào `InventoryUI`

`ItemUseHandler` chỉ gửi `ServerRpc` qua `GameplayCommandService`. Nếu service chưa spawn thì đó là bug config network, không phải case để fallback sang REST.

## 3. Runtime order đúng

1. Chạy `GameServerApi`
2. Chạy `ServerScene`
3. `MapWorldBootstrap` start server
4. `MapWorldBootstrap` spawn `NetworkManagers.prefab`
5. `GameplayCommandService` xuất hiện trên server và replicate sang client
6. Client vào `GameScene`
7. Bấm dùng item

## 4. Dấu hiệu setup đúng

Ở `ServerScene` nên thấy log:

- `[MapWorldBootstrap] ✓ NetworkManagers spawned.`

Khi client dùng item, không được thấy log:

- `GameplayCommandService chưa spawn`

Nếu setup đúng, item use sẽ đi qua:

- `ItemUseHandler`
- `GameplayCommandService.UseInventoryItemServerRpc(...)`
- `UseItemResultClientRpc(...)`

## 5. Nếu vẫn không dùng được item

Kiểm tra theo thứ tự này:

1. `ServerBootstrap` có gán đúng `NetworkManagers.prefab` không
2. `NetworkManagers.prefab` có `GameplayCommandService` không
3. `NetworkManagers.prefab` có `NetworkObject` không
4. Prefab này có nằm trong network prefab list không
5. Client đã connect vào server trước khi mở inventory chưa
6. Console client có log `GameplayCommandService chưa spawn` không

## 6. Nguyên tắc kiến trúc

Gameplay runtime:

- Dùng `ServerRpc` cho item use, skill use, inventory actions, equipment actions
- Server giữ session và gọi API nội bộ nếu cần
- Client không giữ thêm một luồng REST gameplay riêng

Pre-game flows như login hoặc register có thể đi REST trực tiếp.

In-game authoritative actions thì không.
