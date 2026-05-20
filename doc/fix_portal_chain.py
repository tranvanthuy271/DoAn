"""
Rebuild portal chain for sequential scene order:
  GameScene(0) → Map00(99) → Map01(100) → Map02(101) → Map03(102) → Map04(103)
  → Map05(104) → Map06(105) → Map07(106) → Map08(98) → Map09(83) → Map10(57)
  → Map11(56) → Map12(88) → Map13(87)

Each UPDATE repurposes or fixes existing portal rows so no INSERT needed.
"""
import mysql.connector

conn = mysql.connector.connect(
    host='127.0.0.1', user='root', password='',
    database='gamedb', charset='utf8mb4',
    collation='utf8mb4_unicode_ci'
)
cur = conn.cursor()

# (portal_id, new_source_map_id, new_direction, new_dest_map_id, new_dest_scene)
updates = [
    # ── GameScene ↔ Map00(99) ────────────────────────────────────────────────
    (1,  0,   'right', 99,  'Map00'),  # GameScene →R→ Map00
    (64, 99,  'left',  0,   'GameScene'), # Map00 →L→ GameScene

    # ── Map00(99) ↔ Map01(100) ──────────────────────────────────────────────
    # portals 65/66 already correct — no change

    # ── Map01(100) ↔ Map02(101) ─────────────────────────────────────────────
    (67, 100, 'right', 101, 'Map02'),  # Map01 →R→ Map02  (was 100→102)
    # repurpose orphaned portal 3 as Map02→L→Map01
    (3,  101, 'left',  100, 'Map01'),

    # ── Map02(101) ↔ Map03(102) ─────────────────────────────────────────────
    # repurpose orphaned portal 4 as Map02→R→Map03
    (4,  101, 'right', 102, 'Map03'),
    (68, 102, 'left',  101, 'Map02'),  # Map03 →L→ Map02  (was 102→100)

    # ── Map03(102) ↔ Map04(103) — portals 69/70 already correct ─────────────
    # ── Map04(103) ↔ Map05(104) — portals 71/72 already correct ─────────────
    # ── Map05(104) ↔ Map06(105) — portals 73/74 already correct ─────────────
    # ── Map06(105) ↔ Map07(106) — portals 75/76 already correct ─────────────

    # ── Map07(106) ↔ Map08(98) ──────────────────────────────────────────────
    # repurpose orphaned portal 52 as Map07→R→Map08
    (52, 106, 'right', 98,  'Map08'),
    (62, 98,  'left',  106, 'Map07'),  # Map08 →L→ Map07  (was 98→83)

    # ── Map08(98) ↔ Map09(83) ───────────────────────────────────────────────
    (61, 98,  'right', 83,  'Map09'),  # Map08 →R→ Map09  (was 83→98 REVERSED)
    (60, 83,  'left',  98,  'Map08'),  # Map09 →L→ Map08  (was 83→57)

    # ── Map09(83) ↔ Map10(57) ───────────────────────────────────────────────
    (59, 83,  'right', 57,  'Map10'),  # Map09 →R→ Map10  (was 57→83 REVERSED)
    (58, 57,  'left',  83,  'Map09'),  # Map10 →L→ Map09  (was 57→56)

    # ── Map10(57) ↔ Map11(56) ───────────────────────────────────────────────
    (57, 57,  'right', 56,  'Map11'),  # Map10 →R→ Map11  (was 56→57 REVERSED)
    (56, 56,  'left',  57,  'Map10'),  # Map11 →L→ Map10  (was 56→88)

    # ── Map11(56) ↔ Map12(88) ───────────────────────────────────────────────
    (55, 56,  'right', 88,  'Map12'),  # Map11 →R→ Map12  (was 88→56 REVERSED)
    (54, 88,  'left',  56,  'Map11'),  # Map12 →L→ Map11  (was 88→87)

    # ── Map12(88) ↔ Map13(87) ───────────────────────────────────────────────
    (53, 88,  'right', 87,  'Map13'),  # Map12 →R→ Map13  (was 87→88 REVERSED)
    # repurpose orphaned portal 2 as Map13→L→Map12
    (2,  87,  'left',  88,  'Map12'),
]

for pid, src, direction, dest, scene in updates:
    cur.execute(
        """UPDATE map_portal
           SET source_map_id=%s, portal_direction=%s,
               dest_map_id=%s, dest_scene_name=%s
           WHERE portal_id=%s""",
        (src, direction, dest, scene, pid)
    )
    print(f"  portal {pid:>3}: ({src}, {direction}) → ({dest}, '{scene}')")

conn.commit()
print("\nDone — sequential portal chain committed.")
cur.close()
conn.close()
