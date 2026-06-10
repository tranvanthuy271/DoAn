from PIL import Image, ImageFont, ImageDraw
import math

# Create a clean high-resolution canvas for the tech stack infographic
# Size 800x480 for standard slide display
canvas = Image.new('RGBA', (800, 480), (255, 255, 255, 0))
draw = ImageDraw.Draw(canvas)

# 1. Draw card background with rounded corners and a premium shadow
card_box = [15, 15, 785, 465]
for i in range(5):
    shadow_box = [card_box[0] - i, card_box[1] - i, card_box[2] + i, card_box[3] + i]
    alpha = int(12 - i * 2)
    draw.rounded_rectangle(shadow_box, radius=16 + i, outline=(0, 0, 0, alpha), width=1)

draw.rounded_rectangle(card_box, radius=16, fill=(248, 250, 252, 255), outline=(226, 232, 240, 255), width=2)

# 2. Draw Title
font_title = ImageFont.truetype(r'C:\Windows\Fonts\segoeuib.ttf', 20)
title_text = "STACK CÔNG NGHỆ & KIẾN TRÚC MẠNG"
bbox = draw.textbbox((0, 0), title_text, font=font_title)
t_w = bbox[2] - bbox[0]
draw.text((400 - t_w // 2, 35), title_text, fill=(15, 23, 42, 255), font=font_title)

# Fonts for boxes and columns
font_col = ImageFont.truetype(r'C:\Windows\Fonts\segoeuib.ttf', 13)
font_box_title = ImageFont.truetype(r'C:\Windows\Fonts\segoeuib.ttf', 11)
font_box_desc = ImageFont.truetype(r'C:\Windows\Fonts\segoeui.ttf', 9)

# Draw column backgrounds and titles
# We have 3 main columns: Client, Network, Server/DB
col_y = 90
col_h = 350
cols_x = [45, 285, 525]
col_w = 230

col_titles = [
    "TẦNG CLIENT (UNITY)",
    "GIAO THỨC TRUYỀN THÔNG",
    "TẦNG SERVER & DATABASE"
]

colors_bg = [
    (239, 246, 255, 255), # Light Blue
    (240, 253, 250, 255), # Light Teal
    (245, 243, 255, 255)  # Light Purple
]

colors_border = [
    (191, 219, 254, 255),
    (153, 246, 228, 255),
    (221, 214, 254, 255)
]

for idx, x in enumerate(cols_x):
    # Draw column background panel
    col_box = [x, col_y, x + col_w, col_y + col_h]
    draw.rounded_rectangle(col_box, radius=12, fill=colors_bg[idx], outline=colors_border[idx], width=1)
    # Column Title
    bbox_ct = draw.textbbox((0, 0), col_titles[idx], font=font_col)
    ct_w = bbox_ct[2] - bbox_ct[0]
    draw.text((x + col_w // 2 - ct_w // 2, col_y + 15), col_titles[idx], fill=(30, 41, 59, 255), font=font_col)

# Helper function to draw technology boxes
def draw_tech_box(draw_obj, x, y, w, h, title, desc, icon_color):
    box = [x, y, x + w, y + h]
    # White box with clean border
    draw_obj.rounded_rectangle(box, radius=8, fill=(255, 255, 255, 255), outline=(226, 232, 240, 255), width=2)
    # Colored indicator block on the left
    draw_obj.rounded_rectangle([x + 2, y + 2, x + 8, y + h - 2], radius=4, fill=icon_color)
    # Title
    draw_obj.text((x + 16, y + 8), title, fill=(15, 23, 42, 255), font=font_box_title)
    # Desc
    draw_obj.text((x + 16, y + 24), desc, fill=(100, 116, 139, 255), font=font_box_desc)

# Column 1: Client side
draw_tech_box(draw, 60, col_y + 50, 200, 50, "Unity 2D Engine", "Game Client, Animations & UI", (59, 130, 246, 255))
draw_tech_box(draw, 60, col_y + 120, 200, 50, "C# Programming", "OOP Logic, Scripting & Client Netcode", (37, 99, 235, 255))
draw_tech_box(draw, 60, col_y + 190, 200, 50, "Client-Side Prediction", "Local input prediction to zero latency", (96, 165, 250, 255))
draw_tech_box(draw, 60, col_y + 260, 200, 50, "Physics & Colliders 2D", "Character platformer movement physics", (29, 78, 216, 255))

# Column 2: Network protocols
draw_tech_box(draw, 300, col_y + 50, 200, 65, "UDP Protocol", "Netcode for GameObjects (NGO)\nRealtime movement & combat sync", (13, 148, 136, 255))
draw_tech_box(draw, 300, col_y + 135, 200, 65, "TCP Protocol", "SignalR WebSockets Hub\nMeta-game Chat, Party & Lobby", (20, 184, 166, 255))
draw_tech_box(draw, 300, col_y + 220, 200, 50, "dedicated Server RPC", "ServerRpc & ClientRpc packet delivery", (45, 212, 191, 255))
draw_tech_box(draw, 300, col_y + 285, 200, 50, "Connection Approval", "Token-based handshake authorization", (15, 118, 110, 255))

# Column 3: Server/DB side
draw_tech_box(draw, 540, col_y + 50, 200, 50, "Dedicated Server", "Unity Headless Build, Server-Authoritative", (124, 58, 237, 255))
draw_tech_box(draw, 540, col_y + 110, 200, 50, "REST Web API (.NET 8)", "ASP.NET Core, JWT, User & Inventory DB", (139, 92, 246, 255))
draw_tech_box(draw, 540, col_y + 170, 200, 50, "MySQL Database", "Relational persistence via Entity Framework", (109, 40, 217, 255))
draw_tech_box(draw, 540, col_y + 230, 200, 50, "Docker & Docker Compose", "Isolated container virtualization on VPS", (167, 139, 250, 255))
draw_tech_box(draw, 540, col_y + 290, 200, 50, "Linux Cloud VPS (Ubuntu)", "Dedicated hosting on cloud VPS network", (76, 29, 149, 255))

# Draw connecting arrows between columns
def draw_connector(draw_obj, start, end, color=(169, 178, 195, 180)):
    draw_obj.line([start, end], fill=color, width=2)

# Connect Client to Network
draw_connector(draw, (260, col_y + 75), (300, col_y + 82))
draw_connector(draw, (260, col_y + 145), (300, col_y + 167))

# Connect Network to Server
draw_connector(draw, (500, col_y + 82), (540, col_y + 75))
draw_connector(draw, (500, col_y + 167), (540, col_y + 135))

# Save the final clean image
final_img = canvas.convert('RGB')
final_img.save(r'c:\Hub\DoAn\extracted_images\network_and_tech_stack.png', 'PNG')
print("Successfully generated Technology Stack infographic!")
