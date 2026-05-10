using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hiển thị một dòng tin nhắn trong ScrollView.
/// Prefab: HorizontalLayoutGroup → [TimestampText] [SenderText] [MessageText]
/// </summary>
public class ChatMessageEntryUI : MonoBehaviour
{
    private static readonly Color TimestampColor = new Color32(0x7C, 0x67, 0x55, 0xFF);
    private static readonly Color MessageTextColor = new Color32(0x3E, 0x29, 0x18, 0xFF);
    private static readonly Color MessageBackgroundColor = new Color32(0xFF, 0xF6, 0xEA, 0x78);

    [SerializeField] private TextMeshProUGUI timestampText;
    [SerializeField] private TextMeshProUGUI senderText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Image           background;    // optional tint

    private void Awake()
    {
        EnsureBackground();
    }

    public void Setup(ChatMessageDto msg)
    {
        EnsureBackground();

        var ch    = msg.GetChannel();
        var color = ch.MessageColor();

        if (timestampText != null)
        {
            timestampText.text  = msg.timestamp;
            timestampText.color = TimestampColor;
        }

        if (senderText != null)
        {
            senderText.text  = $"[{msg.senderName}]";
            senderText.color = color;
            senderText.fontStyle = FontStyles.Bold;
        }

        if (messageText != null)
        {
            messageText.text  = msg.message;
            messageText.color = MessageTextColor;
            messageText.fontStyle = FontStyles.Normal;
        }

        if (background != null)
            background.color = MessageBackgroundColor;

        // Tin riêng hiển thị target
        if (ch == ChatChannel.Private && !string.IsNullOrEmpty(msg.targetId) && senderText != null)
        {
            var myId = PlayerPrefs.GetInt("USER_ID", 0).ToString();
            if (msg.senderId == myId && senderText != null)
                senderText.text = $"[{msg.senderName} → {msg.targetId}]";
        }
    }

    private void EnsureBackground()
    {
        if (background != null)
            return;

        background = GetComponent<Image>();
        if (background == null)
            background = gameObject.AddComponent<Image>();

        background.raycastTarget = false;
        background.type = Image.Type.Sliced;
    }
}
