#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor tool – tự động tạo prefab UI cho hệ thống skill effects:
///   • StatusIconEntry prefab (32×32)
///   • Tự động điền giá trị cho 7 file .asset SkillEffectConfig
///
/// Menu: GameTools → Skill Effects → ...
/// </summary>
public static class SkillEffectUiBuilder
{
    private const string PREFAB_DIR   = "Assets/Resources/Prefabs/UI";
    private const string NOTO_SANS    = "Assets/TextMesh Pro/Resources/Fonts & Materials/NotoSans-Regular SDF.asset";
    private const string SKILL_SO_DIR = "Assets/Resources/ScriptableObjects/skill effect";

    private static TMP_FontAsset _font;

    // ────────────────────────────────────────────────────────────────────────
    [MenuItem("GameTools/Skill Effects/Create Status Icon Prefab")]
    public static void CreateStatusIconPrefab()
    {
        EnsureDir(PREFAB_DIR);
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NOTO_SANS);

        CreateStatusIconEntry();

        AssetDatabase.Refresh();
        Debug.Log("[SkillEffectUiBuilder] ✓ Tạo xong StatusIconEntry prefab tại " + PREFAB_DIR);
    }

    // ────────────────────────────────────────────────────────────────────────
    // CẤU HÌNH TỰ ĐỘNG 7 FILE .ASSET SkillEffectConfig
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tự động điền giá trị cho 7 file SkillEffectConfig .asset.
    /// Tìm file theo tên trong Assets/Resources/ScriptableObjects/skill effect/.
    ///
    /// Các file cần tồn tại trước (tạo qua Assets → Create → DoAn → Skill Effect Config):
    ///   Burn_Config, DefenseDown_Config, Freeze_Config, Slow_Config, Weaken_Config,
    ///   Buff_WaterArmor_Config, Buff_EarthAura_Config
    /// </summary>
    [MenuItem("GameTools/Skill Effects/Configure All Effect Configs")]
    public static void ConfigureAllEffectConfigs()
    {
        // ── Định nghĩa 7 preset ──────────────────────────────────────────
        var presets = new[]
        {
            // DEBUFFS ─────────────────────────────────────────────────────
            new EffectPreset
            {
                assetName     = "Slow_Config",
                isBuff        = false,
                debuffType    = SkillDebuffType.Slow,
                debuffValue   = 50,
                debuffDuration= 3f,
                iconId        = 201,
                debuffName    = "Chậm Chạp",
                debuffTint    = new Color(1f, 1f, 0f, 0.5f),          // vàng
                ringColor     = new Color(1f, 0.8f, 0f, 1f),
            },
            new EffectPreset
            {
                assetName     = "Weaken_Config",
                isBuff        = false,
                debuffType    = SkillDebuffType.Weaken,
                debuffValue   = 25,
                debuffDuration= 5f,
                iconId        = 202,
                debuffName    = "Suy Yếu",
                debuffTint    = new Color(0.7f, 0f, 1f, 0.5f),        // tím
                ringColor     = new Color(0.7f, 0.1f, 1f, 1f),
            },
            new EffectPreset
            {
                assetName     = "Burn_Config",
                isBuff        = false,
                debuffType    = SkillDebuffType.Burn,
                debuffValue   = 8,
                debuffDuration= 4f,
                iconId        = 203,
                debuffName    = "Bỏng Lửa",
                debuffTint    = new Color(1f, 0.3f, 0f, 0.6f),        // cam đỏ
                ringColor     = new Color(1f, 0.3f, 0f, 1f),
            },
            new EffectPreset
            {
                assetName     = "Freeze_Config",
                isBuff        = false,
                debuffType    = SkillDebuffType.Freeze,
                debuffValue   = 0,
                debuffDuration= 2f,
                iconId        = 204,
                debuffName    = "Đóng Băng",
                debuffTint    = new Color(0.3f, 0.8f, 1f, 0.7f),      // xanh băng
                ringColor     = new Color(0.3f, 0.8f, 1f, 1f),
            },
            new EffectPreset
            {
                assetName     = "DefenseDown_Config",
                isBuff        = false,
                debuffType    = SkillDebuffType.DefenseDown,
                debuffValue   = 30,
                debuffDuration= 5f,
                iconId        = 205,
                debuffName    = "Giảm Phòng Thủ",
                debuffTint    = new Color(0.5f, 0f, 0f, 0.5f),        // đỏ tối
                ringColor     = new Color(0.8f, 0.1f, 0.1f, 1f),
            },
            // BUFFS ───────────────────────────────────────────────────────
            new EffectPreset
            {
                assetName     = "Buff_WaterArmor_Config",
                isBuff        = true,
                debuffType    = SkillDebuffType.None,
                debuffValue   = 0,
                debuffDuration= 0.5f,
                iconId        = 151,
                debuffName    = "",
                buffName      = "Thủy Giáp Hộ Thể",
                buffDuration  = 5f,
                buffTint      = new Color(0.2f, 0.8f, 1f, 0.6f),      // cyan
                ringColor     = new Color(0.2f, 0.9f, 1f, 1f),
                debuffTint    = new Color(0.2f, 0.8f, 1f, 0.6f),
            },
            new EffectPreset
            {
                assetName     = "Buff_EarthAura_Config",
                isBuff        = true,
                debuffType    = SkillDebuffType.None,
                debuffValue   = 0,
                debuffDuration= 0.5f,
                iconId        = 152,
                debuffName    = "",
                buffName      = "Địa Uy Khí",
                buffDuration  = 6f,
                buffTint      = new Color(1f, 0.85f, 0.1f, 0.5f),     // vàng gold
                ringColor     = new Color(1f, 0.9f, 0.2f, 1f),
                debuffTint    = new Color(1f, 0.85f, 0.1f, 0.5f),
            },
        };

        int configured = 0;
        int missing    = 0;

        foreach (var p in presets)
        {
            // Tìm asset trong SKILL_SO_DIR (không phân biệt hoa thường)
            string assetPath = $"{SKILL_SO_DIR}/{p.assetName}.asset";
            var so = AssetDatabase.LoadAssetAtPath<SkillEffectConfig>(assetPath);

            if (so == null)
            {
                // Tìm theo GUID trong toàn bộ project
                var guids = AssetDatabase.FindAssets($"t:SkillEffectConfig {p.assetName}");
                if (guids.Length > 0)
                    so = AssetDatabase.LoadAssetAtPath<SkillEffectConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            if (so == null)
            {
                Debug.LogWarning($"[SkillEffectUiBuilder] ✗ Không tìm thấy '{p.assetName}.asset'. " +
                                 $"Hãy tạo file trước qua Assets → Create → DoAn → Skill Effect Config.");
                missing++;
                continue;
            }

            // Dùng SerializedObject để set giá trị (an toàn, undo-able)
            var serialized = new SerializedObject(so);
            serialized.FindProperty("isBuff")         .boolValue   = p.isBuff;
            serialized.FindProperty("debuffType")     .enumValueIndex = (int)p.debuffType;
            serialized.FindProperty("debuffValue")    .intValue    = p.debuffValue;
            serialized.FindProperty("debuffDuration") .floatValue  = p.debuffDuration;
            serialized.FindProperty("iconId")         .intValue    = p.iconId;
            serialized.FindProperty("debuffName")     .stringValue = p.debuffName;
            serialized.FindProperty("debuffTintColor").colorValue  = p.debuffTint;
            serialized.FindProperty("ringColor")      .colorValue  = p.ringColor;
            serialized.FindProperty("buffName")       .stringValue = p.buffName;
            serialized.FindProperty("buffDuration")   .floatValue  = p.buffDuration;
            serialized.FindProperty("buffTintColor")  .colorValue  = p.buffTint;
            serialized.ApplyModifiedProperties();

            EditorUtility.SetDirty(so);
            Debug.Log($"[SkillEffectUiBuilder] ✓ Đã config '{p.assetName}'");
            configured++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string summary = $"[SkillEffectUiBuilder] Hoàn tất: {configured} config ✓, {missing} file thiếu ✗";
        Debug.Log(summary);
        EditorUtility.DisplayDialog("Configure Effect Configs", summary, "OK");
    }

    // ────────────────────────────────────────────────────────────────────────
    // STATUS ICON ENTRY  (32×32 – dùng cho OverheadStatusDisplay)
    // ────────────────────────────────────────────────────────────────────────

    private static void CreateStatusIconEntry()
    {
        var root = new GameObject("StatusIconEntry");
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(32f, 32f);

        // Background
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.65f);
        bg.raycastTarget = false;

        // ── Icon ──────────────────────────────────────────────────────────
        var iconGo   = new GameObject("Icon");
        iconGo.transform.SetParent(root.transform, false);
        var iconRect = iconGo.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.1f, 0.1f);
        iconRect.anchorMax = new Vector2(0.9f, 0.9f);
        iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;

        var iconImg = iconGo.AddComponent<Image>();
        iconImg.preserveAspect  = true;
        iconImg.raycastTarget   = false;

        // ── CountdownRing (Radial360 fill) ────────────────────────────────
        var ringGo   = new GameObject("CountdownRing");
        ringGo.transform.SetParent(root.transform, false);
        var ringRect = ringGo.AddComponent<RectTransform>();
        ringRect.anchorMin = Vector2.zero;
        ringRect.anchorMax = Vector2.one;
        ringRect.offsetMin = ringRect.offsetMax = Vector2.zero;

        var ringImg = ringGo.AddComponent<Image>();
        ringImg.color           = new Color(1f, 0.2f, 0.2f, 0.80f);
        ringImg.type            = Image.Type.Filled;
        ringImg.fillMethod      = Image.FillMethod.Radial360;
        ringImg.fillOrigin      = (int)Image.Origin360.Top;
        ringImg.fillClockwise   = true;
        ringImg.fillAmount      = 1f;
        ringImg.raycastTarget   = false;

        // ── TimeLabel ─────────────────────────────────────────────────────
        var labelGo   = new GameObject("TimeLabel");
        labelGo.transform.SetParent(root.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0.35f);
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;

        var labelText = labelGo.AddComponent<TextMeshProUGUI>();
        labelText.text                 = "5s";
        labelText.fontSize             = 8f;
        labelText.alignment            = TextAlignmentOptions.Center;
        labelText.color                = Color.white;
        labelText.enableWordWrapping   = false;
        labelText.overflowMode         = TextOverflowModes.Truncate;
        labelText.raycastTarget        = false;
        if (_font != null) labelText.font = _font;

        // ── Gắn Script ────────────────────────────────────────────────────
        var script = root.AddComponent<StatusIconEntry>();

        // Dùng SerializedObject để set private serialized fields
        var so = new SerializedObject(script);
        so.FindProperty("iconImage")    .objectReferenceValue = iconImg;
        so.FindProperty("countdownRing").objectReferenceValue = ringImg;
        so.FindProperty("timeLabel")    .objectReferenceValue = labelText;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Lưu prefab
        string path = $"{PREFAB_DIR}/StatusIconEntry.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        Debug.Log($"[SkillEffectUiBuilder] Đã tạo StatusIconEntry tại {path}");
    }

    // ────────────────────────────────────────────────────────────────────────

    private static void EnsureDir(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            var parts  = path.Split('/');
            var parent = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = parent + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(parent, parts[i]);
                parent = next;
            }
        }
    }

    // ── Helper struct cho preset config ──────────────────────────────────────
    private struct EffectPreset
    {
        public string           assetName;
        public bool             isBuff;
        public SkillDebuffType  debuffType;
        public int              debuffValue;
        public float            debuffDuration;
        public int              iconId;
        public string           debuffName;
        public Color            debuffTint;
        public Color            ringColor;
        // Buff-specific
        public string           buffName;
        public float            buffDuration;
        public Color            buffTint;
    }
}

// ════════════════════════════════════════════════════════════════════════════
// Tool: Gắn effectConfig vào từng SkillData trong 12 player prefabs
// Menu: GameTools → Skill Effects → Assign Effect Configs to Prefabs
// ════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Tự động gán SkillEffectConfig vào trường effectConfig của từng SkillData
/// trong PlayerSkillManager trên tất cả 12 player prefabs.
///
/// Mapping theo skillCode (đáng tin cậy, không phụ thuộc tên Unicode):
///   FIRE_BOLT / FIRE_BURST / FIRE_RAIN / HYBRID_FIRE_EARTH_LAVA_AURA → Burn_Config
///   WATER_BOLT / WATER_PILLAR / EARTH_BLINK                          → Slow_Config
///   WATER_ARMOR                                                       → Buff_WaterArmor_Config
///   EARTH_AURA                                                        → Buff_EarthAura_Config
///   EARTH_BOOMERANG / HYBRID_METAL_WIND_BARRAGE                       → DefenseDown_Config
///   HYBRID_WATER_WOOD_VENOM                                           → Weaken_Config
/// </summary>
public static class SkillEffectConfigAssigner
{
    private static readonly string[] PLAYER_PREFAB_PATHS = new[]
    {
        "Assets/Prefabs/Player/He/Hoa.prefab",
        "Assets/Prefabs/Player/He/Kim.prefab",
        "Assets/Prefabs/Player/He/Moc.prefab",
        "Assets/Prefabs/Player/He/Phong.prefab",
        "Assets/Prefabs/Player/He/Tho.prefab",
        "Assets/Prefabs/Player/He/Thuy.prefab",
        "Assets/Prefabs/Player/Fusion/F_Hoa.prefab",
        "Assets/Prefabs/Player/Fusion/F_Kim.prefab",
        "Assets/Prefabs/Player/Fusion/F_Moc.prefab",
        "Assets/Prefabs/Player/Fusion/F_Phong.prefab",
        "Assets/Prefabs/Player/Fusion/F_Tho.prefab",
        "Assets/Prefabs/Player/Fusion/F_Thuy.prefab",
    };

    private const string SO_DIR = "Assets/Resources/ScriptableObjects/skill effect";

    // skillCode → asset file name (without extension)
    private static readonly System.Collections.Generic.Dictionary<string, string> CODE_TO_ASSET
        = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
    {
        { "FIRE_BOLT",                    "Burn_Config"            },
        { "FIRE_BURST",                   "Burn_Config"            },
        { "FIRE_RAIN",                    "Burn_Config"            },
        { "HYBRID_FIRE_EARTH_LAVA_AURA",  "Burn_Config"            },
        { "WATER_BOLT",                   "Slow_Config"            },
        { "WATER_PILLAR",                 "Slow_Config"            },
        { "EARTH_BLINK",                  "Slow_Config"            },
        { "WATER_ARMOR",                  "Buff_WaterArmor_Config" },
        { "EARTH_AURA",                   "Buff_EarthAura_Config"  },
        { "EARTH_BOOMERANG",              "DefenseDown_Config"     },
        { "HYBRID_METAL_WIND_BARRAGE",    "DefenseDown_Config"     },
        { "HYBRID_WATER_WOOD_VENOM",      "Weaken_Config"         },
    };

    [MenuItem("GameTools/Skill Effects/Assign Effect Configs to Prefabs")]
    public static void AssignAllEffectConfigs()
    {
        // ── 1. Nạp tất cả SkillEffectConfig assets ──────────────────────────
        var configCache = new System.Collections.Generic.Dictionary<string, SkillEffectConfig>();
        foreach (var assetName in new System.Collections.Generic.HashSet<string>(CODE_TO_ASSET.Values))
        {
            string path = $"{SO_DIR}/{assetName}.asset";
            var cfg = AssetDatabase.LoadAssetAtPath<SkillEffectConfig>(path);
            if (cfg == null)
            {
                // Fallback: tìm trong toàn project
                var guids = AssetDatabase.FindAssets($"t:SkillEffectConfig {assetName}");
                if (guids.Length > 0)
                    cfg = AssetDatabase.LoadAssetAtPath<SkillEffectConfig>(
                              AssetDatabase.GUIDToAssetPath(guids[0]));
            }
            if (cfg != null)
                configCache[assetName] = cfg;
            else
                Debug.LogWarning($"[SkillEffectConfigAssigner] ✗ Không tìm thấy '{assetName}.asset'. " +
                                 "Hãy chạy Configure All Effect Configs trước.");
        }

        int totalAssigned = 0;
        int totalPrefabs  = 0;

        // ── 2. Duyệt từng prefab ─────────────────────────────────────────────
        foreach (string prefabPath in PLAYER_PREFAB_PATHS)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (asset == null)
            {
                Debug.LogWarning($"[SkillEffectConfigAssigner] ✗ Không tìm thấy: {prefabPath}");
                continue;
            }

            using var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath);
            var root = scope.prefabContentsRoot;

            var psm = root.GetComponent<PlayerSkillManager>()
                   ?? root.GetComponentInChildren<PlayerSkillManager>(true);
            if (psm == null)
            {
                Debug.LogWarning($"[SkillEffectConfigAssigner] ✗ Không có PlayerSkillManager: {prefabPath}");
                continue;
            }

            var so     = new SerializedObject(psm);
            var skills = so.FindProperty("skills");
            int changed = 0;

            for (int i = 0; i < skills.arraySize; i++)
            {
                var elem      = skills.GetArrayElementAtIndex(i);
                var codeProp  = elem.FindPropertyRelative("skillCode");
                var cfgProp   = elem.FindPropertyRelative("effectConfig");

                if (codeProp == null || cfgProp == null) continue;

                string code = codeProp.stringValue;
                if (string.IsNullOrEmpty(code)) continue;
                if (!CODE_TO_ASSET.TryGetValue(code, out string assetName)) continue;
                if (!configCache.TryGetValue(assetName, out var cfg)) continue;

                // Chỉ gán nếu chưa có hoặc sai
                if (cfgProp.objectReferenceValue == cfg) continue;

                cfgProp.objectReferenceValue = cfg;
                changed++;
                totalAssigned++;
                Debug.Log($"[SkillEffectConfigAssigner] ✓ {System.IO.Path.GetFileNameWithoutExtension(prefabPath)}" +
                          $" | skill '{code}' → {assetName}");
            }

            if (changed > 0)
            {
                so.ApplyModifiedProperties();
                totalPrefabs++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string msg = $"Hoàn tất: gán {totalAssigned} effectConfig trên {totalPrefabs} prefab.";
        Debug.Log($"[SkillEffectConfigAssigner] {msg}");
        EditorUtility.DisplayDialog("Assign Effect Configs", msg, "OK");
    }
}
#endif
