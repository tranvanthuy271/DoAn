#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ServerGroundColliderDatabaseBaker
{
    private const string MapWorldConfigPath = "ScriptableObjects/MapWorldConfig";
    private const string OutputAssetPath = "Assets/Resources/ScriptableObjects/ServerGroundColliderDatabase.asset";

    [MenuItem("Tools/DoAn/Bake Server Ground Colliders")]
    public static void Bake()
    {
        MapWorldConfig mapWorldConfig = Resources.Load<MapWorldConfig>(MapWorldConfigPath);
        if (mapWorldConfig == null || mapWorldConfig.maps == null)
        {
            Debug.LogError("[ServerGroundColliderDatabaseBaker] MapWorldConfig not found in Resources/ScriptableObjects.");
            return;
        }

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0)
        {
            Debug.LogError("[ServerGroundColliderDatabaseBaker] Layer 'Ground' not found.");
            return;
        }

        var bakedMaps = new List<ServerGroundColliderDatabase.MapGroundData>();
        var bakedMapIds = new HashSet<int>();

        foreach (var mapDef in mapWorldConfig.maps)
        {
            if (mapDef == null || bakedMapIds.Contains(mapDef.mapId))
                continue;

            bakedMapIds.Add(mapDef.mapId);
            ServerGroundColliderDatabase.MapGroundData bakedMap = BakeMap(mapDef.mapId, mapDef.sceneName, groundLayer);
            if (bakedMap != null)
                bakedMaps.Add(bakedMap);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(OutputAssetPath));

        ServerGroundColliderDatabase database = AssetDatabase.LoadAssetAtPath<ServerGroundColliderDatabase>(OutputAssetPath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<ServerGroundColliderDatabase>();
            AssetDatabase.CreateAsset(database, OutputAssetPath);
        }

        database.maps = bakedMaps.ToArray();
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[ServerGroundColliderDatabaseBaker] Baked {bakedMaps.Count} map(s) into {OutputAssetPath}.");
    }

    private static ServerGroundColliderDatabase.MapGroundData BakeMap(int mapId, string sceneName, int groundLayer)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning($"[ServerGroundColliderDatabaseBaker] mapId={mapId} has empty sceneName.");
            return null;
        }

        string scenePath = ResolveScenePath(sceneName);
        if (string.IsNullOrEmpty(scenePath))
        {
            Debug.LogWarning($"[ServerGroundColliderDatabaseBaker] Cannot resolve scene '{sceneName}' for mapId={mapId}.");
            return null;
        }

        Scene scene = SceneManager.GetSceneByPath(scenePath);
        bool closeAfterBake = false;
        if (!scene.IsValid() || !scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            closeAfterBake = true;
        }

        var colliders = new List<ServerGroundColliderDatabase.GroundColliderData>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            BoxCollider2D[] sourceColliders = root.GetComponentsInChildren<BoxCollider2D>(true);
            foreach (BoxCollider2D sourceCollider in sourceColliders)
            {
                if (sourceCollider == null || sourceCollider.gameObject.layer != groundLayer)
                    continue;

                colliders.Add(ToGroundColliderData(sourceCollider));
            }
        }

        if (closeAfterBake)
            EditorSceneManager.CloseScene(scene, true);

        Debug.Log($"[ServerGroundColliderDatabaseBaker] mapId={mapId} scene='{sceneName}' colliders={colliders.Count}.");
        return new ServerGroundColliderDatabase.MapGroundData
        {
            mapId = mapId,
            sceneName = sceneName,
            colliders = colliders.ToArray()
        };
    }

    private static ServerGroundColliderDatabase.GroundColliderData ToGroundColliderData(BoxCollider2D sourceCollider)
    {
        PlatformEffector2D effector = sourceCollider.GetComponent<PlatformEffector2D>();
        return new ServerGroundColliderDatabase.GroundColliderData
        {
            name = sourceCollider.gameObject.name,
            position = sourceCollider.transform.position,
            rotationZ = sourceCollider.transform.eulerAngles.z,
            scale = sourceCollider.transform.lossyScale,
            offset = sourceCollider.offset,
            size = sourceCollider.size,
            edgeRadius = sourceCollider.edgeRadius,
            isTrigger = sourceCollider.isTrigger,
            usedByEffector = sourceCollider.usedByEffector,
            hasPlatformEffector = effector != null,
            useOneWay = effector != null && effector.useOneWay,
            useOneWayGrouping = effector != null && effector.useOneWayGrouping,
            surfaceArc = effector != null ? effector.surfaceArc : 180f,
            sideArc = effector != null ? effector.sideArc : 0f,
            rotationalOffset = effector != null ? effector.rotationalOffset : 0f,
            useSideFriction = effector != null && effector.useSideFriction,
            useSideBounce = effector != null && effector.useSideBounce
        };
    }

    private static string ResolveScenePath(string sceneName)
    {
        foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
        {
            if (string.IsNullOrWhiteSpace(buildScene.path))
                continue;

            if (Path.GetFileNameWithoutExtension(buildScene.path) == sceneName)
                return buildScene.path;
        }

        string[] guids = AssetDatabase.FindAssets($"{sceneName} t:Scene");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == sceneName)
                return path;
        }

        return null;
    }
}
#endif
