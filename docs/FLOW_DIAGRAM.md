# 🔄 FLOW DIAGRAM - Sơ đồ luồng xử lý

## 🎯 Tổng quan
Tài liệu này mô tả chi tiết flow từ Login → Character Creation → Main Scene → Spawn Character.

---

## 📊 FLOW 1: LOGIN VÀ VÀO GAME (User đã có player_data)

```
┌─────────────────────────────────────────────────────────────────┐
│                    LOGIN SCENE                                  │
└─────────────────────────────────────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  User nhập username + password    │
        │  Click "Login" button              │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  LoginController.OnLoginClicked()  │
        │  - Validate input                  │
        │  - Disable login button            │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  APIClient.Login()                │
        │  POST /api/auth/login             │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  Server API Response:              │
        │  { token, user_id, username }     │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  Lưu vào PlayerPrefs:             │
        │  - JWT_TOKEN = token              │
        │  - USER_ID = user_id              │
        │  - USERNAME = username            │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  APIClient.LoadPlayerData()      │
        │  GET /api/player/{userId}/data     │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  Server API Response:              │
        │  PlayerDataResponse (full data)   │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  GameManager.SetPlayerData()     │
        │  - Save player data               │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  Set flags:                       │
        │  - CONNECT_TO_SERVER = "true"     │
        │  - SERVER_IP = "127.0.0.1"        │
        │  - SERVER_PORT = 2003            │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  SceneManager.LoadScene("Main")  │
        └───────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                    MAIN SCENE                                    │
└─────────────────────────────────────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  MainSceneNetworkInitializer      │
        │  .Start()                         │
        │  - Check GameManager.HasPlayerData()│
        │  - If yes: TryConnectToServer()  │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  NetworkManagerCustom             │
        │  .ConnectToServer()               │
        │  - Set UnityTransport IP/Port      │
        │  - NetworkManager.StartClient()   │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  Client Connected to Server        │
        │  (Dedicated Server)                │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  ClientAuthHandler                │
        │  .OnNetworkSpawn()                 │
        │  - SendAuthServerRpc(token, userId)│
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  ServerPlayerDataManager          │
        │  .LoadPlayerDataForClient()       │
        │  - Load from API                  │
        │  - Cache: clientId → PlayerData  │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  NetworkPlayerSpawner             │
        │  .OnClientConnectedCallback()    │
        │  - Wait for player data           │
        │  - Get prefab (element + gender)  │
        │  - Spawn at position or spawn point│
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  Player Spawned Successfully!     │
        │  - Correct prefab                 │
        │  - Correct position               │
        │  - Network synced                 │
        └───────────────────────────────────┘
```

---

## 📊 FLOW 2: LOGIN VÀ TẠO NHÂN VẬT (User chưa có player_data)

```
┌─────────────────────────────────────────────────────────────────┐
│                    LOGIN SCENE                                  │
└─────────────────────────────────────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  Login → LoadPlayerData()         │
        │  → Server trả về 404              │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  Load Scene "SelectElement"       │
        └───────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│              SELECTELEMENT SCENE                                 │
└─────────────────────────────────────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  User chọn:                       │
        │  - Element (Fire/Water/etc)       │
        │  - Gender (Male/Female)            │
        │  - Character Name                 │
        │  Click "Confirm"                   │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  SelectElementController           │
        │  .OnConfirmButtonClicked()         │
        │  - Validate input                 │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  APIClient.CreatePlayer()         │
        │  POST /api/player/create           │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  Server API Response:              │
        │  PlayerDataResponse (new player)   │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  GameManager.SetPlayerData()     │
        │  - Save new player data           │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  Enable "Go" button               │
        │  Show success message              │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  User clicks "Go" button           │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  APIClient.LoadPlayerData()       │
        │  GET /api/player/{userId}/data     │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  GameManager.SetPlayerData()     │
        │  Set flags: CONNECT_TO_SERVER     │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  SceneManager.LoadScene("Main")  │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  (Tiếp tục như FLOW 1)            │
        │  Main Scene → Connect → Spawn    │
        └───────────────────────────────────┘
```

---

## 📊 FLOW 3: SPAWN CHARACTER (Chi tiết)

```
┌─────────────────────────────────────────────────────────────────┐
│  CLIENT SIDE                                                    │
└─────────────────────────────────────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  NetworkPlayer Prefab Spawned     │
        │  (NetworkObject spawned)          │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  ClientAuthHandler                 │
        │  .OnNetworkSpawn()                 │
        │  - IsOwner && IsClient?            │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  SendAuthServerRpc()              │
        │  - token (JWT)                     │
        │  - userId                          │
        └───────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│  SERVER SIDE                                                    │
└─────────────────────────────────────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  ServerPlayerDataManager          │
        │  .LoadPlayerDataForClient()       │
        │  - Receive clientId, userId       │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  Check cache first                │
        │  - If cached: return cached data  │
        │  - If not: load from API          │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  APIClient.LoadPlayerData()      │
        │  (Server-side API call)           │
        │  GET /api/player/{userId}/data     │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  Cache player data:               │
        │  - clientIdToUserId[clientId]     │
        │  - clientIdToPlayerData[clientId]  │
        │  - playerDataCache[userId]         │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  NetworkPlayerSpawner             │
        │  .OnClientConnectedCallback()     │
        │  - Wait for player data           │
        │  (Retry every 0.5s, max 10 times) │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  GetPlayerPrefabForClient()       │
        │  - Get playerData from cache      │
        │  - Extract element_type + gender  │
        │  - Select prefab:                  │
        │    Fire_Male, Fire_Female, etc.    │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  Calculate spawn position:        │
        │  - If position_x/y != 0:          │
        │    Spawn at (position_x, position_y)│
        │  - Else:                          │
        │    Spawn at spawn point           │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  Instantiate prefab                │
        │  - At calculated position          │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  NetworkObject.SpawnWithOwnership()│
        │  - clientId as owner              │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  Player Spawned!                   │
        │  - Correct prefab                 │
        │  - Correct position               │
        │  - Network synced                 │
        └───────────────────────────────────┘
```

---

## 🔑 KEY POINTS

### 1. Data Storage
- **PlayerPrefs**: JWT_TOKEN, USER_ID, USERNAME, CONNECT_TO_SERVER, SERVER_IP, SERVER_PORT
- **GameManager**: PlayerDataResponse (current player data)
- **ServerPlayerDataManager**: Cache player data per client (server-side)

### 2. Scene Transitions
- **Login** → **Main** (nếu có player_data)
- **Login** → **SelectElement** (nếu chưa có player_data)
- **SelectElement** → **Main** (sau khi tạo nhân vật)

### 3. Network Flow
- Client connect → Send auth → Server load data → Server spawn player
- Player prefab được chọn dựa trên `element_type` + `gender` từ server data
- Position được lấy từ `position_x`, `position_y` hoặc spawn point

### 4. Error Handling
- Login fail → Show error, enable login button
- LoadPlayerData 404 → Go to SelectElement
- LoadPlayerData other error → Go to Login
- Server not ready → Show error message

---

## 📝 NOTES

- Tất cả API calls đều async (Coroutines)
- Network spawn chỉ xảy ra trên server-side
- Client chỉ nhận spawned player qua network sync
- Player data được cache trên server để tránh gọi API nhiều lần

---

**Last Updated**: [Date]
**Version**: 1.0
