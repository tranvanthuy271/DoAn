import re

file_path = r'c:\Hub\DoAn\docs\ĐATN-slide.md'

with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

slides = content.split('\n\n---\n\n')
print("Current slides count:", len(slides))

with open(r'c:\Hub\DoAn\scratch\inspect_titles.txt', 'w', encoding='utf-8') as f_out:
    for idx, s in enumerate(slides):
        first_lines = [l.strip() for l in s.split('\n') if l.strip()]
        title = first_lines[0] if first_lines else "Empty"
        f_out.write(f"{idx+1}: {title}\n")
print("Titles saved!")
