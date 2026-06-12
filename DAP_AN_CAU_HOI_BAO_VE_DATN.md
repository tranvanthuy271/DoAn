# ĐÁP ÁN THAM KHẢO CHO 50 CÂU HỎI BẢO VỆ ĐỒ ÁN
**Dự án:** Mutants Arena - Game 2D Action RPG
*(Lưu ý: Đây là sườn đáp án tham khảo, bạn cần điều chỉnh từ ngữ cho phù hợp với cách bạn đã code thực tế trong đồ án).*

---

## 1. Kiến trúc Hệ thống & Mạng

**1. Tại sao em chọn mô hình Server-Authoritative thay vì Peer-to-Peer (P2P) hay Listen Server?**
Mô hình Server-Authoritative (Server làm chủ) kiểm soát mọi logic quan trọng (máu, sát thương, di chuyển) giúp ngăn chặn triệt để gian lận. Với P2P hay Listen Server, Client tự tính toán dữ liệu của mình, rất dễ bị hack (như hack tốc độ, bất tử) và dễ bị mất kết nối nếu Host thoát game.

**2. Giao thức mạng thời gian thực nào được sử dụng trong ASP.NET Core?**
Dự án sử dụng UDP/TCP (thông qua Unity Netcode kết nối với Server) hoặc SignalR. UDP thường được dùng cho game hành động vì tốc độ truyền tải cực nhanh, chấp nhận mất một số gói tin nhỏ để đảm bảo thời gian thực (real-time).

**3. Tại sao chọn Unity Netcode for GameObjects (NGO) mà không phải Photon (PUN) hay Mirror?**
NGO là giải pháp mạng chính thức từ Unity, được hỗ trợ dài hạn và tích hợp sâu vào engine. Nó hỗ trợ rất tốt kiến trúc Client-Server với các tính năng như NetworkVariable và RPCs, tối ưu cho việc xây dựng Dedicated Server.

**4. Giải thích cơ chế chia Zone độc lập trên server?**
Server chia bản đồ thành các khu vực nhỏ (Zone/Room). Server chỉ đồng bộ thông tin (vị trí, hoạt ảnh) giữa những người chơi đứng trong cùng một Zone. Điều này giúp giảm đáng kể lượng băng thông mạng và tải CPU so với việc gửi dữ liệu cho toàn bộ máy chủ.

**5. Cơ chế Client-prediction hoạt động thế nào?**
Thay vì đợi Server xác nhận vị trí rồi mới vẽ lại hình ảnh, Client sẽ tự động mô phỏng việc di chuyển ngay khi người chơi bấm phím. Điều này giúp người chơi không cảm nhận được độ trễ (lag), mang lại trải nghiệm mượt mà dù Ping cao.

**6. Xử lý mất đồng bộ (Server Reconciliation) ra sao?**
Nếu Server phát hiện vị trí Client dự đoán bị sai (do kẹt tường, bị choáng), Server sẽ gửi vị trí chính xác về. Client buộc phải "roll-back" (lùi lại) đúng vị trí đó và cập nhật lại trạng thái.

**7. Đảm bảo công bằng giữa Ping 40ms và Ping 200ms?**
Việc tính toán va chạm (Hitbox) hoàn toàn dựa trên Server. Mọi người đều phải tuân theo sự thật duy nhất từ Server. Ngoài ra, có thể kết hợp cơ chế Lag Compensation (bù trừ độ trễ) để tính toán lùi thời gian cho các đòn đánh của người chơi có Ping cao.

**8. Ưu điểm của Dedicated Server khi cập nhật game?**
Tách biệt Server giúp em có thể cập nhật các bản vá (patch) sửa lỗi logic game, thay đổi thông số quái vật trực tiếp trên Server mà không cần bắt người chơi tải lại ứng dụng Client.

**9. Định dạng truyền tải dữ liệu (Serialization)?**
Dữ liệu mạng được mã hóa dưới dạng nhị phân (Binary). Nhị phân nhẹ hơn rất nhiều so với chuỗi văn bản (JSON/XML), giúp tiết kiệm băng thông và tăng tốc độ xử lý gói tin.

**10. UDP hay TCP cho đồng bộ vị trí?**
Đồng bộ Transform dùng UDP là tốt nhất. Vì gói tin vị trí được gửi liên tục (vd: 30 lần/giây), nếu rớt 1 gói tin UDP cũng không sao vì gói tin vị trí mới nhất sẽ đè lên vị trí cũ. TCP tốn thời gian xác nhận, dễ gây khựng (stuttering) trong game hành động.

---

## 2. Cơ sở dữ liệu & Tối ưu hóa

**11. Hệ quản trị CSDL quan hệ nào được sử dụng?**
*(Tùy dự án của bạn: SQL Server / MySQL / PostgreSQL)*.

**12. Tại sao kết hợp CSDL SQL và JSON Column?**
SQL đảm bảo tính toàn vẹn dữ liệu (ACID) cho tài khoản và giao dịch an toàn. Nhưng với các dữ liệu linh hoạt (như mảng túi đồ chứa các trang bị có chỉ số random), lưu dạng JSON Column giúp không phải tạo quá nhiều bảng liên kết, thiết kế schema nhanh chóng hơn.

**13. Nhược điểm của JSON Column?**
Rất khó và chậm khi muốn dùng câu lệnh SQL truy vấn sâu hoặc thống kê (ví dụ: tìm tất cả người chơi có trang bị "Kiếm lửa"). Không thể đánh Index hiệu quả như cột truyền thống.

**14. Bottleneck khi Parse JSON lên Server?**
Để tránh nặng CPU khi Deserialize JSON liên tục, Server chỉ đọc JSON từ DB đúng 1 lần khi người chơi Đăng nhập, đưa vào RAM (Memory) để thao tác. Khi Đăng xuất hoặc lưu định kỳ (Autosave) mới Serialize và ghi lại xuống DB.

**15. Dữ liệu nào bắt buộc dùng cột riêng biệt?**
ID, Username, PasswordHash, Level, Tiền Vàng. Đây là những cột cần đánh Index để tìm kiếm, sắp xếp và giao dịch nhanh chóng.

**16. Xử lý tranh chấp dữ liệu (Race conditions)?**
Sử dụng Transaction của SQL hoặc Optimistic Concurrency (Cơ chế kiểm soát đồng thời lạc quan - dùng cờ version/RowVersion) để đảm bảo hai luồng không thể cùng trừ tiền vào 1 thời điểm.

**17. Tối ưu Túi đồ (Inventory) trên CSDL?**
Toàn bộ túi đồ (gồm hàng chục món đồ) được nén thành 1 chuỗi JSON duy nhất lưu trong 1 cột `InventoryData`. Khi cần tải túi đồ, DB chỉ thực thi 1 câu lệnh SELECT đơn giản thay vì JOIN nhiều bảng với nhau.

**18. Kỹ thuật Caching trên Server?**
Sử dụng MemoryCache (hoặc Redis) trên RAM của Game Server. Mọi hành động nhặt đồ, tăng EXP đều thay đổi trong RAM, giúp game chạy với tốc độ cực nhanh mà không tạo áp lực ghi liên tục lên Ổ cứng cơ sở dữ liệu.

**19. Lưu trạng thái nhiệm vụ phức tạp?**
Tiến trình (VD: giết 5/10 con quái) được lưu thành JSON dictionary (`{"KillSlime": 5}`). Chỉ khi hoàn thành toàn bộ, mới gọi cập nhật xuống hệ thống chính để trao thưởng.

**20. Khả năng Scale (Mở rộng) của hệ thống?**
Server Game được thiết kế Stateless (phi trạng thái phần đăng nhập) kết hợp JWT, dễ dàng chạy thêm nhiều server (Load Balancing). CSDL có thể áp dụng Master-Slave (1 ghi - nhiều đọc) để chịu tải cao.

---

## 3. Gameplay, Điều khiển & AI

**21. Cơ chế Coyote Time & Jump Buffer?**
- **Coyote Time:** Khi rớt khỏi bục, có 1 bộ đếm (VD: 0.2s). Trong 0.2s này bấm nhảy vẫn tính.
- **Jump Buffer:** Bấm nhảy trước khi chạm đất 0.2s, game lưu lại lệnh này, vừa chạm đất nhân vật sẽ tự nhảy luôn, tạo cảm giác điều khiển phản hồi tốt.
- **Vị trí & Logic trong Code:**
  * **Chế độ Singleplayer / Local (`PlayerMovement.cs`):** 
    * Khai báo các tham số/timer ở dòng 39-44 (`coyoteTime`, `coyoteTimeCounter`, `jumpBufferTime`, `jumpBufferCounter`).
    * Cập nhật thời gian đếm ngược và kiểm tra điều kiện nhảy ở hàm `HandleInput()` (dòng 163-192).
    * Thực hiện nhảy ở hàm `HandleMovement()` (dòng 276-280) khi `shouldJump` được kích hoạt.
  * **Chế độ Multiplayer / Network (`NetworkPlayerController.cs`):**
    * Khai báo các tham số/timer ở dòng 22-26.
    * Ghi nhận và đếm ngược Jump Buffer trong hàm `Update()` (dòng 168-191) bằng `Time.deltaTime` của frame để tránh bỏ lỡ phím nhấn.
    * Cập nhật Coyote Time trong `FixedUpdate()` (dòng 196-290) bằng `Time.fixedDeltaTime`, đồng thời kiểm tra điều kiện nhảy đồng bộ và truyền lệnh qua ServerRpc.

**22. Dash I-Frames được kiểm soát ở đâu?**
Thời điểm bắt đầu lướt (Dash) phải gửi lên Server. Server sẽ gán cờ `isInvincible = true` trong 0.2s. Mọi sát thương nhận vào trong lúc này đều bị Server hủy bỏ. Client không tự quyết định để tránh hack bất tử.

**23. Máy trạng thái hữu hạn (FSM) cho AI?**
AI được chia thành các trạng thái (Patrol, Chase, Attack). Trong hàm `Update()`, AI chỉ kiểm tra và thực thi đoạn code của trạng thái hiện tại, giúp code sạch, dễ mở rộng và không bị xung đột logic lẫn nhau.

**24. Thuật toán tìm đường (Pathfinding)?**
Dùng NavMesh/A* xử lý trực tiếp trên Game Server. Quái vật tự động tính toán vật cản để tìm đường ngắn nhất đến người chơi. Client chỉ nhận vị trí trả về để render mượt mà.

**25. Tối ưu AI khi có 100 quái vật?**
AI không cập nhật 60 lần/giây (60 Tick) mà giảm xuống còn khoảng 5-10 lần/giây (Tick Rate riêng cho AI). Hoặc dùng Coroutine rải đều tính toán AI ở các Frame khác nhau để giảm tải đỉnh cho CPU Server.

**26. Thuật toán sát thương Ngũ Hành?**
Dựa vào ma trận hoặc mảng hệ số. Khi kiểm tra va chạm, lấy (Hệ của Kỹ năng) so sánh (Hệ của Mục tiêu). Ví dụ: Thủy gặp Hỏa -> Sát thương x 1.5. Hỏa đánh Thủy -> Sát thương x 0.5.

**27. Hệ Phong (Trung lập)?**
Hệ phong sát thương x1.0 lên tất cả các hệ khác và ngược lại. Ưu điểm là an toàn ở mọi khu vực bản đồ, không bị phạt sát thương, thích hợp cho người chơi muốn sự ổn định.

**28. Tính toán Hitbox/Hurtbox ở đâu?**
Server là nơi kiểm tra. Khi Client ra chiêu, Server kiểm tra vùng Hitbox có giao cắt với vị trí (Hurtbox) của quái trên không gian của Server hay không. Nếu trúng, Server mới trừ máu.

**29. Đồng bộ Animation chiến đấu?**
Khi tung chiêu, Client gửi lệnh lên Server. Server xác nhận và gửi RPC (Remote Procedure Call) Broadcast cho mọi người xung quanh, gọi hàm `Animator.Play("Attack")` để đồng loạt kích hoạt hoạt ảnh.

**30. Cơ chế sinh vật phẩm (Loot)?**
Khi quái chết, Server dùng hàm `Random` sinh số ngẫu nhiên đối chiếu với bảng Tỉ lệ rớt đồ (Drop Rate Table) lưu cứng trên Server. Người chơi không thể chỉnh sửa tỉ lệ này hay đoán trước được vật phẩm.

---

## 4. Hệ thống Tiến hóa Gene & Nhân vật

**31. Điểm khác biệt cốt lõi của Tiến hóa Gene?**
Nó mang tính tự do cực cao. Thay vì bị bó buộc vào "Kiếm Sĩ" hay "Cung Thủ", người chơi có thể cộng chỉ số linh hoạt để vừa mang khiên, vừa xài phép. Chiều sâu nằm ở hệ thống "Dung hợp".

**32. Điều kiện Dung hợp (Hybrid Fusion)?**
Người chơi phải nâng cấp tối đa (Max Level) ở ít nhất 2 nhánh Gene khác nhau (Ví dụ: Nhánh Sức mạnh + Nhánh Nguyên tố). Cộng thêm việc thu thập đủ một nguyên liệu đặc biệt từ Phó bản để kích hoạt.

**33. Cấu trúc dữ liệu lưu trữ?**
Sử dụng `Dictionary<int, int>` (với Key là ID nhánh Gene, Value là Cấp độ hiện tại) lưu trên CSDL. Ở Runtime dùng các cấu trúc dữ liệu Object-Oriented để tính toán chỉ số buff.

**34. Xử lý Tẩy điểm (Reset)?**
Khi tẩy điểm, Server tự động trừ hết các chỉ số thưởng tương ứng, đưa mức Level của Gene về 0, và hoàn trả một phần (hoặc toàn bộ) điểm tài nguyên để người chơi cộng lại từ đầu.

**35. Broadcast hiệu ứng hào quang?**
Sử dụng `NetworkVariable` hoặc SyncVar của Netcode. Khi Server bật cờ `HasUltimateAura = true`, biến này tự động đồng bộ xuống tất cả Client. Client bắt sự kiện `OnValueChanged` để bật hệ thống Particle System phát sáng.

**36. Xử lý hoạt ảnh dài khi Dung hợp?**
Server chuyển trạng thái nhân vật sang "Invulnerable" (Bất tử) và khóa toàn bộ Input di chuyển, giúp người chơi yên tâm tận hưởng hoạt ảnh mà không sợ quái vật đánh chết.

**37. Design Pattern áp dụng cho Kỹ năng?**
Sử dụng Component-based và Strategy Pattern. Mỗi kỹ năng là một class riêng triển khai interface `ISkill`. Nhờ vậy dễ dàng tạo hàng chục kỹ năng mới mà không phải nhét chung hàng ngàn dòng code vào class của Nhân vật.

---

## 5. Bảo mật & Chống gian lận

**38. Từ chối máu sai từ Cheat Engine?**
Server lưu 1 biến `Health` của riêng nó. Bất chấp việc Client dùng phần mềm sửa số máu thành 9999 trên màn hình, khi trúng đòn, Server vẫn trừ máu từ giá trị gốc. Khi máu gốc <= 0, Server gửi lệnh ép Client phải "Chết".

**39. Chống Spam gửi gói tin (Packet Editor)?**
Hệ thống sử dụng Rate Limiting (giới hạn số gói tin mỗi giây) và Cooldown check (Kiểm tra hồi chiêu). Gửi quá nhanh lệnh "Nhặt đồ", Server thấy chưa hết Cooldown sẽ tự động Reject bỏ qua lệnh đó.

**40. Nhược điểm của JWT và cách xử lý?**
Nhược điểm lớn nhất là không thể thu hồi Token ngay lập tức trước khi nó hết hạn. Cách giải quyết là lưu thêm thời gian sống (Exp) ngắn (VD: 15 phút), kết hợp với cơ chế Refresh Token lưu trong DB để có quyền thu hồi.

**41. Chống nhân bản đồ (Dupe Item) khi chuyển Map?**
Game Server áp dụng khóa tài nguyên (Lock). Khi bắt đầu đổi Zone, tài khoản chuyển sang trạng thái "Đang luân chuyển", cấm mọi hành động giao dịch/vứt đồ cho đến khi kết nối thành công ở Zone mới.

**42. Thuật toán băm Mật khẩu?**
Mật khẩu được băm (Hashing) bằng thuật toán chuẩn như BCrypt hoặc ASP.NET Core Identity PasswordHasher, có kết hợp Random Salt tự động cho từng tài khoản để chống lại tấn công Rainbow Table.

**43. Kiểm tra Thời gian hồi chiêu (Cooldown)?**
Logic luôn nằm ở Server. Server lưu biến `lastCastTime`. Khi nhận lệnh tung chiêu, nếu `CurrentTime < lastCastTime + Cooldown`, lệnh sẽ bị coi là gian lận và không được thực thi.

---

## 6. Đánh giá & Tương lai

**44. Cấu hình máy tầm trung?**
Khoảng Core i3/i5 đời cũ, RAM 8GB, Card đồ họa tích hợp hoặc GTX 1050. Game 2D không yêu cầu card đồ họa mạnh, chỉ cần CPU xử lý logic nhanh.

**45. Tầm quan trọng của RTT, CPU/RAM Server?**
RTT (Ping) thấp đảm bảo game hành động mượt mà. Trong khi CPU/RAM quyết định tính sống còn của Server: tải CPU cao sẽ làm giảm Tick Rate của máy chủ, khiến toàn bộ người chơi bị giật lag (Server Lag).

**46. Công cụ kiểm thử tương lai?**
Em dự kiến sử dụng Unity Test Framework cho Unit Test phía Client, và sử dụng công cụ giả lập Client tự động (headless clients) để bắn request liên tục vào Server giúp Stress Test khả năng chịu tải.

**47. Nguy cơ từ Marketplace (Chợ giao dịch)?**
Rủi ro lớn nhất là lỗi Logic đồng thời (Concurrency) dẫn đến việc cả 2 người cùng mua thành công 1 món đồ, hoặc lỗi bảo mật SQL Injection khi tìm kiếm vật phẩm.

**48. Nâng cấp mạng cho Ranked PvP?**
Sẽ phải triển khai kiến trúc Server Rollback (Client Side Prediction kết hợp Lag Compensation phức tạp). Nghĩa là Server phải lưu lại lịch sử vị trí của mọi nhân vật trong 1-2 giây qua để phán xử các pha va chạm ở quá khứ.

**49 & 50. (Câu hỏi mở tự do)**
*(Với hai câu này, bạn hãy tự chọn một tính năng bạn làm tốt nhất và một lỗi khó nhất bạn từng gặp để kể thành một câu chuyện thực tế nhé).*
