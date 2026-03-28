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
}
