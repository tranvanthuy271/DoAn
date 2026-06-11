using UnityEngine;
using TMPro;

// StatRowEntry – 1 dòng stat trong UpgradeItemCard.
// Prefab: GameObject có Text (TMP_Text).
public class StatRowEntry : MonoBehaviour
{
    [SerializeField] private TMP_Text labelText;

    public void Set(string text, Color color)
    {
        if (labelText == null) return;
        labelText.text  = text;
        labelText.color = color;
    }
}
