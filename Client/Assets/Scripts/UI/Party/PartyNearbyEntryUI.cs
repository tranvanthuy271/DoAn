using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartyNearbyEntryUI : MonoBehaviour
{
    private const string LogPrefix = "[PartyNearbyEntryUI]";

    [SerializeField] private TMP_Text infoText;
    [SerializeField] private Button   inviteButton;
    [SerializeField] private Image    elementIcon;
    [SerializeField] private ElementIconConfig elementIconConfig;

    private string _targetUserId      = string.Empty;
    private string _targetCharacterName = string.Empty;

    private void Awake()
    {
        inviteButton?.onClick.AddListener(OnInviteClicked);
    }

    public void Setup(NearbyPlayerDto dto)
    {
        EnsureRuntimeReferences();
        _targetUserId        = dto?.userId ?? string.Empty;
        _targetCharacterName = dto?.characterName ?? string.Empty;

        Debug.Log($"{LogPrefix} Setup | userId={dto?.userId} characterName={dto?.characterName} level={dto?.level} inParty={dto?.inParty}", this);

        if (infoText != null)
            infoText.text = $"Tên: {dto?.characterName}, Cấp: {Mathf.Max(1, dto?.level ?? 1)}, Lớp: {ResolveClass(dto)}";

        if (inviteButton != null)
        {
            // Chỉ hiển thị nút Mời khi: người chơi local đang trong nhóm VÀ là nhóm trưởng,
            // và không phải mời chính mình.
            string localUserId = ResolveLocalUserId();
            var pm = PartyManager.Instance;
            bool localIsLeader = pm != null && pm.HasParty && pm.IsLeader;
            bool isSelf = !string.IsNullOrWhiteSpace(localUserId)
                && string.Equals(dto?.userId, localUserId, StringComparison.Ordinal);
            bool canInvite = localIsLeader
                && dto != null
                && !string.IsNullOrWhiteSpace(dto.userId)
                && !isSelf
                && !dto.inParty;

            Debug.Log($"{LogPrefix} InviteVisibility | localIsLeader={localIsLeader} isSelf={isSelf} canInvite={canInvite}", this);

            inviteButton.gameObject.SetActive(canInvite);
            inviteButton.interactable = canInvite;
        }

        ApplyElementIcon(!string.IsNullOrWhiteSpace(dto?.elementType) ? dto.elementType : dto?.className);
    }

    private void EnsureRuntimeReferences()
    {
        elementIcon = PartyUiRuntimeHelper.ResolveElementIcon(transform, elementIcon, this, LogPrefix);
    }

    private void ApplyElementIcon(string elementType)
    {
        Debug.Log($"[PartyNearbyEntryUI] ApplyElementIcon | elementType='{elementType}' elementIcon={(elementIcon == null ? "NULL" : elementIcon.name)}", this);
        if (elementIcon == null)
        {
            Debug.LogWarning("[PartyNearbyEntryUI] elementIcon is NULL – runtime resolution failed.", this);
            return;
        }
        elementIconConfig = ElementIconConfig.Resolve(elementIconConfig, this, nameof(PartyNearbyEntryUI));
        int elementId = ElementHelper.ToId(elementType);
        bool hasConfig = elementIconConfig != null;
        bool validId   = ElementHelper.IsValid(elementId);
        Debug.Log($"[PartyNearbyEntryUI] ApplyElementIcon | resolved elementId={elementId} hasConfig={hasConfig} validId={validId}", this);
        if (hasConfig && validId)
        {
            var sprite = elementIconConfig.GetIcon(elementId);
            Debug.Log($"[PartyNearbyEntryUI] ApplyElementIcon | sprite={(sprite == null ? "NULL" : sprite.name)}", this);
            elementIcon.sprite  = sprite;
            elementIcon.color   = sprite != null ? Color.white : elementIconConfig.GetColor(elementId);
            elementIcon.enabled = true;
        }
        else
        {
            Debug.LogWarning($"[PartyNearbyEntryUI] ApplyElementIcon | SKIPPED – hasConfig={hasConfig} validId={validId} elementType='{elementType}'", this);
            elementIcon.enabled = false;
        }
    }

    private void OnInviteClicked()
    {
        if (string.IsNullOrWhiteSpace(_targetUserId))
        {
            Debug.LogWarning($"{LogPrefix} Invite ignored because targetUserId is empty.", this);
            return;
        }

        Debug.Log($"{LogPrefix} Invite clicked | userId={_targetUserId} characterName={_targetCharacterName}", this);

        PartyManager.EnsureInstance()?.InviteMember(_targetUserId);
    }

    private static string ResolveLocalUserId()
    {
        // GameManager takes priority — PlayerPrefs is shared across ParrelSync clones on Windows
        if (GameManager.Instance?.currentPlayerData != null && GameManager.Instance.currentPlayerData.user_id > 0)
            return GameManager.Instance.currentPlayerData.user_id.ToString();

        int userId = PlayerPrefs.GetInt("USER_ID", 0);
        return userId > 0 ? userId.ToString() : string.Empty;
    }

    private static string ResolveClass(NearbyPlayerDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.className))
            return "Khác";

        return dto.className;
    }
}