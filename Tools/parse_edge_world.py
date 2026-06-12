import re
from pathlib import Path

scene_dir = Path(r'C:\Hub\DoAn\Client\Assets\Scenes')

for sn in ['Map00','Map01','Map02','Map03','Map04','Map05']:
    text = (scene_dir / f'{sn}.unity').read_text(encoding='utf-8')
    for lbl in ['EdgeLeft','EdgeRight']:
        h = text.find(f'value: {lbl}')
        if h == -1:
            print(f'{sn} {lbl}: NOT FOUND')
            continue
        pf_start = text.rfind('PrefabInstance:', 0, h)
        chunk = text[pf_start:h+50]
        parent_m = re.search(r'm_TransformParent:\s*\{fileID:\s*(\d+)', chunk)
        local_x_m = re.search(r'm_LocalPosition\.x\s*\n\s*value:\s*([-\d.e]+)', text[h:h+600])
        local_y_m = re.search(r'm_LocalPosition\.y\s*\n\s*value:\s*([-\d.e]+)', text[h:h+600])
        lx = float(local_x_m.group(1)) if local_x_m else 0.0
        ly = float(local_y_m.group(1)) if local_y_m else 0.0

        px, py = 0.0, 0.0
        if parent_m:
            fid = parent_m.group(1)
            idx = text.find(f'--- !u!4 &{fid}')
            if idx != -1:
                pm = re.search(r'm_LocalPosition:\s*\{x:\s*([-\d.e]+),\s*y:\s*([-\d.e]+)', text[idx:idx+400])
                if pm:
                    px = float(pm.group(1))
                    py = float(pm.group(2))

        wx = round(px + lx, 3)
        wy = round(py + ly, 3)
        print(f'{sn} {lbl}: world_x={wx}, world_y={wy}')
