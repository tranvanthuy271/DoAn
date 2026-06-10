# Slide 1: PHÁT TRIỂN TRÒ CHƠI MUTANTS ARENA VỚI HỆ THỐNG TIẾN HÓA GENE BẰNG UNITY
## ĐỒ ÁN TỐT NGHIỆP ĐẠI HỌC

- Sinh viên thực hiện: **Trần Văn Thủy**
- Mã sinh viên: **CT060439**
- Lớp: **AT16D**
- Người hướng dẫn khoa học: **TS. Nguyễn Đức Hiếu**
- Khoa Công nghệ thông tin – Học viện Kỹ thuật Mật mã
- Năm thực hiện: 2026



---



# Slide 2: Thách thức, Giải pháp & Kết quả
## Thách thức, Giải pháp & Kết quả

<div style="text-align: center; width: 100%;">

![slide_summary_infographic.png](../extracted_images/slide_summary_infographic.png)

</div>



---



# Slide 3: Nội dung báo cáo
## Mục lục

- **Chương 1: Tổng quan về đề tài và Cơ sở lý thuyết**
  - Khái quát thể loại 2D Action RPG, khảo sát game tham chiếu bao gồm Hollow Knight, Dead Cells, Celeste, MapleStory và Làng Lá, cơ sở lý thuyết về di chuyển, AI quái vật FSM và kiến trúc mạng.
- **Chương 2: Phân tích và Thiết kế hệ thống**
  - Thiết kế kiến trúc tổng thể 3 tầng, đặc tả Use Case chi tiết, và thiết kế cơ sở dữ liệu vật lý ERD.
- **Chương 3: Xây dựng các cơ chế game**
  - Chi tiết lập trình cơ chế di chuyển platformer như Coyote time và Jump buffer, ma trận tương khắc Ngũ Hành, hệ thống Gene 5 Tier & Hybrid Fusion, AI FSM quái vật, đồng bộ Client-side Prediction và Zone-based Server.
- **Chương 4: Kết quả và Thực nghiệm**
  - Kết quả triển khai VPS Linux qua Docker Compose, cơ chế bảo mật gồm JWT, Connection Approval và API Key, giao diện thực tế và đánh giá hiệu năng mạng/FPS.



---



# Slide 4: Chương 1: Tổng quan và Cơ sở lý thuyết
## Chương 1 giới thiệu

<div style="display: flex; gap: 20px; align-items: center;">
<div style="flex: 1.2;">

- **Tổng quan game 2D Action RPG**: Tìm hiểu lịch sử, xu hướng phát triển và sự bùng nổ của thể loại hành động nhập vai màn hình ngang.
- **Khảo sát sản phẩm tiêu biểu**: Phân tích bài học thiết kế từ các tựa game nổi tiếng như Hollow Knight, Dead Cells, Celeste, MapleStory, Làng Lá.
- **Cơ sở lý thuyết nền tảng**:
  - *Gameplay*: Hệ thống di chuyển platformer và trí tuệ nhân tạo quái vật FSM.
  - *Mạng & Bảo mật*: Kiến trúc Server-Authoritative đồng bộ realtime qua UDP, xác thực JWT và ảo hóa Docker.

</div>
<div style="flex: 0.8; text-align: center;">

![image_unnamed_3.png](../extracted_images/image_unnamed_3.png)

</div>
</div>



---



# Slide 5: Khái niệm 2D Side-Scrolling Action RPG
## Định nghĩa & Đặc trưng hệ thống

<div style="display: flex; gap: 20px; align-items: center;">
<div style="flex: 1.2; text-align: justify; line-height: 1.6;">
Thể loại game **2D Side-Scrolling Action RPG** hay hành động nhập vai màn hình ngang là sự kết hợp hài hòa giữa cơ chế điều khiển phản xạ thời gian thực trên bản đồ di chuyển đa nền tảng platformer cùng chiều sâu phát triển nhân vật thông qua hệ thống chỉ số, trang bị và kỹ năng.

</div>
<div style="flex: 0.8; text-align: center;">

![generic_2d_action_rpg.png](../extracted_images/generic_2d_action_rpg.png)

</div>
</div>



---



# Slide 6: Khảo sát game tham chiếu và Bài học thiết kế
## Bài học thiết kế game

<div style="text-align: center; width: 100%;">

![survey_games_comparison.png](../extracted_images/survey_games_comparison.png)

</div>



---



# Slide 7: Cơ sở lý thuyết di chuyển Platformer & AI FSM
## Lý thuyết di chuyển & Trí tuệ nhân tạo

<div style="display: flex; gap: 20px; align-items: center;">
<div style="flex: 1.2;">

- **Vật lý di chuyển 2D Box2D**: Sử dụng các lực gia tốc, vận tốc ngang kết hợp điều chỉnh trọng lực rơi lớn hơn trọng lực bay lên qua cơ chế Variable Gravity Jump tạo cảm giác điều khiển mượt mà, chân thực.
- **Ground Detection**: Sử dụng OverlapCircle phát hiện chân chạm đất để cấp quyền nhảy.
- **Finite State Machine FSM cho quái vật**:
  - AI quái vật được mô hình hóa thành các trạng thái rời rạc: *Idle/Patrol* tuần tra, *Chase* đuổi theo người chơi, *Attack* tấn công khi đủ gần, và *Hit/Dead* nhận sát thương hoặc chết giải phóng tài nguyên.
  - Giúp quản lý logic AI đơn giản, dễ sửa lỗi và tối ưu hóa tài nguyên CPU cho máy chủ.

</div>
<div style="flex: 0.8; text-align: center;">

![image_unnamed_2.png](../extracted_images/image_unnamed_2.png)

</div>
</div>



---



# Slide 8: Phát triển nhân vật & Tiến hóa Gene
## Lý thuyết phát triển nhân vật & Tiến hóa Gene

<div style="display: flex; gap: 20px; align-items: center;">
<div style="flex: 1.2;">

- **Khái niệm tiến hóa Gene**: Thay thế cơ chế chọn lớp nhân vật truyền thống, cho phép nhân vật tự do phát triển thông qua việc mở khóa và nâng cấp các nhánh Gene thuộc tính ngũ hành.
- **Cơ chế Multi-Gene**: Hỗ trợ khảm đồng thời 1 Gene chính nhận 100% hiệu năng chỉ số và kỹ năng cùng Gene phụ nhận 30% chỉ số cộng thêm, tạo nên các hướng build cực kỳ đa dạng.
- **Dung hợp Hybrid Fusion**: Khi đạt cấp độ tối đa ở cả Gene chính và Gene phụ thuộc hai nguyên tố khác nhau, người chơi có thể tiến hành dung hợp để mở khóa lớp nhân vật lai với sức mạnh vượt trội.
- **Gene tối thượng**: Cấp độ tiến hóa cao nhất khi dung hợp thành công các Gene lai đạt phẩm chất cực hạn, mở khóa các kỹ năng nộ chủ động diện rộng và thay đổi cục diện trận đấu.

</div>
<div style="flex: 0.8; text-align: center;">

![gene_evolution_theory.png](../extracted_images/gene_evolution_theory.png)

</div>
</div>



---


# Slide 9: Kiến trúc mạng Server-Authoritative
## Nguyên lý vận hành & Luồng xử lý dữ liệu

<div style="display: flex; gap: 20px; align-items: center;">
<div style="flex: 1.2;">

- **Nguyên lý Server-Authoritative**: Dedicated Server chạy độc lập trên máy chủ ảo là nguồn dữ liệu duy nhất đáng tin cậy. Client chỉ gửi lệnh điều khiển và nhận dữ liệu trạng thái đồng bộ về.
- **Luồng xử lý hành động**:
  1. Client gửi yêu cầu điều khiển thông qua ServerRpc lên Server.
  2. Server thực hiện tính toán vật lý, kiểm tra tính hợp lệ như năng lượng, thời gian hồi chiêu và va chạm hitbox.
  3. Server cập nhật chỉ số máu và vị trí nhân vật, sau đó phát gói tin ClientRpc đồng bộ tới các client trong vùng.
- **Khả năng chống gian lận**: Client không thể tự ý thay đổi máu, vàng, sát thương hoặc bay nhảy tự do vì Server liên tục xác thực mọi hành vi.

</div>
<div style="flex: 0.8; text-align: center;">

![image_unnamed_3.png](../extracted_images/image_unnamed_3.png)

</div>
</div>


---

# Slide 10: Cơ sở lý thuyết kiến trúc mạng Game & Công nghệ
## Kiến trúc mạng & Công nghệ sử dụng

<div style="text-align: center; width: 100%;">

![network_and_tech_stack.png](../extracted_images/network_and_tech_stack.png)

</div>

---



# Slide 11: Chương 2: Phân tích và Thiết kế hệ thống
## Chương 2 giới thiệu

- Chương 2 tập trung đặc tả yêu cầu bài toán yêu cầu chức năng và phi chức năng, thiết kế kiến trúc hệ thống 3 tầng tổng thể, xây dựng các biểu đồ Use Case chi tiết, và sơ đồ cơ sở dữ liệu vật lý ERD.



---



# Slide 12: Mô hình kiến trúc tổng thể hệ thống
## Kiến trúc 3 lớp

<div style="display: flex; gap: 20px; align-items: center;">
<div style="flex: 1.2;">

- **Tầng Client Unity Client**: Xây dựng trên ngôn ngữ C#, chịu trách nhiệm render hình ảnh, hoạt ảnh nhân vật, xử lý các dự đoán di chuyển Client-side prediction và gửi input điều khiển mạng lên Server.
- **Tầng Server**:
  - *Dedicated Server Unity Headless*: Chạy logic vật lý, va chạm, AI FSM quái vật, tính toán sát thương thời gian thực.
  - *API & SignalR Server ASP.NET Core 8*: Xác thực JWT, lưu trữ tiến trình qua EF Core, quản lý sảnh chờ, kênh chat và tổ đội.
- **Tầng Database MySQL Database**: Lưu trữ thông tin tài khoản, nhân vật, túi đồ, trang bị và cấu hình hệ thống.

</div>
<div style="flex: 0.8; text-align: center;">

![image_unnamed_7.png](../extracted_images/image_unnamed_7.png)

</div>
</div>



---

# Slide 13: Biểu đồ Use Case mức tổng quát
## Tác nhân và Ca sử dụng

<div style="text-align: center; width: 100%;">

![image_unnamed_8.png](../extracted_images/image_unnamed_8.png)

</div>

---

# Slide 14: Use Case chi tiết: Chiến đấu & Kỹ năng
## Đặc tả ca sử dụng Chiến đấu & Kỹ năng

<div style="text-align: center; width: 100%;">

![image_unnamed_13.png](../extracted_images/image_unnamed_13.png)

</div>

---

# Slide 15: Use Case chi tiết: Tiến hóa Gene Ngũ Hành
## Đặc tả ca sử dụng Tiến hóa Gene

<div style="text-align: center; width: 100%;">

![image_unnamed_16.png](../extracted_images/image_unnamed_16.png)

</div>

---

# Slide 16: Use Case chi tiết: Tham gia phó bản Wave
## Đặc tả ca sử dụng Tham gia phó bản

<div style="text-align: center; width: 100%;">

![image_unnamed_22.png](../extracted_images/image_unnamed_22.png)

</div>

---


# Slide 17: Sơ đồ Cơ sở dữ liệu vật lý ERD
## Thiết kế CSDL & Cột JSON linh hoạt

<div style="display: flex; gap: 20px; align-items: center;">
<div style="flex: 1.2;">

- **Sơ đồ thực thể ERD**: Gồm các bảng liên kết chặt chẽ như `Users` tài khoản, `Player_Data` chỉ số nhân vật, `Inventory` vật phẩm, `Gene_Data` dữ liệu Gene.
- **Thiết kế JSON Column linh hoạt**:
  - Lưu trữ trực tiếp các dữ liệu có cấu trúc động như danh sách chỉ số phụ trang bị cường hóa ngẫu nhiên, trạng thái nhiệm vụ đang nhận, danh sách kỹ năng nhanh.
  - Tối ưu hóa: Tránh tạo quá nhiều bảng liên kết 1-nhiều phức tạp, giảm thiểu số lượng truy vấn `JOIN` lớn, giúp tăng tốc độ đọc/ghi dữ liệu nhân vật lên đến 40%.

</div>
<div style="flex: 0.8; text-align: center;">

![database_erd.png](../extracted_images/database_erd.png)

</div>
</div>


---



# Slide 18: Chương 3: Xây dựng các cơ chế game
## Chương 3 giới thiệu

- Chương 3 trình bày chi tiết quá trình lập trình và xây dựng các cơ chế trò chơi cốt lõi trên Unity và Dedicated Server bao gồm: điều khiển nhân vật platformer, tương khắc ngũ hành, hệ thống tiến hóa Gene, trí tuệ nhân tạo quái vật và cơ chế đồng bộ mạng dự đoán giảm độ trễ di chuyển.



---



# Slide 19: Xây dựng hệ thống điều khiển & di chuyển
## Triển khai di chuyển Platformer

<div style="display: flex; gap: 20px; align-items: center;">
<div style="flex: 1.2;">

- **Animator Controller**: Thiết lập đồ thị trạng thái nhân vật chuyển đổi tức thì theo input bằng cách đặt `Exit Time = 0` và `Transition Duration = 0.05s` giữa các hoạt ảnh chạy, nhảy, lướt.
- **Coyote Time**: Cho phép người chơi nhảy trong khoảng 0.1 giây ngay cả khi vừa đi hụt ra khỏi mép vực.
- **Jump Buffer**: Lưu lệnh nhảy nhấn trước khi chạm đất tối đa 0.15 giây và tự kích hoạt ngay khi chạm đất, đảm bảo điều khiển mượt mà liên tục.
- **Dash I-Frames**: Lập trình trạng thái bất tử ngắn 0.2 giây khi lướt nhanh bằng cách tắt tạm thời va chạm của nhân vật với các hitbox đòn đánh của quái vật.

</div>
<div style="flex: 0.8; text-align: center;">

![image_unnamed_1.png](../extracted_images/image_unnamed_1.png)

</div>
</div>



---



# Slide 20: Xây dựng cơ chế chiến đấu & Tương khắc Ngũ hành
## Triển khai chiến đấu Ngũ Hành

- **Hitbox & Hurtbox**: Triển khai bằng Trigger Collider 2D trên Dedicated Server. Khi xảy ra va chạm, server thực hiện tính toán sát thương trực tiếp, client chỉ chịu trách nhiệm render hiệu ứng hoạt ảnh và text sát thương damage pop-up.
- **Vòng tương khắc Ngũ Hành**:
  - Kim ➔ Mộc ➔ Thủy ➔ Hỏa ➔ Thổ ➔ Kim. Tấn công khắc hệ được nhân **x1.5** sát thương; bị khắc hệ giảm còn **x0.75** sát thương.
  - Hệ thứ 6 **Phong Wind** đóng vai trò là hệ trung lập đặc biệt, không khắc và không bị khắc bởi 5 hệ còn lại với hệ số sát thương nhân x1.0, giúp tăng tính cân bằng chiến thuật.
- **Công thức sát thương cuối**: `Damage = ATK * skill_mult * element_mult - DEF * red_factor`. Luôn đảm bảo tối thiểu gây 1 sát thương để tránh lỗi bất tử.



---



# Slide 21: Xây dựng hệ thống Gene & Dung hợp Hybrid Gene
## Hệ thống tiến hóa Gene đặc trưng

<div style="display: flex; gap: 20px; align-items: center;">
<div style="flex: 1.2;">

- **Cấu trúc Gene 5 Tier**: Thay thế lớp nhân vật class cố định. Nâng cấp Gene mở khóa kỹ năng chủ động, bị động tương ứng.
- **Multi-Gene**: Khi đạt Tier 5, người chơi khảm đồng thời 1 Gene chính nhận 100% hiệu năng chỉ số và 1 Gene phụ nhận 30% chỉ số, cho phép tự do kết hợp tạo hơn 30 hướng build.
- **Hybrid Fusion**: Dung hợp 2 Gene hệ khác nhau ở Tier 5 như hệ Kim kết hợp với hệ Phong tạo class lai đặc biệt với kỹ năng lai độc nhất.
- **Gene Tối Thượng Ultimate Gene**: Mở khóa khi tích lũy đủ 1,000,000 EXP Tối Thượng ở cấp Hybrid, kích hoạt trạng thái nhân **x1.5** các chỉ số cơ bản HP, MP, ATK, DEF và hiển thị Aura hiệu ứng quanh người.

</div>
<div style="flex: 0.8; text-align: center;">

![image_unnamed_52.png](../extracted_images/image_unnamed_52.png)

</div>
</div>



---



# Slide 22: Xây dựng hệ thống AI quái vật & Boss đa giai đoạn
## AI FSM & Boss Phase System

<div style="display: flex; gap: 20px; align-items: center;">
<div style="flex: 1.2;">

- **Dedicated Server FSM**: Chạy tuần hoàn kiểm tra khoảng cách định kỳ để chuyển đổi các trạng thái tuần tra, đuổi theo và tấn công người chơi gần nhất trong vùng phát hiện.
- **Boss Phase System**:
  - Boss được cấu hình đa giai đoạn dựa trên phần trăm HP còn lại ví dụ Phase 1 có 100% HP, Phase 2 dưới 60% HP, Phase 3 dưới 30% HP.
  - Khi chuyển giai đoạn, Boss tự động kích hoạt animation chuyển trạng thái, thay đổi pattern tấn công, tăng tốc độ và sát thương, tạo thử thách tăng dần cho người chơi.
  - Cấu hình các giai đoạn boss được nạp động từ database qua cột `phases_json`.

</div>
<div style="flex: 0.8; text-align: center;">

![image_unnamed_2.png](../extracted_images/image_unnamed_2.png)

</div>
</div>



---



# Slide 23: Triển khai cơ chế đồng bộ di chuyển & Zone Server
## Đồng bộ mạng & Phân khu máy chủ

<div style="display: flex; gap: 20px; align-items: center;">
<div style="flex: 1.2;">

- **Client-prediction & Server Reconciliation**:
  - Client dự đoán di chuyển và render tức thì để triệt tiêu cảm giác trễ mạng.
  - Server gửi State chuẩn về; client so khớp, nếu sai lệch vượt ngưỡng, client tự động mượt mà lướt về vị trí chuẩn từ Server, loại bỏ hiện tượng giật lùi rubber banding.
- **Zone-based Server & Instance**:
  - Bản đồ thế giới được chia nhỏ thành các Zone độc lập quản lý bởi `ZoneRoomRegistry`. Người chơi chỉ nhận gói tin đồng bộ từ đối tượng trong cùng Zone để tối ưu băng thông.
  - Tạo động các phó bản Dungeon Instance riêng tư cho các tổ đội và tự giải phóng tài nguyên sau khi phó bản hoàn thành.

</div>
<div style="flex: 0.8; text-align: center;">

![image_unnamed_47.png](../extracted_images/image_unnamed_47.png)

</div>
</div>



---



# Slide 24: Chương 4: Kết quả triển khai và Đánh giá hệ thống
## Chương 4 giới thiệu

- Chương 4 trình bày môi trường triển khai thực tế trên VPS Linux sử dụng Docker Compose, các cơ chế bảo mật xác thực an toàn cho Dedicated Server và Web API, hình ảnh kết quả giao diện các tính năng của nguyên mẫu trò chơi hoàn chỉnh và phần thực nghiệm đánh giá hiệu năng hệ thống.



---



# Slide 25: Kiến trúc triển khai Docker Compose trên VPS Linux
## Triển khai VPS & Container hóa

<div style="display: flex; gap: 20px; align-items: center;">
<div style="flex: 1.2;">

- **Môi trường Cloud**: Đóng gói toàn bộ hệ thống vào các container Docker độc lập chạy trên máy chủ ảo VPS Linux Ubuntu Server.
- **Cấu trúc Docker Compose**:
  - `db-mysql`: Container cơ sở dữ liệu MySQL 8.0, cấu hình lưu trữ volume bền vững, hoàn toàn cô lập trong mạng nội bộ.
  - `web-api`: Container REST API & SignalR Hub ASP.NET Core 8, mở cổng HTTP 80 và 443 nhận kết nối.
  - `game-server`: Container Unity Dedicated Server headless, mở cổng UDP 7777 phục vụ realtime gameplay.
  - Sử dụng mạng Docker bridge nội bộ giúp bảo mật tối đa cho Database.

</div>
<div style="flex: 0.8; text-align: center;">

![image_unnamed_49.png](../extracted_images/image_unnamed_49.png)

</div>
</div>



---



# Slide 26: Cơ chế bảo mật hệ thống nhiều lớp
## Bảo mật & Chống can thiệp

<div style="display: flex; gap: 20px; align-items: center;">
<div style="flex: 1.2;">

- **Xác thực REST API với JWT**: Mọi yêu cầu truy vấn dữ liệu, thay đổi túi đồ, nâng cấp Gene đều phải mang kèm mã JWT hợp lệ trong header, server xác thực chữ ký HMAC-SHA256 để chống giả mạo thông tin.
- **Bảo mật kết nối Game Server Dedicated Server**:
  - *Connection Approval*: Khi client kết nối qua cổng UDP 7777, Server chặn gói tin kết nối ban đầu và yêu cầu client gửi JWT xác thực.
  - *Zone API Key*: Kiểm tra xác thực API Key nội bộ giữa Dedicated Server và Web API Server trước khi ghi nhận trạng thái lưu trữ của nhân vật, ngăn chặn các client giả mạo làm Server để ghi khống dữ liệu.

</div>
<div style="flex: 0.8; text-align: center;">

![image_unnamed_41.png](../extracted_images/image_unnamed_41.png)

</div>
</div>



---



# Slide 27: Giao diện Đăng ký, Chọn hệ, Sảnh chính & NPC
## Kết quả giao diện chính

<div style="display: flex; gap: 20px; align-items: center;">
<div style="flex: 1.2;">

- **Đăng ký & Đăng nhập**: Giao diện đăng nhập kiểm duyệt tài khoản mật khẩu nhanh chóng thông qua REST API, lưu trữ phiên token bảo mật.
- **Chọn nguyên tố ban đầu**: Cho phép chọn hệ nguyên tố cơ bản đầu tiên trước khi vào sảnh.
- **Sảnh chính & Tương tác NPC**: Sảnh chính rộng rãi, hỗ trợ tương tác với các NPC chức năng qua menu hội thoại động để nhận nhiệm vụ, mở cửa hàng trang bị hoặc tiến hành dung hợp.

</div>
<div style="flex: 0.8; text-align: center;">

![image_unnamed_50.png](../extracted_images/image_unnamed_50.png)

![image_unnamed_52.png](../extracted_images/image_unnamed_52.png)

</div>
</div>



---



# Slide 28: Giao diện Nâng cấp, Dung hợp Gene & Trang bị
## Kết quả giao diện phát triển

<div style="display: flex; gap: 20px; align-items: center;">
<div style="flex: 1.2;">

- **Nâng cấp Gene chính/phụ**: Hiển thị chi tiết cây nâng cấp Gene 5 Tier. Giao diện trực quan hóa việc khảm Gene chính và Gene phụ để thay đổi bộ chỉ số nhân vật.
- **Dung hợp Hybrid Gene**: Giao diện dung hợp 2 nguyên tố ở Tier 5 để mở khóa lớp lai cao cấp với hiệu ứng hào quang bắt mắt.
- **Thông tin nhân vật & Trang bị**: Bảng chỉ số chi tiết, giao diện kéo thả trang bị và nâng cấp vũ khí tại NPC Blacksmith.

</div>
<div style="flex: 0.8; text-align: center;">

![image_unnamed_59.png](../extracted_images/image_unnamed_59.png)

![image_unnamed_62.png](../extracted_images/image_unnamed_62.png)

</div>
</div>



---



# Slide 29: Giao diện Phó bản Wave, Chat & Tổ đội
## Giao diện Combat & Cộng đồng

<div style="display: flex; gap: 20px; align-items: center;">
<div style="flex: 1.2;">

- **Phó bản Wave**: Giao diện HUD hiển thị số đợt quái Wave 3 trên 5, số quái còn lại, và thanh máu Boss ở đỉnh màn hình.
- **Chiến đấu tổ đội**: Đồng bộ mượt mà đòn đánh và trạng thái máu của các thành viên trong tổ đội thời gian thực.
- **Hệ thống Chat & Cộng đồng**: Chat thế giới, chat riêng tư và chat tổ đội thời gian thực qua SignalR. Hiển thị thông báo trạng thái online/offline của bạn bè.

</div>
<div style="flex: 0.8; text-align: center;">

![image_unnamed_72.png](../extracted_images/image_unnamed_72.png)

![image_unnamed_67.png](../extracted_images/image_unnamed_67.png)

</div>
</div>



---



# Slide 30: Thực nghiệm & Đánh giá hiệu năng hệ thống
## Đánh giá thực nghiệm

- **Thử nghiệm độ trễ mạng Latency**: Kiểm thử kết nối client-server trong điều kiện mạng thực tế với RTT dao động từ 40ms đến 90ms. Cơ chế dự đoán di chuyển Client-prediction hoạt động ổn định, triệt tiêu hoàn toàn cảm giác khựng trễ di chuyển.
- **Thử nghiệm tải và FPS**:
  - Client chạy ổn định ở mức **60 FPS** trên cấu hình máy tầm trung.
  - Dedicated Server tối ưu hóa tốt tài nguyên CPU nhờ thuật toán FSM quái vật đơn giản và kiến trúc Zone-based Server phân khu tải dữ liệu.
- **Đánh giá bảo mật**: Thực nghiệm sử dụng các công cụ thay đổi bộ nhớ tại Client Cheat Engine để hack vàng và HP; Dedicated Server phát hiện và từ chối ghi nhận, chứng minh mô hình Server-Authoritative chống gian lận thành công.



---



# Slide 31: Kết luận đề tài
## Kết quả đạt được & Hạn chế

<div style="display: flex; gap: 20px; align-items: center;">
<div style="flex: 1.2;">

- **Kết quả đạt được**:
  - Xây dựng nguyên mẫu game nhập vai 2D đa người chơi Mutants Arena hoạt động ổn định trên VPS Linux qua Docker Compose.
  - Triển khai thành công cơ chế Gene Ngũ Hành, dung hợp Hybrid Gene và Gene Tối Thượng tạo lối chơi có chiều sâu.
  - Đảm bảo độ trễ đồng bộ mạng dưới 100ms mượt mà và bảo mật Dedicated Server chống gian lận tài nguyên thành công.
- **Hạn chế**: Số lượng bản đồ và quái vật còn ít; giao diện đồ họa 2D pixel art cần được tối ưu bóng bẩy và sống động hơn.

</div>
<div style="flex: 0.8; text-align: center;">

![image_unnamed_49.png](../extracted_images/image_unnamed_49.png)

</div>
</div>


---



# Slide 32: Định hướng phát triển
## Kế hoạch phát triển & Nâng cấp hệ thống

- **Tối ưu hóa Đồng bộ & Quy mô mạng**: Áp dụng cơ chế quản lý mức độ quan tâm Interest Management để chỉ truyền gói tin đồng bộ trong tầm nhìn, giảm băng thông server và mở rộng quy mô đồng thời lên hàng ngàn người chơi.
- **Nâng cao Cơ chế Bảo mật & Chống gian lận**: Triển khai các thuật toán kiểm tra tính hợp lệ của lệnh điều khiển Coyote time, Jump buffer verification trên server và tích hợp Easy Anti-Cheat ở client để ngăn chặn triệt để hack/cheat.
- **Phát triển Chế độ chơi & AI Boss đa dạng**: Thiết kế đấu trường PvP xếp hạng trực tuyến, bổ sung các phó bản bang hội đột kích Guild Raid Dungeons với các Boss đa giai đoạn sử dụng Behavior Trees phức tạp hơn.
- **Hệ thống Kinh tế & Giao dịch trực tuyến**: Xây dựng hệ thống chợ giao dịch trực tuyến Auction House giữa người chơi thông qua SignalR Hub, đảm bảo đồng bộ tài nguyên và ngăn ngừa các lỗi nhân bản vật phẩm dupe items.
- **Tự động hóa co giãn server Dedicated**: Triển khai hệ thống tự động co giãn Dedicated Server Auto-scaling trên cụm Kubernetes K8s dựa trên số lượng người chơi trực tuyến, tối ưu chi phí vận hành VPS.


---



# Slide 33: CẢM ƠN QUÝ THẦY CÔ VÀ CÁC BẠN ĐÃ LẮNG NGHE!
## Lời cảm ơn

- Sinh viên thực hiện: **Trần Văn Thủy**
- Mã sinh viên: **CT060439**
- Lớp: **AT16D**
- Người hướng dẫn khoa học: **TS. Nguyễn Đức Hiếu**
- Khoa Công nghệ thông tin – Học viện Kỹ thuật Mật mã
