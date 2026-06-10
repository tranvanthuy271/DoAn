using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// EnemyInfoPanel — Panel hiển thị thông số enemy khi player click chọn.
/// Singleton — gắn vào một Panel trong Scene Canvas (Screen Space - Overlay).
///
/// Layout (theo ảnh tham khảo):
///   ┌─────────────────────────────┐
///   │  Linh dương Topi            │  ← nameText
///   ├─────────────────────────────┤
///   │ [Thổ]  [═══HP BAR═══]       │  ← elementText  +  hpSlider
///   │         48140 / 48140       │  ← hpText
///   │   Lv: 52 + 28045 Exp        │  ← levelExpText
///   └─────────────────────────────┘
///
/// Setup trong Unity Editor:
///   1. Tạo Panel trong Canvas chính (Screen Space - Overlay).
///   2. Gắn script này lên Panel root.
///   3. Kéo các Text/Slider vào fields.
/// </summary>
public class EnemyInfoPanel : MonoBehaviour
{
    public static EnemyInfoPanel Instance { get; private set; }

    [Header("Panel Root")]
    public GameObject panelRoot;

    [Header("Tên quái")]
    public TextMeshProUGUI nameText;

    [Header("Hệ nguyên tố")]
    public TextMeshProUGUI elementText;   // Badge nhỏ: "Thổ", "Hỏa", v.v.

    [Header("HP")]
    public Slider hpSlider;
    public TextMeshProUGUI hpText;        // "48140 / 48140"

    [Header("Level & EXP")]
    public TextMeshProUGUI levelExpText;  // "Lv: 52 + 28045 Exp"

    [Header("Stacking")]
    [SerializeField] private int sortingOrder = 1;

    // ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        ConfigureStacking();
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────

    public void Show(EnemyStats stats)
    {
        if (panelRoot == null || stats == null) return;
        ConfigureStacking();
        panelRoot.SetActive(true);
        panelRoot.transform.SetAsFirstSibling();

        if (nameText != null)
            nameText.text = stats.enemyName;

        if (elementText != null)
            elementText.text = TranslateElement(stats.elementType);

        UpdateHP(stats.currentHp, stats.maxHp);

        if (levelExpText != null)
        {
            levelExpText.text = stats.expReward > 0
                ? $"Lv: {stats.level} + {stats.expReward} Exp"
                : $"Lv: {stats.level}";
        }
    }

    /// <summary>Cập nhật HP realtime khi enemy bị đánh (gọi từ EnemyClickHandler.RefreshPanelIfSelected).</summary>
    public void UpdateHP(int current, int max)
    {
        if (hpText != null)
            hpText.text = $"{current} / {max}";

        if (hpSlider != null)
            hpSlider.value = max > 0 ? (float)current / max : 0f;
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void ConfigureStacking()
    {
        var canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
        }

        var group = GetComponent<CanvasGroup>();
        if (group == null)
            group = gameObject.AddComponent<CanvasGroup>();

        group.blocksRaycasts = false;
        group.interactable = false;
    }

    // ─────────────────────────────────────────────────────────────────

    private static string TranslateElement(string element)
    {
        switch (element)
        {
            case "Fire":  return "Hỏa";
            case "Water": return "Thủy";
            case "Earth": return "Thổ";
            case "Metal": return "Kim";
            case "Wood":  return "Mộc";
            case "Wind":  return "Phong";
            default:      return "Vô Hệ";
        }
    }
}
