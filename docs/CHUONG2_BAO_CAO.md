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
▪ Dịch chuyển qua portal, chuyển map/scene, kiểm tra khoảng cách đến cổng để chống gian lận vị trí. ----- [BẮT ĐẦU PHẦN THÊM MỚI] ----- Cổng dịch chuyển yêu cầu người chơi phải đáp ứng các điều kiện khắt khe trước khi cho phép qua map: kiểm tra cấp độ tối thiểu của nhân vật (`min_level`), kiểm tra việc hoàn thành nhiệm vụ cốt truyện bắt buộc (`required_quest_id`), và kiểm tra việc sở hữu vật phẩm chìa khóa tương ứng (`required_item_id`) trong túi đồ. ----- [KẾT THÚC PHẦN THÊM MỚI] -----

Hệ thống Gene Evolution:
▪ Nâng Gene chính từ Tier 1 đến Tier 5 bằng `gene_exp`, vàng và item cấu hình theo DB
▪ Chọn Gene phụ theo cặp cố định đã triển khai trong code: Hỏa↔Thổ, Thủy↔Mộc, Kim↔Phong. ----- [BẮT ĐẦU PHẦN THÊM MỚI] ----- Hệ nguyên tố Phong (Wind) là hệ đặc biệt: không tham gia vòng tương khắc Ngũ Hành truyền thống (Kim-Mộc-Thủy-Hỏa-Thổ) nhằm tránh gây mất cân bằng thuộc tính, nhưng tham gia vào cặp Gene phụ cố định với Kim và tham gia đầy đủ hệ thống dung hợp Hybrid. ----- [KẾT THÚC PHẦN THÊM MỚI] -----
▪ Nâng Gene phụ từ Tier 1 đến Tier 5; bonus chỉ số nhỏ hơn Gene chính và là điều kiện để mở Hybrid Fusion
▪ Fusion hai Gene Tier 5 hợp lệ thành Hybrid Gene; hệ thống lưu `hybridId`, `prefabPath`, bonus target, immune element và thêm bộ kỹ năng hybrid. Nhận 50% chỉ số bonus phụ, chuyển đổi sang visual prefab Hybrid tương ứng (Hybrid_Earth_Fire, Hybrid_Water_Wood, Hybrid_Metal_Wind).
▪ ----- [BẮT ĐẦU PHẦN THÊM MỚI] ----- **Gene Tối Thượng (Ultimate Gene)**: Sau khi hoàn thành Hybrid Fusion, nhân vật tích lũy EXP Tối Thượng qua diệt quái/boss hoặc dùng item đặc biệt để đạt mốc 1,000,000 EXP, tự động kích hoạt trạng thái Tối Thượng (`is_ultimate = true`). Khi đó, toàn bộ chỉ số HP, MP, ATK, DEF được nhân x1.5, hiển thị hào quang Aura tương ứng theo cặp Fusion (aura1, aura2, aura3) phía sau lưng nhân vật và hiển thị biểu tượng Tối Thượng ✦ trên HUD. ----- [KẾT THÚC PHẦN THÊM MỚI] -----

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
▪ **Tương thích**: Hỗ trợ đa nền tảng, game chạy ổn định trên Windows (10/11 64-bit), Android (từ Android 9.0 trở lên) và iOS (từ iOS 13 trở lên); tối ưu hiển thị giao diện và thao tác điều khiển phù hợp cho cả máy tính và thiết bị di động.


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

![Sơ đồ kiến trúc tổng thể hệ thống Mutants Arena](extracted_images/image8.png)

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

### 2.2.2. Biểu đồ ca sử dụng mức tổng quát

Để phản ánh đúng dự án đang triển khai, phần use case của Mutants Arena được tổ chức thành hai mức: mức tổng quát theo module chức năng và mức chi tiết theo ca sử dụng hoàn chỉnh. Mức tổng quát dùng để trình bày phạm vi hệ thống; mức chi tiết dùng để đặc tả luồng nghiệp vụ đủ gần với mã nguồn hiện tại nhưng vẫn giữ được ý nghĩa phân tích hệ thống.

**Bảng 2.1b: Danh mục use case toàn hệ thống Mutants Arena**

| Mã | Use case | Actor chính | Nhóm chức năng |
|---|---|---|---|
| UC01 | Đăng ký tài khoản | Khách | Xác thực |
| UC02 | Đăng nhập và vào game | Khách / Người chơi | Xác thực |
| UC03 | Di chuyển và chuyển map | Người chơi | Bản đồ |
| UC04 | Chiến đấu và sử dụng kỹ năng | Người chơi | Combat |
| UC05 | Quản lý túi đồ và trang bị | Người chơi | Inventory |
| UC06 | Nâng cấp trang bị | Người chơi | Trang bị |
| UC07 | Phát triển Gene, Hybrid và Tối Thượng | Người chơi | Gene Evolution |
| UC08 | Phân bổ tiềm năng và kỹ năng | Người chơi | Progression |
| UC09 | Tương tác NPC và mua vật phẩm | Người chơi | NPC / Shop |
| UC10 | Quản lý nhiệm vụ | Người chơi | Quest |
| UC11 | Quản lý bạn bè | Người chơi | Social |
| UC12 | Quản lý tổ đội và chat | Người chơi | Party / Chat |
| UC13 | Tham gia và hoàn tất phó bản | Người chơi | Dungeon |
| UC14 | Xem leaderboard | Người chơi | Leaderboard |
| UC15 | Đăng ký và duy trì gameplay server | Gameplay Server | Vận hành |
| UC16 | Host map và phát thưởng dungeon | Gameplay Server | Vận hành |

![Biểu đồ Use case tổng quát hệ thống Mutants Arena](extracted_images/image9.png)

Hình 2.2. Biểu đồ ca sử dụng mức tổng quát hệ thống Mutants Arena

Mô tả: Vùng hệ thống mang tên "Hệ thống Mutants Arena" bao gồm các cụm use case mức cao: xác thực tài khoản, quản lý nhân vật, gameplay chiến đấu, Gene Evolution, inventory - trang bị, NPC - nhiệm vụ - cửa hàng, bạn bè - tổ đội - chat, phó bản đồng đội, đồng bộ thời gian thực, chuyển zone/visibility, spawn runtime, leaderboard và vận hành gameplay server. Bên ngoài vùng hệ thống có ba actor chính: Khách, Người chơi, và Gameplay Server. Các actor này tương tác với những nhóm chức năng khác nhau tùy theo vai trò nghiệp vụ và vai trò kỹ thuật của mình.

Ngoài sơ đồ tổng quát, bộ tài liệu còn có một sơ đồ tổng hợp chi tiết toàn bộ UC01 đến UC16 để đối chiếu giữa danh mục use case và các nhóm chức năng chuyên biệt.

### 2.2.2b. Đánh giá tính chuẩn hóa UML và Quy tắc thiết kế sơ đồ Use Case

Để đảm bảo các sơ đồ Use Case (cả tổng quát và chi tiết) tuân thủ đúng tiêu chuẩn quốc tế UML (Unified Modeling Language) và phương pháp luận phân tích thiết kế hướng đối tượng (OOAD), hệ thống sơ đồ trong đề tài đã được thiết kế và kiểm chứng theo các quy tắc chuẩn hóa sau:

1. **Xác định ranh giới hệ thống (System Boundary)**:
   * **Định nghĩa**: Hệ thống trong đề tài được giới hạn là **dịch vụ trung tâm Backend API và Cơ sở dữ liệu** (không bao gồm toàn bộ runtime của client standalone).
   * **Giải thích**: Việc xác định ranh giới này giúp giải thích tại sao `Máy chủ gameplay` (NGO Dedicated Server) và `Client` giao tiếp độc lập. Do chạy trên các tiến trình và VPS riêng biệt, `Gameplay Server` đóng vai trò là một tác nhân kỹ thuật (Technical Actor) tương tác với hệ thống qua các API được bảo mật bằng Zone API Key. Ranh giới hệ thống được biểu diễn rõ ràng bằng hình chữ nhật bao quanh toàn bộ các ca sử dụng (Use Case), với tên hệ thống được ghi ở góc trên bên trái.

2. **Chuẩn hóa các tác nhân (Actors)**:
   * **Quy tắc**: Các tác nhân luôn nằm ngoài ranh giới hệ thống và đại diện cho các thực thể bên ngoài tương tác trực tiếp với hệ thống.
   * **Chuẩn hóa**:
     * *Khách (Guest)*: Người dùng chưa xác thực, chỉ tương tác với các Use Case khởi tạo như Đăng ký (UC01) và Đăng nhập (UC02).
     * *Người chơi (Player)*: Tác nhân thừa kế từ Khách sau khi đã có Token phiên (JWT), có quyền tương tác với toàn bộ Use Case gameplay và phát triển nhân vật (UC03 - UC14).
     * *Máy chủ gameplay (Gameplay Server)*: Tác nhân kỹ thuật chịu trách nhiệm vận hành thế giới realtime, tương tác với các Use Case hệ thống như Đăng ký/Duy trì server (UC15), Host map và phát thưởng (UC16).
     * *Quản trị viên (Admin)*: Đã được lược bỏ khỏi các sơ đồ chi tiết (như Leaderboard và Vận hành server) do hệ thống thực tế phân quyền bảo mật qua middleware và cấu hình tự động (Self-registration), tránh việc vẽ các tác nhân "ảo" không có chức năng đi kèm trong mã nguồn.

3. **Tính đúng đắn của ca sử dụng (Use Cases)**:
   * **Quy tắc**: Mỗi Use Case phải biểu diễn một mục tiêu hoàn chỉnh, có giá trị nghiệp vụ thực tế đối với tác nhân, được đặt tên bắt đầu bằng một động từ (ví dụ: *Đăng ký tài khoản*, *Nâng cấp trang bị*, *Dung hợp Gene*).
   * **Khắc phục lỗi phổ biến**: Tránh lỗi thiết kế kinh điển của sinh viên là vẽ các bước xử lý nội bộ hoặc luồng thuật toán kỹ thuật thành các ca sử dụng độc lập (ví dụ: *Kiểm tra mật khẩu*, *Lưu cơ sở dữ liệu*, *Mã hóa BCrypt*, *Gửi kết quả*). Trong hệ thống sơ đồ của đề tài, các bước kỹ thuật này đã được loại bỏ hoàn toàn khỏi biểu đồ vẽ để tránh làm rối cấu trúc UML, thay vào đó chúng được mô tả chi tiết trong phần **Luồng chính (Basic Flow)** và **Quy tắc nghiệp vụ** của bảng đặc tả.

4. **Sử dụng đúng các quan hệ cấu trúc UML**:
   * **Quan hệ Bao hàm (<<include>>)**: Được sử dụng khi một ca sử dụng bắt buộc phải gọi đến một ca sử dụng khác để hoàn thành mục tiêu. Ví dụ: Ca sử dụng *Nâng cấp trang bị* (UC06) bắt buộc phải có quan hệ `<<include>>` tới *Quản lý túi đồ và trang bị* (UC05) để truy vấn và tiêu hao nguyên liệu. Mũi tên nét đứt hướng từ ca sử dụng gốc sang ca sử dụng được bao hàm.
   * **Quan hệ Mở rộng (<<extend>>)**: Chỉ sử dụng cho các luồng xử lý tùy chọn hoặc có điều kiện (Conditional/Optional flow). Ví dụ: Nhánh xử lý phát thưởng và lưu log của *Host map và phát thưởng dungeon* (UC16) chỉ mở rộng từ *Tham gia và hoàn tất phó bản* (UC13) tại điểm mở rộng (Extension Point) khi tổ đội chiến thắng toàn bộ các wave quái. Mũi tên nét đứt hướng từ ca sử dụng mở rộng ngược về ca sử dụng gốc.
   * **Quan hệ Hiệp hội (Association)**: Giữa các tác nhân và ca sử dụng được kết nối bằng một đường thẳng không có mũi tên (để tránh nhầm lẫn với luồng truyền dữ liệu), thể hiện tác nhân tham gia vào ca sử dụng đó.

Nhờ việc áp dụng nghiêm ngặt các quy tắc trên, bộ biểu đồ Use Case của đề tài không chỉ đạt tính thẩm mỹ cao mà còn phản ánh chính xác kiến trúc phần mềm, hỗ trợ đắc lực cho đội ngũ phát triển chuyển đổi trực tiếp từ mô hình phân tích sang mã nguồn C# và cấu hình Unity Netcode.

Các sơ đồ Usecase chi tiết của chương này được biên tập theo cùng một mẫu trình bày của luận văn; trong nội dung chương chỉ giữ phần hình minh họa và phần đặc tả nghiệp vụ tương ứng.

### 2.2.3. Đặc tả ca sử dụng mức chi tiết

#### 2.2.3.1. Nhóm 1 - Tài khoản và gameplay (UC01 — UC06)

Nhóm này bao gồm các chức năng nền tảng cho phép người chơi đăng nhập, di chuyển và thực hiện các tương tác chiến đấu cơ bản.

##### UC01 — Đăng ký tài khoản

| Thuộc tính | Nội dung |
|---|---|
| **Mã Use Case** | **UC01** |
| **Tên Use Case** | Đăng ký tài khoản |
| **Tác nhân chính (Actor)** | Khách |
| **Mô tả** | Khách tạo tài khoản mới để bắt đầu sử dụng hệ thống Mutants Arena. Hệ thống nhận thông tin đăng ký, kiểm tra tính hợp lệ và lưu tài khoản khi dữ liệu đáp ứng đủ điều kiện. |
| **Tiền điều kiện** | Khách chưa có tài khoản trong hệ thống. Có kết nối mạng ổn định. |
| **Luồng chính** | 1. Khách mở màn hình đăng ký từ giao diện chính.<br>2. Hệ thống hiển thị biểu mẫu gồm tên đăng nhập, mật khẩu và email.<br>3. Khách nhập thông tin và xác nhận gửi đăng ký.<br>4. Hệ thống kiểm tra tính hợp lệ của dữ liệu và phát hiện trùng lặp nếu có.<br>5. Hệ thống mã hóa mật khẩu và lưu tài khoản vào cơ sở dữ liệu.<br>6. Hệ thống trả thông báo thành công và chuyển sang màn hình đăng nhập. |
| **Luồng phụ / Ngoại lệ** | Tên đăng nhập hoặc email đã tồn tại, hoặc dữ liệu thiếu trường bắt buộc → Hệ thống báo lỗi tương ứng trên giao diện UI. |
| **Kết quả** | Tài khoản mới được đăng ký thành công trên cơ sở dữ liệu và người chơi có thể bắt đầu sử dụng. |


![Biểu đồ ca sử dụng cho mô-đun Đăng ký tài khoản](extracted_images/image10.png)

*Hình 2.3. Biểu đồ ca sử dụng cho mô-đun Đăng ký tài khoản*

##### UC02 — Đăng nhập và vào game

| Thuộc tính | Nội dung |
|---|---|
| **Mã Use Case** | **UC02** |
| **Tên Use Case** | Đăng nhập và vào game |
| **Tác nhân chính (Actor)** | Khách, Người chơi |
| **Mô tả** | Người dùng xác thực tài khoản, chọn nhân vật và kết nối vào thế giới game. Hệ thống tạo phiên đăng nhập hợp lệ và đưa nhân vật vào map tương ứng. |
| **Tiền điều kiện** | Tài khoản đã tồn tại trong hệ thống. Máy chủ game đang sẵn sàng kết nối. |
| **Luồng chính** | 1. Người dùng nhập thông tin đăng nhập trên màn hình xác thực.<br>2. Hệ thống xác thực tên đăng nhập và mật khẩu.<br>3. Hệ thống tạo token phiên và gửi về cho client.<br>4. Người dùng chọn nhân vật hiện có hoặc tạo nhanh nhân vật mới nếu chưa có.<br>5. Client dùng thông tin phiên để kết nối đến gameplay server.<br>6. Máy chủ nạp dữ liệu nhân vật và đưa người chơi vào map khởi đầu. |
| **Luồng phụ / Ngoại lệ** | Không có. |
| **Kết quả** | Phiên đăng nhập hợp lệ được tạo và nhân vật xuất hiện trong game. Nếu thất bại, hệ thống hiển thị thông báo lỗi tương ứng. |


![Biểu đồ ca sử dụng cho mô-đun Đăng nhập và vào game](extracted_images/image11.png)

*Hình 2.4. Biểu đồ ca sử dụng cho mô-đun Đăng nhập và vào game*

##### UC03 — Di chuyển và chuyển map

| Thuộc tính | Nội dung |
|---|---|
| **Mã Use Case** | **UC03** |
| **Tên Use Case** | Di chuyển và chuyển map |
| **Tác nhân chính (Actor)** | Người chơi |
| **Mô tả** | Người chơi di chuyển qua portal để sang map hoặc khu vực mới. Hệ thống kiểm tra điều kiện vào khu vực đích, tải map mới và đặt nhân vật vào vị trí spawn tương ứng. |
| **Tiền điều kiện** | Nhân vật đang ở gần portal. Người chơi đáp ứng điều kiện vào khu vực đích. |
| **Luồng chính** | 1. Người chơi tiến vào vùng tương tác của portal.<br>2. Hệ thống hiển thị gợi ý xác nhận di chuyển sang khu vực đích.<br>3. Người chơi xác nhận thao tác chuyển map.<br>4. Máy chủ kiểm tra điều kiện như level, nhiệm vụ hoặc giới hạn số người.<br>5. Hệ thống tải map mới và cập nhật zone của nhân vật.<br>6. Nhân vật xuất hiện tại vị trí spawn tương ứng của map đích. |
| **Luồng phụ / Ngoại lệ** | Nếu chưa đủ điều kiện vào map, hệ thống từ chối và hiển thị lý do. Nếu quá trình tải map lỗi, hệ thống giữ nguyên vị trí hiện tại và thông báo thử lại. |
| **Kết quả** | Người chơi được đưa sang map mới và hiển thị đúng trong zone mới. Nếu thất bại, nhân vật ở nguyên vị trí ban đầu và có thông báo nguyên nhân. |


![Biểu đồ ca sử dụng cho mô-đun Di chuyển và chuyển map](extracted_images/image12.png)

*Hình 2.5. Biểu đồ ca sử dụng cho mô-đun Di chuyển và chuyển map*

##### UC04 — Chiến đấu và sử dụng kỹ năng

| Thuộc tính | Nội dung |
|---|---|
| **Mã Use Case** | **UC04** |
| **Tên Use Case** | Chiến đấu và sử dụng kỹ năng |
| **Tác nhân chính (Actor)** | Người chơi |
| **Mô tả** | Người chơi chiến đấu với quái hoặc boss bằng đòn đánh thường và kỹ năng. Máy chủ xác nhận, tính toán sát thương và đồng bộ kết quả cho toàn bộ client liên quan. |
| **Tiền điều kiện** | Nhân vật còn sống, đang ở khu vực có mục tiêu chiến đấu và không bị khóa thao tác. |
| **Luồng chính** | 1. Người chơi chọn mục tiêu hoặc thực hiện tấn công trong phạm vi cho phép.<br>2. Client gửi yêu cầu đánh thường hoặc dùng kỹ năng lên máy chủ.<br>3. Máy chủ kiểm tra cooldown, mana và điều kiện mục tiêu.<br>4. Máy chủ tính sát thương và áp dụng kết quả lên đối tượng liên quan.<br>5. Hệ thống đồng bộ vị trí, animation và trạng thái cho các client quan sát.<br>6. Nếu mục tiêu bị hạ gục, phần thưởng kinh nghiệm và vật phẩm được phát cho người chơi. |
| **Luồng phụ / Ngoại lệ** | Không có. |
| **Kết quả** | Sát thương, trạng thái buff/debuff và phần thưởng chiến đấu được cập nhật chính xác trên tất cả client. Nếu thất bại, trạng thái chiến đấu giữ nguyên và có thông báo nguyên nhân. |


![Biểu đồ ca sử dụng cho mô-đun Chiến đấu và sử dụng kỹ năng](extracted_images/image13.png)

*Hình 2.6. Biểu đồ ca sử dụng cho mô-đun Chiến đấu và sử dụng kỹ năng*

##### UC05 — Quản lý túi đồ và trang bị

| Thuộc tính | Nội dung |
|---|---|
| **Mã Use Case** | **UC05** |
| **Tên Use Case** | Quản lý túi đồ và trang bị |
| **Tác nhân chính (Actor)** | Người chơi |
| **Mô tả** | Người chơi xem, sử dụng và thay đổi trang bị trong túi đồ cá nhân. Hệ thống kiểm tra điều kiện slot và cập nhật chỉ số nhân vật sau mỗi thao tác. |
| **Tiền điều kiện** | Nhân vật đã đăng nhập thành công. Có vật phẩm trong kho đồ hoặc trang bị đang được sử dụng. |
| **Luồng chính** | 1. Người chơi mở giao diện túi đồ từ HUD hoặc phím tắt.<br>2. Hệ thống hiển thị danh sách vật phẩm và thông tin cơ bản từng ô.<br>3. Người chơi chọn một vật phẩm để xem chi tiết hoặc thực hiện thao tác.<br>4. Nếu người chơi chọn trang bị, hệ thống kiểm tra slot và điều kiện sử dụng.<br>5. Nếu người chơi chọn dùng vật phẩm tiêu hao, hiệu ứng được áp dụng ngay.<br>6. Hệ thống cập nhật túi đồ, trang bị và chỉ số nhân vật sau khi thao tác hoàn tất. |
| **Luồng phụ / Ngoại lệ** | Không có. |
| **Kết quả** | Túi đồ và chỉ số nhân vật phản ánh đúng thay đổi mới nhất. Nếu thao tác không hợp lệ, trạng thái vật phẩm và chỉ số giữ nguyên. |


![Biểu đồ ca sử dụng cho mô-đun Quản lý túi đồ và trang bị](extracted_images/image14.png)

*Hình 2.7. Biểu đồ ca sử dụng cho mô-đun Quản lý túi đồ và trang bị*

##### UC06 — Nâng cấp trang bị

| Thuộc tính | Nội dung |
|---|---|
| **Mã Use Case** | **UC06** |
| **Tên Use Case** | Nâng cấp trang bị |
| **Tác nhân chính (Actor)** | Người chơi |
| **Mô tả** | Người chơi nâng cấp trang bị tại NPC hoặc giao diện cường hóa chuyên dụng. Hệ thống kiểm tra nguyên liệu, thực hiện cường hóa và cập nhật chỉ số vật phẩm theo kết quả. |
| **Tiền điều kiện** | Người chơi có trang bị phù hợp trong túi đồ. Có đủ nguyên liệu và vàng cần thiết theo yêu cầu nâng cấp. |
| **Luồng chính** | 1. Người chơi mở giao diện nâng cấp trang bị.<br>2. Hệ thống hiển thị các vật phẩm có thể cường hóa trong túi đồ.<br>3. Người chơi chọn trang bị mục tiêu và xem yêu cầu nguyên liệu.<br>4. Hệ thống kiểm tra số lượng nguyên liệu, vàng và điều kiện nâng cấp.<br>5. Người chơi xác nhận thao tác nâng cấp.<br>6. Hệ thống xử lý kết quả, trừ tài nguyên và cập nhật lại chỉ số của vật phẩm sau khi hoàn tất. |
| **Luồng phụ / Ngoại lệ** | Nếu thiếu nguyên liệu hoặc vàng, thao tác bị từ chối và hiển thị lượng còn thiếu. Nếu nâng cấp thất bại theo tỷ lệ, hệ thống áp dụng đúng quy tắc rủi ro đã cấu hình và thông báo kết quả. |
| **Kết quả** | Trang bị được cập nhật cấp cường hóa và tài nguyên bị trừ đúng. Nếu thất bại, trạng thái trang bị được xử lý theo quy tắc rủi ro và hiển thị thông báo rõ ràng. |


![Biểu đồ ca sử dụng cho mô-đun Nâng cấp trang bị](extracted_images/image15.png)

*Hình 2.8. Biểu đồ ca sử dụng cho mô-đun Nâng cấp trang bị*

---

#### 2.2.3.2. Nhóm 2 - Phát triển nhân vật (UC07 — UC10)

Nhóm này tập trung vào sự phát triển lâu dài của nhân vật qua hệ thống Gene tiến hóa, kỹ năng và nhiệm vụ.

##### UC07 — Phát triển Gene, Hybrid và Tối Thượng

| Thuộc tính | Nội dung |
|---|---|
| **Mã Use Case** | **UC07** |
| **Tên Use Case** | Phát triển Gene, Hybrid và Tối Thượng |
| **Tác nhân chính (Actor)** | Người chơi |
| **Mô tả** | Người chơi phát triển hệ Gene của nhân vật (nâng Gene chính, chọn và nâng Gene phụ), tiến hành dung hợp Hybrid Gene, và tích lũy EXP để kích hoạt Gene Tối Thượng nhằm nhân x1.5 toàn bộ thuộc tính cơ bản và hiển thị hào quang Aura tương ứng sau lưng nhân vật. |
| **Tiền điều kiện** | Người chơi có đủ điểm Gene, đá tiến hóa, vàng, hoặc điểm EXP Tối Thượng tương ứng. |
| **Luồng chính** | 1. Người chơi mở giao diện Gene Evolution.<br>2. Hệ thống hiển thị trạng thái Gene chính, Gene phụ, Hybrid và tiến trình Tối Thượng hiện thời.<br>3. Người chơi chọn nâng cấp Gene chính/Gene phụ, hoặc chọn dung hợp Hybrid khi cả hai đạt Tier 5.<br>4. Hệ thống kiểm tra điều kiện, trừ tài nguyên và cập nhật trạng thái tương ứng.<br>5. Sau khi nhân vật đã dung hợp Hybrid, việc tiêu diệt quái/boss hoặc sử dụng vật phẩm GeneExpAdd sẽ tích lũy EXP Tối Thượng (`ultimate_gene_exp`).<br>6. Khi `ultimate_gene_exp` đạt mốc 1,000,000, hệ thống tự động kích hoạt trạng thái Gene Tối Thượng (`is_ultimate = true`), nhân x1.5 toàn bộ chỉ số HP, MP, ATK, DEF và hiển thị Aura tương ứng phía sau nhân vật. |
| **Luồng phụ / Ngoại lệ** | Nếu thiếu tài nguyên nâng cấp, hệ thống từ chối và báo lỗi. Nếu tích lũy chưa đủ 1,000,000 EXP Tối Thượng, trạng thái Tối Thượng hiển thị phần trăm tiến trình hiện tại trên giao diện. |
| **Kết quả** | Cấp Gene của nhân vật được cập nhật. Nếu đạt Tối Thượng, chỉ số tăng x1.5, hiển thị Aura sau lưng nhân vật và HUD hiển thị biểu tượng ✦. |


![Biểu đồ ca sử dụng cho mô-đun Phát triển Gene và Hybrid](extracted_images/image16.png)

*Hình 2.9. Biểu đồ ca sử dụng cho mô-đun Phát triển Gene và Hybrid*

##### UC08 — Phân bổ tiềm năng và kỹ năng

| Thuộc tính | Nội dung |
|---|---|
| **Mã Use Case** | **UC08** |
| **Tên Use Case** | Phân bổ tiềm năng và kỹ năng |
| **Tác nhân chính (Actor)** | Người chơi |
| **Mô tả** | Người chơi phân bổ điểm chỉ số và sắp xếp kỹ năng phù hợp với lối chơi. Hệ thống kiểm tra hạn mức, lưu cấu hình và cập nhật HUD chiến đấu sau khi xác nhận. |
| **Tiền điều kiện** | Nhân vật còn điểm tiềm năng chưa phân bổ hoặc có kỹ năng khả dụng để sắp xếp. |
| **Luồng chính** | 1. Người chơi mở bảng chỉ số và kỹ năng của nhân vật.<br>2. Hệ thống hiển thị các chỉ số hiện tại, điểm còn lại và danh sách kỹ năng khả dụng.<br>3. Người chơi cộng điểm vào các chỉ số mong muốn.<br>4. Người chơi kéo thả kỹ năng vào các vị trí trên thanh kỹ năng nhanh.<br>5. Người chơi xác nhận lưu cấu hình vừa chỉnh sửa.<br>6. Hệ thống cập nhật lại chỉ số, kỹ năng đang trang bị và HUD chiến đấu. |
| **Luồng phụ / Ngoại lệ** | Nếu cộng quá số điểm hiện có, hệ thống không cho phép xác nhận và hiển thị lỗi. Nếu kỹ năng chưa mở khóa hoặc không phù hợp ô gắn, người chơi không thể thả vào thanh nhanh. |
| **Kết quả** | Bộ chỉ số và thanh kỹ năng của nhân vật được lưu theo cấu hình mới. Nếu không hợp lệ, hệ thống giữ cấu hình cũ và hiển thị thông báo lỗi. |


![Biểu đồ ca sử dụng cho mô-đun Phân bổ tiềm năng và kỹ năng](extracted_images/image17.png)

*Hình 2.10. Biểu đồ ca sử dụng cho mô-đun Phân bổ tiềm năng và kỹ năng*

##### UC09 — Tương tác NPC và mua vật phẩm

| Thuộc tính | Nội dung |
|---|---|
| **Mã Use Case** | **UC09** |
| **Tên Use Case** | Tương tác NPC và mua vật phẩm |
| **Tác nhân chính (Actor)** | Người chơi |
| **Mô tả** | Người chơi tương tác với NPC để mở dịch vụ, mua vật phẩm hoặc dùng các tiện ích hỗ trợ. Hệ thống hiển thị menu động theo loại NPC, xử lý giao dịch và cập nhật túi đồ sau khi xác nhận. |
| **Tiền điều kiện** | Người chơi đang ở trong phạm vi tương tác hợp lệ của NPC. |
| **Luồng chính** | 1. Người chơi tiếp cận NPC và kích hoạt tương tác.<br>2. Hệ thống hiển thị menu động theo loại NPC hiện tại.<br>3. Người chơi chọn chức năng mua vật phẩm hoặc dịch vụ mong muốn.<br>4. Hệ thống hiển thị danh sách hàng hóa, giá bán và số lượng cần mua.<br>5. Người chơi xác nhận giao dịch.<br>6. Hệ thống trừ vàng, thêm vật phẩm hoặc áp dụng dịch vụ tương ứng cho nhân vật. |
| **Luồng phụ / Ngoại lệ** | Nếu không đủ vàng hoặc vật phẩm đã hết, giao dịch bị từ chối và hiển thị nguyên nhân. Nếu NPC thuộc loại hỗ trợ đặc biệt, hệ thống mở dịch vụ tương ứng thay cho cửa hàng vật phẩm. |
| **Kết quả** | Giao dịch với NPC được ghi nhận và túi đồ của người chơi được cập nhật. Nếu thất bại, tài nguyên giữ nguyên và hiển thị thông báo nguyên nhân. |


![Biểu đồ ca sử dụng cho mô-đun Tương tác NPC và mua vật phẩm](extracted_images/image18.png)

*Hình 2.11. Biểu đồ ca sử dụng cho mô-đun Tương tác NPC và mua vật phẩm*

##### UC10 — Quản lý nhiệm vụ

| Thuộc tính | Nội dung |
|---|---|
| **Mã Use Case** | **UC10** |
| **Tên Use Case** | Quản lý nhiệm vụ |
| **Tác nhân chính (Actor)** | Người chơi |
| **Mô tả** | Người chơi nhận, theo dõi và hoàn tất nhiệm vụ để nhận thưởng và mở khóa nội dung mới. Hệ thống tự động theo dõi tiến độ và cấp thưởng khi đầy đủ điều kiện hoàn thành. |
| **Tiền điều kiện** | Nhân vật đáp ứng điều kiện nhận nhiệm vụ tương ứng, chẳng hạn level hoặc chuỗi nhiệm vụ trước đó. |
| **Luồng chính** | 1. Người chơi tương tác với NPC hoặc giao diện nhiệm vụ để nhận quest mới.<br>2. Hệ thống hiển thị mô tả, mục tiêu và phần thưởng của nhiệm vụ.<br>3. Người chơi xác nhận nhận nhiệm vụ và quest được thêm vào nhật ký.<br>4. Trong quá trình chơi, hệ thống tự động cập nhật tiến độ hoàn thành các mục tiêu.<br>5. Khi đã đủ điều kiện, người chơi quay lại NPC hoặc giao diện để nộp nhiệm vụ.<br>6. Hệ thống cấp thưởng và cập nhật trạng thái chuỗi nhiệm vụ tiếp theo nếu có. |
| **Luồng phụ / Ngoại lệ** | Nếu người chơi chưa đủ level hoặc chưa hoàn thành quest trước, hệ thống không cho nhận nhiệm vụ mới và hiển thị lý do. Nếu người chơi hủy nhiệm vụ, tiến độ bị xóa và có thể nhận lại sau nếu hệ thống cho phép. |
| **Kết quả** | Tiến độ nhiệm vụ được cập nhật và phần thưởng được cấp đúng sau khi hoàn thành. Nếu không đủ điều kiện, hệ thống giữ trạng thái quest hiện tại và thông báo rõ nguyên nhân. |


![Biểu đồ ca sử dụng cho mô-đun Quản lý nhiệm vụ](extracted_images/image19.png)

*Hình 2.12. Biểu đồ ca sử dụng cho mô-đun Quản lý nhiệm vụ*

---

#### 2.2.3.3. Nhóm 3 - Tương tác và hoạt động (UC11 — UC14)

Nhóm này bao gồm các tính năng xã hội thời gian thực giúp kết nối người chơi và tổ chức hoạt động co-op.

##### UC11 — Quản lý bạn bè

| Thuộc tính | Nội dung |
|---|---|
| **Mã Use Case** | **UC11** |
| **Tên Use Case** | Quản lý bạn bè |
| **Tác nhân chính (Actor)** | Người chơi |
| **Mô tả** | Người chơi quản lý danh sách bạn bè và trạng thái kết nối xã hội trong game. Hệ thống hỗ trợ gửi lời mời, phản hồi lời mời, xóa bạn bè và đồng bộ trạng thái online giữa các người chơi. |
| **Tiền điều kiện** | Người chơi đang online và có thể truy cập bảng bạn bè. |
| **Luồng chính** | 1. Người chơi mở bảng bạn bè từ giao diện xã hội.<br>2. Hệ thống hiển thị danh sách bạn bè cùng trạng thái online và map hiện tại.<br>3. Người chơi nhập tên người nhận và gửi lời mời kết bạn.<br>4. Người nhận nhận được thông báo phản hồi lời mời.<br>5. Nếu người nhận chấp nhận, hệ thống thêm hai bên vào danh sách bạn bè của nhau.<br>6. Người chơi có thể tiếp tục xóa bạn hoặc xem lại trạng thái của từng người trong danh sách. |
| **Luồng phụ / Ngoại lệ** | Nếu người nhận không tồn tại hoặc đã là bạn, hệ thống từ chối lời mời. Nếu người nhận từ chối lời mời, yêu cầu kết bạn kết thúc mà không thay đổi dữ liệu. |
| **Kết quả** | Danh sách bạn bè được cập nhật đồng bộ ở cả hai phía liên quan. Nếu thất bại, danh sách giữ nguyên và hệ thống hiển thị thông báo nguyên nhân. |


![Biểu đồ ca sử dụng cho mô-đun Quản lý bạn bè](extracted_images/image20.png)

*Hình 2.13. Biểu đồ ca sử dụng cho mô-đun Quản lý bạn bè*

##### UC12 — Quản lý tổ đội và chat

| Thuộc tính | Nội dung |
|---|---|
| **Mã Use Case** | **UC12** |
| **Tên Use Case** | Quản lý tổ đội và chat |
| **Tác nhân chính (Actor)** | Người chơi |
| **Mô tả** | Người chơi tạo party, quản lý thành viên và trao đổi thông tin qua các kênh chat phù hợp. Hệ thống phân phối tin nhắn đến đúng phạm vi người nhận và cập nhật giao diện xã hội liên quan. |
| **Tiền điều kiện** | Người chơi đang online và không bị chặn tính năng xã hội do vi phạm quy định. |
| **Luồng chính** | 1. Người chơi mở giao diện xã hội và chọn tạo tổ đội hoặc mời thành viên.<br>2. Hệ thống tạo party mới và gán vai trò trưởng nhóm cho người khởi tạo.<br>3. Người chơi mời thêm thành viên từ danh sách bạn bè hoặc người chơi gần đó.<br>4. Sau khi party được hình thành, bảng tổ đội hiển thị trạng thái của từng thành viên.<br>5. Người chơi sử dụng khung chat để trao đổi trong các kênh chung hoặc kênh tổ đội.<br>6. Hệ thống phân phối tin nhắn đến đúng phạm vi người nhận và cập nhật giao diện xã hội liên quan. |
| **Luồng phụ / Ngoại lệ** | Nếu người được mời đang ở tổ đội khác, lời mời bị từ chối và thông báo cho người mời. Nếu người chơi gửi tin sai kênh hoặc vi phạm bộ lọc chat, hệ thống chặn và thông báo lỗi. |
| **Kết quả** | Party và nội dung trao đổi được cập nhật cho đúng thành viên hoặc đúng kênh nhận tin. Nếu thất bại, trạng thái party và chat giữ nguyên và hiển thị thông báo lỗi. |


![Biểu đồ ca sử dụng cho mô-đun Quản lý tổ đội và chat](extracted_images/image21.png)

*Hình 2.14. Biểu đồ ca sử dụng cho mô-đun Quản lý tổ đội và chat*

##### UC13 — Tham gia và hoàn tất phó bản

| Thuộc tính | Nội dung |
|---|---|
| **Mã Use Case** | **UC13** |
| **Tên Use Case** | Tham gia và hoàn tất phó bản |
| **Tác nhân chính (Actor)** | Người chơi, Máy chủ |
| **Mô tả** | Người chơi hoặc tổ đội tham gia phó bản, vượt qua các wave quái và nhận phần thưởng khi hoàn thành. Gameplay server khởi tạo phiên phó bản, spawn wave và xử lý phát thưởng khi kết thúc. |
| **Tiền điều kiện** | Người chơi đáp ứng điều kiện vào phó bản như level hoặc lượt vào. Gameplay server đang còn slot hoạt động. |
| **Luồng chính** | 1. Người chơi mở giao diện hoặc portal phó bản và chọn loại phó bản.<br>2. Hệ thống kiểm tra điều kiện về level, lượt vào và trạng thái tổ đội.<br>3. Gameplay server khởi tạo phiên phó bản và spawn các wave quái tương ứng.<br>4. Người chơi vượt qua từng đợt quái và tiến tới boss cuối.<br>5. Khi điều kiện hoàn thành được đáp ứng, hệ thống tổng kết kết quả phó bản.<br>6. Hệ thống phát phần thưởng và đưa người chơi rời khỏi phó bản sau khi kết thúc. |
| **Luồng phụ / Ngoại lệ** | Nếu người chơi hoặc tổ đội thất bại trong phó bản, hệ thống kết thúc phiên với trạng thái thất bại và không phát thưởng. Nếu máy chủ gặp lỗi spawn ở một wave, hệ thống ghi log và xử lý theo cấu hình fallback của phó bản. |
| **Kết quả** | Kết quả phó bản được ghi nhận và phần thưởng được phát theo trạng thái hoàn thành. Nếu thất bại, hệ thống kết thúc phiên và thông báo rõ nguyên nhân cho người chơi. |


![Biểu đồ ca sử dụng cho mô-đun Tham gia và hoàn tất phó bản](extracted_images/image22.png)

*Hình 2.15. Biểu đồ ca sử dụng cho mô-đun Tham gia và hoàn tất phó bản*

##### UC14 — Xem leaderboard

| Thuộc tính | Nội dung |
|---|---|
| **Mã Use Case** | **UC14** |
| **Tên Use Case** | Xem leaderboard |
| **Tác nhân chính (Actor)** | Người chơi |
| **Mô tả** | Người chơi theo dõi thứ hạng trong game; quản trị viên có thể làm mới hoặc reset dữ liệu xếp hạng. Hệ thống tải và hiển thị bảng xếp hạng theo hạng mục được chọn. |
| **Tiền điều kiện** | Dịch vụ leaderboard đang khả dụng. Người dùng có quyền truy cập tương ứng. |
| **Luồng chính** | 1. Người dùng mở giao diện bảng xếp hạng.<br>2. Hệ thống tải dữ liệu theo hạng mục mặc định hoặc hạng mục được chọn.<br>3. Người dùng chuyển đổi giữa các tab để xem top theo từng tiêu chí.<br>4. Hệ thống đánh dấu vị trí hiện tại của nhân vật nếu có mặt trên bảng xếp hạng.<br>5. Người dùng có thể yêu cầu làm mới để tải lại dữ liệu mới nhất.<br>6. Nếu là quản trị viên, hệ thống cho phép thực hiện thao tác reset theo quyền hạn được cấp. |
| **Luồng phụ / Ngoại lệ** | Nếu dữ liệu tạm thời chưa sẵn sàng, hệ thống hiển thị trạng thái chờ tải hoặc lấy từ cache gần nhất. Nếu người dùng không có quyền quản trị, hệ thống không cho phép reset bảng xếp hạng. |
| **Kết quả** | Bảng xếp hạng hiển thị đúng dữ liệu mới nhất hoặc được reset thành công khi có quyền quản trị. Nếu thất bại, dữ liệu cũ được giữ nguyên và có thông báo nguyên nhân. |


![Biểu đồ ca sử dụng cho mô-đun Xem leaderboard](extracted_images/image23.png)

*Hình 2.16. Biểu đồ ca sử dụng cho mô-đun Xem leaderboard*

---

#### 2.2.3.4. Nhóm 4 - Vận hành kỹ thuật (UC15 — UC16)

Nhóm này mô tả các tác vụ kỹ thuật chạy ngầm giữa máy chủ game thời gian thực và backend REST API.

##### UC15 — Đăng ký và duy trì gameplay server

| Thuộc tính | Nội dung |
|---|---|
| **Mã Use Case** | **UC15** |
| **Tên Use Case** | Quản lý gameplay server |
| **Tác nhân chính (Actor)** | Gameplay Server |
| **Mô tả** | Gameplay server tự đăng ký trạng thái hoạt động và được quản trị viên giám sát trong quá trình vận hành. Hệ thống xử lý heartbeat định kỳ và cập nhật trạng thái online/offline của server. |
| **Tiền điều kiện** | GameServerApi đang hoạt động. Gameplay server có thông tin xác thực hợp lệ. |
| **Luồng chính** | 1. Gameplay server khởi động và gửi yêu cầu đăng ký đến dịch vụ quản lý máy chủ.<br>2. Hệ thống xác thực thông tin server và lưu trạng thái online ban đầu.<br>3. Server gửi heartbeat theo chu kỳ để cập nhật tình trạng hoạt động.<br>4. Quản trị viên theo dõi trạng thái, tải hiện tại và tình trạng map của server.<br>5. Khi server cần dừng hoạt động, hệ thống thực hiện quy trình giải phóng hoặc unregister.<br>6. Dịch vụ quản lý máy chủ cập nhật lại trạng thái offline và xử lý người chơi còn kết nối nếu cần. |
| **Luồng phụ / Ngoại lệ** | Nếu heartbeat quá thời hạn cho phép, hệ thống tự đánh dấu server offline. Nếu xác thực server không hợp lệ, yêu cầu đăng ký bị từ chối và ghi log. |
| **Kết quả** | Trạng thái hoạt động của gameplay server được cập nhật chính xác trong hệ thống quản trị. Nếu thất bại, trạng thái server không được ghi nhận và có log chẩn đoán kèm theo. |


![Biểu đồ ca sử dụng cho mô-đun Quản lý gameplay server](extracted_images/image24.png)

*Hình 2.17. Biểu đồ ca sử dụng cho mô-đun Quản lý gameplay server*

##### UC16 — Host map và phát thưởng dungeon

| Thuộc tính | Nội dung |
|---|---|
| **Mã Use Case** | **UC16** |
| **Tên Use Case** | Host map và phát thưởng phó bản |
| **Tác nhân chính (Actor)** | Gameplay Server |
| **Mô tả** | Gameplay server đồng bộ cấu hình map đang host và xử lý phần thưởng khi phó bản kết thúc. Hệ thống backend kiểm tra dữ liệu kết quả, phân phối phần thưởng và lưu log phó bản. |
| **Tiền điều kiện** | Gameplay server đã đăng ký thành công. Server có thể giao tiếp với dịch vụ backend. |
| **Luồng chính** | 1. Gameplay server gửi danh sách map đang host lên hệ thống quản lý.<br>2. Quản trị viên hoặc quy trình tự động cập nhật cấu hình spawn cho từng map.<br>3. Gameplay server tải cấu hình mới nhất để áp dụng cho quái, boss và đối tượng mạng.<br>4. Khi một phó bản kết thúc, server tổng hợp dữ liệu kết quả của người chơi hoặc tổ đội.<br>5. Hệ thống backend xử lý phần thưởng, kinh nghiệm và log kết quả phó bản.<br>6. Kết quả cuối cùng được đồng bộ lại cho client và lưu vào cơ sở dữ liệu. |
| **Luồng phụ / Ngoại lệ** | Nếu cấu hình spawn không hợp lệ, hệ thống giữ cấu hình cũ và ghi log cảnh báo. Nếu phát thưởng lỗi do dữ liệu không hợp lệ, hệ thống dừng thao tác và chuyển vào hàng chờ xử lý lại. |
| **Kết quả** | Map host được đồng bộ đúng cấu hình và kết quả phó bản được phát thưởng chính xác. Nếu thất bại, dữ liệu được đưa vào hàng chờ xử lý lại và có log ghi nhận chi tiết. |


![Biểu đồ ca sử dụng cho mô-đun Host map và phát thưởng phó bản](extracted_images/image25.png)

*Hình 2.18. Biểu đồ ca sử dụng cho mô-đun Host map và phát thưởng phó bản*





### 2.2.4. Các biểu đồ tuần tự đặc tả ca sử dụng

Căn cứ vào các kịch bản Use case trọng tâm đã được đặc tả trong hệ thống Mutants Arena, phần này trình bày các biểu đồ tuần tự cho những luồng gameplay cốt lõi đang được triển khai thực tế trong dự án. Các biểu đồ được xây dựng bám sát kiến trúc client Unity, gameplay server, ASP.NET Core Web API và cơ sở dữ liệu MySQL, qua đó phản ánh rõ thứ tự tương tác giữa người chơi, giao diện, lớp xử lý nghiệp vụ và dữ liệu lưu trữ trong từng chức năng chính.

#### 2.2.4.1. Biểu đồ tuần tự Chiến đấu và sử dụng kỹ năng

Căn cứ vào kịch bản Use case UC04 - Chiến đấu và sử dụng kỹ năng, ta xây dựng các bước thực hiện của hệ thống với chức năng chiến đấu thời gian thực bằng biểu đồ tuần tự. Trong luồng này, người chơi chọn mục tiêu hoặc kích hoạt đòn đánh thường, kỹ năng trên thanh Q/W/E/R; client chiến đấu gửi yêu cầu lên gameplay server; gameplay server tiếp tục kiểm tra điều kiện cooldown, MP, hitbox và trạng thái khóa thao tác trước khi chuyển sang lớp CombatResolver hoặc đối tượng địch để tính sát thương.

Sau khi yêu cầu hợp lệ, hệ thống áp dụng DamageResult, cập nhật HP, buff, debuff và đồng bộ kết quả cho client điều khiển lẫn client quan sát bằng cơ chế server-authoritative. Khi mục tiêu bị tiêu diệt, hệ thống tiếp tục kích hoạt luồng phát EXP, vật phẩm rơi và các hook nhiệm vụ liên quan. Trong cùng biểu đồ, các tuần tự thất bại cũng được mô tả rõ cho trường hợp kỹ năng đang hồi chiêu, không đủ MP, mục tiêu ngoài phạm vi, trượt hitbox hoặc nhân vật đã chết.

![Biểu đồ tuần tự đặc tả ca sử dụng Chiến đấu và sử dụng kỹ năng](extracted_images/image26.png)

*Hình 2.19. Biểu đồ tuần tự đặc tả ca sử dụng Chiến đấu và sử dụng kỹ năng*

#### 2.2.4.2. Biểu đồ tuần tự Nâng cấp trang bị tại Blacksmith

Căn cứ vào kịch bản Use case UC06 - Nâng cấp trang bị, ta xây dựng các bước thực hiện của hệ thống với chức năng cường hóa trang bị tại Blacksmith bằng biểu đồ tuần tự. Luồng này bắt đầu khi người chơi mở giao diện Blacksmith, chọn trang bị mục tiêu và nạp cấu hình nâng cấp hiện tại từ hệ thống. Tại đây, hệ thống truy vấn inventory, equipment và cấu hình nâng cấp theo bậc hiện tại để trả về các tham số như `stoneId`, `stoneNeeded`, `success rate`, `failPolicy` và `upgradeLevel`.

Sau khi người chơi chọn đúng trang bị và các slot đá, charm tương ứng, client gửi yêu cầu nâng cấp kèm thông tin `slotIndex + count` để hệ thống kiểm tra số lượng vật liệu trên từng stack, phòng chống gian lận số lượng khi dùng vật liệu dạng chồng. Nếu hợp lệ, hệ thống trừ bạc, tiêu hao vật liệu, tính kết quả theo `failPolicy`, cập nhật `upgradeLevel`, `strOptions`, inventory and equipment. Biểu đồ đồng thời thể hiện rõ hai nhánh thất bại chính: thất bại xác thực do thiếu bạc, thiếu vật liệu, sai slot stack hoặc chạm mốc +24; và thất bại theo tỷ lệ, trong đó hệ thống giữ nguyên hoặc làm tụt cấp trang bị đúng theo cấu hình rủi ro đang dùng trong dự án.

![Biểu đồ tuần tự đặc tả ca sử dụng Nâng cấp trang bị tại Blacksmith](extracted_images/image27.png)

*Hình 2.20. Biểu đồ tuần tự đặc tả ca sử dụng Nâng cấp trang bị tại Blacksmith*

#### 2.2.4.3. Biểu đồ tuần tự Nâng Gene chính và Gene phụ

Căn cứ vào kịch bản Use case UC07 - Phát triển Gene, Hybrid và Tối Thượng, ta xây dựng các bước thực hiện của hệ thống với chức năng nâng Gene chính và Gene phụ bằng biểu đồ tuần tự. Ở giai đoạn Gene chính, người chơi mở giao diện Gene Evolution, nạp `player_data` and cấu hình trong `gene_upgrade_config`, sau đó xác nhận yêu cầu nâng cấp. Hệ thống kiểm tra `gene_exp`, bạc, vật liệu và giới hạn Tier trước khi quyết định kết quả nâng cấp; nếu thành công, hệ thống lưu Tier mới, `final_stats`, danh sách kỹ năng mở khóa và đồng bộ lại giao diện Gene cho người chơi.

Đối với Gene phụ, biểu đồ mô tả rõ quá trình chọn hệ phụ lần đầu, kiểm tra cặp hệ cố định đã triển khai trong dự án gồm Hỏa↔Thổ, Thủy↔Mộc và Kim↔Phong, sau đó nạp cấu hình từ `gene_multi_config` để thực hiện nâng hệ phụ. Nếu Gene phụ được nâng thành công, hệ thống cập nhật `secondaryElement`, `secondary_gene_tier`, bonus chỉ số theo hệ số giảm so với Gene chính, đồng thời bật cờ `canFuse` khi cả hai hệ đã đạt điều kiện hợp lệ. Các luồng phụ thất bại cũng được biểu diễn đầy đủ cho trường hợp thiếu `gene_exp`, thiếu bạc, thiếu vật liệu, Gene chính đã đạt Tier tối đa, chọn sai cặp hệ hoặc Gene phụ đã bị khóa trước đó.

![Biểu đồ tuần tự đặc tả ca sử dụng Nâng Gene chính và Gene phụ](extracted_images/image28.png)

*Hình 2.21. Biểu đồ tuần tự đặc tả ca sử dụng Nâng Gene chính và Gene phụ*

#### 2.2.4.4. Biểu đồ tuần tự Dung hợp Hybrid Gene

Căn cứ vào kịch bản Use case UC07 - Phát triển Gene, Hybrid và Tối Thượng, ta tiếp tục xây dựng các bước thực hiện của hệ thống với chức năng dung hợp Hybrid Gene bằng biểu đồ tuần tự. Luồng này bắt đầu khi người chơi mở tab Hybrid Fusion, yêu cầu nạp điều kiện thực hiện và hệ thống truy vấn các bảng `gene_hybrid_config`, `gene_hybrid_skill` cùng dữ liệu hiện thời trong `player_data`. Từ đó, giao diện nhận về các thông tin như số lượng Fusion Core, `gold cost`, bonus, prefab và bộ kỹ năng Hybrid tương ứng để hiển thị cho người chơi xác nhận.

Khi người chơi gửi yêu cầu dung hợp, hệ thống kiểm tra đồng thời các điều kiện quan trọng gồm trạng thái `isHybrid`, Tier của Gene chính và Gene phụ, tính hợp lệ của cặp hệ, số lượng Fusion Core và lượng vàng hiện có. Nếu dung hợp thành công, hệ thống lưu `HybridId`, `prefab`, các `immune elements`, bonus chiến đấu và bộ kỹ năng Hybrid mới vào dữ liệu nhân vật, sau đó đồng bộ lại `final_stats` và giao diện Hybrid cho client. Biểu đồ cũng thể hiện rõ hai nhóm điều kiện thất bại: chưa đủ Tier 5 hoặc cặp hệ không hợp lệ; thiếu Fusion Core, thiếu vàng hoặc nhân vật đã là Hybrid trước đó.

![Biểu đồ tuần tự đặc tả ca sử dụng Dung hợp Hybrid Gene](extracted_images/image29.png)

*Hình 2.22. Biểu đồ tuần tự đặc tả ca sử dụng Dung hợp Hybrid Gene*

----- [BẮT ĐẦU PHẦN THÊM MỚI] -----

#### 2.2.4.4a. Biểu đồ tuần tự Kích hoạt Gene Tối Thượng

Căn cứ vào kịch bản Use case UC07 - Phát triển Gene, Hybrid và Tối Thượng, ta xây dựng các bước thực hiện của hệ thống với chức năng kích hoạt trạng thái Gene Tối Thượng (Ultimate Gene) bằng biểu đồ tuần tự. Tiến trình này bắt đầu khi người chơi (sau khi đã dung hợp Hybrid thành công) thực hiện các hành động trong thế giới game như đánh quái, tiêu diệt Boss phó bản hoặc sử dụng vật phẩm hỗ trợ tăng EXP Tối Thượng (`GeneExpAdd`).

Gameplay Server hoặc Web API khi nhận được sự kiện tăng EXP sẽ cộng dồn giá trị vào trường `ultimate_gene_exp` trong cơ sở dữ liệu và gọi `NetworkPlayerDataSync` để đồng bộ thuộc tính về client. Khi điểm số này vượt qua mốc giới hạn 1,000,000 EXP, server sẽ tự động kích hoạt trạng thái Tối Thượng (`is_ultimate = true`). Ngay lập tức, `StatCalculator` tính toán lại và nhân x1.5 toàn bộ chỉ số HP, MP, ATK, DEF của nhân vật. Đồng thời, máy chủ gửi thông điệp ClientRpc yêu cầu client hiển thị Aura hào quang tương ứng với hệ Hybrid phía sau nhân vật (`aura1` cho Hỏa-Thổ, `aura2` cho Thủy-Mộc, `aura3` cho Kim-Phong) và hiển thị biểu tượng Tối Thượng trên HUD.

![Biểu đồ tuần tự đặc tả ca sử dụng Kích hoạt Gene Tối Thượng](extracted_images/image_ultimate_gene_sequence.png)

*Hình 2.22a. Biểu đồ tuần tự đặc tả ca sử dụng Kích hoạt Gene Tối Thượng*

----- [KẾT THÚC PHẦN THÊM MỚI] -----

#### 2.2.4.5. Biểu đồ tuần tự Tham gia và hoàn tất phó bản

Căn cứ vào kịch bản Use case UC13 - Tham gia và hoàn tất phó bản, ta xây dựng các bước thực hiện của hệ thống với chức năng tham gia dungeon, chiến đấu qua các wave và nhận thưởng hoàn tất bằng biểu đồ tuần tự. Luồng này bắt đầu khi người chơi chọn loại phó bản hoặc portal dungeon; hệ thống tiếp nhận yêu cầu và kiểm tra level, lượt vào cùng trạng thái tổ đội trước khi truy vấn `player_data`, `dungeon_config` và `dungeon_wave_config`. Nếu điều kiện hợp lệ, gameplay server khởi tạo phiên dungeon, spawn wave đầu, nạp boss và chuyển người chơi vào map phó bản.

Trong giai đoạn xử lý chính, gameplay server điều khiển toàn bộ vòng lặp chiến đấu qua wave, theo dõi tiến độ, spawn wave tiếp theo và boss cuối, sau đó tổng kết kết quả cho từng người chơi hoặc cả tổ đội khi đạt điều kiện chiến thắng. Ở bước kết thúc, hệ thống gọi xử lý phát thưởng, cập nhật inventory, reward, log và dữ liệu `dungeon_best_waves`, rồi đồng bộ EXP và phần thưởng cho client trước khi đưa người chơi trở về map an toàn. Biểu đồ cũng mô tả rõ các trường hợp ngoại lệ của dự án như người chơi hoặc tổ đội thất bại, hết thời gian, bị hạ gục toàn bộ nên không nhận thưởng hoàn tất; hoặc máy chủ gặp lỗi spawn ở một wave và phải xử lý theo cấu hình fallback của dungeon.

![Biểu đồ tuần tự đặc tả ca sử dụng Tham gia và hoàn tất phó bản](extracted_images/image30.png)

*Hình 2.23. Biểu đồ tuần tự đặc tả ca sử dụng Tham gia và hoàn tất phó bản*

### 2.2.5. Thiết kế cơ sở dữ liệu

Cơ sở dữ liệu của hệ thống Mutants Arena được thiết kế theo hướng chuẩn hóa các bảng định danh, cấu hình và ánh xạ nghiệp vụ, đồng thời sử dụng các cột JSON ở những thành phần gameplay biến đổi nhanh. Cách tổ chức này giúp hệ thống vẫn bảo đảm tính toàn vẹn dữ liệu ở các quan hệ cốt lõi như tài khoản, vật phẩm, Gene, bản đồ, nhiệm vụ và phó bản, nhưng không làm tăng quá nhiều số lượng bảng phụ khi cần đồng bộ túi đồ, trang bị, kỹ năng, buff và tiến trình nhiệm vụ giữa Unity Client, gameplay server và GameServerApi.

Đối chiếu trực tiếp với schema trong `gamedb.sql` và mã nguồn backend, mô hình dữ liệu của đề tài được tổ chức thành bốn cụm logic chính: cụm tài khoản và hồ sơ nhân vật, cụm vật phẩm và nâng cấp trang bị, cụm Gene - Hybrid - kỹ năng, và cụm thế giới game gồm quái vật, bản đồ, nhiệm vụ, boss và dungeon.

#### 2.2.5.1. Sơ đồ kết nối các bảng

Sơ đồ ERD dưới đây thể hiện mối liên kết giữa các bảng dữ liệu trọng tâm đang được sử dụng trực tiếp bởi hệ thống. Bố cục sơ đồ được nhóm theo đúng luồng xử lý thực tế của dự án, từ đăng nhập và hồ sơ nhân vật, sang vật phẩm và Gene, rồi tới nội dung PvE gồm bản đồ, quái vật, nhiệm vụ và phó bản.

![Sơ đồ cơ sở dữ liệu của hệ thống Mutants Arena](extracted_images/image31.png)

Hình 2.24. Sơ đồ cơ sở dữ liệu của hệ thống Mutants Arena

#### 2.2.5.2. Cấu trúc các bảng chính

Bảng 2.18. Nhóm bảng tài khoản, hồ sơ nhân vật và xã hội

| Bảng | Vai trò | Thuộc tính tiêu biểu |
|---|---|---|
| users | Lưu định danh tài khoản, phục vụ đăng ký, đăng nhập và phát hành JWT | user_id, username, email, password_hash, created_at, last_login |
| player_data | Lưu hồ sơ gameplay chính của người chơi, gồm chỉ số, trang bị, túi đồ, kỹ năng, buff và tiến trình nhiệm vụ | player_id, character_name, info_char, equipment, inventory, skills, active_buffs |
| player2_data | Lưu hồ sơ nhân vật hệ gene thứ hai, được backend sử dụng ở các luồng secondary gene và kỹ năng hệ phụ | player_id, character_name, info_char, skills, potential_stats, active_buffs |
| friend_relations | Lưu quan hệ bạn bè và trạng thái chấp nhận lời mời giữa các tài khoản | id, user_id, friend_id, status, created_at |

Bảng 2.19. Nhóm bảng vật phẩm, option và nâng cấp trang bị

| Bảng | Vai trò | Thuộc tính tiêu biểu |
|---|---|---|
| item_template | Định nghĩa toàn bộ mẫu vật phẩm của game như trang bị, potion, đá cường hóa, nguyên liệu Gene, vé dungeon và túi mở rộng | id, name, type, idClass, idIcon, levelNeed, isLock, sellPrice |
| item_effect_template | Cấu hình hiệu ứng phát sinh khi sử dụng item tiêu hao hoặc item buff | item_template_id, effect_type, value, duration_sec, icon_id, display_name |
| option_template | Định nghĩa các dòng chỉ số trang bị và tiến trình tăng chỉ số theo cấp cường hóa | id, name, type, level, strOption |
| equipment_upgrade_config | Cấu hình bạc tiêu hao, đá cần dùng, tỉ lệ thành công và chính sách thất bại khi cường hóa | upgrade_level, silver_cost, stone_id, stone_needed, base_success_rate, fail_policy |

Bảng 2.20. Nhóm bảng Gene, Hybrid và kỹ năng

| Bảng | Vai trò | Thuộc tính tiêu biểu |
|---|---|---|
| gene_upgrade_config | Cấu hình nâng Gene chính theo từng hệ nguyên tố và mốc tier | tier_from, element_type, gene_exp_required, silver_cost, stone_id, stone_needed, base_success_rate |
| gene_multi_config | Cấu hình nâng Gene phụ, phục vụ cơ chế đa hệ và mở rộng Hybrid | tier_from, element_type, gene_exp_required, silver_cost, stone_id, stone_needed, base_success_rate |
| gene_tier_stat_config | Quy định lượng HP, MP, tấn công và phòng thủ cộng thêm khi đạt tier mới | element_type, tier_to, hp_bonus, mp_bonus, attack_bonus, defense_bonus |
| gene_hybrid_config | Định nghĩa các tổ hợp Hybrid, vật phẩm dung hợp, bonus chỉ số và prefab hiển thị trong Unity | hybrid_id, element_a, element_b, hybrid_name, fusion_item_id, fusion_item_count, atk_bonus_percent, prefab_path |
| gene_hybrid_skill | Ánh xạ mỗi tổ hợp Hybrid với kỹ năng đặc biệt tương ứng | id, hybrid_id, skill_code, slot_priority |
| skill_template | Lưu cấu hình kỹ năng tĩnh của game, bao gồm kỹ năng thường, kỹ năng nguyên tố và kỹ năng Hybrid | skill_id, skill_code, skill_name, element_type, levels_json, gene_tier_required, hybrid_id |

Bảng 2.21. Nhóm bảng thế giới game, quái vật, nhiệm vụ và dungeon

| Bảng | Vai trò | Thuộc tính tiêu biểu |
|---|---|---|
| enemy | Lưu dữ liệu quái thường, elite và boss, bao gồm chỉ số chiến đấu, kháng nguyên tố, skill và phần thưởng rơi | enemy_id, enemy_name, level, base_hp, base_damage, enemy_type, drop_items_json, skills_json |
| boss_config | Cấu hình boss theo từng map với thời điểm spawn, vị trí và chu kỳ hồi sinh | boss_id, map_id, spawn_x, spawn_y, respawn_minutes, is_active |
| map_config | Lưu thông tin bản đồ, scene Unity, ngưỡng cấp độ và điều kiện nhiệm vụ để mở khóa map | map_id, map_name, scene_name, min_level, max_level, required_quest_id |
| quest_config | Lưu chuỗi nhiệm vụ, NPC liên quan, phần thưởng và các bước tiến trình dưới dạng JSON | id, name, npc_id, level_need, item_reward, step, is_active |
| dungeon_config | Lưu cấu hình tổng quát của phó bản, gồm loại dungeon, map sử dụng, boss và reward tổng | dungeon_id, dungeon_name, dungeon_type, map_id, max_players, min_level_required, boss_enemy_id, reward_json |
| dungeon_wave_config | Lưu cấu hình wave, giới hạn lượt vào và mốc thưởng theo tiến trình dungeon dạng sóng | dungeon_id, max_waves, wave_time_seconds, enemy_scale_percent, daily_entry_limit, milestone_reward_json |

Thiết kế cơ sở dữ liệu trên phục vụ ba mục tiêu chính. Thứ nhất, dữ liệu định danh tài khoản được tách khỏi dữ liệu gameplay: `users` chỉ lưu thông tin đăng nhập và phát hành JWT, còn `player_data`, `player2_data` và `player_equipment` lưu trạng thái nhân vật, trang bị, túi đồ, kỹ năng, buff và tiến trình nhiệm vụ. Cách tổ chức này giúp bảo vệ thông tin tài khoản và giảm rủi ro khi thay đổi các logic gameplay như combat, Gene, inventory hoặc dungeon. Thứ hai, dữ liệu cấu hình tiến trình RPG được chia thành các cụm riêng gồm vật phẩm - nâng cấp trang bị (`item_template`, `item_effect_template`, `option_template`, `equipment_upgrade_config`), Gene - Hybrid - kỹ năng (`gene_upgrade_config`, `gene_multi_config`, `gene_tier_stat_config`, `gene_hybrid_config`, `gene_hybrid_skill`, `skill_template`) và nội dung PvE (`enemy`, `map_config`, `quest_config`, `boss_config`, `dungeon_config`, `dungeon_wave_config`). Nhờ vậy, hệ thống có thể truy vết và cân bằng đầy đủ vòng đời phát triển nhân vật từ nhận vật phẩm, nâng trang bị, tiến hóa Gene đến làm nhiệm vụ và hoàn thành phó bản mà không phải hard-code trực tiếp ở client. Thứ ba, dữ liệu bản đồ và vận hành được tách khỏi dữ liệu nhân vật thông qua các bảng như `map_portal`, `map_spawn_config`, `leaderboard_cache` và `player_action_log`, hỗ trợ điều hướng map, cấu hình spawn, xếp hạng, audit hành vi người chơi và theo dõi các luồng vận hành runtime của gameplay server qua các API host/heartbeat.

---

## 2.3. Tổng kết chương 2

Chương 2 đã được chuẩn hóa lại theo đúng trạng thái triển khai thực tế của dự án Mutants Arena thay vì chỉ dừng ở mức mô tả ý tưởng. Trên cơ sở đọc trực tiếp mã nguồn API, SignalR Hub và client Unity, hệ thống được xác định có bốn actor chính gồm Guest, Player, Admin/Operator và Máy chủ gameplay / Netcode server. Từ đó, các yêu cầu chức năng được tái cấu trúc thành các nhóm triển khai thật: xác thực, nhân vật, bản đồ, combat, Gene Evolution, inventory/equipment, NPC/quest/shop, social, dungeon, đồng bộ thời gian thực và vận hành server.

Phần Use Case là trọng tâm được viết lại toàn diện với 20 use case đầy đủ, bao phủ toàn bộ tính năng đã có trong mã nguồn: từ đăng ký, đăng nhập, tạo nhân vật, combat, Gene chính/Gene phụ/Hybrid Fusion, blacksmith, NPC service, quest event-driven, friend, party, chat, dungeon, leaderboard đến các luồng vận hành kỹ thuật như zone heartbeat, host runtime, spawn config và dungeon reward grant. Nhờ đó, chương 2 không chỉ mô tả đầy đủ nghiệp vụ của hệ thống mà còn tạo được sự liên kết rõ ràng giữa kiến trúc cài đặt, sơ đồ phân tích và đặc tả chức năng trong báo cáo.

Toàn bộ kết quả phân tích và đặc tả trong chương này là cơ sở trực tiếp cho Chương 3, nơi từng hệ thống sẽ được triển khai, giải thích kiến trúc code và kiểm chứng bằng luồng runtime tương ứng.
