"""
add_enemy_spawners.py
─────────────────────
1. Parses each Map00–Map13 scene to extract platform (BoxCollider2D+PlatformEffector2D) positions.
2. Inserts / replaces spawn_json in `map_spawn_config` for each of the 14 world maps.
3. Injects an EnemyPrefabManager + HostSpawnConfigLoader GameObject into each Map scene YAML.

Each map gets its own enemy type(s) based on difficulty:
  Map00 (lv1-2) : Slime(1), Goblin(2)
  Map01 (lv2-3) : Goblin(2), Fire Slime(4)
  Map02 (lv3-8) : Orc Warrior(3), Goblin Archer(6)
  Map03 (lv3-10): Orc Warrior(3), Snow Goblin(7)
  Map04 (lv8-10): Goblin Archer(6), Snow Goblin(7)
  Map05 (lv10-15): Snow Goblin(7), Băng Binh(11)
  Map06 (lv8-11): Cây Thứ Mộc(13), Mộc Linh boss(12)
  Map07 (lv15-18): Hắc Quân Binh(15), Hắc Quân Võ(16)
  Map08 (lv15-18): Hắc Quân Binh(15), Boss Dragon boss(5)
  Map09 (lv18-20): Hắc Quân Võ(16), Ice Witch boss(9)
  Map10 (lv15-20): Băng Binh(11), Hắc Quân Võ(16)
  Map11 (lv11-18): Cây Thứ Mộc(13), Hắc Quân Võ(16)
  Map12 (lv18-20): Hắc Quân Binh(15), Fire Dragon boss(8)
  Map13 (lv20-25): Hắc Quân Võ(16), Chúa Tể Bóng Tối boss(17)
"""

import re, os, json, random, hashlib
import mysql.connector

os.chdir(r'C:\Hub\DoAn')
SCENE_DIR = r'Client\Assets\Scenes'

# ─── Map metadata ──────────────────────────────────────────────────────────────
MAP_INFO = {
    'Map00': {'map_id': 99,  'enemies': [
        {'enemy_id': 1, 'hp': 120, 'exp': 30,  'level': 1, 'is_boss': False, 'count': 1, 'respawn': 20},
        {'enemy_id': 2, 'hp': 160, 'exp': 40,  'level': 2, 'is_boss': False, 'count': 1, 'respawn': 20},
    ]},
    'Map01': {'map_id': 100, 'enemies': [
        {'enemy_id': 2, 'hp': 180, 'exp': 45,  'level': 3, 'is_boss': False, 'count': 1, 'respawn': 22},
        {'enemy_id': 4, 'hp': 150, 'exp': 38,  'level': 2, 'is_boss': False, 'count': 1, 'respawn': 22},
    ]},
    'Map02': {'map_id': 101, 'enemies': [
        {'enemy_id': 3, 'hp': 250, 'exp': 80,  'level': 4, 'is_boss': False, 'count': 1, 'respawn': 25},
        {'enemy_id': 6, 'hp': 280, 'exp': 90,  'level': 6, 'is_boss': False, 'count': 1, 'respawn': 25},
    ]},
    'Map03': {'map_id': 102, 'enemies': [
        {'enemy_id': 3, 'hp': 300, 'exp': 90,  'level': 5, 'is_boss': False, 'count': 1, 'respawn': 25},
        {'enemy_id': 7, 'hp': 320, 'exp': 100, 'level': 8, 'is_boss': False, 'count': 1, 'respawn': 25},
    ]},
    'Map04': {'map_id': 103, 'enemies': [
        {'enemy_id': 6, 'hp': 350, 'exp': 110, 'level': 7, 'is_boss': False, 'count': 1, 'respawn': 28},
        {'enemy_id': 7, 'hp': 380, 'exp': 120, 'level': 9, 'is_boss': False, 'count': 1, 'respawn': 28},
    ]},
    'Map05': {'map_id': 104, 'enemies': [
        {'enemy_id': 7,  'hp': 400,  'exp': 140, 'level': 10, 'is_boss': False, 'count': 1, 'respawn': 30},
        {'enemy_id': 11, 'hp': 500,  'exp': 160, 'level': 13, 'is_boss': False, 'count': 1, 'respawn': 30},
    ]},
    'Map06': {'map_id': 105, 'enemies': [
        {'enemy_id': 13, 'hp': 600,  'exp': 180, 'level': 11, 'is_boss': False, 'count': 1, 'respawn': 30},
        {'enemy_id': 12, 'hp': 1200, 'exp': 500, 'level': 8,  'is_boss': True,  'count': 1, 'respawn': 120},
    ]},
    'Map07': {'map_id': 106, 'enemies': [
        {'enemy_id': 15, 'hp': 550,  'exp': 160, 'level': 14, 'is_boss': False, 'count': 1, 'respawn': 30},
        {'enemy_id': 16, 'hp': 800,  'exp': 250, 'level': 16, 'is_boss': False, 'count': 1, 'respawn': 35},
    ]},
    'Map08': {'map_id': 98,  'enemies': [
        {'enemy_id': 15, 'hp': 600,  'exp': 180, 'level': 15, 'is_boss': False, 'count': 1, 'respawn': 30},
        {'enemy_id': 5,  'hp': 2500, 'exp': 1000,'level': 10, 'is_boss': True,  'count': 1, 'respawn': 180},
    ]},
    'Map09': {'map_id': 83,  'enemies': [
        {'enemy_id': 16, 'hp': 900,  'exp': 280, 'level': 17, 'is_boss': False, 'count': 1, 'respawn': 35},
        {'enemy_id': 9,  'hp': 3000, 'exp': 1200,'level': 15, 'is_boss': True,  'count': 1, 'respawn': 180},
    ]},
    'Map10': {'map_id': 57,  'enemies': [
        {'enemy_id': 11, 'hp': 700,  'exp': 220, 'level': 15, 'is_boss': False, 'count': 1, 'respawn': 30},
        {'enemy_id': 16, 'hp': 950,  'exp': 300, 'level': 18, 'is_boss': False, 'count': 1, 'respawn': 35},
    ]},
    'Map11': {'map_id': 56,  'enemies': [
        {'enemy_id': 13, 'hp': 800,  'exp': 250, 'level': 12, 'is_boss': False, 'count': 1, 'respawn': 30},
        {'enemy_id': 16, 'hp': 1000, 'exp': 320, 'level': 18, 'is_boss': False, 'count': 1, 'respawn': 35},
    ]},
    'Map12': {'map_id': 88,  'enemies': [
        {'enemy_id': 15, 'hp': 750,  'exp': 240, 'level': 16, 'is_boss': False, 'count': 1, 'respawn': 30},
        {'enemy_id': 8,  'hp': 5000, 'exp': 1600,'level': 15, 'is_boss': True,  'count': 1, 'respawn': 300},
    ]},
    'Map13': {'map_id': 87,  'enemies': [
        {'enemy_id': 16, 'hp': 1100, 'exp': 360, 'level': 19, 'is_boss': False, 'count': 1, 'respawn': 35},
        {'enemy_id': 17, 'hp': 6000, 'exp': 2500,'level': 20, 'is_boss': True,  'count': 1, 'respawn': 360},
    ]},
}

# ─── Enemy prefab GUIDs (all share the same root fileID) ──────────────────────
ENEMY_PREFAB_GUID = {
    1:  '8c9e2686dbd56884bbcb61d1c85a0ac5',
    2:  '7053c1f878c1e8e4d811e3077acf7296',
    3:  '53ebffade81e53e46bfae9645f21e5a6',
    4:  'e556b69e4f1391d42b65a5af6403da78',
    5:  '32d1aca84ae95174bb16e32cbe74decc',
    6:  'e103deec0be8cd24496bd764863cc0a1',
    7:  '5d00e42a4fd24c94f80a298108ba0dd5',
    8:  '2234a218d30a03b47b31ed6f22b6c116',
    9:  '67c53d83cc094fc47ab3be1470e333e8',
    10: '41121062cd23e094aa203ea267cc25ae',
    11: '94f45d9de42eb7543b808d637db2e333',
    12: 'cd1bc4a2baaad3c4f850f74277fd4866',
    13: '2ac0f0522dc94034fb2f280696e04ee0',
    14: 'c3a4833e80603634e9b548a5e4edda09',
    15: 'd42d7fc1e047829459e5471796dec642',
    16: 'cf6b138b5d4be474ca424183e5c28f52',
    17: 'e1a902c186165d84d94effa23e1e0a8f',
}
ENEMY_NAMES = {
    1: 'Slime', 2: 'Goblin', 3: 'Orc Warrior', 4: 'Fire Slime', 5: 'Boss Dragon',
    6: 'Goblin Archer', 7: 'Snow Goblin', 8: 'Fire Dragon', 9: 'Ice Witch',
    10: 'Final Dragon', 11: 'Bang Binh', 12: 'Moc Linh', 13: 'Cay Thu Moc',
    14: 'Rong Chua', 15: 'Hac Quan Binh', 16: 'Hac Quan Vo', 17: 'Chua Te Bong Toi',
}

# Script GUIDs
GUID_NETWORK_OBJECT = 'd5a57f767e5e46a458fc5d3c628d0cbb'
GUID_ENEMY_PREFAB_MGR = 'ffca5f52e12934541b415eb6a17900ef'
GUID_HOST_SPAWN_LOADER = '032c39a771bbb1f448896a492a6cb12c'
ENEMY_ROOT_FILE_ID = '4006840501700251420'


# ─── Unique ID generation ──────────────────────────────────────────────────────
def stable_id(seed_str):
    """Generate a stable positive 64-bit-like integer from a string seed."""
    h = int(hashlib.sha256(seed_str.encode()).hexdigest()[:15], 16)
    return h % (2**62)  # stay positive, within Unity's range


# ─── Scene platform parser ─────────────────────────────────────────────────────
def parse_platforms(content):
    transforms = {}
    for m in re.finditer(r'--- !u!4 &(\d+)\nTransform:(.*?)(?=--- !u!)', content, re.DOTALL):
        fid, block = m.group(1), m.group(2)
        px = re.search(r'm_LocalPosition: \{x: ([\-0-9.]+)', block)
        py = re.search(r'm_LocalPosition: \{x: [\-0-9.]+, y: ([\-0-9.]+)', block)
        go = re.search(r'm_GameObject: \{fileID: (\d+)\}', block)
        if px and py and go:
            transforms[fid] = {'x': float(px.group(1)), 'y': float(py.group(1)), 'go': go.group(1)}

    ground_gos = set()
    for m in re.finditer(r'--- !u!251 &\d+\nPlatformEffector2D:(.*?)(?=--- !u!)', content, re.DOTALL):
        go = re.search(r'm_GameObject: \{fileID: (\d+)\}', m.group(1))
        if go:
            ground_gos.add(go.group(1))

    colliders = {}
    for m in re.finditer(r'--- !u!61 &\d+\nBoxCollider2D:(.*?)(?=--- !u!)', content, re.DOTALL):
        block = m.group(1)
        go = re.search(r'm_GameObject: \{fileID: (\d+)\}', block)
        if not go or go.group(1) not in ground_gos:
            continue
        ox = re.search(r'm_Offset: \{x: ([\-0-9.]+)', block)
        oy = re.search(r'm_Offset: \{x: [\-0-9.]+, y: ([\-0-9.]+)', block)
        sx = re.search(r'm_Size: \{x: ([\-0-9.]+)', block)
        sy = re.search(r'm_Size: \{x: [\-0-9.]+, y: ([\-0-9.]+)', block)
        if ox and oy and sx and sy:
            colliders[go.group(1)] = {
                'ox': float(ox.group(1)), 'oy': float(oy.group(1)),
                'sx': float(sx.group(1)), 'sy': float(sy.group(1))
            }

    platforms = []
    for go_id in ground_gos:
        t = next((td for td in transforms.values() if td['go'] == go_id), None)
        c = colliders.get(go_id)
        if not t or not c:
            continue
        top_y = t['y'] + c['oy'] + c['sy'] / 2
        cx = t['x'] + c['ox']
        platforms.append((round(cx, 2), round(top_y + 0.5, 2)))
    return sorted(set(platforms), key=lambda p: p[0])


# ─── Spawn JSON builder ────────────────────────────────────────────────────────
def build_spawn_json(map_name, platforms, enemies):
    """
    Distribute enemy spawn entries across platforms.
    Bosses go to the rightmost platform; normal enemies distributed evenly.
    """
    if not platforms:
        # Fallback positions if parsing failed
        platforms = [(0.0, 1.0), (5.0, 1.0), (10.0, 1.0)]

    normals = [e for e in enemies if not e['is_boss']]
    bosses  = [e for e in enemies if e['is_boss']]

    entries = []

    # Distribute normal enemies across platforms (skip first/last for edge safety)
    usable = platforms[1:-1] if len(platforms) > 2 else platforms
    # Pick evenly spaced platforms for normal enemies
    step = max(1, len(usable) // max(1, len(normals) * 3))  # ~3 spawns per enemy type
    slot = 0
    for e in normals:
        placed = 0
        while placed < 3 and slot < len(usable):
            cx, cy = usable[slot]
            entries.append({
                'enemy_id': e['enemy_id'],
                'hp': e['hp'],
                'exp': e['exp'],
                'cx': cx,
                'cy': cy,
                'is_boss': False,
                'count': e['count'],
                'respawn_time': e['respawn'],
                'level': e['level'],
            })
            slot += step
            placed += 1

    # Bosses go near the rightmost area
    boss_platforms = platforms[-3:] if len(platforms) >= 3 else platforms
    for i, e in enumerate(bosses):
        cx, cy = boss_platforms[i % len(boss_platforms)]
        entries.append({
            'enemy_id': e['enemy_id'],
            'hp': e['hp'],
            'exp': e['exp'],
            'cx': cx,
            'cy': cy,
            'is_boss': True,
            'count': 1,
            'respawn_time': e['respawn'],
            'level': e['level'],
        })

    return json.dumps(entries, ensure_ascii=False, indent=2)


# ─── Scene YAML injection ──────────────────────────────────────────────────────
def build_enemy_prefab_manager_yaml(map_name, enemy_ids, go_fid, tf_fid, mb_fid):
    """Build YAML for an EnemyPrefabManager GameObject."""
    prefab_entries = ''
    for eid in enemy_ids:
        g = ENEMY_PREFAB_GUID[eid]
        n = ENEMY_NAMES[eid]
        prefab_entries += f'  - enemyId: {eid}\n    enemyPrefab: {{fileID: {ENEMY_ROOT_FILE_ID}, guid: {g}, type: 3}}\n    enemyName: {n}\n'

    return f"""--- !u!1 &{go_fid}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {tf_fid}}}
  - component: {{fileID: {mb_fid}}}
  m_Layer: 0
  m_Name: EnemyPrefabManager
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{tf_fid}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_fid}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!114 &{mb_fid}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_fid}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {GUID_ENEMY_PREFAB_MGR}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  enemyPrefabs:
{prefab_entries}"""


def build_host_spawn_loader_yaml(map_name, map_id, go_fid, tf_fid, no_fid, mb_fid, epm_mb_fid):
    """Build YAML for a HostSpawnConfigLoader + NetworkObject GameObject."""
    # Stable GlobalObjectIdHash from map_name
    goid_hash = stable_id(f'HostSpawnLoader_{map_name}') % (2**31)

    return f"""--- !u!1 &{go_fid}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {tf_fid}}}
  - component: {{fileID: {no_fid}}}
  - component: {{fileID: {mb_fid}}}
  m_Layer: 0
  m_Name: HostSpawnConfigLoader
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{tf_fid}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_fid}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!114 &{no_fid}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_fid}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {GUID_NETWORK_OBJECT}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  GlobalObjectIdHash: {goid_hash}
  InScenePlacedSourceGlobalObjectIdHash: 0
  AlwaysReplicateAsRoot: 0
  SynchronizeTransform: 1
  ActiveSceneSynchronization: 0
  SceneMigrationSynchronization: 1
  SpawnWithObservers: 1
  DontDestroyWithOwner: 0
  AutoObjectParentSync: 1
  SyncOwnerTransformWhenParented: 1
  AllowOwnerToParent: 0
--- !u!114 &{mb_fid}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_fid}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {GUID_HOST_SPAWN_LOADER}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  apiBaseURL: http://localhost:5000/api
  mapId: {map_id}
  enemyPrefabManager: {{fileID: {epm_mb_fid}}}
  fallbackSpawner: {{fileID: 0}}
  fallbackToOldSpawner: 1
  multiSpawnSpreadRadius: 0.8
  OnSpawnComplete:
    m_PersistentCalls:
      m_Calls: []
  OnSpawnError:
    m_PersistentCalls:
      m_Calls: []
"""


def inject_into_scene(scene_path, map_name, map_id, enemy_ids):
    """Read scene, check if spawner already exists, inject if not."""
    with open(scene_path, encoding='utf-8') as f:
        content = f.read()

    if 'HostSpawnConfigLoader' in content:
        print(f'  [{map_name}] HostSpawnConfigLoader already present — skipping scene injection.')
        return

    # Generate stable unique fileIDs
    seed = f'{map_name}'
    epm_go   = stable_id(seed + '_epm_go')
    epm_tf   = stable_id(seed + '_epm_tf')
    epm_mb   = stable_id(seed + '_epm_mb')
    hscl_go  = stable_id(seed + '_hscl_go')
    hscl_tf  = stable_id(seed + '_hscl_tf')
    hscl_no  = stable_id(seed + '_hscl_no')
    hscl_mb  = stable_id(seed + '_hscl_mb')

    epm_yaml  = build_enemy_prefab_manager_yaml(map_name, enemy_ids, epm_go, epm_tf, epm_mb)
    hscl_yaml = build_host_spawn_loader_yaml(map_name, map_id, hscl_go, hscl_tf, hscl_no, hscl_mb, epm_mb)

    # Insert before SceneRoots block
    scene_roots_pattern = r'(--- !u!1660057539 &9223372036854775807\nSceneRoots:.*?m_Roots:)(.*?)(--- |$)'
    # Find SceneRoots section
    sr_match = re.search(r'(--- !u!1660057539 &9223372036854775807\nSceneRoots:.*?m_Roots:\n)((?:  - \{fileID: \d+\}\n)*)', content, re.DOTALL)
    
    if sr_match:
        # Add new root entries
        new_roots = (f'  - {{fileID: {epm_go}}}\n'
                     f'  - {{fileID: {hscl_go}}}\n')
        new_content = (
            content[:sr_match.end(1)]
            + sr_match.group(2)
            + new_roots
            + content[sr_match.end(2):]
        )
    else:
        new_content = content

    # Append the new YAML blocks before SceneRoots
    insertion_point = new_content.find('--- !u!1660057539')
    if insertion_point == -1:
        # Append at end
        new_content += '\n' + epm_yaml + hscl_yaml
    else:
        new_content = new_content[:insertion_point] + epm_yaml + '\n' + hscl_yaml + '\n' + new_content[insertion_point:]

    with open(scene_path, 'w', encoding='utf-8') as f:
        f.write(new_content)

    print(f'  [{map_name}] Injected EnemyPrefabManager & HostSpawnConfigLoader (mapId={map_id})')


# ─── DB update ────────────────────────────────────────────────────────────────
def update_db(map_id, spawn_json_str):
    conn = mysql.connector.connect(host='localhost', user='root', password='', database='gamedb')
    cur = conn.cursor()
    cur.execute("""
        INSERT INTO map_spawn_config (map_id, spawn_json, drop_json)
        VALUES (%s, %s, '[]')
        ON DUPLICATE KEY UPDATE spawn_json = VALUES(spawn_json)
    """, (map_id, spawn_json_str))
    conn.commit()
    cur.close()
    conn.close()


# ─── Main ──────────────────────────────────────────────────────────────────────
def main():
    for map_name, info in MAP_INFO.items():
        map_id  = info['map_id']
        enemies = info['enemies']
        scene_path = os.path.join(SCENE_DIR, map_name + '.unity')

        print(f'\n=== {map_name} (map_id={map_id}) ===')

        # 1. Parse platforms
        with open(scene_path, encoding='utf-8') as f:
            content = f.read()
        platforms = parse_platforms(content)
        print(f'  Platforms found: {len(platforms)}')

        # 2. Build spawn JSON
        spawn_json_str = build_spawn_json(map_name, platforms, enemies)
        entries = json.loads(spawn_json_str)
        print(f'  Spawn entries: {len(entries)}')

        # 3. Update DB
        update_db(map_id, spawn_json_str)
        print(f'  DB updated: map_spawn_config[{map_id}]')

        # 4. Inject into scene
        enemy_ids = list({e['enemy_id'] for e in enemies})
        inject_into_scene(scene_path, map_name, map_id, enemy_ids)

    print('\n✓ Done. All 14 maps processed.')


if __name__ == '__main__':
    main()
