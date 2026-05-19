# CHƯƠNG 2. PHÂN TÍCH THIẾT KẾ HỆ THỐNG

Chương 2 tập trung vào việc phân tích và thiết kế toàn diện hệ thống game Mutants Arena. Dựa trên cơ sở lý thuyết đã trình bày tại Chương 1, chương này xác định rõ bài toán cần giải quyết, các đối tượng tham gia, yêu cầu chức năng và phi chức năng, từ đó đưa ra thiết kế kiến trúc tổng thể, biểu đồ ca sử dụng và cơ sở dữ liệu phù hợp. Kết quả của chương này là nền tảng kỹ thuật trực tiếp để triển khai xây dựng game trong Chương 3.

---

## 2.1. Phân tích bài toán và yêu cầu hệ thống

### 2.1.1. Bài toán đặt ra

Game 2D hành động nhập vai nhiều người chơi (Multiplayer 2D Action RPG) là một sản phẩm kỹ thuật số phức tạp đòi hỏi tích hợp đồng thời nhiều hệ thống con độc lập: điều khiển nhân vật theo thời gian thực với vật lý 2D, hệ thống chiến đấu với nhiều loại kỹ năng và hiệu ứng nguyên tố, AI quái vật và boss đa giai đoạn, đồng bộ hóa trạng thái game giữa nhiều người chơi qua mạng, và lưu trữ dữ liệu nhân vật lâu dài trong cơ sở dữ liệu. Sự phức tạp này đặt ra nhiều thách thức kỹ thuật mà các hướng tiếp cận thông thường khó giải quyết thỏa đáng trong cùng một dự án quy mô học thuật.

Bài toán cốt lõi của đề tài là xây dựng tựa game "Mutants Arena" — một game 2D Action RPG side-scrolling nhiều người chơi trên PC, trong đó người chơi điều khiển nhân vật thuộc một trong sáu lớp nguyên tố (Hỏa, Kim, Mộc, Phong, Thổ, Thủy), tham gia khám phá bản đồ, chiến đấu quái vật và boss, nâng cấp hệ thống Gene nguyên tố đặc trưng, và tương tác với người chơi khác qua chế độ nhóm (Party) và phó bản đồng đội (Dungeon). Hệ thống cần đảm bảo trải nghiệm gameplay mượt mà ở cả chế độ đơn lẫn đa người chơi, với dữ liệu nhân vật được lưu trữ an toàn và phục hồi chính xác sau mỗi phiên chơi.

Về mặt kiến trúc kỹ thuật, hệ thống áp dụng mô hình phân tầng rõ ràng: phần client xây dựng bằng Unity 2D với Unity Netcode for GameObjects (NGO) cho chế độ multiplayer; phần backend gồm ASP.NET Core REST API cho các tác vụ persistence (đăng nhập, lưu nhân vật, nâng cấp) và SignalR cho thông tin thời gian thực (hệ thống nhóm, thông báo toàn server). Cơ sở dữ liệu MySQL 8.0 lưu trữ toàn bộ dữ liệu game, trong đó một số trường phức tạp như cấu hình boss, danh sách kỹ năng và tiến trình nhiệm vụ được lưu dưới dạng JSON column để tối ưu tính linh hoạt. Hệ thống được đóng gói và triển khai qua Docker Compose với ba container (game-server, api-server, mysql-db) trên VPS Linux.

Mục tiêu cuối cùng là tạo ra một sản phẩm game chơi được hoàn chỉnh — có đủ vòng lặp gameplay từ tạo nhân vật, chiến đấu, nâng cấp Gene đến khám phá phó bản nhiều người — đồng thời chứng minh khả năng kết hợp Unity, ASP.NET Core, NGO và MySQL trong một dự án thực tế có độ phức tạp cao và kiến trúc chuẩn công nghiệp.

### 2.1.2. Các đối tượng của hệ thống

Đối chiếu trực tiếp với mã nguồn hiện tại cho thấy Mutants Arena không chỉ có các tác nhân nghiệp vụ truyền thống mà còn có các tác nhân kỹ thuật bên ngoài hệ thống game client. Việc bổ sung actor kỹ thuật là cần thiết vì nhiều use case quan trọng trong dự án được thực thi bởi Zone Server, Dungeon Host và các tiến trình gameplay gọi API bằng JWT hoặc Zone API Key thay vì do người chơi thao tác trực tiếp.

Bảng 2.1: Các tác nhân tham gia hệ thống

| Tên tác nhân | Nhóm | Vai trò |
|---|---|---|
| Khách (Guest) | Nghiệp vụ | Người dùng chưa đăng nhập. Chỉ thực hiện đăng ký và đăng nhập tài khoản. |
| Người chơi (Player) | Nghiệp vụ | Tác nhân trung tâm của hệ thống. Thực hiện tạo nhân vật, vào game, di chuyển, chiến đấu, nâng Gene, quản lý túi đồ, làm nhiệm vụ, tương tác NPC, bạn bè, party, chat, dungeon và leaderboard. |
| Quản trị / vận hành (Admin/Operator) | Nghiệp vụ vận hành | Theo dõi trạng thái server, cập nhật spawn config, kiểm tra host map, refresh leaderboard, giám sát session gameplay và xử lý cấu hình vận hành. |
| Máy chủ gameplay / Netcode server (Zone Server / Dungeon Host) | Kỹ thuật | Actor hệ thống kỹ thuật bên ngoài lớp REST API. Xác thực kết nối NGO, gán map/zone ban đầu, đồng bộ vị trí - animation - trạng thái chiến đấu qua NetworkVariable/ClientRpc, cập nhật tiến trình quest theo event runtime, quản lý session dungeon, spawn đối tượng mạng và đăng ký heartbeat/host gameplay. |

### 2.1.3. Yêu cầu chức năng

Sau khi đối chiếu các controller ASP.NET Core, SignalR Hub và phần client Unity, có thể xác định phạm vi chức năng thực sự đã được triển khai theo từng module nghiệp vụ như sau. Trong chương này, một *use case* được hiểu là một mục tiêu nghiệp vụ hoàn chỉnh; các endpoint phụ trợ như `config`, `detail`, `heartbeat` hoặc `refresh` sẽ được gộp vào use case gần nhất nếu chúng cùng phục vụ một mục tiêu nghiệp vụ duy nhất.

**Bảng 2.1a: Phân nhóm chức năng theo module triển khai thực tế**

| Module | Vai trò trong đề tài | Mức ưu tiên | Trạng thái |
|---|---|---|---|
| Xác thực tài khoản + JWT | Khởi tạo phiên và bảo vệ toàn bộ hệ thống | **Cốt lõi** | Hoàn thành |
| Nhân vật + vào game + đồng bộ vị trí | Vòng đời nhân vật và bootstrap gameplay | **Cốt lõi** | Hoàn thành |
| Combat thời gian thực + kỹ năng + buff | Gameplay nền cho Gene và Dungeon | **Cốt lõi** | Hoàn thành |
| **Gene Evolution: Gene chính / Gene phụ / Hybrid Fusion** | **Trục đặc trưng của đề tài** | **Cốt lõi (trọng tâm)** | Hoàn thành |
| Túi đồ + trang bị + Blacksmith | Vòng lặp progression và nâng sức mạnh | **Cốt lõi** | Hoàn thành |
| NPC + Quest + Shop + dịch vụ đặc biệt | Nội dung PvE và điều hướng người chơi | **Cốt lõi** | Hoàn thành |
| Friend + Party + Chat SignalR | Lớp tương tác xã hội thời gian thực | **Cốt lõi** | Hoàn thành cơ bản |
| **Dungeon Wave/Boss + Reward** | **Trục multiplayer co-op chính** | **Cốt lõi (trọng tâm)** | Hoàn thành |
| Leaderboard | Theo dõi thành tích và vòng lặp cạnh tranh mềm | Phụ trợ | Hoàn thành cơ bản |
| Map/Portal/Zone Ops + Spawn Config | Hạ tầng vận hành gameplay server-authoritative | Phụ trợ quan trọng | Hoàn thành |

Các nhóm chức năng đã có trong mã nguồn hiện tại được mô tả chi tiết như sau:

Quản lý tài khoản và phiên đăng nhập:
▪ Đăng ký tài khoản bằng `username`, `email`, `password`; kiểm tra trùng lặp và băm mật khẩu trước khi lưu database
▪ Đăng nhập bằng `username/password`; server cấp JWT, cập nhật `last_login` và ghi nhận điểm danh ngày đầu tiên trong phiên đăng nhập
▪ JWT được gắn vào REST API, SignalR Hub và kết nối gameplay; request không hợp lệ bị từ chối

Quản lý nhân vật và hồ sơ chơi:
▪ Tạo nhân vật chính với `element_type` thuộc 6 hệ: Kim, Mộc, Thủy, Hỏa, Thổ, Phong; tên nhân vật giới hạn từ 3 đến 20 ký tự
▪ Nạp dữ liệu nhân vật, kỹ năng, trang bị, buff đang hoạt động và trạng thái Gene khi vào game
▪ Đồng bộ vị trí, map, zone và trạng thái nhân vật giữa client, gameplay server và API persistence
▪ Nhận EXP, lên cấp tự động, cộng điểm tiềm năng/kỹ năng; hỗ trợ khóa cấp bằng dịch vụ NPC

Di chuyển, bản đồ và chiến đấu:
▪ Di chuyển trái/phải, nhảy, dash và thao tác chiến đấu thời gian thực trong scene 2D side-scrolling
▪ Dùng đòn đánh thường và kỹ năng; server kiểm tra hitbox, cooldown, MP và áp dụng sát thương theo mô hình server-authoritative
▪ Áp dụng tương khắc nguyên tố và hiệu ứng trạng thái Burn, Freeze, Stun, Poison cùng hệ buff/debuff đồng bộ lên HUD
▪ Dịch chuyển qua portal, chuyển map/scene, kiểm tra khoảng cách đến cổng và vật phẩm yêu cầu nếu map có khóa truy cập

Hệ thống Gene Evolution:
▪ Nâng Gene chính từ Tier 1 đến Tier 5 bằng `gene_exp`, vàng và item cấu hình theo DB
▪ Chọn Gene phụ theo cặp cố định đã triển khai trong code: Hỏa↔Thổ, Thủy↔Mộc, Kim↔Phong
▪ Nâng Gene phụ từ Tier 1 đến Tier 5; bonus chỉ số nhỏ hơn Gene chính và là điều kiện để mở Hybrid Fusion
▪ Fusion hai Gene Tier 5 hợp lệ thành Hybrid Gene; hệ thống lưu `hybridId`, `prefabPath`, bonus target, immune element và thêm bộ kỹ năng hybrid

Túi đồ, trang bị và nâng cấp:
▪ Xem túi đồ, sắp xếp inventory, dùng consumable, cộng buff và tự động cập nhật số ô túi
▪ Mang/tháo trang bị giữa inventory và equipment slot; hỗ trợ item mở rộng túi nhanh với giới hạn tối đa 3 quick-slot expansion item
▪ Nâng cấp trang bị tại Blacksmith từ +1 đến +24 bằng đá cường hóa, Lucky Stone, Protection Stone và bạc; server kiểm tra từng slot vật liệu chống gian lận số lượng

NPC, quest và shop:
▪ Liệt kê NPC theo map, tương tác NPC, đi qua từng node hội thoại kế tiếp và mở menu động theo loại NPC
▪ Mua vật phẩm từ NPC shop; server kiểm tra tiền, cấp độ, stock và thêm item vào inventory
▪ Nhận, bỏ, theo dõi và hoàn thành quest; quest progress được cập nhật tự động theo event `kill`, `collect`, `talk`, `reach`
▪ Chỉ cho phép một quest active tại một thời điểm trên mỗi nhân vật
▪ Cung cấp dịch vụ đặc biệt qua NPC: reset potential, reset skill, learn skill, exchange skill, exchange charm, lock/unlock level

Chức năng xã hội và co-op:
▪ Tìm kiếm bạn bè theo tên nhân vật, gửi lời mời, chấp nhận và xóa quan hệ bạn bè
▪ Tạo party tối đa 4 người, gửi invite, xin vào party, bật/tắt auto-accept, lock party, tìm party hoặc người chơi gần đó theo map/zone
▪ Chat thời gian thực qua SignalR với các kênh world, proximity, group, private; phần hạ tầng chat cũng đã có nhánh clan/class trong code

Phó bản và reward:
▪ Liệt kê dungeon, xem chi tiết dungeon, xem boss và reward config
▪ Tạo session dungeon, tham gia session, vào wave, cập nhật session wave, kết thúc wave, kết thúc dungeon và rời dungeon giữa chừng
▪ Cấp thưởng phó bản qua API riêng dùng Zone API Key, thêm item reward trực tiếp vào inventory player

Vận hành gameplay server:
▪ Zone Server đăng ký, heartbeat và deregister với API để báo số map, số player và thống kê zone
▪ Host gameplay kiểm tra/đăng ký/heartbeat/hủy host cho từng map hoặc từng session dungeon
▪ Admin cập nhật spawn config theo JSON cho từng map; server xác thực `enemy_id` trước khi lưu và làm mới cache runtime
▪ Leaderboard hỗ trợ 5 hạng mục: level, quest, attendance, dungeon, gold; có cơ chế cache 5 phút và manual refresh

Các chức năng chưa có luồng hoàn chỉnh trong mã nguồn hiện tại và vì thế không được đưa vào use case chính của chương này gồm marketplace P2P, ranked PvP, achievement và daily quest.

### 2.1.4. Yêu cầu phi chức năng

Ngoài các yêu cầu chức năng, hệ thống cần đáp ứng một tập yêu cầu phi chức năng để đảm bảo trải nghiệm người chơi ở mức chấp nhận được trong điều kiện vận hành thực tế:

▪ **Hiệu năng**: Vòng lặp game Unity duy trì tối thiểu 60 FPS trên phần cứng đáp ứng cấu hình khuyến nghị; độ trễ đồng bộ hóa vị trí nhân vật không vượt quá 100 ms trên mạng LAN và 200 ms trên mạng WAN thông thường
▪ **Khả năng chịu tải**: NGO Dedicated Server hỗ trợ tối thiểu 4 người chơi kết nối đồng thời trên một zone; REST API xử lý tối thiểu 100 request/giây trong điều kiện bình thường mà không làm tăng thời gian phản hồi trung bình
▪ **Độ tin cậy**: Dữ liệu nhân vật được lưu tự động sau mỗi sự kiện quan trọng; khi ngắt kết nối đột ngột, nhân vật được xử lý graceful disconnect — server lưu trạng thái hiện tại trước khi xóa khỏi scene
▪ **Khả năng phục hồi**: Khi mất kết nối với server, client hiển thị thông báo và cho phép thử kết nối lại mà không cần khởi động lại ứng dụng
▪ **Khả năng mở rộng**: Kiến trúc Zone-based (ZoneRoomRegistry) cho phép thêm zone/dungeon mới mà không ảnh hưởng zone đang chạy; các hệ thống game (Quest, Gene, Equipment) được thiết kế module độc lập
▪ **Khả năng bảo trì**: Source code tổ chức theo nguyên tắc separation of concerns; mỗi hệ thống là một module riêng; thay đổi logic một hệ thống không ảnh hưởng đến hệ thống khác
▪ **Tương thích**: Game chạy ổn định trên Windows 10/11 64-bit; yêu cầu tối thiểu GPU hỗ trợ DirectX 11 và 4 GB RAM

### 2.1.5. Yêu cầu bảo mật

Trong game multiplayer với mô hình Server Authoritative, bảo mật là yêu cầu thiết yếu để ngăn chặn gian lận (cheating) và bảo vệ dữ liệu người chơi.

Xác thực và ủy quyền: Mọi request REST API đều yêu cầu JWT token hợp lệ trong header Authorization. Token được tạo bằng thuật toán HS256 với secret key lưu trong biến môi trường server — không được hardcode trong source code hay image Docker. Token hết hạn sau 24 giờ. Kết nối NGO cũng yêu cầu JWT hợp lệ trong ConnectionApproval callback; kết nối không có token hợp lệ bị từ chối tức thì và ghi log.

Mã hóa mật khẩu: Mật khẩu người dùng không bao giờ được lưu dạng plaintext. Hệ thống sử dụng BCrypt với cost factor 12 để hash mật khẩu trước khi ghi vào database. Quá trình xác thực so sánh hash — không bao giờ có thao tác giải mã ngược mật khẩu gốc. Phản hồi lỗi đăng nhập không tiết lộ field nào sai (tên đăng nhập hay mật khẩu) để tránh user enumeration.

Server Authoritative — chống gian lận: Mọi tính toán game-critical (HP, EXP, sát thương, vị trí) được thực hiện và xác nhận trên Dedicated Server. Client chỉ gửi input (phím bấm, hướng di chuyển, ID kỹ năng được dùng), không gửi kết quả tính toán. Giá trị HP hay EXP đến từ client đều bị bỏ qua hoàn toàn — chỉ server mới có quyền cập nhật các giá trị này. Cơ chế này triệt tiêu phần lớn các công cụ memory hack hay speed hack phía client.

Bảo vệ database: Thông tin kết nối database (host, user, password, database name) được truyền qua biến môi trường Docker Compose — không được nhúng trong image hay commit vào source code. Toàn bộ truy vấn database thực hiện qua Entity Framework Core với parameterized query, loại bỏ nguy cơ SQL Injection. Người dùng database được cấp quyền tối thiểu cần thiết (không dùng root).

---

## 2.2. Thiết kế kiến trúc hệ thống

### 2.2.1. Mô hình tổng thể

Hệ thống Mutants Arena được thiết kế theo kiến trúc phân ba tầng rõ ràng: tầng Client (Unity), tầng Server (NGO Dedicated Server + ASP.NET Core), và tầng Dữ liệu (MySQL). Mỗi tầng đảm nhiệm trách nhiệm xác định và giao tiếp với các tầng khác qua giao thức chuẩn, đảm bảo tính tách biệt và khả năng thay thế độc lập. Toàn bộ hệ thống được đóng gói và triển khai qua Docker Compose trên VPS Linux gồm ba container chạy song song.

Tầng Client (Unity 2D): Đây là tầng người chơi trực tiếp tương tác. Client được xây dựng bằng Unity 2D 2022.3 LTS, chịu trách nhiệm về rendering, xử lý input người chơi, animation, hiệu ứng âm thanh và toàn bộ giao diện người dùng (HUD, menu, dialog, inventory). Client kết nối với NGO Dedicated Server qua giao thức UDP (Unity Transport Package) để nhận và gửi game state theo thời gian thực. Song song đó, client gọi REST API qua HTTPS để thực hiện các tác vụ có tính persistence: đăng nhập, tải dữ liệu nhân vật, nâng cấp Gene, mua vật phẩm và hoàn thành nhiệm vụ. SignalR WebSocket được sử dụng riêng cho hệ thống Party và thông báo global.

Tầng Server (Game Server + API Server): Game Server là bản build Unity Dedicated Server — Unity runtime không có giao diện đồ họa, chạy trên Linux container. Server này là "nguồn sự thật" (source of truth) cho mọi trạng thái game: tính toán sát thương với nguyên tố multiplier, kiểm tra va chạm hitbox, điều khiển vòng đời AI quái vật và boss, quản lý Zone và Dungeon Instance. Khi có sự kiện xảy ra (quái chết, boss vào phase mới, người chơi lên cấp), server đồng bộ kết quả xuống tất cả client qua NetworkVariable và ClientRpc. API Server là ASP.NET Core 7 Web API, xử lý các endpoint REST: xác thực JWT, CRUD nhân vật và trang bị, hệ thống Gene và Quest. API Server cũng host SignalR Hub phục vụ Party system và thông báo.

Tầng Dữ liệu (MySQL 8.0): MySQL lưu trữ toàn bộ dữ liệu game có tính lâu dài: tài khoản, nhân vật, trang bị, Gene, nhiệm vụ, cấu hình map và boss. Một số trường phức tạp được lưu dạng JSON column để tránh over-normalization trong khi vẫn hỗ trợ truy vấn JSON_EXTRACT. Entity Framework Core 7 được dùng làm ORM với Code First Migration, cho phép cập nhật schema tự động và có version control.

Mô hình tổng thể được minh họa qua sơ đồ kiến trúc (Hình 2.1):

▪ Tầng trên: Unity Client kết nối đến Game Server qua UDP/NGO và đến API Server qua HTTPS/REST
▪ Tầng giữa: Game Server (Unity DS) và API Server (ASP.NET Core) giao tiếp nội bộ trong Docker network; Game Server gọi API Server khi cần cập nhật dữ liệu persistence
▪ Tầng dưới: MySQL Database được truy cập độc quyền bởi API Server; không có kết nối trực tiếp từ Game Server hay Client đến database

Hình 2.1: Sơ đồ kiến trúc tổng thể hệ thống Mutants Arena (Unity Client — Game Server/API Server — MySQL)

### 2.2.1a. Lý do chọn kiến trúc multiplayer Server-Authoritative

Cùng với câu hỏi "vì sao chia ba tầng?", một câu hỏi cốt yếu khác mà đề tài phải trả lời rõ là *"vì sao chọn mô hình Server-Authoritative cho multiplayer mà không phải Client-Authoritative hay Peer-to-Peer?"*. Đây là quyết định ảnh hưởng đến toàn bộ luồng đồng bộ combat và Gene upgrade ở Chương 3, đồng thời quyết định khả năng chống gian lận của hệ thống.

**Bảng 2.0b: So sánh ba mô hình đồng bộ multiplayer cho game RPG online**

| Mô hình | Cơ chế cốt lõi | Ưu điểm | Nhược điểm | Phù hợp với đề tài? |
|---|---|---|---|---|
| **Client Authoritative** | Mỗi client tự tính damage, HP, vị trí; chỉ gửi *kết quả* cho server forward sang client khác | Phản hồi tức thì, code đơn giản | Dễ hack damage/gold/loot bằng memory editor; không thể kiểm chứng | ❌ Không phù hợp với RPG có persistence |
| **Peer-to-Peer (P2P)** | Các client trực tiếp giao tiếp, một client làm "host" tạm | Không cần dedicated server, chi phí thấp | Host có lợi thế bất công; NAT traversal phức tạp; mất host gây sập trận | ❌ Không phù hợp game public |
| **Server Authoritative** *(dedicated)* | Server là "source of truth" duy nhất; client chỉ gửi *input*, server tính và phát kết quả | Chống cheat tốt; dữ liệu nhất quán; dễ scale theo zone | Cần xử lý latency (RTT); cần host VPS; code phức tạp hơn | ✅ **Chọn** — bắt buộc cho RPG online |
| Hybrid *(Prediction + Reconciliation)* | Server Authoritative + client dự đoán cục bộ và sửa khi nhận snapshot | Vừa mượt vừa an toàn | Cài đặt khó nhất | ✅ **Chọn áp dụng cho di chuyển** |

→ **Kết luận đề tài chọn Server Authoritative làm mô hình chính** cho mọi tính toán game-critical (damage, HP, EXP, Gene upgrade, loot drop, dungeon completion), kết hợp **Client Prediction + Server Reconciliation** chỉ ở tầng di chuyển nhân vật để giữ phản hồi input ≤ 16 ms (60 FPS). Mọi giá trị HP, gold, gene_tier do client gửi lên đều bị server bỏ qua — chỉ nhận từ input thô (phím bấm, hướng nhắm, skill_id). Chi tiết triển khai mô hình này được trình bày ở §3.0 và §3.3.

### 2.2.2. Biểu đồ Use Case tổng quát

Để phản ánh đúng dự án đang triển khai, phần use case của Mutants Arena được tổ chức thành hai mức: mức tổng quát theo module chức năng và mức chi tiết theo ca sử dụng hoàn chỉnh. Mức tổng quát dùng để trình bày phạm vi hệ thống; mức chi tiết dùng để đặc tả luồng nghiệp vụ đủ gần với mã nguồn hiện tại nhưng vẫn giữ được ý nghĩa phân tích hệ thống.

**Bảng 2.1b: Danh mục use case toàn hệ thống Mutants Arena**

| Mã | Use case | Actor chính | Nhóm chức năng |
|---|---|---|---|
| UC01 | Đăng ký tài khoản | Guest | Xác thực |
| UC02 | Đăng nhập và khởi tạo phiên JWT | Guest / Player | Xác thực |
| UC03 | Tạo nhân vật chính và vào game | Player | Nhân vật |
| UC04 | Di chuyển qua portal và chuyển map | Player | Bản đồ |
| UC05 | Chiến đấu và dùng kỹ năng thời gian thực | Player | Combat |
| UC06 | Quản lý túi đồ, vật phẩm và trang bị | Player | Inventory |
| UC07 | Nâng cấp trang bị tại Blacksmith | Player | Trang bị |
| UC08 | Nâng Gene chính | Player | Gene Evolution |
| UC09 | Chọn và nâng Gene phụ | Player | Gene Evolution |
| UC10 | Dung hợp Hybrid Gene | Player | Gene Evolution |
| UC11 | Phân bổ tiềm năng và quản lý kỹ năng | Player | Progression |
| UC12 | Tương tác NPC, mua vật phẩm và dùng dịch vụ đặc biệt | Player | NPC / Shop |
| UC13 | Nhận, bỏ, theo dõi và hoàn thành nhiệm vụ | Player / Máy chủ gameplay | Quest |
| UC14 | Quản lý bạn bè | Player | Social |
| UC15 | Tạo và quản lý tổ đội | Player | Party |
| UC16 | Chat đa kênh thời gian thực | Player | Chat |
| UC17 | Tạo, tham gia và hoàn tất phó bản | Player / Máy chủ gameplay | Dungeon |
| UC18 | Xem và làm mới leaderboard | Player / Admin | Leaderboard |
| UC19 | Đăng ký, heartbeat và giải phóng gameplay server | Máy chủ gameplay / Admin | Vận hành |
| UC20 | Đăng ký host map, cập nhật spawn config và phát thưởng dungeon | Máy chủ gameplay / Admin | Vận hành |

Hình 2.2 Biểu đồ Usecase tổng quát hệ thống Mutants Arena

Mô tả: Vùng hệ thống mang tên "Hệ thống Mutants Arena" bao gồm các cụm use case mức cao: xác thực tài khoản, quản lý nhân vật, gameplay chiến đấu, Gene Evolution, inventory - trang bị, NPC - nhiệm vụ - cửa hàng, bạn bè - tổ đội - chat, phó bản đồng đội, đồng bộ thời gian thực, chuyển zone/visibility, spawn runtime, leaderboard và vận hành gameplay server. Bên ngoài vùng hệ thống có bốn actor: Khách, Người chơi, Quản trị viên và Máy chủ gameplay / Netcode server. Các actor này tương tác với những nhóm chức năng khác nhau tùy theo vai trò nghiệp vụ và vai trò kỹ thuật của mình.

Ngoài sơ đồ tổng quát, bộ tài liệu còn có một sơ đồ tổng hợp chi tiết toàn bộ UC01 đến UC20 để đối chiếu giữa danh mục use case và các nhóm chức năng chuyên biệt.

Các sơ đồ Usecase chi tiết của chương này được biên tập theo cùng một mẫu trình bày của luận văn; trong nội dung chương chỉ giữ phần hình minh họa và phần đặc tả nghiệp vụ tương ứng.

### 2.2.3. Đặc tả Use Case chi tiết

Phần đặc tả dưới đây mô tả đầy đủ các use case đã có trong dự án ở mức mục tiêu nghiệp vụ. Những endpoint phụ trợ như `config`, `detail`, `list`, `refresh`, `heartbeat` được gộp vào use case tương ứng để tránh tách nhỏ quá mức kỹ thuật.

a) Biểu đồ Usecase chức năng Xác thực và khởi tạo nhân vật

Hình 2.3 Biểu đồ Usecase chức năng Xác thực và khởi tạo nhân vật

Bảng 2.3 Bảng đặc tả nhóm chức năng Xác thực và khởi tạo nhân vật

| Trường | Nội dung |
|---|---|
| **Mã / Use Case** | **UC01 - Đăng ký tài khoản** |
| **Actor chính** | Guest |
| **Mô tả** | Người dùng tạo tài khoản mới bằng `username`, `email`, `password`; hệ thống kiểm tra trùng lặp và lưu mật khẩu dưới dạng hash. |
| **Tiền điều kiện** | 1. Người dùng đang ở màn hình Register.<br>2. API Auth đang sẵn sàng. |
| **Luồng chính** | 1. Người dùng nhập thông tin đăng ký.<br>2. Client gửi `POST /api/auth/register`.<br>3. Server kiểm tra `username/email` chưa tồn tại.<br>4. Server hash password, tạo bản ghi user, phát JWT và trả `user_id`.<br>5. Client thông báo đăng ký thành công và khởi tạo phiên đầu tiên hoặc chuyển về login tùy luồng UI. |
| **Luồng thay thế / ngoại lệ** | 1. Thiếu trường bắt buộc → trả `400 BadRequest`.<br>2. `username` hoặc `email` đã tồn tại → trả lỗi trùng lặp.<br>3. Lỗi kết nối API → client hiển thị lỗi và cho phép thử lại. |
| **Hậu điều kiện** | Tài khoản được tạo thành công và có thể dùng ngay cho đăng nhập hoặc khởi tạo phiên đầu tiên. |

| Trường | Nội dung |
|---|---|
| **Mã / Use Case** | **UC02 - Đăng nhập và khởi tạo phiên JWT** |
| **Actor chính** | Guest / Player |
| **Mô tả** | Người dùng xác thực bằng `username/password`; server cấp JWT, cập nhật `last_login` và ghi nhận điểm danh ngày. |
| **Tiền điều kiện** | 1. Tài khoản đã tồn tại.<br>2. API Server đang hoạt động. |
| **Luồng chính** | 1. Người dùng nhập tài khoản và mật khẩu.<br>2. Client gửi `POST /api/auth/login`.<br>3. Server tìm user theo `username` và xác thực password hash.<br>4. Server cập nhật `last_login`, gọi logic điểm danh ngày và phát JWT mới.<br>5. Client lưu token, khởi tạo các service gameplay/SignalR và chuyển sang bước tạo hoặc nạp nhân vật. |
| **Luồng thay thế / ngoại lệ** | 1. Sai `username/password` → trả `401 Unauthorized` với thông báo chung.<br>2. API timeout → client báo lỗi kết nối.<br>3. Token lỗi hoặc không lưu được → không cho đi tiếp vào gameplay. |
| **Hậu điều kiện** | Phiên người dùng hợp lệ được khởi tạo; JWT sẵn sàng dùng cho REST API, SignalR và gameplay server. |

| Trường | Nội dung |
|---|---|
| **Mã / Use Case** | **UC03 - Tạo nhân vật chính và vào game** |
| **Actor chính** | Player |
| **Mô tả** | Người chơi tạo nhân vật chính theo 1 trong 6 hệ nguyên tố, nạp dữ liệu nhân vật và kết nối vào gameplay world. |
| **Tiền điều kiện** | 1. Người chơi đã có JWT hợp lệ.<br>2. Tài khoản chưa có dữ liệu `player_data` hoặc đang cần nạp lại dữ liệu nhân vật. |
| **Luồng chính** | 1. Người chơi chọn `element_type` và nhập tên nhân vật.<br>2. Client gửi `POST /api/player/create`.<br>3. Server kiểm tra tính hợp lệ của tên, hệ và ràng buộc `user_id` từ JWT.<br>4. Server tạo `PlayerData`, gán `InfoChar` mặc định theo hệ nguyên tố.<br>5. Client gọi các API nạp dữ liệu như `data`, `skills`, `equipment`, `active-buffs` và chuẩn bị kết nối gameplay server.<br>6. Netcode server thực hiện connection approval, gán map/zone ban đầu, trả vị trí entry point và đưa nhân vật vào scene gameplay. |
| **Luồng thay thế / ngoại lệ** | 1. Tên nhân vật rỗng hoặc dài ngoài 3-20 ký tự → trả lỗi validation.<br>2. Tài khoản đã có nhân vật chính → trả `409 Conflict`.<br>3. Kết nối gameplay server thất bại → quay về màn hình chờ và cho phép thử lại. |
| **Hậu điều kiện** | Nhân vật chính được tạo hoặc nạp thành công; người chơi đi vào vòng lặp gameplay. |

b) Biểu đồ Usecase chức năng Gameplay cốt lõi

Hình 2.4 Biểu đồ Usecase chức năng Gameplay cốt lõi

Bảng 2.4 Bảng đặc tả nhóm chức năng Gameplay cốt lõi

| Trường | Nội dung |
|---|---|
| **Mã / Use Case** | **UC04 - Di chuyển qua portal và chuyển map** |
| **Actor chính** | Player |
| **Mô tả** | Người chơi di chuyển giữa các map/scene thông qua portal hoặc nút chuyển map; server xác thực vị trí và điều kiện truy cập. |
| **Tiền điều kiện** | 1. Nhân vật đang online trong gameplay world.<br>2. Map hiện tại có portal đang active. |
| **Luồng chính** | 1. Client lấy danh sách portal hoặc portal trái/phải của map hiện tại.<br>2. Người chơi chạm vùng portal hoặc kích hoạt thao tác chuyển map.<br>3. Client gửi `POST /api/map/travel` kèm `portal_id`, `current_map_id`, vị trí nhân vật.<br>4. Server kiểm tra đúng source map, khoảng cách đến portal và item yêu cầu nếu có khóa truy cập.<br>5. Server trả `dest_map_id`, `scene_name`, `dest_x`, `dest_y`.<br>6. Client tải scene đích và reposition nhân vật. |
| **Luồng thay thế / ngoại lệ** | 1. Người chơi đứng quá xa portal → từ chối dịch chuyển.<br>2. Thiếu item chìa khóa của portal → từ chối truy cập.<br>3. Portal bị khóa hoặc không tồn tại → trả lỗi. |
| **Hậu điều kiện** | Nhân vật được dịch chuyển thành công đến map đích và tiếp tục gameplay ở zone mới. |

| Trường | Nội dung |
|---|---|
| **Mã / Use Case** | **UC05 - Chiến đấu và dùng kỹ năng thời gian thực** |
| **Actor chính** | Player |
| **Actor phụ** | Máy chủ gameplay |
| **Mô tả** | Người chơi thực hiện di chuyển chiến đấu, tấn công thường, dùng kỹ năng, nhận/trả sát thương và kích hoạt buff/debuff theo mô hình server-authoritative. |
| **Tiền điều kiện** | 1. Người chơi đã vào scene gameplay.<br>2. Nhân vật còn sống và có quyền điều khiển. |
| **Luồng chính** | 1. Người chơi di chuyển, nhảy hoặc dash để tiếp cận mục tiêu.<br>2. Player gửi input tấn công hoặc cast skill đến gameplay server.<br>3. Server kiểm tra cooldown, MP, hitbox và quyền thi triển.<br>4. Server tính sát thương, áp dụng tương khắc nguyên tố và trạng thái Burn/Freeze/Stun/Poison nếu có.<br>5. Netcode server đồng bộ vị trí, hướng mặt, animation chiến đấu, HP/buff và các trạng thái liên quan xuống client bằng NetworkVariable, ClientRpc hoặc cơ chế sync phù hợp; HUD cập nhật theo thời gian thực.<br>6. Khi enemy chết, gameplay server kích hoạt loot, EXP và các hook như quest progress/report reward. |
| **Luồng thay thế / ngoại lệ** | 1. Không đủ MP hoặc skill đang cooldown → thao tác bị từ chối.<br>2. Mục tiêu ngoài phạm vi/hitbox → không ghi nhận hit.<br>3. Player tử vong → nhân vật vào trạng thái chết/hồi sinh theo logic runtime. |
| **Hậu điều kiện** | Trạng thái chiến đấu được đồng bộ nhất quán; EXP, drop và hiệu ứng phụ được xử lý server-side. |

| Trường | Nội dung |
|---|---|
| **Mã / Use Case** | **UC06 - Quản lý túi đồ, vật phẩm và trang bị** |
| **Actor chính** | Player |
| **Mô tả** | Người chơi xem inventory, sắp xếp túi, dùng consumable, gắn/tháo trang bị và quản lý quick-slot mở rộng túi. |
| **Tiền điều kiện** | 1. Người chơi đã có dữ liệu inventory hợp lệ.<br>2. Nhân vật đang ở trạng thái cho phép thao tác UI. |
| **Luồng chính** | 1. Người chơi mở UI túi đồ.<br>2. Client lấy dữ liệu inventory/equipment hiện tại từ API cache hoặc refresh từ server.<br>3. Người chơi có thể sắp xếp túi, dùng item, gắn trang bị vào slot hoặc tháo ra khỏi slot.<br>4. Server cập nhật `inventory`, `equipment`, `active-buffs` và chỉ số tổng hợp của nhân vật.<br>5. Client làm mới UI túi đồ, thông tin nhân vật và HUD buff. |
| **Luồng thay thế / ngoại lệ** | 1. Túi đầy → không thể thêm item mới.<br>2. Item không hợp lệ hoặc sai loại slot → từ chối thao tác.<br>3. Đã gắn tối đa 3 quick-slot mở rộng túi → không cho gắn thêm. |
| **Hậu điều kiện** | Inventory, equipment và chỉ số nhân vật được lưu nhất quán sau thao tác. |

| Trường | Nội dung |
|---|---|
| **Mã / Use Case** | **UC07 - Nâng cấp trang bị tại Blacksmith** |
| **Actor chính** | Player |
| **Mô tả** | Người chơi nâng cấp một trang bị lên các mốc cao hơn bằng đá cường hóa, Lucky Stone, Protection Stone và bạc. |
| **Tiền điều kiện** | 1. Có trang bị hợp lệ trong inventory hoặc equipment slot.<br>2. Có đủ vật liệu và tiền theo cấu hình bậc nâng cấp.<br>3. Đang mở giao diện Blacksmith hoặc giao diện tương đương. |
| **Luồng chính** | 1. Client gọi API config của bậc mục tiêu để lấy `stoneId`, `stoneNeeded`, `baseSuccessRate`, `failPolicy`.<br>2. Người chơi chọn vật phẩm nâng cấp và các slot vật liệu tương ứng.<br>3. Client gửi `POST /api/upgrade/equipment` kèm danh sách slot đá/charm thực dùng.<br>4. Server kiểm tra số lượng từng slot chống gian lận stack, trừ bạc và vật liệu.<br>5. Server tính kết quả nâng cấp, cập nhật `upgradeLevel`, `strOptions` và các bonus liên quan.<br>6. Client hiển thị kết quả thành công/thất bại và cập nhật lại inventory/equipment. |
| **Luồng thay thế / ngoại lệ** | 1. Không đủ vật liệu hoặc bạc → trả lỗi rõ nguyên nhân.<br>2. Vật phẩm đã đạt +24 → không cho nâng thêm.<br>3. Theo `failPolicy`, nâng cấp thất bại có thể giữ nguyên hoặc tụt cấp tùy mốc. |
| **Hậu điều kiện** | Trạng thái trang bị được lưu sau lần cường hóa; chỉ số nhân vật thay đổi theo kết quả cuối cùng. |

| Trường | Nội dung |
|---|---|
| **Mã / Use Case** | **UC08 - Nâng Gene chính** |
| **Actor chính** | Player |
| **Mô tả** | Người chơi nâng Gene chính từ Tier 1 đến Tier 5 bằng gene EXP, vàng và vật liệu theo cấu hình DB. |
| **Tiền điều kiện** | 1. Nhân vật đã có Gene chính.<br>2. Tier hiện tại nhỏ hơn 5.<br>3. Đủ `gene_exp`, vàng và item yêu cầu. |
| **Luồng chính** | 1. Client lấy `GET /api/gene/config` theo `elementType` và `tier` hiện tại.<br>2. Người chơi chọn số lượng item hỗ trợ nâng cấp.<br>3. Client gửi `POST /api/gene/upgrade`.<br>4. Server kiểm tra điều kiện tài nguyên, tiêu hao vàng/item/gene EXP và tính xác suất thành công.<br>5. Nếu thành công, Gene tăng Tier, cộng thêm HP/MP/ATK/DEF và mở khóa kỹ năng đúng `gene_tier`.<br>6. Client cập nhật UI Gene, final stats và inventory. |
| **Luồng thay thế / ngoại lệ** | 1. Gene đã đạt Tier 5 → trả lỗi tối đa.<br>2. Thiếu `gene_exp`, vàng hoặc vật liệu → từ chối nâng cấp.<br>3. Nâng cấp thất bại → tier không tăng nhưng tài nguyên vẫn tiêu hao theo cấu hình gọi API. |
| **Hậu điều kiện** | Trạng thái Gene chính, inventory và chỉ số nhân vật được lưu mới. |

| Trường | Nội dung |
|---|---|
| **Mã / Use Case** | **UC09 - Chọn và nâng Gene phụ** |
| **Actor chính** | Player |
| **Mô tả** | Người chơi chọn một Gene phụ hợp lệ cho nhân vật và nâng Gene phụ đến Tier 5 để chuẩn bị Hybrid Fusion. |
| **Tiền điều kiện** | 1. Nhân vật đã có Gene chính.<br>2. Nếu là bước chọn Gene phụ: nhân vật chưa chọn Gene phụ trước đó.<br>3. Nếu là bước nâng Gene phụ: đã có `secondaryElement`. |
| **Luồng chính** | 1. Người chơi chọn Gene phụ lần đầu bằng `POST /api/gene/secondary/select`.<br>2. Server kiểm tra cặp đối tác cố định theo code: Hỏa↔Thổ, Thủy↔Mộc, Kim↔Phong.<br>3. Sau khi chọn thành công, client lấy `GET /api/gene/multi/config` cho Tier hiện tại.<br>4. Người chơi gửi `POST /api/gene/secondary/upgrade` để nâng hệ phụ.<br>5. Server tính tiêu hao và xác suất thành công; nếu thành công thì tăng Tier và cộng bonus chỉ số hệ phụ ở mức giảm hệ số so với Gene chính.<br>6. Khi Gene chính và Gene phụ cùng Tier 5, hệ thống bật cờ `canFuse`. |
| **Luồng thay thế / ngoại lệ** | 1. Gene phụ đã được chọn trước đó → không cho đổi hệ.<br>2. Chọn sai cặp nguyên tố → trả lỗi không hợp lệ.<br>3. Thiếu vàng/vật liệu/gene EXP hệ phụ → nâng cấp bị từ chối. |
| **Hậu điều kiện** | Gene phụ được khóa theo cặp hợp lệ và/hoặc đạt Tier mới, sẵn sàng cho Fusion nếu đủ điều kiện. |

| Trường | Nội dung |
|---|---|
| **Mã / Use Case** | **UC10 - Dung hợp Hybrid Gene** |
| **Actor chính** | Player |
| **Mô tả** | Người chơi hợp nhất Gene chính và Gene phụ đã đạt điều kiện thành Hybrid Gene để mở bonus chiến đấu và kỹ năng hybrid. |
| **Tiền điều kiện** | 1. Chưa là Hybrid.<br>2. Gene chính và Gene phụ đều đạt Tier 5.<br>3. Cặp hệ nằm trong 3 cặp hybrid hợp lệ.<br>4. Đủ vàng và số lượng Fusion Core theo config. |
| **Luồng chính** | 1. Client gọi `GET /api/gene/hybrid/config` để lấy điều kiện fusion, item, gold cost và bonus.<br>2. Người chơi xác nhận fusion.<br>3. Client gửi `POST /api/gene/hybrid/fuse`.<br>4. Server kiểm tra điều kiện, trừ vàng và Fusion Core theo hệ chính.<br>5. Server đánh dấu `IsHybrid=true`, lưu `HybridId`, `HybridPrefabPath`, bonus target, immune elements và bonus ATK/HP/MP/DEF.<br>6. Server thay đổi bộ kỹ năng để giữ một phần kỹ năng hệ chính và thêm kỹ năng hybrid.<br>7. Client cập nhật UI hybrid, prefab và final stats. |
| **Luồng thay thế / ngoại lệ** | 1. Cặp hệ không hợp lệ hoặc chưa đủ Tier → trả lỗi chi tiết.<br>2. Thiếu Fusion Core hoặc vàng → từ chối fusion.<br>3. Nhân vật đã là Hybrid → không cho thực hiện lại. |
| **Hậu điều kiện** | Nhân vật chuyển sang trạng thái Hybrid Gene với bonus, prefab và bộ kỹ năng mới. |

| Trường | Nội dung |
|---|---|
| **Mã / Use Case** | **UC11 - Phân bổ tiềm năng và quản lý kỹ năng** |
| **Actor chính** | Player |
| **Mô tả** | Người chơi tăng điểm chỉ số và quản lý kỹ năng đã học thông qua Character Panel và các dịch vụ NPC. |
| **Tiền điều kiện** | 1. Người chơi có điểm tiềm năng hoặc điểm kỹ năng, hoặc đứng gần NPC dịch vụ tương ứng.<br>2. Phiên đăng nhập còn hiệu lực. |
| **Luồng chính** | 1. Client gọi API lấy `potential` và `skills` hiện tại.<br>2. Người chơi phân bổ điểm tiềm năng hoặc nâng level skill trực tiếp từ UI nhân vật.<br>3. Nếu cần reset/learn/exchange skill hoặc reset potential, client gọi các API `npc/action/*` tương ứng.<br>4. Server kiểm tra bạc, điểm kỹ năng và tính hợp lệ của skill trước khi áp dụng thay đổi.<br>5. Client cập nhật lại chỉ số, skill list và trạng thái `level lock` nếu thao tác có liên quan. |
| **Luồng thay thế / ngoại lệ** | 1. Không đủ điểm hoặc skill đã đạt level tối đa → từ chối nâng.<br>2. Không đủ bạc cho dịch vụ NPC → trả thông báo lỗi.<br>3. Đổi sang skill đã có hoặc reset khi không có dữ liệu tương ứng → thao tác không hợp lệ. |
| **Hậu điều kiện** | Chỉ số tiềm năng, điểm kỹ năng, skill list và trạng thái khóa cấp được lưu mới. |

c) Biểu đồ Usecase chức năng NPC, nhiệm vụ và cửa hàng

Hình 2.5 Biểu đồ Usecase chức năng NPC, nhiệm vụ và cửa hàng

Bảng 2.5 Bảng đặc tả nhóm chức năng NPC, nhiệm vụ và cửa hàng

| Trường | Nội dung |
|---|---|
| **Mã / Use Case** | **UC12 - Tương tác NPC, mua vật phẩm và dùng dịch vụ đặc biệt** |
| **Actor chính** | Player |
| **Mô tả** | Người chơi tương tác với NPC trên map để đọc hội thoại, mở shop hoặc gọi các màn hình chức năng như Blacksmith, Dungeon Menu, Skill Service. |
| **Tiền điều kiện** | 1. NPC đang active trên map hiện tại.<br>2. Người chơi ở đủ gần để tương tác. |
| **Luồng chính** | 1. Client lấy danh sách NPC theo map qua `GET /api/npc/list`.<br>2. Người chơi chọn một NPC và gửi `POST /api/npc/interact`.<br>3. Server trả node hội thoại đầu tiên và loại NPC/menu phù hợp.<br>4. Nếu còn hội thoại, client gửi `POST /api/npc/dialogue/next` để đi tiếp các node.<br>5. Nếu là shop, client gọi `GET /api/npc/shop` và người chơi thực hiện `POST /api/npc/shop/buy` để mua item.<br>6. Nếu là NPC dịch vụ, client điều hướng sang Blacksmith, Dungeon menu hoặc các API `npc/action/*` tương ứng. |
| **Luồng thay thế / ngoại lệ** | 1. NPC không tồn tại hoặc bị vô hiệu hóa → trả lỗi not found.<br>2. Người chơi không đủ cấp hoặc không đủ tiền để mua item → từ chối giao dịch.<br>3. Shop chưa cấu hình đúng → server báo lỗi `Shop NPC này chưa được cấu hình`. |
| **Hậu điều kiện** | Người chơi hoàn tất hội thoại, mua vật phẩm hoặc mở được luồng chức năng chuyên biệt từ NPC. |

| Trường | Nội dung |
|---|---|
| **Mã / Use Case** | **UC13 - Nhận, bỏ, theo dõi và hoàn thành nhiệm vụ** |
| **Actor chính** | Player |
| **Actor phụ** | Máy chủ gameplay |
| **Mô tả** | Người chơi nhận quest từ NPC, theo dõi tiến trình trong HUD và hoàn thành quest bằng các event runtime của gameplay server. |
| **Tiền điều kiện** | 1. Người chơi đã đăng nhập và có nhân vật active.<br>2. Đứng gần NPC phát quest hoặc đang có quest active.<br>3. Nếu nhận quest mới, nhân vật không được có quest active khác. |
| **Luồng chính** | 1. Client gọi `GET /api/quest/list?npcId=...` để xem quest theo trạng thái `available/active/completed/locked`.<br>2. Người chơi nhận quest bằng `POST /api/quest/accept`.<br>3. Server ghi `ActiveQuestId`, reset `QuestStep` và `QuestProgress` cho player.<br>4. Trong gameplay, Zone Server gửi `POST /api/quest/progress-by-event` cho các sự kiện `kill`, `collect`, `talk`, `reach`.<br>5. HUD cập nhật tiến trình active quest theo phản hồi từ server.<br>6. Người chơi có thể bỏ quest bằng `POST /api/quest/abandon`.<br>7. Khi đạt đủ điều kiện, người chơi gửi `POST /api/quest/complete`; server phát EXP, vàng, bạc, item và đánh dấu quest hoàn thành. |
| **Luồng thay thế / ngoại lệ** | 1. Nhân vật chưa đủ level quest → quest ở trạng thái `locked`.<br>2. Đã có quest active → không cho nhận quest mới.<br>3. Event runtime không khớp bước quest hiện tại → server bỏ qua không cộng tiến trình.<br>4. Quest chưa đủ điều kiện hoàn thành → không thể nộp. |
| **Hậu điều kiện** | Tiến trình quest được cập nhật chính xác; player nhận thưởng và lưu dấu completed khi hoàn tất. |

d) Biểu đồ Usecase chức năng Xã hội, phó bản và vận hành

Hình 2.6 Biểu đồ Usecase chức năng Xã hội, phó bản và vận hành

Bảng 2.6 Bảng đặc tả nhóm chức năng Xã hội, phó bản và vận hành

| Trường | Nội dung |
|---|---|
| **Mã / Use Case** | **UC14 - Quản lý bạn bè** |
| **Actor chính** | Player |
| **Mô tả** | Người chơi tìm kiếm nhân vật khác, gửi lời mời kết bạn, chấp nhận lời mời hoặc xóa quan hệ bạn bè. |
| **Tiền điều kiện** | 1. Người chơi đã đăng nhập.<br>2. Friend API khả dụng. |
| **Luồng chính** | 1. Người chơi mở Friend Panel.<br>2. Client lấy danh sách hiện tại bằng `GET /api/friends`.<br>3. Người chơi nhập từ khóa tên nhân vật và gọi `GET /api/friends/search?q=...`.<br>4. Người chơi gửi lời mời qua `POST /api/friends/request`.<br>5. Người nhận mở panel và chấp nhận bằng `PUT /api/friends/{id}/accept` hoặc xóa bằng `DELETE /api/friends/{id}`.<br>6. UI cập nhật trạng thái `pending_sent`, `pending_received`, `accepted`. |
| **Luồng thay thế / ngoại lệ** | 1. Tìm kiếm dưới 2 ký tự → API từ chối.<br>2. Gửi lời mời cho chính mình → từ chối.<br>3. Quan hệ đã tồn tại hoặc target không tồn tại → trả lỗi. |
| **Hậu điều kiện** | Quan hệ bạn bè được tạo, chấp nhận hoặc xóa theo thao tác cuối cùng. |

| Trường | Nội dung |
|---|---|
| **Mã / Use Case** | **UC15 - Tạo và quản lý tổ đội** |
| **Actor chính** | Player |
| **Mô tả** | Người chơi tạo party, mời thành viên, xử lý yêu cầu tham gia, khóa party và quản trị trạng thái tổ đội qua SignalR. |
| **Tiền điều kiện** | 1. Người chơi đã kết nối `PartyHub` và cập nhật presence map/zone/level.<br>2. Người chơi chưa nằm trong party khác nếu muốn tạo hoặc xin vào party mới. |
| **Luồng chính** | 1. Player gọi `CreateParty` để tạo tổ đội mới.<br>2. Leader mời thành viên bằng `InviteMember` hoặc người chơi khác xin vào bằng `RequestJoinParty`.<br>3. Leader chấp nhận hoặc từ chối yêu cầu thông qua `AcceptJoinRequest` / `RejectJoinRequest`.<br>4. Hệ thống đồng bộ `PartyStateUpdated` cho toàn bộ thành viên trong group SignalR.<br>5. Leader có thể bật/tắt `autoAccept`, khóa party hoặc giải tán party; thành viên có thể tự rời party. |
| **Luồng thay thế / ngoại lệ** | 1. Party đã đủ 4 người → không thể thêm thành viên.<br>2. Người chơi đang ở party khác → không được xin vào party mới.<br>3. Party bị khóa → từ chối yêu cầu join thường.<br>4. Leader thoát khỏi party → hệ thống tự chuyển leader hoặc giải tán nếu không còn ai. |
| **Hậu điều kiện** | Trạng thái tổ đội được cập nhật thời gian thực cho mọi thành viên và sẵn sàng cho nội dung co-op. |

| Trường | Nội dung |
|---|---|
| **Mã / Use Case** | **UC16 - Chat đa kênh thời gian thực** |
| **Actor chính** | Player |
| **Mô tả** | Người chơi gửi và nhận chat qua nhiều kênh SignalR gồm world, proximity, group, private; phần hạ tầng còn hỗ trợ clan/class. |
| **Tiền điều kiện** | 1. Người chơi đã kết nối `ChatHub` bằng JWT.<br>2. Với proximity/group, người chơi đã join đúng group map hoặc group party. |
| **Luồng chính** | 1. Người chơi nhập nội dung tin nhắn trên kênh đã chọn.<br>2. Client gọi method SignalR tương ứng như `SendWorldMessage`, `SendProximityMessage`, `SendGroupMessage`, `SendPrivateMessage`.<br>3. Hub kiểm tra rỗng và giới hạn chiều dài 300 ký tự.<br>4. Server broadcast tin nhắn đến đúng group người nhận và echo lại cho caller nếu cần.<br>5. Khi đổi map hoặc đổi party, client gọi `JoinMap/LeaveMap` hoặc `JoinGroup/LeaveGroup` để cập nhật phạm vi chat. |
| **Luồng thay thế / ngoại lệ** | 1. Nội dung rỗng hoặc vượt quá 300 ký tự → tin nhắn bị bỏ qua.<br>2. Người nhận private offline → không có phản hồi realtime về phía người nhận.<br>3. Sai group/map hiện tại → tin nhắn không tới đúng phạm vi. |
| **Hậu điều kiện** | Tin nhắn được phân phối đúng kênh và được hiển thị tức thời trên UI chat. |

| Trường | Nội dung |
|---|---|
| **Mã / Use Case** | **UC17 - Tạo, tham gia và hoàn tất phó bản** |
| **Actor chính** | Player |
| **Actor phụ** | Máy chủ gameplay |
| **Mô tả** | Người chơi truy cập danh sách dungeon, tạo hoặc tham gia session, vượt wave quái và boss, sau đó nhận thưởng phó bản. |
| **Tiền điều kiện** | 1. Dungeon đang active trong database.<br>2. Người chơi hoặc party đạt level yêu cầu.<br>3. Session còn chỗ trống nếu join session đã tồn tại. |
| **Luồng chính** | 1. Client gọi `GET /api/dungeon/list` và `GET /api/dungeon/{id}` để hiển thị dungeon list/detail.<br>2. Nếu chưa có session phù hợp, host gọi `POST /api/dungeon/session/create`; nếu đã có session thì player gọi `POST /api/dungeon/session/{id}/join`.<br>3. Khi vào phó bản, gameplay server gọi `POST /api/dungeon/wave/{dungeonId}/enter` và các API cập nhật session wave trong quá trình chiến đấu.<br>4. Server spawn wave quái, sau wave cuối thì nạp boss config và cho boss xuất hiện.<br>5. Khi dungeon kết thúc, host gọi `POST /api/dungeon/session/{id}/end` hoặc `leave/end wave session` tương ứng.<br>6. Reward service gọi `POST /api/dungeonreward/grant` để cấp item cho player tham gia. |
| **Luồng thay thế / ngoại lệ** | 1. Session đã đầy hoặc đã kết thúc → không thể join.<br>2. Người chơi không đủ level → không thể vào dungeon.<br>3. Thành viên rời giữa chừng → session vẫn tiếp tục nếu còn người khác.<br>4. Inventory đầy khi grant reward → chỉ thêm được các item còn chỗ hợp lệ. |
| **Hậu điều kiện** | Session phó bản được cập nhật hoặc kết thúc; người chơi nhận reward, EXP và quay về gameplay thường. |

| Trường | Nội dung |
|---|---|
| **Mã / Use Case** | **UC18 - Xem và làm mới leaderboard** |
| **Actor chính** | Player / Admin |
| **Mô tả** | Người chơi xem bảng xếp hạng theo nhiều hạng mục; Admin có thể ép hệ thống refresh thủ công khi cần. |
| **Tiền điều kiện** | 1. Người dùng đã xác thực.<br>2. Bảng cache leaderboard đã được seed hợp lệ. |
| **Luồng chính** | 1. Client gọi `GET /api/leaderboard/all` hoặc `GET /api/leaderboard/{id}`.<br>2. Server kiểm tra cache 5 phút; nếu stale thì chạy `RefreshAllAsync` để tính top 50 theo từng hạng mục.<br>3. Server trả dữ liệu xếp hạng level, quest, attendance, dungeon, gold.<br>4. Admin có thể gọi `POST /api/leaderboard/refresh` để ép refresh thủ công. |
| **Luồng thay thế / ngoại lệ** | 1. Category không tồn tại → trả not found.<br>2. Cache rỗng hoặc cũ → hệ thống tự tính lại trước khi trả về. |
| **Hậu điều kiện** | Dữ liệu leaderboard được cập nhật hoặc hiển thị thành công cho người dùng. |

e) Đặc tả riêng nhóm chức năng Vận hành kỹ thuật

Bảng 2.7 Bảng đặc tả riêng nhóm chức năng Vận hành kỹ thuật

| Trường | Nội dung |
|---|---|
| **Mã / Use Case** | **UC19 - Đăng ký, heartbeat và giải phóng gameplay server** |
| **Actor chính** | Máy chủ gameplay / Admin |
| **Mô tả** | Gameplay server / Netcode server thông báo trạng thái sống với API để hệ thống vận hành biết số zone, số player, tình trạng session và năng lực phục vụ đồng bộ runtime đang chạy. |
| **Tiền điều kiện** | 1. Gameplay server có quyền `GameServer` hợp lệ.<br>2. Server biết IP/port vận hành của chính nó. |
| **Luồng chính** | 1. Khi khởi động, server gọi `POST /api/zone/server/register` để đăng ký `ip`, `port`, `mapCount`.<br>2. Trong runtime, server định kỳ gọi `PUT /api/zone/server/heartbeat` gửi `playerCount`, `zoneStats` và trạng thái vận hành các room/zone đang quản lý.<br>3. Trong thời gian phục vụ, server tiếp tục duy trì connection approval, zone registry và các cơ chế đồng bộ runtime cho client đang online.<br>4. Khi shutdown, server gọi `DELETE /api/zone/server/deregister` để gỡ khỏi registry.<br>5. Admin theo dõi registry và dựa trên dữ liệu heartbeat để giám sát trạng thái zone. |
| **Luồng thay thế / ngoại lệ** | 1. Port/IP không hợp lệ → API từ chối đăng ký.<br>2. Auth sai role → request bị chặn.<br>3. Server chết đột ngột trước khi deregister → trạng thái cuối cùng chỉ còn heartbeat cũ trong registry. |
| **Hậu điều kiện** | Registry server phản ánh tương đối chính xác tình trạng zone/gameplay server đang vận hành. |

| Trường | Nội dung |
|---|---|
| **Mã / Use Case** | **UC20 - Đăng ký host map, cập nhật spawn config và phát thưởng dungeon** |
| **Actor chính** | Máy chủ gameplay / Admin |
| **Mô tả** | Nhóm use case vận hành kỹ thuật gồm kiểm tra host map, đăng ký host runtime, cập nhật spawn JSON theo map và phát item reward cho player sau dungeon. |
| **Tiền điều kiện** | 1. Map hoặc dungeon liên quan phải tồn tại trong cấu hình.<br>2. Actor gọi API có đủ quyền tương ứng.<br>3. Player mục tiêu tồn tại nếu cần phát thưởng. |
| **Luồng chính** | 1. Host runtime kiểm tra host hiện có bằng `GET /api/map/host/check`.<br>2. Nếu phù hợp, host gọi `POST /api/map/host/register` rồi heartbeat định kỳ bằng `POST /api/map/host/heartbeat`; khi rời map thì gọi `unregister`.<br>3. Admin hoặc tool cập nhật spawn bằng `PUT /api/map/{mapId}/spawn-config`; API xác thực `enemy_id`, ghi DB và xóa cache runtime.<br>4. Khi dungeon kết thúc, gameplay server gọi `POST /api/dungeonreward/grant` để thêm item reward vào inventory player. |
| **Luồng thay thế / ngoại lệ** | 1. `spawn_json` không hợp lệ hoặc chứa `enemy_id` không tồn tại → từ chối cập nhật.<br>2. Host không khớp player hiện hành → unregister không thành công.<br>3. Reward target player không tồn tại hoặc inventory không còn slot → phần thưởng không được thêm đủ. |
| **Hậu điều kiện** | Host registry, spawn config runtime và inventory reward của player được cập nhật đúng với thao tác vận hành cuối cùng. |

### 2.2.4. Thiết kế cơ sở dữ liệu

Cơ sở dữ liệu MySQL được thiết kế theo mô hình quan hệ với 14 bảng chính, tổ chức xung quanh thực thể trung tâm là **characters** (nhân vật). Mỗi nhân vật thuộc một tài khoản người chơi và liên kết với các bảng con mô tả trang bị, Gene, nhiệm vụ và kỹ năng. Nguyên tắc thiết kế là chuẩn hóa đến mức đủ để đảm bảo toàn vẹn dữ liệu, đồng thời sử dụng JSON column cho các trường có cấu trúc linh hoạt thay đổi thường xuyên theo nội dung game. Sơ đồ ERD tổng quát được mô tả tại Hình 2.12.

Hình 2.12: Biểu đồ ERD tổng quát cơ sở dữ liệu hệ thống Mutants Arena

a) Nhóm thực thể tài khoản và nhân vật

▪ **players**: Lưu thông tin tài khoản — `id` (PK), `username` (UNIQUE), `password_hash`, `email`, `created_at`, `last_login`
▪ **characters**: Thực thể trung tâm — `id` (PK), `player_id` (FK → players), `name`, `class` (ENUM: Hoa/Kim/Moc/Phong/Tho/Thuy), `level`, `exp`, `hp`, `max_hp`, `mp`, `max_mp`, `current_map_id`, `position_x`, `position_y`, `gold`
▪ **character_stats**: Chỉ số chiến đấu tổng hợp — `character_id` (FK), `attack`, `defense`, `critical_rate`, `critical_damage`, `speed`, `element_bonus` (JSON — tăng cường nguyên tố theo Gene)

b) Nhóm thực thể trang bị và vật phẩm

▪ **items**: Danh mục vật phẩm (định nghĩa tĩnh) — `id` (PK), `name`, `type` (ENUM: Weapon/Armor/Accessory/Consumable/Material), `rarity` (ENUM: Common/Uncommon/Rare/Epic/Legendary), `base_stats` (JSON), `description`
▪ **character_equipment**: Trang bị đang mặc — `character_id` (FK), `slot` (ENUM: Weapon/Armor/Accessory), `item_id` (FK → items), `enhancement_level` (0–20)
▪ **inventory**: Túi đồ nhân vật — `id` (PK), `character_id` (FK), `item_id` (FK → items), `quantity`, `slot_index`

c) Nhóm thực thể Gene

▪ **genes**: Dữ liệu Gene của nhân vật — `id` (PK), `character_id` (FK), `gene_type` (ENUM: Kim/Moc/Thuy/Hoa/Tho/Phong), `tier` (1–5), `fragments`, `is_primary` (BOOLEAN), `is_equipped` (BOOLEAN)
▪ **gene_definitions**: Bảng cấu hình tĩnh (nội dung game) — `gene_type`, `tier`, `required_fragments`, `required_gold`, `stat_bonuses` (JSON), `unlocked_skill_id` (FK → skills)

d) Nhóm thực thể nhiệm vụ

▪ **quests**: Định nghĩa nhiệm vụ (nội dung game) — `id` (PK), `name`, `type` (ENUM: Main/Side), `min_level`, `objectives` (JSON — mảng mục tiêu với type, target, count), `reward_exp`, `reward_gold`, `reward_items` (JSON)
▪ **player_quests**: Tiến trình nhiệm vụ theo nhân vật — `player_id` (FK), `quest_id` (FK), `status` (ENUM: Not_Started/In_Progress/Completed), `progress` (JSON — đếm từng mục tiêu), `completed_at`

e) Nhóm thực thể bản đồ và quái vật

▪ **maps**: Định nghĩa bản đồ — `id` (PK), `name`, `zone_type` (ENUM: Overworld/Dungeon/Town), `min_level`, `spawn_config` (JSON — danh sách spawn points, loại quái, số lượng)
▪ **enemies**: Định nghĩa quái vật và boss — `id` (PK), `name`, `type` (ENUM: Normal/Elite/Boss), `element` (ENUM nguyên tố), `hp`, `attack`, `defense`, `exp_reward`, `phases_json` (JSON — chỉ áp dụng cho Boss, chứa ngưỡng HP và kỹ năng từng phase)
▪ **skills**: Định nghĩa kỹ năng — `id` (PK), `name`, `element`, `damage_multiplier`, `cooldown`, `mp_cost`, `effect_type`, `effect_duration`, `description`

f) Nhóm thực thể hệ thống nhóm

▪ **parties**: Nhóm người chơi — `id` (PK), `leader_character_id` (FK → characters), `member_ids` (JSON — mảng characterId), `created_at`, `status` (ENUM: Active/Dissolved)

Bảng 2.2: Tổng hợp cấu trúc cơ sở dữ liệu

| Bảng | Mô tả | Ghi chú |
|---|---|---|
| players | Tài khoản người dùng | Khóa chính cho toàn hệ thống |
| characters | Nhân vật game | Thực thể trung tâm — liên kết hầu hết bảng khác |
| character_stats | Chỉ số chiến đấu tổng hợp | Cập nhật mỗi khi Gene/trang bị thay đổi |
| items | Danh mục vật phẩm | Bảng lookup tĩnh — không thay đổi thường xuyên |
| character_equipment | Trang bị đang mặc | Tối đa 3 hàng mỗi nhân vật (3 slot) |
| inventory | Túi đồ nhân vật | Tối đa 60 slot mỗi nhân vật |
| genes | Gene nguyên tố nhân vật | Tối đa 3 Gene mỗi nhân vật (1 chính + 2 phụ) |
| gene_definitions | Cấu hình nâng cấp Gene | Bảng lookup tĩnh — 6 loại × 5 Tier = 30 hàng |
| quests | Định nghĩa nhiệm vụ | Bảng lookup tĩnh — nội dung game |
| player_quests | Tiến trình nhiệm vụ | Theo dõi trạng thái từng nhiệm vụ mỗi nhân vật |
| maps | Định nghĩa bản đồ | Cấu hình spawn qua JSON column |
| enemies | Quái vật và boss | phases_json chỉ dùng cho Boss type |
| skills | Định nghĩa kỹ năng | Liên kết với genes và characters |
| parties | Nhóm người chơi | Xóa khi nhóm giải tán |

---

## 2.3. Tổng kết chương 2

Chương 2 đã được chuẩn hóa lại theo đúng trạng thái triển khai thực tế của dự án Mutants Arena thay vì chỉ dừng ở mức mô tả ý tưởng. Trên cơ sở đọc trực tiếp mã nguồn API, SignalR Hub và client Unity, hệ thống được xác định có bốn actor chính gồm Guest, Player, Admin/Operator và Máy chủ gameplay / Netcode server. Từ đó, các yêu cầu chức năng được tái cấu trúc thành các nhóm triển khai thật: xác thực, nhân vật, bản đồ, combat, Gene Evolution, inventory/equipment, NPC/quest/shop, social, dungeon, đồng bộ thời gian thực và vận hành server.

Phần Use Case là trọng tâm được viết lại toàn diện với 20 use case đầy đủ, bao phủ toàn bộ tính năng đã có trong mã nguồn: từ đăng ký, đăng nhập, tạo nhân vật, combat, Gene chính/Gene phụ/Hybrid Fusion, blacksmith, NPC service, quest event-driven, friend, party, chat, dungeon, leaderboard đến các luồng vận hành kỹ thuật như zone heartbeat, host runtime, spawn config và dungeon reward grant. Nhờ đó, chương 2 không chỉ mô tả đầy đủ nghiệp vụ của hệ thống mà còn tạo được sự liên kết rõ ràng giữa kiến trúc cài đặt, sơ đồ phân tích và đặc tả chức năng trong báo cáo.

Toàn bộ kết quả phân tích và đặc tả trong chương này là cơ sở trực tiếp cho Chương 3, nơi từng hệ thống sẽ được triển khai, giải thích kiến trúc code và kiểm chứng bằng luồng runtime tương ứng.
