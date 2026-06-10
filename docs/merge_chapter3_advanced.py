import os
import sys
import re

sys.stdout.reconfigure(encoding='utf-8')

filepath_c3 = r"c:\Hub\DoAn\docs\CHUONG3_BAO_CAO_VIET_LAI.md"
filepath_sec = r"c:\Hub\DoAn\docs\CHUONG3_KIEN_TRUC_THEM_VAO.md"

if not os.path.exists(filepath_c3):
    print(f"Error: {filepath_c3} not found.")
    exit(1)
if not os.path.exists(filepath_sec):
    print(f"Error: {filepath_sec} not found.")
    exit(1)

with open(filepath_c3, 'r', encoding='utf-8') as f:
    c3_content = f.read()

with open(filepath_sec, 'r', encoding='utf-8') as f:
    sec_content = f.read()

# 1. Renumber headings in sec_content to match 3.5, 3.6, 3.7
sec_content = sec_content.replace("## 3.3.2 Tăng cường kiểm soát truy cập và bảo mật hệ thống", "3.5Tăng cường kiểm soát truy cập và bảo mật hệ thống")

# Inject multi-layered security diagram right under the intro paragraph of 3.5
multi_layer_security_target = "đảm bảo rằng việc vượt qua một lớp không tự động mang lại quyền truy cập vào toàn bộ hệ thống."
multi_layer_security_insertion = """đảm bảo rằng việc vượt qua một lớp không tự động mang lại quyền truy cập vào toàn bộ hệ thống.

![Mô hình bảo mật nhiều lớp của hệ thống](extracted_images/image42.jpeg)

*Hình 3.1. Mô hình bảo mật nhiều lớp của hệ thống Mutants Arena*"""

sec_content = sec_content.replace(multi_layer_security_target, multi_layer_security_insertion)

sec_content = sec_content.replace("### a) Kiểm soát truy cập toàn bộ endpoint API bằng thuộc tính `[Authorize]`", "3.5.1Kiểm soát truy cập toàn bộ endpoint API bằng thuộc tính `[Authorize]`")
sec_content = sec_content.replace("### b) Giới hạn tốc độ yêu cầu đăng nhập (Rate Limiting)", "3.5.2Giới hạn tốc độ yêu cầu đăng nhập (Rate Limiting)")

# Inject Zone API key authentication flow diagram
sec_content = sec_content.replace("### c) Xác thực nội bộ Zone Server bằng Zone API Key và so sánh hằng thời gian", "3.5.3Xác thực nội bộ Zone Server bằng Zone API Key và so sánh hằng thời gian")

zone_api_end_text = 'vì nó thuộc scheme `JwtBearer` khác scheme.'
zone_api_replacement = """vì nó thuộc scheme `JwtBearer` khác scheme.

![Luồng xác thực nội bộ Zone Server bằng Zone API Key](extracted_images/image47.jpeg)

*Hình 3.2. Luồng xác thực nội bộ Zone Server bằng Zone API Key*"""

sec_content = sec_content.replace(zone_api_end_text, zone_api_replacement)

# Inject Connection approval flow diagram
sec_content = sec_content.replace("### d) Xác thực kết nối tại tầng transport của NGO Dedicated Server", "3.5.4Xác thực kết nối tại tầng transport của NGO Dedicated Server")
ngo_connection_end_text = "đảm bảo luồng server-authoritative không bị phá vỡ."
ngo_connection_replacement = """đảm bảo luồng server-authoritative không bị phá vỡ.

![Luồng kiểm duyệt kết nối NGO Dedicated Server](extracted_images/image49.jpeg)

*Hình 3.3. Luồng kiểm duyệt kết nối NGO Dedicated Server*"""

sec_content = sec_content.replace(ngo_connection_end_text, ngo_connection_replacement)

sec_content = sec_content.replace("### e) Ngăn lộ thông tin kỹ thuật nhạy cảm qua `ErrorHandlingMiddleware`", "3.5.5Ngăn lộ thông tin kỹ thuật nhạy cảm qua `ErrorHandlingMiddleware`")
sec_content = sec_content.replace("### f) Kiểm định dữ liệu đầu vào hai tầng (Input Validation)", "3.5.6Kiểm định dữ liệu đầu vào hai tầng (Input Validation)")

sec_content = sec_content.replace("## 3.3.6 Hiện thực hóa triển khai và vận hành với Docker Compose", "3.6Hiện thực hóa triển khai và vận hành với Docker Compose")

# Inject Docker Compose architecture diagram
docker_intro_end = "cập nhật từng thành phần độc lập mà không ảnh hưởng đến các thành phần còn lại đang chạy."
docker_intro_replacement = """cập nhật từng thành phần độc lập mà không ảnh hưởng đến các thành phần còn lại đang chạy.

![Kiến trúc triển khai Docker Compose của hệ thống](extracted_images/image50.jpeg)

*Hình 3.4. Kiến trúc triển khai Docker Compose của hệ thống*"""

sec_content = sec_content.replace(docker_intro_end, docker_intro_replacement)

sec_content = sec_content.replace("### a) Kiến trúc ba container và phân tách mạng nội bộ", "3.6.1Kiến trúc ba container và phân tách mạng nội bộ")
sec_content = sec_content.replace("### b) Quản lý thông tin bí mật qua biến môi trường", "3.6.2Quản lý thông tin bí mật qua biến môi trường")
sec_content = sec_content.replace("### c) Quy trình cập nhật hệ thống không gián đoạn", "3.6.3Quy trình cập nhật hệ thống không gián đoạn")

sec_content = sec_content.replace("## Bảng tổng hợp kiểm thử các biện pháp bảo mật", "3.7Bảng tổng hợp kiểm thử các biện pháp bảo mật")

# 2. Rename existing 3.5 and 3.6 inside c3_content
c3_content = c3_content.replace("3.5Xây dựng giao diện chức năng hệ thống", "3.8Xây dựng giao diện chức năng hệ thống")
c3_content = c3_content.replace("3.5.1Giao diện xác thực tài khoản", "3.8.1Giao diện xác thực tài khoản")
c3_content = c3_content.replace("3.5.2Giao diện khởi tạo và lựa chọn nhân vật", "3.8.2Giao diện khởi tạo và lựa chọn nhân vật")
c3_content = c3_content.replace("3.5.3Giao diện trong trận đấu (HUD)", "3.8.3Giao diện trong trận đấu (HUD)")
c3_content = c3_content.replace("3.5.4Giao diện hệ thống Gene", "3.8.4Giao diện hệ thống Gene")
c3_content = c3_content.replace("3.5.5Giao diện thông tin nhân vật", "3.8.5Giao diện thông tin nhân vật")
c3_content = c3_content.replace("3.5.6Giao diện xã hội", "3.8.6Giao diện xã hội")
c3_content = c3_content.replace("3.5.7Giao diện phó bản", "3.8.7Giao diện phó bản")
c3_content = c3_content.replace("3.5.8Giao diện nhiệm vụ và tương tác NPC thế giới", "3.8.8Giao diện nhiệm vụ và tương tác NPC thế giới")
c3_content = c3_content.replace("3.5.9Giao diện hệ thống bản đồ thế giới", "3.8.9Giao diện hệ thống bản đồ thế giới")
c3_content = c3_content.replace("3.6Tổng kết chương 3", "3.9Tổng kết chương 3")

# 3. Embed all screenshot image links into 3.8 Xây dựng giao diện
screenshots_mapping = {
    "Hình 3.2 Giao diện đăng nhập": """![Giao diện đăng nhập](extracted_images/image51.png)

*Hình 3.5. Giao diện đăng nhập*""",
    "Hình 3.3 Giao diện đăng ký": """![Giao diện đăng ký](extracted_images/image52.png)

*Hình 3.6. Giao diện đăng ký*""",
    "Hình 3.4 Giao diện chọn hệ nguyên tố": """![Giao diện chọn hệ nguyên tố](extracted_images/image53.png)

*Hình 3.7. Giao diện chọn hệ nguyên tố*""",
    "Hình 3.5 Giao diện chọn nhân vật (SelectGene)": """![Giao diện chọn nhân vật](extracted_images/image54.png)

*Hình 3.8. Giao diện chọn nhân vật (SelectGene)*""",
    "Hình 3.6 Giao diện tạo nhân vật Gene 2 mới": """![Giao diện tạo nhân vật Gene 2 mới](extracted_images/image55.png)

*Hình 3.9. Giao diện tạo nhân vật Gene 2 mới*""",
    "Hình 3.8 Giao diện thanh trạng thái nhân vật (HealthBar / MpBar / PlayerInfoUI)": """![Giao diện thanh trạng thái nhân vật](extracted_images/image56.png)

*Hình 3.10. Giao diện thanh trạng thái nhân vật (HealthBar / MpBar / PlayerInfoUI)*""",
    "Hình 3.9 Giao diện thanh kỹ năng và Buff (SkillHotbarUI / BuffHudPanel)": """![Giao diện thanh kỹ năng](extracted_images/image57.png)

*Hình 3.11. Giao diện thanh kỹ năng và Buff (SkillHotbarUI / BuffHudPanel)*""",
    "Hình 3.10 Giao diện thông tin quái (EnemyInfoPanel)": """![Giao diện thông tin quái](extracted_images/image58.png)

*Hình 3.12. Giao diện thông tin quái (EnemyInfoPanel)*""",
    "Hình 3.11 Giao diện thông báo hệ thống (GlobalNotificationUI)": """![Giao diện thông báo hệ thống](extracted_images/image59.png)

*Hình 3.13. Giao diện thông báo hệ thống (GlobalNotificationUI)*""",
    "Hình 3.12 Giao diện nâng cấp Gene chính (GeneUpgradePanel)": """![Giao diện nâng cấp Gene chính](extracted_images/image60.png)

*Hình 3.14. Giao diện nâng cấp Gene chính (GeneUpgradePanel)*""",
    "Hình 3.13 Giao diện xác nhận Gene phụ cố định (SecondaryGeneSelectPanel)": """![Giao diện xác nhận Gene phụ cố định](extracted_images/image61.png)

*Hình 3.15. Giao diện xác nhận Gene phụ cố định (SecondaryGeneSelectPanel)*""",
    "Hình 3.14 Giao diện nâng cấp Gene phụ (SecondaryGeneUpgradePanel)": """![Giao diện nâng cấp Gene phụ](extracted_images/image62.png)

*Hình 3.16. Giao diện nâng cấp Gene phụ (SecondaryGeneUpgradePanel)*""",
    "Hình 3.15 Giao diện dung hợp Hybrid (HybridFusionPanel)": """![Giao diện dung hợp Hybrid](extracted_images/image63.png)

*Hình 3.17. Giao diện dung hợp Hybrid (HybridFusionPanel)*""",
    "Hình 3.16 Giao diện bảng tóm tắt nhân vật (CharacterMenuPanelUI)": """![Giao diện bảng tóm tắt nhân vật](extracted_images/image64.png)

*Hình 3.18. Giao diện bảng tóm tắt nhân vật (CharacterMenuPanelUI)*""",
    "Hình 3.17 Giao diện tab Chỉ số và Trang bị (StatsTabUI)": """![Giao diện tab Chỉ số và Trang bị](extracted_images/image65.png)

*Hình 3.19. Giao diện tab Chỉ số và Trang bị (StatsTabUI)*""",
    "Hình 3.18 Giao diện tab Kỹ năng (SkillTabUI / SkillDetailPanelUI)": """![Giao diện tab Kỹ năng](extracted_images/image66.png)

*Hình 3.20. Giao diện tab Kỹ năng (SkillTabUI / SkillDetailPanelUI)*""",
    "Hình 3.19 Giao diện tab Tiềm Năng (PotentialTabUI)": """![Giao diện tab Tiềm Năng](extracted_images/image67.png)

*Hình 3.21. Giao diện tab Tiềm Năng (PotentialTabUI)*""",
    "Hình 3.20 Giao diện chat đa kênh (ChatPanelUI)": """![Giao diện chat đa kênh](extracted_images/image68.png)

*Hình 3.22. Giao diện chat đa kênh (ChatPanelUI)*""",
    "Hình 3.21 Giao diện danh sách bạn bè (FriendListUI)": """![Giao diện danh sách bạn bè](extracted_images/image69.png)

*Hình 3.23. Giao diện danh sách bạn bè (FriendListUI)*""",
    "Hình 3.22 Giao diện tổ đội (PartyPanelUI)": """![Giao diện tổ đội](extracted_images/image70.png)

*Hình 3.24. Giao diện tổ đội (PartyPanelUI)*""",
    "Hình 3.23 Giao diện bảng xếp hạng (LeaderboardPanelUI)": """![Giao diện bảng xếp hạng](extracted_images/image71.png)

*Hình 3.25. Giao diện bảng xếp hạng (LeaderboardPanelUI)*""",
    "Hình 3.24 Giao diện chọn phó bản (DungeonListUI)": """![Giao diện chọn phó bản](extracted_images/image72.png)

*Hình 3.26. Giao diện chọn phó bản (DungeonListUI)*""",
    "Hình 3.25 Giao diện HUD phó bản wave (WaveHUD)": """![Giao diện HUD phó bản wave](extracted_images/image73.png)

*Hình 3.27. Giao diện HUD phó bản wave (WaveHUD)*""",
    "Hình 3.27 Giao diện widget nhiệm vụ góc màn hình (QuestHudWidget)": """![Giao diện widget nhiệm vụ góc màn hình](extracted_images/image74.png)

*Hình 3.28. Giao diện widget nhiệm vụ góc màn hình (QuestHudWidget)*""",
    "Hình 3.29 Giao diện menu NPC động và cửa hàng (NpcDynamicMenuUI / NpcMenuUI)": """![Giao diện menu NPC động và cửa hàng](extracted_images/image75.png)

*Hình 3.29. Giao diện menu NPC động và cửa hàng (NpcDynamicMenuUI / NpcMenuUI)*""",
    "Hình 3.30 Giao diện chuyển map qua biên (MapEdgeTrigger / MapTransitionButton)": """![Giao diện chuyển map qua biên](extracted_images/image76.png)

*Hình 3.30. Giao diện chuyển map qua biên (MapEdgeTrigger / MapTransitionButton)*"""
}

for caption, replacement in screenshots_mapping.items():
    if caption in c3_content:
        c3_content = c3_content.replace(caption, replacement)
        print(f"Successfully embedded image for caption: '{caption}'")
    else:
        print(f"Warning: Caption '{caption}' not found!")

# 4. Insert the sec_content (3.5, 3.6, 3.7) right before "3.8Xây dựng giao diện chức năng hệ thống"
split_phrase = "3.8Xây dựng giao diện chức năng hệ thống"
if split_phrase in c3_content:
    parts = c3_content.split(split_phrase)
    new_c3_content = parts[0] + sec_content + "\n\n---\n\n" + split_phrase + parts[1]
    
    with open(filepath_c3, 'w', encoding='utf-8') as f:
        f.write(new_c3_content)
    print("\nSuccessfully merged and renumbered CHUONG3_BAO_CAO_VIET_LAI.md!")
else:
    print("Error: Split phrase not found in CHUONG3_BAO_CAO_VIET_LAI.md!")
