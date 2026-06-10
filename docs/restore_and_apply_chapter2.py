import os
import sys

def restore_and_apply():
    sys.stdout.reconfigure(encoding='utf-8')
    
    thesis_path = r"c:\Hub\DoAn\docs\DO_AN_TOT_NGHIEP_FINAL.md"
    report_path = r"c:\Hub\DoAn\docs\CHUONG2_BAO_CAO.md"
    
    if not os.path.exists(thesis_path):
        print(f"Error: {thesis_path} not found.")
        return
    if not os.path.exists(report_path):
        print(f"Error: {report_path} not found.")
        return
        
    # 1. Read thesis content and extract lines 1030 to 1221 (1-indexed)
    with open(thesis_path, 'r', encoding='utf-8') as f:
        thesis_lines = f.readlines()
    
    # Lines 1030 to 1221 in 0-indexed form are lines 1029 to 1221
    extracted_section_lines = thesis_lines[1029:1221]
    extracted_section = "".join(extracted_section_lines).replace("\r\n", "\n")
    
    # 2. Read current report content
    with open(report_path, 'r', encoding='utf-8') as f:
        report_content = f.read().replace("\r\n", "\n")
        
    # 3. Locate the Use Case tables section in clean CHUONG2_BAO_CAO.md
    # It starts at "### 2.2.3. Đặc tả Use Case chi tiết"
    # and ends right before "### 2.2.4. Thiết kế cơ sở dữ liệu"
    start_str = "### 2.2.3. Đặc tả Use Case chi tiết"
    end_str = "### 2.2.4. Thiết kế cơ sở dữ liệu"
    
    start_idx = report_content.find(start_str)
    end_idx = report_content.find(end_str)
    
    if start_idx == -1 or end_idx == -1:
        print(f"Error: Could not locate Use Case section in report. start_idx={start_idx}, end_idx={end_idx}")
        return
        
    print("Found Use Case table section in clean report. Replacing with bullet points...")
    
    # Keep the heading "### 2.2.3. Đặc tả Use Case chi tiết\n\n"
    new_report_content = report_content[:start_idx] + extracted_section + "\n\n" + report_content[end_idx:]
    
    # 4. Update Figure 2.2 General Use Case to use image9.png instead of image10.png
    new_report_content = new_report_content.replace(
        "extracted_images/image10.png)\n\n*Hình 2.2. Biểu đồ Use Case tổng quát hệ thống Mutants Arena*",
        "extracted_images/image9.png)\n\n*Hình 2.2. Biểu đồ Use Case tổng quát hệ thống Mutants Arena*"
    )
    new_report_content = new_report_content.replace(
        "extracted_images/image10.png)\n\n*Hình 2.2. Biểu đồ Use Case tổng quát hệ thống Mutants Arena*",
        "extracted_images/image9.png)\n\n*Hình 2.2. Biểu đồ Use Case tổng quát hệ thống Mutants Arena*"
    )
    
    # 5. Insert detailed Use Case diagrams under bullet points
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
    
    print("\nInserting detailed Use Case diagrams and captions...")
    for target, replacement in replacements.items():
        if target in new_report_content:
            new_report_content = new_report_content.replace(target, replacement)
            print(f"Successfully replaced target: '{target[:40]}...'")
        else:
            print(f"Warning: Target not found: '{target}'")
            
    # 6. Apply sequence diagrams and ERD section and update captions
    # Note: In the reverted report_content, 2.2.4 was "Thiết kế cơ sở dữ liệu".
    # We want to change "### 2.2.4. Thiết kế cơ sở dữ liệu" to the sequence diagrams section, and then add "### 2.2.5. Thiết kế cơ sở dữ liệu" after.
    # Let's do this replacement:
    seq_and_db_replacement = """### 2.2.4. Các biểu đồ tuần tự
57: 
58: Căn cứ vào các kịch bản Use case trọng tâm đã được đặc tả trong hệ thống Mutants Arena, phần này trình bày các biểu đồ tuần tự cho những luồng gameplay cốt lõi đang được triển khai thực tế trong dự án. Các biểu đồ được xây dựng bám sát kiến trúc client Unity, gameplay server, ASP.NET Core Web API và cơ sở dữ liệu MySQL, qua đó phản ánh rõ thứ tự tương tác giữa người chơi, giao diện, lớp xử lý nghiệp vụ và dữ liệu lưu trữ trong từng chức năng chính.
59: 
60: #### 2.2.4.1. Biểu đồ tuần tự Chiến đấu và sử dụng kỹ năng
61: 
62: Căn cứ vào kịch bản Use case UC04 - Chiến đấu và sử dụng kỹ năng, ta xây dựng các bước thực hiện của hệ thống với chức năng chiến đấu thời gian thực bằng biểu đồ tuần tự. Trong luồng này, người chơi chọn mục tiêu hoặc kích hoạt đòn đánh thường, kỹ năng trên thanh Q/W/E/R; client chiến đấu gửi yêu cầu lên gameplay server; gameplay server tiếp tục kiểm tra điều kiện cooldown, MP, hitbox và trạng thái khóa thao tác trước khi chuyển sang lớp CombatResolver hoặc đối tượng địch để tính sát thương.
63: 
64: Sau khi yêu cầu hợp lệ, hệ thống áp dụng DamageResult, cập nhật HP, buff, debuff và đồng bộ kết quả cho client điều khiển lẫn client quan sát bằng cơ chế server-authoritative. Khi mục tiêu bị tiêu diệt, hệ thống tiếp tục kích hoạt luồng phát EXP, vật phẩm rơi và các hook nhiệm vụ liên quan. Trong cùng biểu đồ, các tuần tự thất bại cũng được mô tả rõ cho trường hợp kỹ năng đang hồi chiêu, không đủ MP, mục tiêu ngoài phạm vi, trượt hitbox hoặc nhân vật đã chết.
65: 
66: ![Biểu đồ tuần tự Chiến đấu và sử dụng kỹ năng](extracted_images/image26.png)
67: 
68: *Hình 2.19. Biểu đồ tuần tự Chiến đấu và sử dụng kỹ năng*
69: 
70: #### 2.2.4.2. Biểu đồ tuần tự Nâng cấp trang bị tại Blacksmith
71: 
72: Căn cứ vào kịch bản Use case UC06 - Nâng cấp trang bị, ta xây dựng các bước thực hiện của hệ thống với chức năng cường hóa trang bị tại Blacksmith bằng biểu đồ tuần tự. Luồng này bắt đầu khi người chơi mở giao diện Blacksmith, chọn trang bị mục tiêu và nạp cấu hình nâng cấp hiện tại từ hệ thống. Tại đây, hệ thống truy vấn inventory, equipment và cấu hình nâng cấp theo bậc hiện tại để trả về các tham số như `stoneId`, `stoneNeeded`, `success rate`, `failPolicy` và `upgradeLevel`.
73: 
74: Sau khi người chơi chọn đúng trang bị và các slot đá, charm tương ứng, client gửi yêu cầu nâng cấp kèm thông tin `slotIndex + count` để hệ thống kiểm tra số lượng vật liệu trên từng stack, phòng chống gian lận số lượng khi dùng vật liệu dạng chồng. Nếu hợp lệ, hệ thống trừ bạc, tiêu hao vật liệu, tính kết quả theo `failPolicy`, cập nhật `upgradeLevel`, `strOptions`, inventory and equipment. Biểu đồ đồng thời thể hiện rõ hai nhánh thất bại chính: thất bại xác thực do thiếu bạc, thiếu vật liệu, sai slot stack hoặc chạm mốc +24; và thất bại theo tỷ lệ, trong đó hệ thống giữ nguyên hoặc làm tụt cấp trang bị đúng theo cấu hình rủi ro đang dùng trong dự án.
75: 
76: ![Biểu đồ tuần tự Nâng cấp trang bị tại Blacksmith](extracted_images/image27.png)
77: 
78: *Hình 2.20. Biểu đồ tuần tự Nâng cấp trang bị tại Blacksmith*
79: 
80: #### 2.2.4.3. Biểu đồ tuần tự Nâng Gene chính và Gene phụ
81: 
82: Căn cứ vào kịch bản Use case UC07 - Phát triển Gene và Hybrid, ta xây dựng các bước thực hiện của hệ thống với chức năng nâng Gene chính và Gene phụ bằng biểu đồ tuần tự. Ở giai đoạn Gene chính, người chơi mở giao diện Gene Evolution, nạp `player_data` and cấu hình trong `gene_upgrade_config`, sau đó xác nhận yêu cầu nâng cấp. Hệ thống kiểm tra `gene_exp`, bạc, vật liệu và giới hạn Tier trước khi quyết định kết quả nâng cấp; nếu thành công, hệ thống lưu Tier mới, `final_stats`, danh sách kỹ năng mở khóa và đồng bộ lại giao diện Gene cho người chơi.
83: 
84: Đối với Gene phụ, biểu đồ mô tả rõ quá trình chọn hệ phụ lần đầu, kiểm tra cặp hệ cố định đã triển khai trong dự án gồm Hỏa↔Thổ, Thủy↔Mộc và Kim↔Phong, sau đó nạp cấu hình từ `gene_multi_config` để thực hiện nâng hệ phụ. Nếu Gene phụ được nâng thành công, hệ thống cập nhật `secondaryElement`, `secondary_gene_tier`, bonus chỉ số theo hệ số giảm so với Gene chính, đồng thời bật cờ `canFuse` khi cả hai hệ đã đạt điều kiện hợp lệ. Các luồng phụ thất bại cũng được biểu diễn đầy đủ cho trường hợp thiếu `gene_exp`, thiếu bạc, thiếu vật liệu, Gene chính đã đạt Tier tối đa, chọn sai cặp hệ hoặc Gene phụ đã bị khóa trước đó.
85: 
86: ![Biểu đồ tuần tự Nâng Gene chính và Gene phụ](extracted_images/image28.png)
87: 
88: *Hình 2.21. Biểu đồ tuần tự Nâng Gene chính và Gene phụ*
89: 
90: #### 2.2.4.4. Biểu đồ tuần tự Dung hợp Hybrid Gene
91: 
92: Căn cứ vào kịch bản Use case UC07 - Phát triển Gene và Hybrid, ta tiếp tục xây dựng các bước thực hiện của hệ thống với chức năng dung hợp Hybrid Gene bằng biểu đồ tuần tự. Luồng này bắt đầu khi người chơi mở tab Hybrid Fusion, yêu cầu nạp điều kiện thực hiện và hệ thống truy vấn các bảng `gene_hybrid_config`, `gene_hybrid_skill` cùng dữ liệu hiện thời trong `player_data`. Từ đó, giao diện nhận về các thông tin như số lượng Fusion Core, `gold cost`, bonus, prefab và bộ kỹ năng Hybrid tương ứng để hiển thị cho người chơi xác nhận.
93: 
94: Khi người chơi gửi yêu cầu dung hợp, hệ thống kiểm tra đồng thời các điều kiện quan trọng gồm trạng thái `isHybrid`, Tier của Gene chính và Gene phụ, tính hợp lệ của cặp hệ, số lượng Fusion Core và lượng vàng hiện có. Nếu dung hợp thành công, hệ thống lưu `HybridId`, `prefab`, các `immune elements`, bonus chiến đấu và bộ kỹ năng Hybrid mới vào dữ liệu nhân vật, sau đó đồng bộ lại `final_stats` và giao diện Hybrid cho client. Biểu đồ cũng thể hiện rõ hai nhóm điều kiện thất bại: chưa đủ Tier 5 hoặc cặp hệ không hợp lệ; thiếu Fusion Core, thiếu vàng hoặc nhân vật đã là Hybrid trước đó.
95: 
96: ![Biểu đồ tuần tự Dung hợp Hybrid Gene](extracted_images/image29.png)
97: 
98: *Hình 2.22. Biểu đồ tuần tự Dung hợp Hybrid Gene*
99: 
100: #### 2.2.4.5. Biểu đồ tuần tự Tham gia và hoàn tất phó bản
101: 
102: Căn cứ vào kịch bản Use case UC13 - Tham gia và hoàn tất phó bản, ta xây dựng các bước thực hiện của hệ thống với chức năng tham gia dungeon, chiến đấu qua các wave và nhận thưởng hoàn tất bằng biểu đồ tuần tự. Luồng này bắt đầu khi người chơi chọn loại phó bản hoặc portal dungeon; hệ thống tiếp nhận yêu cầu và kiểm tra level, lượt vào cùng trạng thái tổ đội trước khi truy vấn `player_data`, `dungeon_config` và `dungeon_wave_config`. Nếu điều kiện hợp lệ, gameplay server khởi tạo phiên dungeon, spawn wave đầu, nạp boss và chuyển người chơi vào map phó bản.
103: 
104: Trong giai đoạn xử lý chính, gameplay server điều khiển toàn bộ vòng lặp chiến đấu qua wave, theo dõi tiến độ, spawn wave tiếp theo và boss cuối, sau đó tổng kết kết quả cho từng người chơi hoặc cả tổ đội khi đạt điều kiện chiến thắng. Ở bước kết thúc, hệ thống gọi xử lý phát thưởng, cập nhật inventory, reward, log và dữ liệu `dungeon_best_waves`, rồi đồng bộ EXP và phần thưởng cho client trước khi đưa người chơi trở về map an toàn. Biểu đồ cũng mô tả rõ các trường hợp ngoại lệ của dự án như người chơi hoặc tổ đội thất bại, hết thời gian, bị hạ gục toàn bộ nên không nhận thưởng hoàn tất; hoặc máy chủ gặp lỗi spawn ở một wave và phải xử lý theo cấu hình fallback của dungeon.
105: 
106: ![Biểu đồ tuần tự Tham gia và hoàn tất phó bản](extracted_images/image30.png)
107: 
108: *Hình 2.23. Biểu đồ tuần tự Tham gia và hoàn tất phó bản*
109: 
110: ### 2.2.5. Thiết kế cơ sở dữ liệu"""
    
    new_report_content = new_report_content.replace("### 2.2.4. Thiết kế cơ sở dữ liệu", seq_and_db_replacement)
    
    # 7. Update Database ERD caption and structure table headings
    new_report_content = new_report_content.replace("#### 2.2.4.1. Sơ đồ kết nối các bảng", "#### 2.2.5.1. Sơ đồ kết nối các bảng")
    new_report_content = new_report_content.replace("#### 2.2.4.2. Cấu trúc các bảng chính", "#### 2.2.5.2. Cấu trúc các bảng chính")
    
    new_report_content = new_report_content.replace(
        "Bảng 2.8. Nhóm bảng tài khoản, hồ sơ nhân vật và xã hội",
        "Bảng 2.18. Nhóm bảng tài khoản, hồ sơ nhân vật và xã hội"
    )
    new_report_content = new_report_content.replace(
        "Bảng 2.9. Nhóm bảng vật phẩm, option và nâng cấp trang bị",
        "Bảng 2.19. Nhóm bảng vật phẩm, option và nâng cấp trang bị"
    )
    new_report_content = new_report_content.replace(
        "Bảng 2.10. Nhóm bảng Gene, Hybrid và kỹ năng",
        "Bảng 2.20. Nhóm bảng Gene, Hybrid và kỹ năng"
    )
    new_report_content = new_report_content.replace(
        "Bảng 2.11. Nhóm bảng thế giới game, quái vật, nhiệm vụ và dungeon",
        "Bảng 2.21. Nhóm bảng thế giới game, quái vật, nhiệm vụ và dungeon"
    )
    new_report_content = new_report_content.replace(
        "Bảng 2.11. Nhóm bảng thế giới game, quái vật, nhiệm vụ và dungeon",
        "Bảng 2.21. Nhóm bảng thế giới game, quái vật, nhiệm vụ và dungeon"
    )
    new_report_content = new_report_content.replace(
        "Bảng 2.12. Sơ đồ cơ sở dữ liệu của hệ thống Mutants Arena",
        "Bảng 2.24. Sơ đồ cơ sở dữ liệu của hệ thống Mutants Arena"
    )
    new_report_content = new_report_content.replace(
        "Hình 2.12. Sơ đồ cơ sở dữ liệu của hệ thống Mutants Arena",
        "Hình 2.24. Sơ đồ cơ sở dữ liệu của hệ thống Mutants Arena"
    )
    new_report_content = new_report_content.replace(
        "(Chèn hình từ file `docs/UseCase/erd_csdl_du_an_thuc_te.drawio` tại vị trí này)",
        "![Sơ đồ cơ sở dữ liệu của hệ thống Mutants Arena](extracted_images/image31.png)"
    )
    
    # Save the finalized report
    with open(report_path, 'w', encoding='utf-8') as f:
        f.write(new_report_content)
        
    print("\nSuccessfully finished restoring and applying Chapter 2 fixes!")

if __name__ == "__main__":
    restore_and_apply()
