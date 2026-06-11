using UnityEngine;
using UnityEngine.EventSystems;

// Nút di chuyển sang PHẢI cho mobile.
// Attach vào Button "BtnRight" trên Canvas.
// Giữ ngón tay → nhân vật đi phải. Thả ra → dừng.
public class MobileRightButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private void Awake()
    {
        bool show = Application.isMobilePlatform || Application.isEditor;
        gameObject.SetActive(show);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        { /* PRESSED */ }
        InputManager.Instance.SetMobileAxis(1f, 0f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        { /* RELEASED */ }
        InputManager.Instance.SetMobileAxis(0f, 0f);
    }
}
