using UnityEngine;

/// <summary>
/// ItemData - ScriptableObject để định nghĩa các loại item trong game
/// Tạo asset trong Unity Editor: Right-click > Create > Item > ItemData
/// </summary>
[CreateAssetMenu(fileName = "New Item", menuName = "Item/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Tên hiển thị của item")]
    public string itemName = "New Item";
    
    [Tooltip("Mô tả của item")]
    [TextArea(3, 5)]
    public string description = "Item description";
    
    [Tooltip("Icon của item (Sprite)")]
    public Sprite icon;
    
    [Header("Item Properties")]
    [Tooltip("ID duy nhất của item (dùng để sync trên network)")]
    public int itemID = 0;
    
    [Tooltip("Loại item (Weapon, Consumable, Material, etc.)")]
    public ItemType itemType = ItemType.Material;
    
    [Tooltip("Có thể stack được không (ví dụ: đá, gỗ)")]
    public bool stackable = true;
    
    [Tooltip("Số lượng tối đa có thể stack (nếu stackable = true)")]
    public int maxStack = 99;
    
    [Header("Drop Settings")]
    [Tooltip("Prefab của item khi drop trên ground")]
    public GameObject dropPrefab;
    
    [Tooltip("Prefab của item khi hiển thị trong inventory UI")]
    public GameObject inventoryUIPrefab;
    
    [Header("Effects (Optional)")]
    [Tooltip("Giá trị sử dụng (ví dụ: heal amount, damage boost)")]
    public int value = 0;
    
    [Tooltip("Có thể sử dụng được không")]
    public bool usable = false;
}

/// <summary>
/// Enum định nghĩa các loại item
/// </summary>
public enum ItemType
{
    Weapon,      // Vũ khí
    Consumable,  // Đồ dùng (potion, food)
    Material,    // Nguyên liệu (đá, gỗ, kim loại)
    Armor,       // Giáp
    Accessory,   // Phụ kiện
    Quest,       // Item nhiệm vụ
    Other        // Khác
}
