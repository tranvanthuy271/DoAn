import os
import re

file_path = r'c:\Hub\DoAn\docs\ĐATN-slide.md'
docs_dir = r'c:\Hub\DoAn\docs'

with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Find all occurrences of image syntax ![alt](path) or <img src="path">
# e.g. ![...](path)
matches = re.findall(r'!\[.*?\]\((.*?)\)', content)

# Also check for HTML img tags
img_tags = re.findall(r'<img.*?src=["\'](.*?)["\']', content)

all_paths = list(set(matches + img_tags))

with open(r'c:\Hub\DoAn\scratch\image_existence.txt', 'w', encoding='utf-8') as f_out:
    f_out.write(f"Total unique images found: {len(all_paths)}\n\n")
    for p in sorted(all_paths):
        # Resolve relative path against docs_dir
        resolved = os.path.normpath(os.path.join(docs_dir, p))
        exists = os.path.exists(resolved)
        f_out.write(f"Path: {p}\n")
        f_out.write(f"  Resolved: {resolved}\n")
        f_out.write(f"  Exists: {exists}\n\n")
print("Done checking image existence!")
