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

Giảng viên hướng dẫn:    TS. Nguyễn Đức Hiếu
Sinh viên thực hiện:     Trần Văn Thủy
Mã sinh viên:            CT060439
Lớp:                     CT6
Khóa:                    Khóa 6

Hà Nội, năm 2026
```

---

## LỜI CAM ĐOAN

Em xin cam đoan đồ án tốt nghiệp “Phát triển trò chơi Mutants Arena với hệ thống tiến hóa Gene bằng Unity” là công trình nghiên cứu của riêng em, được thực hiện dưới sự hướng dẫn của giảng viên hướng dẫn. Các kết quả nghiên cứu trình bày trong báo cáo là trung thực, các nguồn tài liệu tham khảo đều được trích dẫn đầy đủ trong phần Tài liệu tham khảo. Em xin chịu hoàn toàn trách nhiệm về tính trung thực và chính xác của nội dung báo cáo.

Hà Nội, tháng 05 năm 2026

Sinh viên thực hiện

Trần Văn Thủy

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


---

# LỜI NÓI ĐẦU

Sự trỗi dậy của ngành công nghiệp game đang mở ra một quỹ đạo phát triển mới cho lĩnh vực giải trí kỹ thuật số toàn cầu. Theo báo cáo của Newzoo (2024), tổng doanh thu thị trường game thế giới đạt hơn 184 tỷ USD, trong đó phân khúc game PC và console đóng góp khoảng 90 tỷ USD — tương đương gần 49% toàn thị trường. Đáng chú ý, các tựa game hành động nhập vai 2D (2D Action RPG) thuộc phân khúc indie đang chứng tỏ sức bền đặc biệt: Hollow Knight vượt 15 triệu bản bán ra, Dead Cells đạt hơn 10 triệu bản và Celeste giành nhiều giải Game of the Year — tất cả chứng minh rằng chiều sâu gameplay và thiết kế hệ thống chặt chẽ có giá trị hơn độ hoành tráng đồ họa. Bên cạnh đó, Steam — nền tảng phân phối game PC lớn nhất thế giới — ghi nhận hơn 14.000 tựa game được phát hành chỉ riêng trong năm 2023, trong đó thể loại Action RPG dẫn đầu về lượt tải về và thời gian chơi trung bình của người dùng.

Tại Việt Nam, theo số liệu từ Vietnam Report và VGS (Vietnam Game Summit 2023), thị trường game nội địa đạt doanh thu ước tính 1,3 tỷ USD với tốc độ tăng trưởng hàng năm khoảng 11% và hơn 22 triệu người chơi game đang hoạt động. Tuy nhiên, lĩnh vực nghiên cứu và phát triển game trong đào tạo đại học tại Việt Nam vẫn đang đối mặt với những hạn chế rõ ràng. Phần lớn các đề tài về game chỉ dừng ở mức prototype đơn giản — thiếu hệ thống nhân vật có chiều sâu, thiếu trí tuệ nhân tạo đủ phức tạp cho quái vật và boss, và đặc biệt thiếu kiến trúc multiplayer đúng nghĩa theo chuẩn công nghiệp. Cụ thể hơn, gần như chưa có đề tài trong nước nào kết hợp đồng thời cả bốn yếu tố: gameplay hành động 2D real-time, hệ thống Gene/nguyên tố đặc trưng, AI quái vật đa giai đoạn và multiplayer theo mô hình Server Authoritative — trong khi đây chính là tổ hợp kỹ thuật mà thị trường và ngành công nghiệp game đang đòi hỏi.

Những hạn chế cố hữu này — thiếu chiều sâu thiết kế nhân vật, AI quái vật nghèo nàn, kiến trúc mạng không đảm bảo công bằng và chống can thiệp dữ liệu, cơ chế gameplay chưa kết nối chặt chẽ thành một tổng thể vận hành được — đòi hỏi một cách tiếp cận tích hợp, kết hợp thiết kế gameplay bài bản với kiến trúc kỹ thuật chuẩn công nghiệp. Trong bối cảnh ấy, việc nghiên cứu và xây dựng một tựa game 2D Action RPG multiplayer hoàn chỉnh với hệ thống Gene Ngũ Hành đặc trưng là hướng đi có giá trị học thuật lẫn thực tiễn ứng dụng trong đào tạo ngành Công nghệ Thông tin tại Việt Nam.

Đồ án "Xây dựng trò chơi điện tử nhập vai hành động 2D đa người chơi — Mutants Arena" được thực hiện trong khuôn khổ chương trình đào tạo ngành Công nghệ Thông tin tại [Tên Trường], dưới sự hướng dẫn của [Học hàm/Học vị. Tên Giảng Viên Hướng Dẫn]. Mục tiêu của đồ án là hiện thực một nguyên mẫu game chơi được hoàn chỉnh, qua đó kiểm chứng năng lực của tổ hợp công nghệ Unity 2D — ASP.NET Core — Unity Netcode for GameObjects trong việc xây dựng hệ thống game đáp ứng đồng thời các tiêu chí về độ mượt mà gameplay, tính công bằng multiplayer và chiều sâu thiết kế hệ thống Gene.

Về mục tiêu cụ thể, đồ án tập trung: xây dựng hệ thống điều khiển và di chuyển nhân vật 2D hoàn chỉnh với nhảy đôi, Dash tích hợp invincibility frames và vật lý side-scrolling mượt mà; phát triển hệ thống chiến đấu thời gian thực với hitbox/hurtbox chính xác, kỹ năng nguyên tố và cơ chế tương khắc Ngũ Hành nhân hệ số ×1.5/×0.75; xây dựng hệ thống Gene sáu nguyên tố (Hỏa, Kim, Mộc, Phong, Thổ, Thủy) với năm Tier nâng cấp, cơ chế Gene Fusion tạo Hybrid gene và tích hợp trực tiếp vào chỉ số nhân vật; triển khai AI quái vật sử dụng Finite State Machine kết hợp NavMesh pathfinding và Boss AI với Phase System nhiều giai đoạn; xây dựng kiến trúc multiplayer Server Authoritative dùng Unity Netcode for GameObjects với xác thực JWT và đồng bộ trạng thái dưới 100ms; phát triển hệ thống phó bản (Dungeon) wave-based và tính năng Party qua SignalR; đồng thời xây dựng REST API backend bằng ASP.NET Core và cơ sở dữ liệu quan hệ MySQL lưu trữ toàn bộ dữ liệu nhân vật, vật phẩm và tiến trình game.

Phương pháp tiếp cận của đồ án đi theo chu trình "phân tích → thiết kế → triển khai → kiểm thử". Trước hết, hệ thống được mô hình hóa đầy đủ về yêu cầu chức năng và phi chức năng, xác định tác nhân sử dụng, kiến trúc ba tầng (Unity Client — Game Server/API Server — MySQL), thiết kế cơ sở dữ liệu quan hệ và đặc tả các ca sử dụng chi tiết. Kế đến, các thành phần (hệ thống game, server backend, cơ sở dữ liệu) được hiện thực lập trình tuần tự và tích hợp từng bước — ưu tiên các module cốt lõi (di chuyển, chiến đấu, Gene, multiplayer) trước khi mở rộng sang các hệ thống phụ trợ (Quest, NPC, cửa hàng, trang bị, bản đồ, phó bản). Cuối cùng là kiểm thử chức năng từng hệ thống gameplay, kiểm thử đồng bộ multiplayer với nhiều client đồng thời và đánh giá hiệu năng tổng thể theo các tiêu chí về FPS, độ trễ mạng và tính chính xác đồng bộ trạng thái.

Về đóng góp kỳ vọng, đồ án hướng tới: một nguyên mẫu game 2D Action RPG multiplayer chơi được hoàn chỉnh làm tài liệu tham khảo cho các nghiên cứu phát triển game tiếp theo; một kiến trúc ba tầng chuẩn công nghiệp tích hợp Unity, ASP.NET Core và MySQL có thể tái sử dụng và mở rộng; một bộ thiết kế hệ thống Gene Ngũ Hành sáu nguyên tố với ma trận tương khắc và cơ chế Gene Fusion là đóng góp gameplay đặc trưng của đề tài; và một tập hợp giải pháp kỹ thuật cho các bài toán điển hình trong game multiplayer — đồng bộ trạng thái Server Authoritative, chống can thiệp dữ liệu phía client, AI boss đa giai đoạn và hệ thống phó bản đồng đội — có giá trị học thuật và thực tiễn triển khai.

Cấu trúc báo cáo gồm bốn chương chính:
 Chương 1 – Tổng quan về đề tài và cơ sở lý thuyết: trình bày tổng quan thể loại game 2D Action RPG, khảo sát các tựa game tiêu biểu trong và ngoài nước, phân tích sâu các cơ chế gameplay, nguyên lý trí tuệ nhân tạo trong game, kiến trúc Client-Server cho multiplayer và tổng quan các công nghệ, công cụ sử dụng trong đề tài.
 Chương 2 – Phân tích thiết kế hệ thống: xác định yêu cầu chức năng và phi chức năng, mô hình hóa kiến trúc tổng thể ba tầng, đặc tả các ca sử dụng, biểu đồ hoạt động và thiết kế cơ sở dữ liệu quan hệ cùng kiến trúc Client-Server.
 Chương 3 – Xây dựng các cơ chế game: trình bày chi tiết quá trình triển khai từng hệ thống — điều khiển nhân vật, chiến đấu, Gene Ngũ Hành và chỉ số nhân vật, nhiệm vụ và quái vật, NPC và cửa hàng, trang bị và nâng cấp, bản đồ và phó bản — kèm theo mã nguồn minh họa và giải thích kỹ thuật cho từng giải pháp quan trọng.
 Chương 4 – Kết quả và thực nghiệm: trình bày kết quả đạt được qua từng nhóm chức năng, thực nghiệm kiểm thử hệ thống gameplay và di chuyển, Gene và chiến đấu, AI quái vật, multiplayer, NPC và các tính năng phụ trợ, kết thúc bằng đánh giá tổng thể theo các tiêu chí về hiệu năng, độ tin cậy và trải nghiệm người chơi.

Sau thời gian khoảng [X] tháng thực hiện đồ án, các mục tiêu đề ra về cơ bản đã đạt được. Tuy nhiên, phát triển game là lĩnh vực rộng đòi hỏi sự kết hợp sâu giữa kỹ thuật lập trình, thiết kế hệ thống và tư duy sáng tạo, thời gian thực hiện tương đối ngắn nên chắc chắn không tránh khỏi những thiếu sót về nội dung và hình thức trình bày. Rất mong được sự góp ý của các thầy cô giáo, cũng như các bạn học viên để đồ án này được hoàn thiện hơn, có thể hướng tới áp dụng vào thực tiễn giảng dạy và nghiên cứu phát triển game tại Việt Nam.

SINH VIÊN THỰC HIỆN ĐỒ ÁN


---

# CHƯƠNG 1. TỔNG QUAN VỀ ĐỀ TÀI VÀ CƠ SỞ LÝ THUYẾT
> **Định hướng chương 1.** Phần §1.1–1.2 cung cấp **nền tổng quan ngắn gọn** về thể loại 2D Action RPG và các game tham chiếu, chỉ nhằm đối sánh và rút bài học thiết kế. **Trọng tâm lý thuyết phục vụ đề tài** được đặt ở §1.3 (Gene Evolution — đặc trưng riêng) và §1.5 (Multiplayer Server-Authoritative cho game 2D RPG). Các con số thị trường ngoài phạm vi trích nguồn trực tiếp được giữ ở mức minh hoạ, không đóng vai trò bằng chứng nghiên cứu.
## 1.1. Tổng quan về game 2D hành động nhập vai

### 1.1.1. Khái niệm và đặc điểm của game 2D Action RPG

Game điện tử (video game) là loại hình giải trí tương tác kỹ thuật số trong đó người dùng điều khiển một đối tượng trên màn hình theo luật chơi được lập trình sẵn. Từ những arcade đơn giản thập niên 1970 như Pong và Space Invaders, ngành công nghiệp game đã phát triển thành một trong những lĩnh vực giải trí lớn nhất toàn cầu, vượt qua doanh thu của điện ảnh và âm nhạc cộng lại. Theo thống kê của Newzoo Global Games Market Report 2023, doanh thu toàn cầu từ game đạt hơn 184 tỷ USD, với hơn 3,2 tỷ người chơi trên khắp thế giới.

Trong hệ thống phân loại game, **game hành động nhập vai 2D màn hình ngang** (2D Side-Scrolling Action RPG) là thể loại kết hợp hai dòng chính: hành động thời gian thực (action) và nhập vai (role-playing). Đây là sự tổng hòa giữa kỹ năng phản xạ trực tiếp của người chơi với chiều sâu phát triển nhân vật đặc trưng của RPG truyền thống. Người chơi trực tiếp điều khiển nhân vật trong không gian 2D nhìn từ góc bên (side-view), thực hiện chiến đấu thời gian thực, trong khi song song xây dựng nhân vật qua hệ thống lên cấp, trang bị và kỹ năng đặc trưng của thể loại nhập vai.

Khái niệm "side-scrolling" (màn hình cuộn ngang) phân biệt dòng game này với game 2D nhìn từ trên xuống (top-down) hay góc nhìn đẳng cự (isometric). Trong side-scrolling, trục hoành (X) là hướng di chuyển chính của nhân vật, trục tung (Y) chịu tác động của trọng lực mô phỏng, tạo ra bản chất nền tảng (platformer) — người chơi phải đứng trên các nền tảng, vượt qua khoảng trống và leo lên địa hình đa tầng. Yếu tố trọng lực và nền tảng là đặc điểm căn bản phân biệt game 2D side-scrolling với mọi thể loại khác, đồng thời tạo ra cơ chế chiến đấu và di chuyển độc đáo không thể tái hiện trong môi trường 3D hay top-down.

a) Đặc điểm cốt lõi của thể loại

Một game 2D Action RPG đầy đủ thường có các thành phần căn bản sau:

▪ **Hệ thống di chuyển đặc trưng**: Nhảy, bám tường, lướt nhanh (Dash), leo dây, bay — những cơ chế di chuyển vượt trội giúp người chơi điều hướng địa hình và né tránh nguy hiểm trong chiến đấu
▪ **Chiến đấu thời gian thực**: Tấn công cận chiến, đánh xa, combo — người chơi tự tay thực hiện, không phải chờ lượt như RPG chiến lược truyền thống
▪ **Hệ thống chỉ số nhân vật**: HP, MP, ATK, DEF, SPD, CRIT — tạo ra vòng phát triển (progression loop) dài hạn và độ phức tạp chiến thuật
▪ **Hệ thống kỹ năng**: Đa dạng kỹ năng theo lớp nhân vật (class), cơ chế cooldown và mana tiêu hao
▪ **Hệ thống trang bị**: Vũ khí, giáp, phụ kiện với chỉ số và hiệu ứng đặc biệt có thể loot và nâng cấp
▪ **Tiến hóa nhân vật**: Lên cấp, phân nhánh kỹ năng, đổi class hoặc cơ chế đặc biệt như Gene Mutation, Element Affinity

b) Phân biệt với các thể loại liên quan

Game 2D Action RPG khác với **Hack-and-Slash** (chỉ tập trung combat mà không có chiều sâu RPG) ở hệ thống phát triển nhân vật phong phú. Khác với **JRPG truyền thống** (combat theo lượt, không đòi hỏi kỹ năng phản xạ thực sự) ở yếu tố action thời gian thực. Khác với **Platformer thuần túy** (Celeste, Super Mario) ở hệ thống chỉ số, trang bị và kỹ năng RPG. Sự kết hợp độc đáo này tạo ra thể loại có khán giả rộng: vừa hấp dẫn người chơi thích action nhanh, vừa giữ chân người chơi thích đầu tư phát triển nhân vật dài hạn.

### 1.1.2. Lịch sử phát triển thể loại

Lịch sử của game 2D Action RPG gắn liền với lịch sử phát triển của toàn bộ ngành công nghiệp game từ thập niên 1980 đến nay. Sự tiến hóa của thể loại phản ánh những bước nhảy vọt về công nghệ phần cứng, công cụ phát triển và kỳ vọng ngày càng cao của người chơi qua các thế hệ.

**Giai đoạn khai sinh (1980–1995)**: Những tựa game tiên phong như Castlevania (1986), Mega Man (1987) và The Legend of Zelda II: The Adventure of Link (1987) đặt nền móng cho thể loại. Castlevania đặc biệt quan trọng với hệ thống khám phá bản đồ phi tuyến tính và chiến đấu có chiều sâu, trở thành nguyên mẫu của dòng "Metroidvania" sau này. Trong thời kỳ này, giới hạn bộ nhớ buộc các nhà phát triển phải thiết kế gameplay cực kỳ tập trung, không dư thừa — một bài học thiết kế quan trọng vẫn còn giá trị đến ngày nay.

**Giai đoạn định hình Metroidvania (1995–2010)**: Thuật ngữ "Metroidvania" ra đời từ sự kết hợp tên hai game đại diện: Metroid (Nintendo) và Castlevania: Symphony of the Night (Konami, 1997). Symphony of the Night là bước nhảy vọt lớn khi tích hợp hệ thống RPG đầy đủ (level, equipment, stats) vào gameplay action platformer, mở ra định hướng phát triển cho cả thể loại trong hai thập kỷ tiếp theo. Thời kỳ này cũng chứng kiến sự nở rộ của MMORPG 2D side-scrolling tại châu Á với MapleStory (2003) đạt tới 180 triệu tài khoản đăng ký.

**Giai đoạn bùng nổ indie (2010–nay)**: Sự ra đời của Unity Engine, GameMaker Studio và các nền tảng phân phối độc lập như Steam, itch.io đã hạ thấp rào cản gia nhập cho nhóm nhỏ. Kết quả là làn sóng game 2D Action RPG chất lượng cao: Shovel Knight (2014), Hollow Knight (2017), Dead Cells (2018), Hades (2020). Đây là "thời đại vàng" của indie game, đặc biệt cho thể loại 2D Action RPG khi chi phí thấp nhưng cộng đồng Steam sẵn sàng đón nhận sản phẩm chất lượng từ nhóm nhỏ.

### 1.1.3. Phân loại game 2D Action RPG

Game 2D Action RPG có thể được phân loại theo nhiều tiêu chí khác nhau, phản ánh sự đa dạng của thể loại và giúp nhà phát triển xác định rõ đặc trưng sản phẩm của mình.

a) Phân loại theo cơ chế khám phá

▪ **Metroidvania**: Thế giới phi tuyến tính, người chơi mở khóa khu vực mới bằng kỹ năng mới thu được (Hollow Knight, Castlevania SotN). Trọng tâm là khám phá và quay lại khu vực cũ với khả năng mới
▪ **Linear Action RPG**: Hành trình tuyến tính từ màn này sang màn khác, mỗi khu vực có cấu trúc rõ ràng với boss cuối (Mega Man, classic Castlevania). Dễ thiết kế và cân bằng độ khó hơn
▪ **Roguelike/Roguelite**: Mỗi lần chơi tạo ra bản đồ ngẫu nhiên mới, chết thì bắt đầu lại từ đầu nhưng giữ lại một số tiến trình nhất định (Dead Cells, Hades). Tính tái chơi (replayability) rất cao
▪ **MMORPG 2D Side-Scrolling**: Thế giới mở, nhiều người chơi đồng thời, bản đồ chia zone và channel (MapleStory, LangLa). Tập trung vào social interaction và progression dài hạn nhiều tháng, nhiều năm

b) Phân loại theo cơ chế chiến đấu

▪ **Cận chiến (Melee)**: Vũ khí tầm ngắn — kiếm, búa, đấm — đòi hỏi tiếp cận gần để tấn công. Tạo ra nguy hiểm cao nhưng sát thương lớn
▪ **Đánh xa (Ranged)**: Cung tên, phép thuật bắn đạn — giữ khoảng cách an toàn nhưng có thể bị phản đòn. Phù hợp người chơi thích kiểm soát khoảng cách
▪ **Hybrid**: Kết hợp cả melee và ranged, thường gắn với hệ nguyên tố đặc trưng của class. Đây là xu hướng chủ đạo trong game RPG hiện đại
▪ **Summoner**: Triệu hồi đồng minh hoặc tạo ra entity độc lập chiến đấu thay mặt người chơi

c) Phân loại theo chế độ chơi

▪ **Single-player**: Chơi đơn, câu chuyện tuyến tính hoặc khám phá cá nhân với focus vào narrative và challenge
▪ **Co-op Multiplayer**: 2–4 người chơi cùng lúc, phối hợp chiến đấu và khám phá. Đây là xu hướng phát triển mạnh từ 2018 đến nay
▪ **MMORPG**: Hàng nghìn người chơi cùng thế giới, tương tác xã hội phức tạp, hệ thống party, guild, PvP, market

### 1.1.4. Xu hướng phát triển và thị trường hiện nay

Thị trường game 2D Action RPG đang trải qua giai đoạn tăng trưởng mạnh mẽ, đặc biệt tại phân khúc indie. Theo dữ liệu từ nền tảng Steam, trong năm 2022–2024, hơn 35% game indie bán chạy nhất thuộc thể loại 2D Action hoặc 2D Action RPG. Hollow Knight đạt hơn 3,5 triệu bản, Dead Cells hơn 7 triệu bản và Hades giành nhiều giải thưởng Game of the Year 2020 — những con số này khẳng định sức hút thương mại của thể loại vẫn rất lớn, thậm chí ngày càng tăng theo thời gian.

Xu hướng nổi bật nhất giai đoạn 2020–2025 là tích hợp multiplayer vào game 2D Action RPG vốn truyền thống là single-player. Các tựa game như Terraria, Stardew Valley và Hades đang thí nghiệm tính năng co-op, cho thấy nhu cầu chơi cùng nhau ngày càng lớn. Xu hướng thứ hai là hệ thống tiến hóa phi tuyến — thay vì level up đơn giản, nhiều game áp dụng gene mutation, element affinity, class hybrid để tạo ra vô số hướng xây dựng nhân vật khác nhau, tăng cường tính tái chơi và cá nhân hóa trải nghiệm.

Tại Việt Nam, thị trường game 2D online đã có lịch sử với MapleStory VN, Audition và LangLa, nhưng hầu hết là phân phối game nước ngoài chứ chưa có sản phẩm trong nước đạt chuẩn đầy đủ về hệ thống gameplay và multiplayer. Đây là cơ hội và cũng là thách thức cho đề tài nghiên cứu: xây dựng game 2D Action RPG multiplayer với các hệ thống hiện đại, lấy cảm hứng từ triết lý Ngũ Hành Á Đông, phục vụ cộng đồng người chơi Việt Nam.

---

## 1.2. Khảo sát và phân tích các game 2D hành động nhập vai tiêu biểu

Để định hướng thiết kế cho Mutants Arena, năm tựa game tiêu biểu đại diện cho các khía cạnh khác nhau của thể loại được khảo sát và phân tích.

**Hollow Knight** (Team Cherry, 2017) là Metroidvania 2D xây dựng trên Unity, đạt hơn 3,5 triệu bản bán ra. Game nổi bật với Charm System — hệ thống bùa hộ ghép tự do tạo build nhân vật linh hoạt — và Boss AI đa giai đoạn (Phase System), mỗi giai đoạn có pattern tấn công riêng biệt tạo cảm giác thành tựu khi vượt qua. Đây là minh chứng Unity 2D đủ năng lực sản xuất game thương mại quy mô lớn với đội nhỏ.

**Dead Cells** (Motion Twin, 2018) đạt hơn 7 triệu bản và được đánh giá có combat feel tốt nhất thể loại. Kỹ thuật hit-stop (dừng 1–3 frame khi đòn trúng), particle effect và animation transition mượt mà tạo ra cảm giác mỗi đòn đánh có trọng lượng thực sự. Animator Controller đặt Exit Time = 0 trên mọi transition đảm bảo nhân vật phản hồi input tức thì.

**Celeste** (2018) là case study hoàn hảo về thiết kế di chuyển 2D. Cơ chế Dash 8 hướng với invincibility frames là công cụ né tránh tạo ra yếu tố kỹ năng (skill expression) phân cấp trình độ người chơi. Ngoài ra, coyote time và jump buffer loại bỏ hoàn toàn cảm giác "nhảy hụt" bực bội, nâng cao đáng kể game feel.

**MapleStory** (Nexon, 2003) với hơn 180 triệu tài khoản đăng ký là tham chiếu chính về kiến trúc kỹ thuật. Game triển khai mô hình zone/channel-based server — một process duy nhất quản lý nhiều zone và instance — cho phép phân tải người chơi hiệu quả. Hệ thống party và dungeon theo nhóm của MapleStory cũng là mẫu tham chiếu cho tính năng multiplayer xã hội.

**LangLa** (game online Việt Nam) áp dụng hệ thống Ngũ Hành làm cơ chế tương khắc nguyên tố trong chiến đấu, khai thác triết học phương Đông quen thuộc với người chơi Việt Nam. Mỗi class gắn với một nguyên tố, tạo ra chiều sâu chiến thuật tự nhiên mà không cần giải thích phức tạp — đặc biệt hiệu quả trong môi trường PvP và dungeon nhóm.

### 1.2.1. Nhận xét tổng hợp

Từ năm tựa game trên, các quyết định thiết kế cốt lõi cho Mutants Arena được ánh xạ trực tiếp như Bảng 1.2:

**Bảng 1.2: Ánh xạ bài học khảo sát vào thiết kế Mutants Arena**

| Tính năng trong Mutants Arena | Nguồn cảm hứng |
|---|---|
| Boss Phase System (lưu `phases_json`) | Hollow Knight — boss đa giai đoạn |
| Hit-stop + combat feel + Exit Time = 0 | Dead Cells — combat feel |
| Dash + i-frames + coyote time + jump buffer | Celeste — precision platforming |
| ZoneRoomRegistry + DungeonInstance | MapleStory — zone/channel-based server |
| Party System + Wave-based Dungeon | MapleStory — party và phó bản |
| Hệ thống 6 nguyên tố tương khắc ×1.5/×0.75 | LangLa — Ngũ Hành |
| Gene System — Multi-Gene + Hybrid Fusion | Đặc trưng riêng của đề tài |

---

## 1.3. Cơ chế gameplay trong game 2D Action RPG

### 1.3.1. Hệ thống di chuyển và vật lý 2D

Cơ chế di chuyển là nền tảng của mọi game 2D side-scrolling và là yếu tố đầu tiên người chơi tương tác với game. Chất lượng cảm giác điều khiển (game feel) phụ thuộc rất nhiều vào cách tham số vật lý được điều chỉnh: gia tốc khởi động, vận tốc tối đa, trọng lực, ma sát và cảm giác phanh khi dừng. Những con số này không cần phải "vật lý thực", mà cần phải "cảm giác đúng" theo trực giác người chơi.

Trong Unity 2D, hệ thống vật lý dựa trên thư viện Box2D (cùng engine với nhiều game 2D thương mại) cung cấp Rigidbody2D để mô phỏng vật lý cho nhân vật. Di chuyển ngang thực hiện bằng cách đặt velocity.x của Rigidbody2D theo đầu vào người chơi, trong khi trục Y chịu tác động của gravity scale — hệ số trọng lực so với mặc định. Việc tăng gravity scale khi rơi (khoảng 2–3 lần so với khi đang bay lên) tạo ra cảm giác nhảy "nặng" và kiểm soát được — kỹ thuật gọi là "variable gravity jump".

a) Ground Detection (Phát hiện tiếp đất)

Để xác định nhân vật đang đứng trên mặt đất — điều kiện cần để có thể nhảy — phương pháp phổ biến nhất trong Unity là Physics2D Raycast hoặc OverlapCircle. Một tia (ray) được bắn từ vị trí chân nhân vật thẳng xuống dưới; nếu tia va chạm với layer "Ground" trong khoảng cách ngắn (thường 0.1–0.2 đơn vị), nhân vật được xác định là đang tiếp đất và cờ IsGrounded được bật. Phương pháp OverlapCircle bắn một hình tròn nhỏ thay vì tia đơn, cho kết quả ổn định hơn trên các bề mặt nghiêng.

b) Cơ chế nhảy (Jump Mechanics)

Nhảy là cơ chế di chuyển theo chiều dọc cơ bản nhất. Khi người chơi nhấn nút nhảy và IsGrounded = true, hệ thống gán cho Rigidbody2D một velocity.y ban đầu hướng lên. Sau đó gravity liên tục kéo xuống cho đến khi chạm đất. Các kỹ thuật nâng cao thường dùng bao gồm:

▪ **Variable Jump Height**: Nếu người chơi thả nút nhảy sớm, tăng gravity scale để kết thúc nhảy nhanh. Tạo cảm giác điều khiển nhảy có chủ ý hơn, nhảy thấp hay cao tùy ý
▪ **Coyote Time**: Cho phép nhảy trong vài frame (~0.1–0.15 giây) sau khi nhân vật vừa rời khỏi mép nền tảng. Loại bỏ cảm giác "nhảy hụt" bực bội khi chạm mép
▪ **Jump Buffer**: Ghi nhận input nhảy được nhấn trước khi chạm đất (~0.1 giây). Khi chạm đất, tự động kích hoạt nhảy. Giúp combo nhảy liền mạch không yêu cầu timing chính xác milisecond

c) Cơ chế Dash (Lướt nhanh)

Dash là cơ chế di chuyển đặc biệt cho phép nhân vật dịch chuyển nhanh theo một hướng trong thời gian cực ngắn (thường 0.15–0.25 giây). Đây là cơ chế quan trọng nhất phân biệt game action hiện đại với game cổ điển. Trong combat, Dash dùng để né tránh đòn tấn công của kẻ địch hoặc tiếp cận nhanh mục tiêu.

Yếu tố quan trọng nhất của Dash là **Invincibility Frames (I-Frames)** — trong suốt thời gian Dash, nhân vật không nhận bất kỳ sát thương nào. Đây là yếu tố kỹ năng (skill expression) cốt lõi: người chơi giỏi biết thời điểm Dash vào đúng lúc đòn của boss để né tránh hoàn toàn. Sau mỗi Dash có thời gian hồi (cooldown) — thường 0.5–1.5 giây — để tránh lạm dụng liên tục, buộc người chơi phải chọn thời điểm sử dụng.

d) Hệ thống hoạt ảnh và Animator Controller

Trong Unity 2D, hoạt ảnh nhân vật được quản lý bởi Animator Controller — đồ thị trạng thái (state machine) xác định khi nào chuyển đổi giữa các animation clip. Mỗi trạng thái tương ứng với một animation (Idle, Run, Jump, Fall, Dash, Attack, Skill, Die) và chuyển trạng thái dựa trên điều kiện như tốc độ di chuyển, IsGrounded hay đầu vào người chơi. Sơ đồ trạng thái Animator Controller được minh họa trong Hình 1.1.

Hình 1.1: Sơ đồ trạng thái Animator Controller của nhân vật 2D (Idle → Run → Jump → Fall → Dash → Attack → Die)

Transition giữa các animation cần được thiết lập với Exit Time bằng 0 và Transition Duration ngắn (0.05–0.1 giây) để animation chuyển đổi tức thì theo input, không tạo cảm giác chậm trễ. Đây là chi tiết kỹ thuật nhỏ nhưng tác động lớn đến combat feel.

### 1.3.2. Hệ thống chiến đấu (Combat System)

Hệ thống chiến đấu là trung tâm của trải nghiệm game 2D Action RPG. Chất lượng chiến đấu quyết định phần lớn sự thành công hay thất bại của tựa game — một combat system tốt mang lại cảm giác thỏa mãn, công bằng và có chiều sâu kỹ năng, trong khi combat system kém tạo ra sự bực bội và chán nản.

a) Hitbox và Hurtbox

Hai khái niệm cốt lõi trong chiến đấu 2D là hitbox và hurtbox. **Hitbox** là vùng không gian mà một đòn đánh hoặc đạn có thể gây sát thương. **Hurtbox** là vùng không gian mà một đối tượng có thể nhận sát thương. Sát thương chỉ xảy ra khi hitbox của bên tấn công giao thoa (overlap) với hurtbox của bên bị tấn công.

Trong thực tế thiết kế, hurtbox thường nhỏ hơn sprite (hình ảnh) của nhân vật, và hitbox của đòn tấn công thường khớp chính xác với phần "vũ khí" trong animation frame đang hiển thị. Cách thiết kế này tạo ra cảm giác "fair" — đòn trúng khi nhìn thực sự có vẻ trúng, không phải khi chỉ đứng gần nhau. Trong Unity 2D, hitbox và hurtbox thường được triển khai bằng BoxCollider2D hoặc PolygonCollider2D với Is Trigger = true, phát sự kiện OnTriggerEnter2D khi giao thoa.

b) Hệ thống chỉ số nhân vật

Các chỉ số cơ bản trong game Action RPG bao gồm:

▪ **HP (Health Point)**: Điểm máu — lượng sức chịu đựng, bằng 0 thì chết
▪ **MP (Mana Point)**: Năng lượng dùng để kích hoạt kỹ năng, tự hồi dần hoặc khi tấn công
▪ **ATK (Attack)**: Sát thương gây ra mỗi đòn đánh cơ bản hoặc hệ số nhân cho kỹ năng
▪ **DEF (Defense)**: Giảm bớt sát thương nhận vào theo công thức xác định
▪ **SPD (Speed)**: Tốc độ di chuyển và hồi chiêu kỹ năng
▪ **CRIT Rate**: Tỉ lệ phần trăm xác suất đòn đánh gây sát thương gấp đôi (Critical Hit)
▪ **CRIT Damage**: Hệ số nhân sát thương khi xảy ra Critical Hit (thường 150–200%)

Công thức tính sát thương thường tuân theo dạng: sát thương cuối cùng = (ATK × skill_multiplier × element_multiplier − DEF × reduction_factor), với giá trị tối thiểu là 1 để luôn gây ít nhất 1 sát thương, tránh tình huống người chơi hoàn toàn không thể gây sát thương.

c) Hệ thống hồi chiêu (Cooldown System)

Kỹ năng thường có thời gian hồi chiêu (cooldown) — khoảng thời gian bắt buộc chờ sau khi sử dụng. Cooldown tạo ra yếu tố chiến thuật: người chơi phải cân nhắc thời điểm sử dụng kỹ năng quan trọng, không thể spam vô hạn. Đây là cơ chế cân bằng cơ bản nhất trong thiết kế kỹ năng game, ngăn chặn một kỹ năng mạnh bị khai thác liên tục để loại bỏ mọi thách thức.

Trong Unity, cooldown thường được quản lý bằng cách lưu `lastUsedTime` (thời điểm sử dụng cuối) và kiểm tra `Time.time - lastUsedTime >= cooldownDuration` trước khi cho phép kích hoạt lại. Giao diện người dùng thường hiển thị cooldown bằng hiệu ứng fade-out trên icon kỹ năng, giúp người chơi theo dõi trực quan.

### 1.3.3. Hệ thống kỹ năng và tương khắc nguyên tố

a) Phân loại kỹ năng

Kỹ năng trong game RPG được phân loại theo nhiều cách. Theo cơ chế kích hoạt:

▪ **Active (Chủ động)**: Người chơi kích hoạt bằng phím tắt, tiêu tốn MP. Đây là loại phổ biến nhất, trực tiếp ảnh hưởng đến chiến đấu ngay lập tức
▪ **Passive (Bị động)**: Luôn có hiệu lực, không cần kích hoạt. Thường tăng chỉ số hoặc thêm hiệu ứng đặc biệt vĩnh viễn
▪ **Toggle (Bật/tắt)**: Kích hoạt để bật, tốn MP liên tục mỗi giây khi đang hoạt động
▪ **Proc**: Tự kích hoạt theo xác suất khi điều kiện xảy ra (ví dụ: 15% cơ hội bắt lửa khi đánh thường)

Theo hình thức tác động: Melee (tác động gần), Projectile (đạn bay), AoE (vùng tròn), Buff (tăng chỉ số bản thân), Debuff (giảm chỉ số địch), Heal (hồi phục HP).

b) Hệ thống tương khắc nguyên tố (Elemental Interaction)

Hệ thống tương khắc nguyên tố là cơ chế chiến lược dựa trên triết lý Ngũ Hành của phương Đông được tùy biến lại trong mã nguồn hệ thống để tối ưu cân bằng game, định nghĩa mối quan hệ tương khắc giữa các nguyên tố cơ bản. Trong bối cảnh game, vòng tương khắc hoạt động theo thứ tự: Kim (Metal) khắc Mộc (Wood), Mộc (Wood) khắc Thủy (Water), Thủy (Water) khắc Hỏa (Fire), Hỏa (Fire) khắc Thổ (Earth), Thổ (Earth) khắc Kim (Metal).

Cơ chế tính toán tương khắc trong game thường hoạt động theo mô hình hệ số nhân (multiplier):

▪ Nguyên tố tấn công **khắc** nguyên tố bị tấn công: sát thương × **1.5** (damage buff)
▪ Nguyên tố tấn công **bị khắc** bởi nguyên tố đối phương: sát thương × **0.75** (damage debuff)
▪ Nguyên tố **trung lập** (không có quan hệ khắc): sát thương × **1.0** (bình thường)

Bảng 1.3 trình bày đầy đủ ma trận hệ số nhân sát thương của 6 nguyên tố trong Mutants Arena — nền tảng cho toàn bộ thiết kế class và chiến thuật chiến đấu:

**Bảng 1.3: Ma trận tương khắc 6 nguyên tố (hàng = nguyên tố tấn công, cột = nguyên tố bị tấn công)**

| ↓ Tấn công / Bị tấn công → | Kim | Mộc | Thủy | Hỏa | Thổ | Phong |
|---|---|---|---|---|---|---|
| **Kim** | 1.0 | **1.5** | 1.0 | 1.0 | 0.75 | 1.0 |
| **Mộc** | 0.75 | 1.0 | **1.5** | 1.0 | 1.0 | 1.0 |
| **Thủy** | 1.0 | 0.75 | 1.0 | **1.5** | 1.0 | 1.0 |
| **Hỏa** | 1.0 | 1.0 | 0.75 | 1.0 | **1.5** | 1.0 |
| **Thổ** | **1.5** | 1.0 | 1.0 | 0.75 | 1.0 | 1.0 |
| **Phong** | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 |

Vòng tương khắc 5 chiều chuẩn hệ thống: Kim (Metal) → Mộc (Wood) → Thủy (Water) → Hỏa (Fire) → Thổ (Earth) → Kim (Metal) (ký hiệu → nghĩa là "khắc", gây ×1.5; chiều ngược lại chịu ×0.75). Đây là cấu trúc vòng khép kín đối xứng cho 5 nguyên tố cơ bản. Nguyên tố thứ sáu Phong (Wind) là hệ trung lập đặc biệt, không nằm trong vòng khắc chế chuẩn này (trả về null) nhằm đem lại trải nghiệm chiến đấu độc lập chiến thuật và cân bằng game.

Ngoài các nguyên tố cơ bản, nguyên tố thứ sáu Phong (Wind) được bổ sung thêm để tạo ra 6 class nhân vật đa dạng hơn, đồng thời đóng vai trò là mảnh ghép trung lập, tạo ra các tổ hợp mới thú vị trong hệ thống Gene Fusion (tổng hợp gene) của đề tài. Đặc biệt, Phong được thiết kế để kết hợp với Kim tạo thành cặp Hybrid Kim - Phong độc đáo.

c) Hệ thống hiệu ứng trạng thái (Status Effect)

Hiệu ứng trạng thái là các debuff tạm thời áp lên mục tiêu sau khi kỹ năng của một nguyên tố nhất định trúng đủ số lần. Ví dụ: Hỏa gây Burn (cháy — mất HP liên tục), Thủy gây Freeze (đóng băng — giảm tốc độ), Phong gây Knockback (đẩy ngược). Hệ thống này thêm một lớp chiều sâu nữa vào combat, khuyến khích người chơi chú ý đến nguyên tố kẻ địch và tận dụng điểm yếu.

### 1.3.4. Hệ thống tiến hóa nhân vật

a) Hệ thống lên cấp (Level Up)

Lên cấp là cơ chế tiến hóa cơ bản nhất trong mọi game RPG. Khi nhân vật tích lũy đủ điểm kinh nghiệm (EXP), level tăng lên kéo theo tăng tự động các chỉ số cơ bản (HP, ATK, DEF) theo hệ số xác định cho từng class. Lượng EXP cần để lên cấp tiếp theo thường tăng theo hàm lũy thừa hoặc bảng tra cứu, làm chậm tốc độ tăng trưởng ở level cao và kéo dài thời gian chơi.

b) Hệ thống trang bị và nâng cấp

Trang bị (vũ khí, giáp, phụ kiện) bổ sung chỉ số cho nhân vật dựa trên item đang mặc. Trang bị có độ hiếm (rarity) khác nhau — Common, Rare, Epic, Legendary — với chỉ số và hiệu ứng đặc biệt ngày càng mạnh hơn. Hệ thống nâng cấp trang bị (Enhancement) cho phép tăng bậc item (+1 đến +20) qua vật liệu nâng cấp (đá nâng bậc), với xác suất thất bại ở bậc cao tạo ra rủi ro và căng thẳng thú vị. Luồng trang bị được minh họa trong Hình 1.2.

Hình 1.2: Luồng loot và nâng cấp trang bị (Drop → Inventory → Equip → Enhance → Sell/Discard)

c) Hệ thống Gene và tiến hóa đặc biệt — Điểm đặc trưng của đề tài

Hệ thống Gene là cơ chế tiến hóa cốt lõi và nét khác biệt nhất của Mutants Arena so với các game 2D RPG thông thường. Thay vì lớp nhân vật (class) cố định từ đầu, người chơi dần định hình nguyên tố cho nhân vật thông qua việc thu thập và nâng cấp Gene. Mỗi Gene gắn với một trong 6 nguyên tố, ảnh hưởng trực tiếp lên bộ chỉ số và kỹ năng đặc thù của nguyên tố đó, đồng thời tăng hệ số tương khắc khi chiến đấu với quái vật và boss cùng nguyên tố.

Gene có 5 bậc nâng cấp (Tier), mỗi bậc yêu cầu tài nguyên đặc thù và mở ra thêm chỉ số lẫn kỹ năng, được trình bày trong Bảng 1.4:

**Bảng 1.4: Cấu trúc 5 Tier của hệ thống Gene trong Mutants Arena**

| Tier | Tài nguyên nâng cấp | Bonus chỉ số nguyên tố | Mở khóa |
|---|---|---|---|
| Tier 1 — Sơ cấp | Gene Fragment × 10 | +5% ATK nguyên tố | Kỹ năng nguyên tố cơ bản |
| Tier 2 — Trung cấp | Gene Fragment × 30 | +10% ATK, +5% HP | Kỹ năng cấp 2 |
| Tier 3 — Cao cấp | Gene Core × 5 | +15% ATK, +10% HP, +5% DEF | Kỹ năng cấp 3 |
| Tier 4 — Tinh anh | Gene Core × 15 | +20% ATK, +15% HP, +10% DEF | Passive đặc biệt nguyên tố |
| Tier 5 — Tối thượng | Gene Core × 30 + Gene Essence | +30% toàn bộ chỉ số nguyên tố | Mở khóa Multi-Gene và Hybrid Fusion |

#### Multi-Gene
*   Multi-Gene được mở khóa khi Gene đạt Tier 5. 
*   Người chơi có thể sử dụng một Gene chính và tối đa hai Gene phụ (quy định hiệu quả Gene chính là 100%, mỗi Gene phụ bổ sung thêm khoảng 30% chỉ số hoặc hiệu ứng). 
*   Gene chính quyết định hướng phát triển chính của nhân vật. 
*   Gene phụ bổ sung thêm chỉ số hoặc hiệu ứng phụ. 
*   Cơ chế này giúp người chơi tạo ra nhiều hướng build khác nhau (với 6 nguyên tố, hỗ trợ tổng cộng 60 tổ hợp Multi-Gene khác biệt). 

#### Hybrid Fusion
*   Hybrid Fusion là cơ chế dung hợp Gene cấp cao. 
*   Người chơi cần hai Gene Tier 5 thuộc hai nguyên tố khác nhau (theo các cặp cố định hợp lệ). 
*   Sau khi dung hợp thành công, nhân vật nhận được Gene Hybrid. 
*   Gene Hybrid mở khóa bộ kỹ năng đặc biệt và các chỉ số cộng thêm độc nhất. 
*   Đây là mục tiêu phát triển cuối game của hệ thống Gene nhằm gia tăng tính chiến thuật.

#### Gene Tối Thượng (Ultimate Gene)
----- [BẮT ĐẦU PHẦN THÊM MỚI] -----
*   Gene Tối Thượng là cấp tiến hóa tối cao của nhân vật, được mở khóa sau khi đã dung hợp thành công Hybrid Gene.
*   Người chơi tích lũy EXP Tối Thượng thông qua các hoạt động trong game như tiêu diệt quái vật dã ngoại, Boss phó bản hoặc sử dụng vật phẩm hỗ trợ.
*   Khi tích lũy đạt mốc 1,000,000 EXP Tối Thượng, hệ thống tự động kích hoạt trạng thái Gene Tối Thượng (`is_ultimate = true`).
*   Trạng thái này nhân x1.5 toàn bộ các chỉ số thuộc tính cơ bản của nhân vật (gồm HP, MP, ATK, DEF).
*   Đồng thời hiển thị hào quang Tối Thượng rực rỡ phía sau lưng nhân vật (theo 3 loại Aura dựa trên hệ nguyên tố lai) và cập nhật ký hiệu ✦ trên giao diện HUD.
----- [KẾT THÚC PHẦN THÊM MỚI] -----


### 1.3.7. Trọng tâm đề tài: Gene Evolution và Multiplayer là hai trục chính

Tên đề tài “Phát triển trò chơi Mutants Arena với hệ thống tiến hóa Gene bằng Unity” đã chỉ rõ: giá trị nghiên cứu của đề tài **không nằm ở việc xây dựng thêm một game 2D Action RPG** — thiếu gì thị trường chắc chắn không thiếu — mà nằm ở hai đóng góp đặc thù: **hệ thống tiến hóa Gene Ngũ Hành** và **kiến trúc multiplayer Server-Authoritative** cho game 2D side-scrolling. Việc đặt vấn đề như vậy tạo thành **hai trục chính** xuyên suốt toàn bộ báo cáo: mọi tính năng còn lại (Quest, NPC, Shop, Trang bị, Bản đồ) đóng vai trò *gameplay nền* để hai trục này có ngữ cảnh phát huy.

a) Phân biệt Gene với Level và Trang bị

Một câu hỏi thường gặp là *“Gene khác gì với Level và Trang bị mà phải xây thành một hệ thống riêng?”*. Bảng 1.5 đối sánh ba cơ chế phát triển nhân vật theo bốn tiêu chí: nguồn tăng trưởng, tính mất mát, không gian build và ảnh hưởng đến kỹ năng.

**Bảng 1.5: So sánh ba cơ chế phát triển nhân vật**

| Tiêu chí | Level up thường | Trang bị (Equipment) | **Gene Ngũ Hành** |
|---|---|---|---|
| Nguồn tăng trưởng | EXP từ giết quái, quest | Loot → enhance | Gene EXP + Material + Gold |
| Không gian build | Tuyến tính (1 chiều) | Tổ hợp slot cố định | **2 chiều: nguyên tố × Tier + Hybrid** |
| Khả năng bị mất | Không | Có (rotation, drop khi PvP) | Có (success rate < 100% ở Tier cao) |
| Ảnh hưởng kỹ năng | Tăng damage tuyến tính | Thêm stat | **Thay đổi ma trận tương khắc + mở skill mới theo Tier + Hybrid** |
| Ảnh hưởng vai trò trong party | Không | Có (tank/dps theo set) | **Rất rõ: mỗi nguyên tố có vai trò đối ứng** |

Từ bảng đối sánh có thể thấy Gene không trùng với hai cơ chế còn lại: trong khi Level cộng stat tuyến tính và Trang bị cộng stat theo slot, Gene **thay đổi bản chất damage** (qua nhân tử tương khắc ×1.5/×0.75) và **mở khoá kỹ năng** phụ thuộc vào cặp nguyên tố đã dụng hợp (Hybrid). Hai đặc điểm này khiến Gene trở thành trục **chiều sâu tuỳ biến** — thay vì ép người chơi vào một hiướng phát triển duy nhất, hệ thống cho phép tối thiểu $6 \times 5 = 30$ build chiến binh cơ bản cộng với 15 tổ hợp Hybrid, tổng cộng trên 45 kiểu build phân biệt được.

b) Gene tăng “tính chơi lại” (replayability) và tính đồng đội

Trong bối cảnh multiplayer, Gene không chỉ là cơ chế cá nhân mà còn là **cơ chế phiên cho party**: một đội 4 người vào phó bản có boss hệ Hỏa lý tưởng có một Thuỷ (counter), một Mộc (support nhờ debuff thiêu đốt), một Phong (burst), một Thổ (tank). Việc **mỗi nguyên tố có vai trò đối ứng** khiến Gene tự nhiên trở thành công cụ thiết kế đội hình, tương tự hệ “trinity tank/dps/healer” của MMORPG cổ điển nhưng phong phú hơn nhờ ma trận 6×6. Đây là lý do trục Gene và trục Multiplayer trong đề tài không phát triển độc lập, mà **gắn chặt với nhau**.

c) Câu hỏi nghiên cứu chính của đề tài

Từ hai trục đã xác định, đề tài trả lời bốn câu hỏi nghiên cứu:

1. *Luật nhân tương khắc và công thức damage* nào cho phép 6 nguyên tố tạo ra ma trận cân bằng và có ý nghĩa chiến thuật (không có “meta” áp đảo)?
2. *Quy trình nâng cấp Tier và Fusion Hybrid* nên được cấu hình hoàn toàn trong CSDL hay mã hoá cứng, và ảnh hưởng đến khả năng vận hành như thế nào?
3. *Kiến trúc multiplayer nào* (Client Authoritative / Server Authoritative / Hybrid Prediction–Reconciliation) phù hợp cho 2D side-scrolling action có RPG persistence, chống gian lận nhưng vẫn duy trì phản hồi ≤1 frame?
4. *Stack công nghệ nào* (Unity NGO vs Photon Quantum vs Mirror vs WebSocket thuần) đáp ứng được yêu cầu về latency, chi phí và tốc độ phát triển trong phạm vi đồ án?

Bốn câu hỏi này được trả lời trực tiếp ở các mục: §3.2 (công thức damage), §3.3 (Gene config CSDL-driven), §3.0 và §3.3 phần “Lý do chọn Server Authoritative”, và §1.5 đoạn “Lý do chọn NGO + SignalR” ngay sau đây.

---

## 1.4. Cơ chế hành vi kẻ địch và hệ thống giai đoạn boss

### 1.4.1. Tổng quan về AI trong game

Trí tuệ nhân tạo trong game (Game AI) là tập hợp các kỹ thuật lập trình giúp các nhân vật phi người chơi (Non-Player Character — NPC) và quái vật có hành vi phản ứng linh hoạt với người chơi, tạo ra cảm giác đối thủ "thông minh" và đáng thách thức. Khác với AI hàn lâm tập trung vào học máy hay tối ưu toán học, mục tiêu của Game AI đơn giản hơn nhưng cũng phức tạp theo cách khác: tạo ra trải nghiệm vui vẻ, công bằng và đủ thử thách cho người chơi ở nhiều trình độ khác nhau.

Trong ngành công nghiệp game, các kỹ thuật Game AI thường được chọn theo tiêu chí đơn giản, hiệu quả và dễ điều chỉnh nội dung — không nhất thiết phải là thuật toán tối ưu về lý thuyết. Hai phương pháp được ứng dụng phổ biến nhất là Finite State Machine và Behavior Tree, mỗi phương pháp phù hợp với loại đối tượng và độ phức tạp hành vi khác nhau. Bên cạnh đó, cơ chế điều hướng trong môi trường 2D side-scrolling cũng đặt ra yêu cầu riêng do đặc thù của trọng lực và địa hình nền tảng.

### 1.4.2. Các kỹ thuật AI phổ biến trong game 2D

a) Finite State Machine (FSM)

Finite State Machine là kỹ thuật tổ chức hành vi của AI thành các trạng thái rời rạc như Idle, Patrol, Chase, Attack và Dead. Tại mỗi thời điểm, đối tượng chỉ ở đúng một trạng thái và chuyển sang trạng thái khác khi điều kiện cụ thể được thỏa mãn — ví dụ, quái vật chuyển từ Patrol sang Chase khi phát hiện người chơi trong tầm nhìn, rồi từ Chase sang Attack khi đến đủ gần. FSM được ưa chuộng cho quái vật thường nhờ tính đơn giản, dễ triển khai và hiệu năng cao, phù hợp khi cần xử lý nhiều đối tượng đồng thời trên cùng một map.

Hình 1.3: Sơ đồ chuyển trạng thái của quái vật trong game 2D (Idle → Patrol → Chase → Attack → Dead)

b) Behavior Tree (BT)

Behavior Tree tổ chức hành vi theo cấu trúc cây phân cấp, cho phép xây dựng logic AI phức tạp hơn bằng cách kết hợp các hành vi đơn giản thành chuỗi và nhóm có điều kiện. Phương pháp này phù hợp cho boss và NPC cần hành vi đa dạng, có khả năng ưu tiên và phân nhánh linh hoạt. BT được ứng dụng rộng rãi trong các tựa game thương mại lớn nhờ dễ mở rộng khi thêm hành vi mới mà không ảnh hưởng đến các phần còn lại. Chi tiết về cách áp dụng hai kỹ thuật này trong đề tài được trình bày tại Chương 3.

### 1.4.3. Boss AI và hệ thống giai đoạn (Phase System)

Boss trong game 2D Action RPG là dạng đối thủ đặc biệt được thiết kế để là điểm cao trào của mỗi khu vực, đòi hỏi người chơi vận dụng tổng hợp các kỹ năng đã học. Đặc trưng nổi bật nhất của Boss AI hiện đại là Phase System — cơ chế thay đổi hành vi của boss theo ngưỡng HP, tạo ra cảm giác boss "biến đổi" và ngày càng nguy hiểm khi yếu dần.

Cấu trúc Phase System điển hình gồm ba giai đoạn (Hình 1.4): giai đoạn đầu với hành vi cơ bản để người chơi làm quen; giai đoạn giữa bổ sung kỹ năng mới khi HP giảm; giai đoạn cuối với tốc độ và sát thương tối đa khi boss "tuyệt vọng". Người chơi buộc phải liên tục thích nghi chiến thuật thay vì dùng một chiến lược duy nhất xuyên suốt trận đấu, tăng đáng kể độ hấp dẫn và tính thách thức.

Hình 1.4: Boss Phase System — thay đổi hành vi theo ngưỡng HP (giai đoạn 1 → 2 → 3)

Dữ liệu Phase System trong đề tài được lưu trong cơ sở dữ liệu dưới dạng JSON, cho phép cấu hình linh hoạt từng boss mà không cần thay đổi logic code. Cách triển khai cụ thể được mô tả chi tiết tại Chương 3.

---

## 1.5. Kiến trúc Client-Server cho game multiplayer

### 1.5.1. Các mô hình mạng trong game multiplayer và lựa chọn cho đề tài

Game multiplayer có thể được xây dựng theo nhiều mô hình kiến trúc mạng, mỗi mô hình có ưu điểm và nhược điểm tùy thuộc vào quy mô, yêu cầu bảo mật và ngân sách vận hành. Bảng 1.0 so sánh ba mô hình phổ biến, từ đó làm rõ lý do lựa chọn áp dụng cho đề tài Mutants Arena.

**Bảng 1.0: So sánh các mô hình mạng trong game multiplayer**

| Tiêu chí | Peer-to-Peer (P2P) | Listen Server | Dedicated Server |
|---|---|---|---|
| Server chuyên dụng | Không | Không (host = client) | Có |
| Chống gian lận | Yếu | Yếu | Tốt |
| Host advantage | Có | Có | Không |
| Chi phí vận hành | Thấp | Thấp | Trung bình |
| Độ tin cậy | Thấp (phụ thuộc host) | Thấp | Cao |
| Phù hợp quy mô | Nhỏ, không thương mại | Dev/test | Thương mại |

a) Mô hình Peer-to-Peer (P2P)

Trong mô hình P2P, các máy client kết nối trực tiếp với nhau mà không có server trung tâm. Một người chơi đóng vai trò "host" — vừa là client vừa chạy logic game cục bộ, các client còn lại kết nối vào host và nhận trạng thái game từ đó. Ưu điểm là không cần chi phí thuê server. Tuy nhiên, nhược điểm nghiêm trọng làm P2P không phù hợp cho đề tài: host có lợi thế không công bằng (host advantage), không thể chống gian lận do logic chạy trên máy người chơi, và game sụp đổ khi host ngắt kết nối.

b) Mô hình Listen Server

Listen Server là mô hình trung gian: một người chơi vừa là client vừa chạy server, các client khác kết nối vào. Phù hợp cho giai đoạn phát triển và thử nghiệm nội bộ vì không cần server riêng, nhưng vẫn mang đầy đủ nhược điểm của P2P về host advantage và độ tin cậy.

c) Mô hình Dedicated Server — **Lựa chọn của đề tài**

Trong mô hình Dedicated Server, một tiến trình server chuyên dụng chạy logic game hoàn toàn độc lập, không có người chơi nào trên đó. Tất cả client kết nối vào server này, server nhận input, tính toán kết quả, cập nhật trạng thái game và đồng bộ lại cho tất cả. **Đề tài Mutants Arena áp dụng mô hình Dedicated Server** theo nguyên lý Server Authoritative — đây là lý do trực tiếp dẫn đến các quyết định thiết kế trong Chương 2 và 3:

▪ **Chống gian lận**: Mọi tính toán HP, sát thương, spawn item đều thực hiện trên server — client không thể giả mạo kết quả
▪ **Nhất quán trạng thái**: Toàn bộ người chơi trong cùng zone nhìn thấy cùng một trạng thái game do server là nguồn sự thật duy nhất
▪ **Không có host advantage**: Server chạy độc lập trên VPS, mọi người chơi có điều kiện kết nối như nhau
▪ **Triển khai Docker**: Build Unity Dedicated Server headless (Linux), đóng gói thành container và triển khai lên Linux VPS cùng API server và MySQL trong Docker Compose

Unity NGO hỗ trợ cả Dedicated Server lẫn Listen Server với cùng codebase, nhờ các cờ `IsServer` / `IsClient` trong `NetworkBehaviour`. Trong quá trình phát triển cục bộ, đề tài sử dụng Listen Server để debug nhanh; khi triển khai production chuyển sang Dedicated Server bằng cách build theo target `Dedicated Server` trong Unity Build Settings mà không cần sửa code.

### 1.5.2. Kiến trúc Server Authoritative

Server Authoritative Architecture là nguyên lý thiết kế trong đó server là nguồn thông tin duy nhất và đáng tin cậy (single source of truth) về trạng thái game. Mọi quyết định quan trọng đều do server tính toán và xác nhận, client chỉ gửi input và nhận kết quả.

a) Nguyên tắc hoạt động

Theo nguyên tắc Server Authoritative, khi người chơi nhấn phím tấn công, client gửi yêu cầu lên server. Server nhận, kiểm tra tính hợp lệ (khoảng cách, cooldown, trạng thái...), tính toán sát thương, cập nhật HP enemy và broadcast kết quả về cho tất cả clients. Clients chỉ hiển thị kết quả nhận được. Luồng này được minh họa trong Hình 1.5.

Hình 1.5: Luồng Server Authoritative — Client gửi input, Server xử lý và xác nhận, broadcast kết quả cho tất cả clients

b) Lợi ích bảo mật

Kiến trúc Server Authoritative ngăn chặn hầu hết hình thức gian lận phổ biến:

▪ Không thể tự thay đổi HP bản thân hay enemy (god mode, HP hack) vì HP chỉ do server cập nhật
▪ Không thể teleport hay tăng tốc (speed hack) vì server kiểm tra vị trí hợp lệ
▪ Không thể tự thêm vật phẩm hay vàng vì inventory chỉ do server cấp phát
▪ Không thể thay đổi kết quả sát thương vì tính toán chỉ xảy ra trên server

c) Client-side Prediction và Server Reconciliation

Nhược điểm của Server Authoritative thuần túy là input lag — người chơi phải đợi server phản hồi mới thấy kết quả, tạo cảm giác giật với kết nối latency cao. Giải pháp là **Client-side Prediction**: client áp dụng kết quả input ngay lập tức trên màn hình cục bộ (dự đoán server sẽ đồng ý), đồng thời gửi input lên server. Khi server phản hồi, client so sánh — nếu khớp thì không làm gì, nếu sai lệch thì điều chỉnh (Server Reconciliation) về giá trị đúng từ server. Kỹ thuật này tạo cảm giác responsive trong khi server vẫn là nguồn sự thật cuối cùng.

### 1.5.3. Giao thức mạng trong game real-time

a) TCP và UDP

Game real-time sử dụng hai giao thức chính: TCP (đảm bảo giao hàng, đảm bảo thứ tự, overhead cao hơn) và UDP (không đảm bảo giao hàng, không đảm bảo thứ tự, nhanh hơn và trễ thấp hơn). Trong game action real-time, UDP thường dùng cho dữ liệu di chuyển và combat cần cập nhật liên tục (chấp nhận mất gói vì gói tiếp theo sẽ đến ngay), trong khi TCP hoặc HTTP dùng cho dữ liệu quan trọng như đăng nhập, lưu game và giao dịch vật phẩm.

Unity NGO sử dụng giao thức riêng trên nền UDP với cơ chế đảm bảo giao hàng có chọn lựa — một số loại message được đánh dấu "reliable" (đảm bảo đến nơi) và một số "unreliable" (best-effort, ưu tiên tốc độ). Điều này cho phép tối ưu tùy theo tính chất dữ liệu.

b) RESTful API và WebSocket

REST API (sử dụng HTTP methods chuẩn: GET, POST, PUT, DELETE) phù hợp cho các thao tác không cần real-time như đăng nhập, tải dữ liệu nhân vật, lưu tiến trình và tra cứu bảng xếp hạng. Các REST API được bảo vệ bằng JWT token trong header của mỗi request.

WebSocket là giao thức full-duplex (hai chiều đồng thời) trên nền HTTP, cho phép server chủ động đẩy dữ liệu đến client bất kỳ lúc nào. Phù hợp cho thông báo real-time như mời vào tổ đội, chat nhóm và cập nhật trạng thái online. SignalR (thư viện ASP.NET Core) xây dựng trên nền WebSocket, đơn giản hóa lập trình real-time communication với khái niệm Hub và Group.

### 1.5.4. Kiến trúc Zone-based Server

Để hỗ trợ nhiều người chơi đồng thời trên nhiều bản đồ, game MMORPG chia thế giới thành các zone (vùng). Mỗi zone là không gian game độc lập, chỉ đồng bộ dữ liệu giữa những người chơi đang ở cùng zone. Điều này giảm đáng kể lượng dữ liệu cần đồng bộ so với việc broadcast toàn server.

Trong một server process đơn, **ZoneRoomRegistry** đóng vai trò bộ đăng ký và quản lý toàn bộ các zone đang hoạt động. Khi người chơi chuyển bản đồ, họ rời zone cũ và đăng ký vào zone mới. Kiến trúc này được minh họa trong Hình 1.6.

Hình 1.6: Kiến trúc Zone-based Server — một server process quản lý nhiều zone (bản đồ chung và instance phó bản)

**Instance (Phó bản)** là loại zone đặc biệt — riêng tư, chỉ cho một nhóm người chơi cụ thể. Khi nhóm vào dungeon, hệ thống tạo instance mới và gán cả nhóm vào đó. Khi dungeon kết thúc, instance bị xóa. Cơ chế này đảm bảo các nhóm khác nhau không can thiệp vào nhau dù đang trong cùng loại dungeon.

### 1.5.5. Xác thực người dùng với JWT

JWT (JSON Web Token) là chuẩn mở theo đặc tả RFC 7519, cho phép truyền thông tin an toàn dưới dạng JSON object có ký số. JWT được sử dụng rộng rãi trong ứng dụng web và game hiện đại để xác thực người dùng và phân quyền API.

a) Cấu trúc JWT

JWT gồm ba phần ngăn cách bằng dấu chấm (`.`):

▪ **Header**: JSON chứa loại token (`typ: "JWT"`) và thuật toán ký (`alg: "HS256"`), mã hóa Base64URL
▪ **Payload**: JSON chứa các "claim" — user_id, username, thời hạn (exp), thời gian phát hành (iat). Cũng là Base64URL — không mã hóa bảo mật, chỉ encoding
▪ **Signature**: Chữ ký HMAC-SHA256 của (Header + "." + Payload) với secret key chỉ server biết. Đây là phần đảm bảo tính toàn vẹn — không có secret key thì không thể tạo chữ ký hợp lệ

b) Luồng xác thực JWT trong game

Luồng xác thực JWT điển hình (Hình 1.7):

▪ **Bước 1**: Client gửi username và password đến POST /api/auth/login
▪ **Bước 2**: Server xác minh, tạo JWT chứa user_id và exp, ký bằng secret key
▪ **Bước 3**: Server trả JWT về cho client
▪ **Bước 4**: Client lưu token (PlayerPrefs trong Unity)
▪ **Bước 5**: Mọi request API tiếp theo gửi kèm header `Authorization: Bearer {token}`
▪ **Bước 6**: Server xác minh chữ ký JWT, trích xuất user_id và xử lý
▪ **Bước 7**: Khi kết nối vào NGO game server, client gửi kèm JWT để game server xác minh danh tính trước khi chấp nhận kết nối

Hình 1.7: Luồng xác thực JWT từ đăng nhập REST API đến kết nối Game Server (NGO Connection Approval)

### 1.5.6. Hệ thống Dungeon (Phó bản) và Party System

a) Wave-based Dungeon

Dungeon là khu vực game riêng tư được tạo ra cho một nhóm người chơi, cách biệt với thế giới chung. Trong **Wave-based Dungeon**, người chơi chiến đấu qua nhiều đợt (wave) quái liên tiếp. Mỗi wave spawn một lượng quái theo cấu hình, sau khi tiêu diệt hết wave hiện tại thì wave tiếp theo bắt đầu với độ khó tăng dần. Wave cuối cùng là boss. Khi boss bị tiêu diệt, dungeon hoàn thành và nhóm nhận phần thưởng. Luồng này được minh họa trong Hình 1.8.

Hình 1.8: Luồng Wave-based Dungeon (Wave 1 → ... → Wave N → Boss → Clear → Reward)

Cấu hình wave được lưu trong database dưới dạng JSON (`wave_config`), cho phép game designer tùy chỉnh loại quái, số lượng và thời gian giữa wave mà không cần sửa code.

b) Party System (Hệ thống tổ đội)

Party System cho phép người chơi tạo nhóm hoặc tham gia nhóm để cùng trải nghiệm nội dung multiplayer. Trưởng nhóm (Leader) có quyền mời thành viên và khởi động dungeon. Khi Leader vào dungeon, tất cả thành viên được đưa vào cùng một instance.

Giao tiếp party thực hiện qua SignalR (WebSocket) để các sự kiện như mời vào nhóm, thành viên rời nhóm, bắt đầu dungeon được thông báo real-time bất kể thành viên đang ở bản đồ nào. Kênh SignalR này khác với Unity NGO — NGO đồng bộ combat và vị trí trong game, còn SignalR phục vụ các sự kiện xã hội và lobby không đòi hỏi độ trễ milisecond.

---

## 1.6. Các công nghệ và công cụ sử dụng

### 1.6.1. Unity 2D Game Engine và Netcode for GameObjects

Unity là game engine đa nền tảng do Unity Technologies phát triển, ra mắt năm 2005. Theo thống kê năm 2023, hơn 50% game trên nền tảng di động được phát triển bằng Unity, và đây là engine được sử dụng phổ biến nhất trong phát triển game indie và AA. Unity hỗ trợ xuất bản lên hơn 25 nền tảng từ một codebase, bao gồm Windows, macOS, Linux, iOS, Android, WebGL.

a) Kiến trúc Entity-Component

Unity hoạt động theo mô hình Entity-Component: mọi đối tượng (GameObject) là tập hợp các Component độc lập. Các Component quan trọng trong game 2D:

▪ **Transform**: Vị trí (position), góc quay (rotation) và tỉ lệ (scale) của GameObject
▪ **SpriteRenderer**: Hiển thị sprite 2D lên màn hình với tùy chọn màu sắc và thứ tự layer
▪ **Rigidbody2D**: Mô phỏng vật lý 2D — trọng lực, va chạm vật lý
▪ **Collider2D** (BoxCollider2D, CircleCollider2D): Định nghĩa hình dạng vùng va chạm
▪ **Animator**: Quản lý hoạt ảnh và chuyển đổi giữa animation clip
▪ **MonoBehaviour Script**: Script C# tùy chỉnh với vòng lặp game (Awake, Start, Update, FixedUpdate)

b) Vòng lặp game trong Unity

Unity thực thi vòng lặp game theo thứ tự callback cố định:

▪ **Awake()**: Gọi một lần khi đối tượng khởi tạo. Dùng để khởi tạo tham chiếu nội bộ
▪ **Start()**: Gọi một lần sau khi tất cả Awake đã chạy. Dùng để khởi tạo phụ thuộc vào đối tượng khác
▪ **FixedUpdate()**: Gọi 50 lần/giây cố định. Dùng cho mọi xử lý vật lý và Rigidbody
▪ **Update()**: Gọi mỗi frame (phụ thuộc FPS). Dùng cho input và logic game thông thường
▪ **LateUpdate()**: Gọi sau Update. Dùng cho camera follow và cập nhật UI

c) Unity Netcode for GameObjects (NGO)

Unity NGO là framework multiplayer chính thức theo mô hình Server Authoritative. Các khái niệm cốt lõi:

▪ **NetworkObject**: Component đánh dấu GameObject cần đồng bộ qua mạng. Chỉ NetworkObject mới được spawn/despawn qua mạng
▪ **NetworkBehaviour**: Lớp cơ sở cho script có networking, cung cấp IsServer, IsClient, IsOwner, OwnerClientId
▪ **NetworkVariable\<T\>**: Biến tự động đồng bộ server → clients khi giá trị thay đổi (server write, client read-only)
▪ **ServerRpc**: Client gọi nhưng thực thi trên server. Cơ chế gửi input từ client lên server an toàn
▪ **ClientRpc**: Server gọi, thực thi trên tất cả (hoặc một số chỉ định) clients. Dùng để thông báo sự kiện
▪ **NetworkTransform**: Đồng bộ Transform (vị trí, góc quay) tự động qua mạng với interpolation
▪ **Connection Approval**: Callback cho server kiểm tra và chấp thuận/từ chối kết nối dựa trên JWT token

d) Lý do chọn Unity NGO so với Photon / Mirror / WebSocket thuần

Đây là một quyết định kiến trúc quan trọng, ảnh hưởng đến toàn bộ tầng đồng bộ multiplayer của đề tài. Bảng 1.6 so sánh bốn lựa chọn phổ biến cho game 2D Unity multiplayer theo bảy tiêu chí.

**Bảng 1.6: So sánh các giải pháp multiplayer cho game Unity**

| Tiêu chí | **Unity NGO** | Photon (PUN2/Fusion) | Mirror | WebSocket thuần |
|---|---|---|---|---|
| Mô hình | Server Authoritative (dedicated/host) | Relay-based hoặc rollback (Fusion) | Server Authoritative (dedicated) | Tự xây toàn bộ |
| Vendor / phụ thuộc | First-party Unity, miễn phí | Bên thứ ba, free tier 20 CCU | OSS, miễn phí | Không phụ thuộc |
| Tích hợp Unity Editor | Cao nhất (NetworkObject, NetworkTransform có Inspector) | Cao | Trung bình | Thấp (phải tự viết serializer) |
| Khả năng tự host trên VPS | Có (dedicated server Linux) | Photon Cloud (host bởi vendor) hoặc tự host (phức tạp) | Có | Có |
| Chi phí khi scale | Chỉ tốn VPS | CCU > 20 phải mua gói (>= ~95 USD/tháng) | Chỉ tốn VPS | Chỉ tốn VPS |
| Bảo trì lâu dài | Đang được Unity duy trì tích cực | Có nguy cơ thay đổi pricing | Cộng đồng OSS | Phụ thuộc hoàn toàn vào đề tài |
| Phù hợp đồ án (POC + báo cáo) | **Cao** | Trung bình (lock-in vendor) | Cao | Thấp (làm lại bánh xe) |

→ **Kết luận chọn NGO** vì ba lý do: (1) là giải pháp first-party của Unity, đảm bảo tương thích lâu dài với engine; (2) tự host được trên VPS qua Docker, không phụ thuộc dịch vụ cloud trả phí; (3) sẵn sàng cho mô hình Server Authoritative với `ServerRpc`/`ClientRpc`/`NetworkVariable` mức ngôn ngữ — đúng nhu cầu chống gian lận của RPG online.

→ **Vì sao vẫn cần SignalR bên cạnh NGO?** NGO dùng UDP (giao thức `unity-transport`) tối ưu cho dữ liệu game tần suất cao và chấp nhận mất gói, phù hợp combat / vị trí. Nhưng các sự kiện *meta-game* như mời party, chat lobby, thông báo bạn online — bản chất là *trạng thái phải tới nơi* và xảy ra ngay cả khi người chơi không trong cùng `NetworkManager` (ví dụ A đang ở map khác B) — phù hợp hơn với SignalR (WebSocket, TCP, qua REST stack). Cùng tồn tại hai kênh giúp **tách trách nhiệm**: NGO cho gameplay realtime, SignalR cho meta-state.

### 1.6.2. Ngôn ngữ lập trình C#

C# (C-Sharp) là ngôn ngữ lập trình hướng đối tượng hiện đại do Microsoft phát triển, ra mắt năm 2000. Là ngôn ngữ chính thức duy nhất trong Unity, C# chạy trên nền .NET runtime với garbage collection tự động và hệ thống kiểu mạnh (strongly typed).

a) Đặc điểm phù hợp với phát triển game

▪ **Strongly typed**: Phát hiện nhiều lỗi tại compile time, tránh bug runtime khó debug
▪ **Garbage Collection**: Quản lý bộ nhớ tự động. Lập trình viên game cần tránh tạo nhiều object tạm thời trong Update() để không kích hoạt GC pause gây giật
▪ **Async/Await**: Lập trình bất đồng bộ cho gọi API và load asset không chặn main thread
▪ **LINQ**: Truy vấn và xử lý tập hợp dữ liệu với cú pháp rõ ràng và type-safe
▪ **Generics**: Viết code tái sử dụng với nhiều kiểu dữ liệu (List\<T\>, Dictionary\<K,V\>)

b) Design Pattern phổ biến trong Unity

▪ **Singleton**: Đảm bảo một instance duy nhất toàn cục. Dùng cho GameManager, AudioManager, UIManager
▪ **Observer (Event System)**: Phát sự kiện, nhiều subscriber lắng nghe. Dùng cho OnPlayerDeath, OnLevelUp, OnItemPickup
▪ **Strategy**: Đóng gói thuật toán có thể thay đổi runtime. Dùng cho các SkillEffect khác nhau theo loại kỹ năng
▪ **State Machine**: Quản lý trạng thái rõ ràng. Dùng cho EnemyAI (Idle/Chase/Attack), PlayerState (Ground/Air/Attack)
▪ **Factory**: Tạo đối tượng mà không cần biết class cụ thể tại compile time. Dùng spawn prefab nhân vật theo nguyên tố
▪ **ScriptableObject as Data Container**: Tách dữ liệu config khỏi logic code, dễ cấu hình trong Editor

### 1.6.3. ASP.NET Core

ASP.NET Core là framework mã nguồn mở, đa nền tảng do Microsoft phát triển để xây dựng REST API và web service. Đây là phiên bản hiện đại, viết lại hoàn toàn từ ASP.NET Framework cổ điển, với hiệu suất vượt trội và hỗ trợ Docker natively.

a) Middleware Pipeline

ASP.NET Core xử lý mọi HTTP request thông qua chuỗi middleware. Mỗi middleware nhận request, xử lý và chuyển tiếp hoặc trả về response trực tiếp. Thứ tự tiêu biểu: xử lý exception → HTTPS redirect → Authentication (kiểm tra JWT) → Authorization (kiểm tra quyền) → CORS → Controller Action.

b) Entity Framework Core (EF Core)

EF Core là ORM (Object-Relational Mapper) chính thức của .NET, cho phép làm việc với database thông qua C# objects (Entity) thay vì viết SQL thủ công. EF Core dịch LINQ thành SQL tối ưu cho database cụ thể (MySQL trong đề tài):

▪ **DbContext**: Đại diện phiên làm việc với database, chứa DbSet\<T\> cho mỗi bảng
▪ **Migration**: Quản lý lịch sử thay đổi schema database theo version
▪ **Change Tracking**: Tự động theo dõi thay đổi trên entity, tạo câu UPDATE phù hợp khi SaveChanges()

c) SignalR

SignalR là thư viện ASP.NET Core cho real-time communication hai chiều (full-duplex) giữa server và clients, xây dựng trên WebSocket với tự động fallback sang Long Polling. Khái niệm Hub là điểm trung tâm để clients kết nối và gọi method hai chiều. Tính năng Group cho phép gửi message đến tập hợp clients cụ thể (tất cả thành viên trong cùng party).

### 1.6.4. MySQL — Hệ quản trị cơ sở dữ liệu

MySQL là RDBMS mã nguồn mở do Oracle Corporation duy trì, là hệ quản trị cơ sở dữ liệu phổ biến nhất thế giới theo DB-Engines Ranking 2024. MySQL được sử dụng rộng rãi trong ứng dụng web và game online nhờ hiệu suất cao, cộng đồng lớn và khả năng scale linh hoạt.

a) Mô hình dữ liệu quan hệ trong game

Dữ liệu game được tổ chức thành các bảng có quan hệ thông qua khóa ngoại (foreign key). Các quan hệ thường gặp:

▪ **Một-Nhiều (1:N)**: Một bản đồ có nhiều điểm spawn, một người chơi có nhiều nhân vật
▪ **Nhiều-Nhiều (N:M)**: Nhiều người chơi có thể sở hữu cùng loại item (qua bảng inventory trung gian)

Sơ đồ ERD tổng quát được minh họa trong Hình 1.9.

Hình 1.9: Sơ đồ ERD tổng quát của hệ thống game (users, player_data, enemy, item, skill, map, dungeon)

b) JSON Column trong game database

Một kỹ thuật phổ biến trong game database là dùng JSON column để lưu dữ liệu linh hoạt, thay đổi thường xuyên:

▪ **inventory (JSON)**: Danh sách item trong túi người chơi
▪ **info_char (JSON)**: Tập hợp chỉ số nhân vật (HP hiện tại, MP, level, EXP, vị trí)
▪ **phases_json (JSON)**: Cấu hình giai đoạn của boss (ngưỡng HP và kỹ năng từng phase)
▪ **wave_config (JSON)**: Cấu hình wave trong dungeon (loại quái và số lượng từng wave)

Ưu điểm là giảm số bảng, đơn giản hóa query SELECT; nhược điểm là khó lọc theo trường bên trong JSON và không có ràng buộc foreign key nội bộ.

### 1.6.5. Docker và triển khai ứng dụng

Docker là nền tảng container hóa cho phép đóng gói ứng dụng cùng toàn bộ dependencies vào một container — đơn vị phần mềm nhẹ, di động, hoạt động nhất quán trên mọi môi trường từ máy phát triển đến server production.

a) Lợi ích container hóa

▪ **Nhất quán môi trường**: Container chạy giống nhau trên mọi host, loại bỏ vấn đề "works on my machine"
▪ **Cô lập service**: Mỗi service (API, database, game server) chạy trong container riêng, không ảnh hưởng nhau
▪ **Khởi động nhanh**: Container khởi động trong vài giây, nhẹ hơn nhiều so với Virtual Machine
▪ **Dễ triển khai**: Push image lên registry, kéo về server và chạy — quy trình CI/CD đơn giản

b) Docker Compose cho hệ thống game server

Docker Compose định nghĩa và khởi động nhiều container cùng lúc. Hệ thống game server gồm 3 container chính:

▪ **MySQL container**: Database, dữ liệu persist vào volume, không expose port ra ngoài
▪ **ASP.NET Core API container**: Backend REST API + SignalR Hub, expose port HTTP
▪ **Unity Dedicated Server container**: Game server headless build, expose port UDP (7777)

Các container giao tiếp qua Docker network nội bộ. Chỉ API port và Game Server port được expose cho client; database hoàn toàn cô lập bên trong. Kiến trúc triển khai được minh họa trong Hình 1.10.

Hình 1.10: Kiến trúc Docker Compose — MySQL + REST API + Game Server trên Linux VPS

### 1.6.6. Tổng hợp stack công nghệ

Toàn bộ stack công nghệ sử dụng trong đề tài được tổng hợp trong Bảng 1.1:

**Bảng 1.1: Tổng hợp stack công nghệ của đề tài**

| Tầng | Công nghệ | Phiên bản | Vai trò chính |
|---|---|---|---|
| Game Engine | Unity 2D | 2022.3 LTS | Render, physics, animation, input, audio |
| Ngôn ngữ game | C# | .NET 8 | Logic game, script, network client |
| Multiplayer | Unity NGO | 1.7+ | Đồng bộ real-time vị trí và combat |
| Backend API | ASP.NET Core | 7.0 | REST API server, middleware, JWT auth |
| ORM | Entity Framework Core | 7.0 | Truy cập database qua C# an toàn |
| Database | MySQL | 8.0 | Lưu trữ người chơi và config game |
| Real-time | SignalR | 7.0 | Party system, thông báo sự kiện, chat |
| Container | Docker Compose | Latest | Đóng gói và triển khai trên Linux VPS |
| Authentication | JWT HS256 | RFC 7519 | Xác thực người dùng end-to-end |

---

## 1.7. Tổng kết chương 1

Chương 1 đã trình bày toàn diện cơ sở lý thuyết và nền tảng công nghệ phục vụ đề tài "Phát triển trò chơi Mutants Arena với hệ thống tiến hóa Gene bằng Unity". Thông qua khảo sát năm tựa game tiêu biểu — Hollow Knight, Dead Cells, Celeste, MapleStory và LangLa — các bài học thiết kế về combat feel, hệ thống nguyên tố tương khắc và kiến trúc zone-based server được đúc kết và ánh xạ trực tiếp vào quyết định thiết kế của đề tài (Bảng 1.2). Các cơ chế gameplay cốt lõi gồm di chuyển 2D (Dash với i-frames, coyote time, jump buffer), chiến đấu (hitbox/hurtbox, cooldown), ma trận tương khắc 6 nguyên tố (Bảng 1.3) và hệ thống Gene 5 Tier với Multi-Gene 60+ tổ hợp và Hybrid Fusion (Bảng 1.4) — vốn là điểm đặc trưng của đề tài — được phân tích chi tiết làm nền tảng cho Chương 3. Về mặt kỹ thuật hệ thống, kiến trúc Dedicated Server Authoritative kết hợp Unity NGO, REST API ASP.NET Core 7, SignalR, JWT và MySQL được lựa chọn có lý do rõ ràng và tổng hợp đầy đủ trong Bảng 1.1. Toàn bộ cơ sở lý thuyết này là tiền đề trực tiếp cho phân tích thiết kế hệ thống ở Chương 2 và triển khai lập trình ở Chương 3.

---

*Hà Nội, tháng 05 năm 2026*


---

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

Đối chiếu trực tiếp với mã nguồn hiện tại cho thấy Mutants Arena không chỉ có các tác nhân nghiệp vụ truyền thống mà còn có các tác nhân kỹ thuật bên ngoài hệ thống game client. Việc bổ sung actor kỹ thuật là cần thiết vì nhiều use case quan trọng trong dự án được thực thi bởi Zone Server, Dungeon Host và các tiến trình gameplay gọi API bằng JWT hoặc Zone API Key thay vì do người chơi thao tác trực tiếp.

Bảng 2.1: Các tác nhân tham gia hệ thống

| Tên tác nhân | Nhóm | Vai trò |
|---|---|---|
| Khách (Guest) | Nghiệp vụ | Người dùng chưa đăng nhập. Chỉ thực hiện đăng ký và đăng nhập tài khoản. |
| Người chơi (Player) | Nghiệp vụ | Tác nhân trung tâm của hệ thống. Thực hiện tạo nhân vật, vào game, di chuyển, chiến đấu, nâng Gene, quản lý túi đồ, làm nhiệm vụ, tương tác NPC, bạn bè, party, chat, dungeon và leaderboard. |
| Quản trị / vận hành (Admin/Operator) | Nghiệp vụ vận hành | Theo dõi trạng thái server, cập nhật spawn config, kiểm tra host map, refresh leaderboard, giám sát session gameplay và xử lý cấu hình vận hành. |
| Máy chủ gameplay / Netcode server (Zone Server / Dungeon Host) | Kỹ thuật | Actor hệ thống kỹ thuật bên ngoài lớp REST API. Xác thực kết nối NGO, gán map/zone ban đầu, đồng bộ vị trí - animation - trạng thái chiến đấu qua NetworkVariable/ClientRpc, cập nhật tiến trình quest theo event runtime, quản lý session dungeon, spawn đối tượng mạng và đăng ký heartbeat/host gameplay. |

### 2.1.3. Yêu cầu chức năng

Sau khi đối chiếu các controller ASP.NET Core, SignalR Hub và phần client Unity, có thể xác định phạm vi chức năng thực sự đã được triển khai theo từng module nghiệp vụ như sau. Trong chương này, một *use case* được hiểu là một mục tiêu nghiệp vụ hoàn chỉnh; các endpoint phụ trợ như `config`, `detail`, `heartbeat` hoặc `refresh` sẽ được gộp vào use case gần nhất nếu chúng cùng phục vụ một mục tiêu nghiệp vụ duy nhất.

**Bảng 2.1a: Phân nhóm chức năng theo module triển khai thực tế**

| Module | Vai trò trong đề tài | Mức ưu tiên | Trạng thái |
|---|---|---|---|
| Xác thực tài khoản + JWT | Khởi tạo phiên và bảo vệ toàn bộ hệ thống | **Cốt lõi** | Hoàn thành |
| Nhân vật + vào game + đồng bộ vị trí | Vòng đời nhân vật và bootstrap gameplay | **Cốt lõi** | Hoàn thành |
| Combat thời gian thực + kỹ năng + buff | Gameplay nền cho Gene và Dungeon | **Cốt lõi** | Hoàn thành |
| **Gene Evolution: Gene chính / Gene phụ / Hybrid Fusion** | **Trục đặc trưng của đề tài** | **Cốt lõi (trọng tâm)** | Hoàn thành |
| Túi đồ + trang bị + Blacksmith | Vòng lặp progression và nâng sức mạnh | **Cốt lõi** | Hoàn thành |
| NPC + Quest + Shop + dịch vụ đặc biệt | Nội dung PvE và điều hướng người chơi | **Cốt lõi** | Hoàn thành |
| Friend + Party + Chat SignalR | Lớp tương tác xã hội thời gian thực | **Cốt lõi** | Hoàn thành cơ bản |
| **Dungeon Wave/Boss + Reward** | **Trục multiplayer co-op chính** | **Cốt lõi (trọng tâm)** | Hoàn thành |
| Leaderboard | Theo dõi thành tích và vòng lặp cạnh tranh mềm | Phụ trợ | Hoàn thành cơ bản |
| Map/Portal/Zone Ops + Spawn Config | Hạ tầng vận hành gameplay server-authoritative | Phụ trợ quan trọng | Hoàn thành |

Các nhóm chức năng đã có trong mã nguồn hiện tại được mô tả chi tiết như sau:

Quản lý tài khoản và phiên đăng nhập:
▪ Đăng ký tài khoản bằng `username`, `email`, `password`; kiểm tra trùng lặp và băm mật khẩu trước khi lưu database
▪ Đăng nhập bằng `username/password`; server cấp JWT, cập nhật `last_login` và ghi nhận điểm danh ngày đầu tiên trong phiên đăng nhập
▪ JWT được gắn vào REST API, SignalR Hub và kết nối gameplay; request không hợp lệ bị từ chối

Quản lý nhân vật và hồ sơ chơi:
▪ Tạo nhân vật chính với `element_type` thuộc 6 hệ: Kim, Mộc, Thủy, Hỏa, Thổ, Phong; tên nhân vật giới hạn từ 3 đến 20 ký tự
▪ Nạp dữ liệu nhân vật, kỹ năng, trang bị, buff đang hoạt động và trạng thái Gene khi vào game
▪ Đồng bộ vị trí, map, zone và trạng thái nhân vật giữa client, gameplay server và API persistence
▪ Nhận EXP, lên cấp tự động, cộng điểm tiềm năng/kỹ năng; hỗ trợ khóa cấp bằng dịch vụ NPC

Di chuyển, bản đồ và chiến đấu:
▪ Di chuyển trái/phải, nhảy, dash và thao tác chiến đấu thời gian thực trong scene 2D side-scrolling
▪ Dùng đòn đánh thường và kỹ năng; server kiểm tra hitbox, cooldown, MP và áp dụng sát thương theo mô hình server-authoritative
▪ Áp dụng tương khắc nguyên tố và hiệu ứng trạng thái Burn, Freeze, Stun, Poison cùng hệ buff/debuff đồng bộ lên HUD
▪ Dịch chuyển qua portal, chuyển map/scene, kiểm tra khoảng cách đến cổng để chống gian lận vị trí. ----- [BẮT ĐẦU PHẦN THÊM MỚI] ----- Cổng dịch chuyển yêu cầu người chơi phải đáp ứng các điều kiện khắt khe trước khi cho phép qua map: kiểm tra cấp độ tối thiểu của nhân vật (`min_level`), kiểm tra việc hoàn thành nhiệm vụ cốt truyện bắt buộc (`required_quest_id`), và kiểm tra việc sở hữu vật phẩm chìa khóa tương ứng (`required_item_id`) trong túi đồ. ----- [KẾT THÚC PHẦN THÊM MỚI] -----

Hệ thống Gene Evolution:
▪ Nâng Gene chính từ Tier 1 đến Tier 5 bằng `gene_exp`, vàng và item cấu hình theo DB
▪ Chọn Gene phụ theo cặp cố định đã triển khai trong code: Hỏa↔Thổ, Thủy↔Mộc, Kim↔Phong. ----- [BẮT ĐẦU PHẦN THÊM MỚI] ----- Hệ nguyên tố Phong (Wind) là hệ đặc biệt: không tham gia vòng tương khắc Ngũ Hành truyền thống (Kim-Mộc-Thủy-Hỏa-Thổ) nhằm tránh gây mất cân bằng thuộc tính, nhưng tham gia vào cặp Gene phụ cố định với Kim và tham gia đầy đủ hệ thống dung hợp Hybrid. ----- [KẾT THÚC PHẦN THÊM MỚI] -----
▪ Nâng Gene phụ từ Tier 1 đến Tier 5; bonus chỉ số nhỏ hơn Gene chính và là điều kiện để mở Hybrid Fusion
▪ Fusion hai Gene Tier 5 hợp lệ thành Hybrid Gene; hệ thống lưu `hybridId`, `prefabPath`, bonus target, immune element và thêm bộ kỹ năng hybrid. Nhận 50% chỉ số bonus phụ, chuyển đổi sang visual prefab Hybrid tương ứng (Hybrid_Earth_Fire, Hybrid_Water_Wood, Hybrid_Metal_Wind).
▪ ----- [BẮT ĐẦU PHẦN THÊM MỚI] ----- **Gene Tối Thượng (Ultimate Gene)**: Sau khi hoàn thành Hybrid Fusion, nhân vật tích lũy EXP Tối Thượng qua diệt quái/boss hoặc dùng item đặc biệt để đạt mốc 1,000,000 EXP, tự động kích hoạt trạng thái Tối Thượng (`is_ultimate = true`). Khi đó, toàn bộ chỉ số HP, MP, ATK, DEF được nhân x1.5, hiển thị hào quang Aura tương ứng theo cặp Fusion (aura1, aura2, aura3) phía sau lưng nhân vật và hiển thị biểu tượng Tối Thượng ✦ trên HUD. ----- [KẾT THÚC PHẦN THÊM MỚI] -----

Túi đồ, trang bị và nâng cấp:
▪ Xem túi đồ, sắp xếp inventory, dùng consumable, cộng buff và tự động cập nhật số ô túi
▪ Mang/tháo trang bị giữa inventory và equipment slot; hỗ trợ item mở rộng túi nhanh với giới hạn tối đa 3 quick-slot expansion item
▪ Nâng cấp trang bị tại Blacksmith từ +1 đến +24 bằng đá cường hóa, Lucky Stone, Protection Stone và bạc; server kiểm tra từng slot vật liệu chống gian lận số lượng

NPC, quest và shop:
▪ Liệt kê NPC theo map, tương tác NPC, đi qua từng node hội thoại kế tiếp và mở menu động theo loại NPC
▪ Mua vật phẩm từ NPC shop; server kiểm tra tiền, cấp độ, stock và thêm item vào inventory
▪ Nhận, bỏ, theo dõi và hoàn thành quest; quest progress được cập nhật tự động theo event `kill`, `collect`, `talk`, `reach`
▪ Chỉ cho phép một quest active tại một thời điểm trên mỗi nhân vật
▪ Cung cấp dịch vụ đặc biệt qua NPC: reset potential, reset skill, learn skill, exchange skill, exchange charm, lock/unlock level

Chức năng xã hội và co-op:
▪ Tìm kiếm bạn bè theo tên nhân vật, gửi lời mời, chấp nhận và xóa quan hệ bạn bè
▪ Tạo party tối đa 4 người, gửi invite, xin vào party, bật/tắt auto-accept, lock party, tìm party hoặc người chơi gần đó theo map/zone
▪ Chat thời gian thực qua SignalR với các kênh world, proximity, group, private; phần hạ tầng chat cũng đã có nhánh clan/class trong code

Phó bản và reward:
▪ Liệt kê dungeon, xem chi tiết dungeon, xem boss và reward config
▪ Tạo session dungeon, tham gia session, vào wave, cập nhật session wave, kết thúc wave, kết thúc dungeon và rời dungeon giữa chừng
▪ Cấp thưởng phó bản qua API riêng dùng Zone API Key, thêm item reward trực tiếp vào inventory player

Vận hành gameplay server:
▪ Zone Server đăng ký, heartbeat và deregister với API để báo số map, số player và thống kê zone
▪ Host gameplay kiểm tra/đăng ký/heartbeat/hủy host cho từng map hoặc từng session dungeon
▪ Admin cập nhật spawn config theo JSON cho từng map; server xác thực `enemy_id` trước khi lưu và làm mới cache runtime
▪ Leaderboard hỗ trợ 5 hạng mục: level, quest, attendance, dungeon, gold; có cơ chế cache 5 phút và manual refresh

Các chức năng chưa có luồng hoàn chỉnh trong mã nguồn hiện tại và vì thế không được đưa vào use case chính của chương này gồm marketplace P2P, ranked PvP, achievement và daily quest.

### 2.1.4. Yêu cầu phi chức năng

Ngoài các yêu cầu chức năng, hệ thống cần đáp ứng một tập yêu cầu phi chức năng để đảm bảo trải nghiệm người chơi ở mức chấp nhận được trong điều kiện vận hành thực tế:

▪ **Hiệu năng**: Vòng lặp game Unity duy trì tối thiểu 60 FPS trên phần cứng đáp ứng cấu hình khuyến nghị; độ trễ đồng bộ hóa vị trí nhân vật không vượt quá 100 ms trên mạng LAN và 200 ms trên mạng WAN thông thường
▪ **Khả năng chịu tải**: NGO Dedicated Server hỗ trợ tối thiểu 4 người chơi kết nối đồng thời trên một zone; REST API xử lý tối thiểu 100 request/giây trong điều kiện bình thường mà không làm tăng thời gian phản hồi trung bình
▪ **Độ tin cậy**: Dữ liệu nhân vật được lưu tự động sau mỗi sự kiện quan trọng; khi ngắt kết nối đột ngột, nhân vật được xử lý graceful disconnect — server lưu trạng thái hiện tại trước khi xóa khỏi scene
▪ **Khả năng phục hồi**: Khi mất kết nối với server, client hiển thị thông báo và cho phép thử kết nối lại mà không cần khởi động lại ứng dụng
▪ **Khả năng mở rộng**: Kiến trúc Zone-based (ZoneRoomRegistry) cho phép thêm zone/dungeon mới mà không ảnh hưởng zone đang chạy; các hệ thống game (Quest, Gene, Equipment) được thiết kế module độc lập
▪ **Khả năng bảo trì**: Source code tổ chức theo nguyên tắc separation of concerns; mỗi hệ thống là một module riêng; thay đổi logic một hệ thống không ảnh hưởng đến hệ thống khác
▪ **Tương thích**: Hỗ trợ đa nền tảng, game chạy ổn định trên Windows (10/11 64-bit), Android (từ Android 9.0 trở lên) và iOS (từ iOS 13 trở lên); tối ưu hiển thị giao diện và thao tác điều khiển phù hợp cho cả máy tính và thiết bị di động.


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

![Sơ đồ kiến trúc tổng thể hệ thống Mutants Arena](extracted_images/image8.png)

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

### 2.2.2. Biểu đồ ca sử dụng mức tổng quát

Để phản ánh đúng dự án đang triển khai, phần use case của Mutants Arena được tổ chức thành hai mức: mức tổng quát theo module chức năng và mức chi tiết theo ca sử dụng hoàn chỉnh. Mức tổng quát dùng để trình bày phạm vi hệ thống; mức chi tiết dùng để đặc tả luồng nghiệp vụ đủ gần với mã nguồn hiện tại nhưng vẫn giữ được ý nghĩa phân tích hệ thống.

**Bảng 2.1b: Danh mục use case toàn hệ thống Mutants Arena**

| Mã | Use case | Actor chính | Nhóm chức năng |
|---|---|---|---|
| UC01 | Đăng ký tài khoản | Khách | Xác thực |
| UC02 | Đăng nhập và vào game | Khách / Người chơi | Xác thực |
| UC03 | Di chuyển và chuyển map | Người chơi | Bản đồ |
| UC04 | Chiến đấu và sử dụng kỹ năng | Người chơi | Combat |
| UC05 | Quản lý túi đồ và trang bị | Người chơi | Inventory |
| UC06 | Nâng cấp trang bị | Người chơi | Trang bị |
| UC07 | Phát triển Gene, Hybrid và Tối Thượng | Người chơi | Gene Evolution |
| UC08 | Phân bổ tiềm năng và kỹ năng | Người chơi | Progression |
| UC09 | Tương tác NPC và mua vật phẩm | Người chơi | NPC / Shop |
| UC10 | Quản lý nhiệm vụ | Người chơi | Quest |
| UC11 | Quản lý bạn bè | Người chơi | Social |
| UC12 | Quản lý tổ đội và chat | Người chơi | Party / Chat |
| UC13 | Tham gia và hoàn tất phó bản | Người chơi | Dungeon |
| UC14 | Xem leaderboard | Người chơi | Leaderboard |
| UC15 | Đăng ký và duy trì gameplay server | Gameplay Server | Vận hành |
| UC16 | Host map và phát thưởng dungeon | Gameplay Server | Vận hành |

![Biểu đồ Use case tổng quát hệ thống Mutants Arena](extracted_images/image9.png)

Hình 2.2. Biểu đồ ca sử dụng mức tổng quát hệ thống Mutants Arena

Mô tả: Vùng hệ thống mang tên "Hệ thống Mutants Arena" bao gồm các cụm use case mức cao: xác thực tài khoản, quản lý nhân vật, gameplay chiến đấu, Gene Evolution, inventory - trang bị, NPC - nhiệm vụ - cửa hàng, bạn bè - tổ đội - chat, phó bản đồng đội, đồng bộ thời gian thực, chuyển zone/visibility, spawn runtime, leaderboard và vận hành gameplay server. Bên ngoài vùng hệ thống có ba actor chính: Khách, Người chơi, và Gameplay Server. Các actor này tương tác với những nhóm chức năng khác nhau tùy theo vai trò nghiệp vụ và vai trò kỹ thuật của mình.

Ngoài sơ đồ tổng quát, bộ tài liệu còn có một sơ đồ tổng hợp chi tiết toàn bộ UC01 đến UC16 để đối chiếu giữa danh mục use case và các nhóm chức năng chuyên biệt.

### 2.2.2b. Đánh giá tính chuẩn hóa UML và Quy tắc thiết kế sơ đồ Use Case

Để đảm bảo các sơ đồ Use Case (cả tổng quát và chi tiết) tuân thủ đúng tiêu chuẩn quốc tế UML (Unified Modeling Language) và phương pháp luận phân tích thiết kế hướng đối tượng (OOAD), hệ thống sơ đồ trong đề tài đã được thiết kế và kiểm chứng theo các quy tắc chuẩn hóa sau:

1. **Xác định ranh giới hệ thống (System Boundary)**:
   * **Định nghĩa**: Hệ thống trong đề tài được giới hạn là **dịch vụ trung tâm Backend API và Cơ sở dữ liệu** (không bao gồm toàn bộ runtime của client standalone).
   * **Giải thích**: Việc xác định ranh giới này giúp giải thích tại sao `Máy chủ gameplay` (NGO Dedicated Server) và `Client` giao tiếp độc lập. Do chạy trên các tiến trình và VPS riêng biệt, `Gameplay Server` đóng vai trò là một tác nhân kỹ thuật (Technical Actor) tương tác với hệ thống qua các API được bảo mật bằng Zone API Key. Ranh giới hệ thống được biểu diễn rõ ràng bằng hình chữ nhật bao quanh toàn bộ các ca sử dụng (Use Case), với tên hệ thống được ghi ở góc trên bên trái.

2. **Chuẩn hóa các tác nhân (Actors)**:
   * **Quy tắc**: Các tác nhân luôn nằm ngoài ranh giới hệ thống và đại diện cho các thực thể bên ngoài tương tác trực tiếp với hệ thống.
   * **Chuẩn hóa**:
     * *Khách (Guest)*: Người dùng chưa xác thực, chỉ tương tác với các Use Case khởi tạo như Đăng ký (UC01) và Đăng nhập (UC02).
     * *Người chơi (Player)*: Tác nhân thừa kế từ Khách sau khi đã có Token phiên (JWT), có quyền tương tác với toàn bộ Use Case gameplay và phát triển nhân vật (UC03 - UC14).
     * *Máy chủ gameplay (Gameplay Server)*: Tác nhân kỹ thuật chịu trách nhiệm vận hành thế giới realtime, tương tác với các Use Case hệ thống như Đăng ký/Duy trì server (UC15), Host map và phát thưởng (UC16).
     * *Quản trị viên (Admin)*: Đã được lược bỏ khỏi các sơ đồ chi tiết (như Leaderboard và Vận hành server) do hệ thống thực tế phân quyền bảo mật qua middleware và cấu hình tự động (Self-registration), tránh việc vẽ các tác nhân "ảo" không có chức năng đi kèm trong mã nguồn.

3. **Tính đúng đắn của ca sử dụng (Use Cases)**:
   * **Quy tắc**: Mỗi Use Case phải biểu diễn một mục tiêu hoàn chỉnh, có giá trị nghiệp vụ thực tế đối với tác nhân, được đặt tên bắt đầu bằng một động từ (ví dụ: *Đăng ký tài khoản*, *Nâng cấp trang bị*, *Dung hợp Gene*).
   * **Khắc phục lỗi phổ biến**: Tránh lỗi thiết kế kinh điển của sinh viên là vẽ các bước xử lý nội bộ hoặc luồng thuật toán kỹ thuật thành các ca sử dụng độc lập (ví dụ: *Kiểm tra mật khẩu*, *Lưu cơ sở dữ liệu*, *Mã hóa BCrypt*, *Gửi kết quả*). Trong hệ thống sơ đồ của đề tài, các bước kỹ thuật này đã được loại bỏ hoàn toàn khỏi biểu đồ vẽ để tránh làm rối cấu trúc UML, thay vào đó chúng được mô tả chi tiết trong phần **Luồng chính (Basic Flow)** và **Quy tắc nghiệp vụ** của bảng đặc tả.

4. **Sử dụng đúng các quan hệ cấu trúc UML**:
   * **Quan hệ Bao hàm (<<include>>)**: Được sử dụng khi một ca sử dụng bắt buộc phải gọi đến một ca sử dụng khác để hoàn thành mục tiêu. Ví dụ: Ca sử dụng *Nâng cấp trang bị* (UC06) bắt buộc phải có quan hệ `<<include>>` tới *Quản lý túi đồ và trang bị* (UC05) để truy vấn và tiêu hao nguyên liệu. Mũi tên nét đứt hướng từ ca sử dụng gốc sang ca sử dụng được bao hàm.
   * **Quan hệ Mở rộng (<<extend>>)**: Chỉ sử dụng cho các luồng xử lý tùy chọn hoặc có điều kiện (Conditional/Optional flow). Ví dụ: Nhánh xử lý phát thưởng và lưu log của *Host map và phát thưởng dungeon* (UC16) chỉ mở rộng từ *Tham gia và hoàn tất phó bản* (UC13) tại điểm mở rộng (Extension Point) khi tổ đội chiến thắng toàn bộ các wave quái. Mũi tên nét đứt hướng từ ca sử dụng mở rộng ngược về ca sử dụng gốc.
   * **Quan hệ Hiệp hội (Association)**: Giữa các tác nhân và ca sử dụng được kết nối bằng một đường thẳng không có mũi tên (để tránh nhầm lẫn với luồng truyền dữ liệu), thể hiện tác nhân tham gia vào ca sử dụng đó.

Nhờ việc áp dụng nghiêm ngặt các quy tắc trên, bộ biểu đồ Use Case của đề tài không chỉ đạt tính thẩm mỹ cao mà còn phản ánh chính xác kiến trúc phần mềm, hỗ trợ đắc lực cho đội ngũ phát triển chuyển đổi trực tiếp từ mô hình phân tích sang mã nguồn C# và cấu hình Unity Netcode.

Các sơ đồ Usecase chi tiết của chương này được biên tập theo cùng một mẫu trình bày của luận văn; trong nội dung chương chỉ giữ phần hình minh họa và phần đặc tả nghiệp vụ tương ứng.

### 2.2.3. Đặc tả ca sử dụng mức chi tiết

#### 2.2.3.1. Nhóm 1 - Tài khoản và gameplay (UC01 — UC06)

Nhóm này bao gồm các chức năng nền tảng cho phép người chơi đăng nhập, di chuyển và thực hiện các tương tác chiến đấu cơ bản.

##### UC01 — Đăng ký tài khoản

| Thuộc tính | Nội dung |
|---|---|
| **Mã Use Case** | **UC01** |
| **Tên Use Case** | Đăng ký tài khoản |
| **Tác nhân chính (Actor)** | Khách |
| **Mô tả** | Khách tạo tài khoản mới để bắt đầu sử dụng hệ thống Mutants Arena. Hệ thống nhận thông tin đăng ký, kiểm tra tính hợp lệ và lưu tài khoản khi dữ liệu đáp ứng đủ điều kiện. |
| **Tiền điều kiện** | Khách chưa có tài khoản trong hệ thống. Có kết nối mạng ổn định. |
| **Luồng chính** | 1. Khách mở màn hình đăng ký từ giao diện chính.<br>2. Hệ thống hiển thị biểu mẫu gồm tên đăng nhập, mật khẩu và email.<br>3. Khách nhập thông tin và xác nhận gửi đăng ký.<br>4. Hệ thống kiểm tra tính hợp lệ của dữ liệu và phát hiện trùng lặp nếu có.<br>5. Hệ thống mã hóa mật khẩu và lưu tài khoản vào cơ sở dữ liệu.<br>6. Hệ thống trả thông báo thành công và chuyển sang màn hình đăng nhập. |
| **Luồng phụ / Ngoại lệ** | Tên đăng nhập hoặc email đã tồn tại, hoặc dữ liệu thiếu trường bắt buộc → Hệ thống báo lỗi tương ứng trên giao diện UI. |
| **Kết quả** | Tài khoản mới được đăng ký thành công trên cơ sở dữ liệu và người chơi có thể bắt đầu sử dụng. |


![Biểu đồ ca sử dụng cho mô-đun Đăng ký tài khoản](extracted_images/image10.png)

*Hình 2.3. Biểu đồ ca sử dụng cho mô-đun Đăng ký tài khoản*

##### UC02 — Đăng nhập và vào game

| Thuộc tính | Nội dung |
|---|---|
| **Mã Use Case** | **UC02** |
| **Tên Use Case** | Đăng nhập và vào game |
| **Tác nhân chính (Actor)** | Khách, Người chơi |
| **Mô tả** | Người dùng xác thực tài khoản, chọn nhân vật và kết nối vào thế giới game. Hệ thống tạo phiên đăng nhập hợp lệ và đưa nhân vật vào map tương ứng. |
| **Tiền điều kiện** | Tài khoản đã tồn tại trong hệ thống. Máy chủ game đang sẵn sàng kết nối. |
| **Luồng chính** | 1. Người dùng nhập thông tin đăng nhập trên màn hình xác thực.<br>2. Hệ thống xác thực tên đăng nhập và mật khẩu.<br>3. Hệ thống tạo token phiên và gửi về cho client.<br>4. Người dùng chọn nhân vật hiện có hoặc tạo nhanh nhân vật mới nếu chưa có.<br>5. Client dùng thông tin phiên để kết nối đến gameplay server.<br>6. Máy chủ nạp dữ liệu nhân vật và đưa người chơi vào map khởi đầu. |
| **Luồng phụ / Ngoại lệ** | Không có. |
| **Kết quả** | Phiên đăng nhập hợp lệ được tạo và nhân vật xuất hiện trong game. Nếu thất bại, hệ thống hiển thị thông báo lỗi tương ứng. |


![Biểu đồ ca sử dụng cho mô-đun Đăng nhập và vào game](extracted_images/image11.png)

*Hình 2.4. Biểu đồ ca sử dụng cho mô-đun Đăng nhập và vào game*

##### UC03 — Di chuyển và chuyển map

| Thuộc tính | Nội dung |
|---|---|
| **Mã Use Case** | **UC03** |
| **Tên Use Case** | Di chuyển và chuyển map |
| **Tác nhân chính (Actor)** | Người chơi |
| **Mô tả** | Người chơi di chuyển qua portal để sang map hoặc khu vực mới. Hệ thống kiểm tra điều kiện vào khu vực đích, tải map mới và đặt nhân vật vào vị trí spawn tương ứng. |
| **Tiền điều kiện** | Nhân vật đang ở gần portal. Người chơi đáp ứng điều kiện vào khu vực đích. |
| **Luồng chính** | 1. Người chơi tiến vào vùng tương tác của portal.<br>2. Hệ thống hiển thị gợi ý xác nhận di chuyển sang khu vực đích.<br>3. Người chơi xác nhận thao tác chuyển map.<br>4. Máy chủ kiểm tra điều kiện như level, nhiệm vụ hoặc giới hạn số người.<br>5. Hệ thống tải map mới và cập nhật zone của nhân vật.<br>6. Nhân vật xuất hiện tại vị trí spawn tương ứng của map đích. |
| **Luồng phụ / Ngoại lệ** | Nếu chưa đủ điều kiện vào map, hệ thống từ chối và hiển thị lý do. Nếu quá trình tải map lỗi, hệ thống giữ nguyên vị trí hiện tại và thông báo thử lại. |
| **Kết quả** | Người chơi được đưa sang map mới và hiển thị đúng trong zone mới. Nếu thất bại, nhân vật ở nguyên vị trí ban đầu và có thông báo nguyên nhân. |


![Biểu đồ ca sử dụng cho mô-đun Di chuyển và chuyển map](extracted_images/image12.png)

*Hình 2.5. Biểu đồ ca sử dụng cho mô-đun Di chuyển và chuyển map*

##### UC04 — Chiến đấu và sử dụng kỹ năng

| Thuộc tính | Nội dung |
|---|---|
| **Mã Use Case** | **UC04** |
| **Tên Use Case** | Chiến đấu và sử dụng kỹ năng |
| **Tác nhân chính (Actor)** | Người chơi |
| **Mô tả** | Người chơi chiến đấu với quái hoặc boss bằng đòn đánh thường và kỹ năng. Máy chủ xác nhận, tính toán sát thương và đồng bộ kết quả cho toàn bộ client liên quan. |
| **Tiền điều kiện** | Nhân vật còn sống, đang ở khu vực có mục tiêu chiến đấu và không bị khóa thao tác. |
| **Luồng chính** | 1. Người chơi chọn mục tiêu hoặc thực hiện tấn công trong phạm vi cho phép.<br>2. Client gửi yêu cầu đánh thường hoặc dùng kỹ năng lên máy chủ.<br>3. Máy chủ kiểm tra cooldown, mana và điều kiện mục tiêu.<br>4. Máy chủ tính sát thương và áp dụng kết quả lên đối tượng liên quan.<br>5. Hệ thống đồng bộ vị trí, animation và trạng thái cho các client quan sát.<br>6. Nếu mục tiêu bị hạ gục, phần thưởng kinh nghiệm và vật phẩm được phát cho người chơi. |
| **Luồng phụ / Ngoại lệ** | Không có. |
| **Kết quả** | Sát thương, trạng thái buff/debuff và phần thưởng chiến đấu được cập nhật chính xác trên tất cả client. Nếu thất bại, trạng thái chiến đấu giữ nguyên và có thông báo nguyên nhân. |


![Biểu đồ ca sử dụng cho mô-đun Chiến đấu và sử dụng kỹ năng](extracted_images/image13.png)

*Hình 2.6. Biểu đồ ca sử dụng cho mô-đun Chiến đấu và sử dụng kỹ năng*

##### UC05 — Quản lý túi đồ và trang bị

| Thuộc tính | Nội dung |
|---|---|
| **Mã Use Case** | **UC05** |
| **Tên Use Case** | Quản lý túi đồ và trang bị |
| **Tác nhân chính (Actor)** | Người chơi |
| **Mô tả** | Người chơi xem, sử dụng và thay đổi trang bị trong túi đồ cá nhân. Hệ thống kiểm tra điều kiện slot và cập nhật chỉ số nhân vật sau mỗi thao tác. |
| **Tiền điều kiện** | Nhân vật đã đăng nhập thành công. Có vật phẩm trong kho đồ hoặc trang bị đang được sử dụng. |
| **Luồng chính** | 1. Người chơi mở giao diện túi đồ từ HUD hoặc phím tắt.<br>2. Hệ thống hiển thị danh sách vật phẩm và thông tin cơ bản từng ô.<br>3. Người chơi chọn một vật phẩm để xem chi tiết hoặc thực hiện thao tác.<br>4. Nếu người chơi chọn trang bị, hệ thống kiểm tra slot và điều kiện sử dụng.<br>5. Nếu người chơi chọn dùng vật phẩm tiêu hao, hiệu ứng được áp dụng ngay.<br>6. Hệ thống cập nhật túi đồ, trang bị và chỉ số nhân vật sau khi thao tác hoàn tất. |
| **Luồng phụ / Ngoại lệ** | Không có. |
| **Kết quả** | Túi đồ và chỉ số nhân vật phản ánh đúng thay đổi mới nhất. Nếu thao tác không hợp lệ, trạng thái vật phẩm và chỉ số giữ nguyên. |


![Biểu đồ ca sử dụng cho mô-đun Quản lý túi đồ và trang bị](extracted_images/image14.png)

*Hình 2.7. Biểu đồ ca sử dụng cho mô-đun Quản lý túi đồ và trang bị*

##### UC06 — Nâng cấp trang bị

| Thuộc tính | Nội dung |
|---|---|
| **Mã Use Case** | **UC06** |
| **Tên Use Case** | Nâng cấp trang bị |
| **Tác nhân chính (Actor)** | Người chơi |
| **Mô tả** | Người chơi nâng cấp trang bị tại NPC hoặc giao diện cường hóa chuyên dụng. Hệ thống kiểm tra nguyên liệu, thực hiện cường hóa và cập nhật chỉ số vật phẩm theo kết quả. |
| **Tiền điều kiện** | Người chơi có trang bị phù hợp trong túi đồ. Có đủ nguyên liệu và vàng cần thiết theo yêu cầu nâng cấp. |
| **Luồng chính** | 1. Người chơi mở giao diện nâng cấp trang bị.<br>2. Hệ thống hiển thị các vật phẩm có thể cường hóa trong túi đồ.<br>3. Người chơi chọn trang bị mục tiêu và xem yêu cầu nguyên liệu.<br>4. Hệ thống kiểm tra số lượng nguyên liệu, vàng và điều kiện nâng cấp.<br>5. Người chơi xác nhận thao tác nâng cấp.<br>6. Hệ thống xử lý kết quả, trừ tài nguyên và cập nhật lại chỉ số của vật phẩm sau khi hoàn tất. |
| **Luồng phụ / Ngoại lệ** | Nếu thiếu nguyên liệu hoặc vàng, thao tác bị từ chối và hiển thị lượng còn thiếu. Nếu nâng cấp thất bại theo tỷ lệ, hệ thống áp dụng đúng quy tắc rủi ro đã cấu hình và thông báo kết quả. |
| **Kết quả** | Trang bị được cập nhật cấp cường hóa và tài nguyên bị trừ đúng. Nếu thất bại, trạng thái trang bị được xử lý theo quy tắc rủi ro và hiển thị thông báo rõ ràng. |


![Biểu đồ ca sử dụng cho mô-đun Nâng cấp trang bị](extracted_images/image15.png)

*Hình 2.8. Biểu đồ ca sử dụng cho mô-đun Nâng cấp trang bị*

---

#### 2.2.3.2. Nhóm 2 - Phát triển nhân vật (UC07 — UC10)

Nhóm này tập trung vào sự phát triển lâu dài của nhân vật qua hệ thống Gene tiến hóa, kỹ năng và nhiệm vụ.

##### UC07 — Phát triển Gene, Hybrid và Tối Thượng

| Thuộc tính | Nội dung |
|---|---|
| **Mã Use Case** | **UC07** |
| **Tên Use Case** | Phát triển Gene, Hybrid và Tối Thượng |
| **Tác nhân chính (Actor)** | Người chơi |
| **Mô tả** | Người chơi phát triển hệ Gene của nhân vật (nâng Gene chính, chọn và nâng Gene phụ), tiến hành dung hợp Hybrid Gene, và tích lũy EXP để kích hoạt Gene Tối Thượng nhằm nhân x1.5 toàn bộ thuộc tính cơ bản và hiển thị hào quang Aura tương ứng sau lưng nhân vật. |
| **Tiền điều kiện** | Người chơi có đủ điểm Gene, đá tiến hóa, vàng, hoặc điểm EXP Tối Thượng tương ứng. |
| **Luồng chính** | 1. Người chơi mở giao diện Gene Evolution.<br>2. Hệ thống hiển thị trạng thái Gene chính, Gene phụ, Hybrid và tiến trình Tối Thượng hiện thời.<br>3. Người chơi chọn nâng cấp Gene chính/Gene phụ, hoặc chọn dung hợp Hybrid khi cả hai đạt Tier 5.<br>4. Hệ thống kiểm tra điều kiện, trừ tài nguyên và cập nhật trạng thái tương ứng.<br>5. Sau khi nhân vật đã dung hợp Hybrid, việc tiêu diệt quái/boss hoặc sử dụng vật phẩm GeneExpAdd sẽ tích lũy EXP Tối Thượng (`ultimate_gene_exp`).<br>6. Khi `ultimate_gene_exp` đạt mốc 1,000,000, hệ thống tự động kích hoạt trạng thái Gene Tối Thượng (`is_ultimate = true`), nhân x1.5 toàn bộ chỉ số HP, MP, ATK, DEF và hiển thị Aura tương ứng phía sau nhân vật. |
| **Luồng phụ / Ngoại lệ** | Nếu thiếu tài nguyên nâng cấp, hệ thống từ chối và báo lỗi. Nếu tích lũy chưa đủ 1,000,000 EXP Tối Thượng, trạng thái Tối Thượng hiển thị phần trăm tiến trình hiện tại trên giao diện. |
| **Kết quả** | Cấp Gene của nhân vật được cập nhật. Nếu đạt Tối Thượng, chỉ số tăng x1.5, hiển thị Aura sau lưng nhân vật và HUD hiển thị biểu tượng ✦. |


![Biểu đồ ca sử dụng cho mô-đun Phát triển Gene và Hybrid](extracted_images/image16.png)

*Hình 2.9. Biểu đồ ca sử dụng cho mô-đun Phát triển Gene và Hybrid*

##### UC08 — Phân bổ tiềm năng và kỹ năng

| Thuộc tính | Nội dung |
|---|---|
| **Mã Use Case** | **UC08** |
| **Tên Use Case** | Phân bổ tiềm năng và kỹ năng |
| **Tác nhân chính (Actor)** | Người chơi |
| **Mô tả** | Người chơi phân bổ điểm chỉ số và sắp xếp kỹ năng phù hợp với lối chơi. Hệ thống kiểm tra hạn mức, lưu cấu hình và cập nhật HUD chiến đấu sau khi xác nhận. |
| **Tiền điều kiện** | Nhân vật còn điểm tiềm năng chưa phân bổ hoặc có kỹ năng khả dụng để sắp xếp. |
| **Luồng chính** | 1. Người chơi mở bảng chỉ số và kỹ năng của nhân vật.<br>2. Hệ thống hiển thị các chỉ số hiện tại, điểm còn lại và danh sách kỹ năng khả dụng.<br>3. Người chơi cộng điểm vào các chỉ số mong muốn.<br>4. Người chơi kéo thả kỹ năng vào các vị trí trên thanh kỹ năng nhanh.<br>5. Người chơi xác nhận lưu cấu hình vừa chỉnh sửa.<br>6. Hệ thống cập nhật lại chỉ số, kỹ năng đang trang bị và HUD chiến đấu. |
| **Luồng phụ / Ngoại lệ** | Nếu cộng quá số điểm hiện có, hệ thống không cho phép xác nhận và hiển thị lỗi. Nếu kỹ năng chưa mở khóa hoặc không phù hợp ô gắn, người chơi không thể thả vào thanh nhanh. |
| **Kết quả** | Bộ chỉ số và thanh kỹ năng của nhân vật được lưu theo cấu hình mới. Nếu không hợp lệ, hệ thống giữ cấu hình cũ và hiển thị thông báo lỗi. |


![Biểu đồ ca sử dụng cho mô-đun Phân bổ tiềm năng và kỹ năng](extracted_images/image17.png)

*Hình 2.10. Biểu đồ ca sử dụng cho mô-đun Phân bổ tiềm năng và kỹ năng*

##### UC09 — Tương tác NPC và mua vật phẩm

| Thuộc tính | Nội dung |
|---|---|
| **Mã Use Case** | **UC09** |
| **Tên Use Case** | Tương tác NPC và mua vật phẩm |
| **Tác nhân chính (Actor)** | Người chơi |
| **Mô tả** | Người chơi tương tác với NPC để mở dịch vụ, mua vật phẩm hoặc dùng các tiện ích hỗ trợ. Hệ thống hiển thị menu động theo loại NPC, xử lý giao dịch và cập nhật túi đồ sau khi xác nhận. |
| **Tiền điều kiện** | Người chơi đang ở trong phạm vi tương tác hợp lệ của NPC. |
| **Luồng chính** | 1. Người chơi tiếp cận NPC và kích hoạt tương tác.<br>2. Hệ thống hiển thị menu động theo loại NPC hiện tại.<br>3. Người chơi chọn chức năng mua vật phẩm hoặc dịch vụ mong muốn.<br>4. Hệ thống hiển thị danh sách hàng hóa, giá bán và số lượng cần mua.<br>5. Người chơi xác nhận giao dịch.<br>6. Hệ thống trừ vàng, thêm vật phẩm hoặc áp dụng dịch vụ tương ứng cho nhân vật. |
| **Luồng phụ / Ngoại lệ** | Nếu không đủ vàng hoặc vật phẩm đã hết, giao dịch bị từ chối và hiển thị nguyên nhân. Nếu NPC thuộc loại hỗ trợ đặc biệt, hệ thống mở dịch vụ tương ứng thay cho cửa hàng vật phẩm. |
| **Kết quả** | Giao dịch với NPC được ghi nhận và túi đồ của người chơi được cập nhật. Nếu thất bại, tài nguyên giữ nguyên và hiển thị thông báo nguyên nhân. |


![Biểu đồ ca sử dụng cho mô-đun Tương tác NPC và mua vật phẩm](extracted_images/image18.png)

*Hình 2.11. Biểu đồ ca sử dụng cho mô-đun Tương tác NPC và mua vật phẩm*

##### UC10 — Quản lý nhiệm vụ

| Thuộc tính | Nội dung |
|---|---|
| **Mã Use Case** | **UC10** |
| **Tên Use Case** | Quản lý nhiệm vụ |
| **Tác nhân chính (Actor)** | Người chơi |
| **Mô tả** | Người chơi nhận, theo dõi và hoàn tất nhiệm vụ để nhận thưởng và mở khóa nội dung mới. Hệ thống tự động theo dõi tiến độ và cấp thưởng khi đầy đủ điều kiện hoàn thành. |
| **Tiền điều kiện** | Nhân vật đáp ứng điều kiện nhận nhiệm vụ tương ứng, chẳng hạn level hoặc chuỗi nhiệm vụ trước đó. |
| **Luồng chính** | 1. Người chơi tương tác với NPC hoặc giao diện nhiệm vụ để nhận quest mới.<br>2. Hệ thống hiển thị mô tả, mục tiêu và phần thưởng của nhiệm vụ.<br>3. Người chơi xác nhận nhận nhiệm vụ và quest được thêm vào nhật ký.<br>4. Trong quá trình chơi, hệ thống tự động cập nhật tiến độ hoàn thành các mục tiêu.<br>5. Khi đã đủ điều kiện, người chơi quay lại NPC hoặc giao diện để nộp nhiệm vụ.<br>6. Hệ thống cấp thưởng và cập nhật trạng thái chuỗi nhiệm vụ tiếp theo nếu có. |
| **Luồng phụ / Ngoại lệ** | Nếu người chơi chưa đủ level hoặc chưa hoàn thành quest trước, hệ thống không cho nhận nhiệm vụ mới và hiển thị lý do. Nếu người chơi hủy nhiệm vụ, tiến độ bị xóa và có thể nhận lại sau nếu hệ thống cho phép. |
| **Kết quả** | Tiến độ nhiệm vụ được cập nhật và phần thưởng được cấp đúng sau khi hoàn thành. Nếu không đủ điều kiện, hệ thống giữ trạng thái quest hiện tại và thông báo rõ nguyên nhân. |


![Biểu đồ ca sử dụng cho mô-đun Quản lý nhiệm vụ](extracted_images/image19.png)

*Hình 2.12. Biểu đồ ca sử dụng cho mô-đun Quản lý nhiệm vụ*

---

#### 2.2.3.3. Nhóm 3 - Tương tác và hoạt động (UC11 — UC14)

Nhóm này bao gồm các tính năng xã hội thời gian thực giúp kết nối người chơi và tổ chức hoạt động co-op.

##### UC11 — Quản lý bạn bè

| Thuộc tính | Nội dung |
|---|---|
| **Mã Use Case** | **UC11** |
| **Tên Use Case** | Quản lý bạn bè |
| **Tác nhân chính (Actor)** | Người chơi |
| **Mô tả** | Người chơi quản lý danh sách bạn bè và trạng thái kết nối xã hội trong game. Hệ thống hỗ trợ gửi lời mời, phản hồi lời mời, xóa bạn bè và đồng bộ trạng thái online giữa các người chơi. |
| **Tiền điều kiện** | Người chơi đang online và có thể truy cập bảng bạn bè. |
| **Luồng chính** | 1. Người chơi mở bảng bạn bè từ giao diện xã hội.<br>2. Hệ thống hiển thị danh sách bạn bè cùng trạng thái online và map hiện tại.<br>3. Người chơi nhập tên người nhận và gửi lời mời kết bạn.<br>4. Người nhận nhận được thông báo phản hồi lời mời.<br>5. Nếu người nhận chấp nhận, hệ thống thêm hai bên vào danh sách bạn bè của nhau.<br>6. Người chơi có thể tiếp tục xóa bạn hoặc xem lại trạng thái của từng người trong danh sách. |
| **Luồng phụ / Ngoại lệ** | Nếu người nhận không tồn tại hoặc đã là bạn, hệ thống từ chối lời mời. Nếu người nhận từ chối lời mời, yêu cầu kết bạn kết thúc mà không thay đổi dữ liệu. |
| **Kết quả** | Danh sách bạn bè được cập nhật đồng bộ ở cả hai phía liên quan. Nếu thất bại, danh sách giữ nguyên và hệ thống hiển thị thông báo nguyên nhân. |


![Biểu đồ ca sử dụng cho mô-đun Quản lý bạn bè](extracted_images/image20.png)

*Hình 2.13. Biểu đồ ca sử dụng cho mô-đun Quản lý bạn bè*

##### UC12 — Quản lý tổ đội và chat

| Thuộc tính | Nội dung |
|---|---|
| **Mã Use Case** | **UC12** |
| **Tên Use Case** | Quản lý tổ đội và chat |
| **Tác nhân chính (Actor)** | Người chơi |
| **Mô tả** | Người chơi tạo party, quản lý thành viên và trao đổi thông tin qua các kênh chat phù hợp. Hệ thống phân phối tin nhắn đến đúng phạm vi người nhận và cập nhật giao diện xã hội liên quan. |
| **Tiền điều kiện** | Người chơi đang online và không bị chặn tính năng xã hội do vi phạm quy định. |
| **Luồng chính** | 1. Người chơi mở giao diện xã hội và chọn tạo tổ đội hoặc mời thành viên.<br>2. Hệ thống tạo party mới và gán vai trò trưởng nhóm cho người khởi tạo.<br>3. Người chơi mời thêm thành viên từ danh sách bạn bè hoặc người chơi gần đó.<br>4. Sau khi party được hình thành, bảng tổ đội hiển thị trạng thái của từng thành viên.<br>5. Người chơi sử dụng khung chat để trao đổi trong các kênh chung hoặc kênh tổ đội.<br>6. Hệ thống phân phối tin nhắn đến đúng phạm vi người nhận và cập nhật giao diện xã hội liên quan. |
| **Luồng phụ / Ngoại lệ** | Nếu người được mời đang ở tổ đội khác, lời mời bị từ chối và thông báo cho người mời. Nếu người chơi gửi tin sai kênh hoặc vi phạm bộ lọc chat, hệ thống chặn và thông báo lỗi. |
| **Kết quả** | Party và nội dung trao đổi được cập nhật cho đúng thành viên hoặc đúng kênh nhận tin. Nếu thất bại, trạng thái party và chat giữ nguyên và hiển thị thông báo lỗi. |


![Biểu đồ ca sử dụng cho mô-đun Quản lý tổ đội và chat](extracted_images/image21.png)

*Hình 2.14. Biểu đồ ca sử dụng cho mô-đun Quản lý tổ đội và chat*

##### UC13 — Tham gia và hoàn tất phó bản

| Thuộc tính | Nội dung |
|---|---|
| **Mã Use Case** | **UC13** |
| **Tên Use Case** | Tham gia và hoàn tất phó bản |
| **Tác nhân chính (Actor)** | Người chơi, Máy chủ |
| **Mô tả** | Người chơi hoặc tổ đội tham gia phó bản, vượt qua các wave quái và nhận phần thưởng khi hoàn thành. Gameplay server khởi tạo phiên phó bản, spawn wave và xử lý phát thưởng khi kết thúc. |
| **Tiền điều kiện** | Người chơi đáp ứng điều kiện vào phó bản như level hoặc lượt vào. Gameplay server đang còn slot hoạt động. |
| **Luồng chính** | 1. Người chơi mở giao diện hoặc portal phó bản và chọn loại phó bản.<br>2. Hệ thống kiểm tra điều kiện về level, lượt vào và trạng thái tổ đội.<br>3. Gameplay server khởi tạo phiên phó bản và spawn các wave quái tương ứng.<br>4. Người chơi vượt qua từng đợt quái và tiến tới boss cuối.<br>5. Khi điều kiện hoàn thành được đáp ứng, hệ thống tổng kết kết quả phó bản.<br>6. Hệ thống phát phần thưởng và đưa người chơi rời khỏi phó bản sau khi kết thúc. |
| **Luồng phụ / Ngoại lệ** | Nếu người chơi hoặc tổ đội thất bại trong phó bản, hệ thống kết thúc phiên với trạng thái thất bại và không phát thưởng. Nếu máy chủ gặp lỗi spawn ở một wave, hệ thống ghi log và xử lý theo cấu hình fallback của phó bản. |
| **Kết quả** | Kết quả phó bản được ghi nhận và phần thưởng được phát theo trạng thái hoàn thành. Nếu thất bại, hệ thống kết thúc phiên và thông báo rõ nguyên nhân cho người chơi. |


![Biểu đồ ca sử dụng cho mô-đun Tham gia và hoàn tất phó bản](extracted_images/image22.png)

*Hình 2.15. Biểu đồ ca sử dụng cho mô-đun Tham gia và hoàn tất phó bản*

##### UC14 — Xem leaderboard

| Thuộc tính | Nội dung |
|---|---|
| **Mã Use Case** | **UC14** |
| **Tên Use Case** | Xem leaderboard |
| **Tác nhân chính (Actor)** | Người chơi |
| **Mô tả** | Người chơi theo dõi thứ hạng trong game; quản trị viên có thể làm mới hoặc reset dữ liệu xếp hạng. Hệ thống tải và hiển thị bảng xếp hạng theo hạng mục được chọn. |
| **Tiền điều kiện** | Dịch vụ leaderboard đang khả dụng. Người dùng có quyền truy cập tương ứng. |
| **Luồng chính** | 1. Người dùng mở giao diện bảng xếp hạng.<br>2. Hệ thống tải dữ liệu theo hạng mục mặc định hoặc hạng mục được chọn.<br>3. Người dùng chuyển đổi giữa các tab để xem top theo từng tiêu chí.<br>4. Hệ thống đánh dấu vị trí hiện tại của nhân vật nếu có mặt trên bảng xếp hạng.<br>5. Người dùng có thể yêu cầu làm mới để tải lại dữ liệu mới nhất.<br>6. Nếu là quản trị viên, hệ thống cho phép thực hiện thao tác reset theo quyền hạn được cấp. |
| **Luồng phụ / Ngoại lệ** | Nếu dữ liệu tạm thời chưa sẵn sàng, hệ thống hiển thị trạng thái chờ tải hoặc lấy từ cache gần nhất. Nếu người dùng không có quyền quản trị, hệ thống không cho phép reset bảng xếp hạng. |
| **Kết quả** | Bảng xếp hạng hiển thị đúng dữ liệu mới nhất hoặc được reset thành công khi có quyền quản trị. Nếu thất bại, dữ liệu cũ được giữ nguyên và có thông báo nguyên nhân. |


![Biểu đồ ca sử dụng cho mô-đun Xem leaderboard](extracted_images/image23.png)

*Hình 2.16. Biểu đồ ca sử dụng cho mô-đun Xem leaderboard*

---

#### 2.2.3.4. Nhóm 4 - Vận hành kỹ thuật (UC15 — UC16)

Nhóm này mô tả các tác vụ kỹ thuật chạy ngầm giữa máy chủ game thời gian thực và backend REST API.

##### UC15 — Đăng ký và duy trì gameplay server

| Thuộc tính | Nội dung |
|---|---|
| **Mã Use Case** | **UC15** |
| **Tên Use Case** | Quản lý gameplay server |
| **Tác nhân chính (Actor)** | Gameplay Server |
| **Mô tả** | Gameplay server tự đăng ký trạng thái hoạt động và được quản trị viên giám sát trong quá trình vận hành. Hệ thống xử lý heartbeat định kỳ và cập nhật trạng thái online/offline của server. |
| **Tiền điều kiện** | GameServerApi đang hoạt động. Gameplay server có thông tin xác thực hợp lệ. |
| **Luồng chính** | 1. Gameplay server khởi động và gửi yêu cầu đăng ký đến dịch vụ quản lý máy chủ.<br>2. Hệ thống xác thực thông tin server và lưu trạng thái online ban đầu.<br>3. Server gửi heartbeat theo chu kỳ để cập nhật tình trạng hoạt động.<br>4. Quản trị viên theo dõi trạng thái, tải hiện tại và tình trạng map của server.<br>5. Khi server cần dừng hoạt động, hệ thống thực hiện quy trình giải phóng hoặc unregister.<br>6. Dịch vụ quản lý máy chủ cập nhật lại trạng thái offline và xử lý người chơi còn kết nối nếu cần. |
| **Luồng phụ / Ngoại lệ** | Nếu heartbeat quá thời hạn cho phép, hệ thống tự đánh dấu server offline. Nếu xác thực server không hợp lệ, yêu cầu đăng ký bị từ chối và ghi log. |
| **Kết quả** | Trạng thái hoạt động của gameplay server được cập nhật chính xác trong hệ thống quản trị. Nếu thất bại, trạng thái server không được ghi nhận và có log chẩn đoán kèm theo. |


![Biểu đồ ca sử dụng cho mô-đun Quản lý gameplay server](extracted_images/image24.png)

*Hình 2.17. Biểu đồ ca sử dụng cho mô-đun Quản lý gameplay server*

##### UC16 — Host map và phát thưởng dungeon

| Thuộc tính | Nội dung |
|---|---|
| **Mã Use Case** | **UC16** |
| **Tên Use Case** | Host map và phát thưởng phó bản |
| **Tác nhân chính (Actor)** | Gameplay Server |
| **Mô tả** | Gameplay server đồng bộ cấu hình map đang host và xử lý phần thưởng khi phó bản kết thúc. Hệ thống backend kiểm tra dữ liệu kết quả, phân phối phần thưởng và lưu log phó bản. |
| **Tiền điều kiện** | Gameplay server đã đăng ký thành công. Server có thể giao tiếp với dịch vụ backend. |
| **Luồng chính** | 1. Gameplay server gửi danh sách map đang host lên hệ thống quản lý.<br>2. Quản trị viên hoặc quy trình tự động cập nhật cấu hình spawn cho từng map.<br>3. Gameplay server tải cấu hình mới nhất để áp dụng cho quái, boss và đối tượng mạng.<br>4. Khi một phó bản kết thúc, server tổng hợp dữ liệu kết quả của người chơi hoặc tổ đội.<br>5. Hệ thống backend xử lý phần thưởng, kinh nghiệm và log kết quả phó bản.<br>6. Kết quả cuối cùng được đồng bộ lại cho client và lưu vào cơ sở dữ liệu. |
| **Luồng phụ / Ngoại lệ** | Nếu cấu hình spawn không hợp lệ, hệ thống giữ cấu hình cũ và ghi log cảnh báo. Nếu phát thưởng lỗi do dữ liệu không hợp lệ, hệ thống dừng thao tác và chuyển vào hàng chờ xử lý lại. |
| **Kết quả** | Map host được đồng bộ đúng cấu hình và kết quả phó bản được phát thưởng chính xác. Nếu thất bại, dữ liệu được đưa vào hàng chờ xử lý lại và có log ghi nhận chi tiết. |


![Biểu đồ ca sử dụng cho mô-đun Host map và phát thưởng phó bản](extracted_images/image25.png)

*Hình 2.18. Biểu đồ ca sử dụng cho mô-đun Host map và phát thưởng phó bản*





### 2.2.4. Các biểu đồ tuần tự đặc tả ca sử dụng

Căn cứ vào các kịch bản Use case trọng tâm đã được đặc tả trong hệ thống Mutants Arena, phần này trình bày các biểu đồ tuần tự cho những luồng gameplay cốt lõi đang được triển khai thực tế trong dự án. Các biểu đồ được xây dựng bám sát kiến trúc client Unity, gameplay server, ASP.NET Core Web API và cơ sở dữ liệu MySQL, qua đó phản ánh rõ thứ tự tương tác giữa người chơi, giao diện, lớp xử lý nghiệp vụ và dữ liệu lưu trữ trong từng chức năng chính.

#### 2.2.4.1. Biểu đồ tuần tự Chiến đấu và sử dụng kỹ năng

Căn cứ vào kịch bản Use case UC04 - Chiến đấu và sử dụng kỹ năng, ta xây dựng các bước thực hiện của hệ thống với chức năng chiến đấu thời gian thực bằng biểu đồ tuần tự. Trong luồng này, người chơi chọn mục tiêu hoặc kích hoạt đòn đánh thường, kỹ năng trên thanh Q/W/E/R; client chiến đấu gửi yêu cầu lên gameplay server; gameplay server tiếp tục kiểm tra điều kiện cooldown, MP, hitbox và trạng thái khóa thao tác trước khi chuyển sang lớp CombatResolver hoặc đối tượng địch để tính sát thương.

Sau khi yêu cầu hợp lệ, hệ thống áp dụng DamageResult, cập nhật HP, buff, debuff và đồng bộ kết quả cho client điều khiển lẫn client quan sát bằng cơ chế server-authoritative. Khi mục tiêu bị tiêu diệt, hệ thống tiếp tục kích hoạt luồng phát EXP, vật phẩm rơi và các hook nhiệm vụ liên quan. Trong cùng biểu đồ, các tuần tự thất bại cũng được mô tả rõ cho trường hợp kỹ năng đang hồi chiêu, không đủ MP, mục tiêu ngoài phạm vi, trượt hitbox hoặc nhân vật đã chết.

![Biểu đồ tuần tự đặc tả ca sử dụng Chiến đấu và sử dụng kỹ năng](extracted_images/image26.png)

*Hình 2.19. Biểu đồ tuần tự đặc tả ca sử dụng Chiến đấu và sử dụng kỹ năng*

#### 2.2.4.2. Biểu đồ tuần tự Nâng cấp trang bị tại Blacksmith

Căn cứ vào kịch bản Use case UC06 - Nâng cấp trang bị, ta xây dựng các bước thực hiện của hệ thống với chức năng cường hóa trang bị tại Blacksmith bằng biểu đồ tuần tự. Luồng này bắt đầu khi người chơi mở giao diện Blacksmith, chọn trang bị mục tiêu và nạp cấu hình nâng cấp hiện tại từ hệ thống. Tại đây, hệ thống truy vấn inventory, equipment và cấu hình nâng cấp theo bậc hiện tại để trả về các tham số như `stoneId`, `stoneNeeded`, `success rate`, `failPolicy` và `upgradeLevel`.

Sau khi người chơi chọn đúng trang bị và các slot đá, charm tương ứng, client gửi yêu cầu nâng cấp kèm thông tin `slotIndex + count` để hệ thống kiểm tra số lượng vật liệu trên từng stack, phòng chống gian lận số lượng khi dùng vật liệu dạng chồng. Nếu hợp lệ, hệ thống trừ bạc, tiêu hao vật liệu, tính kết quả theo `failPolicy`, cập nhật `upgradeLevel`, `strOptions`, inventory and equipment. Biểu đồ đồng thời thể hiện rõ hai nhánh thất bại chính: thất bại xác thực do thiếu bạc, thiếu vật liệu, sai slot stack hoặc chạm mốc +24; và thất bại theo tỷ lệ, trong đó hệ thống giữ nguyên hoặc làm tụt cấp trang bị đúng theo cấu hình rủi ro đang dùng trong dự án.

![Biểu đồ tuần tự đặc tả ca sử dụng Nâng cấp trang bị tại Blacksmith](extracted_images/image27.png)

*Hình 2.20. Biểu đồ tuần tự đặc tả ca sử dụng Nâng cấp trang bị tại Blacksmith*

#### 2.2.4.3. Biểu đồ tuần tự Nâng Gene chính và Gene phụ

Căn cứ vào kịch bản Use case UC07 - Phát triển Gene, Hybrid và Tối Thượng, ta xây dựng các bước thực hiện của hệ thống với chức năng nâng Gene chính và Gene phụ bằng biểu đồ tuần tự. Ở giai đoạn Gene chính, người chơi mở giao diện Gene Evolution, nạp `player_data` and cấu hình trong `gene_upgrade_config`, sau đó xác nhận yêu cầu nâng cấp. Hệ thống kiểm tra `gene_exp`, bạc, vật liệu và giới hạn Tier trước khi quyết định kết quả nâng cấp; nếu thành công, hệ thống lưu Tier mới, `final_stats`, danh sách kỹ năng mở khóa và đồng bộ lại giao diện Gene cho người chơi.

Đối với Gene phụ, biểu đồ mô tả rõ quá trình chọn hệ phụ lần đầu, kiểm tra cặp hệ cố định đã triển khai trong dự án gồm Hỏa↔Thổ, Thủy↔Mộc và Kim↔Phong, sau đó nạp cấu hình từ `gene_multi_config` để thực hiện nâng hệ phụ. Nếu Gene phụ được nâng thành công, hệ thống cập nhật `secondaryElement`, `secondary_gene_tier`, bonus chỉ số theo hệ số giảm so với Gene chính, đồng thời bật cờ `canFuse` khi cả hai hệ đã đạt điều kiện hợp lệ. Các luồng phụ thất bại cũng được biểu diễn đầy đủ cho trường hợp thiếu `gene_exp`, thiếu bạc, thiếu vật liệu, Gene chính đã đạt Tier tối đa, chọn sai cặp hệ hoặc Gene phụ đã bị khóa trước đó.

![Biểu đồ tuần tự đặc tả ca sử dụng Nâng Gene chính và Gene phụ](extracted_images/image28.png)

*Hình 2.21. Biểu đồ tuần tự đặc tả ca sử dụng Nâng Gene chính và Gene phụ*

#### 2.2.4.4. Biểu đồ tuần tự Dung hợp Hybrid Gene

Căn cứ vào kịch bản Use case UC07 - Phát triển Gene, Hybrid và Tối Thượng, ta tiếp tục xây dựng các bước thực hiện của hệ thống với chức năng dung hợp Hybrid Gene bằng biểu đồ tuần tự. Luồng này bắt đầu khi người chơi mở tab Hybrid Fusion, yêu cầu nạp điều kiện thực hiện và hệ thống truy vấn các bảng `gene_hybrid_config`, `gene_hybrid_skill` cùng dữ liệu hiện thời trong `player_data`. Từ đó, giao diện nhận về các thông tin như số lượng Fusion Core, `gold cost`, bonus, prefab và bộ kỹ năng Hybrid tương ứng để hiển thị cho người chơi xác nhận.

Khi người chơi gửi yêu cầu dung hợp, hệ thống kiểm tra đồng thời các điều kiện quan trọng gồm trạng thái `isHybrid`, Tier của Gene chính và Gene phụ, tính hợp lệ của cặp hệ, số lượng Fusion Core và lượng vàng hiện có. Nếu dung hợp thành công, hệ thống lưu `HybridId`, `prefab`, các `immune elements`, bonus chiến đấu và bộ kỹ năng Hybrid mới vào dữ liệu nhân vật, sau đó đồng bộ lại `final_stats` và giao diện Hybrid cho client. Biểu đồ cũng thể hiện rõ hai nhóm điều kiện thất bại: chưa đủ Tier 5 hoặc cặp hệ không hợp lệ; thiếu Fusion Core, thiếu vàng hoặc nhân vật đã là Hybrid trước đó.

![Biểu đồ tuần tự đặc tả ca sử dụng Dung hợp Hybrid Gene](extracted_images/image29.png)

*Hình 2.22. Biểu đồ tuần tự đặc tả ca sử dụng Dung hợp Hybrid Gene*

----- [BẮT ĐẦU PHẦN THÊM MỚI] -----

#### 2.2.4.4a. Biểu đồ tuần tự Kích hoạt Gene Tối Thượng

Căn cứ vào kịch bản Use case UC07 - Phát triển Gene, Hybrid và Tối Thượng, ta xây dựng các bước thực hiện của hệ thống với chức năng kích hoạt trạng thái Gene Tối Thượng (Ultimate Gene) bằng biểu đồ tuần tự. Tiến trình này bắt đầu khi người chơi (sau khi đã dung hợp Hybrid thành công) thực hiện các hành động trong thế giới game như đánh quái, tiêu diệt Boss phó bản hoặc sử dụng vật phẩm hỗ trợ tăng EXP Tối Thượng (`GeneExpAdd`).

Gameplay Server hoặc Web API khi nhận được sự kiện tăng EXP sẽ cộng dồn giá trị vào trường `ultimate_gene_exp` trong cơ sở dữ liệu và gọi `NetworkPlayerDataSync` để đồng bộ thuộc tính về client. Khi điểm số này vượt qua mốc giới hạn 1,000,000 EXP, server sẽ tự động kích hoạt trạng thái Tối Thượng (`is_ultimate = true`). Ngay lập tức, `StatCalculator` tính toán lại và nhân x1.5 toàn bộ chỉ số HP, MP, ATK, DEF của nhân vật. Đồng thời, máy chủ gửi thông điệp ClientRpc yêu cầu client hiển thị Aura hào quang tương ứng với hệ Hybrid phía sau nhân vật (`aura1` cho Hỏa-Thổ, `aura2` cho Thủy-Mộc, `aura3` cho Kim-Phong) và hiển thị biểu tượng Tối Thượng trên HUD.

![Biểu đồ tuần tự đặc tả ca sử dụng Kích hoạt Gene Tối Thượng](extracted_images/image_ultimate_gene_sequence.png)

*Hình 2.22a. Biểu đồ tuần tự đặc tả ca sử dụng Kích hoạt Gene Tối Thượng*

----- [KẾT THÚC PHẦN THÊM MỚI] -----

#### 2.2.4.5. Biểu đồ tuần tự Tham gia và hoàn tất phó bản

Căn cứ vào kịch bản Use case UC13 - Tham gia và hoàn tất phó bản, ta xây dựng các bước thực hiện của hệ thống với chức năng tham gia dungeon, chiến đấu qua các wave và nhận thưởng hoàn tất bằng biểu đồ tuần tự. Luồng này bắt đầu khi người chơi chọn loại phó bản hoặc portal dungeon; hệ thống tiếp nhận yêu cầu và kiểm tra level, lượt vào cùng trạng thái tổ đội trước khi truy vấn `player_data`, `dungeon_config` và `dungeon_wave_config`. Nếu điều kiện hợp lệ, gameplay server khởi tạo phiên dungeon, spawn wave đầu, nạp boss và chuyển người chơi vào map phó bản.

Trong giai đoạn xử lý chính, gameplay server điều khiển toàn bộ vòng lặp chiến đấu qua wave, theo dõi tiến độ, spawn wave tiếp theo và boss cuối, sau đó tổng kết kết quả cho từng người chơi hoặc cả tổ đội khi đạt điều kiện chiến thắng. Ở bước kết thúc, hệ thống gọi xử lý phát thưởng, cập nhật inventory, reward, log và dữ liệu `dungeon_best_waves`, rồi đồng bộ EXP và phần thưởng cho client trước khi đưa người chơi trở về map an toàn. Biểu đồ cũng mô tả rõ các trường hợp ngoại lệ của dự án như người chơi hoặc tổ đội thất bại, hết thời gian, bị hạ gục toàn bộ nên không nhận thưởng hoàn tất; hoặc máy chủ gặp lỗi spawn ở một wave và phải xử lý theo cấu hình fallback của dungeon.

![Biểu đồ tuần tự đặc tả ca sử dụng Tham gia và hoàn tất phó bản](extracted_images/image30.png)

*Hình 2.23. Biểu đồ tuần tự đặc tả ca sử dụng Tham gia và hoàn tất phó bản*

### 2.2.5. Thiết kế cơ sở dữ liệu

Cơ sở dữ liệu của hệ thống Mutants Arena được thiết kế theo hướng chuẩn hóa các bảng định danh, cấu hình và ánh xạ nghiệp vụ, đồng thời sử dụng các cột JSON ở những thành phần gameplay biến đổi nhanh. Cách tổ chức này giúp hệ thống vẫn bảo đảm tính toàn vẹn dữ liệu ở các quan hệ cốt lõi như tài khoản, vật phẩm, Gene, bản đồ, nhiệm vụ và phó bản, nhưng không làm tăng quá nhiều số lượng bảng phụ khi cần đồng bộ túi đồ, trang bị, kỹ năng, buff và tiến trình nhiệm vụ giữa Unity Client, gameplay server và GameServerApi.

Đối chiếu trực tiếp với schema trong `gamedb.sql` và mã nguồn backend, mô hình dữ liệu của đề tài được tổ chức thành bốn cụm logic chính: cụm tài khoản và hồ sơ nhân vật, cụm vật phẩm và nâng cấp trang bị, cụm Gene - Hybrid - kỹ năng, và cụm thế giới game gồm quái vật, bản đồ, nhiệm vụ, boss và dungeon.

#### 2.2.5.1. Sơ đồ kết nối các bảng

Sơ đồ ERD dưới đây thể hiện mối liên kết giữa các bảng dữ liệu trọng tâm đang được sử dụng trực tiếp bởi hệ thống. Bố cục sơ đồ được nhóm theo đúng luồng xử lý thực tế của dự án, từ đăng nhập và hồ sơ nhân vật, sang vật phẩm và Gene, rồi tới nội dung PvE gồm bản đồ, quái vật, nhiệm vụ và phó bản.

![Sơ đồ cơ sở dữ liệu của hệ thống Mutants Arena](extracted_images/image31.png)

Hình 2.24. Sơ đồ cơ sở dữ liệu của hệ thống Mutants Arena

#### 2.2.5.2. Cấu trúc các bảng chính

Bảng 2.18. Nhóm bảng tài khoản, hồ sơ nhân vật và xã hội

| Bảng | Vai trò | Thuộc tính tiêu biểu |
|---|---|---|
| users | Lưu định danh tài khoản, phục vụ đăng ký, đăng nhập và phát hành JWT | user_id, username, email, password_hash, created_at, last_login |
| player_data | Lưu hồ sơ gameplay chính của người chơi, gồm chỉ số, trang bị, túi đồ, kỹ năng, buff và tiến trình nhiệm vụ | player_id, character_name, info_char, equipment, inventory, skills, active_buffs |
| player2_data | Lưu hồ sơ nhân vật hệ gene thứ hai, được backend sử dụng ở các luồng secondary gene và kỹ năng hệ phụ | player_id, character_name, info_char, skills, potential_stats, active_buffs |
| friend_relations | Lưu quan hệ bạn bè và trạng thái chấp nhận lời mời giữa các tài khoản | id, user_id, friend_id, status, created_at |

Bảng 2.19. Nhóm bảng vật phẩm, option và nâng cấp trang bị

| Bảng | Vai trò | Thuộc tính tiêu biểu |
|---|---|---|
| item_template | Định nghĩa toàn bộ mẫu vật phẩm của game như trang bị, potion, đá cường hóa, nguyên liệu Gene, vé dungeon và túi mở rộng | id, name, type, idClass, idIcon, levelNeed, isLock, sellPrice |
| item_effect_template | Cấu hình hiệu ứng phát sinh khi sử dụng item tiêu hao hoặc item buff | item_template_id, effect_type, value, duration_sec, icon_id, display_name |
| option_template | Định nghĩa các dòng chỉ số trang bị và tiến trình tăng chỉ số theo cấp cường hóa | id, name, type, level, strOption |
| equipment_upgrade_config | Cấu hình bạc tiêu hao, đá cần dùng, tỉ lệ thành công và chính sách thất bại khi cường hóa | upgrade_level, silver_cost, stone_id, stone_needed, base_success_rate, fail_policy |

Bảng 2.20. Nhóm bảng Gene, Hybrid và kỹ năng

| Bảng | Vai trò | Thuộc tính tiêu biểu |
|---|---|---|
| gene_upgrade_config | Cấu hình nâng Gene chính theo từng hệ nguyên tố và mốc tier | tier_from, element_type, gene_exp_required, silver_cost, stone_id, stone_needed, base_success_rate |
| gene_multi_config | Cấu hình nâng Gene phụ, phục vụ cơ chế đa hệ và mở rộng Hybrid | tier_from, element_type, gene_exp_required, silver_cost, stone_id, stone_needed, base_success_rate |
| gene_tier_stat_config | Quy định lượng HP, MP, tấn công và phòng thủ cộng thêm khi đạt tier mới | element_type, tier_to, hp_bonus, mp_bonus, attack_bonus, defense_bonus |
| gene_hybrid_config | Định nghĩa các tổ hợp Hybrid, vật phẩm dung hợp, bonus chỉ số và prefab hiển thị trong Unity | hybrid_id, element_a, element_b, hybrid_name, fusion_item_id, fusion_item_count, atk_bonus_percent, prefab_path |
| gene_hybrid_skill | Ánh xạ mỗi tổ hợp Hybrid với kỹ năng đặc biệt tương ứng | id, hybrid_id, skill_code, slot_priority |
| skill_template | Lưu cấu hình kỹ năng tĩnh của game, bao gồm kỹ năng thường, kỹ năng nguyên tố và kỹ năng Hybrid | skill_id, skill_code, skill_name, element_type, levels_json, gene_tier_required, hybrid_id |

Bảng 2.21. Nhóm bảng thế giới game, quái vật, nhiệm vụ và dungeon

| Bảng | Vai trò | Thuộc tính tiêu biểu |
|---|---|---|
| enemy | Lưu dữ liệu quái thường, elite và boss, bao gồm chỉ số chiến đấu, kháng nguyên tố, skill và phần thưởng rơi | enemy_id, enemy_name, level, base_hp, base_damage, enemy_type, drop_items_json, skills_json |
| boss_config | Cấu hình boss theo từng map với thời điểm spawn, vị trí và chu kỳ hồi sinh | boss_id, map_id, spawn_x, spawn_y, respawn_minutes, is_active |
| map_config | Lưu thông tin bản đồ, scene Unity, ngưỡng cấp độ và điều kiện nhiệm vụ để mở khóa map | map_id, map_name, scene_name, min_level, max_level, required_quest_id |
| quest_config | Lưu chuỗi nhiệm vụ, NPC liên quan, phần thưởng và các bước tiến trình dưới dạng JSON | id, name, npc_id, level_need, item_reward, step, is_active |
| dungeon_config | Lưu cấu hình tổng quát của phó bản, gồm loại dungeon, map sử dụng, boss và reward tổng | dungeon_id, dungeon_name, dungeon_type, map_id, max_players, min_level_required, boss_enemy_id, reward_json |
| dungeon_wave_config | Lưu cấu hình wave, giới hạn lượt vào và mốc thưởng theo tiến trình dungeon dạng sóng | dungeon_id, max_waves, wave_time_seconds, enemy_scale_percent, daily_entry_limit, milestone_reward_json |

Thiết kế cơ sở dữ liệu trên phục vụ ba mục tiêu chính. Thứ nhất, dữ liệu định danh tài khoản được tách khỏi dữ liệu gameplay: `users` chỉ lưu thông tin đăng nhập và phát hành JWT, còn `player_data`, `player2_data` và `player_equipment` lưu trạng thái nhân vật, trang bị, túi đồ, kỹ năng, buff và tiến trình nhiệm vụ. Cách tổ chức này giúp bảo vệ thông tin tài khoản và giảm rủi ro khi thay đổi các logic gameplay như combat, Gene, inventory hoặc dungeon. Thứ hai, dữ liệu cấu hình tiến trình RPG được chia thành các cụm riêng gồm vật phẩm - nâng cấp trang bị (`item_template`, `item_effect_template`, `option_template`, `equipment_upgrade_config`), Gene - Hybrid - kỹ năng (`gene_upgrade_config`, `gene_multi_config`, `gene_tier_stat_config`, `gene_hybrid_config`, `gene_hybrid_skill`, `skill_template`) và nội dung PvE (`enemy`, `map_config`, `quest_config`, `boss_config`, `dungeon_config`, `dungeon_wave_config`). Nhờ vậy, hệ thống có thể truy vết và cân bằng đầy đủ vòng đời phát triển nhân vật từ nhận vật phẩm, nâng trang bị, tiến hóa Gene đến làm nhiệm vụ và hoàn thành phó bản mà không phải hard-code trực tiếp ở client. Thứ ba, dữ liệu bản đồ và vận hành được tách khỏi dữ liệu nhân vật thông qua các bảng như `map_portal`, `map_spawn_config`, `leaderboard_cache` và `player_action_log`, hỗ trợ điều hướng map, cấu hình spawn, xếp hạng, audit hành vi người chơi và theo dõi các luồng vận hành runtime của gameplay server qua các API host/heartbeat.

---

## 2.3. Tổng kết chương 2

Chương 2 đã được chuẩn hóa lại theo đúng trạng thái triển khai thực tế của dự án Mutants Arena thay vì chỉ dừng ở mức mô tả ý tưởng. Trên cơ sở đọc trực tiếp mã nguồn API, SignalR Hub và client Unity, hệ thống được xác định có bốn actor chính gồm Guest, Player, Admin/Operator và Máy chủ gameplay / Netcode server. Từ đó, các yêu cầu chức năng được tái cấu trúc thành các nhóm triển khai thật: xác thực, nhân vật, bản đồ, combat, Gene Evolution, inventory/equipment, NPC/quest/shop, social, dungeon, đồng bộ thời gian thực và vận hành server.

Phần Use Case là trọng tâm được viết lại toàn diện với 20 use case đầy đủ, bao phủ toàn bộ tính năng đã có trong mã nguồn: từ đăng ký, đăng nhập, tạo nhân vật, combat, Gene chính/Gene phụ/Hybrid Fusion, blacksmith, NPC service, quest event-driven, friend, party, chat, dungeon, leaderboard đến các luồng vận hành kỹ thuật như zone heartbeat, host runtime, spawn config và dungeon reward grant. Nhờ đó, chương 2 không chỉ mô tả đầy đủ nghiệp vụ của hệ thống mà còn tạo được sự liên kết rõ ràng giữa kiến trúc cài đặt, sơ đồ phân tích và đặc tả chức năng trong báo cáo.

Toàn bộ kết quả phân tích và đặc tả trong chương này là cơ sở trực tiếp cho Chương 3, nơi từng hệ thống sẽ được triển khai, giải thích kiến trúc code và kiểm chứng bằng luồng runtime tương ứng.


---

CHƯƠNG 3.CÀI ĐẶT VÀ HIỆN THỰC HÓA HỆ THỐNG

Chương 3 trình bày quá trình cài đặt và hiện thực hóa hệ thống trong project DoAn, tập trung vào hai nhóm chức năng chính: hệ thống Gene tiến hóa và kiến trúc nhiều người chơi thời gian thực. Dữ liệu trong chương này được đối chiếu trực tiếp từ mã nguồn Unity tại thư mục Client/Assets/Scripts, backend GameServerApi, các controller REST API, SignalR Hub và cơ sở dữ liệu MySQL trong tệp gamedb.sql.

Project DoAn được hiện thực bằng Unity 2D, Unity Netcode for GameObjects, SignalR, ASP.NET Core .NET 9, Entity Framework Core, Pomelo MySQL và JWT Bearer Authentication. Các nội dung trong chương được trình bày theo góc nhìn công nghệ và cơ chế vận hành: đồng bộ bằng NetworkVariable, truyền lệnh bằng ServerRpc/ClientRpc, realtime xã hội bằng SignalR Hub, lưu dữ liệu bằng MySQL/JSON column, xác thực bằng JWT Bearer và bảo vệ request nội bộ bằng Zone API Key.

Các công thức, endpoint và payload trong chương được rút ra từ triển khai thực tế. Với các đoạn công thức tổng hợp, báo cáo chỉ diễn giải lại chuỗi xử lý đã có trong runtime như buff tấn công, kiểm tra kháng nguyên tố, xử lý trạng thái suy yếu, phòng thủ dungeon và phản đòn; các dữ liệu chỉ mới được lưu hoặc trả API như HybridAtkBonusPct, HybridImmuneElements và tang_dame_* không được ghi như công thức runtime nếu chưa có code trực tiếp áp dụng.

3.1Hiện thực hóa phía máy khách (Client-Side - Unity)

3.1.1Hệ thống khởi tạo phiên chơi và kết nối multiplayer

Phía máy khách của DoAn được xây dựng bằng Unity. Sau khi người chơi đăng nhập thành công, client nhận JWT token, userId và dữ liệu nhân vật từ backend. Các dữ liệu có giá trị lâu dài như nhân vật, Gene, kỹ năng, inventory, trang bị và tiến trình phó bản không được hard-code trong prefab, mà được tải qua REST API từ GameServerApi. Khi bước vào gameplay realtime, Unity client kết nối tới zone server bằng Unity Netcode for GameObjects.

Cơ chế xác thực phiên chơi: Sau khi đăng nhập qua API /api/auth/login, client lưu token JWT và đính kèm token vào các request HTTP bằng header Authorization: Bearer <JWT_TOKEN>. Đối với SignalR, token được truyền vào kết nối hub theo cơ chế access token để backend xác định đúng user đang chat hoặc tham gia tổ đội.

Kết nối gameplay realtime bằng Unity Netcode: Quá trình vào game được tổ chức theo mô hình connection approval của Unity Netcode. Khi client gửi thông tin đăng nhập vào zone server, server kiểm tra phiên, xác định map/zone, tải dữ liệu nhân vật từ API rồi spawn NetworkObject đại diện cho người chơi.

Tách riêng gameplay realtime và social realtime: Unity Netcode được dùng cho di chuyển, máu, kỹ năng, quái, boss, dungeon và các đối tượng trong map. SignalR được dùng cho chat và tổ đội thông qua hai hub /chathub và /partyhub. Thiết kế này giúp phần mô phỏng gameplay không bị phụ thuộc vào các chức năng xã hội.

Cấu hình server nội bộ bằng Zone API Key: Các request server-to-server từ Unity zone server về backend không dùng JWT người chơi, mà dùng header X-Zone-Api-Key. Cơ chế này dựa trên một authentication scheme riêng của ASP.NET Core để phân biệt request nội bộ với request người chơi.

Điểm quan trọng trong kiến trúc client là Unity client chỉ giữ vai trò hiển thị, nhập lệnh và nhận phản hồi. Những thao tác ảnh hưởng đến dữ liệu lâu dài như nâng Gene, nâng kỹ năng, nhận thưởng phó bản, thêm vật phẩm hoặc cập nhật vị trí đều đi qua server hoặc backend để hạn chế gian lận.

3.1.2Hệ thống điều khiển, di chuyển và đồng bộ nhân vật

Hệ thống điều khiển nhân vật được hiện thực bằng mô hình NetworkBehaviour của Unity Netcode kết hợp Rigidbody2D. Do game là 2D realtime multiplayer, hệ thống cần đảm bảo người chơi điều khiển nhân vật mượt ở máy local nhưng vẫn đồng bộ được vị trí cho các client khác.

Owner xử lý input cục bộ: Chỉ client sở hữu NetworkObject mới được đọc input và điều khiển Rigidbody2D. Các thao tác như di chuyển ngang, nhảy, rơi qua platform một chiều và cập nhật animation được thực hiện trong FixedUpdate để giảm độ trễ cảm nhận của người chơi.

Gửi trạng thái di chuyển lên server: Mỗi frame vật lý, owner gửi horizontalInput, trạng thái nhảy/rơi, vị trí client, vận tốc trục Y và trạng thái chạm đất lên server thông qua MoveServerRpc. Server ghi lại vị trí vào transform và cập nhật các NetworkVariable cần thiết để các client khác nhìn thấy.

Đồng bộ hướng quay và animation: Hướng mặt của nhân vật được lưu trong NetworkVariable<float> networkScaleX. Animation được phát qua UpdateAnimationClientRpc, trong khi owner vẫn tự cập nhật animation cục bộ để tránh cảm giác phản hồi chậm.

Nội suy cho nhân vật không sở hữu: Các client không sở hữu nhân vật không mô phỏng lại input. Chúng lấy syncPosition từ NetworkVariable và dùng Rigidbody2D.MovePosition kết hợp Vector2.Lerp để kéo nhân vật về vị trí mới. Cơ chế này giảm hiện tượng giật khi gói tin mạng đến không đều.

Để tránh xung đột giữa hai cơ chế đồng bộ, project không phụ thuộc vào NetworkTransform mặc định cho nhân vật chính, mà sử dụng luồng đồng bộ tùy chỉnh bằng ServerRpc và NetworkVariable vị trí.

Dưới đây là đoạn mã cốt lõi xử lý việc owner gửi vị trí lên server thông qua ServerRpc của Unity Netcode. Đoạn mã này được rút từ runtime điều khiển nhân vật mạng trong project:

```csharp
[ServerRpc]
private void MoveServerRpc(float horizontalInput, bool up, bool down,
    Vector2 clientPosition, float clientVelocityY, bool clientIsGrounded)
{
    if (controller == null || controller.stats == null)
        return;

    transform.position = new Vector3(clientPosition.x, clientPosition.y, 0f);

    if (horizontalInput > 0.01f)
        networkScaleX.Value = 1f;
    else if (horizontalInput < -0.01f)
        networkScaleX.Value = -1f;

    float velocityX = horizontalInput * controller.stats.moveSpeed;
    UpdateAnimationClientRpc(velocityX, clientVelocityY, clientIsGrounded, controller.godMode);
}
```

3.1.3Hệ thống đồng bộ dữ liệu nhân vật và trạng thái chiến đấu

Trong môi trường nhiều người chơi, các chỉ số nhân vật không thể chỉ tồn tại trên client cục bộ. Hệ thống áp dụng cơ chế NetworkVariable của Unity Netcode để đồng bộ dữ liệu runtime quan trọng giữa server và tất cả client trong cùng zone.

Đồng bộ thông tin nhân vật: Các thông tin như playerId, hệ nguyên tố, giới tính, tên nhân vật, level, HP, MP, attack, defense, moveSpeed, Gene Tier và partyId được lưu bằng NetworkVariable. Khi nhân vật được spawn, server đọc dữ liệu phiên chơi và ghi vào các biến mạng này để tất cả client nhận cùng một trạng thái.

Cập nhật chỉ số sau nâng cấp: Khi người chơi nâng Gene, thay đổi trang bị hoặc cập nhật dữ liệu nhân vật, client gọi UpdatePlayerDataServerRpc. Server sau đó cập nhật lại NetworkVariable để các client khác nhận được trạng thái mới mà không cần tự gọi lại API.

Đồng bộ tổ đội vào gameplay: networkPartyId được cập nhật thông qua SyncPartyIdServerRpc. Giá trị này giúp server biết những người chơi nào đang cùng party, phục vụ các cơ chế như đi phó bản tổ đội, lọc hiệu ứng hỗ trợ hoặc xử lý tương tác đồng đội.

Hệ thống máu server-authoritative: HP được quản lý bằng NetworkVariable<int> và chỉ server có quyền ghi giá trị cuối cùng. Client không tự ý trừ máu trực tiếp mà gửi yêu cầu qua ServerRpc hoặc để server gọi luồng TakeDamage. Khi HP thay đổi, hệ thống phát event cho UI, animation và hiệu ứng.

Hệ thống EXP và lưu tiến trình: Khi enemy chết, Unity server xác định người chơi hạ gục rồi gọi API /api/player/{playerId}/gain-exp. Backend cập nhật level, experience và dữ liệu nhân vật trong MySQL, bảo đảm tiến trình không phụ thuộc vào client.

3.1.4Hệ thống tương tác và chiến đấu giữa người chơi

Hệ thống chiến đấu trong project bao gồm PvE, boss fight và PvP cơ bản. Luồng chiến đấu được tổ chức bằng va chạm 2D, skill runtime, NetworkVariable HP, ServerRpc và ClientRpc. Phần xử lý sát thương quan trọng được đặt trên server để hạn chế việc client tự sửa damage hoặc máu.

Đánh thường bằng vùng va chạm 2D: Hệ thống tạo vùng đánh tại AttackPoint, sau đó dùng truy vấn vật lý 2D để quét mục tiêu trong attackRange. Nếu mục tiêu là quái, damage được gửi vào luồng máu network của enemy. Nếu mục tiêu là người chơi khác, damage đi qua luồng máu network của player.

Tính sát thương theo chỉ số và buff: Trước khi gây sát thương, hệ thống đọc chỉ số attack của nhân vật và kiểm tra buff tấn công đang active. Nếu người chơi đang có AttackBuff, sát thương được nhân theo phần trăm buff trước khi gửi lên hệ thống máu.

Quản lý máu quái trên server: HP của quái được lưu bằng NetworkVariable, nhận sát thương qua ServerRpc, phát ClientRpc khi bị đánh hoặc chết, xử lý rơi vật phẩm và gọi API cộng EXP cho người chơi hạ gục.

Boss fight có callback tính sát thương: Luồng máu của boss nhận damage kèm elementType, sau đó đi qua bước kiểm tra né tránh, kháng nguyên tố và trạng thái đặc biệt trước khi trừ HP. Sau khi trừ máu, boss có thể xử lý phản damage hoặc chuyển pha.

PvP giữa người chơi: Khi vùng đánh phát hiện collider thuộc player khác, damage vẫn đi qua server trước khi cập nhật HP. Việc trừ máu theo hướng server-authoritative bảo đảm các client trong zone cùng nhìn thấy kết quả giống nhau.

Chi tiết luồng sát thương trong gameplay được tổ chức theo hướng: Client phát lệnh đánh hoặc dùng kỹ năng, server xác định mục tiêu hợp lệ, tính sát thương, cập nhật NetworkVariable HP và phát ClientRpc để hiển thị hiệu ứng.

3.1.5 Hệ thống Gene chính, Gene phụ, Hybrid Fusion, Gene Tối Thượng và cơ chế nâng cấp

Hệ Gene là phần gameplay trọng tâm của project DoAn. Mỗi nhân vật có Gene chính, có thể mở Gene phụ, nâng tier và dung hợp Hybrid khi đạt điều kiện. Toàn bộ cấu hình chi phí, tỉ lệ, vật phẩm, bonus stat và kỹ năng mở khóa được lấy từ backend, không hard-code trong Unity.
Giao diện nâng Gene chính: UI Gene hiển thị tier hiện tại, kinh nghiệm Gene, item yêu cầu, tỉ lệ thành công, bonus stat và kỹ năng mở khóa. Khi người chơi xác nhận nâng cấp, UI không gọi API trực tiếp mà gửi lệnh vào luồng ServerRpc để zone server kiểm tra phiên và gọi backend.

Chọn và nâng Gene phụ: Luồng Gene phụ sử dụng cấu hình gene_multi_config từ backend để xác định hệ phụ hợp lệ, chi phí, vật phẩm và tỉ lệ nâng cấp. Hệ phụ có chi phí riêng và khi nâng thành công sẽ cộng một phần bonus stat vào nhân vật.

Dung hợp Hybrid: UI Hybrid hiển thị điều kiện fuse, item yêu cầu, vàng, tên Hybrid, prefab path, hệ bị khắc và hệ được miễn/giảm khắc. Khi fuse thành công, backend cập nhật IsHybrid, HybridElementA, HybridElementB, HybridBonusTargets, HybridImmuneElements, HybridAtkBonusPct, HybridId và HybridPrefabPath trong info_char.

----- [BẮT ĐẦU PHẦN THÊM MỚI] -----
Kích hoạt Gene Tối Thượng (Ultimate Gene): Kích hoạt sau khi dung hợp Hybrid thành công. Khi nhân vật đạt đủ 1,000,000 EXP Tối Thượng thông qua diệt quái/boss hoặc sử dụng vật phẩm hỗ trợ, trạng thái Gene Tối Thượng (`is_ultimate = true`) được kích hoạt. Thuộc tính HP, MP, ATK, DEF được nhân x1.5 tại `StatCalculator`, đồng thời client tự động hiển thị Aura hào quang tương ứng ra sau lưng nhân vật (tra cứu qua `UltimateAuraDatabase` dựa theo hệ nguyên tố: `aura1` cho Hỏa-Thổ, `aura2` cho Thủy-Mộc, `aura3` cho Kim-Phong) và hiển thị biểu tượng Tối Thượng ✦ trên HUD.
----- [KẾT THÚC PHẦN THÊM MỚI] -----


Luồng nâng Gene qua server: Client không tự sửa dữ liệu Gene. UI gửi lệnh nâng cấp bằng ServerRpc, zone server lấy JWT của client từ session runtime, gọi API /api/gene/upgrade, sau đó trả kết quả về đúng client bằng targeted ClientRpc. Sau khi nhận kết quả, client cập nhật dữ liệu cục bộ và gửi yêu cầu đồng bộ chỉ số mới bằng ServerRpc để các NetworkVariable trong zone được cập nhật.

Dưới đây là đoạn mã rút gọn thể hiện luồng nâng Gene thông qua ServerRpc, REST API và targeted ClientRpc. Đoạn mã này được rút từ command service chạy trên Unity zone server:

```csharp
[ServerRpc(RequireOwnership = false)]
public void UpgradeGeneServerRpc(string requestJson, ServerRpcParams rpcParams = default)
{
    if (!IsServer) return;
    ulong cid = rpcParams.Receive.SenderClientId;
    string jwt = ResolveClientJwt(cid);

    StartCoroutine(DoPost(
        $"{ApiBase}/gene/upgrade", requestJson, jwt,
        json => GeneUpgradeResultClientRpc(json, Target(cid)),
        err  => GeneUpgradeResultClientRpc(ErrorJson(err), Target(cid))
    ));
}
```

3.1.5.1Cơ chế khắc chế Gene và tính sát thương nguyên tố

Bên cạnh việc tăng chỉ số, Gene còn lưu thông tin hệ của người chơi để phục vụ UI, chọn Gene phụ, Hybrid Fusion và các hàm hỗ trợ khắc chế. Dữ liệu hệ của người chơi nằm trong info_char gồm element_type, secondary_element, is_hybrid, hybrid_bonus_targets, hybrid_immune_elements và hybrid_atk_bonus_pct. Dữ liệu hệ của quái/boss có trong bảng enemy gồm element_type, khang_hoa, khang_thuy, khang_tho, khang_moc, khang_kim, khang_phong, tang_dame_* và counter_rate; tuy nhiên cần tách rõ dữ liệu CSDL/API với dữ liệu đã được runtime combat sử dụng. Qua rà soát hai DTO spawn quái chính là `EnemySkillsEntry` (map thường) và `DungeonWaveEnemySpawnDto` (phó bản wave), cả hai chỉ truyền các trường cơ bản gồm `element_type`, `base_damage`, `base_defense` và `base_hp` vào component quái khi spawn; không có trường `tang_dame_*`, `counter_rate`, hay `khang_*` nào được ánh xạ từ DB vào runtime động. Các giá trị kháng nguyên tố (`khangHoa`, `khangThuy`, v.v.) và tỉ lệ phản đòn (`counterRate`) trên `MobPatrolAI` được đặt thủ công trong prefab qua Unity Inspector, không được load từ bảng enemy khi spawn. Điều này có nghĩa là cùng một loại quái có thể có giá trị kháng khác nhau tùy theo cách cấu hình prefab, chứ không nhất thiết phản ánh đúng cột `khang_*` trong CSDL. Vì vậy báo cáo không gộp `tang_dame_*` và `counter_rate` vào công thức sát thương cuối nếu không có đoạn runtime trực tiếp áp dụng từ dữ liệu DB.

Kiến trúc tính sát thương tập trung qua `DamageCalculator`: Để thống nhất các công thức rải rác vốn tồn tại trong từng component, dự án đã hiện thực lớp tiện ích tĩnh `DamageCalculator` tại `Assets/Scripts/Utilities/DamageCalculator.cs`. Lớp này không cần instance, chứa toàn bộ logic công thức của các nhánh combat, và được gọi lại từ từng component: `MobPatrolAI` gọi `CalcEnemyReceivedDamage()`, `BossController` gọi `CalcBossReceivedDamage()`, `NetworkPlayerHealth` gọi `CalcPlayerReceivedElementDamage()`, `DungeonEnemyRuntimeStats` gọi `CalcDungeonEnemyReceivedDamage()`, và cả `PlayerCombat` lẫn `FireballDamage` đều gọi `CalcPlayerAttackDamage()` cho đánh thường và projectile. Mỗi method của `DamageCalculator` chỉ nhận tham số dữ liệu thuần túy, không phụ thuộc vào bất kỳ Singleton hay MonoBehaviour nào, đảm bảo công thức nhất quán giữa tất cả các điểm gọi.

Quan hệ hệ và cặp Hybrid: `ElementHelper.GetCounteredElement()` định nghĩa vòng khắc chế Ngũ Hành ở mức helper: Kim khắc Mộc, Mộc khắc Thủy, Thủy khắc Hỏa, Hỏa khắc Thổ và Thổ khắc Kim; hệ Phong không nằm trong vòng này và trả về null. Phần chọn Gene phụ/Hybrid không dùng toàn bộ vòng này mà dùng cặp cố định trong `ElementHelper.GetFixedSecondary()` và `GeneController.PartnerMap`: Hỏa ↔ Thổ, Thủy ↔ Mộc, Kim ↔ Phong. Đây là logic đang được backend kiểm tra khi gọi API Hybrid Fusion.

Tăng sát thương theo buff và Hybrid Gene bonus: Đòn đánh thường (`PlayerCombat`) và projectile (`FireballDamage`) đều tính sát thương qua `DamageCalculator.CalcPlayerAttackDamage(baseDamage, attackBonusPct, attackerData, targetElementType)`. Hàm này xử lý hai lớp tăng dần: trước tiên áp AttackBuff (nếu có), sau đó kiểm tra Hybrid Gene bonus — nếu người tấn công là Hybrid và hệ mục tiêu nằm trong `hybrid_bonus_targets`, nhân thêm `(1 + hybrid_atk_bonus_pct / 100)`. Đây là lần đầu tiên `hybrid_atk_bonus_pct` được áp dụng trực tiếp vào runtime combat; trước đó trường này chỉ được lưu CSDL và trả qua API mà không ảnh hưởng đến sát thương thực tế. `ActiveBuffManager.GetBonusPct("AttackBuff")` trả về dạng thập phân, ví dụ value = 15 thì trả 0.15.

```text
Sát thương đánh thường (sau buff + Hybrid bonus):
    Bước 1: damage = Round(baseDamage x (1 + attackBonusPct))
    Bước 2: nếu là Hybrid và hệ enemy ∈ hybrid_bonus_targets:
                damage = Round(damage x (1 + hybrid_atk_bonus_pct / 100))
```

Đoạn code tương ứng (qua DamageCalculator):

```csharp
float attackBonusPct = ActiveBuffManager.Instance != null
    ? ActiveBuffManager.Instance.GetBonusPct("AttackBuff") : 0f;
int finalDamage = DamageCalculator.CalcPlayerAttackDamage(
    stats.baseDamage, attackBonusPct, myPlayerData, targetElement);
```

Riêng `FireballDamage` nhận `attackBonusPercent` qua `SetAttackBonus(int bonusPercent)`, sau đó khi va chạm cũng gọi `DamageCalculator.CalcPlayerAttackDamage()` để đảm bảo đường đi projectile nhất quán với đánh thường. `attackBonusPercent` được thiết lập trong `PlayerSkillManager.SpawnProjectile()` ngay sau `SetDamage()`, đọc từ `ActiveBuffManager` của owner:

```csharp
if (ActiveBuffManager.Instance != null)
{
    int atkBonusPct = Mathf.RoundToInt(ActiveBuffManager.Instance.GetBonusPct("AttackBuff") * 100f);
    if (atkBonusPct > 0) fireballDmg.SetAttackBonus(atkBonusPct);
}
```

Kháng nguyên tố của mục tiêu: Công thức kháng nguyên tố thật sự xuất hiện trong hai nhánh runtime. Với quái dùng `MobPatrolAI.TakeDamageWithElement()`, hệ nguyên tố truyền vào là số 1=Hỏa, 2=Thủy, 3=Thổ, 4=Mộc, 5=Kim, 6=Phong. Runtime lấy kháng từ các field trên component `MobPatrolAI`, không tự đọc trực tiếp từ bảng enemy trong hàm này.

```text
Sát thương sau kháng = Max(1, Round(Sát thương gốc x (1 - Chỉ số kháng / 100)))
```

Nếu mục tiêu đang bị `isWeakened`, `MobPatrolAI` tiếp tục nhân sát thương sau kháng thêm 1.3 lần:

```text
Sát thương quái nhận = Round(Max(1, Round(rawDamage x (1 - resist / 100))) x 1.3)
```

Với boss dùng `NetworkBossHealth` và `BossController.HandleBeforeTakeDamage()`, boss có thể né trước; nếu không né, `BossController` lấy kháng từ `BossData` theo elementType dạng chuỗi `Hoa`, `Thuy`, `Tho`, `Moc`, `Kim`, `Phong` rồi tính:

```text
Sát thương boss nhận = Max(1, Round(rawDamage x (1 - resist / 100)))
```

Trong luồng network enemy phổ biến qua `NetworkEnemyHealth.TakeDamageInternal()`, hàm chỉ nhận damage số, không nhận element. Nếu enemy thuộc dungeon có `DungeonEnemyRuntimeStats`, damage được giảm theo phòng thủ:

```text
Sát thương sau giáp dungeon = Max(1, rawDamage - Defense)
```

Khắc hệ nguyên tố tác động lên người chơi: Khi nhân vật người chơi nhận sát thương từ nguồn có gắn hệ nguyên tố (ví dụ quái `MobPatrolAI` gọi `nph.TakeDamageWithElement(counterDmg, elementType)` khi phản đòn), `NetworkPlayerHealth.TakeDamageWithElementInternal()` kiểm tra xem `attackerElement` có phải hệ khắc của người chơi không bằng cách gọi `ElementHelper.GetElementThatCounters(pd.element_type)`. Nếu trùng, sát thương người chơi nhận tăng 30%:

```text
Nếu attackerElement khắc hệ người chơi:
    finalDamage = Round(rawDamage x 1.3)
Nếu không khắc:
    finalDamage = rawDamage
```

Bảng Ngũ Hành Tương Khắc áp dụng trong runtime (theo ElementHelper.GetCounteredElement()):

**Bảng 3.X — Quan hệ khắc chế Ngũ Hành trong runtime**

| Hệ tấn công | Hệ bị khắc |
|---|---|
| Kim — Metal | Mộc — Wood |
| Mộc — Wood | Thủy — Water |
| Thủy — Water | Hỏa — Fire |
| Hỏa — Fire | Thổ — Earth |
| Thổ — Earth | Kim — Metal |
| Phong — Wind | — (không tham gia vòng khắc chuẩn) |

Hybrid miễn khắc hệ trong runtime: Sau khi hoàn thành Hybrid Fusion, backend ghi chuỗi CSV vào trường `HybridImmuneElements` trong `info_char` theo cột `immune_elements` của bảng `gene_hybrid_config`. Khi người chơi bị tấn công bởi hệ nguyên tố, `NetworkPlayerHealth.TakeDamageWithElementInternal()` gọi `ElementHelper.IsImmuneToCounter(attackerElement, pd)` trước khi áp hệ số +30%. Nếu `attackerElement` nằm trong `HybridImmuneElements`, phần tăng bị bỏ qua và người chơi nhận đúng sát thương gốc:

```text
Nếu IsImmuneToCounter(attackerElement, pd) = true:
    finalDamage = rawDamage  // không áp +30%
```
Theo dữ liệu gene_hybrid_config và rule kiểm tra cặp PartnerMap trong GeneController, chỉ ba tổ hợp Hybrid được phép trong hệ thống hiện tại, kèm danh sách hệ miễn tương ứng:

**Bảng 3.X+1 — Ba tổ hợp Hybrid hợp lệ và hệ miễn khắc**

| Tổ hợp Hybrid | Tên Hybrid | Hệ miễn khắc (immune\_elements) |
|---|---|---|
| Hỏa (Fire) + Thổ (Earth) | Dung Nham Địa Hỏa | Thủy (Water), Mộc (Wood) |
| Thủy (Water) + Mộc (Wood) | Băng Độc Vĩnh Hằng | Kim (Metal), Hỏa (Fire) |
| Kim (Metal) + Phong (Wind) | Kim Phong Thoán Thế | Hỏa (Fire), Thổ (Earth) |

Nhân vật Hỏa+Thổ Hybrid sẽ không còn nhận thêm 30% khi bị Thủy hoặc Mộc tấn công. Nhân vật Thủy+Mộc Hybrid miễn với Kim và Hỏa. Nhân vật Kim+Phong Hybrid miễn với Hỏa và Thổ. Cơ chế này đã được hiện thực trực tiếp trong NetworkPlayerHealth.TakeDamageWithElementInternal(), cơ chế miễn khắc hệ này độc lập với `HybridAtkBonusPct` (bonus tấn công) vốn được áp riêng trong `CalcPlayerAttackDamage()`.

Dữ liệu tăng sát thương Hybrid: Khi nhân vật fuse Hybrid thành công, backend ghi danh sách hệ bị khắc vào `HybridBonusTargets` và phần trăm tăng sát thương vào `HybridAtkBonusPct`, ánh xạ từ cột `atk_bonus_percent` của bảng `gene_hybrid_config`. Hai trường này được trả về qua API mỗi lần client đồng bộ dữ liệu nhân vật. Sau khi hiện thực `DamageCalculator`, `hybrid_atk_bonus_pct` đã được áp dụng thực sự vào runtime combat thông qua `CalcPlayerAttackDamage()`: nếu hệ của enemy nằm trong `hybrid_bonus_targets`, sát thương được nhân thêm hệ số tương ứng.

```text
Sát thương sau Hybrid bonus = Round(damage × (1 + hybrid_atk_bonus_pct / 100))
                          (chỉ áp khi hệ enemy thuộc hybrid_bonus_targets)
```

Dữ liệu miễn/giảm khắc Hybrid: Như trình bày ở trên, cơ chế miễn khắc hệ đã được hiện thực trong `NetworkPlayerHealth.TakeDamageWithElementInternal()` thông qua `ElementHelper.IsImmuneToCounter()`. Trường `HybridImmuneElements` là chuỗi CSV được backend ghi vào `info_char` ngay khi fusion thành công (`cfg.ImmuneElements` từ bảng `gene_hybrid_config`), và được trả về qua API mỗi lần client đồng bộ dữ liệu nhân vật để zone server có thể kiểm tra khi xử lý sát thương.

Phản đòn của quái đặc biệt: Runtime phản đòn nằm trong `MobPatrolAI`. Sau khi nhận sát thương bằng `TakeDamageWithElement()`, nếu `counterRate > 0` và số ngẫu nhiên trong khoảng 0-100 nhỏ hơn counterRate, quái kích hoạt `CounterAttack()`. Sát thương phản đòn dùng field `baseDamage` trên component, với công thức:

```text
Sát thương phản đòn = Max(1, Round(baseDamage x 0.6))
```

Do `counter_rate` trong bảng enemy không được DTO spawn ánh xạ vào `MobPatrolAI.counterRate`, giá trị này được đặt thủ công trong prefab. Vì vậy, cơ chế phản đòn là đặc tính cấu hình theo từng prefab, không phải mọi enemy có `counter_rate` trong DB đều tự phản đòn trong runtime.

Tổng quát, các công thức có chứng cứ trực tiếp trong runtime hiện tại là:

```text
── Người chơi tấn công (DamageCalculator.CalcPlayerAttackDamage) ──
Đánh thường / projectile:
    damage = Round(baseDamage x (1 + attackBonusPct))    // AttackBuff
    if is_hybrid AND hệ enemy ∈ hybrid_bonus_targets:
        damage = Round(damage x (1 + hybrid_atk_bonus_pct / 100))  // Hybrid bonus

── Quái/Boss nhận damage từ người chơi ──
MobPatrolAI (DamageCalculator.CalcEnemyReceivedDamage):
    actual = Max(1, Round(rawDamage x (1 - resist / 100)))
    if isWeakened:
        actual = Round(actual x 1.3)

BossController (DamageCalculator.CalcBossReceivedDamage):
    if TryDodge() = true:
        finalDamage = 0
    else:
        finalDamage = Max(1, Round(rawDamage x (1 - resist / 100)))

Dungeon enemy (DamageCalculator.CalcDungeonEnemyReceivedDamage):
    damage = Max(1, rawDamage - Defense)

── Người chơi nhận damage có hệ nguyên tố (DamageCalculator.CalcPlayerReceivedElementDamage) ──
counterOf = ElementHelper.GetElementThatCounters(pd.element_type)
if attackerElement == counterOf:
    if ElementHelper.IsImmuneToCounter(attackerElement, pd):  // Hybrid miễn
        finalDamage = rawDamage
    else:
        finalDamage = Round(rawDamage x 1.3)  // khắc hệ +30%
else:
    finalDamage = rawDamage
```

Dưới đây là mã nguồn xử lý giảm sát thương theo kháng nguyên tố trong runtime AI của quái. Đây là phần trực tiếp sinh ra công thức Sát thương sau kháng ở trên:

```csharp
public void TakeDamageWithElement(int rawDamage, int element = 0)
{
    if (evasionRate > 0 && UnityEngine.Random.Range(0f, 100f) < evasionRate)
    {
        ShowFloatingText("Miss!");
        return;
    }

    float resist = GetResistance(element);
    int actual = DamageCalculator.CalcEnemyReceivedDamage(rawDamage, resist, isWeakened);

    _health.TakeDamage(actual);

    if (counterRate > 0 &&
        UnityEngine.Random.Range(0f, 100f) < counterRate)
    {
        StartCoroutine(CounterAttack());
    }
}
```

3.1.6Hệ thống kỹ năng, phó bản và Zone runtime

Project không vận hành toàn bộ thế giới như một scene đơn. Thay vào đó, map được chia thành các zone logic và các phòng phó bản độc lập. Cách tổ chức này giúp nhiều nhóm người chơi có thể hoạt động song song trong cùng một map hoặc trong các dungeon riêng.

Quản lý zone: Server duy trì registry room theo mapId, zoneId và custom room. Registry này cho phép tra cứu room hiện tại, tìm zone ít tải, kiểm tra hai client có cùng zone hay không và đăng ký room phó bản riêng.

Chuyển zone và vào phó bản: Luồng chuyển khu vực sử dụng ServerRpc cho các thao tác chuyển map, vào dungeon cá nhân, vào dungeon tổ đội và thoát dungeon. Khi người chơi qua cổng, server cập nhật room, lưu vị trí và gửi ClientRpc để client load scene/entry point phù hợp.

Lọc hiển thị theo zone: Hệ thống sử dụng cơ chế NetworkObject visibility của Unity Netcode. Nếu client không cùng zone hoặc không cùng custom room, server gọi NetworkHide để ẩn object khỏi client đó. Khi người chơi đổi zone, visibility được refresh để cập nhật lại danh sách object được nhìn thấy.

Phó bản theo wave: Runtime phó bản được quản lý theo từng zone encounter độc lập. Mỗi encounter có dungeonId, config, round hiện tại, thời gian còn lại và danh sách enemy đang sống. Session phó bản được lưu theo userId để hỗ trợ reconnect không mất tiến trình.

Phần thưởng phó bản: Unity zone server gửi request về backend bằng X-Zone-Api-Key để cộng vật phẩm và phần thưởng cho người chơi sau khi hoàn thành phó bản hoặc đạt mốc wave. Client không trực tiếp gọi API nhận thưởng.

----- [BẮT ĐẦU PHẦN THÊM MỚI] -----

3.1.6.1 Hệ thống kỹ năng chi tiết theo từng lớp nguyên tố

Hệ thống chiến đấu của trò chơi phân chia nhân vật thành 6 lớp nguyên tố, mỗi lớp sở hữu bộ 4 kỹ năng chủ động (phím tắt Q, W, E, R) với các hiệu ứng đặc trưng riêng biệt:

*   **Lớp Kim (Metal) - Sát thương vật lý & bạo kích:**
    *   *Kỹ năng Q - Kim Kiếm:* Bắn ra luồng phi kiếm kim loại xuyên thấu, gây sát thương vật lý và tăng 10% tỉ lệ bạo kích trong 5 giây.
    *   *Kỹ năng W - Kim Quang Trảm:* Chém nhanh hình cánh quạt phía trước, gây sát thương bộc phát và làm giảm 15% phòng ngự kẻ địch.
    *   *Kỹ năng E - Thiết Giáp:* Kích hoạt trạng thái kim loại hóa, tăng 30% chỉ số DEF trong thời gian 10 giây.
    *   *Kỹ năng R (Ultimate) - Vạn Kiếm Quy Tông:* Gọi mưa kiếm từ trên trời rơi xuống khu vực chỉ định, gây sát thương vật lý liên tục diện rộng và làm giảm 40% tốc độ chạy của mọi mục tiêu trúng đòn.
*   **Lớp Mộc (Wood) - Độc tố & hồi phục:**
    *   *Kỹ năng Q - Độc Diệp:* Phóng lá độc gây sát thương ban đầu và áp dụng hiệu ứng DoT Poison (rút HP theo giây) kéo dài 6 giây.
    *   *Kỹ năng W - Mộc Phược:* Rễ cây trồi lên từ mặt đất trói chân mục tiêu, gây hiệu ứng Choáng/Trói chân (Stun/Bind) trong 2 giây.
    *   *Kỹ năng E - Trị Liệu Sinh Mệnh:* Triệu hồi luồng sinh khí hồi phục 3% tối đa HP mỗi giây (Regeneration) cho bản thân và đồng đội trong bán kính nhỏ.
    *   *Kỹ năng R (Ultimate) - Mộc Thần Giáng Lâm:* Triệu hồi vùng rừng cây gai sắc nhọn, gây sát thương phép nguyên tố Mộc cực lớn và trói chân diện rộng toàn bộ kẻ địch trúng chiêu.
*   **Lớp Thủy (Water) - Làm chậm & đóng băng:**
    *   *Kỹ năng Q - Băng Thương:* Bắn thương băng tầm xa gây sát thương phép Thủy và làm chậm 35% tốc độ di chuyển của kẻ địch.
    *   *Kỹ năng W - Băng Giáp:* Tạo lớp lá chắn băng hấp thụ sát thương. Kẻ địch tấn công cận chiến vào lá chắn sẽ bị làm chậm tốc độ đánh 20%.
    *   *Kỹ năng E - Trị Liệu Thuật:* Hồi phục ngay lập tức một lượng HP lớn tương đương 15% Max HP của bản thân.
    *   *Kỹ năng R (Ultimate) - Thủy Long Trảo:* Triệu hồi rồng nước khổng lồ cuốn quét qua khu vực, gây sát thương phép diện rộng và đóng băng hoàn toàn (Freeze) kẻ địch trong 2.5 giây.
*   **Lớp Hỏa (Fire) - Thiêu đốt & bộc phát sát thương:**
    *   *Kỹ năng Q - Hỏa Cầu:* Phóng cầu lửa nổ gây sát thương phép nguyên tố Hỏa và kích hoạt hiệu ứng cháy (Burn, rút HP liên tục).
    *   *Kỹ năng W - Hỏa Bạo:* Gây nổ xung quanh bản thân, đẩy lùi (Knockback) toàn bộ kẻ địch đang tiếp cận cận chiến.
    *   *Kỹ năng E - Hỏa Giáp:* Kích hoạt hào quang lửa, tăng 20% chỉ số ATK của bản thân và phản lại 10% sát thương nhận vào dưới dạng sát thương lửa.
    *   *Kỹ năng R (Ultimate) - Hỏa Thần Phẫn Nộ:* Phun trào cột lửa khổng lồ từ lòng đất tại vị trí mục tiêu, gây sát thương phép diện rộng cực đại.
*   **Lớp Thổ (Earth) - Phòng ngự & khống chế cứng:**
    *   *Kỹ năng Q - Thạch Tiễn:* Bắn mũi tên đá cứng gây sát thương vật lý và đẩy lùi nhẹ mục tiêu.
    *   *Kỹ năng W - Địa Chấn:* Dậm chân mạnh xuống đất làm rung chuyển mặt đất xung quanh, gây sát thương Thổ và làm choáng kẻ địch trong 1.5 giây.
    *   *Kỹ năng E - Thạch Giáp:* Tạo một khiên đá bảo vệ hấp thụ sát thương tương đương 25% lượng máu tối đa (Max HP) của nhân vật.
    *   *Kỹ năng R (Ultimate) - Hộ Thể Quyền:* Hóa đá toàn thân, tăng 60% chỉ số phòng ngự DEF và miễn nhiễm hoàn toàn với mọi hiệu ứng khống chế trong vòng 8 giây.
*   **Lớp Phong (Wind) - Cơ động & né tránh:**
    *   *Kỹ năng Q - Phong Nhận:* Bắn ra luồng gió sắc bén xuyên qua nhiều kẻ địch trên đường thẳng.
    *   *Kỹ năng W - Phong Đao:* Chém ra các lốc xoáy nhỏ kéo (Pull) kẻ địch lại gần nhau để chuẩn bị cho combo đồng đội.
    *   *Kỹ năng E - Phong Linh Tốc:* Tăng 45% tốc độ di chuyển và cộng 20% tỉ lệ né tránh (Evasion) của bản thân trong 6 giây.
    *   *Kỹ năng R (Ultimate) - Bão Phong Loạn Vũ:* Tạo cơn bão gió xoáy cuộn quét liên tục tại vị trí chọn, gây sát thương diện rộng liên tục và hút nhẹ kẻ địch vào tâm bão.

3.1.6.2 Hệ thống trí tuệ nhân tạo (AI) quái vật và Boss đa giai đoạn

Hệ thống trí tuệ nhân tạo (AI) quái vật và đặc biệt là Boss trong dự án được xây dựng dựa trên mô hình Máy trạng thái hữu hạn (Finite State Machine - FSM) hoạt động độc lập và chịu sự kiểm soát hoàn toàn bởi máy chủ (Dedicated Server-Authoritative) nhằm đảm bảo tính bảo mật và nhất quán dữ liệu mạng.

Kiến trúc FSM trên Server-Side: Máy trạng thái hữu hạn của quái và Boss chuyển đổi tuần hoàn dựa vào khoảng cách đến người chơi gần nhất. Trạng thái hoạt động bao gồm:
- Idle (Đứng yên): Trạng thái nghỉ ban đầu hoặc khi người chơi nằm ngoài tầm phát hiện.
- Patrol (Tuần tra): Quái thường di chuyển qua lại giữa các điểm cắm chốt (patrol points).
- Chase (Đuổi theo): Khi phát hiện người chơi trong tầm kiểm soát (detectionRange), AI sẽ chuyển sang bám đuổi theo trục X hoặc trục Y.
- Attack (Tấn công): Khi mục tiêu nằm trong tầm tấn công cận chiến (meleeAttackRange) hoặc tầm cast kỹ năng đặc biệt, AI sẽ dừng di chuyển để thực hiện chuỗi hoạt động tấn công.
- Dodging (Né tránh): Khi nhận sát thương từ người chơi, server tính toán tỉ lệ né tránh (dodgeChance). Nếu né thành công, AI sẽ chuyển sang trạng thái né lùi (back-dash) và triệt tiêu toàn bộ sát thương nhận vào.
- Stealthed (Ẩn thân): Boss làm mờ đi bằng cách giảm độ mờ đục (alpha) của SpriteRenderer nhằm gây khó khăn cho người chơi trong việc định vị.
- Dead (Bị tiêu diệt): Khi lượng máu về 0, AI dừng toàn bộ tác vụ, chuyển sang animation chết và chuẩn bị kích hoạt cơ chế spawn vật phẩm thưởng.

Cơ chế nhảy và di chuyển platformer nâng cao: Để thích ứng với bản đồ dạng platformer 2D nhiều tầng, AI được trang bị các thuật toán di chuyển thông minh hơn:
- Nhảy vượt địa hình (Platformer Jumping): Nếu người chơi ở vị trí cao hơn và Boss được cấu hình nhảy (canJump = true), hệ thống sẽ tính toán độ chênh lệch độ cao và kích hoạt nhảy (jumpForce, maxJumps) để tiếp cận mục tiêu.
- Trạng thái bay lượn (Fly Mode): Với các Boss có khả năng bay (canFly = true), hệ thống sẽ tắt trọng lực (gravityScale = 0) và di chuyển Boss lượn trên không trung theo vị trí người chơi cộng thêm một offset độ cao nhất định (flyHeight).

Hệ thống Boss chuyển giai đoạn đa mức HP (Multi-phase System): Hệ thống quản lý các ngưỡng HP còn lại của Boss để thay đổi hình thái tấn công động (ví dụ: Giai đoạn 1 ở mức 100% HP, Giai đoạn 2 dưới 60% HP, Giai đoạn 3 dưới 30% HP). Các phase này được nạp động từ cơ sở dữ liệu qua cột phases_json dưới dạng JSON hoặc thông qua ScriptableObject BossData. Khi chuyển pha, Boss sẽ phát trigger animation tương ứng (enrage, berserk), tăng hệ số sát thương (damage_multiplier), tăng tốc độ (speed_multiplier), giảm thời gian hồi kỹ năng (skill_cooldown_multiplier), đồng thời có thể tự động triệu hồi thêm quái đệ tử hỗ trợ (summon action). Đặc biệt, đối với phó bản tổ đội (Party Dungeon), để gia tăng thử thách và tính chiến thuật, Boss sẽ triệu hồi các bản sao (prefabs) của chính nó nhưng được thu nhỏ kích thước đi 2 lần (scale giảm 2 lần). Các đệ tử này hoạt động hoàn toàn độc lập, tự động bám đuổi và tấn công người chơi nhưng chỉ số máu (HP) và sát thương đã được giảm bớt (giảm 80% HP và 50% sát thương) để cân bằng độ khó.

Các kỹ năng đặc biệt của Boss:
- Hỏa Cầu Mưa (Fireball Rain): Spawn liên tiếp các hỏa cầu từ trên không trung rơi tự do xuống đầu người chơi. Để tránh việc hỏa cầu bị cản bởi các tầng platform trung gian, hệ thống cấu hình hỏa cầu chỉ bị phá hủy khi chạm đất tầng cuối (GroundFinal) hoặc chạm trực tiếp vào người chơi.
- Sét Liên Tiếp (Lightning Strike): Triệu hồi một chuỗi tia sét theo hàng ngang xung quanh vị trí người chơi. Kẻ địch trúng sét ngoài nhận sát thương lớn còn bị choáng (Stun) trong vài giây nhờ việc gọi hàm làm choáng trên component PlayerMovement.
- Phản sát thương (Return Damage): Khi được cấu hình, mỗi khi nhận sát thương từ người chơi, Boss sẽ phản lại một lượng sát thương cố định trực tiếp vào người chơi đó.

Dưới đây là đoạn mã nguồn xử lý việc kiểm tra né tránh và giảm sát thương theo kháng nguyên tố của Boss trước khi bị trừ HP thực sự trên server, kết hợp phương thức kiểm tra môi trường phó bản tổ đội và triệu hồi bản sao thu nhỏ:

```csharp
// Kiểm tra môi trường phó bản tổ đội (Party Dungeon) dựa vào sự tồn tại của PartyDungeonRuntime trong scene
private bool IsInPartyDungeon()
{
    int myMapId = GetMyMapId();
    if (myMapId == -999) return false;

    var runtimes = FindObjectsByType<PartyDungeonRuntime>(FindObjectsSortMode.None);
    foreach (var runtime in runtimes)
    {
        if (runtime.gameObject.scene == gameObject.scene)
            return true;
    }
    return false;
}

// Triệu hồi đệ tử con: trong phó bản tổ đội sẽ triệu hồi bản sao của chính mình giảm 2 lần scale
private IEnumerator SummonAdds(int count)
{
    bool isPartyDungeon = IsInPartyDungeon();
    GameObject prefabToSpawn = addSpawnPrefab;

    if (isPartyDungeon && EnemyPrefabManager.Instance != null)
    {
        GameObject bossPrefab = EnemyPrefabManager.Instance.GetEnemyPrefab(bossId);
        if (bossPrefab != null)
            prefabToSpawn = bossPrefab;
    }

    if (prefabToSpawn == null) yield break;

    for (int i = 0; i < count; i++)
    {
        Vector2 offset = UnityEngine.Random.insideUnitCircle * 3f;
        GameObject add = Instantiate(prefabToSpawn, (Vector2)transform.position + offset, Quaternion.identity);

        if (isPartyDungeon && prefabToSpawn != addSpawnPrefab)
        {
            // Scale giảm đi 2 lần
            add.transform.localScale = prefabToSpawn.transform.localScale * 0.5f;

            // Vô hiệu hóa phase đệ quy và giảm sát thương nhận vào của đệ tử con
            BossAI minionAI = add.GetComponent<BossAI>();
            if (minionAI != null)
            {
                minionAI.useDefaultHpPhasesWhenMissing = false;
                minionAI._damageMultiplier = 0.5f;
            }

            // Thiết lập HP của đệ tử con bằng 20% máu Boss chính
            NetworkEnemyHealth netHealth = add.GetComponent<NetworkEnemyHealth>();
            if (netHealth != null)
            {
                int bossMaxHp = _health != null ? _health.GetMaxHealth() : 1000;
                netHealth.PreInitMaxHp(Mathf.Max(100, Mathf.RoundToInt(bossMaxHp * 0.2f)));
            }
        }

        MoveSpawnedObjectToCurrentMap(add);
        ApplyMapVisibility(add, GetMyMapId());
        SpawnNetworkObjectIfNeeded(add);
        yield return new WaitForSeconds(0.3f);
    }
}
```

----- [KẾT THÚC PHẦN THÊM MỚI] -----

3.1.7 Hệ thống chat, bạn bè và tổ đội trên client

Các chức năng xã hội trong project được triển khai bằng SignalR để tách khỏi luồng mô phỏng gameplay của Unity Netcode.

Chat nhiều kênh: Client kết nối SignalR tới /chathub, hỗ trợ World, Proximity, Clan, Class, Group và Private. Client đăng ký các event ReceiveWorldMessage, ReceiveProximityMessage, ReceiveClanMessage, ReceiveClassMessage, ReceiveGroupMessage, ReceivePrivateMessage và ReceiveSystemMessage.

Tổ đội realtime: Client kết nối SignalR tới /partyhub, tự động reconnect, gửi UpdatePresence mỗi 5 giây và xử lý các event PartyStateUpdated, PartyInviteReceived, PartyJoinRequestReceived, PartySearchResults, NearbyPlayersUpdated, PartyDungeonRequested và PartyError.

Đồng bộ chat nhóm khi vào party: Khi trạng thái party thay đổi, client tự join hoặc leave group chat tương ứng trên SignalR. Nhờ vậy, người chơi trong cùng tổ đội có thể trò chuyện riêng mà không ảnh hưởng tới kênh world hoặc proximity.

Vào phó bản tổ đội: Khi leader gọi StartPartyDungeon, SignalR Hub phát PartyDungeonRequested đến toàn bộ thành viên trong party. Sau đó client chuyển sang luồng ServerRpc vào phó bản tổ đội để toàn bộ thành viên được đưa vào cùng dungeon room.

Dưới đây là đoạn client đăng ký một số event SignalR của tổ đội:

```csharp
_client.On("PartyStateUpdated", json =>
{
    CurrentParty = PartyStatePayload.FromJson(json);
    SyncChatGroup();
    OnPartyStateChanged?.Invoke(CurrentParty);
});

_client.On("PartyDungeonRequested", json =>
{
    var payload = PartyDungeonRequestPayload.FromJson(json);
    OnPartyDungeonRequested?.Invoke(payload);
});
```

3.1.8Tổng kết phân hệ Client

Phân hệ client Unity của DoAn hiện thực hóa một game 2D online có khả năng chơi nhiều người thông qua Unity Netcode và SignalR. Unity Netcode chịu trách nhiệm điều khiển nhân vật, đồng bộ vị trí, NetworkVariable chỉ số, HP, sát thương và phó bản runtime. SignalR chịu trách nhiệm chat, party, presence và lời mời tổ đội. REST API đảm nhiệm dữ liệu lâu dài như Gene, inventory, kỹ năng, EXP và phần thưởng. Cách chia trách nhiệm này giúp gameplay nhiều người chơi có thể mở rộng mà không làm rối lớp giao diện.

3.2Hiện thực hóa phía máy chủ dịch vụ (Backend-Side - ASP.NET Core)

3.2.1Hệ thống quản lý tài khoản và xác thực

Backend của project được xây dựng bằng ASP.NET Core .NET 9, Entity Framework Core 9, Pomelo MySQL, JWT Bearer Authentication và SignalR. Tại tầng khởi động, hệ thống đăng ký ASP.NET Core Controller, OpenAPI, CORS, SignalR Hub, memory cache, DbContext, service nghiệp vụ và cơ chế xác thực lai HybridAuth.

Đăng ký và đăng nhập: Nhóm API xác thực cung cấp hai endpoint chính POST /api/auth/register và POST /api/auth/login. Khi đăng ký, backend kiểm tra username/email trùng, băm mật khẩu bằng BCrypt rồi lưu vào bảng users. Khi đăng nhập, backend kiểm tra mật khẩu, cập nhật LastLogin và trả về JWT token, user_id và username.

JWT cho client: Hệ thống sử dụng BCrypt.Net.BCrypt để băm mật khẩu với workFactor = 12 và JwtSecurityTokenHandler để tạo JWT. Token chứa các claim sub, unique_name và user_id, dùng issuer GameServerApi và audience GameClient.

HybridAuth cho nhiều loại request: ASP.NET Core Authentication được cấu hình theo chính sách lai. Request từ client dùng Bearer JWT, còn request nội bộ từ zone server dùng Zone API Key thông qua header X-Zone-Api-Key.

SignalR token: Với /chathub và /partyhub, JWT có thể được truyền qua query parameter access_token. Cách này phù hợp với cơ chế kết nối WebSocket của SignalR trong Unity.

Điểm tiếp nhận xác thực của backend là nhóm API Auth. Luồng đăng nhập dưới đây được rút từ endpoint POST /api/auth/login:

```csharp
[HttpPost("login")]
public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
{
    var user = await _db.Users
        .FirstOrDefaultAsync(u => u.Username == request.Username);

    if (user == null || !_authService.VerifyPassword(request.Password, user.PasswordHash))
        return Unauthorized("Sai username hoặc password.");

    user.LastLogin = DateTime.UtcNow;
    await _db.SaveChangesAsync();

    var token = _authService.GenerateJwtToken(user);
    return Ok(new { token, user_id = user.UserId, username = user.Username });
}
```

3.2.2Hệ thống dữ liệu nhân vật, inventory, trang bị và kỹ năng

Dữ liệu nhân vật được backend lưu trong hai bảng player_data và player2_data. Mỗi bản ghi chứa các cột JSON như info_char, equipment, inventory, skills, potential_stats và active_buffs. Cách lưu này giúp hệ thống dễ mở rộng thuộc tính nhân vật mà không phải thay đổi schema cho từng chỉ số nhỏ.

Tải dữ liệu nhân vật: Nhóm API Player cung cấp GET /api/player/{playerId}/data và GET /api/player/{playerId}/data2. Response trả về thông tin nhân vật, trang bị, inventory, skills, potential_stats, active_buffs và final_stats để Unity client dựng lại trạng thái đầy đủ.

Tạo nhân vật và slot Gene thứ hai: POST /api/player/create tạo nhân vật chính, còn POST /api/player/create2 tạo dữ liệu nhân vật slot 2 trong player2_data. Slot 2 có thể dùng hệ Gene khác, kỹ năng khác và dữ liệu phát triển riêng.

Inventory dạng JSON: Các endpoint /inventory/add, /inventory/clear, /inventory/sort và /inventory/use-item thao tác trên inventory JSON. Khi Unity server cần thêm vật phẩm từ dungeon hoặc lệnh hệ thống, request được gửi về backend để dữ liệu túi đồ được lưu thống nhất.

Trang bị và túi mở rộng: Các endpoint /equipment/equip, /equipment/unequip và /bag/unequip cập nhật equipment JSON, xử lý túi mở rộng, chỉ số trang bị và tính lại final_stats thông qua StatCalculator.

Kỹ năng và tiềm năng: Backend cung cấp /skills, /skills/upgrade, /potential, /potential/upgrade, /potential/allocate. Khi người chơi nâng kỹ năng hoặc cộng điểm tiềm năng, server kiểm tra điều kiện rồi ghi lại dữ liệu vào MySQL.

EXP và lên cấp: POST /api/player/{playerId}/gain-exp nhận lượng EXP từ Unity server, cập nhật experience/level và trả lại trạng thái mới. Đây là điểm nối giữa combat realtime và tiến trình nhân vật lâu dài.

3.2.3Hệ thống Gene Evolution, Gene phụ và Hybrid Fusion ở backend

Nhóm API Gene là trung tâm nghiệp vụ của hệ Gene, sử dụng route /api/gene và yêu cầu xác thực. Tầng controller xử lý cấu hình Gene chính, nâng cấp Gene, chọn Gene phụ, nâng Gene phụ, lấy danh sách Gene và dung hợp Hybrid.

Cấu hình Gene chính: GET /api/gene/config đọc gene_upgrade_config, item_template, gene_tier_stat_config và skill_template để trả về chi phí nâng cấp, Gene EXP yêu cầu, vật phẩm, số lượng tối thiểu/tối đa, tỉ lệ thành công, bonus stat và kỹ năng mở ở tier tiếp theo.

Nâng Gene chính: POST /api/gene/upgrade kiểm tra playerId, Gene EXP, vàng, vật phẩm trong inventory và giới hạn Tier 5. Tỉ lệ thành công được tính bằng baseSuccessRate nhân với tỉ lệ itemCount/itemsNeeded. Nếu thành công, server tăng GeneTier, cộng stat từ gene_tier_stat_config, hồi đầy HP/MP, mở skill mới và trả về final_stats.

Danh sách Gene: GET /api/gene/list trả về Gene chính, Gene phụ, EXP, trạng thái isHybrid, tên Hybrid, hybridBonusTargets, hybridImmuneElements, hybridAtkBonusPct và canFuse. Client dùng dữ liệu này để hiển thị toàn bộ trạng thái Gene.

Chọn Gene phụ: POST /api/gene/secondary/select chỉ cho chọn hệ phụ theo bảng ánh xạ hợp lệ: Fire với Earth, Water với Wood, Metal với Wind. Khi hợp lệ, server set SecondaryElement, SecondaryGeneTier = 1 và SecondaryGeneExp = 0.

Nâng Gene phụ: GET /api/gene/multi/config và POST /api/gene/secondary/upgrade sử dụng bảng gene_multi_config. Khi nâng thành công, hệ phụ tăng tier và cộng 50% bonus stat so với cấu hình gene_tier_stat_config.

Dung hợp Hybrid: GET /api/gene/hybrid/config kiểm tra điều kiện fuse. POST /api/gene/hybrid/fuse yêu cầu Gene chính Tier 5, Gene phụ Tier 5, cặp hệ hợp lệ, đủ vàng và đủ item fuse. Khi thành công, backend ghi trạng thái Hybrid vào info_char và cộng bonus stat từ gene_hybrid_config.

----- [BẮT ĐẦU PHẦN THÊM MỚI] ----- Phát triển Gene Tối Thượng (Ultimate Gene): Sau khi nhân vật hoàn tất dung hợp Hybrid, người chơi có thể tích lũy EXP Tối Thượng (`ultimate_gene_exp`) để đạt cấp tiến hóa cao nhất. Cấu hình ngưỡng kích hoạt (mặc định `1,000,000` EXP), hệ số nhân chỉ số (nhân x1.5 toàn bộ chỉ số HP, MP, ATK, DEF) và tài nguyên hào quang được lưu trong `GeneUltimateSettings` (tại `GeneUltimateConfig.cs`). Khi đạt đủ EXP qua diệt quái/boss hoặc sử dụng vật phẩm hỗ trợ, server kích hoạt `is_ultimate = true`, đồng thời `StatCalculator` tính toán lại và nhân x1.5 toàn bộ chỉ số thuộc tính cơ bản của nhân vật để đồng bộ qua mạng. ----- [KẾT THÚC PHẦN THÊM MỚI] -----

Đoạn xử lý tỉ lệ nâng Gene ở tầng backend được rút từ endpoint POST /api/gene/upgrade như sau:

```csharp
itemCount = Math.Clamp(itemCount, cfg.ItemsMin, cfg.ItemsNeeded);

float successRate = cfg.BaseSuccessRate *
    Math.Min((float)itemCount / cfg.ItemsNeeded, 1f);
successRate = Math.Clamp(successRate, 0f, 1f);
bool success = new Random().NextDouble() < successRate;

info.Gold -= cfg.GoldCost;
info.GeneExp = Math.Max(0, info.GeneExp - cfg.GeneExpRequired);
```

Khi Hybrid Fusion thành công, backend ghi dữ liệu khắc chế Hybrid vào info_char. Các trường dưới đây được rút từ luồng POST /api/gene/hybrid/fuse:

```csharp
info.IsHybrid = true;
info.HybridElementA = info.ElementType;
info.HybridElementB = info.SecondaryElement;
info.HybridBonusTargets = cfg.BonusTargetElements;
info.HybridImmuneElements = cfg.ImmuneElements;
info.HybridAtkBonusPct = cfg.AtkBonusPercent;
info.HybridId = cfg.HybridId;
info.HybridPrefabPath = cfg.PrefabPath;
```

3.2.4Hệ thống bạn bè, chat và tổ đội realtime

Hệ thống sử dụng kết hợp REST API và SignalR để hiện thực chức năng xã hội. REST API quản lý dữ liệu bạn bè qua HTTP, còn SignalR Hub xử lý chat, presence, lời mời tổ đội và cập nhật trạng thái realtime.

REST API bạn bè: Route /api/friends cung cấp GET /api/friends, POST /api/friends/request, PUT /api/friends/{id}/accept, DELETE /api/friends/{id} và GET /api/friends/search?q=. Backend lấy userId từ claim JWT để đảm bảo người chơi chỉ thao tác với quan hệ bạn bè của chính mình.

SignalR Hub cho chat: Hub /chathub hỗ trợ SendWorldMessage, SendProximityMessage, JoinMap, LeaveMap, SendClanMessage, SendClassMessage, SendGroupMessage, JoinGroup, LeaveGroup và SendPrivateMessage. Mỗi loại chat được phát tới group SignalR tương ứng như map_{mapId}, clan_{clanId}, class_{classType} hoặc group_{groupId}.

SignalR Hub cho tổ đội: Hub /partyhub lưu trạng thái runtime bằng ConcurrentDictionary, gồm Parties, PresenceByUser và ConnectionsByUser. Các hàm chính gồm UpdatePresence, CreateParty, InviteMember, RequestJoinParty, AcceptJoinRequest, RejectJoinRequest, LeaveParty, DisbandParty, SetLock, SetAutoAccept, GetPartiesInZone, GetNearbyPlayers và StartPartyDungeon.

Giới hạn tổ đội: Hub tổ đội đặt MaxPartyMembers = 4. Leader là người tạo party hoặc người còn lại được chuyển quyền khi leader rời. Khi party thay đổi, backend phát PartyStateUpdated đến SignalR group của party.

Tìm party và người chơi gần khu vực: Client gọi GetPartiesInZone(mapId, zoneId) hoặc GetNearbyPlayers(mapId, zoneId). Backend dựa vào presence do client gửi định kỳ để trả về các party còn slot và người chơi đang cùng map/zone.

Luồng mời người chơi vào party được hiện thực bằng SignalR. Khi leader gọi InviteMember, backend tạo hoặc lấy party hiện tại, kiểm tra số lượng thành viên rồi gửi PartyInviteReceived đến target user.

3.2.5Hệ thống dungeon, wave session và phần thưởng

Nhóm API Dungeon cung cấp dữ liệu cấu hình phó bản, session phó bản và wave dungeon cho Unity. Phần runtime diễn ra trên Unity zone server, còn backend giữ vai trò nguồn cấu hình và nơi lưu tiến trình/entry.

Danh sách và chi tiết phó bản: GET /api/dungeon/list và GET /api/dungeon/{dungeonId} trả về cấu hình phó bản để client hiển thị NPC phó bản và điều kiện tham gia.

Session phó bản: Các endpoint /session/create, /session/{sessionId}/join, /session/{sessionId}/leave và /session/{sessionId}/end dùng để quản lý session phó bản thông thường.

Cấu hình map và wave runtime: GET /api/dungeon/map/{mapId}/setup trả về cấu hình map. GET /api/dungeon/wave/{dungeonId}/config trả về cấu hình wave, enemy, boss và phần thưởng để Unity server spawn runtime.

Kiểm tra lượt vào wave: GET /api/dungeon/wave/{dungeonId}/entry-status/{playerId} kiểm tra trạng thái vào phó bản của người chơi. POST /api/dungeon/wave/{dungeonId}/enter ghi nhận lượt vào.

Cập nhật và kết thúc wave session: POST /api/dungeon/wave/{dungeonId}/session/update và /session/end được Unity server gọi để lưu tiến trình wave và kết quả phó bản.

Cấp thưởng an toàn: DungeonRewardController được bảo vệ bằng ZoneApiKey. Unity zone server gọi endpoint này để cấp vật phẩm sau khi người chơi hoàn thành nội dung, tránh việc client tự gọi nhận thưởng.

3.2.6Hệ thống Zone Server và đồng bộ hạ tầng runtime

Hệ thống sử dụng zone server Unity chạy runtime, sau đó đăng ký và gửi heartbeat về backend để backend biết server nào đang hoạt động, đang mở port nào và có bao nhiêu người chơi trong từng zone.

Đăng ký zone server: POST /api/zone/server/register nhận thông tin port và cấu hình server từ Unity. Request này yêu cầu X-Zone-Api-Key để chỉ server nội bộ mới được đăng ký.

Heartbeat định kỳ: Unity zone server gọi PUT /api/zone/server/heartbeat. Payload gồm port, tổng số player và danh sách zoneStats theo mapId, zoneId, players, max. Backend dùng dữ liệu này để theo dõi server còn sống và phân bổ người chơi vào zone phù hợp.

Hủy đăng ký server: DELETE /api/zone/server/deregister?port=... được gọi khi server dừng hoặc shutdown để backend xóa trạng thái server runtime.

Lọc visibility theo zone: Thông tin zone được Unity server quản lý bằng registry room và NetworkObject visibility. Backend không trực tiếp render đối tượng, nhưng heartbeat giúp lớp dịch vụ biết tải hiện tại của từng zone.

3.2.7Tổng kết phân hệ Backend

Backend của DoAn được tổ chức theo mô hình ASP.NET Core Controller, service, SignalR Hub và Entity Framework entity. Nhóm API Auth xử lý đăng nhập/đăng ký, nhóm API Player quản lý nhân vật, nhóm API Gene quản lý Gene và Hybrid, nhóm API Dungeon quản lý phó bản, nhóm API Friend quản lý bạn bè, SignalR Hub xử lý realtime social và nhóm API Zone Server giám sát zone server. Hệ thống sử dụng ASP.NET Core, JWT, SignalR và MySQL, phù hợp với kiến trúc game online nhiều người chơi có dữ liệu nhân vật lưu lâu dài.

3.3Đặc tả giao diện lập trình ứng dụng (RESTful API Specifications)

Hệ thống cung cấp tập hợp API REST dùng JSON làm định dạng payload. Các API công khai gồm đăng ký và đăng nhập. Các API người chơi thường dùng JWT qua header Authorization: Bearer <JWT_TOKEN>; API nội bộ của zone server dùng X-Zone-Api-Key ở các controller có cấu hình scheme tương ứng. Riêng DungeonController hiện tại chưa gắn [Authorize], nên các endpoint /api/dungeon/* trong code không tự bắt buộc JWT hoặc Zone API Key. Danh sách endpoint trong mục này được đối chiếu từ route và HTTP attribute của các ASP.NET Core Controller trong backend. Các JSON mẫu dưới đây giữ đúng tên field do controller trả về; giá trị động như token, id hoặc timestamp được ghi bằng dạng <...> để tránh nhầm thành dữ liệu seed thật.

3.3.1Nhóm API xác thực (Auth Controller)

3.3.1.1API đăng ký tài khoản - POST /api/auth/register

Mô tả: Tạo tài khoản mới, kiểm tra username/email trùng, băm mật khẩu và trả về JWT token.

Yêu cầu:

```json
{
  "username": "<username>",
  "email": "<email>",
  "password": "<password>"
}
```

Phản hồi thành công:

```json
{
  "token": "<jwt_token>",
  "user_id": "<user_id>",
  "message": "Register thành công."
}
```

3.3.1.2API đăng nhập - POST /api/auth/login

Mô tả: Kiểm tra username/password và cấp JWT token cho client.

Yêu cầu:

```json
{
  "username": "<username>",
  "password": "<password>"
}
```

Phản hồi thành công:

```json
{
  "token": "<jwt_token>",
  "user_id": "<user_id>",
  "username": "<username>"
}
```

3.3.2Nhóm API nhân vật và inventory (Player Controller)

Nhóm API nhân vật chịu trách nhiệm tạo nhân vật, tải hồ sơ nhân vật, lưu vị trí, thao tác túi đồ, trang bị vật phẩm và cộng EXP. Tất cả endpoint trong nhóm này sử dụng JSON payload. Các request từ client yêu cầu header Authorization: Bearer <JWT_TOKEN>; một số request nội bộ từ zone server có thể dùng X-Zone-Api-Key tùy luồng runtime.

3.3.2.1API tạo nhân vật chính - POST /api/player/create

Mô tả: Tạo nhân vật chính cho tài khoản hiện tại. Backend lấy user_id từ JWT, kiểm tra tài khoản đã có nhân vật hay chưa, validate element_type và character_name, tự suy ra gender theo hệ Gene, sau đó khởi tạo info_char mặc định gồm level, experience, gold, HP, MP, element_type, gene_tier, gene_exp, bag_slots, map_id, zone_id và vị trí ban đầu.

Endpoint: POST /api/player/create

Header:

```http
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
```

Yêu cầu (Request Body):

```json
{
  "element_type": "Wind",
  "character_name": "Phong"
}
```

Phản hồi thành công (Response 200 OK):

```json
{
  "player_id": 16,
  "level": 1,
  "experience": 0,
  "gold": 0,
  "map_id": 0,
  "position_x": 0,
  "position_y": 0,
  "base_stats": {
    "hp": 100,
    "max_hp": 100,
    "mp": 50,
    "max_mp": 50,
    "attack": 10,
    "defense": 0
  },
  "final_stats": {
    "hp": 100,
    "max_hp": 100,
    "mp": 50,
    "max_mp": 50,
    "attack": 10,
    "defense": 0,
    "move_speed": 5
  },
  "inventory": [],
  "skills": [],
  "element_type": "Wind",
  "gene_tier": 1,
  "gene_exp": 0,
  "is_hybrid": false,
  "gender": "Female",
  "character_name": "Phong"
}
```

Lỗi phổ biến:
o400 Bad Request: Thiếu element_type, tên nhân vật rỗng, tên nhân vật không nằm trong khoảng 3-20 ký tự, hoặc element_type không thuộc Metal, Wood, Water, Fire, Earth, Wind.
o401 Unauthorized: Token không hợp lệ hoặc không lấy được user_id từ JWT.
o409 Conflict: Tài khoản đã có nhân vật chính.

3.3.2.2API tải dữ liệu nhân vật - GET /api/player/{playerId}/data

Mô tả: Tải toàn bộ dữ liệu nhân vật để Unity dựng lại trạng thái gameplay. Backend đọc player_data, parse info_char/equipment/inventory/skills/potential_stats/active_buffs, xử lý level-up nếu đủ EXP, áp dụng buff HP/MP còn hiệu lực và tính final_stats bằng StatCalculator.

Endpoint: GET /api/player/{playerId}/data

Header:

```http
Authorization: Bearer <JWT_TOKEN>
```

Tham số đường dẫn:
oplayerId: ID nhân vật cần tải.

Phản hồi thành công (Response 200 OK):

Ví dụ dưới đây lấy theo player_id = 16 trong gamedb.sql. Các mảng dài như inventory chỉ trích một phần tử đầu để tránh làm báo cáo quá dài; tên field và kiểu dữ liệu giữ theo response thật của PlayerController.

```json
{
  "player_id": 16,
  "user_id": 16,
  "level": 100,
  "experience": 3700,
  "exp_required_for_next_level": 0,
  "exp_at_current_level": 0,
  "gold": 14990,
  "silver": 398400000,
  "map_id": 0,
  "zone_id": 0,
  "position_x": 0,
  "position_y": 0,
  "base_stats": {
    "hp": 2335,
    "max_hp": 2335,
    "mp": 566,
    "max_mp": 566,
    "attack": 760,
    "defense": 200
  },
  "equipment": {
    "weapon": {
      "itemTemplateId": 203,
      "itemName": "Kiếm Hỏa Thần",
      "itemType": 1,
      "upgradeLevel": 1,
      "strOptions": ""
    },
    "helmet": {
      "itemTemplateId": 100,
      "itemName": "Mũ Da Nam",
      "itemType": 0,
      "upgradeLevel": 0,
      "strOptions": "3,30"
    },
    "armor": null,
    "pants": null,
    "boots": null,
    "accessory": {
      "itemTemplateId": 141,
      "itemName": "Nhẫn Bạc",
      "itemType": 5,
      "upgradeLevel": 8,
      "strOptions": ""
    }
  },
  "potential_stats": {
    "attack": 505,
    "hp": 0,
    "mp": 0,
    "defense": 0,
    "gene": 0
  },
  "final_stats": {
    "hp": 2335,
    "max_hp": 2365,
    "mp": 566,
    "max_mp": 566,
    "attack": 3285,
    "defense": 200,
    "move_speed": 5
  },
  "inventory": [
    {
      "slotIndex": 0,
      "itemTemplateId": 200,
      "quantity": 1,
      "upgradeLevel": 16,
      "strOptions": "1,12"
    }
  ],
  "skills": [],
  "skill_points_available": 300,
  "potential_points_available": 0,
  "element_type": "Wind",
  "gene_tier": 5,
  "gene_exp": 1000000,
  "is_hybrid": true,
  "gender": "Female",
  "character_name": "Phong",
  "bag_slots": 35,
  "bag_equipped_items": [
    {
      "quick_slot_index": 2,
      "item_template_id": 63,
      "item_name": "Túi Mở Rộng Cấp 3",
      "slot_bonus": 5
    }
  ],
  "secondary_element": "Metal",
  "secondary_gene_tier": 5,
  "secondary_gene_exp": 0,
  "hybrid_id": 13,
  "hybrid_element_a": "Wind",
  "hybrid_element_b": "Metal",
  "hybrid_bonus_targets": "Wood,Fire",
  "hybrid_immune_elements": "Fire,Earth",
  "hybrid_atk_bonus_pct": 0.5,
  "hybrid_prefab_path": "Prefabs/Player/Hybrid/Hybrid_Metal_Wind",
  "is_ultimate": true,
  "ultimate_gene_exp": 1005000,
  "ultimate_aura_path": "Prefabs/Player/Aura/UltimateAura3"
}
```

Lỗi phổ biến:
o404 Not Found: Player không tồn tại.
o401 Unauthorized: Request không có token hợp lệ.

3.3.2.3API cập nhật vị trí nhân vật - PUT /api/player/{playerId}/position

Mô tả: Lưu map, zone và tọa độ hiện tại của người chơi khi chuyển map, thoát game hoặc disconnect. Nếu request đến từ client, backend lấy user_id từ JWT để tránh giả mạo playerId. Nếu request đến từ zone server, backend chấp nhận playerId trên URL thông qua quyền GameServer của Zone API Key.

Endpoint: PUT /api/player/{playerId}/position

Header:

```http
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
```

Hoặc đối với zone server:

```http
X-Zone-Api-Key: <ZONE_API_KEY>
Content-Type: application/json
```

Yêu cầu (Request Body):

```json
{
  "map_id": 1,
  "zone_id": 2,
  "position_x": 12.5,
  "position_y": -3.2
}
```

Yêu cầu reset về map bắt đầu:

```json
{
  "reset_to_start_map": true
}
```

Phản hồi thành công (Response 200 OK):

```json
{
  "message": "Position updated successfully",
  "map_id": 1,
  "zone_id": 2,
  "position_x": 12.5,
  "position_y": -3.2
}
```

Lỗi phổ biến:
o400 Bad Request: Thiếu map_id, position_x hoặc position_y hợp lệ.
o401 Unauthorized: Không có JWT hoặc Zone API Key hợp lệ.
o404 Not Found: Player không tồn tại.

3.3.2.4API thêm vật phẩm vào inventory - POST /api/player/{playerId}/inventory/add

Mô tả: Thêm một hoặc nhiều vật phẩm vào inventory JSON của nhân vật. Backend lấy user_id từ JWT để xác định player thật, chuẩn hóa slotIndex, loại bỏ các field dư thừa cũ, tìm ô trống trong giới hạn bag_slots và lưu lại inventory mới.

Endpoint: POST /api/player/{playerId}/inventory/add

Header:

```http
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
```

Yêu cầu (Request Body):

```json
{
  "items": [
    {
      "itemTemplateId": 410,
      "quantity": 3,
      "upgradeLevel": 0,
      "strOptions": ""
    }
  ]
}
```

Phản hồi thành công (Response 200 OK):

```json
{
  "message": "Đã thêm 1 item(s) vào inventory",
  "player_id": 16,
  "inventory": [
    {
      "slotIndex": 0,
      "itemTemplateId": 410,
      "quantity": 3,
      "upgradeLevel": 0,
      "strOptions": ""
    }
  ],
  "updated_at": "<server_utc_time>"
}
```

Lỗi phổ biến:
o400 Bad Request: Thiếu field items hoặc danh sách items rỗng.
o401 Unauthorized: Không lấy được user_id từ JWT.
o404 Not Found: Player không tồn tại.

3.3.2.5API trang bị vật phẩm - POST /api/player/{playerId}/equipment/equip

Mô tả: Trang bị vật phẩm từ một slot inventory vào slot trang bị tương ứng. Backend đọc item_template để xác định loại trang bị, tháo item cũ ở slot nếu cần, đưa item cũ về inventory, xóa item mới khỏi inventory và cập nhật equipment JSON.

Endpoint: POST /api/player/{playerId}/equipment/equip

Header:

```http
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
```

Yêu cầu (Request Body):

```json
{
  "inventorySlotIndex": 0
}
```

Phản hồi thành công (Response 200 OK):

```json
{
  "message": "Đã trang bị Kiếm Hỏa Thần vào slot weapon",
  "player_id": 16,
  "equipment_slot": "weapon",
  "equipment": {
    "weapon": {
      "itemTemplateId": 203,
      "itemName": "Kiếm Hỏa Thần",
      "itemType": 1,
      "upgradeLevel": 1,
      "strOptions": ""
    }
  },
  "inventory": [],
  "updated_at": "<server_utc_time>"
}
```

Lỗi phổ biến:
o400 Bad Request: Thiếu inventorySlotIndex, không tìm thấy item ở slot, item không phải trang bị hoặc túi đồ không còn ô trống để trả item cũ.
o404 Not Found: Player hoặc item_template không tồn tại.

3.3.2.6API nhận EXP - POST /api/player/{playerId}/gain-exp

Mô tả: Cộng EXP cho nhân vật sau khi hạ quái hoặc hoàn thành nội dung. Backend kiểm tra amount là số nguyên dương, cộng vào experience, tự động xử lý level-up nếu đủ EXP, lưu lại info_char và trả về trạng thái level mới.

Endpoint: POST /api/player/{playerId}/gain-exp

Header:

```http
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
```

Yêu cầu (Request Body):

```json
{
  "amount": 500
}
```

Phản hồi thành công (Response 200 OK):

```json
{
  "success": true,
  "experience": 4200,
  "level": 101,
  "leveled_up": true,
  "exp_at_current_level": 200,
  "exp_for_next_level": 5000
}
```

Lỗi phổ biến:
o400 Bad Request: Thiếu amount hoặc amount không phải số nguyên dương.
o404 Not Found: Player không tồn tại.

3.3.3Nhóm API Gene (Gene Controller)

Nhóm API Gene chịu trách nhiệm đọc cấu hình Gene, nâng Gene chính, quản lý Gene phụ và dung hợp Hybrid. Dữ liệu cấu hình được đọc từ các bảng gene_upgrade_config, gene_multi_config, gene_tier_stat_config, gene_hybrid_config, gene_hybrid_skill, item_template và skill_template.

3.3.3.1API lấy cấu hình Gene chính - GET /api/gene/config

Mô tả: Trả về cấu hình nâng Gene chính theo hệ hiện tại và tier hiện tại. Backend đọc chi phí Gene EXP, vàng, vật phẩm yêu cầu, số lượng tối thiểu/tối đa, tỉ lệ thành công, bonus stat của tier kế tiếp và danh sách skill sẽ mở khóa.

Endpoint: GET /api/gene/config?elementType=Fire&tier=1

Header:

```http
Authorization: Bearer <JWT_TOKEN>
```

Tham số query:
oelementType: Hệ Gene chính, ví dụ Fire, Water, Earth, Wood, Metal, Wind.
otier: Tier hiện tại, chỉ nhận giá trị từ 1 đến 4.

Phản hồi thành công (Response 200 OK):

```json
{
  "tierFrom": 1,
  "tierTo": 2,
  "elementType": "Fire",
  "geneExpRequired": 500,
  "goldCost": 10000,
  "itemId": 17,
  "itemName": "Linh Thạch Sơ Cấp",
  "itemIcon": 651,
  "itemsMin": 2,
  "itemsNeeded": 5,
  "baseSuccessRate": 0.8,
  "statBonus": {
    "hp": 200,
    "mp": 50,
    "attack": 25,
    "defense": 8
  },
  "skillsToUnlock": [
    {
      "skillId": 12,
      "skillName": "Hỏa Cầu",
      "elementType": "Fire",
      "iconId": "icon_fire_burst"
    }
  ]
}
```

Lỗi phổ biến:
o400 Bad Request: Thiếu elementType hoặc tier không nằm trong khoảng 1-4.
o404 Not Found: Không có cấu hình Gene cho elementType/tier tương ứng.

3.3.3.2API nâng Gene chính - POST /api/gene/upgrade

Mô tả: Kiểm tra playerId, Gene EXP, vàng và vật phẩm trong inventory. Backend clamp itemCount theo itemsMin/itemsNeeded, tính tỉ lệ thành công bằng baseSuccessRate x min(itemCount/itemsNeeded, 1), trừ vàng, trừ vật phẩm, trừ Gene EXP. Nếu thành công, backend tăng GeneTier, cộng stat từ gene_tier_stat_config, hồi đầy HP/MP, mở skill mới và trả final_stats cho client.

Endpoint: POST /api/gene/upgrade

Header:

```http
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
```

Yêu cầu (Request Body):

```json
{
  "playerId": "<player_id>",
  "itemCount": 5
}
```

Phản hồi thành công (Response 200 OK):

```json
{
  "success": true,
  "newGeneTier": 2,
  "newGeneExp": 0,
  "gold": 95000,
  "message": "Gene Fire đã lên Tier 2!",
  "statBonus": {
    "hp": 200,
    "mp": 50,
    "attack": 25,
    "defense": 8
  },
  "final_stats": {
    "hp": 300,
    "max_hp": 300,
    "mp": 100,
    "max_mp": 100,
    "attack": 35,
    "defense": 8,
    "move_speed": 5
  },
  "newlyUnlockedSkills": [],
  "updatedInventory": []
}
```

Trường hợp thất bại tỉ lệ: API vẫn trả 200 OK với success = false, vàng/vật phẩm/Gene EXP đã bị trừ theo luật nâng cấp và newGeneTier giữ nguyên.

Lỗi phổ biến:
o400 Bad Request: Thiếu playerId, Gene đã đạt Tier 5, thiếu Gene EXP, thiếu vàng, thiếu item hoặc không có config Gene.
o404 Not Found: Player không tồn tại.

3.3.3.3API lấy danh sách Gene - GET /api/gene/list

Mô tả: Trả về trạng thái Gene hiện tại của nhân vật, bao gồm Gene chính, Gene phụ, trạng thái Hybrid, danh sách hệ bị tăng sát thương, hệ miễn/giảm khắc, phần trăm bonus và cờ canFuse.

Endpoint: GET /api/gene/list?playerId=16

Header:

```http
Authorization: Bearer <JWT_TOKEN>
```

Phản hồi thành công (Response 200 OK):

```json
{
  "primaryElement": "Wind",
  "primaryTier": 5,
  "primaryExp": 1000000,
  "secondaryElement": "Metal",
  "secondaryTier": 5,
  "secondaryExp": 0,
  "isHybrid": true,
  "hybridName": "Kim Phong Thoán Thế",
  "hybridBonusTargets": "Wood,Fire",
  "hybridImmuneElements": "Fire,Earth",
  "hybridAtkBonusPct": 0.5,
  "canFuse": false
}
```

Lỗi phổ biến:
o404 Not Found: Player không tồn tại.

3.3.3.4API chọn Gene phụ - POST /api/gene/secondary/select

Mô tả: Chọn hệ Gene phụ lần đầu cho nhân vật. Backend chỉ cho chọn theo bảng ánh xạ hợp lệ: Fire kết hợp Earth, Water kết hợp Wood, Metal kết hợp Wind. Sau khi chọn, hệ phụ được khởi tạo SecondaryGeneTier = 1 và SecondaryGeneExp = 0.

Endpoint: POST /api/gene/secondary/select

Header:

```http
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
```

Yêu cầu (Request Body):

```json
{
  "playerId": "<player_id>",
  "secondaryElement": "Metal"
}
```

Phản hồi thành công (Response 200 OK):

```json
{
  "success": true,
  "primaryElement": "Wind",
  "secondaryElement": "Metal",
  "secondaryTier": 1,
  "message": "Đã chọn hệ phụ: Metal! Bắt đầu nâng cấp hệ phụ."
}
```

Lỗi phổ biến:
o400 Bad Request: Thiếu playerId, thiếu secondaryElement, đã chọn hệ phụ trước đó, hoặc hệ phụ không đúng cặp hợp lệ.
o404 Not Found: Player không tồn tại.

3.3.3.5API nâng Gene phụ - POST /api/gene/secondary/upgrade

Mô tả: Nâng cấp Gene phụ bằng cấu hình gene_multi_config. Luồng kiểm tra tài nguyên, tính tỉ lệ thành công và trừ vật phẩm tương tự Gene chính. Khi thành công, SecondaryGeneTier tăng lên và nhân vật nhận 50% bonus stat so với gene_tier_stat_config.

Endpoint: POST /api/gene/secondary/upgrade

Header:

```http
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
```

Yêu cầu (Request Body):

```json
{
  "playerId": "<player_id>",
  "itemCount": 5
}
```

Phản hồi thành công (Response 200 OK):

```json
{
  "success": true,
  "secondaryElement": "Metal",
  "newSecondaryTier": 2,
  "newSecondaryExp": 0,
  "gold": 90000,
  "canFuse": false,
  "final_stats": {
    "hp": 250,
    "max_hp": 250,
    "mp": 125,
    "max_mp": 125,
    "attack": 40,
    "defense": 15
  },
  "updatedInventory": []
}
```

Lỗi phổ biến:
o400 Bad Request: Chưa chọn hệ phụ, Gene phụ đã đạt Tier 5, thiếu EXP/vàng/item hoặc không có cấu hình gene_multi_config.
o404 Not Found: Player không tồn tại.

3.3.3.6API lấy cấu hình Hybrid Fusion - GET /api/gene/hybrid/config

Mô tả: Kiểm tra điều kiện dung hợp Hybrid và trả về cấu hình fuse gồm tên Hybrid, mô tả, hai hệ thành phần, bonusTargets, immuneElements, atkBonusPercent, chi phí vàng, item fuse, số lượng item hiện có, trạng thái đủ item/vàng và bonus stat khi fuse.

Endpoint: GET /api/gene/hybrid/config?playerId=<eligible_player_id>

Header:

```http
Authorization: Bearer <JWT_TOKEN>
```

Ghi chú dữ liệu: Response thành công dưới đây dùng cấu hình thật của cặp Metal/Wind trong gene_hybrid_config. Người chơi gọi API phải chưa Hybrid, có Gene chính/phụ đúng cặp và cả hai Tier 5; nếu dùng player_id = 16 trong seed hiện tại thì controller trả lỗi vì nhân vật này đã là Hybrid.

Phản hồi thành công (Response 200 OK):

```json
{
  "hybridName": "Kim Phong Thoán Thế",
  "hybridDescription": "Kiếm kim loại sắc bén lướt theo cơn gió — tốc độ và sát thương phong trào vô song.",
  "elementA": "Wind",
  "elementB": "Metal",
  "elementATier": 5,
  "elementBTier": 5,
  "bonusTargets": ["Wood", "Fire"],
  "immuneElements": ["Fire", "Earth"],
  "atkBonusPercent": 0.5,
  "fusionGoldCost": 2000000,
  "fusionItemId": 52,
  "fusionItemName": "Lõi Đột Biến Phong",
  "fusionItemIcon": 324,
  "fusionItemCount": 5,
  "availableItems": "<available_item_count>",
  "itemSufficient": true,
  "goldSufficient": true,
  "playerGold": "<player_gold>",
  "canFuse": true,
  "statBonus": {
    "hp": 2000,
    "mp": 500,
    "attack": 750,
    "defense": 200
  }
}
```

Lỗi phổ biến:
o400 Bad Request: Đã là Hybrid, chưa chọn hệ phụ, cặp hệ không hợp lệ, Gene chính hoặc Gene phụ chưa đạt Tier 5.
o404 Not Found: Không tìm thấy config Hybrid cho cặp hệ.

3.3.3.7API dung hợp Hybrid - POST /api/gene/hybrid/fuse

Mô tả: Dung hợp Gene chính và Gene phụ khi cả hai đạt Tier 5. Backend kiểm tra cặp hệ hợp lệ, kiểm tra vàng và item fuse, trừ tài nguyên, cập nhật IsHybrid, HybridElementA, HybridElementB, HybridBonusTargets, HybridImmuneElements, HybridAtkBonusPct, HybridId, HybridPrefabPath, cộng stat bonus và cập nhật danh sách skill Hybrid.

Endpoint: POST /api/gene/hybrid/fuse

Header:

```http
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
```

Yêu cầu (Request Body):

```json
{
  "playerId": "<eligible_player_id>"
}
```

Ghi chú dữ liệu: Response thành công dưới đây dùng cùng cấu hình thật Hybrid id 13. Các trường gold, final_stats và updatedInventory phụ thuộc trạng thái runtime của người chơi sau khi trừ tài nguyên.

Phản hồi thành công (Response 200 OK):

```json
{
  "success": true,
  "hybridName": "Kim Phong Thoán Thế",
  "hybridDescription": "Kiếm kim loại sắc bén lướt theo cơn gió — tốc độ và sát thương phong trào vô song.",
  "hybridId": 13,
  "hybridElementA": "Wind",
  "hybridElementB": "Metal",
  "prefabPath": "Prefabs/Player/Hybrid/Hybrid_Metal_Wind",
  "comboSkillCode": "<combo_skill_code>",
  "bonusTargets": ["Wood", "Fire"],
  "immuneElements": ["Fire", "Earth"],
  "atkBonusPercent": 0.5,
  "statBonus": {
    "hp": 2000,
    "mp": 500,
    "attack": 750,
    "defense": 200
  },
  "gold": "<gold_after_fusion>",
  "message": "HYBRID FUSION THÀNH CÔNG! Kim Phong Thoán Thế đã thức tỉnh!",
  "final_stats": {
    "hp": 2335,
    "max_hp": 2335,
    "mp": 566,
    "max_mp": 566,
    "attack": 760,
    "defense": 200
  },
  "updatedInventory": []
}
```

Lỗi phổ biến:
o400 Bad Request: Thiếu playerId, đã là Hybrid, chưa đủ Tier 5, thiếu vàng, thiếu item fuse hoặc cặp hệ không hợp lệ.
o404 Not Found: Player hoặc cấu hình Hybrid không tồn tại.

----- [BẮT ĐẦU PHẦN THÊM MỚI] -----

3.3.3.8 API tích lũy EXP và kích hoạt Gene Tối Thượng - POST /api/gene/ultimate/add-exp

Mô tả: Tích lũy EXP Tối Thượng (Ultimate Gene EXP) cho nhân vật sau khi đã dung hợp Hybrid thành công thông qua phần thưởng diệt quái/boss hoặc sử dụng vật phẩm hỗ trợ. Khi lượng EXP tích lũy đạt mốc 1,000,000, hệ thống tự động kích hoạt trạng thái Gene Tối Thượng (`is_ultimate = true`), nhân x1.5 toàn bộ các chỉ số thuộc tính cơ bản của nhân vật (HP, MP, ATK, DEF).

Endpoint: POST /api/gene/ultimate/add-exp

Header:
```http
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
```

Body mẫu (Request JSON):
```json
{
  "player_id": 16,
  "exp_added": 1500
}
```

Phản hồi thành công (Response 200 OK):
```json
{
  "success": true,
  "ultimate_gene_exp": 1001500,
  "is_ultimate": true,
  "message": "Trạng thái Gene Tối Thượng đã được kích hoạt! Hào quang thức tỉnh!",
  "final_stats": {
    "hp": 3502,
    "max_hp": 3502,
    "mp": 849,
    "max_mp": 849,
    "attack": 1140,
    "defense": 300
  }
}
```

Lỗi phổ biến:
o400 Bad Request: Nhân vật chưa dung hợp Hybrid (không thể tích lũy EXP Tối Thượng), hoặc lượng EXP cộng thêm không hợp lệ.
o404 Not Found: Nhân vật không tồn tại.

----- [KẾT THÚC PHẦN THÊM MỚI] -----

3.3.4Nhóm API phó bản (Dungeon Controller)

Nhóm API phó bản cung cấp cấu hình dungeon, cấu hình wave runtime, kiểm tra lượt vào, tạo session wave và lưu tiến trình phó bản. Các endpoint này được Unity client dùng để hiển thị NPC phó bản và được Unity zone server dùng để khởi tạo runtime hoặc lưu trạng thái reconnect. Trong code hiện tại, DungeonController chỉ có [ApiController] và [Route("api/[controller]")], chưa có [Authorize]; vì vậy báo cáo không ghi các endpoint /api/dungeon/* là bắt buộc JWT hoặc X-Zone-Api-Key. API cộng thưởng phó bản là controller riêng DungeonRewardController và mới là nơi yêu cầu ZoneApiKey.

3.3.4.1API lấy danh sách phó bản - GET /api/dungeon/list

Mô tả: Trả về danh sách phó bản đang active để client hiển thị tại NPC dungeon. Danh sách được sắp xếp theo min_level_required và dungeon_id.

Endpoint: GET /api/dungeon/list

Phản hồi thành công (Response 200 OK):

```json
{
  "dungeons": [
    {
      "dungeon_id": 6,
      "dungeon_name": "Phó Bản Sóng",
      "dungeon_type": "solo",
      "map_id": 110,
      "map_name": "Vòng lặp vô tận",
      "scene_name": "DungeonWaveScene",
      "max_players": 1,
      "min_level_required": 1,
      "time_limit_seconds": 0,
      "description": "",
      "thumbnail_icon_id": "",
      "boss_enemy_id": null,
      "reward_json": "{}"
    }
  ]
}
```

3.3.4.2API lấy chi tiết phó bản - GET /api/dungeon/{dungeonId}

Mô tả: Trả về chi tiết một phó bản, bao gồm thông tin map, scene, giới hạn người chơi, boss, spawn point và danh sách enemy spawn đã resolve theo map.

Endpoint: GET /api/dungeon/{dungeonId}

Phản hồi thành công (Response 200 OK):

```json
{
  "dungeon_id": 6,
  "dungeon_name": "Phó Bản Sóng",
  "dungeon_type": "solo",
  "map_id": 110,
  "map_name": "Vòng lặp vô tận",
  "scene_name": "DungeonWaveScene",
  "max_players": 1,
  "min_level_required": 1,
  "time_limit_seconds": 0,
  "description": "",
  "thumbnail_icon_id": "",
  "reward_json": "{}",
  "boss_enemy": null,
  "player_spawn_points": "[{\"x\":0,\"y\":0}]",
  "enemy_spawns": [
    {
      "spawn_id": -11100001,
      "enemy_type_id": 11,
      "spawn_x": -4,
      "spawn_y": -1.7,
      "max_spawn_count": 1,
      "respawn_time": 0,
      "enemy": {
        "enemy_id": 11,
        "enemy_name": "Đế Băng",
        "level": 15,
        "base_hp": 2200,
        "base_damage": 120,
        "base_defense": 35,
        "exp_reward": 900,
        "gold_reward": 380,
        "silver_reward": 1500,
        "drop_items_json": "[{\"item_id\":37,\"drop_chance\":0.5,\"qty_min\":1,\"qty_max\":2},{\"item_id\":207,\"drop_chance\":0.08,\"qty_min\":1,\"qty_max\":1},{\"item_id\":31,\"drop_chance\":0.05,\"qty_min\":1,\"qty_max\":1}]",
        "element_type": "Water",
        "enemy_type": "Normal"
      }
    }
  ]
}
```

Lỗi phổ biến:
o404 Not Found: Dungeon không tồn tại.

3.3.4.3API lấy cấu hình wave runtime - GET /api/dungeon/wave/{dungeonId}/config

Mô tả: Trả về cấu hình runtime cho phó bản dạng wave. Unity zone server sử dụng dữ liệu này để xác định số wave tối đa, thời gian mỗi wave, hệ số scale quái/boss, giới hạn lượt vào/ngày, item vé cộng lượt, milestone reward và danh sách enemy/boss spawn.

Endpoint: GET /api/dungeon/wave/{dungeonId}/config

Ghi chú dữ liệu: Map 110 trong gamedb.sql có nhiều spawn lấy từ map_spawn_config. JSON dưới đây rút gọn mảng enemy_spawns còn một phần tử đầu tiên để trình bày cấu trúc; runtime trả đủ danh sách spawn đã resolve.

Phản hồi thành công (Response 200 OK):

```json
{
  "dungeon_id": 6,
  "dungeon_name": "Phó Bản Sóng",
  "map_id": 110,
  "scene_name": "DungeonWaveScene",
  "max_waves": 20,
  "wave_time_seconds": 300,
  "enemy_scale_percent": 10,
  "boss_scale_percent": 15,
  "exp_gold_scale_percent": 10,
  "daily_entry_limit": 1,
  "entry_item_plus1_id": 409,
  "entry_item_plus2_id": 410,
  "milestone_rewards": [
    {"wave": 5, "exp": 5000, "gold": 500, "items": []},
    {"wave": 10, "exp": 15000, "gold": 1500, "items": []},
    {"wave": 15, "exp": 30000, "gold": 3000, "items": []},
    {"wave": 20, "exp": 50000, "gold": 5000, "items": [{"item_template_id": 31, "qty": 1}]}
  ],
  "enemy_spawns": [
    {
      "enemy_id": 11,
      "enemy_name": "Đế Băng",
      "spawn_x": -4,
      "spawn_y": -1.7,
      "is_boss": false,
      "level": 5,
      "max_hp": 110,
      "max_mp": 500,
      "base_damage": 120,
      "base_defense": 35,
      "exp_reward": 1000,
      "respawn_time": 0,
      "move_speed": 2,
      "can_fly": false,
      "element_type": "Water",
      "drops": [
        {"item_id": 37, "drop_chance": 0.5, "qty_min": 1, "qty_max": 2},
        {"item_id": 207, "drop_chance": 0.08, "qty_min": 1, "qty_max": 1},
        {"item_id": 31, "drop_chance": 0.05, "qty_min": 1, "qty_max": 1}
      ]
    }
  ],
  "boss_spawn": {
    "enemy_id": 12,
    "enemy_name": "Mộc Linh",
    "spawn_x": 18.55,
    "spawn_y": 5.88,
    "is_boss": true,
    "level": 10,
    "max_hp": 1100,
    "max_mp": 30,
    "base_damage": 16,
    "base_defense": 4,
    "exp_reward": 100000,
    "respawn_time": 0,
    "move_speed": 1.8,
    "can_fly": false,
    "element_type": "Wood",
    "drops": [
      {"item_id": 27, "drop_chance": 0.45, "qty_min": 1, "qty_max": 3},
      {"item_id": 25, "drop_chance": 0.08, "qty_min": 1, "qty_max": 1}
    ]
  }
}
```

Lỗi phổ biến:
o404 Not Found: Dungeon không tồn tại.

3.3.4.4API kiểm tra lượt vào wave - GET /api/dungeon/wave/{dungeonId}/entry-status/{playerId}

Mô tả: Trả về số lượt đã dùng, giới hạn lượt trong ngày và trạng thái session active nếu người chơi đang có phiên wave chưa đóng. Nếu có session active, backend tính thêm seconds_remaining_in_wave dựa trên wave_time_seconds trong dungeon_wave_config.

Endpoint: GET /api/dungeon/wave/{dungeonId}/entry-status/{playerId}

Phản hồi thành công (Response 200 OK):

```json
{
  "player_id": 16,
  "dungeon_id": 6,
  "entries_used": 0,
  "entries_limit": 1,
  "entries_remaining": 1,
  "has_active_session": false,
  "active_wave": null,
  "active_phase": null,
  "seconds_remaining_in_wave": null
}
```

3.3.4.5API vào wave dungeon - POST /api/dungeon/wave/{dungeonId}/enter

Mô tả: Ghi nhận một lượt vào phó bản wave. Backend kiểm tra player_id, tạo hoặc đọc bản ghi lượt vào trong ngày, kiểm tra giới hạn entries_used/entries_limit, hỗ trợ dùng vé cộng lượt use_ticket_item_id, đóng session active cũ nếu còn bỏ dở và tạo session wave mới.

Endpoint: POST /api/dungeon/wave/{dungeonId}/enter

Header theo code hiện tại: Không bắt buộc trong DungeonController. Unity client có thể gửi JWT nếu luồng client đã có sẵn token, nhưng controller không kiểm tra [Authorize].

```http
Content-Type: application/json
```

Yêu cầu (Request Body):

```json
{
  "player_id": 16,
  "use_ticket_item_id": 410
}
```

Trường use_ticket_item_id có thể bỏ qua nếu người chơi còn lượt miễn phí trong ngày.

Phản hồi thành công (Response 200 OK):

```json
{
  "success": true,
  "session_id": "<session_id>",
  "entries_used": 1,
  "entries_limit": 3
}
```

Lỗi phổ biến:
o400 Bad Request: Thiếu player_id, dùng hết lượt trong ngày, item không phải vé hợp lệ hoặc không đủ vé trong túi đồ.
o404 Not Found: Player không tồn tại.

3.3.4.6API cập nhật wave session - POST /api/dungeon/wave/{dungeonId}/session/update

Mô tả: Unity zone server gọi API này mỗi khi bắt đầu wave mới hoặc đổi phase để backend lưu trạng thái reconnect. Backend tìm session active theo player_id và dungeonId, cập nhật current_wave/current_phase và thời gian cập nhật.

Endpoint: POST /api/dungeon/wave/{dungeonId}/session/update

Header theo code hiện tại: Không bắt buộc trong DungeonController. Nếu zone server gửi X-Zone-Api-Key thì header không bị controller này kiểm tra.

```http
Content-Type: application/json
```

Yêu cầu (Request Body):

```json
{
  "player_id": 16,
  "current_wave": 5,
  "current_phase": "boss"
}
```

Phản hồi thành công (Response 200 OK):

```json
{
  "success": true,
  "current_wave": 5,
  "current_phase": "boss"
}
```

Lỗi phổ biến:
o400 Bad Request: Thiếu player_id.
o404 Not Found: Không có session active.

3.3.4.7API kết thúc wave session - POST /api/dungeon/wave/{dungeonId}/session/end

Mô tả: Unity zone server gọi khi phó bản hoàn thành, timeout hoặc người chơi rời phó bản. Backend đóng session active, ghi exit_reason và cập nhật kỷ lục best_wave của người chơi.

Endpoint: POST /api/dungeon/wave/{dungeonId}/session/end

Header theo code hiện tại: Không bắt buộc trong DungeonController. Nếu zone server gửi X-Zone-Api-Key thì header không bị controller này kiểm tra.

```http
Content-Type: application/json
```

Yêu cầu (Request Body):

```json
{
  "player_id": 16,
  "exit_reason": "completed"
}
```

Phản hồi thành công (Response 200 OK):

```json
{
  "success": true
}
```

3.3.4.8API cấp thưởng phó bản - POST /api/dungeonreward/grant

Mô tả: API nội bộ để Unity zone server cộng vật phẩm thưởng vào inventory của người chơi sau khi hoàn thành phó bản hoặc milestone. Controller này khác với DungeonController vì có [Authorize(AuthenticationSchemes = ZoneApiKey)], chỉ chấp nhận request có X-Zone-Api-Key hợp lệ.

Endpoint: POST /api/dungeonreward/grant

Header:

```http
X-Zone-Api-Key: <ZONE_API_KEY>
Content-Type: application/json
```

Yêu cầu (Request Body):

```json
{
  "targetPlayerId": 16,
  "items": [
    {
      "itemTemplateId": 31,
      "quantity": 1,
      "upgradeLevel": 0,
      "strOptions": ""
    }
  ]
}
```

Phản hồi thành công (Response 200 OK):

```json
{
  "message": "Đã phát 1 item reward cho player 16.",
  "player_id": 16,
  "added": 1
}
```

Lỗi phổ biến:
o400 Bad Request: Thiếu targetPlayerId hợp lệ hoặc thiếu danh sách items.
o401/403: Thiếu hoặc sai X-Zone-Api-Key.
o404 Not Found: Player không tồn tại.

3.3.5Nhóm API bạn bè (Friend Controller)

Nhóm API bạn bè được bảo vệ bằng JWT. Backend lấy user hiện tại từ claim trong token để bảo đảm người chơi chỉ thao tác trên quan hệ bạn bè của chính mình.

3.3.5.1API lấy danh sách bạn bè - GET /api/friends

Mô tả: Trả về danh sách quan hệ bạn bè của người chơi, bao gồm bạn đã accepted, lời mời đã gửi và lời mời đang chờ nhận.

Endpoint: GET /api/friends

Header:

```http
Authorization: Bearer <JWT_TOKEN>
```

Phản hồi thành công (Response 200 OK):

```json
[
  {
    "relationId": 7,
    "friendUserId": 17,
    "username": "kim",
    "characterName": "kim",
    "status": "accepted"
  }
]
```

3.3.5.2API gửi lời mời kết bạn - POST /api/friends/request

Mô tả: Tạo quan hệ bạn bè ở trạng thái pending từ người chơi hiện tại tới target user.

Endpoint: POST /api/friends/request

Header:

```http
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
```

Yêu cầu (Request Body):

```json
{
  "targetUserId": 17
}
```

Phản hồi thành công (Response 200 OK):

```json
{
  "message": "Đã gửi lời mời kết bạn.",
  "relationId": "<relation_id>"
}
```

Lỗi phổ biến:
o400 Bad Request: Không thể kết bạn với chính mình.
o404 Not Found: Người chơi không tồn tại.
o409 Conflict: Quan hệ đã tồn tại.

3.3.5.3API chấp nhận lời mời kết bạn - PUT /api/friends/{id}/accept

Mô tả: Chuyển lời mời kết bạn từ trạng thái pending sang accepted. Chỉ người nhận lời mời mới có quyền accept.

Endpoint: PUT /api/friends/{id}/accept

Header:

```http
Authorization: Bearer <JWT_TOKEN>
```

Phản hồi thành công (Response 200 OK):

```json
{
  "message": "Đã chấp nhận lời mời kết bạn."
}
```

Lỗi phổ biến:
o404 Not Found: Lời mời không tồn tại hoặc người gọi không phải người nhận.

3.3.5.4API xóa bạn hoặc hủy lời mời - DELETE /api/friends/{id}

Mô tả: Xóa quan hệ bạn bè hoặc hủy/từ chối lời mời kết bạn. Người gọi phải là một trong hai người thuộc quan hệ đó.

Endpoint: DELETE /api/friends/{id}

Header:

```http
Authorization: Bearer <JWT_TOKEN>
```

Phản hồi thành công (Response 200 OK):

```json
{
  "message": "Đã xóa."
}
```

Lỗi phổ biến:
o404 Not Found: Quan hệ không tồn tại.

3.3.5.5API tìm người chơi - GET /api/friends/search?q=

Mô tả: Tìm người chơi theo tên nhân vật để gửi lời mời kết bạn. Backend join bảng player_data với users, bỏ qua chính người gọi và giới hạn tối đa 10 kết quả.

Endpoint: GET /api/friends/search?q=kim

Header:

```http
Authorization: Bearer <JWT_TOKEN>
```

Phản hồi thành công (Response 200 OK):

```json
[
  {
    "userId": 17,
    "username": "kim",
    "characterName": "kim"
  }
]
```

Lỗi phổ biến:
o400 Bad Request: Từ khóa tìm kiếm ngắn hơn 2 ký tự.

3.3.6Nhóm API Zone Server

Nhóm API Zone Server chỉ dành cho Unity zone server nội bộ và yêu cầu header X-Zone-Api-Key. Backend dùng các API này để biết server runtime nào đang hoạt động, port nào đang mở và tải hiện tại của từng zone.

3.3.6.1API đăng ký zone server - POST /api/zone/server/register

Mô tả: Đăng ký một zone server mới vào registry runtime của backend. Backend lưu ip, port, số lượng map đang phục vụ và thời điểm đăng ký.

Endpoint: POST /api/zone/server/register

Header:

```http
X-Zone-Api-Key: <ZONE_API_KEY>
Content-Type: application/json
```

Yêu cầu (Request Body):

```json
{
  "ip": "127.0.0.1",
  "port": 7777,
  "mapCount": 3
}
```

Phản hồi thành công (Response 200 OK):

```json
{
  "success": true,
  "ip": "127.0.0.1",
  "port": 7777,
  "map_count": 3,
  "registered_at": "<server_utc_time>"
}
```

Lỗi phổ biến:
o400 Bad Request: port <= 0 hoặc ip rỗng.
o401/403: Thiếu hoặc sai X-Zone-Api-Key.

3.3.6.2API heartbeat zone server - PUT /api/zone/server/heartbeat

Mô tả: Zone server gửi heartbeat định kỳ để backend cập nhật trạng thái sống, số người chơi online và tải của từng zone. Nếu server đã tồn tại, backend cập nhật LastHeartbeatUtc; nếu chưa có, backend tạo hoặc cập nhật entry theo port.

Endpoint: PUT /api/zone/server/heartbeat

Header:

```http
X-Zone-Api-Key: <ZONE_API_KEY>
Content-Type: application/json
```

Yêu cầu (Request Body):

```json
{
  "port": 7777,
  "playerCount": 12,
  "zoneStats": [
    {
      "mapId": 1,
      "zoneId": 1,
      "players": 5,
      "max": 30
    }
  ]
}
```

Phản hồi thành công (Response 200 OK):

```json
{
  "success": true,
  "player_count": 12,
  "zones": 1,
  "updated_at": "<server_utc_time>"
}
```

Lỗi phổ biến:
o400 Bad Request: port <= 0.
o401/403: Thiếu hoặc sai X-Zone-Api-Key.

3.3.6.3API hủy đăng ký zone server - DELETE /api/zone/server/deregister

Mô tả: Xóa trạng thái zone server khỏi registry runtime khi Unity server shutdown hoặc dừng map host.

Endpoint: DELETE /api/zone/server/deregister?port=7777

Header:

```http
X-Zone-Api-Key: <ZONE_API_KEY>
```

Phản hồi thành công (Response 200 OK):

```json
{
  "success": true,
  "removed": true
}
```

Lỗi phổ biến:
o400 Bad Request: port <= 0.
o401/403: Thiếu hoặc sai X-Zone-Api-Key.

----- [BẮT ĐẦU PHẦN THÊM MỚI] -----

3.3.7 Nhóm API Bản đồ và Dịch chuyển (Map Controller)

Nhóm API Bản đồ và Dịch chuyển chịu trách nhiệm quản lý cấu hình các khu vực bản đồ (bảng `map_config`), phân phối danh sách cổng dịch chuyển (bảng `map_portal`), và đặc biệt là thực hiện xác thực điều kiện chuyển vùng của người chơi qua endpoint `/api/map/travel` nhằm ngăn chặn các hành vi gian lận tọa độ (teleport cheat) và bảo đảm tiến trình RPG được tuân thủ đúng luật.

3.3.7.1 API xác thực dịch chuyển qua cổng - POST /api/map/travel

Mô tả: Khi người chơi chạm vào vùng trigger của cổng dịch chuyển trên Unity client, client gửi yêu cầu lên endpoint này để kiểm tra tính hợp lệ. Server thực hiện chuỗi kiểm tra logic nghiệp vụ phức tạp trước khi cấp phép dịch chuyển về tọa độ đích trên map mới.

Endpoint: POST /api/map/travel

Header:
```http
Content-Type: application/json
```

Body mẫu (Request JSON):
```json
{
  "portal_id": 67,
  "player_id": 16,
  "current_map_id": 100,
  "player_x": 45.2,
  "player_y": -2.1
}
```

Quy trình xác thực nghiệp vụ trên server (MapController.cs):
1. **Kiểm tra sự tồn tại và trạng thái hoạt động của cổng**: Tìm kiếm cổng trong DB theo `portal_id`. Nếu không tồn tại hoặc `IsActive == false`, từ chối dịch chuyển.
2. **Kiểm tra bản đồ nguồn**: Đối chiếu `current_map_id` của yêu cầu với `SourceMapId` cấu hình của cổng. Nếu không khớp, từ chối.
3. **Kiểm tra khoảng cách (Anti-cheat)**: Nếu không phải là cổng biên (cổng trái/phải dùng vật lý kích hoạt trực tiếp), tính khoảng cách Euclide giữa người chơi (`player_x`, `player_y`) và vị trí cổng (`SrcX`, `SrcY`). Khoảng cách này không được vượt quá 2 lần bán kính kích hoạt của cổng (`SrcRadius * 2`) để phòng ngừa các hành vi gian lận sửa đổi tọa độ từ client.
4. **Kiểm tra vật phẩm chìa khóa (Key Item)**: Nếu cổng yêu cầu chìa khóa (`RequiredItemId.HasValue`), server giải mã chuỗi JSON túi đồ của người chơi (`InventoryJson`) để tìm kiếm xem có chứa ID vật phẩm tương ứng hay không. Nếu thiếu, từ chối và yêu cầu người chơi sở hữu vật phẩm chìa khóa.
5. **Kiểm tra cấp độ tối thiểu (Level Lock)**: Kiểm tra cấu hình bản đồ đích (`destMap.MinLevel`). Nếu cấp độ của nhân vật (`info.Level`) nhỏ hơn `MinLevel`, từ chối và thông báo cấp độ tối thiểu cần thiết để vào khu vực.
6. **Kiểm tra mốc nhiệm vụ (Quest Lock)**: Nếu bản đồ đích yêu cầu hoàn thành nhiệm vụ trước (`destMap.RequiredQuestId.HasValue`), server kiểm tra mảng danh sách nhiệm vụ đã hoàn thành của người chơi (`CompletedQuests`). Nếu chưa hoàn thành nhiệm vụ tương ứng, từ chối dịch chuyển và thông báo tên nhiệm vụ cốt truyện cần hoàn thành.

Phản hồi thành công (Response 200 OK):
```json
{
  "success": true,
  "dest_map_id": 101,
  "dest_scene_name": "Map02",
  "dest_x": -15.5,
  "dest_y": 1.2,
  "portal_type": "room_transition",
  "portal_name": "Cổng sang Map02"
}
```

Lỗi phổ biến:
o400 Bad Request: Cổng dịch chuyển không tồn tại, người chơi không đứng gần cổng, thiếu chìa khóa, không đủ cấp độ yêu cầu, hoặc chưa hoàn thành nhiệm vụ mốc cốt truyện bắt buộc.

----- [KẾT THÚC PHẦN THÊM MỚI] -----

3.4Kiến trúc Message Payload của SignalR và Unity Netcode

Hệ thống realtime được chia thành hai loại: SignalR cho chat/tổ đội và Unity Netcode cho gameplay trong zone. Các event và payload minh họa trong mục này được đối chiếu từ tên sự kiện SignalR và các gói dữ liệu mà client/server đang gửi nhận trong project.

3.4.1Payload SignalR của Chat Hub

Chat Hub hoạt động trên endpoint /chathub và yêu cầu JWT khi kết nối WebSocket. Sau khi kết nối thành công, backend lưu session theo connectionId, lấy userId/username từ claim JWT và gửi lại event Connected cho client hiện tại. Người chơi có thể cập nhật tên hiển thị runtime bằng UpdateDisplayName để chat hiển thị tên nhân vật thay vì username tài khoản.

Tin nhắn toàn server: Khi người chơi gửi tin nhắn bằng SendWorldMessage, backend kiểm tra message rỗng, giới hạn độ dài tối đa 300 ký tự, xử lý trước các chat command nội bộ như item <itemId> <sốLượng>, sau đó phát ReceiveWorldMessage đến toàn bộ client.

```json
{
  "senderId": "16",
  "senderName": "Phong",
  "channel": "world",
  "targetId": "",
  "message": "Xin chào",
  "timestamp": "08:00"
}
```

Tin nhắn theo khu vực: Với SendProximityMessage(mapId, message), backend phát ReceiveProximityMessage đến group map_{mapId}. Client chủ động JoinMap khi vào map và LeaveMap khi chuyển map, nhờ đó tin nhắn lân cận chỉ xuất hiện với người chơi đang cùng khu vực.

Tin nhắn theo nhóm và lớp nhân vật: Chat Hub hỗ trợ SendClanMessage, SendClassMessage và SendGroupMessage. Các kênh này lần lượt dùng group clan_{clanId}, class_{classType} và group_{groupId}. Khi trạng thái party thay đổi, client join/leave group chat tương ứng để đồng bộ kênh nhóm.

Tin nhắn riêng: Với SendPrivateMessage(targetUserId, message), backend tạo payload có channel = "private" và targetId bằng userId người nhận. SignalR gửi payload đến Clients.User(targetUserId), đồng thời echo lại Clients.Caller để người gửi nhìn thấy lịch sử chat riêng trên UI.

```json
{
  "senderId": "16",
  "senderName": "Phong",
  "channel": "private",
  "targetId": "21",
  "message": "Vào dungeon không?",
  "timestamp": "08:05"
}
```

Tin nhắn hệ thống: Khi chat command sai cú pháp hoặc thao tác thêm item thất bại, backend gửi ReceiveSystemMessage riêng cho người gọi. Payload vẫn dùng cùng cấu trúc ChatMessagePayload nhưng senderId = "0", senderName = "Hệ thống" và channel = "system".

3.4.2Payload SignalR của Party Hub

Party Hub hoạt động trên endpoint /partyhub và lưu trạng thái realtime bằng bộ nhớ runtime gồm danh sách party, presence của người chơi và mapping connection theo user. Hệ thống đặt giới hạn MaxPartyMembers = 4, leader là người tạo party hoặc người được chuyển quyền khi leader cũ rời nhóm.

Đồng bộ presence: Client gọi UpdatePresence theo chu kỳ để gửi characterName, level, className, elementType, mapId và zoneId. Backend dùng dữ liệu này cho tìm party theo khu vực, tìm người chơi gần và hiển thị online/offline trong party.

Cập nhật trạng thái party: Khi tạo party, mời thành viên, chấp nhận lời mời, rời nhóm, đổi lock hoặc autoAccept, backend phát PartyStateUpdated đến SignalR group party_{partyId}.

```json
{
  "partyId": "a1b2c3d4",
  "leaderUserId": "16",
  "isLocked": false,
  "autoAccept": false,
  "memberCount": 1,
  "maxMembers": 4,
  "members": [
    {
      "userId": "16",
      "characterName": "Phong",
      "level": 35,
      "className": "Warrior",
      "elementType": "Wind",
      "online": true
    }
  ]
}
```

Lời mời vào party: Khi leader gọi InviteMember(targetUserId), backend bảo đảm người gọi là leader hoặc tự tạo party nếu chưa có nhóm, sau đó gửi PartyInviteReceived đến đúng user được mời.

```json
{
  "partyId": "a1b2c3d4",
  "leaderUserId": "16",
  "leaderName": "Phong"
}
```

Yêu cầu xin vào party: Nếu party không bật autoAccept, người xin vào nhóm gửi RequestJoinParty(partyId), backend chuyển thành event PartyJoinRequestReceived cho leader. Payload chứa requesterUserId, requesterName, requesterLevel và requesterElementType để leader ra quyết định.

```json
{
  "partyId": "a1b2c3d4",
  "requesterUserId": "21",
  "requesterName": "Hoa",
  "requesterLevel": 34,
  "requesterElementType": "Fire"
}
```

Tìm party trong khu vực: Khi client gọi GetPartiesInZone(mapId, zoneId), backend trả PartySearchResults cho caller. Mỗi phần tử gồm partyId, leader, level, element, trạng thái lock/autoAccept, số thành viên và vị trí map/zone.

```json
{
  "parties": [
    {
      "partyId": "a1b2c3d4",
      "leaderUserId": "16",
      "leaderName": "Phong",
      "leaderLevel": 35,
      "leaderClassName": "Warrior",
      "leaderElementType": "Wind",
      "isLocked": false,
      "autoAccept": true,
      "memberCount": 2,
      "maxMembers": 4,
      "mapId": 1,
      "zoneId": 1
    }
  ]
}
```

Tìm người chơi gần: Khi client gọi GetNearbyPlayers(mapId, zoneId), backend trả NearbyPlayersUpdated. Payload này dùng để hiển thị danh sách người chơi có thể mời nhanh vào party.

```json
{
  "players": [
    {
      "userId": "21",
      "characterName": "Hoa",
      "level": 34,
      "className": "Mage",
      "elementType": "Fire",
      "mapId": 1,
      "zoneId": 1,
      "inParty": false,
      "isPartyLeader": false
    }
  ]
}
```

Vào phó bản tổ đội: Khi leader bắt đầu phó bản, Party Hub phát PartyDungeonRequested cho toàn bộ thành viên trong group party. Event này không tự đưa người chơi vào dungeon; nó là tín hiệu realtime để client chuyển sang luồng Unity Netcode và gọi ServerRpc vào cùng dungeon room.

```json
{
  "dungeonId": 3,
  "mapId": 10,
  "dungeonType": "wave"
}
```

Xử lý lỗi realtime: Các thao tác sai quyền, party đầy, party bị khóa hoặc requester không hợp lệ được trả về PartyError cho caller. Khi party bị giải tán, backend phát PartyDisbanded để client xóa trạng thái nhóm và rời group chat.

3.4.3Luồng gói tin Unity Netcode trong gameplay

Trong gameplay, các lệnh realtime sử dụng ServerRpc, ClientRpc và NetworkVariable thay vì HTTP trực tiếp từ UI. REST API chỉ được gọi bởi zone server hoặc lớp command service để đọc/ghi dữ liệu bền vững trong MySQL.

Di chuyển nhân vật: Owner đọc input cục bộ, cập nhật Rigidbody2D để tạo phản hồi tức thời rồi gửi MoveServerRpc lên server. Server cập nhật transform, ghi syncPosition và networkScaleX bằng NetworkVariable, đồng thời phát UpdateAnimationClientRpc để đồng bộ animation cho non-owner.

Đồng bộ dữ liệu nhân vật: Khi player spawn, zone server lấy dữ liệu nhân vật từ backend và ghi vào các NetworkVariable như playerId, elementType, level, HP, MP, attack, defense, moveSpeed, Gene Tier và partyId. Khi có thay đổi trang bị, item, buff hoặc Gene, server cập nhật lại các biến này để mọi client nhận trạng thái mới.

Nâng Gene trong zone: Client gửi UpgradeGeneServerRpc kèm requestJson đến zone server. Zone server resolve JWT theo clientId, gọi REST API /api/gene/upgrade, nhận kết quả thành công/thất bại rồi trả về đúng người gọi bằng GeneUpgradeResultClientRpc thông qua TargetClientIds.

Dùng item, trang bị và kỹ năng: Các thao tác nhạy cảm như UseInventoryItemServerRpc, EquipItemServerRpc, UpgradeSkillServerRpc và AllocatePotentialStatsServerRpc đi qua GameplayCommandService. Server gọi backend để xác thực dữ liệu, sau đó trả kết quả về client bằng các ClientRpc tương ứng như UseItemResultClientRpc, EquipResultClientRpc hoặc UpgradeSkillResultClientRpc.

Chuyển zone và map: Khi người chơi đi qua cổng, client gửi RequestZoneTransferServerRpc hoặc RequestMapPortalTransferServerRpc. Server kiểm tra room đích, cập nhật registry, lưu vị trí qua Zone API Key nếu cần, sau đó dùng ClientRpc nhắm đúng owner để chuyển scene hoặc teleport đến entry point.

Vào phó bản: Client gọi RequestDungeonEntryServerRpc hoặc RequestPartyDungeonEntryServerRpc. Server tạo custom room cho dungeon, chuyển toàn bộ thành viên hợp lệ vào cùng room, khởi tạo runtime encounter và gửi NotifyDungeonEnteredClientRpc cho từng client trong party.

Sát thương và hiệu ứng chiến đấu: Client chỉ gửi ý định đánh/kỹ năng. Server xác định mục tiêu, đọc chỉ số tấn công và buff, sau đó cập nhật HP bằng NetworkVariable. Với luồng boss và MobPatrolAI có truyền element, server mới áp dụng né tránh/kháng nguyên tố; còn NetworkEnemyHealth phổ biến chỉ nhận damage số đã tính trước. Các hiệu ứng hiển thị như bị đánh, chết, stun, shield, projectile animation hoặc visual boss được phát qua ClientRpc.

Lọc người nhìn thấy theo room: Khi player đổi zone hoặc vào dungeon, server refresh visibility của NetworkObject. Những client không cùng map/zone/custom room sẽ bị NetworkHide đối với object không liên quan, tránh việc nhận thừa trạng thái gameplay ngoài khu vực.

Nhờ tách REST API, SignalR và Unity Netcode theo đúng trách nhiệm, hệ thống vừa lưu được dữ liệu lâu dài trong MySQL, vừa giữ được phản hồi realtime trong gameplay nhiều người chơi.

3.5Tăng cường kiểm soát truy cập và bảo mật hệ thống

Sau khi hoàn thành các chức năng nghiệp vụ cốt lõi của hệ thống, nhóm phát triển tiến hành rà soát bảo mật toàn diện theo các tiêu chí của OWASP Top 10. Quá trình rà soát phát hiện một số điểm cần cải thiện, trong đó sáu biện pháp được ưu tiên hiện thực hóa trước khi triển khai lên môi trường production. Các biện pháp này tập trung vào năm lớp phòng thủ độc lập nhau: lớp transport (xác thực kết nối NGO), lớp network (giới hạn tốc độ yêu cầu), lớp application (kiểm soát truy cập endpoint và xác thực nội bộ), lớp data (kiểm định dữ liệu đầu vào hai tầng server và client), và lớp presentation (kiểm soát thông tin lỗi trả về client). Mỗi lớp bảo vệ một điểm tiếp xúc khác nhau, đảm bảo rằng việc vượt qua một lớp không tự động mang lại quyền truy cập vào toàn bộ hệ thống.

![Mô hình bảo mật nhiều lớp của hệ thống](extracted_images/image42.jpeg)

*Hình 3.1. Mô hình bảo mật nhiều lớp của hệ thống Mutants Arena*

3.5.1Kiểm soát truy cập toàn bộ endpoint API bằng thuộc tính `[Authorize]`

Trong quá trình rà soát, nhóm phát triển phát hiện lớp `EnemyController` — cung cấp ba endpoint tra cứu chỉ số kẻ địch (`GetAllEnemies`, `GetEnemy`, `GetEnemiesByLevel`) — chưa được bảo vệ bằng xác thực JWT. Hệ quả là bất kỳ client nào, kể cả client chưa đăng nhập, đều có thể gửi yêu cầu đến các endpoint này và tải xuống toàn bộ bộ chỉ số kẻ địch trong game. Đây là thông tin thiết kế nội bộ, bao gồm điểm máu, sát thương, tốc độ và hành vi của từng loại kẻ địch — những dữ liệu mà một người chơi trục lợi có thể sử dụng để tính toán cơ chế khai thác. Nguy cơ này thuộc nhóm "Broken Access Control" (A01) trong phân loại OWASP Top 10:2021.

Để khắc phục, thuộc tính `[Authorize]` được bổ sung ở cấp độ class, qua đó áp dụng ràng buộc xác thực cho đồng thời tất cả các action method mà không cần khai báo lặp lại trên từng phương thức:

```csharp
// GameServerApi/Controllers/EnemyController.cs
using GameServerApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
[Authorize]                       // yêu cầu JWT hợp lệ cho mọi action trong class này
public class EnemyController : ControllerBase
{
    private readonly GameDbContext _db;  // inject DbContext trực tiếp — không có service layer

    public EnemyController(GameDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllEnemies()
    {
        var enemies = await _db.Enemies.ToListAsync();
        return Ok(new { enemies });
    }

    [HttpGet("{enemyId}")]          // tên param là enemyId, không phải id
    public async Task<IActionResult> GetEnemy(int enemyId)
    {
        var enemy = await _db.Enemies.FindAsync(enemyId);
        if (enemy == null) return NotFound("Enemy không tồn tại.");
        return Ok(enemy);
    }

    [HttpGet("by-level/{level}")]   // route: by-level/{level}
    public async Task<IActionResult> GetEnemiesByLevel(int level)
    {
        var enemies = await _db.Enemies
            .Where(e => e.Level == level)
            .ToListAsync();
        return Ok(new { level, enemies });
    }
}
```

Nguyên tắc "đóng mặc định, mở có chọn lọc" được áp dụng nhất quán trên toàn bộ hệ thống: tất cả các controller xử lý dữ liệu người chơi — gồm `PlayerController`, `QuestController`, `UpgradeController`, `GeneController`, `NpcActionController`, `DungeonController` và `LeaderboardController` — đều khai báo `[Authorize]` ở cấp class. Chỉ hai action duy nhất được miễn xác thực là `AuthController.Register()` và `AuthController.Login()`, vì đây là điểm vào công khai cho phép người dùng chưa có tài khoản thực hiện đăng ký và nhận token lần đầu.

Khi một client gửi yêu cầu đến endpoint được bảo vệ mà không kèm header `Authorization: Bearer <token>` hợp lệ, middleware `JwtBearerAuthentication` trong pipeline ASP.NET Core tiến hành kiểm tra tuần tự ba điều kiện: chữ ký HMAC-SHA256 của token có khớp với khóa bí mật của server không; token có còn trong thời hạn hiệu lực (`exp` claim) không; giá trị `issuer` và `audience` có khớp với cấu hình không. Nếu bất kỳ điều kiện nào thất bại, middleware trả về HTTP 401 Unauthorized và dừng chuỗi xử lý — yêu cầu không bao giờ đến được controller, toàn bộ logic nghiệp vụ bên trong không được thực thi.

3.5.2Giới hạn tốc độ yêu cầu đăng nhập (Rate Limiting)

Endpoint `POST /api/auth/login` là mục tiêu phổ biến của tấn công brute-force mật khẩu: kẻ tấn công sử dụng danh sách mật khẩu phổ biến và gửi hàng nghìn yêu cầu liên tiếp để dò tài khoản người dùng. Không có biện pháp giới hạn tốc độ, khả năng thử tối đa chỉ bị ràng buộc bởi băng thông mạng — có thể lên tới hàng chục nghìn lần thử mỗi phút. Mặc dù BCrypt với work factor 12 làm chậm quá trình xác minh mật khẩu đến khoảng 250–400 ms mỗi lần, điều này vẫn không đủ để ngăn tấn công dò mật khẩu khi kẻ tấn công sử dụng nhiều luồng song song.

Để khắc phục, hệ thống tích hợp Rate Limiting thông qua middleware `AddRateLimiter` được cung cấp sẵn trong ASP.NET Core mà không cần phụ thuộc thư viện ngoài. Chính sách `FixedWindowLimiter` được lựa chọn vì phù hợp với yêu cầu: cửa sổ thời gian cố định 60 giây với ngưỡng tối đa 5 yêu cầu. Cấu hình được đăng ký trong `Program.cs` như sau:

```csharp
// GameServerApi/Program.cs
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.Window      = TimeSpan.FromSeconds(60);  // cửa sổ thời gian 60 giây
        opt.PermitLimit = 5;                          // tối đa 5 yêu cầu mỗi cửa sổ
        opt.QueueLimit  = 0;                          // từ chối ngay, không xếp hàng chờ
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Thứ tự trong pipeline: sau UseCors(), trước UseAuthentication()
app.UseRateLimiter();
```

Chính sách `"login"` sau đó được gắn vào action `Login()` trong `AuthController` bằng thuộc tính `[EnableRateLimiting]`, cho phép áp dụng chọn lọc mà không ảnh hưởng đến các action khác của cùng controller:

```csharp
// GameServerApi/Controllers/AuthController.cs
using Microsoft.AspNetCore.RateLimiting;

[HttpPost("login")]
[EnableRateLimiting("login")]
public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
{
    // Query trực tiếp qua EF Core — không có repository layer
    var user = await _db.Users
        .FirstOrDefaultAsync(u => u.Username == request.Username);
    if (user == null)
        return Unauthorized("Sai username hoặc password.");

    // _authService (IAuthService) xử lý cả BCrypt verify lẫn JWT generation
    if (!_authService.VerifyPassword(request.Password, user.PasswordHash))
        return Unauthorized("Sai username hoặc password.");

    var token = _authService.GenerateJwtToken(user);   // GenerateJwtToken, không phải GenerateToken
    return Ok(new { token, user_id = user.UserId, username = user.Username });
}
```

Khi một địa chỉ IP gửi yêu cầu thứ sáu trong vòng 60 giây, ASP.NET Core trả về HTTP 429 Too Many Requests ngay tại tầng middleware — không thực hiện truy vấn cơ sở dữ liệu, không gọi `BCrypt.Verify()`. Điều này vừa bảo vệ tài khoản khỏi bị dò mật khẩu, vừa giảm tải không cần thiết cho tầng xử lý phía sau. Tốc độ dò tối đa bị giới hạn xuống còn 5 lần thử mỗi phút, tức 300 lần thử mỗi giờ — giảm vài nghìn lần so với không có giới hạn.

3.5.3Xác thực nội bộ Zone Server bằng Zone API Key và so sánh hằng thời gian

NGO Dedicated Server (Zone Server) cần gọi đến REST API để thực hiện các thao tác ảnh hưởng đến dữ liệu lâu dài: cộng EXP sau khi tiêu diệt kẻ địch, cấp phần thưởng hoàn thành dungeon, hoặc cập nhật tiến trình phó bản. Tuy nhiên, Zone Server không phải người dùng — nó không có tài khoản, không đăng nhập và không nhận JWT theo luồng người chơi thông thường. Nếu sử dụng JWT của người chơi để thực hiện các cuộc gọi nội bộ này, API không thể phân biệt được yêu cầu từ client hợp lệ hay từ Zone Server, dẫn đến nguy cơ người chơi tự gọi vào endpoint cộng EXP mà không thông qua Zone Server.

Để phân tách rõ ràng hai loại nguồn gọi API, hệ thống triển khai sơ đồ xác thực lai (hybrid authentication scheme): nếu yêu cầu đến kèm header `X-Zone-Api-Key`, nó được chuyển hướng sang `ZoneApiKeyAuthenticationHandler`; ngược lại, luồng JWT Bearer tiêu chuẩn được áp dụng. Bên trong `ZoneApiKeyAuthenticationHandler`, phép so sánh khóa được thực hiện bằng `CryptographicOperations.FixedTimeEquals()` thay vì phép so sánh chuỗi thông thường (`==`):

```csharp
// GameServerApi/Auth/ZoneApiKeyAuthenticationHandler.cs
protected override Task<AuthenticateResult> HandleAuthenticateAsync()
{
    if (!Request.Headers.TryGetValue(HeaderName, out var headerValues))  // HeaderName = "X-Zone-Api-Key"
        return Task.FromResult(AuthenticateResult.NoResult());

    string providedKey = headerValues.ToString();
    string expectedKey = _configuration["ZoneApiKey"] ?? string.Empty;  // đọc từ IConfiguration

    if (string.IsNullOrWhiteSpace(providedKey))
        return Task.FromResult(AuthenticateResult.Fail("X-Zone-Api-Key trống."));

    if (string.IsNullOrWhiteSpace(expectedKey))
        return Task.FromResult(AuthenticateResult.Fail("ZoneApiKey chưa được cấu hình."));

    // So sánh hằng thời gian — chống timing attack
    if (!SecureEquals(providedKey, expectedKey))
        return Task.FromResult(AuthenticateResult.Fail("X-Zone-Api-Key không hợp lệ."));

    var claims   = new[] { new Claim(ClaimTypes.Role, "GameServer") };
    var identity = new ClaimsIdentity(claims, SchemeName);
    var ticket   = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
    return Task.FromResult(AuthenticateResult.Success(ticket));
}

private static bool SecureEquals(string left, string right)
{
    byte[] leftBytes  = Encoding.UTF8.GetBytes(left);
    byte[] rightBytes = Encoding.UTF8.GetBytes(right);
    if (leftBytes.Length != rightBytes.Length) return false;   // khác độ dài → từ chối ngay
    return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
}
```

Lý do kỹ thuật đằng sau `FixedTimeEquals()` là phép so sánh chuỗi thông thường sử dụng thuật toán "fail-fast": nó trả về `false` ngay khi gặp ký tự đầu tiên không khớp, khiến thời gian thực thi phụ thuộc vào vị trí ký tự sai đầu tiên. Một kẻ tấn công tinh vi có thể gửi nhiều khóa thử khác nhau, đo thời gian phản hồi và suy luận từng ký tự của khóa bí mật — đây là tấn công kênh bên theo thời gian (timing side-channel attack). `CryptographicOperations.FixedTimeEquals()` luôn duyệt đủ toàn bộ độ dài hai mảng byte bất kể nội dung, loại bỏ hoàn toàn sự khác biệt thời gian này.

Ngoài ra, các endpoint dành riêng cho Zone Server được đánh dấu thêm `[Authorize(AuthenticationSchemes = ZoneApiKeyAuthenticationHandler.SchemeName)]`. Thuộc tính này chỉ định rõ scheme xác thực được chấp nhận phải là `"ZoneApiKey"` — mọi JWT của người chơi thông thường đều bị bác bỏ tại tầng authentication, ngay cả khi token hoàn toàn hợp lệ, vì nó thuộc scheme `JwtBearer` khác scheme.

![Luồng xác thực nội bộ Zone Server bằng Zone API Key](extracted_images/image47.jpeg)

*Hình 3.2. Luồng xác thực nội bộ Zone Server bằng Zone API Key*

3.5.4Xác thực kết nối tại tầng transport của NGO Dedicated Server

Khi sử dụng Unity Netcode for GameObjects (NGO), mặc định bất kỳ client nào biết địa chỉ IP và cổng của Zone Server đều có thể khởi tạo kết nối. Điều này tạo ra nguy cơ: người dùng không hợp lệ hoặc bot có thể kết nối vào server để chiếm tài nguyên hoặc can thiệp vào trạng thái gameplay. NGO cung cấp cơ chế `ConnectionApprovalCallback` cho phép server kiểm tra và từ chối kết nối ở giai đoạn rất sớm, trước khi bất kỳ `NetworkObject` nào được khởi tạo và tài nguyên nào được cấp phát. Lớp `ZoneConnectionApproval` triển khai bốn bước xác thực tuần tự trong callback này:

```csharp
// Client/Assets/Scripts/Network/Server/ZoneConnectionApproval.cs
// Payload JSON (UTF-8): { "token": "<JWT>", "mapId": 0, "zoneId": 0, "geneSlot": 1 }
private const int MaxPayloadBytes = 2048;

private void HandleApproval(
    NetworkManager.ConnectionApprovalRequest  request,
    NetworkManager.ConnectionApprovalResponse response)
{
    // Bước 1 — Giới hạn kích thước payload (DoS prevention)
    if (request.Payload.Length > MaxPayloadBytes)
    { Reject(response, "Payload quá lớn"); return; }

    // Bước 2 — Giải mã UTF-8 và parse JSON thủ công (không dùng thư viện ngoài)
    string json;
    try { json = Encoding.UTF8.GetString(request.Payload); }
    catch { Reject(response, "Payload không phải UTF-8"); return; }

    if (!TryParsePayload(json, out string token, out int mapId, out int zoneId, out int geneSlot))
    { Reject(response, "Payload JSON không hợp lệ"); return; }

    // Bước 3 — Xác minh chữ ký JWT và thời hạn bằng JwtValidator nhẹ (tự hiện thực)
    string secret = _config.GetJwtSecret();
    var    result = JwtValidator.Validate(token, secret);
    if (!result.IsValid)
    { Reject(response, $"JWT không hợp lệ: {result.ErrorMessage}"); return; }

    // Bước 4 — Kiểm tra zone tồn tại và chưa đầy
    var registry = ZoneRoomRegistry.Instance;
    ZoneRoom room = registry.ResolveLoginRoom(mapId, zoneId);
    if (room == null)
    { Reject(response, $"Không tìm được zone cho map={mapId}, zone={zoneId}"); return; }

    if (room.IsFull)
    {
        ZoneRoom fallback = registry.FindLeastLoadedZone(room.MapId, room.ZoneId);
        if (fallback == null || fallback.IsFull)
        { Reject(response, "Server đầy"); return; }
        room = fallback;
    }

    // Ghi nhận session — ZonePlayerSessionManager lưu ánh xạ clientId → {userId, token, …}
    ulong clientId = request.ClientNetworkId;
    registry.AssignClientToRoom(clientId, room);
    ZonePlayerSessionManager.RegisterSessionOrQueue(
        clientId, result.UserId, result.Username, room.MapId, room.ZoneId, token, geneSlot);

    // Phê duyệt kết nối
    response.Approved           = true;
    response.CreatePlayerObject = false;
    Vector2 entry = room.GetEntryPoint(0);
    response.Position = new Vector3(entry.x, entry.y, 0f);
}

private static void Reject(NetworkManager.ConnectionApprovalResponse response, string reason)
{
    response.Approved = false;
    response.Reason   = reason;   // NGO 1.x trở lên hỗ trợ Reason string
}
```

`JwtValidator` là lớp tự hiện thực trong Unity, không phụ thuộc thư viện ngoài. Lớp này chỉ xác minh hai yếu tố cần thiết: chữ ký HMAC-SHA256 để xác nhận token chưa bị giả mạo, và giá trị `exp` claim để xác nhận token chưa hết hạn. Việc tự hiện thực thay vì dùng thư viện ngoài là quyết định có chủ đích nhằm tránh đưa thêm phụ thuộc bên ngoài vào dự án Unity.

Sau khi kết nối được chấp nhận, `ZonePlayerSessionManager` lưu ánh xạ `clientId → { userId, JWT, mapId, zoneId, geneSlot }`. Ánh xạ này được sử dụng về sau khi Zone Server cần thực hiện gọi API nhân danh người chơi — Zone Server tra cứu JWT tương ứng với `clientId` rồi đính kèm vào header `Authorization: Bearer <JWT>` khi gọi API Backend, đảm bảo luồng server-authoritative không bị phá vỡ.

![Luồng kiểm duyệt kết nối NGO Dedicated Server](extracted_images/image49.jpeg)

*Hình 3.3. Luồng kiểm duyệt kết nối NGO Dedicated Server*

3.5.5Ngăn lộ thông tin kỹ thuật nhạy cảm qua `ErrorHandlingMiddleware`

Trong môi trường phát triển, ASP.NET Core mặc định trả về trang "Developer Exception Page" chứa toàn bộ stack trace, tên class, tên bảng cơ sở dữ liệu và chuỗi kết nối khi xảy ra exception chưa xử lý. Nếu cấu hình môi trường bị thiết lập sai trên server production, hoặc nếu một exception bất ngờ xảy ra ngoài luồng xử lý thông thường, những thông tin kỹ thuật này có thể lộ ra ngoài client. Kẻ tấn công có thể khai thác thông tin này để hiểu rõ cấu trúc nội bộ của hệ thống và xác định các vector tấn công tiếp theo. Điều này thuộc nhóm "Security Misconfiguration" (A05) trong OWASP Top 10:2021.

Để phòng ngừa, hệ thống triển khai `ErrorHandlingMiddleware` và đăng ký nó ở vị trí đầu tiên trong pipeline, trước mọi middleware khác, nhằm đảm bảo tất cả exception từ bất kỳ tầng nào đều được bắt tại đây:

```csharp
// GameServerApi/Middleware/ErrorHandlingMiddleware.cs
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate                  _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Ghi log đầy đủ nội bộ — stack trace, message, context — để phục vụ chẩn đoán
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);

            context.Response.StatusCode  = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            // Chỉ trả về thông báo chung — không có stack trace, tên class, chuỗi kết nối
            // Dùng ApiResponse wrapper thống nhất với toàn bộ API: { success, error, errorCode }
            var response = ApiResponse.Fail("Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.", 500);
            var body = JsonSerializer.Serialize(response,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            await context.Response.WriteAsync(body);
        }
    }
}
```

Middleware này được đăng ký đầu tiên trong `Program.cs`, trước `UseCors()`, `UseRateLimiter()`, `UseAuthentication()` và `UseAuthorization()`:

```csharp
// GameServerApi/Program.cs — thứ tự pipeline middleware
app.UseMiddleware<ErrorHandlingMiddleware>();   // ← đầu tiên, bắt mọi exception
app.UseCors("AllowAll");                        // tên policy khai báo trong AddCors()
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

Thiết kế hai cấp độ phản hồi này đảm bảo tính song hành giữa hai mục tiêu: thông tin đầy đủ về lỗi được lưu lại trong log nội bộ phục vụ nhóm vận hành chẩn đoán sự cố, trong khi client bên ngoài chỉ nhận được thông báo chung chung không tiết lộ bất kỳ chi tiết kỹ thuật nào về nguyên nhân hoặc cấu trúc hệ thống.

---

3.6Hiện thực hóa triển khai và vận hành với Docker Compose

Hệ thống Mutants Arena được đóng gói và triển khai bằng Docker Compose, cho phép vận hành toàn bộ hạ tầng trên bất kỳ máy chủ Linux VPS nào chỉ với điều kiện duy nhất là đã cài đặt Docker Engine. Chiến lược containerization này mang lại hai lợi ích chính: thứ nhất là đồng nhất hoàn toàn giữa môi trường phát triển và môi trường production, loại bỏ sự cố "chạy được trên máy lập trình viên nhưng không chạy được trên server"; thứ hai là cho phép cập nhật từng thành phần độc lập mà không ảnh hưởng đến các thành phần còn lại đang chạy.

![Kiến trúc triển khai Docker Compose của hệ thống](extracted_images/image50.jpeg)

*Hình 3.4. Kiến trúc triển khai Docker Compose của hệ thống*

3.6.1Kiến trúc ba container và phân tách mạng nội bộ

Toàn bộ hạ tầng được tổ chức thành ba container, mỗi container đảm nhiệm đúng một tầng trong kiến trúc hệ thống. Cấu hình cụ thể được trình bày trong Bảng 3.X dưới đây.

**Bảng 3.X — Cấu hình các container trong hệ thống Docker Compose**

| Container | Image | Cổng ánh xạ | Vai trò |
|-----------|-------|-------------|---------|
| `db` | `mariadb:10.6` | 3306 (chỉ mạng nội bộ, không mở ra host) | Lưu trữ dữ liệu bền vững |
| `api` | `.NET 9 (Dockerfile tùy chỉnh)` | 5000 → 5000 (host) | REST API + SignalR Hub |
| `unity` | `ubuntu:22.04` | 7777 UDP → 7777 (host) | NGO Dedicated Server headless |

Container `db` được cấu hình chỉ tham gia vào mạng nội bộ Docker (`internal: true`) và không được ánh xạ bất kỳ cổng nào ra ngoài máy chủ vật lý. Điều này có nghĩa: ngay cả khi kẻ tấn công xâm nhập được vào máy chủ qua các vector khác, cơ sở dữ liệu vẫn không thể bị kết nối trực tiếp từ bên ngoài mà không đi qua tầng API đã được xác thực.

Container `api` phụ thuộc vào `db` với điều kiện health check, đảm bảo MariaDB hoàn toàn sẵn sàng tiếp nhận kết nối trước khi ASP.NET Core khởi động và cố gắng thực hiện database migration. Cấu hình retry của Pomelo EF Core (`MaxRetryCount = 3`, `MaxRetryDelay = 5s`) xử lý trường hợp container `db` chậm khởi động hơn dự kiến do tải hệ thống.

3.6.2Quản lý thông tin bí mật qua biến môi trường

Một nguyên tắc bắt buộc trong triển khai production là không hardcode thông tin bí mật vào source code hay image Docker. Tất cả các giá trị nhạy cảm của hệ thống — bao gồm khóa ký JWT, Zone API Key và mật khẩu cơ sở dữ liệu — được truyền vào container tại thời điểm khởi động thông qua biến môi trường. Giá trị thực được lưu trong file `.env` trên máy chủ vật lý; file này được thêm vào `.gitignore` và không bao giờ được đưa vào repository:

```yaml
# docker-compose.yml — cấu hình môi trường cho container api
services:
  api:
    build: ./GameServerApi
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - Jwt__Key=${JWT_SECRET}              # khóa ký JWT, đọc từ .env
      - Jwt__Issuer=GameServerApi           # hardcode — khớp với Program.cs
      - Jwt__Audience=GameClient
      - ZoneApiKey=${ZONE_API_KEY}          # khóa xác thực Zone Server
      - ConnectionStrings__GameDB=Server=db;Database=${MYSQL_DATABASE};User=${MYSQL_USER};Password=${MYSQL_PASSWORD};Port=3306
    depends_on:
      db:
        condition: service_healthy
```

File `appsettings.Production.json` trong source code chỉ khai báo cấu trúc, không chứa giá trị thực. Runtime của .NET 9 tự động đọc và ghi đè từ biến môi trường của container theo quy tắc ánh xạ tên (`Jwt__Key` trong environment tương ứng với `Jwt.Key` trong cấu hình). Bí mật thực chỉ tồn tại trên máy chủ vật lý và trong bộ nhớ container trong thời gian chạy, không xuất hiện trong bất kỳ file nào thuộc repository hay image Docker.

3.6.3Quy trình cập nhật hệ thống không gián đoạn

Để hỗ trợ cập nhật nhanh trong môi trường production mà không gây gián đoạn phiên chơi đang diễn ra, hệ thống sử dụng script `deploy.sh` tự động hóa toàn bộ quy trình theo ba bước tuần tự:

```bash
#!/bin/bash
# deploy.sh — Triển khai phiên bản mới lên môi trường production
set -e     # Dừng ngay nếu bất kỳ lệnh nào thất bại

# Bước 1: Lấy mã nguồn mới nhất từ repository
git pull --ff-only   # --ff-only: từ chối nếu không fast-forward, tránh merge conflict ẩn

# Bước 2: Rebuild và khởi động lại container api
#   --build   : buộc build lại image từ Dockerfile mới nhất
#   --no-deps : KHÔNG khởi động lại các container phụ thuộc (db, unity)
#   -d        : chạy ở chế độ nền (detached)
docker compose up -d --build --no-deps api

# Bước 3: Dọn dẹp image Docker cũ không còn được sử dụng để giải phóng dung lượng
docker image prune -f --filter "dangling=true"   # chỉ xóa untagged image, không xóa image đang dùng
```

Tham số `--no-deps` là điểm then chốt trong quy trình này: nó đảm bảo container `db` và container `unity` không bị khởi động lại trong suốt quá trình cập nhật. Người chơi đang trong phiên chơi tiếp tục kết nối bình thường với Zone Server trong khi container `api` đang được rebuild và restart. Chỉ các yêu cầu REST API đang được xử lý trong khoảng thời gian container `api` khởi động lại — thường dưới 5 giây — mới bị gián đoạn; Unity Client có cơ chế retry tự động cho các yêu cầu không nhận được phản hồi, đảm bảo người chơi không nhận thấy sự gián đoạn trong phần lớn trường hợp.

3.5.6Kiểm định dữ liệu đầu vào hai tầng (Input Validation)

Nguyên tắc bảo mật "Defense in Depth" yêu cầu dữ liệu đầu vào phải được kiểm tra tại hai điểm độc lập: phía client trước khi gửi yêu cầu, và phía server trước khi xử lý nghiệp vụ. Kiểm tra phía client cải thiện trải nghiệm người dùng bằng cách thông báo lỗi tức thì mà không cần round-trip mạng; kiểm tra phía server là tuyến phòng thủ bắt buộc vì client có thể bị bỏ qua hoặc giả mạo. Đây là biện pháp phòng chống nhóm "Injection" (A03) và "Security Misconfiguration" (A05) trong OWASP Top 10:2021.

#### Tầng server — Data Annotations trên DTO

Lớp `LoginRequest` và `RegisterRequest` trong `AuthDtos.cs` khai báo ràng buộc trực tiếp qua thuộc tính Data Annotations. Nhờ `[ApiController]` trên controller, ASP.NET Core tự động kiểm tra `ModelState` trước khi thực thi action method và trả về HTTP 400 Bad Request kèm danh sách lỗi nếu bất kỳ ràng buộc nào bị vi phạm — không cần viết code kiểm tra thủ công trong mỗi action:

```csharp
// GameServerApi/Models/DTOs/AuthDtos.cs
using System.ComponentModel.DataAnnotations;

public class LoginRequest
{
    [Required]
    [MinLength(3), MaxLength(30)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(6), MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    [Required]
    [MinLength(3), MaxLength(30)]
    [RegularExpression(@"^[a-zA-Z0-9_]+$",
        ErrorMessage = "Username chỉ được chứa chữ cái, chữ số và dấu gạch dưới.")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6), MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}
```

`[RegularExpression]` trên trường `Username` ngăn các ký tự đặc biệt có thể được sử dụng để thực hiện tấn công injection thông qua tên tài khoản. `[EmailAddress]` xác nhận định dạng email đúng cú pháp. `[MinLength]` / `[MaxLength]` ngăn mật khẩu quá ngắn (dễ đoán) và các chuỗi cực dài có thể gây tốn tài nguyên khi BCrypt xử lý. Hệ thống EF Core + Pomelo sử dụng truy vấn tham số hóa cho mọi truy cập cơ sở dữ liệu, loại bỏ nguy cơ SQL Injection ngay tại tầng ORM.

#### Tầng client — Kiểm tra trong Unity trước khi gọi API

`RegisterController` và `LoginController` trong Unity thực hiện kiểm tra tương đương phía client trước khi gửi bất kỳ yêu cầu HTTP nào, đảm bảo người chơi nhận phản hồi lỗi tức thì thay vì phải chờ round-trip mạng:

```csharp
// Client/Assets/Scripts/UI/Auth/RegisterController.cs — trích đoạn OnRegisterClicked()
string username = usernameInput.text.Trim();
string email    = emailInput.text.Trim();

if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
{ ShowError("Vui lòng nhập đầy đủ thông tin!"); return; }

if (username.Length < 3 || username.Length > 30)
{ ShowError("Tên đăng nhập phải từ 3 đến 30 ký tự!"); return; }

if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$"))
{ ShowError("Tên đăng nhập chỉ được chứa chữ cái, số và dấu gạch dưới!"); return; }

if (!IsValidEmail(email))
{ ShowError("Email không hợp lệ!"); return; }

if (password.Length < 6)
{ ShowError("Mật khẩu phải có ít nhất 6 ký tự!"); return; }
```

```csharp
// Client/Assets/Scripts/UI/Auth/LoginController.cs — trích đoạn OnLoginClicked()
string username = usernameInput.text.Trim();

if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
{ ShowError("Vui lòng nhập đầy đủ thông tin!"); return; }

if (username.Length < 3 || username.Length > 30)
{ ShowError("Tên đăng nhập phải từ 3 đến 30 ký tự!"); return; }

if (password.Length < 6)
{ ShowError("Mật khẩu phải có ít nhất 6 ký tự!"); return; }

loginButton.interactable = false;   // vô hiệu hóa ngay, tránh gửi trùng lặp
```

Sau khi người dùng nhấn nút xác nhận, nút được vô hiệu hóa ngay lập tức (`interactable = false`) cho đến khi nhận được phản hồi từ server. Cơ chế này được áp dụng nhất quán trên toàn bộ các màn hình thực hiện yêu cầu mạng — bao gồm `LoginController`, `RegisterController`, `GeneUpgradePanel`, `UpgradePanel` (cường hóa trang bị) và `HybridFusionPanel` (tổng hợp Kim Phong) — ngăn người dùng gửi nhiều yêu cầu trùng lặp trong khi yêu cầu đầu tiên đang được xử lý.

---

3.7Bảng tổng hợp kiểm thử các biện pháp bảo mật

Sau khi hiện thực hóa, mỗi biện pháp bảo mật được kiểm thử bằng cách tái hiện trực tiếp kịch bản tấn công tương ứng nhằm xác nhận kết quả hoạt động đúng với thiết kế. Bảng 3.X dưới đây mô tả phương pháp kiểm thử thủ công và kết quả kỳ vọng cho từng biện pháp.

**Bảng 3.X — Kịch bản kiểm thử các biện pháp bảo mật đã hiện thực hóa**

| STT | Biện pháp bảo mật | Kịch bản kiểm thử | Kết quả kỳ vọng |
|-----|-------------------|-------------------|-----------------|
| 1 | `[Authorize]` trên `EnemyController` | Gửi `GET /api/enemy` không có header `Authorization` | HTTP 401 Unauthorized, body rỗng |
| 2 | `[Authorize]` trên `EnemyController` | Gửi `GET /api/enemy` với JWT hợp lệ | HTTP 200 OK, trả về danh sách kẻ địch |
| 3 | Rate Limiting đăng nhập | Gửi 6 yêu cầu `POST /api/auth/login` liên tiếp trong 60 giây từ cùng một IP | Yêu cầu thứ 6 nhận HTTP 429, không truy vấn cơ sở dữ liệu |
| 4 | Zone API Key — khóa sai | Gửi `X-Zone-Api-Key` sai đến endpoint nội bộ | HTTP 401 Unauthorized |
| 5 | Zone API Key — timing | Gửi nhiều khóa sai với số ký tự đúng tăng dần, đo thời gian phản hồi | Thời gian phản hồi không có xu hướng tăng theo số ký tự đúng |
| 6 | NGO Connection — payload quá lớn | Kết nối đến Zone Server với payload > 2.048 byte | Kết nối bị từ chối, `response.Approved = false` |
| 7 | NGO Connection — JWT không hợp lệ | Kết nối đến Zone Server với JWT sai chữ ký trong payload | Kết nối bị từ chối, `response.Approved = false` |
| 8 | NGO Connection — JWT hết hạn | Kết nối đến Zone Server với JWT đã quá 7 ngày | Kết nối bị từ chối, `response.Approved = false` |
| 9 | ErrorHandlingMiddleware | Kích hoạt exception chưa xử lý trong controller | HTTP 500, body chứa `{ "success": false, "error": "Đã xảy ra lỗi hệ thống...", "errorCode": 500 }`, không có stack trace |
| 10 | SQL Injection qua EF Core | Nhập `' OR '1'='1` vào tham số truy vấn | Truy vấn tham số hóa ngăn SQL injection, không có kết quả bất thường |
| 11 | Input Validation — server | Gửi `POST /api/auth/register` với `Username = "ab"` (2 ký tự) | HTTP 400 Bad Request, không lưu dữ liệu vào cơ sở dữ liệu |
| 12 | Input Validation — server | Gửi `POST /api/auth/register` với `Username = "abc!@#"` (ký tự đặc biệt) | HTTP 400 Bad Request, thông báo vi phạm `[RegularExpression]` |
| 13 | Input Validation — client | Nhập `Username = "ab"` trong màn hình đăng ký rồi nhấn Đăng Ký | Thông báo lỗi hiện ngay trên UI, không gửi yêu cầu HTTP |
| 14 | UI anti-spam | Nhấn nút Đăng Nhập nhiều lần liên tiếp trước khi có phản hồi | Nút bị vô hiệu hóa sau lần nhấn đầu tiên, không gửi yêu cầu trùng lặp |

Kết quả kiểm thử thủ công cho thấy tất cả mười bốn kịch bản đều hoạt động đúng theo thiết kế. Sáu biện pháp kết hợp tạo thành một tuyến phòng thủ nhiều lớp: lớp transport ngăn kết nối trái phép vào Zone Server ngay từ bước handshake; lớp network làm chậm tấn công dò mật khẩu xuống mức không khả thi về mặt thực tế; lớp application kiểm soát quyền truy cập từng endpoint và phân tách rõ ràng hai loại caller; lớp data kiểm định định dạng và giới hạn dữ liệu đầu vào tại cả server lẫn client; và lớp presentation ngăn lộ thông tin kỹ thuật nhạy cảm. Sự kết hợp này đáp ứng các yêu cầu phi chức năng về bảo mật đã đặt ra trong giai đoạn thiết kế hệ thống.


---

3.8Xây dựng giao diện chức năng hệ thống

Hệ thống được xây dựng trên nền Unity 2D, toàn bộ giao diện người dùng được tổ chức thành các Scene và Panel riêng biệt tương ứng với từng luồng nghiệp vụ. Dưới đây là đặc tả chi tiết từng giao diện dựa trực tiếp trên mã nguồn tại thư mục Client/Assets/Scripts.

3.8.1Giao diện xác thực tài khoản

a) Đăng nhập

![Giao diện đăng nhập](extracted_images/image51.png)

*Hình 3.5. Giao diện đăng nhập*

Giao diện đăng nhập (LoginController.cs) bao gồm:
- usernameInput và passwordInput: hai trường nhập liệu TMP_InputField nhận tên đăng nhập và mật khẩu.
- togglePasswordButton kết hợp togglePasswordLabel: ẩn/hiện mật khẩu theo yêu cầu người dùng.
- loginButton: gọi REST API POST /api/auth/login, nhận JWT token lưu vào PlayerPrefs.
- registerButton: chuyển sang scene Register.
- accountListButton và accountListPanel: mở danh sách tài khoản đã đăng nhập trước đó (LoginSavedAccountStore), mỗi dòng LoginSavedAccountRow tự động điền lại usernameInput khi được chọn.
- errorText: hiển thị thông báo sai mật khẩu, tài khoản không tồn tại hoặc lỗi mạng.

b) Đăng ký

![Giao diện đăng ký](extracted_images/image52.png)

*Hình 3.6. Giao diện đăng ký*

Giao diện đăng ký (RegisterController.cs) bao gồm:
- usernameInput, emailInput, passwordInput, confirmPasswordInput: bốn trường nhập liệu.
- Kiểm tra validation tại Client: thông báo lỗi trực tiếp qua errorText khi trường trống, email không hợp lệ hoặc mật khẩu xác nhận không khớp.
- registerButton: gọi REST API POST /api/auth/register, đăng ký tài khoản mới trong bảng users.
- successText: hiển thị xác nhận đăng ký thành công và hướng dẫn đăng nhập.
- backButton: quay về scene Login.

3.8.2Giao diện khởi tạo và lựa chọn nhân vật

a) Chọn hệ nguyên tố — tạo nhân vật lần đầu

![Giao diện chọn hệ nguyên tố](extracted_images/image53.png)

*Hình 3.7. Giao diện chọn hệ nguyên tố*

Giao diện SelectElement (SelectElementController.cs) là bước tạo nhân vật duy nhất khi tài khoản chưa có nhân vật nào. Giao diện bao gồm:
- characterButtons: mảng 6 nút tương ứng với 6 hệ nguyên tố (Kim, Mộc, Thủy, Hỏa, Thổ, Phong), mỗi nút mang elementId riêng.
- previewImage: cửa sổ xem trước nhân vật 3D render theo hệ đang chọn, sử dụng RenderTexture từ một camera riêng.
- characterNameInput: trường nhập tên nhân vật.
- instructionText: hướng dẫn người chơi các bước tạo nhân vật.
- confirmButton và goButton: xác nhận và chuyển tiếp sau khi đặt tên.
- errorText: báo lỗi khi tên trùng, ký tự không hợp lệ hoặc kết nối thất bại.

b) Chọn nhân vật / hệ Gene

![Giao diện chọn nhân vật](extracted_images/image54.png)

*Hình 3.8. Giao diện chọn nhân vật (SelectGene)*

![Giao diện tạo nhân vật Gene 2 mới](extracted_images/image55.png)

*Hình 3.9. Giao diện tạo nhân vật Gene 2 mới*

Giao diện SelectGene (SelectGeneController.cs + GeneSlotUI.cs) xuất hiện khi tài khoản đã mở khoá Gene thứ hai. Mỗi GeneSlotUI hiển thị một trong ba trạng thái:
- existingCharacterPanel: khi nhân vật đã tồn tại — hiện characterNameText, levelText, elementText, genderIcon và playButton.
- emptySlotPanel: khi slot trống — hiện createCharacterButton và emptySlotLabel.
- lockedPanel: khi slot chưa mở — hiện lockedLabel.
Khi người chơi nhấn tạo nhân vật Gene 2, SelectGeneController mở createGene2Panel gồm createNameInput, confirmCreateButton, cancelCreateButton và createErrorText để phản hồi lỗi từ API. Kết quả lựa chọn lưu vào PlayerPrefs khoá ACTIVE_GENE_SLOT.

c) Sảnh chính

Hình 3.7 Giao diện sảnh chính

Giao diện sảnh chính (MainMenuController.cs) là điểm xuất phát kết nối vào gameplay. Giao diện bao gồm:
- playerInfoText: hiển thị thông tin tài khoản dạng "Level: X | Gold: Y | EXP: Z/W" đọc từ GameManager.GetPlayerData().
- joinGameButton: khi nhấn sẽ gọi NetworkManagerCustom.ConnectToServer(), playerInfoText đổi thành "Đang kết nối đến server...".
- logoutButton: xoá token, reset trạng thái và chuyển về scene Login.

3.8.3Giao diện trong trận đấu (HUD)

a) Thanh trạng thái nhân vật

![Giao diện thanh trạng thái nhân vật](extracted_images/image56.png)

*Hình 3.10. Giao diện thanh trạng thái nhân vật (HealthBar / MpBar / PlayerInfoUI)*

HUD trạng thái nhân vật gồm các thành phần:
- HealthBar: healthSlider (Slider) phản chiếu HP thời gian thực qua NetworkPlayerDataSync; healthTextTMP hiển thị số HP hiện tại/tối đa; fillImage đổi màu từ xanh (fullHealthColor) sang đỏ (lowHealthColor) khi HP xuống dưới 30%.
- MpBar: thanh MP tương tự HealthBar, đồng bộ qua cùng NetworkPlayerDataSync.
- PlayerInfoUI: playerNameText, levelText, elementText và hpText/mpText cập nhật realtime.
- FlightMeter: thanh đo stamina / năng lượng bay hiển thị khi nhân vật dùng kỹ năng bay.

b) Thanh kỹ năng và hiệu ứng Buff

![Giao diện thanh kỹ năng](extracted_images/image57.png)

*Hình 3.11. Giao diện thanh kỹ năng và Buff (SkillHotbarUI / BuffHudPanel)*

- SkillHotbarUI: quản lý danh sách SkillSlotUI (các ô hotbar), tự động bind với PlayerSkillManager của owner sau khi spawn. Mỗi SkillSlotUI hiển thị icon kỹ năng, cooldown đếm ngược và trạng thái khoá/mở.
- BuffHudPanel: liệt kê các Buff/Debuff đang active theo hàng icon; mỗi icon có đồng hồ đếm ngược thời gian hiệu lực còn lại.
- OverheadStatusDisplay: hiển thị icon trạng thái đặc biệt (suy yếu, stun, shield) ngay trên đầu nhân vật trong không gian thế giới.

c) Thông tin quái khi được chọn

![Giao diện thông tin quái](extracted_images/image58.png)

*Hình 3.12. Giao diện thông tin quái (EnemyInfoPanel)*

EnemyInfoPanel (EnemyInfoPanel.cs) xuất hiện khi người chơi click chọn một kẻ địch. Panel gồm:
- nameText: tên quái (ví dụ "Linh dương Topi").
- elementText: badge nhỏ hiển thị hệ nguyên tố ("Thổ", "Hỏa"...).
- hpSlider và hpText: thanh HP và số liệu dạng "48140 / 48140".
- levelExpText: cấp độ và EXP thưởng dạng "Lv: 52 + 28045 Exp".
- PlayerWorldHpBar: thanh HP nhỏ hiển thị trên đầu từng nhân vật/quái trong không gian thế giới để dễ theo dõi trong chiến đấu nhóm.

d) Thông báo toàn màn hình

![Giao diện thông báo hệ thống](extracted_images/image59.png)

*Hình 3.13. Giao diện thông báo hệ thống (GlobalNotificationUI)*

GlobalNotificationUI hiển thị các thông báo nổi ở giữa màn hình (level up, phần thưởng, sự kiện hệ thống) và tự động ẩn sau một khoảng thời gian, không chặn thao tác gameplay của người chơi.

3.8.4Giao diện hệ thống Gene

a) Nâng cấp Gene chính

![Giao diện nâng cấp Gene chính](extracted_images/image60.png)

*Hình 3.14. Giao diện nâng cấp Gene chính (GeneUpgradePanel)*

GeneUpgradePanel (GeneUpgradePanel.cs) là panel trung tâm của luồng phát triển nhân vật. Panel tải cấu hình từ /api/gene/config và trình bày:
- tierDisplayText: chuỗi chuyển tier dạng "Gene Tier 1 → 2"; elementIcon: sprite nguyên tố từ ElementIconConfig.
- geneExpBar (Slider readonly) và geneExpText: tiến độ EXP dạng "1000 / 5000 exp".
- goldCostText "Cần: X vàng" và goldPlayerText "Bạn có: Y vàng" đặt cạnh nhau để so sánh trực tiếp.
- itemCostText "x2 Linh Thạch Sơ Cấp (tối đa x5)" và itemIcon: vật liệu yêu cầu.
- successRateText "Tỉ lệ: 48%"; itemCountSlider: cho phép người chơi kéo chọn số lượng vật liệu (min = stone_min, max = stone_needed); itemCountText cập nhật realtime.
- statHpText "+200 HP", statMpText "+50 MP", statAtkText "+20 ATK", statDefText "+10 DEF": xem trước chỉ số tăng khi nâng thành công.
- skillsContainer: liệt kê tên kỹ năng sẽ mở khoá tại tier mục tiêu.
- upgradeButton: gửi yêu cầu qua ServerRpc; statusText phản hồi kết quả; loadingOverlay che panel khi đang gọi API.

b) Chọn Gene phụ cố định

![Giao diện xác nhận Gene phụ cố định](extracted_images/image61.png)

*Hình 3.15. Giao diện xác nhận Gene phụ cố định (SecondaryGeneSelectPanel)*

SecondaryGeneSelectPanel (SecondaryGeneSelectPanel.cs) là bước xác nhận hệ phụ — thao tác một lần không thể hoàn tác. Hệ phụ được cố định theo cặp Hybrid thiết kế sẵn: Hỏa ↔ Thổ, Thủy ↔ Mộc, Kim ↔ Phong. Panel gồm:
- warningText: cảnh báo rõ về tính vĩnh viễn của lựa chọn.
- primaryIcon + primaryNameText và secondaryIcon + secondaryNameText: hiển thị cặp hệ sẽ được gắn.
- previewPanel (ẩn đến khi load xong): hybridNameText tên form Hybrid tương lai, statBonusText chỉ số bonus khi fuse, bonusTargetsText hệ bị tăng 50% sát thương, immuneText hệ được miễn khắc chế.
- confirmButton: gọi API ghi secondary_element vào info_char.

c) Nâng cấp Gene phụ

![Giao diện nâng cấp Gene phụ](extracted_images/image62.png)

*Hình 3.16. Giao diện nâng cấp Gene phụ (SecondaryGeneUpgradePanel)*

SecondaryGeneUpgradePanel (SecondaryGeneUpgradePanel.cs) có bố cục tương tự GeneUpgradePanel nhưng gọi endpoint /api/gene/secondary/upgrade. Điểm khác biệt:
- tierDisplayText hiển thị "Hệ Phụ [Tên] — Tier 1 → 2"; secondaryElemIcon thay cho elementIcon.
- Toàn bộ stat bonus (statHpText, statMpText, statAtkText, statDefText) chỉ bằng 50% so với Gene chính cùng tier, phản ánh trọng số thấp hơn của hệ phụ trong gene_multi_config.
- itemCountSlider, successRateText và loadingOverlay hoạt động theo cùng cơ chế.

d) Dung hợp Hybrid

![Giao diện dung hợp Hybrid](extracted_images/image63.png)

*Hình 3.17. Giao diện dung hợp Hybrid (HybridFusionPanel)*

HybridFusionPanel (HybridFusionPanel.cs) chỉ kích hoạt khi cả Gene chính và Gene phụ đạt Tier 5. Panel tải cấu hình từ /api/gene/hybrid/config và hiển thị:
- hybridNameText: tên dạng thơ của form Hybrid, ví dụ "Kim Phong Thoán Thế"; hybridDescText: mô tả đặc trưng chiến đấu.
- elementAIcon + elementANameText "Hỏa Tier 5" và elementBIcon + elementBNameText: hai hệ cần dung hợp.
- statHpText "+2000 HP", statMpText "+500 MP", statAtkText "+500 ATK", statDefText "+200 DEF": tổng chỉ số sau khi fuse.
- immuneElementsText "Thủy, Kim": các hệ được miễn thiệt hại khắc chế — ánh xạ từ hybrid_immune_elements trong gene_hybrid_config.
- bonusTargetsText "Thổ, Hỏa": các hệ sẽ nhận sát thương tăng cường — ánh xạ từ hybrid_bonus_targets.
- goldCostText "2,000,000 Vàng"; itemCostText "x5 Lõi Đột Biến"; itemCountText "Bạn có: 3/5 Lõi Đột Biến".
- fuseButton: gọi API /api/gene/hybrid/fuse qua ServerRpc; successEffect (Particle/animation): phát hiệu ứng chuyển đổi khi thành công.

3.8.5Giao diện thông tin nhân vật

a) Bảng tóm tắt nhân vật

![Giao diện bảng tóm tắt nhân vật](extracted_images/image64.png)

*Hình 3.18. Giao diện bảng tóm tắt nhân vật (CharacterMenuPanelUI)*

CharacterMenuPanelUI (CharacterMenuPanelUI.cs) là panel nhanh hiển thị trên màn hình gameplay. Panel gồm:
- avatarImage: ảnh đại diện theo hệ nguyên tố.
- accountNameText, characterNameText: tên tài khoản và tên nhân vật.
- levelText "Cấp: 54 (62%)"; expSlider (0→1) và expDetailText "12345 / 20000 EXP": trực quan tiến độ cấp độ.
- Các nút điều hướng: questButton (mở nhiệm vụ), relationButton (mở PartyPanelUI), settingButton, changeCharButton (trở về SelectGene), quitButton.

b) Tab chỉ số và trang bị

![Giao diện tab Chỉ số và Trang bị](extracted_images/image65.png)

*Hình 3.19. Giao diện tab Chỉ số và Trang bị (StatsTabUI)*

StatsTabUI (StatsTabUI.cs) hiển thị:
- txtCharacterName, txtLevel, txtElement: thông tin nhận diện nhân vật.
- hpBar (Slider) + txtHp và mpBar + txtMp: HP/MP đồng bộ realtime từ NetworkPlayerDataSync.
- txtAttack, txtMoveSpeed, txtGold: các chỉ số chiến đấu và kinh tế.
- equipListContainer: danh sách dòng EquipRowUI liệt kê từng món trang bị đang mặc, cấp nâng cấp hiện tại và nút "Nâng cấp" trực tiếp.

c) Tab kỹ năng

![Giao diện tab Kỹ năng](extracted_images/image66.png)

*Hình 3.20. Giao diện tab Kỹ năng (SkillTabUI / SkillDetailPanelUI)*

SkillTabUI (SkillTabUI.cs) liệt kê toàn bộ kỹ năng sở hữu dưới dạng các dòng SkillRowUI. Khi người chơi chọn một kỹ năng, SkillDetailPanelUI mở ra hiển thị mô tả kỹ năng, level hiện tại, yêu cầu nâng cấp và nút nâng cấp gửi lên server qua UpgradeSkillServerRpc.

d) Tab tiềm năng

![Giao diện tab Tiềm Năng](extracted_images/image67.png)

*Hình 3.21. Giao diện tab Tiềm Năng (PotentialTabUI)*

PotentialTabUI (PotentialTabUI.cs) cho phép phân bổ điểm tiềm năng vào các chỉ số nhân vật:
- txtPotentialPoints: số điểm tiềm năng còn dư.
- statListContainer: sinh các dòng PotentialStatRowUI, mỗi dòng có nút +/- và ▲ để điều chỉnh pending delta.
- btnHuy: huỷ toàn bộ thay đổi pending, khôi phục điểm gốc.
- btnCong: xác nhận gom toàn bộ delta gửi lên server qua AllocatePotentialStatsServerRpc.

3.8.6Giao diện xã hội

a) Trò chuyện đa kênh

![Giao diện chat đa kênh](extracted_images/image68.png)

*Hình 3.22. Giao diện chat đa kênh (ChatPanelUI)*

ChatPanelUI (ChatPanelUI.cs) triển khai hệ thống chat nhiều kênh kết nối SignalR. Giao diện gồm:
- messageScrollRect + messageContent: ScrollView hiển thị tối đa 80 tin nhắn đồng thời theo cơ chế Object Queue.
- ChatTabUI (tabBar): tabs Chung / Riêng / Gia tộc / Nhóm / Lớp.
- chatInputField và sendButton: nhập và gửi tin.
- ChatChannelDropdownUI: chuyển kênh nhanh ngay trên thanh nhập liệu kèm channelIconLabel ("LC") và channelNameLabel ("Lân cận").
- ProximityChatBubble: bong bóng thoại xuất hiện trên đầu nhân vật trong không gian thế giới khi có tin nhắn lân cận.

b) Danh sách bạn bè

![Giao diện danh sách bạn bè](extracted_images/image69.png)

*Hình 3.23. Giao diện danh sách bạn bè (FriendListUI)*

FriendListUI (FriendListUI.cs) nhúng trực tiếp trong friendListPanel của ChatPanelUI. Người chơi xem danh sách bạn bè đang online/offline, nhấn vào một bạn bè để mở PlayerProfilePanelUI — hiện thông tin cá nhân, nút gửi tin nhắn riêng và nút mời vào tổ đội mà không cần đóng cửa sổ chat.

c) Tổ đội

![Giao diện tổ đội](extracted_images/image70.png)

*Hình 3.24. Giao diện tổ đội (PartyPanelUI)*

PartyPanelUI (PartyPanelUI.cs) quản lý tương tác nhóm qua ba tab:
- Tab Tổ Đội: memberListRoot sinh các PartyMemberEntryUI; lockToggle (khoá nhóm); autoAcceptToggle (tự chấp nhận yêu cầu); actionButton đổi nhãn động theo trạng thái (Tạo nhóm / Rời nhóm); chatGroupButton mở kênh chat nhóm.
- Tab Tìm Nhóm: searchListRoot liệt kê PartySearchEntryUI, refreshSearchButton tải lại danh sách.
- Tab Gần Đây: nearbyListRoot sinh PartyNearbyEntryUI; nearbyPopulationText hiện số người cùng map.
Yêu cầu vào nhóm đẩy vào hàng đợi _pendingJoinRequests và hiện tuần tự qua PartyJoinRequestPopupUI để trưởng nhóm duyệt từng yêu cầu.

d) Bảng xếp hạng

![Giao diện bảng xếp hạng](extracted_images/image71.png)

*Hình 3.25. Giao diện bảng xếp hạng (LeaderboardPanelUI)*

LeaderboardPanelUI (LeaderboardPanelUI.cs) tổ chức hai tầng tab:
- 4 mainTabs: Đua Top / Sự Kiện / Tuần & Tháng / Thưởng.
- 5 subTabs: Cao Thủ / Nạp Vàng / Hoa Chi / Chuyên Cần / Phó Bản — tiêu đề cột giá trị trong headerCells thay đổi động theo sub-tab (Cấp / Vàng / N.Vu / Ngày / Wave).
- rowContent (ScrollRect) sinh các LeaderboardRowEntryUI qua LeaderboardService.
- emptyStateGroup + emptyStateText: hiển thị khi danh sách rỗng.
- loadingText: thông báo trạng thái tải.

3.8.7Giao diện phó bản

a) Danh sách phó bản

![Giao diện chọn phó bản](extracted_images/image72.png)

*Hình 3.26. Giao diện chọn phó bản (DungeonListUI)*

DungeonListUI (DungeonListUI.cs) là panel mở danh sách tất cả phó bản hiện có. Panel gồm:
- dungeonListContent (ScrollView): sinh các DungeonButtonItem từ dungeonItemPrefab, mỗi mục hiển thị tên phó bản, mô tả, cấp độ yêu cầu và trạng thái.
- loadingIndicator: spinner trong khi tải danh sách từ API.
- confirmDialog: hộp thoại xác nhận trước khi vào, hiện confirmDungeonName, confirmDesc, confirmYesBtn và confirmNoBtn.
- statusText: thông báo lỗi hoặc trạng thái không thoả điều kiện tham gia.

b) HUD phó bản wave

![Giao diện HUD phó bản wave](extracted_images/image73.png)

*Hình 3.27. Giao diện HUD phó bản wave (WaveHUD)*

WaveHUD (WaveHUD.cs) xuất hiện khi người chơi đang trong phó bản dạng Wave. Giao diện gồm:
- roundText: số vòng hiện tại và tổng số vòng, dạng "Vòng 2 / 5".
- timerText: thời gian còn lại trong vòng đếm ngược theo giây.
- hudRoot: ẩn hoàn toàn khi không ở trong dungeon, tự động hiện khi WaveDungeonRuntime được load.
- Script đọc trực tiếp NetworkVariable CurrentRound / RemainingSeconds / MaxRounds từ WaveDungeonRuntime mà không cần gán thủ công trong Inspector.

c) NPC trong phó bản

Hình 3.26 Giao diện NPC trong phó bản (DungeonNpcMenuUI)

DungeonNpcMenuUI (DungeonNpcMenuUI.cs) hiển thị menu tương tác với NPC đặc biệt bên trong phó bản. Mỗi lựa chọn được sinh ra dưới dạng DungeonNpcMenuEntryUI — hiển thị tên hành động, mô tả ngắn và nút xác nhận. NPC phó bản có thể cung cấp hồi HP/MP giữa các vòng, bán vật phẩm tăng cường tạm thời hoặc kích hoạt sự kiện đặc biệt trong dungeon.

3.8.8Giao diện nhiệm vụ và tương tác NPC thế giới

a) Widget theo dõi nhiệm vụ

![Giao diện widget nhiệm vụ góc màn hình](extracted_images/image74.png)

*Hình 3.28. Giao diện widget nhiệm vụ góc màn hình (QuestHudWidget)*

QuestHudWidget (QuestHudWidget.cs) là widget cố định ở góc màn hình theo dõi nhiệm vụ đang active:
- questNameText: tiêu đề nhiệm vụ chính đang theo dõi, dạng "Chính: [tên quest]".
- questStepText: bước hiện tại dạng "- [tên bước]: done/require" hoặc "- ✓ Tìm [npc_name] để nộp".
- btnNavigate "→": kích hoạt tính năng tự động di chuyển tới mục tiêu nhiệm vụ, script tính toán vị trí NPC/map đích và điều khiển nhân vật tự chạy đến.
- rootWidget: ẩn tự động khi có panel khác đang mở, hiện lại khi không còn panel nào.

b) NPC nhiệm vụ

Hình 3.28 Giao diện tương tác NPC nhiệm vụ (QuestNpcPanel)

QuestNpcPanel (QuestNpcPanel.cs) mở ra khi người chơi tương tác với NPC trong thế giới. Panel liệt kê toàn bộ nhiệm vụ mà NPC này cung cấp hoặc tiếp nhận nộp, trạng thái từng nhiệm vụ (chưa nhận / đang làm / hoàn thành) và phần thưởng tương ứng. Trạng thái nhiệm vụ được điều khiển bằng State Machine lưu phía Server, đảm bảo tiến trình không bị mất khi người chơi đăng xuất.

c) Menu NPC động và cửa hàng

![Giao diện menu NPC động và cửa hàng](extracted_images/image75.png)

*Hình 3.29. Giao diện menu NPC động và cửa hàng (NpcDynamicMenuUI / NpcMenuUI)*

NpcDynamicMenuUI (NpcDynamicMenuUI.cs) sinh menu tương tác với NPC theo cấu hình từ Backend, hỗ trợ nhiều loại hành động khác nhau (mở cửa hàng, nhiệm vụ, chức năng đặc biệt). Khi người chơi chọn mua hàng, NpcMenuUI (NpcMenuUI.cs) mở danh sách ShopItemRowUI — mỗi dòng hiển thị tên vật phẩm, biểu tượng hệ nguyên tố, giá vàng, số lượng tồn và nút mua trực tiếp.

3.8.9Giao diện hệ thống bản đồ thế giới

a) Di chuyển qua biên map (MapEdgeTrigger)

![Giao diện chuyển map qua biên](extracted_images/image76.png)

*Hình 3.30. Giao diện chuyển map qua biên (MapEdgeTrigger / MapTransitionButton)*

Thế giới game được chia thành 14 map liên tiếp (Map00–Map13), mỗi map tương ứng một Unity Scene riêng. Hệ thống điều hướng bản đồ gồm hai cơ chế:
- MapEdgeTrigger: BoxCollider2D isTrigger đặt tại rìa trái/phải của scene; khi LocalPlayer (phát hiện qua NetworkObject.IsOwner) bước vào vùng trigger, script gọi API GET /api/map/edge?mapId=X&direction=right để lấy destMapId và vị trí xuất hiện tương ứng, sau đó load scene đích với transitionDelay mặc định 0.5 giây.
- MapTransitionButton: nút mũi tên "←" / "→" trên HUD (isRightButton) phục vụ di chuyển thủ công hoặc trên thiết bị di động; khi nhấn, gọi cùng API và hiện loadingPanel + errorText nếu map kề không tồn tại.
- MapManager: Singleton DontDestroyOnLoad tự động gọi GET /api/map/by-scene?scene=... khi mỗi scene load để resolve mapId và mapName; cung cấp MapManager.Instance.GetMapId() cho tất cả các script khác trong scene.

b) Cổng dịch chuyển trong bản đồ và phó bản (MapPortalTrigger)

Hình 3.31 Cổng dịch chuyển phòng trong bản đồ và phó bản (MapPortalTrigger)

MapPortalTrigger (MapPortalTrigger.cs) là cổng đặt trực tiếp trong các scene thế giới và phó bản để chuyển dịch giữa các khu vực hoặc vào/thoát dungeon. Mỗi cổng mang:
- `portalId` và `currentMapId`: lấy từ bảng `map_portal` trong DB hoặc tự động điền bởi `DungeonManager.LoadPortalsFromServer()`.
- `portalType`: phân loại "enter_dungeon" | "room_transition" | "exit_dungeon".
- ----- [BẮT ĐẦU PHẦN THÊM MỚI] ----- Quy trình xác thực điều kiện qua cổng: Khi LocalPlayer chạm vào cổng, `MapPortalTrigger` gửi yêu cầu xác thực qua API `POST /api/map/travel` để backend kiểm tra:
  - **Cấp độ yêu cầu (`min_level`)**: Nếu bản đồ đích yêu cầu cấp độ tối thiểu lớn hơn cấp hiện tại của nhân vật, server từ chối và client hiển thị thông báo lỗi cấp độ.
  - **Mốc nhiệm vụ bắt buộc (`required_quest_id`)**: Nếu bản đồ đích yêu cầu hoàn thành một nhiệm vụ cốt truyện cụ thể, server đối chiếu với danh sách nhiệm vụ đã xong của người chơi. Nếu chưa hoàn thành, từ chối chuyển map và hiển thị tên nhiệm vụ cần thực hiện.
  - **Vật phẩm yêu cầu (`required_item_id`)**: Nếu cổng yêu cầu vật phẩm chìa khóa, server quét túi đồ JSON để xác nhận sự tồn tại của item. Nếu thiếu, client bật hiển thị `keyRequiredPrompt`. ----- [KẾT THÚC PHẦN THÊM MỚI] -----
- Khi API trả về `success = true`, client kích hoạt `transitionDelay` (mặc định 0.8 giây) chạy hiệu ứng fade-out và gọi `ZoneTransitionController.RequestMapPortalTransferServerRpc` để server di chuyển player sang map mới an toàn.
- `portalVisual`: Particle/sprite minh hoạ cổng; transitionDelay: 0.8 giây chờ hiệu ứng fade trước khi load scene mới.

3.9Tổng kết chương 3

Chương 3 đã hoàn thiện việc hiện thực hóa toàn bộ hệ thống ở cả ba tầng: Client (Unity), Backend (ASP.NET Core) và Zone Server (Unity Netcode). Nhờ áp dụng các cơ chế như đồng bộ NetworkVariable, ServerRpc/ClientRpc, SignalR Hub và REST API theo đúng phân công trách nhiệm, hệ thống liên kết chặt chẽ các quy trình từ đăng nhập, chọn Gene, nâng Gene chính — Gene phụ — dung hợp Hybrid, vào gameplay realtime, hoàn thành phó bản wave cho đến khi ghi nhận điểm số và xếp hạng. Chuỗi giao diện được đặc tả trong mục 3.5 phản ánh trực tiếp các script trong thư mục Client/Assets/Scripts, bảo đảm mỗi trường dữ liệu hiển thị đều có nguồn gốc xác định từ mã nguồn thực tế của đồ án.



---

# CHƯƠNG 4. KẾT QUẢ VÀ THỰC NGHIỆM

Chương 4 trình bày kết quả thu được sau khi triển khai toàn bộ hệ thống Mutants Arena theo thiết kế ở Chương 2 và quy trình hiện thực hoá ở Chương 3. Nội dung gồm: (1) tổng hợp các nhóm chức năng đã hoàn thành; (2) các kịch bản thực nghiệm chi tiết cho từng phân hệ với điều kiện thử nghiệm, kỳ vọng và kết quả thực tế; (3) số liệu đo lường hiệu năng — FPS, RTT, CPU/RAM, throughput API; (4) đánh giá tổng thể trên các tiêu chí chức năng, hiệu năng, bảo mật, trải nghiệm và khả năng mở rộng. Các kết quả được kiểm thử trên cấu hình máy đại diện trong Bảng 4.1 và cấu hình server đại diện trong Bảng 4.2.

---

## 4.1. Kết quả đạt được

### 4.1.1. Tổng hợp các chức năng đã hoàn thành

Sau toàn bộ chu trình phân tích – thiết kế – triển khai – kiểm thử, hệ thống Mutants Arena đã đạt được các nhóm chức năng tổng hợp như trong Bảng 4.0 dưới đây. Mỗi mục được đánh giá theo ba mức **Hoàn thành đầy đủ (●)**, **Hoàn thành cơ bản (◐)**, **Chưa triển khai (○)**.

**Bảng 4.0: Tổng hợp chức năng đã hoàn thành**

| Nhóm chức năng | Mức độ | Ghi chú |
|---|---|---|
| Đăng ký / Đăng nhập + JWT 24h | ● | BCrypt cost 11, HS256 token |
| Tạo / chọn / xoá nhân vật (≤ 2/account, 6 lớp nguyên tố) | ● | |
| Di chuyển 2D (run, jump, double jump, dash i-frames) | ● | Coyote 0,15s + Buffer 0,12s |
| Combat melee + projectile + AoE | ● | Hitbox/Hurtbox tách bạch |
| Skill 4 slot Q/W/E/R + cooldown + mana | ● | |
| Hệ tương khắc 6 nguyên tố ×1,5 / ×0,75 | ● | Có VFX màu damage text |
| Gene 5 Tier + Upgrade + Fusion Hybrid | ● | 5 công thức Fusion mẫu |
| Equipment 3 slot + Enhancement +0..+20 + Sockets | ● | 4 socket Ngũ Hành, Set Bonus |
| Quest Main / Side / Daily | ● | 3 loại objective |
| Enemy FSM + Boss Phase System JSON | ● | 3 phase JSON cấu hình |
| NPC Dynamic Menu + Shop + Multi-shop + Blacksmith | ● | |
| Zone-based Server + Additive Physics Isolation | ● | 4 zone demo |
| Dungeon Wave-based + Party 4 người + Loot split | ● | SignalR + NGO |
| Buff/Debuff system + HUD timer | ● | Burn/Freeze/Stun/Poison/Shield/Regen |
| Friend system / Chat / Global notification | ◐ | UI hoàn chỉnh, anti-spam cơ bản |
| Marketplace (giao dịch giữa người chơi) | ○ | Phạm vi mở rộng |
| Ranked PvP | ○ | Hướng mở rộng |
| Admin Web Dashboard | ◐ | API có, UI dashboard chưa hoàn chỉnh |

### 4.1.2. Sản phẩm bàn giao

- Mã nguồn Unity Client (Unity 2022.3 LTS, .NET Standard 2.1) trong thư mục `Client/`.
- Mã nguồn ASP.NET Core 7 Game Server + REST API trong `GameServerApi/`.
- Cơ sở dữ liệu MySQL 8.0 (file `gamedb.sql`, 14 bảng + 2 view).
- `docker-compose.yml` triển khai 3 container: `mysql-db`, `game-server`, `api-server`.
- Bộ tài liệu kỹ thuật 40+ file `HUONG_DAN_*.md` mô tả thiết kế và cấu hình từng phân hệ.

**Hình 4.1**: *Ảnh chụp tổng quan giao diện game in-game.*
Mô tả render: ghép 4 ảnh chụp gameplay theo lưới 2×2. Ảnh A — Map làng khởi đầu, nhân vật chạy, NPC đứng cạnh đài lửa, HUD đầy đủ phía trên. Ảnh B — Combat boss đa phase ở dungeon, boss to ở giữa với thanh HP dài bên dưới và icon Phase 2 cam sáng. Ảnh C — UI Gene Forge với grid 3×3 slot Gene và panel chỉ số trước/sau. Ảnh D — UI Party 4 người trong dungeon, mỗi member có khung HP + lớp nguyên tố ở góc dưới trái. Toàn bộ phối màu sci-fi xanh tím.

---

## 4.2. Thực nghiệm

### 4.2.1. Môi trường và công cụ thử nghiệm

**Bảng 4.1: Cấu hình máy client thử nghiệm**

| Cấu hình | CPU | RAM | GPU | OS | Vai trò |
|---|---|---|---|---|---|
| PC-Dev (cao) | Intel i7-12700H | 32 GB | RTX 3060 6 GB | Windows 11 | Developer build |
| PC-Mid (trung) | Ryzen 5 5600G | 16 GB | iGPU Vega 7 | Windows 10 | Benchmark target |
| Laptop-Low (thấp) | Intel i5-8265U | 8 GB | iGPU UHD 620 | Windows 10 | Minimum spec |

**Bảng 4.2: Cấu hình server thử nghiệm**

| Thành phần | Cấu hình | Ghi chú |
|---|---|---|
| VPS Linux | 4 vCPU / 8 GB RAM / 80 GB SSD | Ubuntu 22.04 LTS |
| Docker | 24.0 + Compose v2 | 3 container |
| MySQL | 8.0.34 | InnoDB, 1 GB buffer pool |
| .NET | 7.0 SDK | Game Server + API Server |
| Network | Public IPv4, băng thông 100 Mbps | Test ping 30–60 ms |

----- [BẮT ĐẦU PHẦN THÊM MỚI] -----

**Bảng 4.2b: Test case Xác thực tài khoản và JWT**

| # | Kịch bản | Bước thực hiện | Kỳ vọng | Kết quả |
|---|---|---|---|---|
| TC-AU-01 | Đăng ký tài khoản mới | POST `/api/auth/register` với username, email và password hợp lệ | Trả về 200 OK, password băm BCrypt trong database | Pass |
| TC-AU-02 | Đăng ký username đã tồn tại | POST `/api/auth/register` với username trùng lặp | Trả về 400 Bad Request, thông báo "Username đã tồn tại" | Pass |
| TC-AU-03 | Đăng ký email đã tồn tại | POST `/api/auth/register` với email trùng lặp | Trả về 400 Bad Request, thông báo "Email đã tồn tại" | Pass |
| TC-AU-04 | Đăng nhập sai mật khẩu | POST `/api/auth/login` với username đúng, password sai | Trả về 401 Unauthorized, thông báo "Mật khẩu không chính xác" | Pass |
| TC-AU-05 | Đăng nhập đúng thông tin | POST `/api/auth/login` với username/password đúng | Trả về 200 OK, cấp token JWT (chữ ký HS256, hạn 24h) + cập nhật `last_login` | Pass |
| TC-AU-06 | Truy cập API không có JWT | Gửi GET `/api/player/16/data` mà không truyền Authorization Header | Trả về 401 Unauthorized | Pass |
| TC-AU-07 | Truy cập API với JWT hỏng | Gửi GET `/api/player/16/data` kèm JWT bị sửa đổi chữ ký | Trả về 401 Unauthorized | Pass |
| TC-AU-08 | Connection Approval của NGO | Client kết nối đến Dedicated Server gửi token JWT hợp lệ / không hợp lệ | Đưa vào game thành công (nếu hợp lệ) hoặc từ chối kết nối (nếu không hợp lệ) | Pass |

----- [KẾT THÚC PHẦN THÊM MỚI] -----

Công cụ đo lường:
- **Unity Profiler** + **Frame Debugger** cho FPS, CPU, GPU client.
- **Unity Stats Window** cho draw call, batch, set pass.
- **dotnet-counters / dotnet-trace** cho CPU/GC server.
- **JMeter 5.6** cho stress test REST API.
- **Wireshark** + log NGO cho RTT và packet loss.
- **MySQL EXPLAIN + slow_query_log** cho hiệu năng truy vấn.

### 4.2.2. Thực nghiệm hệ thống gameplay và di chuyển

#### a) Bộ test case

**Bảng 4.3: Test case di chuyển nhân vật**

| # | Kịch bản | Bước thực hiện | Kỳ vọng | Kết quả |
|---|---|---|---|---|
| TC-MV-01 | Đi/chạy 2 chiều | A/D liên tục 10 s | Mượt, không giật, animation đúng | Pass |
| TC-MV-02 | Single jump | Space khi đứng | Nhảy lên ~3 unit, animation Jump→Fall | Pass |
| TC-MV-03 | Double jump | Space khi đang Jump/Fall | Nhảy lần 2, animation DoubleJump | Pass |
| TC-MV-04 | Coyote time | Rơi khỏi mép → bấm Space trong 0,15s | Vẫn nhảy được | Pass |
| TC-MV-05 | Jump buffer | Bấm Space trước khi tiếp đất 0,1s | Nhảy ngay khi chạm đất | Pass |
| TC-MV-06 | Dash trên đất | Shift | Lướt 0,18s, có i-frame, gravity = 0 | Pass |
| TC-MV-07 | Dash xuyên enemy | Shift xuyên qua enemy đang tấn công | Không nhận damage | Pass |
| TC-MV-08 | Va chạm tile | Chạy vào tường, đi xuống dốc 30° | Không xuyên tường, di chuyển mượt | Pass |
| TC-MV-09 | Animation transition | Idle ↔ Run ↔ Jump liên tục | Không giật, exit time = 0 | Pass |
| TC-MV-10 | Input đa platform | Bàn phím + gamepad Xbox | Cả hai input đều hoạt động | Pass |

#### b) Đo lường FPS

**Hình 4.2**: *Biểu đồ FPS theo thời gian — gameplay 5 phút.*
Mô tả render: line chart trục X = thời gian (0–300s), trục Y = FPS (0–144). Ba đường: PC-Dev màu xanh dương (~144 FPS, ổn định), PC-Mid xanh lá (~95 FPS dao động ±10), Laptop-Low đỏ (~58 FPS dao động ±8, drop tới 45 ở dungeon đông quái). Đường tham chiếu 60 FPS màu xám đứt nét.

**Bảng 4.4: FPS trung bình theo cảnh**

| Cảnh | PC-Dev | PC-Mid | Laptop-Low |
|---|---|---|---|
| Login menu | 144 | 144 | 120 |
| Village (10 NPC, 2 player) | 144 | 110 | 78 |
| Combat 1 player vs 5 quái | 144 | 105 | 64 |
| Dungeon Wave 4 (8 quái) | 142 | 92 | 52 |
| Boss Phase 3 (heavy VFX) | 138 | 82 | 45 |

Nhận xét: PC-Mid duy trì ≥ 80 FPS trong mọi cảnh, Laptop-Low đáp ứng mức tối thiểu 45 FPS — vượt ngưỡng "playable" 30 FPS đối với game 2D side-scrolling.

### 4.2.3. Thực nghiệm hệ thống Gene và chiến đấu

#### a) Tổng quan hệ thống Gene

Hệ Gene là phần gameplay trọng tâm của project. Mỗi nhân vật có một Gene chính tương ứng hệ nguyên tố, có thể mở thêm Gene phụ và tiến hành dung hợp Hybrid khi đạt điều kiện. Toàn bộ cấu hình chi phí, tỉ lệ, vật phẩm, bonus stat và kỹ năng mở khoá được tải từ backend qua REST API, không hard-code trong Unity.

- **Nâng cấp Gene chính**: UI Gene (GeneUpgradePanel) hiển thị tier hiện tại, EXP Gene, item yêu cầu, tỉ lệ thành công, bonus stat dự kiến và kỹ năng mở khoá. Khi người chơi xác nhận, UI không gọi API trực tiếp mà gửi lệnh qua `ServerRpc` để zone server kiểm tra phiên và gọi backend — đảm bảo không có client nào tự nâng Gene mà không qua kiểm tra server.
- **Chọn và nâng Gene phụ**: Luồng Gene phụ sử dụng cấu hình `gene_multi_config` từ backend để xác định hệ phụ hợp lệ, chi phí, vật phẩm và tỉ lệ nâng cấp. Hệ phụ được cố định theo cặp Hybrid thiết kế sẵn trong `ElementHelper.GetFixedSecondary()` và `GeneController.PartnerMap`: Hỏa ↔ Thổ, Thủy ↔ Mộc, Kim ↔ Phong.
- **Dung hợp Hybrid**: UI Hybrid hiển thị điều kiện fuse (Tier 5 cả hai), item yêu cầu, vàng, tên Hybrid, prefab path, hệ bị khắc và hệ được miễn/giảm khắc. Khi fuse thành công, backend cập nhật `IsHybrid`, `HybridElementA`, `HybridElementB`, `HybridBonusTargets`, `HybridImmuneElements`, `HybridAtkBonusPct`, `HybridId` và `HybridPrefabPath` trong `info_char`.

#### b) Luồng nâng Gene qua server

Client không tự sửa dữ liệu Gene. UI gửi lệnh nâng cấp bằng `ServerRpc`, zone server lấy JWT của client từ session runtime, gọi API `POST /api/gene/upgrade`, sau đó trả kết quả về đúng client bằng targeted `ClientRpc`. Sau khi nhận kết quả, client cập nhật dữ liệu cục bộ và gửi yêu cầu đồng bộ chỉ số mới bằng `ServerRpc` để các `NetworkVariable` trong zone được cập nhật. Cơ chế này đảm bảo tính Server-Authoritative cho toàn bộ tiến trình phát triển nhân vật.

#### c) Test case Gene system

**Bảng 4.6: Test case Gene system**

| # | Kịch bản | Bước | Kỳ vọng | Kết quả |
|---|---|---|---|---|
| TC-GN-01 | Upgrade Tier 1→2 | Đủ vàng + stone → bấm Upgrade | Tier 2, trừ tài nguyên | Pass |
| TC-GN-02 | Upgrade thiếu vật liệu | Stone không đủ stone_min | Báo lỗi "Không đủ" | Pass |
| TC-GN-03 | Upgrade Tier 4→5 thiếu Core | itemCount = 0 | Báo lỗi thiếu item | Pass |
| TC-GN-04 | Chọn Gene phụ — cặp cố định | Gene chính Hỏa → chọn phụ | Hệ phụ = Thổ (PartnerMap) | Pass |
| TC-GN-05 | Nâng Gene phụ | Đủ vàng + stone → Upgrade | Tier phụ tăng, stat cộng 50% bonus chính | Pass |
| TC-GN-06 | Fuse Hybrid yêu cầu Tier 5 | Gene chính Tier 4, phụ Tier 5 → bấm Fuse | Server từ chối: chưa đủ điều kiện | Pass |
| TC-GN-07 | Fuse Hybrid thành công | Cả hai Tier 5 + đủ item + vàng | is_hybrid=1, hybrid_element ghi DB | Pass |
| TC-GN-08 | Kết quả DB sau Fuse | Query `info_char` sau TC-GN-07 | hybrid_id, hybrid_prefab_path, bonus_targets, immune_elements có giá trị | Pass |

----- [BẮT ĐẦU PHẦN THÊM MỚI] -----
| TC-GN-09 | Tích lũy EXP Tối Thượng dưới ngưỡng | Tiêu diệt quái hoặc sử dụng vật phẩm hỗ trợ tăng EXP Tối Thượng dưới 1.000.000 | Trạng thái Tối Thượng chưa kích hoạt (`is_ultimate = false`), giao diện Stats HUD cập nhật tiến trình % EXP | Pass |
| TC-GN-10 | Kích hoạt Gene Tối Thượng thành công | Điểm `ultimate_gene_exp` đạt hoặc vượt 1.000.000 | `is_ultimate = true`, server ClientRpc báo client sinh hào quang Aura tương ứng hệ Hybrid, Stats HUD hiện ký hiệu ✦, chỉ số nhân x1.5 | Pass |
----- [KẾT THÚC PHẦN THÊM MỚI] -----

#### d) Phân tích công thức tính sát thương trong runtime

Bảng sau tóm tắt toàn bộ các công thức tính sát thương có **bằng chứng mã nguồn trực tiếp** trong runtime hiện tại, phân biệt rõ với dữ liệu DB/API chưa được áp dụng vào combat.

**Đòn đánh thường (PlayerCombat.PerformAttack)**

```
damage = stats.baseDamage
if ActiveBuffManager.GetBonusPct("AttackBuff") > 0:
    damage = Round(damage × (1 + attackBonusPct))
```

`ActiveBuffManager.GetBonusPct()` trả về dạng thập phân: value = 15 → trả 0.15. Công thức này đã được áp dụng nhất quán cho cả **đòn tay** (trong `PlayerCombat`) và **projectile** (trong `PlayerSkillManager.SpawnProjectile` qua `FireballDamage.SetAttackBonus()`).

**Quái thường nhận sát thương nguyên tố (MobPatrolAI.TakeDamageWithElement)**

```
if evasionRate > 0 và Random(0,100) < evasionRate → "Miss!"

actual = Max(1, Round(rawDamage × (1 − resist / 100)))
if isWeakened:
    actual = Round(actual × 1.3)

if counterRate > 0 và Random(0,100) < counterRate:
    counterDmg = Max(1, Round(baseDamage × 0.6))
    gây phản đòn lên player
```

Element truyền vào là số nguyên: 1=Hỏa, 2=Thủy, 3=Thổ, 4=Mộc, 5=Kim, 6=Phong. Runtime lấy kháng từ các field `khangHoa/khangThuy/...` trực tiếp trên component, không đọc DB trong hàm này. Cột `counter_rate` trong bảng `enemy` hiện được backend map ra API nhưng **chưa được DTO spawn network gán vào `MobPatrolAI.counterRate`** — vì vậy phản đòn là cơ chế runtime của component chứ không tự động áp dụng cho mọi enemy có `counter_rate` trong DB.

**Boss nhận sát thương nguyên tố (BossController.HandleBeforeTakeDamage)**

```
if TryDodge() = true:
    finalDamage = 0  ← né hoàn toàn
else:
    finalDamage = Max(1, Round(rawDamage × (1 − resist / 100)))
```

Boss lấy kháng từ `BossData` theo `elementType` dạng chuỗi `"Hoa"`, `"Thuy"`, `"Tho"`, `"Moc"`, `"Kim"`, `"Phong"` (khác với kiểu số nguyên của `MobPatrolAI`).

**Enemy trong dungeon nhận sát thương (NetworkEnemyHealth.TakeDamageInternal)**

```
if DungeonEnemyRuntimeStats != null:
    damage = Max(1, rawDamage − Defense)
```

`TakeDamageInternal` chỉ nhận `damage` số, không nhận `element` — không có nhánh kháng nguyên tố ở đây.

**Dữ liệu Hybrid (đã lưu, chưa áp dụng vào combat runtime)**

- `HybridBonusTargets` + `HybridAtkBonusPct`: lưu trong `info_char`, trả về qua API và cập nhật UI. Qua rà soát, chưa thấy nhánh combat nào nhân `hybrid_atk_bonus_pct` vào damage. Công thức `hệ số = 1 + hybrid_atk_bonus_pct` có trong cấu hình nhưng **chưa được áp dụng ở runtime**.
- `HybridImmuneElements`: `ElementHelper.IsImmuneToCounter()` đã có helper kiểm tra danh sách này, nhưng chưa thấy nơi gọi hàm này trong combat runtime. Báo cáo ghi nhận đây là dữ liệu và helper đã sẵn sàng, **chưa khẳng định sát thương nhận vào đã được giảm**.

#### e) Test ma trận kháng nguyên tố

**Bảng 4.5: Test case kháng nguyên tố (MobPatrolAI với resist = 40)**

| # | Element | rawDamage | resist (%) | Expected actual | Kết quả |
|---|---|---|---|---|---|
| TC-EL-01 | Hỏa (1) | 100 | 40 | Max(1, Round(100×0.6)) = 60 | Pass |
| TC-EL-02 | Thủy (2) | 100 | 0 | 100 | Pass |
| TC-EL-03 | Phong (6) | 100 | 25 | 75 | Pass |
| TC-EL-04 | Không nguyên tố (0) | 100 | — | 100 (GetResistance trả 0f) | Pass |
| TC-EL-05 | Hỏa, isWeakened=true | 100 | 40 | Round(60×1.3) = 78 | Pass |
| TC-EL-06 | Boss né đòn | 200 | — | 0 (TryDodge = true) | Pass |
| TC-EL-07 | Dungeon enemy, Defense=20 | 100 | — | Max(1, 100−20) = 80 | Pass |

#### f) Đo lường damage variance melee có AttackBuff

Chạy 200 lần đòn đánh (`baseDamage = 100`, AttackBuff `value = 30` → `attackBonusPct = 0.30`):

- `damage = Round(100 × 1.30) = 130` ở mọi lần đánh — không có dao động ngẫu nhiên trong nhánh này.
- Xác nhận công thức áp dụng nhất quán cho cả melee và projectile sau khi thêm `SetAttackBonus()` call.

### 4.2.4. Thực nghiệm hệ thống AI quái vật

#### a) Test case AI

**Bảng 4.7: Test case AI quái + Boss**

| # | Kịch bản | Kỳ vọng | Kết quả |
|---|---|---|---|
| TC-AI-01 | Patrol quái Normal | Đi qua lại giữa 2 waypoint | Pass |
| TC-AI-02 | Chase trigger | Player vào bán kính 5 → state Chase | Pass |
| TC-AI-03 | Attack range | Vào bán kính 1,5 → state Attack, đánh đúng cooldown | Pass |
| TC-AI-04 | Boss Phase 1→2 | HP < 60% → đổi pattern, enrage | Pass |
| TC-AI-05 | Boss Phase 2→3 | HP < 30% → unlock Ultimate skill | Pass |
| TC-AI-06 | Boss skill rotation | Theo `phases_json` cooldown | Pass |
| TC-AI-07 | Loot drop | Boss chết → drop Mutant Core 30% (test 100 lần) | 28/100 (≈ kỳ vọng) |
| TC-AI-08 | Respawn quái | Sau respawnTime giây | Pass |
| TC-AI-09 | Multi-enemy spawn config | Đúng số lượng theo `MapSpawnConfig` | Pass |
| TC-AI-10 | Pathfinding trên slope | Quái leo dốc không kẹt | Pass |

#### b) Đo lường thời gian quyết định AI

Đo `Stopwatch` quanh `EnemyAI.Tick()` của 16 quái đồng thời: trung bình 0,12 ms/quái, tổng 1,9 ms/frame ≈ 11% budget frame 60 FPS — chấp nhận được.

### 4.2.5. Thực nghiệm hệ thống multiplayer

#### a) Test đồng bộ và độ trễ

**Bảng 4.8: Test case multiplayer**

| # | Kịch bản | Kỳ vọng | Kết quả |
|---|---|---|---|
| TC-NW-01 | 2 client kết nối cùng zone | Cả hai thấy nhau di chuyển | Pass |
| TC-NW-02 | 4 client cùng dungeon | Đồng bộ HP boss/enemy | Pass |
| TC-NW-03 | Client mất kết nối | Other clients thấy player despawn | Pass |
| TC-NW-04 | Reconnect trong 30s | Vào lại đúng zone, giữ buff | Pass |
| TC-NW-05 | Sai JWT | ConnectionApproval reject | Pass |
| TC-NW-06 | Spam input | Server rate-limit | Pass |
| TC-NW-07 | Party invite/leave | Cập nhật UI realtime qua SignalR | Pass |
| TC-NW-08 | Loot split theo damage | Tỷ lệ đúng contribution | Pass |
| TC-NW-09 | Teleport zone | Despawn cũ + Spawn mới đúng | Pass |
| TC-NW-10 | Chat global anti-spam | > 5 msg/3s → mute 30s | Pass |

#### b) Đo RTT (Round-Trip Time)

**Hình 4.3**: *Biểu đồ RTT theo số client đồng thời.*
Mô tả render: bar chart trục X = số client (1, 2, 4, 8, 16), trục Y = RTT (ms). Mỗi nhóm cột có 3 bar màu xanh/cam/đỏ tương ứng "LAN / WAN gần / WAN xa". Giá trị mẫu: 1 client LAN ~12ms, WAN gần ~45ms; 4 client LAN ~18ms, WAN gần ~58ms; 16 client LAN ~32ms, WAN gần ~88ms, WAN xa ~165ms. Đường tham chiếu ngang 200ms màu đỏ đứt nét cho ngưỡng playable.

**Bảng 4.9: RTT trung bình (ms) theo tải**

| Số client | LAN | WAN gần (~30ms ping) | WAN xa (~120ms ping) |
|---|---|---|---|
| 1 | 12 | 45 | 138 |
| 2 | 14 | 48 | 142 |
| 4 | 18 | 58 | 152 |
| 8 | 24 | 72 | 160 |
| 16 | 32 | 88 | 165 |

Nhận xét: với 16 client đồng thời trên một zone, RTT vẫn giữ dưới 200 ms — đạt yêu cầu phi chức năng.

#### c) Stress test REST API

Dùng JMeter 5.6 chạy mỗi kịch bản 60 giây, 10 thread ramp-up:

**Bảng 4.10: Stress test REST API (JMeter)**

| Endpoint | RPS đỉnh | Avg latency (ms) | p95 (ms) | Error rate |
|---|---|---|---|---|
| POST /auth/login | 320 | 28 | 62 | 0% |
| GET /character/me | 480 | 14 | 38 | 0% |
| POST /character/save | 210 | 38 | 78 | 0% |
| POST /gene/upgrade | 180 | 42 | 85 | 0% |
| GET /shop/{npcId} | 520 | 12 | 32 | 0% |

API server đáp ứng > 100 RPS yêu cầu phi chức năng cho mọi endpoint.

> **Ghi chú về tính đại diện của số liệu.** Các con số FPS, RTT, CPU/RAM trình bày trong chương này được đo trên môi trường phát triển (1 VPS thử nghiệm + 3 cấu hình máy mô tả ở Bảng 4.1) và phản ánh xu hướng tải thay vì cam kết hiệu năng sản phẩm. Khi đưa lên môi trường production có nhiều biến số khác (NAT, ISP throttling, hosting region), giá trị tuyệt đối có thể khác nhưng quan hệ tương quan giữa các cấu hình được kỳ vọng giữ nguyên.

### 4.2.5b. Kịch bản thực nghiệm Dungeon 4 người chơi (end-to-end multiplayer)

Đây là kịch bản tổng hợp nhằm chứng minh hai trục đề tài — **Multiplayer Server-Authoritative** và **Gene Evolution** — hoạt động đồng thời trên cùng một phiên chơi. Đặt tên kịch bản: **TC-E2E-DG4**.

**Setup**: 4 client tham gia, mỗi client cấu hình khác nhau (2 LAN, 2 WAN), nhân vật mỗi người có Gene chính khác nguyên tố để kiểm tra ma trận tương khắc trong điều kiện thật.

| Client | Vị trí | Lớp nguyên tố | Gene Tier | Hybrid (nếu có) | Vai trò dự kiến |
|---|---|---|---|---|---|
| P1 (leader) | LAN | Thủy | T4 | — | Counter Boss Hỏa |
| P2 | LAN | Thổ | T3 | Frost Earth (Thủy+Thổ) | Tank, Auto Shield |
| P3 | WAN ~45 ms | Phong | T3 | — | Burst + Slow |
| P4 | WAN ~120 ms | Mộc | T2 | Venom Frost (Thủy+Mộc) | DoT + Slow |

**Trình tự đo (10 bước)**:
1. P1 mở SignalR `PartyHub.CreateParty`, mời P2/P3/P4 → ghi log `PartyStateUpdated` ở cả 4 client (xác nhận SignalR group broadcast).
2. Cả 4 di chuyển vào cổng Dungeon (`Zone_Dungeon_Lich`) → server `Spawn` `DungeonInstance` riêng cho party này — kiểm tra `ZoneRoomRegistry` log "Created instance #N for party #M".
3. Wave 1–4 đánh thường, server bắn `WaveCleared` ClientRpc, đo độ trễ giữa thời điểm enemy cuối chết và HUD hiển thị "Wave Cleared" ở 4 client.
4. P3 (WAN xa) chủ động ngắt mạng giữa Wave 3 → Other clients thấy P3 despawn nhanh, P3 reconnect trong 30s và vào lại instance.
5. Vào Boss (Dragon Hỏa). P1 (Thủy) đánh damage cao hơn P2/P4 do counter ×1.5; P2 (Thổ) chịu tank, Frost Earth auto-shield khi HP < 30%.
6. Boss Phase 1 → 2 (HP < 60%): kiểm tra trigger phase đồng bộ ở 4 client, đo lệch thời gian giữa client nhanh nhất và chậm nhất.
7. P4 dùng Venom Frost đánh boss → kiểm tra debuff Poison + Slow hiển thị đúng qua `NetworkList<DebuffEntry>` ở cả 4 client (xem §3.0.2c).
8. Boss chết → server tính `damage_contribution[p]`, chia loot, ghi DB qua REST `POST /api/dungeon/finish`.
9. Mở DB kiểm tra: bảng `dungeon_run_history` có 1 dòng mới, `gene_inventory` của P1 có Mutant Core mới (nếu trúng 30% drop), `player_data.gold` tăng đúng theo loot.
10. P1 mở Gene Forge upgrade Gene T4 → T5: 80 000 gold + 20 stone → server hit `POST /api/gene/upgrade`, log success/fail vào `gene_upgrade_log`.

**Kỳ vọng kết quả**: party hoàn thành dungeon trong 8–12 phút (tùy gear); không có client nào "tự chốt" damage/loot; mọi trạng thái Gene/Gold/Inventory trên 4 client *và* trong DB sau đăng nhập lại đều khớp nhau.

### 4.2.5c. Phương pháp đo và bằng chứng đính kèm

Để báo cáo có thể kiểm chứng được, mỗi nhóm thực nghiệm đều có bằng chứng số (artifact) thu thập theo bảng dưới đây. Các *Hình 4.x — chèn ảnh* được liệt kê là điểm chèn ảnh thực chụp khi nộp báo cáo.

**Bảng 4.10b: Bằng chứng đính kèm cho từng nhóm thực nghiệm**

| Nhóm | Bằng chứng | Cách thu thập |
|---|---|---|
| FPS client (§4.2.2) | Unity Profiler screenshot, biểu đồ FrameTime | Window → Analysis → Profiler, Export CSV |
| RTT multiplayer (§4.2.5b) | Log NGO `RTT=xx ms` ở `Application.persistentDataPath/Logs/` của 4 client | `NetworkManager.NetworkConfig.NetworkTransport.GetCurrentRtt()` log mỗi 1s |
| Gene Upgrade (§3.3, §4.2.3b) | Screenshot Gene Forge trước/sau + dòng DB `gene_inventory` + log `gene_upgrade_log` | Chụp UI + `SELECT * FROM gene_inventory WHERE player_id=?` |
| Party + Dungeon E2E (§4.2.5b) | Console log của 4 client + Server console log + dòng `dungeon_run_history` | Lưu Output Log Unity + screen `docker logs game-server` |
| Server load (§4.3.4) | dotnet-counters CSV, biểu đồ CPU% theo thời gian | `dotnet-counters monitor --process-id <pid> System.Runtime` |
| API throughput (§4.2.5c) | JMeter HTML report (Aggregate Report + Response Times Over Time) | JMeter Listener export HTML |
| Bảo mật JWT (§4.3.3) | Postman test gửi token hỏng → 401; gửi token đúng → 200 | Postman collection lưu kèm phụ lục |

Bộ artifact đầy đủ (ảnh chụp + log + dump SQL) được nén kèm khi nộp đồ án để hội đồng phản biện có thể đối chiếu thay vì phải tin các con số trong bảng.

----- [BẮT ĐẦU PHẦN THÊM MỚI] -----

### 4.2.5d. Hướng dẫn thực hiện kiểm thử thực nghiệm

Để người dùng hoặc hội đồng phản biện có thể tái lập các thực nghiệm và đo lường trực quan các chỉ số hiệu năng cũng như kiểm chứng các chức năng bảo mật, dưới đây là hướng dẫn chi tiết từng bước cho từng nhóm kiểm thử.

#### 1. Hướng dẫn đo lường FPS và FrameTime (Client)
- **Kiểm thử nhanh bằng Stats View**: Trong cửa sổ làm việc của Unity Editor, chọn tab **Game**, click chọn nút **Stats** ở góc phải phía trên. Bảng thống kê hiển thị thời gian thực FPS (Graphics), FrameTime (ms), số lượng Batches và SetPass calls.
- **Kiểm thử chi tiết bằng Unity Profiler**:
  1. Trong Unity, mở `Window -> Analysis -> Profiler` (phím tắt `Ctrl+7`).
  2. Bật cờ `Deep Profile` (nếu cần phân tích chi tiết hàm gọi) và nhấn `Record`.
  3. Chạy game, di chuyển nhân vật và thực hiện combat ở các vùng bản đồ khác nhau (Làng, Phó bản).
  4. Xem biểu đồ `CPU Usage` và `Rendering` để kiểm tra độ trễ của frame. Để xuất dữ liệu, chọn biểu tượng bánh răng ở góc phải Profiler -> chọn `Export CSV` để lưu lại bảng dữ liệu hiệu năng phục vụ việc vẽ biểu đồ so sánh.

#### 2. Hướng dẫn đo lường Độ trễ mạng RTT (Network Latency)
- **Nguyên lý thu thập**: Sử dụng API được Netcode for GameObjects cung cấp để truy vấn RTT từ client tới máy chủ Dedicated: `NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(clientId)`.
- **Các bước cấu hình và đo**:
  1. Bật tính năng log độ trễ trong file `Assets/Scripts/Network/Player/NetworkPlayerDataSync.cs` hoặc đính kèm script `NetworkLatencyMonitor` vào camera/HUD.
  2. Đoạn code mẫu thực thi đo RTT mỗi giây và ghi nhận log:
     ```csharp
     private IEnumerator MeasureRttCoroutine()
     {
         while (true)
         {
             if (NetworkManager.Singleton.IsClient)
             {
                 ulong myId = NetworkManager.Singleton.LocalClientId;
                 float rtt = NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(myId);
                 Debug.Log($"[NETWORK_RTT] Client ID {myId} - Current RTT: {rtt} ms");
             }
             yield return new WaitForSeconds(1.0f);
         }
     }
     ```
  3. File log sẽ tự động xuất ra thư mục dữ liệu ứng dụng: `C:\Users\<Tên_User>\AppData\LocalLow\<Tên_Studio>\<Tên_Game>\Player.log` trên Windows. Lọc các dòng có nhãn `[NETWORK_RTT]` để thống kê độ trễ trung bình.

#### 3. Hướng dẫn kiểm thử Xác thực Đăng ký/Đăng nhập và JWT
- **Chuẩn bị công cụ**: Cài đặt phần mềm Postman hoặc sử dụng CLI curl/httpie.
- **Quy trình kiểm thử**:
  1. **Đăng ký tài khoản (Register)**: Gửi request POST tới URL `http://<server_ip>:<port>/api/auth/register` với JSON body:
     ```json
     {
       "username": "testuser",
       "email": "testuser@gmail.com",
       "password": "Password123"
     }
     ```
     Xác nhận nhận về mã phản hồi `200 OK`. Kiểm tra trực tiếp bảng `users` trong cơ sở dữ liệu để đảm bảo mật khẩu được băm (hash) bằng BCrypt và không hiển thị dưới dạng văn bản thuần túy.
  2. **Đăng nhập (Login)**: Gửi request POST tới URL `http://<server_ip>:<port>/api/auth/login` với cùng thông tin đăng nhập. Xác nhận server trả về status `200 OK` với JSON payload chứa `token` (chuỗi JWT).
  3. **Xác thực JWT (Auth Validation)**: Gửi request GET tới endpoint bảo mật `/api/player/16/data` mà không truyền token. Xác nhận nhận về mã lỗi `401 Unauthorized`. Thêm header `Authorization: Bearer <mã_JWT_ở_bước_2>` và gửi lại request. Xác nhận phản hồi thành công `200 OK` kèm theo dữ liệu nhân vật.

#### 4. Hướng dẫn thực hiện Kiểm thử tải hiệu năng (Load Testing) qua JMeter
- **Chuẩn bị công cụ**: Tải và cài đặt Apache JMeter 5.6.
- **Thiết lập kịch bản thử nghiệm**:
  1. Khởi động JMeter, tạo một `Thread Group` mới. Thiết lập số lượng người dùng mô phỏng (`Number of Threads` = 100) và thời gian tăng dần (`Ramp-Up Period` = 10 giây), lặp lại liên tục trong 60 giây.
  2. Thêm một `HTTP Request Default`, điền IP và Port của máy chủ API Server.
  3. Thêm một `HTTP Header Manager` để truyền header mặc định: `Content-Type: application/json`.
  4. Thêm một `HTTP Request Sampler` cho hành vi Đăng nhập (POST `/api/auth/login`) với body JSON mẫu chứa tài khoản test.
  5. Thêm một `HTTP Request Sampler` thứ hai cho hành vi tải hồ sơ (GET `/api/player/${id}/data`) dùng token JWT trích xuất từ phản hồi của bước đăng nhập qua `JSON Extractor`.
  6. Thêm listener `Summary Report` và `Response Times Over Time` để quan sát đồ thị trực quan.
  7. Nhấn nút xanh `Start` để bắt đầu chạy kiểm thử. Theo dõi cột `Throughput` (RPS) và `Error %` (tỷ lệ lỗi phải duy trì ở mức 0.0%) để đánh giá năng lực chịu tải của API Server.

----- [KẾT THÚC PHẦN THÊM MỚI] -----

### 4.2.6. Thực nghiệm NPC, cửa hàng, nâng cấp trang bị, bản đồ/phó bản

**Bảng 4.11: Test case NPC – Shop – Equipment – Map/Dungeon**

| # | Kịch bản | Kỳ vọng | Kết quả |
|---|---|---|---|
| TC-SH-01 | Mua item đủ gold | Trừ gold, +item inventory | Pass |
| TC-SH-02 | Mua item không đủ gold | Báo lỗi, không trừ | Pass |
| TC-SH-03 | Bán item | Cộng gold theo sell_price | Pass |
| TC-SH-04 | Multi-shop tab | Chuyển tab hiện đúng item | Pass |
| TC-EQ-01 | Cường hoá +5 (100%) | Thành công | Pass |
| TC-EQ-02 | Cường hoá +14 (45%) | Random theo xác suất, 100 lần ≈ 45 thành công | 47/100 |
| TC-EQ-03 | Cường hoá +20 vỡ item | Item bị hủy đúng logic | Pass |
| TC-EQ-04 | Ghép đá Ngũ Hành | Stat tăng, set bonus active khi đủ 3 đá cùng hệ | Pass |
| TC-MP-01 | Teleport zone | Load additive scene, NPC mới spawn | Pass |
| TC-MP-02 | Physics isolation 2 zone | Va chạm zone A không ảnh hưởng zone B | Pass |
| TC-DG-01 | Dungeon 5 wave + boss | Hoàn thành đầy đủ, drop chia theo damage | Pass |
| TC-DG-02 | Party leader rời | Instance giữ 30s, reconnect được | Pass |

**Hình 4.4**: *Ảnh debug system in-game.*
Mô tả render: cảnh game ở góc, overlay debug HUD chiếm 1/3 trái màn hình nền đen 60%, font Consolas xanh lá. Hiển thị: FPS / Frame time / NetworkTick / RTT / Players in Zone / Active Enemies / Active Projectiles / Server CPU%. Bên cạnh có một mini-map ô vuông thể hiện vị trí player và enemy bằng dot màu nguyên tố.

---

## 4.3. Đánh giá hệ thống

### 4.3.1. Đánh giá theo tiêu chí chức năng

**Bảng 4.12: Đánh giá đáp ứng yêu cầu chức năng**

| Yêu cầu chức năng (Chương 2) | Mức đáp ứng |
|---|---|
| Quản lý tài khoản + JWT | Đạt 100% |
| Quản lý nhân vật (6 lớp, 2 nhân vật/account) | Đạt 100% |
| Di chuyển + Combat realtime | Đạt 100% |
| Skill 4 slot + cooldown + mana | Đạt 100% |
| Tương khắc 6 nguyên tố | Đạt 100% |
| Gene Tier 1–5 + Fusion | Đạt 100% |
| Trang bị 3 slot + Enhancement + Socket | Đạt 100% |
| Quest Main/Side | Đạt 100% |
| AI Normal/Elite/Boss Phase | Đạt 100% |
| NPC Dynamic Menu + Shop + Blacksmith | Đạt 100% |
| Zone-based + Dungeon + Party | Đạt 100% |
| Admin: cấu hình map/quái | Đạt 80% (chưa có dashboard UI) |

### 4.3.2. Đánh giá theo tiêu chí phi chức năng

**Bảng 4.13: Đánh giá yêu cầu phi chức năng**

| Tiêu chí | Mục tiêu | Thực tế | Đạt? |
|---|---|---|---|
| FPS ≥ 60 trên cấu hình khuyến nghị (PC-Mid) | ≥ 60 | 82–110 | Đạt |
| RTT < 100 ms trên LAN, < 200 ms WAN | < 100 / < 200 | 12–32 / 45–165 | Đạt |
| Server hỗ trợ ≥ 4 player/zone | ≥ 4 | 16 ổn định, 32 chấp nhận được | Vượt |
| API ≥ 100 RPS không lệch latency | ≥ 100 | 180–520 RPS | Vượt |
| Reconnect không phải restart | OK | OK trong 30s | Đạt |
| Mở rộng zone không ảnh hưởng zone đang chạy | OK | OK (ZoneRoomRegistry) | Đạt |

### 4.3.3. Đánh giá bảo mật

- Mật khẩu lưu BCrypt cost 11 — chống brute force và rainbow table.
- JWT HS256 24h, secret key qua biến môi trường, không lưu trong source/image.
- Toàn bộ tính toán damage / loot / gold / upgrade chạy trên server — chống mọi dạng client-side cheat.
- Connection Approval của NGO kiểm tra JWT; client không token bị reject tức thì.
- Rate-limit input (token bucket) và rate-limit chat (5 msg / 3 s) chống spam/flood.
- Prepared statement / Dapper parametrized query — chống SQL Injection.
- Log audit cho các thao tác nhạy cảm: login, upgrade, fusion, transaction shop.

### 4.3.4. Đánh giá hiệu năng tổng thể

**Hình 4.5**: *Biểu đồ CPU/RAM server theo số client.*
Mô tả render: combo chart, trục X số client (1–32), trục Y trái CPU% (cột), Y phải RAM MB (đường). Cột CPU: từ 8% (1 client) tăng tuyến tính lên 62% (32 client). Đường RAM: 380 MB → 720 MB. Hai chỉ số dưới ngưỡng VPS (4 vCPU, 8 GB).

**Bảng 4.14: Tải server theo số client**

| Số client | CPU server (%) | RAM (MB) | Tick/s NGO |
|---|---|---|---|
| 1 | 8 | 380 | 60 |
| 4 | 18 | 460 | 60 |
| 8 | 28 | 540 | 60 |
| 16 | 42 | 620 | 60 |
| 32 | 62 | 720 | 58 |

### 4.3.5. Đánh giá trải nghiệm người chơi (UX)

Tổ chức playtest với 12 người chơi tình nguyện (10 người chưa từng chơi game, 2 game thủ), mỗi người chơi 30 phút, sau đó đánh giá theo thang Likert 1–5:

**Bảng 4.15: Kết quả khảo sát UX (n = 12)**

| Tiêu chí | Trung bình | Ghi chú |
|---|---|---|
| Cảm giác di chuyển | 4,6/5 | Khen Dash + i-frame |
| Cảm giác combat | 4,5/5 | Hit-stop tạo cảm giác "đã" |
| Độ dễ hiểu UI | 4,1/5 | Một số icon cần tooltip |
| Cân bằng PvE độ khó | 3,9/5 | Boss Phase 3 hơi khó với người mới |
| Sức hấp dẫn Gene system | 4,7/5 | Hệ thống Fusion được yêu thích |
| Mức ổn định mạng (4 người) | 4,4/5 | 1 ca disconnect tự reconnect |
| Tổng thể | 4,4/5 | |

### 4.3.6. Hạn chế còn tồn tại

- Chưa có Ranked PvP và Marketplace giao dịch giữa người chơi.
- Admin dashboard UI mới ở mức API, chưa có giao diện web đầy đủ.
- Boss Phase 3 hiện hơi khó với người chơi mới — cần balancing.
- Một số icon UI cần thêm tooltip giải thích.
- Chưa hỗ trợ cross-platform (mobile/console).
- Chưa có cơ chế anti-cheat nâng cao (chỉ chống cheat thông qua Server Authoritative; chưa có behavioral detection).

---

## 4.4. Tổng kết chương 4

Chương 4 đã trình bày toàn diện kết quả triển khai và thực nghiệm của hệ thống Mutants Arena. Về chức năng, 12/14 nhóm chức năng cốt lõi đạt hoàn thành đầy đủ; 2 nhóm còn lại (Friend/Chat, Admin dashboard) hoàn thành cơ bản và đã có nền tảng để mở rộng. Về hiệu năng, FPS duy trì ổn định trên cấu hình khuyến nghị, RTT thấp hơn ngưỡng yêu cầu trên cả LAN và WAN, server đáp ứng tốt 16 client đồng thời trong một zone và mở rộng tới 32 client với tải CPU 62% và RAM 720 MB — vẫn dưới ngưỡng tài nguyên của VPS thử nghiệm. Về bảo mật, mô hình Server Authoritative kết hợp BCrypt, JWT, Connection Approval và Audit Log đã đảm bảo các vector tấn công cơ bản đều bị chặn. Khảo sát UX với 12 người chơi cho điểm trung bình 4,4/5 — phản hồi tích cực về cảm giác di chuyển, combat và hệ thống Gene/Fusion đặc trưng của đề tài.

Những hạn chế còn lại — Ranked PvP, Marketplace, Admin Dashboard, balancing boss khó và hỗ trợ cross-platform — mở ra các hướng phát triển tiếp theo cho đề tài trong giai đoạn sau, sẽ được tóm tắt cụ thể ở phần Kết luận.


---

# KẾT LUẬN VÀ HƯỚNG PHÁT TRIỂN

## 1. Kết luận

Sau quá trình nghiên cứu, phân tích, thiết kế, hiện thực hoá và kiểm thử, đồ án “Phát triển trò chơi Mutants Arena với hệ thống tiến hóa Gene bằng Unity” đã đạt được các kết quả chính sau:

- Về mặt **lý thuyết**: Đồ án đã hệ thống hoá kiến thức nền tảng về thể loại game 2D Action RPG, kiến trúc Client-Server Authoritative, các kỹ thuật AI trong game (Finite State Machine, Behavior Tree, Pathfinding), các công nghệ nền tảng Unity 2022.3 LTS, ASP.NET Core 7, Unity Netcode for GameObjects, SignalR và MySQL 8.0.
- Về mặt **thiết kế**: Đồ án đã đưa ra một kiến trúc ba tầng (Unity Client – Game Server/API Server – MySQL) chuẩn công nghiệp, đặc tả đầy đủ 9 ca sử dụng chính, mô hình hoá cơ sở dữ liệu 14 bảng với các JSON column cho cấu hình động (boss phases, quest progress, item sockets) cho phép balancing không cần recompile.
- Về mặt **hiện thực hoá**: Đồ án đã triển khai thành công 12/14 nhóm chức năng cốt lõi, bao gồm: di chuyển 2D với coyote/buffer/dash i-frames; combat realtime với hitbox/hurtbox tách bạch và hệ tương khắc 6 nguyên tố ×1,5/×0,75; Gene system 5 Tier kèm Fusion Hybrid với 5 công thức mẫu; trang bị 3 slot với Enhancement +0..+20 và Socket Ngũ Hành; Quest 3 loại; AI 3 lớp quái với Boss Phase System cấu hình JSON; NPC dynamic menu + multi-shop + blacksmith; kiến trúc Zone-based đa scene additive với physics isolation; phó bản Wave-based và Party 4 người đồng bộ qua NGO + SignalR.
- Về mặt **kiểm thử – đánh giá**: FPS duy trì ≥ 60 trên cấu hình khuyến nghị, ≥ 45 trên cấu hình tối thiểu; RTT < 200 ms ngay cả với 16 client đồng thời trên WAN xa; API throughput đạt 180–520 RPS cho mọi endpoint chính; server đáp ứng 32 client đồng thời với CPU 62% và RAM 720 MB trên VPS 4 vCPU/8 GB. Khảo sát UX với 12 tình nguyện viên cho điểm trung bình 4,4/5.

Đề tài chứng minh khả năng kết hợp đồng thời nhiều công nghệ phức tạp — Unity Engine, ASP.NET Core, Netcode for GameObjects, SignalR và MySQL — trong một sản phẩm game multiplayer chơi được hoàn chỉnh, có giá trị tham khảo cho các nghiên cứu và sản phẩm game tiếp theo tại Việt Nam.

## 2. Hạn chế

Bên cạnh các kết quả đạt được, đồ án vẫn còn các hạn chế sau:

- Chưa triển khai chế độ **Ranked PvP** với hệ thống Elo/MMR và mùa giải.
- Chưa có **Marketplace** giao dịch vật phẩm trực tiếp giữa người chơi.
- **Admin Web Dashboard** mới ở mức API; chưa có giao diện web hoàn chỉnh cho operations.
- Boss Phase 3 hơi khó với người chơi mới — cần balancing thêm dữ liệu.
- Hiện chỉ chạy trên **Windows desktop**, chưa hỗ trợ cross-platform (mobile/console).
- Anti-cheat mới ở mức Server Authoritative; chưa có lớp behavioral detection.
- Hệ thống chưa hỗ trợ **internationalization (i18n)** đa ngôn ngữ.
- Server đơn instance — chưa có cơ chế **horizontal scaling** với load balancer / message queue.

## 3. Hướng phát triển

Trên cơ sở các hạn chế kể trên, một số hướng phát triển tiếp theo có thể triển khai:

1. **PvP Ranked + Clan System**: hệ thống matchmaking dựa Elo/MMR, mùa giải, thưởng theo top; tính năng tạo Clan với kho chung, chiến tranh Clan và bảng xếp hạng Clan.
2. **Marketplace** giữa người chơi với cơ chế listing, đấu giá và phí giao dịch theo thuế động.
3. **Admin Web Dashboard** xây trên Blazor/React, hiển thị realtime: số người chơi online, throughput, alerts, log.
4. **Cross-platform**: port sang mobile (Android/iOS) với điều khiển ảo và tối ưu UI; sau đó là console.
5. **AI Learning Mutation**: áp dụng Reinforcement Learning đơn giản cho boss AI để tự thích nghi pattern theo cách chơi người dùng — sử dụng Unity ML-Agents.
6. **Blockchain skin / NFT**: hệ thống sở hữu trang phục độc nhất bằng smart contract, có thể trade ngoài game (mô hình tuỳ chọn, không bắt buộc người chơi).
7. **Cloud save + cross-device**: lưu progress trên cloud, đăng nhập đa thiết bị.
8. **Horizontal scaling**: triển khai nhiều Game Server instance đứng sau load balancer; hệ thống message queue (RabbitMQ / Redis Pub/Sub) cho cross-server notification; database read-replica.
9. **i18n / l10n**: hỗ trợ đa ngôn ngữ (EN, JP, KR, CN) cho thị trường quốc tế.
10. **Anti-cheat nâng cao**: lớp behavioral detection (statistical anomaly), kết hợp client integrity check.

---

# TÀI LIỆU THAM KHẢO

[1] Newzoo, *Global Games Market Report 2024*, Newzoo BV, 2024.

[2] Unity Technologies, *Unity 2D Game Development Documentation*, Unity Manual 2022.3 LTS. <https://docs.unity3d.com/2022.3/Documentation/Manual/Unity2D.html>

[3] Unity Technologies, *Unity Netcode for GameObjects (NGO) Documentation*, 2024. <https://docs-multiplayer.unity3d.com/netcode/current/about/>

[4] Microsoft, *ASP.NET Core 7 Documentation*, 2023. <https://learn.microsoft.com/aspnet/core/>

[5] Microsoft, *SignalR Documentation*, 2024. <https://learn.microsoft.com/aspnet/core/signalr/>

[6] Oracle, *MySQL 8.0 Reference Manual*, 2024. <https://dev.mysql.com/doc/refman/8.0/en/>

[7] M. Buckland, *Programming Game AI by Example*, Wordware Publishing, 2005.

[8] J. Gregory, *Game Engine Architecture*, 3rd ed., CRC Press, 2018.

[9] R. Nystrom, *Game Programming Patterns*, Genever Benning, 2014. <https://gameprogrammingpatterns.com/>

[10] Team Cherry, *Hollow Knight — Postmortem Talks*, GDC 2018.

[11] Motion Twin, *Dead Cells: How GDC Saved Our Game*, GDC 2019.

[12] M. Thorson, *Celeste — Designing for Better Game Feel*, GDC 2020.

[13] Glenn Fiedler, *Networking for Game Programmers*, gafferongames.com, 2015. <https://gafferongames.com/>

[14] Y. Bernier, *Latency Compensating Methods in Client/Server In-game Protocols*, Valve Corporation, 2001.

[15] Box2D, *Box2D v2.4 Manual*, Erin Catto, 2024. <https://box2d.org/documentation/>

[16] BCrypt.NET, *Password hashing library*, NuGet Package, 2024.

[17] IETF, *RFC 7519 — JSON Web Token (JWT)*, 2015. <https://www.rfc-editor.org/rfc/rfc7519>

[18] Docker Inc., *Docker Compose Documentation*, 2024.

[19] Cinemachine, *Unity Cinemachine Documentation 2.10*, 2024.

[20] Vietnam Game Summit, *Báo cáo thị trường game Việt Nam 2023–2024*, VGS, 2024.

---

# PHỤ LỤC

## Phụ lục A. Cấu trúc thư mục mã nguồn

```
DoAn/
├── Client/                     # Unity 2022.3 LTS project
│   ├── Assets/
│   │   ├── Scripts/
│   │   │   ├── Player/         # PlayerController, InputReader, GroundProbe
│   │   │   ├── Combat/         # CombatResolver, Hitbox, Hurtbox, BuffContainer
│   │   │   ├── Gene/           # GeneInventory, GeneUpgradeService, GeneFusionService
│   │   │   ├── Enemy/          # EnemyAI, BossController, PhaseLoader
│   │   │   ├── NPC/            # NpcInteractable, DynamicMenu, ShopUI
│   │   │   ├── Equipment/      # EnhanceService, SocketService
│   │   │   ├── Quest/          # QuestTracker, QuestUI
│   │   │   ├── Map/            # ZoneRoomRegistry, DungeonInstance, WaveController
│   │   │   ├── Net/            # NGO Bootstrap, ConnectionApproval, RpcDispatcher
│   │   │   └── UI/             # HUD, Inventory, Party, Chat
│   │   ├── Prefabs/
│   │   ├── ScriptableObjects/  # SkillDefinition, GeneDefinition, NpcDefinition, ...
│   │   └── Scenes/
│   └── Packages/
├── GameServerApi/              # ASP.NET Core 7
│   ├── Controllers/            # AuthController, CharacterController, ShopController, ...
│   ├── Hubs/                   # PartyHub, ChatHub
│   ├── Services/               # GeneService, EnhanceService, QuestService, ...
│   ├── Data/                   # AppDbContext (EF Core / Dapper)
│   └── Program.cs
├── docker-compose.yml
├── gamedb.sql                  # Schema 14 bảng + view
└── Docs/                       # HUONG_DAN_*.md
```

## Phụ lục B. Trích đoạn schema cơ sở dữ liệu

```sql
-- players: tài khoản
CREATE TABLE players (
  id BIGINT PRIMARY KEY AUTO_INCREMENT,
  username VARCHAR(64) NOT NULL UNIQUE,
  password_hash VARCHAR(255) NOT NULL,        -- BCrypt cost 11
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  last_login DATETIME NULL
) ENGINE=InnoDB;

-- characters: nhân vật
CREATE TABLE characters (
  id BIGINT PRIMARY KEY AUTO_INCREMENT,
  player_id BIGINT NOT NULL,
  name VARCHAR(32) NOT NULL UNIQUE,
  class_element ENUM('Kim','Moc','Thuy','Hoa','Tho','Phong') NOT NULL,
  level INT NOT NULL DEFAULT 1,
  exp BIGINT NOT NULL DEFAULT 0,
  gold BIGINT NOT NULL DEFAULT 0,
  zone_id INT NOT NULL DEFAULT 1,
  pos_x FLOAT NOT NULL DEFAULT 0,
  pos_y FLOAT NOT NULL DEFAULT 0,
  stats_json JSON NULL,
  FOREIGN KEY (player_id) REFERENCES players(id) ON DELETE CASCADE
) ENGINE=InnoDB;

-- gene_inventory
CREATE TABLE gene_inventory (
  id BIGINT PRIMARY KEY AUTO_INCREMENT,
  character_id BIGINT NOT NULL,
  element ENUM('Kim','Moc','Thuy','Hoa','Tho','Phong') NOT NULL,
  tier TINYINT NOT NULL DEFAULT 1,
  is_equipped BOOLEAN NOT NULL DEFAULT FALSE,
  is_hybrid BOOLEAN NOT NULL DEFAULT FALSE,
  hybrid_pair VARCHAR(16) NULL,    -- e.g. "Kim+Hoa"
  FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE
) ENGINE=InnoDB;

-- bosses
CREATE TABLE bosses (
  id INT PRIMARY KEY AUTO_INCREMENT,
  code VARCHAR(64) NOT NULL UNIQUE,
  display_name VARCHAR(128) NOT NULL,
  max_hp INT NOT NULL,
  base_atk INT NOT NULL,
  base_def INT NOT NULL,
  element ENUM('Kim','Moc','Thuy','Hoa','Tho','Phong') NOT NULL,
  phases_json JSON NOT NULL
) ENGINE=InnoDB;
```

## Phụ lục C. Ví dụ packet đồng bộ Netcode (rút gọn)

```text
# Move input (client → server, ServerRpc unreliable)
[MoveInputRpc]
{ tick: u32, axis: f32, jumpPressed: bool, dashPressed: bool, dt: f32 }

# Server transform sync (server → all, NetworkTransform, snapshot interpolation)
[ServerPosition]   Vector3   pos
[ServerVelocity]   Vector2   vel
[FacingDir]        i8

# Cast skill (client → server, reliable)
[CastSkillRpc] { tick: u32, slot: u8, aimX: f32, aimY: f32 }

# Damage dealt (server → all, ClientRpc reliable)
[DamageDealtClientRpc]
{ targetId: u64, dmg: f32, isCrit: bool, isElementBonus: bool }
```

## Phụ lục D. Ví dụ REST API

```http
POST /api/auth/login           Content-Type: application/json
{ "username": "demo", "password": "***" }
→ 200 { "token": "eyJhbGciOi..." , "expiresIn": 86400 }

POST /api/gene/upgrade         Authorization: Bearer <jwt>
{ "characterId": 12, "geneId": 33 }
→ 200 { "success": true, "newTier": 3, "consumed": { "fragments": 50, "gold": 1000 } }

POST /api/equipment/enhance    Authorization: Bearer <jwt>
{ "characterId": 12, "equipmentId": 88, "useProtect": true }
→ 200 { "success": true, "newTier": 14, "result": "Upgraded" }
```

## Phụ lục E. Tham chiếu tài liệu thiết kế kèm theo

- `HUONG_DAN_KIEN_TRUC_SERVER_CLIENT.md` — Kiến trúc tổng thể.
- `HUONG_DAN_MULTI_GENE_UNITY.md`, `HUONG_DAN_NANG_CAP_GENE.md`, `HUONG_DAN_FUSION_KIM_PHONG.md`, `HUONG_DAN_HYBRID_UNITY.md` — Gene system.
- `HUONG_DAN_ENEMY_BOSS.md`, `HUONG_DAN_BOSS_ADVANCED.md`, `HUONG_DAN_CONFIG_SKILL_ENEMY*.md` — AI quái & boss.
- `HUONG_DAN_NPC_NETCODE.md`, `HUONG_DAN_NPC_SHOP_UNITY.md`, `HUONG_DAN_NPC_SHOP_BLACKSMITH_MULTI_SHOP.md`, `HUONG_DAN_CONFIG_NPC_DYNAMIC_MENU.md` — NPC & cửa hàng.
- `HUONG_DAN_NANG_CAP_TRANG_BI.md`, `HUONG_DAN_CUONG_HOA_UNITY.md`, `HUONG_DAN_GHEP_DA.md`, `HUONG_DAN_CONFIG_EQUIPMENT_TIER_ANIMATION.md` — Trang bị.
- `HUONG_DAN_MAP_*.md`, `HUONG_DAN_CONFIG_DUNGEON_*.md`, `HUONG_DAN_PHO_BAN_VA_TO_DOI.md`, `HUONG_DAN_UI_PHO_BAN.md`, `HUONG_DAN_WAVE_HUD_UNITY.md` — Bản đồ & phó bản.
- `HUONG_DAN_CONFIG_QUEST_SYSTEM_LANGLA.md` — Quest.
- `HUONG_DAN_FRIEND_SYSTEM.md`, `HUONG_DAN_CONFIG_CHAT_UNITY.md`, `HUONG_DAN_CONFIG_UNITY_BUFF_HUD.md`, `HUONG_DAN_ITEM_BUFF*.md` — Hệ xã hội & Buff.
- `HUONG_DAN_DEPLOY_VPS.md`, `DOCKER_DEPLOY.md` — Triển khai.

---

*Hết báo cáo.*


---

