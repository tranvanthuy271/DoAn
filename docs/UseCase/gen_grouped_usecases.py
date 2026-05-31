"""
Sinh bộ use case chức năng rút gọn cho Mutants Arena.

Đầu ra:
- 01 sơ đồ tổng quát với 16 use case chức năng chính
- 16 sơ đồ use case riêng lẻ theo mẫu báo cáo
- 01 tài liệu use case tổng quan
- 01 file đặc tả use case đã gộp

Chạy:
    python gen_grouped_usecases.py
"""

from __future__ import annotations

import html
import os
from typing import Iterable


OUT_DIR = os.path.dirname(os.path.abspath(__file__))
OVERVIEW_DRAWIO = os.path.join(OUT_DIR, "mutants_arena_usecase_overview.drawio")
OVERVIEW_PUML = os.path.join(OUT_DIR, "mutants_arena_usecase_overview.puml")
DOC_FILE = os.path.join(OUT_DIR, "mutants_arena_usecase_tonghop.md")
SPEC_FILE = os.path.join(OUT_DIR, "mutants_arena_usecase_dacta.md")


ACTORS = {
    "guest": "Khách",
    "player": "Người chơi",
    "admin": "Quản trị viên",
    "server": "Máy chủ",
}


ACTOR_DESCRIPTIONS = {
    "guest": "Người dùng chưa xác thực, thực hiện đăng ký và khởi tạo truy cập ban đầu vào hệ thống.",
    "player": "Người dùng đã đăng nhập, trực tiếp tham gia các hoạt động gameplay và tương tác trong game.",
    "admin": "Nhân sự vận hành hệ thống, giám sát dịch vụ và thực hiện các thao tác quản trị được phân quyền.",
    "server": "Thành phần backend hoặc gameplay server, chịu trách nhiệm xử lý đồng bộ dữ liệu và vận hành phiên chơi.",
}


GROUP_DESCRIPTIONS = {
    "Tài khoản và gameplay": "Nhóm chức năng nền tảng, bao phủ toàn bộ quá trình từ tạo tài khoản, đăng nhập, di chuyển đến chiến đấu và quản lý vật phẩm cơ bản.",
    "Phát triển nhân vật": "Nhóm chức năng phục vụ tiến trình phát triển sức mạnh nhân vật thông qua Gene, kỹ năng, NPC hỗ trợ và hệ thống nhiệm vụ.",
    "Tương tác và hoạt động": "Nhóm chức năng xã hội và hoạt động tập thể, cho phép người chơi kết nối với nhau, tham gia phó bản và theo dõi thứ hạng.",
    "Vận hành kỹ thuật": "Nhóm chức năng hỗ trợ lớp vận hành backend, bảo đảm gameplay server, map host và cơ chế phát thưởng phó bản hoạt động ổn định.",
}


USE_CASES = [
    {
        "id": "uc01",
        "code": "UC01",
        "name": "Đăng ký tài khoản",
        "group": "Tài khoản và gameplay",
        "actors": ["guest"],
        "related": [
            {"name": "Nhập thông tin đăng ký", "relation": "include"},
            {"name": "Kiểm tra hợp lệ", "relation": "include"},
            {"name": "Tạo tài khoản", "relation": "include"},
        ],
        "description": "Khách tạo tài khoản mới để bắt đầu sử dụng hệ thống.",
        "preconditions": "Khách chưa có tài khoản và có kết nối mạng ổn định.",
        "postconditions": "Tài khoản mới được tạo thành công và sẵn sàng để đăng nhập.",
        "main_flow": [
            "Khách mở màn hình đăng ký từ giao diện chính.",
            "Hệ thống hiển thị biểu mẫu gồm tên đăng nhập, mật khẩu và email.",
            "Khách nhập thông tin và xác nhận gửi đăng ký.",
            "Hệ thống kiểm tra tính hợp lệ của dữ liệu và phát hiện trùng lặp nếu có.",
            "Hệ thống mã hóa mật khẩu và lưu tài khoản vào cơ sở dữ liệu.",
            "Hệ thống trả thông báo thành công và chuyển người dùng sang màn hình đăng nhập.",
        ],
        "alternate_flow": [
            "Tên đăng nhập đã tồn tại thì hệ thống yêu cầu nhập tên khác.",
            "Thiếu trường bắt buộc hoặc mật khẩu không hợp lệ thì biểu mẫu hiển thị lỗi tương ứng.",
        ],
    },
    {
        "id": "uc02",
        "code": "UC02",
        "name": "Đăng nhập và vào game",
        "group": "Tài khoản và gameplay",
        "actors": ["guest", "player"],
        "related": [
            {"name": "Xác thực tài khoản", "relation": "include"},
            {"name": "Khởi tạo phiên đăng nhập", "relation": "include"},
            {"name": "Chọn hoặc tạo nhân vật", "relation": "include"},
            {"name": "Kết nối vào máy chủ", "relation": "include"},
        ],
        "description": "Người dùng xác thực tài khoản, chọn nhân vật và vào thế giới game.",
        "preconditions": "Tài khoản đã tồn tại và máy chủ game đang sẵn sàng kết nối.",
        "postconditions": "Phiên đăng nhập hợp lệ được tạo và nhân vật xuất hiện trong game.",
        "main_flow": [
            "Người dùng nhập thông tin đăng nhập trên màn hình xác thực.",
            "Hệ thống xác thực tên đăng nhập và mật khẩu.",
            "Hệ thống tạo token phiên và gửi về cho client.",
            "Người dùng chọn nhân vật hiện có hoặc tạo nhanh nhân vật mới nếu chưa có.",
            "Client dùng thông tin phiên để kết nối đến gameplay server.",
            "Máy chủ nạp dữ liệu nhân vật và đưa người chơi vào map khởi đầu.",
        ],
        "alternate_flow": [
            "Sai thông tin đăng nhập thì hệ thống từ chối và yêu cầu nhập lại.",
            "Kết nối đến gameplay server thất bại thì client thông báo để người chơi thử lại.",
        ],
    },
    {
        "id": "uc03",
        "code": "UC03",
        "name": "Di chuyển và chuyển map",
        "group": "Tài khoản và gameplay",
        "actors": ["player"],
        "related": [
            {"name": "Tương tác portal", "relation": "include"},
            {"name": "Kiểm tra điều kiện vào map", "relation": "include"},
            {"name": "Chuyển zone", "relation": "include"},
        ],
        "description": "Người chơi di chuyển qua portal để sang map hoặc khu vực mới.",
        "preconditions": "Nhân vật đang ở gần portal và đáp ứng điều kiện vào khu vực đích.",
        "postconditions": "Người chơi được đưa sang map mới và hiển thị đúng trong zone mới.",
        "main_flow": [
            "Người chơi tiến vào vùng tương tác của portal.",
            "Hệ thống hiển thị gợi ý xác nhận di chuyển sang khu vực đích.",
            "Người chơi xác nhận thao tác chuyển map.",
            "Máy chủ kiểm tra điều kiện như level, nhiệm vụ hoặc giới hạn số người.",
            "Hệ thống tải map mới và cập nhật zone của nhân vật.",
            "Nhân vật xuất hiện tại vị trí spawn tương ứng của map đích.",
        ],
        "alternate_flow": [
            "Chưa đủ điều kiện vào map thì hệ thống từ chối và hiển thị lý do.",
            "Quá trình tải map lỗi thì hệ thống giữ nguyên vị trí hiện tại và thông báo thử lại.",
        ],
    },
    {
        "id": "uc04",
        "code": "UC04",
        "name": "Chiến đấu và sử dụng kỹ năng",
        "group": "Tài khoản và gameplay",
        "actors": ["player"],
        "related": [
            {"name": "Tấn công thường", "relation": "include"},
            {"name": "Kích hoạt kỹ năng", "relation": "include"},
            {"name": "Đồng bộ trạng thái chiến đấu", "relation": "include"},
        ],
        "description": "Người chơi chiến đấu với quái hoặc boss bằng đòn đánh thường và kỹ năng.",
        "preconditions": "Nhân vật còn sống, đang ở khu vực có mục tiêu chiến đấu.",
        "postconditions": "Sát thương, trạng thái và phần thưởng chiến đấu được cập nhật chính xác.",
        "main_flow": [
            "Người chơi chọn mục tiêu hoặc thực hiện tấn công trong phạm vi cho phép.",
            "Client gửi yêu cầu đánh thường hoặc dùng kỹ năng lên máy chủ.",
            "Máy chủ kiểm tra cooldown, mana và điều kiện mục tiêu.",
            "Máy chủ tính sát thương và áp dụng kết quả lên đối tượng liên quan.",
            "Hệ thống đồng bộ vị trí, animation và trạng thái cho các client quan sát.",
            "Nếu mục tiêu bị hạ gục thì phần thưởng kinh nghiệm và vật phẩm được phát cho người chơi.",
        ],
        "alternate_flow": [
            "Kỹ năng đang hồi chiêu hoặc không đủ tài nguyên thì yêu cầu bị từ chối.",
            "Mục tiêu đã rời khỏi phạm vi hợp lệ thì đòn đánh không được áp dụng.",
        ],
    },
    {
        "id": "uc05",
        "code": "UC05",
        "name": "Quản lý túi đồ và trang bị",
        "group": "Tài khoản và gameplay",
        "actors": ["player"],
        "related": [
            {"name": "Xem túi đồ", "relation": "include"},
            {"name": "Trang bị vật phẩm", "relation": "include"},
            {"name": "Sử dụng vật phẩm", "relation": "include"},
        ],
        "description": "Người chơi xem, sử dụng và thay đổi trang bị trong túi đồ cá nhân.",
        "preconditions": "Nhân vật đã đăng nhập và có vật phẩm trong kho đồ hoặc trang bị đang dùng.",
        "postconditions": "Túi đồ và chỉ số nhân vật phản ánh đúng thay đổi mới nhất.",
        "main_flow": [
            "Người chơi mở giao diện túi đồ từ HUD hoặc phím tắt.",
            "Hệ thống hiển thị danh sách vật phẩm và thông tin cơ bản từng ô.",
            "Người chơi chọn một vật phẩm để xem chi tiết hoặc thực hiện thao tác.",
            "Nếu người chơi chọn trang bị, hệ thống kiểm tra slot và điều kiện sử dụng.",
            "Nếu người chơi chọn dùng vật phẩm tiêu hao, hiệu ứng được áp dụng ngay.",
            "Hệ thống cập nhật túi đồ, trang bị và chỉ số nhân vật sau khi thao tác hoàn tất.",
        ],
        "alternate_flow": [
            "Vật phẩm không đủ điều kiện sử dụng thì hệ thống chặn thao tác và nêu rõ yêu cầu.",
            "Túi đồ đầy khi nhận thêm vật phẩm thì hệ thống từ chối thêm mới và thông báo cho người chơi.",
        ],
    },
    {
        "id": "uc06",
        "code": "UC06",
        "name": "Nâng cấp trang bị",
        "group": "Tài khoản và gameplay",
        "actors": ["player"],
        "related": [
            {"name": "Chọn trang bị cần nâng cấp", "relation": "include"},
            {"name": "Kiểm tra nguyên liệu", "relation": "include"},
            {"name": "Thực hiện cường hóa", "relation": "include"},
        ],
        "description": "Người chơi nâng cấp trang bị tại NPC hoặc giao diện cường hóa chuyên dụng.",
        "preconditions": "Người chơi có trang bị phù hợp, đủ nguyên liệu và đủ vàng.",
        "postconditions": "Trang bị được cập nhật cấp cường hóa và trừ đúng tài nguyên liên quan.",
        "main_flow": [
            "Người chơi mở giao diện nâng cấp trang bị.",
            "Hệ thống hiển thị các vật phẩm có thể cường hóa trong túi đồ.",
            "Người chơi chọn trang bị mục tiêu và xem yêu cầu nguyên liệu.",
            "Hệ thống kiểm tra số lượng nguyên liệu, vàng và điều kiện nâng cấp.",
            "Người chơi xác nhận thao tác nâng cấp.",
            "Hệ thống xử lý kết quả và cập nhật lại chỉ số của vật phẩm sau khi hoàn tất.",
        ],
        "alternate_flow": [
            "Thiếu nguyên liệu hoặc vàng thì thao tác bị từ chối.",
            "Nâng cấp thất bại thì hệ thống áp dụng đúng quy tắc rủi ro đã cấu hình.",
        ],
    },
    {
        "id": "uc07",
        "code": "UC07",
        "name": "Phát triển Gene và Hybrid",
        "group": "Phát triển nhân vật",
        "actors": ["player"],
        "related": [
            {"name": "Nâng Gene chính", "relation": "include"},
            {"name": "Nâng Gene phụ", "relation": "include"},
            {"name": "Dung hợp Hybrid Gene", "relation": "extend"},
        ],
        "description": "Người chơi phát triển hệ Gene của nhân vật để mở khóa và tăng sức mạnh chiến đấu.",
        "preconditions": "Người chơi có đủ điểm Gene hoặc tài nguyên dung hợp tương ứng.",
        "postconditions": "Cấp Gene và hiệu ứng Hybrid của nhân vật được cập nhật theo lựa chọn mới.",
        "main_flow": [
            "Người chơi mở giao diện Gene Evolution.",
            "Hệ thống hiển thị Gene chính, Gene phụ và các điều kiện nâng cấp hiện tại.",
            "Người chơi chọn nhánh Gene muốn nâng hoặc mở khóa.",
            "Hệ thống kiểm tra điểm Gene và điều kiện mở slot tương ứng.",
            "Nếu đủ điều kiện, hệ thống cập nhật cấp Gene và hiệu ứng mới cho nhân vật.",
            "Khi đã đủ vật liệu cần thiết, người chơi có thể thực hiện dung hợp để tạo Hybrid Gene mới.",
        ],
        "alternate_flow": [
            "Không đủ điểm Gene thì thao tác nâng cấp bị chặn và hiển thị lượng còn thiếu.",
            "Công thức dung hợp không hợp lệ thì hệ thống từ chối tạo Hybrid Gene.",
        ],
    },
    {
        "id": "uc08",
        "code": "UC08",
        "name": "Phân bổ tiềm năng và kỹ năng",
        "group": "Phát triển nhân vật",
        "actors": ["player"],
        "related": [
            {"name": "Cộng điểm tiềm năng", "relation": "include"},
            {"name": "Gắn kỹ năng vào thanh nhanh", "relation": "include"},
            {"name": "Lưu cấu hình nhân vật", "relation": "include"},
        ],
        "description": "Người chơi phân bổ điểm chỉ số và sắp xếp kỹ năng phù hợp với lối chơi.",
        "preconditions": "Nhân vật còn điểm tiềm năng hoặc có kỹ năng khả dụng để sắp xếp.",
        "postconditions": "Bộ chỉ số và thanh kỹ năng của nhân vật được lưu theo cấu hình mới.",
        "main_flow": [
            "Người chơi mở bảng chỉ số và kỹ năng của nhân vật.",
            "Hệ thống hiển thị các chỉ số hiện tại, điểm còn lại và danh sách kỹ năng khả dụng.",
            "Người chơi cộng điểm vào các chỉ số mong muốn.",
            "Người chơi kéo thả kỹ năng vào các vị trí trên thanh kỹ năng nhanh.",
            "Người chơi xác nhận lưu cấu hình vừa chỉnh sửa.",
            "Hệ thống cập nhật lại chỉ số, kỹ năng đang trang bị và HUD chiến đấu.",
        ],
        "alternate_flow": [
            "Cộng quá số điểm hiện có thì hệ thống không cho phép xác nhận.",
            "Kỹ năng chưa mở khóa hoặc không phù hợp ô gắn thì không thể thả vào thanh nhanh.",
        ],
    },
    {
        "id": "uc09",
        "code": "UC09",
        "name": "Tương tác NPC và mua vật phẩm",
        "group": "Phát triển nhân vật",
        "actors": ["player"],
        "related": [
            {"name": "Mở menu NPC", "relation": "include"},
            {"name": "Xem cửa hàng", "relation": "include"},
            {"name": "Mua vật phẩm", "relation": "include"},
        ],
        "description": "Người chơi tương tác với NPC để mở dịch vụ, mua vật phẩm hoặc dùng các tiện ích hỗ trợ.",
        "preconditions": "Người chơi đang ở trong phạm vi tương tác hợp lệ của NPC.",
        "postconditions": "Giao dịch với NPC được ghi nhận và túi đồ của người chơi được cập nhật.",
        "main_flow": [
            "Người chơi tiếp cận NPC và kích hoạt tương tác.",
            "Hệ thống hiển thị menu động theo loại NPC hiện tại.",
            "Người chơi chọn chức năng mua vật phẩm hoặc dịch vụ mong muốn.",
            "Hệ thống hiển thị danh sách hàng hóa, giá bán và số lượng cần mua.",
            "Người chơi xác nhận giao dịch.",
            "Hệ thống trừ vàng, thêm vật phẩm hoặc áp dụng dịch vụ tương ứng cho nhân vật.",
        ],
        "alternate_flow": [
            "Không đủ vàng hoặc vật phẩm đã hết thì giao dịch bị từ chối.",
            "NPC thuộc loại hỗ trợ đặc biệt thì hệ thống mở dịch vụ tương ứng thay cho cửa hàng vật phẩm.",
        ],
    },
    {
        "id": "uc10",
        "code": "UC10",
        "name": "Quản lý nhiệm vụ",
        "group": "Phát triển nhân vật",
        "actors": ["player"],
        "related": [
            {"name": "Nhận nhiệm vụ", "relation": "include"},
            {"name": "Theo dõi tiến độ", "relation": "include"},
            {"name": "Hoàn thành nhiệm vụ", "relation": "include"},
        ],
        "description": "Người chơi nhận, theo dõi và hoàn tất nhiệm vụ để nhận thưởng và mở khóa nội dung mới.",
        "preconditions": "Nhân vật đáp ứng điều kiện nhận nhiệm vụ tương ứng.",
        "postconditions": "Tiến độ nhiệm vụ được cập nhật và phần thưởng được cấp đúng sau khi hoàn thành.",
        "main_flow": [
            "Người chơi tương tác với NPC hoặc giao diện nhiệm vụ để nhận quest mới.",
            "Hệ thống hiển thị mô tả, mục tiêu và phần thưởng của nhiệm vụ.",
            "Người chơi xác nhận nhận nhiệm vụ và quest được thêm vào nhật ký.",
            "Trong quá trình chơi, hệ thống tự động cập nhật tiến độ hoàn thành các mục tiêu.",
            "Khi đã đủ điều kiện, người chơi quay lại NPC hoặc giao diện để nộp nhiệm vụ.",
            "Hệ thống cấp thưởng và cập nhật trạng thái chuỗi nhiệm vụ tiếp theo nếu có.",
        ],
        "alternate_flow": [
            "Người chơi chưa đủ điều kiện level hoặc chưa hoàn thành quest trước đó thì không thể nhận nhiệm vụ mới.",
            "Người chơi có thể hủy nhiệm vụ nếu hệ thống cho phép và tiến độ sẽ bị xóa.",
        ],
    },
    {
        "id": "uc11",
        "code": "UC11",
        "name": "Quản lý bạn bè",
        "group": "Tương tác và hoạt động",
        "actors": ["player"],
        "related": [
            {"name": "Gửi lời mời kết bạn", "relation": "include"},
            {"name": "Phản hồi lời mời", "relation": "include"},
            {"name": "Xóa bạn bè", "relation": "include"},
        ],
        "description": "Người chơi quản lý danh sách bạn bè và trạng thái kết nối xã hội trong game.",
        "preconditions": "Người chơi đang online và có thể truy cập bảng bạn bè.",
        "postconditions": "Danh sách bạn bè được cập nhật đồng bộ ở cả hai phía liên quan.",
        "main_flow": [
            "Người chơi mở bảng bạn bè từ giao diện xã hội.",
            "Hệ thống hiển thị danh sách bạn bè cùng trạng thái online và map hiện tại.",
            "Người chơi nhập tên người nhận và gửi lời mời kết bạn.",
            "Người nhận nhận được thông báo phản hồi lời mời.",
            "Nếu chấp nhận, hệ thống thêm hai bên vào danh sách bạn bè của nhau.",
            "Người chơi có thể tiếp tục xóa bạn hoặc xem lại trạng thái của từng người trong danh sách.",
        ],
        "alternate_flow": [
            "Người nhận không tồn tại hoặc đã là bạn thì hệ thống từ chối lời mời.",
            "Người nhận từ chối lời mời thì yêu cầu kết bạn kết thúc mà không thay đổi dữ liệu.",
        ],
    },
    {
        "id": "uc12",
        "code": "UC12",
        "name": "Quản lý tổ đội và chat",
        "group": "Tương tác và hoạt động",
        "actors": ["player"],
        "related": [
            {"name": "Tạo tổ đội", "relation": "include"},
            {"name": "Quản lý thành viên", "relation": "include"},
            {"name": "Chat đa kênh", "relation": "include"},
        ],
        "description": "Người chơi tạo party, quản lý thành viên và trao đổi thông tin qua các kênh chat phù hợp.",
        "preconditions": "Người chơi đang online và không bị chặn tính năng xã hội.",
        "postconditions": "Party và nội dung trao đổi được cập nhật cho đúng thành viên hoặc đúng kênh nhận tin.",
        "main_flow": [
            "Người chơi mở giao diện xã hội và chọn tạo tổ đội hoặc mời thành viên.",
            "Hệ thống tạo party mới và gán vai trò trưởng nhóm cho người khởi tạo.",
            "Người chơi mời thêm thành viên từ danh sách bạn bè hoặc người chơi gần đó.",
            "Sau khi party được hình thành, bảng tổ đội hiển thị trạng thái của từng thành viên.",
            "Người chơi sử dụng khung chat để trao đổi trong các kênh chung hoặc kênh tổ đội.",
            "Hệ thống phân phối tin nhắn đến đúng phạm vi người nhận và cập nhật giao diện xã hội liên quan.",
        ],
        "alternate_flow": [
            "Người được mời đang ở tổ đội khác thì lời mời bị từ chối.",
            "Người chơi gửi tin sai kênh hoặc vi phạm bộ lọc chat thì hệ thống chặn và thông báo lỗi.",
        ],
    },
    {
        "id": "uc13",
        "code": "UC13",
        "name": "Tham gia và hoàn tất phó bản",
        "group": "Tương tác và hoạt động",
        "actors": ["player", "server"],
        "related": [
            {"name": "Chọn độ khó phó bản", "relation": "include"},
            {"name": "Spawn wave quái", "relation": "include"},
            {"name": "Nhận thưởng hoàn thành", "relation": "include"},
        ],
        "description": "Người chơi hoặc tổ đội tham gia dungeon, vượt qua các wave và nhận phần thưởng khi hoàn thành.",
        "preconditions": "Người chơi đáp ứng điều kiện vào dungeon và gameplay server đang còn slot hoạt động.",
        "postconditions": "Kết quả phó bản được ghi nhận và phần thưởng được phát theo trạng thái hoàn thành.",
        "main_flow": [
            "Người chơi mở giao diện hoặc portal dungeon và chọn loại phó bản muốn tham gia.",
            "Hệ thống kiểm tra điều kiện về level, lượt vào và trạng thái tổ đội.",
            "Gameplay server khởi tạo phiên dungeon và spawn các wave quái tương ứng.",
            "Người chơi vượt qua từng đợt quái và tiến tới boss cuối.",
            "Khi điều kiện hoàn thành được đáp ứng, hệ thống tổng kết kết quả phó bản.",
            "Hệ thống phát phần thưởng và đưa người chơi rời khỏi dungeon sau khi kết thúc.",
        ],
        "alternate_flow": [
            "Người chơi hoặc tổ đội thất bại trong dungeon thì hệ thống kết thúc phiên với trạng thái thất bại.",
            "Máy chủ gặp lỗi spawn ở một wave thì hệ thống ghi log và xử lý theo cấu hình fallback của dungeon.",
        ],
    },
    {
        "id": "uc14",
        "code": "UC14",
        "name": "Xem leaderboard",
        "group": "Tương tác và hoạt động",
        "actors": ["player", "admin"],
        "related": [
            {"name": "Xem bảng xếp hạng", "relation": "include"},
            {"name": "Lọc theo hạng mục", "relation": "include"},
            {"name": "Reset bảng xếp hạng", "relation": "extend"},
        ],
        "description": "Người chơi theo dõi thứ hạng, còn quản trị viên có thể làm mới hoặc reset dữ liệu xếp hạng.",
        "preconditions": "Dịch vụ leaderboard đang khả dụng và người dùng có quyền truy cập tương ứng.",
        "postconditions": "Bảng xếp hạng hiển thị đúng dữ liệu mới nhất hoặc được reset thành công khi có quyền quản trị.",
        "main_flow": [
            "Người dùng mở giao diện bảng xếp hạng.",
            "Hệ thống tải dữ liệu theo hạng mục mặc định hoặc hạng mục được chọn.",
            "Người dùng chuyển đổi giữa các tab để xem top theo từng tiêu chí.",
            "Hệ thống đánh dấu vị trí hiện tại của nhân vật nếu có mặt trên bảng xếp hạng.",
            "Người dùng có thể yêu cầu làm mới để tải lại dữ liệu mới nhất.",
            "Nếu là quản trị viên, hệ thống cho phép thực hiện thao tác reset theo quyền hạn được cấp.",
        ],
        "alternate_flow": [
            "Dữ liệu tạm thời chưa sẵn sàng thì hệ thống hiển thị trạng thái chờ tải hoặc lấy từ cache gần nhất.",
            "Người dùng không có quyền quản trị thì không thể thực hiện thao tác reset bảng xếp hạng.",
        ],
    },
    {
        "id": "uc15",
        "code": "UC15",
        "name": "Quản lý gameplay server",
        "group": "Vận hành kỹ thuật",
        "actors": ["admin", "server"],
        "related": [
            {"name": "Đăng ký server", "relation": "include"},
            {"name": "Gửi heartbeat", "relation": "include"},
            {"name": "Giải phóng server", "relation": "include"},
        ],
        "description": "Gameplay server tự đăng ký trạng thái hoạt động và được quản trị viên giám sát trong quá trình vận hành.",
        "preconditions": "GameServerApi hoạt động và gameplay server có thông tin xác thực hợp lệ.",
        "postconditions": "Trạng thái hoạt động của gameplay server được cập nhật chính xác trong hệ thống quản trị.",
        "main_flow": [
            "Gameplay server khởi động và gửi yêu cầu đăng ký đến dịch vụ quản lý máy chủ.",
            "Hệ thống xác thực thông tin server và lưu trạng thái online ban đầu.",
            "Server gửi heartbeat theo chu kỳ để cập nhật tình trạng hoạt động.",
            "Quản trị viên theo dõi trạng thái, tải hiện tại và tình trạng map của server.",
            "Khi server cần dừng hoạt động, hệ thống thực hiện quy trình giải phóng hoặc unregister.",
            "Dịch vụ quản lý máy chủ cập nhật lại trạng thái offline và xử lý người chơi còn kết nối nếu cần.",
        ],
        "alternate_flow": [
            "Heartbeat quá thời hạn cho phép thì hệ thống tự đánh dấu server offline.",
            "Xác thực server không hợp lệ thì yêu cầu đăng ký bị từ chối.",
        ],
    },
    {
        "id": "uc16",
        "code": "UC16",
        "name": "Host map và phát thưởng dungeon",
        "group": "Vận hành kỹ thuật",
        "actors": ["admin", "server"],
        "related": [
            {"name": "Đồng bộ map host", "relation": "include"},
            {"name": "Cập nhật spawn config", "relation": "include"},
            {"name": "Phát thưởng dungeon", "relation": "extend"},
        ],
        "description": "Gameplay server đồng bộ cấu hình map đang host và xử lý phần thưởng khi dungeon kết thúc.",
        "preconditions": "Gameplay server đã đăng ký thành công và có thể giao tiếp với dịch vụ backend.",
        "postconditions": "Map host được đồng bộ đúng cấu hình và kết quả dungeon được phát thưởng chính xác.",
        "main_flow": [
            "Gameplay server gửi danh sách map đang host lên hệ thống quản lý.",
            "Quản trị viên hoặc quy trình tự động cập nhật cấu hình spawn cho từng map.",
            "Gameplay server tải cấu hình mới nhất để áp dụng cho quái, boss và đối tượng mạng.",
            "Khi một dungeon kết thúc, server tổng hợp dữ liệu kết quả của người chơi hoặc tổ đội.",
            "Hệ thống backend xử lý phần thưởng, kinh nghiệm và log kết quả dungeon.",
            "Kết quả cuối cùng được đồng bộ lại cho client và lưu vào cơ sở dữ liệu báo cáo.",
        ],
        "alternate_flow": [
            "Cấu hình spawn không hợp lệ thì hệ thống giữ cấu hình cũ và ghi log cảnh báo.",
            "Phát thưởng lỗi do dữ liệu không hợp lệ thì hệ thống dừng thao tác và chuyển sang hàng chờ xử lý lại.",
        ],
    },
]


GROUP_ORDER = [
    "Tài khoản và gameplay",
    "Phát triển nhân vật",
    "Tương tác và hoạt động",
    "Vận hành kỹ thuật",
]


GROUP_CASE_IDS = {
    "Tài khoản và gameplay": ["uc01", "uc02", "uc03", "uc04", "uc05", "uc06"],
    "Phát triển nhân vật": ["uc07", "uc08", "uc09", "uc10"],
    "Tương tác và hoạt động": ["uc11", "uc12", "uc13", "uc14"],
    "Vận hành kỹ thuật": ["uc15", "uc16"],
}


OVERVIEW_RELATIONS = [
    {
        "source": "uc06",
        "target": "uc05",
        "relation": "include",
        "meaning": "Use case nâng cấp trang bị luôn cần truy xuất và kiểm tra dữ liệu vật phẩm trong túi đồ trước khi xử lý cường hóa.",
    },
    {
        "source": "uc16",
        "target": "uc13",
        "relation": "extend",
        "meaning": "Phát thưởng dungeon chỉ phát sinh khi một phiên phó bản đã được khởi tạo, hoàn tất và tổng hợp kết quả hợp lệ.",
    },
]


OBSOLETE_FILES = [
    "uc17_usecase.drawio",
    "uc18_usecase.drawio",
    "uc19_usecase.drawio",
    "uc20_usecase.drawio",
    "unetauth_usecase.drawio",
    "unetsync_usecase.drawio",
    "unetzone_usecase.drawio",
    "unetruntime_usecase.drawio",
]


def xml_escape(text: str) -> str:
    return html.escape(text, quote=True).replace("\n", "&#xa;")


def cell(cid: str, value: str, style: str, x: float, y: float, w: float, h: float, parent: str = "1") -> str:
    return (
        f'        <mxCell id="{cid}" value="{xml_escape(value)}"\n'
        f'          style="{style}"\n'
        f'          vertex="1" parent="{parent}">\n'
        f'          <mxGeometry x="{x}" y="{y}" width="{w}" height="{h}" as="geometry"/>\n'
        f'        </mxCell>\n'
    )


def edge(cid: str, value: str, style: str, source: str, target: str) -> str:
    return (
        f'        <mxCell id="{cid}" value="{xml_escape(value)}"\n'
        f'          style="{style}"\n'
        f'          edge="1" source="{source}" target="{target}" parent="1">\n'
        f'          <mxGeometry relative="1" as="geometry"/>\n'
        f'        </mxCell>\n'
    )


def use_case_by_id(case_id: str) -> dict:
    for case in USE_CASES:
        if case["id"] == case_id:
            return case
    raise KeyError(case_id)


def actor_names(actor_keys: Iterable[str]) -> str:
    return ", ".join(ACTORS[key] for key in actor_keys)


def detail_diagram_filename(case: dict) -> str:
    return f"{case['id']}_usecase.drawio"


def markdown_link(filename: str, label: str | None = None) -> str:
    text = label if label is not None else filename
    return f"[{text}]({filename})"


def relation_label(relation: str) -> str:
    return "«include»" if relation == "include" else "«extend»"


def relation_style() -> str:
    return (
        "dashed=1;dashPattern=4 4;strokeWidth=1.2;endArrow=open;endFill=0;"
        "html=1;fontSize=12;labelBackgroundColor=#ffffff;align=center;verticalAlign=middle;"
    )


def actor_style() -> str:
    return (
        "shape=umlActor;whiteSpace=wrap;html=1;fillColor=#ffffff;strokeColor=#000000;"
        "verticalLabelPosition=bottom;verticalAlign=top;align=center;fontSize=14;outlineConnect=0;"
    )


def main_uc_style() -> str:
    return "ellipse;whiteSpace=wrap;html=1;fontSize=15;fillColor=#ffffff;strokeColor=#000000;shadow=1;"


def sub_uc_style() -> str:
    return "ellipse;whiteSpace=wrap;html=1;fontSize=13;fillColor=#ffffff;strokeColor=#000000;shadow=1;"


def assoc_style() -> str:
    return "endArrow=none;html=1;strokeWidth=1;"


def system_style() -> str:
    return (
        "rounded=0;whiteSpace=wrap;html=1;fontSize=16;fontStyle=1;align=center;verticalAlign=top;"
        "spacingTop=10;fillColor=none;strokeColor=#000000;"
    )


def related_positions(count: int) -> list[tuple[int, int]]:
    lookup = {
        1: [(600, 235)],
        2: [(600, 140), (600, 340)],
        3: [(590, 110), (625, 235), (590, 360)],
        4: [(520, 110), (660, 190), (660, 310), (520, 390)],
    }
    return lookup.get(count, [(600, 235)])


def generate_detail_diagram(case: dict) -> str:
    page_w, page_h = 980, 640
    system_x, system_y, system_w, system_h = 220, 60, 640, 470
    main_x, main_y, main_w, main_h = 300, 235, 260, 64
    actor_x, actor_w, actor_h = 70, 44, 84
    actor_gap = 140
    actors = case["actors"]
    actor_start_y = 105 + max(0, (3 - len(actors)) * 20)

    cells = [
        cell("sys", case["name"], system_style(), system_x, system_y, system_w, system_h),
        cell("main", case["name"], main_uc_style(), main_x, main_y, main_w, main_h),
    ]

    for index, actor_key in enumerate(actors):
        actor_id = f"actor_{actor_key}"
        ay = actor_start_y + index * actor_gap
        cells.append(cell(actor_id, ACTORS[actor_key], actor_style(), actor_x, ay, actor_w, actor_h))
        cells.append(edge(f"edge_actor_{index}", "", assoc_style(), actor_id, "main"))

    related = case["related"]
    positions = related_positions(len(related))
    for index, item in enumerate(related):
        rel_id = f"rel_{index + 1}"
        rx, ry = positions[index]
        cells.append(cell(rel_id, item["name"], sub_uc_style(), rx, ry, 220, 58))
        if item["relation"] == "include":
            cells.append(edge(f"edge_rel_{index}", relation_label("include"), relation_style(), "main", rel_id))
        else:
            cells.append(edge(f"edge_rel_{index}", relation_label("extend"), relation_style(), rel_id, "main"))

    body = "".join(cells)
    return f"""<mxfile host=\"app.diagrams.net\" modified=\"2026-05-24T00:00:00.000Z\" agent=\"GitHub Copilot\" version=\"24.7.17\">\n  <diagram id=\"{case['id']}-detail\" name=\"{xml_escape(case['name'])}\">\n    <mxGraphModel dx=\"1024\" dy=\"768\" grid=\"0\" gridSize=\"10\" guides=\"1\" tooltips=\"1\" connect=\"1\" arrows=\"1\" fold=\"1\" page=\"1\" pageScale=\"1\" pageWidth=\"{page_w}\" pageHeight=\"{page_h}\" math=\"0\" shadow=\"0\">\n      <root>\n        <mxCell id=\"0\"/>\n        <mxCell id=\"1\" parent=\"0\"/>\n{body}      </root>\n    </mxGraphModel>\n  </diagram>\n</mxfile>\n"""


def overview_group_x(group_index: int) -> int:
    return [320, 670, 1020, 1370][group_index]


def overview_group_width(group_index: int) -> int:
    return [300, 300, 300, 250][group_index]


def overview_actor_positions() -> dict[str, tuple[int, int]]:
    return {
        "guest": (105, 135),
        "player": (105, 455),
        "admin": (1755, 250),
        "server": (1755, 590),
    }


def generate_overview_diagram() -> str:
    page_w, page_h = 1950, 980
    system_x, system_y, system_w, system_h = 240, 50, 1450, 840
    case_pos: dict[str, tuple[float, float, float, float]] = {}
    cells: list[str] = [
        cell("system", "Hệ thống Mutants Arena", system_style().replace("fontSize=16", "fontSize=18"), system_x, system_y, system_w, system_h),
    ]

    for group_index, group_name in enumerate(GROUP_ORDER):
        gx = overview_group_x(group_index)
        gw = overview_group_width(group_index)
        cells.append(
            cell(f"group_{group_index}", group_name, "text;whiteSpace=wrap;html=1;fontSize=13;fontStyle=1;align=center;verticalAlign=middle;", gx, 88, gw, 24)
        )
        ids = GROUP_CASE_IDS[group_name]
        count = len(ids)
        top_y = 130
        bottom_y = 760
        if count == 1:
            positions_y = [445]
        else:
            gap = (bottom_y - top_y) / (count - 1)
            positions_y = [top_y + gap * index for index in range(count)]

        for row_index, case_id in enumerate(ids):
            case = use_case_by_id(case_id)
            cx = gx
            cy = positions_y[row_index]
            cw = gw
            ch = 58
            case_pos[case_id] = (cx, cy, cw, ch)
            cells.append(cell(case_id, case["name"], sub_uc_style(), cx, cy, cw, ch))

    actor_positions = overview_actor_positions()
    for actor_key, (ax, ay) in actor_positions.items():
        cells.append(cell(actor_key, ACTORS[actor_key], actor_style(), ax, ay, 48, 84))

    for case in USE_CASES:
        for actor_key in case["actors"]:
            if actor_key == "server" and case["id"] not in {"uc13", "uc15", "uc16"}:
                continue
            if actor_key == "admin" and case["id"] not in {"uc14", "uc15", "uc16"}:
                continue
            if actor_key == "guest" and case["id"] not in {"uc01", "uc02"}:
                continue
            if actor_key == "player" and case["id"] in {"uc15", "uc16"}:
                continue
            cells.append(edge(f"assoc_{actor_key}_{case['id']}", "", assoc_style(), actor_key, case["id"]))

    for index, relation in enumerate(OVERVIEW_RELATIONS):
        cells.append(
            edge(
                f"overview_rel_{index}",
                relation_label(relation["relation"]),
                relation_style(),
                relation["source"],
                relation["target"],
            )
        )

    body = "".join(cells)
    return f"""<mxfile host=\"app.diagrams.net\" modified=\"2026-05-24T00:00:00.000Z\" agent=\"GitHub Copilot\" version=\"24.7.17\">\n  <diagram id=\"overview-usecase\" name=\"UseCaseTongQuat\">\n    <mxGraphModel dx=\"1600\" dy=\"980\" grid=\"0\" gridSize=\"10\" guides=\"1\" tooltips=\"1\" connect=\"1\" arrows=\"1\" fold=\"1\" page=\"1\" pageScale=\"1\" pageWidth=\"{page_w}\" pageHeight=\"{page_h}\" math=\"0\" shadow=\"0\">\n      <root>\n        <mxCell id=\"0\"/>\n        <mxCell id=\"1\" parent=\"0\"/>\n{body}      </root>\n    </mxGraphModel>\n  </diagram>\n</mxfile>\n"""


def generate_overview_puml() -> str:
    lines = [
        "@startuml",
        "left to right direction",
        'skinparam shadowing false',
        'skinparam usecase {',
        '  BackgroundColor white',
        '  BorderColor black',
        '}',
        'actor "Khách" as Guest',
        'actor "Người chơi" as Player',
        'actor "Quản trị viên" as Admin',
        'actor "Máy chủ" as Server',
        'rectangle "Hệ thống Mutants Arena" {',
    ]
    for case in USE_CASES:
        lines.append(f'  usecase "{case["name"]}" as {case["code"]}')
    for relation in OVERVIEW_RELATIONS:
        source = use_case_by_id(relation["source"])["code"]
        target = use_case_by_id(relation["target"])["code"]
        stereotype = "<<include>>" if relation["relation"] == "include" else "<<extend>>"
        lines.append(f'  {source} .> {target} : {stereotype}')
    lines.append('}')

    for case in USE_CASES:
        for actor_key in case["actors"]:
            if actor_key == "server" and case["id"] not in {"uc13", "uc15", "uc16"}:
                continue
            if actor_key == "admin" and case["id"] not in {"uc14", "uc15", "uc16"}:
                continue
            if actor_key == "guest" and case["id"] not in {"uc01", "uc02"}:
                continue
            if actor_key == "player" and case["id"] in {"uc15", "uc16"}:
                continue
            actor_alias = actor_key.capitalize() if actor_key != "player" else "Player"
            if actor_key == "guest":
                actor_alias = "Guest"
            elif actor_key == "admin":
                actor_alias = "Admin"
            elif actor_key == "server":
                actor_alias = "Server"
            lines.append(f"{actor_alias} -- {case['code']}")

    lines.append("@enduml")
    return "\n".join(lines) + "\n"


def related_summary(related: Iterable[dict]) -> str:
    includes = [item["name"] for item in related if item["relation"] == "include"]
    extends = [item["name"] for item in related if item["relation"] == "extend"]
    parts = []
    if includes:
        parts.append("«include»: " + ", ".join(includes))
    if extends:
        parts.append("«extend»: " + ", ".join(extends))
    return "; ".join(parts) if parts else "Không có"


def letter_prefix(index: int) -> str:
    return f"{chr(ord('a') + index)})"


def generate_document_markdown() -> str:
    lines = [
        "# TÀI LIỆU USE CASE TỔNG QUAN — HỆ THỐNG MUTANTS ARENA",
        "",
        "## 1. Mục đích tài liệu",
        "",
        "Tài liệu này trình bày bộ use case chức năng mới nhất của hệ thống Mutants Arena ở mức nghiệp vụ.",
        "Nội dung được dùng để đồng bộ giữa sơ đồ tổng quát, các sơ đồ chi tiết và phần thuyết minh trong báo cáo.",
        "",
        "## 2. Phạm vi và nguyên tắc mô hình hóa",
        "",
        "Phạm vi tài liệu gồm 16 use case chức năng chính, được tổ chức thành bốn nhóm nghiệp vụ.",
        "Các xử lý kỹ thuật nội bộ như xác thực, kiểm tra điều kiện, đồng bộ trạng thái hoặc phát thưởng được biểu diễn bằng quan hệ «include» và «extend» để sơ đồ tổng quát gọn và dễ đọc hơn.",
        "",
        "## 3. Danh sách tác nhân",
        "",
        "Bảng 1. Danh sách tác nhân tham gia hệ thống",
        "",
        "| Tác nhân | Vai trò |",
        "|---|---|",
    ]

    for actor_key in ACTORS:
        lines.append(f"| {ACTORS[actor_key]} | {ACTOR_DESCRIPTIONS[actor_key]} |")

    lines.extend(
        [
            "",
            "## 4. Danh mục use case chức năng",
            "",
            "Bảng 2. Danh mục use case chức năng mới nhất",
            "",
            "| Mã | Tên use case | Nhóm chức năng | Tác nhân chính | Mô tả ngắn |",
            "|---|---|---|---|---|",
        ]
    )

    for case in USE_CASES:
        lines.append(
            f"| {case['code']} | {case['name']} | {case['group']} | {actor_names(case['actors'])} | {case['description']} |"
        )

    lines.extend(
        [
            "",
            "## 5. Phân nhóm chức năng",
            "",
        ]
    )

    table_number = 3
    for group_index, group_name in enumerate(GROUP_ORDER, start=1):
        lines.extend(
            [
                f"### 5.{group_index}. {group_name}",
                "",
                GROUP_DESCRIPTIONS[group_name],
                "",
                f"Bảng {table_number}. Danh sách use case thuộc nhóm {group_name.lower()}",
                "",
                "| Mã | Tên use case | Mục tiêu nghiệp vụ | Sơ đồ chi tiết |",
                "|---|---|---|---|",
            ]
        )
        for case_id in GROUP_CASE_IDS[group_name]:
            case = use_case_by_id(case_id)
            lines.append(
                f"| {case['code']} | {case['name']} | {case['description']} | {markdown_link(detail_diagram_filename(case), 'Mở sơ đồ')} |"
            )
        lines.append("")
        table_number += 1

    lines.extend(
        [
            "## 6. Quan hệ use case trọng tâm",
            "",
            f"Bảng {table_number}. Các quan hệ «include» và «extend» trong sơ đồ tổng quát",
            "",
            "| Use case nguồn | Use case đích | Loại quan hệ | Ý nghĩa |",
            "|---|---|---|---|",
        ]
    )

    for relation in OVERVIEW_RELATIONS:
        source = use_case_by_id(relation["source"])
        target = use_case_by_id(relation["target"])
        lines.append(
            f"| {source['name']} | {target['name']} | {relation_label(relation['relation'])} | {relation['meaning']} |"
        )

    lines.extend(
        [
            "",
            "## 7. Tệp bàn giao liên quan",
            "",
            f"Hình 1. Sơ đồ use case tổng quát: {markdown_link(os.path.basename(OVERVIEW_DRAWIO))}",
            "",
            f"Bảng {table_number + 1}. Danh mục tệp bàn giao use case",
            "",
            "| Nội dung | Tệp |",
            "|---|---|",
            f"| Sơ đồ use case tổng quát | {markdown_link(os.path.basename(OVERVIEW_DRAWIO))} |",
            f"| Sơ đồ tổng quát ở dạng PlantUML | {markdown_link(os.path.basename(OVERVIEW_PUML))} |",
            f"| Tài liệu use case tổng quan | {markdown_link(os.path.basename(DOC_FILE))} |",
            f"| Đặc tả use case chi tiết | {markdown_link(os.path.basename(SPEC_FILE))} |",
        ]
    )
    return "\n".join(lines)


def generate_spec_markdown() -> str:
    lines = [
        "# ĐẶC TẢ USE CASE CHỨC NĂNG — HỆ THỐNG MUTANTS ARENA",
        "",
        "## 1. Mục đích và phạm vi",
        "",
        "Tài liệu này đặc tả chi tiết bộ use case chức năng chính của hệ thống Mutants Arena sau khi đã gộp các xử lý kỹ thuật nội bộ vào bên trong từng chức năng nghiệp vụ tương ứng.",
        "Cách tổ chức này giúp sơ đồ tổng quát gọn hơn, đồng thời vẫn giữ đầy đủ nội dung cần thiết cho phần mô tả nghiệp vụ và trình bày trong báo cáo.",
        "",
        "## 2. Quy ước mô tả",
        "",
        "Bảng 1. Quy ước đọc đặc tả use case",
        "",
        "| Thành phần | Ý nghĩa |",
        "|---|---|",
        "| Tác nhân chính | Đối tượng trực tiếp khởi tạo hoặc tham gia vào luồng xử lý của use case. |",
        "| Tiền điều kiện | Điều kiện phải thỏa mãn trước khi use case được thực hiện. |",
        "| Hậu điều kiện | Trạng thái hệ thống sau khi use case kết thúc thành công. |",
        f"| {relation_label('include')} | Use case con bắt buộc, luôn được thực hiện như một phần của use case chính. |",
        f"| {relation_label('extend')} | Use case mở rộng, chỉ phát sinh khi có điều kiện hoặc ngữ cảnh phù hợp. |",
        "",
        "## 3. Bảng tổng hợp use case chức năng",
        "",
        "Bảng 2. Bảng tổng hợp use case chức năng",
        "",
        "| Mã | Tên use case | Nhóm chức năng | Tác nhân chính |",
        "|---|---|---|---|",
    ]

    for case in USE_CASES:
        lines.append(f"| {case['code']} | {case['name']} | {case['group']} | {actor_names(case['actors'])} |")

    lines.extend(["", "## 4. Đặc tả chi tiết theo nhóm chức năng", ""])

    table_number = 3

    for group_index, group_name in enumerate(GROUP_ORDER, start=1):
        lines.append(f"### 4.{group_index}. Nhóm {group_index} - {group_name}")
        lines.append("")
        lines.append(GROUP_DESCRIPTIONS[group_name])
        lines.append("")
        for case_id in GROUP_CASE_IDS[group_name]:
            case = use_case_by_id(case_id)
            lines.extend(
                [
                    f"#### {case['code']} — {case['name']}",
                    "",
                    f"Bảng {table_number}. Đặc tả use case {case['code']} — {case['name']}",
                    "",
                    "| Thuộc tính | Nội dung |",
                    "|---|---|",
                    f"| **Mã use case** | {case['code']} |",
                    f"| **Tên use case** | {case['name']} |",
                    f"| **Nhóm chức năng** | {case['group']} |",
                    f"| **Mục tiêu** | {case['description']} |",
                    f"| **Tác nhân chính** | {actor_names(case['actors'])} |",
                    f"| **Sự kiện kích hoạt** | {case['main_flow'][0]} |",
                    f"| **Tiền điều kiện** | {case['preconditions']} |",
                    f"| **Hậu điều kiện** | {case['postconditions']} |",
                    f"| **Use case liên quan** | {related_summary(case['related'])} |",
                    f"| **Sơ đồ chi tiết** | {markdown_link(detail_diagram_filename(case))} |",
                    "",
                    "**Luồng chính:**",
                    "",
                ]
            )
            for index, step in enumerate(case["main_flow"], start=1):
                lines.append(f"{index}. {step}")
            lines.append("")
            lines.append("**Luồng thay thế và ngoại lệ:**")
            lines.append("")
            for alt_index, alt in enumerate(case["alternate_flow"]):
                lines.append(f"{letter_prefix(alt_index)} {alt}")
            lines.append("")
            table_number += 1

    lines.extend(
        [
            "## 5. Tổng hợp quan hệ trong sơ đồ tổng quát",
            "",
            f"Bảng {table_number}. Quan hệ trọng tâm trong sơ đồ use case tổng quát",
            "",
            "| Use case nguồn | Use case đích | Loại quan hệ | Ý nghĩa |",
            "|---|---|---|---|",
            "",
        ]
    )

    for relation in OVERVIEW_RELATIONS:
        source = use_case_by_id(relation["source"])
        target = use_case_by_id(relation["target"])
        lines.append(
            f"| {source['name']} | {target['name']} | {relation_label(relation['relation'])} | {relation['meaning']} |"
        )

    return "\n".join(lines)


def write_text_file(path: str, content: str) -> None:
    with open(path, "w", encoding="utf-8") as file:
        file.write(content)


def cleanup_obsolete_files() -> list[str]:
    removed = []
    for filename in OBSOLETE_FILES:
        path = os.path.join(OUT_DIR, filename)
        if os.path.exists(path):
            os.remove(path)
            removed.append(filename)
    return removed


def main() -> None:
    created = []
    for case in USE_CASES:
        filename = detail_diagram_filename(case)
        write_text_file(os.path.join(OUT_DIR, filename), generate_detail_diagram(case))
        created.append(filename)

    write_text_file(OVERVIEW_DRAWIO, generate_overview_diagram())
    write_text_file(OVERVIEW_PUML, generate_overview_puml())
    write_text_file(DOC_FILE, generate_document_markdown())
    write_text_file(SPEC_FILE, generate_spec_markdown())
    removed = cleanup_obsolete_files()

    print(f"Đã tạo {len(created)} sơ đồ use case riêng lẻ.")
    print("Đã cập nhật sơ đồ tổng quát, file PlantUML, tài liệu use case và file đặc tả.")
    if removed:
        print("Đã xóa file cũ không còn dùng:")
        for filename in removed:
            print(f"  - {filename}")


if __name__ == "__main__":
    main()