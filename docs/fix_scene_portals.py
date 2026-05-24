"""
Fix map_config.scene_name and map_portal.dest_scene_name to match actual Unity scene files.

Scene assignment:
  Map00 = map_id 99   (Cửa phía tây)
  Map01 = map_id 100  (Cửa phía đông)   [already set in Unity]
  Map02 = map_id 101  (Chiến trường phó bản) [already set in Unity, side area]
  Map03 = map_id 102  (Làng Mưa)         [already set in Unity]
  Map04 = map_id 103  (Pháo Đài Amega)
  Map05 = map_id 104  (Vùng trống Kusa)
  Map06 = map_id 105  (Lãnh địa thiên thần)
  Map07 = map_id 106  (Căn cứ Akatsuki)
  Map08 = map_id 98   (Chiến trường)
  Map09 = map_id 83   (Thung lũng Tấn Công)
  Map10 = map_id 57   (Thánh Địa Thất Kiếm)
  Map11 = map_id 56   (Đồi trung tâm)
  Map12 = map_id 88   (Cầu Kannabi)
  Map13 = map_id 87   (Hang Khô)

World chain (right→):
  GameScene(0) ↔ Map13(87) ↔ Map12(88) ↔ Map11(56) ↔ Map10(57) ↔ Map09(83)
  ↔ Map08(98) ↔ Map00(99) ↔ Map01(100) ↔ Map03(102) ↔ Map04(103)
  ↔ Map05(104) ↔ Map06(105) ↔ Map07(106)
"""
import mysql.connector

conn = mysql.connector.connect(
    host='127.0.0.1',
    user='root',
    password='',
    database='gamedb',
    charset='utf8mb4',
    collation='utf8mb4_unicode_ci'
)
cursor = conn.cursor()

# ── 1. Update map_config.scene_name ──────────────────────────────────────────
map_scene_updates = [
    (99,  'Map00'),
    (100, 'Map01'),
    (101, 'Map02'),
    (102, 'Map03'),
    (103, 'Map04'),
    (104, 'Map05'),
    (105, 'Map06'),
    (106, 'Map07'),
    (98,  'Map08'),
    (83,  'Map09'),
    (57,  'Map10'),
    (56,  'Map11'),
    (88,  'Map12'),
    (87,  'Map13'),
]

for map_id, scene_name in map_scene_updates:
    cursor.execute(
        "UPDATE map_config SET scene_name=%s WHERE map_id=%s",
        (scene_name, map_id)
    )
    print(f"  map_config: map_id={map_id} → scene_name='{scene_name}'")

# ── 2. Update map_portal entries ──────────────────────────────────────────────
# (portal_id, new_dest_map_id_or_None, new_dest_scene_name)
portal_updates = [
    # Portal from GameScene → first world map (was 75, now 87=Map13)
    (1,  87, 'Map13'),    # GameScene →right→ Map13(87)
    # Portal from first world map back → GameScene (was 75→GameScene, repurpose 52)
    (52,  0, 'GameScene'), # Map13(87) →left→ GameScene

    # Portals 53–76: fix dest_scene_name only (dest_map_id stays correct)
    (53, None, 'Map12'),  # 87→right→88   Map12
    (54, None, 'Map13'),  # 88→left→87    Map13
    (55, None, 'Map11'),  # 88→right→56   Map11
    (56, None, 'Map12'),  # 56→left→88    Map12
    (57, None, 'Map10'),  # 56→right→57   Map10
    (58, None, 'Map11'),  # 57→left→56    Map11
    (59, None, 'Map09'),  # 57→right→83   Map09
    (60, None, 'Map10'),  # 83→left→57    Map10
    (61, None, 'Map08'),  # 83→right→98   Map08
    (62, None, 'Map09'),  # 98→left→83    Map09
    (63, None, 'Map00'),  # 98→right→99   Map00
    (64, None, 'Map08'),  # 99→left→98    Map08
    (65, None, 'Map01'),  # 99→right→100  Map01
    (66, None, 'Map00'),  # 100→left→99   Map00
    (67, None, 'Map03'),  # 100→right→102 Map03
    (68, None, 'Map01'),  # 102→left→100  Map01
    (69, None, 'Map04'),  # 102→right→103 Map04
    (70, None, 'Map03'),  # 103→left→102  Map03
    (71, None, 'Map05'),  # 103→right→104 Map05
    (72, None, 'Map04'),  # 104→left→103  Map04
    (73, None, 'Map06'),  # 104→right→105 Map06
    (74, None, 'Map05'),  # 105→left→104  Map05
    (75, None, 'Map07'),  # 105→right→106 Map07
    (76, None, 'Map06'),  # 106→left→105  Map06
]

for portal_id, dest_map_id, dest_scene in portal_updates:
    if dest_map_id is not None:
        cursor.execute(
            "UPDATE map_portal SET dest_map_id=%s, dest_scene_name=%s WHERE portal_id=%s",
            (dest_map_id, dest_scene, portal_id)
        )
        print(f"  portal {portal_id}: dest_map_id={dest_map_id}, dest_scene='{dest_scene}'")
    else:
        cursor.execute(
            "UPDATE map_portal SET dest_scene_name=%s WHERE portal_id=%s",
            (dest_scene, portal_id)
        )
        print(f"  portal {portal_id}: dest_scene='{dest_scene}'")

conn.commit()
print("\nAll DB changes committed.")
cursor.close()
conn.close()
