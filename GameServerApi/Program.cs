using System.Text;
using GameServerApi.Data;
using GameServerApi.Middleware;
using GameServerApi.Services;
using GameServerApi.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// In-memory cache: dùng cho spawn-config, enemy data (tránh gọi DB thừa)
builder.Services.AddMemoryCache();

// ── Application services ──────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService,   AuthService>();
builder.Services.AddScoped<IPlayerService, PlayerService>();

// DbContext
builder.Services.AddDbContext<GameDbContext>(options =>
{
    // Thử các connection string khác nhau nếu cần
    var connectionString = builder.Configuration.GetConnectionString("GameDB");
    
    // Nếu không có connection string, thử alternative
    if (string.IsNullOrEmpty(connectionString))
    {
        connectionString = builder.Configuration.GetConnectionString("GameDB_WithPassword");
    }
    
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("Không tìm thấy ConnectionString 'GameDB' trong appsettings.json");
    }
    
    // Sử dụng MySQL 8.0 hoặc MariaDB 10.5+ (tương thích với hầu hết các phiên bản)
    // Nếu gặp lỗi version, có thể thử: ServerVersion.Create(new Version(8, 0, 21), ServerType.MySql)
    var serverVersion = ServerVersion.AutoDetect(connectionString);
    options.UseMySql(connectionString, serverVersion, mySqlOptions =>
    {
        mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
    });
});

// JWT Authentication
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? "DEV_KEY_CHANGE_ME";
var jwtIssuer = jwtSection["Issuer"] ?? "GameServerApi";
var jwtAudience = jwtSection["Audience"] ?? "GameClient";

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

var app = builder.Build();

// Tự động tạo database và migrations nếu chưa tồn tại
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<GameDbContext>();
        dbContext.Database.EnsureCreated();
        Console.WriteLine("✓ Database đã được kiểm tra/tạo thành công.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "✗ Lỗi khi tạo database. Vui lòng kiểm tra:");
        logger.LogError("  1. MySQL/MariaDB đã được cài đặt và đang chạy chưa?");
        logger.LogError("  2. Connection string trong appsettings.json có đúng không?");
        logger.LogError("  3. Kiểm tra MySQL service đã chạy chưa? (thường là MySQL80 hoặc MariaDB)");
        logger.LogError("  4. Kiểm tra username và password trong connection string có đúng không?");
        throw;
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
