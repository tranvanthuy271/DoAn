file_path = r'c:\Hub\DoAn\docs\ĐATN-slide.md'

with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Make final exact replacements
replacements = {
    "và *Hit/Dead* (nhận sát thương/chết giải phóng tài nguyên)": "và *Hit/Dead* nhận sát thương hoặc chết giải phóng tài nguyên",
    "Sơ đồ Cơ sở dữ liệu vật lý (ERD)": "Sơ đồ Cơ sở dữ liệu vật lý ERD",
    "mở cổng HTTP (80/443) nhận kết nối": "mở cổng HTTP 80 và 443 nhận kết nối",
    "số đợt quái (Wave 3/5)": "số đợt quái Wave 3 trên 5"
}

for old, new in replacements.items():
    content = content.replace(old, new)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)

print("Final parentheses cleanup completed!")
