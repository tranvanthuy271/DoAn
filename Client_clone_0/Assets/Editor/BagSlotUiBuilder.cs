#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// Tools/DoAn — Tạo và dây nối ItemBg overlay cho túi đồ.
// Hai menu item:
// 1. "Rebuild Inventory Slot Prefab (Add ItemBg + ItemIcon)"
// → Mở prefab InventorySlot, thêm con ItemBg (nền tối 90%) và ItemIcon (icon item),
// kết nối vào InventorySlotUI.iconImage / itemBgImage.
// 2. "Add ItemBg to Bag Quick Slots (GameScene)"
// → Mở GameScene, thêm con ItemBg vào BagSlot0/BagSlot1/BagSlot3,
// kết nối vào ItemUseHandler.bagSlotItemBgs[].
public static class BagSlotUiBuilder
{
    private const string GameScenePath    = "Assets/Scenes/GameScene.unity";
    private const string InvSlotPrefabPath = "Assets/Prefabs/UI/Thông tin/InventorySlot.prefab";
    private const string ItemBgName       = "ItemBg";
    private const string ItemIconName     = "ItemIcon";

    // Màu nền tối: #212121 @ ~88% alpha
    private static readonly Color ItemBgColor = new Color(0.13f, 0.13f, 0.13f, 0.88f);

    //  1) Rebuild InventorySlot prefab
    [MenuItem("Tools/DoAn/Rebuild Inventory Slot Prefab (Add ItemBg + ItemIcon)")]
    public static void RebuildInventorySlotPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(InvSlotPrefabPath);
        if (root == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tải được prefab InventorySlot tại:\n" + InvSlotPrefabPath, "OK");
            return;
        }

        try
        {
            // 1. Thêm ItemBg con tại sibling 0 (nền tối 90% kích thước slot)
            Image itemBgImg = EnsureStretchImageChild(
                root.transform, ItemBgName, siblingIndex: 0,
                sizeDelta: new Vector2(-10f, -10f),   // -5px mỗi cạnh = 90% của 100x100
                color: ItemBgColor,
                raycastTarget: false,
                startEnabled: false);

            // 2. Thêm ItemIcon con tại sibling 1 (hiển thị icon item)
            Image itemIconImg = EnsureCenterImageChild(
                root.transform, ItemIconName, siblingIndex: 1,
                sizeDelta: new Vector2(80f, 80f),
                color: Color.white,
                raycastTarget: false,
                preserveAspect: true,
                startEnabled: false);

            // 3. Kết nối InventorySlotUI serialized fields
            var slotUI = root.GetComponent<InventorySlotUI>();
            if (slotUI != null)
            {
                SerializedObject so = new SerializedObject(slotUI);

                SerializedProperty iconProp   = so.FindProperty("iconImage");
                SerializedProperty itemBgProp = so.FindProperty("itemBgImage");

                if (iconProp != null)
                    iconProp.objectReferenceValue = itemIconImg;
                else
                    { /* Cảnh báo: Không tìm thấy property 'iconImage' trên InventorySlotUI */ }

                if (itemBgProp != null)
                    itemBgProp.objectReferenceValue = itemBgImg;
                else
                    { /* Cảnh báo: Không tìm thấy property 'itemBgImage' trên InventorySlotUI */ }

                so.ApplyModifiedProperties();
            }
            else
            {
                { /* Cảnh báo: Không tìm thấy component InventorySlotUI trên root prefab */ }
            }

            PrefabUtility.SaveAsPrefabAsset(root, InvSlotPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog(
            "Thành công",
            "Đã cập nhật InventorySlot.prefab:\n" +
            "• Thêm con ItemBg  (nền tối 90%, tắt mặc định)\n" +
            "• Thêm con ItemIcon (icon item, tắt mặc định)\n" +
            "• Kết nối iconImage → ItemIcon\n" +
            "• Kết nối itemBgImage → ItemBg\n\n" +
            "Nhớ chạy menu item thứ 2 để cập nhật BagSlot HUD trong GameScene.",
            "OK");
    }

    //  2) Add ItemBg overlay to BagSlot0/1/3 in GameScene
    [MenuItem("Tools/DoAn/Add ItemBg to Bag Quick Slots (GameScene)")]
    public static void AddItemBgToBagSlots()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

        // Tên các BagSlot và thứ tự index tương ứng với bagQuickSlotIcons / bagSlotItemBgs
        string[] bagSlotNames = { "BagSlot0", "BagSlot1", "BagSlot3" };
        Image[]  itemBgImages = new Image[bagSlotNames.Length];

        for (int i = 0; i < bagSlotNames.Length; i++)
        {
            GameObject bagSlotGO = GameObject.Find(bagSlotNames[i]);
            if (bagSlotGO == null)
            {
                { /* Cảnh báo: Không tìm thấy '{bagSlotNames[i]}' trong scene  bỏ qua */ }
                continue;
            }

            Transform t = bagSlotGO.transform;

            // Thêm ItemBg tại sibling 0
            Image itemBg = EnsureStretchImageChild(
                t, ItemBgName, siblingIndex: 0,
                sizeDelta: new Vector2(-10f, -10f),
                color: ItemBgColor,
                raycastTarget: false,
                startEnabled: false);

            itemBgImages[i] = itemBg;

            // Bật preserveAspect trên icon Image hiện tại (bây giờ ở sibling 1)
            // để sprite item hiển thị đúng tỷ lệ, để lộ ItemBg ở viền
            for (int j = 0; j < t.childCount; j++)
            {
                Transform child = t.GetChild(j);
                if (child.name == ItemBgName) continue;   // bỏ qua overlay vừa thêm

                Image existingIcon = child.GetComponent<Image>();
                if (existingIcon != null)
                {
                    existingIcon.preserveAspect = true;
                    EditorUtility.SetDirty(existingIcon);
                    break;
                }
            }

            EditorUtility.SetDirty(bagSlotGO);
        }

        // Kết nối ItemUseHandler.bagSlotItemBgs[]
        GameObject invMgrGO = GameObject.Find("InventoryManager");
        if (invMgrGO != null)
        {
            ItemUseHandler handler = invMgrGO.GetComponent<ItemUseHandler>();
            if (handler != null)
            {
                SerializedObject so   = new SerializedObject(handler);
                SerializedProperty prop = so.FindProperty("bagSlotItemBgs");

                if (prop != null)
                {
                    prop.arraySize = itemBgImages.Length;
                    for (int i = 0; i < itemBgImages.Length; i++)
                        prop.GetArrayElementAtIndex(i).objectReferenceValue = itemBgImages[i];
                    so.ApplyModifiedProperties();
                }
                else
                {
                    { /* Cảnh báo: Không tìm thấy property 'bagSlotItemBgs' trên ItemUseHandler.\n */ }
                }
            }
            else
            {
                { /* Cảnh báo: Không tìm thấy component ItemUseHandler trên 'InventoryManager' */ }
            }
        }
        else
        {
            { /* Cảnh báo: Không tìm thấy GameObject 'InventoryManager' trong scene */ }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "Thành công",
            "Đã thêm ItemBg overlay vào BagSlot0, BagSlot1, BagSlot3:\n" +
            "• ItemBg (nền tối 90%, tắt mặc định)\n" +
            "• preserveAspect = true trên icon Image hiện tại\n" +
            "• Kết nối bagSlotItemBgs[] vào ItemUseHandler\n\n" +
            "Mở lại GameScene trong Editor để kiểm tra.",
            "OK");
    }

    // Hàm hỗ trợ dùng nội bộ để tách nhỏ xử lý chính.

    // Tạo hoặc tìm child có kiểu stretch anchor (co giãn theo parent trừ padding).
    // anchorMin=(0,0) anchorMax=(1,1), sizeDelta = -2*padding
    private static Image EnsureStretchImageChild(
        Transform parent, string name, int siblingIndex,
        Vector2 sizeDelta, Color color, bool raycastTarget, bool startEnabled)
    {
        Transform existing = parent.Find(name);
        GameObject go;

        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            go = new GameObject(name,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
        }

        go.SetActive(startEnabled);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = sizeDelta;

        Image img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.color         = color;
        img.sprite        = null;
        img.type          = Image.Type.Simple;
        img.raycastTarget = raycastTarget;
        img.enabled       = startEnabled;

        go.transform.SetSiblingIndex(siblingIndex);

        EditorUtility.SetDirty(go);
        return img;
    }

    // Tạo hoặc tìm child có kiểu center anchor (kích thước cố định, căn giữa).
    private static Image EnsureCenterImageChild(
        Transform parent, string name, int siblingIndex,
        Vector2 sizeDelta, Color color, bool raycastTarget, bool preserveAspect, bool startEnabled)
    {
        Transform existing = parent.Find(name);
        GameObject go;

        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            go = new GameObject(name,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
        }

        // ItemIcon phải active (enabled/disabled qua Image.enabled)
        go.SetActive(true);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = sizeDelta;

        Image img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.color          = color;
        img.sprite         = null;
        img.type           = Image.Type.Simple;
        img.raycastTarget  = raycastTarget;
        img.preserveAspect = preserveAspect;
        img.enabled        = startEnabled;

        go.transform.SetSiblingIndex(siblingIndex);

        EditorUtility.SetDirty(go);
        return img;
    }
}
#endif
