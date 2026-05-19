import json

d = json.loads(open(r'C:\Hub\DoAn\Scripts\ground_spawns.json').read())
for sn in ['Map00','Map01','Map02','Map03','Map04','Map05']:
    gs = d[sn]['grounds']
    main = max(gs, key=lambda g: g['right_x'] - g['left_x'])
    lx = main['left_x']
    rx = main['right_x']
    ty = main['top_y']
    print(sn + ": left=" + str(round(lx,2)) + "  right=" + str(round(rx,2)) + "  top_y=" + str(round(ty,2)))
