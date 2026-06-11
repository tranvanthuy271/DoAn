import re

with open('/root/DoAn/gamedb.sql', 'r', encoding='utf-8') as f:
    content = f.read()

lines = content.split('\n')
new_lines = []
for line in lines:
    if 'longtext' in line and "DEFAULT '[]'" in line:
        line = line.replace("DEFAULT '[]'", "")
    if 'longtext' in line and "DEFAULT '{}'" in line:
        line = line.replace("DEFAULT '{}'", "")
    new_lines.append(line)

new_content = '\n'.join(new_lines)
with open('/root/DoAn/gamedb.sql', 'w', encoding='utf-8') as f:
    f.write(new_content)
print("gamedb.sql cleaned successfully!")
