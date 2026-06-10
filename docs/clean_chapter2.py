import re

file_path = 'docs/CHUONG2_BAO_CAO.md'

with open(file_path, 'r', encoding='utf-8', errors='replace') as f:
    content = f.read()

# 1. Clean up leftover duplicate tables
# Find the start of the corrupt text: "i qua `POST /api/friends/request`"
start_marker = "i qua `POST /api/friends/request`"
# Find the end of the corrupt text: the table row for UC20 ending with:
# "inventory reward của player được cập nhật đúng với thao tác vận hành cuối cùng. |"
end_marker = "inventory reward của player được cập nhật đúng với thao tác vận hành cuối cùng. |"

start_idx = content.find(start_marker)
end_idx = content.find(end_marker)

if start_idx != -1 and end_idx != -1:
    print(f"Found leftover block from index {start_idx} to {end_idx}")
    end_idx += len(end_marker)
    # Remove the block and replace with a newline
    content = content[:start_idx] + "\n" + content[end_idx:]
    print("Successfully removed the leftover block.")
else:
    print("Warning: Leftover markers not found. Checking alternate search...")
    # Alternate fallback search by table row
    pattern = r"i qua `POST /api/friends/request`[\s\S]+?với thao tác vận hành cuối cùng\. \|"
    content, count = re.subn(pattern, "\n", content)
    print(f"Regex replacement replaced {count} occurrences.")

# 2. Update summary section
old_summary_block = """Chương 2 đã được chuẩn hóa lại theo đúng trạng thái triển khai thực tế của dự án Mutants Arena thay vì chỉ dừng ở mức mô tả ý tưởng. Trên cơ sở đọc trực tiếp mã nguồn API, SignalR Hub và client Unity, hệ thống được xác định có bốn actor chính gồm Guest, Player, Admin/Operator và Máy chủ gameplay / Netcode server. Từ đó, các yêu cầu chức năng được tái cấu trúc thành các nhóm triển khai thật: xác thực, nhân vật, bản đồ, combat, Gene Evolution, inventory/equipment, NPC/quest/shop, social, dungeon, đồng bộ thời gian thực và vận hành server.

Phần Use Case là trọng tâm được viết lại toàn diện với 20 use case đầy đủ, bao phủ toàn bộ tính năng đã có trong mã nguồn: từ đăng ký, đăng nhập, tạo nhân vật, combat, Gene chính/Gene phụ/Hybrid Fusion, blacksmith, NPC service, quest event-driven, friend, party, chat, dungeon, leaderboard đến các luồng vận hành kỹ thuật như zone heartbeat, host runtime, spawn config và dungeon reward grant. Nhờ đó, chương 2 không chỉ mô tả đầy đủ nghiệp vụ của hệ thống mà còn tạo được sự liên kết rõ ràng giữa kiến trúc cài đặt, sơ đồ phân tích và đặc tả chức năng trong báo cáo."""

new_summary_block = """Chương 2 đã được chuẩn hóa lại theo đúng trạng thái triển khai thực tế của dự án Mutants Arena thay vì chỉ dừng ở mức mô tả ý tưởng. Trên cơ sở đọc trực tiếp mã nguồn API, SignalR Hub và client Unity, hệ thống được xác định có ba tác nhân chính gồm Guest (Khách), Player (Người chơi) và Gameplay Server (Zone Server/Dungeon Host). Từ đó, các yêu cầu chức năng được tái cấu trúc thành các nhóm nghiệp vụ triển khai thật: Tài khoản và gameplay, Phát triển nhân vật, Tương tác và hoạt động, Vận hành kỹ thuật.

Phần Use Case là trọng tâm được viết lại toàn diện với 16 ca sử dụng (Use Cases) chuẩn UML nghiệp vụ và đồng bộ tuyệt đối với mã nguồn: từ đăng ký, đăng nhập, di chuyển bản đồ, chiến đấu, Gene chính/Gene phụ/dung hợp Hybrid Gene, blacksmith, NPC service, quest event-driven, friend, party, chat, dungeon, leaderboard đến các luồng vận hành kỹ thuật như zone heartbeat, host map và dungeon reward grant. Nhờ đó, chương 2 không chỉ mô tả chính xác nghiệp vụ của hệ thống mà còn tạo được sự liên kết chặt chẽ giữa kiến trúc cài đặt, sơ đồ phân tích và đặc tả chức năng trong báo cáo tốt nghiệp."""

if old_summary_block in content:
    content = content.replace(old_summary_block, new_summary_block)
    print("Successfully updated the summary block.")
else:
    # Try substring replacement for robust match
    content = content.replace("bốn actor chính gồm Guest, Player, Admin/Operator và Máy chủ gameplay / Netcode server", "ba tác nhân chính gồm Guest (Khách), Player (Người chơi) và Gameplay Server (Zone Server/Dungeon Host)")
    content = content.replace("20 use case đầy đủ, bao phủ toàn bộ tính năng đã có trong mã nguồn", "16 ca sử dụng (Use Cases) chuẩn UML nghiệp vụ và đồng bộ tuyệt đối với mã nguồn")
    print("Applied fallback substring replacements for summary block.")

# Write back in UTF-8
with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)

print("Done clean-up successfully!")
