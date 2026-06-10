import os
import sys

def fix_chapter2_final():
    sys.stdout.reconfigure(encoding='utf-8')
    filepath = r"c:\Hub\DoAn\docs\CHUONG2_BAO_CAO.md"
    
    if not os.path.exists(filepath):
        print(f"Error: {filepath} not found.")
        return
        
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()
        
    # Normalize line endings to single newlines
    content = content.replace("\r\n", "\n")
    
    # 1. Update Figure 2.2 General Use Case to use image9.png instead of image10.png
    content = content.replace(
        "extracted_images/image10.png)\n\n*Hình 2.2. Biểu đồ Use Case tổng quát hệ thống Mutants Arena*",
        "extracted_images/image9.png)\n\n*Hình 2.2. Biểu đồ Use Case tổng quát hệ thống Mutants Arena*"
    )
    
    # 2. Detailed Use Case Replacements using final lines of each description block
    replacements = {
        "thiếu trường bắt buộc → Hệ thống hiển thị lỗi tương ứng trên UI.": 
        "thiếu trường bắt buộc → Hệ thống hiển thị lỗi tương ứng trên UI.\n\n![Biểu đồ Use case chức năng Đăng ký tài khoản](extracted_images/image10.png)\n\n*Hình 2.3. Biểu đồ Use case chức năng Đăng ký tài khoản*",

        "báo lỗi và cho phép thử lại.":
        "báo lỗi và cho phép thử lại.\n\n![Biểu đồ Use case chức năng Đăng nhập và vào game](extracted_images/image11.png)\n\n*Hình 2.4. Biểu đồ Use case chức năng Đăng nhập và vào game*",

        "hiển thị thông báo khóa map.":
        "hiển thị thông báo khóa map.\n\n![Biểu đồ Use case chức năng Di chuyển và chuyển map](extracted_images/image12.png)\n\n*Hình 2.5. Biểu đồ Use case chức năng Di chuyển và chuyển map*",

        "không đủ mana → Hệ thống từ chối thi triển.":
        "không đủ mana → Hệ thống từ chối thi triển.\n\n![Biểu đồ Use case chức năng Chiến đấu và sử dụng kỹ năng](extracted_images/image13.png)\n\n*Hình 2.6. Biểu đồ Use case chức năng Chiến đấu và sử dụng kỹ năng*",

        "từ phó bản hoặc NPC.":
        "từ phó bản hoặc NPC.\n\n![Biểu đồ Use case chức năng Quản lý túi đồ và trang bị](extracted_images/image14.png)\n\n*Hình 2.7. Biểu đồ Use case chức năng Quản lý túi đồ và trang bị*",

        "theo cấu hình `failPolicy`.":
        "theo cấu hình `failPolicy`.\n\n![Biểu đồ Use case chức năng Nâng cấp trang bị](extracted_images/image15.png)\n\n*Hình 2.8. Biểu đồ Use case chức năng Nâng cấp trang bị*",

        "nguyên tố phụ → Hệ thống báo lỗi.":
        "nguyên tố phụ → Hệ thống báo lỗi.\n\n![Biểu đồ Use case chức năng Phát triển Gene và Hybrid](extracted_images/image16.png)\n\n*Hình 2.9. Biểu đồ Use case chức năng Phát triển Gene và Hybrid*",

        "cộng điểm bị vô hiệu hóa.":
        "cộng điểm bị vô hiệu hóa.\n\n![Biểu đồ Use case chức năng Phân bổ tiềm năng và kỹ năng](extracted_images/image17.png)\n\n*Hình 2.10. Biểu đồ Use case chức năng Phân bổ tiềm năng và kỹ năng*",

        "từ chối giao dịch.":
        "từ chối giao dịch.\n\n![Biểu đồ Use case chức năng Tương tác NPC và mua vật phẩm](extracted_images/image18.png)\n\n*Hình 2.11. Biểu đồ Use case chức năng Tương tác NPC và mua vật phẩm*",

        "hiển thị trạng thái bị khóa.":
        "hiển thị trạng thái bị khóa.\n\n![Biểu đồ Use case chức năng Quản lý nhiệm vụ](extracted_images/image19.png)\n\n*Hình 2.12. Biểu đồ Use case chức năng Quản lý nhiệm vụ*",

        "quan hệ bạn bè trong DB.":
        "quan hệ bạn bè trong DB.\n\n![Biểu đồ Use case chức năng Quản lý bạn bè](extracted_images/image20.png)\n\n*Hình 2.13. Biểu đồ Use case chức năng Quản lý bạn bè*",

        "phân phối tin nhắn đến đúng phạm vi phòng.":
        "phân phối tin nhắn đến đúng phạm vi phòng.\n\n![Biểu đồ Use case chức năng Quản lý tổ đội và chat](extracted_images/image21.png)\n\n*Hình 2.14. Biểu đồ Use case chức năng Quản lý tổ đội và chat*",

        "không được phát thưởng.":
        "không được phát thưởng.\n\n![Biểu đồ Use case chức năng Tham gia và hoàn tất phó bản](extracted_images/image22.png)\n\n*Hình 2.15. Biểu đồ Use case chức năng Tham gia và hoàn tất phó bản*",

        "cache 5 phút để bảo vệ hiệu năng database.":
        "cache 5 phút để bảo vệ hiệu năng database.\n\n![Biểu đồ Use case chức năng Xem leaderboard](extracted_images/image23.png)\n\n*Hình 2.16. Biểu đồ Use case chức năng Xem leaderboard*",

        "Quá 90 giây không có heartbeat, API Server tự giải phóng registry.":
        "Quá 90 giây không có heartbeat, API Server tự giải phóng registry.\n\n![Biểu đồ Use case chức năng Quản lý gameplay server](extracted_images/image24.png)\n\n*Hình 2.17. Biểu đồ Use case chức năng Quản lý gameplay server*",

        "trả kết quả thành công cho server spawn vật phẩm.":
        "trả kết quả thành công cho server spawn vật phẩm.\n\n![Biểu đồ Use case chức năng Host map và phát thưởng phó bản](extracted_images/image25.png)\n\n*Hình 2.18. Biểu đồ Use case chức năng Host map và phát thưởng phó bản*"
    }
    
    print("Editing detailed Use Case diagrams and captions...")
    for target, replacement in replacements.items():
        if target in content:
            content = content.replace(target, replacement)
            print(f"Successfully replaced target: '{target[:40]}...'")
        else:
            print(f"Warning: Target not found: '{target}'")
            
    # 3. Update Sequence Diagrams figure captions (2.3 - 2.7 -> 2.19 - 2.23)
    seq_replacements = {
        "*Hình 2.3. Biểu đồ tuần tự Chiến đấu và sử dụng kỹ năng*": "*Hình 2.19. Biểu đồ tuần tự Chiến đấu và sử dụng kỹ năng*",
        "*Hình 2.4. Biểu đồ tuần tự Nâng cấp trang bị tại Blacksmith*": "*Hình 2.20. Biểu đồ tuần tự Nâng cấp trang bị tại Blacksmith*",
        "*Hình 2.5. Biểu đồ tuần tự Nâng Gene chính và Gene phụ*": "*Hình 2.21. Biểu đồ tuần tự Nâng Gene chính và Gene phụ*",
        "*Hình 2.6. Biểu đồ tuần tự Dung hợp Hybrid Gene*": "*Hình 2.22. Biểu đồ tuần tự Dung hợp Hybrid Gene*",
        "*Hình 2.7. Biểu đồ tuần tự Tham gia và hoàn tất phó bản*": "*Hình 2.23. Biểu đồ tuần tự Tham gia và hoàn tất phó bản*",
    }
    
    print("\nUpdating Sequence Diagram figure captions...")
    for old, new in seq_replacements.items():
        if old in content:
            content = content.replace(old, new)
            print(f"Updated caption to: {new}")
        else:
            print(f"Warning: Caption not found: {old}")
            
    # 4. Update Database ERD caption (2.8 -> 2.24) and structural table numbers (2.8 - 2.11 -> 2.18 - 2.21)
    db_replacements = {
        "*Hình 2.8. Sơ đồ cơ sở dữ liệu của hệ thống Mutants Arena*": "*Hình 2.24. Sơ đồ cơ sở dữ liệu của hệ thống Mutants Arena*",
        "Bảng 2.8. Nhóm bảng tài khoản, hồ sơ nhân vật và xã hội": "Bảng 2.18. Nhóm bảng tài khoản, hồ sơ nhân vật và xã hội",
        "Bảng 2.9. Nhóm bảng vật phẩm, option và nâng cấp trang bị": "Bảng 2.19. Nhóm bảng vật phẩm, option và nâng cấp trang bị",
        "Bảng 2.10. Nhóm bảng Gene, Hybrid và kỹ năng": "Bảng 2.20. Nhóm bảng Gene, Hybrid và kỹ năng",
        "Bảng 2.11. Nhóm bảng thế giới game, quái vật, nhiệm vụ và dungeon": "Bảng 2.21. Nhóm bảng thế giới game, quái vật, nhiệm vụ và dungeon",
    }
    
    print("\nUpdating Database captions and table numbers...")
    for old, new in db_replacements.items():
        if old in content:
            content = content.replace(old, new)
            print(f"Updated: {old} -> {new}")
        else:
            print(f"Warning: Not found: {old}")
            
    # Save the updated file back
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)
        
    print("\nSuccessfully finished fixing CHUONG2_BAO_CAO.md!")

if __name__ == "__main__":
    fix_chapter2_final()
