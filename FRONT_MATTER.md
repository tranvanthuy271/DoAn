# BÌA VÀ FRONT MATTER

> **Lưu ý cho người biên tập Word**: Trang bìa, lời cam đoan, lời cảm ơn nên được tách thành các trang riêng, đặt trước Mục lục.

---

## TRANG BÌA

```
HỌC VIỆN KỸ THUẬT MẬT MÃ
KHOA CÔNG NGHỆ THÔNG TIN
──────────────────────────────

ĐỒ ÁN TỐT NGHIỆP

ĐỀ TÀI:
PHÁT TRIỂN TRÒ CHƠI MUTANTS ARENA
VỚI HỆ THỐNG TIẾN HÓA GENE BẰNG UNITY

(Tập trung vào hệ thống tiến hóa Gene Ngũ Hành
và kiến trúc multiplayer Server-Authoritative)

Ngành: Công nghệ Thông tin
Chuyên ngành: Phát triển phần mềm

Giảng viên hướng dẫn:    [Họ và tên GVHD]
Sinh viên thực hiện:     [Họ và tên]
Mã sinh viên:            [MSV]
Lớp:                     [Lớp]
Khóa:                    [Khóa]

Hà Nội, năm 2026
```

---

## LỜI CAM ĐOAN

Em xin cam đoan đồ án tốt nghiệp “Phát triển trò chơi Mutants Arena với hệ thống tiến hóa Gene bằng Unity” là công trình nghiên cứu của riêng em, được thực hiện dưới sự hướng dẫn của giảng viên hướng dẫn. Các kết quả nghiên cứu trình bày trong báo cáo là trung thực, các nguồn tài liệu tham khảo đều được trích dẫn đầy đủ trong phần Tài liệu tham khảo. Em xin chịu hoàn toàn trách nhiệm về tính trung thực và chính xác của nội dung báo cáo.

Hà Nội, tháng 05 năm 2026

Sinh viên thực hiện

[Ký và ghi rõ họ tên]

---

## LỜI CẢM ƠN

Để hoàn thành đồ án tốt nghiệp này, em xin gửi lời cảm ơn chân thành và sâu sắc nhất tới:

- Quý thầy cô trong Khoa Công nghệ Thông tin – Học viện Kỹ thuật Mật mã đã tận tình giảng dạy, truyền đạt kiến thức và phương pháp nghiên cứu khoa học trong suốt quá trình học tập.
- Đặc biệt, em xin gửi lời cảm ơn đến giảng viên hướng dẫn — người đã trực tiếp định hướng đề tài, góp ý nội dung kỹ thuật và đồng hành cùng em trong toàn bộ quá trình thực hiện đồ án.
- Gia đình và bạn bè đã ủng hộ, động viên trong suốt thời gian học tập và nghiên cứu.
- Các bạn tình nguyện viên đã tham gia playtest và góp ý thiết thực giúp hoàn thiện sản phẩm.

Do thời gian và kinh nghiệm thực tế còn hạn chế, đồ án không tránh khỏi những thiếu sót. Em rất mong nhận được sự góp ý của quý thầy cô và các bạn để đề tài được hoàn thiện hơn.

Em xin chân thành cảm ơn!

---

## MỤC LỤC

- MỞ ĐẦU
- CHƯƠNG 1. TỔNG QUAN VỀ ĐỀ TÀI VÀ CƠ SỞ LÝ THUYẾT
  - 1.1. Tổng quan về game 2D hành động nhập vai
  - 1.2. Khảo sát và phân tích các game 2D tiêu biểu
  - 1.3. Cơ chế gameplay trong game 2D Action RPG
  - 1.4. Trí tuệ nhân tạo trong game
  - 1.5. Kiến trúc Client-Server cho multiplayer game
  - 1.6. Các công nghệ và công cụ sử dụng
  - 1.7. Tổng kết chương 1
- CHƯƠNG 2. PHÂN TÍCH THIẾT KẾ HỆ THỐNG
  - 2.1. Phân tích bài toán và yêu cầu hệ thống
  - 2.2. Thiết kế hệ thống
  - 2.3. Tổng kết chương 2
- CHƯƠNG 3. XÂY DỰNG CÁC CƠ CHẾ GAME
  - 3.1. Hệ thống điều khiển và di chuyển nhân vật
  - 3.2. Hệ thống chiến đấu
  - 3.3. Hệ thống Gene (Ngũ Hành) và quản lý chỉ số
  - 3.4. Hệ thống nhiệm vụ và quái vật
  - 3.5. Hệ thống NPC và cửa hàng
  - 3.6. Hệ thống trang bị và nâng cấp
  - 3.7. Hệ thống bản đồ, khu vực và phó bản
  - 3.8. Tổng kết chương 3
- CHƯƠNG 4. KẾT QUẢ VÀ THỰC NGHIỆM
  - 4.1. Kết quả đạt được
  - 4.2. Thực nghiệm
  - 4.3. Đánh giá hệ thống
  - 4.4. Tổng kết chương 4
- KẾT LUẬN VÀ HƯỚNG PHÁT TRIỂN
- TÀI LIỆU THAM KHẢO
- PHỤ LỤC

---

## DANH MỤC HÌNH ẢNH

| Số hiệu | Tên hình | Vị trí |
|---|---|---|
| Hình 1.1 | Lịch sử phát triển game 2D Action RPG | Chương 1 |
| Hình 1.2 | Sơ đồ phân loại thể loại 2D Action RPG | Chương 1 |
| Hình 1.3 | Biểu đồ doanh thu thị trường game thế giới | Chương 1 |
| Hình 1.4 | Mockup gameplay Hollow Knight (tham chiếu) | Chương 1 |
| Hình 1.5 | Sơ đồ vòng tương khắc Ngũ Hành | Chương 1 |
| Hình 1.6 | Kiến trúc Server Authoritative tổng quát | Chương 1 |
| Hình 2.1 | Kiến trúc ba tầng tổng thể hệ thống | Chương 2 |
| Hình 2.2 | Use Case Diagram tổng quát | Chương 2 |
| Hình 2.3 | Sơ đồ ERD cơ sở dữ liệu | Chương 2 |
| Hình 2.4 | Activity Diagram đăng nhập | Chương 2 |
| Hình 2.5 | Activity Diagram chiến đấu | Chương 2 |
| Hình 2.6 | Component Diagram client – server – DB | Chương 2 |
| Hình 3.1 | Class Diagram module di chuyển | Chương 3 |
| Hình 3.2 | State Diagram di chuyển nhân vật | Chương 3 |
| Hình 3.3 | Sequence Diagram đồng bộ di chuyển multiplayer | Chương 3 |
| Hình 3.4 | Sequence Diagram phóng skill projectile | Chương 3 |
| Hình 3.5 | Mockup UI Gene Forge | Chương 3 |
| Hình 3.6 | Activity Diagram quy trình nhiệm vụ | Chương 3 |
| Hình 3.7 | Flowchart Boss Phase System | Chương 3 |
| Hình 3.8 | Wireframe UI hội thoại NPC | Chương 3 |
| Hình 3.9 | Sequence Diagram giao dịch mua hàng | Chương 3 |
| Hình 3.10 | Mockup UI Cường hoá + Ghép đá | Chương 3 |
| Hình 3.11 | Sơ đồ Zone-based Server Architecture | Chương 3 |
| Hình 3.12 | UI HUD Wave và Boss Phase | Chương 3 |
| Hình 4.1 | Ảnh tổng quan giao diện game in-game | Chương 4 |
| Hình 4.2 | Biểu đồ FPS theo thời gian | Chương 4 |
| Hình 4.3 | Biểu đồ RTT theo số client | Chương 4 |
| Hình 4.4 | Ảnh debug system in-game | Chương 4 |
| Hình 4.5 | Biểu đồ CPU/RAM server | Chương 4 |

## DANH MỤC BẢNG

| Số hiệu | Tên bảng | Vị trí |
|---|---|---|
| Bảng 1.1 | So sánh các game 2D Action RPG tiêu biểu | Chương 1 |
| Bảng 1.2 | Ánh xạ bài học khảo sát vào Mutants Arena | Chương 1 |
| Bảng 1.3 | So sánh game engine 2D phổ biến | Chương 1 |
| Bảng 2.1 | Các tác nhân tham gia hệ thống | Chương 2 |
| Bảng 2.2 | Danh sách Use Case | Chương 2 |
| Bảng 2.3 | Đặc tả Use Case Đăng nhập | Chương 2 |
| Bảng 2.4 | Đặc tả Use Case Chiến đấu | Chương 2 |
| Bảng 2.5 | Đặc tả Use Case Nâng cấp Gene | Chương 2 |
| Bảng 2.6 | Lược đồ bảng `players` | Chương 2 |
| Bảng 2.7 | Lược đồ bảng `characters` | Chương 2 |
| Bảng 2.8 | Lược đồ bảng `gene_inventory` | Chương 2 |
| Bảng 2.9 | Lược đồ bảng `bosses` (phases_json) | Chương 2 |
| Bảng 2.10 | API endpoints chính | Chương 2 |
| Bảng 3.1 | State machine di chuyển | Chương 3 |
| Bảng 3.2 | Ma trận tương khắc 6 nguyên tố | Chương 3 |
| Bảng 3.3 | Bonus chỉ số theo Tier Gene | Chương 3 |
| Bảng 3.4 | Các công thức Fusion tiêu biểu | Chương 3 |
| Bảng 3.5 | Cấu hình AI cho 3 lớp quái | Chương 3 |
| Bảng 3.6 | Action điển hình của NPC | Chương 3 |
| Bảng 3.7 | Bảng cường hoá trang bị | Chương 3 |
| Bảng 3.8 | Cấu hình dungeon 5 wave + Boss | Chương 3 |
| Bảng 4.0 | Tổng hợp chức năng đã hoàn thành | Chương 4 |
| Bảng 4.1 | Cấu hình máy client thử nghiệm | Chương 4 |
| Bảng 4.2 | Cấu hình server thử nghiệm | Chương 4 |
| Bảng 4.3 | Test case di chuyển | Chương 4 |
| Bảng 4.4 | FPS trung bình theo cảnh | Chương 4 |
| Bảng 4.5 | Test case tương khắc nguyên tố | Chương 4 |
| Bảng 4.6 | Test case Gene system | Chương 4 |
| Bảng 4.7 | Test case AI quái và Boss | Chương 4 |
| Bảng 4.8 | Test case multiplayer | Chương 4 |
| Bảng 4.9 | RTT trung bình theo tải | Chương 4 |
| Bảng 4.10 | Stress test REST API | Chương 4 |
| Bảng 4.11 | Test case NPC/Shop/Equip/Map | Chương 4 |
| Bảng 4.12 | Đánh giá yêu cầu chức năng | Chương 4 |
| Bảng 4.13 | Đánh giá yêu cầu phi chức năng | Chương 4 |
| Bảng 4.14 | Tải server theo số client | Chương 4 |
| Bảng 4.15 | Kết quả khảo sát UX | Chương 4 |

---

## DANH MỤC TỪ VIẾT TẮT

| Viết tắt | Nghĩa tiếng Anh | Nghĩa tiếng Việt |
|---|---|---|
| RPG | Role-Playing Game | Game nhập vai |
| FSM | Finite State Machine | Máy trạng thái hữu hạn |
| AI | Artificial Intelligence | Trí tuệ nhân tạo |
| NGO | Unity Netcode for GameObjects | Bộ thư viện đồng bộ Unity |
| RPC | Remote Procedure Call | Lời gọi hàm từ xa |
| JWT | JSON Web Token | Token xác thực JSON |
| ERD | Entity Relationship Diagram | Sơ đồ thực thể liên kết |
| API | Application Programming Interface | Giao diện lập trình ứng dụng |
| DoT | Damage over Time | Sát thương theo thời gian |
| AoE | Area of Effect | Sát thương diện rộng |
| CC | Crowd Control | Khống chế |
| RTT | Round-Trip Time | Thời gian khứ hồi mạng |
| FPS | Frames Per Second | Khung hình mỗi giây |
| VFX/SFX | Visual / Sound Effect | Hiệu ứng hình / âm thanh |
