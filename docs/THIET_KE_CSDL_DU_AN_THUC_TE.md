# 2.3 Thiết kế cơ sở dữ liệu

Trên cơ sở phân tích yêu cầu hệ thống game Mutants Arena và đối chiếu trực tiếp với schema triển khai trong tệp `gamedb.sql`, cơ sở dữ liệu của đề tài được xây dựng theo hướng kết hợp giữa các bảng quan hệ truyền thống và các trường dữ liệu JSON. Cách tổ chức này phù hợp với đặc thù của game multiplayer vì vừa bảo đảm tính chuẩn hóa ở các bảng cấu hình cốt lõi, vừa cho phép lưu trữ linh hoạt các trạng thái runtime như thông tin nhân vật, trang bị, túi đồ, buff đang hoạt động, tiến trình nhiệm vụ và dữ liệu phó bản.

Khác với mô hình ERD khái niệm chỉ mô tả quan hệ giữa vật phẩm, chỉ số và nhân vật ở mức tổng quát, schema thực tế của dự án tập trung vào bốn nhóm dữ liệu chính: dữ liệu tài khoản và hồ sơ người chơi, dữ liệu vật phẩm và nâng cấp trang bị, dữ liệu Gene và kỹ năng, cùng dữ liệu thế giới game gồm bản đồ, quái vật, nhiệm vụ và phó bản. Đây cũng là những nhóm bảng xuất hiện trực tiếp trong luồng xử lý của Unity Client và GameServerApi.

## 2.3.1 Sơ đồ kết nối các bảng

Dựa trên cấu trúc triển khai thực tế của dự án, các bảng dữ liệu trọng tâm được kết nối với nhau theo sơ đồ sau:

(Chèn hình từ file `docs/UseCase/erd_csdl_du_an_thuc_te.drawio` tại vị trí này)

Hình 2.20. Sơ đồ kết nối các bảng dữ liệu trọng tâm của dự án Mutants Arena

Từ sơ đồ trên có thể nhận thấy một số quan hệ chính như sau:

a) Bảng `users` liên kết 1 - 1 với bảng `player_data`, trong đó `users` lưu thông tin xác thực còn `player_data` lưu toàn bộ hồ sơ gameplay của nhân vật chính.

b) Bảng `friend_relations` liên kết đến `users` để quản lý quan hệ bạn bè giữa các tài khoản trong hệ thống.

c) Bảng `item_template` là bảng lõi của hệ thống vật phẩm. Từ bảng này, hệ thống tiếp tục liên kết sang `item_effect_template` để mô tả hiệu ứng tiêu hao, sang `equipment_upgrade_config` để cấu hình cường hóa trang bị và liên kết logic sang `player_data` để biểu diễn inventory, equipment và quick bag slot.

d) Bảng `option_template` dùng để cấu hình các dòng chỉ số của trang bị. Các option này không gắn cứng theo khóa ngoại truyền thống mà được ánh xạ động qua `strOptions` trong dữ liệu trang bị của người chơi.

e) Cụm bảng `gene_upgrade_config`, `gene_multi_config`, `gene_tier_stat_config`, `gene_hybrid_config`, `gene_hybrid_skill` và `skill_template` tạo thành khối dữ liệu đặc trưng cho hệ Gene Evolution và Hybrid Fusion của đề tài.

f) Cụm bảng `enemy`, `boss_config`, `map_config`, `quest_config`, `dungeon_config` và `dungeon_wave_config` mô tả nội dung PvE của game, bao gồm quái vật, boss, nhiệm vụ, bản đồ và cấu hình phó bản.

g) Nhiều liên hệ trong schema thực tế được triển khai thông qua các trường JSON như `info_char`, `inventory`, `equipment`, `skills`, `drop_items_json`, `reward_json`, `step` hoặc `levels_json`. Đây là điểm khác biệt quan trọng giữa hệ thống triển khai và ERD khái niệm truyền thống.

## 2.3.2 Cấu trúc các bảng trọng tâm

Trong phạm vi mục này, luận văn tập trung mô tả các bảng dữ liệu trọng tâm đang được sử dụng trực tiếp bởi hệ thống gameplay. Các cột thời gian hoặc cột kỹ thuật phụ trợ như `created_at`, `updated_at` vẫn tồn tại trong schema thật, nhưng chỉ được nhấn mạnh khi chúng có ảnh hưởng rõ rệt đến nghiệp vụ.

### 2.3.2.1 Bảng users

Bảng `users` dùng để lưu trữ thông tin tài khoản của người dùng. Đây là bảng gốc phục vụ quá trình đăng ký, đăng nhập và phát hành JWT.

Bảng 2.1. Bảng users

| STT | Tên cột | Kiểu dữ liệu | Mô tả |
|---|---|---|---|
| 1 | user_id | INT | Mã định danh duy nhất của tài khoản |
| 2 | username | VARCHAR(50) | Tên đăng nhập của người dùng |
| 3 | email | VARCHAR(100) | Địa chỉ email dùng để quản lý tài khoản |
| 4 | password_hash | VARCHAR(255) | Mật khẩu đã được băm bằng BCrypt |
| 5 | created_at | DATETIME | Thời điểm tạo tài khoản |
| 6 | last_login | DATETIME | Thời điểm đăng nhập gần nhất |

### 2.3.2.2 Bảng player_data

Bảng `player_data` là bảng trung tâm của toàn bộ hệ thống gameplay. Bảng này lưu dữ liệu nhân vật chính của người chơi, bao gồm chỉ số, trang bị, túi đồ, kỹ năng, tiềm năng và các buff đang hoạt động.

Bảng 2.2. Bảng player_data

| STT | Tên cột | Kiểu dữ liệu | Mô tả |
|---|---|---|---|
| 1 | player_id | INT | Khóa chính, đồng thời là khóa ngoại tham chiếu tới `users.user_id` |
| 2 | character_name | VARCHAR(50) | Tên hiển thị của nhân vật |
| 3 | gender | ENUM('Male','Female') | Giới tính nhân vật |
| 4 | info_char | LONGTEXT/JSON | Dữ liệu chỉ số tổng quát của nhân vật: level, gold, silver, element, gene, hp, mp, map, quest... |
| 5 | equipment | LONGTEXT/JSON | Dữ liệu trang bị đang mặc |
| 6 | inventory | LONGTEXT/JSON | Danh sách vật phẩm trong túi đồ |
| 7 | skills | LONGTEXT/JSON | Danh sách kỹ năng đã học và cấp kỹ năng |
| 8 | potential_stats | LONGTEXT/JSON | Chỉ số tiềm năng đã phân bổ |
| 9 | active_buffs | LONGTEXT/JSON | Danh sách buff đang còn hiệu lực |
| 10 | updated_at | DATETIME | Thời điểm cập nhật gần nhất của hồ sơ nhân vật |

### 2.3.2.3 Bảng friend_relations

Bảng `friend_relations` dùng để quản lý quan hệ bạn bè giữa các người chơi trong hệ thống.

Bảng 2.3. Bảng friend_relations

| STT | Tên cột | Kiểu dữ liệu | Mô tả |
|---|---|---|---|
| 1 | id | INT | Mã định danh của quan hệ bạn bè |
| 2 | user_id | INT | ID người gửi hoặc chủ thể của quan hệ |
| 3 | friend_id | INT | ID người chơi còn lại trong quan hệ bạn bè |
| 4 | status | VARCHAR(20) | Trạng thái lời mời như `pending` hoặc `accepted` |
| 5 | created_at | DATETIME | Thời điểm tạo quan hệ |

### 2.3.2.4 Bảng item_template

Bảng `item_template` dùng để định nghĩa toàn bộ mẫu vật phẩm trong game. Đây là bảng lõi cho các nhóm vật phẩm như trang bị, vũ khí, đá nâng cấp, potion, vật phẩm buff, nguyên liệu fusion, vé phó bản và túi mở rộng.

Bảng 2.4. Bảng item_template

| STT | Tên cột | Kiểu dữ liệu | Mô tả |
|---|---|---|---|
| 1 | id | INT | Mã định danh mẫu vật phẩm |
| 2 | name | VARCHAR(200) | Tên vật phẩm |
| 3 | detail | VARCHAR(500) | Mô tả ngắn của vật phẩm |
| 4 | isXepChong | VARCHAR(5) | Xác định vật phẩm có thể xếp chồng hay không |
| 5 | gioiTinh | TINYINT | Giới tính sử dụng: nam, nữ hoặc dùng chung |
| 6 | type | TINYINT | Phân loại vật phẩm như mũ, vũ khí, áo, đá cường hóa, potion, food, gene stone, material, wave ticket, bag expansion |
| 7 | idClass | TINYINT | Điều kiện hệ hoặc lớp nhân vật được sử dụng vật phẩm |
| 8 | idIcon | INT | Mã icon hiển thị trong Unity |
| 9 | levelNeed | SMALLINT | Cấp yêu cầu để sử dụng |
| 10 | taiPhuNeed | SMALLINT | Yêu cầu tài phú nếu có |
| 11 | idMob | INT | ID quái vật gắn logic rơi vật phẩm hoặc dữ liệu tham chiếu nội dung |
| 12 | idChar | INT | ID nhân vật hoặc cấu hình đặc thù nếu có |
| 13 | isLock | TINYINT | Trạng thái khóa vật phẩm |
| 14 | sellPrice | INT | Giá bán lại cho NPC theo đơn vị bạc |

### 2.3.2.5 Bảng item_effect_template

Bảng `item_effect_template` dùng để cấu hình hiệu ứng phát sinh khi người chơi sử dụng vật phẩm tiêu hao hoặc vật phẩm buff.

Bảng 2.5. Bảng item_effect_template

| STT | Tên cột | Kiểu dữ liệu | Mô tả |
|---|---|---|---|
| 1 | id | INT | Mã định danh hiệu ứng |
| 2 | item_template_id | INT | Khóa ngoại logic tới `item_template.id` |
| 3 | effect_type | VARCHAR(50) | Loại hiệu ứng như `HpRestore`, `MpRestore`, `HpBuff`, `AttackBuff`, `GeneExpBuff`, `ExpBuff` |
| 4 | value | INT | Giá trị tác động của hiệu ứng |
| 5 | duration_sec | INT | Thời gian tác dụng của buff; bằng 0 nếu là tác động tức thời |
| 6 | icon_id | INT | Mã icon hiện trên HUD buff |
| 7 | display_name | VARCHAR(200) | Tên hiển thị trong tooltip buff |
| 8 | detail | VARCHAR(500) | Mô tả chi tiết của hiệu ứng |
| 9 | sort_order | TINYINT | Thứ tự hiển thị khi một vật phẩm có nhiều hiệu ứng |

### 2.3.2.6 Bảng option_template

Bảng `option_template` dùng để định nghĩa các dòng chỉ số có thể gắn lên trang bị và vũ khí. Đây là bảng cốt lõi của cơ chế `strOptions` trong hệ thống trang bị.

Bảng 2.6. Bảng option_template

| STT | Tên cột | Kiểu dữ liệu | Mô tả |
|---|---|---|---|
| 1 | id | INT | Mã định danh option |
| 2 | name | VARCHAR(200) | Tên option, ký tự `#` đóng vai trò placeholder cho giá trị thực |
| 3 | type | TINYINT | Nhóm option theo loại hiển thị hoặc loại chỉ số |
| 4 | level | TINYINT | Cấp nâng tối thiểu của trang bị để kích hoạt option |
| 5 | strOption | LONGTEXT | Chuỗi 20 giá trị phân tách bằng dấu `;`, biểu diễn tiến trình tăng chỉ số theo cấp nâng |

### 2.3.2.7 Bảng equipment_upgrade_config

Bảng `equipment_upgrade_config` dùng để cấu hình chi phí và tỉ lệ thành công khi cường hóa trang bị.

Bảng 2.7. Bảng equipment_upgrade_config

| STT | Tên cột | Kiểu dữ liệu | Mô tả |
|---|---|---|---|
| 1 | upgrade_level | TINYINT | Mốc cấp cường hóa cần đạt, ví dụ từ +1 đến +24 |
| 2 | silver_cost | INT | Lượng bạc cần tiêu hao |
| 3 | stone_id | INT | ID loại đá cường hóa, liên hệ logic tới `item_template.id` |
| 4 | stone_needed | TINYINT | Số lượng đá cần để đạt tỉ lệ cơ sở |
| 5 | stone_min | TINYINT | Số đá tối thiểu được phép dùng |
| 6 | base_success_rate | FLOAT | Tỉ lệ thành công cơ bản |
| 7 | fail_policy | TINYINT | Chính sách khi thất bại: giữ nguyên, giảm bậc hoặc về +0 |

### 2.3.2.8 Bảng gene_upgrade_config

Bảng `gene_upgrade_config` dùng để cấu hình nâng cấp Gene chính theo từng hệ nguyên tố.

Bảng 2.8. Bảng gene_upgrade_config

| STT | Tên cột | Kiểu dữ liệu | Mô tả |
|---|---|---|---|
| 1 | tier_from | TINYINT | Tier hiện tại của Gene chính |
| 2 | element_type | VARCHAR(10) | Hệ Gene chính như Fire, Water, Earth, Metal, Wood, Wind |
| 3 | gene_exp_required | INT | Lượng Gene EXP cần để nâng cấp |
| 4 | silver_cost | INT | Lượng bạc tiêu hao |
| 5 | stone_id | INT | ID vật phẩm nguyên liệu nâng Gene |
| 6 | stone_needed | TINYINT | Số lượng đá cần cho tỉ lệ cơ sở |
| 7 | stone_min | TINYINT | Số lượng đá tối thiểu |
| 8 | base_success_rate | FLOAT | Tỉ lệ thành công cơ sở |

### 2.3.2.9 Bảng gene_multi_config

Bảng `gene_multi_config` dùng để cấu hình nâng cấp Gene phụ, tức hệ nguyên tố thứ hai của nhân vật.

Bảng 2.9. Bảng gene_multi_config

| STT | Tên cột | Kiểu dữ liệu | Mô tả |
|---|---|---|---|
| 1 | tier_from | TINYINT | Tier hiện tại của Gene phụ |
| 2 | element_type | VARCHAR(10) | Hệ Gene phụ |
| 3 | gene_exp_required | INT | EXP Gene phụ cần có để nâng cấp |
| 4 | silver_cost | INT | Bạc tiêu hao khi nâng |
| 5 | stone_id | INT | ID vật phẩm nguyên liệu |
| 6 | stone_needed | TINYINT | Số lượng đá chuẩn |
| 7 | stone_min | TINYINT | Số đá tối thiểu |
| 8 | base_success_rate | FLOAT | Tỉ lệ thành công cơ sở |

### 2.3.2.10 Bảng gene_tier_stat_config

Bảng `gene_tier_stat_config` dùng để quy định lượng chỉ số cộng thêm cho từng hệ Gene khi đạt tới một tier mới.

Bảng 2.10. Bảng gene_tier_stat_config

| STT | Tên cột | Kiểu dữ liệu | Mô tả |
|---|---|---|---|
| 1 | element_type | VARCHAR(10) | Hệ Gene được cấu hình |
| 2 | tier_to | TINYINT | Tier đạt được sau khi nâng |
| 3 | hp_bonus | INT | Chỉ số HP cộng thêm |
| 4 | mp_bonus | INT | Chỉ số MP cộng thêm |
| 5 | attack_bonus | INT | Chỉ số tấn công cộng thêm |
| 6 | defense_bonus | INT | Chỉ số phòng thủ cộng thêm |

### 2.3.2.11 Bảng gene_hybrid_config

Bảng `gene_hybrid_config` dùng để lưu cấu hình cho các tổ hợp Hybrid Gene. Đây là bảng thể hiện rõ tính đặc trưng của đề tài khi cho phép kết hợp hai hệ Gene tier cao thành một dạng lai.

Bảng 2.11. Bảng gene_hybrid_config

| STT | Tên cột | Kiểu dữ liệu | Mô tả |
|---|---|---|---|
| 1 | hybrid_id | INT | Mã định danh của tổ hợp Hybrid |
| 2 | element_a | VARCHAR(10) | Hệ nguyên tố thứ nhất |
| 3 | element_b | VARCHAR(10) | Hệ nguyên tố thứ hai |
| 4 | hybrid_name | VARCHAR(100) | Tên tổ hợp Hybrid |
| 5 | hybrid_description | VARCHAR(500) | Mô tả ngắn về vai trò chiến đấu của Hybrid |
| 6 | bonus_target_elements | VARCHAR(100) | Các hệ nhận bonus khi dùng Hybrid này |
| 7 | immune_elements | VARCHAR(100) | Các hệ mà Hybrid có khả năng kháng hoặc miễn nhiễm |
| 8 | fusion_silver_cost | INT | Chi phí bạc để thực hiện fusion |
| 9 | fusion_item_id | INT | ID vật phẩm cần dùng để fusion |
| 10 | fusion_item_count | INT | Số lượng vật phẩm fusion cần tiêu hao |
| 11 | atk_bonus_percent | FLOAT | Tỉ lệ cộng thêm sát thương khi fusion thành công |
| 12 | stat_bonus_hp | INT | HP cộng thêm từ Hybrid |
| 13 | stat_bonus_mp | INT | MP cộng thêm từ Hybrid |
| 14 | stat_bonus_atk | INT | Tấn công cộng thêm từ Hybrid |
| 15 | stat_bonus_def | INT | Phòng thủ cộng thêm từ Hybrid |
| 16 | prefab_path | VARCHAR(200) | Đường dẫn prefab nhân vật Hybrid trong Unity |
| 17 | primary_skill_keep_count | INT | Số kỹ năng của hệ chính được giữ lại sau khi fusion |

### 2.3.2.12 Bảng gene_hybrid_skill

Bảng `gene_hybrid_skill` dùng để ánh xạ mỗi Hybrid Gene với kỹ năng đặc biệt tương ứng.

Bảng 2.12. Bảng gene_hybrid_skill

| STT | Tên cột | Kiểu dữ liệu | Mô tả |
|---|---|---|---|
| 1 | id | INT | Mã định danh bản ghi ánh xạ |
| 2 | hybrid_id | INT | ID Hybrid, liên hệ tới `gene_hybrid_config.hybrid_id` |
| 3 | skill_code | VARCHAR(50) | Mã kỹ năng đặc biệt, khớp với `skill_template.skill_code` |
| 4 | slot_priority | INT | Vị trí ưu tiên của kỹ năng trong hotbar |

### 2.3.2.13 Bảng skill_template

Bảng `skill_template` dùng để lưu cấu hình toàn bộ kỹ năng trong game, bao gồm kỹ năng thường, kỹ năng nguyên tố và kỹ năng Hybrid.

Bảng 2.13. Bảng skill_template

| STT | Tên cột | Kiểu dữ liệu | Mô tả |
|---|---|---|---|
| 1 | skill_id | INT | Mã định danh kỹ năng |
| 2 | skill_code | VARCHAR(50) | Mã kỹ năng duy nhất dùng trong gameplay và đồng bộ mạng |
| 3 | skill_name | VARCHAR(100) | Tên kỹ năng |
| 4 | description | TEXT | Mô tả kỹ năng |
| 5 | element_type | VARCHAR(20) | Hệ nguyên tố của kỹ năng |
| 6 | max_level | INT | Cấp tối đa của kỹ năng |
| 7 | level_to_unlock | INT | Cấp người chơi cần đạt để mở kỹ năng |
| 8 | levels_json | LONGTEXT/JSON | Dữ liệu chi tiết của từng cấp kỹ năng |
| 9 | icon_id | VARCHAR(100) | Mã icon hiển thị trên UI |
| 10 | gene_tier_required | INT | Tier Gene tối thiểu để dùng kỹ năng |
| 11 | hybrid_id | INT | ID Hybrid liên quan nếu đây là kỹ năng lai |

### 2.3.2.14 Bảng enemy

Bảng `enemy` dùng để lưu cấu hình quái vật và boss trong toàn bộ hệ thống PvE. Đây là một trong những bảng có phạm vi dữ liệu rộng nhất vì chứa cả chỉ số chiến đấu, phần thưởng, kỹ năng và các tham số kháng nguyên tố.

Bảng 2.14. Bảng enemy

| STT | Tên cột | Kiểu dữ liệu | Mô tả |
|---|---|---|---|
| 1 | enemy_id | INT | Mã định danh quái vật |
| 2 | enemy_name | VARCHAR(50) | Tên quái vật |
| 3 | enemy_description | TEXT | Mô tả ngắn |
| 4 | level | INT | Cấp độ quái vật |
| 5 | base_hp | INT | Máu cơ bản |
| 6 | base_mp | INT | Năng lượng cơ bản |
| 7 | base_damage | INT | Sát thương cơ bản |
| 8 | base_defense | INT | Phòng thủ cơ bản |
| 9 | move_speed | FLOAT | Tốc độ di chuyển |
| 10 | attack_speed | FLOAT | Tốc độ tấn công |
| 11 | exp_reward | INT | Kinh nghiệm thưởng khi tiêu diệt |
| 12 | gold_reward | INT | Vàng thưởng |
| 13 | silver_reward | INT | Bạc thưởng |
| 14 | drop_items_json | LONGTEXT/JSON | Danh sách vật phẩm rơi ra |
| 15 | element_type | VARCHAR(20) | Hệ nguyên tố chính của quái |
| 16 | enemy_type | ENUM | Phân loại `Normal`, `Elite`, `Boss` |
| 17 | skills_json | LONGTEXT/JSON | Danh sách kỹ năng của quái |
| 18 | khang_hoa | INT | Kháng nguyên tố Hỏa |
| 19 | khang_thuy | INT | Kháng nguyên tố Thủy |
| 20 | khang_tho | INT | Kháng nguyên tố Thổ |
| 21 | khang_moc | INT | Kháng nguyên tố Mộc |
| 22 | khang_kim | INT | Kháng nguyên tố Kim |
| 23 | khang_phong | INT | Kháng nguyên tố Phong |
| 24 | tang_dame_hoa | INT | Tăng sát thương Hỏa |
| 25 | tang_dame_thuy | INT | Tăng sát thương Thủy |
| 26 | tang_dame_tho | INT | Tăng sát thương Thổ |
| 27 | tang_dame_moc | INT | Tăng sát thương Mộc |
| 28 | tang_dame_kim | INT | Tăng sát thương Kim |
| 29 | tang_dame_phong | INT | Tăng sát thương Phong |
| 30 | hp_regen_per_sec | INT | Hồi máu theo giây |
| 31 | evasion_rate | INT | Tỉ lệ né tránh |
| 32 | counter_rate | INT | Tỉ lệ phản đòn |
| 33 | phases_json | LONGTEXT/JSON | Dữ liệu phase của boss |

### 2.3.2.15 Bảng boss_config

Bảng `boss_config` dùng để lưu dữ liệu xuất hiện boss trên bản đồ thế giới.

Bảng 2.15. Bảng boss_config

| STT | Tên cột | Kiểu dữ liệu | Mô tả |
|---|---|---|---|
| 1 | boss_id | INT | ID boss, liên hệ tới `enemy.enemy_id` |
| 2 | map_id | INT | ID bản đồ xuất hiện boss |
| 3 | spawn_x | FLOAT | Tọa độ X sinh boss |
| 4 | spawn_y | FLOAT | Tọa độ Y sinh boss |
| 5 | min_spawn_hour | INT | Giờ bắt đầu cho phép spawn |
| 6 | max_spawn_hour | INT | Giờ kết thúc cho phép spawn |
| 7 | respawn_minutes | INT | Thời gian hồi sinh boss |
| 8 | is_active | TINYINT | Cờ bật hoặc tắt boss |

### 2.3.2.16 Bảng map_config

Bảng `map_config` dùng để lưu cấu hình các bản đồ trong game, bao gồm scene tương ứng, điểm xuất hiện và điều kiện truy cập.

Bảng 2.16. Bảng map_config

| STT | Tên cột | Kiểu dữ liệu | Mô tả |
|---|---|---|---|
| 1 | map_id | INT | Mã bản đồ |
| 2 | map_name | VARCHAR(100) | Tên bản đồ |
| 3 | scene_name | VARCHAR(100) | Tên scene trong Unity |
| 4 | spawn_points_json | TEXT/JSON | Danh sách điểm sinh của nhân vật |
| 5 | min_level | INT | Cấp tối thiểu để vào bản đồ |
| 6 | max_level | INT | Cấp tối đa phù hợp với bản đồ |
| 7 | required_quest_id | INT | ID nhiệm vụ cần hoàn thành trước khi truy cập, nếu có |
| 8 | updated_at | DATETIME | Thời điểm cập nhật cấu hình gần nhất |

### 2.3.2.17 Bảng quest_config

Bảng `quest_config` dùng để cấu hình hệ thống nhiệm vụ. Mỗi bản ghi mô tả NPC giao nhiệm vụ, lời thoại, phần thưởng và chuỗi bước cần hoàn thành.

Bảng 2.17. Bảng quest_config

| STT | Tên cột | Kiểu dữ liệu | Mô tả |
|---|---|---|---|
| 1 | id | INT | Mã nhiệm vụ |
| 2 | name | VARCHAR(200) | Tên nhiệm vụ |
| 3 | level_need | INT | Cấp độ yêu cầu để nhận nhiệm vụ |
| 4 | npc_id | INT | NPC giao và nhận hoàn thành nhiệm vụ |
| 5 | str1 | TEXT | Hội thoại lúc nhận nhiệm vụ |
| 6 | str2 | TEXT | Hội thoại lúc hoàn thành nhiệm vụ |
| 7 | str3 | TEXT | Ghi chú hoặc hướng dẫn thêm |
| 8 | exp_reward | INT | Kinh nghiệm thưởng |
| 9 | gold_reward | INT | Vàng thưởng |
| 10 | silver_reward | INT | Bạc thưởng |
| 11 | item_reward | VARCHAR(500) | Danh sách vật phẩm thưởng |
| 12 | step | LONGTEXT/JSON | Danh sách bước nhiệm vụ |
| 13 | sort_order | INT | Thứ tự sắp xếp nhiệm vụ |
| 14 | is_active | TINYINT | Trạng thái kích hoạt nhiệm vụ |

### 2.3.2.18 Bảng dungeon_config

Bảng `dungeon_config` dùng để lưu cấu hình phó bản của game, bao gồm phó bản solo và phó bản tổ đội.

Bảng 2.18. Bảng dungeon_config

| STT | Tên cột | Kiểu dữ liệu | Mô tả |
|---|---|---|---|
| 1 | dungeon_id | INT | Mã phó bản |
| 2 | dungeon_name | VARCHAR(100) | Tên phó bản |
| 3 | dungeon_type | ENUM('solo','multi') | Loại phó bản |
| 4 | map_id | INT | ID bản đồ gắn với phó bản |
| 5 | scene_name | VARCHAR(100) | Scene chạy phó bản trong Unity |
| 6 | max_players | INT | Số người chơi tối đa |
| 7 | min_level_required | INT | Cấp tối thiểu để vào phó bản |
| 8 | time_limit_seconds | INT | Thời gian giới hạn của phó bản |
| 9 | description | TEXT | Mô tả phó bản |
| 10 | boss_enemy_id | INT | ID boss của phó bản nếu có |
| 11 | reward_json | LONGTEXT/JSON | Cấu hình phần thưởng |
| 12 | thumbnail_icon_id | VARCHAR(50) | Icon đại diện của phó bản |
| 13 | is_active | TINYINT | Trạng thái kích hoạt |
| 14 | created_at | DATETIME | Thời điểm tạo cấu hình |
| 15 | updated_at | DATETIME | Thời điểm cập nhật gần nhất |

### 2.3.2.19 Bảng dungeon_wave_config

Bảng `dungeon_wave_config` dùng để lưu cấu hình chi tiết cho phó bản dạng wave. Bảng này hoạt động như phần mở rộng của `dungeon_config`.

Bảng 2.19. Bảng dungeon_wave_config

| STT | Tên cột | Kiểu dữ liệu | Mô tả |
|---|---|---|---|
| 1 | dungeon_id | INT | Mã phó bản, đồng thời liên hệ tới `dungeon_config.dungeon_id` |
| 2 | max_waves | INT | Số wave tối đa |
| 3 | wave_time_seconds | INT | Thời gian của mỗi wave |
| 4 | enemy_scale_percent | FLOAT | Tỉ lệ tăng chỉ số quái theo từng wave |
| 5 | boss_scale_percent | FLOAT | Tỉ lệ tăng chỉ số boss theo từng wave |
| 6 | exp_gold_scale_percent | FLOAT | Tỉ lệ tăng phần thưởng EXP và vàng |
| 7 | daily_entry_limit | INT | Số lượt vào tối đa mỗi ngày |
| 8 | entry_item_plus1_id | INT | Vé cộng thêm 1 lượt vào |
| 9 | entry_item_plus2_id | INT | Vé cộng thêm 2 lượt vào |
| 10 | milestone_reward_json | LONGTEXT/JSON | Phần thưởng mốc theo từng wave |

## 2.3.3 Nhận xét

Thiết kế cơ sở dữ liệu của dự án Mutants Arena cho thấy hệ thống được tổ chức theo hướng lai giữa mô hình quan hệ và mô hình lưu trữ bán cấu trúc. Các bảng cấu hình như `item_template`, `item_effect_template`, `option_template`, `gene_upgrade_config`, `gene_hybrid_config`, `enemy`, `map_config`, `dungeon_config` và `quest_config` giúp hệ thống duy trì được tính chuẩn hóa ở phần dữ liệu nghiệp vụ cốt lõi. Trong khi đó, các trường JSON trong `player_data`, `enemy`, `skill_template`, `quest_config` hay `dungeon_wave_config` giúp hệ thống thích nghi tốt với đặc thù runtime của game multiplayer, nơi trạng thái thay đổi liên tục và khó biểu diễn hiệu quả nếu tách quá nhỏ thành nhiều bảng quan hệ.

Điểm nổi bật nhất của cơ sở dữ liệu này là phần dữ liệu Gene Evolution và Hybrid Fusion được thiết kế thành một cụm cấu hình riêng, phản ánh đúng tính mới và tính đặc trưng của đồ án. Ngoài ra, hệ thống vật phẩm, nâng cấp trang bị và phó bản cũng được cấu hình đủ linh hoạt để có thể mở rộng nội dung mà không cần thay đổi nhiều mã nguồn. Đây là một hướng thiết kế phù hợp với dự án game nhập vai hành động nhiều người chơi có yêu cầu vừa ổn định trong vận hành, vừa dễ mở rộng ở giai đoạn phát triển tiếp theo.