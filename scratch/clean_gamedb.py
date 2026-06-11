import re

# Read current gamedb.sql
with open('/root/DoAn/gamedb.sql', 'r', encoding='utf-8') as f:
    content = f.read()

# Replace invalid MySQL 8.0 default constraints on longtext columns
lines = content.split('\n')
new_lines = []
for line in lines:
    if 'longtext' in line and "DEFAULT '[]'" in line:
        line = line.replace("DEFAULT '[]'", "")
    if 'longtext' in line and "DEFAULT '{}'" in line:
        line = line.replace("DEFAULT '{}'", "")
    new_lines.append(line)

new_content = '\n'.join(new_lines)

# Ensure FOREIGN_KEY_CHECKS = 0 is at the top of the file
if 'SET FOREIGN_KEY_CHECKS = 0;' not in new_content:
    new_content = "SET FOREIGN_KEY_CHECKS = 0;\n" + new_content

# Append dummy maps for 1, 2, 3 and SET FOREIGN_KEY_CHECKS = 1 at the end
dummy_insert = """
-- Insert dummy maps to satisfy boss_config and map_spawn_config references
INSERT INTO `map_config` (`map_id`, `map_name`, `scene_name`, `spawn_points_json`, `min_level`, `max_level`, `required_quest_id`) VALUES
(1, 'Map 1 (Dummy)', 'Map1', '[]', 1, 100, NULL),
(2, 'Map 2 (Dummy)', 'Map2', '[]', 1, 100, NULL),
(3, 'Map 3 (Dummy)', 'Map3', '[]', 1, 100, NULL)
ON DUPLICATE KEY UPDATE `map_name`=`map_name`;

SET FOREIGN_KEY_CHECKS = 1;
"""

if 'SET FOREIGN_KEY_CHECKS = 1;' not in new_content:
    new_content = new_content + "\n" + dummy_insert

# Write cleaned content back
with open('/root/DoAn/gamedb.sql', 'w', encoding='utf-8') as f:
    f.write(new_content)

print("gamedb.sql cleaned and modified successfully!")
