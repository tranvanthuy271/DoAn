import os
import re

def update_chapter2():
    c2_path = "docs/CHUONG2_BAO_CAO.md"
    if not os.path.exists(c2_path):
        print(f"Error: {c2_path} does not exist.")
        return
        
    with open(c2_path, "r", encoding="utf-8") as f:
        content = f.read()

    # 1. Heading replacements
    content = content.replace("### 2.2.2. Biểu đồ Use Case tổng quát", "### 2.2.2. Biểu đồ ca sử dụng mức tổng quát")
    content = content.replace("Hình 2.2 Biểu đồ Usecase tổng quát hệ thống Mutants Arena", "Hình 2.2. Biểu đồ ca sử dụng mức tổng quát hệ thống Mutants Arena")
    content = content.replace("### 2.2.3. Đặc tả Use Case chi tiết", "### 2.2.3. Đặc tả ca sử dụng mức chi tiết")
    
    # 2. Detailed use cases map (captions and alts)
    modules = [
        ("Đăng ký tài khoản", "10", "2.3"),
        ("Đăng nhập và vào game", "11", "2.4"),
        ("Di chuyển và chuyển map", "12", "2.5"),
        ("Chiến đấu và sử dụng kỹ năng", "13", "2.6"),
        ("Quản lý túi đồ và trang bị", "14", "2.7"),
        ("Nâng cấp trang bị", "15", "2.8"),
        ("Phát triển Gene và Hybrid", "16", "2.9"),
        ("Phân bổ tiềm năng và kỹ năng", "17", "2.10"),
        ("Tương tác NPC và mua vật phẩm", "18", "2.11"),
        ("Quản lý nhiệm vụ", "19", "2.12"),
        ("Quản lý bạn bè", "20", "2.13"),
        ("Quản lý tổ đội và chat", "21", "2.14"),
        ("Tham gia và hoàn tất phó bản", "22", "2.15"),
        ("Xem leaderboard", "23", "2.16"),
        ("Quản lý gameplay server", "24", "2.17"),
        ("Host map và phát thưởng phó bản", "25", "2.18"),
    ]
    
    for name, img_num, fig_num in modules:
        old_alt = f"![Biểu đồ Use case chức năng {name}]"
        new_alt = f"![Biểu đồ ca sử dụng cho mô-đun {name}]"
        content = content.replace(old_alt, new_alt)
        
        old_caption = f"*Hình {fig_num}. Biểu đồ Use case chức năng {name}*"
        new_caption = f"*Hình {fig_num}. Biểu đồ ca sử dụng cho mô-đun {name}*"
        content = content.replace(old_caption, new_caption)

    # 3. Sequence diagrams map (headings, captions, alts)
    sequences = [
        ("Chiến đấu và sử dụng kỹ năng", "26", "2.19"),
        ("Nâng cấp trang bị tại Blacksmith", "27", "2.20"),
        ("Nâng Gene chính và Gene phụ", "28", "2.21"),
        ("Dung hợp Hybrid Gene", "29", "2.22"),
        ("Kích hoạt Gene Tối Thượng", "_ultimate_gene_sequence", "2.22a"),
        ("Tham gia và hoàn tất phó bản", "30", "2.23"),
    ]
    
    content = content.replace("### 2.2.4. Các biểu đồ tuần tự", "### 2.2.4. Các biểu đồ tuần tự đặc tả ca sử dụng")
    
    for name, img_suffix, fig_num in sequences:
        old_heading = f"#### {fig_num}. Biểu đồ tuần tự {name}"
        new_heading = f"#### {fig_num}. Biểu đồ tuần tự đặc tả ca sử dụng {name}"
        content = content.replace(old_heading, new_heading)
        
        old_alt = f"![Biểu đồ tuần tự {name}]"
        new_alt = f"![Biểu đồ tuần tự đặc tả ca sử dụng {name}]"
        content = content.replace(old_alt, new_alt)
        
        old_caption = f"*Hình {fig_num}. Biểu đồ tuần tự {name}*"
        new_caption = f"*Hình {fig_num}. Biểu đồ tuần tự đặc tả ca sử dụng {name}*"
        content = content.replace(old_caption, new_caption)

    # Write changes back
    with open(c2_path, "w", encoding="utf-8") as f:
        f.write(content)
    print("Chapter 2 Use cases and Sequence headings/captions updated successfully!")

def update_front_matter():
    fm_path = "docs/FRONT_MATTER.md"
    if not os.path.exists(fm_path):
        print(f"Error: {fm_path} does not exist.")
        return
        
    with open(fm_path, "r", encoding="utf-8") as f:
        fm_content = f.read()
        
    # Add UC to abbreviations list if not exists
    if "| UC |" not in fm_content:
        # We can find the place after RPG
        old_abbrev = "| RPG | Role-Playing Game | Game nhập vai |"
        new_abbrev = "| RPG | Role-Playing Game | Game nhập vai |\n| UC | Use Case (hoặc Ca sử dụng) | Ca sử dụng |"
        fm_content = fm_content.replace(old_abbrev, new_abbrev)
        print("Added UC to abbreviations list in FRONT_MATTER.md")
        
    # Dynamically extract all figures and tables from all chapter files
    chapters = [
        "docs/CHUONG1_BAO_CAO.md",
        "docs/CHUONG2_BAO_CAO.md",
        "docs/CHUONG3_BAO_CAO_VIET_LAI.md",
        "docs/CHUONG4_BAO_CAO.md",
        "docs/KET_LUAN_PHU_LUC.md"
    ]
    
    figures = []
    tables = []
    
    # Regex to find figures
    # Matches patterns like:
    # *Hình 1.1: Caption* or Hình 1.1: Caption or *Hình 2.3. Caption* or Hình 2.3: Caption
    fig_pattern = re.compile(r'^\*?Hình\s+(\d+\.\d+[a-z]?)\s*[\.:\-]?\s*(.*?)\*?$', re.MULTILINE)
    # Matches patterns like:
    # **Bảng 1.1: Caption** or Bảng 1.1: Caption or **Bảng 2.1. Caption**
    tab_pattern = re.compile(r'^\*?\*?Bảng\s+(\d+\.\d+[a-z]?)\s*[\.:\-]?\s*(.*?)\*?\*?$', re.MULTILINE)
    
    for ch in chapters:
        if not os.path.exists(ch):
            continue
            
        ch_name = "Chương 1"
        if "CHUONG2" in ch:
            ch_name = "Chương 2"
        elif "CHUONG3" in ch:
            ch_name = "Chương 3"
        elif "CHUONG4" in ch:
            ch_name = "Chương 4"
        elif "KET_LUAN" in ch:
            ch_name = "Phụ lục"
            
        with open(ch, "r", encoding="utf-8") as f:
            ch_content = f.read()
            
        # Find figures
        for match in fig_pattern.finditer(ch_content):
            num, title = match.groups()
            title = title.strip().rstrip("*").strip()
            figures.append((num, title, ch_name))
            
        # Find tables
        for match in tab_pattern.finditer(ch_content):
            num, title = match.groups()
            title = title.strip().rstrip("*").strip()
            tables.append((num, title, ch_name))
            
    # Sort figures and tables by chapter and number
    def parse_num(num_str):
        # splits 2.22a into (2, 22, 'a')
        parts = re.split(r'(\d+)\.(\d+)(.*)', num_str)
        if len(parts) >= 4:
            return (int(parts[1]), int(parts[2]), parts[3])
        return (99, 99, '')
        
    figures.sort(key=lambda x: parse_num(x[0]))
    tables.sort(key=lambda x: parse_num(x[0]))
    
    # Generate Markdown lists
    fig_list_str = "| Số hiệu | Tên hình | Vị trí |\n|---|---|---|\n"
    for num, title, ch in figures:
        fig_list_str += f"| Hình {num} | {title} | {ch} |\n"
        
    tab_list_str = "| Số hiệu | Tên bảng | Vị trí |\n|---|---|---|\n"
    for num, title, ch in tables:
        tab_list_str += f"| Bảng {num} | {title} | {ch} |\n"
        
    # Replace sections in FRONT_MATTER.md
    # Replace from ## DANH MỤC HÌNH ẢNH to ## DANH MỤC BẢNG
    # and ## DANH MỤC BẢNG to ---
    fig_header = "## DANH MỤC HÌNH ẢNH\n\n"
    tab_header = "## DANH MỤC BẢNG\n\n"
    
    # Locate markers
    try:
        parts_fig = fm_content.split("## DANH MỤC HÌNH ẢNH")
        pre_fig = parts_fig[0]
        post_fig_parts = parts_fig[1].split("## DANH MỤC BẢNG")
        post_tab_parts = post_fig_parts[1].split("## DANH MỤC TỪ VIẾT TẮT")
        pre_abbrev = post_tab_parts[1]
        
        # Re-build FRONT_MATTER.md
        new_fm_content = (
            pre_fig + 
            fig_header + fig_list_str + "\n" +
            tab_header + tab_list_str + "\n" +
            "## DANH MỤC TỪ VIẾT TẮT" + pre_abbrev
        )
        
        with open(fm_path, "w", encoding="utf-8") as f:
            f.write(new_fm_content)
        print("FRONT_MATTER.md lists of figures and tables rebuilt and updated successfully!")
    except Exception as e:
        print(f"Error rebuilding front matter: {e}")

if __name__ == "__main__":
    update_chapter2()
    update_front_matter()
