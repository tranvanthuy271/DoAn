"""
Update currentMapId in Unity scene files to match the scene→map_id assignments.

Scenes with currentMapId=1 (default placeholder) need to be updated.
Map01/Map02/Map03 are already correct (100/101/102).
"""
import re, os

SCENES_DIR = r"C:\Hub\DoAn\Client\Assets\Scenes"

# (scene_filename_without_ext, new_map_id)
SCENE_MAP_IDS = [
    ('Map00', 99),
    ('Map04', 103),
    ('Map05', 104),
    ('Map06', 105),
    ('Map07', 106),
    ('Map08', 98),
    ('Map09', 83),
    ('Map10', 57),
    ('Map11', 56),
    ('Map12', 88),
    ('Map13', 87),
    ('DungeonPartyScene', 111),
]

pattern = re.compile(
    r'(      propertyPath: currentMapId\r?\n      value: )1(\r?\n      objectReference:)',
    re.MULTILINE
)

for scene_name, map_id in SCENE_MAP_IDS:
    path = os.path.join(SCENES_DIR, f"{scene_name}.unity")
    if not os.path.exists(path):
        print(f"  SKIP (not found): {scene_name}")
        continue

    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()

    count = len(pattern.findall(content))
    if count == 0:
        print(f"  {scene_name}: no 'currentMapId=1' found (already correct or no trigger)")
        continue

    new_content = pattern.sub(
        rf'\g<1>{map_id}\2',
        content
    )

    with open(path, 'w', encoding='utf-8') as f:
        f.write(new_content)

    print(f"  {scene_name}: updated {count} occurrence(s) → currentMapId={map_id}")

print("\nAll Unity scene files updated.")
