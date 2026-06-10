import re
import sys

sys.stdout.reconfigure(encoding='utf-8')

file_path = r"c:\Hub\DoAn\docs\CHUONG3_BAO_CAO_VIET_LAI.md"
with open(file_path, "r", encoding="utf-8", errors="ignore") as f:
    lines = f.readlines()

print(f"Read {len(lines)} lines from {file_path}")

# Print lines that look like headings or figure references
for idx, line in enumerate(lines):
    if "hình" in line.lower() or "image" in line.lower() or "[" in line or "]" in line:
        # Only print first 30 matches
        print(f"Line {idx+1}: {line.strip()[:100]}")
