# Hướng Dẫn Các Bước Tiếp Theo

> **Trạng thái hiện tại (VPS `98.70.26.19`):**
> - ✅ `gameserver-db` (MariaDB 10.6) → `healthy`
> - ✅ `gameserver-api` (.NET 9, port 5000) → `healthy`
> - ⏳ `gameserver-unity` → **Chờ upload Unity Linux Server Build**

---

## Bước 1 — Build Unity Dedicated Server (Trên Máy Local Windows)

### 1.1 Cài module Linux Server

Mở **Unity Hub** → **Installs** → chọn Unity version đang dùng → Add modules:
- ✅ **Linux Dedicated Server Build Support**

### 1.2 Cấu hình Build Settings trong Unity Editor

1. `File` → `Build Settings`
2. Chọn:
   - **Platform:** `Dedicated Server`
   - **Target OS:** `Linux`
3. **Scenes In Build** — chỉ giữ lại **1 scene duy nhất:**
   - `Scenes/ServerScene` (index 0)
   - ❌ Xóa hết các scene client (Main, Login, Game...)
4. `Player Settings` → `Other Settings` → `Scripting Define Symbols`:
   - Thêm: `UNITY_SERVER`
5. Nhấn **Build**
6. Đặt tên output: `GameServer`
7. Chọn thư mục output: `Client/build/LinuxServer/`

**Kết quả sau khi build:**
```
Client/build/LinuxServer/
├── GameServer              ← file thực thi
├── GameServer_Data/
│   └── StreamingAssets/
└── UnityPlayer.so
```

---

## Bước 2 — Upload Unity Server Build Lên VPS

Mở **PowerShell** hoặc **Terminal** trên máy Windows:

```powershell
# Upload thư mục build vào đúng chỗ docker đang mount
scp -r "C:\Hub\DoAn\Client\build\LinuxServer\GameServer" azureuser@98.70.26.19:/home/azureuser/DoAn/DoAn/unity-server/
```

> **Lưu ý:** Nếu thư mục `unity-server/` chưa có → VPS tự tạo sẵn rồi.

Sau khi upload xong, **SSH vào VPS** và cấp quyền chạy:

```bash
ssh azureuser@98.70.26.19
chmod +x /home/azureuser/DoAn/DoAn/unity-server/GameServer/GameServer.x86_64
```

---

## Bước 3 — Start Unity Dedicated Server Docker

```bash
cd /home/azureuser/DoAn/DoAn
docker compose up unity -d
```

Kiểm tra log:
```bash
docker logs -f gameserver-unity
```

**Kết quả mong đợi trong log:**
```
Starting Unity Dedicated Server on port 7777...
[ServerBootstrap] ✓✓✓ Dedicated Server started successfully on 0.0.0.0:7777
```

---

## Bước 4 — Config Client Unity (Trên Máy Local)

File này đã được cập nhật sẵn tại `Client/Assets/StreamingAssets/server_config.json`:

```json
{
    "apiBaseUrl": "http://98.70.26.19:5000",
    "gameServerIp": "98.70.26.19",
    "gameServerPort": 7777
}
```

> ✅ **Không cần sửa gì thêm** — client sẽ tự đọc file này khi chạy.
>
> ✅ **HTTP đã được bật trong Unity project** (`insecureHttpOption: Always Allowed`). Nếu Unity Editor đang mở từ trước khi tôi sửa project settings, hãy đóng và mở lại project rồi test login lại.

### Nếu muốn build client để phát cho người chơi:

1. Trong Unity Editor → `File` → `Build Settings`
2. Platform: **PC, Mac & Linux Standalone** → Target: **Windows**
3. Thêm đầy đủ scenes client vào (Main, Login, Game...)
4. Build → đặt tên `GameClient`
5. Gửi cho người chơi kèm **nguyên thư mục build** (có `GameClient_Data/StreamingAssets/server_config.json`)

---

## Bước 5 — Kiểm Tra Toàn Bộ Hệ Thống

### Trên VPS — kiểm tra tất cả container:
```bash
docker ps
```

Expected:
```
NAMES            STATUS       PORTS
gameserver-api   Up (healthy) 0.0.0.0:5000->5000/tcp
gameserver-db    Up (healthy) 127.0.0.1:3306->3306/tcp
gameserver-unity Up           0.0.0.0:7777->7777/udp
```

### Test API từ bên ngoài:
```bash
curl -X POST http://98.70.26.19:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser","email":"test@test.com","password":"Test1234!"}'
```
→ Trả về JSON có `token` là ✅

### Test port UDP 7777 (sau khi Unity server start):
```powershell
# Trên Windows (PowerShell)
Test-NetConnection -ComputerName 98.70.26.19 -Port 7777
```

---

## Quản Lý Docker Hàng Ngày

| Lệnh | Tác dụng |
|------|----------|
| `docker compose up -d` | Start tất cả container |
| `docker compose down` | Stop tất cả (giữ data DB) |
| `docker compose down -v` | Stop + **XÓA TOÀN BỘ DB DATA** |
| `docker compose logs -f api` | Xem log API realtime |
| `docker compose logs -f unity` | Xem log Unity server realtime |
| `docker compose up api --build -d` | Rebuild + restart API (sau khi sửa code) |
| `docker compose restart unity` | Restart Unity server |
| `docker ps` | Xem trạng thái các container |

---

## Khi Cập Nhật Code API (Workflow)

```bash
# 1. Sửa code trên máy local → push lên GitHub
git add . && git commit -m "update" && git push

# 2. Trên VPS — pull code mới
cd /home/azureuser/DoAn/DoAn
git pull

# 3. Rebuild và restart API
docker compose up api --build -d
```

---

## Khi Cập Nhật Unity Server Build

```powershell
# 1. Build lại trên máy local (Bước 1 ở trên)

# 2. Upload lại
scp -r "C:\Hub\DoAn\Client\build\LinuxServer\GameServer" azureuser@98.70.26.19:/home/azureuser/DoAn/DoAn/unity-server/
```

```bash
# 3. Trên VPS — restart Unity container
chmod +x /home/azureuser/DoAn/DoAn/unity-server/GameServer/GameServer.x86_64
docker compose restart unity
```

---

## Lỗi Thường Gặp

| Lỗi | Nguyên nhân | Cách sửa |
|-----|-------------|----------|
| Unity server crash ngay khi start | File `GameServer.x86_64` chưa có quyền chạy | `chmod +x unity-server/GameServer/GameServer.x86_64` |
| Unity server crash: "cannot open display" | Build bị lẫn scene có UI | Chỉ build scene `ServerScene` |
| Client không connect được game server | Unity server chưa start | Check `docker logs gameserver-unity` |
| API trả 500 | DB chưa ready | `docker compose up db -d` → đợi healthy |
| `no such file: GameServer` hoặc `GameServer.x86_64` | Chưa upload đúng thư mục build Linux | Upload lại vào `unity-server/GameServer/` |
| `Non-secure network connections disabled in Player Settings` | Unity đang chặn HTTP request | Mở lại project sau khi tôi đã set `Allow downloads over HTTP = Always Allowed` |
| Port 7777 không connect | Azure NSG chưa mở | Đã mở sẵn (rule `GameServer7777`) |

---

## Tóm Tắt TODO List

```
✅ VPS setup xong
✅ Docker DB + API đang chạy (healthy)
✅ Azure NSG đã mở 5000/TCP + 7777/UDP
✅ Client server_config.json đã trỏ tới 98.70.26.19

⏳ [ ] Build Unity Dedicated Server (Linux) trên máy local
⏳ [ ] Upload build vào unity-server/
⏳ [ ] docker compose up unity -d
⏳ [ ] Test client connect từ máy local vào VPS
```
