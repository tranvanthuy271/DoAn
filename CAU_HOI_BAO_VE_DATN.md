# TỔNG HỢP 50 CÂU HỎI BẢO VỆ ĐỒ ÁN TỐT NGHIỆP
**Dự án:** Mutants Arena - Game 2D Action RPG với hệ thống tiến hóa Gene
**Dựa trên:** Kịch bản thuyết trình & Slide ĐATN

> [!TIP]
> Dưới đây là danh sách các câu hỏi được phân loại theo từng nhóm kiến thức. Bạn hãy chuẩn bị câu trả lời ngắn gọn, đi thẳng vào vấn đề (từ 3-5 câu cho mỗi câu hỏi) và bám sát vào những gì đã xây dựng thực tế trong code.

---

## 1. Kiến trúc Hệ thống & Mạng (Networking & Architecture)
*(Trọng tâm vào Slide 9, 11, 18)*

1. Tại sao em chọn mô hình Server-Authoritative thay vì Peer-to-Peer (P2P) hay Listen Server cho game này?
2. Trong ASP.NET Core Game Server, giao thức mạng thời gian thực nào được sử dụng để duy trì kết nối? Tại sao lại chọn nó?
3. Tại sao Unity Netcode for GameObjects (NGO) được lựa chọn để xử lý phía Client mà không phải là Photon (PUN) hay Mirror?
4. Giải thích chi tiết cơ chế chia Zone độc lập trên server. Điều này giúp ích gì cho hiệu năng và băng thông mạng?
5. Em giải thích cơ chế Client-prediction hoạt động như thế nào trong game của em để khử độ trễ (Lag compensation)?
6. Nếu xảy ra trường hợp mất đồng bộ (desync) vị trí giữa Client tự dự đoán và Server tính toán trả về, hệ thống xử lý (Server Reconciliation) bằng cách nào?
7. Làm thế nào em đảm bảo tính công bằng (fairness) giữa những người chơi có mức Ping khác nhau (VD: 40ms vs 200ms)?
8. Việc tách biệt Client và Dedicated Server mang lại ưu điểm gì trong việc cập nhật game (Update/Patch) sau này?
9. Cơ chế đóng gói và truyền tải dữ liệu (Serialization) giữa Client và Server sử dụng định dạng gì? Tại sao?
10. Đối với việc đồng bộ vị trí nhân vật (Transform), giao thức UDP hay TCP là phù hợp nhất? Hệ thống của em đang dùng gì?

## 2. Cơ sở dữ liệu & Tối ưu hóa (Database & Optimization)
*(Trọng tâm vào Slide 13, 14)*

11. Em sử dụng hệ quản trị CSDL quan hệ (RDBMS) nào cho dự án này?
12. Tại sao em quyết định lưu trữ dữ liệu dưới dạng JSON Column trong CSDL SQL thay vì sử dụng một CSDL NoSQL thuần túy (như MongoDB)?
13. Việc lưu dữ liệu động vào JSON Column mang lại những nhược điểm hoặc giới hạn gì so với việc chuẩn hóa CSDL (Normalization) thông thường?
14. Quá trình Parse (phân tích) đọc/ghi chuỗi JSON từ CSDL lên Game Server có gây ra hiện tượng nghẽn cổ chai (Bottleneck) về CPU không? Em xử lý ra sao?
15. Dựa vào ERD, hãy cho biết những loại dữ liệu nào bắt buộc phải lưu ở các cột riêng biệt (để đánh Index) thay vì gộp chung vào cột JSON?
16. Server xử lý vấn đề tranh chấp dữ liệu (Race conditions/Concurrency) như thế nào khi có nhiều luồng cùng ghi vào một dữ liệu người chơi?
17. Việc lưu trữ Túi đồ (Inventory) trên CSDL được thiết kế ra sao để vừa linh hoạt lưu các chỉ số ngẫu nhiên của trang bị, vừa truy vấn nhanh?
18. Em có xem xét hoặc áp dụng kỹ thuật Caching (như Redis) ở phía Server để giảm tải số lượng truy vấn đọc vào CSDL chưa?
19. Làm sao để lưu lại tiến trình trạng thái nhiệm vụ phức tạp (ví dụ: đánh 5/10 con quái) một cách tối ưu nhất?
20. Nếu số lượng người chơi kết nối đồng thời (CCU) tăng lên gấp 10 lần, phần CSDL của em có khả năng mở rộng (Scale) như thế nào?

## 3. Gameplay, Điều khiển & AI (Gameplay, Controls & AI)
*(Trọng tâm vào Slide 16, 17, 20)*

21. Em tự lập trình cơ chế Coyote Time và Jump Buffer như thế nào trong Unity? Giải thích logic toán học hoặc bộ đếm thời gian của nó.
22. Khung thời gian bất tử (Dash I-Frames) được kiểm soát tính toán ở Client hay Server? Làm sao để không bị lạm dụng (Spam)?
23. Hãy giải thích chi tiết về Máy trạng thái hữu hạn (FSM) em áp dụng cho AI quái vật. Tại sao không dùng Behaviour Tree?
24. Quái vật tìm đường (Pathfinding) tiếp cận người chơi trên bản đồ Platformer như thế nào? Có dùng NavMesh hay A* không?
25. Nếu trong một Zone có 100 quái vật cùng hoạt động, Game Server sẽ xử lý AI của chúng ra sao để không bị quá tải CPU (Tick rate drop)?
26. Vòng tương khắc Ngũ hành (Kim-Mộc-Thủy-Hỏa-Thổ) được áp dụng vào công thức tính sát thương (Damage Calculation) cụ thể ra sao?
27. Hệ thứ 6 (Hệ Phong) đóng vai trò trung lập. Việc đưa hệ này vào có làm phá vỡ tính cân bằng của 5 hệ kia không? Ưu điểm của hệ này là gì?
28. Cơ chế tính toán va chạm (Collision Detection) cho các đòn đánh (Hitbox/Hurtbox) được thực hiện ở đâu? Server hay Client?
29. Làm thế nào để em đồng bộ trạng thái Hoạt ảnh (Animation) đánh nhau một cách mượt mà và chính xác thời gian giữa các Client?
30. Việc sinh ra vật phẩm rơi (Loot/Drop) từ quái vật được Server quyết định theo cơ chế ngẫu nhiên (RNG) như thế nào để Client không thể đoán trước hoặc hack?

## 4. Hệ thống Tiến hóa Gene & Nhân vật (Core Feature)
*(Trọng tâm vào Slide 19, 32)*

31. Hệ thống "Tiến hóa Gene" của em có điểm gì khác biệt cốt lõi và mang lại giá trị chơi lại cao hơn so với hệ thống "Cây kỹ năng" (Skill Tree) truyền thống?
32. Điều kiện chính xác về mặt logic/dữ liệu để kích hoạt được tính năng "Dung hợp" (Hybrid Fusion) là gì?
33. Cấu trúc dữ liệu nào trong code được sử dụng để quản lý và kiểm tra điều kiện của từng nhánh tiến hóa Gene?
34. Người chơi có thể tẩy điểm (Reset) tiến hóa Gene không? Nếu có, hệ thống xử lý việc trừ lại các chỉ số đã cộng như thế nào để không bị sai lệch?
35. Khi nhân vật đạt "Gene tối thượng" và có hiệu ứng hào quang, quá trình Server thông báo (Broadcast) cờ trạng thái này cho các người chơi khác trong cùng Map diễn ra như thế nào?
36. Khi người chơi đang trong quá trình Dung hợp (có hoạt ảnh kéo dài), làm sao để đảm bảo nhân vật không bị quái vật đánh chết hoặc ngắt quãng?
37. Bài toán khó nhất trong việc thiết kế và lập trình kiến trúc cho hàng chục loại kỹ năng/Gene khác nhau là gì (ví dụ: dùng mô hình Component-based, Strategy Pattern...)?

## 5. Bảo mật & Chống gian lận (Security & Anti-Cheat)
*(Trọng tâm vào Slide 22, 23, 35)*

38. Em đã trình bày về việc dùng Cheat Engine để hack máu. Chi tiết cơ chế từ chối của Server diễn ra như thế nào khi nhận được một gói tin báo máu sai từ Client?
39. Nếu Hacker sử dụng phần mềm can thiệp gói tin (Packet Editor) để gửi liên tục lệnh "Nhận vật phẩm" lên Server, hệ thống của em phòng chống bằng cách nào?
40. Tại sao em dùng JWT (JSON Web Token) cho quá trình xác thực đăng nhập? Nhược điểm của JWT là gì (ví dụ: không thể thu hồi tức thời) và em có xử lý không?
41. Khi người chơi chuyển đổi giữa các Zone hoặc đi vào Phó bản, làm sao đảm bảo quá trình di chuyển dữ liệu nhân vật không bị nhân bản (Dupe Item)?
42. Mật khẩu người chơi lưu trong CSDL được bảo vệ bằng thuật toán băm (Hashing) nào? Có kết hợp với Salt động không?
43. Logic kiểm tra "Thời gian hồi chiêu" (Cooldown) của kỹ năng được đặt ở đâu? Nếu đặt ở Client thì làm sao chống hack giảm hồi chiêu (No Cooldown)?

## 6. Đánh giá thực nghiệm & Tương lai (Testing & Future Work)
*(Trọng tâm vào Slide 24)*

44. Trong slide có nhắc đến "máy tầm trung" chạy mượt 60 FPS. Cấu hình cụ thể mà em định nghĩa cho máy tầm trung ở đây là gì?
45. Tại sao các chỉ số như RTT (Round Trip Time), tải CPU/RAM của Server lại là những thước đo sống còn đối với thể loại game này?
46. Để bổ sung "Kiểm thử tự động và kiểm thử tải" trong tương lai, em dự định áp dụng công cụ hay framework nào (Ví dụ: NUnit, k6, xUnit...)?
47. Tính năng Marketplace (Chợ giao dịch) dự kiến sẽ tiềm ẩn những nguy cơ bảo mật, lỗi logic (Bugs/Exploits) nào lớn nhất mà em cần đề phòng?
48. Nếu phát triển chế độ Ranked PvP, yêu cầu về độ trễ và tính đồng bộ sẽ khắt khe hơn PvE rất nhiều. Kiến trúc mạng hiện tại cần nâng cấp thêm cơ chế gì (ví dụ: Lag Compensation Rollback)?
49. Trong toàn bộ quá trình phát triển dự án, vấn đề kỹ thuật nào làm em mất nhiều thời gian để debug và giải quyết nhất? Em đã giải quyết ra sao?
50. Nếu được chọn 1 class/đoạn code mà em tự hào nhất về tính tối ưu và cấu trúc trong project, đó là đoạn code xử lý phần nào? Tại sao?

---

> [!IMPORTANT]
> **Lời khuyên khi trả lời Hội đồng:**
> - Nếu gặp câu hỏi chưa từng làm, hãy thành thật: *"Dạ phần này hiện tại đồ án của em chưa cover tới, nhưng hướng giải quyết theo em tìm hiểu là..."*
> - Luôn nhấn mạnh vào việc **Server là nguồn chân lý** (Server-Authoritative) khi giải thích bất kỳ tính năng nào liên quan đến bảo mật hoặc xử lý logic quan trọng.
> - Nếu thầy cô hỏi sâu về một pattern (ví dụ State Machine, Singleton...), hãy mở trực tiếp file code tương ứng (nếu được phép) để giải thích cho trực quan.
