import pymysql
import json
import sys

def update_character(char_name):
    connection = pymysql.connect(
        host='127.0.0.1',
        user='root',
        password='',
        database='gamedb',
        port=3306
    )

    try:
        with connection.cursor() as cursor:
            # 1. Fetch character data
            cursor.execute("SELECT player_id, info_char, inventory FROM player_data WHERE character_name = %s", (char_name,))
            row = cursor.fetchone()
            if not row:
                print(f"Error: Không tìm thấy nhân vật có tên '{char_name}'")
                return
            
            player_id, info_char_raw, inventory_raw = row
            
            # 2. Parse JSONs
            try:
                info_char = json.loads(info_char_raw)
            except Exception:
                info_char = {}
                
            try:
                inventory = json.loads(inventory_raw)
            except Exception:
                inventory = []
                
            # 3. Add items to inventory
            # We want to add:
            # 17: Linh Thach So Cap
            # 18: Linh Thach Trung Cap
            # 19: Linh Thach Cao Cap
            # 20: Linh Thach Thuong Cap
            # 31: Loi Dot Bien (chung)
            # 47-52: Loi Dot Bien theo he
            target_items = [17, 18, 19, 20, 31, 47, 48, 49, 50, 51, 52]
            
            # Find existing item templates in inventory to update their quantity
            inventory_map = {item['itemTemplateId']: item for item in inventory if 'itemTemplateId' in item}
            
            # Calculate next slot index
            existing_slots = {item.get('slotIndex') for item in inventory if 'slotIndex' in item}
            next_slot = 0
            
            for item_id in target_items:
                if item_id in inventory_map:
                    inventory_map[item_id]['quantity'] = 99
                else:
                    while next_slot in existing_slots:
                        next_slot += 1
                    new_item = {
                        "slotIndex": next_slot,
                        "itemTemplateId": item_id,
                        "quantity": 99,
                        "upgradeLevel": 0,
                        "strOptions": ""
                    }
                    inventory.append(new_item)
                    existing_slots.add(next_slot)
            
            # 4. Set silver and gene exp in info_char
            info_char['silver'] = max(info_char.get('silver', 0), 10000000) # Give 10M silver
            info_char['gene_exp'] = max(info_char.get('gene_exp', 0), 500000) # Give 500k gene exp
            if info_char.get('secondary_element') is not None:
                info_char['secondary_gene_exp'] = max(info_char.get('secondary_gene_exp', 0) or 0, 500000)
                
            # 5. Update database
            updated_info_char_raw = json.dumps(info_char, ensure_ascii=False)
            updated_inventory_raw = json.dumps(inventory, ensure_ascii=False)
            
            cursor.execute(
                "UPDATE player_data SET info_char = %s, inventory = %s WHERE player_id = %s",
                (updated_info_char_raw, updated_inventory_raw, player_id)
            )
            connection.commit()
            print(f"Thành công! Đã thêm vật phẩm tiến hóa/dung hợp, 10 triệu bạc và 500k EXP gene vào nhân vật '{char_name}' (ID: {player_id}).")
    finally:
        connection.close()

if __name__ == '__main__':
    if len(sys.argv) < 2:
        print("Sử dụng: python update_inventory.py <character_name>")
    else:
        update_character(sys.argv[1])
