# Docker Deployment Guide - Game Server

## Tổng Quan

```
┌──────────────────────────────────────────────────────────────┐
│                    Docker Compose (VPS)                       │
│                                                              │
│  ┌──────────────┐  ┌──────────────┐  ┌────────────────────┐ │
│  │  MariaDB 10.6 │  │ GameServerApi│  │ Unity Dedicated    │ │
│  │  (gamedb)    │←─│ (.NET 9)     │  │ Server (Netcode)   │ │
│  │  Port: 3306  │  │ Port: 5000   │  │ Port: 7777/UDP     │ │
│  │  (internal)  │  │ (public TCP) │  │ (public UDP)       │ │
│  └──────────────┘  └──────────────┘  └────────────────────┘ │
│         ↑                ↑                    ↑              │
│    gameserver-net   gameserver-net       gameserver-net       │
└──────────────────────────┼────────────────────┼──────────────┘
                           │                    │
              ┌────────────┼────────────────────┼──────────┐
              │  Azure NSG │                    │          │
              │  5000/TCP ✓│  7777/UDP ✓        │  22/TCP ✓│
              └────────────┼────────────────────┼──────────┘
                           │                    │
                    Unity Client (connect)
```

## Quick Start

```bash
# 1. Clone repo
git clone https://github.com/tranvanthuy271/DoAn.git
cd DoAn

# 2. Start DB + API
docker compose up db api -d --build

# 3. Upload Unity server build (từ máy local)
#    scp -r LinuxServer/* azureuser@98.70.26.19:/home/azureuser/DoAn/DoAn/unity-server/
#    chmod +x unity-server/GameServer

# 4. Start tất cả (sau khi có Unity build)
docker compose up -d

# 5. Check status
docker ps
docker logs gameserver-api
docker logs gameserver-db
```

## Cấu Trúc Thư Mục Cần Thiết

```
DoAn/
├── .env                          ← Secrets (KHÔNG commit git)
├── docker-compose.yml            ← Docker orchestration
├── GameServerApi/                ← REST API source code
│   ├── Dockerfile
│   ├── .dockerignore
│   ├── GameServerApi.csproj      ← Reconstructed (bị .gitignore)
│   ├── appsettings.json          ← Base config
│   ├── appsettings.Production.json ← Production overrides
│   ├── Program.cs
│   ├── Controllers/
│   ├── Data/
│   ├── Models/
│   ├── Services/
│   ├── Middleware/
│   └── gamedb.sql                ← DB schema + seed data
├── unity-server/                 ← Unity Dedicated Server build (Linux)
│   ├── GameServer                ← Executable
│   ├── GameServer_Data/
│   └── UnityPlayer.so
└── Client/                       ← Unity Client project (KHÔNG chạy trên VPS)
    └── Assets/
        └── StreamingAssets/
            └── server_config.json  ← Client kết nối tới VPS
```

## Quản Lý Docker

```bash
# Start tất cả
docker compose up -d

# Stop tất cả
docker compose down

# Xem logs realtime
docker compose logs -f api
docker compose logs -f db

# Restart sau khi sửa code API
docker compose up api --build -d

# Reset DB (XÓA TOÀN BỘ DATA)
docker compose down -v
docker compose up -d

# Xem status
docker compose ps
```

## Azure NSG (Đã cấu hình)

| Port | Protocol | Rule Name | Status |
|------|----------|-----------|--------|
| 22 | TCP | SSH | ✅ Đã mở |
| 5000 | TCP | 5000 | ✅ Đã mở |
| 7777 | UDP | GameServer7777 | ✅ Đã mở |

## Client Config

### Cách 1: File `server_config.json` (Khuyến nghị)

Tạo/sửa file `Client/Assets/StreamingAssets/server_config.json`:
```json
{
    "apiBaseUrl": "http://98.70.26.19:5000",
    "gameServerIp": "98.70.26.19",
    "gameServerPort": 7777
}
```

Sau khi build client (Windows), file này nằm ở:
- `GameClient_Data/StreamingAssets/server_config.json`

### Cách 2: ServerAddressConfig asset (Unity Editor)

1. Mở Unity Editor
2. Assets → Create → DoAn → ServerAddressConfig
3. Di chuyển vào `Assets/Resources/`
4. Sửa trong Inspector:
   - Api Base Url: `http://98.70.26.19:5000`
   - Game Server Ip: `98.70.26.19`
   - Game Server Port: `7777`

### Cách 3: Nhập IP trong game (ConnectionUI)

Game có UI nhập IP - player nhập `98.70.26.19` trực tiếp.

## Troubleshooting

```bash
# API không start
docker logs gameserver-api

# DB connection error
docker exec gameserver-db mysql -u gameuser -p'PASSWORD' gamedb -e "SHOW TABLES;"

# Check ports
ss -tuln | grep -E "5000|7777|3306"

# Test API từ bên ngoài
curl http://98.70.26.19:5000/api/auth/register -X POST \
  -H "Content-Type: application/json" \
  -d '{"username":"test","email":"t@t.com","password":"Test1234!"}'
```
