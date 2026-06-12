import pymysql
import json

connection = pymysql.connect(
    host='127.0.0.1',
    user='root',
    password='',
    database='gamedb',
    port=3306
)

try:
    with connection.cursor() as cursor:
        cursor.execute("SELECT player_id, character_name, info_char, inventory, updated_at FROM player_data")
        rows = cursor.fetchall()
        players = []
        for row in rows:
            player_id, char_name, info_char_raw, inventory_raw, updated_at = row
            try:
                info_char = json.loads(info_char_raw)
            except:
                info_char = info_char_raw
            
            try:
                inventory = json.loads(inventory_raw)
            except:
                inventory = []
            
            players.append({
                "player_id": player_id,
                "character_name": char_name,
                "info_char": info_char,
                "inventory": inventory,
                "updated_at": str(updated_at)
            })
            
        with open("Scripts/player_info.json", "w", encoding="utf-8") as f:
            json.dump(players, f, indent=2, ensure_ascii=False)
            
        print("Success! Written to Scripts/player_info.json")
finally:
    connection.close()
