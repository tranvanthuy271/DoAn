using TMPro;
using UnityEngine;

// Hiển thị tên NPC phía trên sprite bằng World-Space Canvas + TextMeshProUGUI.
// Component tự tạo UI child object lúc Awake — không cần chỉnh prefab thủ công.
// NpcInteraction sẽ tự AddComponent nếu prefab chưa có.
// Cách tuỳ chỉnh:
// - offset      : điều chỉnh vị trí label so với tâm NPC
// - fontSize    : cỡ chữ (đơn vị pixel tại scale 0.01)
// - textColor   : màu chữ
// - canvasSize  : chiều rộng/cao vùng text (pixel)
public class NpcNameLabel : MonoBehaviour
{
    [Header("Vị trí")]
    [Tooltip("Offset so với tâm NPC (Y dương = lên trên)")]
    [SerializeField] private Vector3 offset      = new Vector3(0f, 1.4f, 0f);
    [SerializeField] private float   canvasScale = 0.01f;
    [SerializeField] private Vector2 canvasSize  = new Vector2(300f, 40f);

    [Header("Kiểu chữ")]
    [SerializeField] private float fontSize  = 26f;
    [SerializeField] private Color textColor = new Color(1f, 0.95f, 0.3f, 1f); // vàng nhạt


    private TextMeshProUGUI _tmp;
    private Camera          _mainCam;

    private void Awake()
    {
        _mainCam = Camera.main;
        BuildLabel();
    }

    private void LateUpdate()
    {
        // Billboard: giữ canvas luôn quay về phía camera
        if (_mainCam != null && _tmp != null)
            _tmp.canvas.transform.forward = _mainCam.transform.forward;
    }

    private void BuildLabel()
    {
        var go = new GameObject("NpcNameLabel");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = offset;
        go.transform.localScale    = Vector3.one * canvasScale;

        var canvas             = go.AddComponent<Canvas>();
        canvas.renderMode      = RenderMode.WorldSpace;
        canvas.sortingLayerID  = SortingLayer.NameToID("Default");
        canvas.sortingOrder    = 101;   // trên sprite (SortingOrder = 100)

        var rt       = go.GetComponent<RectTransform>();
        rt.sizeDelta = canvasSize;

        _tmp            = go.AddComponent<TextMeshProUGUI>();
        _tmp.alignment  = TextAlignmentOptions.Center;
        _tmp.fontSize   = fontSize;
        _tmp.color      = textColor;
        _tmp.fontStyle  = FontStyles.Bold;
        _tmp.text       = string.Empty;
        UIRuntimeAssetHelper.ApplyNotoSans(_tmp);
    }

    // Cập nhật tên hiển thị trên label.
    public void SetName(string npcName)
    {
        if (_tmp != null)
            _tmp.text = npcName;
    }
}
