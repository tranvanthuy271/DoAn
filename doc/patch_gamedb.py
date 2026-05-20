import re

with open(r'C:\Hub\DoAn\maps_full.sql', encoding='utf-8') as f:
    maps_sql = f.read()

cfg_match = re.search(r'(INSERT INTO `map_config`.*?;)', maps_sql, re.DOTALL)
portal_match = re.search(r'(INSERT INTO `map_portal`.*?;)', maps_sql, re.DOTALL)

new_cfg_insert = cfg_match.group(1)
new_portal_insert = portal_match.group(1)

with open(r'C:\Hub\DoAn\gamedb.sql', encoding='utf-8') as f:
    content = f.read()

old_cfg = re.search(r'INSERT INTO `map_config`.*?;', content, re.DOTALL).group(0)
content = content.replace(old_cfg, new_cfg_insert, 1)

old_portal = re.search(r'INSERT INTO `map_portal`.*?;', content, re.DOTALL).group(0)
content = content.replace(old_portal, new_portal_insert, 1)

with open(r'C:\Hub\DoAn\gamedb.sql', 'w', encoding='utf-8') as f:
    f.write(content)

print('gamedb.sql updated')
print(f'map_config insert: {len(new_cfg_insert)} chars')
print(f'map_portal insert: {len(new_portal_insert)} chars')
