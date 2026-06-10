import os
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')

filepath = r"c:\Hub\DoAn\docs\CHUONG2_BAO_CAO.md"

if not os.path.exists(filepath):
    print(f"Error: {filepath} not found.")
    exit(1)

with open(filepath, 'r', encoding='utf-8', errors='replace') as f:
    content = f.read()

# Let's find all mentions of "Hình 2." or "Hình 2."
pattern = r'(?:Hình|Hình)\s*2\.\d+'
matches = re.findall(pattern, content, re.IGNORECASE)

print(f"Total mentions of Figure 2.x in CHUONG2_BAO_CAO.md: {len(matches)}")
print("Matches found:")
for m in sorted(list(set(matches))):
    print(f"- {m}")

# Let's print out lines containing "Hình 2." to see their surrounding context
lines = content.split('\n')
for i, line in enumerate(lines):
    if re.search(pattern, line, re.IGNORECASE):
        print(f"Line {i+1}: {line.strip()}")
