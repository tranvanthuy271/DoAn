from PIL import Image, ImageFont, ImageDraw
import math

# Load the correct source image
src_path = r'C:\Users\fl2k3\.gemini\antigravity\brain\5a503eea-7891-4ec5-8964-f7653533ac35\media__1780916337749.png'
img = Image.open(src_path)

# Create a clean high-resolution canvas for the card diagram
# We use size 800x480 for a modern, clean slide aspect ratio
canvas = Image.new('RGBA', (800, 480), (255, 255, 255, 0))
draw = ImageDraw.Draw(canvas)

# 1. Draw the card background with rounded corners and a premium shadow/border
# Card bounds inside canvas
card_box = [15, 15, 785, 465]
# Draw soft shadow (simulated with multiple semi-transparent rounded rectangles)
for i in range(5):
    shadow_box = [card_box[0] - i, card_box[1] - i, card_box[2] + i, card_box[3] + i]
    alpha = int(12 - i * 2)
    draw.rounded_rectangle(shadow_box, radius=16 + i, outline=(0, 0, 0, alpha), width=1)

# Draw card body
draw.rounded_rectangle(card_box, radius=16, fill=(248, 250, 252, 255), outline=(226, 232, 240, 255), width=2)

# 2. Draw Title and Subtitle at the top
font_title = ImageFont.truetype(r'C:\Windows\Fonts\segoeuib.ttf', 20)
font_subtitle = ImageFont.truetype(r'C:\Windows\Fonts\segoeui.ttf', 12)

title_text = "HỆ THỐNG TIẾN HÓA GENE TRONG GAME RPG"
bbox = draw.textbbox((0, 0), title_text, font=font_title)
t_w = bbox[2] - bbox[0]
draw.text((400 - t_w // 2, 40), title_text, fill=(15, 23, 42, 255), font=font_title)

subtitle_text = "Mô hình thiết kế và cơ chế hoạt động"
bbox_sub = draw.textbbox((0, 0), subtitle_text, font=font_subtitle)
sub_w = bbox_sub[2] - bbox_sub[0]
draw.text((400 - sub_w // 2, 70), subtitle_text, fill=(100, 116, 139, 255), font=font_subtitle)

# Helper function to make cropped elements transparent
def get_transparent_crop(src_img, x1, y1, x2, y2, bg_color=(248, 249, 251)):
    crop = src_img.crop((x1, y1, x2, y2))
    crop_rgba = crop.convert('RGBA')
    transparent = Image.new('RGBA', crop.size)
    for cx in range(crop.width):
        for cy in range(crop.height):
            r, g, b, a = crop_rgba.getpixel((cx, cy))
            dist = math.sqrt((r - bg_color[0])**2 + (g - bg_color[1])**2 + (b - bg_color[2])**2)
            if dist < 15:
                transparent.putpixel((cx, cy), (0, 0, 0, 0))
            else:
                if dist < 30:
                    alpha = int((dist - 15) / 15.0 * 255)
                else:
                    alpha = 255
                transparent.putpixel((cx, cy), (r, g, b, alpha))
    return transparent

# 3. Column 1: 1. Gene Cấp 1 đến 5
# Crop the DNA ladder block from the source image
col1_trans = get_transparent_crop(img, 260, 140, 400, 420)
canvas.paste(col1_trans, (40, 130), col1_trans)

# Draw Column 1 title
font_col_title = ImageFont.truetype(r'C:\Windows\Fonts\segoeuib.ttf', 13)
draw.text((40, 110), "1. Gene Cấp 1 đến 5", fill=(15, 23, 42, 255), font=font_col_title)

# 4. Column 2: 2. Khảm Multi-Gene
draw.text((220, 110), "2. Khảm Multi-Gene", fill=(15, 23, 42, 255), font=font_col_title)

# Helper function to draw hexagon
def draw_hexagon(draw_obj, cx, cy, r, outline_color, fill_color, width=2):
    points = []
    for i in range(6):
        angle = math.radians(i * 60 - 30)
        x = cx + r * math.cos(angle)
        y = cy + r * math.sin(angle)
        points.append((x, y))
    draw_obj.polygon(points, fill=fill_color, outline=outline_color, width=width)

# Hexagon 1: Gene Chính (100%)
draw_hexagon(draw, 270, 240, 36, (59, 130, 246, 255), (255, 255, 255, 255))
font_hex1 = ImageFont.truetype(r'C:\Windows\Fonts\segoeuib.ttf', 14)
draw.text((270 - 18, 240 - 10), "100%", fill=(59, 130, 246, 255), font=font_hex1)

font_label = ImageFont.truetype(r'C:\Windows\Fonts\segoeuib.ttf', 11)
font_label_sub = ImageFont.truetype(r'C:\Windows\Fonts\segoeui.ttf', 9)

txt_c1 = "Gene Chính"
bbox_c1 = draw.textbbox((0, 0), txt_c1, font=font_label)
draw.text((270 - (bbox_c1[2]-bbox_c1[0]) // 2, 295), txt_c1, fill=(15, 23, 42, 255), font=font_label)
txt_c2 = "Thuộc tính"
bbox_c2 = draw.textbbox((0, 0), txt_c2, font=font_label_sub)
draw.text((270 - (bbox_c2[2]-bbox_c2[0]) // 2, 310), txt_c2, fill=(100, 116, 139, 255), font=font_label_sub)

# Hexagon 2: Gene Phụ (30%)
draw_hexagon(draw, 355, 240, 30, (148, 163, 184, 255), (255, 255, 255, 255))
font_hex2 = ImageFont.truetype(r'C:\Windows\Fonts\segoeuib.ttf', 12)
draw.text((355 - 13, 240 - 8), "30%", fill=(148, 163, 184, 255), font=font_hex2)

txt_p1 = "Gene Phụ"
bbox_p1 = draw.textbbox((0, 0), txt_p1, font=font_label)
draw.text((355 - (bbox_p1[2]-bbox_p1[0]) // 2, 295), txt_p1, fill=(15, 23, 42, 255), font=font_label)
txt_p2 = "Thuộc tính"
bbox_p2 = draw.textbbox((0, 0), txt_p2, font=font_label_sub)
draw.text((355 - (bbox_p2[2]-bbox_p2[0]) // 2, 310), txt_p2, fill=(100, 116, 139, 255), font=font_label_sub)

# 5. Column 3: 3. Dung hợp Hybrid
draw.text((435, 110), "3. Dung hợp Hybrid", fill=(15, 23, 42, 255), font=font_col_title)
col3_trans = get_transparent_crop(img, 640, 140, 765, 420)
canvas.paste(col3_trans, (430, 130), col3_trans)

# 6. Column 4: 4. Gene Tối Thượng
draw.text((615, 110), "4. Gene Tối Thượng", fill=(15, 23, 42, 255), font=font_col_title)

# Draw golden DNA helix in Column 4 (using a crop of Hybrid DNA tinted to golden yellow)
hybrid_dna_crop = img.crop((640, 240, 700, 310))
gold_dna = Image.new('RGBA', hybrid_dna_crop.size)
for cx in range(hybrid_dna_crop.width):
    for cy in range(hybrid_dna_crop.height):
        r, g, b = hybrid_dna_crop.getpixel((cx, cy))[:3]
        dist = math.sqrt((r - 248)**2 + (g - 249)**2 + (b - 251)**2)
        if dist < 15:
            gold_dna.putpixel((cx, cy), (0, 0, 0, 0))
        else:
            if dist < 30:
                alpha = int((dist - 15) / 15.0 * 255)
            else:
                alpha = 255
            # Apply golden color tint
            lum = int(0.299*r + 0.587*g + 0.114*b)
            tr = int(251 * (255 - lum) / 255 + lum * 0.8)
            tg = int(191 * (255 - lum) / 255 + lum * 0.6)
            tb = int(36 * (255 - lum) / 255)
            gold_dna.putpixel((cx, cy), (min(255, tr), min(255, tg), min(255, tb), alpha))

# Paste golden aura background circle centered at (675, 260)
draw.ellipse([645, 230, 705, 290], fill=(254, 243, 199, 140))
canvas.paste(gold_dna, (645, 225), gold_dna)

# Draw Column 4 labels
txt_u1 = "Gene Tối Thượng"
bbox_u1 = draw.textbbox((0, 0), txt_u1, font=font_label)
draw.text((675 - (bbox_u1[2]-bbox_u1[0]) // 2, 295), txt_u1, fill=(15, 23, 42, 255), font=font_label)
txt_u2 = "Cấp Cực Hạn"
bbox_u2 = draw.textbbox((0, 0), txt_u2, font=font_label_sub)
draw.text((675 - (bbox_u2[2]-bbox_u2[0]) // 2, 310), txt_u2, fill=(100, 116, 139, 255), font=font_label_sub)

# 7. Draw clean, vector-quality Connecting Arrows
arrow_color = (169, 178, 195, 255)

def draw_arrow_line(draw_obj, start, end, thickness=3, color=arrow_color):
    draw_obj.line([start, end], fill=color, width=thickness)
    # Draw arrowhead
    dx = end[0] - start[0]
    dy = end[1] - start[1]
    angle = math.atan2(dy, dx)
    head_len = 5
    p1 = (end[0] - head_len * math.cos(angle - math.pi/6), end[1] - head_len * math.sin(angle - math.pi/6))
    p2 = (end[0] - head_len * math.cos(angle + math.pi/6), end[1] - head_len * math.sin(angle + math.pi/6))
    draw_obj.polygon([end, p1, p2], fill=color)

# Arrow from Col 1 to Col 2
draw_arrow_line(draw, (180, 240), (225, 240))
# Arrow from Hex 1 to Col 3 DNA
draw_arrow_line(draw, (308, 240), (425, 240))
# Arrow from Hex 2 to Col 3 DNA
draw_arrow_line(draw, (387, 240), (425, 240))
# Arrow from Col 3 to Col 4
draw_arrow_line(draw, (555, 240), (630, 240))

# 8. Draw Bottom Icons & Labels (100% clean and free of spelling squiggles!)
# Crop the actual icons from source image
rocket_icon = get_transparent_crop(img, 390, 425, 435, 465)
hand_icon = get_transparent_crop(img, 500, 425, 545, 465)
flow_icon = get_transparent_crop(img, 610, 425, 655, 465)

font_bottom = ImageFont.truetype(r'C:\Windows\Fonts\segoeuib.ttf', 11)

# Rocket section
canvas.paste(rocket_icon, (110, 390), rocket_icon)
draw.text((160, 400), "Tăng Cường Sức Mạnh", fill=(51, 65, 85, 255), font=font_bottom)

# Hand section
canvas.paste(hand_icon, (330, 390), hand_icon)
draw.text((380, 400), "Mở Khóa Kỹ Năng Mới", fill=(51, 65, 85, 255), font=font_bottom)

# Flow section
canvas.paste(flow_icon, (550, 390), flow_icon)
draw.text((600, 400), "Tùy Biến Đa Dạng", fill=(51, 65, 85, 255), font=font_bottom)

# Save the final clean card image
final_img = canvas.convert('RGB')
final_img.save(r'c:\Hub\DoAn\extracted_images\gene_evolution_theory.png', 'PNG')
print("Successfully generated clean, vector-quality diagram without squiggles, footers, or stretch artifacts!")
