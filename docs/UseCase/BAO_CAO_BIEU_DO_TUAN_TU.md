# 2.2 Các biểu đồ tuần tự

Căn cứ vào các kịch bản Use case trọng tâm đã được đặc tả trong hệ thống Mutants Arena, phần này trình bày các biểu đồ tuần tự cho những luồng gameplay cốt lõi đang được triển khai thực tế trong dự án DoAn. Các biểu đồ được xây dựng bám sát kiến trúc client Unity, gameplay server, ASP.NET Core Web API và cơ sở dữ liệu MySQL, qua đó phản ánh rõ thứ tự tương tác giữa người chơi, giao diện, lớp xử lý nghiệp vụ và dữ liệu lưu trữ trong từng chức năng chính.

## 2.2.1 Biểu đồ tuần tự Chiến đấu và sử dụng kỹ năng

### 2.2.1.1 Biểu đồ tuần tự Chiến đấu và sử dụng kỹ năng

Căn cứ vào kịch bản Use case UC04 - Chiến đấu và sử dụng kỹ năng, ta xây dựng các bước thực hiện của hệ thống với chức năng chiến đấu thời gian thực bằng biểu đồ tuần tự. Trong luồng này, người chơi chọn mục tiêu hoặc kích hoạt đòn đánh thường, kỹ năng trên thanh Q/W/E/R; client chiến đấu gửi yêu cầu lên gameplay server; gameplay server tiếp tục kiểm tra điều kiện cooldown, MP, hitbox và trạng thái khóa thao tác trước khi chuyển sang lớp CombatResolver hoặc đối tượng địch để tính sát thương.

Sau khi yêu cầu hợp lệ, hệ thống áp dụng DamageResult, cập nhật HP, buff, debuff và đồng bộ kết quả cho client điều khiển lẫn client quan sát bằng cơ chế server-authoritative. Khi mục tiêu bị tiêu diệt, hệ thống tiếp tục kích hoạt luồng phát EXP, vật phẩm rơi và các hook nhiệm vụ liên quan. Trong cùng biểu đồ, các tuần tự thất bại cũng được mô tả rõ cho trường hợp kỹ năng đang hồi chiêu, không đủ MP, mục tiêu ngoài phạm vi, trượt hitbox hoặc nhân vật đã chết.

Hình 2.13. Biểu đồ tuần tự Chiến đấu và sử dụng kỹ năng

## 2.2.2 Biểu đồ tuần tự Nâng cấp trang bị

### 2.2.2.1 Biểu đồ tuần tự Nâng cấp trang bị tại Blacksmith

Căn cứ vào kịch bản Use case UC06 - Nâng cấp trang bị, ta xây dựng các bước thực hiện của hệ thống với chức năng cường hóa trang bị tại Blacksmith bằng biểu đồ tuần tự. Luồng này bắt đầu khi người chơi mở giao diện Blacksmith, chọn trang bị mục tiêu và nạp cấu hình nâng cấp hiện tại từ hệ thống. Tại đây, hệ thống truy vấn inventory, equipment và cấu hình nâng cấp theo bậc hiện tại để trả về các tham số như `stoneId`, `stoneNeeded`, `success rate`, `failPolicy` và `upgradeLevel`.

Sau khi người chơi chọn đúng trang bị và các slot đá, charm tương ứng, client gửi yêu cầu nâng cấp kèm thông tin `slotIndex + count` để hệ thống kiểm tra số lượng vật liệu trên từng stack, phòng chống gian lận số lượng khi dùng vật liệu dạng chồng. Nếu hợp lệ, hệ thống trừ bạc, tiêu hao vật liệu, tính kết quả theo `failPolicy`, cập nhật `upgradeLevel`, `strOptions`, inventory và equipment. Biểu đồ đồng thời thể hiện rõ hai nhánh thất bại chính: thất bại xác thực do thiếu bạc, thiếu vật liệu, sai slot stack hoặc chạm mốc +24; và thất bại theo tỷ lệ, trong đó hệ thống giữ nguyên hoặc làm tụt cấp trang bị đúng theo cấu hình rủi ro đang dùng trong dự án.

Hình 2.14. Biểu đồ tuần tự Nâng cấp trang bị tại Blacksmith

## 2.2.3 Biểu đồ tuần tự Phát triển Gene và Hybrid

### 2.2.3.1 Biểu đồ tuần tự Nâng Gene chính và Gene phụ

Căn cứ vào kịch bản Use case UC07 - Phát triển Gene và Hybrid, ta xây dựng các bước thực hiện của hệ thống với chức năng nâng Gene chính và Gene phụ bằng biểu đồ tuần tự. Ở giai đoạn Gene chính, người chơi mở giao diện Gene Evolution, nạp `player_data` và cấu hình trong `gene_upgrade_config`, sau đó xác nhận yêu cầu nâng cấp. Hệ thống kiểm tra `gene_exp`, bạc, vật liệu và giới hạn Tier trước khi quyết định kết quả nâng cấp; nếu thành công, hệ thống lưu Tier mới, `final_stats`, danh sách kỹ năng mở khóa và đồng bộ lại giao diện Gene cho người chơi.

Đối với Gene phụ, biểu đồ mô tả rõ quá trình chọn hệ phụ lần đầu, kiểm tra cặp hệ cố định đã triển khai trong dự án gồm Hỏa↔Thổ, Thủy↔Mộc và Kim↔Phong, sau đó nạp cấu hình từ `gene_multi_config` để thực hiện nâng hệ phụ. Nếu Gene phụ được nâng thành công, hệ thống cập nhật `secondaryElement`, `secondary_gene_tier`, bonus chỉ số theo hệ số giảm so với Gene chính, đồng thời bật cờ `canFuse` khi cả hai hệ đã đạt điều kiện hợp lệ. Các luồng phụ thất bại cũng được biểu diễn đầy đủ cho trường hợp thiếu `gene_exp`, thiếu bạc, thiếu vật liệu, Gene chính đã đạt Tier tối đa, chọn sai cặp hệ hoặc Gene phụ đã bị khóa trước đó.

Hình 2.15. Biểu đồ tuần tự Nâng Gene chính và Gene phụ

### 2.2.3.2 Biểu đồ tuần tự Dung hợp Hybrid Gene

Căn cứ vào kịch bản Use case UC07 - Phát triển Gene và Hybrid, ta tiếp tục xây dựng các bước thực hiện của hệ thống với chức năng dung hợp Hybrid Gene bằng biểu đồ tuần tự. Luồng này bắt đầu khi người chơi mở tab Hybrid Fusion, yêu cầu nạp điều kiện thực hiện và hệ thống truy vấn các bảng `gene_hybrid_config`, `gene_hybrid_skill` cùng dữ liệu hiện thời trong `player_data`. Từ đó, giao diện nhận về các thông tin như số lượng Fusion Core, `gold cost`, bonus, prefab và bộ kỹ năng Hybrid tương ứng để hiển thị cho người chơi xác nhận.

Khi người chơi gửi yêu cầu dung hợp, hệ thống kiểm tra đồng thời các điều kiện quan trọng gồm trạng thái `isHybrid`, Tier của Gene chính và Gene phụ, tính hợp lệ của cặp hệ, số lượng Fusion Core và lượng vàng hiện có. Nếu dung hợp thành công, hệ thống lưu `HybridId`, `prefab`, các `immune elements`, bonus chiến đấu và bộ kỹ năng Hybrid mới vào dữ liệu nhân vật, sau đó đồng bộ lại `final_stats` và giao diện Hybrid cho client. Biểu đồ cũng thể hiện rõ hai nhóm điều kiện thất bại: chưa đủ Tier 5 hoặc cặp hệ không hợp lệ; thiếu Fusion Core, thiếu vàng hoặc nhân vật đã là Hybrid trước đó.

Hình 2.16. Biểu đồ tuần tự Dung hợp Hybrid Gene

## 2.2.4 Biểu đồ tuần tự Tham gia và hoàn tất phó bản

### 2.2.4.1 Biểu đồ tuần tự Tham gia và hoàn tất phó bản

Căn cứ vào kịch bản Use case UC13 - Tham gia và hoàn tất phó bản, ta xây dựng các bước thực hiện của hệ thống với chức năng tham gia dungeon, chiến đấu qua các wave và nhận thưởng hoàn tất bằng biểu đồ tuần tự. Luồng này bắt đầu khi người chơi chọn loại phó bản hoặc portal dungeon; hệ thống tiếp nhận yêu cầu và kiểm tra level, lượt vào cùng trạng thái tổ đội trước khi truy vấn `player_data`, `dungeon_config` và `dungeon_wave_config`. Nếu điều kiện hợp lệ, gameplay server khởi tạo phiên dungeon, spawn wave đầu, nạp boss và chuyển người chơi vào map phó bản.

Trong giai đoạn xử lý chính, gameplay server điều khiển toàn bộ vòng lặp chiến đấu qua wave, theo dõi tiến độ, spawn wave tiếp theo và boss cuối, sau đó tổng kết kết quả cho từng người chơi hoặc cả tổ đội khi đạt điều kiện chiến thắng. Ở bước kết thúc, hệ thống gọi xử lý phát thưởng, cập nhật inventory, reward, log và dữ liệu `dungeon_best_waves`, rồi đồng bộ EXP và phần thưởng cho client trước khi đưa người chơi trở về map an toàn. Biểu đồ cũng mô tả rõ các trường hợp ngoại lệ của dự án như người chơi hoặc tổ đội thất bại, hết thời gian, bị hạ gục toàn bộ nên không nhận thưởng hoàn tất; hoặc máy chủ gặp lỗi spawn ở một wave và phải xử lý theo cấu hình fallback của dungeon.

Hình 2.17. Biểu đồ tuần tự Tham gia và hoàn tất phó bản