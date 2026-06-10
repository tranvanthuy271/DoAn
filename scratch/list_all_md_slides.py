with open(r'c:\Hub\DoAn\docs\ĐATN-slide.md', 'r', encoding='utf-8') as f:
    content = f.read()

slides = content.split('\n---\n')
with open(r'c:\Hub\DoAn\scratch\list_all_md_slides.txt', 'w', encoding='utf-8') as f_out:
    for idx, s in enumerate(slides):
        title = ""
        for line in s.split('\n'):
            if line.strip().startswith('# Slide'):
                title = line.strip()
                break
        f_out.write(f"Slide {idx+1}: {title}\n")
print("Done listing!")
