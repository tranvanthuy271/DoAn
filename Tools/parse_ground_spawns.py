#!/usr/bin/env python3
"""
Parse Map00–Map05 Unity scenes, extract all Ground BoxCollider2D positions,
and generate map_spawn_config JSON rows for enemies standing on each platform.
Each enemy stands at the TOP of the collider, spaced 1 Unity unit apart on X.
Schema: spawn_json = [{enemy_id, hp, exp, cx, cy, is_boss, count, respawn_time, level}]
"""
import re, json, math
from pathlib import Path

SCENE_DIR = Path(r"C:\Hub\DoAn\Client\Assets\Scenes")
MAPS = [f"Map0{i}" for i in range(6)]   # Map00–Map05

# Per-map config: enemy cycling + default stats
# Enemies: 1=Slime(Lv1,HP50,EXP50) 2=Goblin(Lv2,HP80,EXP20) 3=OrcWarrior(Lv3,HP150,EXP50)
#          4=FireSlime(Lv2,HP70,EXP15) 6=GoblinArcher(Lv8,HP200,EXP60) 7=SnowGoblin(Lv10,HP220,EXP65)
MAP_ENEMY_CONFIG = {
    #  map_id, [(enemy_id, hp, exp, level), ...]
    # scene_name Map00 → map_id=99 (Cửa phía tây)
    # scene_name Map01 → map_id=100 (Cửa phía đông)
    # scene_name Map02 → map_id=101 (Chiến trường phó bản)
    # scene_name Map03 → map_id=102 (Làng Mưa)
    # scene_name Map04 → map_id=103 (Pháo Đài Amega)
    # scene_name Map05 → map_id=104 (Vùng trống Kusa)
    "Map00": (99,  [(1, 50,  50,  1), (2,  80,  20, 2)]),
    "Map01": (100, [(1, 50,  50,  1), (2,  80,  20, 2), (3, 150, 50, 3)]),
    "Map02": (101, [(2, 80,  20,  2), (3, 150,  50, 3)]),
    "Map03": (102, [(3, 150, 50,  3), (4,  70,  15, 2)]),
    "Map04": (103, [(4, 70,  15,  2), (6, 200,  60, 8)]),
    "Map05": (104, [(6, 200, 60,  8), (7, 220,  65, 10)]),
}

# Max enemies per platform (to avoid flooding huge base-floor Ground)
MAX_PER_PLATFORM = 12
# Minimum platform width to place enemies at all (skip tiny 0.7-unit pillars)
MIN_WIDTH = 1.2
# Existing rows in map_spawn_config: map_id -> row id
EXISTING_ROWS = {99: 8, 100: 6, 101: 10, 102: 11, 103: 12, 104: 13}  # map_id: row id (from DB)

def parse_vec(s, key):
    """Extract {x: .., y: ..} from a YAML line string."""
    m = re.search(rf"{key}:\s*{{x:\s*([-\d.e]+),\s*y:\s*([-\d.e]+)", s)
    if m:
        return float(m.group(1)), float(m.group(2))
    return None

def parse_float(s, key):
    m = re.search(rf"{key}:\s*([-\d.e]+)", s)
    return float(m.group(1)) if m else None

def parse_scene(scene_path: Path):
    text = scene_path.read_text(encoding="utf-8")
    lines = text.splitlines()

    # ── Index all YAML blocks by fileID ────────────────────────────────────
    # Each block starts with "--- !u!<type> &<fileID>"
    blocks = {}  # fileID -> {type, start_line}
    for i, ln in enumerate(lines):
        m = re.match(r"^--- !u!(\d+) &(\d+)", ln)
        if m:
            blocks[int(m.group(2))] = {"type": int(m.group(1)), "start": i}
    
    fids = sorted(blocks.keys())

    def get_block_lines(fid):
        start = blocks[fid]["start"]
        # find next block start
        idx = fids.index(fid)
        end = blocks[fids[idx + 1]]["start"] if idx + 1 < len(fids) else len(lines)
        return lines[start:end]

    # ── Parse all Transform components (type 4) ───────────────────────────
    transforms = {}  # fid -> {pos_x, pos_y, scale_x, scale_y, parent_fid, children, go_fid}
    for fid, info in blocks.items():
        if info["type"] != 4:
            continue
        blk = "\n".join(get_block_lines(fid))
        pos  = parse_vec(blk, "m_LocalPosition") or (0, 0)
        scl  = parse_vec(blk, "m_LocalScale")    or (1, 1)
        go_m = re.search(r"m_GameObject:\s*\{fileID:\s*(\d+)", blk)
        par_m = re.search(r"m_Father:\s*\{fileID:\s*(\d+)", blk)
        children_m = re.findall(r"\{fileID:\s*(\d+)\}", 
                                re.search(r"m_Children:(.*?)m_Father:", blk, re.S).group(1)
                                if re.search(r"m_Children:", blk) else "")
        transforms[fid] = {
            "pos": pos, "scale": scl,
            "go_fid": int(go_m.group(1)) if go_m else None,
            "parent": int(par_m.group(1)) if par_m and int(par_m.group(1)) != 0 else None,
            "children": [int(c) for c in children_m],
        }

    # ── Parse all GameObjects (type 1) ────────────────────────────────────
    gameobjects = {}  # go_fid -> {name, components: [fid, ...]}
    for fid, info in blocks.items():
        if info["type"] != 1:
            continue
        blk = "\n".join(get_block_lines(fid))
        name_m = re.search(r"m_Name:\s*(.+)", blk)
        comp_fids = re.findall(r"component:\s*\{fileID:\s*(\d+)\}", blk)
        gameobjects[fid] = {
            "name": name_m.group(1).strip() if name_m else "",
            "components": [int(c) for c in comp_fids],
        }

    # ── Parse all BoxCollider2D (type 61) ─────────────────────────────────
    colliders = {}  # fid -> {go_fid, offset, size}
    for fid, info in blocks.items():
        if info["type"] != 61:
            continue
        blk = "\n".join(get_block_lines(fid))
        go_m   = re.search(r"m_GameObject:\s*\{fileID:\s*(\d+)", blk)
        offset = parse_vec(blk, "m_Offset") or (0, 0)
        size   = parse_vec(blk, "m_Size")   or (0, 0)
        colliders[fid] = {
            "go_fid": int(go_m.group(1)) if go_m else None,
            "offset": offset,
            "size": size,
        }

    # ── Build go -> transform map ─────────────────────────────────────────
    go_to_transform = {}  # go_fid -> transform_fid
    for tfid, tdata in transforms.items():
        if tdata["go_fid"]:
            go_to_transform[tdata["go_fid"]] = tfid

    # ── Compute world position (recursive) ───────────────────────────────
    _world_cache = {}
    def world_pos(tfid):
        if tfid in _world_cache:
            return _world_cache[tfid]
        t = transforms.get(tfid)
        if t is None:
            return (0.0, 0.0, 1.0, 1.0)  # x, y, sx, sy
        par = t["parent"]
        if par is None:
            wx, wy, wsx, wsy = t["pos"][0], t["pos"][1], t["scale"][0], t["scale"][1]
        else:
            px, py, psx, psy = world_pos(par)
            wx  = px + psx * t["pos"][0]
            wy  = py + psy * t["pos"][1]
            wsx = psx * t["scale"][0]
            wsy = psy * t["scale"][1]
        _world_cache[tfid] = (wx, wy, wsx, wsy)
        return _world_cache[tfid]

    # ── Find Ground GameObjects and their BoxCollider2D ───────────────────
    grounds = []
    for go_fid, go_data in gameobjects.items():
        name = go_data["name"]
        if not re.match(r"^Ground(\s*\(\d+\))?$", name):
            continue
        # get transform
        tfid = go_to_transform.get(go_fid)
        if tfid is None:
            continue
        wx, wy, wsx, wsy = world_pos(tfid)

        # get BoxCollider2D on this GO
        col = None
        for cfid in go_data["components"]:
            if cfid in colliders:
                col = colliders[cfid]
                break
        if col is None:
            continue

        # offset & size are in LOCAL space of the object (scale already baked in world?)
        # In Unity the collider offset/size are in the object's local space but
        # the world-space size = object_world_scale * size.
        # World center of collider
        cx = wx + wsx * col["offset"][0]
        cy = wy + wsy * col["offset"][1]
        world_w = wsx * col["size"][0]
        world_h = wsy * col["size"][1]
        top_y   = cy + world_h / 2.0

        grounds.append({
            "name": name,
            "go_fid": go_fid,
            "world_center": (round(cx, 4), round(cy, 4)),
            "world_size":   (round(world_w, 4), round(world_h, 4)),
            "top_y":        round(top_y, 4),
            "left_x":       round(cx - world_w / 2.0, 4),
            "right_x":      round(cx + world_w / 2.0, 4),
        })

    return grounds


def generate_spawn_entries(map_name, grounds):
    """Generate spawn_json entries: 1 enemy per 1 Unity unit across each platform."""
    map_id, enemy_pool = MAP_ENEMY_CONFIG[map_name]
    entries = []
    idx = 0
    for g in sorted(grounds, key=lambda g: g["left_x"]):
        width = g["right_x"] - g["left_x"]
        if width < MIN_WIDTH:
            continue
        num = min(MAX_PER_PLATFORM, max(1, int(math.floor(width))))
        # Distribute evenly across the platform
        step = width / (num + 1)
        for i in range(num):
            x = g["left_x"] + step * (i + 1)
            eid, hp, exp, level = enemy_pool[idx % len(enemy_pool)]
            entries.append({
                "enemy_id":    eid,
                "hp":          hp,
                "exp":         exp,
                "cx":          round(x, 2),
                "cy":          round(g["top_y"] + 0.5, 2),  # 0.5u above surface
                "is_boss":     False,
                "count":       1,
                "respawn_time": 10,
                "level":       level
            })
            idx += 1
    return entries


def main():
    all_results = {}
    for map_name in MAPS:
        scene_path = SCENE_DIR / f"{map_name}.unity"
        if not scene_path.exists():
            print(f"[SKIP] {scene_path} not found")
            continue
        print(f"\n{'='*50}\n{map_name}")
        grounds = parse_scene(scene_path)
        print(f"  Found {len(grounds)} Ground objects:")
        for g in sorted(grounds, key=lambda g: g["left_x"]):
            print(f"    {g['name']:20s}  center=({g['world_center'][0]:8.3f}, {g['world_center'][1]:8.3f})"
                  f"  size=({g['world_size'][0]:6.3f} x {g['world_size'][1]:5.3f})"
                  f"  top_y={g['top_y']:8.3f}")
        
        entries = generate_spawn_entries(map_name, grounds)
        map_id = MAP_ENEMY_CONFIG[map_name][0]
        all_results[map_name] = {"map_id": map_id, "grounds": grounds, "spawn_entries": entries}
        print(f"  → {len(entries)} spawn entries generated")

    # Output JSON (human-readable reference)
    out_json = Path(r"C:\Hub\DoAn\Scripts\ground_spawns.json")
    serial = {mn: {"map_id": d["map_id"], "grounds": d["grounds"], "spawn_entries": d["spawn_entries"]}
              for mn, d in all_results.items()}
    out_json.write_text(json.dumps(serial, indent=2, ensure_ascii=False), encoding="utf-8")
    print(f"\n✓ JSON saved to {out_json}")

    # Output SQL
    out_sql = Path(r"C:\Hub\DoAn\Scripts\ground_spawns.sql")
    sql_lines = [
        "-- Auto-generated ground-based enemy spawn config (Map00–Map05)",
        "-- spawn_json format: {enemy_id,hp,exp,cx,cy,is_boss,count,respawn_time,level}",
        "-- cy = top surface of BoxCollider2D + 0.5u  |  enemies 1 Unity unit apart on X",
        "",
    ]
    for map_name, data in all_results.items():
        map_id   = data["map_id"]
        entries  = data["spawn_entries"]
        spawn_j  = json.dumps(entries, ensure_ascii=False, separators=(",", ":"))
        spawn_j_escaped = spawn_j.replace("'", "\\'")
        sql_lines.append(f"-- {map_name}: {len(entries)} spawn points")
        if map_id in EXISTING_ROWS:
            row_id = EXISTING_ROWS[map_id]
            sql_lines.append(
                f"UPDATE `map_spawn_config` SET `spawn_json`='{spawn_j_escaped}', "
                f"`updated_at`=NOW() WHERE `id`={row_id};"
            )
        else:
            drop_j = "[]"
            sql_lines.append(
                f"INSERT INTO `map_spawn_config` (`map_id`,`spawn_json`,`drop_json`) VALUES "
                f"({map_id},'{spawn_j_escaped}','{drop_j}');"
            )
        sql_lines.append("")

    out_sql.write_text("\n".join(sql_lines), encoding="utf-8")
    print(f"✓ SQL saved to {out_sql}")


if __name__ == "__main__":
    main()
