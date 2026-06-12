#!/usr/bin/env python3
"""
bake_server_ground_db.py
------------------------
Parse all Unity map scenes, extract BoxCollider2D data for objects on the
Ground layer (layer 6), and write a complete ServerGroundColliderDatabase.asset
in Unity YAML format.

Equivalent to running "Tools/DoAn/Bake Server Ground Colliders" in the Unity Editor,
but operates purely on the raw .unity YAML files.

Usage:
    python bake_server_ground_db.py

Output:
    Client/Assets/Resources/ScriptableObjects/ServerGroundColliderDatabase.asset
"""
import re
import math
import sys
from pathlib import Path

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

# ── Config ─────────────────────────────────────────────────────────────────────
SCENE_DIR  = Path(r"C:\Hub\DoAn\Client\Assets\Scenes")
OUTPUT     = Path(r"C:\Hub\DoAn\Client\Assets\Resources\ScriptableObjects\ServerGroundColliderDatabase.asset")
# GUID of ServerGroundColliderDatabase script (read from existing asset or .meta file)
SCRIPT_GUID = "2f6b8d5a7e7b4f8498c71d21f0ab8f22"

# Unity layer index for "Ground" (from ProjectSettings/TagManager.asset)
GROUND_LAYER = 6
MAXMAP_LAYER = 10
LAYER_NAMES = {
    GROUND_LAYER: "Ground",
    MAXMAP_LAYER: "MaxMap",
}

# All scenes to bake: sceneName -> (mapId, scene_filename)
# mapId is read dynamically from the scene YAML if not overridden here.
# Add every scene that players can inhabit on the server.
SCENES = [
    # special scenes with mapId from MapWorldConfig
    ("GameScene",         None),   # mapId auto-detected from scene
    ("DungeonWaveScene",  110),    # mapId from MapWorldConfig
    ("DungeonPartyScene", 111),    # mapId from MapWorldConfig
    # regular maps
    ("Map00", None),
    ("Map01", None),
    ("Map02", None),
    ("Map03", None),
    ("Map04", None),
    ("Map05", None),
    ("Map06", None),
    ("Map07", None),
    ("Map08", None),
    ("Map09", None),
    ("Map10", None),
    ("Map11", None),
    ("Map12", None),
    ("Map13", None),
]

# ── YAML helpers ───────────────────────────────────────────────────────────────

def parse_vec2(text: str, key: str):
    """Return (x, y) float tuple or (0, 0)."""
    m = re.search(rf"{key}:\s*\{{x:\s*([-\d.eE+]+),\s*y:\s*([-\d.eE+]+)", text)
    if m:
        return float(m.group(1)), float(m.group(2))
    return (0.0, 0.0)

def parse_vec3(text: str, key: str):
    """Return (x, y, z) float tuple or (0,0,0)."""
    m = re.search(rf"{key}:\s*\{{x:\s*([-\d.eE+]+),\s*y:\s*([-\d.eE+]+),\s*z:\s*([-\d.eE+]+)", text)
    if m:
        return float(m.group(1)), float(m.group(2)), float(m.group(3))
    return (0.0, 0.0, 0.0)

def parse_vec4(text: str, key: str):
    """Return (x, y, z, w) float tuple or identity quaternion."""
    m = re.search(
        rf"{key}:\s*\{{x:\s*([-\d.eE+]+),\s*y:\s*([-\d.eE+]+),\s*z:\s*([-\d.eE+]+),\s*w:\s*([-\d.eE+]+)",
        text)
    if m:
        return float(m.group(1)), float(m.group(2)), float(m.group(3)), float(m.group(4))
    return (0.0, 0.0, 0.0, 1.0)

def parse_float(text: str, key: str, default: float = 0.0) -> float:
    m = re.search(rf"{key}:\s*([-\d.eE+]+)", text)
    return float(m.group(1)) if m else default

def parse_int(text: str, key: str, default: int = 0) -> int:
    m = re.search(rf"{key}:\s*(-?\d+)", text)
    return int(m.group(1)) if m else default

def parse_bool(text: str, key: str, default: int = 0) -> int:
    """Returns 0 or 1."""
    return parse_int(text, key, default)

def quat_to_euler_z(qx, qy, qz, qw) -> float:
    """Extract the Z Euler angle (degrees) from a unit quaternion."""
    # For a 2D sprite the only meaningful rotation is around Z.
    # Euler Z = atan2(2*(qw*qz + qx*qy), 1 - 2*(qy*qy + qz*qz))
    sinz = 2.0 * (qw * qz + qx * qy)
    cosz = 1.0 - 2.0 * (qy * qy + qz * qz)
    return math.degrees(math.atan2(sinz, cosz))

# ── Scene parser ───────────────────────────────────────────────────────────────

def parse_scene(scene_path: Path):
    """
    Parse a Unity YAML scene file.
    Returns a list of GroundColliderData dicts suitable for the asset YAML.
    """
    text = scene_path.read_text(encoding="utf-8")
    lines = text.splitlines()

    # Index YAML blocks by fileID: {fid: {type, start_line}}
    blocks = {}
    for i, ln in enumerate(lines):
        m = re.match(r"^--- !u!(\d+) &(\d+)", ln)
        if m:
            blocks[int(m.group(2))] = {"type": int(m.group(1)), "start": i}

    fids = sorted(blocks.keys())

    def get_block_text(fid):
        start = blocks[fid]["start"]
        idx = fids.index(fid)
        end = blocks[fids[idx + 1]]["start"] if idx + 1 < len(fids) else len(lines)
        return "\n".join(lines[start:end])

    # ── GameObject (type 1): name + layer + component refs ───────────────
    gameobjects = {}  # fid -> {name, layer, component_fids}
    for fid, info in blocks.items():
        if info["type"] != 1:
            continue
        blk = get_block_text(fid)
        name_m = re.search(r"m_Name:\s*(.+)", blk)
        layer  = parse_int(blk, "m_Layer", 0)
        comp_fids = [int(x) for x in re.findall(r"component:\s*\{fileID:\s*(\d+)\}", blk)]
        gameobjects[fid] = {
            "name": name_m.group(1).strip() if name_m else "",
            "layer": layer,
            "component_fids": comp_fids,
        }

    # ── Transform (type 4): position + scale + rotation + hierarchy ───────
    transforms = {}  # fid -> {pos, scale, rot, parent_fid, go_fid}
    for fid, info in blocks.items():
        if info["type"] != 4:
            continue
        blk = get_block_text(fid)
        pos  = parse_vec3(blk, "m_LocalPosition")
        scl  = parse_vec3(blk, "m_LocalScale")
        rot  = parse_vec4(blk, "m_LocalRotation")
        go_m = re.search(r"m_GameObject:\s*\{fileID:\s*(\d+)\}", blk)
        par_m = re.search(r"m_Father:\s*\{fileID:\s*(\d+)\}", blk)
        parent_fid = int(par_m.group(1)) if par_m else 0

        transforms[fid] = {
            "pos": pos,
            "scale": scl,
            "rot": rot,
            "parent_fid": parent_fid if parent_fid != 0 else None,
            "go_fid": int(go_m.group(1)) if go_m else None,
        }

    # ── BoxCollider2D (type 61) ────────────────────────────────────────────
    box_colliders = {}  # fid -> {go_fid, offset, size, edgeRadius, isTrigger, usedByEffector}
    for fid, info in blocks.items():
        if info["type"] != 61:
            continue
        blk = get_block_text(fid)
        go_m = re.search(r"m_GameObject:\s*\{fileID:\s*(\d+)\}", blk)
        box_colliders[fid] = {
            "go_fid":        int(go_m.group(1)) if go_m else None,
            "offset":        parse_vec2(blk, "m_Offset"),
            "size":          parse_vec2(blk, "m_Size"),
            "edgeRadius":    parse_float(blk, "m_EdgeRadius", 0.0),
            "isTrigger":     parse_bool(blk, "m_IsTrigger", 0),
            "usedByEffector":parse_bool(blk, "m_UsedByEffector", 0),
        }

    # ── PlatformEffector2D (type 146) ─────────────────────────────────────
    platform_effectors = {}  # go_fid -> {useOneWay, ...}
    for fid, info in blocks.items():
        if info["type"] != 146:
            continue
        blk = get_block_text(fid)
        go_m = re.search(r"m_GameObject:\s*\{fileID:\s*(\d+)\}", blk)
        if not go_m:
            continue
        go_fid = int(go_m.group(1))
        platform_effectors[go_fid] = {
            "useOneWay":         parse_bool(blk, "m_UseOneWay", 0),
            "useOneWayGrouping": parse_bool(blk, "m_UseOneWayGrouping", 0),
            "surfaceArc":        parse_float(blk, "m_SurfaceArc", 180.0),
            "sideArc":           parse_float(blk, "m_SideArc", 0.0),
            "rotationalOffset":  parse_float(blk, "m_RotationalOffset", 0.0),
            "useSideFriction":   parse_bool(blk, "m_UseSideFriction", 0),
            "useSideBounce":     parse_bool(blk, "m_UseSideBounce", 0),
        }

    # ── Build go_fid -> transform_fid map ─────────────────────────────────
    go_to_transform = {}
    for tfid, tdata in transforms.items():
        if tdata["go_fid"]:
            go_to_transform[tdata["go_fid"]] = tfid

    # ── Compute world transform (recursive) ───────────────────────────────
    _world_cache = {}

    def world_transform(tfid):
        """Returns (wx, wy, wsx, wsy, weulerZ) — world pos, world scale, world eulerZ."""
        if tfid in _world_cache:
            return _world_cache[tfid]
        t = transforms.get(tfid)
        if t is None:
            _world_cache[tfid] = (0.0, 0.0, 1.0, 1.0, 0.0)
            return _world_cache[tfid]

        local_x, local_y, _ = t["pos"]
        local_sx, local_sy, _ = t["scale"]
        qx, qy, qz, qw = t["rot"]
        local_ez = quat_to_euler_z(qx, qy, qz, qw)

        par = t["parent_fid"]
        if par is None:
            result = (local_x, local_y, local_sx, local_sy, local_ez)
        else:
            px, py, psx, psy, pez = world_transform(par)
            wx  = px + psx * local_x
            wy  = py + psy * local_y
            wsx = psx * local_sx
            wsy = psy * local_sy
            wez = pez + local_ez
            result = (wx, wy, wsx, wsy, wez)

        _world_cache[tfid] = result
        return result

    # ── Extract Ground colliders ───────────────────────────────────────────
    # Build reverse map: go_fid -> box_collider_fid
    go_to_boxcollider = {}
    for cfid, cdata in box_colliders.items():
        gf = cdata["go_fid"]
        if gf is not None:
            go_to_boxcollider[gf] = cfid

    results = []
    for go_fid, go_data in gameobjects.items():
        if go_data["layer"] not in LAYER_NAMES:
            continue
        # must have a BoxCollider2D
        cfid = go_to_boxcollider.get(go_fid)
        if cfid is None:
            continue
        col = box_colliders[cfid]

        # get world transform
        tfid = go_to_transform.get(go_fid)
        if tfid is None:
            continue
        wx, wy, wsx, wsy, wez = world_transform(tfid)

        # PlatformEffector2D data
        pe = platform_effectors.get(go_fid)
        has_pe = pe is not None
        if not has_pe:
            pe = {
                "useOneWay": 0, "useOneWayGrouping": 0,
                "surfaceArc": 180.0, "sideArc": 0.0, "rotationalOffset": 0.0,
                "useSideFriction": 0, "useSideBounce": 0,
            }

        results.append({
            "name":               go_data["name"],
            "layerName":          LAYER_NAMES[go_data["layer"]],
            "position":           (wx, wy),
            "rotationZ":          wez,
            "scale":              (wsx, wsy),
            "offset":             col["offset"],
            "size":               col["size"],
            "edgeRadius":         col["edgeRadius"],
            "isTrigger":          col["isTrigger"],
            "usedByEffector":     col["usedByEffector"],
            "hasPlatformEffector": 1 if has_pe else 0,
            "useOneWay":          pe["useOneWay"],
            "useOneWayGrouping":  pe["useOneWayGrouping"],
            "surfaceArc":         pe["surfaceArc"],
            "sideArc":            pe["sideArc"],
            "rotationalOffset":   pe["rotationalOffset"],
            "useSideFriction":    pe["useSideFriction"],
            "useSideBounce":      pe["useSideBounce"],
        })

    return results


def get_map_id_from_scene(scene_path: Path) -> int | None:
    """Try to read mapId from the first MapManager component in the scene."""
    text = scene_path.read_text(encoding="utf-8")
    m = re.search(r"\bmapId:\s*(\d+)", text)
    return int(m.group(1)) if m else None


# ── Asset YAML generation ──────────────────────────────────────────────────────

def fmt_float(v: float) -> str:
    """Format float: remove trailing zeros but keep at least one decimal."""
    s = f"{v:.7g}"
    return s

def fmt_vec2(x: float, y: float) -> str:
    return f"{{x: {fmt_float(x)}, y: {fmt_float(y)}}}"

def collider_to_yaml(c: dict, indent: str = "    ") -> str:
    lines = []
    lines.append(f"{indent}- name: {c['name']}")
    lines.append(f"{indent}  layerName: {c.get('layerName', 'Ground')}")
    px, py = c['position']
    lines.append(f"{indent}  position: {{x: {fmt_float(px)}, y: {fmt_float(py)}}}")
    lines.append(f"{indent}  rotationZ: {fmt_float(c['rotationZ'])}")
    sx, sy = c['scale']
    lines.append(f"{indent}  scale: {{x: {fmt_float(sx)}, y: {fmt_float(sy)}}}")
    ox, oy = c['offset']
    lines.append(f"{indent}  offset: {{x: {fmt_float(ox)}, y: {fmt_float(oy)}}}")
    szx, szy = c['size']
    lines.append(f"{indent}  size: {{x: {fmt_float(szx)}, y: {fmt_float(szy)}}}")
    lines.append(f"{indent}  edgeRadius: {fmt_float(c['edgeRadius'])}")
    lines.append(f"{indent}  isTrigger: {c['isTrigger']}")
    lines.append(f"{indent}  usedByEffector: {c['usedByEffector']}")
    lines.append(f"{indent}  hasPlatformEffector: {c['hasPlatformEffector']}")
    lines.append(f"{indent}  useOneWay: {c['useOneWay']}")
    lines.append(f"{indent}  useOneWayGrouping: {c['useOneWayGrouping']}")
    lines.append(f"{indent}  surfaceArc: {fmt_float(c['surfaceArc'])}")
    lines.append(f"{indent}  sideArc: {fmt_float(c['sideArc'])}")
    lines.append(f"{indent}  rotationalOffset: {fmt_float(c['rotationalOffset'])}")
    lines.append(f"{indent}  useSideFriction: {c['useSideFriction']}")
    lines.append(f"{indent}  useSideBounce: {c['useSideBounce']}")
    return "\n".join(lines)


def write_asset(baked_maps: list, output_path: Path, script_guid: str):
    """Write the complete ServerGroundColliderDatabase.asset in Unity YAML format."""
    lines = [
        "%YAML 1.1",
        "%TAG !u! tag:unity3d.com,2011:",
        "--- !u!114 &11400000",
        "MonoBehaviour:",
        "  m_ObjectHideFlags: 0",
        "  m_CorrespondingSourceObject: {fileID: 0}",
        "  m_PrefabInstance: {fileID: 0}",
        "  m_PrefabAsset: {fileID: 0}",
        "  m_GameObject: {fileID: 0}",
        "  m_Enabled: 1",
        "  m_EditorHideFlags: 0",
        f"  m_Script: {{fileID: 11500000, guid: {script_guid}, type: 3}}",
        "  m_Name: ServerGroundColliderDatabase",
        "  m_EditorClassIdentifier: ",
        "  maps:",
    ]

    for entry in baked_maps:
        map_id    = entry["mapId"]
        scene_name = entry["sceneName"]
        colliders  = entry["colliders"]
        lines.append(f"  - mapId: {map_id}")
        lines.append(f"    sceneName: {scene_name}")
        if colliders:
            lines.append("    colliders:")
            for col in colliders:
                lines.append(collider_to_yaml(col, indent="    "))
        else:
            lines.append("    colliders: []")

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text("\n".join(lines) + "\n", encoding="utf-8")


# ── Main ───────────────────────────────────────────────────────────────────────

def main():
    baked_maps = []
    seen_map_ids = set()

    for scene_name, override_map_id in SCENES:
        scene_file = SCENE_DIR / f"{scene_name}.unity"
        if not scene_file.exists():
            print(f"[SKIP] {scene_file} not found")
            continue

        map_id = override_map_id
        if map_id is None:
            map_id = get_map_id_from_scene(scene_file)
        if map_id is None:
            print(f"[WARN] Cannot determine mapId for {scene_name} — skipping")
            continue
        if map_id in seen_map_ids:
            print(f"[SKIP] mapId={map_id} ({scene_name}) already baked")
            continue
        seen_map_ids.add(map_id)

        print(f"Baking {scene_name} (mapId={map_id}) ...", end=" ", flush=True)
        try:
            colliders = parse_scene(scene_file)
        except Exception as e:
            print(f"ERROR: {e}")
            continue

        print(f"{len(colliders)} colliders")
        baked_maps.append({
            "mapId":     map_id,
            "sceneName": scene_name,
            "colliders": colliders,
        })

    # Sort by mapId for clean output
    baked_maps.sort(key=lambda m: m["mapId"])

    write_asset(baked_maps, OUTPUT, SCRIPT_GUID)
    print(f"\n✓ Wrote {len(baked_maps)} map(s) to:\n  {OUTPUT}")

    # Summary
    print("\nSummary:")
    for entry in baked_maps:
        print(f"  mapId={entry['mapId']:4d}  scene={entry['sceneName']:25s}  "
              f"colliders={len(entry['colliders'])}")


if __name__ == "__main__":
    main()
