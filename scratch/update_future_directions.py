file_path = r'c:\Hub\DoAn\docs\ĐATN-slide.md'

with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# We look for the last part of the file starting with '# Slide 31: Kết luận & Định hướng phát triển'
target = """# Slide 31: Kết luận & Định hướng phát triển"""
idx = content.find(target)
if idx == -1:
    print("Could not find target Slide 31!")
    sys.exit(1)

new_tail = """# Slide 31: Kết luận đề tài
## Kết quả đạt được & Hạn chế

<div style="display: flex; gap: 20px; align-items: center;">
<div style="flex: 1.2;">

- **Kết quả đạt được**:
  - Xây dựng nguyên mẫu game nhập vai 2D đa người chơi Mutants Arena hoạt động ổn định trên VPS Linux qua Docker Compose.
  - Triển khai thành công cơ chế Gene Ngũ Hành, dung hợp Hybrid Gene và Gene Tối Thượng tạo lối chơi có chiều sâu.
  - Đảm bảo độ trễ đồng bộ mạng dưới 100ms mượt mà và bảo mật Dedicated Server chống gian lận tài nguyên thành công.
- **Hạn chế**: Số lượng bản đồ và quái vật còn ít; giao diện đồ họa 2D pixel art cần được tối ưu bóng bẩy và sống động hơn.

</div>
<div style="flex: 0.8; text-align: center;">

![image_unnamed_49.png](../extracted_images/image_unnamed_49.png)

</div>
</div>

---


# Slide 32: Định hướng phát triển
## Kế hoạch phát triển & Nâng cấp hệ thống

- **Tối ưu hóa Đồng bộ & Quy mô mạng**: Áp dụng cơ chế quản lý mức độ quan tâm (Interest Management) để chỉ truyền gói tin đồng bộ trong tầm nhìn, giảm băng thông server và mở rộng quy mô đồng thời lên hàng ngàn người chơi.
- **Nâng cao Cơ chế Bảo mật & Chống gian lận**: Triển khai các thuật toán kiểm tra tính hợp lệ của lệnh điều khiển (Coyote time, Jump buffer verification) trên server và tích hợp Easy Anti-Cheat ở client để ngăn chặn triệt để hack/cheat.
- **Phát triển Chế độ chơi & AI Boss đa dạng**: Thiết kế đấu trường PvP xếp hạng trực tuyến, bổ sung các phó bản bang hội đột kích (Guild Raid Dungeons) với các Boss đa giai đoạn sử dụng Behavior Trees phức tạp hơn.
- **Hệ thống Kinh tế & Giao dịch trực tuyến**: Xây dựng hệ thống chợ giao dịch trực tuyến (Auction House) giữa người chơi thông qua SignalR Hub, đảm bảo đồng bộ tài nguyên và ngăn ngừa các lỗi nhân bản vật phẩm (dupe items).
- **Tự động hóa co giãn server Dedicated**: Triển khai hệ thống tự động co giãn Dedicated Server (Auto-scaling) trên cụm Kubernetes (K8s) dựa trên số lượng người chơi trực tuyến, tối ưu chi phí vận hành VPS.

---


# Slide 33: CẢM ƠN QUÝ THẦY CÔ VÀ CÁC BẠN ĐÃ LẮNG NGHE!
## Lời cảm ơn

- Sinh viên thực hiện: **Trần Văn Thủy**
- Mã sinh viên: **CT060439**
- Lớp: **AT16D**
- Người hướng dẫn khoa học: **TS. Nguyễn Đức Hiếu**
- Khoa Công nghệ thông tin – Học viện Kỹ thuật Mật mã
"""

updated_content = content[:idx] + new_tail

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(updated_content)

print("Slide tail successfully updated via python script!")
