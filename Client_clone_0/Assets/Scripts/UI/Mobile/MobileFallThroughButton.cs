using UnityEngine;
using UnityEngine.EventSystems;

// Nút rơi xuống platform (mobile) — biểu tượng mũi tên xuống ↓.
// Attach vào Button RectTransform trong HUD mobile.
// Nhấn 1 lần → kích hoạt fall-through nếu đang đứng trên one-way platform.
public class MobileFallThroughButton : MonoBehaviour, IPointerDownHandler
{
    private void Awake()
    {
        bool show = Application.isMobilePlatform || Application.isEditor;
        gameObject.SetActive(show);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        InputManager.Instance.SetMobileFallThrough();
    }
}
