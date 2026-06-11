using UnityEditor;
using UnityEngine;

// Custom Inspector cho EquipmentTierConfig:
// - Hiển thị warning nếu Tiers chưa cấu hình
// - Thêm nút "Auto-fill 4 Tiers (4/8/12/14)" để tạo nhanh
// - Preview màu tier ngay trong Inspector
[CustomEditor(typeof(EquipmentTierConfig))]
public class EquipmentTierConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var config = (EquipmentTierConfig)target;

        // Validation warnings
        bool hasError = false;

        if (config.tiers == null || config.tiers.Length == 0)
        {
            EditorGUILayout.HelpBox(
                "⚠ Mảng Tiers đang TRỐNG!\n" +
                "Nhấn nút bên dưới để tạo nhanh 4 tier (4/8/12/14).",
                MessageType.Error);
            hasError = true;
        }
        else
        {
            // Kiểm tra từng tier
            for (int i = 0; i < config.tiers.Length; i++)
            {
                var t = config.tiers[i];
                if (t == null) continue;
                if (t.borderSprite == null && t.bgSprite == null)
                {
                    EditorGUILayout.HelpBox(
                        $"⚠ Tier[{i}] (minLevel={t.minLevel}): borderSprite và bgSprite đều NULL!\n" +
                        "Kéo sprite vào ít nhất 1 trong 2 trường.",
                        MessageType.Warning);
                }
            }
        }

        // Nút auto-fill
        EditorGUILayout.Space(4);
        if (GUILayout.Button("◆ Auto-fill 4 Tiers (minLevel = 4 / 8 / 12 / 14)", GUILayout.Height(28)))
        {
            Undo.RecordObject(config, "Auto-fill Tiers");
            int[] levels = { 4, 8, 12, 14 };
            config.tiers = new EquipmentTierConfig.TierEntry[levels.Length];
            for (int i = 0; i < levels.Length; i++)
            {
                config.tiers[i] = new EquipmentTierConfig.TierEntry
                {
                    minLevel    = levels[i],
                    borderColor = TierColor(i),
                    bgColor     = TierColor(i) * new Color(1, 1, 1, 0.35f),
                };
            }
            EditorUtility.SetDirty(config);
            Debug.Log("[TierConfig] Auto-fill 4 tiers xong. Nhớ kéo Sprite vào từng tier!");
        }

        // Nút clear
        if (!hasError && GUILayout.Button("✕ Xoá toàn bộ Tiers", GUILayout.Height(22)))
        {
            if (EditorUtility.DisplayDialog("Xác nhận", "Xoá toàn bộ Tiers?", "OK", "Huỷ"))
            {
                Undo.RecordObject(config, "Clear Tiers");
                config.tiers = new EquipmentTierConfig.TierEntry[0];
                EditorUtility.SetDirty(config);
            }
        }

        EditorGUILayout.Space(6);

        // Preview tier hiện tại
        if (config.tiers != null && config.tiers.Length > 0)
        {
            EditorGUILayout.LabelField("Preview màu tier:", EditorStyles.boldLabel);
            foreach (var t in config.tiers)
            {
                if (t == null) continue;
                var rect = EditorGUILayout.GetControlRect(false, 18);
                EditorGUI.DrawRect(rect, t.borderColor);
                EditorGUI.LabelField(rect,
                    $"  Level ≥ {t.minLevel}   " +
                    $"border={(t.borderSprite != null ? t.borderSprite.name : "NULL")}  " +
                    $"bg={(t.bgSprite != null ? t.bgSprite.name : "NULL")}",
                    new GUIStyle(EditorStyles.label) { normal = { textColor = Color.white } });
            }
            EditorGUILayout.Space(4);
        }

        // Inspector gốc
        DrawDefaultInspector();
    }

    private static Color TierColor(int index)
    {
        return index switch
        {
            0 => new Color(0.2f, 0.6f, 1f),    // Xanh dương – tier 4
            1 => new Color(0.6f, 0.2f, 1f),    // Tím – tier 8
            2 => new Color(1f,  0.8f, 0f),     // Vàng – tier 12
            3 => new Color(1f,  0.2f, 0.2f),   // Đỏ – tier 14
            _ => Color.white,
        };
    }
}
