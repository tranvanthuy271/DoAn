using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// EquipmentSelectionForUpgrade – Tab "Trang Bị" trong cửa sổ Thợ Rèn.
///
/// Hiển thị 2 phần:
///   [1] Trang bị đang mặc   → mỗi ô: icon + tên + "+X" + [Nâng Cấp]
///   [2] Trang bị trong túi  → tương tự (type 0~5)
///
/// Khi nhấn [Nâng Cấp]:
///   → UpgradePanel.Instance.SetChosenEquipItem(item, slotKey, fromInventory, inventory)
///   → BlacksmithTabPanel.SwitchTab(0) được gọi bên trong SetChosenEquipItem
///
/// ══════════════════════════════════════════════════════════════════
/// HIERARCHY GỢI Ý:
///   PanelTrangBi                            [EquipmentSelectionForUpgrade.cs]
///   ├─ HeaderEquipped    TMP_Text
///   ├─ ContainerEquipped ScrollRect → Content (VerticalLayoutGroup)
///   ├─ HeaderInventory   TMP_Text
///   └─ ContainerInventory ScrollRect → Content (VerticalLayoutGroup)
///
///   Prefab EquipUpgradeRow:
///     EquipUpgradeRow            [HorizontalLayoutGroup]
///     ├─ IconImage               [Image]
///     ├─ NameText                [TMP_Text]
///     ├─ LevelText               [TMP_Text]  "+3"
///     └─ UpgradeButton           [Button]    "Nâng Cấp"
/// ══════════════════════════════════════════════════════════════════
/// </summary>
public class EquipmentSelectionForUpgrade : MonoBehaviour
{
    [Header("Trang bị đang mặc")]
    [SerializeField] private GameObject headerEquipped;
    [SerializeField] private Transform  containerEquipped;

    [Header("Trang bị trong túi (type 0~5)")]
    [SerializeField] private GameObject headerInventory;
    [SerializeField] private Transform  containerInventory;

    [Header("Prefab row")]
    [Tooltip("Prefab 1 hàng hiển thị trang bị. Cần: Image icon, TMP_Text name, TMP_Text level, Button upgrade.")]
    [SerializeField] private GameObject equipUpgradeRowPrefab;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void OnEnable()
    {
        Refresh();
    }

    // ── Public API ────────────────────────────────────────────────

    public void Show()
    {
        gameObject.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // ── Core ──────────────────────────────────────────────────────

    private void Refresh()
    {
        ClearContainer(containerEquipped);
        ClearContainer(containerInventory);

        // --- Phần 1: Trang bị đang mặc (load async từ API để có đủ strOptions + upgradeLevel) ---
        int playerId = GameManager.Instance?.currentPlayerData?.player_id ?? 0;
        if (headerEquipped) headerEquipped.SetActive(false);
        if (playerId > 0 && APIClient.Instance != null)
            APIClient.Instance.GetPlayerEquipment(playerId, OnEquipmentLoaded);

        // --- Phần 2: Trang bị trong túi ---
        var invUI = FindObjectOfType<InventoryUI>(true);
        var inventory = invUI?.CurrentSlots;
        bool hasInvEquip = false;

        if (inventory != null)
        {
            foreach (var slot in inventory)
            {
                if (slot == null || slot.quantity <= 0) continue;
                var tmpl = ItemTemplateManager.Instance?.GetItemTemplate(slot.id);
                if (tmpl == null) continue;
                if (tmpl.type < 0 || tmpl.type > 5) continue; // chỉ trang bị (type 0~5)

                SpawnInventoryRow(slot, inventory);
                hasInvEquip = true;
            }
        }

        if (headerInventory) headerInventory.SetActive(hasInvEquip);
    }

    private void OnEquipmentLoaded(PlayerEquipmentDto dto)
    {
        ClearContainer(containerEquipped);
        bool hasEquipped = false;
        var slots = new (EquipmentItemDto item, string key)[]
        {
            (dto.weapon,    "weapon"),
            (dto.helmet,    "helmet"),
            (dto.armor,     "armor"),
            (dto.pants,     "pants"),
            (dto.boots,     "boots"),
            (dto.accessory, "accessory"),
        };
        foreach (var s in slots)
        {
            if (s.item == null || s.item.id <= 0) continue;
            SpawnEquippedRow(s.item, s.key);
            hasEquipped = true;
        }
        if (headerEquipped) headerEquipped.SetActive(hasEquipped);
    }

    // ── Row builders ──────────────────────────────────────────────

    private void SpawnEquippedRow(EquipmentItemDto item, string slotKey)
    {
        if (equipUpgradeRowPrefab == null || containerEquipped == null) return;

        var row = Instantiate(equipUpgradeRowPrefab, containerEquipped);
        var tmpl = ItemTemplateManager.Instance?.GetItemTemplate(item.id);

        SetRowIcon (row, tmpl?.idIcon.ToString());
        SetRowName (row, tmpl != null ? tmpl.name : $"Item #{item.id}");
        SetRowLevel(row, item.upgradeLevel);

        var btn = row.GetComponentInChildren<Button>();
        if (btn != null)
        {
            var capturedItem = item;
            var capturedKey  = slotKey;
            btn.onClick.AddListener(() =>
            {
                var invUI  = FindObjectOfType<InventoryUI>(true);
                UpgradePanel.Instance?.SetChosenEquipItem(
                    capturedItem, capturedKey,
                    fromInventory: false,
                    inventory: invUI?.CurrentSlots);
            });
        }
    }

    private void SpawnInventoryRow(InventorySlotDto slot, InventorySlotDto[] fullInventory)
    {
        if (equipUpgradeRowPrefab == null || containerInventory == null) return;

        var row  = Instantiate(equipUpgradeRowPrefab, containerInventory);
        var tmpl = ItemTemplateManager.Instance?.GetItemTemplate(slot.id);

        SetRowIcon (row, slot.iconId);
        SetRowName (row, tmpl != null ? tmpl.name : slot.itemCode ?? $"Item #{slot.id}");
        SetRowLevel(row, slot.upgradeLevel);

        var btn = row.GetComponentInChildren<Button>();
        if (btn != null)
        {
            var capturedSlot = slot;
            btn.onClick.AddListener(() =>
            {
                var dto = new EquipmentItemDto
                {
                    id           = capturedSlot.id,
                    upgradeLevel = capturedSlot.upgradeLevel,
                    strOptions   = capturedSlot.strOptions
                };
                UpgradePanel.Instance?.SetChosenEquipItem(
                    dto,
                    slotKey:       capturedSlot.slotIndex.ToString(),
                    fromInventory: true,
                    inventory:     fullInventory);
            });
        }
    }

    // ── Helpers ───────────────────────────────────────────────────

    private static void ClearContainer(Transform container)
    {
        if (container == null) return;
        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);
    }

    private static void SetRowIcon(GameObject row, string iconId)
    {
        var img = row.GetComponentInChildren<Image>();
        if (img == null || string.IsNullOrEmpty(iconId) || IconDatabase.Instance == null) return;
        var sp = IconDatabase.Instance.GetIcon(iconId);
        img.sprite  = sp;
        img.enabled = sp != null;
    }

    private static void SetRowName(GameObject row, string name)
    {
        var texts = row.GetComponentsInChildren<TMP_Text>();
        // index 0 = tên, index 1 = level
        if (texts.Length > 0) texts[0].text = name;
    }

    private static void SetRowLevel(GameObject row, int level)
    {
        var texts = row.GetComponentsInChildren<TMP_Text>();
        if (texts.Length > 1) texts[1].text = level > 0 ? $"+{level}" : "+0";
    }
}
