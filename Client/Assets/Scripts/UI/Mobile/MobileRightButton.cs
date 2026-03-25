using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Nút di chuyển sang PHẢI cho mobile.
/// Attach vào Button "BtnRight" trên Canvas.
/// Giữ ngón tay → nhân vật đi phải. Thả ra → dừng.
/// </summary>
public class MobileRightButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private void Awake()
    {
        bool show = Application.isMobilePlatform || Application.isEditor;
        gameObject.SetActive(show);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("[MobileRightButton] DOWN");
        InputManager.Instance.SetMobileAxis(1f, 0f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("[MobileRightButton] UP");
        InputManager.Instance.SetMobileAxis(0f, 0f);
    }
}
