## Unity Server Build

Thư mục này chứa Unity Dedicated Server build (Linux).

### Cách upload:

1. Build trong Unity Editor:
   - File → Build Settings → Target Platform: **Dedicated Server** → Target OS: **Linux**
   - Chỉ thêm scene `ServerScene` (index 0)
   - Player Settings → Scripting Define Symbols: `UNITY_SERVER`
   - Build → Chọn tên: `GameServer`

2. Upload toàn bộ file build vào thư mục này:
   ```bash
   scp -r LinuxServer/* azureuser@98.70.26.19:/home/azureuser/DoAn/DoAn/unity-server/
   chmod +x /home/azureuser/DoAn/DoAn/unity-server/GameServer
   ```

3. Cấu trúc sau khi upload:
   ```
   unity-server/
   ├── GameServer              ← executable (phải có +x)
   ├── GameServer_Data/
   │   └── StreamingAssets/    ← Docker sẽ tự tạo server_config.json
   └── UnityPlayer.so
   ```
