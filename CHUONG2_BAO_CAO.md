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

Hệ thống game Mutants Arena phục vụ ba nhóm đối tượng chính với vai trò và quyền hạn khác nhau. Sự phân chia rõ ràng này đảm bảo mỗi nhóm chỉ truy cập các chức năng phù hợp với vai trò, tránh xung đột quyền hạn và tăng cường bảo mật hệ thống.

Bảng 2.1: Các tác nhân tham gia hệ thống

| Tên tác nhân | Vai trò |
|---|---|
| Khách (Guest) | Người dùng chưa đăng nhập. Chỉ truy cập được màn hình đăng nhập và đăng ký. Không có quyền tương tác với bất kỳ nội dung game nào. |
| Người chơi (Player) | Người dùng đã đăng ký và đăng nhập thành công. Thực hiện toàn bộ hoạt động gameplay: tạo nhân vật, di chuyển, chiến đấu, nâng cấp Gene, làm nhiệm vụ, giao dịch với NPC, tham gia nhóm và phó bản. |
| Quản trị viên (Admin) | Người vận hành hệ thống server. Có quyền quản lý cấu hình bản đồ, điều chỉnh chỉ số quái vật/boss, theo dõi trạng thái server và xử lý sự cố. Tương tác qua giao diện quản trị server-side, không qua client game. |

### 2.1.3. Yêu cầu chức năng

Hệ thống game Mutants Arena cần đáp ứng các yêu cầu chức năng được nhóm theo từng lĩnh vực nghiệp vụ. Mỗi nhóm chức năng tương ứng với một module độc lập trong kiến trúc phần mềm, cho phép phát triển và kiểm thử từng phần một cách độc lập trước khi tích hợp tổng thể.

**Phân loại theo phạm vi đề tài.** Trong bối cảnh tên đề tài đã định danh rõ trục Gene Evolution + Multiplayer, các yêu cầu chức năng được phân thành hai nhóm: *chức năng cốt lõi* (trực tiếp phục vụ hai trục chính) và *chức năng mở rộng* (gameplay nền hoặc demo có thể bị thu hẹp khi cần). Cách phân loại này giúp người đọc và hội đồng chấm đồ án không nhầm lẫn giữa "phần phải đánh giá đầy đủ" và "phần để mở cho hướng phát triển".

**Bảng 2.0a: Phân loại chức năng cốt lõi và mở rộng**

| Nhóm chức năng | Vai trò trong đề tài | Mức ưu tiên | Trạng thái |
|---|---|---|---|
| Đăng nhập / nhân vật | Lưu tiến trình, gắn JWT cho NGO | **Cốt lõi** | Hoàn thành |
| Di chuyển / combat real-time | Gameplay nền cho Gene và Multiplayer | **Cốt lõi** | Hoàn thành |
| **Gene Upgrade + Fusion + Tương khắc** | **Trục đặc trưng đề tài** | **Cốt lõi (trọng tâm)** | Hoàn thành |
| **Multiplayer Server-Authoritative (NGO + Zone)** | **Trục đặc trưng đề tài** | **Cốt lõi (trọng tâm)** | Hoàn thành |
| Party (SignalR) + Dungeon đồng đội | Tạo ngữ cảnh để Gene + Multiplayer phát huy | **Cốt lõi** | Hoàn thành |
| Lưu CSDL (player_data, gene_inventory, party_groups) | Persistence cho hai trục trên | **Cốt lõi** | Hoàn thành |
| Quest / NPC / Shop / Trang bị | Gameplay nền | Phụ trợ | Hoàn thành cơ bản |
| Marketplace (P2P trading) | Demo tham khảo | **Mở rộng** | Chưa triển khai |
| Ranked PvP | Demo tham khảo | **Mở rộng** | Chưa triển khai |
| Friend system | Tiện ích xã hội | **Mở rộng** | Hoàn thành cơ bản |
| Admin Web Dashboard | Vận hành | **Mở rộng** | Hoàn thành mức cơ bản |
| Daily Quest, Achievement | Tăng vòng quay người chơi | **Mở rộng** | Chưa triển khai |

Phần liệt kê chi tiết dưới đây tuân theo thứ tự ưu tiên trên: nhóm cốt lõi trước, nhóm mở rộng sau.

Quản lý tài khoản và xác thực:
▪ Người dùng đăng ký tài khoản bằng tên đăng nhập và mật khẩu; hệ thống kiểm tra tính hợp lệ, mã hóa mật khẩu bằng BCrypt và lưu vào database
▪ Người dùng đăng nhập; hệ thống xác thực và cấp JWT token HS256 có thời hạn 24 giờ
▪ JWT token được đính kèm trong mọi request REST API và trong handshake kết nối NGO; kết nối không có token hợp lệ bị từ chối
▪ Người chơi đăng xuất; hệ thống vô hiệu hóa phiên và xóa token phía client

Quản lý nhân vật:
▪ Tạo nhân vật mới: chọn tên và lớp nguyên tố (Hỏa/Kim/Mộc/Phong/Thổ/Thủy); mỗi lớp có bộ chỉ số cơ bản và kỹ năng đặc trưng khác nhau; mỗi tài khoản tối đa 2 nhân vật
▪ Xem và theo dõi thông tin nhân vật: cấp độ, HP/MP, chỉ số chiến đấu, Gene đang trang bị, trang bị đang mặc
▪ Hệ thống lên cấp tự động khi nhân vật tích lũy đủ EXP; mỗi cấp tăng chỉ số cơ bản theo bảng tính sẵn
▪ Lưu tự động tiến trình nhân vật sau mỗi sự kiện quan trọng: hoàn thành nhiệm vụ, đánh boss, nâng cấp trang bị hoặc Gene

Di chuyển và chiến đấu:
▪ Nhân vật di chuyển trái/phải, nhảy đơn và nhảy đôi, Dash với invincibility frames
▪ Đòn tấn công thường (melee hoặc ranged tùy lớp) với hitbox xác định vùng va chạm; toàn bộ tính toán sát thương thực hiện trên server
▪ Hệ thống kỹ năng: mỗi nhân vật có tối đa 4 kỹ năng gắn vào slot Q/W/E/R, mỗi kỹ năng có cooldown và tiêu MP
▪ Hệ thống tương khắc nguyên tố: sát thương nhân được ×1.5 (ưu thế) hoặc ×0.75 (bất lợi) tùy thuộc vào cặp nguyên tố giữa người tấn công và người nhận
▪ Trạng thái chiến đấu (status effects) gồm Burn, Freeze, Stun, Poison — mỗi trạng thái có thời lượng và hiệu ứng riêng

Hệ thống Gene và nâng cấp:
▪ Mỗi nhân vật có Gene chính (cùng lớp nguyên tố) và tối đa hai Gene phụ (khác lớp)
▪ Gene có 5 Tier (1 → 5); nâng Tier cần Fragment và Gold theo bảng yêu cầu tăng dần
▪ Gene Fusion: kết hợp hai Gene để tạo Hybrid Gene với hiệu ứng từ cả hai nguyên tố
▪ Gene trang bị cung cấp chỉ số thụ động (ATK, DEF, HP, Critical Rate) và mở khóa kỹ năng đặc trưng theo Tier

Trang bị và vật phẩm:
▪ Hệ thống trang bị gồm ba slot: Weapon, Armor, Accessory — mỗi slot ảnh hưởng chỉ số khác nhau
▪ Nâng cấp trang bị từ +1 đến +20 tại NPC Blacksmith bằng nguyên liệu và Gold; xác suất thành công giảm dần ở mức cao
▪ Inventory lưu trữ vật phẩm, phân loại theo loại và độ hiếm từ Common đến Legendary
▪ Giao dịch mua/bán tại NPC cửa hàng; mỗi NPC có danh sách hàng hóa riêng theo zone

Nhiệm vụ (Quest):
▪ Hệ thống nhiệm vụ chính (Main Quest) dẫn dắt cốt truyện theo từng chương game
▪ Hệ thống nhiệm vụ phụ (Side Quest) cung cấp thêm EXP, vật phẩm và Gold
▪ Theo dõi tiến trình nhiệm vụ: hoàn thành mục tiêu (giết X quái vật, thu thập Y vật phẩm, đến địa điểm Z)
▪ Nhận thưởng tự động khi hoàn thành; lịch sử nhiệm vụ lưu trong database theo trạng thái Not_Started/In_Progress/Completed

Chế độ nhóm và phó bản:
▪ Tạo nhóm (Party) tối đa 4 người; mời người chơi khác qua tên nhân vật qua SignalR Hub
▪ Chia sẻ EXP và loot khi chiến đấu cùng nhóm; tỷ lệ phân chia theo đóng góp sát thương
▪ Phó bản (Dungeon): khu vực đặc biệt dành riêng cho nhóm với hệ thống wave quái liên tiếp, kết thúc bằng Boss
▪ Hệ thống Wave-based: quái xuất hiện theo từng đợt có cấu hình riêng; độ khó tăng dần; Boss xuất hiện sau wave cuối

Quản trị hệ thống (Admin):
▪ Cấu hình thuộc tính zone/map: kích thước, spawn points quái, điểm hồi sinh, vùng trigger event
▪ Quản lý chỉ số quái vật và boss qua trường phases_json; thay đổi không cần recompile code
▪ Theo dõi số lượng người chơi online theo zone; xem log kết nối và xử lý sự cố ngắt kết nối bất thường

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

Biểu đồ Use Case tổng quát mô tả các nhóm chức năng chính mà hệ thống cung cấp cho từng loại tác nhân. Mỗi use case đại diện cho một mục tiêu nghiệp vụ hoàn chỉnh từ góc nhìn người dùng — từ khi bắt đầu tương tác đến khi hệ thống phản hồi kết quả cuối cùng. Biểu đồ này giúp hình dung tổng quan phạm vi chức năng và ranh giới quyền hạn giữa các tác nhân trước khi đi vào đặc tả chi tiết.

Bảng 2.1 đã liệt kê ba tác nhân chính: Khách (Guest), Người chơi (Player) và Quản trị viên (Admin). Trên biểu đồ Use Case tổng quát (Hình 2.2), Guest chỉ tương tác với nhóm chức năng Quản lý tài khoản (Đăng ký, Đăng nhập). Sau khi đăng nhập, Người chơi có quyền truy cập toàn bộ chức năng gameplay gồm sáu nhóm: Quản lý nhân vật, Chiến đấu & Gene, Trang bị & Vật phẩm, Nhiệm vụ, Nhóm & Phó bản. Admin tương tác với nhóm chức năng Quản trị hệ thống qua giao diện riêng.

**Hình 2.2: Biểu đồ Use Case tổng quát hệ thống Mutants Arena**

*Mô tả biểu đồ:* Biểu đồ bao gồm một vùng hệ thống (system boundary) mang tên "Hệ thống Mutants Arena". Bên ngoài ranh giới hệ thống có ba tác nhân: **Guest** (bên trái trên), **Player** (bên trái dưới) và **Admin** (bên phải). Bên trong vùng hệ thống có 7 use case được bố trí thành hai hàng:

- Hàng trên (phải): **Đăng ký/Đăng nhập** → **Quản lý nhân vật** → **Chiến đấu & Skill**
- Hàng dưới (phải): **Nâng cấp Gene** → **Party & Dungeon** → **Quest/NPC/Shop**
- Ngoài vùng biên (phải trên): **Quản trị server** (dành riêng cho Admin)

Quan hệ tác nhân – use case: **Guest** chỉ kết nối tới "Đăng ký/Đăng nhập". **Player** kết nối tới tất cả 6 use case bên trong vùng hệ thống (bao gồm "Đăng ký/Đăng nhập" như Guest nhưng mở rộng thêm toàn bộ chức năng gameplay). **Admin** kết nối tới "Quản trị server". Mũi tên từ các tác nhân đến use case thể hiện quan hệ kết hợp (association), không có quan hệ <<include>> hay <<extend>> ở biểu đồ tổng quát này.

### 2.2.3. Biểu đồ Use Case chi tiết

a) Use Case Đăng ký tài khoản

**Hình 2.3: Biểu đồ Use Case — Đăng ký tài khoản**

*Mô tả biểu đồ:* Vùng hệ thống "Hệ thống Mutants Arena" chứa 4 use case nằm trên một chuỗi tuần tự nối bằng mũi tên: **Nhập thông tin** → **Kiểm tra hợp lệ** → **Lưu tài khoản/JWT** → **Trả kết quả**. Tác nhân **Guest** và **Player** (bên trái) kết nối vào use case "Nhập thông tin" — đây là điểm khởi phát. Tác nhân **Admin** (bên phải) kết nối vào use case "Trả kết quả" — thể hiện vai trò giám sát log và phê duyệt tài khoản khi cần. Toàn bộ chuỗi use case nằm hoàn toàn bên trong vùng hệ thống, phản ánh việc logic xử lý thuộc trách nhiệm của API Server.

**Đặc tả Use Case:**

| Trường | Nội dung |
|---|---|
| **Use Case** | Đăng ký tài khoản |
| **Actor** | Khách (Guest) |
| **Mô tả** | Người dùng chưa có tài khoản tạo tài khoản mới với tên đăng nhập và mật khẩu. Hệ thống kiểm tra tính hợp lệ, mã hóa mật khẩu và lưu vào database. |
| **Tiền điều kiện** | 1. Người dùng đang ở màn hình đăng nhập và chọn "Đăng ký". 2. API Server đang hoạt động và có thể kết nối. |
| **Luồng chính** | 1. Người dùng nhập tên đăng nhập, mật khẩu và xác nhận mật khẩu. 2. Client kiểm tra validation phía client (độ dài, ký tự hợp lệ, khớp mật khẩu). 3. Client gửi POST /api/auth/register với thông tin đăng ký. 4. Server kiểm tra tên đăng nhập chưa tồn tại trong database. 5. Server hash mật khẩu bằng BCrypt (cost factor 12). 6. Server tạo bản ghi Player mới; trả về 201 Created. 7. Client hiển thị thông báo thành công và chuyển hướng về màn hình đăng nhập. |
| **Luồng phụ** | 2.1. Mật khẩu và xác nhận không khớp → client hiển thị lỗi ngay lập tức, không gửi request. 2.2. Tên đăng nhập ít hơn 4 ký tự hoặc chứa ký tự đặc biệt → client hiển thị lỗi validation. 4.1. Tên đăng nhập đã tồn tại → server trả về 409 Conflict; client hiển thị "Tên đăng nhập đã được sử dụng". 3.1. Timeout kết nối → client hiển thị "Không thể kết nối server" và cho phép thử lại. |
| **Kết quả** | Tài khoản được tạo thành công; người dùng có thể đăng nhập ngay với thông tin vừa tạo. |

b) Use Case Đăng nhập

**Hình 2.4: Biểu đồ Use Case — Đăng nhập**

*Mô tả biểu đồ:* Cấu trúc biểu đồ tương tự Hình 2.3. Vùng hệ thống chứa 4 use case theo chuỗi: **Nhập thông tin** → **Kiểm tra hợp lệ** → **Lưu khoản/JWT** → **Trả kết quả**. Tác nhân **Guest** và **Player** (bên trái) khởi phát tại "Nhập thông tin". Tác nhân **Admin** (bên phải) kết nối vào "Trả kết quả" để theo dõi log đăng nhập. Điểm khác biệt với Hình 2.3 là use case thứ ba mang tên "Lưu khoản/JWT" — nhấn mạnh việc server cấp JWT token và lưu phiên đăng nhập, là điều kiện để các request tiếp theo được xác thực.

**Đặc tả Use Case:**

| Trường | Nội dung |
|---|---|
| **Use Case** | Đăng nhập |
| **Actor** | Khách (Guest) |
| **Mô tả** | Người dùng xác thực tài khoản bằng tên đăng nhập và mật khẩu. Hệ thống cấp JWT token để sử dụng cho các request và kết nối game sau đó. |
| **Tiền điều kiện** | 1. Người dùng đã có tài khoản hợp lệ. 2. API Server đang hoạt động. |
| **Luồng chính** | 1. Người dùng nhập tên đăng nhập và mật khẩu. 2. Client gửi POST /api/auth/login. 3. Server tìm Player theo tên đăng nhập trong database. 4. Server so sánh mật khẩu nhập vào với hash BCrypt đã lưu. 5. Server tạo JWT token (HS256, thời hạn 24 giờ) và trả về kèm thông tin player. 6. Client lưu token vào PlayerPrefs; chuyển đến màn hình chọn nhân vật. |
| **Luồng phụ** | 3.1. Tên đăng nhập không tồn tại → server trả về 401; client hiển thị "Tên đăng nhập hoặc mật khẩu không đúng" (không tiết lộ field nào sai). 4.1. Mật khẩu không khớp → trả về 401 với thông báo giống trường hợp 3.1. 2.1. Timeout kết nối → client hiển thị "Không thể kết nối server, vui lòng thử lại" và giữ nguyên màn hình. |
| **Kết quả** | Người dùng đăng nhập thành công; JWT token được lưu; truy cập được toàn bộ chức năng game. |

c) Use Case Tạo và chọn nhân vật

**Hình 2.5: Biểu đồ Use Case — Tạo và chọn nhân vật**

*Mô tả biểu đồ:* Vùng hệ thống chứa 4 use case theo chuỗi: **Tạo và chọn nhân vật** → **Kiểm tra điều kiện** → **Cập nhật dữ liệu** → **Phản hồi**. Tác nhân **Guest** (bên trái trên) và **Player** (bên trái dưới) cùng kết nối vào use case đầu tiên "Tạo và chọn nhân vật" — Guest có thể xem màn hình chọn nhân vật nhưng chỉ Player đã đăng nhập mới thực sự tạo và vào game. Tác nhân **Admin** (bên phải) kết nối vào "Phản hồi" để theo dõi trạng thái. Use case "Kiểm tra điều kiện" bao gồm: xác thực JWT, kiểm tra số lượng nhân vật chưa đầy (≤2), kiểm tra tên không trùng. "Cập nhật dữ liệu" ghi nhân vật mới vào DB và nạp character data khi chọn nhân vật cũ.

**Đặc tả Use Case:**

| Trường | Nội dung |
|---|---|
| **Use Case** | Tạo và chọn nhân vật |
| **Actor** | Người chơi (Player) |
| **Mô tả** | Người chơi tạo nhân vật mới với lớp nguyên tố hoặc chọn nhân vật đã có để bắt đầu phiên chơi. Mỗi tài khoản tối đa 3 nhân vật. |
| **Tiền điều kiện** | 1. Người chơi đã đăng nhập và có JWT token hợp lệ. 2. Màn hình Character Select đang hiển thị. |
| **Luồng chính — Tạo mới** | 1. Người chơi chọn slot trống và nhấn "Tạo nhân vật". 2. Nhập tên nhân vật và chọn lớp nguyên tố từ 6 lựa chọn. 3. Client gửi POST /api/characters với thông tin nhân vật kèm JWT. 4. Server tạo bản ghi Character với chỉ số khởi đầu theo lớp. 5. Server trả về 201; client hiển thị nhân vật mới trong slot. |
| **Luồng chính — Chọn nhân vật** | 1. Người chơi click vào nhân vật đã có. 2. Client lưu characterId được chọn. 3. Người chơi nhấn "Vào game". 4. Client kết nối đến Game Server NGO với JWT token và characterId trong Connection Data. 5. Server xác thực token và nạp dữ liệu nhân vật từ database. 6. Client chuyển đến scene bản đồ cuối cùng nhân vật đang đứng. |
| **Luồng phụ** | 2.1. Tên nhân vật đã tồn tại → server trả về 409; client hiển thị "Tên nhân vật đã được sử dụng". 1.1. Đã có 3 nhân vật → slot tạo mới bị vô hiệu hóa; hiển thị "Đã đầy slot nhân vật". 4.1. JWT hết hạn → server từ chối kết nối NGO; client hiển thị "Phiên đăng nhập hết hạn" và chuyển về màn hình đăng nhập. |
| **Kết quả** | Nhân vật được tạo hoặc chọn thành công; người chơi vào game tại bản đồ tương ứng. |

d) Use Case Chiến đấu với quái vật

**Hình 2.6: Biểu đồ Use Case — Chiến đấu với quái vật**

*Mô tả biểu đồ:* Vùng hệ thống chứa 4 use case theo chuỗi: **Chiến đấu với quái vật** → **Kiểm tra điều kiện** → **Cập nhật dữ liệu** → **Phản hồi**. Tác nhân **Guest** (trái trên) và **Player** (trái dưới) kết nối vào use case khởi phát — thực tế chỉ Player đã đăng nhập và đang trong bản đồ mới thực hiện được. Tác nhân **Admin** (phải) kết nối vào "Phản hồi" để theo dõi log server. "Kiểm tra điều kiện" ở đây bao gồm xác nhận player còn sống, đang trong zone có quái, kỹ năng không đang cooldown. "Cập nhật dữ liệu" là bước server tính sát thương (áp dụng hệ số nguyên tố ×1.5/×0.75), trừ HP quái, cập nhật NetworkVariable và ghi EXP vào DB khi quái chết.

**Đặc tả Use Case:**

| Trường | Nội dung |
|---|---|
| **Use Case** | Chiến đấu với quái vật |
| **Actor** | Người chơi (Player) |
| **Mô tả** | Người chơi tiêu diệt quái vật trong bản đồ để nhận EXP và vật phẩm drop. Toàn bộ tính toán sát thương và HP được thực hiện trên server, không phụ thuộc client. |
| **Tiền điều kiện** | 1. Người chơi đang ở trong bản đồ có quái vật spawn. 2. Nhân vật đang sống (HP > 0). |
| **Luồng chính** | 1. Quái vật phát hiện người chơi trong tầm nhìn; AI chuyển sang trạng thái Chase. 2. Quái vật tiếp cận và tấn công; server tính sát thương áp dụng hệ số nguyên tố. 3. Người chơi gửi input tấn công lên server qua AttackServerRpc. 4. Server kiểm tra hitbox, áp dụng nguyên tố multiplier và trừ HP quái. 5. Server đồng bộ HP quái mới xuống tất cả client qua NetworkVariable. 6. Khi HP quái về 0: server kích hoạt death animation, tính toán và spawn vật phẩm drop. 7. Server gọi API lưu EXP mới, kiểm tra điều kiện lên cấp và cập nhật database. |
| **Luồng phụ** | 2.1. Người chơi nhận đủ sát thương → HP = 0; nhân vật chết; hồi sinh tại checkpoint gần nhất sau 5 giây. 6.1. Không có vật phẩm drop theo xác suất → chỉ cộng EXP, không spawn item vật lý. 7.1. Đủ EXP để lên cấp → server xử lý lên cấp, cập nhật chỉ số và thông báo cho client. |
| **Kết quả** | Quái vật bị tiêu diệt; người chơi nhận EXP và vật phẩm (nếu có); dữ liệu được lưu database. |

e) Use Case Nâng cấp Gene

**Hình 2.7: Biểu đồ Use Case — Nâng cấp Gene**

*Mô tả biểu đồ:* Đây là use case có chuỗi dài nhất — 5 bước bên trong vùng hệ thống: **Xem Gene** → **Chọn Tier** → **Kiểm tra vật liệu** → **Nâng cấp/Fusion** → **Cập nhật chỉ số**. Tác nhân **Guest** (trái trên) và **Player** (trái dưới) kết nối vào "Xem Gene" — Guest có thể xem thông tin Gene nhưng không thể thực hiện nâng cấp. Tác nhân **Admin** (phải) kết nối vào "Cập nhật chỉ số". Chuỗi 5 bước phản ánh đúng luồng thực tế: (1) player vào menu Gene, (2) chọn Gene mục tiêu và Tier đích, (3) server kiểm tra đủ vật liệu (stone + gold theo `gene_upgrade_config`), (4) server thực hiện nâng cấp hoặc Fusion, (5) server cập nhật HP/ATK/DEF của nhân vật theo `gene_tier_stat_config`. Use case "Nâng cấp/Fusion" tách thành hai nhánh: nâng Tier (Tier 1→5 cho một nguyên tố) hoặc Fusion (ghép hai Gene khác nguyên tố thành Hybrid).

**Đặc tả Use Case:**

| Trường | Nội dung |
|---|---|
| **Use Case** | Nâng cấp Gene |
| **Actor** | Người chơi (Player) |
| **Mô tả** | Người chơi nâng cấp Tier của Gene nguyên tố để tăng chỉ số thụ động và mở khóa kỹ năng đặc trưng mới. Cần đủ Fragment và Gold theo bảng yêu cầu từng Tier. |
| **Tiền điều kiện** | 1. Người chơi đã đăng nhập và đang ở menu Gene. 2. Nhân vật có Gene chưa đạt Tier tối đa (Tier 5). |
| **Luồng chính** | 1. Người chơi vào menu Gene, chọn Gene cần nâng cấp. 2. Hệ thống hiển thị yêu cầu Fragment và Gold cho Tier tiếp theo và so sánh với số hiện có. 3. Người chơi nhấn "Nâng cấp". 4. Client gửi POST /api/gene/upgrade với characterId và geneType kèm JWT. 5. Server kiểm tra đủ Fragment và Gold; trừ nguyên liệu, tăng Tier Gene trong database. 6. Server tính toán lại chỉ số nhân vật dựa trên Gene mới và cập nhật character_stats. 7. Server trả về dữ liệu Gene mới; client cập nhật UI hiển thị Tier và chỉ số mới. |
| **Luồng phụ** | 2.1. Không đủ Fragment → nút "Nâng cấp" bị vô hiệu hóa; hiển thị số Fragment còn thiếu và gợi ý địa điểm farm. 2.2. Không đủ Gold → tương tự, hiển thị số Gold còn thiếu. 5.1. Gene đã ở Tier 5 → server trả về 400; client hiển thị "Gene đã đạt cấp tối đa". |
| **Kết quả** | Gene được nâng Tier thành công; chỉ số nhân vật cập nhật tự động; kỹ năng mới được mở khóa nếu Tier đủ điều kiện. |

f) Use Case Nhận và hoàn thành nhiệm vụ

**Hình 2.8: Biểu đồ Use Case — Nhận và hoàn thành nhiệm vụ**

*Mô tả biểu đồ:* Vùng hệ thống chứa 4 use case theo chuỗi: **Nhận và hoàn thành nhiệm vụ** → **Kiểm tra điều kiện** → **Cập nhật dữ liệu** → **Phản hồi**. Tác nhân **Guest** (trái trên) và **Player** (trái dưới) cùng kết nối vào use case đầu, thực tế chỉ Player mới nhận được nhiệm vụ từ NPC. Tác nhân **Admin** (phải) theo dõi ở bước "Phản hồi". "Kiểm tra điều kiện" gồm: xác nhận Player đủ cấp độ yêu cầu, nhiệm vụ chưa được nhận trước đó, đang đứng trong vùng tương tác NPC. "Cập nhật dữ liệu" tạo bản ghi `player_quests` (trạng thái IN_PROGRESS) khi nhận, hoặc đánh dấu COMPLETED và phát thưởng khi nộp nhiệm vụ.

**Đặc tả Use Case:**

| Trường | Nội dung |
|---|---|
| **Use Case** | Nhận và hoàn thành nhiệm vụ |
| **Actor** | Người chơi (Player) |
| **Mô tả** | Người chơi nhận nhiệm vụ từ NPC, thực hiện các mục tiêu trong game và nhận thưởng khi hoàn thành. Tiến trình nhiệm vụ được theo dõi và lưu tự động trên server. |
| **Tiền điều kiện** | 1. Người chơi đang ở gần NPC quest-giver (trong vùng tương tác). 2. Nhân vật đủ cấp độ yêu cầu của nhiệm vụ (nếu có). |
| **Luồng chính** | 1. Người chơi tiếp cận NPC và nhấn phím tương tác (F). 2. NPC hiển thị menu dialog; người chơi chọn nhiệm vụ cần nhận. 3. Client gửi POST /api/quests/accept với questId và characterId. 4. Server tạo bản ghi PlayerQuest với trạng thái IN_PROGRESS và progress ban đầu. 5. Người chơi thực hiện mục tiêu (giết quái, thu thập item, đến địa điểm). 6. Game Server tự động cập nhật progress khi điều kiện được thỏa mãn; đồng bộ về client. 7. Khi progress đạt 100%, server đánh dấu nhiệm vụ COMPLETED. 8. Người chơi quay lại NPC nộp nhiệm vụ; client gửi POST /api/quests/complete. 9. Server phát thưởng (EXP, Gold, vật phẩm) và cập nhật trạng thái trong database. |
| **Luồng phụ** | 2.1. Nhiệm vụ đã được nhận → NPC hiển thị tiến trình hiện tại thay vì cho nhận lại. 3.1. Nhân vật không đủ cấp độ → server trả về 403; NPC hiển thị "Cần đạt cấp X để nhận nhiệm vụ này". 8.1. Nhiệm vụ chưa hoàn thành (progress < 100%) → server từ chối; NPC nhắc nhở mục tiêu còn lại. |
| **Kết quả** | Nhiệm vụ hoàn thành; phần thưởng được cộng vào nhân vật; trạng thái COMPLETED lưu database. |

g) Use Case Nâng cấp trang bị

**Hình 2.9: Biểu đồ Use Case — Nâng cấp trang bị**

*Mô tả biểu đồ:* Vùng hệ thống chứa 4 use case theo chuỗi: **Nâng cấp trang bị** → **Kiểm tra điều kiện** → **Cập nhật dữ liệu** → **Phản hồi**. Tác nhân **Guest** (trái trên) và **Player** (trái dưới) kết nối vào use case đầu tiên; chỉ Player đứng gần NPC Blacksmith với đủ nguyên liệu mới thực hiện được. Tác nhân **Admin** (phải) kết nối vào "Phản hồi". "Kiểm tra điều kiện" xác nhận: trang bị chưa đạt +20, số lượng Enhancement Stone và Gold đủ theo mức nâng cấp. "Cập nhật dữ liệu" server tung xúc xắc theo xác suất thành công, cập nhật `enhancement_level` trong bảng `character_equipment`, tính lại chỉ số ATK/DEF của nhân vật.

**Đặc tả Use Case:**

| Trường | Nội dung |
|---|---|
| **Use Case** | Nâng cấp trang bị |
| **Actor** | Người chơi (Player) |
| **Mô tả** | Người chơi mang trang bị đến NPC Blacksmith để nâng cấp từ +0 đến +20 bằng nguyên liệu và Gold. Xác suất thành công giảm dần ở mức nâng cấp cao. |
| **Tiền điều kiện** | 1. Người chơi đang ở gần NPC Blacksmith. 2. Nhân vật có trang bị chưa đạt +20 trong inventory. 3. Có đủ nguyên liệu Enhancement Stone và Gold. |
| **Luồng chính** | 1. Người chơi tương tác NPC Blacksmith, chọn chức năng nâng cấp. 2. Giao diện Blacksmith hiển thị; người chơi kéo trang bị vào slot và xem xác suất thành công. 3. Người chơi nhấn "Nâng cấp". 4. Client gửi POST /api/equipment/enhance với itemId và characterId kèm JWT. 5. Server kiểm tra nguyên liệu và Gold; trừ nguyên liệu. 6. Server tính kết quả theo xác suất: Thành công → tăng enhancement_level +1; Thất bại → không thay đổi hoặc giảm cấp (tùy cấu hình mức nguy hiểm). 7. Server trả về kết quả; client hiển thị animation thành công/thất bại và cập nhật chỉ số trang bị. |
| **Luồng phụ** | 2.1. Không đủ nguyên liệu → nút "Nâng cấp" bị vô hiệu hóa; hiển thị thiếu bao nhiêu. 2.2. Trang bị đã đạt +20 → không hiển thị tùy chọn nâng cấp thêm. 6.1. Thất bại ở mức +15 trở lên → enhancement_level giảm 1; thông báo rõ ràng cho người chơi. |
| **Kết quả** | Trang bị được nâng cấp (hoặc thất bại); chỉ số equipment_level cập nhật trong database; chỉ số nhân vật được tính lại. |

h) Use Case Tạo và quản lý nhóm (Party)

**Hình 2.10: Biểu đồ Use Case — Tạo và quản lý nhóm**

*Mô tả biểu đồ:* Vùng hệ thống chứa 4 use case theo chuỗi: **Tạo và quản lý nhóm** → **Kiểm tra điều kiện** → **Cập nhật dữ liệu** → **Phản hồi**. Tác nhân **Guest** (trái trên) và **Player** (trái dưới) kết nối vào use case đầu — thực tế chỉ Player đã đăng nhập và chưa trong nhóm nào mới tạo được. Tác nhân **Admin** (phải) kết nối vào "Phản hồi". Điểm đặc biệt của use case này so với các use case REST khác: "Cập nhật dữ liệu" không đi qua HTTP mà qua **SignalR Hub** (`PartyHub`) — server đẩy `PartyStateUpdated` realtime cho tất cả thành viên trong SignalR group mỗi khi có thay đổi (mời/chấp nhận/rời/chuyển leader).

**Đặc tả Use Case:**

| Trường | Nội dung |
|---|---|
| **Use Case** | Tạo và quản lý nhóm |
| **Actor** | Người chơi (Player) |
| **Mô tả** | Người chơi tạo nhóm hoặc tham gia nhóm có sẵn để cùng chiến đấu và khám phá phó bản. Hệ thống nhóm vận hành real-time qua SignalR Hub. |
| **Tiền điều kiện** | 1. Người chơi đã đăng nhập và đang trong game. 2. Người chơi chưa thuộc nhóm nào. |
| **Luồng chính — Tạo nhóm** | 1. Người chơi mở UI Party và nhấn "Tạo nhóm". 2. Client gửi yêu cầu tạo nhóm qua SignalR Hub với JWT xác thực. 3. Server tạo Party mới, gán leaderId là người chơi hiện tại. 4. Server gửi xác nhận tạo nhóm thành công về client qua SignalR. 5. Người chơi nhập tên nhân vật cần mời và nhấn "Mời". 6. Server kiểm tra người chơi đang online; gửi lời mời đến người được mời qua SignalR. 7. Người được mời chấp nhận; server cập nhật danh sách thành viên và push cho tất cả. |
| **Luồng phụ** | 5.1. Tên nhân vật không tồn tại hoặc offline → server thông báo "Không tìm thấy người chơi". 7.1. Người được mời từ chối → server thông báo cho người mời; không thay đổi nhóm. 5.2. Nhóm đã đủ 4 người → nút mời bị vô hiệu hóa; hiển thị "Nhóm đã đầy". 1.1. Người chơi đang trong nhóm khác → hệ thống yêu cầu rời nhóm cũ trước. |
| **Kết quả** | Nhóm được tạo; thành viên cùng thấy danh sách nhóm real-time; có thể cùng vào phó bản. |

i) Use Case Vào phó bản (Dungeon)

**Hình 2.11: Biểu đồ Use Case — Vào phó bản**

*Mô tả biểu đồ:* Đây là use case phức tạp nhất — chuỗi dài 6 bước bên trong vùng hệ thống, phản ánh toàn bộ vòng đời một phiên Dungeon: **Tạo party** → **Mời thành viên** → **Vào Dungeon** → **Đánh wave** → **Đánh Boss** → **Nhận thưởng**. Tác nhân **Guest** (trái trên) và **Player** (trái dưới) kết nối vào use case đầu tiên "Tạo party" — điểm khởi phát toàn bộ luồng. Tác nhân **Admin** (phải) kết nối vào use case cuối "Nhận thưởng" để giám sát log loot distribution. Lưu ý cấu trúc luồng: bước 1–2 ("Tạo party" và "Mời thành viên") đi qua **SignalR PartyHub**; bước 3–6 (từ "Vào Dungeon" đến "Nhận thưởng") chuyển sang **Unity NGO Dedicated Server** quản lý `DungeonInstance` — đây là điểm chuyển giao giữa hai kênh giao tiếp đã trình bày tại §3.0.1. Boss xuất hiện sau khi tất cả wave thường đã bị tiêu diệt; loot chia theo damage contribution như mô tả tại §3.7.4.

**Đặc tả Use Case:**

| Trường | Nội dung |
|---|---|
| **Use Case** | Vào phó bản |
| **Actor** | Người chơi (Player) |
| **Mô tả** | Nhóm người chơi vào phó bản — khu vực đặc biệt với hệ thống wave quái liên tiếp kết thúc bằng Boss, mang lại phần thưởng cao hơn bản đồ thường. |
| **Tiền điều kiện** | 1. Người chơi đang ở trong nhóm (tối thiểu 1 người). 2. Người chơi đứng tại cổng vào phó bản. 3. Nhóm chưa đang trong phó bản khác. |
| **Luồng chính** | 1. Trưởng nhóm tương tác cổng phó bản; hệ thống hiển thị thông tin (cấp yêu cầu, số wave, boss). 2. Trưởng nhóm xác nhận; client gửi DungeonEnterRequest qua SignalR. 3. Server tạo DungeonInstance riêng trong ZoneRoomRegistry cho nhóm. 4. Server dịch chuyển tất cả thành viên đến Scene phó bản. 5. Wave 1 bắt đầu: server spawn quái theo cấu hình wave. 6. Nhóm tiêu diệt toàn bộ quái trong wave → server kích hoạt wave tiếp theo tự động. 7. Sau wave cuối: Boss xuất hiện với Phase System đã cấu hình. 8. Nhóm tiêu diệt Boss; server tính thưởng, phát EXP và vật phẩm đặc biệt. 9. Server dịch chuyển nhóm về bản đồ chính; xóa DungeonInstance. |
| **Luồng phụ** | 1.1. Nhóm không đủ cấp độ yêu cầu → hiển thị cảnh báo; không cho vào. 5.1. Thành viên chết → hồi sinh tại điểm spawn phó bản sau 10 giây; tiếp tục chiến đấu. 8.1. Toàn bộ nhóm chết trước khi Boss chết → DungeonInstance hủy; nhóm trở về bản đồ chính không có thưởng. |
| **Kết quả** | Phó bản hoàn thành; nhóm nhận thưởng EXP và vật phẩm đặc biệt; DungeonInstance được giải phóng. |

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

Chương 2 đã trình bày đầy đủ quá trình phân tích và thiết kế hệ thống game Mutants Arena từ góc nhìn kỹ thuật phần mềm. Qua phân tích bài toán, hệ thống được xác định phục vụ ba nhóm tác nhân chính (Guest, Player, Admin) với hai mươi chức năng nghiệp vụ được nhóm theo sáu lĩnh vực: quản lý tài khoản, quản lý nhân vật, chiến đấu và Gene, nhiệm vụ, trang bị và cộng đồng (nhóm, phó bản). Các yêu cầu phi chức năng (hiệu năng, độ tin cậy, bảo mật) và yêu cầu bảo mật (JWT, BCrypt, Server Authoritative) được định nghĩa rõ ràng làm tiêu chí kiểm thử và nghiệm thu cuối kỳ.

Về thiết kế, kiến trúc phân ba tầng (Unity Client — Game Server/API Server — MySQL) cung cấp sự tách biệt rõ ràng giữa tầng trình bày, tầng nghiệp vụ và tầng dữ liệu. Mô hình Server Authoritative đảm bảo tính toàn vẹn dữ liệu game và ngăn chặn gian lận phía client một cách có hệ thống. Chín use case chi tiết được đặc tả với đầy đủ luồng chính, luồng phụ và điều kiện tiên quyết, tạo nền tảng rõ ràng cho việc kiểm thử chức năng. Mười bốn bảng database được thiết kế theo hướng thực thể-quan hệ với JSON column cho dữ liệu cấu trúc linh hoạt, cân bằng giữa tính chuẩn hóa quan hệ và tính linh hoạt cần thiết cho nội dung game thay đổi thường xuyên.

Toàn bộ nội dung phân tích và thiết kế trong chương này là cơ sở trực tiếp để triển khai lập trình trong Chương 3 — xây dựng từng hệ thống game cụ thể theo bản thiết kế đã được xây dựng ở đây.
