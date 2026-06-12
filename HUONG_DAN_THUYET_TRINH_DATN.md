# 🎓 Cẩm Nang Thuyết Trình Bảo Vệ Đồ Án Tốt Nghiệp
## Đề tài: Phát triển trò chơi Mutants Arena với hệ thống tiến hóa Gene bằng Unity
**Sinh viên thực hiện:** Trần Văn Thủy
**Người hướng dẫn:** TS. Nguyễn Đức Hiếu

---

## 💡 Chiến Lược Thuyết Trình Trước Hội Đồng
* **Thời gian giới hạn:** Thông thường từ **10 đến 15 phút**. Bạn không được đọc lại toàn bộ chữ trên slide. Hãy tập trung giải thích **TẠI SAO** bạn làm thế và **KỸ THUẬT** bạn giải quyết vấn đề đó là gì.
* **Phong thái:** Tự tin, giọng nói rõ ràng, mạch lạc. Khi bị ngắt lời hoặc nhận câu hỏi phản biện, hãy bình tĩnh ghi chép lại và trả lời lễ phép, đi thẳng vào giải pháp kỹ thuật đã cài đặt trong code.
* **Từ khóa ghi điểm:** *Server-Authoritative (Server làm chủ)*, *Client-Prediction (Dự đoán phía Client)*, *Hybrid Architecture (Kiến trúc lai)*, *Zone-based Server*, *Offline JWT Validation*.

---

## 📑 Hướng Dẫn Chi Tiết Từng Slide (1 - 32)

### Slide 1: Slide Mở Đầu (Giới thiệu)
* **Thời gian đề xuất:** 30 giây
* **Thông điệp cốt lõi:** Lời chào trang trọng, giới thiệu đề tài ngắn gọn và người hướng dẫn khoa học.
* **Lời thoại đề xuất (Script):**
  > *"Kính thưa các thầy cô trong Hội đồng bảo vệ đồ án tốt nghiệp và toàn thể các bạn sinh viên. Em tên là Trần Văn Thủy, lớp CT6D, mã số sinh viên CT060439. Hôm nay, em xin phép được trình bày báo cáo đồ án tốt nghiệp của mình với đề tài: **'Phát triển trò chơi Mutants Arena với hệ thống tiến hóa Gene bằng Unity'**, dưới sự hướng dẫn khoa học của thầy TS. Nguyễn Đức Hiếu. Kính mong hội đồng lắng nghe và đóng góp ý kiến."*
* **Mẹo trình bày:** Đứng thẳng, nhìn bao quát Hội đồng, cười nhẹ để tạo thiện cảm ban đầu.

---

### Slide 2: Đặt vấn đề
* **Thời gian đề xuất:** 45 giây
* **Thông điệp cốt lõi:** Tại sao đề tài này cần thiết? Những thách thức kỹ thuật lớn trong game multiplayer là gì?
* **Lời thoại đề xuất (Script):**
  > *"Thưa thầy cô, ngành công nghiệp game RPG 2D, đặc biệt là game chơi mạng thời gian thực (Multiplayer), đang phát triển mạnh mẽ. Tuy nhiên, việc phát triển các dòng game này đối mặt với 3 thách thức kỹ thuật lớn: Thứ nhất là **đồng bộ hóa trạng thái di chuyển** trong thời gian thực dưới tác động của độ trễ mạng (latency); Thứ hai là **vấn đề bảo mật, chống gian lận** (cheat/hack) do dữ liệu xử lý hoàn toàn ở client; Thứ ba là **cân bằng tài nguyên hệ thống** khi số lượng người chơi tăng cao. Từ những thách thức đó, em quyết định phát triển trò chơi Mutants Arena kết hợp hệ thống tiến hóa Gene độc đáo, nhằm áp dụng các giải pháp kỹ thuật tối ưu hóa mạng để giải quyết các vấn đề trên."*
* **Mẹo trình bày:** Nhấn mạnh vào 3 thách thức (Đồng bộ - Bảo mật - Hiệu năng) vì đây sẽ là sợi chỉ đỏ xuyên suốt các chương sau.

---

### Slide 3: Nội dung báo cáo
* **Thời gian đề xuất:** 15 giây
* **Thông điệp cốt lõi:** Cấu trúc bài báo cáo rõ ràng, mạch lạc.
* **Lời thoại đề xuất (Script):**
  > *"Bài báo cáo của em ngày hôm nay gồm có 4 phần chính: Chương 1 - Tổng quan về đề tài; Chương 2 - Phân tích và thiết kế hệ thống; Chương 3 - Xây dựng các cơ chế game; và cuối cùng là Chương 4 - Thực nghiệm và đánh giá kết quả."*
* **Mẹo trình bày:** Đi nhanh qua, không dừng lại lâu ở slide mục lục này.

---

### Slide 4: Chương 01 - Tổng quan về đề tài
* **Thời gian đề xuất:** 10 giây
* **Thông điệp cốt lõi:** Bắt đầu giới thiệu bối cảnh đề tài.
* **Lời thoại đề xuất (Script):**
  > *"Sau đây, em xin phép đi vào Chương 1 - Tổng quan về đề tài, để làm rõ định hướng gameplay và các cơ chế cốt lõi của trò chơi."*

---

### Slide 5: Game 2D Action RPG
* **Thời gian đề xuất:** 30 giây
* **Thông điệp cốt lõi:** Định nghĩa và đặc trưng của thể loại game Action RPG 2D.
* **Lời thoại đề xuất (Script):**
  > *"Trò chơi thuộc thể loại 2D Action RPG. Đây là sự kết hợp hài hòa giữa cơ chế điều khiển phản xạ thời gian thực trên bản đồ di chuyển đa nền tảng (Platformer), cùng với chiều sâu phát triển nhân vật thông qua hệ thống chỉ số thuộc tính, trang bị và kỹ năng chiến đấu. Điều này đòi hỏi hệ thống điều khiển phía client phải cực kỳ nhạy và mượt mà."*

---

### Slide 6: Bài học thiết kế game
* **Thời gian đề xuất:** 30 giây
* **Thông điệp cốt lõi:** Áp dụng lý thuyết Game Design bài bản vào sản phẩm.
* **Lời thoại đề xuất (Script):**
  > *"Khi thiết kế Mutants Arena, em đã áp dụng các nguyên lý game design cốt lõi để thu hút người chơi. Cụ thể là xây dựng một vòng lặp gameplay (gameplay loop) rõ ràng: Người chơi chiến đấu vượt phó bản -> thu thập tài nguyên -> nâng cấp và dung hợp Gene thuộc tính ngũ hành -> mở khóa sức mạnh mới và tiếp tục thử thách ở các phó bản khó hơn."*

---

### Slide 7: Lý thuyết di chuyển & AI FSM
* **Thời gian đề xuất:** 1 phút
* **Thông điệp cốt lõi:** Cơ sở vật lý điều khiển mượt mà và thuật toán AI quái vật.
* **Lời thoại đề xuất (Script):**
  > *"Về mặt kỹ thuật lập trình client, em áp dụng vật lý di chuyển 2D Box2D. Để tạo cảm giác điều khiển mượt mà chân thực, em sử dụng cơ chế **Variable Gravity Jump** (điều chỉnh trọng lực khi rơi lớn hơn lúc nhảy lên) kết hợp kiểm tra mặt đất bằng **OverlapCircle**. Đối với quái vật, em mô hình hóa hành vi bằng **Finite State Machine (FSM - Máy trạng thái hữu hạn)** gồm các trạng thái rời rạc như tuần tra, đuổi theo và tấn công để tiết kiệm tài nguyên tính toán của CPU máy chủ."*
* **Q&A Phản biện:**
  * **Hỏi:** *Tại sao lại dùng OverlapCircle thay vì Raycast để Ground Detection (phát hiện chạm đất)?*
  * **Trả lời:** *Dạ thưa thầy cô, OverlapCircle quét một vùng hình tròn dưới chân nhân vật, giúp phát hiện mặt đất ổn định hơn khi nhân vật đứng ở sát mép (edges) của collider, tránh hiện tượng bị hụt nhảy hoặc giật hoạt ảnh nhảy khi mép chân hơi lệch khỏi mặt đất, điều mà một tia Raycast đơn lẻ hướng xuống dưới rất dễ bỏ sót.*

---

### Slide 8: Phát triển nhân vật & Tiến hóa Gene
* **Thời gian đề xuất:** 1 phút
* **Thông điệp cốt lõi:** Điểm độc đáo và sáng tạo nhất về gameplay của đề tài.
* **Lời thoại đề xuất (Script):**
  > *"Điểm đặc sắc của đồ án là cơ chế Tiến hóa Gene thay thế cho hệ thống chọn lớp nhân vật truyền thống. Người chơi có cơ chế **Multi-Gene** cho phép lắp đồng thời 1 Gene chính (nhận 100% hiệu năng) và 1 Gene phụ (nhận 30% hiệu năng cộng thêm). Khi cả hai Gene đạt cấp độ tối đa, người chơi có thể tiến hành **Dung hợp (Hybrid Fusion)** để tạo ra lớp nhân vật lai nguyên tố với bộ kỹ năng tối thượng diện rộng, mang tính chiến thuật rất cao."*
* **Q&A Phản biện:**
  * **Hỏi:** *Hệ thống Gene này được lưu trữ và tính toán chỉ số như thế nào trên Server/Database?*
  * **Trả lời:** *Dạ, dữ liệu Gene của người chơi gồm ID Gene chính, Gene phụ, cấp độ và danh sách mảnh Gene được lưu trữ dưới dạng quan hệ trong bảng `Gene_Data`. Khi người chơi khảm hoặc dung hợp, Dedicated Server sẽ gửi request lên Web API để cập nhật dữ liệu. Sau đó, Server tính toán lại tổng thuộc tính nhân vật theo công thức: **Tổng chỉ số = Chỉ số cơ bản + Chỉ số Gene chính + 30% Chỉ số Gene phụ** và ghi nhận trực tiếp vào session chơi game.*

---

### Slide 9: Kiến trúc mạng & Công nghệ sử dụng
* **Thời gian đề xuất:** 15 giây
* **Thông điệp cốt lõi:** Giới thiệu tổng quan công nghệ.
* **Lời thoại đề xuất (Script):**
  > *"Để hiện thực hóa các cơ chế game phức tạp và đảm bảo khả năng chơi mạng thời gian thực, em đã thiết kế một kiến trúc mạng phân tầng sử dụng các công nghệ hiện đại. Chi tiết sẽ được trình bày ở Chương 2 tiếp theo."*

---

### Slide 10: Chương 02 - Phân tích và thiết kế hệ thống
* **Thời gian đề xuất:** 10 giây
* **Thông điệp cốt lõi:** Bắt đầu giới thiệu kiến trúc kỹ thuật của đồ án.
* **Lời thoại đề xuất (Script):**
  > *"Em xin phép trình bày Chương 2: Phân tích và thiết kế hệ thống, trọng tâm vào kiến trúc mạng và thiết kế cơ sở dữ liệu..."*

---

### Slide 11: Mô tả hệ thống (Kiến trúc Hybrid 3-Layer)
* **Thời gian đề xuất:** 1.5 phút
* **Thông điệp cốt lõi:** Giải thích mô hình kiến trúc mạng cực kỳ chuyên nghiệp của dự án.
* **Lời thoại đề xuất (Script):**
  > *"Hệ thống của em xây dựng theo mô hình **Hybrid 3-Layer** gồm 3 phần chạy độc lập..."*
* **Q&A Phản biện:**
  * **Hỏi:** *Tại sao lại tách Dedicated Server (chạy Unity Headless) riêng biệt với ASP.NET Core API? Sao không gộp chung?*
  * **Trả lời:** *Dạ thưa thầy cô, việc tách biệt mang lại 3 ưu điểm lớn...*

---

### Slide 12: Biểu đồ Use Case tổng quát
* **Thời gian đề xuất:** 30 giây
* **Thông điệp cốt lõi:** Khái quát các tính năng của hệ thống.
* **Lời thoại đề xuất (Script):**
  > *"Đây là biểu đồ Use Case tổng quát của hệ thống..."*

---

### Slide 13: Biểu đồ Use Case Chiến đấu & Kỹ năng
* **Thời gian đề xuất:** 20 giây
* **Thông điệp cốt lõi:** Luồng chức năng chiến đấu trong game.
* **Lời thoại đề xuất (Script):**
  > *"Biểu đồ này chi tiết hóa use case chiến đấu..."*

---

### Slide 14: Biểu đồ Use Case Tiết hóa Gene
* **Thời gian đề xuất:** 20 giây
* **Thông điệp cốt lõi:** Luồng chức năng nâng cấp và dung hợp Gene.
* **Lời thoại đề xuất (Script):**
  > *"Biểu đồ Use Case tiến hóa Gene mô tả chi tiết quy trình..."*

---

### Slide 15: Biểu đồ Use Case Tham gia phó bản và hoàn tất phó bản
* **Thời gian đề xuất:** 25 giây
* **Thông điệp cốt lõi:** Luồng tạo phòng phó bản riêng tư (Instance).
* **Lời thoại đề xuất (Script):**
  > *"Use case tham gia phó bản thể hiện cơ chế tạo phó bản..."*

---

### Slide 16: Cơ sở dữ liệu vật lý ERD (Giải pháp tối ưu hóa)
* **Thời gian đề xuất:** 1 phút
* **Thông điệp cốt lõi:** Thiết kế cơ sở dữ liệu tối ưu hóa bằng JSON Column.
* **Lời thoại đề xuất (Script):**
  > *"Cơ sở dữ liệu vật lý gồm các bảng liên kết chặt chẽ... em đã áp dụng giải pháp thiết kế JSON Column trong MySQL..."*
* **Q&A Phản biện:**
  * **Hỏi:** *Nhược điểm của việc sử dụng JSON Column trong MySQL là gì? Khi nào thì không nên dùng?*
  * **Trả lời:** *Dạ thưa thầy cô, nhược điểm của JSON Column là...*

---

### Slide 17: Sơ đồ Cơ sở dữ liệu vật lý ERD (Hình vẽ)
* **Thời gian đề xuất:** 30 giây
* **Thông điệp cốt lõi:** Minh họa trực quan các thực thể.
* **Lời thoại đề xuất (Script):**
  > *"Kính thưa thầy cô, đây là sơ đồ ERD chi tiết của hệ thống..."*

---

### Slide 18: Chương 03 - Xây dựng các cơ chế game
* **Thời gian đề xuất:** 10 giây
* **Thông điệp cốt lõi:** Bắt đầu phần triển khai mã nguồn thực tế.
* **Lời thoại đề xuất (Script):**
  > *"Sau đây, em xin phép trình bày Chương 3 - Xây dựng các cơ chế game..."*

---

### Slide 19: Xây dựng hệ thống điều khiển & di chuyển (Game Feel)
* **Thời gian đề xuất:** 1 phút
* **Thông điệp cốt lõi:** Kỹ thuật lập trình tạo cảm giác điều khiển mượt mà (Game Feel).
* **Lời thoại đề xuất (Script):**
  > *"Để mang lại trải nghiệm điều khiển tốt nhất cho người chơi trên nền tảng 2D Platformer, em đã lập trình 4 cơ chế nâng cao..."*
* **Q&A Phản biện:**
  * **Hỏi:** *Em lập trình Coyote Time như thế nào trong code?*
  * **Trả lời:** *Dạ, trong script PlayerMovement.cs...*

---

### Slide 20: Xây dựng cơ chế chiến đấu & Tương khắc Ngũ hành
* **Thời gian đề xuất:** 1.5 phút
* **Thông điệp cốt lõi:** Cơ chế combat công bằng, chống hack và tính chiến thuật của game.
* **Lời thoại đề xuất (Script):**
  > *"Đối với hệ thống combat, để chống gian lận, em sử dụng cơ chế Hitbox & Hurtbox chạy hoàn toàn trên Dedicated Server..."*
* **Q&A Phản biện:**
  * **Hỏi:** *Tại sao lại chạy va chạm Hitbox/Hurtbox trên Dedicated Server? Liệu có gây trễ (delay) đòn đánh cho người chơi không?*
  * **Trả lời:** *Dạ thưa thầy cô, việc chạy va chạm trên Server là bắt buộc...*

---

### Slide 21: Xây dựng hệ thống AI quái vật & Boss đa giai đoạn
* **Thời gian đề xuất:** 1 phút
* **Thông điệp cốt lõi:** Lập trình AI thông minh và cơ chế Boss đa giai đoạn linh hoạt.
* **Lời thoại đề xuất (Script):**
  > *"AI quái vật chạy hoàn toàn trên Dedicated Server bằng mô hình FSM..."*

---

### Slide 22: Triển khai cơ chế đồng bộ di chuyển & Zone Server
* **Thời gian đề xuất:** 1.5 phút
* **Thông điệp cốt lõi:** Giải quyết triệt để bài toán đồng bộ mạng và giật lag (Rubber Banding).
* **Lời thoại đề xuất (Script):**
  > *"Đây là một trong những phần kỹ thuật phức tạp nhất của đồ án. Để đồng bộ di chuyển thời gian thực, em áp dụng cơ chế Client-prediction & Server Reconciliation..."*
* **Q&A Phản biện:**
  * **Hỏi:** *Hãy phân biệt Client-prediction và Server Reconciliation?*
  * **Trả lời:** *Dạ thưa thầy cô...*

---

### Slide 23 - 28: Kết quả thực hiện (Giao diện trực quan)
* **Thời gian đề xuất:** 1.5 phút (cho cả chuỗi slide giao diện)
* **Thông điệp cốt lõi:** Minh chứng sản phẩm hoàn thiện, hoạt động thực tế với giao diện đẹp mắt, chuyên nghiệp.
* **Lời thoại đề xuất (Script):**
  > *"Sau đây, em xin phép giới thiệu sản phẩm thực tế của trò chơi... Slide 23: đăng ký, sảnh chính... Slide 24 & 25: nâng cấp, dung hợp Gene... Slide 26: chỉ số nhân vật... Slide 27: dung hợp Gene... Slide 28: chiến đấu phó bản..."*

---

### Slide 29: Chương 04 - Thực nghiệm & Đánh giá hiệu năng
* **Thời gian đề xuất:** 10 giây
* **Thông điệp cốt lõi:** Bắt đầu phần đánh giá số liệu thực tế.
* **Lời thoại đề xuất (Script):**
  > *"Cuối cùng, em xin trình bày Chương 4 - Thực nghiệm và Đánh giá hiệu năng..."*

---

### Slide 30: Đánh giá thực nghiệm
* **Thời gian đề xuất:** 1.5 phút
* **Thông điệp cốt lõi:** Chứng minh hệ thống đạt tiêu chuẩn kỹ thuật về độ trễ, FPS và khả năng bảo mật thực tế.
* **Lời thoại đề xuất (Script):**
  > *"Em đã tiến hành thực nghiệm hệ thống trong điều kiện mạng thực tế... Độ trễ... Tải... Bảo mật chống Cheat Engine..."*
* **Q&A Phản biện:**
  * **Hỏi:** *Cụ thể Dedicated Server phát hiện và từ chối hack HP/Vàng bằng Cheat Engine như thế nào?*
  * **Trả lời:** *Dạ thưa thầy cô, đối với lượng HP...*

---

### Slide 31: Định hướng phát triển
* **Thời gian đề xuất:** 30 giây
* **Thông điệp cốt lõi:** Tầm nhìn mở rộng và nâng cấp sản phẩm.
* **Lời thoại đề xuất (Script):**
  > *"Về định hướng phát triển trong tương lai, em mong muốn: 1. Chống hack nâng cao... 2. PvP đấu trường... 3. Protocol Buffers..."*

---

### Slide 32: Slide Kết Thúc & Cảm Ơn
* **Thời gian đề xuất:** 30 giây
* **Thông điệp cốt lõi:** Lời cảm ơn trang trọng và sẵn sàng nhận câu hỏi.
* **Lời thoại đề xuất (Script):**
  > *"Trên đây là toàn bộ nội dung trình bày đồ án tốt nghiệp của em. Em xin chân thành cảm ơn thầy cô trong Hội đồng đã dành thời gian lắng nghe. Em rất mong nhận được những câu hỏi và ý kiến đóng góp từ quý thầy cô để đồ án được hoàn thiện hơn. Em xin chân thành cảm ơn!"*

---

## 🛠️ Bộ Câu Hỏi Phản Biện Kỹ Thuật Dự Phòng (Cực Kỳ Quan Trọng)

### 1. Offline JWT Validation là gì? Tại sao Dedicated Server kiểm tra được JWT mà không cần gọi API?
* **Cách trả lời:** *"Dạ thưa thầy cô, khi ASP.NET Core API tạo ra JWT, nó ký mã hóa token này bằng một Secret Key bí mật (thuật toán HS256). Dedicated Server và API cùng chia sẻ Secret Key này thông qua cấu hình môi trường an toàn. Khi Client gửi JWT lên Dedicated Server trong payload kết nối Netcode, Dedicated Server sẽ tự chạy thuật toán giải mã và kiểm tra chữ ký số của token bằng Secret Key đó ngay trên bộ nhớ cục bộ (Offline Validation). Nếu chữ ký khớp và thời hạn token còn hiệu lực, Server chấp nhận kết nối. Cách này giúp loại bỏ hoàn toàn việc Dedicated Server phải gọi một HTTP request qua API chỉ để xác thực token, giúp giảm tối đa độ trễ mạng khi người chơi kết nối vào game."*

### 2. Em phân chia Zone-based Server như thế nào để tối ưu băng thông?
* **Cách trả lời:** *"Dạ, em triển khai một lớp quản lý có tên là `ZoneRoomRegistry`. Bản đồ thế giới game được chia thành nhiều phòng (Room/Zone) độc lập. Khi một người chơi di chuyển vào phân vùng cụ thể, Server sẽ đưa `clientId` của họ vào danh sách quản lý của Zone đó. Netcode sử dụng bộ lọc hiển thị mạng (Network Visibility Filter) để cấu hình: Server chỉ gửi thông tin di chuyển, hành động của các Network Object trong cùng Zone đó xuống cho người chơi. Người chơi ở Zone A sẽ hoàn toàn không nhận các gói tin di chuyển của người chơi ở Zone B, giúp tiết kiệm băng thông mạng và giảm tải xử lý render ở client."*

### 3. Tại sao lại dùng Hybrid Authentication (xác thực kép)?
* **Cách trả lời:** *"Dạ, hệ thống của em cần bảo mật hai luồng giao tiếp khác nhau:
  * **Luồng Client kết nối trực tiếp đến API**: Sử dụng **JWT Bearer Token** đại diện cho định danh của người chơi (User Identity).
  * **Luồng Dedicated Server giao tiếp trực tiếp đến API (Server-to-Server)**: Sử dụng **Zone API Key** đính kèm trong header `X-Zone-Api-Key`. Việc này giúp Dedicated Server có thể tải nhanh dữ liệu nhân vật hoặc đồng bộ trạng thái ngắt kết nối mà không cần phải đăng nhập lấy JWT của từng người chơi, đồng thời ngăn chặn việc giả mạo request API từ bên ngoài vì API Key này được lưu bảo mật trên máy chủ."*

### 4. Phần Netcode for GameObjects (NGO) này là em tự viết hay có sẵn? Cách sử dụng trong đồ án như thế nào?
* **Cách trả lời:** *"Dạ thưa thầy cô, **Netcode for GameObjects (NGO) là framework mạng chính thức được cung cấp sẵn bởi Unity**. Việc tự phát triển một networking engine từ đầu (với các tính năng như socket transmission, serialization, replication, RPC) là cực kỳ phức tạp và không nằm trong phạm vi mục tiêu của đồ án này.
  Tuy nhiên, **toàn bộ phần tích hợp, thiết kế kiến trúc game chơi mạng và viết kịch bản đồng bộ cụ thể đều do em tự thực hiện**:
  * Em thiết kế luồng **Server-Authoritative** để chống hack, trong đó Server kiểm soát tuyệt đối lượng HP, sát thương, trạng thái quái vật.
  * Em tự lập trình các kịch bản RPC (như `TakeDamageServerRpc` hay `HealServerRpc`) để client gửi yêu cầu lên server và `ClientRpc` để phát các hiệu ứng hạt/âm thanh từ server xuống client.
  * Em cấu hình và điều khiển các `NetworkVariable` để đồng bộ thuộc tính người chơi tự động qua mạng.
  * Em viết code tích hợp xác thực JWT tại hàm callback phê duyệt kết nối (`ConnectionApprovalCallback`) khi client kết nối vào server game."*

### 5. Sự khác biệt giữa ServerRpc và ClientRpc trong Unity NGO? Cho ví dụ thực tế trong code của đồ án.
* **Cách trả lời:** *"Dạ thưa thầy cô:
  * **ServerRpc (Server Remote Procedure Call)**: Là hàm được gọi từ phía Client nhưng được thực thi trên Server. Nó dùng để truyền các lệnh hay dữ liệu đầu vào của người chơi lên server để xử lý và xác thực bảo mật (ví dụ: yêu cầu dùng skill, yêu cầu hồi máu). Hàm này bắt buộc phải có hậu tố `ServerRpc` và thuộc tính `[ServerRpc]`.
  * **ClientRpc (Client Remote Procedure Call)**: Là hàm được gọi từ phía Server nhưng thực thi trên toàn bộ (hoặc một nhóm) Client đang kết nối. Nó thường dùng để đồng bộ các hiệu ứng không ảnh hưởng đến logic game như âm thanh, hoạt ảnh, hoặc hiệu ứng đồ họa. Hàm này bắt buộc có hậu tố `ClientRpc` và thuộc tính `[ClientRpc]`.
  * **Ví dụ thực tế**: Trong file `NetworkPlayerHealth.cs`, khi người chơi nhận sát thương, Client gọi `TakeDamageServerRpc(damage)` lên Server. Server thực thi trừ máu trên server và gọi `OnTakeDamageClientRpc(damage)` để truyền tin cho các client khác phát âm thanh rên rỉ/mất máu và hiển thị text sát thương."*

### 6. NetworkVariable là gì? Tại sao dùng nó thay vì dùng ClientRpc để cập nhật lượng HP của nhân vật?
* **Cách trả lời:** *"Dạ thưa thầy cô, `NetworkVariable` là một wrapper kiểu dữ liệu của Unity Netcode, tự động đồng bộ giá trị của nó từ Server xuống tất cả các Client khi có sự thay đổi.
  Em chọn dùng `NetworkVariable` cho HP thay vì RPC vì 2 lý do lớn:
  * **Hỗ trợ người chơi vào sau (Late-Joiners)**: Nếu dùng RPC để đồng bộ máu, một client kết nối vào game sau khi trận đấu đã diễn ra sẽ không nhận được các sự kiện RPC cũ, dẫn đến hiển thị sai thanh máu của các nhân vật khác. Với `NetworkVariable`, giá trị hiện tại trên server sẽ được tự động gửi và đồng bộ chính xác cho client ngay khi họ kết nối thành công.
  * **Phân quyền bảo mật**: Em cấu hình `NetworkVariable` với quyền ghi thuộc về Server (`WritePermission.Server`) và quyền đọc cho tất cả mọi người (`ReadPermission.Everyone`), đảm bảo client không thể tự ý hack thay đổi giá trị máu trực tiếp dưới client."*

### 7. Đồng bộ di chuyển của nhân vật qua mạng được thực hiện như thế nào để tránh giật lag (Rubber Banding)?
* **Cách trả lời:** *"Dạ, em kết hợp hai cơ chế để tối ưu di chuyển qua mạng:
  * **Client-Prediction (Dự đoán Client)**: Khi người chơi bấm phím di chuyển, Client sẽ lập tức tính toán vật lý và di chuyển nhân vật cục bộ để người chơi cảm nhận sự mượt màng tức thì (không phải đợi gói tin phản hồi từ server gửi về).
  * **Server Reconciliation & NetworkTransform**: Đồng thời Client gửi input lên Server. Server mô phỏng lại di chuyển đó. Vị trí thực tế của người chơi sẽ được server kiểm soát và đồng bộ liên tục xuống Client qua component `NetworkTransform`. Nếu khoảng cách giữa vị trí dự đoán ở Client và vị trí thực ở Server lệch nhau quá lớn (vượt ngưỡng sai số do lag mạng hoặc hack), Client sẽ thực hiện hòa giải bằng cách nội suy (interpolate) mượt mà kéo nhân vật về vị trí chuẩn của Server chứ không giật giật đột ngột."*

### 8. Làm thế nào Dedicated Server (Unity Headless Build) có thể vận hành mà không cần giao diện đồ họa hay GPU trên máy chủ VPS?
* **Cách trả lời:** *"Dạ thưa thầy cô, khi build game server, em chọn mục tiêu build là **Dedicated Server** với chế độ **Headless Mode** của Unity.
  * Trong chế độ này, Unity tắt toàn bộ hệ thống render đồ họa (Graphics API không được khởi tạo) và không tải các texture, mesh hay tài nguyên đồ họa nặng lên RAM.
  * Toàn bộ phần render camera, render ảnh động UI đều bị vô hiệu hóa, server chỉ chạy các dòng code logic, tính toán va chạm vật lý (2D Physics Engine) và trao đổi dữ liệu mạng qua socket.
  * Nhờ vậy, server chạy cực kỳ nhẹ, tiết kiệm hơn 90% CPU và RAM, có thể dễ dàng chạy trên các máy chủ ảo VPS Linux/Windows giá rẻ chỉ có CPU mà không cần card màn hình (GPU)."*

### 9. Cơ chế Ground Detection (phát hiện chạm đất) cho nhân vật 2D Platformer trong Unity được xử lý ra sao? Tại sao không dùng Raycast thông thường?
* **Cách trả lời:** *"Dạ thưa thầy cô, thay vì dùng một tia `Physics2D.Raycast` đơn lẻ bắn thẳng từ chân nhân vật xuống dưới, em sử dụng hàm `Physics2D.OverlapCircle` quét một vùng hình tròn nhỏ dưới chân nhân vật.
  * **Lý do**: Nếu chỉ dùng một tia Raycast đơn, khi nhân vật đứng ở sát mép của collider mặt đất (edge platform), tia Raycast sẽ bị bắn lệch ra ngoài không gian trống làm game hiểu lầm là nhân vật đang rơi và không cho phép nhảy.
  * Sử dụng `OverlapCircle` quét cả một vùng diện tích hình tròn giúp phát hiện mặt đất ổn định hơn khi nhân vật đứng mấp mé ở các cạnh, loại bỏ hiện tượng bị hụt nhảy hoặc hoạt ảnh nhảy bị giật cục."*

### 10. Làm thế nào để Server và Client đồng bộ được các Prefab (Player, Enemy, Skills) when sinh ra (spawn) trong lúc chơi?
* **Cách trả lời:** *"Dạ, để các Prefab có thể spawn đồng bộ qua mạng, em thực hiện các bước:
  1. Gắn component `NetworkObject` lên các Prefab đó (như Player Prefab, Enemy Prefab, Projectile kỹ năng).
  2. Đăng ký các Prefab này vào danh sách **Network Prefabs List** của `NetworkManager` trong Unity (hoặc qua file asset `DefaultNetworkPrefabs`).
  3. Khi chơi game, thay vì dùng `Instantiate` thông thường, Server sẽ gọi hàm `Instantiate` cục bộ rồi gọi tiếp phương thức `NetworkObject.Spawn()`. Lúc này, Unity Netcode sẽ tự động gửi gói tin mạng chứa ID của Prefab đó xuống các client để các client tự spawn và đồng bộ hóa thuộc tính của đối tượng tương ứng."*

### 11. Thuật toán FSM (Finite State Machine) của quái vật chạy trên Server có gây nghẽn CPU không? Em tối ưu hóa nó như thế nào?
* **Cách trả lời:** *"Dạ thưa thầy cô, nếu hàng chục quái vật cùng liên tục quét khoảng cách tìm người chơi trong hàm `Update()` chạy mỗi frame (60 lần/giây) thì server sẽ bị quá tải CPU rất nhanh.
  Để tối ưu hóa, em đã áp dụng các kỹ thuật sau:
  * **Giảm tần suất quét**: Em đưa logic quét khoảng cách và chuyển đổi trạng thái AI vào một **Co-routine** chạy lặp lại định kỳ (ví dụ 0.2 giây một lần - tương đương 5Hz) thay vì chạy liên tục mỗi frame. Tốc độ phản hồi 5Hz là đủ nhanh để AI xử lý mà giúp giảm tải CPU đi hơn 12 lần.
  * **Quản lý trạng thái ngủ đông**: Khi một Zone/Bản đồ không có bất kỳ người chơi nào hoạt động, Server sẽ tạm ngưng (deactivate) các script AI của quái vật ở khu vực đó, không cho chạy cập nhật FSM nữa nhằm bảo toàn tài nguyên cho các khu vực có người chơi."*

---

> [!TIP]
> **Lời khuyên cuối cùng:** Hãy mở sẵn mã nguồn của các lớp quan trọng như `ZoneConnectionApproval.cs`, `GameplayCommandService.cs`, và `NetworkPlayerController.cs` trên máy tính phòng trường hợp Hội đồng yêu cầu xem mã nguồn thực tế để chứng minh tính tự thực hiện của đồ án. Chúc bạn có một buổi bảo vệ thành công rực rỡ!