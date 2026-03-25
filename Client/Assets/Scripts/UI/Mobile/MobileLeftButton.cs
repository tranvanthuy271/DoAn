using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Nút di chuyển sang TRÁI cho mobile.
/// Attach vào Button "BtnLeft" trên Canvas.
/// Giữ ngón tay → nhân vật đi trái. Thả ra → dừng.
/// </summary>
public class MobileLeftButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private void Awake()
    {
        bool show = Application.isMobilePlatform || Application.isEditor;
        gameObject.SetActive(show);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("[MobileLeftButton] DOWN");
        InputManager.Instance.SetMobileAxis(-1f, 0f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("[MobileLeftButton] UP");
        InputManager.Instance.SetMobileAxis(0f, 0f);
    }
}
