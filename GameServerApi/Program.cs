using System.Text;
using GameServerApi.Auth;
using GameServerApi.Data;
using GameServerApi.Hubs;
using GameServerApi.Middleware;
using GameServerApi.Services;
using GameServerApi.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ── Kestrel: lắng nghe trên tất cả interface (0.0.0.0) ───────────────────────
// Cho phép client từ bất kỳ đâu kết nối tới API, không chỉ localhost.
// Có thể override bằng --urls="http://0.0.0.0:5000" hoặc biến môi trường ASPNETCORE_URLS.
var urls = builder.Configuration["Urls"] ?? "http://0.0.0.0:5000";
builder.WebHost.UseUrls(urls);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// ── SignalR: real-time chat ───────────────────────────────────────────────────
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, GameUserIdProvider>();

// ── CORS: cho phép Unity client gọi API từ bất kỳ origin ─────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// In-memory cache: dùng cho spawn-config, enemy data (tránh gọi DB thừa)
builder.Services.AddMemoryCache();

builder.Services.AddAuthorization();

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
        options.DefaultAuthenticateScheme = "HybridAuth";
        options.DefaultChallengeScheme = "HybridAuth";
    })
    .AddPolicyScheme("HybridAuth", "JWT hoặc Zone API key", options =>
    {
        options.ForwardDefaultSelector = context =>
            context.Request.Headers.ContainsKey(ZoneApiKeyAuthenticationHandler.HeaderName)
                ? ZoneApiKeyAuthenticationHandler.SchemeName
                : JwtBearerDefaults.AuthenticationScheme;
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
        // SignalR WebSocket: JWT không thể đặt trong header → dùng query param ?access_token=
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken)
                    && (path.StartsWithSegments("/chathub") || path.StartsWithSegments("/partyhub")))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    })
    .AddScheme<AuthenticationSchemeOptions, ZoneApiKeyAuthenticationHandler>(
        ZoneApiKeyAuthenticationHandler.SchemeName,
        _ => { });

var app = builder.Build();

// Tự động tạo database và migrations nếu chưa tồn tại
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<GameDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        dbContext.Database.EnsureCreated();

        // Normalize legacy NULL string data so older rows do not crash EF string materialization.
        var repairStatements = new (string Label, string Sql)[]
        {
            ("dungeon_config.description", "UPDATE dungeon_config SET description = '' WHERE description IS NULL"),
            ("dungeon_config.scene_name", "UPDATE dungeon_config SET scene_name = '' WHERE scene_name IS NULL"),
            ("dungeon_config.reward_json", "UPDATE dungeon_config SET reward_json = '{{}}' WHERE reward_json IS NULL OR reward_json = ''"),
            ("dungeon_config.thumbnail_icon_id", "UPDATE dungeon_config SET thumbnail_icon_id = '' WHERE thumbnail_icon_id IS NULL"),
            ("map_config.map_name", "UPDATE map_config SET map_name = CONCAT('Map ', map_id) WHERE map_name IS NULL OR map_name = ''"),
            ("map_config.scene_name", "UPDATE map_config SET scene_name = '' WHERE scene_name IS NULL"),
            ("map_config.spawn_points_json", "UPDATE map_config SET spawn_points_json = '[]' WHERE spawn_points_json IS NULL OR spawn_points_json = ''"),
            // Đồng bộ map_config.scene_name cho dungeon maps: lấy scene_name đúng từ dungeon_config
            ("map_config↔dungeon_config scene sync",
             "UPDATE map_config mc INNER JOIN dungeon_config dc ON mc.map_id = dc.map_id AND dc.is_active = 1 " +
             "SET mc.scene_name = dc.scene_name " +
             "WHERE CAST(mc.scene_name AS CHAR) COLLATE utf8mb4_general_ci != CAST(dc.scene_name AS CHAR) COLLATE utf8mb4_general_ci " +
             "AND dc.scene_name != ''")
        };

        foreach (var repair in repairStatements)
        {
            int affected = dbContext.Database.ExecuteSqlRaw(repair.Sql);
            if (affected > 0)
            {
                logger.LogWarning("Startup data repair normalized {Count} row(s) in {Label}.", affected, repair.Label);
            }
        }

        bool hasActiveDungeons = dbContext.DungeonConfigs.AsNoTracking().Any(d => d.IsActive);
        if (!hasActiveDungeons)
        {
            bool hasRequiredDungeonMaps = dbContext.MapConfigs.AsNoTracking()
                .Count(m => m.MapId == 110 || m.MapId == 111) == 2;

            if (hasRequiredDungeonMaps)
            {
                int seeded = dbContext.Database.ExecuteSqlRaw(
                    "INSERT INTO dungeon_config " +
                    "(dungeon_id, dungeon_name, dungeon_type, map_id, scene_name, max_players, min_level_required, time_limit_seconds, description, boss_enemy_id, reward_json, thumbnail_icon_id, is_active) VALUES " +
                    "(6, 'Phó Bản Sóng', 'solo', 110, 'DungeonWaveScene', 1, 1, 0, '', NULL, '{{}}', '', 1), " +
                    "(7, 'Phó Bản Tổ Đội', 'multi', 111, 'DungeonPartyScene', 4, 1, 0, '', NULL, '{{}}', '', 1)");

                logger.LogWarning("Startup dungeon seed inserted {Count} default row(s).", seeded);
            }
            else
            {
                logger.LogWarning("Skipped default dungeon seed because map_id 110 and 111 were not both present in map_config.");
            }
        }

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

// Bỏ HTTPS redirect khi chạy production HTTP (nếu cần HTTPS thì dùng reverse proxy)
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ── SignalR Chat Hub ──────────────────────────────────────────────────────────
app.MapHub<GameServerApi.Hubs.ChatHub>("/chathub");
app.MapHub<GameServerApi.Hubs.PartyHub>("/partyhub");

app.Run();
