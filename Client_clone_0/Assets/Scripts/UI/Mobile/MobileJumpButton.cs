using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Mobile Jump button.
/// Attach to the Jump Button RectTransform.
/// Press → jump triggered; release → jump held cleared.
/// </summary>
public class MobileJumpButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private void Awake()
    {
        bool show = Application.isMobilePlatform || Application.isEditor;
        gameObject.SetActive(show);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("[MobileJumpButton] PRESSED");
        InputManager.Instance.SetMobileJump(pressed: true, held: true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("[MobileJumpButton] RELEASED");
        InputManager.Instance.SetMobileJump(pressed: false, held: false);
    }
}
