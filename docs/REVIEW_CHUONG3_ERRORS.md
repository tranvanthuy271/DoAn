# BÁO CÁO KIỂM TRA LỖI — CHUONG3_KIEN_TRUC_THEM_VAO.md

> Ngày kiểm tra: 31/05/2026  
> Phương pháp: Đối chiếu từng đoạn code trong file báo cáo với source code thực tế trong project.  
> Tổng số vấn đề phát hiện: **14 lỗi dữ liệu kỹ thuật**, **0 lỗi chính tả nghiêm trọng**.

---

## TÓM TẮT NHANH

| Mức độ | Số lượng | Mô tả |
|--------|----------|-------|
| 🔴 Sai nghiêm trọng | 5 | Code snippet sai hoàn toàn so với thực tế, nếu giảng viên kiểm tra sẽ phát hiện |
| 🟡 Sai một phần | 6 | Tên biến/method/route sai, ý tưởng đúng |
| 🟢 Sai nhỏ / khác biệt | 3 | Chi tiết implementation khác nhưng không ảnh hưởng logic |

---

## CHI TIẾT TỪNG LỖI

### LỖI 1 🔴 — EnemyController: Sai dependency injection và method signatures hoàn toàn

**Báo cáo viết:**
```csharp
private readonly IEnemyService _enemyService;

public EnemyController(IEnemyService enemyService)
    => _enemyService = enemyService;

public async Task<ActionResult<IEnumerable<Enemy>>> GetAllEnemies()
    => Ok(await _enemyService.GetAllAsync());

public async Task<ActionResult<Enemy>> GetEnemy(int id)
    => Ok(await _enemyService.GetByIdAsync(id));

public async Task<ActionResult<IEnumerable<Enemy>>> GetEnemiesByLevel(int level)
    => Ok(await _enemyService.GetByLevelAsync(level));
```

**Thực tế trong `GameServerApi/Controllers/EnemyController.cs`:**
```csharp
private readonly GameDbContext _db;  // inject DbContext trực tiếp, không có IEnemyService

public EnemyController(GameDbContext db) { _db = db; }

public async Task<IActionResult> GetAllEnemies()
{
    var enemies = await _db.Enemies.ToListAsync();
    return Ok(new { enemies = result });  // trả về anonymous object, không phải Enemy[]
}

public async Task<IActionResult> GetEnemy(int enemyId)    // param tên là enemyId, không phải id
    // ...

[HttpGet("by-level/{level}")]   // route thực tế
public async Task<IActionResult> GetEnemiesByLevel(int level)
```

**Cần sửa:** Không có interface `IEnemyService` trong project. Controller query DB trực tiếp qua EF Core. Return type là `IActionResult`, không phải generic `ActionResult<T>`. Route thứ ba là `by-level/{level}` không phải `level/{level:int}`.

---

### LỖI 2 🔴 — AuthController Login: Sai tên method và pattern hoàn toàn

**Báo cáo viết:**
```csharp
var user = await _userRepository.FindByUsernameAsync(request.Username);
if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
    return Unauthorized(new { message = "Tên đăng nhập hoặc mật khẩu không đúng" });

var token = _jwtService.GenerateToken(user);
return Ok(new LoginResponse { Token = token });
```

**Thực tế trong `GameServerApi/Controllers/AuthController.cs`:**
```csharp
// Không có _userRepository, inject GameDbContext _db trực tiếp
var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
if (user == null) return Unauthorized("Sai username hoặc password.");

// Dùng _authService (IAuthService), không phải _jwtService
if (!_authService.VerifyPassword(request.Password, user.PasswordHash))
    return Unauthorized("Sai username hoặc password.");

var token = _authService.GenerateJwtToken(user);  // GenerateJwtToken, không phải GenerateToken

return Ok(new { token = token, user_id = user.UserId, username = user.Username });
// Không có class LoginResponse — trả về anonymous object
```

**Cần sửa:** Không có `_userRepository` hay `_jwtService`. Dùng `_db` (EF Core) và `_authService` (IAuthService). Method là `VerifyPassword` + `GenerateJwtToken`. Response là anonymous object không phải `LoginResponse`.

---

### LỖI 3 🔴 — ZoneApiKeyAuthenticationHandler: Sai nguồn đọc config và sai cách FixedTimeEquals

**Báo cáo viết:**
```csharp
var providedKey = providedKeyValues.ToString();
var expectedKey = _options.ZoneApiKey;

var provided = Encoding.UTF8.GetBytes(providedKey.PadRight(expectedKey.Length));
var expected = Encoding.UTF8.GetBytes(expectedKey);

if (!CryptographicOperations.FixedTimeEquals(provided, expected))
```

**Thực tế trong `GameServerApi/Auth/ZoneApiKeyAuthenticationHandler.cs`:**
```csharp
// Không có _options.ZoneApiKey — đọc từ IConfiguration
string expectedKey = _configuration["ZoneApiKey"] ?? string.Empty;

// Không dùng PadRight — dùng private method SecureEquals()
private static bool SecureEquals(string left, string right)
{
    byte[] leftBytes  = Encoding.UTF8.GetBytes(left);
    byte[] rightBytes = Encoding.UTF8.GetBytes(right);
    if (leftBytes.Length != rightBytes.Length) return false;  // ← kiểm tra độ dài trước
    return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
}
```

**Cần sửa:** Không có `_options.ZoneApiKey` (không có Options class riêng). Cấu hình đọc qua `IConfiguration`. Không có kỹ thuật `PadRight` — thực tế kiểm tra độ dài trước rồi mới `FixedTimeEquals`. Logic được bọc trong private method `SecureEquals()`.

---

### LỖI 4 🔴 — Zone Server endpoint authorization: Sai attribute

**Báo cáo viết:**
> Các endpoint dành riêng cho Zone Server được đánh dấu thêm `[Authorize(Roles = "GameServer")]`

```csharp
// ví dụ: POST /api/gameplaycommand/award-exp
[Authorize(Roles = "GameServer")]
```

**Thực tế:**
```csharp
// GameServerApi/Controllers/DungeonRewardController.cs, line 14:
[Authorize(AuthenticationSchemes = ZoneApiKeyAuthenticationHandler.SchemeName)]

// GameServerApi/Controllers/QuestController.cs, line 192 (action cụ thể):
[Authorize(AuthenticationSchemes = ZoneApiKeyAuthenticationHandler.SchemeName)]
```

Chỉ có `ZoneServerController` dùng `[Authorize(Roles = "GameServer")]`, còn phần lớn endpoint nội bộ dùng `AuthenticationSchemes` chỉ định scheme `"ZoneApiKey"` trực tiếp. Đây là hai cơ chế hoàn toàn khác nhau.

**Cần sửa:** Thay `[Authorize(Roles = "GameServer")]` bằng `[Authorize(AuthenticationSchemes = "ZoneApiKey")]` trong mô tả. Giải thích đúng là: đặt `AuthenticationSchemes` buộc endpoint đó chỉ chấp nhận xác thực từ scheme `ZoneApiKey`, không chấp nhận JWT người chơi.

---

### LỖI 5 🔴 — ErrorHandlingMiddleware: Sai format JSON response

**Báo cáo viết:**
```csharp
var body = JsonSerializer.Serialize(
    new { message = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau." });
```
→ Báo cáo ngầm hiểu response là `{ "message": "..." }`

**Thực tế trong `GameServerApi/Middleware/ErrorHandlingMiddleware.cs`:**
```csharp
var response = ApiResponse.Fail("Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.", 500);
var json = JsonSerializer.Serialize(response, ...);
```

Class `ApiResponse` (`GameServerApi/Models/Responses/ApiResponse.cs`) serialize ra:
```json
{
  "success": false,
  "data": null,
  "error": "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.",
  "errorCode": 500
}
```

**Cần sửa:** Field là `error` (không phải `message`), kèm thêm `success: false` và `errorCode: 500`. Trong bảng kiểm thử dòng 9 ghi `{ "message": "..." }` cũng sai tương tự.

---

### LỖI 6 🟡 — Docker Compose: Cổng db không phải "chỉ mạng nội bộ"

**Báo cáo viết:**
> `db` | `mariadb:10.6` | **3306 (chỉ mạng nội bộ, không mở ra host)**

**Thực tế trong `docs/docker-compose.yml`:**
```yaml
ports:
  - "127.0.0.1:3306:3306"   # Chỉ bind localhost, KHÔNG expose ra ngoài
```

**Sự khác biệt:** `127.0.0.1:3306:3306` nghĩa là cổng 3306 **được bind vào localhost của máy chủ vật lý** — administrator có thể kết nối bằng `mysql -h 127.0.0.1` từ chính máy chủ đó. Đây không phải "hoàn toàn cô lập trong mạng nội bộ Docker" mà là "chỉ accessible từ localhost host". Mô tả "không mở ra ngoài" đúng về mặt internet nhưng sai về mặt kỹ thuật Docker.

**Cần sửa:** Thay mô tả thành "3306 bind localhost (127.0.0.1:3306:3306 — chỉ truy cập được từ localhost máy chủ)".

---

### LỖI 7 🟡 — Docker Compose: Unity container không ánh xạ port 7777

**Báo cáo viết:**
> `unity` | `ubuntu:22.04` | **7777 UDP → 7777 (host)**

**Thực tế:**
```yaml
unity:
  image: ubuntu:22.04
  network_mode: host   # ← dùng host network, không có port mapping
```

Với `network_mode: host`, container Unity chia sẻ trực tiếp network namespace với host — mọi cổng container lắng nghe đều tự động accessible trên host mà không cần khai báo `ports`. Không có dòng `ports` nào trong cấu hình Unity container.

**Cần sửa:** Thay "7777 UDP → 7777" bằng "`network_mode: host` (chia sẻ network namespace với host, cổng tự động được expose)".

---

### LỖI 8 🟡 — Docker Compose: Sai tên biến môi trường và connection string key

**Báo cáo viết:**
```yaml
- Jwt__Issuer=${JWT_ISSUER}
- ConnectionStrings__DefaultConnection=Server=db;...
```

**Thực tế:**
```yaml
- Jwt__Issuer: GameServerApi          # hardcode, không phải env var
- ConnectionStrings__GameDB: "Server=db;Database=..."  # key là GameDB, không phải DefaultConnection
```

Trong `Program.cs`: `builder.Configuration.GetConnectionString("GameDB")` — tên key là `GameDB`.

**Cần sửa:** Connection string key là `ConnectionStrings__GameDB`. `Jwt__Issuer` hardcode là `"GameServerApi"`.

---

### LỖI 9 🟡 — deploy.sh: Sai lệnh git pull và docker image prune

**Báo cáo viết:**
```bash
git pull origin main
docker image prune -f
```

**Thực tế trong `deploy.sh`:**
```bash
git pull --ff-only          # không chỉ định branch, dùng --ff-only
docker image prune -f --filter "dangling=true"   # filter dangling only
```

`--ff-only` đảm bảo không tự động merge — sẽ fail nếu có conflict thay vì tạo merge commit. `--filter "dangling=true"` chỉ xóa image không có tag, không xóa image đang dùng.

---

### LỖI 10 🟡 — Program.cs middleware: Bỏ sót điều kiện HTTPS redirect

**Báo cáo viết:**
```csharp
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseCors();
app.UseRateLimiter();
```

**Thực tế:**
```csharp
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();   // ← có thêm bước này khi Development
}

app.UseCors("AllowAll");   // ← tên policy là "AllowAll", không phải không tên
app.UseRateLimiter();
```

Thứ tự đúng nhưng thiếu `UseHttpsRedirection` và tên policy CORS là `"AllowAll"`.

---

### LỖI 11 🟢 — EnemyController endpoint URL thứ ba sai

**Báo cáo viết (bảng kiểm thử row 1):**
> Gửi `GET /api/enemy` không có header

Điều này đúng. Nhưng nếu kiểm thử endpoint theo level, URL đúng là:

- Báo cáo ngầm hiểu: `GET /api/enemy/level/5`  
- Thực tế: `GET /api/enemy/by-level/5`

(Route `[HttpGet("by-level/{level}")]` thay vì `[HttpGet("level/{level:int}")]`)

---

### LỖI 12 🟢 — Mô tả JWT middleware tên không chuẩn

**Báo cáo viết:**
> middleware `JwtBearerAuthentication` trong pipeline ASP.NET Core

**Thực tế:** Tên chính xác trong ASP.NET Core là `JwtBearerHandler` (handler) hoặc đơn giản là "JWT Bearer middleware" được kích hoạt qua `UseAuthentication()`. Không có class tên `JwtBearerAuthentication`.

---

### LỖI 13 🟢 — ErrorHandlingMiddleware: Thiếu mention `ApiResponse` wrapper

**Báo cáo viết:**
> Middleware... ghi thông tin đầy đủ vào log nội bộ... chỉ trả về client một thông báo chung chung

Về ý tưởng đúng, nhưng phần code snippet không nhắc đến class `ApiResponse` — đây là convention thống nhất cho mọi response trong project, không phải chỉ error response. Bỏ qua điều này khiến người đọc không hiểu tại sao JSON có `success`, `error`, `errorCode` thay vì chỉ `message`.

---

### LỖI 14 🟢 — Mô tả `ConnectionApprovalResponse.Reason` — field không tồn tại trong NGO API

**Báo cáo viết:**
```csharp
response.Reason = "Payload too large";
response.Reason = "Invalid payload format";
```

**Thực tế NGO API:** `NetworkManager.ConnectionApprovalResponse` trong NGO 1.x **không có** property `Reason`. Chỉ có `Approved` (bool), `CreatePlayerObject` (bool), `Position`, `Rotation`, `PlayerPrefabHash`. Lý do từ chối không được gửi về client — chỉ là internal logic.

---

## DANH SÁCH SỬA ĐỔI ĐỀ XUẤT

| # | Vị trí trong file | Hành động |
|---|-------------------|-----------|
| 1 | Mục a) code EnemyController | Thay toàn bộ code snippet bằng code thực từ file |
| 2 | Mục b) code Login | Thay `_userRepository`, `_jwtService` → `_db`, `_authService` |
| 3 | Mục c) code ZoneApiKeyHandler | Thay `_options.ZoneApiKey` → `_configuration["ZoneApiKey"]`; thay PadRight → SecureEquals() |
| 4 | Mục c) đoạn về Zone Server endpoint | Thay `[Authorize(Roles = "GameServer")]` → `[Authorize(AuthenticationSchemes = "ZoneApiKey")]` |
| 5 | Mục e) response JSON | Thay `{ "message": "..." }` → `{ "success": false, "error": "...", "errorCode": 500 }` |
| 6 | Bảng 3.X Docker compose | Sửa cột "Cổng ánh xạ" cho db và unity |
| 7 | Mục b) 3.3.6 env vars | Sửa `Jwt__Issuer`, `ConnectionStrings__DefaultConnection` |
| 8 | Mục c) 3.3.6 deploy.sh | Sửa 2 lệnh git và prune |
| 9 | Mục d) ApprovalCheck code | Xóa `response.Reason = "..."` — field không tồn tại |
| 10 | Bảng kiểm thử row 9 | Sửa format JSON kỳ vọng |

---

## CÁC PHẦN KHÔNG CÓ LỖI

- Cấu trúc mục, tiêu đề, thứ tự các biện pháp ✅  
- Giải thích khái niệm OWASP A01 và A05 ✅  
- Lý do kỹ thuật của `FixedTimeEquals` (timing attack) ✅  
- Giải thích Rate Limiting FixedWindow ✅  
- Thứ tự middleware pipeline (về mặt ý tưởng) ✅  
- Giải thích `--no-deps` trong deploy ✅  
- Mô tả BCrypt work factor 12, 250–400ms ✅  
- Cấu hình Rate Limiting `Window=60s`, `PermitLimit=5` ✅  
- `[Authorize]` ở cấp class cho EnemyController ✅  
- `[EnableRateLimiting("login")]` trên Login action ✅  
- Sơ đồ xác thực lai (hybrid scheme) ✅  
- `mariadb:10.6` image ✅  
- `Jwt__Key=${JWT_SECRET}` ✅  
- `docker compose up -d --build --no-deps api` ✅  
- Chính tả tiếng Việt: không phát hiện lỗi đáng kể ✅
