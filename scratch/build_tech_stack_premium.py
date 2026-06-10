from PIL import Image, ImageFont, ImageDraw
import os

# Create a clean high-resolution canvas for the tech stack infographic (1200x675)
width, height = 1200, 675
canvas = Image.new('RGBA', (width, height), (255, 255, 255, 0))
draw = ImageDraw.Draw(canvas)

# 1. Draw premium subtle background shadow for the outer card
outer_box = [20, 20, 1180, 655]
for i in range(8):
    shadow_box = [outer_box[0] - i, outer_box[1] - i, outer_box[2] + i, outer_box[3] + i]
    alpha = int(10 - i * 1.2)
    if alpha > 0:
        draw.rounded_rectangle(shadow_box, radius=20 + i, outline=(15, 23, 42, alpha), width=1)

# Card main body (very clean white-blue background)
draw.rounded_rectangle(outer_box, radius=20, fill=(250, 252, 254, 255), outline=(226, 232, 240, 255), width=2)

# 2. Draw Title
font_title = ImageFont.truetype(r'C:\Windows\Fonts\segoeuib.ttf', 24)
title_text = "HỆ THỐNG CÔNG NGHỆ & KIẾN TRÚC MẠNG"
bbox = draw.textbbox((0, 0), title_text, font=font_title)
t_w = bbox[2] - bbox[0]
draw.text((width // 2 - t_w // 2, 45), title_text, fill=(15, 23, 42, 255), font=font_title)

# Underline/indicator line for title
draw.line([(width // 2 - 80, 85), (width // 2 + 80, 85)], fill=(59, 130, 246, 255), width=3)

# Fonts
font_col = ImageFont.truetype(r'C:\Windows\Fonts\segoeuib.ttf', 15)
font_box_title = ImageFont.truetype(r'C:\Windows\Fonts\segoeuib.ttf', 12)
font_box_desc = ImageFont.truetype(r'C:\Windows\Fonts\segoeui.ttf', 10)

# Column config
col_y = 110
col_h = 515
cols_x = [50, 440, 830]
col_w = 320

col_titles = [
    "TẦNG CLIENT (UNITY)",
    "GIAO THỨC TRUYỀN THÔNG",
    "TẦNG SERVER & DATABASE"
]

colors_bg = [
    (240, 246, 255, 255), # Soft blue
    (240, 253, 250, 255), # Soft teal
    (247, 245, 255, 255)  # Soft purple
]

colors_border = [
    (191, 219, 254, 255),
    (153, 246, 228, 255),
    (221, 214, 254, 255)
]

for idx, x in enumerate(cols_x):
    col_box = [x, col_y, x + col_w, col_y + col_h]
    # Draw column panel
    draw.rounded_rectangle(col_box, radius=16, fill=colors_bg[idx], outline=colors_border[idx], width=2)
    # Header background strip
    draw.rounded_rectangle([x + 2, col_y + 2, x + col_w - 2, col_y + 40], radius=14, fill=colors_border[idx])
    # Column Title
    bbox_ct = draw.textbbox((0, 0), col_titles[idx], font=font_col)
    ct_w = bbox_ct[2] - bbox_ct[0]
    draw.text((x + col_w // 2 - ct_w // 2, col_y + 12), col_titles[idx], fill=(30, 41, 59, 255), font=font_col)

# Helper to load and paste logos with high quality resizing
def paste_logo(canvas_img, logo_name, x, y, size=38):
    path = f'c:\\Hub\\DoAn\\scratch\\logo_{logo_name}.png'
    if not os.path.exists(path):
        # Fallback to key/shield/api icon if not exists
        path = f'c:\\Hub\\DoAn\\scratch\\logo_key.png'
    if os.path.exists(path):
        try:
            logo = Image.open(path).convert('RGBA')
            logo = logo.resize((size, size), Image.Resampling.LANCZOS)
            canvas_img.paste(logo, (x, y), logo)
        except Exception as e:
            print(f"Error pasting {logo_name}: {e}")

# Helper function to draw technology boxes
def draw_tech_box(draw_obj, canvas_img, x, y, w, h, title, desc, logo_name, border_color):
    box = [x, y, x + w, y + h]
    # Draw white rounded box
    draw_obj.rounded_rectangle(box, radius=10, fill=(255, 255, 255, 255), outline=border_color, width=2)
    
    # Draw Logo Icon on the left
    icon_padding = (h - 38) // 2
    paste_logo(canvas_img, logo_name, x + 12, y + icon_padding, size=38)
    
    # Title
    draw_obj.text((x + 62, y + 12), title, fill=(15, 23, 42, 255), font=font_box_title)
    # Desc
    draw_obj.text((x + 62, y + 34), desc, fill=(100, 116, 139, 255), font=font_box_desc)

# Column 1: Client side (2 items, centered vertically)
draw_tech_box(draw, canvas, 65, col_y + 120, 290, 75, "Unity 2D Engine", "Xây dựng Game Client, hoạt ảnh & HUD", "unity", (191, 219, 254, 255))
draw_tech_box(draw, canvas, 65, col_y + 270, 290, 75, "Ngôn ngữ C#", "Lập trình logic OOP & Client Netcode", "csharp", (191, 219, 254, 255))

# Column 2: Network protocols (2 items, centered vertically)
draw_tech_box(draw, canvas, 455, col_y + 120, 290, 75, "Giao thức UDP (NGO)", "Đồng bộ di chuyển & chiến đấu realtime", "globe", (153, 246, 228, 255))
draw_tech_box(draw, canvas, 455, col_y + 270, 290, 75, "Giao thức TCP (SignalR)", "Dịch vụ Sảnh, Kênh chat & Tổ đội", "chat", (153, 246, 228, 255))

# Column 3: Server/DB side (4 items, distributed vertically)
draw_tech_box(draw, canvas, 845, col_y + 60, 290, 75, "Dedicated Server", "Unity Headless chạy logic vật lý & AI FSM", "unity", (221, 214, 254, 255))
draw_tech_box(draw, canvas, 845, col_y + 165, 290, 75, "ASP.NET Core 8 Web API", "REST API quản lý account & JWT token", "dotnet", (221, 214, 254, 255))
draw_tech_box(draw, canvas, 845, col_y + 270, 290, 75, "Cơ sở dữ liệu MySQL", "Lưu trữ tài khoản, túi đồ qua EF Core", "mysql", (221, 214, 254, 255))
draw_tech_box(draw, canvas, 845, col_y + 375, 290, 75, "Docker Container", "Đóng gói ảo hóa và VPS deployment", "docker", (221, 214, 254, 255))

# Draw premium curved arrows between columns
def draw_premium_connector(draw_obj, start_x, start_y, end_x, end_y, color=(148, 163, 184, 180)):
    # Draw horizontal line with a small circle at start
    draw_obj.ellipse([start_x - 3, start_y - 3, start_x + 3, start_y + 3], fill=color)
    # Line
    draw_obj.line([(start_x, start_y), (end_x, end_y)], fill=color, width=3)
    # Arrow head
    draw_obj.polygon([(end_x - 6, end_y - 4), (end_x, end_y), (end_x - 6, end_y + 4)], fill=color)

# Connect Client components to UDP & TCP Protocols
draw_premium_connector(draw, 355, col_y + 157, 455, col_y + 157) # Unity to UDP
draw_premium_connector(draw, 355, col_y + 307, 455, col_y + 307) # C# to TCP

# Connect Network Protocols to Server/DB
draw_premium_connector(draw, 745, col_y + 157, 845, col_y + 97) # UDP to Dedicated Server
draw_premium_connector(draw, 745, col_y + 307, 845, col_y + 202) # TCP to ASP.NET Core API

# Save the final clean image
final_img = canvas.convert('RGB')
final_img.save(r'c:\Hub\DoAn\extracted_images\network_and_tech_stack.png', 'PNG')
print("Successfully generated Premium Simplified Technology Stack infographic!")
