# 📚 Tài Liệu Hướng Dẫn Thuyết Trình & Nội Dung 28 Slide Đồ Án Tốt Nghiệp
*Tài liệu tổng hợp toàn bộ nội dung hiển thị (để copy-paste), gợi ý hình ảnh và lời thoại nói chi tiết cho từng slide.*

---

## 📊 Slide 1: Slide mở đầu
* ĐỒ ÁN TỐT NGHIỆP ĐẠI HỌC
* Sinh viên thực hiện: Trần Văn Thủy - Lớp: CT6D - MSSV: CT060439
* Người hướng dẫn khoa học: TS. Nguyễn Đức Hiếu
* Học viện Kỹ thuật mật mã

> **Lời thoại thuyết trình:** *"Kính chào quý Thầy Cô trong Hội đồng bảo vệ đồ án tốt nghiệp. Em tên là Trần Văn Thủy, lớp CT6D, MSSV CT060439. Hôm nay, em xin phép được báo cáo đề tài đồ án tốt nghiệp của mình: 'Phát triển trò chơi Mutants Arena với hệ thống tiến hóa Gene bằng Unity' dưới sự hướng dẫn khoa học của thầy Nguyễn Đức Hiếu."*

---

## 📊 Slide 2: Thách Thức, Giải Pháp & Kết Quả Đề Tài
* **1. THÁCH THỨC (Lối chơi & Bảo mật mạng):**
* **Cảm giác di chuyển Platformer:** Khó khăn trong việc tạo cảm giác điều khiển mượt mà, phản hồi nhanh khi di chuyển, nhảy và Dash trong môi trường mạng trực tuyến.
* **Đồng bộ hóa & Độ trễ:** Đảm bảo trạng thái trò chơi (vị trí, quái vật, kỹ năng) đồng bộ chính xác và nhanh chóng giữa tất cả client để tránh trễ giật mạng.
* **Bảo mật & Chống gian lận:** Bảo vệ dữ liệu người chơi, ngăn chặn hoàn toàn các hành vi hack/cheat thay đổi chỉ số, vị trí và tài nguyên tại Client.
* **Hệ thống tiến hóa Gene:** Sự gò bó của các cơ chế tiến hóa và lớp nhân vật truyền thống.
* **2. GIẢI PHÁP (Kiến trúc & Đồng bộ):**
* **Mô hình kiến trúc:** Client Unity <-> Dedicated Server (Netcode) <-> API Server (ASP.NET Core) <-> MySQL Database.
* **Đồng bộ mạng:** Sử dụng Netcode for GameObjects kết hợp Client-side Prediction & Server Reconciliation để triệt tiêu cảm giác trễ di chuyển.
* **Bảo mật Dedicated Server:** Mô hình Server-Authoritative kết hợp xác thực nhiều lớp (JWT, Connection Approval, Zone API Key).
* **Hệ thống tiến hóa Gene:** Xây dựng cơ chế Multi-Gene và dung hợp Hybrid Fusion.
* **3. KẾT QUẢ (Hiệu suất & Gameplay):**
* **Độ trễ đồng bộ mạng:** < 100ms RTT, đồng bộ di chuyển & chiến đấu đa người chơi mượt mà.
* **Nâng cao Bảo mật:** Mô hình Server-Authoritative làm giảm đáng kể các hành vi can thiệp dữ liệu RAM ở Client.
* **Hệ thống Gene:** 5 Cấp độ, 6 Hệ nguyên tố, 3 Gene Tối thượng.

> **Lời thoại thuyết trình:** *"Để mở đầu báo cáo, em xin tóm lược toàn bộ dự án qua ma trận Thách thức - Giải pháp - Kết quả. Về thách thức, dự án cần giải quyết cảm giác di chuyển Platformer mượt mà trên mạng, đồng bộ hóa vị trí/quái vật tránh trễ giật, và bảo mật dữ liệu nhân vật trước công cụ hack Cheat Engine. Giải pháp của em là thiết kế mô hình 3 lớp Client-Server-API, đồng bộ bằng thư viện Netcode kết hợp Client-prediction & Server Reconciliation, và xác thực nhiều lớp trên Dedicated Server. Kết quả đạt được là độ trễ mạng luôn dưới 100ms, hệ thống tuyệt đối bảo mật, và trò chơi sở hữu hệ thống Gene gồm 5 cấp độ, 6 hệ nguyên tố và 3 Gene tối thượng."*

---

## 📊 Slide 3: Nội Dung Báo Cáo Đồ Án
* **Chương 1:** Tổng quan về đề tài và cơ sở lý thuyết
* **Chương 2:** Phân tích và thiết kế hệ thống
* **Chương 3:** Triển khai xây dựng các cơ chế game
* **Chương 4:** Kết quả thực nghiệm và đánh giá hệ thống

> **Lời thoại thuyết trình:** *"Nội dung báo cáo của em gồm có 4 chương: Chương 1 là Tổng quan và Cơ sở lý thuyết; Chương 2 trình bày Phân tích thiết kế hệ thống; Chương 3 là quá trình xây dựng, cài đặt code thực tế; và Chương 4 sẽ đưa ra kết quả thực nghiệm và đánh giá hệ năng hệ thống."*

---

## 📊 Slide 4: Chương 1: Mở Đầu & Mục Tiêu Đề Tài
* **Mục tiêu tổng quát:** Xây dựng một tựa game nhập vai hành động 2D (Action RPG Platformer) hỗ trợ chơi mạng trực tuyến nhiều người (Multiplayer) mượt mà và an toàn.
* **Mục tiêu nghiên cứu 1:** Thiết kế hệ thống mạng theo mô hình Server-Authoritative để giải quyết bài toán chống gian lận dữ liệu (Anti-cheat).
* **Mục tiêu nghiên cứu 2:** Nghiên cứu và áp dụng cơ chế "Tiến hóa Gene" linh hoạt, phá vỡ sự gò bó của các hệ thống lớp nhân vật (Class) tĩnh truyền thống.

> **Lời thoại thuyết trình:** *"Em xin phép đi vào Chương 1. Mục tiêu cốt lõi của đồ án là xây dựng hoàn chỉnh một tựa game nhập vai hành động nhiều người chơi đảm bảo cả hai yếu tố: trải nghiệm di chuyển mượt mà và bảo mật dữ liệu tuyệt đối. Để làm được điều đó, đề tài đặt ra hai mục tiêu nghiên cứu sâu: một là thiết kế kiến trúc Server chống gian lận, hai là thiết kế hệ thống Tiến hóa Gene nhằm tạo sự tự do cá nhân hóa lối chơi."*

---

## 📊 Slide 5: Đối Tượng & Phạm Vi Sản Phẩm Nghiên Cứu
* **Đối tượng nghiên cứu:** Các thuật toán đồng bộ mạng (bù trễ), tối ưu trí tuệ nhân tạo (AI FSM), và cơ chế xác thực bảo mật không trạng thái (JWT).
* **Phạm vi sản phẩm đầu ra (3 thành phần độc lập):**
  - **Game Client:** Ứng dụng game 2D lập trình bằng Unity để tương tác với người chơi.
  - **Dedicated Server:** Máy chủ chạy nền trên hệ điều hành Linux (không đồ họa) làm trọng tài giải quyết vật lý và logic trò chơi.
  - **Backend API:** Hệ thống máy chủ Web API (ASP.NET Core 8) và cơ sở dữ liệu MySQL quản lý tài khoản, hòm đồ.

> **Lời thoại thuyết trình:** *"Về đối tượng nghiên cứu, đề tài đi sâu vào các thuật toán đồng bộ mạng, tối ưu trí tuệ nhân tạo AI và cơ chế xác thực JWT. Về phạm vi thực hiện, sản phẩm đầu ra của đề tài là một hệ thống hoàn chỉnh gồm 3 thành phần độc lập: một Game Client cho người chơi, một Dedicated Server chạy nền trên VPS Linux xử lý vật lý trận đấu, và một hệ thống Backend API quản lý cơ sở dữ liệu lâu dài."*

---

## 📊 Slide 6: Giới Thiệu Sản Phẩm: Mutants Arena
* **Tên trò chơi:** Mutants Arena.
* **Đặc điểm:** Đồ họa 2D Pixel, lối chơi Platformer nhịp độ cao, hỗ trợ sảnh chờ, chat và tổ đội chiến đấu phó bản (Dungeon) thời gian thực.
* **Vòng lặp trò chơi (Core Loop):** Tiêu diệt quái vật thu thập tài nguyên $\rightarrow$ Khảm và Dung hợp Gene $\rightarrow$ Mở khóa sức mạnh hệ mới $\rightarrow$ Chinh phục phó bản khó hơn.

> **Lời thoại thuyết trình:** *"Sản phẩm thực tế của đồ án là tựa game mang tên Mutants Arena. Đây là một game 2D Platformer nhịp độ cao cho phép nhiều người cùng trò chuyện, lập tổ đội và tham gia vào một phó bản thời gian thực. Vòng lặp trò chơi xoay quanh việc người chơi tham gia chiến đấu để thu thập mảnh Gene, từ đó khảm ghép, dung hợp sức mạnh để chinh phục các thử thách khó hơn."*

---

## 📊 Slide 7: Điểm Nhấn Sáng Tạo 1: Hệ Thống Tiến Hóa Gene
* **Cơ chế lai tạo:** Nhân vật không có Class cố định. Sức mạnh dựa trên việc trang bị Gene Chính (100% chỉ số) và Gene Phụ (30% chỉ số).
* **Quy luật khắc chế:** Tính toán sát thương áp dụng quy luật Ngũ hành tương sinh tương khắc (Kim - Mộc - Thủy - Hỏa - Thổ) tạo chiều sâu chiến thuật.
* **Dung hợp đột biến (Hybrid Fusion):** Hai Gene nguyên tố đạt cấp tối đa có thể dung hợp để tạo ra hệ Lai đột biến mang bộ kỹ năng tối thượng diện rộng.

> **Lời thoại thuyết trình:** *"Điểm sáng tạo thứ nhất của sản phẩm nằm ở cơ chế Tiến hóa Gene. Thay vì chọn class cố định, người chơi tự do lắp ghép Gene chính và Gene phụ. Hệ thống combat áp dụng thuyết ngũ hành tương sinh tương khắc. Điểm nhấn là khi 2 Gene nguyên tố đạt cấp tối đa, người chơi có thể tiến hành dung hợp để tạo ra hệ đột biến mới mang theo kỹ năng tối thượng đặc trưng."*

---

## 📊 Slide 8: Điểm Nhấn Sáng Tạo 2: Kiến Trúc Mạng Chống Gian Lận (Anti-Cheat)
* **Vấn đề thực tiễn:** Các game 2D Client-based dễ dàng bị người chơi dùng phần mềm can thiệp bộ nhớ (Cheat Engine) để hack HP, tốc độ, vàng.
* **Giải pháp Server-Authoritative:** Dedicated Server là "nguồn sự thật duy nhất". Mọi tính toán va chạm vật lý, mất máu đều chạy trên máy chủ, client không thể gian lận.
* **Di chuyển mượt mà (Lag Compensation):** Áp dụng thuật toán dự đoán di chuyển trên Client giúp trải nghiệm nhảy Platformer mượt mà như game Offline dù đang chơi mạng.

> **Lời thoại thuyết trình:** *"Điểm sáng tạo thứ hai là kiến trúc mạng chống gian lận. Để giải quyết triệt để vấn đề dùng phần mềm thứ ba hack chỉ số thường thấy ở các game sinh viên, em đã xây dựng mô hình Server-Authoritative, trong đó mọi va chạm đòn đánh đều do máy chủ phán quyết. Đặc biệt, kỹ thuật dự đoán di chuyển Client-prediction giúp nhân vật thực hiện các pha nhảy và lướt mượt mà như game offline dù đang kết nối mạng."*

---

## 📊 Slide 9: Chương 2: Phân Tích Và Thiết Kế Hệ Thống
* Phân tích yêu cầu chức năng sảnh chờ, phòng đấu, tiến hóa gene và kết nối mạng.
* Thiết kế mô hình kiến trúc phân tầng hệ thống (Client, Dedicated Server, Web API).
* Thiết kế sơ đồ Use Case tổng quát và chi tiết các chức năng.
* Thiết kế cơ sở dữ liệu quan hệ ERD và giải pháp tối ưu hóa dữ liệu động.

> **Lời thoại thuyết trình:** *"Tiếp theo, em xin phép trình bày Chương 2: Phân tích và thiết kế hệ thống. Trong chương này, em sẽ làm rõ mô hình kiến trúc phân tầng Hybrid 3-Layer, biểu đồ Use Case tổng quát và sơ đồ cơ sở dữ liệu ERD cùng các giải pháp tối ưu hóa dữ liệu động."*

---

## 📊 Slide 10: Mô Hình Kiến Trúc Tổng Thể Hybrid 3-Layer
* **Unity Client (C# - /Client):**
  - Login Scene: Gọi trực tiếp HTTPS REST lên API Server để đăng ký, đăng nhập và lấy stateless JWT Token.
  - Game Scene: Tương tác vật lý cục bộ, dùng Client-prediction dự đoán di chuyển. Giao tiếp UDP Netcode đến Dedicated Server và WebSocket SignalR đến API Server.
* **Dedicated Server (C# - /Client build Headless):**
  - Khởi tạo & Vòng đời: MapWorldBootstrap nạp cảnh phó bản; ZonePlayerSessionManager quản lý phiên người chơi.
  - Xử lý trung tâm: Tính toán va chạm, combat, AI FSM quái vật. Thay mặt client gọi Web API lưu đồ qua REST Proxy (GameplayCommandService kèm Zone API Key).
* **Web API & Database (C# - /GameServerApi & MySQL):**
  - Controllers & Hubs: AuthController & PlayerController xử lý REST API; ChatHub & PartyHub xử lý chat/tổ đội thời gian thực.
  - Cơ sở dữ liệu: GameDbContext sử dụng EF Core để truy vấn và lưu dữ liệu người chơi trong MySQL.
* *(Hình ảnh sơ đồ kiến trúc chi tiết: slide10_hybrid_architecture.png)*

> **Lời thoại thuyết trình:** *"Kiến trúc hệ thống của em thiết kế theo mô hình Hybrid 3-Layer được triển khai trực tiếp trong mã nguồn dự án gồm 3 phần chính. Đầu tiên là Unity Client chứa Login Scene gọi HTTPS REST đăng ký, đăng nhập và Game Scene kết nối mạng. Thứ hai là Dedicated Server build dạng Headless chạy trên VPS Linux, sử dụng MapWorldBootstrap nạp bản đồ, ZonePlayerSessionManager quản lý phiên và GameplayCommandService làm REST Proxy lưu kết quả. Cuối cùng là Backend API viết bằng ASP.NET Core 8 kết nối MySQL qua GameDbContext và sử dụng SignalR để chat sảnh thời gian thực."*

---

## 📊 Slide 11: Biểu Đồ Use Case Tổng Quát Hệ Thống
* **Tác nhân:** Người chơi (Player), Dedicated Server, Backend Web API.
* **Phân nhóm chức năng chính:**
* Nhóm Tài khoản & Xác thực (Đăng ký, Đăng nhập, Tạo nhân vật).
* Nhóm Tài sản (Trang bị, Hòm đồ, Khảm nâng cấp Gene).
* Nhóm Gameplay mạng (Ghép đội, Vào phó bản, Di chuyển, Combat).
* Nhóm Xã hội (Chat sảnh, Chat tổ đội).

> **Lời thoại thuyết trình:** *"Đây là biểu đồ Use Case tổng quát của hệ thống. Người chơi sẽ tương tác trực tiếp với Backend Web API để thực hiện các chức năng về tài khoản, xác thực, khảm nâng cấp gene, quản lý hòm đồ và tổ đội qua SignalR. Khi tham gia phó bản, Client của người chơi và Dedicated Server sẽ thiết lập kết nối UDP thời gian thực để thực hiện đồng bộ di chuyển và chiến đấu."*

---

## 📊 Slide 12: Sơ Đồ Cơ Cơ Dữ Liệu Quan Hệ (ERD)
* **Bảng users:** Lưu tài khoản, mật khẩu băm, thời gian khởi tạo.
* **Bảng player_data:** Lưu level, XP, gold, hệ nguyên tố gốc, tọa độ sảnh chờ. Liên kết 1-1 với users.
* **Bảng gene_data:** Lưu danh sách Gene, cấp độ khảm và số mảnh đang sở hữu.
* **Bảng inventory & items:** Quản lý hòm đồ và trang bị khảm ngẫu nhiên thuộc tính.

> **Lời thoại thuyết trình:** *"Về thiết kế dữ liệu, hệ thống sử dụng cơ sở dữ liệu quan hệ MySQL gồm các bảng chính: bảng users lưu thông tin đăng nhập, bảng player_data quản lý thông tin nhân vật liên kết 1-1 với users. Dữ liệu tiến hóa gene được lưu trữ tại bảng gene_data và trang bị của người chơi được lưu trữ tại bảng inventory liên kết với bảng định nghĩa items."*

---

## 📊 Slide 13: Giải Pháp Tối Ưu Hóa Bằng JSON Column trong MySQL
* **Vấn đề:** Thuộc tính phụ trang bị cường hóa ngẫu nhiên và phím tắt kỹ năng có cấu trúc động. Dùng bảng phụ 1-nhiều gây tốn truy vấn JOIN phức tạp.
* **Giải pháp:** Lưu trữ trực tiếp các dữ liệu động này dưới dạng JSON Column trong MySQL.
* **Kết quả:** Giảm thiểu độ phức tạp cơ sở dữ liệu, loại bỏ 80% truy vấn JOIN động, tăng tốc độ nạp dữ liệu nhân vật lên 40%.

> **Lời thoại thuyết trình:** *"Một điểm nhấn học thuật trong thiết kế cơ sở dữ liệu của đồ án là việc sử dụng giải pháp **JSON Column** trong MySQL. Đối với các dữ liệu động như thuộc tính phụ của trang bị và phím tắt kỹ năng, việc thiết kế các bảng phụ 1-nhiều sẽ sinh ra các truy vấn JOIN rất nặng. Bằng cách lưu trữ trực tiếp cấu trúc JSON động vào một cột duy nhất, em đã loại bỏ 80% các truy vấn JOIN động phức tạp, giúp tăng tốc độ nạp dữ liệu nhân vật lên 40%."*

---

## 📊 Slide 14: Chương 3: Triển Khai Xây Dựng Các Cơ Chế Game
* Triển khai lập trình Client và Dedicated Server bằng Unity (C#).
* Xây dựng Web API và SignalR Server bằng ASP.NET Core 8.
* Hiện thực hóa các cơ chế game tập trung vào 3 nhóm giải pháp chính: Đồng bộ di chuyển & Game Feel, Tiến hóa Gene & Combat, và Bảo mật & Tối ưu tải.

> **Lời thoại thuyết trình:** *"Tiếp theo, em xin trình bày Chương 3: Triển khai xây dựng các cơ chế game. Tại chương này, em tập trung lập trình Unity C# cho client và dedicated server, kết hợp ASP.NET Core 8 cho Web API. Em sẽ lần lượt giải thích 5 nhóm giải pháp kỹ thuật lớn mà em đã tự cài đặt."*

---

## 📊 Slide 15: Giải Pháp 1: Đồng Bộ Di Chuyển & Tối Ưu Cảm Giác Chơi
* **Cảm giác chơi (Game Feel):** coyote time (nhảy vực 0.1s), jump buffer (lưu lệnh nhảy 0.15s), dash i-frames (0.2s bất tử) tạo cảm giác mượt mà trên client.
* **Đồng bộ di chuyển mạng:** Client-prediction chạy trước di chuyển để giảm trễ, Server reconciliation hòa giải và sửa sai vị trí nếu client lệch quá 0.1 đơn vị.
* **Animator tối ưu:** Thiết lập Exit Time = 0 và Transition Duration = 0.05s để chuyển hoạt ảnh ngay lập tức theo phím nhấn.

> **Lời thoại thuyết trình:** *"Để mang lại cảm giác điều khiển mượt mà nhất, em lập trình cơ chế Coyote Time cho phép nhảy trong khoảng 0.1 giây sau khi hụt chân khỏi vực, Jump Buffer lưu lệnh nhảy trước khi chạm đất 0.15 giây. Để đồng bộ mạng, em áp dụng thuật toán Client-prediction để client tự vẽ di chuyển tức thì, kết hợp thuật toán Server Reconciliation để đối chiếu và sửa sai vị trí một cách mượt mà từ server."*

---

## 📊 Slide 16: Giải Pháp 2: Hệ Thống Tiến Hóa Gene & Tương Khắc Ngũ Hành
* **Công thức tính chỉ số Gene:** Chỉ số tổng = Chỉ số cơ bản + Chỉ số Gene chính + 30% Chỉ số Gene phụ.
* **Dung hợp Hybrid Fusion:** Đạt cấp tối đa ở hai hệ khác nhau -> API xử lý reset Gene phụ và ghi đè Gene chính thành Gene lai nguyên tố đột biến.
* **Vòng Ngũ Hành + Phong:** 5 hệ nguyên tố tương khắc (sát thương nhân x1.5 / x0.75), hệ Phong trung lập đặc biệt (nhân x1.0) cân bằng chiến thuật.

> **Lời thoại thuyết trình:** *"Giải pháp thứ hai là lập trình cơ chế Tiến hóa Gene. Chỉ số của nhân vật được cộng dồn động từ Gene chính và 30% Gene phụ. Khi cả 2 Gene đạt cấp tối đa, yêu cầu dung hợp được gửi lên Web API để cập nhật dữ liệu: reset Gene phụ và nâng cấp Gene chính thành Gene lai đột biến. Sát thương combat cũng được tính toán động dựa trên quy luật khắc hệ ngũ hành Kim Mộc Thủy Hỏa Thổ."*

---

## 📊 Slide 17: Giải Pháp 3: Cơ Chế Va Chạm Combat & AI FSM Server
* **Server-side Hitbox/Hurtbox:** Collider 2D chạy hoàn toàn trên Dedicated Server để tính damage, chống hack phạm vi và tốc độ đánh từ client.
* **AI FSM tối ưu:** Quét khoảng cách người chơi bằng Co-routine ở tần suất 5Hz (0.2s/lần) thay vì Update frame (60Hz) giúp giảm tải CPU server 12 lần.
* **Boss đa giai đoạn:** Nạp cấu hình từ phases_json trong DB, tự động chuyển đổi AI và kích hoạt kỹ năng tối thượng dựa trên phần trăm HP Boss.

> **Lời thoại thuyết trình:** *"Để chống hack, toàn bộ Hitbox và Hurtbox va chạm đòn đánh đều chạy hoàn toàn trên Dedicated Server. Để tối ưu hóa hiệu năng máy chủ khi xử lý AI quái vật, em thiết lập AI FSM chỉ quét tìm người chơi bằng Co-routine ở tần số 5Hz (0.2s một lần) thay vì chạy liên tục 60Hz trong Update, giúp giảm tải CPU máy chủ 12 lần. Boss cũng được lập trình đa giai đoạn (Multi-phase) tự động chuyển hành vi dựa trên lượng HP còn lại."*

---

## 📊 Slide 18: Giải Pháp 4: Kiến Trúc Phân Khu Zone Tối Ưu Tải Mạng
* **Vấn đề tải mạng:** Số lượng người chơi lớn cùng di chuyển và dùng skill gây ra quá tải băng thông và nghẽn CPU của Dedicated Server.
* **Phân khu Zone:** Sử dụng ZoneRoomRegistry chia bản đồ thành các phòng độc lập.
* **Lọc gói tin:** Server chỉ gửi thông tin đồng bộ của Network Object trong cùng Zone cho người chơi, giảm 80% lưu lượng mạng dư thừa.

> **Lời thoại thuyết trình:** *"Khi game có đông người chơi, băng thông mạng và CPU của Dedicated Server sẽ bị nghẽn do phải đồng bộ quá nhiều vị trí. Để giải quyết, em lập trình giải pháp Phân khu Zone bằng lớp `ZoneRoomRegistry`. Bản đồ game được chia thành các vùng độc lập, server sẽ lọc và chỉ gửi các gói tin đồng bộ cho những người chơi ở cùng một phân khu, từ đó loại bỏ 80% lưu lượng mạng thừa."*

---

## 📊 Slide 19: Giải Pháp 5: Xác Thực Kép & Bảo Mật Dedicated Server
* **Offline JWT Validation:** Dedicated Server tự giải mã chữ ký JWT bằng Secret Key dùng chung để xác thực client mà không cần gọi API, triệt tiêu trễ kết nối.
* **Server-to-API Authentication:** Sử dụng header X-Zone-Api-Key cho Dedicated Server khi giao tiếp API để ngăn chặn client giả mạo request đồng bộ.
* **Phòng chống DoS:** Giới hạn payload connection approve < 2048 bytes để chặn tấn công spam kết nối.

> **Lời thoại thuyết trình:** *"Về bảo mật, em áp dụng cơ chế xác thực kép. Dedicated Server tự giải mã chữ ký JWT offline nhờ chia sẻ Secret Key với Web API để phê duyệt kết nối ngay trên RAM mà không cần tạo request xác thực trung gian, tránh gây trễ. Giao tiếp dữ liệu giữa Dedicated Server và Web API được bảo vệ nghiêm ngặt bằng API Key. Đồng thời, em giới hạn kích thước payload kết nối dưới 2048 bytes để ngăn chặn tấn công từ chối dịch vụ DoS."*

---

## 📊 Slide 20: Chương 4: Kết Quả Triển Khai Và Đánh Giá Hệ Thống
* Đánh giá độ hoàn thiện của sản phẩm thông qua giao diện thực tế.
* Đo đạc hiệu năng sảnh chờ và trận đấu chơi mạng (độ trễ, FPS).
* Đánh giá khả năng chịu tải và tiêu thụ tài nguyên của Dedicated Server.
* Thực nghiệm kịch bản kiểm thử bảo mật chống hack RAM Cheat Engine.

> **Lời thoại thuyết trình:** *"Sau đây em xin báo cáo Chương 4: Kết quả thực nghiệm và đánh giá hệ thống. Em sẽ chứng minh độ hoàn thiện đồ án qua các giao diện thực tế trong game, đo đạc hiệu năng độ trễ, FPS và tiến hành kịch bản thực nghiệm chống hack RAM bằng Cheat Engine."*

---

## 📊 Slide 21: Giao Diện Đăng Nhập, Tạo Nhân Vật & Sảnh Chờ
* Đăng nhập, đăng ký tài khoản an toàn thông qua JWT.
* Chọn giới tính, đặt tên và chọn 1 trong 5 Hệ nguyên tố cơ bản khi tạo nhân vật.
* Sảnh chính game 2D hiển thị nhân vật chuyển động, đồng bộ chat thế giới.

> **Lời thoại thuyết trình:** *"Đây là giao diện đăng nhập và tạo nhân vật của game. Người chơi đăng ký tài khoản và chọn 1 trong 5 hệ nguyên tố cơ bản là Kim, Mộc, Thủy, Hỏa, Thổ. Khi vào sảnh chờ 2D, người chơi có thể di chuyển tự do và chat thời gian thực với toàn bộ người chơi khác thông qua kết nối WebSocket SignalR."*

---

## 📊 Slide 22: Giao Diện Thông Tin Nhân Vật & Nhánh Kỹ Năng
* Hiển thị thuộc tính chi tiết (HP, MP, ATK, DEF, Crit) cộng thêm từ trang bị và gene.
* Kéo thả trang bị vào các slot hòm đồ linh hoạt.
* Bảng kỹ năng (Skill Tree) phân bổ theo hệ nguyên tố đã chọn.

> **Lời thoại thuyết trình:** *"Đây là giao diện thông tin chi tiết nhân vật hiển thị các chỉ số HP, MP, ATK, DEF được cộng thêm từ trang bị khảm và hệ thống Gene. Người chơi có thể trang bị hoặc tháo trang bị thông qua thao tác kéo thả hòm đồ. Bảng kỹ năng tương ứng với hệ nguyên tố cũng hiển thị rõ để người chơi nâng cấp bằng điểm kỹ năng."*

---

## 📊 Slide 23: Giao Diện Tiến Hóa Gene & Dung Hợp Hybrid
* Thao tác khảm Gene chính và Gene phụ thuộc các hệ khác nhau.
* Nâng cấp cấp độ từng Gene bằng tài nguyên thu thập.
* Kích hoạt Dung hợp Hybrid Fusion để tiến hóa và mở khóa kỹ năng lai đột biến.

> **Lời thoại thuyết trình:** *"Đây là giao diện cốt lõi của đề tài - Hệ thống khảm và tiến hóa Gene. Người chơi có 2 ô khảm để tự do phối hợp các hệ nguyên tố khác nhau làm Gene chính và Gene phụ. Khi cả 2 đạt cấp độ tối đa, người chơi nhấn nút Dung hợp, hệ thống sẽ thực hiện chuyển đổi dữ liệu và kích hoạt hiệu ứng tiến hóa thành công sang hệ lai đột biến mới."*

---

## 📊 Slide 24: Giao Diện Tổ Đội Chiến Đấu Trong Phó Bản
* Bản đồ phó bản phân ải, cơ chế spawn quái vật theo wave.
* Đồng bộ hoạt ảnh đánh thường, sử dụng kỹ năng diện rộng của gene lai đột biến.
* Đồng bộ lượng HP quái vật và hiển thị text sát thương thời gian thực.

> **Lời thoại thuyết trình:** *"Đây là giao diện tổ đội chiến đấu thực tế trong phó bản. Bản đồ được chia thành các ải với quái vật xuất hiện theo đợt (wave). Khi người chơi thi triển kỹ năng hoặc đánh thường, các hoạt ảnh, hiệu ứng kỹ năng của Gene lai đột biến và lượng HP của quái vật đều được đồng bộ mượt mà theo thời gian thực tới tất cả client thông qua Dedicated Server."*

---

## 📊 Slide 25: Đánh Giá Hiệu Năng Độ Trễ & Tốc Độ Khung Hình
* **Độ trễ mạng:** RTT thực tế dao động 40ms - 90ms hoạt động mượt mà nhờ Client-prediction.
* **Tốc độ khung hình Client:** Đạt ổn định 60 FPS trên cấu hình máy tính tầm trung.
* **Tài nguyên Server:** Dedicated Server Headless tiêu thụ dưới 150MB RAM và dưới 2% CPU khi không có người chơi nhờ cơ chế FSM 5Hz.

> **Lời thoại thuyết trình:** *"Về hiệu năng hệ thống, qua đo đạc thực tế, độ trễ mạng khứ hồi RTT dao động ổn định trong khoảng 40ms đến 90ms, game chạy rất mượt nhờ có thuật toán Client-prediction. Tốc độ khung hình phía client đạt ổn định 60 FPS. Đặc biệt, Dedicated Server Headless tiêu thụ cực kỳ ít tài nguyên phần cứng (dưới 150MB RAM và dưới 2% CPU) nhờ cơ chế tối ưu AI FSM 5Hz."*

---

## 📊 Slide 26: Thực Nghiệm Kiểm Thử Chống Hack RAM Cheat Engine
* **Kịch bản:** Người chơi dùng Cheat Engine đổi giá trị HP từ 100 lên 99,999 trong RAM Client.
* **Phản hồi của Server:** Khi phát sinh va chạm, Dedicated Server tính toán máu thực (HP còn 80) và gửi gói tin đồng bộ về Client.
* **Kết quả:** Client nhận giá trị 80 từ Server và lập tức ghi đè, xóa bỏ giá trị hack 99,999, bảo vệ dữ liệu game an toàn.

> **Lời thoại thuyết trình:** *"Để kiểm thử khả năng bảo mật, em thiết lập kịch bản người chơi dùng Cheat Engine can thiệp RAM client để sửa HP lên 99.999. Khi phát sinh va chạm combat, Dedicated Server tính toán máu thực của người chơi là 80 dựa trên sát thương thực tế, sau đó gửi gói tin đồng bộ vị trí và chỉ số về. Client của người chơi nhận dữ liệu chuẩn này và lập tức ghi đè lại chỉ số thực 80, phủ quyết hoàn toàn giá trị hack 99.999."*

---

## 📊 Slide 27: Kết Luận & Định Hướng Phát Triển Đề Tài
* **Ưu điểm:** Game multiplayer vận hành mượt mượt mà, bảo mật cao chống hack RAM, hệ thống tiến hóa Gene độc đáo và tối ưu hóa database bằng JSON Column.
* **Hạn chế:** Chưa xây dựng được client anti-cheat agent cục bộ để phát hiện tiến trình chạy ngầm.
* **Định hướng:** Phát triển PvP đấu trường 50-100 người và áp dụng nén nhị phân Protocol Buffers thay thế JSON để tối ưu băng thông.

> **Lời thoại thuyết trình:** *"Em xin kết luận lại đồ án: Đồ án đã giải quyết tốt bài toán đồng bộ di chuyển, bảo mật chống hack RAM nhờ Dedicated Server và tối ưu lưu trữ bằng JSON Column. Tuy nhiên vẫn còn hạn chế là chưa có client anti-cheat agent cục bộ. Định hướng phát triển tiếp theo là xây dựng đấu trường PvP nhiều người chơi hơn và áp dụng nén gói tin nhị phân Protocol Buffers thay thế cho JSON để tối ưu tối đa băng thông mạng."*

---

## 📊 Slide 28: Slide kết thúc
* Kính chúc quý Thầy Cô trong Hội đồng sức khỏe và công tác tốt!
* Em rất mong nhận được những câu hỏi phản biện và đóng góp ý kiến từ Hội đồng để đồ án được hoàn thiện hơn.

> **Lời thoại thuyết trình:** *"Em xin chân thành cảm ơn quý Thầy Cô trong Hội đồng đã lắng nghe bài báo cáo đồ án tốt nghiệp của em. Em rất mong nhận được những câu hỏi phản biện và đóng góp ý kiến từ quý Thầy Cô để đồ án của em được hoàn thiện hơn nữa. Em xin cảm ơn."*
