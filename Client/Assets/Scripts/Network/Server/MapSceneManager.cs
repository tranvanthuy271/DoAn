using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// Server-side: each map gets its own Physics2D scene to isolate cross-map queries.
// Static ground colliders from loaded client scenes are mirrored into those scenes so
// server-authoritative enemies can use gravity and land on platforms.
[DisallowMultipleComponent]
public class MapSceneManager : MonoBehaviour
{
    public static MapSceneManager Instance { get; private set; }

    private readonly Dictionary<int, Scene> _mapScenes = new();
    private readonly Dictionary<int, string> _mapSceneNames = new();
    private readonly Dictionary<int, GameObject> _groundProxyRoots = new();
    private MapWorldConfig _config;
    private ServerGroundColliderDatabase _groundDatabase;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        SceneManager.sceneLoaded -= OnSceneLoaded;

        foreach (var proxyRoot in _groundProxyRoots.Values)
        {
            if (proxyRoot != null)
                Destroy(proxyRoot);
        }
        _groundProxyRoots.Clear();

        foreach (var kvp in _mapScenes)
        {
            if (kvp.Value.IsValid())
                SceneManager.UnloadSceneAsync(kvp.Value);
        }

        _mapScenes.Clear();
        _mapSceneNames.Clear();
    }

    // Must run once during server boot before any enemy/NPC/player is spawned.
    // Creates one local Physics2D scene for each map id.
    public void Initialize(MapWorldConfig config)
    {
        if (config?.maps == null)
        {
            { /* Lỗi: MapWorldConfig is null or has no maps */ }
            return;
        }

        _config = config;
        _groundDatabase = Resources.Load<ServerGroundColliderDatabase>(ServerGroundColliderDatabase.ResourcesPath);
        _mapSceneNames.Clear();

        foreach (var mapDef in config.maps)
        {
            if (_mapScenes.ContainsKey(mapDef.mapId))
                continue;

            var scene = SceneManager.CreateScene(
                $"ServerMap_{mapDef.mapId}",
                new CreateSceneParameters(LocalPhysicsMode.Physics2D));

            _mapScenes[mapDef.mapId] = scene;
            _mapSceneNames[mapDef.mapId] = mapDef.sceneName;

            { /* Created physics scene for map {mapDef.mapId} ({mapDef.mapName ?? */ }
        }

        BuildGroundProxiesFromDatabase();
        RebuildGroundProxiesForLoadedScenes();

        // Log layer collision matrix Enemy vs Ground \u0111\u1ec3 ch\u1ea9n \u0111o\u00e1n
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (enemyLayer >= 0 && groundLayer >= 0)
        {
            bool ignored = Physics2D.GetIgnoreLayerCollision(enemyLayer, groundLayer);
            { /* Layer collision Enemy({enemyLayer}) vs Ground({groundLayer}) = {(ignored ? */ }
        }
        else
        {
            { /* Lỗi: Layer m\u1ea5t: Enemy={enemyLayer} Ground={groundLayer} */ }
        }

        { /* Ready with {_mapScenes.Count} map physics scene(s). proxiesBuilt={_groundProxyRoots.Count} */ }
    }

    // Move a GameObject into the target map physics scene before NetworkObject.Spawn().
    public void MoveToMapScene(GameObject obj, int mapId)
    {
        if (obj == null)
            return;

        if (_mapScenes.TryGetValue(mapId, out Scene scene) && scene.IsValid())
        {
            ConfigureNetworkObjectForServerOnlyScene(obj.GetComponent<NetworkObject>());

            bool hasGroundProxy = _groundProxyRoots.TryGetValue(mapId, out var proxyRoot) && proxyRoot != null;
            int proxyChildCount = hasGroundProxy ? proxyRoot.transform.childCount : 0;

            if (obj.scene == scene)
            {
                { /* [MoveToMapScene] obj='{obj.name}' đã ở physicsScene='{scene.name}' (mapId={mapId}). groundProxy={hasGroundProxy} children={proxyChildCount} */ }
                return;
            }

            string oldSceneName = obj.scene.IsValid() ? obj.scene.name : "<invalid>";
            SceneManager.MoveGameObjectToScene(obj, scene);
            { /* [MoveToMapScene] obj='{obj.name}' từ '{oldSceneName}' → '{scene.name}' (mapId={mapId}). groundProxy={hasGroundProxy} children={proxyChildCount} */ }

            if (!hasGroundProxy || proxyChildCount == 0)
            {
                { /* Lỗi: [MoveToMapScene] mapId={mapId} KHÔNG CÓ GROUND PROXY → boss/enemy sẽ rơi mãi mãi! Hãy chạy Tools/DoAn/Bake Server Ground Colliders */ }
            }
        }
        else
        {
            { /* Cảnh báo: Missing physics scene for map {mapId}. Object stays in main scene. (knownMaps=[{string.Join( */ }
        }
    }

    public bool HasScene(int mapId) =>
        _mapScenes.TryGetValue(mapId, out Scene scene) && scene.IsValid();

    public int SceneCount => _mapScenes.Count;

    // Local physics scenes do not exist on clients. Disable NGO scene migration sync.
    public static void ConfigureNetworkObjectForServerOnlyScene(NetworkObject networkObject)
    {
        if (networkObject == null)
            return;

        networkObject.ActiveSceneSynchronization = false;
        networkObject.SceneMigrationSynchronization = false;
    }

    private void FixedUpdate()
    {
        float deltaTime = Time.fixedDeltaTime;
        foreach (var kvp in _mapScenes)
        {
            if (!kvp.Value.IsValid())
                continue;

            kvp.Value.GetPhysicsScene2D().Simulate(deltaTime);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_config == null || !scene.IsValid() || !scene.isLoaded)
            return;

        foreach (var kvp in _mapSceneNames)
        {
            if (!string.Equals(kvp.Value, scene.name, System.StringComparison.Ordinal))
                continue;

            BuildGroundProxyForMap(kvp.Key, scene);
        }
    }

    private void RebuildGroundProxiesForLoadedScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene loadedScene = SceneManager.GetSceneAt(i);
            if (!loadedScene.IsValid() || !loadedScene.isLoaded)
                continue;

            foreach (var kvp in _mapSceneNames)
            {
                if (!string.Equals(kvp.Value, loadedScene.name, System.StringComparison.Ordinal))
                    continue;

                BuildGroundProxyForMap(kvp.Key, loadedScene);
            }
        }
    }

    private void BuildGroundProxiesFromDatabase()
    {
        if (_groundDatabase == null)
        {
            { /* Cảnh báo: ServerGroundColliderDatabase not found */ }
            return;
        }

        foreach (var kvp in _mapScenes)
        {
            if (!_groundDatabase.TryGetMap(kvp.Key, out ServerGroundColliderDatabase.MapGroundData mapData))
            {
                { /* Cảnh báo: Ground database has no data for map {kvp.Key} */ }
                continue;
            }

            BuildGroundProxyForMap(kvp.Key, mapData);
        }
    }

    private void BuildGroundProxyForMap(int mapId, ServerGroundColliderDatabase.MapGroundData mapData)
    {
        if (mapData?.colliders == null)
            return;

        if (!_mapScenes.TryGetValue(mapId, out Scene targetScene) || !targetScene.IsValid())
            return;

        DestroyExistingGroundProxy(mapId);

        GameObject root = new GameObject($"__ServerGroundProxy_map{mapId}");
        root.hideFlags = HideFlags.HideAndDontSave;
        SceneManager.MoveGameObjectToScene(root, targetScene);
        _groundProxyRoots[mapId] = root;

        int oneWayCount = 0;
        foreach (var colliderData in mapData.colliders)
        {
            CloneGroundCollider(root.transform, colliderData);
            if (colliderData.hasPlatformEffector && colliderData.useOneWay)
                oneWayCount++;
        }

        // Log full collider list để debug vị trí va chạm
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[MapSceneManager][BuildGroundProxy] map={mapId} scene='{mapData.sceneName}' colliders={mapData.colliders.Length} oneWay={oneWayCount}");
        for (int i = 0; i < mapData.colliders.Length; i++)
        {
            var c = mapData.colliders[i];
            // Tính vị trí center thực tế của collider trong world (position + offset*scale)
            float worldCenterX = c.position.x + c.offset.x * c.scale.x;
            float worldCenterY = c.position.y + c.offset.y * c.scale.y;
            float halfH = (c.size.y * c.scale.y) * 0.5f;
            sb.AppendLine($"  [{i}] {c.name} pos=({c.position.x:F2},{c.position.y:F2}) scale=({c.scale.x:F2},{c.scale.y:F2}) offset=({c.offset.x:F2},{c.offset.y:F2}) size=({c.size.x:F2},{c.size.y:F2}) → worldCenter=({worldCenterX:F2},{worldCenterY:F2}) topY={worldCenterY + halfH:F2} bottomY={worldCenterY - halfH:F2} oneWay={c.useOneWay} trigger={c.isTrigger}");
        }
        { /* Ghi nhận: sb.ToString() */ }
    }

    private void BuildGroundProxyForMap(int mapId, Scene sourceScene)
    {
        if (!_mapScenes.TryGetValue(mapId, out Scene targetScene) || !targetScene.IsValid())
            return;

        int groundLayer = LayerMask.NameToLayer("Ground");
        int maxMapLayer = LayerMask.NameToLayer("MaxMap");
        if (groundLayer < 0)
        {
            { /* Cảnh báo: Layer 'Ground' not found. Skipping ground proxy build */ }
            return;
        }

        DestroyExistingGroundProxy(mapId);

        GameObject root = new GameObject($"__ServerGroundProxy_map{mapId}");
        root.hideFlags = HideFlags.HideAndDontSave;
        SceneManager.MoveGameObjectToScene(root, targetScene);
        _groundProxyRoots[mapId] = root;

        int clonedCount = 0;
        foreach (GameObject sourceRoot in sourceScene.GetRootGameObjects())
        {
            BoxCollider2D[] groundColliders = sourceRoot.GetComponentsInChildren<BoxCollider2D>(true);
            foreach (BoxCollider2D sourceCollider in groundColliders)
            {
                if (sourceCollider == null
                    || !sourceCollider.enabled
                    || sourceCollider.isTrigger
                    || !IsServerObstacleLayer(sourceCollider.gameObject.layer, groundLayer, maxMapLayer))
                    continue;

                CloneGroundCollider(root.transform, sourceCollider);
                clonedCount++;
            }
        }

        if (clonedCount > 0)
        {
            { /* Built {clonedCount} server obstacle proxy collider(s) for map {mapId} from scene '{sourceScene.name}' */ }
        }
        else
        {
            { /* Cảnh báo: No Ground/MaxMap BoxCollider2D found in scene '{sourceScene.name}' for map {mapId} */ }
        }
    }

    private void DestroyExistingGroundProxy(int mapId)
    {
        if (!_groundProxyRoots.TryGetValue(mapId, out GameObject existingRoot) || existingRoot == null)
            return;

        existingRoot.SetActive(false);
        Destroy(existingRoot);
        _groundProxyRoots.Remove(mapId);
    }

    private static void CloneGroundCollider(Transform parent, BoxCollider2D sourceCollider)
    {
        GameObject proxy = new GameObject($"__GroundProxy_{sourceCollider.gameObject.name}");
        proxy.layer = sourceCollider.gameObject.layer;
        proxy.tag = sourceCollider.gameObject.tag;
        proxy.hideFlags = HideFlags.HideAndDontSave;

        Transform proxyTransform = proxy.transform;
        proxyTransform.SetParent(parent, false);
        proxyTransform.position = sourceCollider.transform.position;
        proxyTransform.rotation = sourceCollider.transform.rotation;
        proxyTransform.localScale = sourceCollider.transform.lossyScale;

        // Server proxies are always solid — no one-way effector needed.
        BoxCollider2D proxyCollider = proxy.AddComponent<BoxCollider2D>();
        proxyCollider.enabled = sourceCollider.enabled;
        proxyCollider.isTrigger = false;
        proxyCollider.offset = sourceCollider.offset;
        proxyCollider.size = sourceCollider.size;
        proxyCollider.edgeRadius = sourceCollider.edgeRadius;
        proxyCollider.sharedMaterial = sourceCollider.sharedMaterial;
        proxyCollider.usedByEffector = false;
    }

    private static void CloneGroundCollider(
        Transform parent,
        ServerGroundColliderDatabase.GroundColliderData colliderData)
    {
        GameObject proxy = new GameObject($"__GroundProxy_{colliderData.name}");
        proxy.layer = ResolveProxyLayer(colliderData);
        proxy.tag = proxy.layer == LayerMask.NameToLayer("Ground") ? "Ground" : "Untagged";
        proxy.hideFlags = HideFlags.HideAndDontSave;

        Transform proxyTransform = proxy.transform;
        proxyTransform.SetParent(parent, false);
        proxyTransform.position = colliderData.position;
        proxyTransform.rotation = Quaternion.Euler(0f, 0f, colliderData.rotationZ);
        proxyTransform.localScale = colliderData.scale;

        // Server proxies are always solid — no one-way effector needed.
        // One-way platforms would require enemies to approach from the correct
        // side, which causes pass-through issues in local physics scenes.
        BoxCollider2D proxyCollider = proxy.AddComponent<BoxCollider2D>();
        proxyCollider.isTrigger = false;
        proxyCollider.offset = colliderData.offset;
        proxyCollider.size = colliderData.size;
        proxyCollider.edgeRadius = colliderData.edgeRadius;
        proxyCollider.usedByEffector = false;
        { /* Created solid proxy '{colliderData.name}' layer={LayerMask.LayerToName(proxy.layer)} in scene '{parent.gameObject.scene.name}' pos={proxyTransform.position} scale={proxyTransform.localScale} offset={proxyCollider.offset} size={proxyCollider.size} */ }
    }

    private static bool IsServerObstacleLayer(int layer, int groundLayer, int maxMapLayer)
    {
        return layer == groundLayer || (maxMapLayer >= 0 && layer == maxMapLayer);
    }

    private static int ResolveProxyLayer(ServerGroundColliderDatabase.GroundColliderData colliderData)
    {
        string layerName = string.IsNullOrWhiteSpace(colliderData.layerName) ? "Ground" : colliderData.layerName;
        int layer = LayerMask.NameToLayer(layerName);
        if (layer >= 0)
            return layer;

        int groundLayer = LayerMask.NameToLayer("Ground");
        return groundLayer >= 0 ? groundLayer : 0;
    }
}
