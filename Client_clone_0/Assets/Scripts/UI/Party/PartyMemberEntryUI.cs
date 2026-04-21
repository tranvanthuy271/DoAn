using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartyMemberEntryUI : MonoBehaviour
{
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private TMP_Text detailText;
    [SerializeField] private Image    leaderBadge;
    [SerializeField] private GameObject leaderBadgeRoot;
    [SerializeField] private Image    offlineMask;
    [SerializeField] private Image    elementIcon;
    [SerializeField] private ElementIconConfig elementIconConfig;

    public void Setup(PartyMemberDto member, bool isLeader)
    {
        EnsureRuntimeReferences();

        if (characterNameText != null)
        {
            characterNameText.text = string.IsNullOrWhiteSpace(member?.characterName) ? member?.userId : member.characterName;
            characterNameText.fontStyle = isLeader ? FontStyles.Bold : FontStyles.Normal;
        }

        if (detailText != null)
            detailText.text = $"Cấp: {Mathf.Max(1, member?.level ?? 1)}, Lớp: {ResolveClass(member)}";

        if (leaderBadgeRoot != null)
            leaderBadgeRoot.SetActive(isLeader);
        else if (leaderBadge != null)
            leaderBadge.gameObject.SetActive(isLeader);

        if (offlineMask != null)
            offlineMask.gameObject.SetActive(member != null && !member.online);

        ApplyElementIcon(!string.IsNullOrWhiteSpace(member?.elementType) ? member.elementType : member?.className);
    }

    private void EnsureRuntimeReferences()
    {
        if (leaderBadgeRoot == null && leaderBadge != null)
            leaderBadgeRoot = leaderBadge.gameObject;

        if (leaderBadgeRoot == null)
            leaderBadgeRoot = transform.Find("LeaderBadge")?.gameObject;

        if (leaderBadge == null)
            leaderBadge = transform.Find("LeaderBadge")?.GetComponent<Image>();

        if (offlineMask == null)
            offlineMask = transform.Find("OfflineMask")?.GetComponent<Image>();

        elementIcon = PartyUiRuntimeHelper.ResolveElementIcon(transform, elementIcon, this, "[PartyMemberEntryUI]");
    }

    private void ApplyElementIcon(string elementType)
    {
        Debug.Log($"[PartyMemberEntryUI] ApplyElementIcon | elementType='{elementType}' elementIcon={(elementIcon == null ? "NULL" : elementIcon.name)}", this);
        if (elementIcon == null)
        {
            Debug.LogWarning("[PartyMemberEntryUI] elementIcon is NULL – runtime resolution failed.", this);
            return;
        }
        elementIconConfig = ElementIconConfig.Resolve(elementIconConfig, this, nameof(PartyMemberEntryUI));
        int elementId = ElementHelper.ToId(elementType);
        bool hasConfig = elementIconConfig != null;
        bool validId   = ElementHelper.IsValid(elementId);
        Debug.Log($"[PartyMemberEntryUI] ApplyElementIcon | resolved elementId={elementId} hasConfig={hasConfig} validId={validId}", this);
        if (hasConfig && validId)
        {
            var sprite = elementIconConfig.GetIcon(elementId);
            Debug.Log($"[PartyMemberEntryUI] ApplyElementIcon | sprite={(sprite == null ? "NULL" : sprite.name)}", this);
            elementIcon.sprite  = sprite;
            elementIcon.color   = sprite != null ? Color.white : elementIconConfig.GetColor(elementId);
            elementIcon.enabled = true;
        }
        else
        {
            Debug.LogWarning($"[PartyMemberEntryUI] ApplyElementIcon | SKIPPED – hasConfig={hasConfig} validId={validId} elementType='{elementType}'", this);
            elementIcon.enabled = false;
        }
    }

    private static string ResolveClass(PartyMemberDto member)
    {
        if (member == null || string.IsNullOrWhiteSpace(member.className))
            return "Khác";

        return member.className;
    }
}

internal static class PartyUiRuntimeHelper
{
    public static Image ResolveElementIcon(Transform root, Image current, Object logContext, string logPrefix)
    {
        if (current != null)
            return current;

        current = root?.Find("AvatarImage/ElementIconImage")?.GetComponent<Image>();
        if (current != null)
            return current;

        current = root?.Find("ElementIconImage")?.GetComponent<Image>();
        if (current != null)
            return current;

        current = root?.Find("AvatarImage")?.GetComponent<Image>();
        if (current != null)
        {
            Debug.Log($"{logPrefix} Using AvatarImage directly because ElementIconImage does not exist in this prefab.", logContext);
            return current;
        }

        if (root != null)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child != null && child.name == "ElementIconImage")
                {
                    current = child.GetComponent<Image>();
                    if (current != null)
                        return current;
                }
            }

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child != null && child.name == "AvatarImage")
                {
                    current = child.GetComponent<Image>();
                    if (current != null)
                    {
                        Debug.Log($"{logPrefix} Using AvatarImage directly because ElementIconImage does not exist in this prefab.", logContext);
                        return current;
                    }
                }
            }
        }

        Debug.LogWarning($"{logPrefix} Could not resolve ElementIconImage or AvatarImage in this prefab.", logContext);
        return null;
    }
}