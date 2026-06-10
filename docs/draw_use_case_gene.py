import os
from PIL import Image, ImageDraw, ImageFont, ImageFilter

def draw_diagram():
    # 4x size for supersampling (anti-aliasing)
    scale = 4
    width, height = 891 * scale, 557 * scale
    
    # Create image
    img = Image.new("RGBA", (width, height), (255, 255, 255, 255))
    
    # Create a separate layer for shadows to apply blur
    shadow_img = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    shadow_draw = ImageDraw.Draw(shadow_img)
    
    # Load fonts
    try:
        font_path = "C:\\Windows\\Fonts\\arial.ttf"
        font_bold_path = "C:\\Windows\\Fonts\\arialbd.ttf"
        font_reg = ImageFont.truetype(font_path, 13 * scale)
        font_bold = ImageFont.truetype(font_bold_path, 15 * scale)
        font_title = ImageFont.truetype(font_bold_path, 16 * scale)
        font_small = ImageFont.truetype(font_path, 11 * scale)
    except IOError:
        font_reg = ImageFont.load_default()
        font_bold = ImageFont.load_default()
        font_title = ImageFont.load_default()
        font_small = ImageFont.load_default()

    # Define ellipses (x, y coordinates in original space * scale)
    # format: (center_x, center_y, rx, ry, text, is_bold)
    ellipses = [
        # Main use case
        (380 * scale, 260 * scale, 130 * scale, 40 * scale, "Phát triển Gene\nvà Hybrid", True),
        # Included / Extended UCs
        (720 * scale, 110 * scale, 110 * scale, 28 * scale, "Nâng Gene chính", False),
        (720 * scale, 210 * scale, 110 * scale, 28 * scale, "Nâng Gene phụ", False),
        (720 * scale, 310 * scale, 110 * scale, 28 * scale, "Dung hợp Hybrid Gene", False),
        (720 * scale, 440 * scale, 110 * scale, 28 * scale, "Thức tỉnh Gene\nTối Thượng", False),
    ]

    # 1. Draw shadows on shadow layer
    shadow_offset_x = 3 * scale
    shadow_offset_y = 4 * scale
    for cx, cy, rx, ry, text, is_bold in ellipses:
        # Draw gray shadow ellipse
        shadow_draw.ellipse(
            [cx - rx + shadow_offset_x, cy - ry + shadow_offset_y, cx + rx + shadow_offset_x, cy + ry + shadow_offset_y],
            fill=(200, 200, 200, 150)
        )
        
    # Blur the shadow layer
    shadow_blurred = shadow_img.filter(ImageFilter.GaussianBlur(radius=3 * scale))
    img.alpha_composite(shadow_blurred)
    
    # 2. Draw system boundary box and actor
    draw = ImageDraw.Draw(img)
    
    # System boundary box (black outline)
    box_x1, box_y1 = 190 * scale, 30 * scale
    box_x2, box_y2 = 860 * scale, 520 * scale
    draw.rectangle([box_x1, box_y1, box_x2, box_y2], outline=(0, 0, 0, 255), width=1*scale)
    
    # Title of System boundary box
    title_text = "Phát triển Gene và Hybrid"
    # Measure text size using draw.textlength or font.getbbox
    title_w = font_title.getbbox(title_text)[2] - font_title.getbbox(title_text)[0]
    draw.text((((box_x1 + box_x2) - title_w) // 2, box_y1 + 10 * scale), title_text, fill=(0, 0, 0, 255), font=font_title)

    # Actor: Stick figure
    act_cx, act_cy = 60 * scale, 210 * scale
    # Head
    head_r = 14 * scale
    draw.ellipse([act_cx - head_r, act_cy - head_r, act_cx + head_r, act_cy + head_r], outline=(0, 0, 0, 255), width=1*scale)
    # Body line
    body_top = act_cy + head_r
    body_bottom = body_top + 40 * scale
    draw.line([act_cx, body_top, act_cx, body_bottom], fill=(0, 0, 0, 255), width=1*scale)
    # Arms (angled)
    arm_y = body_top + 10 * scale
    draw.line([act_cx, arm_y, act_cx - 25 * scale, arm_y - 20 * scale], fill=(0, 0, 0, 255), width=1*scale)
    draw.line([act_cx, arm_y, act_cx + 25 * scale, arm_y + 10 * scale], fill=(0, 0, 0, 255), width=1*scale) # arm extending to the right
    # Legs
    draw.line([act_cx, body_bottom, act_cx - 18 * scale, body_bottom + 35 * scale], fill=(0, 0, 0, 255), width=1*scale)
    draw.line([act_cx, body_bottom, act_cx + 18 * scale, body_bottom + 35 * scale], fill=(0, 0, 0, 255), width=1*scale)
    # Label "Người chơi"
    label_text = "Người chơi"
    label_w = font_reg.getbbox(label_text)[2] - font_reg.getbbox(label_text)[0]
    draw.text((act_cx - label_w // 2, body_bottom + 42 * scale), label_text, fill=(0, 0, 0, 255), font=font_reg)

    # Connection from Actor arm to Main UC
    # From right arm tip (act_cx + 25, arm_y + 10) to left edge of Main UC (380 - 130 = 250, 260)
    draw.line([act_cx + 25 * scale, arm_y + 10 * scale, 250 * scale, 260 * scale], fill=(0, 0, 0, 255), width=1*scale)

    # 3. Draw white ellipses with black borders
    for cx, cy, rx, ry, text, is_bold in ellipses:
        draw.ellipse([cx - rx, cy - ry, cx + rx, cy + ry], fill=(255, 255, 255, 255), outline=(0, 0, 0, 255), width=1*scale)
        
        # Draw centered multiline text
        lines = text.split("\n")
        total_h = 0
        line_heights = []
        for line in lines:
            bbox = font_bold.getbbox(line) if is_bold else font_reg.getbbox(line)
            h = bbox[3] - bbox[1]
            line_heights.append(h)
            total_h += h
        
        curr_y = cy - total_h // 2
        for i, line in enumerate(lines):
            curr_font = font_bold if is_bold else font_reg
            bbox = curr_font.getbbox(line)
            w = bbox[2] - bbox[0]
            draw.text((cx - w // 2, curr_y), line, fill=(0, 0, 0, 255), font=curr_font)
            curr_y += line_heights[i] + 2 * scale

    # 4. Draw arrows (relationships)
    # Auxiliary functions to draw dashed lines and arrowheads
    def draw_dashed_line(x1, y1, x2, y2, dash_len=8*scale, gap_len=6*scale):
        dx, dy = x2 - x1, y2 - y1
        dist = (dx**2 + dy**2)**0.5
        if dist == 0: return
        ux, uy = dx / dist, dy / dist
        
        curr_dist = 0
        while curr_dist < dist:
            next_dist = min(curr_dist + dash_len, dist)
            draw.line([x1 + ux * curr_dist, y1 + uy * curr_dist, x1 + ux * next_dist, y1 + uy * next_dist], fill=(0, 0, 0, 255), width=1*scale)
            curr_dist += dash_len + gap_len

    def draw_arrowhead(target_x, target_y, from_x, from_y, size=10*scale):
        dx, dy = target_x - from_x, target_y - from_y
        dist = (dx**2 + dy**2)**0.5
        if dist == 0: return
        ux, uy = dx / dist, dy / dist
        
        # Perpendicular vector
        px, py = -uy, ux
        
        # Arrowhead wings
        wing1_x = target_x - ux * size + px * (size * 0.5)
        wing1_y = target_y - uy * size + py * (size * 0.5)
        wing2_x = target_x - ux * size - px * (size * 0.5)
        wing2_y = target_y - uy * size - py * (size * 0.5)
        
        draw.line([target_x, target_y, wing1_x, wing1_y], fill=(0, 0, 0, 255), width=1*scale)
        draw.line([target_x, target_y, wing2_x, wing2_y], fill=(0, 0, 0, 255), width=1*scale)

    def draw_relationship(from_cx, from_cy, from_rx, from_ry, to_cx, to_cy, to_rx, to_ry, label, is_include=True):
        # Calculate intersection points with ellipses roughly (using angle)
        import math
        angle = math.atan2(to_cy - from_cy, to_cx - from_cx)
        
        # Start point on edge of from_ellipse
        x_start = from_cx + from_rx * math.cos(angle)
        y_start = from_cy + from_ry * math.sin(angle)
        
        # End point on edge of to_ellipse
        x_end = to_cx - to_rx * math.cos(angle)
        y_end = to_cy - to_ry * math.sin(angle)
        
        # Draw line
        draw_dashed_line(x_start, y_start, x_end, y_end)
        
        # Draw arrowhead pointing to the target of the arrow (for include, it points to 'to', for extend it points to 'from' / 'to' depending on standard, wait:
        # In UML, include points to the included UC. Extend points from extending UC to base UC!
        # So for <<include>>, arrow goes from base (Main UC) -> target. Arrowhead is at to_ellipse.
        # For <<extend>>, arrow goes from extending UC -> base UC (Main UC). Arrowhead is at base.
        if is_include:
            draw_arrowhead(x_end, y_end, x_start, y_start)
        else:
            # extend: arrow points from extending (from) to base (to)
            draw_arrowhead(x_end, y_end, x_start, y_start) # target is base UC
            
        # Draw label in the middle of the line
        mid_x = (x_start + x_end) / 2
        mid_y = (x_start + x_end) / 2 # wait, typo! (y_start + y_end)/2
        mid_y = (y_start + y_end) / 2
        
        # Adjust label offset based on line orientation
        label_w = font_small.getbbox(label)[2] - font_small.getbbox(label)[0]
        label_h = font_small.getbbox(label)[3] - font_small.getbbox(label)[1]
        
        # Draw label slightly offset
        draw.text((mid_x - label_w // 2, mid_y - label_h - 4 * scale), label, fill=(0, 0, 0, 255), font=font_small)

    # Connection: Main UC -> Nâng Gene chính (Include)
    draw_relationship(380 * scale, 260 * scale, 130 * scale, 40 * scale, 
                     720 * scale, 110 * scale, 110 * scale, 28 * scale, 
                     "«include»", is_include=True)
                     
    # Connection: Main UC -> Nâng Gene phụ (Include)
    draw_relationship(380 * scale, 260 * scale, 130 * scale, 40 * scale, 
                     720 * scale, 210 * scale, 110 * scale, 28 * scale, 
                     "«include»", is_include=True)

    # Connection: Dung hợp Hybrid Gene -> Main UC (Extend)
    draw_relationship(720 * scale, 310 * scale, 110 * scale, 28 * scale,
                     380 * scale, 260 * scale, 130 * scale, 40 * scale, 
                     "«extend»", is_include=False)

    # Connection: Thức tỉnh Gene Tối Thượng -> Main UC (Extend)
    draw_relationship(720 * scale, 440 * scale, 110 * scale, 28 * scale,
                     380 * scale, 260 * scale, 130 * scale, 40 * scale, 
                     "«extend»", is_include=False)

    # Resize image to original target dimensions using LANCZOS for high quality
    final_img = img.resize((891, 557), resample=Image.Resampling.LANCZOS)
    
    # Save image
    output_path = "docs/extracted_images/image16.png"
    final_img.save(output_path, "PNG")
    print(f"Diagram drawn and saved successfully to {output_path}!")

if __name__ == "__main__":
    draw_diagram()
