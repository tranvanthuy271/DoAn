# BÁO CÁO RÀ SOÁT DỮ LIỆU GIẢ VỜ VÀ BẢN MẪU (PLACEHOLDERS)
**Tài liệu rà soát**: `DoAn.docx`
**Ngày thực hiện**: 02/06/2026
**Phạm vi kiểm tra**: Toàn bộ nội dung văn bản và bảng biểu trong tài liệu chính nhằm phát hiện thông tin mẫu, tên giả định, ký tự chờ nhập hoặc các nội dung chưa hoàn chỉnh cần bổ sung trước khi nộp đồ án.

---

## 1. Tóm tắt kết quả
- **Tổng số vị trí phát hiện mẫu nghi vấn**: 42 mục.
- **Mức độ ảnh hưởng**:
  > [!IMPORTANT]
  > Hầu hết các dữ liệu giả lập (như "Nguyễn Văn A", "MSSV", "202x") nằm ở các trang bìa phụ, trang điền thông tin cá nhân và đề cương nhiệm vụ. Các phần nội dung chuyên môn chính (Chương 1, 2, 3, 4) được viết khá đầy đủ và bám sát dự án thực tế. Tuy nhiên, vẫn còn một số chỗ cần thay thế thông tin cá nhân chính xác trước khi xuất bản.

---

## 2. Chi tiết các vị trí phát hiện dữ liệu giả lập / Bản mẫu

| Vị trí | Loại lỗi / Ký hiệu | Nội dung phát hiện | Đoạn văn cảnh (Context) |
|---|---|---|---|
| Paragraph Line 69 | Bracketed placeholder [text] | `[Authorize]` | 3.3.1.	Kiểm soát truy cập toàn bộ endpoint API bằng thuộc tính [Authorize]	86 |
| Paragraph Line 213 | Placeholder student metadata/year templates | `2024` | Sự trỗi dậy của ngành công nghiệp game đang mở ra một quỹ đạo phát triển mới cho lĩnh vực giải trí kỹ thuật số toàn cầu. Theo báo cáo của Newzoo (2024 |
| Paragraph Line 213 | Placeholder student metadata/year templates | `2023` | Sự trỗi dậy của ngành công nghiệp game đang mở ra một quỹ đạo phát triển mới cho lĩnh vực giải trí kỹ thuật số toàn cầu. Theo báo cáo của Newzoo (2024 |
| Paragraph Line 215 | Placeholder student metadata/year templates | `2023` | Tại Việt Nam, theo số liệu từ Vietnam Report và VGS (Vietnam Game Summit 2023), thị trường game nội địa đạt doanh thu ước tính 1,3 tỷ USD với tốc độ t |
| Paragraph Line 418 | Placeholder student metadata/year templates | `2023` | Unity là game engine đa nền tảng do Unity Technologies phát triển, ra mắt năm 2005. Theo thống kê năm 2023, hơn 50% game trên nền tảng di động được ph |
| Paragraph Line 438 | Angle-bracketed placeholder <text> | `<T\>` | NetworkVariable\<T\>: Biến tự động đồng bộ server → clients khi giá trị thay đổi (server write, client read-only) |
| Paragraph Line 456 | Angle-bracketed placeholder <text> | `<T\>` | Generics: Viết code tái sử dụng với nhiều kiểu dữ liệu (List\<T\>, Dictionary\<K,V\>) |
| Paragraph Line 456 | Angle-bracketed placeholder <text> | `<K,V\>` | Generics: Viết code tái sử dụng với nhiều kiểu dữ liệu (List\<T\>, Dictionary\<K,V\>) |
| Paragraph Line 470 | Angle-bracketed placeholder <text> | `<T\>` | DbContext: Đại diện phiên làm việc với database, chứa DbSet\<T\> cho mỗi bảng |
| Paragraph Line 476 | Placeholder student metadata/year templates | `2024` | MySQL là RDBMS mã nguồn mở do Oracle Corporation duy trì, là hệ quản trị cơ sở dữ liệu phổ biến nhất thế giới theo DB-Engines Ranking 2024. MySQL được |
| Paragraph Line 522 | Draft markers / missing content notes | `nhập vào` | Khách (Guest): Là người dùng chưa đăng nhập vào hệ thống. Guest có thể thực hiện các chức năng cơ bản như đăng ký tài khoản, đăng nhập và khởi tạo phi |
| Paragraph Line 630 | Placeholder student metadata/year templates | `2022` | Tầng Client (Unity 2D): Đây là tầng người chơi trực tiếp tương tác. Client được xây dựng bằng Unity 2D 2022.3 LTS, chịu trách nhiệm về rendering, xử l |
| Paragraph Line 801 | Angle-bracketed placeholder <text> | `<JWT_TOKEN>` | Cơ chế xác thực phiên chơi: Sau khi đăng nhập qua API /api/auth/login, client lưu token JWT và đính kèm token vào các request HTTP bằng header Authori |
| Paragraph Line 802 | Draft markers / missing content notes | `nhập vào` | Kết nối gameplay realtime bằng Unity Netcode: Quá trình vào game được tổ chức theo mô hình connection approval của Unity Netcode. Khi client gửi thông |
| Paragraph Line 810 | Angle-bracketed placeholder <text> | `<float>` | Đồng bộ hướng quay và animation: Hướng mặt của nhân vật được lưu trong NetworkVariable<float> networkScaleX. Animation được phát qua UpdateAnimationCl |
| Paragraph Line 820 | Angle-bracketed placeholder <text> | `<int>` | Hệ thống máu server-authoritative: HP được quản lý bằng NetworkVariable<int> và chỉ server có quyền ghi giá trị cuối cùng. Client không tự ý trừ máu t |
| Paragraph Line 940 | Bracketed placeholder [text] | `[Authorize]` | Kiểm soát truy cập toàn bộ endpoint API bằng thuộc tính [Authorize] |
| Paragraph Line 941 | Placeholder student metadata/year templates | `2021` | Trong quá trình rà soát, nhóm phát triển phát hiện lớp EnemyController — cung cấp ba endpoint tra cứu chỉ số kẻ địch (GetAllEnemies, GetEnemy, GetEnem |
| Paragraph Line 942 | Bracketed placeholder [text] | `[Authorize]` | Để khắc phục, thuộc tính [Authorize] được bổ sung ở cấp độ class, qua đó áp dụng ràng buộc xác thực cho đồng thời tất cả các action method mà không cầ |
| Paragraph Line 944 | Bracketed placeholder [text] | `[Authorize]` | Nguyên tắc "đóng mặc định, mở có chọn lọc" được áp dụng nhất quán trên toàn bộ hệ thống: tất cả các controller xử lý dữ liệu người chơi — gồm PlayerCo |
| Paragraph Line 945 | Angle-bracketed placeholder <text> | `<token>` | Khi một client gửi yêu cầu đến endpoint được bảo vệ mà không kèm header Authorization: Bearer <token> hợp lệ, middleware JwtBearerAuthentication trong |
| Paragraph Line 950 | Bracketed placeholder [text] | `[EnableRateLimiting]` | Chính sách "login" sau đó được gắn vào action Login() trong AuthController bằng thuộc tính [EnableRateLimiting], cho phép áp dụng chọn lọc mà không ản |
| Paragraph Line 965 | Angle-bracketed placeholder <text> | `<JWT>` | Sau khi kết nối được chấp nhận, ZonePlayerSessionManager lưu ánh xạ clientId → { userId, JWT, mapId, zoneId, geneSlot }. Ánh xạ này được sử dụng về sa |
| Paragraph Line 1077 | Bracketed placeholder [text] | `[Tên]` | Tierdisplaytext hiển thị dạng “Hệ phụ [Tên] - Tier 1 → 2”; secondaryelemicon thay cho biểu tượng nguyên tố chính. |
| Paragraph Line 1201 | Bracketed placeholder [text] | `[Authorize]` | Về phía backend và hạ tầng, chương đã trình bày các chức năng quản lý tài khoản, xác thực, dữ liệu nhân vật, inventory, trang bị, kỹ năng, Gene Evolut |
| Paragraph Line 1233 | Draft markers / missing content notes | `cần bổ sung` | Cấu trúc hệ thống hiện tại có khả năng mở rộng nhờ tách riêng client, backend và cơ sở dữ liệu. Các nhóm nghiệp vụ được chia thành controller riêng, d |
| Paragraph Line 1250 | Placeholder student metadata/year templates | `2022` | [1] Jeremy Gibson Bond, Introduction to Game Design, Prototyping, and Development: From Concept to Playable Game with Unity and C#, 3rd ed., Addison-W |
| Paragraph Line 1255 | Placeholder student metadata/year templates | `2020` | [6] Mark Richards and Neal Ford, Fundamentals of Software Architecture: An Engineering Approach, O’Reilly Media, 2020. |
| Paragraph Line 1256 | Placeholder student metadata/year templates | `2021` | [7] OWASP Foundation, OWASP Top 10:2021 – The Ten Most Critical Web Application Security Risks, 2021. |
| Paragraph Line 1258 | Placeholder student metadata/year templates | `2024` | [9] Newzoo, Global Games Market Report 2024, Newzoo, 2024. |
| Paragraph Line 1259 | Bracketed placeholder [text] | `[10]` | [10] Unity Technologies, Unity User Manual / Unity Scripting API, Unity Technologies, truy cập năm 2026. |
| Paragraph Line 1259 | Placeholder student metadata/year templates | `2026` | [10] Unity Technologies, Unity User Manual / Unity Scripting API, Unity Technologies, truy cập năm 2026. |
| Paragraph Line 1260 | Bracketed placeholder [text] | `[11]` | [11] Microsoft, ASP.NET Core Documentation, Microsoft, truy cập năm 2026. |
| Paragraph Line 1260 | Placeholder student metadata/year templates | `2026` | [11] Microsoft, ASP.NET Core Documentation, Microsoft, truy cập năm 2026. |
| Paragraph Line 1261 | Bracketed placeholder [text] | `[12]` | [12] Oracle, MySQL 8.0 Reference Manual, Oracle, truy cập năm 2026. |
| Paragraph Line 1261 | Placeholder student metadata/year templates | `2026` | [12] Oracle, MySQL 8.0 Reference Manual, Oracle, truy cập năm 2026. |
| Table 1, Row 3, Column 1 | Placeholder student metadata/year templates | `MSSV` | Ngành: Công nghệ thông tin  Mã số: 748.02.01    	Sinh viên thực hiện: Trần Văn Thủy – MSSV: CT060439   	Người hướng dẫn: 		TS. Nguyễn Đức Hiếu 	Khoa c |
| Table 1, Row 4, Column 1 | Placeholder student metadata/year templates | `2026` | Hà Nội, 2026 |
| Table 1, Row 7, Column 1 | Placeholder student metadata/year templates | `MSSV` | Ngành: Công nghệ thông tin  Mã số: 748.02.01    	Sinh viên thực hiện: Trần Văn Thủy – MSSV: CT060439   	Người hướng dẫn: 		TS. Nguyễn Đức Hiếu 	Khoa c |
| Table 1, Row 8, Column 1 | Placeholder student metadata/year templates | `2026` | Hà Nội, 2026 |
| Table 4, Row 1, Column 2 | Placeholder student metadata/year templates | `2026` | Hà Nội, ngày     tháng     năm 2026     Sinh viên thực hiện (Ký tên và ghi rõ họ tên)        Trần Văn Thủy |
| Table 12, Row 2, Column 3 | Placeholder student metadata/year templates | `2022` | 2022.3 LTS |

---

## 3. Khuyến nghị khắc phục hành chính
1. **Trang bìa & Thông tin cá nhân**: Thay thế toàn bộ "Nguyễn Văn A" và mã số sinh viên demo bằng tên và mã số thực của bạn.
2. **Năm học**: Rà soát lại năm học (hiện đang là 2026 hoặc 202x) xem có khớp với thời gian nộp đồ án thực tế của trường hay không.
3. **Các ký tự chờ điền (`...` hoặc `___`)**: Điền đầy đủ thông tin hoặc xóa bớt nếu đó là các biểu mẫu không áp dụng.
