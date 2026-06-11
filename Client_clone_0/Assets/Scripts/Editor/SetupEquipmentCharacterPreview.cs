#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

// Editor tool: Tự động tạo toàn bộ cấu trúc Character Preview
// trong tab Trang Bị (ContentEquipment) của InformationCanvas.
// Chạy từ menu:  Tools ▸ Setup Equipment Character Preview
// Cấu trúc sẽ tạo ra:
// InformationCanvas
// ├── CharacterPanel / Window / TabContents / ContentEquipment
// ├── [EquipmentPanelUI]
// └── CharPreviewSlot       ← NEW (EquipmentCharacterPreview)
// └── RawImage_CharPreview ← NEW (RawImage hiển thị nhân vật)
// └── PreviewCamera            ← NEW (Camera ngoài Canvas)
public static class SetupEquipmentCharacterPreview
{
    private const string MENU = "Tools/Setup Equipment Character Preview";

    [MenuItem(MENU)]
    public static void Run()
    {
        { /* ══════════ BẮT ĐẦU SETUP ══════════ */ }

        // 1. Tìm ContentEquipment
        { /* Đang tìm ContentEquipment */ }
        var contentEquipment = FindContentEquipment();
        if (contentEquipment == null)
        {
            { /* Lỗi: KHÔNG TÌM THẤY ContentEquipment */ }
                if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("Setup Preview",
                    "Không tìm thấy ContentEquipment trong scene!\n\n" +
                    "Đảm bảo scene đang mở có InformationCanvas > CharacterPanel > Window > TabContents > ContentEquipment.\n" +
                    "Hoặc tên GameObject chứa EquipmentPanelUI.",
                    "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(contentEquipment, "Setup Equipment Character Preview");

        { /* ✓ Tìm thấy ContentEquipment: {GetPath(contentEquipment.transform)} */ }

        // 2. Tạo Camera preview (ngoài Canvas)
        { /* Đang tạo EquipPreviewCamera */ }
        var cam = SetupPreviewCamera();
        { /* ✓ Camera: {(cam != null ? cam.gameObject.name */ }

        // 3. Tạo CharPreviewSlot trong ContentEquipment
        { /* Đang tạo CharPreviewSlot */ }
        var slotGO = SetupCharPreviewSlot(contentEquipment, cam);
        { /* ✓ Slot: {(slotGO != null ? slotGO.name */ }

        // 4. Tạo RawImage con của CharPreviewSlot
        { /* Đang tạo RawImage_CharPreview */ }
        var rawImage = SetupRawImage(slotGO);
        { /* ✓ RawImage: {(rawImage != null ? rawImage.gameObject.name */ }

        // 5. Wire references vào EquipmentCharacterPreview
        var preview = slotGO.GetComponent<EquipmentCharacterPreview>();
        if (preview != null)
        {
            var so = new SerializedObject(preview);
            so.FindProperty("previewCamera")      .objectReferenceValue = cam;
            so.FindProperty("renderTargetImage")  .objectReferenceValue = rawImage;
            so.ApplyModifiedProperties();
            { /* ✓ Wire: EquipmentCharacterPreview ← Camera + RawImage */ }
        }
        else
        {
            { /* Lỗi: EquipmentCharacterPreview component là NULL */ }
        }

        // 6. Wire CharPreviewSlot vào EquipmentPanelUI
        var panelUI = contentEquipment.GetComponent<EquipmentPanelUI>();
        { /* EquipmentPanelUI trên ContentEquipment: {(panelUI != null ? */ }
        if (panelUI != null)
        {
            var so2 = new SerializedObject(panelUI);
            var prop = so2.FindProperty("characterPreview");
            if (prop != null)
            {
                prop.objectReferenceValue = preview;
                so2.ApplyModifiedProperties();
                { /* ✓ EquipmentPanelUI.characterPreview → CharPreviewSlot */ }
            }
            else
            {
                { /* Cảnh báo: Không tìm thấy field 'characterPreview' trong EquipmentPanelUI */ }
            }
        }

        // 7. Đánh dấu scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("Setup Preview ✓",
                "Hoàn thành!\n\n" +
                "Đã tạo:\n" +
                "  • PreviewCamera (ngoài canvas)\n" +
                "  • CharPreviewSlot + EquipmentCharacterPreview\n" +
                "  • RawImage_CharPreview\n\n" +
                "Bước tiếp theo:\n" +
                "1. Tạo asset: Create → DoAn → Player Preview Prefab Config\n" +
                "   Lưu vào: Assets/Resources/ScriptableObjects/\n" +
                "2. Điền prefab cho từng hệ (Fire/Metal/Wood/Water/Earth/Wind)\n" +
                "3. (Tuỳ) Kéo HybridPrefabMap vào field Hybrid Prefab Map\n" +
                "4. Điều chỉnh Preview Scale, vị trí Preview World Position cho đẹp\n" +
                "5. Ctrl+S để lưu scene",
                "OK");

        { /* ══════════ SETUP HOÀN TẤT ══════════ */ }
    }

    // Hàm hỗ trợ dùng nội bộ để tách nhỏ xử lý chính.

    // Tìm ContentEquipment: ưu tiên tìm theo đường dẫn chuẩn,
    // fallback tìm bất kỳ GameObject nào có EquipmentPanelUI.
    private static GameObject FindContentEquipment()
    {
        // Thử đường dẫn chuẩn
        string[] paths =
        {
            "InformationCanvas/CharacterPanel/Window/TabContents/ContentEquipment",
            "InformationCanvas/CharacterPanel/TabContents/ContentEquipment",
            "CharacterPanel/Window/TabContents/ContentEquipment",
        };

        foreach (var p in paths)
        {
            var go = GameObject.Find(p);
            if (go != null) return go;
        }

        // Fallback: tìm bất kỳ GameObject có EquipmentPanelUI
        var panelUI = Object.FindObjectOfType<EquipmentPanelUI>(true);
        if (panelUI != null)
        {
            { /* Cảnh báo: Không tìm theo đường dẫn chuẩn, dùng: {panelUI.gameObject.name} */ }
            return panelUI.gameObject;
        }

        return null;
    }

    // Tạo hoặc tái dùng PreviewCamera trong scene (ngoài Canvas).
    private static Camera SetupPreviewCamera()
    {
        const string CAM_NAME = "EquipPreviewCamera";

        // Tái dùng nếu đã tồn tại
        var existing = GameObject.Find(CAM_NAME);
        if (existing != null)
        {
            { /* EquipPreviewCamera đã tồn tại, tái dùng */ }
            // Đảm bảo camera luôn disabled khi setup (runtime script sẽ bật)
            var existingCam = existing.GetComponent<Camera>();
            if (existingCam != null) existingCam.enabled = false;
            return existingCam;
        }

        // Tạo mới
        var camGO = new GameObject(CAM_NAME);
        Undo.RegisterCreatedObjectUndo(camGO, "Create EquipPreviewCamera");

        var cam = camGO.AddComponent<Camera>();

        // Vị trí nhìn vào vị trí spawn nhân vật (1000, 1, 1000)
        camGO.transform.position = new Vector3(1000f, 1f, 998f);   // nhìn về phía +Z
        camGO.transform.rotation = Quaternion.Euler(0f, 0f, 0f);   // nhìn thẳng

        // Cấu hình Camera
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = new Color(0, 0, 0, 0);              // trong suốt
        cam.cullingMask      = 0;                                   // CHƯA set layer – hướng dẫn user tự set
        cam.orthographic     = true;
        cam.orthographicSize = 1.5f;
        cam.depth            = 5;                                   // cao hơn Main Camera
        cam.nearClipPlane    = 0.1f;
        cam.farClipPlane     = 20f;
        cam.allowHDR         = false;
        cam.allowMSAA        = false;

        // █ QUAN TRỌNG: disabled by default
        // EquipmentCharacterPreview.Awake() / OnEnable() sẽ bật lại sau khi tạo xong RenderTexture
        cam.enabled = false;

        { /* Đã tạo {CAM_NAME} tại vị trí (1000, 1, 998). Camera DISABLED by default.\n */ }
        return cam;
    }

    // Tạo hoặc tái dùng CharPreviewSlot trong ContentEquipment.
    private static GameObject SetupCharPreviewSlot(GameObject parent, Camera cam)
    {
        const string SLOT_NAME = "CharPreviewSlot";

        // Tái dùng nếu đã tồn tại
        var existingT = parent.transform.Find(SLOT_NAME);
        if (existingT != null)
        {
            { /* CharPreviewSlot đã tồn tại, tái dùng */ }
            // Đảm bảo EquipmentCharacterPreview có mặt
            if (existingT.GetComponent<EquipmentCharacterPreview>() == null)
                existingT.gameObject.AddComponent<EquipmentCharacterPreview>();
            return existingT.gameObject;
        }

        // Tạo mới
        var slotGO = new GameObject(SLOT_NAME);
        Undo.RegisterCreatedObjectUndo(slotGO, "Create CharPreviewSlot");
        slotGO.transform.SetParent(parent.transform, false);

        // RectTransform – đặt ở giữa panel, chiều cao đủ để thấy nhân vật
        var rt = slotGO.AddComponent<RectTransform>();
        // Căn giữa theo chiều ngang (có thể chỉnh sau)
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(220f, 0f);    // rộng 220px, cao = fill

        // EquipmentCharacterPreview
        var preview = slotGO.AddComponent<EquipmentCharacterPreview>();
        var so = new SerializedObject(preview);
        so.FindProperty("previewCamera")         .objectReferenceValue = cam;
        // previewScale mặc định 1,1,1
        var scaleP = so.FindProperty("previewScale");
        scaleP.vector3Value = Vector3.one;
        so.FindProperty("initialRotationY")      .floatValue = 180f;
        so.FindProperty("overrideLayer")         .intValue   = -1;  // user set sau
        so.FindProperty("previewWorldPosition")  .vector3Value = new Vector3(1000f, 0f, 1000f);
        so.FindProperty("renderTextureSize")     .vector2IntValue = new Vector2Int(220, 400);
        so.ApplyModifiedProperties();

        { /* Đã tạo CharPreviewSlot với EquipmentCharacterPreview */ }
        return slotGO;
    }

    // Tạo hoặc tái dùng RawImage con của CharPreviewSlot.
    private static RawImage SetupRawImage(GameObject slotParent)
    {
        const string RI_NAME = "RawImage_CharPreview";

        // Tái dùng
        var existingT = slotParent.transform.Find(RI_NAME);
        if (existingT != null)
        {
            { /* RawImage_CharPreview đã tồn tại, tái dùng */ }
            return existingT.GetComponent<RawImage>();
        }

        // Tạo mới
        var riGO = new GameObject(RI_NAME);
        Undo.RegisterCreatedObjectUndo(riGO, "Create RawImage_CharPreview");
        riGO.transform.SetParent(slotParent.transform, false);

        var rt = riGO.AddComponent<RectTransform>();
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.offsetMin        = Vector2.zero;
        rt.offsetMax        = Vector2.zero;

        var rawImage = riGO.AddComponent<RawImage>();
        // Transparent by default - chỉ hiển thị khi RenderTexture được gán lúc runtime
        rawImage.color = new Color(1f, 1f, 1f, 0f);

        // Đặt về dưới cùng trong sibling order để nằm dưới các slot
        riGO.transform.SetAsFirstSibling();

        { /* Đã tạo RawImage_CharPreview */ }
        return rawImage;
    }

    private static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
#endif
