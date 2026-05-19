import json

data = json.loads(open(r'C:\Hub\DoAn\Scripts\ground_spawns.json').read())
for sn, grounds in data.items():
    main = max(grounds, key=lambda g: g['right_x'] - g['left_x'])
    lx = main['left_x']
    rx = main['right_x']
    ty = main['top_y']
    print(f"{sn}: main_floor left={lx:.2f}  right={rx:.2f}  top_y={ty:.2f}")
