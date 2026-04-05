# Hướng Dẫn Deploy Game Server Lên VPS

> Tài liệu chi tiết để triển khai **GameServerApi** (REST API .NET 9) và **Game Server** (Unity Netcode Dedicated Server) lên VPS, cho phép client ở bất kỳ đâu connect tới.

---

## Mục Lục

1. [Tổng Quan Kiến Trúc](#1-tổng-quan-kiến-trúc)
2. [Yêu Cầu VPS](#2-yêu-cầu-vps)
3. [Cài Đặt Môi Trường VPS](#3-cài-đặt-môi-trường-vps)
4. [Triển Khai GameServerApi](#4-triển-khai-gameserverapi)
5. [Build Unity Dedicated Server](#5-build-unity-dedicated-server)
6. [Upload & Chạy Game Server Trên VPS](#6-upload--chạy-game-server-trên-vps)
7. [Cấu Hình Client (server_config.json)](#7-cấu-hình-client-server_configjson)
8. [Script Khởi Động Tự Động](#8-script-khởi-động-tự-động)
9. [Kiểm Tra & Debug](#9-kiểm-tra--debug)
10. [Bảo Mật](#10-bảo-mật)
11. [Tóm Tắt Nhanh (Checklist)](#11-tóm-tắt-nhanh-checklist)

---

## 1. Tổng Quan Kiến Trúc

```
┌──────────────────────────────────────────────────────────┐
│                        VPS                               │
│                                                          │
│  ┌─────────────────────┐   ┌──────────────────────────┐  │
│  │  GameServerApi       │   │  Unity Dedicated Server  │  │
│  │  (.NET 9 REST API)  │   │  (ServerScene build)     │  │
│  │  Port 5000 (TCP)    │   │  Port 7777 (UDP)         │  │
│  │                     │   │                           │  │
│  │  - Auth / JWT       │   │  - Netcode for GO        │  │
│  │  - Player data      │   │  - Map / Zone logic      │  │
│  │  - Item / Inventory │   │  - Enemy spawner         │  │
│  │  - NPC / Shop       │   │  - Real-time gameplay    │  │
│  └─────────┬───────────┘   └──────────┬───────────────┘  │
│            │                          │                   │
│            │  MySQL/MariaDB (3306)    │                   │
│            └──────────┬───────────────┘                   │
│                       │                                   │
└───────────────────────┼───────────────────────────────────┘
                        │
          ┌─────────────┼─────────────┐
          │             │             │
     ┌────▼───┐   ┌────▼───┐   ┌────▼───┐
     │Client 1│   │Client 2│   │Client 3│
     │(Unity) │   │(Unity) │   │(Unity) │
     └────────┘   └────────┘   └────────┘
```

**Ports cần mở:**
| Port | Protocol | Mục đích |
|------|----------|----------|
| 5000 | TCP      | GameServerApi (REST API) |
| 7777 | UDP      | Unity Netcode Game Server |
| 3306 | TCP      | MySQL/MariaDB (chỉ localhost, KHÔNG mở ra ngoài) |
| 22   | TCP      | SSH (quản trị) |

---

## 2. Yêu Cầu VPS

| Thông số | Tối thiểu | Khuyến nghị |
|----------|-----------|-------------|
| OS | Ubuntu 22.04+ / Debian 12+ | Ubuntu 24.04 LTS |
| RAM | 2 GB | 4 GB+ |
| CPU | 2 vCPU | 4 vCPU |
| Disk | 20 GB | 40 GB SSD |
| Network | 100 Mbps | 1 Gbps |

> **Lưu ý:** Unity Dedicated Server chạy headless (không cần GPU).

---

## 3. Cài Đặt Môi Trường VPS

### 3.1 SSH vào VPS

```bash
ssh root@YOUR_VPS_IP
```

### 3.2 Cập nhật hệ thống

```bash
apt update && apt upgrade -y
```

### 3.3 Cài .NET 9 SDK

```bash
# Thêm Microsoft package repository
wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

apt update
apt install -y dotnet-sdk-9.0

# Kiểm tra
dotnet --version
```

### 3.4 Cài MySQL / MariaDB

```bash
apt install -y mariadb-server
systemctl enable mariadb
systemctl start mariadb

# Bảo mật MySQL
mysql_secure_installation
```

Sau đó tạo database và user:

```bash
mysql -u root -p
```

```sql
CREATE DATABASE gamedb CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER 'gameuser'@'localhost' IDENTIFIED BY 'YOUR_STRONG_PASSWORD';
GRANT ALL PRIVILEGES ON gamedb.* TO 'gameuser'@'localhost';
FLUSH PRIVILEGES;
EXIT;
```

### 3.5 Mở Firewall

```bash
# UFW (Ubuntu Firewall)
ufw allow 22/tcp
ufw allow 5000/tcp
ufw allow 7777/udp
ufw enable
ufw status
```

> **QUAN TRỌNG:** KHÔNG mở port 3306 ra ngoài. MySQL chỉ nên lắng nghe localhost.

---

## 4. Triển Khai GameServerApi

### 4.1 Cấu hình `appsettings.Production.json`

File đã được tạo tại `GameServerApi/appsettings.Production.json`. **Trước khi deploy, sửa các giá trị sau:**

```json
{
  "ConnectionStrings": {
    "GameDB": "Server=localhost;Database=gamedb;User=gameuser;Password=YOUR_STRONG_PASSWORD;Port=3306"
  },
  "Jwt": {
    "Key": "THAY_BANG_SECRET_KEY_DAI_IT_NHAT_32_KY_TU_RANDOM",
    "Issuer": "GameServerApi",
    "Audience": "GameClient"
  },
  "ZoneApiKey": "THAY_BANG_ZONE_API_KEY_RANDOM",
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

**Sinh key ngẫu nhiên (chạy trên VPS):**

```bash
# Sinh JWT secret (64 ký tự)
openssl rand -base64 48

# Sinh Zone API key
openssl rand -hex 32
```

### 4.2 Publish GameServerApi

**Trên máy local (Windows), chạy:**

```powershell
cd C:\Hub\DoAn\GameServerApi
dotnet publish -c Release -o ./publish
```

Thư mục `GameServerApi/publish/` sẽ chứa toàn bộ file cần deploy.

### 4.3 Upload lên VPS

```bash
# Từ máy local (PowerShell / terminal)
scp -r C:\Hub\DoAn\GameServerApi\publish\ root@YOUR_VPS_IP:/opt/gameserver/api/
scp C:\Hub\DoAn\GameServerApi\appsettings.Production.json root@YOUR_VPS_IP:/opt/gameserver/api/
```

### 4.4 Chạy thử GameServerApi trên VPS

```bash
cd /opt/gameserver/api
ASPNETCORE_ENVIRONMENT=Production dotnet GameServerApi.dll
```

Kiểm tra: `curl http://YOUR_VPS_IP:5000/api/health` (nếu có endpoint health) hoặc bất kỳ endpoint nào.

### 4.5 Tạo Systemd Service (chạy tự động khi VPS restart)

```bash
nano /etc/systemd/system/gameserver-api.service
```

Nội dung:

```ini
[Unit]
Description=Game Server API (.NET 9)
After=network.target mariadb.service

[Service]
Type=notify
WorkingDirectory=/opt/gameserver/api
ExecStart=/usr/bin/dotnet /opt/gameserver/api/GameServerApi.dll
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000
Restart=always
RestartSec=5
User=www-data
Group=www-data

[Install]
WantedBy=multi-user.target
```

```bash
# Phân quyền thư mục cho www-data
chown -R www-data:www-data /opt/gameserver/api

# Kích hoạt service
systemctl daemon-reload
systemctl enable gameserver-api
systemctl start gameserver-api
systemctl status gameserver-api
```

---

## 5. Build Unity Dedicated Server

### 5.1 Cài đặt Linux Dedicated Server module

Trong **Unity Hub** → Installs → Cài thêm module:
- **Linux Dedicated Server Build Support**

### 5.2 Cấu hình Build Settings

1. Mở Unity → **File → Build Settings**
2. **Target Platform:** `Dedicated Server`
3. **Target OS:** `Linux`
4. Chỉ thêm scene `ServerScene` vào build (Scenes In Build):
   - `Scenes/ServerScene` (index 0)
5. **Player Settings → Other Settings → Scripting Define Symbols:** thêm `UNITY_SERVER`

### 5.3 Build

1. Click **Build**
2. Chọn thư mục: ví dụ `Client/build/LinuxServer/`
3. Đặt tên executable: `GameServer`

Kết quả:
```
Client/build/LinuxServer/
├── GameServer                    ← executable
├── GameServer_Data/
│   ├── StreamingAssets/
│   │   └── server_config.json    ← config file (QUAN TRỌNG)
│   └── ...
└── UnityPlayer.so
```

### 5.4 Cấu hình `server_config.json` cho VPS

**Sửa file `Client/build/LinuxServer/GameServer_Data/StreamingAssets/server_config.json`:**

```json
{
    "apiBaseUrl": "http://YOUR_VPS_IP:5000",
    "gameServerIp": "YOUR_VPS_IP",
    "gameServerPort": 7777
}
```

> **Thay `YOUR_VPS_IP` bằng IP public thực tế của VPS.** Ví dụ: `103.45.67.89`

---

## 6. Upload & Chạy Game Server Trên VPS

### 6.1 Upload Unity Server Build

```bash
# Từ máy local
scp -r C:\Hub\DoAn\Client\build\LinuxServer\ root@YOUR_VPS_IP:/opt/gameserver/unity/
```

### 6.2 Cấp quyền chạy

```bash
chmod +x /opt/gameserver/unity/GameServer
```

### 6.3 Chạy thử Game Server

```bash
cd /opt/gameserver/unity
./GameServer -batchmode -nographics \
  --port=7777 \
  --publicIp=YOUR_VPS_IP \
  --apiUrl=http://localhost:5000/api
```

> **Lưu ý:**
> - `-batchmode -nographics`: Chạy headless, không cần màn hình/GPU
> - `--apiUrl=http://localhost:5000/api`: API trên cùng VPS nên dùng localhost (nhanh hơn, không qua NAT)
> - `--publicIp=YOUR_VPS_IP`: IP public để client biết connect tới đâu
> - `--port=7777`: Port UDP cho Netcode

### 6.4 Tạo Systemd Service cho Unity Server

```bash
nano /etc/systemd/system/gameserver-unity.service
```

```ini
[Unit]
Description=Unity Dedicated Game Server
After=network.target gameserver-api.service

[Service]
Type=simple
WorkingDirectory=/opt/gameserver/unity
ExecStart=/opt/gameserver/unity/GameServer -batchmode -nographics --port=7777 --publicIp=YOUR_VPS_IP --apiUrl=http://localhost:5000/api
Restart=always
RestartSec=10
User=gameserver
Group=gameserver

[Install]
WantedBy=multi-user.target
```

```bash
# Tạo user riêng cho game server (bảo mật)
useradd -r -s /bin/false gameserver
chown -R gameserver:gameserver /opt/gameserver/unity

# Kích hoạt service
systemctl daemon-reload
systemctl enable gameserver-unity
systemctl start gameserver-unity
systemctl status gameserver-unity
```

---

## 7. Cấu Hình Client (server_config.json)

### Cách 1: Sửa `ServerAddressConfig` asset trong Unity Editor (cho build mặc định)

1. Mở Unity Editor
2. **Assets → Create → DoAn → ServerAddressConfig**
3. Đặt tên: `ServerAddressConfig`
4. **Di chuyển vào** `Assets/Resources/` (tạo thư mục `Resources` nếu chưa có)
5. Chọn asset, sửa trong Inspector:

| Field | Giá trị Local | Giá trị VPS |
|-------|---------------|-------------|
| Api Base Url | `http://localhost:5000` | `http://YOUR_VPS_IP:5000` |
| Game Server Ip | `127.0.0.1` | `YOUR_VPS_IP` |
| Game Server Port | `7777` | `7777` |

### Cách 2: Dùng `server_config.json` (override runtime — không cần rebuild)

Sau khi build client, sửa file:
- **Windows:** `GameClient_Data/StreamingAssets/server_config.json`
- **Trong Editor:** `Assets/StreamingAssets/server_config.json`

```json
{
    "apiBaseUrl": "http://YOUR_VPS_IP:5000",
    "gameServerIp": "YOUR_VPS_IP",
    "gameServerPort": 7777
}
```

> **Ưu tiên:** `server_config.json` sẽ **ghi đè** giá trị trong `ServerAddressConfig` asset. Rất tiện khi muốn phát client cho nhiều người test — chỉ cần gửi kèm file config mới.

### Cách 3: Sửa trực tiếp `ConnectionUI` trong game

Nếu game có UI nhập IP (ConnectionUI), player có thể nhập IP VPS trực tiếp.

---

## 8. Script Khởi Động Tự Động

### 8.1 Script "1 lệnh chạy cả 2" (cho dev test nhanh trên VPS)

```bash
nano /opt/gameserver/start_all.sh
```

```bash
#!/bin/bash
# start_all.sh — Khởi động cả API + Unity Server

echo "═══════════════════════════════════════"
echo "  GAME SERVER STARTUP"
echo "═══════════════════════════════════════"

VPS_IP="YOUR_VPS_IP"
API_PORT=5000
GAME_PORT=7777

# 1. Start GameServerApi
echo "[1/2] Starting GameServerApi..."
cd /opt/gameserver/api
ASPNETCORE_ENVIRONMENT=Production \
ASPNETCORE_URLS=http://0.0.0.0:${API_PORT} \
nohup dotnet GameServerApi.dll > /var/log/gameserver-api.log 2>&1 &
API_PID=$!
echo "  → API PID: $API_PID (port $API_PORT)"

# Đợi API sẵn sàng
echo "  → Waiting for API to be ready..."
for i in $(seq 1 30); do
    if curl -sf http://localhost:${API_PORT} > /dev/null 2>&1; then
        echo "  → API is ready!"
        break
    fi
    sleep 1
done

# 2. Start Unity Dedicated Server
echo "[2/2] Starting Unity Game Server..."
cd /opt/gameserver/unity
nohup ./GameServer -batchmode -nographics \
    --port=${GAME_PORT} \
    --publicIp=${VPS_IP} \
    --apiUrl=http://localhost:${API_PORT}/api \
    > /var/log/gameserver-unity.log 2>&1 &
GAME_PID=$!
echo "  → Game Server PID: $GAME_PID (port $GAME_PORT)"

echo ""
echo "═══════════════════════════════════════"
echo "  ✓ All servers started!"
echo "  API:  http://${VPS_IP}:${API_PORT}"
echo "  Game: ${VPS_IP}:${GAME_PORT} (UDP)"
echo "═══════════════════════════════════════"
echo ""
echo "Logs:"
echo "  API:  tail -f /var/log/gameserver-api.log"
echo "  Game: tail -f /var/log/gameserver-unity.log"
echo ""
echo "Stop: kill $API_PID $GAME_PID"
```

```bash
chmod +x /opt/gameserver/start_all.sh
```

**Chạy:**

```bash
/opt/gameserver/start_all.sh
```

### 8.2 Script dừng tất cả

```bash
nano /opt/gameserver/stop_all.sh
```

```bash
#!/bin/bash
echo "Stopping all game servers..."
pkill -f "GameServerApi.dll" && echo "  → API stopped" || echo "  → API not running"
pkill -f "GameServer.*-batchmode" && echo "  → Game Server stopped" || echo "  → Game Server not running"
echo "Done."
```

```bash
chmod +x /opt/gameserver/stop_all.sh
```

### 8.3 Dùng Systemd (khuyến nghị cho production)

Nếu đã tạo 2 service ở mục 4.5 và 6.4:

```bash
# Start cả 2
systemctl start gameserver-api
systemctl start gameserver-unity

# Stop cả 2
systemctl stop gameserver-unity
systemctl stop gameserver-api

# Xem logs
journalctl -u gameserver-api -f
journalctl -u gameserver-unity -f

# Restart khi cập nhật code
systemctl restart gameserver-api
systemctl restart gameserver-unity
```

---

## 9. Kiểm Tra & Debug

### 9.1 Kiểm tra API hoạt động

```bash
# Từ VPS
curl http://localhost:5000

# Từ máy local
curl http://YOUR_VPS_IP:5000
```

### 9.2 Kiểm tra Unity Server

```bash
# Xem log
tail -f /var/log/gameserver-unity.log
# Hoặc
journalctl -u gameserver-unity -f

# Kiểm tra port đang lắng nghe
ss -tuln | grep -E "5000|7777"
```

**Kết quả mong đợi:**
```
tcp   LISTEN   0.0.0.0:5000     ← API
udp   LISTEN   0.0.0.0:7777     ← Game Server
```

### 9.3 Test connect từ client

1. Sửa `server_config.json` → IP VPS
2. Mở game trong Unity Editor hoặc build
3. Kiểm tra Console log: `[ServerAddressConfig] Runtime override applied → API=http://YOUR_VPS_IP:5000 GameServer=YOUR_VPS_IP:7777`
4. Đăng nhập → Connect → Chơi game

### 9.4 Lỗi thường gặp

| Lỗi | Nguyên nhân | Cách sửa |
|-----|-------------|----------|
| Client không connect được API | Firewall chưa mở port 5000 | `ufw allow 5000/tcp` |
| Client không connect được Game Server | Firewall chưa mở port 7777 UDP | `ufw allow 7777/udp` |
| API trả 500 Internal Server Error | Sai connection string MySQL | Kiểm tra `appsettings.Production.json` |
| `Could not find file 'server_config.json'` | File không ở đúng thư mục | Đặt trong `StreamingAssets/` hoặc cùng thư mục exe |
| `Connection refused` khi Game Server gọi API | API chưa start hoặc sai URL | Kiểm tra `--apiUrl` argument |
| Unity Server crash ngay | Thiếu dependency Linux | Cài: `apt install libgdiplus libc6-dev` |

---

## 10. Bảo Mật

### 10.1 Checklist bảo mật

- [ ] **JWT Key:** Đổi thành key mạnh (≥ 32 ký tự random) trong `appsettings.Production.json`
- [ ] **Zone API Key:** Đổi trong `appsettings.Production.json` VÀ set biến môi trường `ZONE_API_KEY` trên VPS
- [ ] **MySQL password:** Dùng password mạnh, không dùng root user
- [ ] **MySQL chỉ lắng nghe localhost:** Không mở port 3306 ra ngoài
- [ ] **SSH key-based auth:** Tắt password login cho SSH
- [ ] **UFW firewall:** Chỉ mở các port cần thiết (22, 5000, 7777)

### 10.2 Biến môi trường (thay vì hardcode trong file)

```bash
# Thêm vào /etc/environment hoặc systemd service
export JWT_SECRET="your_very_long_random_secret_key_here"
export ZONE_API_KEY="your_zone_api_key_here"
```

Hoặc thêm vào systemd service file:

```ini
[Service]
Environment=JWT_SECRET=your_very_long_random_secret_key_here
Environment=ZONE_API_KEY=your_zone_api_key_here
```

### 10.3 HTTPS (tùy chọn — khuyến nghị cho production)

Dùng **Nginx reverse proxy** + **Let's Encrypt**:

```bash
apt install -y nginx certbot python3-certbot-nginx

# Cấu hình Nginx proxy cho API
nano /etc/nginx/sites-available/gameapi
```

```nginx
server {
    listen 80;
    server_name api.yourdomain.com;

    location / {
        proxy_pass http://localhost:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

```bash
ln -s /etc/nginx/sites-available/gameapi /etc/nginx/sites-enabled/
nginx -t && systemctl reload nginx
certbot --nginx -d api.yourdomain.com
```

Sau đó client dùng: `https://api.yourdomain.com` thay cho `http://YOUR_VPS_IP:5000`.

---

## 11. Tóm Tắt Nhanh (Checklist)

### Trên VPS:

```
□ 1. Cài .NET 9 SDK
□ 2. Cài MySQL/MariaDB → Tạo database + user
□ 3. Mở firewall: 5000/tcp, 7777/udp
□ 4. Upload GameServerApi → /opt/gameserver/api/
□ 5. Sửa appsettings.Production.json (DB password, JWT key, Zone API key)
□ 6. Upload Unity Server Build → /opt/gameserver/unity/
□ 7. Sửa server_config.json trong GameServer_Data/StreamingAssets/
□ 8. Chạy: /opt/gameserver/start_all.sh
      HOẶC: systemctl start gameserver-api && systemctl start gameserver-unity
□ 9. Kiểm tra: ss -tuln | grep -E "5000|7777"
```

### Trên máy local (Client):

```
□ 1. Tạo ServerAddressConfig asset → Assets/Resources/ServerAddressConfig
□ 2. Sửa server_config.json:
     {
       "apiBaseUrl": "http://YOUR_VPS_IP:5000",
       "gameServerIp": "YOUR_VPS_IP",
       "gameServerPort": 7777
     }
□ 3. Build client (Windows) → Gửi cho người chơi
□ 4. Test connect → Xong!
```

### Cấu trúc thư mục trên VPS:

```
/opt/gameserver/
├── api/                           ← GameServerApi publish output
│   ├── GameServerApi.dll
│   ├── appsettings.json
│   ├── appsettings.Production.json  ← SỬA FILE NÀY
│   └── ...
├── unity/                         ← Unity Dedicated Server build
│   ├── GameServer                 ← executable
│   ├── GameServer_Data/
│   │   └── StreamingAssets/
│   │       └── server_config.json  ← SỬA FILE NÀY
│   └── UnityPlayer.so
├── start_all.sh                   ← Script start cả 2
└── stop_all.sh                    ← Script stop cả 2
```

---

## Tổng Kết Các File Config Cần Sửa

| File | Vị trí | Cần sửa gì |
|------|--------|-------------|
| `appsettings.Production.json` | VPS: `/opt/gameserver/api/` | DB password, JWT key, Zone API key |
| `server_config.json` | VPS: `GameServer_Data/StreamingAssets/` | `apiBaseUrl` → `http://localhost:5000` (cùng VPS) |
| `server_config.json` | Client: `StreamingAssets/` hoặc build | `apiBaseUrl` → `http://VPS_IP:5000`, `gameServerIp` → `VPS_IP` |
| `ServerAddressConfig.asset` | Unity Editor: `Assets/Resources/` | Sửa IP/port trong Inspector (hoặc để mặc định, dùng server_config.json override) |

> **Nguyên tắc:** Toàn bộ config tập trung tại `ServerAddressConfig` + `server_config.json`. Chỉ cần sửa 1-2 file, tất cả script tự động lấy đúng IP/port.
