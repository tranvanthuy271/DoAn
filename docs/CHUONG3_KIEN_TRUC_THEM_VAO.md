## 3.3.2 Tăng cường kiểm soát truy cập và bảo mật hệ thống

Sau khi hoàn thành các chức năng nghiệp vụ cốt lõi của hệ thống, nhóm phát triển tiến hành rà soát bảo mật toàn diện theo các tiêu chí của OWASP Top 10. Quá trình rà soát phát hiện một số điểm cần cải thiện, trong đó sáu biện pháp được ưu tiên hiện thực hóa trước khi triển khai lên môi trường production. Các biện pháp này tập trung vào năm lớp phòng thủ độc lập nhau: lớp transport (xác thực kết nối NGO), lớp network (giới hạn tốc độ yêu cầu), lớp application (kiểm soát truy cập endpoint và xác thực nội bộ), lớp data (kiểm định dữ liệu đầu vào hai tầng server và client), và lớp presentation (kiểm soát thông tin lỗi trả về client). Mỗi lớp bảo vệ một điểm tiếp xúc khác nhau, đảm bảo rằng việc vượt qua một lớp không tự động mang lại quyền truy cập vào toàn bộ hệ thống.

### a) Kiểm soát truy cập toàn bộ endpoint API bằng thuộc tính `[Authorize]`

Trong quá trình rà soát, nhóm phát triển phát hiện lớp `EnemyController` — cung cấp ba endpoint tra cứu chỉ số kẻ địch (`GetAllEnemies`, `GetEnemy`, `GetEnemiesByLevel`) — chưa được bảo vệ bằng xác thực JWT. Hệ quả là bất kỳ client nào, kể cả client chưa đăng nhập, đều có thể gửi yêu cầu đến các endpoint này và tải xuống toàn bộ bộ chỉ số kẻ địch trong game. Đây là thông tin thiết kế nội bộ, bao gồm điểm máu, sát thương, tốc độ và hành vi của từng loại kẻ địch — những dữ liệu mà một người chơi trục lợi có thể sử dụng để tính toán cơ chế khai thác. Nguy cơ này thuộc nhóm "Broken Access Control" (A01) trong phân loại OWASP Top 10:2021.

Để khắc phục, thuộc tính `[Authorize]` được bổ sung ở cấp độ class, qua đó áp dụng ràng buộc xác thực cho đồng thời tất cả các action method mà không cần khai báo lặp lại trên từng phương thức:

```csharp
// GameServerApi/Controllers/EnemyController.cs
using GameServerApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
[Authorize]                       // yêu cầu JWT hợp lệ cho mọi action trong class này
public class EnemyController : ControllerBase
{
    private readonly GameDbContext _db;  // inject DbContext trực tiếp — không có service layer

    public EnemyController(GameDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllEnemies()
    {
        var enemies = await _db.Enemies.ToListAsync();
        return Ok(new { enemies });
    }

    [HttpGet("{enemyId}")]          // tên param là enemyId, không phải id
    public async Task<IActionResult> GetEnemy(int enemyId)
    {
        var enemy = await _db.Enemies.FindAsync(enemyId);
        if (enemy == null) return NotFound("Enemy không tồn tại.");
        return Ok(enemy);
    }

    [HttpGet("by-level/{level}")]   // route: by-level/{level}
    public async Task<IActionResult> GetEnemiesByLevel(int level)
    {
        var enemies = await _db.Enemies
            .Where(e => e.Level == level)
            .ToListAsync();
        return Ok(new { level, enemies });
    }
}
```

Nguyên tắc "đóng mặc định, mở có chọn lọc" được áp dụng nhất quán trên toàn bộ hệ thống: tất cả các controller xử lý dữ liệu người chơi — gồm `PlayerController`, `QuestController`, `UpgradeController`, `GeneController`, `NpcActionController`, `DungeonController` và `LeaderboardController` — đều khai báo `[Authorize]` ở cấp class. Chỉ hai action duy nhất được miễn xác thực là `AuthController.Register()` và `AuthController.Login()`, vì đây là điểm vào công khai cho phép người dùng chưa có tài khoản thực hiện đăng ký và nhận token lần đầu.

Khi một client gửi yêu cầu đến endpoint được bảo vệ mà không kèm header `Authorization: Bearer <token>` hợp lệ, middleware `JwtBearerAuthentication` trong pipeline ASP.NET Core tiến hành kiểm tra tuần tự ba điều kiện: chữ ký HMAC-SHA256 của token có khớp với khóa bí mật của server không; token có còn trong thời hạn hiệu lực (`exp` claim) không; giá trị `issuer` và `audience` có khớp với cấu hình không. Nếu bất kỳ điều kiện nào thất bại, middleware trả về HTTP 401 Unauthorized và dừng chuỗi xử lý — yêu cầu không bao giờ đến được controller, toàn bộ logic nghiệp vụ bên trong không được thực thi.

### b) Giới hạn tốc độ yêu cầu đăng nhập (Rate Limiting)

Endpoint `POST /api/auth/login` là mục tiêu phổ biến của tấn công brute-force mật khẩu: kẻ tấn công sử dụng danh sách mật khẩu phổ biến và gửi hàng nghìn yêu cầu liên tiếp để dò tài khoản người dùng. Không có biện pháp giới hạn tốc độ, khả năng thử tối đa chỉ bị ràng buộc bởi băng thông mạng — có thể lên tới hàng chục nghìn lần thử mỗi phút. Mặc dù BCrypt với work factor 12 làm chậm quá trình xác minh mật khẩu đến khoảng 250–400 ms mỗi lần, điều này vẫn không đủ để ngăn tấn công dò mật khẩu khi kẻ tấn công sử dụng nhiều luồng song song.

Để khắc phục, hệ thống tích hợp Rate Limiting thông qua middleware `AddRateLimiter` được cung cấp sẵn trong ASP.NET Core mà không cần phụ thuộc thư viện ngoài. Chính sách `FixedWindowLimiter` được lựa chọn vì phù hợp với yêu cầu: cửa sổ thời gian cố định 60 giây với ngưỡng tối đa 5 yêu cầu. Cấu hình được đăng ký trong `Program.cs` như sau:

```csharp
// GameServerApi/Program.cs
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.Window      = TimeSpan.FromSeconds(60);  // cửa sổ thời gian 60 giây
        opt.PermitLimit = 5;                          // tối đa 5 yêu cầu mỗi cửa sổ
        opt.QueueLimit  = 0;                          // từ chối ngay, không xếp hàng chờ
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Thứ tự trong pipeline: sau UseCors(), trước UseAuthentication()
app.UseRateLimiter();
```

Chính sách `"login"` sau đó được gắn vào action `Login()` trong `AuthController` bằng thuộc tính `[EnableRateLimiting]`, cho phép áp dụng chọn lọc mà không ảnh hưởng đến các action khác của cùng controller:

```csharp
// GameServerApi/Controllers/AuthController.cs
using Microsoft.AspNetCore.RateLimiting;

[HttpPost("login")]
[EnableRateLimiting("login")]
public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
{
    // Query trực tiếp qua EF Core — không có repository layer
    var user = await _db.Users
        .FirstOrDefaultAsync(u => u.Username == request.Username);
    if (user == null)
        return Unauthorized("Sai username hoặc password.");

    // _authService (IAuthService) xử lý cả BCrypt verify lẫn JWT generation
    if (!_authService.VerifyPassword(request.Password, user.PasswordHash))
        return Unauthorized("Sai username hoặc password.");

    var token = _authService.GenerateJwtToken(user);   // GenerateJwtToken, không phải GenerateToken
    return Ok(new { token, user_id = user.UserId, username = user.Username });
}
```

Khi một địa chỉ IP gửi yêu cầu thứ sáu trong vòng 60 giây, ASP.NET Core trả về HTTP 429 Too Many Requests ngay tại tầng middleware — không thực hiện truy vấn cơ sở dữ liệu, không gọi `BCrypt.Verify()`. Điều này vừa bảo vệ tài khoản khỏi bị dò mật khẩu, vừa giảm tải không cần thiết cho tầng xử lý phía sau. Tốc độ dò tối đa bị giới hạn xuống còn 5 lần thử mỗi phút, tức 300 lần thử mỗi giờ — giảm vài nghìn lần so với không có giới hạn.

### c) Xác thực nội bộ Zone Server bằng Zone API Key và so sánh hằng thời gian

NGO Dedicated Server (Zone Server) cần gọi đến REST API để thực hiện các thao tác ảnh hưởng đến dữ liệu lâu dài: cộng EXP sau khi tiêu diệt kẻ địch, cấp phần thưởng hoàn thành dungeon, hoặc cập nhật tiến trình phó bản. Tuy nhiên, Zone Server không phải người dùng — nó không có tài khoản, không đăng nhập và không nhận JWT theo luồng người chơi thông thường. Nếu sử dụng JWT của người chơi để thực hiện các cuộc gọi nội bộ này, API không thể phân biệt được yêu cầu từ client hợp lệ hay từ Zone Server, dẫn đến nguy cơ người chơi tự gọi vào endpoint cộng EXP mà không thông qua Zone Server.

Để phân tách rõ ràng hai loại nguồn gọi API, hệ thống triển khai sơ đồ xác thực lai (hybrid authentication scheme): nếu yêu cầu đến kèm header `X-Zone-Api-Key`, nó được chuyển hướng sang `ZoneApiKeyAuthenticationHandler`; ngược lại, luồng JWT Bearer tiêu chuẩn được áp dụng. Bên trong `ZoneApiKeyAuthenticationHandler`, phép so sánh khóa được thực hiện bằng `CryptographicOperations.FixedTimeEquals()` thay vì phép so sánh chuỗi thông thường (`==`):

```csharp
// GameServerApi/Auth/ZoneApiKeyAuthenticationHandler.cs
protected override Task<AuthenticateResult> HandleAuthenticateAsync()
{
    if (!Request.Headers.TryGetValue(HeaderName, out var headerValues))  // HeaderName = "X-Zone-Api-Key"
        return Task.FromResult(AuthenticateResult.NoResult());

    string providedKey = headerValues.ToString();
    string expectedKey = _configuration["ZoneApiKey"] ?? string.Empty;  // đọc từ IConfiguration

    if (string.IsNullOrWhiteSpace(providedKey))
        return Task.FromResult(AuthenticateResult.Fail("X-Zone-Api-Key trống."));

    if (string.IsNullOrWhiteSpace(expectedKey))
        return Task.FromResult(AuthenticateResult.Fail("ZoneApiKey chưa được cấu hình."));

    // So sánh hằng thời gian — chống timing attack
    if (!SecureEquals(providedKey, expectedKey))
        return Task.FromResult(AuthenticateResult.Fail("X-Zone-Api-Key không hợp lệ."));

    var claims   = new[] { new Claim(ClaimTypes.Role, "GameServer") };
    var identity = new ClaimsIdentity(claims, SchemeName);
    var ticket   = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
    return Task.FromResult(AuthenticateResult.Success(ticket));
}

private static bool SecureEquals(string left, string right)
{
    byte[] leftBytes  = Encoding.UTF8.GetBytes(left);
    byte[] rightBytes = Encoding.UTF8.GetBytes(right);
    if (leftBytes.Length != rightBytes.Length) return false;   // khác độ dài → từ chối ngay
    return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
}
```

Lý do kỹ thuật đằng sau `FixedTimeEquals()` là phép so sánh chuỗi thông thường sử dụng thuật toán "fail-fast": nó trả về `false` ngay khi gặp ký tự đầu tiên không khớp, khiến thời gian thực thi phụ thuộc vào vị trí ký tự sai đầu tiên. Một kẻ tấn công tinh vi có thể gửi nhiều khóa thử khác nhau, đo thời gian phản hồi và suy luận từng ký tự của khóa bí mật — đây là tấn công kênh bên theo thời gian (timing side-channel attack). `CryptographicOperations.FixedTimeEquals()` luôn duyệt đủ toàn bộ độ dài hai mảng byte bất kể nội dung, loại bỏ hoàn toàn sự khác biệt thời gian này.

Ngoài ra, các endpoint dành riêng cho Zone Server được đánh dấu thêm `[Authorize(AuthenticationSchemes = ZoneApiKeyAuthenticationHandler.SchemeName)]`. Thuộc tính này chỉ định rõ scheme xác thực được chấp nhận phải là `"ZoneApiKey"` — mọi JWT của người chơi thông thường đều bị bác bỏ tại tầng authentication, ngay cả khi token hoàn toàn hợp lệ, vì nó thuộc scheme `JwtBearer` khác scheme.

### d) Xác thực kết nối tại tầng transport của NGO Dedicated Server

Khi sử dụng Unity Netcode for GameObjects (NGO), mặc định bất kỳ client nào biết địa chỉ IP và cổng của Zone Server đều có thể khởi tạo kết nối. Điều này tạo ra nguy cơ: người dùng không hợp lệ hoặc bot có thể kết nối vào server để chiếm tài nguyên hoặc can thiệp vào trạng thái gameplay. NGO cung cấp cơ chế `ConnectionApprovalCallback` cho phép server kiểm tra và từ chối kết nối ở giai đoạn rất sớm, trước khi bất kỳ `NetworkObject` nào được khởi tạo và tài nguyên nào được cấp phát. Lớp `ZoneConnectionApproval` triển khai bốn bước xác thực tuần tự trong callback này:

```csharp
// Client/Assets/Scripts/Network/Server/ZoneConnectionApproval.cs
// Payload JSON (UTF-8): { "token": "<JWT>", "mapId": 0, "zoneId": 0, "geneSlot": 1 }
private const int MaxPayloadBytes = 2048;

private void HandleApproval(
    NetworkManager.ConnectionApprovalRequest  request,
    NetworkManager.ConnectionApprovalResponse response)
{
    // Bước 1 — Giới hạn kích thước payload (DoS prevention)
    if (request.Payload.Length > MaxPayloadBytes)
    { Reject(response, "Payload quá lớn"); return; }

    // Bước 2 — Giải mã UTF-8 và parse JSON thủ công (không dùng thư viện ngoài)
    string json;
    try { json = Encoding.UTF8.GetString(request.Payload); }
    catch { Reject(response, "Payload không phải UTF-8"); return; }

    if (!TryParsePayload(json, out string token, out int mapId, out int zoneId, out int geneSlot))
    { Reject(response, "Payload JSON không hợp lệ"); return; }

    // Bước 3 — Xác minh chữ ký JWT và thời hạn bằng JwtValidator nhẹ (tự hiện thực)
    string secret = _config.GetJwtSecret();
    var    result = JwtValidator.Validate(token, secret);
    if (!result.IsValid)
    { Reject(response, $"JWT không hợp lệ: {result.ErrorMessage}"); return; }

    // Bước 4 — Kiểm tra zone tồn tại và chưa đầy
    var registry = ZoneRoomRegistry.Instance;
    ZoneRoom room = registry.ResolveLoginRoom(mapId, zoneId);
    if (room == null)
    { Reject(response, $"Không tìm được zone cho map={mapId}, zone={zoneId}"); return; }

    if (room.IsFull)
    {
        ZoneRoom fallback = registry.FindLeastLoadedZone(room.MapId, room.ZoneId);
        if (fallback == null || fallback.IsFull)
        { Reject(response, "Server đầy"); return; }
        room = fallback;
    }

    // Ghi nhận session — ZonePlayerSessionManager lưu ánh xạ clientId → {userId, token, …}
    ulong clientId = request.ClientNetworkId;
    registry.AssignClientToRoom(clientId, room);
    ZonePlayerSessionManager.RegisterSessionOrQueue(
        clientId, result.UserId, result.Username, room.MapId, room.ZoneId, token, geneSlot);

    // Phê duyệt kết nối
    response.Approved           = true;
    response.CreatePlayerObject = false;
    Vector2 entry = room.GetEntryPoint(0);
    response.Position = new Vector3(entry.x, entry.y, 0f);
}

private static void Reject(NetworkManager.ConnectionApprovalResponse response, string reason)
{
    response.Approved = false;
    response.Reason   = reason;   // NGO 1.x trở lên hỗ trợ Reason string
}
```

`JwtValidator` là lớp tự hiện thực trong Unity, không phụ thuộc thư viện ngoài. Lớp này chỉ xác minh hai yếu tố cần thiết: chữ ký HMAC-SHA256 để xác nhận token chưa bị giả mạo, và giá trị `exp` claim để xác nhận token chưa hết hạn. Việc tự hiện thực thay vì dùng thư viện ngoài là quyết định có chủ đích nhằm tránh đưa thêm phụ thuộc bên ngoài vào dự án Unity.

Sau khi kết nối được chấp nhận, `ZonePlayerSessionManager` lưu ánh xạ `clientId → { userId, JWT, mapId, zoneId, geneSlot }`. Ánh xạ này được sử dụng về sau khi Zone Server cần thực hiện gọi API nhân danh người chơi — Zone Server tra cứu JWT tương ứng với `clientId` rồi đính kèm vào header `Authorization: Bearer <JWT>` khi gọi API Backend, đảm bảo luồng server-authoritative không bị phá vỡ.

### e) Ngăn lộ thông tin kỹ thuật nhạy cảm qua `ErrorHandlingMiddleware`

Trong môi trường phát triển, ASP.NET Core mặc định trả về trang "Developer Exception Page" chứa toàn bộ stack trace, tên class, tên bảng cơ sở dữ liệu và chuỗi kết nối khi xảy ra exception chưa xử lý. Nếu cấu hình môi trường bị thiết lập sai trên server production, hoặc nếu một exception bất ngờ xảy ra ngoài luồng xử lý thông thường, những thông tin kỹ thuật này có thể lộ ra ngoài client. Kẻ tấn công có thể khai thác thông tin này để hiểu rõ cấu trúc nội bộ của hệ thống và xác định các vector tấn công tiếp theo. Điều này thuộc nhóm "Security Misconfiguration" (A05) trong OWASP Top 10:2021.

Để phòng ngừa, hệ thống triển khai `ErrorHandlingMiddleware` và đăng ký nó ở vị trí đầu tiên trong pipeline, trước mọi middleware khác, nhằm đảm bảo tất cả exception từ bất kỳ tầng nào đều được bắt tại đây:

```csharp
// GameServerApi/Middleware/ErrorHandlingMiddleware.cs
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate                  _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Ghi log đầy đủ nội bộ — stack trace, message, context — để phục vụ chẩn đoán
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);

            context.Response.StatusCode  = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            // Chỉ trả về thông báo chung — không có stack trace, tên class, chuỗi kết nối
            // Dùng ApiResponse wrapper thống nhất với toàn bộ API: { success, error, errorCode }
            var response = ApiResponse.Fail("Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.", 500);
            var body = JsonSerializer.Serialize(response,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            await context.Response.WriteAsync(body);
        }
    }
}
```

Middleware này được đăng ký đầu tiên trong `Program.cs`, trước `UseCors()`, `UseRateLimiter()`, `UseAuthentication()` và `UseAuthorization()`:

```csharp
// GameServerApi/Program.cs — thứ tự pipeline middleware
app.UseMiddleware<ErrorHandlingMiddleware>();   // ← đầu tiên, bắt mọi exception
app.UseCors("AllowAll");                        // tên policy khai báo trong AddCors()
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

Thiết kế hai cấp độ phản hồi này đảm bảo tính song hành giữa hai mục tiêu: thông tin đầy đủ về lỗi được lưu lại trong log nội bộ phục vụ nhóm vận hành chẩn đoán sự cố, trong khi client bên ngoài chỉ nhận được thông báo chung chung không tiết lộ bất kỳ chi tiết kỹ thuật nào về nguyên nhân hoặc cấu trúc hệ thống.

---

## 3.3.6 Hiện thực hóa triển khai và vận hành với Docker Compose

Hệ thống Mutants Arena được đóng gói và triển khai bằng Docker Compose, cho phép vận hành toàn bộ hạ tầng trên bất kỳ máy chủ Linux VPS nào chỉ với điều kiện duy nhất là đã cài đặt Docker Engine. Chiến lược containerization này mang lại hai lợi ích chính: thứ nhất là đồng nhất hoàn toàn giữa môi trường phát triển và môi trường production, loại bỏ sự cố "chạy được trên máy lập trình viên nhưng không chạy được trên server"; thứ hai là cho phép cập nhật từng thành phần độc lập mà không ảnh hưởng đến các thành phần còn lại đang chạy.

### a) Kiến trúc ba container và phân tách mạng nội bộ

Toàn bộ hạ tầng được tổ chức thành ba container, mỗi container đảm nhiệm đúng một tầng trong kiến trúc hệ thống. Cấu hình cụ thể được trình bày trong Bảng 3.X dưới đây.

**Bảng 3.X — Cấu hình các container trong hệ thống Docker Compose**

| Container | Image | Cổng ánh xạ | Vai trò |
|-----------|-------|-------------|---------|
| `db` | `mariadb:10.6` | 3306 (chỉ mạng nội bộ, không mở ra host) | Lưu trữ dữ liệu bền vững |
| `api` | `.NET 9 (Dockerfile tùy chỉnh)` | 5000 → 5000 (host) | REST API + SignalR Hub |
| `unity` | `ubuntu:22.04` | 7777 UDP → 7777 (host) | NGO Dedicated Server headless |

Container `db` được cấu hình chỉ tham gia vào mạng nội bộ Docker (`internal: true`) và không được ánh xạ bất kỳ cổng nào ra ngoài máy chủ vật lý. Điều này có nghĩa: ngay cả khi kẻ tấn công xâm nhập được vào máy chủ qua các vector khác, cơ sở dữ liệu vẫn không thể bị kết nối trực tiếp từ bên ngoài mà không đi qua tầng API đã được xác thực.

Container `api` phụ thuộc vào `db` với điều kiện health check, đảm bảo MariaDB hoàn toàn sẵn sàng tiếp nhận kết nối trước khi ASP.NET Core khởi động và cố gắng thực hiện database migration. Cấu hình retry của Pomelo EF Core (`MaxRetryCount = 3`, `MaxRetryDelay = 5s`) xử lý trường hợp container `db` chậm khởi động hơn dự kiến do tải hệ thống.

### b) Quản lý thông tin bí mật qua biến môi trường

Một nguyên tắc bắt buộc trong triển khai production là không hardcode thông tin bí mật vào source code hay image Docker. Tất cả các giá trị nhạy cảm của hệ thống — bao gồm khóa ký JWT, Zone API Key và mật khẩu cơ sở dữ liệu — được truyền vào container tại thời điểm khởi động thông qua biến môi trường. Giá trị thực được lưu trong file `.env` trên máy chủ vật lý; file này được thêm vào `.gitignore` và không bao giờ được đưa vào repository:

```yaml
# docker-compose.yml — cấu hình môi trường cho container api
services:
  api:
    build: ./GameServerApi
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - Jwt__Key=${JWT_SECRET}              # khóa ký JWT, đọc từ .env
      - Jwt__Issuer=GameServerApi           # hardcode — khớp với Program.cs
      - Jwt__Audience=GameClient
      - ZoneApiKey=${ZONE_API_KEY}          # khóa xác thực Zone Server
      - ConnectionStrings__GameDB=Server=db;Database=${MYSQL_DATABASE};User=${MYSQL_USER};Password=${MYSQL_PASSWORD};Port=3306
    depends_on:
      db:
        condition: service_healthy
```

File `appsettings.Production.json` trong source code chỉ khai báo cấu trúc, không chứa giá trị thực. Runtime của .NET 9 tự động đọc và ghi đè từ biến môi trường của container theo quy tắc ánh xạ tên (`Jwt__Key` trong environment tương ứng với `Jwt.Key` trong cấu hình). Bí mật thực chỉ tồn tại trên máy chủ vật lý và trong bộ nhớ container trong thời gian chạy, không xuất hiện trong bất kỳ file nào thuộc repository hay image Docker.

### c) Quy trình cập nhật hệ thống không gián đoạn

Để hỗ trợ cập nhật nhanh trong môi trường production mà không gây gián đoạn phiên chơi đang diễn ra, hệ thống sử dụng script `deploy.sh` tự động hóa toàn bộ quy trình theo ba bước tuần tự:

```bash
#!/bin/bash
# deploy.sh — Triển khai phiên bản mới lên môi trường production
set -e     # Dừng ngay nếu bất kỳ lệnh nào thất bại

# Bước 1: Lấy mã nguồn mới nhất từ repository
git pull --ff-only   # --ff-only: từ chối nếu không fast-forward, tránh merge conflict ẩn

# Bước 2: Rebuild và khởi động lại container api
#   --build   : buộc build lại image từ Dockerfile mới nhất
#   --no-deps : KHÔNG khởi động lại các container phụ thuộc (db, unity)
#   -d        : chạy ở chế độ nền (detached)
docker compose up -d --build --no-deps api

# Bước 3: Dọn dẹp image Docker cũ không còn được sử dụng để giải phóng dung lượng
docker image prune -f --filter "dangling=true"   # chỉ xóa untagged image, không xóa image đang dùng
```

Tham số `--no-deps` là điểm then chốt trong quy trình này: nó đảm bảo container `db` và container `unity` không bị khởi động lại trong suốt quá trình cập nhật. Người chơi đang trong phiên chơi tiếp tục kết nối bình thường với Zone Server trong khi container `api` đang được rebuild và restart. Chỉ các yêu cầu REST API đang được xử lý trong khoảng thời gian container `api` khởi động lại — thường dưới 5 giây — mới bị gián đoạn; Unity Client có cơ chế retry tự động cho các yêu cầu không nhận được phản hồi, đảm bảo người chơi không nhận thấy sự gián đoạn trong phần lớn trường hợp.

### f) Kiểm định dữ liệu đầu vào hai tầng (Input Validation)

Nguyên tắc bảo mật "Defense in Depth" yêu cầu dữ liệu đầu vào phải được kiểm tra tại hai điểm độc lập: phía client trước khi gửi yêu cầu, và phía server trước khi xử lý nghiệp vụ. Kiểm tra phía client cải thiện trải nghiệm người dùng bằng cách thông báo lỗi tức thì mà không cần round-trip mạng; kiểm tra phía server là tuyến phòng thủ bắt buộc vì client có thể bị bỏ qua hoặc giả mạo. Đây là biện pháp phòng chống nhóm "Injection" (A03) và "Security Misconfiguration" (A05) trong OWASP Top 10:2021.

#### Tầng server — Data Annotations trên DTO

Lớp `LoginRequest` và `RegisterRequest` trong `AuthDtos.cs` khai báo ràng buộc trực tiếp qua thuộc tính Data Annotations. Nhờ `[ApiController]` trên controller, ASP.NET Core tự động kiểm tra `ModelState` trước khi thực thi action method và trả về HTTP 400 Bad Request kèm danh sách lỗi nếu bất kỳ ràng buộc nào bị vi phạm — không cần viết code kiểm tra thủ công trong mỗi action:

```csharp
// GameServerApi/Models/DTOs/AuthDtos.cs
using System.ComponentModel.DataAnnotations;

public class LoginRequest
{
    [Required]
    [MinLength(3), MaxLength(30)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(6), MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    [Required]
    [MinLength(3), MaxLength(30)]
    [RegularExpression(@"^[a-zA-Z0-9_]+$",
        ErrorMessage = "Username chỉ được chứa chữ cái, chữ số và dấu gạch dưới.")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6), MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}
```

`[RegularExpression]` trên trường `Username` ngăn các ký tự đặc biệt có thể được sử dụng để thực hiện tấn công injection thông qua tên tài khoản. `[EmailAddress]` xác nhận định dạng email đúng cú pháp. `[MinLength]` / `[MaxLength]` ngăn mật khẩu quá ngắn (dễ đoán) và các chuỗi cực dài có thể gây tốn tài nguyên khi BCrypt xử lý. Hệ thống EF Core + Pomelo sử dụng truy vấn tham số hóa cho mọi truy cập cơ sở dữ liệu, loại bỏ nguy cơ SQL Injection ngay tại tầng ORM.

#### Tầng client — Kiểm tra trong Unity trước khi gọi API

`RegisterController` và `LoginController` trong Unity thực hiện kiểm tra tương đương phía client trước khi gửi bất kỳ yêu cầu HTTP nào, đảm bảo người chơi nhận phản hồi lỗi tức thì thay vì phải chờ round-trip mạng:

```csharp
// Client/Assets/Scripts/UI/Auth/RegisterController.cs — trích đoạn OnRegisterClicked()
string username = usernameInput.text.Trim();
string email    = emailInput.text.Trim();

if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
{ ShowError("Vui lòng nhập đầy đủ thông tin!"); return; }

if (username.Length < 3 || username.Length > 30)
{ ShowError("Tên đăng nhập phải từ 3 đến 30 ký tự!"); return; }

if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$"))
{ ShowError("Tên đăng nhập chỉ được chứa chữ cái, số và dấu gạch dưới!"); return; }

if (!IsValidEmail(email))
{ ShowError("Email không hợp lệ!"); return; }

if (password.Length < 6)
{ ShowError("Mật khẩu phải có ít nhất 6 ký tự!"); return; }
```

```csharp
// Client/Assets/Scripts/UI/Auth/LoginController.cs — trích đoạn OnLoginClicked()
string username = usernameInput.text.Trim();

if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
{ ShowError("Vui lòng nhập đầy đủ thông tin!"); return; }

if (username.Length < 3 || username.Length > 30)
{ ShowError("Tên đăng nhập phải từ 3 đến 30 ký tự!"); return; }

if (password.Length < 6)
{ ShowError("Mật khẩu phải có ít nhất 6 ký tự!"); return; }

loginButton.interactable = false;   // vô hiệu hóa ngay, tránh gửi trùng lặp
```

Sau khi người dùng nhấn nút xác nhận, nút được vô hiệu hóa ngay lập tức (`interactable = false`) cho đến khi nhận được phản hồi từ server. Cơ chế này được áp dụng nhất quán trên toàn bộ các màn hình thực hiện yêu cầu mạng — bao gồm `LoginController`, `RegisterController`, `GeneUpgradePanel`, `UpgradePanel` (cường hóa trang bị) và `HybridFusionPanel` (tổng hợp Kim Phong) — ngăn người dùng gửi nhiều yêu cầu trùng lặp trong khi yêu cầu đầu tiên đang được xử lý.

---

## Bảng tổng hợp kiểm thử các biện pháp bảo mật

Sau khi hiện thực hóa, mỗi biện pháp bảo mật được kiểm thử bằng cách tái hiện trực tiếp kịch bản tấn công tương ứng nhằm xác nhận kết quả hoạt động đúng với thiết kế. Bảng 3.X dưới đây mô tả phương pháp kiểm thử thủ công và kết quả kỳ vọng cho từng biện pháp.

**Bảng 3.X — Kịch bản kiểm thử các biện pháp bảo mật đã hiện thực hóa**

| STT | Biện pháp bảo mật | Kịch bản kiểm thử | Kết quả kỳ vọng |
|-----|-------------------|-------------------|-----------------|
| 1 | `[Authorize]` trên `EnemyController` | Gửi `GET /api/enemy` không có header `Authorization` | HTTP 401 Unauthorized, body rỗng |
| 2 | `[Authorize]` trên `EnemyController` | Gửi `GET /api/enemy` với JWT hợp lệ | HTTP 200 OK, trả về danh sách kẻ địch |
| 3 | Rate Limiting đăng nhập | Gửi 6 yêu cầu `POST /api/auth/login` liên tiếp trong 60 giây từ cùng một IP | Yêu cầu thứ 6 nhận HTTP 429, không truy vấn cơ sở dữ liệu |
| 4 | Zone API Key — khóa sai | Gửi `X-Zone-Api-Key` sai đến endpoint nội bộ | HTTP 401 Unauthorized |
| 5 | Zone API Key — timing | Gửi nhiều khóa sai với số ký tự đúng tăng dần, đo thời gian phản hồi | Thời gian phản hồi không có xu hướng tăng theo số ký tự đúng |
| 6 | NGO Connection — payload quá lớn | Kết nối đến Zone Server với payload > 2.048 byte | Kết nối bị từ chối, `response.Approved = false` |
| 7 | NGO Connection — JWT không hợp lệ | Kết nối đến Zone Server với JWT sai chữ ký trong payload | Kết nối bị từ chối, `response.Approved = false` |
| 8 | NGO Connection — JWT hết hạn | Kết nối đến Zone Server với JWT đã quá 7 ngày | Kết nối bị từ chối, `response.Approved = false` |
| 9 | ErrorHandlingMiddleware | Kích hoạt exception chưa xử lý trong controller | HTTP 500, body chứa `{ "success": false, "error": "Đã xảy ra lỗi hệ thống...", "errorCode": 500 }`, không có stack trace |
| 10 | SQL Injection qua EF Core | Nhập `' OR '1'='1` vào tham số truy vấn | Truy vấn tham số hóa ngăn SQL injection, không có kết quả bất thường |
| 11 | Input Validation — server | Gửi `POST /api/auth/register` với `Username = "ab"` (2 ký tự) | HTTP 400 Bad Request, không lưu dữ liệu vào cơ sở dữ liệu |
| 12 | Input Validation — server | Gửi `POST /api/auth/register` với `Username = "abc!@#"` (ký tự đặc biệt) | HTTP 400 Bad Request, thông báo vi phạm `[RegularExpression]` |
| 13 | Input Validation — client | Nhập `Username = "ab"` trong màn hình đăng ký rồi nhấn Đăng Ký | Thông báo lỗi hiện ngay trên UI, không gửi yêu cầu HTTP |
| 14 | UI anti-spam | Nhấn nút Đăng Nhập nhiều lần liên tiếp trước khi có phản hồi | Nút bị vô hiệu hóa sau lần nhấn đầu tiên, không gửi yêu cầu trùng lặp |

Kết quả kiểm thử thủ công cho thấy tất cả mười bốn kịch bản đều hoạt động đúng theo thiết kế. Sáu biện pháp kết hợp tạo thành một tuyến phòng thủ nhiều lớp: lớp transport ngăn kết nối trái phép vào Zone Server ngay từ bước handshake; lớp network làm chậm tấn công dò mật khẩu xuống mức không khả thi về mặt thực tế; lớp application kiểm soát quyền truy cập từng endpoint và phân tách rõ ràng hai loại caller; lớp data kiểm định định dạng và giới hạn dữ liệu đầu vào tại cả server lẫn client; và lớp presentation ngăn lộ thông tin kỹ thuật nhạy cảm. Sự kết hợp này đáp ứng các yêu cầu phi chức năng về bảo mật đã đặt ra trong giai đoạn thiết kế hệ thống.
