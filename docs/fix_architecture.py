import os
import re

filepath = r"c:\Hub\DoAn\docs\CHUONG2_BAO_CAO.md"

if not os.path.exists(filepath):
    print(f"Error: {filepath} not found.")
    exit(1)

with open(filepath, 'r', encoding='utf-8', errors='ignore') as f:
    content = f.read()

# Define the replacement text
replacement = """Tầng Server (Game Server + API Server): Game Server là bản build Unity Dedicated Server — Unity runtime không có giao diện đồ họa, chạy trên Linux container. Server này là "nguồn sự thật" (source of truth) cho mọi trạng thái game, bao gồm tính toán sát thương thời gian thực với cơ chế chống gian lận (Server-Authoritative), quản lý di chuyển, spawn quái vật và điều khiển phó bản. API Server được phát triển bằng ASP.NET Core 7, đảm nhận nhiệm vụ xử lý các REST API xác thực tài khoản (JWT), nâng cấp Gene, Hybrid, lưu trữ dữ liệu nhân vật, và cung cấp SignalR Hub phục vụ tổ đội và chat. Hai thành phần server này giao tiếp nội bộ qua Docker network.

Tầng Dữ liệu (MySQL 8.0): MySQL lưu trữ toàn bộ dữ liệu game lâu dài như tài khoản, hồ sơ nhân vật, tiến trình Gene, trang bị, nhiệm vụ và các bảng cấu hình hệ thống. Mọi truy vấn từ API Server được xử lý an toàn thông qua Entity Framework Core 7 để ngăn chặn hoàn toàn nguy cơ SQL Injection.

Mô hình tổng thể và sự tương tác giữa các thành phần được minh họa qua sơ đồ kiến trúc dưới đây:

![Sơ đồ kiến trúc tổng thể hệ thống Mutants Arena](extracted_images/image8.png)

*Hình 2.1. Sơ đồ kiến trúc tổng thể hệ thống Mutants Arena (Unity Client — Game Server/API Server — MySQL)*

### 2.2.2. Biểu đồ Use Case tổng quát"""

# Use regex with DOTALL to match across newlines and ignore the corrupt character
pattern = r'Tầng Server \(Game Server \+ API Server\):.*?(### 2\.2\.2\. Biểu đồ Use Case tổng quát)'

new_content, count = re.subn(pattern, replacement, content, flags=re.DOTALL)

if count > 0:
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(new_content)
    print(f"Successfully fixed architecture corruption! Made {count} replacements.")
else:
    print("Error: Could not find the architecture section to replace!")
