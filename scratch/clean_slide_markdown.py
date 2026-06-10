import re

file_path = r'c:\Hub\DoAn\docs\ĐATN-slide.md'

with open(file_path, 'r', encoding='utf-8') as f:
    lines = f.readlines()

# We will rewrite the slides by processing the text lines.
# To make it robust, let's first map slide headers.
# Each slide starts with a line like '---' or '# Slide ...'.
# Let's group lines into slides by splitting on '---' lines.

content = "".join(lines)
slides_raw = content.split('\n---\n')

slides_processed = []

for idx, slide_content in enumerate(slides_raw):
    # Check slide number by header
    # Slide title is the first non-empty line starting with # Slide
    lines_slide = slide_content.split('\n')
    slide_title = ""
    for line in lines_slide:
        if line.strip().startswith('# Slide'):
            slide_title = line.strip()
            break
    
    # Process Slide 10 (Stack Công nghệ)
    if 'Slide 10:' in slide_title:
        # Replace the entire slide content to show ONLY the image and no bullet points/texts
        new_slide_lines = [
            "# Slide 10: Cơ sở lý thuyết kiến trúc mạng Game & Công nghệ",
            "## Kiến trúc mạng & Công nghệ sử dụng",
            "",
            '<div style="text-align: center; width: 100%;">',
            "",
            "![network_and_tech_stack.png](../extracted_images/network_and_tech_stack.png)",
            "",
            "</div>"
        ]
        slides_processed.append("\n".join(new_slide_lines))
        continue
    
    # Process Slide 13, 14, 15, 16 (Use Cases)
    is_usecase_slide = False
    usecase_images = {
        'Slide 13:': 'image_unnamed_8.png',
        'Slide 14:': 'image_unnamed_13.png',
        'Slide 15:': 'image_unnamed_16.png',
        'Slide 16:': 'image_unnamed_22.png'
    }
    
    matching_key = None
    for key in usecase_images:
        if key in slide_title:
            is_usecase_slide = True
            matching_key = key
            break
            
    if is_usecase_slide:
        # Extract title and subtitle
        title_line = ""
        subtitle_line = ""
        for line in lines_slide:
            if line.strip().startswith('# Slide'):
                title_line = line.strip()
            elif line.strip().startswith('## '):
                subtitle_line = line.strip()
        
        image_name = usecase_images[matching_key]
        new_slide_lines = [
            title_line,
            subtitle_line,
            "",
            '<div style="text-align: center; width: 100%;">',
            "",
            f"![{image_name}](../extracted_images/{image_name})",
            "",
            "</div>"
        ]
        slides_processed.append("\n".join(new_slide_lines))
        continue
        
    # For all other slides, perform exact parentheses cleaning on text bullet points
    cleaned_lines = []
    for line in lines_slide:
        # Check if this line is an image link (keep image link parentheses!)
        if '![' in line and '](' in line:
            cleaned_lines.append(line)
            continue
            
        # Specific replacements to remove parentheses while maintaining meaning
        replacements = {
            "sát thương vật lý (OverlapCircleAll)": "sát thương vật lý thông qua OverlapCircleAll",
            "tham chiếu (Hollow Knight, Dead Cells, Celeste, MapleStory, Làng Lá)": "tham chiếu bao gồm Hollow Knight, Dead Cells, Celeste, MapleStory và Làng Lá",
            "quái vật (FSM)": "quái vật FSM",
            "vật lý (ERD)": "vật lý ERD",
            "platformer (Coyote time, Jump buffer)": "platformer như Coyote time và Jump buffer",
            "bảo mật (JWT, Connection Approval, API Key)": "bảo mật gồm JWT, Connection Approval và API Key",
            "quái vật (FSM)": "quái vật FSM",
            "2D (Box2D)": "2D Box2D",
            "bay lên (Variable Gravity Jump)": "bay lên qua cơ chế Variable Gravity Jump",
            "trí tuệ nhân tạo quái vật (FSM)": "trí tuệ nhân tạo quái vật FSM",
            "nhân vật (class)": "nhân vật class",
            "máy chủ (Server)": "máy chủ Server",
            "Người chơi (Player)": "Người chơi Player",
            "chủ động (Q, W, E, R)": "chủ động Q, W, E, R",
            "đợt (Wave)": "đợt Wave",
            "bản đồ (Instance)": "bản đồ Instance",
            "đăng nhập (mã hóa mật khẩu)": "đăng nhập được mã hóa mật khẩu",
            "ngắn (0.2 giây)": "ngắn 0.2 giây",
            "thực tế (mạng/FPS)": "thực tế gồm độ trễ mạng và FPS",
            "sát thương (damage pop-up)": "sát thương damage pop-up",
            "còn lại (nhân **x1.0**),": "còn lại với hệ số sát thương nhân x1.0,",
            "cuối: `Damage = (ATK * skill_mult * element_mult) - DEF * red_factor`": "cuối: `Damage = ATK * skill_mult * element_mult - DEF * red_factor`",
            "nhân vật (100% hiệu năng chỉ số)": "nhân vật nhận 100% hiệu năng chỉ số",
            "Gene chính (nhận 100% hiệu năng chỉ số và kỹ năng) cùng Gene phụ (nhận 30% chỉ số cộng thêm)": "Gene chính nhận 100% hiệu năng chỉ số và kỹ năng cùng Gene phụ nhận 30% chỉ số cộng thêm",
            "chính (100% hiệu năng chỉ số) và 1 Gene phụ (30% chỉ số)": "chính nhận 100% hiệu năng chỉ số và 1 Gene phụ nhận 30% chỉ số",
            "Tier 5 (ví dụ: Kim + Phong)": "Tier 5 như hệ Kim kết hợp với hệ Phong",
            "Thượng (Ultimate Gene)": "Thượng Ultimate Gene",
            "bản (Dungeon Instance)": "bản Dungeon Instance",
            "API (REST API/SignalR)": "API gồm REST API và SignalR Hub",
            "Database (MySQL)": "Database MySQL",
            "cấu hình (VPS)": "cấu hình trên VPS",
            "Tầng Client (Unity Client)": "Tầng Client Unity Client",
            "di chuyển (Client-side prediction)": "di chuyển Client-side prediction",
            "Server (Unity Headless)": "Server Unity Headless",
            "Server (ASP.NET Core 8)": "Server ASP.NET Core 8",
            "chờ (Lobby)": "chờ Lobby",
            "chờ (Lobby)": "chờ Lobby",
            "tổ đội (Party)": "tổ đội Party",
            "phòng (Host)": "phòng Host",
            "phó bản (Wave)": "phó bản Wave",
            "đợt (wave)": "đợt wave",
            "mật khẩu (BCrypt)": "mật khẩu qua BCrypt",
            "nội bộ (Zone API Key)": "nội bộ Zone API Key",
            "mạng (Dedicated Server)": "mạng Dedicated Server",
            "lại (ví dụ: Phase 1: 100% HP, Phase 2: <60% HP, Phase 3: <30% HP)": "lại ví dụ Phase 1 có 100% HP, Phase 2 dưới 60% HP, Phase 3 dưới 30% HP",
            "trễ (Latency)": "trễ mạng Latency",
            "di chuyển (Client-prediction)": "di chuyển Client-prediction",
            "lùi (rubber banding)": "lùi rubber banding",
            "chủ (Server)": "chủ Server",
            "phân (Zone)": "phân Zone",
            "API (REST API)": "API REST API",
            "DB (MySQL)": "DB MySQL",
            "ảnh (logo động)": "ảnh logo động",
            "lỗi (ví dụ: sai mật khẩu, tài khoản không tồn tại, mất kết nối)": "lỗi ví dụ sai mật khẩu, tài khoản không tồn tại, mất kết nối",
            "thế giới (World)": "thế giới World",
            "đội (Party)": "đội Party",
            "tư (Private)": "tư Private",
            "bạn (Friend list)": "bạn Friend list",
            "Wave (ví dụ: Wave 3/5)": "Wave ví dụ Wave 3 trên 5",
            "trực tuyến (SignalR)": "trực tuyến qua SignalR",
            "trò (gameplay)": "trò gameplay",
            "mạng (UDP/TCP)": "mạng UDP và TCP",
            "phần (Server-Authoritative)": "phần Server-Authoritative",
            "phòng (Cheat Engine)": "phòng Cheat Engine",
            "nhân (x1.5)": "nhân x1.5",
            "nhân (x0.75)": "nhân x0.75",
            "nhân (x1.0)": "nhân x1.0",
            "chúng (Sequence Diagram)": "chúng Sequence Diagram",
            "phần (JWT, Connection Approval, API Key)": "phần gồm JWT, Connection Approval và API Key",
            "máy (VPS Linux)": "máy VPS Linux",
            "hóa (Container)": "hóa Container",
            "chạy (Unity headless build)": "chạy Unity headless build",
            "mã (JWT)": "mã JWT",
            "chữ (HMAC-SHA256)": "chữ HMAC-SHA256",
            "bộ (Zone API Key)": "bộ Zone API Key",
            "thống (Lobby)": "thống Lobby",
            "bộ (SignalR)": "bộ SignalR",
            "nhân (x1.5)": "nhân x1.5",
            "trọng (Variable Gravity Jump)": "trọng Variable Gravity Jump",
            "mép (coyote time)": "mép coyote time",
            "đất (jump buffer)": "đất jump buffer",
            "sau (Lobby)": "sau Lobby",
            "dạng (JSON)": "dạng JSON",
            "nhiều (1-nhiều)": "nhiều một nhiều",
            "quá (JOIN)": "quá JOIN",
            "bản (Instance)": "bản Instance",
            "lượng (Exp, Gold, Gene Core)": "lượng Exp, Gold, Gene Core",
            "lệnh (ServerRpc)": "lệnh ServerRpc",
            "API (REST API)": "API REST API",
            "tin (ClientRpc)": "tin ClientRpc",
            "nhau (ví dụ: Hỏa và Lôi)": "nhau ví dụ Hỏa và Lôi",
            "mới (Hybrid Gene)": "mới Hybrid Gene",
            "quang (Aura)": "quang Aura",
            "khảm (Vũ khí, Giáp, Dây chuyền, Nhẫn)": "khảm gồm Vũ khí, Giáp, Dây chuyền và Nhẫn",
            "điểm (Sức mạnh, Khéo léo, Trí tuệ)": "điểm gồm Sức mạnh, Khéo léo, Trí tuệ",
            "máu (HP)": "máu HP",
            "đột (Wave 3/5)": "đột Wave 3 trên 5",
            "chức (NPC Blacksmith)": "chức NPC Blacksmith",
            "trễ (Latency)": "trễ mạng Latency",
            "mạng (RTT)": "mạng RTT",
            "đầu (Client-prediction)": "đầu Client-prediction",
            "máy (Cheat Engine)": "máy Cheat Engine",
            "mô (Server-Authoritative)": "mô Server-Authoritative",
            "dựng (Dedicated Server)": "dựng Dedicated Server",
            "chế (Gene Ngũ Hành)": "chế Gene Ngũ Hành",
            "thức (Hybrid Fusion)": "thức Hybrid Fusion",
            "thượng (Ultimate Gene)": "thượng Ultimate Gene",
            "mại (PvP)": "mại PvP",
            "quái (Boss thế giới)": "quái Boss thế giới",
            "ngại (Lobby)": "ngại Lobby",
            "mạng (UDP)": "mạng UDP",
            "tin (SignalR)": "tin SignalR",
            "tuần (FSM)": "tuần FSM",
            "chủ (Dedicated Server)": "chủ Dedicated Server",
            "kiến (Server-Authoritative)": "kiến Server-Authoritative",
            "đoán (Client-side Prediction)": "đoán Client-side Prediction",
            "khu (Zone-based Server)": "khu Zone-based Server",
            "dữ (ERD)": "dữ ERD",
            "cấu (JSON)": "cấu JSON",
            "REST API/SignalR": "REST API và SignalR Hub",
            "MySQL (Database)": "MySQL cho Database",
            "Docker Compose (VPS)": "Docker Compose trên VPS",
            "dự đoán (Client-side prediction)": "dự đoán Client-side prediction",
            "Dedicated Server (Unity Headless)": "Dedicated Server Unity Headless",
            "API & SignalR Server (ASP.NET Core 8)": "API và SignalR Server ASP.NET Core 8",
            "Tầng Database (MySQL Database)": "Tầng Database MySQL Database",
            "mức (General Use Case)": "mức Use Case tổng quát",
            "chơi (Player)": "chơi Player",
            "chủ (Server)": "chủ Server",
            "kỹ (Q, W, E, R)": "kỹ Q, W, E, R",
            "ngại (bạc, vật phẩm tiến hóa)": "ngại gồm bạc và vật phẩm tiến hóa",
            "bản (Dungeon Instance)": "bản Dungeon Instance",
            "quái (wave)": "quái wave",
            "thống (Lobby)": "thống Lobby",
            "users (tài khoản)": "users lưu tài khoản",
            "player_data (chỉ số nhân vật)": "player_data lưu chỉ số nhân vật",
            "inventory (vật phẩm)": "inventory lưu vật phẩm",
            "gene_data (dữ liệu Gene)": "gene_data lưu dữ liệu Gene",
            "động (JSON)": "động JSON",
            "nhiều (1-nhiều)": "nhiều một nhiều",
            "lớn (JOIN)": "lớn JOIN",
            "phần (Server-Authoritative)": "phần Server-Authoritative",
            "mạng (UDP)": "mạng UDP",
            "mạng (JWT, Connection Approval, API Key)": "mạng gồm JWT, Connection Approval và API Key",
            "Container (Docker)": "Container Docker",
            "tế (chỉ số FPS/mạng)": "tế gồm chỉ số FPS và độ trễ mạng",
            "cáo (Thesis Report)": "cáo báo cáo",
            "đề (Introduction)": "đề đặt vấn đề",
            "học (UET)": "học",
            "mật (BCrypt)": "mật BCrypt",
            "lớp (Zone API Key)": "lớp Zone API Key",
            "chấp (Connection Approval)": "chấp Connection Approval",
            "trí (Lobby)": "trí Lobby",
            "chức (NPC)": "chức NPC",
            "phòng (Dungeon Instance)": "phòng Dungeon Instance",
            "mạnh (ATK, HP, DEF, CRIT)": "mạnh gồm ATK, HP, DEF và CRIT",
            "lần (Level Up)": "lần Level Up",
            "điểm (Stats)": "điểm Stats",
            "đợt (Wave 3/5)": "đợt Wave 3 trên 5",
            "thế (World)": "thế World",
            "đội (Party)": "đội Party",
            "riêng (Private)": "riêng Private",
            "khả (Friend)": "khả Friend",
            "mạng (RTT)": "mạng RTT",
            "trình (Cheat Engine)": "trình Cheat Engine",
            "thắng (Lobby)": "thắng Lobby",
            "ảnh (Aura)": "ảnh Aura",
            "chủ (Server-Authoritative)": "chủ Server-Authoritative",
            "mạng (UDP/TCP)": "mạng UDP và TCP",
            "phân (Zone-based)": "phân Zone-based",
            "hình (ERD)": "hình ERD",
            "phần (JSON)": "phần JSON",
            "phẩm (Dungeon Instance)": "phẩm Dungeon Instance",
            "lệnh (ServerRpc)": "lệnh ServerRpc",
            "tin (ClientRpc)": "tin ClientRpc",
            "nhau (ví dụ: Hỏa và Lôi)": "nhau ví dụ Hỏa và Lôi",
            "lai (Hybrid Gene)": "lai Hybrid Gene",
            "ảnh (Aura)": "ảnh Aura",
            "trang (Vũ khí, Giáp, Dây chuyền, Nhẫn)": "trang gồm Vũ khí, Giáp, Dây chuyền và Nhẫn",
            "động (Stats)": "động Stats",
            "đợt (Wave 3/5)": "đợt Wave 3 trên 5",
            "chức (NPC)": "chức NPC",
            "trực (SignalR)": "trực SignalR",
            "trình (Cheat Engine)": "trình Cheat Engine",
            "thắng (Lobby)": "thắng Lobby",
            "ảnh (Aura)": "ảnh Aura",
            "chủ (Server-Authoritative)": "chủ Server-Authoritative",
            "mạng (UDP/TCP)": "mạng UDP và TCP",
            "phân (Zone-based)": "phân Zone-based",
            "hình (ERD)": "hình ERD",
            "phần (JSON)": "phần JSON",
            "phẩm (Dungeon Instance)": "phẩm Dungeon Instance",
            "lệnh (ServerRpc)": "lệnh ServerRpc",
            "tin (ClientRpc)": "tin ClientRpc",
            "nhau (ví dụ: Hỏa và Lôi)": "nhau ví dụ Hỏa và Lôi",
            "lai (Hybrid Gene)": "lai Hybrid Gene",
            "ảnh (Aura)": "ảnh Aura",
            "trang (Vũ khí, Giáp, Dây chuyền, Nhẫn)": "trang gồm Vũ khí, Giáp, Dây chuyền và Nhẫn",
            "động (Stats)": "động Stats",
            "đợt (Wave 3/5)": "đợt Wave 3 trên 5",
            "chức (NPC)": "chức NPC",
            "trực (SignalR)": "trực SignalR",
            "trình (Cheat Engine)": "trình Cheat Engine",
            "thắng (Lobby)": "thắng Lobby",
            "ảnh (Aura)": "ảnh Aura",
            "chủ (Server-Authoritative)": "chủ Server-Authoritative",
            "mạng (UDP/TCP)": "mạng UDP và TCP",
            "phân (Zone-based)": "phân Zone-based",
            "hình (ERD)": "hình ERD",
            "phần (JSON)": "phần JSON",
            "phẩm (Dungeon Instance)": "phẩm Dungeon Instance",
            "lệnh (ServerRpc)": "lệnh ServerRpc",
            "tin (ClientRpc)": "tin ClientRpc",
            "nhau (ví dụ: Hỏa và Lôi)": "nhau ví dụ Hỏa và Lôi",
            "lai (Hybrid Gene)": "lai Hybrid Gene",
            "ảnh (Aura)": "ảnh Aura",
            "trang (Vũ khí, Giáp, Dây chuyền, Nhẫn)": "trang gồm Vũ khí, Giáp, Dây chuyền và Nhẫn",
            "động (Stats)": "động Stats",
            "đợt (Wave 3/5)": "đợt Wave 3 trên 5",
            "chức (NPC)": "chức NPC",
            "trực (SignalR)": "trực SignalR",
            "trình (Cheat Engine)": "trình Cheat Engine"
        }
        
        # Apply the specific replacements
        for old, new in replacements.items():
            line = line.replace(old, new)
            
        # Clean any generic remaining parentheses that might surround short explanations
        # e.g., (mã hóa mật khẩu), (độ trễ mạng), (coyote time)
        # We replace '(text)' with 'text'
        # We also want to match things like (World) and remove the parentheses
        # Regex to match content inside parentheses, but avoid matching markdown image syntax ![alt](path)
        # Let's match (text) where text does not contain '/' or '\' to avoid breaking image URLs.
        line = re.sub(r'\(([^)/]*)\)', r'\1', line)
        
        cleaned_lines.append(line)
        
    slides_processed.append("\n".join(cleaned_lines))

new_content = "\n\n---\n\n".join(slides_processed)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(new_content)

print("Slide markdown file cleaned and updated successfully!")
