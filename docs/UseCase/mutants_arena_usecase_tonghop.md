# TÀI LIỆU USE CASE TỔNG QUAN — HỆ THỐNG MUTANTS ARENA

## 1. Mục đích tài liệu

Tài liệu này trình bày bộ use case chức năng mới nhất của hệ thống Mutants Arena ở mức nghiệp vụ.
Nội dung được dùng để đồng bộ giữa sơ đồ tổng quát, các sơ đồ chi tiết và phần thuyết minh trong báo cáo.

## 2. Phạm vi và nguyên tắc mô hình hóa

Phạm vi tài liệu gồm 16 use case chức năng chính, được tổ chức thành bốn nhóm nghiệp vụ.
Các xử lý kỹ thuật nội bộ như xác thực, kiểm tra điều kiện, đồng bộ trạng thái hoặc phát thưởng được biểu diễn bằng quan hệ «include» và «extend» để sơ đồ tổng quát gọn và dễ đọc hơn.

## 3. Danh sách tác nhân

Biểu đồ Use Case tổng quát mô tả các chức năng chính mà hệ thống game nhập vai hành động trực tuyến nhiều người chơi Mutants Arena cung cấp cho từng đối tượng sử dụng (tác nhân). Mỗi chức năng được gọi là một "ca sử dụng" (use case), và được kết nối với các tác nhân có quyền thực hiện hành động đó. Trong hệ thống Mutants Arena, các tác nhân chính bao gồm:

Bảng 2.1 Bảng các tác nhân tham gia hệ thống

| Tên tác nhân | Vai trò |
|---|---|
| Khách | Người dùng chưa xác thực, thực hiện các thao tác khởi tạo như đăng ký tài khoản và đăng nhập để bắt đầu truy cập hệ thống. |
| Người chơi | Người dùng đã xác thực, trực tiếp tham gia các hoạt động gameplay bao gồm chiến đấu, phát triển nhân vật, quản lý vật phẩm và trang bị, tương tác với NPC, quản lý nhiệm vụ, tham gia tổ đội và phó bản. |
| Quản trị viên | Nhân sự vận hành và giám sát hệ thống, có quyền quản lý gameplay server, theo dõi trạng thái dịch vụ, quản lý dữ liệu xếp hạng và thực hiện các thao tác quản trị được phân quyền. |
| Máy chủ | Thành phần backend (gameplay server), chịu trách nhiệm tự đăng ký trạng thái hoạt động, đồng bộ cấu hình map đang host và xử lý phân phối phần thưởng khi phiên phó bản kết thúc. |

## 4. Danh mục use case chức năng

Bảng 2. Danh mục use case chức năng mới nhất

| Mã | Tên use case | Nhóm chức năng | Tác nhân chính | Mô tả ngắn |
|---|---|---|---|---|
| UC01 | Đăng ký tài khoản | Tài khoản và gameplay | Khách | Khách tạo tài khoản mới để bắt đầu sử dụng hệ thống. |
| UC02 | Đăng nhập và vào game | Tài khoản và gameplay | Khách, Người chơi | Người dùng xác thực tài khoản, chọn nhân vật và vào thế giới game. |
| UC03 | Di chuyển và chuyển map | Tài khoản và gameplay | Người chơi | Người chơi di chuyển qua portal để sang map hoặc khu vực mới. |
| UC04 | Chiến đấu và sử dụng kỹ năng | Tài khoản và gameplay | Người chơi | Người chơi chiến đấu với quái hoặc boss bằng đòn đánh thường và kỹ năng. |
| UC05 | Quản lý túi đồ và trang bị | Tài khoản và gameplay | Người chơi | Người chơi xem, sử dụng và thay đổi trang bị trong túi đồ cá nhân. |
| UC06 | Nâng cấp trang bị | Tài khoản và gameplay | Người chơi | Người chơi nâng cấp trang bị tại NPC hoặc giao diện cường hóa chuyên dụng. |
| UC07 | Phát triển Gene và Hybrid | Phát triển nhân vật | Người chơi | Người chơi phát triển hệ Gene của nhân vật để mở khóa và tăng sức mạnh chiến đấu. |
| UC08 | Phân bổ tiềm năng và kỹ năng | Phát triển nhân vật | Người chơi | Người chơi phân bổ điểm chỉ số và sắp xếp kỹ năng phù hợp với lối chơi. |
| UC09 | Tương tác NPC và mua vật phẩm | Phát triển nhân vật | Người chơi | Người chơi tương tác với NPC để mở dịch vụ, mua vật phẩm hoặc dùng các tiện ích hỗ trợ. |
| UC10 | Quản lý nhiệm vụ | Phát triển nhân vật | Người chơi | Người chơi nhận, theo dõi và hoàn tất nhiệm vụ để nhận thưởng và mở khóa nội dung mới. |
| UC11 | Quản lý bạn bè | Tương tác và hoạt động | Người chơi | Người chơi quản lý danh sách bạn bè và trạng thái kết nối xã hội trong game. |
| UC12 | Quản lý tổ đội và chat | Tương tác và hoạt động | Người chơi | Người chơi tạo party, quản lý thành viên và trao đổi thông tin qua các kênh chat phù hợp. |
| UC13 | Tham gia và hoàn tất phó bản | Tương tác và hoạt động | Người chơi, Máy chủ | Người chơi hoặc tổ đội tham gia dungeon, vượt qua các wave và nhận phần thưởng khi hoàn thành. |
| UC14 | Xem leaderboard | Tương tác và hoạt động | Người chơi, Quản trị viên | Người chơi theo dõi thứ hạng, còn quản trị viên có thể làm mới hoặc reset dữ liệu xếp hạng. |
| UC15 | Quản lý gameplay server | Vận hành kỹ thuật | Quản trị viên, Máy chủ | Gameplay server tự đăng ký trạng thái hoạt động và được quản trị viên giám sát trong quá trình vận hành. |
| UC16 | Host map và phát thưởng dungeon | Vận hành kỹ thuật | Quản trị viên, Máy chủ | Gameplay server đồng bộ cấu hình map đang host và xử lý phần thưởng khi dungeon kết thúc. |

## 5. Phân nhóm chức năng

### 5.1. Tài khoản và gameplay

Nhóm chức năng nền tảng, bao phủ toàn bộ quá trình từ tạo tài khoản, đăng nhập, di chuyển đến chiến đấu và quản lý vật phẩm cơ bản.

Bảng 3. Danh sách use case thuộc nhóm tài khoản và gameplay

| Mã | Tên use case | Mục tiêu nghiệp vụ | Sơ đồ chi tiết |
|---|---|---|---|
| UC01 | Đăng ký tài khoản | Khách tạo tài khoản mới để bắt đầu sử dụng hệ thống. | [Mở sơ đồ](uc01_usecase.drawio) |
| UC02 | Đăng nhập và vào game | Người dùng xác thực tài khoản, chọn nhân vật và vào thế giới game. | [Mở sơ đồ](uc02_usecase.drawio) |
| UC03 | Di chuyển và chuyển map | Người chơi di chuyển qua portal để sang map hoặc khu vực mới. | [Mở sơ đồ](uc03_usecase.drawio) |
| UC04 | Chiến đấu và sử dụng kỹ năng | Người chơi chiến đấu với quái hoặc boss bằng đòn đánh thường và kỹ năng. | [Mở sơ đồ](uc04_usecase.drawio) |
| UC05 | Quản lý túi đồ và trang bị | Người chơi xem, sử dụng và thay đổi trang bị trong túi đồ cá nhân. | [Mở sơ đồ](uc05_usecase.drawio) |
| UC06 | Nâng cấp trang bị | Người chơi nâng cấp trang bị tại NPC hoặc giao diện cường hóa chuyên dụng. | [Mở sơ đồ](uc06_usecase.drawio) |

### 5.2. Phát triển nhân vật

Nhóm chức năng phục vụ tiến trình phát triển sức mạnh nhân vật thông qua Gene, kỹ năng, NPC hỗ trợ và hệ thống nhiệm vụ.

Bảng 4. Danh sách use case thuộc nhóm phát triển nhân vật

| Mã | Tên use case | Mục tiêu nghiệp vụ | Sơ đồ chi tiết |
|---|---|---|---|
| UC07 | Phát triển Gene và Hybrid | Người chơi phát triển hệ Gene của nhân vật để mở khóa và tăng sức mạnh chiến đấu. | [Mở sơ đồ](uc07_usecase.drawio) |
| UC08 | Phân bổ tiềm năng và kỹ năng | Người chơi phân bổ điểm chỉ số và sắp xếp kỹ năng phù hợp với lối chơi. | [Mở sơ đồ](uc08_usecase.drawio) |
| UC09 | Tương tác NPC và mua vật phẩm | Người chơi tương tác với NPC để mở dịch vụ, mua vật phẩm hoặc dùng các tiện ích hỗ trợ. | [Mở sơ đồ](uc09_usecase.drawio) |
| UC10 | Quản lý nhiệm vụ | Người chơi nhận, theo dõi và hoàn tất nhiệm vụ để nhận thưởng và mở khóa nội dung mới. | [Mở sơ đồ](uc10_usecase.drawio) |

### 5.3. Tương tác và hoạt động

Nhóm chức năng xã hội và hoạt động tập thể, cho phép người chơi kết nối với nhau, tham gia phó bản và theo dõi thứ hạng.

Bảng 5. Danh sách use case thuộc nhóm tương tác và hoạt động

| Mã | Tên use case | Mục tiêu nghiệp vụ | Sơ đồ chi tiết |
|---|---|---|---|
| UC11 | Quản lý bạn bè | Người chơi quản lý danh sách bạn bè và trạng thái kết nối xã hội trong game. | [Mở sơ đồ](uc11_usecase.drawio) |
| UC12 | Quản lý tổ đội và chat | Người chơi tạo party, quản lý thành viên và trao đổi thông tin qua các kênh chat phù hợp. | [Mở sơ đồ](uc12_usecase.drawio) |
| UC13 | Tham gia và hoàn tất phó bản | Người chơi hoặc tổ đội tham gia dungeon, vượt qua các wave và nhận phần thưởng khi hoàn thành. | [Mở sơ đồ](uc13_usecase.drawio) |
| UC14 | Xem leaderboard | Người chơi theo dõi thứ hạng, còn quản trị viên có thể làm mới hoặc reset dữ liệu xếp hạng. | [Mở sơ đồ](uc14_usecase.drawio) |

### 5.4. Vận hành kỹ thuật

Nhóm chức năng hỗ trợ lớp vận hành backend, bảo đảm gameplay server, map host và cơ chế phát thưởng phó bản hoạt động ổn định.

Bảng 6. Danh sách use case thuộc nhóm vận hành kỹ thuật

| Mã | Tên use case | Mục tiêu nghiệp vụ | Sơ đồ chi tiết |
|---|---|---|---|
| UC15 | Quản lý gameplay server | Gameplay server tự đăng ký trạng thái hoạt động và được quản trị viên giám sát trong quá trình vận hành. | [Mở sơ đồ](uc15_usecase.drawio) |
| UC16 | Host map và phát thưởng dungeon | Gameplay server đồng bộ cấu hình map đang host và xử lý phần thưởng khi dungeon kết thúc. | [Mở sơ đồ](uc16_usecase.drawio) |

## 6. Quan hệ use case trọng tâm

Bảng 7. Các quan hệ «include» và «extend» trong sơ đồ tổng quát

| Use case nguồn | Use case đích | Loại quan hệ | Ý nghĩa |
|---|---|---|---|
| Nâng cấp trang bị | Quản lý túi đồ và trang bị | «include» | Use case nâng cấp trang bị luôn cần truy xuất và kiểm tra dữ liệu vật phẩm trong túi đồ trước khi xử lý cường hóa. |
| Host map và phát thưởng dungeon | Tham gia và hoàn tất phó bản | «extend» | Phát thưởng dungeon chỉ phát sinh khi một phiên phó bản đã được khởi tạo, hoàn tất và tổng hợp kết quả hợp lệ. |

## 7. Tệp bàn giao liên quan

Hình 1. Sơ đồ use case tổng quát: [mutants_arena_usecase_overview.drawio](mutants_arena_usecase_overview.drawio)

Bảng 8. Danh mục tệp bàn giao use case

| Nội dung | Tệp |
|---|---|
| Sơ đồ use case tổng quát | [mutants_arena_usecase_overview.drawio](mutants_arena_usecase_overview.drawio) |
| Sơ đồ tổng quát ở dạng PlantUML | [mutants_arena_usecase_overview.puml](mutants_arena_usecase_overview.puml) |
| Tài liệu use case tổng quan | [mutants_arena_usecase_tonghop.md](mutants_arena_usecase_tonghop.md) |
| Đặc tả use case chi tiết | [mutants_arena_usecase_dacta.md](mutants_arena_usecase_dacta.md) |