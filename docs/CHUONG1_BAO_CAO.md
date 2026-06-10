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
