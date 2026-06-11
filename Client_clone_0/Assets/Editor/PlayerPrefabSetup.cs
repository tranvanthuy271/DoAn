#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Editor tool – tự động thêm component DebuffSpriteTint và OverheadStatusDisplay
// vào tất cả 12 player prefabs (He + Fusion).
// Menu: GameTools → Skill Effects → Setup All Player Prefabs
// Điều kiện:
// • StatusIconEntry.prefab phải tồn tại tại Assets/Resources/Prefabs/UI/
// (tạo bằng GameTools → Skill Effects → Create Status Icon Prefab).
// • Các prefab phải có child tên "PlayerHpBarCanvas".
// Kết quả:
// • DebuffSpriteTint thêm vào root của mỗi prefab (cùng chỗ với SpriteRenderer).
// • OverheadStatusDisplay thêm vào child "PlayerHpBarCanvas".
// - Gán statusIconPrefab = StatusIconEntry.prefab
// - Tạo child "StatusIconContainer" (HorizontalLayoutGroup) làm iconContainer
public static class PlayerPrefabSetup
{
    private static readonly string[] PLAYER_PREFAB_PATHS = new[]
    {
        // Hệ (He)
        "Assets/Prefabs/Player/He/Hoa.prefab",
        "Assets/Prefabs/Player/He/Kim.prefab",
        "Assets/Prefabs/Player/He/Moc.prefab",
        "Assets/Prefabs/Player/He/Phong.prefab",
        "Assets/Prefabs/Player/He/Tho.prefab",
        "Assets/Prefabs/Player/He/Thuy.prefab",
        // Fusion
        "Assets/Prefabs/Player/Fusion/F_Hoa.prefab",
        "Assets/Prefabs/Player/Fusion/F_Kim.prefab",
        "Assets/Prefabs/Player/Fusion/F_Moc.prefab",
        "Assets/Prefabs/Player/Fusion/F_Phong.prefab",
        "Assets/Prefabs/Player/Fusion/F_Tho.prefab",
        "Assets/Prefabs/Player/Fusion/F_Thuy.prefab",
    };

    private const string STATUS_ICON_PREFAB_PATH = "Assets/Resources/Prefabs/UI/StatusIconEntry.prefab";

    [MenuItem("GameTools/Skill Effects/Setup All Player Prefabs")]
    public static void SetupAllPlayerPrefabs()
    {
        // Kiểm tra StatusIconEntry prefab
        var statusIconPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(STATUS_ICON_PREFAB_PATH);
        StatusIconEntry statusIconEntry = null;
        if (statusIconPrefab != null)
            statusIconEntry = statusIconPrefab.GetComponent<StatusIconEntry>();

        if (statusIconEntry == null)
        {
            bool cont = EditorUtility.DisplayDialog(
                "Thiếu StatusIconEntry",
                $"Không tìm thấy '{STATUS_ICON_PREFAB_PATH}'.\n" +
                "Chạy GameTools → Skill Effects → Create Status Icon Prefab trước.\n\n" +
                "Bấm OK để tiếp tục mà không gán statusIconPrefab " +
                "(sẽ phải gán thủ công trong Inspector sau).",
                "OK – Tiếp tục", "Huỷ");
            if (!cont) return;
        }

        int done    = 0;
        int skipped = 0;

        foreach (string path in PLAYER_PREFAB_PATHS)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null)
            {
                { /* Cảnh báo: ✗ Không tìm thấy prefab: {path} */ }
                skipped++;
                continue;
            }

            using var scope = new PrefabUtility.EditPrefabContentsScope(path);
            var prefabRoot = scope.prefabContentsRoot;

            bool changed = false;

            // 1. DebuffSpriteTint trên root (cùng chỗ SpriteRenderer)
            var sr = prefabRoot.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (prefabRoot.GetComponent<DebuffSpriteTint>() == null)
                {
                    prefabRoot.AddComponent<DebuffSpriteTint>();
                    { /* + DebuffSpriteTint → {path} */ }
                    changed = true;
                }
            }
            else
            {
                // Tìm SpriteRenderer ở child đầu tiên không phải SkillEffect
                foreach (var candSr in prefabRoot.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (candSr.gameObject.name == "SkillEffect") continue;
                    if (candSr.GetComponent<DebuffSpriteTint>() == null)
                    {
                        candSr.gameObject.AddComponent<DebuffSpriteTint>();
                        { /* + DebuffSpriteTint (child '{candSr.gameObject.name}') → {path} */ }
                        changed = true;
                    }
                    break;
                }
            }

            // 2. OverheadStatusDisplay trong PlayerHpBarCanvas
            var hpBarCanvas = FindChildByName(prefabRoot.transform, "PlayerHpBarCanvas");
            if (hpBarCanvas == null)
            {
                { /* Cảnh báo: ✗ Không tìm thấy 'PlayerHpBarCanvas' trong {path}. Bỏ qua OverheadStatusDisplay */ }
            }
            else
            {
                var existing = hpBarCanvas.GetComponent<OverheadStatusDisplay>();
                if (existing == null)
                {
                    var osd = hpBarCanvas.gameObject.AddComponent<OverheadStatusDisplay>();

                    // Tạo child "StatusIconContainer" với HorizontalLayoutGroup
                    var containerGo = new GameObject("StatusIconContainer");
                    containerGo.transform.SetParent(hpBarCanvas, false);
                    var rt = containerGo.AddComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(0f, 4f);   // hơi phía trên HP bar
                    rt.sizeDelta        = Vector2.zero;

                    var hlg = containerGo.AddComponent<HorizontalLayoutGroup>();
                    hlg.spacing              = 2f;
                    hlg.childForceExpandWidth  = false;
                    hlg.childForceExpandHeight = false;
                    hlg.childAlignment       = TextAnchor.MiddleCenter;

                    containerGo.AddComponent<ContentSizeFitter>().horizontalFit =
                        ContentSizeFitter.FitMode.PreferredSize;

                    // Gán refs qua SerializedObject
                    var so = new SerializedObject(osd);
                    so.FindProperty("statusIconPrefab").objectReferenceValue =
                        statusIconEntry != null ? (UnityEngine.Object)statusIconEntry : null;
                    so.FindProperty("iconContainer").objectReferenceValue = rt;
                    so.ApplyModifiedProperties();

                    { /* + OverheadStatusDisplay + StatusIconContainer → {path} */ }
                    changed = true;
                }
            }

            if (changed) done++;
            else         skipped++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string msg = $"Hoàn tất: {done} prefab đã cập nhật, {skipped} không cần thay đổi.";
        { /* {msg} */ }
        EditorUtility.DisplayDialog("Setup Player Prefabs", msg, "OK");
    }

    // Hàm hỗ trợ dùng nội bộ để tách nhỏ xử lý chính.

    private static Transform FindChildByName(Transform parent, string name)
    {
        // Tìm BFS để tránh sai nếu có nhiều cấp lồng nhau
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            var found = FindChildByName(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
#endif
