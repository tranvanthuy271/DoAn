#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

// Batch mode entry point: mở GameScene rồi chạy SetupEquipmentCharacterPreview.
// Dùng: Unity.exe -batchmode -projectPath ... -executeMethod BatchSetupPreview.Run -quit
public static class BatchSetupPreview
{
    public static void Run()
    {
        UnityEngine.Debug.Log("[BatchSetup] Mở GameScene...");

        var scene = EditorSceneManager.OpenScene(
            "Assets/Scenes/GameScene.unity",
            OpenSceneMode.Single);

        UnityEngine.Debug.Log("[BatchSetup] Chạy SetupEquipmentCharacterPreview...");
        SetupEquipmentCharacterPreview.Run();

        EditorSceneManager.SaveScene(scene);
        UnityEngine.Debug.Log("[BatchSetup] Scene đã lưu.");
    }
}
#endif
