using UnityEngine;
using UnityEngine.UI;

// Gắn component này lên bất kỳ GameObject nào có Image component.
// Khi scene load xong, tự tra ElementIconConfig theo hệ của player hiện tại
// rồi set sprite + màu cho Image đó.
// CÁCH DÙNG
// 1. Chọn GameObject chứa Image (ví dụ: icon hệ trong HUD, avatar, HP bar)
// 2. Add Component → ElementImage
// 3. Kéo asset ElementIconConfig vào field "Element Icon Config"
// 4. Chọn chế độ:
// • ApplySprite  — đổi sprite của Image
// • ApplyColor   — đổi màu của Image (dùng cho HP bar, glow)
// • ApplyBoth    — đổi cả 2
// 5. Nếu muốn theo dõi hệ của một enemy/player khác (không phải local player),
// set elementOverride = true rồi điền elementId thủ công hoặc gọi SetElement().
[RequireComponent(typeof(Image))]
public class ElementImage : MonoBehaviour
{
    public enum ApplyMode { ApplySprite, ApplyColor, ApplyBoth }

    [SerializeField] private ElementIconConfig elementIconConfig;
    [SerializeField] private ElementIconConfig.SpriteKind spriteKind = ElementIconConfig.SpriteKind.Icon;
    [SerializeField] private ApplyMode applyMode = ApplyMode.ApplySprite;

    [Tooltip("Bật nếu muốn set elementId thủ công thay vì đọc từ local player")]
    [SerializeField] private bool elementOverride = false;
    [SerializeField] private int overrideElementId = 0;

    private Image _image;

    private void Awake()
    {
        _image = GetComponent<Image>();
    }

    private void Start()
    {
        if (!elementOverride)
            RefreshFromLocalPlayer();
        else
            Apply(overrideElementId);
    }

    // Đọc element_type của local player từ GameManager rồi apply.
    // Gọi lại sau khi login xong nếu Start() chạy trước khi data về.
    public void RefreshFromLocalPlayer()
    {
        var data = GameManager.Instance?.currentPlayerData;
        if (data == null)
        {
            // Data chưa về — thử lại frame sau
            StartCoroutine(WaitAndRefresh());
            return;
        }

        int elementId = ElementHelper.ToId(data.element_type);
        Apply(elementId);
    }

    // Set hệ thủ công (dùng cho icon enemy/player khác).
    public void SetElement(int elementId)
    {
        elementOverride = true;
        overrideElementId = elementId;
        Apply(elementId);
    }

    // Set hệ bằng English key (vd: "Fire", "Water").
    public void SetElement(string englishKey) => SetElement(ElementHelper.ToId(englishKey));

    private void Apply(int elementId)
    {
        var resolvedConfig = ResolveConfig();
        if (resolvedConfig == null)
        {
            return;
        }

        if (applyMode is ApplyMode.ApplySprite or ApplyMode.ApplyBoth)
        {
            var sprite = resolvedConfig.GetSpriteOrLog(elementId, spriteKind, this, nameof(ElementImage));
            if (sprite != null)
                _image.sprite = sprite;
        }

        if (applyMode is ApplyMode.ApplyColor or ApplyMode.ApplyBoth)
        {
            _image.color = resolvedConfig.GetColor(elementId);
        }
    }

    private ElementIconConfig ResolveConfig()
    {
        if (elementIconConfig == null)
            elementIconConfig = ElementIconConfig.Resolve(elementIconConfig, this, nameof(ElementImage));

        return elementIconConfig;
    }

    private System.Collections.IEnumerator WaitAndRefresh()
    {
        // Chờ tối đa 5 giây để PlayerData có mặt
        float waited = 0f;
        while (waited < 5f)
        {
            yield return null;
            waited += Time.deltaTime;
            if (GameManager.Instance?.currentPlayerData != null)
            {
                RefreshFromLocalPlayer();
                yield break;
            }
        }
        { /* Cảnh báo: Timeout: không lấy được PlayerData sau 5s */ }
    }
}
