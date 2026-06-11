using UnityEngine;

// SkillEffectOutline – Outline màu trên player/enemy khi bị debuff (hoặc có buff).
// Khác với BuffOutlineVisual (outline tĩnh màu buff):
// • Outline này có alpha mờ dần từ 100% → 0% theo thời gian còn lại của hiệu ứng.
// • Hỗ trợ nhiều effect cùng lúc: chỉ hiển thị outline mạnh nhất (debuff ưu tiên hơn buff).
// Setup trong Unity:
// 1. Tạo child object tên "SkillEffectOutline" trong Player/Enemy prefab.
// 2. Thêm SpriteRenderer vào child object đó.
// 3. Gắn script này vào cùng object với SpriteRenderer.
// 4. DebuffManager / PlayerBuffSync sẽ gọi Activate() / Deactivate() tự động.
[DisallowMultipleComponent]
public class SkillEffectOutline : MonoBehaviour
{
    [Header("Scale")]
    [Tooltip("Hệ số phóng to so với sprite gốc (1.15 = +15% viền)")]
    [SerializeField] private float scaleMultiplier = 1.15f;

    [Tooltip("Sorting order offset so với main sprite (âm = phía sau)")]
    [SerializeField] private int sortingOrderOffset = -1;

    // Xử lý nội bộ phục vụ các hàm public.
    private SpriteRenderer _sourceRenderer;
    private SpriteRenderer _outlineRenderer;

    // Debuff outline (bất lợi) — ưu tiên hơn buff outline
    private Color   _debuffColor;
    private float   _debuffTotalDuration;
    private float   _debuffStartTime;
    private bool    _debuffActive;

    // Buff outline (có lợi) — hiện khi không có debuff
    private Color   _buffColor;
    private float   _buffTotalDuration;
    private float   _buffStartTime;
    private bool    _buffActive;

    // Hàm vòng đời của Unity hoặc ASP.NET được gọi tự động.

    private void Awake()
    {
        _outlineRenderer = GetComponent<SpriteRenderer>();
        if (_outlineRenderer == null)
        {
            { /* Lỗi: Không có SpriteRenderer trên object này */ }
            return;
        }
        _outlineRenderer.maskInteraction = SpriteMaskInteraction.None;
        ResolveSourceRenderer();
    }

    private void LateUpdate()
    {
        SyncTransformAndSprite();
        UpdateOutlineAlpha();
    }

    // Hàm public để script hoặc hệ thống khác gọi vào.

    // Bật outline debuff (bất lợi) màu đỏ/tím, alpha mờ dần theo duration.
    public void ActivateDebuff(Color color, float duration)
    {
        _debuffColor         = color;
        _debuffTotalDuration = duration;
        _debuffStartTime     = Time.time;
        _debuffActive        = true;
    }

    // Tắt outline debuff (gọi khi debuff hết hạn).
    public void DeactivateDebuff()
    {
        _debuffActive = false;
        if (!_buffActive && _outlineRenderer != null)
            _outlineRenderer.enabled = false;
    }

    // Bật outline buff (có lợi) màu xanh/vàng, alpha mờ dần theo duration.
    public void ActivateBuff(Color color, float duration)
    {
        _buffColor         = color;
        _buffTotalDuration = duration;
        _buffStartTime     = Time.time;
        _buffActive        = true;
    }

    // Tắt outline buff.
    public void DeactivateBuff()
    {
        _buffActive = false;
        if (!_debuffActive && _outlineRenderer != null)
            _outlineRenderer.enabled = false;
    }

    // Xử lý nội bộ phục vụ các hàm public.

    private void UpdateOutlineAlpha()
    {
        if (_outlineRenderer == null || _sourceRenderer == null) return;

        // Chọn active effect ưu tiên: debuff > buff
        bool   anyActive;
        Color  targetColor;
        float  totalDur, startTime;

        if (_debuffActive)
        {
            anyActive   = true;
            targetColor = _debuffColor;
            totalDur    = _debuffTotalDuration;
            startTime   = _debuffStartTime;

            // Auto-deactivate khi hết thời gian
            if (Time.time - startTime >= totalDur)
            {
                _debuffActive = false;
                anyActive     = _buffActive; // fallback sang buff nếu có
                if (anyActive)
                {
                    targetColor = _buffColor;
                    totalDur    = _buffTotalDuration;
                    startTime   = _buffStartTime;
                }
            }
        }
        else if (_buffActive)
        {
            anyActive   = true;
            targetColor = _buffColor;
            totalDur    = _buffTotalDuration;
            startTime   = _buffStartTime;

            if (Time.time - startTime >= totalDur)
            {
                _buffActive = false;
                anyActive   = false;
            }
        }
        else
        {
            _outlineRenderer.enabled = false;
            return;
        }

        if (!anyActive)
        {
            _outlineRenderer.enabled = false;
            return;
        }

        // Tính alpha mờ dần: 1 → 0
        float elapsed   = Time.time - startTime;
        float alpha     = totalDur > 0f ? Mathf.Clamp01(1f - elapsed / totalDur) : 0f;
        Color c         = targetColor;
        c.a             = alpha;

        _outlineRenderer.enabled = _sourceRenderer.enabled && _sourceRenderer.sprite != null && alpha > 0.01f;
        _outlineRenderer.color   = c;
    }

    private void SyncTransformAndSprite()
    {
        if (_outlineRenderer == null) return;
        if (_sourceRenderer == null) { ResolveSourceRenderer(); return; }

        _outlineRenderer.sprite       = _sourceRenderer.sprite;
        _outlineRenderer.flipX        = _sourceRenderer.flipX;
        _outlineRenderer.flipY        = _sourceRenderer.flipY;
        _outlineRenderer.sortingOrder = _sourceRenderer.sortingOrder + sortingOrderOffset;

        // Scale: outline lớn hơn source một chút
        Vector3 parentScale = _sourceRenderer.transform.lossyScale;
        float   invX        = parentScale.x == 0 ? 1f : 1f / parentScale.x;
        float   invY        = parentScale.y == 0 ? 1f : 1f / parentScale.y;
        transform.localScale = new Vector3(scaleMultiplier * invX, scaleMultiplier * invY, 1f);
        transform.position   = _sourceRenderer.transform.position;
    }

    private void ResolveSourceRenderer()
    {
        if (transform.parent == null) return;

        // Tìm SpriteRenderer cha (bỏ qua chính nó và object SkillEffect)
        foreach (var sr in transform.parent.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.gameObject == gameObject) continue;
            if (sr.sprite == null) continue;
            if (sr.transform.name == "SkillEffect" || sr.transform.name == "SkillEffectOutline") continue;
            _sourceRenderer = sr;
            return;
        }
    }
}
