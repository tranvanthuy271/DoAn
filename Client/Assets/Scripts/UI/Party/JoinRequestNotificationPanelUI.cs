using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Panel thông báo xin vào nhóm.
// Hiển thị tên, level, hệ của người xin; hai nút Đồng ý / Từ chối.
// Hàng đợi nếu có nhiều request liên tiếp.
// Cách dùng: gán vào trường joinRequestPopup của PartyPanelUI.
public class PartyJoinRequestPopupUI : MonoBehaviour
{
    private const string LogPrefix = "[JoinRequestNotification]";

    [Header("UI")]
    [SerializeField] private TMP_Text    requesterInfoText;
    [SerializeField] private Image       elementIcon;
    [SerializeField] private Button      acceptButton;
    [SerializeField] private Button      declineButton;

    [Header("Assets")]
    [SerializeField] private ElementIconConfig elementIconConfig;

    // Internal state
    private PartyJoinRequestPayload _current;
    private readonly Queue<PartyJoinRequestPayload> _queue = new();

    private Action<string, string> _onAccept;   // (partyId, requesterUserId)
    private Action<string, string> _onDecline;  // (partyId, requesterUserId)

    // Hàm vòng đời của Unity hoặc ASP.NET được gọi tự động.

    private void Awake()
    {
        acceptButton?.onClick.AddListener(OnAcceptClicked);
        declineButton?.onClick.AddListener(OnDeclineClicked);
        gameObject.SetActive(false);
    }

    // Hàm public để script hoặc hệ thống khác gọi vào.

    // Đưa request vào hàng chờ. Nếu panel đang ẩn, hiển thị ngay request này.
    // onAccept / onDecline được gọi với (partyId, requesterUserId).
    public void ShowRequest(PartyJoinRequestPayload payload,
                            Action<string, string> onAccept,
                            Action<string, string> onDecline)
    {
        if (payload == null)
        {
            { /* Cảnh báo: {LogPrefix} ShowRequest called with null payload */ }
            return;
        }

        _onAccept  = onAccept;
        _onDecline = onDecline;
        _queue.Enqueue(payload);

        { /* {LogPrefix} Enqueued | from={payload.requesterName} userId={payload.requesterUserId} queue={_queue.Count} */ }

        if (!gameObject.activeSelf)
            ShowNextRequest();
    }

    // Private helpers

    private void ShowNextRequest()
    {
        if (_queue.Count == 0)
        {
            { /* {LogPrefix} Queue empty  hiding panel */ }
            gameObject.SetActive(false);
            return;
        }

        _current = _queue.Dequeue();

        { /* {LogPrefix} ShowNext | name={_current.requesterName} level={_current.requesterLevel} element={_current.requesterElementType} remaining={_queue.Count} */ }

        // Info text
        if (requesterInfoText != null)
        {
            string elementVn = string.IsNullOrWhiteSpace(_current.requesterElementType)
                ? "Không rõ"
                : ElementHelper.ToVietnamese(_current.requesterElementType);

            requesterInfoText.text =
                $"{_current.requesterName}\n" +
                $"Cấp {Mathf.Max(1, _current.requesterLevel)}  –  Hệ {elementVn}";
        }

        // Element icon
        if (elementIcon != null)
        {
            elementIconConfig = ElementIconConfig.Resolve(elementIconConfig, this, nameof(PartyJoinRequestPopupUI));
            int elementId     = ElementHelper.ToId(_current.requesterElementType);

            Sprite sprite = elementIconConfig != null && ElementHelper.IsValid(elementId)
                ? elementIconConfig.GetIcon(elementId)
                : null;

            elementIcon.sprite  = sprite;
            elementIcon.color   = sprite != null ? Color.white
                : (elementIconConfig != null && ElementHelper.IsValid(elementId)
                    ? elementIconConfig.GetColor(elementId)
                    : Color.gray);
            elementIcon.enabled = true;
        }

        gameObject.SetActive(true);
    }

    private void OnEnable()
    {
        if (elementIcon == null)
            elementIcon = transform.Find("ElementIconImage")?.GetComponent<Image>();
    }

    private void OnAcceptClicked()
    {
        if (_current == null) { ShowNextRequest(); return; }

        { /* {LogPrefix} Accept | partyId={_current.partyId} userId={_current.requesterUserId} */ }
        _onAccept?.Invoke(_current.partyId, _current.requesterUserId);
        _current = null;
        ShowNextRequest();
    }

    private void OnDeclineClicked()
    {
        if (_current == null) { ShowNextRequest(); return; }

        { /* {LogPrefix} Decline | partyId={_current.partyId} userId={_current.requesterUserId} */ }
        _onDecline?.Invoke(_current.partyId, _current.requesterUserId);
        _current = null;
        ShowNextRequest();
    }
}
