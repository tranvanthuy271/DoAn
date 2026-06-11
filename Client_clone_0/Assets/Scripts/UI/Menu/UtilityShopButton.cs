using UnityEngine;
using UnityEngine.UI;

// Gắn vào bất kỳ Button nào trong scene để mở Cửa Hàng Tiện Ích (NPC 999).
// Không cần UtilityDrawerAutoInstaller.
[RequireComponent(typeof(Button))]
public class UtilityShopButton : MonoBehaviour
{
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(OpenShop);
    }

    private void OpenShop()
    {
        var ui = NpcMenuUI.GetOrFind();
        if (ui == null)
        {
            { /* Cảnh báo: Không tìm thấy NpcMenuUI trong scene */ }
            return;
        }
        ui.OpenUtilityMode();
    }
}
