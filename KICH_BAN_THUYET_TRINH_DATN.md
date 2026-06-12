# KỊCH BẢN THUYẾT TRÌNH (Từng Slide - Tổng thời lượng: 15 Phút)

> **Lưu ý về nhịp độ:** 
> - **Slide 1 - 24 (Khoảng 10 phút):** Tốc độ nói vừa phải, tập trung nhấn mạnh vào các giải pháp kĩ thuật (Slide 13, 16, 18, 19).
> - **Slide 25 - 37 (Khoảng 5 phút):** Nói nhanh hơn, tập trung giải thích các thao tác và hiệu ứng đang diễn ra trên màn hình video demo.

---

## PHẦN 1: NỘI DUNG CHÍNH (Khoảng 10 Phút)

**Slide 1: Tiêu đề (20s)**
"Kính thưa Hội đồng bảo vệ đồ án tốt nghiệp, cùng toàn thể các bạn sinh viên. Em là Trần Văn Thủy. Hôm nay, em xin trình bày đồ án tốt nghiệp với đề tài: 'Phát triển trò chơi Mutants Arena với hệ thống tiến hóa Gene bằng Unity', dưới sự hướng dẫn của Tiến sĩ Nguyễn Đức Hiếu. Sau đây em xin bắt đầu."

**Slide 2: Đặt vấn đề (30s)**
"Thưa thầy cô, thị trường hiện nay có nhiều game nhập vai, nhưng đa số giới hạn người chơi ở các lớp nhân vật (Class) cố định từ đầu. Cùng với đó, việc phát triển một game đa người chơi mượt mà và chống gian lận là một bài toán khó. Từ đó, em chọn đề tài này để tạo ra một tựa game có cơ chế tiến hóa nhân vật tự do, trên nền tảng kiến trúc mạng tối ưu và bảo mật."

**Slide 3: Nội dung tổng quan (15s)**
"Bài báo cáo của em gồm 4 phần: Tổng quan đề tài, Phân tích thiết kế, Triển khai hệ thống cơ chế, và cuối cùng là Kết quả thực nghiệm."

**Slide 4: Chương 01 (5s)**
"Đầu tiên, em xin giới thiệu tổng quan về đề tài."

**Slide 5: Game 2D Action RPG (20s)**
"Mutants Arena là một tựa game 2D Action RPG. Điểm nổi bật là sự kết hợp giữa cơ chế điều khiển chặt chém thời gian thực trên bản đồ platformer đa tầng, cùng với chiều sâu phát triển nhân vật qua chỉ số, trang bị và kỹ năng."

**Slide 6: Mục tiêu đề tài (15s)**
"Mục tiêu của em là xây dựng hoàn thiện một vòng lặp game đầy đủ từ máy khách (Client) đến máy chủ (Server), đảm bảo hệ thống có khả năng mở rộng."

**Slide 7: Đối tượng & Phạm vi (15s)**
"Phạm vi sản phẩm tập trung vào luồng chơi cốt lõi, phục vụ những người chơi yêu thích dòng game hành động nhịp độ nhanh và có tư duy tùy biến lối chơi."

**Slide 8: Gameplay & Vòng lặp (20s)**
"Vòng lặp trò chơi xoay quanh việc người chơi tham gia chiến đấu, thu thập tài nguyên từ quái vật và phó bản, sau đó dùng tài nguyên để nâng cấp sức mạnh, tiến hóa Gene và lặp lại quá trình với độ khó cao hơn."

**Slide 9: Kiến trúc mạng (25s)**
"Để đáp ứng multiplayer, hệ thống sử dụng mô hình Client-Server. Unity đóng vai trò Client, kết nối trực tiếp với ASP.NET Core Game Server thông qua giao thức mạng thời gian thực, đảm bảo sự đồng bộ trơn tru giữa người chơi."

**Slide 10: Chương 02 (5s)**
"Tiếp theo là phần Phân tích và Thiết kế hệ thống."

**Slide 11: Mô tả hệ thống (15s)**
"Hệ thống máy chủ được thiết kế để xử lý toàn bộ logic nghiệp vụ, từ đăng nhập, xác thực đến quản lý trận đấu, nhằm bảo vệ dữ liệu."

**Slide 12: Biểu đồ Use Case (20s)**
"Như trên biểu đồ Use Case, người chơi có thể tương tác với nhiều luồng chức năng như Quản lý túi đồ, Nâng cấp Gene, Chiến đấu và Giao tiếp. Server sẽ đóng vai trò xác thực mọi hành động này."

**Slide 13: Cơ sở dữ liệu & JSON Column (45s)**
"Về cơ sở dữ liệu vật lý, đây là điểm em tập trung tối ưu. Thay vì thiết kế truyền thống với quá nhiều bảng liên kết 1-nhiều cho dữ liệu linh hoạt, em đã ứng dụng JSON Column trực tiếp trong CSDL SQL. Các dữ liệu có cấu trúc động như: danh sách chỉ số phụ ngẫu nhiên của trang bị, hay tiến độ nhiệm vụ... đều được lưu dưới dạng JSON. Nhờ đó, em giảm thiểu tối đa các truy vấn JOIN phức tạp, giúp tăng tốc độ đọc/ghi dữ liệu nhân vật đáng kể."

**Slide 14: Hình ảnh ERD (10s)**
"Đây là sơ đồ thực thể ERD tổng quát của toàn hệ thống thể hiện các bảng dữ liệu cốt lõi."

**Slide 15: Chương 03 (5s)**
"Em xin đi vào chi tiết xây dựng các cơ chế Game ở Chương 3."

**Slide 16: Điều khiển & Di chuyển (40s)**
"Để mang lại cảm giác điều khiển mượt mà nhất, em đã đặt thời gian chuyển đổi hoạt ảnh (Transition) gần như bằng 0. Bên cạnh đó, em tự lập trình các cơ chế hỗ trợ phản xạ như: Coyote Time cho phép nhảy khi vừa trượt khỏi mép vực, Jump Buffer lưu lệnh nhảy trước khi chạm đất, và Dash I-Frames cung cấp 0.2 giây bất tử khi lướt nhanh để né chiêu."

**Slide 17: AI Quái vật FSM (25s)**
"AI của quái vật được mô hình hóa thành Máy trạng thái hữu hạn (FSM). Quái vật có các trạng thái rời rạc như: Tuần tra, Truy đuổi, Tấn công và Chết, giúp tiết kiệm tài nguyên tính toán của Server."

**Slide 18: Server-Authoritative & Zone (45s)**
"Kiến trúc Server-Authoritative là cốt lõi bảo mật của game. Server là nguồn sự thật duy nhất tính toán va chạm và máu. Để chống độ trễ (delay) khi di chuyển, em áp dụng cơ chế Client-prediction: Client dự đoán trước kết quả di chuyển để khử độ trễ mạng. Ngoài ra, bản đồ được chia thành các Zone độc lập, Server chỉ đồng bộ dữ liệu giữa các người chơi đứng cùng Zone, giúp tiết kiệm băng thông tối đa."

**Slide 19: Tiến hóa Gene & Dung hợp (40s)**
"Hệ thống Tiến hóa Gene là tính năng đặc sắc nhất. Thay vì chọn Class lúc đầu, người chơi tự do phát triển thông qua việc cộng điểm vào các nhánh Gene thuộc tính. Khi đạt cấp tối đa ở hai nguyên tố khác nhau, người chơi có thể Dung hợp (Hybrid Fusion) để sinh ra lớp nhân vật lai mới, và thậm chí đạt Gene tối thượng kèm theo hiệu ứng hào quang đặc biệt."

**Slide 20: Chiến đấu & Ngũ hành (35s)**
"Về chiến đấu, vòng tương khắc Ngũ hành (Kim - Mộc - Thủy - Hỏa - Thổ) được áp dụng để tính toán sát thương với hệ số tăng giảm rõ rệt. Em cũng đưa vào hệ thứ 6 là hệ Phong đóng vai trò trung lập, không bị khắc chế, giúp tăng tính đa dạng chiến thuật."

**Slide 21: Chương 04 (5s)**
"Cuối cùng là phần Thực nghiệm và Đánh giá."

**Slide 22 & 23: Thực nghiệm độ trễ, FPS và Bảo mật (40s)**
"Qua thực nghiệm, trong điều kiện Ping từ 40-90ms, tính năng Client-prediction hoạt động rất ổn định. Game đạt 60 FPS trên máy tầm trung và Server quản lý CPU tốt. Về bảo mật, em đã dùng phần mềm Cheat Engine thay đổi bộ nhớ Client để hack Máu và Vàng, nhưng Dedicated Server lập tức phát hiện và từ chối ghi nhận, chứng minh hệ thống chống gian lận hoạt động hiệu quả."

**Slide 24: Hạn chế & Định hướng (30s)**
"Mặc dù đã hoàn thiện các cơ chế cốt lõi, đồ án vẫn còn một số hạn chế. Thứ nhất, các đánh giá định lượng (như RTT, FPS, tải CPU/RAM) chưa được đo đạc bằng công cụ chuẩn hóa độc lập. Thứ hai, dự án cần bổ sung kiểm thử tự động và kiểm thử tải. Thứ ba, giao diện và cân bằng game cần tinh chỉnh thêm. 
Từ đó, định hướng tiếp theo của em là bổ sung các tính năng nâng cao như Marketplace, Ranked PvP, Dashboard quản trị; đồng thời xây dựng hệ thống đo lường hiệu năng chuyên sâu hơn."

---

## PHẦN 2: KẾT QUẢ THỰC NGHIỆM - VIDEO & GIAO DIỆN (Khoảng 5 Phút)

*(Lưu ý: Thao tác chuyển slide nhịp nhàng, tay hoặc con trỏ chuột chỉ vào điểm chú ý trên màn hình)*

**Slide 25 -> 30: Giao diện nhân vật & Skill (45s tổng cộng)**
*(Chuyển chậm rãi từng slide trong nhóm này)*
"Sau đây là kết quả thực tế. Trên màn hình là giao diện tổng quan của các nhân vật. Quý thầy cô có thể thấy bộ kỹ năng và hiệu ứng chiêu thức được thiết kế riêng biệt cho từng hệ thuộc tính, màu sắc hiệu ứng thể hiện rõ ràng yếu tố Ngũ hành."

**Slide 31: Video Multiplayer (40s)**
"Đây là video đồng bộ Multiplayer. Thầy cô có thể thấy hai cửa sổ Client khác nhau. Khi một nhân vật di chuyển, đánh quái hay tung chiêu thức, mọi hành động ngay lập tức được truyền về Server và đồng bộ sang Client còn lại với độ trễ gần như không thể nhận ra, đảm bảo tính công bằng."

**Slide 32: Video Tiến hóa Gene (40s)**
"Video tiếp theo mô phỏng quá trình nâng cấp Gene. Khi có đủ tài nguyên, người chơi tương tác với bảng kỹ năng để cộng điểm tiến hóa. Ở mốc dung hợp cực hạn, nhân vật xuất hiện thêm hào quang (aura) phát sáng ở dưới chân, các chỉ số cơ bản cũng được cộng dồn theo thời gian thực."

**Slide 33: Video Chat (20s)**
"Hệ thống Chat kênh thế giới và khu vực cũng được vận hành ổn định. Các tin nhắn đi qua Server đều được xác thực định danh (JWT) trước khi broadcast cho các người chơi khác."

**Slide 34: Video Nhiệm vụ (30s)**
"Video này minh họa luồng thực hiện nhiệm vụ. Trạng thái nhiệm vụ như số lượng quái cần diệt được lưu trữ trong JSON ở Server. Người chơi nhận nhiệm vụ, đi đánh quái, bộ đếm sẽ đồng bộ và cuối cùng trả nhiệm vụ để nhận vật phẩm phần thưởng."

**Slide 35: Video Đăng nhập (20s)**
"Đây là luồng Đăng nhập và Đăng ký. Dữ liệu mật khẩu đều được băm an toàn. Sau khi đăng nhập thành công, Server trả về Token giúp kết nối người chơi vào thẳng thế giới ảo."

**Slide 36: Video Inventory & Phó bản (40s)**
"Cuối cùng, người chơi có thể sử dụng bình máu, mặc trang bị qua kho đồ. Cùng với đó là việc tham gia một phó bản (Dungeon). Server khởi tạo một Zone riêng rẽ (Instance) cho phó bản này. Người chơi tiêu diệt quái, nhặt vật phẩm rơi ra từ máy chủ tính toán, và hoàn thành phó bản."

**Slide 37: Lời cảm ơn (10s)**
"Trên đây là toàn bộ kết quả của đồ án. Em xin chân thành cảm ơn quý thầy cô đã chú ý lắng nghe. Em rất mong nhận được những góp ý từ Hội đồng để hoàn thiện hơn ạ."
