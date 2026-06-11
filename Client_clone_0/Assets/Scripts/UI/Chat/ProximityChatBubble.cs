using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Hiển thị bubble tin nhắn nổi phía trên đầu nhân vật (WorldSpace Canvas).
// Dùng cho kênh Lân cận – tin hiển thị ngắn rồi tự biến mất.
// Attach vào Player prefab hoặc spawn từ ChatManager.
public class ProximityChatBubble : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private Vector3  offset      = new Vector3(0f, 2.2f, 0f);
    [SerializeField] private float    canvasScale = 0.01f;
    [SerializeField] private Vector2  canvasSize  = new Vector2(340f, 60f);

    [Header("Style")]
    [SerializeField] private float    fontSize     = 18f;
    [SerializeField] private Color    textColor    = Color.white;
    [SerializeField] private Color    bgColor      = new Color(0f, 0f, 0f, 0.6f);
    [SerializeField] private float    displayTime  = 5f;   // giây trước khi tự ẩn

    // Runtime

    private TextMeshProUGUI _tmp;
    private Image           _bg;
    private Canvas          _canvas;
    private Camera          _cam;
    private float           _hideTimer;

    // MonoBehaviour

    private void Awake()
    {
        _cam = Camera.main;
        BuildBubble();
        SetVisible(false);

        // Lắng nghe tin lân cận
        if (ChatManager.Instance != null)
            ChatManager.Instance.OnMessageReceived += OnMessageReceived;
    }

    private void OnDestroy()
    {
        if (ChatManager.Instance != null)
            ChatManager.Instance.OnMessageReceived -= OnMessageReceived;
    }

    private void LateUpdate()
    {
        // Billboard
        if (_cam != null && _canvas != null)
            _canvas.transform.forward = _cam.transform.forward;

        // Auto-hide timer
        if (_hideTimer > 0)
        {
            _hideTimer -= Time.deltaTime;
            if (_hideTimer <= 0) SetVisible(false);
        }
    }

    // Public

    // Hiển thị tin nhắn trên bubble (gọi từ bên ngoài nếu muốn).
    public void ShowMessage(string senderName, string message)
    {
        if (_tmp == null) return;
        _tmp.text  = $"<b>{senderName}:</b> {message}";
        _hideTimer = displayTime;
        SetVisible(true);
    }

    // Private

    private void OnMessageReceived(ChatMessageDto msg)
    {
        if (msg.GetChannel() != ChatChannel.Proximity) return;

        var myId = PlayerPrefs.GetInt("USER_ID", 0).ToString();
        if (msg.senderId != myId) return;

        // Show bubble locally
        ShowMessage("Tôi", msg.message);

        // Broadcast to all other clients via NGO ServerRpc
        var sync = GetComponent<NetworkPlayerDataSync>();
        if (sync != null && sync.IsSpawned)
            sync.ShowProximityBubbleServerRpc(msg.senderName, msg.message);
    }

    private void BuildBubble()
    {
        var go = new GameObject("ChatBubble");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = offset;
        go.transform.localScale    = Vector3.one * canvasScale;

        _canvas = go.AddComponent<Canvas>();
        _canvas.renderMode    = RenderMode.WorldSpace;
        _canvas.sortingOrder  = 110;

        var rt       = go.GetComponent<RectTransform>();
        rt.sizeDelta = canvasSize;

        // Background
        _bg            = go.AddComponent<Image>();
        _bg.color      = bgColor;
        _bg.raycastTarget = false;

        // Text
        var txtGo = new GameObject("BubbleText", typeof(RectTransform));
        txtGo.transform.SetParent(go.transform, false);
        var trt   = txtGo.GetComponent<RectTransform>();
        trt.anchorMin  = Vector2.zero;
        trt.anchorMax  = Vector2.one;
        trt.offsetMin  = new Vector2(6, 4);
        trt.offsetMax  = new Vector2(-6, -4);

        _tmp               = txtGo.AddComponent<TextMeshProUGUI>();
        _tmp.alignment     = TextAlignmentOptions.MidlineLeft;
        _tmp.fontSize      = fontSize;
        _tmp.color         = textColor;
        _tmp.enableWordWrapping = true;
        _tmp.raycastTarget = false;
    }

    private void SetVisible(bool v)
    {
        if (_canvas != null) _canvas.gameObject.SetActive(v);
    }
}
