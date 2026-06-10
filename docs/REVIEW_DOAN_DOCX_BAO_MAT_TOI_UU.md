# Rà soát DoAn.docx và Git về tối ưu, bảo mật

Ngày rà soát: 02/06/2026

Phạm vi kiểm tra:
- File Word chính: `DoAn.docx` ở thư mục gốc repo.
- Mã nguồn backend: `GameServerApi`.
- Tài liệu/chương markdown liên quan trong `docs`.
- Cấu hình triển khai: `.env.example`, `deploy.sh`, `docs/docker-compose.yml`.

## 1. Kết luận nhanh

Nên giữ hướng viết hiện tại của `DoAn.docx`, đặc biệt là Chương 4, vì bản Word hiện tại an toàn hơn các bản markdown cũ như `docs/CHUONG4_BAO_CAO.md` và `docs/DO_AN_TOT_NGHIEP_FINAL.md`.

Không nên thay nguyên Chương 4 trong Word bằng các bản markdown cũ, vì các bản đó vẫn có nhiều số liệu hiệu năng chi tiết như FPS, RTT, CPU/RAM, throughput API nhưng trong Word hiện tại đã ghi rõ benchmark định lượng chưa được chuẩn hóa. Nếu chưa có log đo thật, đưa lại các con số đó sẽ làm báo cáo kém tin cậy.

Nên sửa Word theo kiểu chỉnh điểm nhỏ, không thay toàn bộ chương. Đồng thời nên sửa vài điểm code/cấu hình production để phần bảo mật trong báo cáo có cơ sở chắc hơn.

Trạng thái sau lần fix hiện tại:

- Đã sửa trực tiếp `DoAn.docx` các điểm: "ba chương" -> "bốn chương", ASP.NET Core 7 -> ASP.NET Core .NET 9, giảm cam kết 100ms/FPS khi chưa có benchmark, sửa Bảng 4.1 cho khớp repo, chỉnh mô tả pipeline middleware và Docker database bind localhost.
- Đã thêm `docker-compose.yml` ở root để khớp `deploy.sh` và báo cáo.
- Đã sửa `Program.cs`: CORS whitelist theo cấu hình, JWT/ZoneApiKey fail-fast trong Production, admin seed không dùng mật khẩu mặc định ở Production, rate limit login phân vùng theo IP.
- Đã thêm kiểm tra ownership `playerId` cho các API chính trong `GeneController`, `UpgradeController` và `PlayerController`.

## 2. Những chỗ nên sửa ngay trong DoAn.docx

### 2.1. Sửa lỗi số chương

Đang ghi:

```text
Cấu trúc báo cáo gồm ba chương chính:
```

Nên sửa thành:

```text
Cấu trúc báo cáo gồm bốn chương chính:
```

Lý do: Word đã có Chương 4, nên ghi "ba chương" là sai hình thức.

### 2.2. Giảm cam kết FPS và độ trễ nếu chưa có log đo thật

Trong phần mở đầu có câu đánh giá theo tiêu chí FPS, độ trễ mạng và tính chính xác đồng bộ trạng thái. Nếu chưa có log đo, profiler screenshot hoặc file benchmark kèm theo, nên sửa mềm hơn.

Đề xuất thay:

```text
Cuối cùng là kiểm thử chức năng từng hệ thống gameplay, kiểm thử đồng bộ multiplayer với nhiều client đồng thời và đánh giá hiệu năng tổng thể theo các tiêu chí về FPS, độ trễ mạng và tính chính xác đồng bộ trạng thái.
```

Thành:

```text
Cuối cùng là kiểm thử chức năng từng hệ thống gameplay, kiểm thử đồng bộ multiplayer với nhiều client đồng thời và ghi nhận các tiêu chí hiệu năng cần tiếp tục đo lường như FPS, độ trễ mạng và mức sử dụng tài nguyên server.
```

Tương tự, câu "đồng bộ trạng thái dưới 100ms" nên đổi thành "hướng tới đồng bộ trạng thái độ trễ thấp" nếu chưa có số đo thật.

### 2.3. Sửa phiên bản ASP.NET Core

Trong Word có đoạn ghi:

```text
API Server là ASP.NET Core 7 Web API
```

Nhưng `GameServerApi/GameServerApi.csproj` đang là:

```xml
<TargetFramework>net9.0</TargetFramework>
```

Nên sửa thành:

```text
API Server là ASP.NET Core .NET 9 Web API
```

### 2.4. Sửa mô tả Docker Compose cho khớp Git

Word đang mô tả hệ thống đóng gói qua Docker Compose với ba container. Ý này đúng về hướng triển khai, nhưng Git hiện tại chưa đồng bộ:

- `deploy.sh` tìm `docker-compose.yml` ở root repo.
- Repo hiện chỉ thấy `docs/docker-compose.yml`, không có `docker-compose.yml` ở root.
- `docs/docker-compose.yml` lại dùng path tương đối `./GameServerApi` và `./GameServerApi/gamedb.sql`; nếu chạy từ `docs` thì sai path, còn nếu muốn chạy từ root thì file compose nên nằm ở root.
- Repo có `gamedb.sql` ở root, không thấy `GameServerApi/gamedb.sql`.

Nếu chưa sửa compose, trong Word nên viết thận trọng:

```text
Hệ thống đã chuẩn bị Dockerfile, biến môi trường mẫu và tài liệu/cấu hình mẫu phục vụ triển khai backend, database và Unity Dedicated Server trên VPS.
```

Nếu muốn giữ câu Docker Compose ba service, nên sửa Git trước: đưa `docker-compose.yml` về root hoặc sửa `deploy.sh` và các path trong compose cho thống nhất.

### 2.5. Sửa Bảng 4.1 về cấu trúc repo

Trong Bảng 4.1 đang có:

```text
docker/.env.example
docs/Scripts
```

Nên sửa thành:

```text
.env.example, GameServerApi/.env.example
docs, Scripts
```

Lý do: repo hiện có `.env.example` ở root và `GameServerApi/.env.example`; thư mục `docker` chỉ thấy `db-init.sh`. Repo có `docs` và `Scripts` là hai thư mục riêng, không phải `docs/Scripts`.

### 2.6. Cập nhật danh mục bảng sau khi sửa

Các caption bảng Chương 4 trong Word hiện đã có dạng `Bảng 4.1` đến `Bảng 4.4`, không thấy lỗi nghiêm trọng. Sau khi sửa nội dung/caption, cần bấm cập nhật lại danh mục bảng trong Word để số trang và tiêu đề mới khớp.

## 3. Không nên lấy lại từ markdown cũ

Không nên thay Chương 4 bằng `docs/CHUONG4_BAO_CAO.md` hoặc đoạn Chương 4 trong `docs/DO_AN_TOT_NGHIEP_FINAL.md` nếu chưa có bằng chứng đo thật.

Các lý do chính:

- Markdown cũ ghi nhiều số liệu FPS, RTT, throughput, CPU/RAM rất cụ thể. Word hiện tại đã sửa an toàn hơn bằng cách ghi chưa có benchmark định lượng chuẩn hóa.
- Markdown cũ có chỗ ghi BCrypt cost 11, trong code hiện tại `AuthService` dùng `workFactor: 12`.
- Markdown cũ có chỗ ghi JWT 24h, trong code hiện tại `AuthService` mặc định `ExpiryDays = 7` nếu không cấu hình.
- Markdown cũ mô tả `docker-compose.yml` như file ở root, trong khi repo hiện chỉ có `docs/docker-compose.yml`.

## 4. Những chỗ nên sửa trong code/cấu hình để bảo mật chắc hơn

### 4.1. CORS đang mở toàn bộ origin

File: `GameServerApi/Program.cs`

Hiện tại:

```csharp
policy.AllowAnyOrigin()
      .AllowAnyMethod()
      .AllowAnyHeader();
```

Production không nên mở toàn bộ origin. Nên đổi sang whitelist qua cấu hình:

```csharp
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientCors", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

Và trong Production phải cấu hình domain/IP client thật. Nếu Unity standalone không phụ thuộc browser CORS thì càng không cần mở rộng cho mọi origin.

### 4.2. JWT validation còn fallback dev key

File: `GameServerApi/Program.cs`

Hiện tại:

```csharp
var jwtKey = jwtSection["Key"] ?? "DEV_KEY_CHANGE_ME";
```

Production nên fail fast nếu thiếu key thật, key là placeholder hoặc key quá ngắn:

```csharp
var jwtKey = jwtSection["Key"];
if (builder.Environment.IsProduction() &&
    (string.IsNullOrWhiteSpace(jwtKey) ||
     jwtKey is "DEV_KEY_CHANGE_ME" or "OVERRIDE_VIA_ENVIRONMENT_VARIABLE" ||
     jwtKey.Length < 32))
{
    throw new InvalidOperationException("Production JWT key is missing or unsafe.");
}
```

Cũng nên đổi:

```csharp
options.RequireHttpsMetadata = false;
```

thành:

```csharp
options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
```

nếu production dùng HTTPS/reverse proxy đúng chuẩn.

### 4.3. Admin seed còn default password

File: `GameServerApi/Program.cs`

Hiện tại nếu thiếu cấu hình, code có thể seed admin mặc định:

```csharp
var adminUsername = config["Admin:Username"] ?? "admin";
var adminPassword = config["Admin:Password"] ?? "Admin@123!";
```

Production nên bắt buộc set admin qua biến môi trường, không fallback mật khẩu mặc định. Nếu không cần auto-seed admin ở production, nên tắt hoặc chỉ cho chạy trong Development.

### 4.4. Rate limit login nên phân vùng theo IP hoặc username

File: `GameServerApi/Program.cs`

`AddFixedWindowLimiter("login", ...)` hiện có rủi ro dùng chung một limiter cho toàn policy. Như vậy 5 request/phút có thể trở thành giới hạn toàn endpoint, dễ gây tự chặn người dùng hợp lệ khi có nhiều người đăng nhập.

Nên đổi sang policy có partition theo IP, hoặc theo IP + username nếu lấy được username từ body:

```csharp
options.AddPolicy("login", context =>
    RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromSeconds(60),
            PermitLimit = 5,
            QueueLimit = 0
        }));
```

### 4.5. GeneController và UpgradeController cần kiểm tra quyền sở hữu playerId

Các file:

- `GameServerApi/Controllers/GeneController.cs`
- `GameServerApi/Controllers/UpgradeController.cs`

Hiện tại controller có `[Authorize]`, nhưng nhiều API vẫn nhận `playerId` từ body/query rồi gọi `FindAsync(playerId)`. Token hợp lệ có thể thử gửi `playerId` của người khác nếu chưa kiểm tra ownership.

Nên sửa theo một trong hai hướng:

1. Với request từ người chơi: bỏ tin vào `playerId` client gửi, lấy player từ claim `user_id` trong JWT.
2. Với request nội bộ từ Zone Server: chỉ cho dùng `playerId` khi request xác thực bằng `X-Zone-Api-Key`.

Mẫu logic:

```csharp
private int GetAuthorizedPlayerId(int requestedPlayerId)
{
    if (User.IsInRole("GameServer"))
        return requestedPlayerId;

    var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "user_id")?.Value;
    if (!int.TryParse(userIdClaim, out var userId))
        throw new UnauthorizedAccessException();

    if (requestedPlayerId != userId)
        throw new UnauthorizedAccessException();

    return userId;
}
```

Nếu `player_id == user_id` là giả định thiết kế, cần ghi rõ trong code và báo cáo. Nếu không phải lúc nào cũng bằng nhau, nên query `PlayerData` theo `UserId` thay vì so sánh trực tiếp `playerId`.

### 4.6. PlayerController còn nhiều endpoint nhận playerId trực tiếp

`PlayerController` đã có một số endpoint dùng claim `user_id` đúng hướng, ví dụ cập nhật vị trí, cập nhật data, thêm inventory, dùng item, tháo túi. Tuy nhiên vẫn còn nhiều endpoint tìm thẳng bằng `playerId` như clear/sort inventory, equip/unequip equipment, active buffs, skills, potential, gain-exp.

Nên rà lại toàn bộ endpoint theo quy tắc:

- Endpoint người chơi gọi: lấy player hiện tại từ JWT, không tin URL/body `playerId`.
- Endpoint server nội bộ gọi: dùng riêng scheme `ZoneApiKey`.
- Endpoint debug/admin: yêu cầu role `Admin`.

### 4.7. DungeonController chưa bảo vệ toàn bộ endpoint

Word hiện tại đã viết khá thận trọng về bảo mật, nhưng code vẫn nên phân loại lại `DungeonController`:

- API chỉ đọc cấu hình dungeon có thể public nếu không lộ dữ liệu nhạy cảm.
- API entry-status/session/progress có `playerId` nên yêu cầu JWT ownership hoặc Zone API Key.
- API cộng thưởng phải tiếp tục tách riêng như `DungeonRewardController` với Zone API Key.

### 4.8. Random.Shared đã được sửa, không còn là lỗi như ghi chú cũ

Trong Git hiện tại:

- `GeneController` đã dùng `Random.Shared.NextDouble()`.
- `UpgradeController` đã dùng `Random.Shared.NextDouble()`.

Vì vậy không cần ghi trong báo cáo là code vẫn dùng `new Random()` ở hai controller này. Nếu muốn nâng mức công bằng/xác suất tốt hơn, có thể đổi sang `RandomNumberGenerator`, nhưng đây là cải tiến thêm, không phải lỗi hiện tại.

### 4.9. Docker Compose cần đồng bộ trước khi ghi là triển khai hoàn chỉnh

Nên chọn một trong hai cách:

Cách A, khuyến nghị: đưa `docker-compose.yml` ra root repo.

Khi đó cần sửa volume DB:

```yaml
volumes:
  - ./gamedb.sql:/docker-entrypoint-initdb.d/01-schema.sql:ro
```

và giữ `deploy.sh` như hiện tại.

Cách B: giữ compose trong `docs`.

Khi đó cần sửa `deploy.sh`:

```bash
COMPOSE_FILE="$REPO_DIR/docs/docker-compose.yml"
```

và sửa path trong compose thành:

```yaml
context: ../GameServerApi
volumes:
  - ../gamedb.sql:/docker-entrypoint-initdb.d/01-schema.sql:ro
```

Nếu chưa sửa, trong Word chỉ nên ghi "đã chuẩn bị cấu hình mẫu triển khai", không nên ghi như đã có stack compose production hoàn chỉnh.

## 5. Ưu tiên thực hiện

Ưu tiên 1, sửa Word trước khi nộp:

- "ba chương" thành "bốn chương".
- Giảm cam kết FPS/độ trễ nếu chưa có log.
- ASP.NET Core 7 thành ASP.NET Core .NET 9.
- Sửa mô tả Docker Compose và Bảng 4.1.
- Cập nhật danh mục bảng.

Ưu tiên 2, sửa code/cấu hình production:

- CORS whitelist.
- JWT key fail-fast trong Production.
- Không seed admin bằng mật khẩu mặc định trong Production.
- Ownership check cho `playerId`.
- Phân vùng rate limit login theo IP/username.

Ưu tiên 3, làm báo cáo mạnh hơn nếu còn thời gian:

- Tạo benchmark thật cho FPS, RTT, CPU/RAM, throughput API.
- Đính kèm log, screenshot Unity Profiler, Postman/JMeter result hoặc file CSV.
- Khi đã có số đo thật, mới đưa lại bảng số liệu chi tiết vào Chương 4.
