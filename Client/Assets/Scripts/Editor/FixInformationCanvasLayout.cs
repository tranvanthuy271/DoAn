using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor tool: sửa layout InformationCanvas + prefabs cho chuẩn.
/// Chạy từ menu:
///   Tools ▸ Fix InformationCanvas Layout   — sửa scene hierarchy
///   Tools ▸ Fix Prefabs Layout             — sửa SkillRow + PotentialStatRow prefab
/// </summary>
public class FixInformationCanvasLayout
{
    // ════════════════════════════════════════════════════════════════
    // 1. FIX SCENE LAYOUT
    // ════════════════════════════════════════════════════════════════
    [MenuItem("Tools/Fix InformationCanvas Layout")]
    public static void Fix()
    {
        var canvas = GameObject.Find("InformationCanvas");
        if (canvas == null)
        {
            Debug.LogError("[FixLayout] Không tìm thấy InformationCanvas trong scene!");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(canvas, "Fix InformationCanvas Layout");

        // ── CanvasScaler → Scale With Screen Size ──────────────────
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1024, 768);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            Debug.Log("[FixLayout] CanvasScaler → ScaleWithScreenSize 1024×768");
        }

        // ── CharacterPanel → stretch fill canvas ───────────────────
        var charPanel = canvas.transform.Find("CharacterPanel");
        if (charPanel == null) { Debug.LogError("[FixLayout] CharacterPanel not found!"); return; }

        SetStretch(charPanel, Vector2.zero, new Vector2(-100, -60));
        Debug.Log("[FixLayout] CharacterPanel → stretch with padding");

        // ── Window → stretch fill CharacterPanel ───────────────────
        var window = charPanel.Find("Window");
        if (window == null) { Debug.LogError("[FixLayout] Window not found!"); return; }

        SetStretch(window, Vector2.zero, Vector2.zero);
        Debug.Log("[FixLayout] Window → stretch fill parent");

        // ── Header (top bar, height 50) ────────────────────────────
        var header = window.Find("Header");
        if (header != null)
        {
            SetTopStrip(header, 50);
            FixChildStretchFill(header, "Title Text");
            Debug.Log("[FixLayout] Header → top strip h=50");
        }

        // ── TabBar (left sidebar) ──────────────────────────────────
        var tabBar = window.Find("TabBar");
        if (tabBar != null)
        {
            var tbRT = tabBar.GetComponent<RectTransform>();
            tbRT.anchorMin = new Vector2(0, 0);
            tbRT.anchorMax = new Vector2(0, 1);
            tbRT.pivot = new Vector2(0, 0.5f);
            tbRT.offsetMin = new Vector2(5, 5);          // left=5, bottom=5
            tbRT.offsetMax = new Vector2(165, -55);       // width=160, top margin=55(header)
            tbRT.localScale = Vector3.one;

            EnsureVerticalLayoutGroup(tabBar, 5, new RectOffset(5, 5, 10, 10),
                TextAnchor.UpperCenter, expandW: true, expandH: false,
                controlW: true, controlH: false);

            FixTabButton(tabBar, "BtnStats",     "Nhân vật",   45);
            FixTabButton(tabBar, "BtnSkill",     "Chiêu thức", 45);
            FixTabButton(tabBar, "BtnPotential", "Tiềm năng",  45);
            // ── Hide/Remove BtnEquipment if it exists ──────────────
            var btnEq = tabBar.Find("BtnEquipment");
            if (btnEq != null) btnEq.gameObject.SetActive(false);
            Debug.Log("[FixLayout] TabBar → left sidebar VLG – 3 tabs");
        }

        // ── TabContents (fills remaining space) ────────────────────
        var tabContents = window.Find("TabContents");
        if (tabContents != null)
        {
            var tcRT = tabContents.GetComponent<RectTransform>();
            tcRT.anchorMin = Vector2.zero;
            tcRT.anchorMax = Vector2.one;
            tcRT.pivot = new Vector2(0.5f, 0.5f);
            tcRT.offsetMin = new Vector2(175, 5);    // left of sidebar
            tcRT.offsetMax = new Vector2(-10, -55);   // below header
            tcRT.localScale = Vector3.one;

            FixContentPanelStretch(tabContents, "ContentStats");
            FixContentPanelStretch(tabContents, "ContentSkill");
            FixContentPanelStretch(tabContents, "ContentPotential");
            // Disable ContentEquipment if it still exists
            var ceq = tabContents?.Find("ContentEquipment");
            if (ceq != null) ceq.gameObject.SetActive(false);

            FixContentSkillInternals(tabContents);
            FixContentPotentialInternals(tabContents);
            FixContentStatsInternals(tabContents);

            Debug.Log("[FixLayout] TabContents → fill + content panels fixed");
        }

        EditorUtility.SetDirty(canvas);
        Debug.Log("[FixLayout] ══════════ SCENE FIX DONE ══════════");
    }

    // ════════════════════════════════════════════════════════════════
    // 2. FIX PREFABS
    // ════════════════════════════════════════════════════════════════
    [MenuItem("Tools/Fix Prefabs Layout")]
    public static void FixPrefabs()
    {
        FixSkillRowPrefab();
        FixPotentialStatRowPrefab();
        AssetDatabase.SaveAssets();
        Debug.Log("[FixPrefabs] ══════════ PREFAB FIX DONE ══════════");
    }

    // ────────────────────────────────────────────────────────────────
    // SkillRowPrefab – keep VerticalLayoutGroup, fix padding
    // Layout: VLG stacks [TxtSkillName][TxtLevel][TxtRequire][TxtDesc]
    //         BtnUpgrade floats at right (IgnoreLayout)
    // ────────────────────────────────────────────────────────────────
    private static void FixSkillRowPrefab()
    {
        string path = "Assets/Prefabs/UI/Thông tin/SkillRowPrefab.prefab";
        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefabAsset == null) { Debug.LogError($"[FixPrefabs] Not found: {path}"); return; }

        // Mở prefab để edit trực tiếp (không instantiate)
        string assetPath = AssetDatabase.GetAssetPath(prefabAsset);
        var root = PrefabUtility.LoadPrefabContents(assetPath);

        var rootRT = root.GetComponent<RectTransform>();
        rootRT.anchoredPosition = Vector2.zero;
        rootRT.sizeDelta = new Vector2(0, 0);
        rootRT.localScale = Vector3.one;

        // Fix VerticalLayoutGroup padding
        var vlg = root.GetComponent<VerticalLayoutGroup>();
        if (vlg != null)
        {
            vlg.padding = new RectOffset(10, 55, 5, 5); // right=55 for BtnUpgrade
            vlg.spacing = 2;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
        }

        // Fix LayoutElement on root
        var rootLE = root.GetComponent<LayoutElement>();
        if (rootLE != null)
        {
            rootLE.preferredHeight = 80;
            rootLE.minHeight = -1;
            rootLE.flexibleWidth = 1;
        }

        // Fix ContentSizeFitter
        var csf = root.GetComponent<ContentSizeFitter>();
        if (csf != null)
        {
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        // Fix text children – reset anchors, margins
        FixTextChild(root.transform, "TxtSkillName", 22, FontStyles.Bold, TextAlignmentOptions.Left);
        FixTextChild(root.transform, "TxtLevel",     18, FontStyles.Normal, TextAlignmentOptions.Left);
        FixTextChild(root.transform, "TxtRequire",   16, FontStyles.Normal, TextAlignmentOptions.Left);
        FixTextChild(root.transform, "TxtDesc",      14, FontStyles.Italic, TextAlignmentOptions.Left);

        // Fix BtnUpgrade – anchored right-center, IgnoreLayout
        var btnUpgrade = root.transform.Find("BtnUpgrade");
        if (btnUpgrade != null)
        {
            var btnRT = btnUpgrade.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(1, 0.5f);
            btnRT.anchorMax = new Vector2(1, 0.5f);
            btnRT.pivot = new Vector2(1, 0.5f);
            btnRT.anchoredPosition = new Vector2(-5, 0);
            btnRT.sizeDelta = new Vector2(44, 44);
            btnRT.localScale = Vector3.one;

            var btnLE = btnUpgrade.GetComponent<LayoutElement>();
            if (btnLE != null)
            {
                btnLE.ignoreLayout = true;
            }

            FixChildStretchFill(btnUpgrade, "Text (TMP)");
        }

        PrefabUtility.SaveAsPrefabAsset(root, assetPath);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("[FixPrefabs] SkillRowPrefab → VLG padding fixed");
    }

    // ────────────────────────────────────────────────────────────────
    // PotentialStatRowPrefab – HorizontalLayoutGroup
    // Layout: HLG [TxtStatName | TxtPoints | BtnMinus | BtnPlus | BtnMax]
    // ────────────────────────────────────────────────────────────────
    private static void FixPotentialStatRowPrefab()
    {
        string path = "Assets/Prefabs/UI/Thông tin/PotentialStatRowPrefab.prefab";
        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefabAsset == null) { Debug.LogError($"[FixPrefabs] Not found: {path}"); return; }

        string assetPath = AssetDatabase.GetAssetPath(prefabAsset);
        var root = PrefabUtility.LoadPrefabContents(assetPath);

        // ── Root RectTransform ─────────────────────────────────────
        var rootRT = root.GetComponent<RectTransform>();
        rootRT.anchoredPosition = Vector2.zero;
        rootRT.sizeDelta = new Vector2(0, 50);
        rootRT.localScale = Vector3.one;

        // ── Ensure HorizontalLayoutGroup ───────────────────────────
        var hlg = root.GetComponent<HorizontalLayoutGroup>();
        if (hlg == null) hlg = root.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6;
        hlg.padding = new RectOffset(10, 10, 5, 5);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        // ── Ensure LayoutElement ───────────────────────────────────
        var rootLE = root.GetComponent<LayoutElement>();
        if (rootLE == null) rootLE = root.AddComponent<LayoutElement>();
        rootLE.minHeight = 40;
        rootLE.preferredHeight = 50;
        rootLE.flexibleWidth = 1;

        // ── Remove legacy TxtValue / BtnUpgrade if present ─────────
        DestroyChildIfExists(root.transform, "TxtValue");
        DestroyChildIfExists(root.transform, "BtnUpgrade");

        // ── Text children ──────────────────────────────────────────
        // TxtStatName – stretches to fill available space
        FixHLGChild(root.transform, "TxtStatName", minW: 100, prefW: 140, flex: 1f);
        // TxtPoints – fixed width box showing current + pending value
        FixHLGChild(root.transform, "TxtPoints", minW: 50, prefW: 65, flex: 0f);

        // ── 3 action buttons ───────────────────────────────────────
        EnsureRowButton(root.transform, "BtnMinus", "−", 38);
        EnsureRowButton(root.transform, "BtnPlus",  "+", 38);
        EnsureRowButton(root.transform, "BtnMax",   "▲", 38);

        // ── Wire to PotentialStatRowUI ─────────────────────────────
        var rowUI = root.GetComponent<PotentialStatRowUI>();
        if (rowUI != null)
        {
            var so = new SerializedObject(rowUI);
            SetFieldRef(so, "txtStatName", root.transform.Find("TxtStatName")?.GetComponent<TMP_Text>());
            SetFieldRef(so, "txtPoints",   root.transform.Find("TxtPoints")?.GetComponent<TMP_Text>());
            SetButtonFieldRef(so, "btnMinus", root.transform.Find("BtnMinus"));
            SetButtonFieldRef(so, "btnPlus",  root.transform.Find("BtnPlus"));
            SetButtonFieldRef(so, "btnMax",   root.transform.Find("BtnMax"));
            so.ApplyModifiedProperties();
        }

        PrefabUtility.SaveAsPrefabAsset(root, assetPath);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("[FixPrefabs] PotentialStatRowPrefab → HLG layout fixed (BtnMinus/BtnPlus/BtnMax)");
    }

    private static void EnsureRowButton(Transform parent, string name, string label, float size)
    {
        var existing = parent.Find(name);
        if (existing == null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.85f, 0.65f, 0.3f, 1f);
            var txtGO = new GameObject("Text (TMP)", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGO.transform.SetParent(go.transform, false);
            var txtRT = txtGO.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = Vector2.zero; txtRT.offsetMax = Vector2.zero;
            var tmp = txtGO.GetComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = 20; tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;
            existing = go.transform;
        }

        CleanLayoutElements(existing.gameObject);
        var le = existing.GetComponent<LayoutElement>();
        if (le == null) le = existing.gameObject.AddComponent<LayoutElement>();
        le.minWidth = size - 2; le.preferredWidth = size;
        le.minHeight = size - 4; le.preferredHeight = size;
        le.flexibleWidth = 0;

        var btnTMP = existing.Find("Text (TMP)")?.GetComponent<TMP_Text>();
        if (btnTMP != null) { btnTMP.text = label; }
    }

    private static void DestroyChildIfExists(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child != null) Object.DestroyImmediate(child.gameObject);
    }

    // ════════════════════════════════════════════════════════════════
    // UTILITIES
    // ════════════════════════════════════════════════════════════════

    /// <summary>Stretch fill parent (anchor 0,0→1,1), optional padding via sizeDelta.</summary>
    private static void SetStretch(Transform t, Vector2 pad, Vector2 sizeDelta)
    {
        var rt = t.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.anchoredPosition = pad;
        rt.sizeDelta = sizeDelta;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;
    }

    /// <summary>Anchor to top, full width, fixed height.</summary>
    private static void SetTopStrip(Transform t, float height)
    {
        var rt = t.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0, height);
        rt.localScale = Vector3.one;
    }

    /// <summary>Make a child stretch-fill its parent.</summary>
    private static void FixChildStretchFill(Transform parent, string childName)
    {
        var child = parent.Find(childName);
        if (child == null) return;
        var rt = child.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    /// <summary>Content panel stretch fill TabContents with small inset.</summary>
    private static void FixContentPanelStretch(Transform parent, string name)
    {
        var panel = parent?.Find(name);
        if (panel == null) return;
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(5, 5);
        rt.offsetMax = new Vector2(-5, -5);
        rt.localScale = Vector3.one;
    }

    /// <summary>Fix internal layout of ContentStats (character info + equipment list).</summary>
    private static void FixContentStatsInternals(Transform tabContents)
    {
        var cs = tabContents?.Find("ContentStats");
        if (cs == null) return;

        // Fix any ScrollView inside (for equipment list)
        foreach (Transform child in cs)
        {
            if (child.GetComponent<ScrollRect>() != null)
            {
                var rt = child.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.offsetMin = new Vector2(5, 5);
                rt.offsetMax = new Vector2(-5, -5);
                EnsureScrollContent(child);
                break;
            }
        }
    }

    /// <summary>Fix internal layout of ContentSkill.</summary>
    private static void FixContentSkillInternals(Transform tabContents)
    {
        var cs = tabContents?.Find("ContentSkill");
        if (cs == null) return;

        // Fix any ScrollView inside
        foreach (Transform child in cs)
        {
            if (child.GetComponent<ScrollRect>() != null)
            {
                var rt = child.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.offsetMin = new Vector2(5, 40);
                rt.offsetMax = new Vector2(-5, -35);

                EnsureScrollContent(child);
                break;
            }
        }

        // Fix TxtSkillPoints position
        FixTopLabel(cs, "TxtSkillPoints", 30);
        FixBottomLabel(cs, "TxtStatus", 30);
    }

    /// <summary>Fix internal layout of ContentPotential.</summary>
    private static void FixContentPotentialInternals(Transform tabContents)
    {
        var cp = tabContents?.Find("ContentPotential");
        if (cp == null) return;

        // ── Remove SubTabBar if it exists (no longer used) ────────
        var oldSubTabBar = cp.Find("SubTabBar");
        if (oldSubTabBar != null)
        {
            Object.DestroyImmediate(oldSubTabBar.gameObject);
            Debug.Log("[FixLayout] Removed SubTabBar from ContentPotential");
        }

        // ── Create bottom action bar (BtnHuy + BtnCong) if missing ─
        var actionBar = cp.Find("ActionBar");
        if (actionBar == null)
        {
            var barGO = new GameObject("ActionBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            barGO.transform.SetParent(cp, false);
            actionBar = barGO.transform;

            var hlg = barGO.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.padding = new RectOffset(8, 8, 4, 4);
            hlg.childAlignment = TextAnchor.MiddleRight;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            CreateActionButton(actionBar, "BtnHuy",  "Hủy",  new Color(0.72f, 0.35f, 0.25f, 1f));
            CreateActionButton(actionBar, "BtnCong", "Cộng", new Color(0.35f, 0.60f, 0.25f, 1f));

            Debug.Log("[FixLayout] Created ActionBar with BtnHuy + BtnCong");
        }

        // Position ActionBar at bottom
        var abRT = actionBar.GetComponent<RectTransform>();
        abRT.anchorMin = new Vector2(0, 0);
        abRT.anchorMax = new Vector2(1, 0);
        abRT.pivot = new Vector2(0.5f, 0);
        abRT.anchoredPosition = Vector2.zero;
        abRT.sizeDelta = new Vector2(0, 40);

        // ── Wire action buttons to PotentialTabUI ─────────────────
        var potentialTabUI = cp.GetComponent<PotentialTabUI>();
        if (potentialTabUI != null)
        {
            var so = new SerializedObject(potentialTabUI);
            SetButtonRef(so, "btnHuy",  actionBar.Find("BtnHuy"));
            SetButtonRef(so, "btnCong", actionBar.Find("BtnCong"));
            so.ApplyModifiedProperties();
            Debug.Log("[FixLayout] Wired BtnHuy + BtnCong to PotentialTabUI");
        }

        // Fix any ScrollView – leave room for ActionBar + TxtPotentialPoints at bottom
        foreach (Transform child in cp)
        {
            if (child.GetComponent<ScrollRect>() != null)
            {
                var rt = child.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.offsetMin = new Vector2(5, 80);   // bottom: above ActionBar + TxtPotentialPoints
                rt.offsetMax = new Vector2(-5, -5);  // top: full

                EnsureScrollContent(child);
                break;
            }
        }

        FixBottomLabel(cp, "TxtPotentialPoints", 32);
        FixBottomLabel(cp, "TxtStatus", 30);
    }

    private static void CreateActionButton(Transform parent, string name, string label, Color color)
    {
        var btnGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(parent, false);

        btnGO.GetComponent<Image>().color = color;

        var le = btnGO.AddComponent<LayoutElement>();
        le.minWidth = 80;
        le.preferredWidth = 90;
        le.minHeight = 32;
        le.preferredHeight = 36;

        var txtGO = new GameObject("Text (TMP)", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGO.transform.SetParent(btnGO.transform, false);
        var txtRT = txtGO.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero; txtRT.offsetMax = Vector2.zero;

        var tmp = txtGO.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 17;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }

    private static void SetButtonRef(SerializedObject so, string fieldName, Transform btnTransform)
    {
        if (btnTransform == null) return;
        var btn = btnTransform.GetComponent<Button>();
        if (btn == null) return;
        var prop = so.FindProperty(fieldName);
        if (prop != null)
            prop.objectReferenceValue = btn;
    }

    private static void SetButtonFieldRef(SerializedObject so, string fieldName, Transform btnTransform)
        => SetButtonRef(so, fieldName, btnTransform);

    private static void SetFieldRef<T>(SerializedObject so, string fieldName, T obj) where T : Object
    {
        if (obj == null) return;
        var prop = so.FindProperty(fieldName);
        if (prop != null)
            prop.objectReferenceValue = obj;
    }

    // ─── Scroll Content helpers ───────────────────────────────
    private static void EnsureScrollContent(Transform scrollViewTr)
    {
        var viewport = scrollViewTr.Find("Viewport");
        if (viewport == null) return;
        var content = viewport.Find("Content");
        if (content == null) return;

        var vlg = content.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4;
        vlg.padding = new RectOffset(5, 5, 5, 5);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        var csf = content.GetComponent<ContentSizeFitter>();
        if (csf == null) csf = content.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    // ─── Label position helpers ───────────────────────────────
    private static void FixTopLabel(Transform parent, string name, float height)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Contains(name.Replace("Txt", "")))
            {
                var rt = child.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0.5f, 1);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(-20, height);
                return;
            }
        }
    }

    private static void FixBottomLabel(Transform parent, string name, float height)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Contains(name.Replace("Txt", "")))
            {
                var rt = child.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(1, 0);
                rt.pivot = new Vector2(0.5f, 0);
                rt.anchoredPosition = new Vector2(0, 5);
                rt.sizeDelta = new Vector2(-20, height);
                return;
            }
        }
    }

    // ─── Tab button helper ────────────────────────────────────
    private static void FixTabButton(Transform tabBar, string name, string label, float height)
    {
        var btn = tabBar.Find(name);
        if (btn == null) return;

        var rt = btn.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0, height);
        rt.localScale = Vector3.one;

        var le = btn.GetComponent<LayoutElement>();
        if (le == null) le = btn.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight = height;
        le.flexibleWidth = 1;

        var text = btn.Find("Text") ?? btn.Find("Text (TMP)");
        if (text != null)
        {
            FixChildStretchFill(btn, text.name);
            var tmp = text.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.text = label;
                tmp.fontSize = 18;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 12;
                tmp.fontSizeMax = 22;
                tmp.margin = Vector4.zero;
            }
        }
    }

    // ─── VLG helper ───────────────────────────────────────────
    private static VerticalLayoutGroup EnsureVerticalLayoutGroup(
        Transform t, float spacing, RectOffset padding,
        TextAnchor align, bool expandW, bool expandH,
        bool controlW, bool controlH)
    {
        var vlg = t.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = t.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = spacing;
        vlg.padding = padding;
        vlg.childAlignment = align;
        vlg.childForceExpandWidth = expandW;
        vlg.childForceExpandHeight = expandH;
        vlg.childControlWidth = controlW;
        vlg.childControlHeight = controlH;
        return vlg;
    }

    // ─── Prefab text child fix ────────────────────────────────
    private static void FixTextChild(Transform parent, string name, int fontSize,
        FontStyles style, TextAlignmentOptions align)
    {
        var child = parent.Find(name);
        if (child == null) return;

        var rt = child.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.localScale = Vector3.one;

        var le = child.GetComponent<LayoutElement>();
        if (le != null)
        {
            le.minHeight = 20;
            le.flexibleWidth = 1;
        }

        var tmp = child.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            tmp.margin = Vector4.zero;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = Mathf.Max(10, fontSize - 6);
            tmp.fontSizeMax = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = align;
        }
    }

    // ─── HLG child fix (for PotentialStatRowPrefab) ───────────
    private static void FixHLGChild(Transform parent, string name, float minW, float prefW, float flex)
    {
        var child = parent.Find(name);
        if (child == null) return;

        var rt = child.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(prefW, 0);
        rt.localScale = Vector3.one;

        CleanLayoutElements(child.gameObject);
        var le = child.GetComponent<LayoutElement>();
        if (le == null) le = child.gameObject.AddComponent<LayoutElement>();
        le.enabled = true;
        le.ignoreLayout = false;
        le.minWidth = minW;
        le.preferredWidth = prefW;
        le.flexibleWidth = flex;

        var tmp = child.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            tmp.margin = Vector4.zero;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 12;
            tmp.fontSizeMax = 20;
        }
    }

    // ─── Utility: remove duplicate LayoutElements ─────────────
    private static void CleanLayoutElements(GameObject go)
    {
        var all = go.GetComponents<LayoutElement>();
        if (all.Length > 1)
        {
            for (int i = all.Length - 1; i >= 1; i--)
                Object.DestroyImmediate(all[i]);
        }
    }
}
