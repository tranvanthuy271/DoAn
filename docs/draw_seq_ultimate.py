import os
from PIL import Image, ImageDraw, ImageFont, ImageFilter

def draw_sequence():
    scale = 4
    width, height = 877 * scale, 1007 * scale
    
    # Create image
    img = Image.new("RGBA", (width, height), (255, 255, 255, 255))
    
    # Create a separate layer for shadows to apply blur
    shadow_img = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    shadow_draw = ImageDraw.Draw(shadow_img)
    
    # Load fonts
    try:
        font_path = "C:\\Windows\\Fonts\\arial.ttf"
        font_bold_path = "C:\\Windows\\Fonts\\arialbd.ttf"
        font_reg = ImageFont.truetype(font_path, 11 * scale)
        font_bold = ImageFont.truetype(font_bold_path, 12 * scale)
        font_large = ImageFont.truetype(font_bold_path, 13 * scale)
    except IOError:
        font_reg = ImageFont.load_default()
        font_bold = ImageFont.load_default()
        font_large = ImageFont.load_default()

    # X Coordinates for Lifelines in original space * scale
    x_player = 80 * scale
    x_client = 260 * scale
    x_server = 460 * scale
    x_backend = 660 * scale
    x_db = 810 * scale
    
    lifelines = [x_player, x_client, x_server, x_backend, x_db]
    
    # Participant boxes config (x, y, w, h, text)
    # Note: Actor (Player) is drawn differently as stick figure.
    boxes = [
        (x_client, 50 * scale, 120 * scale, 40 * scale, "Client (UI)\nStatsTabUI"),
        (x_server, 50 * scale, 140 * scale, 40 * scale, "Gameplay Server\nZoneServer"),
        (x_backend, 50 * scale, 140 * scale, 40 * scale, "Backend Web API\nGeneController"),
        (x_db, 50 * scale, 130 * scale, 40 * scale, "Database\nMySQL (EF Core)"),
    ]

    # Draw shadows for boxes
    shadow_offset_x = 2 * scale
    shadow_offset_y = 3 * scale
    for cx, cy, w, h, text in boxes:
        shadow_draw.rectangle(
            [cx - w//2 + shadow_offset_x, cy + shadow_offset_y, cx + w//2 + shadow_offset_x, cy + h + shadow_offset_y],
            fill=(210, 210, 210, 150)
        )
        
    shadow_blurred = shadow_img.filter(ImageFilter.GaussianBlur(radius=2 * scale))
    img.alpha_composite(shadow_blurred)
    
    draw = ImageDraw.Draw(img)
    
    # Draw Participant Boxes (white fill, black border)
    for cx, cy, w, h, text in boxes:
        draw.rectangle([cx - w//2, cy, cx + w//2, cy + h], fill=(245, 245, 245, 255), outline=(0, 0, 0, 255), width=1*scale)
        lines = text.split("\n")
        total_h = 0
        line_heights = []
        for line in lines:
            bbox = font_bold.getbbox(line)
            h_line = bbox[3] - bbox[1]
            line_heights.append(h_line)
            total_h += h_line
        
        curr_y = cy + (h - total_h) // 2
        for i, line in enumerate(lines):
            bbox = font_bold.getbbox(line)
            line_w = bbox[2] - bbox[0]
            draw.text((cx - line_w // 2, curr_y), line, fill=(0, 0, 0, 255), font=font_bold)
            curr_y += line_heights[i] + 2 * scale

    # Draw Actor stick figure
    act_cx, act_cy = x_player, 40 * scale
    head_r = 10 * scale
    draw.ellipse([act_cx - head_r, act_cy - head_r, act_cx + head_r, act_cy + head_r], outline=(0, 0, 0, 255), width=1*scale)
    draw.line([act_cx, act_cy + head_r, act_cx, act_cy + 30 * scale], fill=(0, 0, 0, 255), width=1*scale)
    draw.line([act_cx - 15 * scale, act_cy + 15 * scale, act_cx + 15 * scale, act_cy + 15 * scale], fill=(0, 0, 0, 255), width=1*scale)
    draw.line([act_cx, act_cy + 30 * scale, act_cx - 12 * scale, act_cy + 55 * scale], fill=(0, 0, 0, 255), width=1*scale)
    draw.line([act_cx, act_cy + 30 * scale, act_cx + 12 * scale, act_cy + 55 * scale], fill=(0, 0, 0, 255), width=1*scale)
    
    actor_label = "Người chơi"
    actor_label_w = font_bold.getbbox(actor_label)[2] - font_bold.getbbox(actor_label)[0]
    draw.text((act_cx - actor_label_w // 2, act_cy + 60 * scale), actor_label, fill=(0, 0, 0, 255), font=font_bold)

    # Draw vertical dashed lifelines
    def draw_dashed_line(x, y_start, y_end, dash_len=8*scale, gap_len=6*scale):
        curr_y = y_start
        while curr_y < y_end:
            next_y = min(curr_y + dash_len, y_end)
            draw.line([x, curr_y, x, next_y], fill=(0, 0, 0, 255), width=1*scale)
            curr_y += dash_len + gap_len

    for xl in lifelines:
        y_start = 110 * scale if xl == x_player else 90 * scale
        draw_dashed_line(xl, y_start, 970 * scale)

    # Draw activation bars (tall narrow white rectangles on lifelines)
    # Activation format: (x, y_start, y_end)
    activations = [
        (x_client, 160 * scale, 210 * scale),
        (x_server, 200 * scale, 320 * scale),
        (x_backend, 310 * scale, 410 * scale),
        (x_db, 350 * scale, 400 * scale),
        
        # inside alt
        (x_backend, 470 * scale, 700 * scale),
        (x_db, 600 * scale, 650 * scale),
        (x_server, 680 * scale, 810 * scale),
        (x_client, 790 * scale, 940 * scale),
    ]
    for xl, y1, y2 in activations:
        w_act = 6 * scale
        draw.rectangle([xl - w_act, y1, xl + w_act, y2], fill=(255, 255, 255, 255), outline=(0, 0, 0, 255), width=1*scale)

    # Helper function to draw message arrows
    def draw_message(x_from, x_to, y, text, is_dashed=False, is_self=False, self_y2=0):
        if is_self:
            # Draw self call arrow
            x = x_from
            w_self = 25 * scale
            draw.line([x + 6*scale, y, x + w_self, y], fill=(0, 0, 0, 255), width=1*scale)
            draw.line([x + w_self, y, x + w_self, self_y2], fill=(0, 0, 0, 255), width=1*scale)
            draw.line([x + w_self, self_y2, x + 6*scale, self_y2], fill=(0, 0, 0, 255), width=1*scale)
            
            # Arrowhead pointing left back to lifeline
            draw.line([x + 6*scale, self_y2, x + 12*scale, self_y2 - 3*scale], fill=(0, 0, 0, 255), width=1*scale)
            draw.line([x + 6*scale, self_y2, x + 12*scale, self_y2 + 3*scale], fill=(0, 0, 0, 255), width=1*scale)
            
            # Text label
            draw.text((x + w_self + 4*scale, (y + self_y2)//2 - 6*scale), text, fill=(0, 0, 0, 255), font=font_reg)
        else:
            # Draw standard horizontal arrow
            # Line
            if is_dashed:
                draw_dashed_line_horiz(x_from, x_to, y)
            else:
                draw.line([x_from, y, x_to, y], fill=(0, 0, 0, 255), width=1*scale)
            
            # Arrowhead pointing to x_to
            direction = 1 if x_to > x_from else -1
            arrow_size = 6 * scale
            offset = 6 * scale if direction == 1 else -6 * scale
            
            draw.line([x_to - offset, y, x_to - offset - direction * arrow_size, y - 4*scale], fill=(0, 0, 0, 255), width=1*scale)
            draw.line([x_to - offset, y, x_to - offset - direction * arrow_size, y + 4*scale], fill=(0, 0, 0, 255), width=1*scale)
            
            # Text label
            text_w = font_reg.getbbox(text)[2] - font_reg.getbbox(text)[0]
            mid_x = (x_from + x_to) / 2
            draw.text((mid_x - text_w // 2, y - 16 * scale), text, fill=(0, 0, 0, 255), font=font_reg)

    def draw_dashed_line_horiz(x1, x2, y, dash_len=6*scale, gap_len=4*scale):
        direction = 1 if x2 > x1 else -1
        dist = abs(x2 - x1)
        curr_x = x1
        while abs(curr_x - x1) < dist:
            next_x = curr_x + direction * dash_len
            if abs(next_x - x1) > dist:
                next_x = x2
            draw.line([curr_x, y, next_x, y], fill=(0, 0, 0, 255), width=1*scale)
            curr_x = next_x + direction * gap_len

    # Message sequence
    # 1. Player to UI
    draw_message(x_player, x_client - 6*scale, 160 * scale, "1. Tiêu diệt quái / Sử dụng GeneExpAdd")
    
    # 2. UI to Server Rpc
    draw_message(x_client + 6*scale, x_server - 6*scale, 200 * scale, "2. ServerRpc: SendAction(actionId)")
    
    # 3. Server self check
    draw_message(x_server, x_server, 240 * scale, "3. ValidateAction()", is_self=True, self_y2=270 * scale)
    
    # 4. Server to Backend API
    draw_message(x_server + 6*scale, x_backend - 6*scale, 310 * scale, "4. POST /api/gene/add-exp (JWT, expAmount)")
    
    # 5. Backend to DB update
    draw_message(x_backend + 6*scale, x_db - 6*scale, 350 * scale, "5. Update ultimate_gene_exp")
    
    # 6. DB to Backend success
    draw_message(x_db - 6*scale, x_backend + 6*scale, 390 * scale, "6. DB Update Success", is_dashed=True)

    # ALT Box for Ultimate Gene check
    alt_x1, alt_y1 = 20 * scale, 420 * scale
    alt_x2, alt_y2 = 857 * scale, 770 * scale
    draw.rectangle([alt_x1, alt_y1, alt_x2, alt_y2], outline=(0, 0, 0, 255), width=1*scale)
    
    # Alt Label
    draw.rectangle([alt_x1, alt_y1, alt_x1 + 160 * scale, alt_y1 + 22 * scale], fill=(235, 235, 235, 255), outline=(0, 0, 0, 255), width=1*scale)
    draw.text((alt_x1 + 6 * scale, alt_y1 + 4 * scale), "alt  [EXP >= 1.000.000]", fill=(0, 0, 0, 255), font=font_bold)
    
    # Inside Alt Box
    # 7. Backend sets is_ultimate = true
    draw_message(x_backend, x_backend, 470 * scale, "7. Set is_ultimate = true", is_self=True, self_y2=500 * scale)
    
    # 8. Backend multiplies stats x1.5
    draw_message(x_backend, x_backend, 530 * scale, "8. Multiply base_stats by 1.5", is_self=True, self_y2=560 * scale)
    
    # 9. Backend saves to DB
    draw_message(x_backend + 6*scale, x_db - 6*scale, 600 * scale, "9. Save updated player stats & state")
    
    # 10. DB returns success
    draw_message(x_db - 6*scale, x_backend + 6*scale, 640 * scale, "10. Save Success", is_dashed=True)
    
    # 11. Backend returns success response to Gameplay Server
    draw_message(x_backend - 6*scale, x_server + 6*scale, 680 * scale, "11. Response (is_ultimate=true, stats)", is_dashed=True)
    
    # 12. Server updates NetworkVariable
    draw_message(x_server, x_server, 710 * scale, "12. Update NetworkVariable", is_self=True, self_y2=740 * scale)
    
    # Separator line inside Alt box (dotted horizontal)
    draw_dashed_line_horiz(alt_x1, alt_x2, 760 * scale, dash_len=4*scale, gap_len=4*scale)
    
    # 13. Server to ClientRpc (outside/inside alt logic)
    draw_message(x_server - 6*scale, x_client + 6*scale, 790 * scale, "13. ClientRpc: NotifyUltimateAwakened(new_stats)")
    
    # 14. Client UI updates HUD
    draw_message(x_client, x_client, 830 * scale, "14. Display symbol ✦ on HUD", is_self=True, self_y2=860 * scale)
    
    # 15. Client UI spawns Aura
    draw_message(x_client, x_client, 890 * scale, "15. Spawn UltimateAura (aura1/2/3)", is_self=True, self_y2=920 * scale)

    # Resize image to original target dimensions using LANCZOS for high quality
    final_img = img.resize((877, 1007), resample=Image.Resampling.LANCZOS)
    
    # Save image
    output_dir = "docs/extracted_images"
    os.makedirs(output_dir, exist_ok=True)
    output_path = os.path.join(output_dir, "image_ultimate_gene_sequence.png")
    final_img.save(output_path, "PNG")
    print(f"Sequence diagram drawn and saved successfully to {output_path}!")

if __name__ == "__main__":
    draw_sequence()
