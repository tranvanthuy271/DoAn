using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach on root of ShopItemCell prefab (110x110 grid cell).
/// Drag child objects into these fields in Inspector instead of using transform.Find().
/// btnBuy = the root Button -- clicking anywhere on the cell triggers purchase.
/// </summary>
public class ShopItemRowUI : MonoBehaviour
{
    [Header("Icons")]
    [SerializeField] public Image        itemIcon;   // item sprite
    [SerializeField] public Image        coinIcon;   // currency icon next to price

    [Header("Texts")]
    [SerializeField] public TMP_Text     itemName;
    [SerializeField] public TMP_Text     price;
    [SerializeField] public TMP_Text     stock;      // optional, can leave unassigned

    [Header("Button")]
    [SerializeField] public Button       btnBuy;     // root button -- whole cell is clickable

    private Vector2 itemIconMaxSize = new Vector2(100f, 100f);

    private void Awake()
    {
        EnsureVisualsConfigured();
    }

    public void EnsureVisualsConfigured()
    {
        if (itemIcon != null)
        {
            Vector2 currentSize = itemIcon.rectTransform.sizeDelta;
            if (currentSize.x > 0f && currentSize.y > 0f)
            {
                itemIconMaxSize = currentSize;
            }

            itemIcon.preserveAspect = true;
        }

        if (coinIcon != null)
        {
            coinIcon.preserveAspect = true;
        }

        UIRuntimeAssetHelper.ApplyNotoSans(itemName, price, stock);
    }

    public void SetItemIcon(Sprite sprite)
    {
        EnsureVisualsConfigured();
        UIRuntimeAssetHelper.SetSpriteWithNativeFit(itemIcon, sprite, itemIconMaxSize);
    }
}
