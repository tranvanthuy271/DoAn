import re

file_path = r'c:\Hub\DoAn\docs\ĐATN-slide.md'

with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

slides = content.split('\n\n---\n\n')
print("Original slides count:", len(slides))

# The new slide content
new_slide = """# Slide 9: Kiến trúc mạng Server-Authoritative
## Nguyên lý vận hành & Luồng xử lý dữ liệu

- **Nguyên lý Server-Authoritative**: Dedicated Server chạy độc lập trên máy chủ ảo là nguồn dữ liệu duy nhất đáng tin cậy. Client chỉ gửi lệnh điều khiển và nhận dữ liệu trạng thái đồng bộ về.
- **Luồng xử lý hành động**:
  1. Client gửi yêu cầu điều khiển thông qua ServerRpc lên Server.
  2. Server thực hiện tính toán vật lý, kiểm tra tính hợp lệ như năng lượng, thời gian hồi chiêu và va chạm hitbox.
  3. Server cập nhật chỉ số máu và vị trí nhân vật, sau đó phát gói tin ClientRpc đồng bộ tới các client trong vùng.
- **Khả năng chống gian lận**: Client không thể tự ý thay đổi máu, vàng, sát thương hoặc bay nhảy tự do vì Server liên tục xác thực mọi hành vi."""

# Insert after index 7 (Slide 8)
slides.insert(8, new_slide)
print("Slides count after insertion:", len(slides))

# Renumber slide titles
# Each slide header starts with '# Slide X:'
# Let's iterate through all slides and correct the header number to idx + 1
for idx, s in enumerate(slides):
    lines = s.split('\n')
    for line_idx, line in enumerate(lines):
        if line.strip().startswith('# Slide '):
            # Replace '# Slide X:' with '# Slide {idx+1}:'
            new_line = re.sub(r'^# Slide \d+:', f'# Slide {idx+1}:', line)
            lines[line_idx] = new_line
            break # only replace the first occurrences (the slide header)
    slides[idx] = '\n'.join(lines)

# Re-join
new_content = '\n\n---\n\n'.join(slides)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(new_content)

print("Slide inserted and all subsequent slides successfully renumbered!")
