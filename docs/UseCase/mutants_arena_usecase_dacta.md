# ĐẶC TẢ USE CASE CHỨC NĂNG — HỆ THỐNG MUTANTS ARENA

## 1. Mục đích và phạm vi

Tài liệu này đặc tả chi tiết bộ use case chức năng chính của hệ thống Mutants Arena sau khi đã gộp các xử lý kỹ thuật nội bộ vào bên trong từng chức năng nghiệp vụ tương ứng.
Cách tổ chức này giúp sơ đồ tổng quát gọn hơn, đồng thời vẫn giữ đầy đủ nội dung cần thiết cho phần mô tả nghiệp vụ và trình bày trong báo cáo.

## 2. Quy ước mô tả

Bảng 1. Quy ước đọc đặc tả use case

| Thành phần | Ý nghĩa |
|---|---|
| Tác nhân chính | Đối tượng trực tiếp khởi tạo hoặc tham gia vào luồng xử lý của use case. |
| Tiền điều kiện | Điều kiện phải thỏa mãn trước khi use case được thực hiện. |
| Hậu điều kiện | Trạng thái hệ thống sau khi use case kết thúc thành công. |
| «include» | Use case con bắt buộc, luôn được thực hiện như một phần của use case chính. |
| «extend» | Use case mở rộng, chỉ phát sinh khi có điều kiện hoặc ngữ cảnh phù hợp. |

## 3. Bảng tổng hợp use case chức năng

Bảng 2. Bảng tổng hợp use case chức năng

| Mã | Tên use case | Nhóm chức năng | Tác nhân chính |
|---|---|---|---|
| UC01 | Đăng ký tài khoản | Tài khoản và gameplay | Khách |
| UC02 | Đăng nhập và vào game | Tài khoản và gameplay | Khách, Người chơi |
| UC03 | Di chuyển và chuyển map | Tài khoản và gameplay | Người chơi |
| UC04 | Chiến đấu và sử dụng kỹ năng | Tài khoản và gameplay | Người chơi |
| UC05 | Quản lý túi đồ và trang bị | Tài khoản và gameplay | Người chơi |
| UC06 | Nâng cấp trang bị | Tài khoản và gameplay | Người chơi |
| UC07 | Phát triển Gene và Hybrid | Phát triển nhân vật | Người chơi |
| UC08 | Phân bổ tiềm năng và kỹ năng | Phát triển nhân vật | Người chơi |
| UC09 | Tương tác NPC và mua vật phẩm | Phát triển nhân vật | Người chơi |
| UC10 | Quản lý nhiệm vụ | Phát triển nhân vật | Người chơi |
| UC11 | Quản lý bạn bè | Tương tác và hoạt động | Người chơi |
| UC12 | Quản lý tổ đội và chat | Tương tác và hoạt động | Người chơi |
| UC13 | Tham gia và hoàn tất phó bản | Tương tác và hoạt động | Người chơi, Gameplay Server |
| UC14 | Xem leaderboard | Tương tác và hoạt động | Người chơi |
| UC15 | Đăng ký và duy trì gameplay server | Vận hành kỹ thuật | Gameplay Server |
| UC16 | Host map và phát thưởng dungeon | Vận hành kỹ thuật | Gameplay Server |

## 4. Đặc tả chi tiết theo nhóm chức năng

### 4.1. Nhóm 1 - Tài khoản và gameplay

Nhóm chức năng nền tảng, bao phủ toàn bộ quá trình từ tạo tài khoản, đăng nhập, di chuyển đến chiến đấu và quản lý vật phẩm cơ bản.

#### UC01 — Đăng ký tài khoản

Bảng 3. Đặc tả use case UC01 — Đăng ký tài khoản

| Thuộc tính | Nội dung |
|---|---|
| Use Case | Đăng ký tài khoản |
| Actor | Khách |
| Mô tả | Khách tạo tài khoản mới để bắt đầu sử dụng hệ thống Mutants Arena. Hệ thống nhận thông tin đăng ký, kiểm tra tính hợp lệ và lưu tài khoản khi dữ liệu đáp ứng đủ điều kiện. |
| Tiền điều kiện | 1. Khách chưa có tài khoản trong hệ thống. 2. Có kết nối mạng ổn định. |
| Luồng chính | 1. Khách mở màn hình đăng ký từ giao diện chính. 2. Hệ thống hiển thị biểu mẫu gồm tên đăng nhập, mật khẩu và email. 3. Khách nhập thông tin và xác nhận gửi đăng ký. 4. Hệ thống kiểm tra tính hợp lệ của dữ liệu và phát hiện trùng lặp nếu có. 5. Hệ thống mã hóa mật khẩu và lưu tài khoản vào cơ sở dữ liệu. 6. Hệ thống trả thông báo thành công và chuyển sang màn hình đăng nhập. |
| Luồng phụ | a) Tên đăng nhập đã tồn tại → hệ thống yêu cầu nhập tên khác. b) Thiếu trường bắt buộc hoặc mật khẩu không hợp lệ → biểu mẫu hiển thị lỗi tương ứng. |
| Kết quả | Tài khoản mới được tạo thành công và sẵn sàng để đăng nhập. Nếu thất bại, hệ thống hiển thị thông báo lỗi rõ nguyên nhân. |

#### UC02 — Đăng nhập và vào game

Bảng 4. Đặc tả use case UC02 — Đăng nhập và vào game

| Thuộc tính | Nội dung |
|---|---|
| Use Case | Đăng nhập và vào game |
| Actor | Khách, Người chơi |
| Mô tả | Người dùng xác thực tài khoản, chọn nhân vật và kết nối vào thế giới game. Hệ thống tạo phiên đăng nhập hợp lệ và đưa nhân vật vào map tương ứng. |
| Tiền điều kiện | 1. Tài khoản đã tồn tại trong hệ thống. 2. Máy chủ game đang sẵn sàng kết nối. |
| Luồng chính | 1. Người dùng nhập thông tin đăng nhập trên màn hình xác thực. 2. Hệ thống xác thực tên đăng nhập và mật khẩu. 3. Hệ thống tạo token phiên và gửi về cho client. 4. Người dùng chọn nhân vật hiện có hoặc tạo nhanh nhân vật mới nếu chưa có. 5. Client dùng thông tin phiên để kết nối đến gameplay server. 6. Máy chủ nạp dữ liệu nhân vật và đưa người chơi vào map khởi đầu. |
| Luồng phụ | a) Sai thông tin đăng nhập → hệ thống từ chối và yêu cầu nhập lại. b) Kết nối đến gameplay server thất bại → client thông báo để người chơi thử lại. |
| Kết quả | Phiên đăng nhập hợp lệ được tạo và nhân vật xuất hiện trong game. Nếu thất bại, hệ thống hiển thị thông báo lỗi tương ứng. |

#### UC03 — Di chuyển và chuyển map

Bảng 5. Đặc tả use case UC03 — Di chuyển và chuyển map

| Thuộc tính | Nội dung |
|---|---|
| Use Case | Di chuyển và chuyển map |
| Actor | Người chơi |
| Mô tả | Người chơi di chuyển qua portal để sang map hoặc khu vực mới. Hệ thống kiểm tra điều kiện vào khu vực đích, tải map mới và đặt nhân vật vào vị trí spawn tương ứng. |
| Tiền điều kiện | 1. Nhân vật đang ở gần portal. 2. Người chơi đáp ứng điều kiện vào khu vực đích. |
| Luồng chính | 1. Người chơi tiến vào vùng tương tác của portal. 2. Hệ thống hiển thị gợi ý xác nhận di chuyển sang khu vực đích. 3. Người chơi xác nhận thao tác chuyển map. 4. Máy chủ kiểm tra điều kiện như level, nhiệm vụ hoặc giới hạn số người. 5. Hệ thống tải map mới và cập nhật zone của nhân vật. 6. Nhân vật xuất hiện tại vị trí spawn tương ứng của map đích. |
| Luồng phụ | a) Chưa đủ điều kiện vào map → hệ thống từ chối và hiển thị lý do. b) Quá trình tải map lỗi → hệ thống giữ nguyên vị trí hiện tại và thông báo thử lại. |
| Kết quả | Người chơi được đưa sang map mới và hiển thị đúng trong zone mới. Nếu thất bại, nhân vật ở nguyên vị trí ban đầu và có thông báo nguyên nhân. |

#### UC04 — Chiến đấu và sử dụng kỹ năng

Bảng 6. Đặc tả use case UC04 — Chiến đấu và sử dụng kỹ năng

| Thuộc tính | Nội dung |
|---|---|
| Use Case | Chiến đấu và sử dụng kỹ năng |
| Actor | Người chơi |
| Mô tả | Người chơi chiến đấu với quái hoặc boss bằng đòn đánh thường và kỹ năng. Máy chủ xác nhận và tính toán sát thương, sau đó đồng bộ kết quả cho toàn bộ client liên quan. |
| Tiền điều kiện | 1. Nhân vật còn sống và đang ở khu vực có mục tiêu chiến đấu. 2. Không đang trong trạng thái bị khóa thao tác. |
| Luồng chính | 1. Người chơi chọn mục tiêu hoặc thực hiện tấn công trong phạm vi cho phép. 2. Client gửi yêu cầu đánh thường hoặc dùng kỹ năng lên máy chủ. 3. Máy chủ kiểm tra cooldown, mana và điều kiện mục tiêu. 4. Máy chủ tính sát thương và áp dụng kết quả lên đối tượng liên quan. 5. Hệ thống đồng bộ vị trí, animation và trạng thái cho các client quan sát. 6. Nếu mục tiêu bị hạ gục, phần thưởng kinh nghiệm và vật phẩm được phát cho người chơi. |
| Luồng phụ | a) Kỹ năng đang hồi chiêu hoặc không đủ tài nguyên → yêu cầu bị từ chối, hiển thị trạng thái không khả dụng. b) Mục tiêu đã rời khỏi phạm vi hợp lệ → đòn đánh không được áp dụng và hệ thống hủy thao tác. |
| Kết quả | Sát thương, trạng thái buff/debuff và phần thưởng chiến đấu được cập nhật chính xác trên tất cả client. Nếu thất bại, trạng thái chiến đấu giữ nguyên và có thông báo nguyên nhân. |

#### UC05 — Quản lý túi đồ và trang bị

Bảng 7. Đặc tả use case UC05 — Quản lý túi đồ và trang bị

| Thuộc tính | Nội dung |
|---|---|
| Use Case | Quản lý túi đồ và trang bị |
| Actor | Người chơi |
| Mô tả | Người chơi xem, sử dụng và thay đổi trang bị trong túi đồ cá nhân. Hệ thống kiểm tra điều kiện slot và cập nhật chỉ số nhân vật sau mỗi thao tác. |
| Tiền điều kiện | 1. Nhân vật đã đăng nhập thành công. 2. Có vật phẩm trong kho đồ hoặc trang bị đang được sử dụng. |
| Luồng chính | 1. Người chơi mở giao diện túi đồ từ HUD hoặc phím tắt. 2. Hệ thống hiển thị danh sách vật phẩm và thông tin cơ bản từng ô. 3. Người chơi chọn một vật phẩm để xem chi tiết hoặc thực hiện thao tác. 4. Nếu người chơi chọn trang bị, hệ thống kiểm tra slot và điều kiện sử dụng. 5. Nếu người chơi chọn dùng vật phẩm tiêu hao, hiệu ứng được áp dụng ngay. 6. Hệ thống cập nhật túi đồ, trang bị và chỉ số nhân vật sau khi thao tác hoàn tất. |
| Luồng phụ | a) Vật phẩm không đủ điều kiện sử dụng → hệ thống chặn thao tác và nêu rõ yêu cầu còn thiếu. b) Túi đồ đầy khi nhận thêm vật phẩm → hệ thống từ chối và thông báo cho người chơi. |
| Kết quả | Túi đồ và chỉ số nhân vật phản ánh đúng thay đổi mới nhất. Nếu thao tác không hợp lệ, trạng thái vật phẩm và chỉ số giữ nguyên. |

#### UC06 — Nâng cấp trang bị

Bảng 8. Đặc tả use case UC06 — Nâng cấp trang bị

| Thuộc tính | Nội dung |
|---|---|
| Use Case | Nâng cấp trang bị |
| Actor | Người chơi |
| Mô tả | Người chơi nâng cấp trang bị tại NPC hoặc giao diện cường hóa chuyên dụng. Hệ thống kiểm tra nguyên liệu, thực hiện cường hóa và cập nhật chỉ số vật phẩm theo kết quả. |
| Tiền điều kiện | 1. Người chơi có trang bị phù hợp trong túi đồ. 2. Đủ nguyên liệu và vàng cần thiết theo yêu cầu nâng cấp. |
| Luồng chính | 1. Người chơi mở giao diện nâng cấp trang bị. 2. Hệ thống hiển thị các vật phẩm có thể cường hóa trong túi đồ. 3. Người chơi chọn trang bị mục tiêu và xem yêu cầu nguyên liệu. 4. Hệ thống kiểm tra số lượng nguyên liệu, vàng và điều kiện nâng cấp. 5. Người chơi xác nhận thao tác nâng cấp. 6. Hệ thống xử lý kết quả, trừ tài nguyên và cập nhật lại chỉ số của vật phẩm sau khi hoàn tất. |
| Luồng phụ | a) Thiếu nguyên liệu hoặc vàng → thao tác bị từ chối, hiển thị lượng còn thiếu. b) Nâng cấp thất bại theo tỷ lệ → hệ thống áp dụng đúng quy tắc rủi ro đã cấu hình và thông báo kết quả. |
| Kết quả | Trang bị được cập nhật cấp cường hóa và tài nguyên bị trừ đúng. Nếu thất bại, trạng thái trang bị được xử lý theo quy tắc rủi ro và hiển thị thông báo rõ ràng. |

### 4.2. Nhóm 2 - Phát triển nhân vật

Nhóm chức năng phục vụ tiến trình phát triển sức mạnh nhân vật thông qua Gene, kỹ năng, NPC hỗ trợ và hệ thống nhiệm vụ.

#### UC07 — Phát triển Gene và Hybrid

Bảng 9. Đặc tả use case UC07 — Phát triển Gene và Hybrid

| Thuộc tính | Nội dung |
|---|---|
| Use Case | Phát triển Gene và Hybrid |
| Actor | Người chơi |
| Mô tả | Người chơi phát triển hệ Gene của nhân vật để mở khóa và tăng sức mạnh chiến đấu. Hệ thống kiểm tra điểm Gene, cập nhật cấp độ và cho phép thực hiện dung hợp khi đủ điều kiện. |
| Tiền điều kiện | 1. Người chơi có đủ điểm Gene hoặc vật liệu dung hợp tương ứng. 2. Slot Gene mục tiêu đang ở trạng thái có thể nâng cấp. |
| Luồng chính | 1. Người chơi mở giao diện Gene Evolution. 2. Hệ thống hiển thị Gene chính, Gene phụ và các điều kiện nâng cấp hiện tại. 3. Người chơi chọn nhánh Gene muốn nâng hoặc mở khóa. 4. Hệ thống kiểm tra điểm Gene và điều kiện mở slot tương ứng. 5. Nếu đủ điều kiện, hệ thống cập nhật cấp Gene và hiệu ứng mới cho nhân vật. 6. Khi đã đủ vật liệu, người chơi có thể thực hiện dung hợp để tạo Hybrid Gene mới. |
| Luồng phụ | a) Không đủ điểm Gene → thao tác nâng cấp bị chặn, hiển thị lượng còn thiếu. b) Công thức dung hợp không hợp lệ → hệ thống từ chối tạo Hybrid Gene và thông báo lỗi. |
| Kết quả | Cấp Gene và hiệu ứng Hybrid của nhân vật được cập nhật theo lựa chọn mới. Nếu thất bại, trạng thái Gene giữ nguyên và hiển thị thông báo rõ nguyên nhân. |

#### UC08 — Phân bổ tiềm năng và kỹ năng

Bảng 10. Đặc tả use case UC08 — Phân bổ tiềm năng và kỹ năng

| Thuộc tính | Nội dung |
|---|---|
| Use Case | Phân bổ tiềm năng và kỹ năng |
| Actor | Người chơi |
| Mô tả | Người chơi phân bổ điểm chỉ số và sắp xếp kỹ năng phù hợp với lối chơi. Hệ thống kiểm tra hạn mức, lưu cấu hình và cập nhật HUD chiến đấu sau khi xác nhận. |
| Tiền điều kiện | 1. Nhân vật còn điểm tiềm năng chưa phân bổ hoặc có kỹ năng khả dụng để sắp xếp. |
| Luồng chính | 1. Người chơi mở bảng chỉ số và kỹ năng của nhân vật. 2. Hệ thống hiển thị các chỉ số hiện tại, điểm còn lại và danh sách kỹ năng khả dụng. 3. Người chơi cộng điểm vào các chỉ số mong muốn. 4. Người chơi kéo thả kỹ năng vào các vị trí trên thanh kỹ năng nhanh. 5. Người chơi xác nhận lưu cấu hình vừa chỉnh sửa. 6. Hệ thống cập nhật lại chỉ số, kỹ năng đang trang bị và HUD chiến đấu. |
| Luồng phụ | a) Cộng quá số điểm hiện có → hệ thống không cho phép xác nhận và hiển thị lỗi. b) Kỹ năng chưa mở khóa hoặc không phù hợp ô gắn → không thể thả vào thanh nhanh. |
| Kết quả | Bộ chỉ số và thanh kỹ năng của nhân vật được lưu theo cấu hình mới. Nếu không hợp lệ, hệ thống giữ cấu hình cũ và hiển thị thông báo lỗi. |

#### UC09 — Tương tác NPC và mua vật phẩm

Bảng 11. Đặc tả use case UC09 — Tương tác NPC và mua vật phẩm

| Thuộc tính | Nội dung |
|---|---|
| Use Case | Tương tác NPC và mua vật phẩm |
| Actor | Người chơi |
| Mô tả | Người chơi tương tác với NPC để mở dịch vụ, mua vật phẩm hoặc dùng các tiện ích hỗ trợ. Hệ thống hiển thị menu động theo loại NPC, xử lý giao dịch và cập nhật túi đồ sau khi xác nhận. |
| Tiền điều kiện | 1. Người chơi đang ở trong phạm vi tương tác hợp lệ của NPC. |
| Luồng chính | 1. Người chơi tiếp cận NPC và kích hoạt tương tác. 2. Hệ thống hiển thị menu động theo loại NPC hiện tại. 3. Người chơi chọn chức năng mua vật phẩm hoặc dịch vụ mong muốn. 4. Hệ thống hiển thị danh sách hàng hóa, giá bán và số lượng cần mua. 5. Người chơi xác nhận giao dịch. 6. Hệ thống trừ vàng, thêm vật phẩm hoặc áp dụng dịch vụ tương ứng cho nhân vật. |
| Luồng phụ | a) Không đủ vàng hoặc vật phẩm đã hết → giao dịch bị từ chối và hiển thị nguyên nhân. b) NPC thuộc loại hỗ trợ đặc biệt → hệ thống mở dịch vụ tương ứng thay cho cửa hàng vật phẩm. |
| Kết quả | Giao dịch với NPC được ghi nhận và túi đồ của người chơi được cập nhật. Nếu thất bại, tài nguyên giữ nguyên và hiển thị thông báo nguyên nhân. |

#### UC10 — Quản lý nhiệm vụ

Bảng 12. Đặc tả use case UC10 — Quản lý nhiệm vụ

| Thuộc tính | Nội dung |
|---|---|
| Use Case | Quản lý nhiệm vụ |
| Actor | Người chơi |
| Mô tả | Người chơi nhận, theo dõi và hoàn tất nhiệm vụ để nhận thưởng và mở khóa nội dung mới. Hệ thống tự động theo dõi tiến độ và cấp thưởng khi đầy đủ điều kiện hoàn thành. |
| Tiền điều kiện | 1. Nhân vật đáp ứng điều kiện nhận nhiệm vụ tương ứng (level, chuỗi quest). |
| Luồng chính | 1. Người chơi tương tác với NPC hoặc giao diện nhiệm vụ để nhận quest mới. 2. Hệ thống hiển thị mô tả, mục tiêu và phần thưởng của nhiệm vụ. 3. Người chơi xác nhận nhận nhiệm vụ và quest được thêm vào nhật ký. 4. Trong quá trình chơi, hệ thống tự động cập nhật tiến độ hoàn thành các mục tiêu. 5. Khi đã đủ điều kiện, người chơi quay lại NPC hoặc giao diện để nộp nhiệm vụ. 6. Hệ thống cấp thưởng và cập nhật trạng thái chuỗi nhiệm vụ tiếp theo nếu có. |
| Luồng phụ | a) Người chơi chưa đủ điều kiện level hoặc chưa hoàn thành quest trước → không thể nhận nhiệm vụ mới, hiển thị lý do. b) Người chơi hủy nhiệm vụ (nếu hệ thống cho phép) → tiến độ bị xóa và có thể nhận lại sau. |
| Kết quả | Tiến độ nhiệm vụ được cập nhật và phần thưởng được cấp đúng sau khi hoàn thành. Nếu không đủ điều kiện, hệ thống giữ trạng thái quest hiện tại và thông báo rõ nguyên nhân. |

### 4.3. Nhóm 3 - Tương tác và hoạt động

Nhóm chức năng xã hội và hoạt động tập thể, cho phép người chơi kết nối với nhau, tham gia phó bản và theo dõi thứ hạng.

#### UC11 — Quản lý bạn bè

Bảng 13. Đặc tả use case UC11 — Quản lý bạn bè

| Thuộc tính | Nội dung |
|---|---|
| **Mã use case** | UC11 |
| **Tên use case** | Quản lý bạn bè |
| **Nhóm chức năng** | Tương tác và hoạt động |
| **Mục tiêu** | Người chơi quản lý danh sách bạn bè và trạng thái kết nối xã hội trong game. |
| **Tác nhân chính** | Người chơi |
| **Sự kiện kích hoạt** | Người chơi mở bảng bạn bè từ giao diện xã hội. |
| **Tiền điều kiện** | Người chơi đang online và có thể truy cập bảng bạn bè. |
| **Hậu điều kiện** | Danh sách bạn bè được cập nhật đồng bộ ở cả hai phía liên quan. |
| **Use case liên quan** | «include»: Gửi lời mời kết bạn, Phản hồi lời mời, Xóa bạn bè |
| **Sơ đồ chi tiết** | [uc11_usecase.drawio](uc11_usecase.drawio) |

**Luồng chính:**

1. Người chơi mở bảng bạn bè từ giao diện xã hội.
2. Hệ thống hiển thị danh sách bạn bè cùng trạng thái online và map hiện tại.
3. Người chơi nhập tên người nhận và gửi lời mời kết bạn.
4. Người nhận nhận được thông báo phản hồi lời mời.
5. Nếu chấp nhận, hệ thống thêm hai bên vào danh sách bạn bè của nhau.
6. Người chơi có thể tiếp tục xóa bạn hoặc xem lại trạng thái của từng người trong danh sách.

**Luồng thay thế và ngoại lệ:**

a) Người nhận không tồn tại hoặc đã là bạn thì hệ thống từ chối lời mời.
b) Người nhận từ chối lời mời thì yêu cầu kết bạn kết thúc mà không thay đổi dữ liệu.

#### UC12 — Quản lý tổ đội và chat

Bảng 14. Đặc tả use case UC12 — Quản lý tổ đội và chat

| Thuộc tính | Nội dung |
|---|---|
| Use Case | Quản lý tổ đội và chat |
| Actor | Người chơi |
| Mô tả | Người chơi tạo party, quản lý thành viên và trao đổi thông tin qua các kênh chat phù hợp. Hệ thống phân phối tin nhắn đến đúng phạm vi người nhận và cập nhật giao diện xã hội liên quan. |
| Tiền điều kiện | 1. Người chơi đang online. 2. Không bị chặn tính năng xã hội do vi phạm quy định. |
| Luồng chính | 1. Người chơi mở giao diện xã hội và chọn tạo tổ đội hoặc mời thành viên. 2. Hệ thống tạo party mới và gán vai trò trưởng nhóm cho người khởi tạo. 3. Người chơi mời thêm thành viên từ danh sách bạn bè hoặc người chơi gần đó. 4. Sau khi party được hình thành, bảng tổ đội hiển thị trạng thái của từng thành viên. 5. Người chơi sử dụng khung chat để trao đổi trong các kênh chung hoặc kênh tổ đội. 6. Hệ thống phân phối tin nhắn đến đúng phạm vi người nhận và cập nhật giao diện xã hội liên quan. |
| Luồng phụ | a) Người được mời đang ở tổ đội khác → lời mời bị từ chối và thông báo cho người mời. b) Người chơi gửi tin sai kênh hoặc vi phạm bộ lọc chat → hệ thống chặn và thông báo lỗi. |
| Kết quả | Party và nội dung trao đổi được cập nhật cho đúng thành viên hoặc đúng kênh nhận tin. Nếu thất bại, trạng thái party và chat giữ nguyên và hiển thị thông báo lỗi. |

#### UC13 — Tham gia và hoàn tất phó bản

Bảng 15. Đặc tả use case UC13 — Tham gia và hoàn tất phó bản

| Thuộc tính | Nội dung |
|---|---|
| Use Case | Tham gia và hoàn tất phó bản |
| Actor | Người chơi, Máy chủ |
| Mô tả | Người chơi hoặc tổ đội tham gia dungeon, vượt qua các wave quái và nhận phần thưởng khi hoàn thành. Gameplay server khởi tạo phiên dungeon, spawn wave và xử lý phật thưởng khi kết thúc. |
| Tiền điều kiện | 1. Người chơi đáp ứng điều kiện vào dungeon (level, lượt vào). 2. Gameplay server đang còn slot hoạt động. |
| Luồng chính | 1. Người chơi mở giao diện hoặc portal dungeon và chọn loại phó bản. 2. Hệ thống kiểm tra điều kiện về level, lượt vào và trạng thái tổ đội. 3. Gameplay server khởi tạo phiên dungeon và spawn các wave quái tương ứng. 4. Người chơi vượt qua từng đợt quái và tiến tới boss cuối. 5. Khi điều kiện hoàn thành được đáp ứng, hệ thống tổng kết kết quả phó bản. 6. Hệ thống phát phần thưởng và đưa người chơi rời khỏi dungeon sau khi kết thúc. |
| Luồng phụ | a) Người chơi hoặc tổ đội thất bại trong dungeon → hệ thống kết thúc phiên với trạng thái thất bại, không phát thưởng. b) Máy chủ gặp lỗi spawn ở một wave → hệ thống ghi log và xử lý theo cấu hình fallback của dungeon. |
| Kết quả | Kết quả phó bản được ghi nhận và phần thưởng được phát theo trạng thái hoàn thành. Nếu thất bại, hệ thống kết thúc phiên và thông báo rõ nguyên nhân cho người chơi. |

#### UC14 — Xem leaderboard

Bảng 16. Đặc tả use case UC14 — Xem leaderboard

| Thuộc tính | Nội dung |
|---|---|
| Use Case | Xem leaderboard |
| Actor | Người chơi |
| Mô tả | Người chơi theo dõi thứ hạng trong game theo từng hạng mục. Hệ thống tự làm mới cache khi dữ liệu cũ quá 5 phút và trả về bảng xếp hạng mới nhất. |
| Tiền điều kiện | 1. Người chơi đã đăng nhập. 2. Dịch vụ leaderboard đang khả dụng. |
| Luồng chính | 1. Người chơi mở giao diện bảng xếp hạng. 2. Hệ thống kiểm tra cache; nếu stale, tự tính lại ranking từ database. 3. Người chơi chuyển đổi giữa các tab để xem top theo từng tiêu chí (cấp độ, nhiệm vụ, dungeon, vàng, điểm danh). 4. Hệ thống đánh dấu vị trí hiện tại của nhân vật nếu có mặt trên bảng. 5. Người chơi có thể nhấn làm mới để lấy dữ liệu mới nhất. |
| Luồng phụ | a) Dữ liệu tạm thời chưa sẵn sàng → hệ thống lấy từ cache gần nhất hoặc hiển thị trạng thái chờ. |
| Kết quả | Bảng xếp hạng hiển thị đúng dữ liệu mới nhất. Nếu lỗi, dữ liệu cache cũ được giữ nguyên và có thông báo nguyên nhân. |

### 4.4. Nhóm 4 - Vận hành kỹ thuật

Nhóm chức năng hỗ trợ lớp vận hành backend, bảo đảm gameplay server, map host và cơ chế phát thưởng phó bản hoạt động ổn định.

#### UC15 — Đăng ký và duy trì gameplay server

Bảng 17. Đặc tả use case UC15 — Đăng ký và duy trì gameplay server

| Thuộc tính | Nội dung |
|---|---|
| Use Case | Đăng ký và duy trì gameplay server |
| Actor | Gameplay Server |
| Mô tả | Unity/NGO Dedicated Server tự đăng ký trạng thái hoạt động với Backend API và gửi heartbeat định kỳ để duy trì hiện diện trong registry. Xác thực bằng role `GameServer` qua JWT. |
| Tiền điều kiện | 1. Backend API đang hoạt động. 2. Gameplay server có JWT với role `GameServer` hợp lệ (`X-Zone-Api-Key` hoặc tương đương). |
| Luồng chính | 1. Gameplay server khởi động và gọi `POST /api/zone/server/register` với địa chỉ IP, port và số map đang host. 2. Backend API xác thực role `GameServer` và lưu entry vào in-memory registry. 3. Server gửi `PUT /api/zone/server/heartbeat` theo chu kỳ kèm số người chơi và thống kê zone. 4. Backend API cập nhật thời gian heartbeat cuối và trạng thái zone hiện tại. 5. Khi server tắt, gọi `DELETE /api/zone/server/deregister` để xóa khỏi registry. |
| Luồng phụ | a) Heartbeat quá thời hạn → entry tự hết hạn trong registry, coi server đã offline. b) JWT không hợp lệ hoặc thiếu role `GameServer` → Backend API từ chối với HTTP 401/403. |
| Kết quả | Entry của gameplay server được duy trì trong registry khi còn heartbeat. Khi deregister hoặc timeout, entry bị xóa. |

#### UC16 — Host map và phát thưởng dungeon

Bảng 18. Đặc tả use case UC16 — Host map và phát thưởng dungeon

| Thuộc tính | Nội dung |
|---|---|
| Use Case | Host map và phát thưởng dungeon |
| Actor | Gameplay Server |
| Mô tả | Gameplay server đăng ký làm host cho world map, tải cấu hình spawn từ Backend API và gửi yêu cầu phát thưởng khi phiên dungeon kết thúc. Hai nhánh này độc lập: host map phục vụ open-world, phát thưởng dungeon chỉ phát sinh khi dungeon hoàn thành. Phát thưởng dungeon là quan hệ `<<extend>>` với UC13. |
| Tiền điều kiện | 1. Gameplay server đã xác thực bằng `X-Zone-Api-Key`. 2. Backend API đang khả dụng. |
| Luồng chính — nhánh Host map | 1. Gameplay server gọi `GET /api/map/{mapId}/spawn-config` để nạp cấu hình spawn và cấu hình map hiện tại từ DB. 2. Server áp dụng cấu hình để spawn quái, boss và đối tượng mạng đúng theo map. 3. Server gọi `POST /api/map/host/register` để đăng ký làm host và duy trì bằng heartbeat định kỳ. |
| Luồng chính — nhánh Phát thưởng dungeon | 1. Khi dungeon kết thúc thành công, gameplay server tổng hợp danh sách người chơi và vật phẩm thưởng. 2. Server gọi `POST /api/dungeonreward/grant` với `targetPlayerId` và danh sách `items`, xác thực bằng `X-Zone-Api-Key`. 3. Backend API kiểm tra slot túi đồ và thêm vật phẩm vào inventory người chơi, lưu DB. |
| Luồng phụ | a) Cấu hình spawn không hợp lệ hoặc map không tồn tại → gameplay server dùng cấu hình mặc định và ghi log. b) Phát thưởng lỗi do `targetPlayerId` không tồn tại hoặc túi đầy → backend trả lỗi, server ghi log để xử lý thủ công. |
| Kết quả | Map host được đồng bộ đúng cấu hình. Phần thưởng dungeon được thêm vào inventory người chơi và lưu vào database. |

## 5. Tổng hợp quan hệ trong sơ đồ tổng quát

Bảng 19. Quan hệ trọng tâm trong sơ đồ use case tổng quát

| Use case nguồn | Use case đích | Loại quan hệ | Ý nghĩa |
|---|---|---|---|

| Nâng cấp trang bị | Quản lý túi đồ và trang bị | «include» | Use case nâng cấp trang bị luôn cần truy xuất và kiểm tra dữ liệu vật phẩm trong túi đồ trước khi xử lý cường hóa. |
| Host map và phát thưởng dungeon | Tham gia và hoàn tất phó bản | «extend» | Chỉ nhánh phát thưởng dungeon (trong UC16) mới là quan hệ extend với UC13; phần host map không có quan hệ này. Phát thưởng chỉ phát sinh khi dungeon hoàn thành thành công. |