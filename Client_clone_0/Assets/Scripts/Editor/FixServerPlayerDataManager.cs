using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Netcode;

// Remove and recreate ServerPlayerDataManager to fix hash issue
// Menu: Tools > Fix ServerPlayerDataManager Hash
public class FixServerPlayerDataManager : Editor
{
    [MenuItem("Tools/Fix ServerPlayerDataManager Hash")]
    public static void FixHash()
    {
        // Find ServerPlayerDataManager
        GameObject serverManager = GameObject.Find("ServerPlayerDataManager");
        
        if (serverManager == null)
        {
            { /* Lỗi: ServerPlayerDataManager not found in scene */ }
            return;
        }
        
        { /* Found ServerPlayerDataManager, removing */ }
        
        // Get components before deleting
        NetworkObject netObj = serverManager.GetComponent<NetworkObject>();
        
        if (netObj == null)
        {
            { /* Lỗi: No NetworkObject component found */ }
            return;
        }
        
        // Find ServerPlayerDataManager script component
        Component serverPlayerDataScript = null;
        var allComponents = serverManager.GetComponents<Component>();
        foreach (var comp in allComponents)
        {
            if (comp != null && comp.GetType().Name == "ServerPlayerDataManager")
            {
                serverPlayerDataScript = comp;
                break;
            }
        }
        
        // Get NetworkObject settings
        bool alwaysReplicateAsRoot = false;
        bool synchronizeTransform = true;
        bool dontDestroyWithOwner = false;
        
        SerializedObject so = new SerializedObject(netObj);
        SerializedProperty prop;
        
        prop = so.FindProperty("AlwaysReplicateAsRoot");
        if (prop != null) alwaysReplicateAsRoot = prop.boolValue;
        
        prop = so.FindProperty("SynchronizeTransform");
        if (prop != null) synchronizeTransform = prop.boolValue;
        
        prop = so.FindProperty("DontDestroyWithOwner");
        if (prop != null) dontDestroyWithOwner = prop.boolValue;
        
        // Store transform info
        Transform parent = serverManager.transform.parent;
        Vector3 pos = serverManager.transform.localPosition;
        Quaternion rot = serverManager.transform.localRotation;
        Vector3 scale = serverManager.transform.localScale;
        
        // Delete old object
        DestroyImmediate(serverManager);
        
        { /* Creating new ServerPlayerDataManager */ }
        
        // Create new GameObject
        GameObject newServerManager = new GameObject("ServerPlayerDataManager");
        newServerManager.transform.SetParent(parent);
        newServerManager.transform.localPosition = pos;
        newServerManager.transform.localRotation = rot;
        newServerManager.transform.localScale = scale;
        
        // Add NetworkObject component
        NetworkObject newNetObj = newServerManager.AddComponent<NetworkObject>();
        
        // Configure NetworkObject (hash should auto-generate as 0 for scene objects)
        SerializedObject newSo = new SerializedObject(newNetObj);
        
        // Force hash to 0
        SerializedProperty hashProp = newSo.FindProperty("GlobalObjectIdHash");
        if (hashProp != null)
        {
            hashProp.uintValue = 0;
        }
        
        // Set other properties
        prop = newSo.FindProperty("AlwaysReplicateAsRoot");
        if (prop != null) prop.boolValue = alwaysReplicateAsRoot;
        
        prop = newSo.FindProperty("SynchronizeTransform");
        if (prop != null) prop.boolValue = synchronizeTransform;
        
        prop = newSo.FindProperty("DontDestroyWithOwner");
        if (prop != null) prop.boolValue = dontDestroyWithOwner;
        
        prop = newSo.FindProperty("ActiveSceneSynchronization");
        if (prop != null) prop.boolValue = false;
        
        prop = newSo.FindProperty("SceneMigrationSynchronization");
        if (prop != null) prop.boolValue = true;
        
        prop = newSo.FindProperty("SpawnWithObservers");
        if (prop != null) prop.boolValue = true;
        
        newSo.ApplyModifiedProperties();
        
        // Add ServerPlayerDataManager script if it was found
        if (serverPlayerDataScript != null)
        {
            var scriptType = serverPlayerDataScript.GetType();
            newServerManager.AddComponent(scriptType);
            { /* Added {scriptType.Name} component */ }
        }
        else
        {
            { /* Cảnh báo: ServerPlayerDataManager script component not found, skipping */ }
        }
        
        // Mark scene dirty and save
        EditorUtility.SetDirty(newServerManager);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        
        // Verify hash
        newSo = new SerializedObject(newNetObj);
        hashProp = newSo.FindProperty("GlobalObjectIdHash");
        uint finalHash = hashProp != null ? hashProp.uintValue : 0;
        
        { /* ✓ Done! New hash: {finalHash} */ }
        
        if (finalHash != 0)
        {
            { /* Cảnh báo: Hash is still {finalHash}! This may require closing and reopening Unity */ }
        }
        else
        {
            { /* ✓ Hash is 0! Scene saved */ }
        }
        
        // Select the new object
        Selection.activeGameObject = newServerManager;
    }
}
