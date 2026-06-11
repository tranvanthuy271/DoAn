using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Netcode;

// Force refresh GameScene to clear cached NetworkObject hashes
// Menu: Tools > Fix NetworkObject Hash Issues
public class ForceRefreshScene : Editor
{
    [MenuItem("Tools/Fix NetworkObject Hash Issues")]
    public static void FixNetworkObjectHashes()
    {
        // Save current scene
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            { /* Starting fix */ }
            
            // Get current scene path
            string currentScenePath = EditorSceneManager.GetActiveScene().path;
            
            if (string.IsNullOrEmpty(currentScenePath))
            {
                { /* Lỗi: No scene is currently open */ }
                return;
            }
            
            // Close current scene
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            
            // Force reimport scene
            AssetDatabase.ImportAsset(currentScenePath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();
            
            { /* Reimported: {currentScenePath} */ }
            
            // Reopen scene
            EditorSceneManager.OpenScene(currentScenePath);
            
            // Find and log all NetworkObjects
            NetworkObject[] networkObjects = FindObjectsOfType<NetworkObject>();
            { /* Found {networkObjects.Length} NetworkObject(s) in scene */ }
            
            foreach (var netObj in networkObjects)
            {
                SerializedObject so = new SerializedObject(netObj);
                SerializedProperty hashProp = so.FindProperty("GlobalObjectIdHash");
                uint hashValue = hashProp != null ? hashProp.uintValue : 0;
                { /* {netObj.gameObject.name} | Hash: {hashValue} */ }
            }
            
            { /* ✓ Scene refreshed! Check hashes above */ }
            { /* If any scene object has non-zero hash, select it and reset NetworkObject component */ }
        }
    }
    
    [MenuItem("Tools/Reset All Scene NetworkObject Hashes")]
    public static void ResetAllSceneObjectHashes()
    {
        if (!EditorUtility.DisplayDialog("Reset NetworkObject Hashes",
            "This will reset GlobalObjectIdHash to 0 for all scene-placed NetworkObjects. Continue?",
            "Yes", "Cancel"))
        {
            return;
        }
        
        NetworkObject[] networkObjects = FindObjectsOfType<NetworkObject>();
        int resetCount = 0;
        
        foreach (var netObj in networkObjects)
        {
            // Check if it's a scene object (not a prefab instance)
            if (PrefabUtility.GetPrefabInstanceStatus(netObj.gameObject) == PrefabInstanceStatus.NotAPrefab)
            {
                SerializedObject so = new SerializedObject(netObj);
                SerializedProperty hashProp = so.FindProperty("GlobalObjectIdHash");
                
                if (hashProp != null && hashProp.uintValue != 0)
                {
                    { /* Resetting hash for scene object: {netObj.gameObject.name} (was {hashProp.uintValue}) */ }
                    hashProp.uintValue = 0;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(netObj);
                    resetCount++;
                }
            }
        }
        
        if (resetCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            { /* ✓ Reset {resetCount} NetworkObject hash(es). Scene saved */ }
        }
        else
        {
            { /* No scene objects with non-zero hash found */ }
        }
    }
}
