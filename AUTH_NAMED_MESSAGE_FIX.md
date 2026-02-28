# 🔧 FIX: Server không nhận Named Message Auth từ Client

## ❌ VẤN ĐỀ

Client gửi auth qua Named Message NHƯNG server KHÔNG nhận được:

```
[NetworkManagerCustom] ✓ Auth message sent to server    ← Client gửi
...
[NetworkPlayerSpawner] ✗ No player data for clientId 1  ← Server không có data
```

**Nguyên nhân:** `RegisterAuthMessageHandler()` không được gọi hoặc gọi sai timing.

---

## ✅ GIẢI PHÁP ĐÃ IMPLEMENT

### 1. RegisterAuthMessageHandler() được gọi NGAY sau StartHost()

**File:** [GameSceneNetworkInitializer.cs](Client/Assets/Scripts/Network/GameSceneNetworkInitializer.cs#L454-L461) 

```csharp
// Start host
networkManager.StartHost();

// CRITICAL: Đăng ký Named Message handler NGAY sau StartHost()
if (networkManagerSingleton != null && networkManagerSingleton.IsServer)
{
    Debug.Log("[GameSceneNetworkInitializer] Server started. Registering auth handler...");
    networkManager.RegisterAuthMessageHandler();
}
```

**Tại sao:** `OnServerStarted` callback có thể fire quá nhanh hoặc không fire → phải gọi trực tiếp.

### 2. Thêm debug logs trong RegisterAuthMessageHandler()

**File:** [NetworkManagerCustom.cs](Client/Assets/Scripts/Network/Shared/NetworkManagerCustom.cs#L191-L217)

```csharp
public void RegisterAuthMessageHandler()
{
    if (networkManager == null)
    {
        Debug.LogError("[NetworkManagerCustom] NetworkManager is NULL!");
        return;
    }
    
    if (!networkManager.IsServer)
    {
        Debug.LogWarning($"[NetworkManagerCustom] Not server!");
        return;
    }
    
    if (authMessageHandlerRegistered)
    {
        Debug.Log("[NetworkManagerCustom] Already registered, skipping...");
        return;
    }

    networkManager.CustomMessagingManager.RegisterNamedMessageHandler(AUTH_MESSAGE_NAME, OnAuthMessageReceived);
    authMessageHandlerRegistered = true;
    Debug.Log("[NetworkManagerCustom] ✓ Registered Named Message handler for ClientAuth");
}
```

---

## 🧪 TEST & VERIFY

### Bước 1: Restart Unity

```powershell
# Đóng Unity
# Xóa cache (optional)
Remove-Item -Recurse -Force "Client\Library\ScriptAssemblies"
# Mở lại Unity
```

### Bước 2: Test Play (Host + Client cùng máy)

**Nhấn Play trong Unity → Start Host → Start Client**

### Bước 3: Kiểm tra Console Logs

✅ **PHẢI THẤY:**

```
=== HOST SIDE ===
[GameSceneNetworkInitializer] Server started. Registering auth handler...
[NetworkManagerCustom] ✓ Registered Named Message handler for ClientAuth

=== CLIENT SIDE ===
[NetworkManagerCustom] Client-side: Sending auth immediately via Named Message
[NetworkManagerCustom] ===== SENDING AUTH VIA NAMED MESSAGE =====
[NetworkManagerCustom] UserId: 1, Token length: 216
[NetworkManagerCustom] ✓ Auth message sent to server

=== SERVER SIDE (sau khi nhận) ===  
[NetworkManagerCustom] ===== AUTH MESSAGE RECEIVED =====
[NetworkManagerCustom] SenderClientId: 1
[NetworkManagerCustom] UserId: 1
[NetworkManagerCustom] Token length: 216
[ServerPlayerDataManager] Loading player data for client 1...
[ServerPlayerDataManager] ✓ Player data loaded: 1231

=== PLAYER SPAWN ===
[NetworkPlayerSpawner] ✓ Player data found for clientId 1
[NetworkPlayerSpawner] Spawning player with prefab: MetalPrefab
```

❌ **KHÔNG được thấy:**

```
[NetworkPlayerSpawner] ✗ No player data found for clientId 1     ← BAD!
[NetworkPlayerSpawner] ✗ Player data NOT loaded after 120 attempts  ← BAD!
```

---

## 🐛 TROUBLESHOOTING

### Lỗi: "RegisterAuthMessageHandler: NetworkManager is NULL!"

**Nguyên nhân:** NetworkManagerCustom.networkManager chưa được assign.

**Giải pháp:** 
- Đảm bảo NetworkManagerCustom.Awake()/Start() đã chạy
- Kiểm tra NetworkManager.Singleton có tồn tại

### Lỗi: "RegisterAuthMessageHandler: Not server"

**Nguyên nhân:** `IsServer = false` khi gọi RegisterAuthMessageHandler().

**Giải pháp:** 
- Gọi SAU khi `networkManager.StartHost()` thành công
- Check `networkManagerSingleton.IsServer` trước khi gọi

### Lỗi: "Already registered, skipping..."

**Nguyên nhân:** Handler đã được register rồi (OK, không phải lỗi).

**Note:** Đây là log thông báo, không phải error.

### Vẫn không nhận được auth message

**Kiểm tra:**

1. **Client có gửi đúng message name không?**
   ```csharp
   // Phải match AUTH_MESSAGE_NAME = "ClientAuth"
   ```

2. **CustomMessagingManager có sẵn sàng chưa?**
   ```csharp
   if (networkManager.CustomMessagingManager == null)
   {
       Debug.LogError("CustomMessagingManager is NULL!");
   }
   ```

3. **Timing issue:**
   - Client gửi quá sớm trước khi handler register → fixed bằng cách register ngay sau StartHost()
   - Client gửi sau khi disconnect → check clientId valid

4. **Serialization issue:**
   ```csharp
   // Check format của FixedString512Bytes
   ForceNetworkSerializeByMemcpy<FixedString512Bytes> tokenWrapper = ...;
   ```

---

## 📊 FLOW SUMMARY

```
1. Host: StartHost()
   ↓
2. Host: RegisterAuthMessageHandler() ← CRITICAL! Ngay sau StartHost()
   ↓
3. Client: Connect to Host
   ↓
4. Client: OnClientConnected → SendAuthToServer()
   ↓
5. Host: OnAuthMessageReceived() ← Nếu không gọi = handler chưa register!
   ↓
6. Host: ServerPlayerDataManager.LoadPlayerDataForClient()
   ↓
7. Host: Cache player data with clientId as key
   ↓
8. NetworkPlayerSpawner: Đợi player data → Spawn player
```

**KEY POINT:** Bước 2 phải chạy trước bước 5, nếu không auth message bị mất!

---

## ✅ CHECKLIST

- [x] RegisterAuthMessageHandler() được gọi trong GameSceneNetworkInitializer ngay sau StartHost()
- [x] Thêm debug logs để trace registration flow
- [x] Verify IsServer = true trước khi register
- [x] Prevent duplicate registration với authMessageHandlerRegistered flag
- [ ] **User: Test lại trong Unity**
- [ ] **User: Verify logs cho thấy auth được nhận**
- [ ] **User: Verify player spawn thành công**

---

**NEXT:** Chạy Unity, test Host + Client, verify console logs!
