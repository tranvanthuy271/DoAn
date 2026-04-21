using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Server-side: mỗi map có một Physics2D scene riêng biệt.
/// Đảm bảo objects ở map khác nhau KHÔNG bao giờ trigger lẫn nhau
/// (OnTriggerEnter2D, OverlapCircle... chỉ hoạt động trong cùng scene).
///
/// SETUP:
///   Gắn vào "ServerBootstrap" GameObject cùng vị trí MapWorldBootstrap.
///   MapWorldBootstrap.StartServerRoutine() gọi Initialize(_config) sau registry.Initialize().
///
/// DÙNG:
///   MapSceneManager.Instance.MoveToMapScene(gameObject, mapId);
///   → Gọi TRƯỚC NetworkObject.Spawn() để đảm bảo đúng physics world từ frame đầu.
///
/// GHI CHÚ:
///   Local Physics2D scenes KHÔNG tự simulate — FixedUpdate() gọi thủ công ở đây.
///   Physics.autoSimulation phải = false nếu dùng LocalPhysicsMode (tự động khi CreateScene với LocalPhysicsMode).
/// </summary>
[DisallowMultipleComponent]
public class MapSceneManager : MonoBehaviour
{
    public static MapSceneManager Instance { get; private set; }

    // mapId → scene riêng với LocalPhysicsMode.Physics2D
    private readonly Dictionary<int, Scene> _mapScenes = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        foreach (var kvp in _mapScenes)
        {
            if (kvp.Value.IsValid())
                SceneManager.UnloadSceneAsync(kvp.Value);
        }
        _mapScenes.Clear();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi một lần khi server boot TRƯỚC khi spawn bất kỳ enemy/NPC/player nào.
    /// Tạo 1 scene Physics2D riêng cho mỗi map định nghĩa trong config.
    /// </summary>
    public void Initialize(MapWorldConfig config)
    {
        if (config?.maps == null)
        {
            Debug.LogError("[MapSceneManager] MapWorldConfig null hoặc không có maps — bỏ qua init.");
            return;
        }

        foreach (var mapDef in config.maps)
        {
            if (_mapScenes.ContainsKey(mapDef.mapId))
                continue;

            var scene = SceneManager.CreateScene(
                $"ServerMap_{mapDef.mapId}",
                new CreateSceneParameters(LocalPhysicsMode.Physics2D));

            _mapScenes[mapDef.mapId] = scene;
            Debug.Log($"[MapSceneManager] ✓ Created physics scene for map {mapDef.mapId} " +
                      $"({mapDef.mapName ?? "unnamed"})");
        }

        Debug.Log($"[MapSceneManager] ✓ {_mapScenes.Count} map physics scene(s) ready.");
    }

    /// <summary>
    /// Di chuyển GameObject vào scene Physics2D của map tương ứng.
    /// GỌI TRƯỚC NetworkObject.Spawn() để đảm bảo đúng physics world từ frame đầu.
    /// An toàn khi gọi nhiều lần — bỏ qua nếu scene không tồn tại hoặc obj null.
    /// </summary>
    public void MoveToMapScene(GameObject obj, int mapId)
    {
        if (obj == null) return;

        if (_mapScenes.TryGetValue(mapId, out Scene scene) && scene.IsValid())
        {
            ConfigureNetworkObjectForServerOnlyScene(obj.GetComponent<NetworkObject>());

            if (obj.scene == scene)
                return;

            SceneManager.MoveGameObjectToScene(obj, scene);
        }
        else
        {
            Debug.LogWarning($"[MapSceneManager] Scene cho map {mapId} chưa được tạo. " +
                             "Kiểm tra Initialize() đã được gọi trước khi spawn objects. " +
                             "Object sẽ ở main scene — có thể xảy ra cross-map collision!");
        }
    }

    /// <summary>Kiểm tra scene cho map đã được tạo chưa.</summary>
    public bool HasScene(int mapId) =>
        _mapScenes.TryGetValue(mapId, out Scene s) && s.IsValid();

    /// <summary>
    /// Trả về số lượng scenes đang quản lý (dùng cho debug/testing).
    /// </summary>
    public int SceneCount => _mapScenes.Count;

    /// <summary>
    /// Server-only physics scenes không tồn tại trên client.
    /// Tắt NGO scene sync để MoveGameObjectToScene không phát sinh SceneMigration sang client.
    /// </summary>
    public static void ConfigureNetworkObjectForServerOnlyScene(NetworkObject networkObject)
    {
        if (networkObject == null)
            return;

        networkObject.ActiveSceneSynchronization = false;
        networkObject.SceneMigrationSynchronization = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Physics Simulation
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Local Physics2D scenes KHÔNG tự auto-simulate khi dùng LocalPhysicsMode.
    /// FixedUpdate thủ công simulate từng scene — PHẢI có để trigger/collision hoạt động.
    /// </summary>
    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        foreach (var kvp in _mapScenes)
        {
            if (!kvp.Value.IsValid()) continue;
            kvp.Value.GetPhysicsScene2D().Simulate(dt);
        }
    }
}
