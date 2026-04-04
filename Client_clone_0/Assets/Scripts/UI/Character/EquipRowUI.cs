using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// EquipRowUI â€“ Má»™t dÃ²ng trang bá»‹ trong tab NhÃ¢n váº­t.
///
/// Cáº¥u trÃºc GameObject gá»£i Ã½ (HorizontalLayoutGroup trÃªn root):
/// â”Œâ”€ EquipRow   [Image bg + HLG + LayoutElement(prefH=45)]
/// â”‚   â”œâ”€ TxtSlot      [TMP_Text] â€“ "VÅ© khÃ­"          (fixed 70px)
/// â”‚   â”œâ”€ TxtItemName  [TMP_Text] â€“ "Kiáº¿m Lá»­a +3"     (flex)
/// â”‚   â””â”€ BtnUpgrade   [Button]   â€“ "NÃ¢ng cáº¥p"         (hides when empty)
/// </summary>
public class EquipRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text txtSlot;
    [SerializeField] private TMP_Text txtItemName;
    [SerializeField] private Button   btnUpgrade;

    private int               _playerId;
    private string            _slotKey;
    private EquipmentItemDto  _item;
    private Action            _onUpgraded;

    // â”€â”€ Basic row (from legacy EquipmentData, no upgrade level) â”€â”€â”€
    public void SetData(string slotLabel, string itemName, int upgradeLevel,
                        int playerId, Action onUpgraded)
    {
        _playerId = playerId;
        _slotKey  = null;
        _item     = null;
        _onUpgraded = onUpgraded;
        RefreshUI(slotLabel, itemName, upgradeLevel, canUpgrade: false);
    }

    // â”€â”€ Full row (from PlayerEquipmentDto, with slotKey for UpgradePanel) â”€â”€
    public void SetData(string slotLabel, string itemName, int upgradeLevel,
                        int playerId, string slotKey, Action onUpgraded)
        => SetData(slotLabel, itemName, upgradeLevel, playerId, slotKey, item: null, onUpgraded);

    public void SetData(string slotLabel, string itemName, int upgradeLevel,
                        int playerId, string slotKey, EquipmentItemDto item, Action onUpgraded)
    {
        _playerId   = playerId;
        _slotKey    = slotKey;
        _item       = item;
        _onUpgraded = onUpgraded;

        bool canUpgrade = item != null && !string.IsNullOrEmpty(slotKey);
        RefreshUI(slotLabel, itemName, upgradeLevel, canUpgrade);

        btnUpgrade?.onClick.RemoveAllListeners();
        if (canUpgrade)
            btnUpgrade?.onClick.AddListener(OnClickUpgrade);
    }

    private void RefreshUI(string slotLabel, string itemName, int upgradeLevel, bool canUpgrade)
    {
        if (txtSlot != null)
            txtSlot.text = slotLabel;

        if (txtItemName != null)
        {
            if (string.IsNullOrEmpty(itemName))
                txtItemName.text = "<color=#888888>— trống —</color>";
            else
                txtItemName.text = upgradeLevel > 0
                    ? $"{itemName} <color=#FFD700>+{upgradeLevel}</color>"
                    : itemName;
        }

        if (btnUpgrade != null)
            btnUpgrade.interactable = canUpgrade;
    }

    private void OnClickUpgrade()
    {
        if (_item == null || string.IsNullOrEmpty(_slotKey)) return;
        if (btnUpgrade != null) btnUpgrade.interactable = false;

        // Fetch inventory then open UpgradePanel
        if (APIClient.Instance != null)
        {
            APIClient.Instance.GetPlayerInventory(
                _playerId,
                onSuccess: inv =>
                {
                    // Convert InventoryItem[] â†’ InventorySlotDto[]
                    var slots = ConvertInventory(inv);
                    OpenUpgradePanel(slots);
                },
                onError: _ => OpenUpgradePanel(new InventorySlotDto[0])
            );
        }
        else
        {
            OpenUpgradePanel(new InventorySlotDto[0]);
        }
    }

    private void OpenUpgradePanel(InventorySlotDto[] inventory)
    {
        if (btnUpgrade != null) btnUpgrade.interactable = _item != null;

        var panel = UpgradePanel.Instance;
        if (panel == null)
        {
            Debug.LogWarning("[EquipRowUI] Không tìm thấy UpgradePanel.Instance trong scene.");
            _onUpgraded?.Invoke();
            return;
        }

        panel.OpenForEquipped(_item, _slotKey, inventory);
    }

    private static InventorySlotDto[] ConvertInventory(InventoryItem[] items)
    {
        if (items == null) return new InventorySlotDto[0];
        var result = new InventorySlotDto[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            result[i] = new InventorySlotDto
            {
                slotIndex    = items[i].slotIndex > 0 ? items[i].slotIndex : items[i].slot_index,
                id           = items[i].itemTemplateId > 0 ? items[i].itemTemplateId : items[i].item_id,
                quantity     = items[i].quantity,
                itemCode     = items[i].itemCode,
                iconId       = items[i].iconId,
                isEquipped   = items[i].isEquipped,
                isLocked     = items[i].isLocked,
                upgradeLevel = items[i].upgradeLevel,
                strOptions   = items[i].strOptions,
            };
        }
        return result;
    }
}
