import os
import math
from PIL import Image, ImageDraw, ImageFont, ImageFilter

def draw_sequence_diagram(output_path, title, lifeline_names, activations, messages, alt_box=None):
    scale = 4
    width, height = 877 * scale, 1007 * scale
    
    # Create image
    img = Image.new("RGBA", (width, height), (255, 255, 255, 255))
    
    # Shadow layer
    shadow_img = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    shadow_draw = ImageDraw.Draw(shadow_img)
    
    # Load fonts
    try:
        font_path = "C:\\Windows\\Fonts\\arial.ttf"
        font_bold_path = "C:\\Windows\\Fonts\\arialbd.ttf"
        font_reg = ImageFont.truetype(font_path, 11 * scale)
        font_bold = ImageFont.truetype(font_bold_path, 12 * scale)
        font_large = ImageFont.truetype(font_bold_path, 14 * scale)
    except IOError:
        font_reg = ImageFont.load_default()
        font_bold = ImageFont.load_default()
        font_large = ImageFont.load_default()

    # Calculate X coordinates for lifelines
    num_lifelines = len(lifeline_names)
    margin_left = 80 * scale
    margin_right = 80 * scale
    available_w = width - margin_left - margin_right
    
    if num_lifelines > 1:
        spacing = available_w / (num_lifelines - 1)
    else:
        spacing = 0
        
    lifeline_xs = [int(margin_left + i * spacing) for i in range(num_lifelines)]
    
    # 1. Draw Participant Boxes (UI/Server/etc)
    # Define boxes coordinates: (center_x, y, w, h, text)
    boxes = []
    actor_index = -1
    
    for i, name in enumerate(lifeline_names):
        if "Người chơi" in name or "Actor" in name or "Khách" in name:
            actor_index = i
            continue
        # Standard box width depends on text length
        text_w = font_bold.getbbox(name.replace("\n", " "))[2] - font_bold.getbbox(name.replace("\n", " "))[0]
        w = max(110 * scale, text_w + 20 * scale)
        boxes.append((lifeline_xs[i], 50 * scale, w, 40 * scale, name))

    # Shadows for standard boxes
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
    
    # Draw standard boxes
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
        for j, line in enumerate(lines):
            bbox = font_bold.getbbox(line)
            line_w = bbox[2] - bbox[0]
            draw.text((cx - line_w // 2, curr_y), line, fill=(0, 0, 0, 255), font=font_bold)
            curr_y += line_heights[j] + 2 * scale

    # Draw Actor stick figure if present
    if actor_index != -1:
        act_cx = lifeline_xs[actor_index]
        act_cy = 40 * scale
        head_r = 10 * scale
        draw.ellipse([act_cx - head_r, act_cy - head_r, act_cx + head_r, act_cy + head_r], outline=(0, 0, 0, 255), width=1*scale)
        draw.line([act_cx, act_cy + head_r, act_cx, act_cy + 30 * scale], fill=(0, 0, 0, 255), width=1*scale)
        draw.line([act_cx - 15 * scale, act_cy + 15 * scale, act_cx + 15 * scale, act_cy + 15 * scale], fill=(0, 0, 0, 255), width=1*scale)
        draw.line([act_cx, act_cy + 30 * scale, act_cx - 12 * scale, act_cy + 55 * scale], fill=(0, 0, 0, 255), width=1*scale)
        draw.line([act_cx, act_cy + 30 * scale, act_cx + 12 * scale, act_cy + 55 * scale], fill=(0, 0, 0, 255), width=1*scale)
        
        actor_label = lifeline_names[actor_index]
        actor_label_w = font_bold.getbbox(actor_label)[2] - font_bold.getbbox(actor_label)[0]
        draw.text((act_cx - actor_label_w // 2, act_cy + 60 * scale), actor_label, fill=(0, 0, 0, 255), font=font_bold)

    # 2. Draw vertical dashed lifelines
    def draw_dashed_line_vert(x, y_start, y_end, dash_len=8*scale, gap_len=6*scale):
        curr_y = y_start
        while curr_y < y_end:
            next_y = min(curr_y + dash_len, y_end)
            draw.line([x, curr_y, x, next_y], fill=(0, 0, 0, 255), width=1*scale)
            curr_y += dash_len + gap_len

    for xl in lifeline_xs:
        y_start = 110 * scale if xl == lifeline_xs[actor_index] else 90 * scale
        draw_dashed_line_vert(xl, y_start, 970 * scale)

    # 3. Draw Activation Bars
    # format of activations list: [(lifeline_index, y_start, y_end)]
    for idx, y1, y2 in activations:
        xl = lifeline_xs[idx]
        w_act = 6 * scale
        draw.rectangle([xl - w_act, y1, xl + w_act, y2], fill=(255, 255, 255, 255), outline=(0, 0, 0, 255), width=1*scale)

    # 4. Draw Alt Box if present
    # format of alt_box: (y_start, y_end, label, separator_y)
    if alt_box:
        ay1, ay2, alabel, sep_y = alt_box
        draw.rectangle([20 * scale, ay1, (877 - 20) * scale, ay2], outline=(0, 0, 0, 255), width=1*scale)
        
        # Draw small tag for alt label
        tag_w = font_bold.getbbox(alabel)[2] - font_bold.getbbox(alabel)[0] + 16 * scale
        draw.rectangle([20 * scale, ay1, 20 * scale + tag_w, ay1 + 22 * scale], fill=(235, 235, 235, 255), outline=(0, 0, 0, 255), width=1*scale)
        draw.text((20 * scale + 8 * scale, ay1 + 4 * scale), alabel, fill=(0, 0, 0, 255), font=font_bold)
        
        if sep_y:
            # Draw separator dotted line
            # Dotted line
            x_start = 20 * scale
            x_end = (877 - 20) * scale
            curr_x = x_start
            while curr_x < x_end:
                next_x = min(curr_x + 4*scale, x_end)
                draw.line([curr_x, sep_y, next_x, sep_y], fill=(0, 0, 0, 255), width=1*scale)
                curr_x += 8 * scale

    # Helper function to draw message arrows
    def draw_message(idx_from, idx_to, y, text, is_dashed=False, is_self=False, self_y2=0):
        x_from = lifeline_xs[idx_from]
        x_to = lifeline_xs[idx_to]
        
        if is_self:
            w_self = 25 * scale
            draw.line([x_from + 6*scale, y, x_from + w_self, y], fill=(0, 0, 0, 255), width=1*scale)
            draw.line([x_from + w_self, y, x_from + w_self, self_y2], fill=(0, 0, 0, 255), width=1*scale)
            draw.line([x_from + w_self, self_y2, x_from + 6*scale, self_y2], fill=(0, 0, 0, 255), width=1*scale)
            
            # Arrowhead pointing left back to lifeline
            draw.line([x_from + 6*scale, self_y2, x_from + 12*scale, self_y2 - 3*scale], fill=(0, 0, 0, 255), width=1*scale)
            draw.line([x_from + 6*scale, self_y2, x_from + 12*scale, self_y2 + 3*scale], fill=(0, 0, 0, 255), width=1*scale)
            
            draw.text((x_from + w_self + 4*scale, (y + self_y2)//2 - 6*scale), text, fill=(0, 0, 0, 255), font=font_reg)
        else:
            # Standard horizontal arrow
            # Line
            if is_dashed:
                # dashed horizontal line
                direction = 1 if x_to > x_from else -1
                dist = abs(x_to - x_from)
                curr_x = x_from
                dash_len = 6 * scale
                gap_len = 4 * scale
                while abs(curr_x - x_from) < dist:
                    next_x = curr_x + direction * dash_len
                    if abs(next_x - x_from) > dist:
                        next_x = x_to
                    draw.line([curr_x, y, next_x, y], fill=(0, 0, 0, 255), width=1*scale)
                    curr_x = next_x + direction * gap_len
            else:
                draw.line([x_from, y, x_to, y], fill=(0, 0, 0, 255), width=1*scale)
            
            # Arrowhead
            direction = 1 if x_to > x_from else -1
            arrow_size = 6 * scale
            offset = 6 * scale if direction == 1 else -6 * scale
            
            draw.line([x_to - offset, y, x_to - offset - direction * arrow_size, y - 4*scale], fill=(0, 0, 0, 255), width=1*scale)
            draw.line([x_to - offset, y, x_to - offset - direction * arrow_size, y + 4*scale], fill=(0, 0, 0, 255), width=1*scale)
            
            # Text label
            text_w = font_reg.getbbox(text)[2] - font_reg.getbbox(text)[0]
            mid_x = (x_from + x_to) / 2
            draw.text((mid_x - text_w // 2, y - 16 * scale), text, fill=(0, 0, 0, 255), font=font_reg)

    # 5. Draw messages
    # format: (idx_from, idx_to, y, text, is_dashed, is_self, self_y2)
    for idx_from, idx_to, y, text, is_dashed, is_self, self_y2 in messages:
        draw_message(idx_from, idx_to, y, text, is_dashed, is_self, self_y2)

    # Resize to target
    final_img = img.resize((877, 1007), resample=Image.Resampling.LANCZOS)
    
    # Save
    final_img.save(output_path, "PNG")
    print(f"Diagram saved to {output_path}!")


def draw_all_diagrams():
    os.makedirs("docs/extracted_images", exist_ok=True)
    
    # ----------------------------------------------------
    # 1. image26.png: Chiến đấu và sử dụng kỹ năng
    # ----------------------------------------------------
    lifeline_names = ["Người chơi", "Client (UI)", "Gameplay Server", "CombatResolver", "Đối tượng địch"]
    # Indices: 0: Player, 1: Client UI, 2: Gameplay Server, 3: CombatResolver, 4: Enemy
    activations = [
        (1, 160 * 4, 210 * 4),
        (2, 200 * 4, 330 * 4),
        (3, 310 * 4, 380 * 4),
        (4, 420 * 4, 480 * 4),
        (2, 530 * 4, 660 * 4), # for damage return
        (2, 720 * 4, 950 * 4), # dungeon / exp rewards
        (1, 880 * 4, 940 * 4),
    ]
    messages = [
        (0, 1, 160 * 4, "1. Sử dụng kỹ năng chủ động (Q/W/E/R)", False, False, 0),
        (1, 1, 180 * 4, "2. CheckLocalCooldown()", False, True, 200 * 4),
        (1, 2, 220 * 4, "3. ServerRpc: UseSkillRequest(skillId, targetPos)", False, False, 0),
        (2, 2, 240 * 4, "4. CheckCooldownAndMP()", False, True, 270 * 4),
        (2, 2, 280 * 4, "5. SpawnSkillHitbox()", False, True, 300 * 4),
        (2, 3, 320 * 4, "6. CalcPlayerAttackDamage(baseDmg, targetElement)", False, False, 0),
        (3, 3, 340 * 4, "7. GetCounteredElement() / CheckResists", False, True, 360 * 4),
        (3, 2, 380 * 4, "8. Return DamageResult (damage, isCritical)", True, False, 0),
        (2, 4, 420 * 4, "9. TakeDamageWithElement(damage, element)", False, False, 0),
        (4, 4, 440 * 4, "10. ApplyDamageResist & Deduct HP", False, True, 460 * 4),
        (4, 2, 480 * 4, "11. Notify HP changed or Target Dead", True, False, 0),
        
        # inside alt
        (2, 1, 560 * 4, "12. ClientRpc: SpawnSkillVisuals(pos)", False, False, 0),
        (2, 1, 620 * 4, "13. ClientRpc: NotifyDamagePopup(dmg, isCrit)", False, False, 0),
        
        # EXP and Loot
        (2, 2, 730 * 4, "14. DistributeEXPAndGold()", False, True, 760 * 4),
        (2, 2, 780 * 4, "15. SpawnDropLoot()", False, True, 810 * 4),
        (2, 2, 830 * 4, "16. UpdateQuestProgress()", False, True, 860 * 4),
        (2, 1, 890 * 4, "17. ClientRpc: NotifyQuestProgressUpdate()", False, False, 0),
        (1, 1, 910 * 4, "18. UpdateQuestTrackerUI()", False, True, 930 * 4),
    ]
    alt_box = (520 * 4, 690 * 4, "alt  [Đòn đánh trúng hitbox]", 600 * 4)
    draw_sequence_diagram("docs/extracted_images/image26.png", 
                          "Biểu đồ tuần tự Chiến đấu và sử dụng kỹ năng", 
                          lifeline_names, activations, messages, alt_box)

    # ----------------------------------------------------
    # 2. image27.png: Nâng cấp trang bị tại Blacksmith
    # ----------------------------------------------------
    lifeline_names = ["Người chơi", "Blacksmith UI", "Gameplay Server", "Backend Web API", "Database"]
    # Indices: 0: Player, 1: UI, 2: Server, 3: API, 4: DB
    activations = [
        (1, 160 * 4, 210 * 4),
        (2, 230 * 4, 290 * 4),
        (1, 330 * 4, 380 * 4),
        (2, 400 * 4, 910 * 4),
        (3, 440 * 4, 850 * 4),
        (4, 520 * 4, 570 * 4),
        (4, 710 * 4, 760 * 4),
        (1, 930 * 4, 970 * 4),
    ]
    messages = [
        (0, 1, 160 * 4, "1. Chọn trang bị và đá cường hóa trong UI", False, False, 0),
        (1, 1, 180 * 4, "2. Hiển thị chi phí vàng và tỷ lệ thành công", False, True, 200 * 4),
        (1, 2, 230 * 4, "3. ServerRpc: RequestUpgradeConfig(equipId)", False, False, 0),
        (2, 1, 280 * 4, "4. targeted ClientRpc: SyncUpgradeParams()", True, False, 0),
        (0, 1, 330 * 4, "5. Click nút 'Cường hóa'", False, False, 0),
        (1, 2, 400 * 4, "6. ServerRpc: UpgradeEquipment(equipId, stoneId)", False, False, 0),
        (2, 3, 440 * 4, "7. POST /api/upgrade/equipment (JWT, equipId, stoneId)", False, False, 0),
        (3, 4, 520 * 4, "8. Query player_data & item_inventory", False, False, 0),
        (4, 3, 570 * 4, "9. Return Inventory & Equip Info", True, False, 0),
        (3, 3, 600 * 4, "10. ValidateGoldAndStones()", False, True, 630 * 4),
        (3, 3, 640 * 4, "11. RollSuccessRate()", False, True, 670 * 4),
        
        # update DB
        (3, 4, 710 * 4, "12. Save upgraded stats, deduct Gold/Stones", False, False, 0),
        (4, 3, 760 * 4, "13. DB Save Success", True, False, 0),
        
        # return response
        (3, 2, 850 * 4, "14. Response 200 OK (new_level, success=true)", True, False, 0),
        (2, 2, 880 * 4, "15. Sync Inventory & Equipment stats", False, True, 900 * 4),
        (2, 1, 920 * 4, "16. targeted ClientRpc: UpgradeResult(success=true, stats)", True, False, 0),
        (1, 1, 940 * 4, "17. Play upgrade success animation & update UI", False, True, 960 * 4),
    ]
    alt_box = (620 * 4, 870 * 4, "alt  [Cường hóa thành công]", 790 * 4)
    draw_sequence_diagram("docs/extracted_images/image27.png",
                          "Biểu đồ tuần tự Nâng cấp trang bị tại Blacksmith",
                          lifeline_names, activations, messages, alt_box)

    # ----------------------------------------------------
    # 3. image28.png: Nâng Gene chính và Gene phụ
    # ----------------------------------------------------
    lifeline_names = ["Người chơi", "Gene UI", "Gameplay Server", "Backend Web API", "Database"]
    activations = [
        (1, 160 * 4, 210 * 4),
        (2, 230 * 4, 880 * 4),
        (3, 270 * 4, 820 * 4),
        (4, 330 * 4, 380 * 4),
        (4, 520 * 4, 570 * 4),
        (4, 680 * 4, 730 * 4),
        (1, 900 * 4, 960 * 4),
    ]
    messages = [
        (0, 1, 160 * 4, "1. Chọn Gene chính/Gene phụ muốn nâng cấp", False, False, 0),
        (1, 1, 180 * 4, "2. Xem yêu cầu (Mảnh/Lõi/Tinh hoa Gene, Bạc)", False, True, 200 * 4),
        (1, 2, 230 * 4, "3. ServerRpc: UpgradeGeneRequest(geneType)", False, False, 0),
        (2, 3, 270 * 4, "4. POST /api/gene/upgrade (JWT, geneType)", False, False, 0),
        (3, 4, 330 * 4, "5. Query player_gene_data & inventory", False, False, 0),
        (4, 3, 380 * 4, "6. Return current Tier and materials", True, False, 0),
        (3, 3, 410 * 4, "7. ValidateMaterialsAndSilver()", False, True, 440 * 4),
        (3, 3, 450 * 4, "8. RollSuccessRate()", False, True, 480 * 4),
        
        # DB save
        (3, 4, 520 * 4, "9. Save new Tier, deduct resources & add stats", False, False, 0),
        (4, 3, 570 * 4, "10. DB Update Success", True, False, 0),
        
        # update stats
        (3, 3, 600 * 4, "11. RecalculatePlayerStats()", False, True, 630 * 4),
        (3, 4, 680 * 4, "12. Save final stats to player profile", False, False, 0),
        (4, 3, 730 * 4, "13. DB Save Success", True, False, 0),
        
        (3, 2, 810 * 4, "14. Response 200 OK (new_tier, final_stats)", True, False, 0),
        (2, 2, 840 * 4, "15. Sync runtime character variables", False, True, 870 * 4),
        (2, 1, 890 * 4, "16. targeted ClientRpc: SyncGeneUpgradeResult(new_tier)", True, False, 0),
        (1, 1, 910 * 4, "17. Spawn Elemental Aura visual effect & update stats", False, True, 940 * 4),
    ]
    alt_box = (490 * 4, 790 * 4, "alt  [Nâng cấp thành công]", 750 * 4)
    draw_sequence_diagram("docs/extracted_images/image28.png",
                          "Biểu đồ tuần tự Nâng Gene chính và Gene phụ",
                          lifeline_names, activations, messages, alt_box)

    # ----------------------------------------------------
    # 4. image29.png: Dung hợp Hybrid Gene
    # ----------------------------------------------------
    lifeline_names = ["Người chơi", "Hybrid UI", "Gameplay Server", "Backend Web API", "Database"]
    activations = [
        (1, 160 * 4, 210 * 4),
        (2, 230 * 4, 910 * 4),
        (3, 270 * 4, 850 * 4),
        (4, 330 * 4, 380 * 4),
        (4, 520 * 4, 570 * 4),
        (4, 710 * 4, 760 * 4),
        (1, 930 * 4, 970 * 4),
    ]
    messages = [
        (0, 1, 160 * 4, "1. Chọn tab Hybrid Fusion và xem điều kiện", False, False, 0),
        (1, 1, 180 * 4, "2. Kiểm tra hai Gene chính/phụ đạt Tier 5", False, True, 200 * 4),
        (1, 2, 230 * 4, "3. ServerRpc: RequestHybridFusion()", False, False, 0),
        (2, 3, 270 * 4, "4. POST /api/gene/hybrid-fusion (JWT)", False, False, 0),
        (3, 4, 330 * 4, "5. Query PartnerMap & player_data", False, False, 0),
        (4, 3, 380 * 4, "6. Return Element Pair compatibility & Fusion Core quantity", True, False, 0),
        (3, 3, 410 * 4, "7. ValidateElementPair() (Hỏa-Thổ, Thủy-Mộc, Kim-Phong)", False, True, 440 * 4),
        (3, 3, 450 * 4, "8. Consume Fusion Core & Gold", False, True, 480 * 4),
        
        # update DB
        (3, 4, 520 * 4, "9. Set is_hybrid=true, HybridPrefabPath, ImmuneElements", False, False, 0),
        (4, 3, 570 * 4, "10. DB Save Success", True, False, 0),
        
        # stat and skill update
        (3, 3, 600 * 4, "11. Add +20% ATK/DEF & Map Hybrid Skills", False, True, 630 * 4),
        (3, 4, 710 * 4, "12. Save final hybrid stats and skill bindings", False, False, 0),
        (4, 3, 760 * 4, "13. DB Save Success", True, False, 0),
        
        (3, 2, 840 * 4, "14. Response 200 OK (hybridId, prefabPath, skills)", True, False, 0),
        (2, 2, 870 * 4, "15. Instantiate new Hybrid model prefab on zone", False, True, 890 * 4),
        (2, 1, 920 * 4, "16. targeted ClientRpc: SyncHybridFusionResult()", True, False, 0),
        (1, 1, 940 * 4, "17. Play fusion success animation, bind new active skills", False, True, 960 * 4),
    ]
    alt_box = (490 * 4, 820 * 4, "alt  [Dung hợp thành công]", 780 * 4)
    draw_sequence_diagram("docs/extracted_images/image29.png",
                          "Biểu đồ tuần tự Dung hợp Hybrid Gene",
                          lifeline_names, activations, messages, alt_box)

    # ----------------------------------------------------
    # 5. image30.png: Tham gia và hoàn tất phó bản
    # ----------------------------------------------------
    lifeline_names = ["Người chơi", "Portal / UI", "Gameplay Server", "Backend Web API", "Database"]
    activations = [
        (1, 160 * 4, 210 * 4),
        (2, 230 * 4, 500 * 4),
        (3, 270 * 4, 440 * 4),
        (4, 320 * 4, 370 * 4),
        (4, 390 * 4, 420 * 4),
        (2, 530 * 4, 650 * 4), # fighting waves
        (2, 690 * 4, 910 * 4), # completion
        (3, 730 * 4, 860 * 4),
        (4, 770 * 4, 820 * 4),
        (1, 930 * 4, 970 * 4),
    ]
    messages = [
        (0, 1, 160 * 4, "1. Tương tác với Portal Dungeon", False, False, 0),
        (1, 1, 180 * 4, "2. Chọn mức độ phó bản và xác nhận vào", False, True, 200 * 4),
        (1, 2, 230 * 4, "3. ServerRpc: RequestEnterDungeon(dungeonId)", False, False, 0),
        (2, 3, 270 * 4, "4. POST /api/dungeon/enter (JWT, dungeonId)", False, False, 0),
        (3, 4, 320 * 4, "5. Validate ticket limits and player Level", False, False, 0),
        (4, 3, 370 * 4, "6. Limits OK", True, False, 0),
        (3, 4, 390 * 4, "7. Deduct ticket / record dungeon entry", False, False, 0),
        (4, 3, 420 * 4, "8. DB Save Success", True, False, 0),
        (3, 2, 450 * 4, "9. Response 200 OK (entryGranted=true)", True, False, 0),
        (2, 2, 470 * 4, "10. LoadScene() & Spawn Wave 1 Enemies", False, True, 490 * 4),
        
        # fight
        (0, 2, 540 * 4, "11. Tiêu diệt quái vật theo các Wave đấu", False, False, 0),
        (2, 2, 570 * 4, "12. Spawn Next Wave / Spawn Dungeon Boss", False, True, 600 * 4),
        (0, 2, 630 * 4, "13. Tiêu diệt Dungeon Boss cuối cùng", False, False, 0),
        
        # completion
        (2, 2, 700 * 4, "14. SetDungeonCompleteState()", False, True, 720 * 4),
        (2, 3, 740 * 4, "15. POST /api/dungeon/complete (JWT, dungeonId, duration)", False, False, 0),
        (3, 4, 780 * 4, "16. Write rewards (Gold, Fusion Core, Items) to DB", False, False, 0),
        (4, 3, 810 * 4, "17. DB Save Success", True, False, 0),
        (3, 2, 850 * 4, "18. Response 200 OK (rewardsList)", True, False, 0),
        (2, 2, 870 * 4, "19. Spawn loot drops in dungeon room", False, True, 890 * 4),
        (2, 1, 920 * 4, "20. ClientRpc: NotifyDungeonComplete(rewards)", True, False, 0),
        (1, 1, 940 * 4, "21. Show victory interface with rewards list", False, True, 960 * 4),
    ]
    alt_box = (300 * 4, 460 * 4, "alt  [Đủ điều kiện vào phó bản]", 380 * 4)
    draw_sequence_diagram("docs/extracted_images/image30.png",
                          "Biểu đồ tuần tự Tham gia và hoàn tất phó bản",
                          lifeline_names, activations, messages, alt_box)

    print("All core sequence diagrams successfully drawn!")

if __name__ == "__main__":
    draw_all_diagrams()
