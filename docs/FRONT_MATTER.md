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
| Hình 1.1 | Sơ đồ trạng thái Animator Controller của nhân vật 2D (Idle → Run → Jump → Fall → Dash → Attack → Die) | Chương 1 |
| Hình 1.2 | Luồng loot và nâng cấp trang bị (Drop → Inventory → Equip → Enhance → Sell/Discard) | Chương 1 |
| Hình 1.3 | Sơ đồ chuyển trạng thái của quái vật trong game 2D (Idle → Patrol → Chase → Attack → Dead) | Chương 1 |
| Hình 1.4 | Boss Phase System — thay đổi hành vi theo ngưỡng HP (giai đoạn 1 → 2 → 3) | Chương 1 |
| Hình 1.5 | Luồng Server Authoritative — Client gửi input, Server xử lý và xác nhận, broadcast kết quả cho tất cả clients | Chương 1 |
| Hình 1.6 | Kiến trúc Zone-based Server — một server process quản lý nhiều zone (bản đồ chung và instance phó bản) | Chương 1 |
| Hình 1.7 | Luồng xác thực JWT từ đăng nhập REST API đến kết nối Game Server (NGO Connection Approval) | Chương 1 |
| Hình 1.8 | Luồng Wave-based Dungeon (Wave 1 → ... → Wave N → Boss → Clear → Reward) | Chương 1 |
| Hình 1.9 | Sơ đồ ERD tổng quát của hệ thống game (users, player_data, enemy, item, skill, map, dungeon) | Chương 1 |
| Hình 1.10 | Kiến trúc Docker Compose — MySQL + REST API + Game Server trên Linux VPS | Chương 1 |
| Hình 2.1 | Sơ đồ kiến trúc tổng thể hệ thống Mutants Arena (Unity Client — Game Server/API Server — MySQL) | Chương 2 |
| Hình 2.2 | Biểu đồ ca sử dụng mức tổng quát hệ thống Mutants Arena | Chương 2 |
| Hình 2.3 | Biểu đồ ca sử dụng cho mô-đun Đăng ký tài khoản | Chương 2 |
| Hình 2.4 | Biểu đồ ca sử dụng cho mô-đun Đăng nhập và vào game | Chương 2 |
| Hình 2.5 | Biểu đồ ca sử dụng cho mô-đun Di chuyển và chuyển map | Chương 2 |
| Hình 2.6 | Biểu đồ ca sử dụng cho mô-đun Chiến đấu và sử dụng kỹ năng | Chương 2 |
| Hình 2.7 | Biểu đồ ca sử dụng cho mô-đun Quản lý túi đồ và trang bị | Chương 2 |
| Hình 2.8 | Biểu đồ ca sử dụng cho mô-đun Nâng cấp trang bị | Chương 2 |
| Hình 2.9 | Biểu đồ ca sử dụng cho mô-đun Phát triển Gene và Hybrid | Chương 2 |
| Hình 2.10 | Biểu đồ ca sử dụng cho mô-đun Phân bổ tiềm năng và kỹ năng | Chương 2 |
| Hình 2.11 | Biểu đồ ca sử dụng cho mô-đun Tương tác NPC và mua vật phẩm | Chương 2 |
| Hình 2.12 | Biểu đồ ca sử dụng cho mô-đun Quản lý nhiệm vụ | Chương 2 |
| Hình 2.13 | Biểu đồ ca sử dụng cho mô-đun Quản lý bạn bè | Chương 2 |
| Hình 2.14 | Biểu đồ ca sử dụng cho mô-đun Quản lý tổ đội và chat | Chương 2 |
| Hình 2.15 | Biểu đồ ca sử dụng cho mô-đun Tham gia và hoàn tất phó bản | Chương 2 |
| Hình 2.16 | Biểu đồ ca sử dụng cho mô-đun Xem leaderboard | Chương 2 |
| Hình 2.17 | Biểu đồ ca sử dụng cho mô-đun Quản lý gameplay server | Chương 2 |
| Hình 2.18 | Biểu đồ ca sử dụng cho mô-đun Host map và phát thưởng phó bản | Chương 2 |
| Hình 2.19 | Biểu đồ tuần tự đặc tả ca sử dụng Chiến đấu và sử dụng kỹ năng | Chương 2 |
| Hình 2.20 | Biểu đồ tuần tự đặc tả ca sử dụng Nâng cấp trang bị tại Blacksmith | Chương 2 |
| Hình 2.21 | Biểu đồ tuần tự đặc tả ca sử dụng Nâng Gene chính và Gene phụ | Chương 2 |
| Hình 2.22 | Biểu đồ tuần tự đặc tả ca sử dụng Dung hợp Hybrid Gene | Chương 2 |
| Hình 2.22a | Biểu đồ tuần tự đặc tả ca sử dụng Kích hoạt Gene Tối Thượng | Chương 2 |
| Hình 2.23 | Biểu đồ tuần tự đặc tả ca sử dụng Tham gia và hoàn tất phó bản | Chương 2 |
| Hình 2.24 | Sơ đồ cơ sở dữ liệu của hệ thống Mutants Arena | Chương 2 |
| Hình 3.1 | Mô hình bảo mật nhiều lớp của hệ thống Mutants Arena | Chương 3 |
| Hình 3.2 | Luồng xác thực nội bộ Zone Server bằng Zone API Key | Chương 3 |
| Hình 3.3 | Luồng kiểm duyệt kết nối NGO Dedicated Server | Chương 3 |
| Hình 3.4 | Kiến trúc triển khai Docker Compose của hệ thống | Chương 3 |
| Hình 3.5 | Giao diện đăng nhập | Chương 3 |
| Hình 3.6 | Giao diện đăng ký | Chương 3 |
| Hình 3.7 | Giao diện chọn hệ nguyên tố | Chương 3 |
| Hình 3.7 | Giao diện sảnh chính | Chương 3 |
| Hình 3.8 | Giao diện chọn nhân vật (SelectGene) | Chương 3 |
| Hình 3.9 | Giao diện tạo nhân vật Gene 2 mới | Chương 3 |
| Hình 3.10 | Giao diện thanh trạng thái nhân vật (HealthBar / MpBar / PlayerInfoUI) | Chương 3 |
| Hình 3.11 | Giao diện thanh kỹ năng và Buff (SkillHotbarUI / BuffHudPanel) | Chương 3 |
| Hình 3.12 | Giao diện thông tin quái (EnemyInfoPanel) | Chương 3 |
| Hình 3.13 | Giao diện thông báo hệ thống (GlobalNotificationUI) | Chương 3 |
| Hình 3.14 | Giao diện nâng cấp Gene chính (GeneUpgradePanel) | Chương 3 |
| Hình 3.15 | Giao diện xác nhận Gene phụ cố định (SecondaryGeneSelectPanel) | Chương 3 |
| Hình 3.16 | Giao diện nâng cấp Gene phụ (SecondaryGeneUpgradePanel) | Chương 3 |
| Hình 3.17 | Giao diện dung hợp Hybrid (HybridFusionPanel) | Chương 3 |
| Hình 3.18 | Giao diện bảng tóm tắt nhân vật (CharacterMenuPanelUI) | Chương 3 |
| Hình 3.19 | Giao diện tab Chỉ số và Trang bị (StatsTabUI) | Chương 3 |
| Hình 3.20 | Giao diện tab Kỹ năng (SkillTabUI / SkillDetailPanelUI) | Chương 3 |
| Hình 3.21 | Giao diện tab Tiềm Năng (PotentialTabUI) | Chương 3 |
| Hình 3.22 | Giao diện chat đa kênh (ChatPanelUI) | Chương 3 |
| Hình 3.23 | Giao diện danh sách bạn bè (FriendListUI) | Chương 3 |
| Hình 3.24 | Giao diện tổ đội (PartyPanelUI) | Chương 3 |
| Hình 3.25 | Giao diện bảng xếp hạng (LeaderboardPanelUI) | Chương 3 |
| Hình 3.26 | Giao diện chọn phó bản (DungeonListUI) | Chương 3 |
| Hình 3.26 | Giao diện NPC trong phó bản (DungeonNpcMenuUI) | Chương 3 |
| Hình 3.27 | Giao diện HUD phó bản wave (WaveHUD) | Chương 3 |
| Hình 3.28 | Giao diện widget nhiệm vụ góc màn hình (QuestHudWidget) | Chương 3 |
| Hình 3.28 | Giao diện tương tác NPC nhiệm vụ (QuestNpcPanel) | Chương 3 |
| Hình 3.29 | Giao diện menu NPC động và cửa hàng (NpcDynamicMenuUI / NpcMenuUI) | Chương 3 |
| Hình 3.30 | Giao diện chuyển map qua biên (MapEdgeTrigger / MapTransitionButton) | Chương 3 |
| Hình 3.31 | Cổng dịch chuyển phòng trong bản đồ và phó bản (MapPortalTrigger) | Chương 3 |

## DANH MỤC BẢNG

| Số hiệu | Tên bảng | Vị trí |
|---|---|---|
| Bảng 1.0 | So sánh các mô hình mạng trong game multiplayer | Chương 1 |
| Bảng 1.1 | Tổng hợp stack công nghệ của đề tài | Chương 1 |
| Bảng 1.2 | Ánh xạ bài học khảo sát vào thiết kế Mutants Arena | Chương 1 |
| Bảng 1.3 | trình bày đầy đủ ma trận hệ số nhân sát thương của 6 nguyên tố trong Mutants Arena — nền tảng cho toàn bộ thiết kế class và chiến thuật chiến đấu: | Chương 1 |
| Bảng 1.3 | Ma trận tương khắc 6 nguyên tố (hàng = nguyên tố tấn công, cột = nguyên tố bị tấn công) | Chương 1 |
| Bảng 1.4 | Cấu trúc 5 Tier của hệ thống Gene trong Mutants Arena | Chương 1 |
| Bảng 1.5 | So sánh ba cơ chế phát triển nhân vật | Chương 1 |
| Bảng 1.6 | So sánh các giải pháp multiplayer cho game Unity | Chương 1 |
| Bảng 2.0b | So sánh ba mô hình đồng bộ multiplayer cho game RPG online | Chương 2 |
| Bảng 2.1 | Các tác nhân tham gia hệ thống | Chương 2 |
| Bảng 2.1a | Phân nhóm chức năng theo module triển khai thực tế | Chương 2 |
| Bảng 2.1b | Danh mục use case toàn hệ thống Mutants Arena | Chương 2 |
| Bảng 2.18 | Nhóm bảng tài khoản, hồ sơ nhân vật và xã hội | Chương 2 |
| Bảng 2.19 | Nhóm bảng vật phẩm, option và nâng cấp trang bị | Chương 2 |
| Bảng 2.20 | Nhóm bảng Gene, Hybrid và kỹ năng | Chương 2 |
| Bảng 2.21 | Nhóm bảng thế giới game, quái vật, nhiệm vụ và dungeon | Chương 2 |
| Bảng 4.0 | Tổng hợp chức năng đã hoàn thành | Chương 4 |
| Bảng 4.1 | Cấu hình máy client thử nghiệm | Chương 4 |
| Bảng 4.2 | Cấu hình server thử nghiệm | Chương 4 |
| Bảng 4.2b | Test case Xác thực tài khoản và JWT | Chương 4 |
| Bảng 4.3 | Test case di chuyển nhân vật | Chương 4 |
| Bảng 4.4 | FPS trung bình theo cảnh | Chương 4 |
| Bảng 4.5 | Test case kháng nguyên tố (MobPatrolAI với resist = 40) | Chương 4 |
| Bảng 4.6 | Test case Gene system | Chương 4 |
| Bảng 4.7 | Test case AI quái + Boss | Chương 4 |
| Bảng 4.8 | Test case multiplayer | Chương 4 |
| Bảng 4.9 | RTT trung bình (ms) theo tải | Chương 4 |
| Bảng 4.10 | Stress test REST API (JMeter) | Chương 4 |
| Bảng 4.10b | Bằng chứng đính kèm cho từng nhóm thực nghiệm | Chương 4 |
| Bảng 4.11 | Test case NPC – Shop – Equipment – Map/Dungeon | Chương 4 |
| Bảng 4.12 | Đánh giá đáp ứng yêu cầu chức năng | Chương 4 |
| Bảng 4.13 | Đánh giá yêu cầu phi chức năng | Chương 4 |
| Bảng 4.14 | Tải server theo số client | Chương 4 |
| Bảng 4.15 | Kết quả khảo sát UX (n = 12) | Chương 4 |

## DANH MỤC TỪ VIẾT TẮT

| Viết tắt | Nghĩa tiếng Anh | Nghĩa tiếng Việt |
|---|---|---|
| RPG | Role-Playing Game | Game nhập vai |
| UC | Use Case (hoặc Ca sử dụng) | Ca sử dụng |
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
