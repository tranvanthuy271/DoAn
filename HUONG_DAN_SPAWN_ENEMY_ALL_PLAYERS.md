# Hướng dẫn chi tiết tạo và Spawn Enemy cơ bản cho mọi người chơi (Chưa bao gồm Skill)

> Tài liệu này hướng dẫn chi tiết từng bước để tạo một con quái (Enemy) trong Unity từ dự án trống, giúp nó có thể spawn, di chuyển, cập nhật máu UI qua mạng, đồng bộ cho tất cả người chơi trong map cùng lúc và tự động rớt đồ theo cấu hình JSON khi chết.

---

## Bước 1: Chuẩn bị Vật liệu (Sprite) và Animator
Để tất cả người chơi thấy chung một animation, chúng ta cần Setup Animator chuẩn:
1. **Asset hỉnh ảnh**: Tìm và cắt (Slice) đúng tấm ảnh Sprite Sheet của quái. Cấu hình Pivot xuống dưới chân (Bottom Center) để quái đứng chạm đất chuẩn.
2. Tạo một **Animator Controller** mới trong Project (Ví dụ: `Goblin_Animator`).
3. Kéo các Sprite để tạo các Clip Animation cơ bản: `Idle` (Đứng im), `Run` (Chạy), `Die` (Chết), `MeleeAttack` (Đánh thường cận chiến/vung tay).
4. Thiết lập Animator:
   - Các Parameter bắt buộc: `isRunning` (Bool), `isAttacking` (Bool).
   - Any State -> `Die` (nếu có Trigger `Die` hoặc Logic máu cục bộ gọi tới).
   - Idle -> Run (nếu `isRunning` = true), Run -> Idle (Nếu false).
   - **(Quan trọng)** Mở Clip `MeleeAttack`, thêm Animation Event vào frame ở giữa (lúc vung tay trúng) gọi hàm `OnAttackHit` và frame kết thúc clip gọi hàm `OnAttackFinished`. Cái này giúp Sync sát thương cực mịn.

## Bước 2: Tạo Prefab Enemy (Phần Gốc)
1. Tạo một GameObject rỗng trong phân cảnh (Hierarchy), đặt tên là `Quái_Mới`.
2. Gắn Components kết xuất đồ hoạ: Kéo `SpriteRenderer` và `Animator` trực tiếp lên root GameObject để xử lý tập trung. (Gán Controller ở Bước 1 vào Animator).
3. Thêm các Component vật lý hỗ trợ mạng từ Unity:
   - **Rigidbody2D**: Set Body Type là `Dynamic`, kiểm tra `Freeze Rotation Z` để quái không bị lăn lật ngửa cạn. Xử lý va chạm trọng lực với map (Gravity Scale tuỳ game 2D top down thường là 0).
   - **Collider2D**: Gắn `CapsuleCollider2D` hoặc `BoxCollider2D`. Điều chỉnh kích thước (Edit Collider) vừa đúng khung hình con quái.
4. Thêm các Component quan trọng của Netcode for GameObjects:
   - **NetworkObject**: Trái tim giúp Unity đồng bộ qua Object qua Multiplayer.
   - **NetworkTransform**: Chọn sync vị trí (Position) để client thấy quái lướt mượt. Check vào `Sync Position X/Y`.
   - **NetworkAnimator**: Kéo cái `Animator` ở trên vào để đồng bộ chớp nhoáng những frame chạy, chém của quái mà không cần code custom RPC.

## Bước 3: Thêm các Script Logic của Game (Host & Client)
Trên chính GameObject `Quái_Mới` đó (bên dưới NetworkObject), thêm lần lượt các Script sau:
1. **NetworkEnemyHealth**: Quản lý biến máu bằng `NetworkVariable`, tự động cập nhật và phân phát giá trị máu sang màn hình các ngưởi chơi khác mỗi khi Host thay đổi.
2. **EnemyHealth**: Quản lý máu cục bộ, gắn liền với NetworkEnemyHealth.
3. **EnemyAI**:
   - Mở thanh Script ra trên Inspector điền:
   - `moveSpeed`: Tốc độ di chuyển (VD: 2.5).
   - `detectionRange`: Tầm phát hiện người chơi (VD: 5).
   - `meleeAttackRange`: Tầm vung đòn cận chiến (VD: 1.5).
   - `damage`: Sát thương cận chiến cơ bản sẽ khắc vào người chơi.
   - Tạo một Game Object con, gắn thêm một trigger BoxCollider2D và kéo vào `hitbox` để dùng làm vũ khí chém.
4. **NetworkEnemyController**: Gửi Request lên Server yêu cầu kích hoạt hiệu ứng đánh hoặc nhận lệnh RPC đánh từ Host.
5. **EnemyItemDrop**: Quản lý việc ném vòng lặp Item xu, trang bị rớt trên đất khi máu quái dậm về 0.

## Bước 4: Tạo UI Thanh Máu và Biểu tượng Click mục tiêu
Quái MMO thì cần thanh máu nổi trên đầu và mũi tên target dưới chân khi nhấp chuột.
1. **Selection Indicator (Mũi tên chỉ kích chọn / Phát sáng chân)**:
   - Tạo một con (Child GameObject) nằm trong Quái tên là `SelectionIndicator`.
   - Thêm SpriteRenderer và chọn hình mũi tên hướng xuống hoặc vòng tròn nhỏ phát sáng.
   - Kéo trục Y xuống dưới cùng của con quái (VD: `Y: -0.8`).
   - Tắt (Bỏ ô tick SetActive) mặc định của GameObject này.
2. **Click để xem thông tin (EnemyClickHandler)**:
   - Thêm Component `EnemyClickHandler` lên root của con quái.
   - Kéo cái GameObject `SelectionIndicator` vừa tạo thả chìm vào ô biến `selectionIndicator` trên cửa sổ Inspector.
   - Thao tác: Khi người dùng nhấp (OnMouseDown) lên Collider gốc của quái, script bật sáng Mũi Tên và gọi Panel Tên Kẻ địch lên.
3. **Thanh HP Bar bề nổi trên đầu**:
   - Thêm Component `EnemyHealthBarSpawner` lên gốc.
   - Kéo `EnemyHPBar.prefab` (đã tạo sẵn trong UI folder) thả vào ô `Health Bar Prefab`.
   - Mọi nỗ lực spawn của NetworkManager lúc này sẽ bắt Client tự động nẩy sinh thanh máu ngay khoảnh khắc quái xuất hiện trên trán.

## Bước 5: Đăng ký với Hệ thống Server (NetworkManager & PrefabManager)
Để hàm Spawn qua mạng hoạt động, game cần biết thiết kế của cục Prefab lúc Runtime.
1. Nắm GameObject `Quái_Mới` kéo từ Scene Hierarchy vất vào thư mục `Prefabs/Enemies` để biến nó thành file **`.prefab`**. Xoá bản thể rác đang tồn tại trên Scene đi vì sẽ tự động sinh vào lúc khác.
2. Mở **NetworkManager** (Nằm ở root Scene chính hoặc Bootstrap):
   - Cuộn chuột xuống phần danh sách `NetworkPrefabs`.
   - Nhấn dấu **+** và kéo file Prefab thả vào đó. (Bước cực kỳ quan trọng, nếu quên, lúc Host spawn trên mạng nó báo lỗi *Unregistered hay Soft Sync failed*).
3. Định vị lại Map của Database bằng `EnemyPrefabManager`:
   - Mở mảng list `Enemy Prefabs` ra.
   - Thêm một slot định danh `Enemy Id` (Ví dụ bạn đăng ký cho Quái này ID = 6 trong bảng `enemy` DB).
   - Kéo file Prefab vào. Xong!

## Bước 6: Định vị Toạ độ điểm tự động trồi ra (Spawn Point) trên Map
1. Trong Unity Scene chế độ Editor (Giao diện thiết kế Map).
2. Tạo 1 Game Object rỗng tạm thời, di chuyển đồ thị đến điểm mà bạn muốn châm nổ bầy quái đẻ ra.
3. Nhìn sang bảng Transform Inspector, ghi nhớ lại vị trí điểm neo `Position X` và `Position Y` (Toạ độ Absolute Space của map). Ví dụ: `(X: 18.2, Y: -10.5)`.
4. Bạn lấy Toạ độ này sang DB để trút vào các trường cấu trúc `cx` và `cy` ở SQL. Xoá GameObject nháp đi là hoàn hảo.

## Luồng Tương tác Server tới Client chớp nhoáng (Tổng kết)
- **Chuẩn bị môi trường**: Host vào Room xong chạy `HostSpawnConfigLoader` đọc cấu hình Web API từ REST lấy list Quái (`spawn_json`).
- **Gieo mầm (Spawn)**: Web Server gửi lên toạ độ `(18.2, -10.5)` cần sinh 4 con ID=6. Host gọi `EnemyPrefabManager` lôi cái Prefab ID 6 ra nhân bản làm 4 con và gọi lệnh Ngo: `Spawn()`.
- **Đồng bộ Client hiển thị**: 4 Quái hiện thân trong Map Game của TẤT CẢ Player trong Server. Khi quái lang thang bằng `EnemyAI`, `NetworkTransform` ép Client tịnh tiến vị trí mượt mà.
- **Rớt trang bị (Drop Item)**: Khi máu `EnemyHealth` bị chém đến hạn về 0 -> Host tung ra Event chết -> gọi `EnemyItemDrop` đọc trường `drop_json` trút rớt Quặng thép và Đồ ra sân -> Kết thúc kiếp Enemy.
