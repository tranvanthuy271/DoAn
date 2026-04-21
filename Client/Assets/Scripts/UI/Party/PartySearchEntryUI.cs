using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartySearchEntryUI : MonoBehaviour
{
    private const string LogPrefix = "[PartySearchEntryUI]";

    [SerializeField] private TMP_Text infoText;
    [SerializeField] private TMP_Text memberCountText;
    [SerializeField] private Image    lockIcon;
    [SerializeField] private Button   joinButton;
    [SerializeField] private Image    elementIcon;
    [SerializeField] private ElementIconConfig elementIconConfig;

    private string _partyId = string.Empty;

    private void Awake()
    {
        joinButton?.onClick.AddListener(OnJoinClicked);
    }

    public void Setup(PartySearchEntryDto dto)
    {
        EnsureRuntimeReferences();
        _partyId = dto?.partyId ?? string.Empty;

        Debug.Log($"{LogPrefix} Setup | partyId={dto?.partyId} leader={dto?.leaderName} members={dto?.memberCount}", this);

        if (infoText != null)
            infoText.text = $"Tên: {dto?.leaderName}, Cấp: {Mathf.Max(1, dto?.leaderLevel ?? 1)}, Lớp: {ResolveClass(dto)}";

        if (memberCountText != null)
            memberCountText.text = $"({dto?.memberCount ?? 0} thành viên)";

        if (lockIcon != null)
            lockIcon.gameObject.SetActive(dto != null && dto.isLocked);

        if (joinButton != null)
        {
            bool canJoin = CanJoin(dto);
            joinButton.gameObject.SetActive(canJoin);
            joinButton.interactable = canJoin;
        }

        ApplyElementIcon(!string.IsNullOrWhiteSpace(dto?.leaderElementType) ? dto.leaderElementType : dto?.leaderClassName);
    }

    private void EnsureRuntimeReferences()
    {
        elementIcon = PartyUiRuntimeHelper.ResolveElementIcon(transform, elementIcon, this, LogPrefix);
    }

    private static bool CanJoin(PartySearchEntryDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.partyId))
            return false;

        if (dto.isLocked)
            return false;

        if (dto.maxMembers > 0 && dto.memberCount >= dto.maxMembers)
            return false;

        string localUserId = ResolveLocalUserId();
        if (!string.IsNullOrWhiteSpace(localUserId)
            && string.Equals(dto.leaderUserId, localUserId, StringComparison.Ordinal))
        {
            return false;
        }

        var partyManager = PartyManager.Instance;
        if (partyManager != null && partyManager.HasParty)
        {
            if (string.Equals(partyManager.CurrentParty?.partyId, dto.partyId, StringComparison.Ordinal))
                return false;

            return false;
        }

        return true;
    }

    private void ApplyElementIcon(string elementType)
    {
        Debug.Log($"[PartySearchEntryUI] ApplyElementIcon | elementType='{elementType}' elementIcon={(elementIcon == null ? "NULL" : elementIcon.name)}", this);
        if (elementIcon == null)
        {
            Debug.LogWarning("[PartySearchEntryUI] elementIcon is NULL – runtime resolution failed.", this);
            return;
        }
        elementIconConfig = ElementIconConfig.Resolve(elementIconConfig, this, nameof(PartySearchEntryUI));
        int elementId = ElementHelper.ToId(elementType);
        bool hasConfig = elementIconConfig != null;
        bool validId   = ElementHelper.IsValid(elementId);
        Debug.Log($"[PartySearchEntryUI] ApplyElementIcon | resolved elementId={elementId} hasConfig={hasConfig} validId={validId}", this);
        if (hasConfig && validId)
        {
            var sprite = elementIconConfig.GetIcon(elementId);
            Debug.Log($"[PartySearchEntryUI] ApplyElementIcon | sprite={(sprite == null ? "NULL" : sprite.name)}", this);
            elementIcon.sprite  = sprite;
            elementIcon.color   = sprite != null ? Color.white : elementIconConfig.GetColor(elementId);
            elementIcon.enabled = true;
        }
        else
        {
            Debug.LogWarning($"[PartySearchEntryUI] ApplyElementIcon | SKIPPED – hasConfig={hasConfig} validId={validId} elementType='{elementType}'", this);
            elementIcon.enabled = false;
        }
    }

    private void OnJoinClicked()
    {
        if (string.IsNullOrWhiteSpace(_partyId))
        {
            Debug.LogWarning($"{LogPrefix} Join ignored because partyId is empty.", this);
            return;
        }

        Debug.Log($"{LogPrefix} Join clicked | partyId={_partyId}", this);

        PartyManager.EnsureInstance()?.RequestJoinParty(_partyId);
    }

    private static string ResolveClass(PartySearchEntryDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.leaderClassName))
            return "Khác";

        return dto.leaderClassName;
    }

    private static string ResolveLocalUserId()
    {
        // GameManager takes priority — PlayerPrefs is shared across ParrelSync clones on Windows
        if (GameManager.Instance?.currentPlayerData != null && GameManager.Instance.currentPlayerData.user_id > 0)
            return GameManager.Instance.currentPlayerData.user_id.ToString();

        int userId = PlayerPrefs.GetInt("USER_ID", 0);
        return userId > 0 ? userId.ToString() : string.Empty;
    }
}