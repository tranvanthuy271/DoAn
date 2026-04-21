using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hiển thị một dòng tin nhắn trong ScrollView.
/// Prefab: HorizontalLayoutGroup → [TimestampText] [SenderText] [MessageText]
/// </summary>
public class ChatMessageEntryUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timestampText;
    [SerializeField] private TextMeshProUGUI senderText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Image           background;    // optional tint

    public void Setup(ChatMessageDto msg)
    {
        var ch    = msg.GetChannel();
        var color = ch.MessageColor();

        if (timestampText != null)
        {
            timestampText.text  = msg.timestamp;
            timestampText.color = new Color(0.6f, 0.6f, 0.6f);
        }

        if (senderText != null)
        {
            senderText.text  = $"[{msg.senderName}]";
            senderText.color = color;
        }

        if (messageText != null)
        {
            messageText.text  = msg.message;
            messageText.color = Color.white;
        }

        // Tin riêng hiển thị target
        if (ch == ChatChannel.Private && !string.IsNullOrEmpty(msg.targetId) && senderText != null)
        {
            var myId = PlayerPrefs.GetInt("USER_ID", 0).ToString();
            if (msg.senderId == myId && senderText != null)
                senderText.text = $"[{msg.senderName} → {msg.targetId}]";
        }
    }
}
