# 🎤 Kịch Bản Thuyết Trình & Báo Cáo Kiến Trúc Server

Tài liệu này được biên soạn dưới dạng đề cương báo cáo học thuật và kịch bản thuyết trình chi tiết phục vụ cho việc bảo vệ Đồ án tốt nghiệp. Nội dung tập trung làm nổi bật kiến trúc hệ thống và luồng vận hành cốt lõi của Server.

---

## PHẦN 1: MỞ ĐẦU & TỔNG QUAN KIẾN TRÚC

> **Lời thoại thuyết trình gợi ý:**
> *"Kính thưa thầy cô trong Hội đồng, sau đây em xin phép đại diện nhóm trình bày về Luồng kiến trúc tổng thể và cơ chế vận hành của hệ thống Server trong đồ án. Để giải quyết bài toán tải lớn và đảm bảo tính đồng bộ thời gian thực cho game, hệ thống được thiết kế theo mô hình lai (Hybrid Architecture) chia làm 3 lớp dịch vụ chính kết nối với Cơ sở dữ liệu MySQL ở hạ tầng."*

### 1.1. Mô hình Kiến trúc 3 Lớp Lai (Hybrid Architecture)
* **Lớp 1 - ASP.NET Core API Server**: Đóng vai trò là cổng dịch vụ nghiệp vụ (Business Service Layer). Đảm nhận việc quản lý tài khoản, chứng thực bảo mật, xử lý các logic phi thời gian thực (như giao dịch vật phẩm, nâng cấp thuộc tính nhân vật, lưu trữ cơ sở dữ liệu) và cấu hình tĩnh của hệ thống.
* **Lớp 2 - Dedicated Game Server (chạy Unity Netcode for GameObjects)**: Đóng vai trò là máy chủ mô phỏng thời gian thực (Realtime Simulation Layer). Đảm nhận việc tính toán vật lý, kiểm tra va chạm, quản lý trạng thái di chuyển của người chơi và quái vật dưới cơ chế *Server-Authoritative* (mọi quyết định thuộc về Server để chống hack/cheat).
* **Lớp 3 - Realtime Communication Hub (SignalR)**: Đóng vai trò là kênh truyền tải thông điệp không đồng bộ thời gian thực (Realtime Message Bus). Kênh này tách biệt hoàn toàn với Game Server để gánh tải cho các hoạt động phụ trợ như trò chuyện (Chat) toàn bộ máy chủ và duy trì trạng thái tổ đội (Party).

---

## PHẦN 2: CÁC LUỒNG VẬN HÀNH CỐT LÕI (CORE RUNTIME FLOWS)

> **Lời thoại thuyết trình gợi ý:**
> *"Tiếp theo, em xin đi sâu vào các luồng xử lý chính từ lúc người chơi bắt đầu đăng nhập cho đến khi tham gia vào các hoạt động trong game."*

### 2.1. Luồng Xác Thực và Khởi Tạo Kết Nối (Authentication & Connection Approval)
* **Bước 1 (Xác thực phía API)**: Người chơi nhập tài khoản từ Client, hệ thống gửi yêu cầu qua giao thức HTTPS đến `AuthController`. Server xác thực bằng cơ chế băm mật khẩu Bcrypt, tiến hành lưu trữ lịch sử đăng nhập, ghi nhận điểm danh ngày và cấp phát một chuỗi định danh bảo mật **JWT (JSON Web Token)** chứa thông tin người chơi.
* **Bước 2 (Bắt tay kết nối mạng - Handshake)**: Client sử dụng Token JWT này làm Payload để thực hiện thủ tục bắt tay với Unity Dedicated Server. Lớp `ZoneConnectionApprovalV2` trên Dedicated Server sẽ giải mã JWT bằng `JwtValidator` để định danh Client mà không cần truy vấn lại Database. 
* **Bước 3 (Khởi tạo phòng chơi - Room Assignment)**: Sau khi Token hợp lệ, Dedicated Server tra cứu vị trí gần nhất của người chơi từ cơ sở dữ liệu và bàn giao Client cho lớp quản lý vùng chơi `ZoneRoomRegistry` để sắp xếp vào đúng Map và Zone quy định.

---

### 2.2. Cơ Chế Quản Lý Phòng Chơi Động (Zone Room Registry)
> **Lời thoại thuyết trình gợi ý:**
> *"Một trong những điểm cải tiến của đồ án là cơ chế quản lý Map/Zone động kế thừa theo cơ chế của các game MMO hiện đại, thay vì phân mảnh cổng (port) tĩnh."*

* **Map thường (SharedPublic)**: Khi khởi động máy chủ (Server Boot), hệ thống tự động sinh ra một số lượng phòng công khai tĩnh (ví dụ: mặc định là 15 zone mỗi map). Người chơi có quyền chủ động gửi RPC yêu cầu chuyển đổi khu vực dựa trên tải lượng của mỗi phòng.
* **Map phó bản (InstanceOnly)**: Khi người chơi hoặc tổ đội yêu cầu đi ải, Server sẽ **tạo động các phòng riêng lẻ** có mã định danh vùng chơi mang giá trị âm (ví dụ: `-1`, `-2`,...). Các phòng này tồn tại hoàn toàn trên bộ nhớ RAM động và sẽ tự động giải phóng, thu hồi bộ nhớ ngay sau khi không còn người chơi bên trong.

---

### 2.3. Luồng Đồng Bộ Nhân Vật và Tối Ưu Hóa Băng Thông (Spawning & Network Visibility)
* **Đồng bộ dữ liệu**: Sau khi kết nối thành công, Dedicated Server gọi nội bộ API đến REST API Server để lấy toàn bộ cấu hình chỉ số nhân vật (máu, năng lượng, trang bị, kỹ năng hiện tại) và lưu trữ tạm thời (cache) vào bộ nhớ của Server thông qua `ServerPlayerDataManager`.
* **Spawn Thực thể**: Server thực hiện khởi tạo (Instantiate) Prefab nhân vật tương ứng với hệ phái và giới tính của người chơi, gán quyền điều khiển (Ownership) về phía Client, và đồng bộ hóa vị trí qua mạng Netcode.
* **Tối ưu hóa tầm nhìn (Network Visibility Filter)**: Nhằm tối ưu hóa băng thông truyền tải, hệ thống tích hợp bộ lọc tầm nhìn. Người chơi ở Zone này sẽ **không nhận** bất kỳ gói tin đồng bộ vị trí, hành động hay chat của người chơi ở các Zone khác, giúp máy chủ có khả năng chịu tải cao hơn trên một Scene lớn.

---

### 2.4. Luồng Chuyển Màn Chơi (Teleport & Map Transition)
* Khi người chơi chạm vào các khu vực chuyển cảnh (Portal):
  1. Client gửi yêu cầu chuyển map thông qua RPC.
  2. `ZoneTransitionController` trên Server tiếp nhận, kiểm tra tính hợp lệ của điểm đến và cập nhật thông tin phiên chơi tại `ZonePlayerSessionManager`.
  3. Server gửi lệnh ClientRpc yêu cầu Client chuyển đổi giao diện và tải Scene mới thông qua `ClientSceneController`.
  4. Đồng thời, tọa độ mới của người chơi được cập nhật trực tiếp xuống Cơ sở dữ liệu thông qua REST API Server nhằm tránh thất thoát dữ liệu (Rollback) khi xảy ra sự cố đột ngột.

---

### 2.5. Luồng Xử Lý AI Boss Nhiều Pha (Multi-phase Boss AI)
> **Lời thoại thuyết trình gợi ý:**
> *"Để tăng tính hấp dẫn cho các trận đấu Boss trong phó bản, em đã xây dựng cơ chế AI của Boss thay đổi hành vi theo các pha lượng máu hiện tại."*

* **Cơ chế hoạt động**: Khi Boss được sinh ra trong phó bản, lớp điều khiển `BossAI` sẽ tiến hành lấy thông số các pha hành động từ trường `phases_json` cấu hình trong DB. Hệ thống cũng được thiết kế cơ chế dự phòng tự động kích hoạt các pha tại mốc **60% HP** và **30% HP** nếu DB chưa được cấu hình.
* **Luồng thay đổi hành vi (Phase Transition)**:
  * Trong vòng lặp cập nhật trạng thái (Update Loop) trên Server, hệ thống liên tục tính toán tỉ lệ phần trăm máu hiện tại của Boss.
  * Khi máu chạm ngưỡng quy định, lớp AI kích hoạt hàm `ExecutePhase()` để biến đổi trạng thái của Boss:
    * **Pha 2 (Máu <= 60%)**: Kích hoạt trạng thái phẫn nộ (`enrage`), tăng chỉ số tấn công và tốc độ di chuyển.
    * **Pha 3 (Máu <= 30%)**: Kích hoạt trạng thái cuồng nộ (`berserk`), tăng mạnh sát thương, tốc độ di chuyển và giảm thời gian hồi chiêu của các kỹ năng, đồng thời có thể triệu hồi quái đệ tử (`summon`) hỗ trợ.

---

## PHẦN 3: KẾT LUẬN & ĐIỂM SÁNG CÔNG NGHỆ

> **Lời thoại thuyết trình gợi ý:**
> *"Kính thưa Hội đồng, thiết kế kiến trúc này đã mang lại một số ưu điểm vượt trội cho hệ thống game như sau:"*

1. **Bảo mật tối đa**: Mọi thông tin nhạy cảm (như mật khẩu, giao dịch, chỉ số nhân vật) đều được mã hóa bằng JWT và quản lý chặt chẽ qua API Server, loại bỏ nguy cơ can thiệp dữ liệu dưới Client.
2. **Tiết kiệm tài nguyên Server**: Sử dụng cơ chế gộp Zone trên một Port duy nhất thông qua Registry giúp giảm số lượng luồng hệ thống (Thread) và quản lý vòng đời bộ nhớ hiệu quả nhờ cơ chế Custom Room động.
3. **Phân tách trách nhiệm rõ ràng (Separation of Concerns)**: Tách biệt luồng chơi vật lý (Dedicated Server), luồng lưu trữ lâu dài (REST API) và luồng tin nhắn phụ trợ (SignalR) giúp hệ thống dễ dàng mở rộng và bảo trì trong tương lai.

*(Em xin chân thành cảm ơn thầy cô đã lắng nghe, sau đây em xin phép được lắng nghe câu hỏi và nhận xét từ thầy cô.)*
