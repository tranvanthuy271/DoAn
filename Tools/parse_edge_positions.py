import re
from pathlib import Path

scene_dir = Path(r'C:\Hub\DoAn\Client\Assets\Scenes')

for sn in ['Map00','Map01','Map02','Map03','Map04','Map05']:
    text = (scene_dir / f'{sn}.unity').read_text(encoding='utf-8')
    for lbl in ['EdgeLeft','EdgeRight']:
        hits = [m.start() for m in re.finditer(rf'value: {lbl}', text)]
        for h in hits:
            chunk = text[h:h+800]
            px = re.search(r'm_LocalPosition\.x\s*\n\s*value:\s*([-\d.e]+)', chunk)
            py_m = re.search(r'm_LocalPosition\.y\s*\n\s*value:\s*([-\d.e]+)', chunk)
            xv = px.group(1) if px else '?'
            yv = py_m.group(1) if py_m else '?'
            print(f'{sn} {lbl}: x={xv}, y={yv}')
