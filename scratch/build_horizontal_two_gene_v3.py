from PIL import Image, ImageFont
import PIL.ImageDraw as ImageDraw
import math

# Use the correct source image which contains ONLY the diagram, no slide text
src_path = r'C:\Users\fl2k3\.gemini\antigravity\brain\5a503eea-7891-4ec5-8964-f7653533ac35\media__1780916337749.png'
img = Image.open(src_path)

if img.mode != 'RGBA':
    img = img.convert('RGBA')

# 1. Helper functions for gradient interpolation and extrapolation
def get_interpolated_bg(img_obj, x, y):
    c_left = img_obj.getpixel((400, y))
    c_right = img_obj.getpixel((640, y))
    t = (x - 400) / 240.0
    color = []
    for i in range(4):
        val = int(c_left[i] * (1 - t) + c_right[i] * t)
        color.append(val)
    return tuple(color)

def get_extrapolated_bg(img_obj, x, y):
    c_left = img_obj.getpixel((750, y))
    c_right = img_obj.getpixel((764, y))
    slope = [(c_right[i] - c_left[i]) / 14.0 for i in range(4)]
    color = []
    for i in range(4):
        val = int(c_right[i] + slope[i] * (x - 764))
        color.append(max(0, min(255, val)))
    return tuple(color)

def make_crop_transparent(img_obj, start_x, start_y, width, height):
    crop = img_obj.crop((start_x, start_y, start_x + width, start_y + height))
    transparent = Image.new('RGBA', crop.size)
    for cx in range(width):
        for cy in range(height):
            mx = start_x + cx
            my = start_y + cy
            r, g, b, a = crop.getpixel((cx, cy))
            bg_r, bg_g, bg_b, bg_a = img_obj.getpixel((mx, my))
            
            dist = math.sqrt((r - bg_r)**2 + (g - bg_g)**2 + (b - bg_b)**2)
            
            if dist < 12:
                transparent.putpixel((cx, cy), (0, 0, 0, 0))
            else:
                if dist < 24:
                    alpha = int((dist - 12) / 12.0 * 255)
                else:
                    alpha = 255
                transparent.putpixel((cx, cy), (r, g, b, alpha))
    return transparent

# 2. Extract transparent hexagons
# Gene Chính (center hexagon) is at (480, 220) to (560, 390)
gene_chinh_trans = make_crop_transparent(img, 480, 220, 80, 170)
# Gene Phụ (right hexagon) is at (560, 220) to (630, 390)
gene_phu_trans = make_crop_transparent(img, 560, 220, 70, 170)

# 3. Clean the middle column area by interpolating the background gradient
# We ONLY clean y = 180 to y = 395 so the bottom icons at y >= 400 are completely untouched!
for y in range(180, 396):
    for x in range(410, 630):
        bg_color = get_interpolated_bg(img, x, y)
        img.putpixel((x, y), bg_color)

# 4. Paste transparent hexagons horizontally
# We place Gene Chính at x=430, y=220 (centered at 470)
img.paste(gene_chinh_trans, (430, 220), gene_chinh_trans)
# We place Gene Phụ at x=530, y=220 (centered at 565)
img.paste(gene_phu_trans, (530, 220), gene_phu_trans)

# 5. Extend the card body to the right using extrapolated gradient
# Card right edge shadow at x=770 to 794 (width 24)
right_edge = img.crop((770, 0, 794, 510))

# Fill background with extrapolated gradient for x=770 to 890
for y in range(510):
    for x in range(770, 890):
        bg_color = get_extrapolated_bg(img, x, y)
        img.putpixel((x, y), bg_color)

# Paste original right edge shadow at x=890
img.paste(right_edge, (890, 0))

# 6. Draw Arrows
arrow_color = (169, 178, 195, 255)
draw = ImageDraw.Draw(img)

def draw_thick_line(draw_obj, start, end, thickness=3, color=arrow_color):
    draw_obj.line([start, end], fill=color, width=thickness)

# Arrow A: Col 1 to Gene Chính (horizontal at y=305)
draw_thick_line(draw, (403, 305), (428, 305))
draw.polygon([(424, 301), (428, 305), (424, 309)], fill=arrow_color)

# Arrow B: Gene Chính to Col 3 DNA
# Center of Gene Chính is (470, 305). Center of DNA is (670, 275)
draw_thick_line(draw, (510, 305), (638, 278))
draw.polygon([(632, 272), (638, 278), (635, 284)], fill=arrow_color)

# Arrow C: Gene Phụ to Col 3 DNA
# Center of Gene Phụ is (565, 305). Center of DNA is (670, 275)
draw_thick_line(draw, (600, 305), (638, 282))
draw.polygon([(635, 276), (638, 282), (632, 288)], fill=arrow_color)

# Arrow D: Col 3 DNA to Col 4 DNA
draw_thick_line(draw, (715, 288), (755, 288))
draw.polygon([(751, 284), (755, 288), (751, 292)], fill=arrow_color)

# 7. Column 4: Gene Tối Thượng (Ultimate Gene)
# Crop Hybrid DNA from Col 3
hybrid_crop = img.crop((640, 240, 700, 310))
gold_dna = Image.new('RGBA', hybrid_crop.size)

for cx in range(hybrid_crop.width):
    for cy in range(hybrid_crop.height):
        mx = 640 + cx
        my = 240 + cy
        r, g, b, a = hybrid_crop.getpixel((cx, cy))
        bg_r, bg_g, bg_b, bg_a = img.getpixel((mx, my))
        
        dist = math.sqrt((r - bg_r)**2 + (g - bg_g)**2 + (b - bg_b)**2)
        
        if dist < 15:
            gold_dna.putpixel((cx, cy), (0, 0, 0, 0))
        else:
            if dist < 30:
                alpha = int((dist - 15) / 15.0 * 255)
            else:
                alpha = 255
            
            lum = int(0.299*r + 0.587*g + 0.114*b)
            tr = int(250 * (255 - lum) / 255 + lum * 0.8)
            tg = int(185 * (255 - lum) / 255 + lum * 0.6)
            tb = int(20 * (255 - lum) / 255)
            gold_dna.putpixel((cx, cy), (min(255, tr), min(255, tg), min(255, tb), alpha))

# Soft golden aura circle centered at (800, 275)
draw.ellipse([780, 250, 830, 300], fill=(254, 243, 199, 120))

# Paste golden DNA at x=773, y=240
img.paste(gold_dna, (773, 240), gold_dna)

# 8. Draw text labels in Column 4
font_title = ImageFont.truetype(r'C:\Windows\Fonts\segoeuib.ttf', 13)
font_label_bold = ImageFont.truetype(r'C:\Windows\Fonts\segoeuib.ttf', 10)
font_label_reg = ImageFont.truetype(r'C:\Windows\Fonts\segoeui.ttf', 9)

title_text = "4. Gene Tối Thượng"
bbox = draw.textbbox((0, 0), title_text, font=font_title)
t_w = bbox[2] - bbox[0]
draw.text((800 - t_w // 2, 172), title_text, fill=(30, 41, 59, 255), font=font_title)

lbl1 = "Gene Tối Thượng"
bbox1 = draw.textbbox((0, 0), lbl1, font=font_label_bold)
lbl1_w = bbox1[2] - bbox1[0]
draw.text((800 - lbl1_w // 2, 323), lbl1, fill=(51, 65, 85, 255), font=font_label_bold)

lbl2 = "Cấp Cực Hạn"
bbox2 = draw.textbbox((0, 0), lbl2, font=font_label_reg)
lbl2_w = bbox2[2] - bbox2[0]
draw.text((800 - lbl2_w // 2, 337), lbl2, fill=(100, 116, 139, 255), font=font_label_reg)

# 9. Crop ONLY the card (leaving out all bottom footer elements)
# Card top is at 5, card bottom is at 510
cropped = img.crop((229, 5, 914, 510))
if cropped.mode == 'RGBA':
    cropped = cropped.convert('RGB')

cropped.save(r'c:\Hub\DoAn\extracted_images\gene_evolution_theory.png', 'PNG')
print("Successfully generated final clean 2-gene horizontal card diagram without slide text!")
