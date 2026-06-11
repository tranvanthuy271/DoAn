using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using System.IO;

// Creates ShopItemCell prefab (110x110 square) for use in a GridLayoutGroup shop.
// Cell layout:
// Root (Button + Image background, 110x110, VerticalLayoutGroup)
// IconRow  (HLG centered, h=60)
// ItemIcon  (Image 52x52, Preserve Aspect)
// PriceRow (HLG centered, h=18)
// CoinIcon  (Image 14x14, gold placeholder)
// Price     (TMP_Text, gold color)
// ItemName    (TMP_Text, h=22, Ellipsis)
// Menu: Tools -> NPC Shop -> Create ShopItemRow Prefab
public static class CreateShopItemRowPrefab
{
    private const string PREFAB_PATH = "Assets/Prefabs/UI/ShopItemRow.prefab";
    private const string NOTO_SANS_ASSET_PATH = "Assets/TextMesh Pro/Resources/Fonts & Materials/NotoSans-Regular SDF.asset";

    [MenuItem("Tools/NPC Shop/Create ShopItemRow Prefab")]
    public static void Create()
    {
        Directory.CreateDirectory(Application.dataPath + "/Prefabs/UI");
        AssetDatabase.Refresh();

        // Root
        var root   = new GameObject("ShopItemRow");
        var rootRT = root.AddComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(110, 110);

        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.20f, 0.12f, 0.06f, 0.90f);

        var rootBtn    = root.AddComponent<Button>();
        var btnColors  = rootBtn.colors;
        btnColors.normalColor      = Color.white;
        btnColors.highlightedColor = new Color(1.15f, 1.05f, 0.85f, 1f);
        btnColors.pressedColor     = new Color(0.80f, 0.75f, 0.60f, 1f);
        btnColors.disabledColor    = new Color(0.55f, 0.55f, 0.55f, 1f);
        rootBtn.colors = btnColors;

        var vlg = root.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = false;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = 2f;
        vlg.padding = new RectOffset(4, 4, 4, 2);

        // IconRow
        var iconRowGO  = new GameObject("IconRow");
        iconRowGO.transform.SetParent(root.transform, false);
        iconRowGO.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 60);

        var iconRowHLG = iconRowGO.AddComponent<HorizontalLayoutGroup>();
        iconRowHLG.childAlignment        = TextAnchor.MiddleCenter;
        iconRowHLG.childForceExpandWidth  = false;
        iconRowHLG.childForceExpandHeight = false;

        var iconRowLE = iconRowGO.AddComponent<LayoutElement>();
        iconRowLE.preferredHeight = 60;
        iconRowLE.flexibleHeight  = 0;

        // ItemIcon
        var iconGO  = new GameObject("ItemIcon");
        iconGO.transform.SetParent(iconRowGO.transform, false);
        iconGO.AddComponent<RectTransform>().sizeDelta = new Vector2(52, 52);

        var iconImg = iconGO.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget  = false;

        var iconLE = iconGO.AddComponent<LayoutElement>();
        iconLE.preferredWidth  = 52;
        iconLE.preferredHeight = 52;

        // PriceRow
        var priceRowGO = new GameObject("PriceRow");
        priceRowGO.transform.SetParent(root.transform, false);
        priceRowGO.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 18);

        var priceRowHLG = priceRowGO.AddComponent<HorizontalLayoutGroup>();
        priceRowHLG.childAlignment        = TextAnchor.MiddleCenter;
        priceRowHLG.childForceExpandWidth  = false;
        priceRowHLG.childForceExpandHeight = false;
        priceRowHLG.spacing = 2f;

        var priceRowLE = priceRowGO.AddComponent<LayoutElement>();
        priceRowLE.preferredHeight = 18;
        priceRowLE.flexibleHeight  = 0;

        // CoinIcon
        var coinGO = new GameObject("CoinIcon");
        coinGO.transform.SetParent(priceRowGO.transform, false);
        coinGO.AddComponent<RectTransform>().sizeDelta = new Vector2(14, 14);

        var coinImg = coinGO.AddComponent<Image>();
        coinImg.color = new Color(1f, 0.84f, 0f, 1f);
        coinImg.raycastTarget = false;

        var coinLE = coinGO.AddComponent<LayoutElement>();
        coinLE.preferredWidth  = 14;
        coinLE.preferredHeight = 14;

        // Price text
        var priceTxtGO = new GameObject("Price");
        priceTxtGO.transform.SetParent(priceRowGO.transform, false);
        priceTxtGO.AddComponent<RectTransform>().sizeDelta = new Vector2(64, 18);

        var priceTmp = priceTxtGO.AddComponent<TextMeshProUGUI>();
        priceTmp.text          = "100";
        priceTmp.fontSize      = 12;
        priceTmp.color         = new Color(1f, 0.84f, 0f, 1f);
        priceTmp.alignment     = TextAlignmentOptions.MidlineLeft;
        priceTmp.raycastTarget = false;

        var priceLE = priceTxtGO.AddComponent<LayoutElement>();
        priceLE.preferredWidth  = 64;
        priceLE.preferredHeight = 18;

        // ItemName
        var nameGO = new GameObject("ItemName");
        nameGO.transform.SetParent(root.transform, false);
        nameGO.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 22);

        var nameTmp = nameGO.AddComponent<TextMeshProUGUI>();
        nameTmp.text               = "Item Name";
        nameTmp.fontSize           = 11;
        nameTmp.color              = Color.white;
        nameTmp.alignment          = TextAlignmentOptions.Midline;
        nameTmp.overflowMode       = TextOverflowModes.Ellipsis;
        nameTmp.enableWordWrapping = false;
        nameTmp.raycastTarget      = false;

        var nameLE = nameGO.AddComponent<LayoutElement>();
        nameLE.preferredHeight = 22;
        nameLE.flexibleHeight  = 0;

        var notoSans = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NOTO_SANS_ASSET_PATH);
        if (notoSans != null)
        {
            priceTmp.font = notoSans;
            priceTmp.fontSharedMaterial = notoSans.material;
            nameTmp.font = notoSans;
            nameTmp.fontSharedMaterial = notoSans.material;
        }

        // ShopItemRowUI -- wire all references
        var rowUI = root.AddComponent<ShopItemRowUI>();
        var so    = new SerializedObject(rowUI);
        so.FindProperty("itemIcon").objectReferenceValue = iconImg;
        so.FindProperty("coinIcon").objectReferenceValue = coinImg;
        so.FindProperty("itemName").objectReferenceValue = nameTmp;
        so.FindProperty("price").objectReferenceValue    = priceTmp;
        so.FindProperty("btnBuy").objectReferenceValue   = rootBtn;  // whole cell = button
        so.ApplyModifiedPropertiesWithoutUndo();

        // Save prefab
        bool overwrite = true;
        if (File.Exists(Application.dataPath + "/../" + PREFAB_PATH))
        {
            overwrite = EditorUtility.DisplayDialog(
                "ShopItemRow exists",
                $"Prefab at {PREFAB_PATH} already exists. Overwrite?",
                "Overwrite", "Cancel");
        }

        if (overwrite)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            Debug.Log($"[CreateShopItemRowPrefab] Created: {PREFAB_PATH}");
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }

        Object.DestroyImmediate(root);
    }
}