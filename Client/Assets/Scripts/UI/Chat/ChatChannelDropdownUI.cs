using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel dropdown khi nhấn vào icon kênh ở thanh input.
/// Mỗi row chỉ hiển thị một Image icon (kéo thả trong Inspector).
/// Tắt/bật bằng Toggle().
/// </summary>
public class ChatChannelDropdownUI : MonoBehaviour
{
    [System.Serializable]
    public struct ChannelItem
    {
        public ChatChannel channel;
        /// <summary>Kéo Sprite icon vào đây trong Inspector.</summary>
        public Sprite      icon;
        /// <summary>Màu tô lên icon. White = giữ nguyên màu ảnh gốc.</summary>
        public Color       iconTint;
    }

    [Header("Items — kéo icon vào từng ô")]
    [SerializeField] private List<ChannelItem> channelItems = new List<ChannelItem>
    {
        new ChannelItem { channel = ChatChannel.Proximity, iconTint = new Color(0.2f,0.6f,1f) },
        new ChannelItem { channel = ChatChannel.World,     iconTint = new Color(1f,0.8f,0.2f) },
        new ChannelItem { channel = ChatChannel.Class,     iconTint = new Color(0.4f,0.8f,0.4f) },
        new ChannelItem { channel = ChatChannel.Clan,      iconTint = new Color(0.8f,0.4f,1f) },
        new ChannelItem { channel = ChatChannel.Group,     iconTint = new Color(1f,0.6f,0.2f) },
        new ChannelItem { channel = ChatChannel.Private,   iconTint = new Color(1f,0.4f,0.6f) },
    };

    [Header("Row Prefab")]
    [SerializeField] private GameObject rowPrefab;   // optional; sẽ tự tạo nếu null
    [SerializeField] private Transform  rowContainer;

    // ── State ─────────────────────────────────────────────────────────────────

    private Action<ChatChannel> _onSelected;
    private bool                _built;

    // ── MonoBehaviour ─────────────────────────────────────────────────────────

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Toggle(Action<ChatChannel> onSelected)
    {
        _onSelected = onSelected;
        if (!_built) Build();
        gameObject.SetActive(!gameObject.activeSelf);
    }

    public void Close() => gameObject.SetActive(false);

    public bool TryGetChannelItem(ChatChannel channel, out ChannelItem item)
    {
        foreach (var candidate in channelItems)
        {
            if (candidate.channel != channel) continue;
            item = candidate;
            return true;
        }

        item = default;
        return false;
    }

    // ── Build Rows ────────────────────────────────────────────────────────────

    private void Build()
    {
        _built = true;
        var parent = rowContainer != null ? rowContainer : transform;

        foreach (var item in channelItems)
        {
            var row = rowPrefab != null
                ? Instantiate(rowPrefab, parent)
                : BuildDefaultRow(parent);

            // Icon image — tìm child "Icon" hoặc chính Image trên root
            var iconImg = row.transform.Find("Icon")?.GetComponent<Image>()
                       ?? row.GetComponent<Image>();
            var label = row.transform.Find("Label")?.GetComponent<TextMeshProUGUI>()
                     ?? row.GetComponentInChildren<TextMeshProUGUI>(true);
            if (iconImg != null)
            {
                iconImg.sprite = item.icon;
                iconImg.color = Color.white;
                iconImg.enabled = item.icon != null;
            }

            if (label != null)
            {
                bool hasIcon = item.icon != null;
                label.gameObject.SetActive(!hasIcon);
                if (!hasIcon)
                    label.text = item.channel.ShortCode();
            }

            // Button
            var btn = row.GetComponent<Button>();
            var ch  = item.channel;
            btn?.onClick.AddListener(() =>
            {
                _onSelected?.Invoke(ch);
                Close();
            });
        }
    }

    private static GameObject BuildDefaultRow(Transform parent)
    {
        // Root row: square button with a centered icon image
        var row = new GameObject("DropdownRow", typeof(RectTransform), typeof(Image), typeof(Button));
        row.transform.SetParent(parent, false);
        var rowRt = row.GetComponent<RectTransform>();
        rowRt.sizeDelta = new Vector2(40, 40);

        var rowImg = row.GetComponent<Image>();
        rowImg.color = new Color(0.15f, 0.1f, 0.05f, 0.95f);

        // Icon child (fills the row)
        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(row.transform, false);
        var iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.1f, 0.1f);
        iconRt.anchorMax = new Vector2(0.9f, 0.9f);
        iconRt.offsetMin = Vector2.zero;
        iconRt.offsetMax = Vector2.zero;

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(row.transform, false);
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        var label = labelGo.GetComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 12;
        label.fontStyle = FontStyles.Bold;
        label.color = Color.white;

        return row;
    }
}
