using UnityEngine;
using UnityEngine.UI;

// Animation hiệu ứng UI thuần code — gắn lên bất kỳ Image nào.
// Không cần Animator Controller, không cần Animation Clip.
// Hỗ trợ: Pulse (scale), Glow (alpha), Rotate, Color Cycle.
public class UIImageTierAnimation : MonoBehaviour
{
    public enum EffectType
    {
        None,
        Pulse,      // Scale lên xuống
        Glow,       // Alpha lên xuống
        Rotate,     // Xoay liên tục
        ColorCycle, // Đổi màu liên tục
        PulseGlow   // Pulse + Glow kết hợp
    }

    [Header("Effect")]
    [SerializeField] private EffectType effect = EffectType.PulseGlow;

    [Header("Pulse Settings")]
    [SerializeField] private float pulseMinScale = 0.95f;
    [SerializeField] private float pulseMaxScale = 1.05f;
    [SerializeField] private float pulseSpeed = 2f;

    [Header("Glow Settings")]
    [SerializeField] private float glowMinAlpha = 0.6f;
    [SerializeField] private float glowMaxAlpha = 1f;
    [SerializeField] private float glowSpeed = 2f;

    [Header("Rotate Settings")]
    [SerializeField] private float rotateSpeed = 30f;

    [Header("Color Cycle Settings")]
    [SerializeField] private Color colorA = Color.white;
    [SerializeField] private Color colorB = new Color(1f, 0.8f, 0.2f);
    [SerializeField] private float colorSpeed = 1.5f;

    private Image _image;
    private RectTransform _rect;
    private Vector3 _originalScale;
    private Color _originalColor;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _rect = GetComponent<RectTransform>();
        if (_rect != null) _originalScale = _rect.localScale;
        if (_image != null) _originalColor = _image.color;
    }

    private void OnEnable()
    {
        // Reset khi enable
        if (_rect != null) _originalScale = _rect.localScale;
        if (_image != null) _originalColor = _image.color;
    }

    private void Update()
    {
        if (effect == EffectType.None) return;

        float t = Time.unscaledTime;

        switch (effect)
        {
            case EffectType.Pulse:
                ApplyPulse(t);
                break;
            case EffectType.Glow:
                ApplyGlow(t);
                break;
            case EffectType.Rotate:
                ApplyRotate(t);
                break;
            case EffectType.ColorCycle:
                ApplyColorCycle(t);
                break;
            case EffectType.PulseGlow:
                ApplyPulse(t);
                ApplyGlow(t);
                break;
        }
    }

    private void OnDisable()
    {
        // Reset về trạng thái gốc khi tắt
        if (_rect != null) _rect.localScale = _originalScale;
        if (_image != null) _image.color = _originalColor;
    }

    private void ApplyPulse(float t)
    {
        if (_rect == null) return;
        float s = Mathf.Lerp(pulseMinScale, pulseMaxScale, (Mathf.Sin(t * pulseSpeed * Mathf.PI) + 1f) * 0.5f);
        _rect.localScale = new Vector3(s, s, 1f);
    }

    private void ApplyGlow(float t)
    {
        if (_image == null) return;
        float a = Mathf.Lerp(glowMinAlpha, glowMaxAlpha, (Mathf.Sin(t * glowSpeed * Mathf.PI) + 1f) * 0.5f);
        var c = _image.color;
        c.a = a;
        _image.color = c;
    }

    private void ApplyRotate(float t)
    {
        if (_rect == null) return;
        _rect.Rotate(0, 0, rotateSpeed * Time.unscaledDeltaTime);
    }

    private void ApplyColorCycle(float t)
    {
        if (_image == null) return;
        float lerp = (Mathf.Sin(t * colorSpeed * Mathf.PI) + 1f) * 0.5f;
        _image.color = Color.Lerp(colorA, colorB, lerp);
    }

    // API cho code bên ngoài thay đổi effect runtime.
    public void SetEffect(EffectType newEffect)
    {
        // Reset trước khi đổi
        OnDisable();
        effect = newEffect;
    }
}
