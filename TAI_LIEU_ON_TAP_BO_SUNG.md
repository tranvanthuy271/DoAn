# 📘 TÀI LIỆU ÔN TẬP BỔ SUNG: CÂU HỎI BẢO VỆ ĐỒ ÁN TỐT NGHIỆP
**Đề tài:** Phát triển trò chơi Mutants Arena với hệ thống tiến hóa Gene bằng Unity
**Sinh viên thực hiện:** Trần Văn Thủy

Tài liệu này tổng hợp toàn bộ các câu hỏi phản biện chi tiết mà bạn đã trao đổi trong phiên làm việc này, được trình bày một cách khoa học để hỗ trợ ôn tập nhanh trước khi ra hội đồng.

---

## 🎮 CHỦ ĐỀ 1: CƠ CHẾ ĐIỀU KHIỂN & CẢM GIÁC CHƠI (GAME FEEL)

### Câu 1: Cơ chế Coyote Time và Jump Buffer tự lập trình hoạt động thế nào về mặt logic toán học? Nằm ở những dòng nào trong code?

#### 1. Logic Toán học & Bộ đếm thời gian (Timer)
* **Coyote Time (Thời gian đệm sau khi rơi khỏi mép vực):**
  * Khi nhân vật đứng trên đất (`isGrounded == true`), bộ đếm `coyoteTimeCounter` liên tục được reset bằng giá trị tối đa `coyoteTime` ($0.2\text{s}$).
  * Khi nhân vật rơi tự do khỏi mép vực (`isGrounded == false`), `coyoteTimeCounter` đếm ngược giảm dần theo thời gian thực trôi qua mỗi frame:
    $$\text{coyoteTimeCounter} = \text{coyoteTimeCounter} - \Delta t$$
  * Trong khoảng thời gian này, nếu $\text{coyoteTimeCounter} > 0$, nhân vật vẫn được phép nhảy dù không chạm đất.
* **Jump Buffer (Lưu lệnh nhảy trước khi chạm đất):**
  * Khi người chơi nhấn phím nhảy, bộ đếm `jumpBufferCounter` lập tức được gán bằng giá trị tối đa `jumpBufferTime` ($0.2\text{s}$).
  * Trong các frame tiếp theo, bộ đếm này đếm ngược giảm dần:
    $$\text{jumpBufferCounter} = \text{jumpBufferCounter} - \Delta t$$
  * Nếu nhân vật chạm đất (hoặc còn trong thời gian Coyote Time) và $\text{jumpBufferCounter} > 0$, hành động nhảy sẽ tự động kích hoạt lập tức. Cả hai bộ đếm sau đó được reset về $0$ để tránh nhảy lặp lại ngoài ý muốn.

#### 2. Vị trí dòng code cụ thể trong dự án
* **Chế độ di chuyển Local / Singleplayer (File: `PlayerMovement.cs`):**
  * **Dòng 39 - 44:** Khai báo các tham số và bộ đếm `coyoteTimeCounter`, `jumpBufferCounter`.
  * **Dòng 163 - 192 (Hàm `HandleInput()`):** Thực hiện cập nhật đếm ngược và kiểm tra điều kiện nhảy.
  * **Dòng 276 - 280 (Hàm `HandleMovement()`):** Thực hiện lực đẩy vật lý nhảy lên (`AddForce` dạng `Impulse`).
* **Chế độ chơi mạng / Đồng bộ (File: `NetworkPlayerController.cs`):**
  * **Dòng 22 - 26:** Khai báo biến đếm đồng bộ.
  * **Dòng 168 - 191 (Hàm `Update()`):** Bắt phím nhấn nhảy trên Owner Client để gán và giảm `jumpBufferCounter` theo thời gian thực `Time.deltaTime` (tránh bỏ lỡ phím).
  * **Dòng 196 - 290 (Hàm `FixedUpdate()`):** Cập nhật `coyoteTimeCounter` theo `Time.fixedDeltaTime`, thực hiện nhảy vật lý đồng bộ và gửi dữ liệu lên Server qua `MoveServerRpc()`.

---

## 🖥️ CHỦ ĐỀ 2: KIẾN TRÚC MẠNG & PHÂN CHIA HỆ THỐNG

### Câu 2: Mô hình kiến trúc tổng thể Hybrid 3-Layer ở Slide 10 hoạt động thế nào? Hãy giải thích chi tiết vai trò của từng tầng.

#### 1. Tổng quan kiến trúc
Hệ thống được thiết kế theo mô hình **Hybrid 3-Layer (Client - Dedicated Server - Web API)** nhằm phân tách rạch ròi giữa logic tính toán vật lý game nhịp độ cao và logic lưu trữ dữ liệu bền vững.

#### 2. Vai trò chi tiết của 3 tầng
1. **Unity Client (C# - `/Client`):**
   * **Nhiệm vụ:** Nhận tương tác từ người chơi, render đồ họa và thực hiện các thuật toán tối ưu cảm giác chơi cục bộ.
   * **Giao tiếp:** Gọi REST API (HTTPS) để đăng ký/đăng nhập; kết nối SignalR (WebSockets) để chat sảnh và lập tổ đội; kết nối Netcode NGO (UDP) để đồng bộ chuyển động phó bản.
2. **Dedicated Server (C# - Build Headless):**
   * **Nhiệm vụ:** Là "nguồn sự thật duy nhất" (Source of Truth) xử lý toàn bộ logic gameplay, va chạm đòn đánh, và trí tuệ nhân tạo AI của quái vật.
   * **Đặc điểm:** Chạy ở chế độ Headless (không render đồ họa, chỉ tính toán vật lý 2D trên CPU máy chủ Linux), giúp tiết kiệm 90% tài nguyên RAM/CPU.
   * **Giao tiếp:** Xác thực người chơi bằng chữ ký JWT offline trên RAM; đóng vai trò REST Proxy gửi lệnh lưu trữ dữ liệu lên API Server thông qua API Key nội bộ.
3. **Web API & Database (C# - `/GameServerApi` & MySQL):**
   * **Nhiệm vụ:** Lưu trữ dữ liệu nhân vật, tài khoản và cung cấp cấu hình game tĩnh.
   * **Tối ưu hóa:** Sử dụng cột **JSON Column** trong MySQL để lưu trữ dữ liệu động (túi đồ, gene lai), loại bỏ 80% truy vấn `JOIN` phức tạp và đẩy nhanh tốc độ nạp dữ liệu lên 40%.

---

## 🌐 CHỦ ĐỀ 3: CÁC KHÁI NIỆM MẠNG CƠ BẢN TRONG GAME

### Câu 3: Netcode là gì? NGO là gì? UDP là gì? Tại sao lại dùng chúng?

* **Netcode:** Là thuật ngữ chung trong ngành sản xuất game chỉ toàn bộ hệ thống mã nguồn, giao thức truyền tải và thuật toán (như dự đoán di chuyển, bù trễ) dùng để đồng bộ hóa trạng thái trò chơi thời gian thực giữa nhiều máy tính qua Internet.
* **NGO (Netcode for GameObjects):** Thư viện mạng chính thức ở tầng cao do Unity phát triển. NGO giúp đồng bộ các đối tượng game thông qua các biến mạng (`NetworkVariable`) và cơ chế gọi hàm từ xa RPC (`[ServerRpc]` và `[ClientRpc]`).
* **UDP (User Datagram Protocol):** Giao thức truyền tải dữ liệu ở tầng giao vận (Transport Layer). UDP hoạt động theo mô hình không hướng kết nối, không kiểm tra nhận gói tin hay truyền lại gói tin bị mất.
  * **Tại sao dùng UDP cho game hành động?** UDP mang lại tốc độ truyền dữ liệu nhanh nhất và độ trễ tối thiểu. Trong game hành động, dữ liệu vị trí được gửi liên tục (50 lần/giây), nếu mất 1 gói tin thì gói tiếp theo sẽ đè lên vị trí mới ngay lập tức. Nếu dùng TCP, game sẽ bị giật khựng do phải chờ truyền lại gói tin cũ bị mất.

---

### Câu 4: Cơ chế đồng bộ trạng thái tự động thông qua biến mạng (NetworkVariable) hoạt động như thế nào?

* **Nguyên lý:** Thay vì phải viết code gửi gói tin thủ công mỗi khi một chỉ số thay đổi, ta bọc kiểu dữ liệu vào lớp `NetworkVariable<T>`. Hệ thống Netcode sẽ tự động theo dõi biến này.
* **Phân quyền bảo mật:**
  * Chỉ Server mới có quyền thay đổi giá trị của biến mạng (`WritePermission.Server`) nhằm chống hack bộ nhớ RAM từ phía Client.
  * Mọi Client chỉ được quyền đọc giá trị (`ReadPermission.Everyone`).
* **Luồng chạy thực tế:**
  1. Khi nhân vật chạy sang trái, Server phát hiện input và đổi giá trị biến mạng hướng nhìn: `networkScaleX.Value = -1f;`
  2. Unity Netcode tự động gửi gói tin chứa giá trị `-1f` này xuống tất cả các client đang kết nối.
  3. Phía Client lắng nghe sự kiện thay đổi giá trị (`OnValueChanged`) để cập nhật hiển thị hình ảnh nhân vật lật mặt sang trái tương ứng.

---

### Câu 5: Tại sao lại kết hợp cả hai thành phần NGO (Dedicated Server) và Web API Server để lưu trữ dữ liệu nhân vật?

* **Tối ưu hóa hiệu năng (Separation of Concerns):** Dedicated Server (NGO) cần tập trung toàn bộ CPU để xử lý các phép tính vật lý và AI quái vật ở tần số quét cao (50Hz). Việc truy vấn trực tiếp xuống database MySQL là tác vụ cực kỳ nặng và gây nghẽn luồng (blocking). Vì vậy, Dedicated Server chỉ lưu dữ liệu tạm thời trên RAM, còn các tác vụ đọc/ghi cơ sở dữ liệu nặng sẽ do Web API xử lý bất đồng bộ ở một máy chủ riêng.
* **Bảo mật tuyệt đối:** Ngăn chặn hoàn toàn việc Client kết nối trực tiếp vào database MySQL. Web API đứng sau tường lửa, đóng vai trò xác thực mã JWT. Dedicated Server đóng vai trò trung gian phê duyệt hành động (REST Proxy), đảm bảo hacker không thể bypass luật chơi để sửa DB.
* **Khả năng mở rộng (Scalability):** Giúp hệ thống dễ dàng mở rộng theo chiều ngang (Horizontal Scaling). Khi lượng người chơi tăng, ta chỉ cần bật thêm nhiều máy chủ chạy Dedicated Server để chia tải các trận đấu, và tất cả chúng đều trỏ về một Web API trung tâm duy nhất để cất giữ dữ liệu vào database MySQL.

---

### Câu 6: Zone API Key (X-Zone-Api-Key) là gì và tại sao phải thiết lập cơ chế này?

#### 1. Định nghĩa
**Zone API Key** (truyền qua HTTP Header `X-Zone-Api-Key`) là một **mã khóa bí mật dùng chung (Shared Secret)** được cấu hình ở cả hai đầu: Dedicated Server và Web API. Đây là cơ chế xác thực chuyên dụng cho giao tiếp **Server-to-Server (S2S)**.

#### 2. Lý do phải thiết lập Zone API Key
* **Phân quyền vai trò đặc biệt (GameServer Role):**
  * Trong game, có những hành động hệ thống mà Client bình thường không bao giờ được phép làm (ví dụ: thông báo phòng đấu đầy, cập nhật danh sách phòng, ghi nhận phần thưởng sau phó bản, ngắt kết nối người chơi).
  * Web API sử dụng lớp `ZoneApiKeyMiddleware.cs` để nhận diện header `X-Zone-Api-Key`. Nếu khớp khóa bí mật, API sẽ cấp quyền hạn cấp cao `"GameServer"` cho request đó, cho phép Dedicated Server thao tác dữ liệu thay mặt bất cứ người chơi nào.
* **Chặn đứng Client giả mạo:**
  * Dù hacker có sở hữu JWT Token hợp lệ của tài khoản họ đi chăng nữa, họ cũng không có mã Zone API Key nội bộ này. Vì thế, hacker không thể tự gửi các request HTTP trực tiếp lên Web API để tự cộng vàng hay chỉnh sửa Gene.
* **Ngăn chặn tấn công Timing Attack (Tấn công đo thời gian):**
  * Trong mã nguồn middleware của bạn (`ConstantTimeEquals`), việc so sánh khóa được thực hiện theo cơ chế **thời gian không đổi (Constant-Time Comparison)** thay vì toán tử `==` thông thường. Điều này ngăn chặn hacker sử dụng kỹ thuật đo thời gian phản hồi của Server để dò tìm từng ký tự của mã khóa.

---

### Câu 7: Nếu Hacker đã có JWT hợp lệ của chính họ và gửi thẳng lên Web API bằng Postman, Web API chấp nhận JWT rồi thì làm sao từ chối được yêu cầu của Hacker?

Đây là một sự nhầm lẫn giữa **Xác nhận danh tính (Authentication)** và **Cấp quyền hành động (Authorization)**. Dưới đây là cách Web API của hệ thống từ chối Hacker:

#### 1. Server chỉ tin Database, KHÔNG tin payload của Client (Server-Authoritative)
Dù Hacker có JWT hợp lệ và API cho phép request đi qua, Hacker **không thể** tự gửi gói tin định đoạt kết quả (ví dụ: `{"newLevel": 99, "gold": 99999}`).
Code API của bạn chỉ nhận tham số cơ bản (ví dụ: `geneId`). Ngay khi nhận lệnh, Web API tự động mở Database ra để kiểm tra chéo:
* Player này có đủ vàng không? (Đọc từ DB, không đọc từ Client).
* Player này có đủ lõi tiến hóa không? (Đọc từ DB).
Nếu không đủ, Web API lập tức quăng lỗi `400 Bad Request`. Hacker chỉ có quyền "Bấm nút yêu cầu", còn Server mới là người "Định đoạt kết quả".

#### 2. Quyền hạn theo Role (Role-Based Access Control)
Có những đường link API (Endpoint) vô cùng nhạy cảm (ví dụ: cộng vàng sau khi đánh Boss, ghi đè toàn bộ chỉ số). Web API cài đặt bảo mật chỉ dành cho Role `GameServer` (Dedicated Server mang mã `X-Zone-Api-Key`) mới được phép gọi.
Khi Hacker dùng JWT gửi vào đường link này, kịch bản sẽ là:
1. API kiểm tra JWT: *"Đúng, mày là người chơi Nguyễn Văn A."* (Authentication thành công).
2. API kiểm tra Role: *"Nhưng endpoint này chỉ dành cho máy chủ trận đấu. Mày chỉ mang Role `Player`."*
3. API từ chối lập tức: Trả về lỗi **403 Forbidden** (Không có quyền truy cập).

> **Lời thoại chốt hạ:** *"Dạ thưa Hội đồng, việc Client có JWT chỉ giúp xác định họ là ai, chứ không cấp cho họ quyền làm mọi thứ. Hệ thống của em áp dụng nguyên tắc Server-Authoritative (Server làm chủ) và Phân quyền theo Role. JWT của người chơi bị chặn hoàn toàn khỏi các endpoint dành riêng cho Dedicated Server (như thao tác lưu kết quả trận đấu), do đó hacker không thể can thiệp hệ thống."*
