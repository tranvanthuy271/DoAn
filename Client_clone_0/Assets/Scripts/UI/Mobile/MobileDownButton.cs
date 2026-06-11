using UnityEngine;
using UnityEngine.EventSystems;

// Nút di chuyển XUỐNG cho mobile (crouch / nhìn xuống).
// Attach vào Button "BtnDown" trên Canvas.
// Giữ ngón tay → vertical = -1. Thả ra → dừng.
public class MobileDownButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private void Awake()
    {
        bool show = Application.isMobilePlatform || Application.isEditor;
        gameObject.SetActive(show);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("[MobileDownButton] PRESSED");
        InputManager.Instance.SetMobileAxis(0f, -1f);
        InputManager.Instance.SetMobileFallThrough();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("[MobileDownButton] RELEASED");
        InputManager.Instance.SetMobileAxis(0f, 0f);
    }
}
