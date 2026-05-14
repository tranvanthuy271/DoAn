using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mot dong trong bang xep hang - 4 o: Hang | Ten | Gia tri | Thong tin.
/// Prefab can co children: RankText, NameText, ValueText, ExtraText (TMP).
/// </summary>
public class LeaderboardRowEntryUI : MonoBehaviour
{
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private TMP_Text extraText;
    [SerializeField] private Image backgroundImage;

    [Header("Mau top 3")]
    [SerializeField] private Color colorRank1 = new Color(1.00f, 0.25f, 0.25f);
    [SerializeField] private Color colorRank2 = new Color(1.00f, 0.84f, 0.00f);
    [SerializeField] private Color colorRank3 = new Color(1.00f, 0.55f, 0.10f);
    [SerializeField] private Color colorNormal = Color.white;
    [SerializeField] private Color colorValue = new Color(0.55f, 1.00f, 0.55f);

    [Header("Nen xen ke")]
    [SerializeField] private Color rowEvenColor = new Color(0f, 0f, 0f, 0.00f);
    [SerializeField] private Color rowOddColor = new Color(0f, 0f, 0f, 0.18f);

    private void Reset()
    {
        rankText = transform.Find("RankText")?.GetComponent<TMP_Text>();
        nameText = transform.Find("NameText")?.GetComponent<TMP_Text>();
        valueText = transform.Find("ValueText")?.GetComponent<TMP_Text>();
        extraText = transform.Find("ExtraText")?.GetComponent<TMP_Text>();
        backgroundImage = GetComponent<Image>();
    }

    public void SetRefs(TMP_Text rank, TMP_Text name, TMP_Text value, TMP_Text extra)
    {
        rankText = rank;
        nameText = name;
        valueText = value;
        extraText = extra;
        backgroundImage = GetComponent<Image>();
    }

    public void Setup(LeaderboardEntryDto entry, bool oddRow = false)
    {
        Color nameColor = entry.Rank switch
        {
            1 => colorRank1,
            2 => colorRank2,
            3 => colorRank3,
            _ => colorNormal,
        };

        if (rankText != null)
        {
            rankText.text = entry.Rank.ToString();
            rankText.color = nameColor;
        }

        if (nameText != null)
        {
            nameText.text = entry.CharacterName ?? string.Empty;
            nameText.color = nameColor;
        }

        if (valueText != null)
        {
            valueText.text = FormatValue(entry.Value);
            valueText.color = entry.Rank <= 3 ? nameColor : colorValue;
        }

        if (extraText != null)
        {
            extraText.text = entry.Extra ?? string.Empty;
            extraText.color = colorNormal;
        }

        if (backgroundImage != null)
            backgroundImage.color = oddRow ? rowOddColor : rowEvenColor;
    }

    private static string FormatValue(long v)
    {
        if (v >= 1_000_000_000L) return $"{v / 1_000_000_000.0:0.##}B";
        if (v >= 1_000_000L)     return $"{v / 1_000_000.0:0.##}M";
        if (v >= 1_000L)         return $"{v / 1_000.0:0.##}K";
        return v.ToString();
    }
}
