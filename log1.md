[ServerPlayerDataManager] Creating new instance with DontDestroyOnLoad
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:Awake () (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:35)

[ServerPlayerDataManager] Initializing APIClient...
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:InitializeAPIClient () (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:63)
ServerPlayerDataManager:Awake () (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:39)

[ServerPlayerDataManager] No existing APIClient.Instance, creating new one...
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:InitializeAPIClient () (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:76)
ServerPlayerDataManager:Awake () (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:39)

[ServerPlayerDataManager] ✓ New APIClient created
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:InitializeAPIClient () (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:80)
ServerPlayerDataManager:Awake () (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:39)

[ItemTemplateManagerBootstrap] APIClient đã tồn tại
UnityEngine.Debug:Log (object)
ItemTemplateManagerBootstrap:Awake () (at Assets/Scripts/Inventory/ItemTemplateManagerBootstrap.cs:30)

[ItemTemplateManagerBootstrap] ItemTemplateManager chưa có, đang tạo...
UnityEngine.Debug:Log (object)
ItemTemplateManagerBootstrap:Awake () (at Assets/Scripts/Inventory/ItemTemplateManagerBootstrap.cs:36)

[ItemTemplateManager] ✅ Singleton initialized - GameObject: ItemTemplateManager
UnityEngine.Debug:Log (object)
ItemTemplateManager:Awake () (at Assets/Scripts/Inventory/ItemTemplateManager.cs:45)
UnityEngine.GameObject:AddComponent<ItemTemplateManager> ()
ItemTemplateManagerBootstrap:Awake () (at Assets/Scripts/Inventory/ItemTemplateManagerBootstrap.cs:39)

[ItemTemplateManagerBootstrap] ✅ Đã tạo ItemTemplateManager
UnityEngine.Debug:Log (object)
ItemTemplateManagerBootstrap:Awake () (at Assets/Scripts/Inventory/ItemTemplateManagerBootstrap.cs:41)

[IconDatabase] Loaded 7 item icons from Resources/ItemIcons
UnityEngine.Debug:Log (object)
IconDatabase:LoadAllIcons () (at Assets/Scripts/Inventory/IconDatabase.cs:53)
IconDatabase:Awake () (at Assets/Scripts/Inventory/IconDatabase.cs:31)

[ServerPlayerDataManager] ✓ APIClient verified in Start()
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:Start () (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:99)

[NetworkPrefabRegistrar] ItemPickup prefab not found! Please assign it in Inspector or make sure ItemSpawner/EnemyItemDrop exists in scene.
UnityEngine.Debug:LogWarning (object)
NetworkPrefabRegistrar:RegisterItemPickupPrefab (Unity.Netcode.NetworkManager,int&) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:216)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:94)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar] ✓ Registered 0 prefab(s) to NetworkManager
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:97)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar] ===== REGISTERED PREFABS LIST =====
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:111)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar] Total registered prefabs: 13
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:112)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar]   - Prefab: 'Enemy1' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar]   - Prefab: 'FireballProjectile' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar]   - Prefab: 'EarthPrefab' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar]   - Prefab: 'FirePrefab' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar]   - Prefab: 'MetalPrefab' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar]   - Prefab: 'MetalPrefab_1' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar]   - Prefab: 'NetworkPlayer' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar]   - Prefab: 'WaterPrefab' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar]   - Prefab: 'WoodPrefab' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar]   - Prefab: 'InventorySlot' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar]   - Prefab: 'SkillEffect' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar]   - Prefab: 'ItemPickup' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar]   - Prefab: 'AuthSenderNetworkObjectPrefab' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar] ===== END PREFABS LIST =====
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:128)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[GameSceneNetworkInitializer] Setting up host components...
UnityEngine.Debug:Log (object)
GameSceneNetworkInitializer:SetupHostComponents () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:160)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:70)

[GameSceneNetworkInitializer] ServerConnectionApproval already exists.
UnityEngine.Debug:Log (object)
GameSceneNetworkInitializer:SetupHostComponents () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:172)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:70)

[GameSceneNetworkInitializer] ServerPlayerDataManager instance already exists.
UnityEngine.Debug:Log (object)
GameSceneNetworkInitializer:SetupHostComponents () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:185)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:70)

[NetworkPrefabRegistrar] ItemPickup prefab not found! Please assign it in Inspector or make sure ItemSpawner/EnemyItemDrop exists in scene.
UnityEngine.Debug:LogWarning (object)
NetworkPrefabRegistrar:RegisterItemPickupPrefab (Unity.Netcode.NetworkManager,int&) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:216)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:94)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar] ✓ Registered 0 prefab(s) to NetworkManager
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:97)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar] ===== REGISTERED PREFABS LIST =====
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:111)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar] Total registered prefabs: 13
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:112)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar]   - Prefab: 'Enemy1' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar]   - Prefab: 'FireballProjectile' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar]   - Prefab: 'EarthPrefab' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar]   - Prefab: 'FirePrefab' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar]   - Prefab: 'MetalPrefab' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar]   - Prefab: 'MetalPrefab_1' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar]   - Prefab: 'NetworkPlayer' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar]   - Prefab: 'WaterPrefab' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar]   - Prefab: 'WoodPrefab' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar]   - Prefab: 'InventorySlot' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar]   - Prefab: 'SkillEffect' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar]   - Prefab: 'ItemPickup' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar]   - Prefab: 'AuthSenderNetworkObjectPrefab' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar] ===== END PREFABS LIST =====
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:128)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

==================== [InventoryNetworkBridge] START() ĐƯỢC GỌI! ====================
UnityEngine.Debug:Log (object)
InventoryNetworkBridge:Start () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:196)

[InventoryNetworkBridge] ✓ NetworkManager.Singleton exists
UnityEngine.Debug:Log (object)
InventoryNetworkBridge:Start () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:219)

[InventoryNetworkBridge] SubscribeToNetworkEvents() được gọi...
UnityEngine.Debug:Log (object)
InventoryNetworkBridge:SubscribeToNetworkEvents () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:246)
InventoryNetworkBridge:Start () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:223)

[InventoryNetworkBridge] ✓ Đã subscribe OnClientConnectedCallback
UnityEngine.Debug:Log (object)
InventoryNetworkBridge:SubscribeToNetworkEvents () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:259)
InventoryNetworkBridge:Start () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:223)

[InventoryNetworkBridge] Đang tìm NetworkInventory lần đầu tiên...
UnityEngine.Debug:Log (object)
InventoryNetworkBridge:Start () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:228)

[InventoryNetworkBridge] ========== FindPlayerInventory() BẮT ĐẦU ==========
UnityEngine.Debug:Log (object)
InventoryNetworkBridge:FindPlayerInventory () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:372)
InventoryNetworkBridge:Start () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:229)

[InventoryNetworkBridge] NetworkManager.SpawnManager is null! Network may not be initialized yet.
UnityEngine.Debug:LogWarning (object)
InventoryNetworkBridge:FindPlayerInventory () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:383)
InventoryNetworkBridge:Start () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:229)

[InventoryNetworkBridge] ⚠️ Chưa tìm thấy NetworkInventory trong Start(), sẽ tìm lại sau khi client connect.
UnityEngine.Debug:LogWarning (object)
InventoryNetworkBridge:Start () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:240)

[ItemTemplateManager] 🚀 Start() called - autoLoadOnStart=True
UnityEngine.Debug:Log (object)
ItemTemplateManager:Start () (at Assets/Scripts/Inventory/ItemTemplateManager.cs:57)

[ItemTemplateManager] ⏳ Đang đợi APIClient sẵn sàng...
UnityEngine.Debug:Log (object)
ItemTemplateManager/<LoadItemTemplatesWhenReady>d__12:MoveNext () (at Assets/Scripts/Inventory/ItemTemplateManager.cs:74)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
ItemTemplateManager:Start () (at Assets/Scripts/Inventory/ItemTemplateManager.cs:61)

[ItemTemplateManager] ✅ APIClient đã sẵn sàng sau 0.0s
UnityEngine.Debug:Log (object)
ItemTemplateManager/<LoadItemTemplatesWhenReady>d__12:MoveNext () (at Assets/Scripts/Inventory/ItemTemplateManager.cs:93)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
ItemTemplateManager:Start () (at Assets/Scripts/Inventory/ItemTemplateManager.cs:61)

[ItemTemplateManager] 📥 LoadItemTemplatesFromAPI() called - isLoading=False, isLoaded=False
UnityEngine.Debug:Log (object)
ItemTemplateManager:LoadItemTemplatesFromAPI () (at Assets/Scripts/Inventory/ItemTemplateManager.cs:103)
ItemTemplateManager/<LoadItemTemplatesWhenReady>d__12:MoveNext () (at Assets/Scripts/Inventory/ItemTemplateManager.cs:94)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
ItemTemplateManager:Start () (at Assets/Scripts/Inventory/ItemTemplateManager.cs:61)

[ItemTemplateManager] 🌐 Bắt đầu gọi API để load item templates...
UnityEngine.Debug:Log (object)
ItemTemplateManager:LoadItemTemplatesFromAPI () (at Assets/Scripts/Inventory/ItemTemplateManager.cs:126)
ItemTemplateManager/<LoadItemTemplatesWhenReady>d__12:MoveNext () (at Assets/Scripts/Inventory/ItemTemplateManager.cs:94)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
ItemTemplateManager:Start () (at Assets/Scripts/Inventory/ItemTemplateManager.cs:61)

[APIClient] 🌐 Sending GET request to: http://localhost:5000/api/item/templates
UnityEngine.Debug:Log (object)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:631)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
APIClient:GetItemTemplates (System.Action`1<ItemTemplateDto[]>,System.Action`1<string>) (at Assets/Scripts/API/APIClient.cs:625)
ItemTemplateManager:LoadItemTemplatesFromAPI () (at Assets/Scripts/Inventory/ItemTemplateManager.cs:128)
ItemTemplateManager/<LoadItemTemplatesWhenReady>d__12:MoveNext () (at Assets/Scripts/Inventory/ItemTemplateManager.cs:94)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
ItemTemplateManager:Start () (at Assets/Scripts/Inventory/ItemTemplateManager.cs:61)

[APIClient] ✅ Item templates response received - Length: 2505 chars
UnityEngine.Debug:Log (object)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:642)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[APIClient] 📄 Response preview: {"count":11,"item_templates":[{"id":1,"code":"SWORD_001","name":"Iron Sword","description":"A basic iron sword","category":1,"item_type":1,"stackable":false,"max_stack":1,"rarity":1,"icon_id":"client_...
UnityEngine.Debug:Log (object)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:643)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[APIClient] ✅ Parsed 11 item templates successfully
UnityEngine.Debug:Log (object)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:652)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ItemTemplateManager] 📦 OnItemTemplatesLoaded() - Received 11 templates
UnityEngine.Debug:Log (object)
ItemTemplateManager:OnItemTemplatesLoaded (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:150)
ItemTemplateManager:<LoadItemTemplatesFromAPI>b__13_0 (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:132)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:653)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ItemTemplateManager] ✅ Đã load 11 item templates thành công!
UnityEngine.Debug:Log (object)
ItemTemplateManager:OnItemTemplatesLoaded (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:165)
ItemTemplateManager:<LoadItemTemplatesFromAPI>b__13_0 (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:132)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:653)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ItemTemplateManager] 📊 Dictionary Stats - ById: 11, ByCode: 11
UnityEngine.Debug:Log (object)
ItemTemplateManager:OnItemTemplatesLoaded (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:166)
ItemTemplateManager:<LoadItemTemplatesFromAPI>b__13_0 (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:132)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:653)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ItemTemplateManager] 📋 Logging first 10 items:
UnityEngine.Debug:Log (object)
ItemTemplateManager:OnItemTemplatesLoaded (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:170)
ItemTemplateManager:<LoadItemTemplatesFromAPI>b__13_0 (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:132)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:653)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

  [1] ID=1, Name='Iron Sword', Code='SWORD_001', IconId='client_icon_1', Type=1, Stackable=False
UnityEngine.Debug:Log (object)
ItemTemplateManager:OnItemTemplatesLoaded (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:174)
ItemTemplateManager:<LoadItemTemplatesFromAPI>b__13_0 (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:132)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:653)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

  [2] ID=2, Name='Steel Sword', Code='SWORD_002', IconId='client_icon_2', Type=1, Stackable=False
UnityEngine.Debug:Log (object)
ItemTemplateManager:OnItemTemplatesLoaded (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:174)
ItemTemplateManager:<LoadItemTemplatesFromAPI>b__13_0 (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:132)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:653)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

  [3] ID=3, Name='Wooden Bow', Code='BOW_001', IconId='client_icon_3', Type=2, Stackable=False
UnityEngine.Debug:Log (object)
ItemTemplateManager:OnItemTemplatesLoaded (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:174)
ItemTemplateManager:<LoadItemTemplatesFromAPI>b__13_0 (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:132)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:653)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

  [4] ID=4, Name='Small Health Potion', Code='POTION_HP_SMALL', IconId='client_icon_4', Type=1, Stackable=True
UnityEngine.Debug:Log (object)
ItemTemplateManager:OnItemTemplatesLoaded (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:174)
ItemTemplateManager:<LoadItemTemplatesFromAPI>b__13_0 (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:132)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:653)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

  [5] ID=5, Name='Medium Health Potion', Code='POTION_HP_MEDIUM', IconId='client_icon_5', Type=1, Stackable=True
UnityEngine.Debug:Log (object)
ItemTemplateManager:OnItemTemplatesLoaded (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:174)
ItemTemplateManager:<LoadItemTemplatesFromAPI>b__13_0 (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:132)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:653)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

  [6] ID=6, Name='Small Mana Potion', Code='POTION_MP_SMALL', IconId='client_icon_6', Type=2, Stackable=True
UnityEngine.Debug:Log (object)
ItemTemplateManager:OnItemTemplatesLoaded (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:174)
ItemTemplateManager:<LoadItemTemplatesFromAPI>b__13_0 (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:132)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:653)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

  [7] ID=7, Name='Wood', Code='MATERIAL_WOOD', IconId='client_icon_7', Type=1, Stackable=True
UnityEngine.Debug:Log (object)
ItemTemplateManager:OnItemTemplatesLoaded (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:174)
ItemTemplateManager:<LoadItemTemplatesFromAPI>b__13_0 (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:132)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:653)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

  [8] ID=8, Name='Iron Ore', Code='MATERIAL_IRON_ORE', IconId='client_icon_1', Type=1, Stackable=True
UnityEngine.Debug:Log (object)
ItemTemplateManager:OnItemTemplatesLoaded (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:174)
ItemTemplateManager:<LoadItemTemplatesFromAPI>b__13_0 (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:132)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:653)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

  [9] ID=9, Name='Herb', Code='MATERIAL_HERB', IconId='material_herb', Type=2, Stackable=True
UnityEngine.Debug:Log (object)
ItemTemplateManager:OnItemTemplatesLoaded (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:174)
ItemTemplateManager:<LoadItemTemplatesFromAPI>b__13_0 (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:132)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:653)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

  [10] ID=10, Name='Leather Armor', Code='ARMOR_LEATHER', IconId='armor_leather', Type=3, Stackable=False
UnityEngine.Debug:Log (object)
ItemTemplateManager:OnItemTemplatesLoaded (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:174)
ItemTemplateManager:<LoadItemTemplatesFromAPI>b__13_0 (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:132)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:653)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[GameSceneNetworkInitializer] Server started. Host is ready to accept clients.
UnityEngine.Debug:Log (object)
GameSceneNetworkInitializer:OnServerStarted () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:468)
Unity.Netcode.NetworkManager:HostServerInitialize () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1146)
Unity.Netcode.NetworkManager:StartHost () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1096)
NetworkManagerCustom:StartHost () (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:146)
GameSceneNetworkInitializer:StartHost () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:458)
GameSceneNetworkInitializer/<StartHostAfterDelay>d__25:MoveNext () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:434)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[GameSceneNetworkInitializer] Spawning AuthSenderNetworkObject from prefab: AuthSenderNetworkObjectPrefab
UnityEngine.Debug:Log (object)
GameSceneNetworkInitializer:SpawnAuthSenderNetworkObject () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:545)
GameSceneNetworkInitializer:OnServerStarted () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:471)
Unity.Netcode.NetworkManager:HostServerInitialize () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1146)
Unity.Netcode.NetworkManager:StartHost () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1096)
NetworkManagerCustom:StartHost () (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:146)
GameSceneNetworkInitializer:StartHost () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:458)
GameSceneNetworkInitializer/<StartHostAfterDelay>d__25:MoveNext () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:434)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[GameSceneNetworkInitializer] ✓ Spawned AuthSenderNetworkObject from prefab: 'AuthSenderNetworkObjectPrefab'
UnityEngine.Debug:Log (object)
GameSceneNetworkInitializer:SpawnAuthSenderNetworkObject () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:563)
GameSceneNetworkInitializer:OnServerStarted () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:471)
Unity.Netcode.NetworkManager:HostServerInitialize () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1146)
Unity.Netcode.NetworkManager:StartHost () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1096)
NetworkManagerCustom:StartHost () (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:146)
GameSceneNetworkInitializer:StartHost () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:458)
GameSceneNetworkInitializer/<StartHostAfterDelay>d__25:MoveNext () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:434)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[GameSceneNetworkInitializer] AuthSenderNetworkObject IsSpawned=True, NetworkObjectId=2, HasClientAuthSender=True
UnityEngine.Debug:Log (object)
GameSceneNetworkInitializer:SpawnAuthSenderNetworkObject () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:564)
GameSceneNetworkInitializer:OnServerStarted () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:471)
Unity.Netcode.NetworkManager:HostServerInitialize () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1146)
Unity.Netcode.NetworkManager:StartHost () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1096)
NetworkManagerCustom:StartHost () (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:146)
GameSceneNetworkInitializer:StartHost () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:458)
GameSceneNetworkInitializer/<StartHostAfterDelay>d__25:MoveNext () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:434)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[GameSceneNetworkInitializer] AuthSenderNetworkObject components: Transform, NetworkObject, ClientAuthSender
UnityEngine.Debug:Log (object)
GameSceneNetworkInitializer:SpawnAuthSenderNetworkObject () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:578)
GameSceneNetworkInitializer:OnServerStarted () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:471)
Unity.Netcode.NetworkManager:HostServerInitialize () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1146)
Unity.Netcode.NetworkManager:StartHost () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1096)
NetworkManagerCustom:StartHost () (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:146)
GameSceneNetworkInitializer:StartHost () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:458)
GameSceneNetworkInitializer/<StartHostAfterDelay>d__25:MoveNext () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:434)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkManagerCustom] Host-side: Loading player data directly for local client 0...
UnityEngine.Debug:Log (object)
NetworkManagerCustom:OnClientConnected (ulong) (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:204)
Unity.Netcode.NetworkConnectionManager:InvokeOnClientConnectedCallback (ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:128)
Unity.Netcode.NetworkManager:HostServerInitialize () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1152)
Unity.Netcode.NetworkManager:StartHost () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1096)
NetworkManagerCustom:StartHost () (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:146)
GameSceneNetworkInitializer:StartHost () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:458)
GameSceneNetworkInitializer/<StartHostAfterDelay>d__25:MoveNext () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:434)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] ===== LOADING PLAYER DATA FOR CLIENT =====
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:LoadPlayerDataForClient (ulong,int,System.Action`1<PlayerDataResponse>,System.Action`1<string>) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:110)
NetworkManagerCustom:OnClientConnected (ulong) (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:217)
Unity.Netcode.NetworkConnectionManager:InvokeOnClientConnectedCallback (ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:128)
Unity.Netcode.NetworkManager:HostServerInitialize () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1152)
Unity.Netcode.NetworkManager:StartHost () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1096)
NetworkManagerCustom:StartHost () (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:146)
GameSceneNetworkInitializer:StartHost () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:458)
GameSceneNetworkInitializer/<StartHostAfterDelay>d__25:MoveNext () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:434)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] ClientId: 0
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:LoadPlayerDataForClient (ulong,int,System.Action`1<PlayerDataResponse>,System.Action`1<string>) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:111)
NetworkManagerCustom:OnClientConnected (ulong) (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:217)
Unity.Netcode.NetworkConnectionManager:InvokeOnClientConnectedCallback (ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:128)
Unity.Netcode.NetworkManager:HostServerInitialize () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1152)
Unity.Netcode.NetworkManager:StartHost () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1096)
NetworkManagerCustom:StartHost () (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:146)
GameSceneNetworkInitializer:StartHost () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:458)
GameSceneNetworkInitializer/<StartHostAfterDelay>d__25:MoveNext () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:434)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] UserId: 2
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:LoadPlayerDataForClient (ulong,int,System.Action`1<PlayerDataResponse>,System.Action`1<string>) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:112)
NetworkManagerCustom:OnClientConnected (ulong) (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:217)
Unity.Netcode.NetworkConnectionManager:InvokeOnClientConnectedCallback (ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:128)
Unity.Netcode.NetworkManager:HostServerInitialize () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1152)
Unity.Netcode.NetworkManager:StartHost () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1096)
NetworkManagerCustom:StartHost () (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:146)
GameSceneNetworkInitializer:StartHost () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:458)
GameSceneNetworkInitializer/<StartHostAfterDelay>d__25:MoveNext () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:434)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] Current cache state - Total cached users: 0
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:LoadPlayerDataForClient (ulong,int,System.Action`1<PlayerDataResponse>,System.Action`1<string>) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:113)
NetworkManagerCustom:OnClientConnected (ulong) (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:217)
Unity.Netcode.NetworkConnectionManager:InvokeOnClientConnectedCallback (ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:128)
Unity.Netcode.NetworkManager:HostServerInitialize () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1152)
Unity.Netcode.NetworkManager:StartHost () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1096)
NetworkManagerCustom:StartHost () (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:146)
GameSceneNetworkInitializer:StartHost () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:458)
GameSceneNetworkInitializer/<StartHostAfterDelay>d__25:MoveNext () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:434)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] Current clientIdToPlayerData mappings: 0
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:LoadPlayerDataForClient (ulong,int,System.Action`1<PlayerDataResponse>,System.Action`1<string>) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:114)
NetworkManagerCustom:OnClientConnected (ulong) (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:217)
Unity.Netcode.NetworkConnectionManager:InvokeOnClientConnectedCallback (ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:128)
Unity.Netcode.NetworkManager:HostServerInitialize () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1152)
Unity.Netcode.NetworkManager:StartHost () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1096)
NetworkManagerCustom:StartHost () (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:146)
GameSceneNetworkInitializer:StartHost () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:458)
GameSceneNetworkInitializer/<StartHostAfterDelay>d__25:MoveNext () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:434)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] Querying ServerAPI for userId: 2...
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:LoadPlayerDataForClient (ulong,int,System.Action`1<PlayerDataResponse>,System.Action`1<string>) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:144)
NetworkManagerCustom:OnClientConnected (ulong) (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:217)
Unity.Netcode.NetworkConnectionManager:InvokeOnClientConnectedCallback (ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:128)
Unity.Netcode.NetworkManager:HostServerInitialize () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1152)
Unity.Netcode.NetworkManager:StartHost () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1096)
NetworkManagerCustom:StartHost () (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:146)
GameSceneNetworkInitializer:StartHost () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:458)
GameSceneNetworkInitializer/<StartHostAfterDelay>d__25:MoveNext () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:434)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] API Endpoint: /api/player/2/data
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:LoadPlayerDataForClient (ulong,int,System.Action`1<PlayerDataResponse>,System.Action`1<string>) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:145)
NetworkManagerCustom:OnClientConnected (ulong) (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:217)
Unity.Netcode.NetworkConnectionManager:InvokeOnClientConnectedCallback (ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:128)
Unity.Netcode.NetworkManager:HostServerInitialize () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1152)
Unity.Netcode.NetworkManager:StartHost () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1096)
NetworkManagerCustom:StartHost () (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:146)
GameSceneNetworkInitializer:StartHost () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:458)
GameSceneNetworkInitializer/<StartHostAfterDelay>d__25:MoveNext () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:434)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[InventoryNetworkBridge] Client connected (ID: 0), trying to find NetworkInventory...
UnityEngine.Debug:Log (object)
InventoryNetworkBridge:OnClientConnected (ulong) (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:274)
Unity.Netcode.NetworkConnectionManager:InvokeOnClientConnectedCallback (ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:128)
Unity.Netcode.NetworkManager:HostServerInitialize () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1152)
Unity.Netcode.NetworkManager:StartHost () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1096)
NetworkManagerCustom:StartHost () (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:146)
GameSceneNetworkInitializer:StartHost () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:458)
GameSceneNetworkInitializer/<StartHostAfterDelay>d__25:MoveNext () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:434)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[InventoryNetworkBridge] Đang đợi player character spawn (1s)...
UnityEngine.Debug:Log (object)
InventoryNetworkBridge/<FindPlayerInventoryDelayed>d__12:MoveNext () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:284)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
InventoryNetworkBridge:OnClientConnected (ulong) (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:277)
Unity.Netcode.NetworkConnectionManager:InvokeOnClientConnectedCallback (ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:128)
Unity.Netcode.NetworkManager:HostServerInitialize () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1152)
Unity.Netcode.NetworkManager:StartHost () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1096)
NetworkManagerCustom:StartHost () (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:146)
GameSceneNetworkInitializer:StartHost () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:458)
GameSceneNetworkInitializer/<StartHostAfterDelay>d__25:MoveNext () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:434)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] ===== WAITING FOR PLAYER DATA =====
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:227)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
NetworkPlayerSpawner:SpawnPlayer (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:214)
Unity.Netcode.NetworkConnectionManager:InvokeOnClientConnectedCallback (ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:128)
Unity.Netcode.NetworkManager:HostServerInitialize () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1152)
Unity.Netcode.NetworkManager:StartHost () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1096)
NetworkManagerCustom:StartHost () (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:146)
GameSceneNetworkInitializer:StartHost () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:458)
GameSceneNetworkInitializer/<StartHostAfterDelay>d__25:MoveNext () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:434)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] ClientId: 0
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:228)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
NetworkPlayerSpawner:SpawnPlayer (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:214)
Unity.Netcode.NetworkConnectionManager:InvokeOnClientConnectedCallback (ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:128)
Unity.Netcode.NetworkManager:HostServerInitialize () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1152)
Unity.Netcode.NetworkManager:StartHost () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1096)
NetworkManagerCustom:StartHost () (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:146)
GameSceneNetworkInitializer:StartHost () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:458)
GameSceneNetworkInitializer/<StartHostAfterDelay>d__25:MoveNext () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:434)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] Max retries: 120, Interval: 0.1s
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:229)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
NetworkPlayerSpawner:SpawnPlayer (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:214)
Unity.Netcode.NetworkConnectionManager:InvokeOnClientConnectedCallback (ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:128)
Unity.Netcode.NetworkManager:HostServerInitialize () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1152)
Unity.Netcode.NetworkManager:StartHost () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1096)
NetworkManagerCustom:StartHost () (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:146)
GameSceneNetworkInitializer:StartHost () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:458)
GameSceneNetworkInitializer/<StartHostAfterDelay>d__25:MoveNext () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:434)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] GetPlayerDataForClient called for clientId: 0
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:207)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:237)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
NetworkPlayerSpawner:SpawnPlayer (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:214)
Unity.Netcode.NetworkConnectionManager:InvokeOnClientConnectedCallback (ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:128)
Unity.Netcode.NetworkManager:HostServerInitialize () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1152)
Unity.Netcode.NetworkManager:StartHost () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1096)
NetworkManagerCustom:StartHost () (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:146)
GameSceneNetworkInitializer:StartHost () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:458)
GameSceneNetworkInitializer/<StartHostAfterDelay>d__25:MoveNext () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:434)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] Current clientIdToPlayerData cache:
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:210)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:237)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
NetworkPlayerSpawner:SpawnPlayer (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:214)
Unity.Netcode.NetworkConnectionManager:InvokeOnClientConnectedCallback (ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:128)
Unity.Netcode.NetworkManager:HostServerInitialize () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1152)
Unity.Netcode.NetworkManager:StartHost () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1096)
NetworkManagerCustom:StartHost () (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:146)
GameSceneNetworkInitializer:StartHost () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:458)
GameSceneNetworkInitializer/<StartHostAfterDelay>d__25:MoveNext () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:434)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] ✗ No player data found for clientId 0
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:222)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:237)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
NetworkPlayerSpawner:SpawnPlayer (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:214)
Unity.Netcode.NetworkConnectionManager:InvokeOnClientConnectedCallback (ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:128)
Unity.Netcode.NetworkManager:HostServerInitialize () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1152)
Unity.Netcode.NetworkManager:StartHost () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1096)
NetworkManagerCustom:StartHost () (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:146)
GameSceneNetworkInitializer:StartHost () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:458)
GameSceneNetworkInitializer/<StartHostAfterDelay>d__25:MoveNext () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:434)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] Attempt 1/120 - Checking ServerPlayerDataManager for clientId 0...
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:241)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
NetworkPlayerSpawner:SpawnPlayer (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:214)
Unity.Netcode.NetworkConnectionManager:InvokeOnClientConnectedCallback (ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:128)
Unity.Netcode.NetworkManager:HostServerInitialize () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1152)
Unity.Netcode.NetworkManager:StartHost () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1096)
NetworkManagerCustom:StartHost () (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:146)
GameSceneNetworkInitializer:StartHost () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:458)
GameSceneNetworkInitializer/<StartHostAfterDelay>d__25:MoveNext () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:434)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] ServerPlayerDataManager.Instance exists: True
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:242)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
NetworkPlayerSpawner:SpawnPlayer (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:214)
Unity.Netcode.NetworkConnectionManager:InvokeOnClientConnectedCallback (ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:128)
Unity.Netcode.NetworkManager:HostServerInitialize () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1152)
Unity.Netcode.NetworkManager:StartHost () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1096)
NetworkManagerCustom:StartHost () (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:146)
GameSceneNetworkInitializer:StartHost () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:458)
GameSceneNetworkInitializer/<StartHostAfterDelay>d__25:MoveNext () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:434)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] PlayerData found: False
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:243)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
NetworkPlayerSpawner:SpawnPlayer (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:214)
Unity.Netcode.NetworkConnectionManager:InvokeOnClientConnectedCallback (ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:128)
Unity.Netcode.NetworkManager:HostServerInitialize () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1152)
Unity.Netcode.NetworkManager:StartHost () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:1096)
NetworkManagerCustom:StartHost () (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:146)
GameSceneNetworkInitializer:StartHost () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:458)
GameSceneNetworkInitializer/<StartHostAfterDelay>d__25:MoveNext () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:434)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ClientAuthSender] Update() Frame #1 - shouldSendAuth is FALSE (already sent or cleared)
UnityEngine.Debug:Log (object)
ClientAuthSender:Update () (at Assets/Scripts/Network/Client/ClientAuthSender.cs:179)

[ServerPlayerDataManager] GetPlayerDataForClient called for clientId: 0
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:207)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:237)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] Current clientIdToPlayerData cache:
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:210)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:237)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] ✗ No player data found for clientId 0
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:222)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:237)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] ===== PLAYER DATA LOADED FROM API =====
UnityEngine.Debug:Log (object)
ServerPlayerDataManager/<>c__DisplayClass11_0:<LoadPlayerDataForClient>b__0 (PlayerDataResponse) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:151)
APIClient/<LoadPlayerDataCoroutine>d__16:MoveNext () (at Assets/Scripts/API/APIClient.cs:424)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] ✓ API Response received for userId: 2
UnityEngine.Debug:Log (object)
ServerPlayerDataManager/<>c__DisplayClass11_0:<LoadPlayerDataForClient>b__0 (PlayerDataResponse) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:152)
APIClient/<LoadPlayerDataCoroutine>d__16:MoveNext () (at Assets/Scripts/API/APIClient.cs:424)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] ✓ ClientId: 0
UnityEngine.Debug:Log (object)
ServerPlayerDataManager/<>c__DisplayClass11_0:<LoadPlayerDataForClient>b__0 (PlayerDataResponse) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:153)
APIClient/<LoadPlayerDataCoroutine>d__16:MoveNext () (at Assets/Scripts/API/APIClient.cs:424)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] ✓ Character Name: 1231
UnityEngine.Debug:Log (object)
ServerPlayerDataManager/<>c__DisplayClass11_0:<LoadPlayerDataForClient>b__0 (PlayerDataResponse) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:154)
APIClient/<LoadPlayerDataCoroutine>d__16:MoveNext () (at Assets/Scripts/API/APIClient.cs:424)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] ✓ Element Type: Fire
UnityEngine.Debug:Log (object)
ServerPlayerDataManager/<>c__DisplayClass11_0:<LoadPlayerDataForClient>b__0 (PlayerDataResponse) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:155)
APIClient/<LoadPlayerDataCoroutine>d__16:MoveNext () (at Assets/Scripts/API/APIClient.cs:424)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] ✓ Gender: Male
UnityEngine.Debug:Log (object)
ServerPlayerDataManager/<>c__DisplayClass11_0:<LoadPlayerDataForClient>b__0 (PlayerDataResponse) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:156)
APIClient/<LoadPlayerDataCoroutine>d__16:MoveNext () (at Assets/Scripts/API/APIClient.cs:424)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] ✓ Level: 1
UnityEngine.Debug:Log (object)
ServerPlayerDataManager/<>c__DisplayClass11_0:<LoadPlayerDataForClient>b__0 (PlayerDataResponse) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:157)
APIClient/<LoadPlayerDataCoroutine>d__16:MoveNext () (at Assets/Scripts/API/APIClient.cs:424)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] ✓ Map ID: 0
UnityEngine.Debug:Log (object)
ServerPlayerDataManager/<>c__DisplayClass11_0:<LoadPlayerDataForClient>b__0 (PlayerDataResponse) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:158)
APIClient/<LoadPlayerDataCoroutine>d__16:MoveNext () (at Assets/Scripts/API/APIClient.cs:424)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] ===== CACHING PLAYER DATA =====
UnityEngine.Debug:Log (object)
ServerPlayerDataManager/<>c__DisplayClass11_0:<LoadPlayerDataForClient>b__0 (PlayerDataResponse) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:165)
APIClient/<LoadPlayerDataCoroutine>d__16:MoveNext () (at Assets/Scripts/API/APIClient.cs:424)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] ✓ playerDataCache[2] = PlayerData (1231)
UnityEngine.Debug:Log (object)
ServerPlayerDataManager/<>c__DisplayClass11_0:<LoadPlayerDataForClient>b__0 (PlayerDataResponse) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:166)
APIClient/<LoadPlayerDataCoroutine>d__16:MoveNext () (at Assets/Scripts/API/APIClient.cs:424)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] ✓ clientIdToUserId[0] = 2
UnityEngine.Debug:Log (object)
ServerPlayerDataManager/<>c__DisplayClass11_0:<LoadPlayerDataForClient>b__0 (PlayerDataResponse) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:167)
APIClient/<LoadPlayerDataCoroutine>d__16:MoveNext () (at Assets/Scripts/API/APIClient.cs:424)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] ✓ clientIdToPlayerData[0] = PlayerData (1231)
UnityEngine.Debug:Log (object)
ServerPlayerDataManager/<>c__DisplayClass11_0:<LoadPlayerDataForClient>b__0 (PlayerDataResponse) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:168)
APIClient/<LoadPlayerDataCoroutine>d__16:MoveNext () (at Assets/Scripts/API/APIClient.cs:424)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] ===== VERIFY CACHE =====
UnityEngine.Debug:Log (object)
ServerPlayerDataManager/<>c__DisplayClass11_0:<LoadPlayerDataForClient>b__0 (PlayerDataResponse) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:169)
APIClient/<LoadPlayerDataCoroutine>d__16:MoveNext () (at Assets/Scripts/API/APIClient.cs:424)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] ✓ playerDataCache contains userId 2: True
UnityEngine.Debug:Log (object)
ServerPlayerDataManager/<>c__DisplayClass11_0:<LoadPlayerDataForClient>b__0 (PlayerDataResponse) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:170)
APIClient/<LoadPlayerDataCoroutine>d__16:MoveNext () (at Assets/Scripts/API/APIClient.cs:424)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] ✓ clientIdToPlayerData contains clientId 0: True
UnityEngine.Debug:Log (object)
ServerPlayerDataManager/<>c__DisplayClass11_0:<LoadPlayerDataForClient>b__0 (PlayerDataResponse) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:171)
APIClient/<LoadPlayerDataCoroutine>d__16:MoveNext () (at Assets/Scripts/API/APIClient.cs:424)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] ✓ Total cached users: 1
UnityEngine.Debug:Log (object)
ServerPlayerDataManager/<>c__DisplayClass11_0:<LoadPlayerDataForClient>b__0 (PlayerDataResponse) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:172)
APIClient/<LoadPlayerDataCoroutine>d__16:MoveNext () (at Assets/Scripts/API/APIClient.cs:424)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] ✓ Total clientId mappings: 1
UnityEngine.Debug:Log (object)
ServerPlayerDataManager/<>c__DisplayClass11_0:<LoadPlayerDataForClient>b__0 (PlayerDataResponse) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:173)
APIClient/<LoadPlayerDataCoroutine>d__16:MoveNext () (at Assets/Scripts/API/APIClient.cs:424)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] ✓ Player data successfully cached and mapped to clientId: 0
UnityEngine.Debug:Log (object)
ServerPlayerDataManager/<>c__DisplayClass11_0:<LoadPlayerDataForClient>b__0 (PlayerDataResponse) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:174)
APIClient/<LoadPlayerDataCoroutine>d__16:MoveNext () (at Assets/Scripts/API/APIClient.cs:424)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkManagerCustom] ✓ Host player data loaded: 1231
UnityEngine.Debug:Log (object)
NetworkManagerCustom/<>c:<OnClientConnected>b__11_0 (PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:222)
ServerPlayerDataManager/<>c__DisplayClass11_0:<LoadPlayerDataForClient>b__0 (PlayerDataResponse) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:175)
APIClient/<LoadPlayerDataCoroutine>d__16:MoveNext () (at Assets/Scripts/API/APIClient.cs:424)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ClientAuthSender] Update() Frame #2 - shouldSendAuth is FALSE (already sent or cleared)
UnityEngine.Debug:Log (object)
ClientAuthSender:Update () (at Assets/Scripts/Network/Client/ClientAuthSender.cs:179)

[ClientAuthSender] Update() Frame #3 - shouldSendAuth is FALSE (already sent or cleared)
UnityEngine.Debug:Log (object)
ClientAuthSender:Update () (at Assets/Scripts/Network/Client/ClientAuthSender.cs:179)

[ClientAuthSender] Update() Frame #4 - shouldSendAuth is FALSE (already sent or cleared)
UnityEngine.Debug:Log (object)
ClientAuthSender:Update () (at Assets/Scripts/Network/Client/ClientAuthSender.cs:179)

[ClientAuthSender] Update() Frame #5 - shouldSendAuth is FALSE (already sent or cleared)
UnityEngine.Debug:Log (object)
ClientAuthSender:Update () (at Assets/Scripts/Network/Client/ClientAuthSender.cs:179)

  ClientId: 0 => PlayerData: 1231
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:213)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:237)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] ✓ Found player data for clientId 0
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:218)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:237)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] ===== PLAYER DATA READY =====
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:260)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] ✓ Player data ready for client 0 after 3 attempts (0.3s)
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:261)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] ✓ Character: 1231
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:262)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] ✓ Element: Fire
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:263)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] ✓ Gender: Male
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:264)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] ✓ Spawning player now...
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:265)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] ===== GET PLAYER PREFAB FOR CLIENT =====
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner:GetPlayerPrefabForClient (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:426)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:321)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:266)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] ClientId: 0
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner:GetPlayerPrefabForClient (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:427)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:321)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:266)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] Calling ServerPlayerDataManager.GetPlayerDataForClient(0)...
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner:GetPlayerPrefabForClient (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:431)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:321)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:266)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] GetPlayerDataForClient called for clientId: 0
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:207)
NetworkPlayerSpawner:GetPlayerPrefabForClient (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:432)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:321)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:266)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] Current clientIdToPlayerData cache:
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:210)
NetworkPlayerSpawner:GetPlayerPrefabForClient (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:432)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:321)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:266)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

  ClientId: 0 => PlayerData: 1231
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:213)
NetworkPlayerSpawner:GetPlayerPrefabForClient (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:432)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:321)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:266)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] ✓ Found player data for clientId 0
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:218)
NetworkPlayerSpawner:GetPlayerPrefabForClient (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:432)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:321)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:266)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] ✓ Got PlayerData from ServerPlayerDataManager
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner:GetPlayerPrefabForClient (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:436)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:321)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:266)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] ✓ Character: 1231
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner:GetPlayerPrefabForClient (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:437)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:321)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:266)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] ✓ Element: Fire
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner:GetPlayerPrefabForClient (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:438)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:321)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:266)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] ✓ Gender: Male
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner:GetPlayerPrefabForClient (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:439)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:321)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:266)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

Animator is not playing an AnimatorController
UnityEngine.StackTraceUtility:ExtractStackTrace ()
Unity.Netcode.Components.NetworkAnimator:Awake () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Components/NetworkAnimator.cs:569)
UnityEngine.Object:Instantiate<UnityEngine.GameObject> (UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:338)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:266)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

Animator is not playing an AnimatorController
UnityEngine.StackTraceUtility:ExtractStackTrace ()
Unity.Netcode.Components.NetworkAnimator:Awake () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Components/NetworkAnimator.cs:585)
UnityEngine.Object:Instantiate<UnityEngine.GameObject> (UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:338)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:266)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

Animator is not playing an AnimatorController
UnityEngine.StackTraceUtility:ExtractStackTrace ()
Unity.Netcode.Components.NetworkAnimator:Awake () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Components/NetworkAnimator.cs:602)
UnityEngine.Object:Instantiate<UnityEngine.GameObject> (UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:338)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:266)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] NetworkObject found, spawning with ownership for client 0
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:343)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:266)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[PlayerSkillManager] Đã khởi tạo 1 skill(s)
UnityEngine.Debug:Log (object)
PlayerSkillManager:InitializeSkills () (at Assets/Scripts/Player/PlayerSkillManager.cs:82)
PlayerSkillManager:OnNetworkSpawn () (at Assets/Scripts/Player/PlayerSkillManager.cs:31)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:753)
Unity.Netcode.NetworkObject:SpawnInternal (bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:798)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:346)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:266)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkInventory] ===== OnNetworkSpawn CALLED! =====
UnityEngine.Debug:Log (object)
NetworkInventory:OnNetworkSpawn () (at Assets/Scripts/Inventory/NetworkInventory.cs:38)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:753)
Unity.Netcode.NetworkObject:SpawnInternal (bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:798)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:346)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:266)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkInventory] IsServer=True, IsClient=True, IsOwner=True, OwnerClientId=0
UnityEngine.Debug:Log (object)
NetworkInventory:OnNetworkSpawn () (at Assets/Scripts/Inventory/NetworkInventory.cs:39)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:753)
Unity.Netcode.NetworkObject:SpawnInternal (bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:798)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:346)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:266)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkInventory] ===== OnInventoryDataChanged TRIGGERED! =====
UnityEngine.Debug:Log (object)
NetworkInventory:OnInventoryDataChanged (NetworkInventoryData,NetworkInventoryData) (at Assets/Scripts/Inventory/NetworkInventory.cs:106)
Unity.Netcode.NetworkVariable`1<NetworkInventoryData>:set_Value (NetworkInventoryData) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/NetworkVariable/NetworkVariable.cs:165)
NetworkInventory:OnNetworkSpawn () (at Assets/Scripts/Inventory/NetworkInventory.cs:56)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:753)
Unity.Netcode.NetworkObject:SpawnInternal (bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:798)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:346)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:266)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkInventory] IsServer=True, IsClient=True, IsOwner=True
UnityEngine.Debug:Log (object)
NetworkInventory:OnInventoryDataChanged (NetworkInventoryData,NetworkInventoryData) (at Assets/Scripts/Inventory/NetworkInventory.cs:107)
Unity.Netcode.NetworkVariable`1<NetworkInventoryData>:set_Value (NetworkInventoryData) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/NetworkVariable/NetworkVariable.cs:165)
NetworkInventory:OnNetworkSpawn () (at Assets/Scripts/Inventory/NetworkInventory.cs:56)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:753)
Unity.Netcode.NetworkObject:SpawnInternal (bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:798)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:346)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:266)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkInventory] New data has 0 items
UnityEngine.Debug:Log (object)
NetworkInventory:OnInventoryDataChanged (NetworkInventoryData,NetworkInventoryData) (at Assets/Scripts/Inventory/NetworkInventory.cs:117)
Unity.Netcode.NetworkVariable`1<NetworkInventoryData>:set_Value (NetworkInventoryData) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/NetworkVariable/NetworkVariable.cs:165)
NetworkInventory:OnNetworkSpawn () (at Assets/Scripts/Inventory/NetworkInventory.cs:56)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:753)
Unity.Netcode.NetworkObject:SpawnInternal (bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:798)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:346)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:266)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkInventory] Calling OnInventoryChanged?.Invoke()...
UnityEngine.Debug:Log (object)
NetworkInventory:OnInventoryDataChanged (NetworkInventoryData,NetworkInventoryData) (at Assets/Scripts/Inventory/NetworkInventory.cs:121)
Unity.Netcode.NetworkVariable`1<NetworkInventoryData>:set_Value (NetworkInventoryData) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/NetworkVariable/NetworkVariable.cs:165)
NetworkInventory:OnNetworkSpawn () (at Assets/Scripts/Inventory/NetworkInventory.cs:56)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:753)
Unity.Netcode.NetworkObject:SpawnInternal (bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:798)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:346)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:266)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkInventory] ✓ OnInventoryChanged event invoked!
UnityEngine.Debug:Log (object)
NetworkInventory:OnInventoryDataChanged (NetworkInventoryData,NetworkInventoryData) (at Assets/Scripts/Inventory/NetworkInventory.cs:124)
Unity.Netcode.NetworkVariable`1<NetworkInventoryData>:set_Value (NetworkInventoryData) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/NetworkVariable/NetworkVariable.cs:165)
NetworkInventory:OnNetworkSpawn () (at Assets/Scripts/Inventory/NetworkInventory.cs:56)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:753)
Unity.Netcode.NetworkObject:SpawnInternal (bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:798)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:346)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:266)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkInventory] Server: Bắt đầu load inventory từ DB... (OwnerClientId=0)
UnityEngine.Debug:Log (object)
NetworkInventory:OnNetworkSpawn () (at Assets/Scripts/Inventory/NetworkInventory.cs:60)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:753)
Unity.Netcode.NetworkObject:SpawnInternal (bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:798)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:346)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:266)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkInventory] Deserialized inventory on spawn. UsedSlots=0
UnityEngine.Debug:Log (object)
NetworkInventory:OnNetworkSpawn () (at Assets/Scripts/Inventory/NetworkInventory.cs:68)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:753)
Unity.Netcode.NetworkObject:SpawnInternal (bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:798)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:346)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:266)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] GetPlayerDataForClient called for clientId: 0
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:207)
NetworkPlayerDataSync:LoadPlayerDataFromGameManager () (at Assets/Scripts/Network/Shared/NetworkPlayerDataSync.cs:201)
NetworkPlayerDataSync:OnNetworkSpawn () (at Assets/Scripts/Network/Shared/NetworkPlayerDataSync.cs:35)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:753)
Unity.Netcode.NetworkObject:SpawnInternal (bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:798)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:346)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:266)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] Current clientIdToPlayerData cache:
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:210)
NetworkPlayerDataSync:LoadPlayerDataFromGameManager () (at Assets/Scripts/Network/Shared/NetworkPlayerDataSync.cs:201)
NetworkPlayerDataSync:OnNetworkSpawn () (at Assets/Scripts/Network/Shared/NetworkPlayerDataSync.cs:35)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:753)
Unity.Netcode.NetworkObject:SpawnInternal (bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:798)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:346)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:266)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

  ClientId: 0 => PlayerData: 1231
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:213)
NetworkPlayerDataSync:LoadPlayerDataFromGameManager () (at Assets/Scripts/Network/Shared/NetworkPlayerDataSync.cs:201)
NetworkPlayerDataSync:OnNetworkSpawn () (at Assets/Scripts/Network/Shared/NetworkPlayerDataSync.cs:35)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:753)
Unity.Netcode.NetworkObject:SpawnInternal (bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:798)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:346)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:266)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] ✓ Found player data for clientId 0
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:218)
NetworkPlayerDataSync:LoadPlayerDataFromGameManager () (at Assets/Scripts/Network/Shared/NetworkPlayerDataSync.cs:201)
NetworkPlayerDataSync:OnNetworkSpawn () (at Assets/Scripts/Network/Shared/NetworkPlayerDataSync.cs:35)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:753)
Unity.Netcode.NetworkObject:SpawnInternal (bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:798)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:346)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:266)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerHealth] No spawn points found, using current position: (0.40, -3.34, 0.00)
UnityEngine.Debug:LogWarning (object)
NetworkPlayerHealth:Start () (at Assets/Scripts/Combat/NetworkPlayerHealth.cs:118)

Animator is not playing an AnimatorController
UnityEngine.StackTraceUtility:ExtractStackTrace ()
Unity.Netcode.Components.NetworkAnimator:CheckForAnimatorChanges () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Components/NetworkAnimator.cs:947)
Unity.Netcode.Components.NetworkAnimatorStateChangeHandler:NetworkUpdate (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Components/NetworkAnimator.cs:81)
Unity.Netcode.NetworkUpdateLoop:RunNetworkUpdateStage (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:192)
Unity.Netcode.NetworkUpdateLoop/NetworkPreUpdate/<>c:<CreateLoopSystem>b__0_0 () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:239)

[ServerPlayerDataManager] GetPlayerDataForClient called for clientId: 0
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:207)
ServerPlayerDataManager:GetPlayerDataByClientId (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:231)
NetworkInventory:LoadInventoryFromDB () (at Assets/Scripts/Inventory/NetworkInventory.cs:618)
NetworkInventory/<LoadInventoryFromDBDelayed>d__8:MoveNext () (at Assets/Scripts/Inventory/NetworkInventory.cs:83)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] Current clientIdToPlayerData cache:
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:210)
ServerPlayerDataManager:GetPlayerDataByClientId (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:231)
NetworkInventory:LoadInventoryFromDB () (at Assets/Scripts/Inventory/NetworkInventory.cs:618)
NetworkInventory/<LoadInventoryFromDBDelayed>d__8:MoveNext () (at Assets/Scripts/Inventory/NetworkInventory.cs:83)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

  ClientId: 0 => PlayerData: 1231
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:213)
ServerPlayerDataManager:GetPlayerDataByClientId (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:231)
NetworkInventory:LoadInventoryFromDB () (at Assets/Scripts/Inventory/NetworkInventory.cs:618)
NetworkInventory/<LoadInventoryFromDBDelayed>d__8:MoveNext () (at Assets/Scripts/Inventory/NetworkInventory.cs:83)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] ✓ Found player data for clientId 0
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:218)
ServerPlayerDataManager:GetPlayerDataByClientId (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:231)
NetworkInventory:LoadInventoryFromDB () (at Assets/Scripts/Inventory/NetworkInventory.cs:618)
NetworkInventory/<LoadInventoryFromDBDelayed>d__8:MoveNext () (at Assets/Scripts/Inventory/NetworkInventory.cs:83)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkInventory] Đang load inventory từ DB cho player 2 (OwnerClientId=0)...
UnityEngine.Debug:Log (object)
NetworkInventory:LoadInventoryFromDB () (at Assets/Scripts/Inventory/NetworkInventory.cs:638)
NetworkInventory/<LoadInventoryFromDBDelayed>d__8:MoveNext () (at Assets/Scripts/Inventory/NetworkInventory.cs:83)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkInventory] Inventory trong DB trống (player mới).
UnityEngine.Debug:Log (object)
NetworkInventory:<LoadInventoryFromDB>b__34_0 (PlayerDataResponse) (at Assets/Scripts/Inventory/NetworkInventory.cs:654)
APIClient/<LoadPlayerDataCoroutine>d__16:MoveNext () (at Assets/Scripts/API/APIClient.cs:424)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[InventoryNetworkBridge] Lần thử 1/30...
UnityEngine.Debug:Log (object)
InventoryNetworkBridge/<FindPlayerInventoryDelayed>d__12:MoveNext () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:296)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[InventoryNetworkBridge] ========== FindPlayerInventory() BẮT ĐẦU ==========
UnityEngine.Debug:Log (object)
InventoryNetworkBridge:FindPlayerInventory () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:372)
InventoryNetworkBridge/<FindPlayerInventoryDelayed>d__12:MoveNext () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:298)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[InventoryNetworkBridge] SpawnedObjectsList count: 3
UnityEngine.Debug:Log (object)
InventoryNetworkBridge:FindPlayerInventory () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:394)
InventoryNetworkBridge/<FindPlayerInventoryDelayed>d__12:MoveNext () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:298)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[InventoryNetworkBridge] ✓ Tìm thấy player character: 'FirePrefab(Clone)'
UnityEngine.Debug:Log (object)
InventoryNetworkBridge:FindPlayerInventory () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:438)
InventoryNetworkBridge/<FindPlayerInventoryDelayed>d__12:MoveNext () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:298)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[InventoryNetworkBridge] ✓✓✓ TÌM THẤY NetworkInventory của player: FirePrefab(Clone)
UnityEngine.Debug:Log (object)
InventoryNetworkBridge:FindPlayerInventory () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:447)
InventoryNetworkBridge/<FindPlayerInventoryDelayed>d__12:MoveNext () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:298)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[InventoryNetworkBridge] → NetworkInventory GameObject: FirePrefab(Clone)
UnityEngine.Debug:Log (object)
InventoryNetworkBridge:FindPlayerInventory () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:448)
InventoryNetworkBridge/<FindPlayerInventoryDelayed>d__12:MoveNext () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:298)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[InventoryNetworkBridge] → OwnerClientId: 0
UnityEngine.Debug:Log (object)
InventoryNetworkBridge:FindPlayerInventory () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:449)
InventoryNetworkBridge/<FindPlayerInventoryDelayed>d__12:MoveNext () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:298)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[InventoryNetworkBridge] → IsSpawned: True
UnityEngine.Debug:Log (object)
InventoryNetworkBridge:FindPlayerInventory () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:450)
InventoryNetworkBridge/<FindPlayerInventoryDelayed>d__12:MoveNext () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:298)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[InventoryNetworkBridge] → Component found at: NetworkInventory
UnityEngine.Debug:Log (object)
InventoryNetworkBridge:FindPlayerInventory () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:451)
InventoryNetworkBridge/<FindPlayerInventoryDelayed>d__12:MoveNext () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:298)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[InventoryNetworkBridge] ✓✓✓ Tìm thấy NetworkInventory ở lần thử 1!
UnityEngine.Debug:Log (object)
InventoryNetworkBridge/<FindPlayerInventoryDelayed>d__12:MoveNext () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:303)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[InventoryNetworkBridge] → Đang subscribe to inventory events...
UnityEngine.Debug:Log (object)
InventoryNetworkBridge/<FindPlayerInventoryDelayed>d__12:MoveNext () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:304)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[InventoryNetworkBridge] ===== SUBSCRIBING TO INVENTORY EVENTS =====
UnityEngine.Debug:Log (object)
InventoryNetworkBridge:SubscribeToInventoryEvents () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:333)
InventoryNetworkBridge/<FindPlayerInventoryDelayed>d__12:MoveNext () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:305)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[InventoryNetworkBridge] NetworkInventory: FirePrefab(Clone)
UnityEngine.Debug:Log (object)
InventoryNetworkBridge:SubscribeToInventoryEvents () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:334)
InventoryNetworkBridge/<FindPlayerInventoryDelayed>d__12:MoveNext () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:305)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[InventoryNetworkBridge] IsServer=True, IsClient=True, IsOwner=True
UnityEngine.Debug:Log (object)
InventoryNetworkBridge:SubscribeToInventoryEvents () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:335)
InventoryNetworkBridge/<FindPlayerInventoryDelayed>d__12:MoveNext () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:305)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[InventoryNetworkBridge] Calling initial RefreshInventoryUI()...
UnityEngine.Debug:Log (object)
InventoryNetworkBridge:SubscribeToInventoryEvents () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:340)
InventoryNetworkBridge/<FindPlayerInventoryDelayed>d__12:MoveNext () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:305)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[InventoryNetworkBridge] RefreshInventoryUI: Bắt đầu convert từ NetworkInventory...
UnityEngine.Debug:Log (object)
InventoryNetworkBridge:RefreshInventoryUI () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:501)
InventoryNetworkBridge:SubscribeToInventoryEvents () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:341)
InventoryNetworkBridge/<FindPlayerInventoryDelayed>d__12:MoveNext () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:305)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[InventoryNetworkBridge] RefreshInventoryUI: Tìm thấy 0 items trong 20 slots. Đang gửi cho InventoryUI...
UnityEngine.Debug:Log (object)
InventoryNetworkBridge:RefreshInventoryUI () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:563)
InventoryNetworkBridge:SubscribeToInventoryEvents () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:341)
InventoryNetworkBridge/<FindPlayerInventoryDelayed>d__12:MoveNext () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:305)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[InventoryUI] SetInventoryData: Nhận 20 slots, trong đó có 0 slots có item (quantity > 0)
UnityEngine.Debug:Log (object)
InventoryUI:SetInventoryData (InventorySlotDto[]) (at Assets/Scripts/Inventory/InventoryUI.cs:157)
InventoryNetworkBridge:RefreshInventoryUI () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:566)
InventoryNetworkBridge:SubscribeToInventoryEvents () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:341)
InventoryNetworkBridge/<FindPlayerInventoryDelayed>d__12:MoveNext () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:305)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[InventoryUI] RefreshAllSlots: slotUIs is null! Có thể chưa InitSlots()?
UnityEngine.Debug:LogWarning (object)
InventoryUI:RefreshAllSlots () (at Assets/Scripts/Inventory/InventoryUI.cs:170)
InventoryUI:SetInventoryData (InventorySlotDto[]) (at Assets/Scripts/Inventory/InventoryUI.cs:160)
InventoryNetworkBridge:RefreshInventoryUI () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:566)
InventoryNetworkBridge:SubscribeToInventoryEvents () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:341)
InventoryNetworkBridge/<FindPlayerInventoryDelayed>d__12:MoveNext () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:305)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[InventoryNetworkBridge] RefreshInventoryUI: Đã gửi 20 slots cho InventoryUI.
UnityEngine.Debug:Log (object)
InventoryNetworkBridge:RefreshInventoryUI () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:568)
InventoryNetworkBridge:SubscribeToInventoryEvents () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:341)
InventoryNetworkBridge/<FindPlayerInventoryDelayed>d__12:MoveNext () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:305)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[InventoryNetworkBridge] ✅ Subscribed to NetworkInventory.OnInventoryChanged
UnityEngine.Debug:Log (object)
InventoryNetworkBridge:SubscribeToInventoryEvents () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:343)
InventoryNetworkBridge/<FindPlayerInventoryDelayed>d__12:MoveNext () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:305)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[InventoryNetworkBridge] ✓ Subscribe thành công!
UnityEngine.Debug:Log (object)
InventoryNetworkBridge/<FindPlayerInventoryDelayed>d__12:MoveNext () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:306)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

Animator is not playing an AnimatorController
UnityEngine.StackTraceUtility:ExtractStackTrace ()
Unity.Netcode.Components.NetworkAnimator:WriteSynchronizationData<Unity.Netcode.BufferSerializerWriter> (Unity.Netcode.BufferSerializer`1<Unity.Netcode.BufferSerializerWriter>&) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Components/NetworkAnimator.cs:742)
Unity.Netcode.Components.NetworkAnimator:OnSynchronize<Unity.Netcode.BufferSerializerWriter> (Unity.Netcode.BufferSerializer`1<Unity.Netcode.BufferSerializerWriter>&) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Components/NetworkAnimator.cs:813)
Unity.Netcode.NetworkBehaviour:Synchronize<Unity.Netcode.BufferSerializerWriter> (Unity.Netcode.BufferSerializer`1<Unity.Netcode.BufferSerializerWriter>&,ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:1291)
Unity.Netcode.NetworkObject:SynchronizeNetworkBehaviours<Unity.Netcode.BufferSerializerWriter> (Unity.Netcode.BufferSerializer`1<Unity.Netcode.BufferSerializerWriter>&,ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1825)
Unity.Netcode.NetworkObject/SceneObject:Serialize (Unity.Netcode.FastBufferWriter) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1751)
Unity.Netcode.ConnectionApprovedMessage:Serialize (Unity.Netcode.FastBufferWriter,int) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/Messages/ConnectionApprovedMessage.cs:63)
Unity.Netcode.NetworkMessageManager:SendMessage<Unity.Netcode.ConnectionApprovedMessage, Unity.Netcode.NetworkMessageManager/PointerListWrapper`1<ulong>> (Unity.Netcode.ConnectionApprovedMessage&,Unity.Netcode.NetworkDelivery,Unity.Netcode.NetworkMessageManager/PointerListWrapper`1<ulong>&) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:641)
Unity.Netcode.NetworkMessageManager:SendMessage<Unity.Netcode.ConnectionApprovedMessage> (Unity.Netcode.ConnectionApprovedMessage&,Unity.Netcode.NetworkDelivery,ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:818)
Unity.Netcode.NetworkConnectionManager:SendMessage<Unity.Netcode.ConnectionApprovedMessage> (Unity.Netcode.ConnectionApprovedMessage&,Unity.Netcode.NetworkDelivery,ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:1330)
Unity.Netcode.NetworkConnectionManager:HandleConnectionApproval (ulong,Unity.Netcode.NetworkManager/ConnectionApprovalResponse) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:851)
Unity.Netcode.NetworkConnectionManager:ProcessPendingApprovals () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:756)
Unity.Netcode.NetworkManager:NetworkUpdate (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:56)
Unity.Netcode.NetworkUpdateLoop:RunNetworkUpdateStage (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:192)
Unity.Netcode.NetworkUpdateLoop/NetworkEarlyUpdate/<>c:<CreateLoopSystem>b__0_0 () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:215)

Animator is not playing an AnimatorController
UnityEngine.StackTraceUtility:ExtractStackTrace ()
Unity.Netcode.Components.NetworkAnimator:WriteSynchronizationData<Unity.Netcode.BufferSerializerWriter> (Unity.Netcode.BufferSerializer`1<Unity.Netcode.BufferSerializerWriter>&) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Components/NetworkAnimator.cs:801)
Unity.Netcode.Components.NetworkAnimator:OnSynchronize<Unity.Netcode.BufferSerializerWriter> (Unity.Netcode.BufferSerializer`1<Unity.Netcode.BufferSerializerWriter>&) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Components/NetworkAnimator.cs:813)
Unity.Netcode.NetworkBehaviour:Synchronize<Unity.Netcode.BufferSerializerWriter> (Unity.Netcode.BufferSerializer`1<Unity.Netcode.BufferSerializerWriter>&,ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:1291)
Unity.Netcode.NetworkObject:SynchronizeNetworkBehaviours<Unity.Netcode.BufferSerializerWriter> (Unity.Netcode.BufferSerializer`1<Unity.Netcode.BufferSerializerWriter>&,ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1825)
Unity.Netcode.NetworkObject/SceneObject:Serialize (Unity.Netcode.FastBufferWriter) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1751)
Unity.Netcode.ConnectionApprovedMessage:Serialize (Unity.Netcode.FastBufferWriter,int) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/Messages/ConnectionApprovedMessage.cs:63)
Unity.Netcode.NetworkMessageManager:SendMessage<Unity.Netcode.ConnectionApprovedMessage, Unity.Netcode.NetworkMessageManager/PointerListWrapper`1<ulong>> (Unity.Netcode.ConnectionApprovedMessage&,Unity.Netcode.NetworkDelivery,Unity.Netcode.NetworkMessageManager/PointerListWrapper`1<ulong>&) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:641)
Unity.Netcode.NetworkMessageManager:SendMessage<Unity.Netcode.ConnectionApprovedMessage> (Unity.Netcode.ConnectionApprovedMessage&,Unity.Netcode.NetworkDelivery,ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:818)
Unity.Netcode.NetworkConnectionManager:SendMessage<Unity.Netcode.ConnectionApprovedMessage> (Unity.Netcode.ConnectionApprovedMessage&,Unity.Netcode.NetworkDelivery,ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:1330)
Unity.Netcode.NetworkConnectionManager:HandleConnectionApproval (ulong,Unity.Netcode.NetworkManager/ConnectionApprovalResponse) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:851)
Unity.Netcode.NetworkConnectionManager:ProcessPendingApprovals () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:756)
Unity.Netcode.NetworkManager:NetworkUpdate (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:56)
Unity.Netcode.NetworkUpdateLoop:RunNetworkUpdateStage (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:192)
Unity.Netcode.NetworkUpdateLoop/NetworkEarlyUpdate/<>c:<CreateLoopSystem>b__0_0 () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:215)

[NetworkManagerCustom] Server-side: Remote client 1 connected, waiting for auth...
UnityEngine.Debug:Log (object)
NetworkManagerCustom:OnClientConnected (ulong) (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:243)
Unity.Netcode.NetworkConnectionManager:InvokeOnClientConnectedCallback (ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:128)
Unity.Netcode.NetworkConnectionManager:HandleConnectionApproval (ulong,Unity.Netcode.NetworkManager/ConnectionApprovalResponse) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:859)
Unity.Netcode.NetworkConnectionManager:ProcessPendingApprovals () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:756)
Unity.Netcode.NetworkManager:NetworkUpdate (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:56)
Unity.Netcode.NetworkUpdateLoop:RunNetworkUpdateStage (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:192)
Unity.Netcode.NetworkUpdateLoop/NetworkEarlyUpdate/<>c:<CreateLoopSystem>b__0_0 () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:215)

[NetworkPlayerSpawner] ===== WAITING FOR PLAYER DATA =====
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:227)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
NetworkPlayerSpawner:SpawnPlayer (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:214)
Unity.Netcode.NetworkConnectionManager:InvokeOnClientConnectedCallback (ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:128)
Unity.Netcode.NetworkConnectionManager:HandleConnectionApproval (ulong,Unity.Netcode.NetworkManager/ConnectionApprovalResponse) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:859)
Unity.Netcode.NetworkConnectionManager:ProcessPendingApprovals () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:756)
Unity.Netcode.NetworkManager:NetworkUpdate (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:56)
Unity.Netcode.NetworkUpdateLoop:RunNetworkUpdateStage (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:192)
Unity.Netcode.NetworkUpdateLoop/NetworkEarlyUpdate/<>c:<CreateLoopSystem>b__0_0 () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:215)

[NetworkPlayerSpawner] ClientId: 1
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:228)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
NetworkPlayerSpawner:SpawnPlayer (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:214)
Unity.Netcode.NetworkConnectionManager:InvokeOnClientConnectedCallback (ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:128)
Unity.Netcode.NetworkConnectionManager:HandleConnectionApproval (ulong,Unity.Netcode.NetworkManager/ConnectionApprovalResponse) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:859)
Unity.Netcode.NetworkConnectionManager:ProcessPendingApprovals () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:756)
Unity.Netcode.NetworkManager:NetworkUpdate (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:56)
Unity.Netcode.NetworkUpdateLoop:RunNetworkUpdateStage (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:192)
Unity.Netcode.NetworkUpdateLoop/NetworkEarlyUpdate/<>c:<CreateLoopSystem>b__0_0 () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:215)

[NetworkPlayerSpawner] Max retries: 120, Interval: 0.1s
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:229)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
NetworkPlayerSpawner:SpawnPlayer (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:214)
Unity.Netcode.NetworkConnectionManager:InvokeOnClientConnectedCallback (ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:128)
Unity.Netcode.NetworkConnectionManager:HandleConnectionApproval (ulong,Unity.Netcode.NetworkManager/ConnectionApprovalResponse) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:859)
Unity.Netcode.NetworkConnectionManager:ProcessPendingApprovals () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:756)
Unity.Netcode.NetworkManager:NetworkUpdate (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:56)
Unity.Netcode.NetworkUpdateLoop:RunNetworkUpdateStage (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:192)
Unity.Netcode.NetworkUpdateLoop/NetworkEarlyUpdate/<>c:<CreateLoopSystem>b__0_0 () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:215)

[ServerPlayerDataManager] GetPlayerDataForClient called for clientId: 1
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:207)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:237)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
NetworkPlayerSpawner:SpawnPlayer (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:214)
Unity.Netcode.NetworkConnectionManager:InvokeOnClientConnectedCallback (ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:128)
Unity.Netcode.NetworkConnectionManager:HandleConnectionApproval (ulong,Unity.Netcode.NetworkManager/ConnectionApprovalResponse) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:859)
Unity.Netcode.NetworkConnectionManager:ProcessPendingApprovals () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:756)
Unity.Netcode.NetworkManager:NetworkUpdate (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:56)
Unity.Netcode.NetworkUpdateLoop:RunNetworkUpdateStage (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:192)
Unity.Netcode.NetworkUpdateLoop/NetworkEarlyUpdate/<>c:<CreateLoopSystem>b__0_0 () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:215)

[ServerPlayerDataManager] Current clientIdToPlayerData cache:
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:210)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:237)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
NetworkPlayerSpawner:SpawnPlayer (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:214)
Unity.Netcode.NetworkConnectionManager:InvokeOnClientConnectedCallback (ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:128)
Unity.Netcode.NetworkConnectionManager:HandleConnectionApproval (ulong,Unity.Netcode.NetworkManager/ConnectionApprovalResponse) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:859)
Unity.Netcode.NetworkConnectionManager:ProcessPendingApprovals () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:756)
Unity.Netcode.NetworkManager:NetworkUpdate (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:56)
Unity.Netcode.NetworkUpdateLoop:RunNetworkUpdateStage (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:192)
Unity.Netcode.NetworkUpdateLoop/NetworkEarlyUpdate/<>c:<CreateLoopSystem>b__0_0 () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:215)

  ClientId: 0 => PlayerData: 1231
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:213)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:237)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
NetworkPlayerSpawner:SpawnPlayer (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:214)
Unity.Netcode.NetworkConnectionManager:InvokeOnClientConnectedCallback (ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:128)
Unity.Netcode.NetworkConnectionManager:HandleConnectionApproval (ulong,Unity.Netcode.NetworkManager/ConnectionApprovalResponse) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:859)
Unity.Netcode.NetworkConnectionManager:ProcessPendingApprovals () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:756)
Unity.Netcode.NetworkManager:NetworkUpdate (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:56)
Unity.Netcode.NetworkUpdateLoop:RunNetworkUpdateStage (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:192)
Unity.Netcode.NetworkUpdateLoop/NetworkEarlyUpdate/<>c:<CreateLoopSystem>b__0_0 () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:215)

[ServerPlayerDataManager] ✗ No player data found for clientId 1
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:222)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:237)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
NetworkPlayerSpawner:SpawnPlayer (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:214)
Unity.Netcode.NetworkConnectionManager:InvokeOnClientConnectedCallback (ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:128)
Unity.Netcode.NetworkConnectionManager:HandleConnectionApproval (ulong,Unity.Netcode.NetworkManager/ConnectionApprovalResponse) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:859)
Unity.Netcode.NetworkConnectionManager:ProcessPendingApprovals () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:756)
Unity.Netcode.NetworkManager:NetworkUpdate (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:56)
Unity.Netcode.NetworkUpdateLoop:RunNetworkUpdateStage (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:192)
Unity.Netcode.NetworkUpdateLoop/NetworkEarlyUpdate/<>c:<CreateLoopSystem>b__0_0 () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:215)

[NetworkPlayerSpawner] Attempt 1/120 - Checking ServerPlayerDataManager for clientId 1...
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:241)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
NetworkPlayerSpawner:SpawnPlayer (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:214)
Unity.Netcode.NetworkConnectionManager:InvokeOnClientConnectedCallback (ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:128)
Unity.Netcode.NetworkConnectionManager:HandleConnectionApproval (ulong,Unity.Netcode.NetworkManager/ConnectionApprovalResponse) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:859)
Unity.Netcode.NetworkConnectionManager:ProcessPendingApprovals () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:756)
Unity.Netcode.NetworkManager:NetworkUpdate (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:56)
Unity.Netcode.NetworkUpdateLoop:RunNetworkUpdateStage (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:192)
Unity.Netcode.NetworkUpdateLoop/NetworkEarlyUpdate/<>c:<CreateLoopSystem>b__0_0 () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:215)

[NetworkPlayerSpawner] ServerPlayerDataManager.Instance exists: True
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:242)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
NetworkPlayerSpawner:SpawnPlayer (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:214)
Unity.Netcode.NetworkConnectionManager:InvokeOnClientConnectedCallback (ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:128)
Unity.Netcode.NetworkConnectionManager:HandleConnectionApproval (ulong,Unity.Netcode.NetworkManager/ConnectionApprovalResponse) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:859)
Unity.Netcode.NetworkConnectionManager:ProcessPendingApprovals () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:756)
Unity.Netcode.NetworkManager:NetworkUpdate (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:56)
Unity.Netcode.NetworkUpdateLoop:RunNetworkUpdateStage (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:192)
Unity.Netcode.NetworkUpdateLoop/NetworkEarlyUpdate/<>c:<CreateLoopSystem>b__0_0 () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:215)

[NetworkPlayerSpawner] PlayerData found: False
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:243)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
NetworkPlayerSpawner:SpawnPlayer (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:214)
Unity.Netcode.NetworkConnectionManager:InvokeOnClientConnectedCallback (ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:128)
Unity.Netcode.NetworkConnectionManager:HandleConnectionApproval (ulong,Unity.Netcode.NetworkManager/ConnectionApprovalResponse) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:859)
Unity.Netcode.NetworkConnectionManager:ProcessPendingApprovals () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:756)
Unity.Netcode.NetworkManager:NetworkUpdate (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:56)
Unity.Netcode.NetworkUpdateLoop:RunNetworkUpdateStage (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:192)
Unity.Netcode.NetworkUpdateLoop/NetworkEarlyUpdate/<>c:<CreateLoopSystem>b__0_0 () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:215)

[ServerPlayerDataManager] GetPlayerDataForClient called for clientId: 1
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:207)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:237)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] ✗ No player data found for clientId 1
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:222)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:237)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] Attempt 11/120 - Checking ServerPlayerDataManager for clientId 1...
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:241)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] ServerPlayerDataManager.Instance exists: True
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:242)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] PlayerData found: False
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:243)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] Attempt 21/120 - Checking ServerPlayerDataManager for clientId 1...
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:241)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] Attempt 31/120 - Checking ServerPlayerDataManager for clientId 1...
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:241)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] Attempt 41/120 - Checking ServerPlayerDataManager for clientId 1...
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:241)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] Attempt 51/120 - Checking ServerPlayerDataManager for clientId 1...
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:241)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] Attempt 61/120 - Checking ServerPlayerDataManager for clientId 1...
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:241)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] Attempt 71/120 - Checking ServerPlayerDataManager for clientId 1...
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:241)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] Attempt 81/120 - Checking ServerPlayerDataManager for clientId 1...
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:241)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] Attempt 91/120 - Checking ServerPlayerDataManager for clientId 1...
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:241)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] Attempt 101/120 - Checking ServerPlayerDataManager for clientId 1...
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:241)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] Attempt 111/120 - Checking ServerPlayerDataManager for clientId 1...
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:241)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] ===== PLAYER DATA TIMEOUT =====
UnityEngine.Debug:LogError (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:275)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] ✗ Player data NOT loaded after 120 attempts (12 seconds) for clientId 1
UnityEngine.Debug:LogError (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:276)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] ✗ Possible issues:
UnityEngine.Debug:LogError (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:277)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner]   1. Client did not send auth (check ClientAuthSender logs)
UnityEngine.Debug:LogError (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:278)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner]   2. Server failed to load player data from DB (check ServerPlayerDataManager logs)
UnityEngine.Debug:LogError (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:279)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner]   3. Player data was loaded but not cached correctly
UnityEngine.Debug:LogError (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:280)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] ✗ Spawning with DEFAULT prefab as fallback
UnityEngine.Debug:LogError (object)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:281)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] ===== GET PLAYER PREFAB FOR CLIENT =====
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner:GetPlayerPrefabForClient (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:426)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:321)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:282)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] ClientId: 1
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner:GetPlayerPrefabForClient (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:427)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:321)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:282)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] Calling ServerPlayerDataManager.GetPlayerDataForClient(1)...
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner:GetPlayerPrefabForClient (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:431)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:321)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:282)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] GetPlayerDataForClient called for clientId: 1
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:207)
NetworkPlayerSpawner:GetPlayerPrefabForClient (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:432)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:321)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:282)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] Current clientIdToPlayerData cache:
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:210)
NetworkPlayerSpawner:GetPlayerPrefabForClient (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:432)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:321)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:282)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

  ClientId: 0 => PlayerData: 1231
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:213)
NetworkPlayerSpawner:GetPlayerPrefabForClient (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:432)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:321)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:282)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] ✗ No player data found for clientId 1
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:222)
NetworkPlayerSpawner:GetPlayerPrefabForClient (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:432)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:321)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:282)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] ⚠️ ServerPlayerDataManager returned NULL for clientId 1
UnityEngine.Debug:LogWarning (object)
NetworkPlayerSpawner:GetPlayerPrefabForClient (ulong) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:443)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:321)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:282)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

Animator is not playing an AnimatorController
UnityEngine.StackTraceUtility:ExtractStackTrace ()
Unity.Netcode.Components.NetworkAnimator:Awake () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Components/NetworkAnimator.cs:569)
UnityEngine.Object:Instantiate<UnityEngine.GameObject> (UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:338)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:282)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

Animator is not playing an AnimatorController
UnityEngine.StackTraceUtility:ExtractStackTrace ()
Unity.Netcode.Components.NetworkAnimator:Awake () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Components/NetworkAnimator.cs:585)
UnityEngine.Object:Instantiate<UnityEngine.GameObject> (UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:338)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:282)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

Animator is not playing an AnimatorController
UnityEngine.StackTraceUtility:ExtractStackTrace ()
Unity.Netcode.Components.NetworkAnimator:Awake () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Components/NetworkAnimator.cs:602)
UnityEngine.Object:Instantiate<UnityEngine.GameObject> (UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:338)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:282)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerSpawner] NetworkObject found, spawning with ownership for client 1
UnityEngine.Debug:Log (object)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:343)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:282)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[PlayerSkillManager] Đã khởi tạo 1 skill(s)
UnityEngine.Debug:Log (object)
PlayerSkillManager:InitializeSkills () (at Assets/Scripts/Player/PlayerSkillManager.cs:82)
PlayerSkillManager:OnNetworkSpawn () (at Assets/Scripts/Player/PlayerSkillManager.cs:31)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:753)
Unity.Netcode.NetworkObject:SpawnInternal (bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:798)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:346)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:282)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkInventory] ===== OnNetworkSpawn CALLED! =====
UnityEngine.Debug:Log (object)
NetworkInventory:OnNetworkSpawn () (at Assets/Scripts/Inventory/NetworkInventory.cs:38)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:753)
Unity.Netcode.NetworkObject:SpawnInternal (bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:798)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:346)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:282)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkInventory] IsServer=True, IsClient=True, IsOwner=False, OwnerClientId=1
UnityEngine.Debug:Log (object)
NetworkInventory:OnNetworkSpawn () (at Assets/Scripts/Inventory/NetworkInventory.cs:39)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:753)
Unity.Netcode.NetworkObject:SpawnInternal (bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:798)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:346)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:282)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkInventory] ===== OnInventoryDataChanged TRIGGERED! =====
UnityEngine.Debug:Log (object)
NetworkInventory:OnInventoryDataChanged (NetworkInventoryData,NetworkInventoryData) (at Assets/Scripts/Inventory/NetworkInventory.cs:106)
Unity.Netcode.NetworkVariable`1<NetworkInventoryData>:set_Value (NetworkInventoryData) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/NetworkVariable/NetworkVariable.cs:165)
NetworkInventory:OnNetworkSpawn () (at Assets/Scripts/Inventory/NetworkInventory.cs:56)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:753)
Unity.Netcode.NetworkObject:SpawnInternal (bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:798)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:346)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:282)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkInventory] IsServer=True, IsClient=True, IsOwner=False
UnityEngine.Debug:Log (object)
NetworkInventory:OnInventoryDataChanged (NetworkInventoryData,NetworkInventoryData) (at Assets/Scripts/Inventory/NetworkInventory.cs:107)
Unity.Netcode.NetworkVariable`1<NetworkInventoryData>:set_Value (NetworkInventoryData) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/NetworkVariable/NetworkVariable.cs:165)
NetworkInventory:OnNetworkSpawn () (at Assets/Scripts/Inventory/NetworkInventory.cs:56)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:753)
Unity.Netcode.NetworkObject:SpawnInternal (bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:798)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:346)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:282)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkInventory] New data has 0 items
UnityEngine.Debug:Log (object)
NetworkInventory:OnInventoryDataChanged (NetworkInventoryData,NetworkInventoryData) (at Assets/Scripts/Inventory/NetworkInventory.cs:117)
Unity.Netcode.NetworkVariable`1<NetworkInventoryData>:set_Value (NetworkInventoryData) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/NetworkVariable/NetworkVariable.cs:165)
NetworkInventory:OnNetworkSpawn () (at Assets/Scripts/Inventory/NetworkInventory.cs:56)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:753)
Unity.Netcode.NetworkObject:SpawnInternal (bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:798)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:346)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:282)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkInventory] Calling OnInventoryChanged?.Invoke()...
UnityEngine.Debug:Log (object)
NetworkInventory:OnInventoryDataChanged (NetworkInventoryData,NetworkInventoryData) (at Assets/Scripts/Inventory/NetworkInventory.cs:121)
Unity.Netcode.NetworkVariable`1<NetworkInventoryData>:set_Value (NetworkInventoryData) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/NetworkVariable/NetworkVariable.cs:165)
NetworkInventory:OnNetworkSpawn () (at Assets/Scripts/Inventory/NetworkInventory.cs:56)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:753)
Unity.Netcode.NetworkObject:SpawnInternal (bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:798)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:346)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:282)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkInventory] ✓ OnInventoryChanged event invoked!
UnityEngine.Debug:Log (object)
NetworkInventory:OnInventoryDataChanged (NetworkInventoryData,NetworkInventoryData) (at Assets/Scripts/Inventory/NetworkInventory.cs:124)
Unity.Netcode.NetworkVariable`1<NetworkInventoryData>:set_Value (NetworkInventoryData) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/NetworkVariable/NetworkVariable.cs:165)
NetworkInventory:OnNetworkSpawn () (at Assets/Scripts/Inventory/NetworkInventory.cs:56)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:753)
Unity.Netcode.NetworkObject:SpawnInternal (bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:798)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:346)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:282)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkInventory] Server: Bắt đầu load inventory từ DB... (OwnerClientId=1)
UnityEngine.Debug:Log (object)
NetworkInventory:OnNetworkSpawn () (at Assets/Scripts/Inventory/NetworkInventory.cs:60)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:753)
Unity.Netcode.NetworkObject:SpawnInternal (bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:798)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:346)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:282)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkInventory] Deserialized inventory on spawn. UsedSlots=0
UnityEngine.Debug:Log (object)
NetworkInventory:OnNetworkSpawn () (at Assets/Scripts/Inventory/NetworkInventory.cs:68)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:753)
Unity.Netcode.NetworkObject:SpawnInternal (bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:798)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:346)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:282)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] GetPlayerDataForClient called for clientId: 1
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:207)
NetworkPlayerDataSync:LoadPlayerDataFromGameManager () (at Assets/Scripts/Network/Shared/NetworkPlayerDataSync.cs:201)
NetworkPlayerDataSync:OnNetworkSpawn () (at Assets/Scripts/Network/Shared/NetworkPlayerDataSync.cs:35)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:753)
Unity.Netcode.NetworkObject:SpawnInternal (bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:798)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:346)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:282)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] Current clientIdToPlayerData cache:
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:210)
NetworkPlayerDataSync:LoadPlayerDataFromGameManager () (at Assets/Scripts/Network/Shared/NetworkPlayerDataSync.cs:201)
NetworkPlayerDataSync:OnNetworkSpawn () (at Assets/Scripts/Network/Shared/NetworkPlayerDataSync.cs:35)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:753)
Unity.Netcode.NetworkObject:SpawnInternal (bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:798)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:346)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:282)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

  ClientId: 0 => PlayerData: 1231
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:213)
NetworkPlayerDataSync:LoadPlayerDataFromGameManager () (at Assets/Scripts/Network/Shared/NetworkPlayerDataSync.cs:201)
NetworkPlayerDataSync:OnNetworkSpawn () (at Assets/Scripts/Network/Shared/NetworkPlayerDataSync.cs:35)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:753)
Unity.Netcode.NetworkObject:SpawnInternal (bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:798)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:346)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:282)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] ✗ No player data found for clientId 1
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:GetPlayerDataForClient (ulong) (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:222)
NetworkPlayerDataSync:LoadPlayerDataFromGameManager () (at Assets/Scripts/Network/Shared/NetworkPlayerDataSync.cs:201)
NetworkPlayerDataSync:OnNetworkSpawn () (at Assets/Scripts/Network/Shared/NetworkPlayerDataSync.cs:35)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:753)
Unity.Netcode.NetworkObject:SpawnInternal (bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:798)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:346)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:282)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerHealth] Player 4 died!
UnityEngine.Debug:Log (object)
NetworkPlayerHealth:HandleDeath () (at Assets/Scripts/Combat/NetworkPlayerHealth.cs:261)
NetworkPlayerHealth:OnHealthValueChanged (int,int) (at Assets/Scripts/Combat/NetworkPlayerHealth.cs:151)
Unity.Netcode.NetworkVariable`1<int>:set_Value (int) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/NetworkVariable/NetworkVariable.cs:165)
NetworkPlayerHealth:SetMaxHealth (int) (at Assets/Scripts/Combat/NetworkPlayerHealth.cs:398)
NetworkPlayerDataSync:ApplyPlayerData () (at Assets/Scripts/Network/Shared/NetworkPlayerDataSync.cs:273)
NetworkPlayerDataSync:OnNetworkSpawn () (at Assets/Scripts/Network/Shared/NetworkPlayerDataSync.cs:55)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:753)
Unity.Netcode.NetworkObject:SpawnInternal (bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:798)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:346)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:282)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

Animator is not playing an AnimatorController
UnityEngine.StackTraceUtility:ExtractStackTrace ()
Unity.Netcode.Components.NetworkAnimator:WriteSynchronizationData<Unity.Netcode.BufferSerializerWriter> (Unity.Netcode.BufferSerializer`1<Unity.Netcode.BufferSerializerWriter>&) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Components/NetworkAnimator.cs:742)
Unity.Netcode.Components.NetworkAnimator:OnSynchronize<Unity.Netcode.BufferSerializerWriter> (Unity.Netcode.BufferSerializer`1<Unity.Netcode.BufferSerializerWriter>&) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Components/NetworkAnimator.cs:813)
Unity.Netcode.NetworkBehaviour:Synchronize<Unity.Netcode.BufferSerializerWriter> (Unity.Netcode.BufferSerializer`1<Unity.Netcode.BufferSerializerWriter>&,ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:1291)
Unity.Netcode.NetworkObject:SynchronizeNetworkBehaviours<Unity.Netcode.BufferSerializerWriter> (Unity.Netcode.BufferSerializer`1<Unity.Netcode.BufferSerializerWriter>&,ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1825)
Unity.Netcode.NetworkObject/SceneObject:Serialize (Unity.Netcode.FastBufferWriter) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1751)
Unity.Netcode.CreateObjectMessage:Serialize (Unity.Netcode.FastBufferWriter,int) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/Messages/CreateObjectMessage.cs:13)
Unity.Netcode.NetworkMessageManager:SendMessage<Unity.Netcode.CreateObjectMessage, Unity.Netcode.NetworkMessageManager/PointerListWrapper`1<ulong>> (Unity.Netcode.CreateObjectMessage&,Unity.Netcode.NetworkDelivery,Unity.Netcode.NetworkMessageManager/PointerListWrapper`1<ulong>&) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:641)
Unity.Netcode.NetworkMessageManager:SendMessage<Unity.Netcode.CreateObjectMessage> (Unity.Netcode.CreateObjectMessage&,Unity.Netcode.NetworkDelivery,ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:818)
Unity.Netcode.NetworkConnectionManager:SendMessage<Unity.Netcode.CreateObjectMessage> (Unity.Netcode.CreateObjectMessage&,Unity.Netcode.NetworkDelivery,ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:1330)
Unity.Netcode.NetworkSpawnManager:SendSpawnCallForObject (ulong,Unity.Netcode.NetworkObject) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:904)
Unity.Netcode.NetworkObject:SpawnInternal (bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:804)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:346)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:282)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

Animator is not playing an AnimatorController
UnityEngine.StackTraceUtility:ExtractStackTrace ()
Unity.Netcode.Components.NetworkAnimator:WriteSynchronizationData<Unity.Netcode.BufferSerializerWriter> (Unity.Netcode.BufferSerializer`1<Unity.Netcode.BufferSerializerWriter>&) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Components/NetworkAnimator.cs:801)
Unity.Netcode.Components.NetworkAnimator:OnSynchronize<Unity.Netcode.BufferSerializerWriter> (Unity.Netcode.BufferSerializer`1<Unity.Netcode.BufferSerializerWriter>&) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Components/NetworkAnimator.cs:813)
Unity.Netcode.NetworkBehaviour:Synchronize<Unity.Netcode.BufferSerializerWriter> (Unity.Netcode.BufferSerializer`1<Unity.Netcode.BufferSerializerWriter>&,ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:1291)
Unity.Netcode.NetworkObject:SynchronizeNetworkBehaviours<Unity.Netcode.BufferSerializerWriter> (Unity.Netcode.BufferSerializer`1<Unity.Netcode.BufferSerializerWriter>&,ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1825)
Unity.Netcode.NetworkObject/SceneObject:Serialize (Unity.Netcode.FastBufferWriter) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1751)
Unity.Netcode.CreateObjectMessage:Serialize (Unity.Netcode.FastBufferWriter,int) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/Messages/CreateObjectMessage.cs:13)
Unity.Netcode.NetworkMessageManager:SendMessage<Unity.Netcode.CreateObjectMessage, Unity.Netcode.NetworkMessageManager/PointerListWrapper`1<ulong>> (Unity.Netcode.CreateObjectMessage&,Unity.Netcode.NetworkDelivery,Unity.Netcode.NetworkMessageManager/PointerListWrapper`1<ulong>&) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:641)
Unity.Netcode.NetworkMessageManager:SendMessage<Unity.Netcode.CreateObjectMessage> (Unity.Netcode.CreateObjectMessage&,Unity.Netcode.NetworkDelivery,ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:818)
Unity.Netcode.NetworkConnectionManager:SendMessage<Unity.Netcode.CreateObjectMessage> (Unity.Netcode.CreateObjectMessage&,Unity.Netcode.NetworkDelivery,ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:1330)
Unity.Netcode.NetworkSpawnManager:SendSpawnCallForObject (ulong,Unity.Netcode.NetworkObject) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:904)
Unity.Netcode.NetworkObject:SpawnInternal (bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:804)
NetworkPlayerSpawner:SpawnPlayerNow (ulong,PlayerDataResponse) (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:346)
NetworkPlayerSpawner/<SpawnPlayerWhenDataReady>d__27:MoveNext () (at Assets/Scripts/Network/Shared/NetworkPlayerSpawner.cs:282)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[NetworkPlayerHealth] No spawn points found, using current position: (0.40, 0.20, -0.06)
UnityEngine.Debug:LogWarning (object)
NetworkPlayerHealth:Start () (at Assets/Scripts/Combat/NetworkPlayerHealth.cs:118)

Login API Response: {"token":"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIyIiwidW5pcXVlX25hbWUiOiIyIiwidXNlcl9pZCI6IjIiLCJleHAiOjE3NzI4ODM3OTgsImlzcyI6IkdhbWVTZXJ2ZXJBcGkiLCJhdWQiOiJHYW1lQ2xpZW50In0.u_jr8XRCB06HyChdctMYW8X_kgQlpREU_I1QzMirtL0","user_id":2,"username":"2"}
UnityEngine.Debug:Log (object)
APIClient/<LoginCoroutine>d__12:MoveNext () (at Assets/Scripts/API/APIClient.cs:285)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

Final LoginResponse - user_id: 2, username: 2, token length: 216
UnityEngine.Debug:Log (object)
APIClient/<LoginCoroutine>d__12:MoveNext () (at Assets/Scripts/API/APIClient.cs:338)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[GameManager] Player data set: Level 1, Map 0
UnityEngine.Debug:Log (object)
GameManager:SetPlayerData (PlayerDataResponse) (at Assets/Scripts/Core/GameManager.cs:89)
LoginController/<>c:<LoadPlayerData>b__8_0 (PlayerDataResponse) (at Assets/Scripts/UI/LoginController.cs:103)
APIClient/<LoadPlayerDataCoroutine>d__16:MoveNext () (at Assets/Scripts/API/APIClient.cs:424)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ServerPlayerDataManager] Creating new instance with DontDestroyOnLoad
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:Awake () (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:35)

[ServerPlayerDataManager] Initializing APIClient...
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:InitializeAPIClient () (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:63)
ServerPlayerDataManager:Awake () (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:39)

[ServerPlayerDataManager] Using existing APIClient.Instance (has token from login)
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:InitializeAPIClient () (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:68)
ServerPlayerDataManager:Awake () (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:39)

[ServerPlayerDataManager] ✓ APIClient has token: YES, length: 216
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:InitializeAPIClient () (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:72)
ServerPlayerDataManager:Awake () (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:39)

[ItemTemplateManagerBootstrap] APIClient đã tồn tại
UnityEngine.Debug:Log (object)
ItemTemplateManagerBootstrap:Awake () (at Assets/Scripts/Inventory/ItemTemplateManagerBootstrap.cs:30)

[ItemTemplateManagerBootstrap] ItemTemplateManager chưa có, đang tạo...
UnityEngine.Debug:Log (object)
ItemTemplateManagerBootstrap:Awake () (at Assets/Scripts/Inventory/ItemTemplateManagerBootstrap.cs:36)

[ItemTemplateManager] ✅ Singleton initialized - GameObject: ItemTemplateManager
UnityEngine.Debug:Log (object)
ItemTemplateManager:Awake () (at Assets/Scripts/Inventory/ItemTemplateManager.cs:45)
UnityEngine.GameObject:AddComponent<ItemTemplateManager> ()
ItemTemplateManagerBootstrap:Awake () (at Assets/Scripts/Inventory/ItemTemplateManagerBootstrap.cs:39)

[ItemTemplateManagerBootstrap] ✅ Đã tạo ItemTemplateManager
UnityEngine.Debug:Log (object)
ItemTemplateManagerBootstrap:Awake () (at Assets/Scripts/Inventory/ItemTemplateManagerBootstrap.cs:41)

[IconDatabase] Loaded 7 item icons from Resources/ItemIcons
UnityEngine.Debug:Log (object)
IconDatabase:LoadAllIcons () (at Assets/Scripts/Inventory/IconDatabase.cs:53)
IconDatabase:Awake () (at Assets/Scripts/Inventory/IconDatabase.cs:31)

[ServerPlayerDataManager] ✓ APIClient verified in Start()
UnityEngine.Debug:Log (object)
ServerPlayerDataManager:Start () (at Assets/Scripts/Network/Host/ServerPlayerDataManager.cs:99)

[NetworkPrefabRegistrar] ItemPickup prefab not found! Please assign it in Inspector or make sure ItemSpawner/EnemyItemDrop exists in scene.
UnityEngine.Debug:LogWarning (object)
NetworkPrefabRegistrar:RegisterItemPickupPrefab (Unity.Netcode.NetworkManager,int&) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:216)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:94)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar] ✓ Registered 0 prefab(s) to NetworkManager
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:97)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar] ===== REGISTERED PREFABS LIST =====
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:111)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar] Total registered prefabs: 13
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:112)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar]   - Prefab: 'Enemy1' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar]   - Prefab: 'FireballProjectile' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar]   - Prefab: 'EarthPrefab' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar]   - Prefab: 'FirePrefab' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar]   - Prefab: 'MetalPrefab' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar]   - Prefab: 'MetalPrefab_1' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar]   - Prefab: 'NetworkPlayer' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar]   - Prefab: 'WaterPrefab' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar]   - Prefab: 'WoodPrefab' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar]   - Prefab: 'InventorySlot' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar]   - Prefab: 'SkillEffect' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar]   - Prefab: 'ItemPickup' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar]   - Prefab: 'AuthSenderNetworkObjectPrefab' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[NetworkPrefabRegistrar] ===== END PREFABS LIST =====
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:128)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:ReRegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:284)
GameSceneNetworkInitializer:RegisterNetworkPrefabs () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:151)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:44)

[GameSceneNetworkInitializer] Setting up host components...
UnityEngine.Debug:Log (object)
GameSceneNetworkInitializer:SetupHostComponents () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:160)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:70)

[GameSceneNetworkInitializer] ServerConnectionApproval already exists.
UnityEngine.Debug:Log (object)
GameSceneNetworkInitializer:SetupHostComponents () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:172)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:70)

[GameSceneNetworkInitializer] ServerPlayerDataManager instance already exists.
UnityEngine.Debug:Log (object)
GameSceneNetworkInitializer:SetupHostComponents () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:185)
GameSceneNetworkInitializer:Start () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:70)

[NetworkPrefabRegistrar] ItemPickup prefab not found! Please assign it in Inspector or make sure ItemSpawner/EnemyItemDrop exists in scene.
UnityEngine.Debug:LogWarning (object)
NetworkPrefabRegistrar:RegisterItemPickupPrefab (Unity.Netcode.NetworkManager,int&) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:216)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:94)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar] ✓ Registered 0 prefab(s) to NetworkManager
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:97)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar] ===== REGISTERED PREFABS LIST =====
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:111)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar] Total registered prefabs: 13
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:112)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar]   - Prefab: 'Enemy1' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar]   - Prefab: 'FireballProjectile' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar]   - Prefab: 'EarthPrefab' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar]   - Prefab: 'FirePrefab' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar]   - Prefab: 'MetalPrefab' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar]   - Prefab: 'MetalPrefab_1' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar]   - Prefab: 'NetworkPlayer' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar]   - Prefab: 'WaterPrefab' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar]   - Prefab: 'WoodPrefab' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar]   - Prefab: 'InventorySlot' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar]   - Prefab: 'SkillEffect' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar]   - Prefab: 'ItemPickup' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar]   - Prefab: 'AuthSenderNetworkObjectPrefab' (has NetworkObject)
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:120)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

[NetworkPrefabRegistrar] ===== END PREFABS LIST =====
UnityEngine.Debug:Log (object)
NetworkPrefabRegistrar:LogRegisteredPrefabs (Unity.Netcode.NetworkManager) (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:128)
NetworkPrefabRegistrar:RegisterPrefabs () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:100)
NetworkPrefabRegistrar:Start () (at Assets/Scripts/Network/Shared/NetworkPrefabRegistrar.cs:32)

==================== [InventoryNetworkBridge] START() ĐƯỢC GỌI! ====================
UnityEngine.Debug:Log (object)
InventoryNetworkBridge:Start () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:196)

[InventoryNetworkBridge] ✓ NetworkManager.Singleton exists
UnityEngine.Debug:Log (object)
InventoryNetworkBridge:Start () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:219)

[InventoryNetworkBridge] SubscribeToNetworkEvents() được gọi...
UnityEngine.Debug:Log (object)
InventoryNetworkBridge:SubscribeToNetworkEvents () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:246)
InventoryNetworkBridge:Start () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:223)

[InventoryNetworkBridge] ✓ Đã subscribe OnClientConnectedCallback
UnityEngine.Debug:Log (object)
InventoryNetworkBridge:SubscribeToNetworkEvents () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:259)
InventoryNetworkBridge:Start () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:223)

[InventoryNetworkBridge] Đang tìm NetworkInventory lần đầu tiên...
UnityEngine.Debug:Log (object)
InventoryNetworkBridge:Start () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:228)

[InventoryNetworkBridge] ========== FindPlayerInventory() BẮT ĐẦU ==========
UnityEngine.Debug:Log (object)
InventoryNetworkBridge:FindPlayerInventory () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:372)
InventoryNetworkBridge:Start () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:229)

[InventoryNetworkBridge] NetworkManager.SpawnManager is null! Network may not be initialized yet.
UnityEngine.Debug:LogWarning (object)
InventoryNetworkBridge:FindPlayerInventory () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:383)
InventoryNetworkBridge:Start () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:229)

[InventoryNetworkBridge] ⚠️ Chưa tìm thấy NetworkInventory trong Start(), sẽ tìm lại sau khi client connect.
UnityEngine.Debug:LogWarning (object)
InventoryNetworkBridge:Start () (at Assets/Scripts/Inventory/InventoryNetworkBridge.cs:240)

[ItemTemplateManager] 🚀 Start() called - autoLoadOnStart=True
UnityEngine.Debug:Log (object)
ItemTemplateManager:Start () (at Assets/Scripts/Inventory/ItemTemplateManager.cs:57)

[ItemTemplateManager] ⏳ Đang đợi APIClient sẵn sàng...
UnityEngine.Debug:Log (object)
ItemTemplateManager/<LoadItemTemplatesWhenReady>d__12:MoveNext () (at Assets/Scripts/Inventory/ItemTemplateManager.cs:74)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
ItemTemplateManager:Start () (at Assets/Scripts/Inventory/ItemTemplateManager.cs:61)

[ItemTemplateManager] ✅ APIClient đã sẵn sàng sau 0.0s
UnityEngine.Debug:Log (object)
ItemTemplateManager/<LoadItemTemplatesWhenReady>d__12:MoveNext () (at Assets/Scripts/Inventory/ItemTemplateManager.cs:93)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
ItemTemplateManager:Start () (at Assets/Scripts/Inventory/ItemTemplateManager.cs:61)

[ItemTemplateManager] 📥 LoadItemTemplatesFromAPI() called - isLoading=False, isLoaded=False
UnityEngine.Debug:Log (object)
ItemTemplateManager:LoadItemTemplatesFromAPI () (at Assets/Scripts/Inventory/ItemTemplateManager.cs:103)
ItemTemplateManager/<LoadItemTemplatesWhenReady>d__12:MoveNext () (at Assets/Scripts/Inventory/ItemTemplateManager.cs:94)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
ItemTemplateManager:Start () (at Assets/Scripts/Inventory/ItemTemplateManager.cs:61)

[ItemTemplateManager] 🌐 Bắt đầu gọi API để load item templates...
UnityEngine.Debug:Log (object)
ItemTemplateManager:LoadItemTemplatesFromAPI () (at Assets/Scripts/Inventory/ItemTemplateManager.cs:126)
ItemTemplateManager/<LoadItemTemplatesWhenReady>d__12:MoveNext () (at Assets/Scripts/Inventory/ItemTemplateManager.cs:94)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
ItemTemplateManager:Start () (at Assets/Scripts/Inventory/ItemTemplateManager.cs:61)

[APIClient] 🌐 Sending GET request to: http://localhost:5000/api/item/templates
UnityEngine.Debug:Log (object)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:631)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
APIClient:GetItemTemplates (System.Action`1<ItemTemplateDto[]>,System.Action`1<string>) (at Assets/Scripts/API/APIClient.cs:625)
ItemTemplateManager:LoadItemTemplatesFromAPI () (at Assets/Scripts/Inventory/ItemTemplateManager.cs:128)
ItemTemplateManager/<LoadItemTemplatesWhenReady>d__12:MoveNext () (at Assets/Scripts/Inventory/ItemTemplateManager.cs:94)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
ItemTemplateManager:Start () (at Assets/Scripts/Inventory/ItemTemplateManager.cs:61)

[APIClient] ✅ Item templates response received - Length: 2505 chars
UnityEngine.Debug:Log (object)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:642)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[APIClient] 📄 Response preview: {"count":11,"item_templates":[{"id":1,"code":"SWORD_001","name":"Iron Sword","description":"A basic iron sword","category":1,"item_type":1,"stackable":false,"max_stack":1,"rarity":1,"icon_id":"client_...
UnityEngine.Debug:Log (object)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:643)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[APIClient] ✅ Parsed 11 item templates successfully
UnityEngine.Debug:Log (object)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:652)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ItemTemplateManager] 📦 OnItemTemplatesLoaded() - Received 11 templates
UnityEngine.Debug:Log (object)
ItemTemplateManager:OnItemTemplatesLoaded (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:150)
ItemTemplateManager:<LoadItemTemplatesFromAPI>b__13_0 (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:132)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:653)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ItemTemplateManager] ✅ Đã load 11 item templates thành công!
UnityEngine.Debug:Log (object)
ItemTemplateManager:OnItemTemplatesLoaded (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:165)
ItemTemplateManager:<LoadItemTemplatesFromAPI>b__13_0 (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:132)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:653)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ItemTemplateManager] 📊 Dictionary Stats - ById: 11, ByCode: 11
UnityEngine.Debug:Log (object)
ItemTemplateManager:OnItemTemplatesLoaded (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:166)
ItemTemplateManager:<LoadItemTemplatesFromAPI>b__13_0 (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:132)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:653)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[ItemTemplateManager] 📋 Logging first 10 items:
UnityEngine.Debug:Log (object)
ItemTemplateManager:OnItemTemplatesLoaded (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:170)
ItemTemplateManager:<LoadItemTemplatesFromAPI>b__13_0 (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:132)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:653)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

  [1] ID=1, Name='Iron Sword', Code='SWORD_001', IconId='client_icon_1', Type=1, Stackable=False
UnityEngine.Debug:Log (object)
ItemTemplateManager:OnItemTemplatesLoaded (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:174)
ItemTemplateManager:<LoadItemTemplatesFromAPI>b__13_0 (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:132)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:653)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

  [2] ID=2, Name='Steel Sword', Code='SWORD_002', IconId='client_icon_2', Type=1, Stackable=False
UnityEngine.Debug:Log (object)
ItemTemplateManager:OnItemTemplatesLoaded (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:174)
ItemTemplateManager:<LoadItemTemplatesFromAPI>b__13_0 (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:132)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:653)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

  [3] ID=3, Name='Wooden Bow', Code='BOW_001', IconId='client_icon_3', Type=2, Stackable=False
UnityEngine.Debug:Log (object)
ItemTemplateManager:OnItemTemplatesLoaded (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:174)
ItemTemplateManager:<LoadItemTemplatesFromAPI>b__13_0 (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:132)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:653)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

  [4] ID=4, Name='Small Health Potion', Code='POTION_HP_SMALL', IconId='client_icon_4', Type=1, Stackable=True
UnityEngine.Debug:Log (object)
ItemTemplateManager:OnItemTemplatesLoaded (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:174)
ItemTemplateManager:<LoadItemTemplatesFromAPI>b__13_0 (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:132)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:653)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

  [5] ID=5, Name='Medium Health Potion', Code='POTION_HP_MEDIUM', IconId='client_icon_5', Type=1, Stackable=True
UnityEngine.Debug:Log (object)
ItemTemplateManager:OnItemTemplatesLoaded (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:174)
ItemTemplateManager:<LoadItemTemplatesFromAPI>b__13_0 (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:132)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:653)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

  [6] ID=6, Name='Small Mana Potion', Code='POTION_MP_SMALL', IconId='client_icon_6', Type=2, Stackable=True
UnityEngine.Debug:Log (object)
ItemTemplateManager:OnItemTemplatesLoaded (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:174)
ItemTemplateManager:<LoadItemTemplatesFromAPI>b__13_0 (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:132)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:653)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

  [7] ID=7, Name='Wood', Code='MATERIAL_WOOD', IconId='client_icon_7', Type=1, Stackable=True
UnityEngine.Debug:Log (object)
ItemTemplateManager:OnItemTemplatesLoaded (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:174)
ItemTemplateManager:<LoadItemTemplatesFromAPI>b__13_0 (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:132)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:653)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

  [8] ID=8, Name='Iron Ore', Code='MATERIAL_IRON_ORE', IconId='client_icon_1', Type=1, Stackable=True
UnityEngine.Debug:Log (object)
ItemTemplateManager:OnItemTemplatesLoaded (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:174)
ItemTemplateManager:<LoadItemTemplatesFromAPI>b__13_0 (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:132)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:653)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

  [9] ID=9, Name='Herb', Code='MATERIAL_HERB', IconId='material_herb', Type=2, Stackable=True
UnityEngine.Debug:Log (object)
ItemTemplateManager:OnItemTemplatesLoaded (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:174)
ItemTemplateManager:<LoadItemTemplatesFromAPI>b__13_0 (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:132)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:653)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

  [10] ID=10, Name='Leather Armor', Code='ARMOR_LEATHER', IconId='armor_leather', Type=3, Stackable=False
UnityEngine.Debug:Log (object)
ItemTemplateManager:OnItemTemplatesLoaded (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:174)
ItemTemplateManager:<LoadItemTemplatesFromAPI>b__13_0 (ItemTemplateDto[]) (at Assets/Scripts/Inventory/ItemTemplateManager.cs:132)
APIClient/<GetItemTemplatesCoroutine>d__29:MoveNext () (at Assets/Scripts/API/APIClient.cs:653)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[GameSceneNetworkInitializer] ===== STARTING CLIENT MODE =====
UnityEngine.Debug:Log (object)
GameSceneNetworkInitializer:StartClientMode () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:327)
GameSceneNetworkInitializer:OnStartClientButtonClicked () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:403)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@1.0.0/Runtime/EventSystem/EventSystem.cs:530)

[GameSceneNetworkInitializer] Waiting for prefabs to be registered before starting client...
UnityEngine.Debug:Log (object)
GameSceneNetworkInitializer/<StartClientAfterDelay>d__22:MoveNext () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:352)
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
GameSceneNetworkInitializer:StartClientMode () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:328)
GameSceneNetworkInitializer:OnStartClientButtonClicked () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:403)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@1.0.0/Runtime/EventSystem/EventSystem.cs:530)

[GameSceneNetworkInitializer] ✓ Prefabs should be registered now, starting client connection...
UnityEngine.Debug:Log (object)
GameSceneNetworkInitializer/<StartClientAfterDelay>d__22:MoveNext () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:358)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[GameSceneNetworkInitializer] Starting CLIENT mode, connecting to 127.0.0.1:2003...
UnityEngine.Debug:Log (object)
GameSceneNetworkInitializer:StartClientConnection () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:336)
GameSceneNetworkInitializer/<StartClientAfterDelay>d__22:MoveNext () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:359)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[GameSceneNetworkInitializer] After connection, userid will be automatically sent to host via ClientAuthSender.
UnityEngine.Debug:Log (object)
GameSceneNetworkInitializer:StartClientConnection () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:337)
GameSceneNetworkInitializer/<StartClientAfterDelay>d__22:MoveNext () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:359)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[GameSceneNetworkInitializer] NetworkManagerCustom.OnClientConnected will call ClientAuthSender.SendAuthAfterConnection()
UnityEngine.Debug:Log (object)
GameSceneNetworkInitializer:StartClientConnection () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:338)
GameSceneNetworkInitializer/<StartClientAfterDelay>d__22:MoveNext () (at Assets/Scripts/Network/GameSceneNetworkInitializer.cs:359)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

[Netcode] Failed to create object locally. [globalObjectIdHash=818046180]. NetworkPrefab could not be found. Is the prefab registered with NetworkManager?
UnityEngine.Debug:LogError (object)
Unity.Netcode.NetworkLog:LogError (string) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Logging/NetworkLog.cs:34)
Unity.Netcode.NetworkSpawnManager:GetNetworkObjectToSpawn (uint,ulong,System.Nullable`1<UnityEngine.Vector3>,System.Nullable`1<UnityEngine.Quaternion>,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:554)
Unity.Netcode.NetworkSpawnManager:CreateLocalNetworkObject (Unity.Netcode.NetworkObject/SceneObject) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:611)
Unity.Netcode.NetworkObject:AddSceneObject (Unity.Netcode.NetworkObject/SceneObject&,Unity.Netcode.FastBufferReader,Unity.Netcode.NetworkManager) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1959)
Unity.Netcode.ConnectionApprovedMessage:Handle (Unity.Netcode.NetworkContext&) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/Messages/ConnectionApprovedMessage.cs:166)
Unity.Netcode.NetworkMessageManager:ReceiveMessage<Unity.Netcode.ConnectionApprovedMessage> (Unity.Netcode.FastBufferReader,Unity.Netcode.NetworkContext&,Unity.Netcode.NetworkMessageManager) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:582)
Unity.Netcode.NetworkMessageManager:HandleMessage (Unity.Netcode.NetworkMessageHeader&,Unity.Netcode.FastBufferReader,ulong,single,int) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:446)
Unity.Netcode.NetworkMessageManager:ProcessIncomingMessageQueue () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:472)
Unity.Netcode.NetworkManager:NetworkUpdate (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:62)
Unity.Netcode.NetworkUpdateLoop:RunNetworkUpdateStage (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:192)
Unity.Netcode.NetworkUpdateLoop/NetworkEarlyUpdate/<>c:<CreateLoopSystem>b__0_0 () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:215)

[Netcode] Failed to spawn NetworkObject for Hash 818046180.
UnityEngine.Debug:LogError (object)
Unity.Netcode.NetworkLog:LogError (string) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Logging/NetworkLog.cs:34)
Unity.Netcode.NetworkObject:AddSceneObject (Unity.Netcode.NetworkObject/SceneObject&,Unity.Netcode.FastBufferReader,Unity.Netcode.NetworkManager) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1966)
Unity.Netcode.ConnectionApprovedMessage:Handle (Unity.Netcode.NetworkContext&) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/Messages/ConnectionApprovedMessage.cs:166)
Unity.Netcode.NetworkMessageManager:ReceiveMessage<Unity.Netcode.ConnectionApprovedMessage> (Unity.Netcode.FastBufferReader,Unity.Netcode.NetworkContext&,Unity.Netcode.NetworkMessageManager) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:582)
Unity.Netcode.NetworkMessageManager:HandleMessage (Unity.Netcode.NetworkMessageHeader&,Unity.Netcode.FastBufferReader,ulong,single,int) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:446)
Unity.Netcode.NetworkMessageManager:ProcessIncomingMessageQueue () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:472)
Unity.Netcode.NetworkManager:NetworkUpdate (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:62)
Unity.Netcode.NetworkUpdateLoop:RunNetworkUpdateStage (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:192)
Unity.Netcode.NetworkUpdateLoop/NetworkEarlyUpdate/<>c:<CreateLoopSystem>b__0_0 () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:215)

Animator is not playing an AnimatorController
UnityEngine.StackTraceUtility:ExtractStackTrace ()
Unity.Netcode.Components.NetworkAnimator:Awake () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Components/NetworkAnimator.cs:569)
UnityEngine.Object:Instantiate<UnityEngine.GameObject> (UnityEngine.GameObject)
Unity.Netcode.NetworkSpawnManager:InstantiateNetworkPrefab (UnityEngine.GameObject,uint,System.Nullable`1<UnityEngine.Vector3>,System.Nullable`1<UnityEngine.Quaternion>) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:583)
Unity.Netcode.NetworkSpawnManager:GetNetworkObjectToSpawn (uint,ulong,System.Nullable`1<UnityEngine.Vector3>,System.Nullable`1<UnityEngine.Quaternion>,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:560)
Unity.Netcode.NetworkSpawnManager:CreateLocalNetworkObject (Unity.Netcode.NetworkObject/SceneObject) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:611)
Unity.Netcode.NetworkObject:AddSceneObject (Unity.Netcode.NetworkObject/SceneObject&,Unity.Netcode.FastBufferReader,Unity.Netcode.NetworkManager) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1959)
Unity.Netcode.ConnectionApprovedMessage:Handle (Unity.Netcode.NetworkContext&) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/Messages/ConnectionApprovedMessage.cs:166)
Unity.Netcode.NetworkMessageManager:ReceiveMessage<Unity.Netcode.ConnectionApprovedMessage> (Unity.Netcode.FastBufferReader,Unity.Netcode.NetworkContext&,Unity.Netcode.NetworkMessageManager) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:582)
Unity.Netcode.NetworkMessageManager:HandleMessage (Unity.Netcode.NetworkMessageHeader&,Unity.Netcode.FastBufferReader,ulong,single,int) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:446)
Unity.Netcode.NetworkMessageManager:ProcessIncomingMessageQueue () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:472)
Unity.Netcode.NetworkManager:NetworkUpdate (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:62)
Unity.Netcode.NetworkUpdateLoop:RunNetworkUpdateStage (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:192)
Unity.Netcode.NetworkUpdateLoop/NetworkEarlyUpdate/<>c:<CreateLoopSystem>b__0_0 () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:215)

Animator is not playing an AnimatorController
UnityEngine.StackTraceUtility:ExtractStackTrace ()
Unity.Netcode.Components.NetworkAnimator:Awake () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Components/NetworkAnimator.cs:585)
UnityEngine.Object:Instantiate<UnityEngine.GameObject> (UnityEngine.GameObject)
Unity.Netcode.NetworkSpawnManager:InstantiateNetworkPrefab (UnityEngine.GameObject,uint,System.Nullable`1<UnityEngine.Vector3>,System.Nullable`1<UnityEngine.Quaternion>) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:583)
Unity.Netcode.NetworkSpawnManager:GetNetworkObjectToSpawn (uint,ulong,System.Nullable`1<UnityEngine.Vector3>,System.Nullable`1<UnityEngine.Quaternion>,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:560)
Unity.Netcode.NetworkSpawnManager:CreateLocalNetworkObject (Unity.Netcode.NetworkObject/SceneObject) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:611)
Unity.Netcode.NetworkObject:AddSceneObject (Unity.Netcode.NetworkObject/SceneObject&,Unity.Netcode.FastBufferReader,Unity.Netcode.NetworkManager) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1959)
Unity.Netcode.ConnectionApprovedMessage:Handle (Unity.Netcode.NetworkContext&) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/Messages/ConnectionApprovedMessage.cs:166)
Unity.Netcode.NetworkMessageManager:ReceiveMessage<Unity.Netcode.ConnectionApprovedMessage> (Unity.Netcode.FastBufferReader,Unity.Netcode.NetworkContext&,Unity.Netcode.NetworkMessageManager) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:582)
Unity.Netcode.NetworkMessageManager:HandleMessage (Unity.Netcode.NetworkMessageHeader&,Unity.Netcode.FastBufferReader,ulong,single,int) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:446)
Unity.Netcode.NetworkMessageManager:ProcessIncomingMessageQueue () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:472)
Unity.Netcode.NetworkManager:NetworkUpdate (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:62)
Unity.Netcode.NetworkUpdateLoop:RunNetworkUpdateStage (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:192)
Unity.Netcode.NetworkUpdateLoop/NetworkEarlyUpdate/<>c:<CreateLoopSystem>b__0_0 () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:215)

Animator is not playing an AnimatorController
UnityEngine.StackTraceUtility:ExtractStackTrace ()
Unity.Netcode.Components.NetworkAnimator:Awake () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Components/NetworkAnimator.cs:602)
UnityEngine.Object:Instantiate<UnityEngine.GameObject> (UnityEngine.GameObject)
Unity.Netcode.NetworkSpawnManager:InstantiateNetworkPrefab (UnityEngine.GameObject,uint,System.Nullable`1<UnityEngine.Vector3>,System.Nullable`1<UnityEngine.Quaternion>) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:583)
Unity.Netcode.NetworkSpawnManager:GetNetworkObjectToSpawn (uint,ulong,System.Nullable`1<UnityEngine.Vector3>,System.Nullable`1<UnityEngine.Quaternion>,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:560)
Unity.Netcode.NetworkSpawnManager:CreateLocalNetworkObject (Unity.Netcode.NetworkObject/SceneObject) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:611)
Unity.Netcode.NetworkObject:AddSceneObject (Unity.Netcode.NetworkObject/SceneObject&,Unity.Netcode.FastBufferReader,Unity.Netcode.NetworkManager) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1959)
Unity.Netcode.ConnectionApprovedMessage:Handle (Unity.Netcode.NetworkContext&) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/Messages/ConnectionApprovedMessage.cs:166)
Unity.Netcode.NetworkMessageManager:ReceiveMessage<Unity.Netcode.ConnectionApprovedMessage> (Unity.Netcode.FastBufferReader,Unity.Netcode.NetworkContext&,Unity.Netcode.NetworkMessageManager) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:582)
Unity.Netcode.NetworkMessageManager:HandleMessage (Unity.Netcode.NetworkMessageHeader&,Unity.Netcode.FastBufferReader,ulong,single,int) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:446)
Unity.Netcode.NetworkMessageManager:ProcessIncomingMessageQueue () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:472)
Unity.Netcode.NetworkManager:NetworkUpdate (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:62)
Unity.Netcode.NetworkUpdateLoop:RunNetworkUpdateStage (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:192)
Unity.Netcode.NetworkUpdateLoop/NetworkEarlyUpdate/<>c:<CreateLoopSystem>b__0_0 () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:215)

[PlayerSkillManager] Đã khởi tạo 1 skill(s)
UnityEngine.Debug:Log (object)
PlayerSkillManager:InitializeSkills () (at Assets/Scripts/Player/PlayerSkillManager.cs:82)
PlayerSkillManager:OnNetworkSpawn () (at Assets/Scripts/Player/PlayerSkillManager.cs:31)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,Unity.Netcode.NetworkObject/SceneObject&,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:778)
Unity.Netcode.NetworkObject:AddSceneObject (Unity.Netcode.NetworkObject/SceneObject&,Unity.Netcode.FastBufferReader,Unity.Netcode.NetworkManager) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:2005)
Unity.Netcode.ConnectionApprovedMessage:Handle (Unity.Netcode.NetworkContext&) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/Messages/ConnectionApprovedMessage.cs:166)
Unity.Netcode.NetworkMessageManager:ReceiveMessage<Unity.Netcode.ConnectionApprovedMessage> (Unity.Netcode.FastBufferReader,Unity.Netcode.NetworkContext&,Unity.Netcode.NetworkMessageManager) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:582)
Unity.Netcode.NetworkMessageManager:HandleMessage (Unity.Netcode.NetworkMessageHeader&,Unity.Netcode.FastBufferReader,ulong,single,int) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:446)
Unity.Netcode.NetworkMessageManager:ProcessIncomingMessageQueue () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:472)
Unity.Netcode.NetworkManager:NetworkUpdate (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:62)
Unity.Netcode.NetworkUpdateLoop:RunNetworkUpdateStage (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:192)
Unity.Netcode.NetworkUpdateLoop/NetworkEarlyUpdate/<>c:<CreateLoopSystem>b__0_0 () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:215)

[NetworkInventory] ===== OnNetworkSpawn CALLED! =====
UnityEngine.Debug:Log (object)
NetworkInventory:OnNetworkSpawn () (at Assets/Scripts/Inventory/NetworkInventory.cs:38)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,Unity.Netcode.NetworkObject/SceneObject&,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:778)
Unity.Netcode.NetworkObject:AddSceneObject (Unity.Netcode.NetworkObject/SceneObject&,Unity.Netcode.FastBufferReader,Unity.Netcode.NetworkManager) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:2005)
Unity.Netcode.ConnectionApprovedMessage:Handle (Unity.Netcode.NetworkContext&) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/Messages/ConnectionApprovedMessage.cs:166)
Unity.Netcode.NetworkMessageManager:ReceiveMessage<Unity.Netcode.ConnectionApprovedMessage> (Unity.Netcode.FastBufferReader,Unity.Netcode.NetworkContext&,Unity.Netcode.NetworkMessageManager) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:582)
Unity.Netcode.NetworkMessageManager:HandleMessage (Unity.Netcode.NetworkMessageHeader&,Unity.Netcode.FastBufferReader,ulong,single,int) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:446)
Unity.Netcode.NetworkMessageManager:ProcessIncomingMessageQueue () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:472)
Unity.Netcode.NetworkManager:NetworkUpdate (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:62)
Unity.Netcode.NetworkUpdateLoop:RunNetworkUpdateStage (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:192)
Unity.Netcode.NetworkUpdateLoop/NetworkEarlyUpdate/<>c:<CreateLoopSystem>b__0_0 () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:215)

[NetworkInventory] IsServer=False, IsClient=True, IsOwner=False, OwnerClientId=0
UnityEngine.Debug:Log (object)
NetworkInventory:OnNetworkSpawn () (at Assets/Scripts/Inventory/NetworkInventory.cs:39)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,Unity.Netcode.NetworkObject/SceneObject&,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:778)
Unity.Netcode.NetworkObject:AddSceneObject (Unity.Netcode.NetworkObject/SceneObject&,Unity.Netcode.FastBufferReader,Unity.Netcode.NetworkManager) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:2005)
Unity.Netcode.ConnectionApprovedMessage:Handle (Unity.Netcode.NetworkContext&) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/Messages/ConnectionApprovedMessage.cs:166)
Unity.Netcode.NetworkMessageManager:ReceiveMessage<Unity.Netcode.ConnectionApprovedMessage> (Unity.Netcode.FastBufferReader,Unity.Netcode.NetworkContext&,Unity.Netcode.NetworkMessageManager) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:582)
Unity.Netcode.NetworkMessageManager:HandleMessage (Unity.Netcode.NetworkMessageHeader&,Unity.Netcode.FastBufferReader,ulong,single,int) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:446)
Unity.Netcode.NetworkMessageManager:ProcessIncomingMessageQueue () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:472)
Unity.Netcode.NetworkManager:NetworkUpdate (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:62)
Unity.Netcode.NetworkUpdateLoop:RunNetworkUpdateStage (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:192)
Unity.Netcode.NetworkUpdateLoop/NetworkEarlyUpdate/<>c:<CreateLoopSystem>b__0_0 () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:215)

[NetworkInventory] Deserialized inventory on spawn. UsedSlots=0
UnityEngine.Debug:Log (object)
NetworkInventory:OnNetworkSpawn () (at Assets/Scripts/Inventory/NetworkInventory.cs:68)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,Unity.Netcode.NetworkObject/SceneObject&,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:778)
Unity.Netcode.NetworkObject:AddSceneObject (Unity.Netcode.NetworkObject/SceneObject&,Unity.Netcode.FastBufferReader,Unity.Netcode.NetworkManager) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:2005)
Unity.Netcode.ConnectionApprovedMessage:Handle (Unity.Netcode.NetworkContext&) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/Messages/ConnectionApprovedMessage.cs:166)
Unity.Netcode.NetworkMessageManager:ReceiveMessage<Unity.Netcode.ConnectionApprovedMessage> (Unity.Netcode.FastBufferReader,Unity.Netcode.NetworkContext&,Unity.Netcode.NetworkMessageManager) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:582)
Unity.Netcode.NetworkMessageManager:HandleMessage (Unity.Netcode.NetworkMessageHeader&,Unity.Netcode.FastBufferReader,ulong,single,int) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:446)
Unity.Netcode.NetworkMessageManager:ProcessIncomingMessageQueue () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:472)
Unity.Netcode.NetworkManager:NetworkUpdate (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:62)
Unity.Netcode.NetworkUpdateLoop:RunNetworkUpdateStage (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:192)
Unity.Netcode.NetworkUpdateLoop/NetworkEarlyUpdate/<>c:<CreateLoopSystem>b__0_0 () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:215)

[NetworkInventory] Client: Scheduling delayed OnInventoryChanged trigger...
UnityEngine.Debug:Log (object)
NetworkInventory:OnNetworkSpawn () (at Assets/Scripts/Inventory/NetworkInventory.cs:74)
Unity.Netcode.NetworkBehaviour:VisibleOnNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkBehaviour.cs:700)
Unity.Netcode.NetworkObject:InvokeBehaviourNetworkSpawn () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:1438)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocallyCommon (Unity.Netcode.NetworkObject,ulong,bool,bool,ulong,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:862)
Unity.Netcode.NetworkSpawnManager:SpawnNetworkObjectLocally (Unity.Netcode.NetworkObject,Unity.Netcode.NetworkObject/SceneObject&,bool) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Spawning/NetworkSpawnManager.cs:778)
Unity.Netcode.NetworkObject:AddSceneObject (Unity.Netcode.NetworkObject/SceneObject&,Unity.Netcode.FastBufferReader,Unity.Netcode.NetworkManager) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkObject.cs:2005)
Unity.Netcode.ConnectionApprovedMessage:Handle (Unity.Netcode.NetworkContext&) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/Messages/ConnectionApprovedMessage.cs:166)
Unity.Netcode.NetworkMessageManager:ReceiveMessage<Unity.Netcode.ConnectionApprovedMessage> (Unity.Netcode.FastBufferReader,Unity.Netcode.NetworkContext&,Unity.Netcode.NetworkMessageManager) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:582)
Unity.Netcode.NetworkMessageManager:HandleMessage (Unity.Netcode.NetworkMessageHeader&,Unity.Netcode.FastBufferReader,ulong,single,int) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:446)
Unity.Netcode.NetworkMessageManager:ProcessIncomingMessageQueue () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:472)
Unity.Netcode.NetworkManager:NetworkUpdate (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:62)
Unity.Netcode.NetworkUpdateLoop:RunNetworkUpdateStage (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:192)
Unity.Netcode.NetworkUpdateLoop/NetworkEarlyUpdate/<>c:<CreateLoopSystem>b__0_0 () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:215)

[NetworkManagerCustom] Client-side: Will send auth after player spawns for clientId 0...
UnityEngine.Debug:Log (object)
NetworkManagerCustom:OnClientConnected (ulong) (at Assets/Scripts/Network/Shared/NetworkManagerCustom.cs:238)
Unity.Netcode.NetworkConnectionManager:InvokeOnClientConnectedCallback (ulong) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Connection/NetworkConnectionManager.cs:128)
Unity.Netcode.ConnectionApprovedMessage:Handle (Unity.Netcode.NetworkContext&) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/Messages/ConnectionApprovedMessage.cs:172)
Unity.Netcode.NetworkMessageManager:ReceiveMessage<Unity.Netcode.ConnectionApprovedMessage> (Unity.Netcode.FastBufferReader,Unity.Netcode.NetworkContext&,Unity.Netcode.NetworkMessageManager) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:582)
Unity.Netcode.NetworkMessageManager:HandleMessage (Unity.Netcode.NetworkMessageHeader&,Unity.Netcode.FastBufferReader,ulong,single,int) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:446)
Unity.Netcode.NetworkMessageManager:ProcessIncomingMessageQueue () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Messaging/NetworkMessageManager.cs:472)
Unity.Netcode.NetworkManager:NetworkUpdate (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkManager.cs:62)
Unity.Netcode.NetworkUpdateLoop:RunNetworkUpdateStage (Unity.Netcode.NetworkUpdateStage) (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:192)
Unity.Netcode.NetworkUpdateLoop/NetworkEarlyUpdate/<>c:<CreateLoopSystem>b__0_0 () (at ./Library/PackageCache/com.unity.netcode.gameobjects@1.15.0/Runtime/Core/NetworkUpdateLoop.cs:215)

[NetworkPlayerHealth] No spawn points found, using current position: (0.40, -3.34, 0.00)
UnityEngine.Debug:LogWarning (object)
NetworkPlayerHealth:Start () (at Assets/Scripts/Combat/NetworkPlayerHealth.cs:118)

[ClientAuthSender] Update() Frame #1 - shouldSendAuth is FALSE (already sent or cleared)
UnityEngine.Debug:Log (object)
ClientAuthSender:Update () (at Assets/Scripts/Network/Client/ClientAuthSender.cs:179)

