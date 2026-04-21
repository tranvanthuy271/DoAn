using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Bridge mở CharacterPanel có sẵn để hiển thị hồ sơ bạn bè ở chế độ read-only.
/// </summary>
public class PlayerProfilePanelUI : MonoBehaviour
{
    [SerializeField] private CharacterPanelController characterPanel;
    [SerializeField] private bool hideOtherInfoPanelsBeforeShowing = true;

    private int _loadingUserId;
    private string _loadingUsername;

    private void Awake()
    {
        ResolveReferences();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void LoadProfile(int userId, string username)
    {
        _loadingUserId = userId;
        _loadingUsername = username;

        Debug.Log($"[PlayerProfileBridge] LoadProfile requested userId={userId} username='{username}'");

        ResolveReferences();
        if (characterPanel == null)
        {
            Debug.LogError("[PlayerProfileBridge] CharacterPanelController is null. Cannot open friend profile.");
            return;
        }

        var friendManager = FriendManager.EnsureInstance();
        if (friendManager == null)
        {
            Debug.LogError("[PlayerProfileBridge] FriendManager.EnsureInstance returned null. Cannot request friend profile.");
            return;
        }

        friendManager.GetPlayerProfile(userId, HandleProfileLoaded);
    }

    private void HandleProfileLoaded(PlayerProfileDto dto)
    {
        if (dto == null)
        {
            Debug.LogWarning($"[PlayerProfileBridge] Profile load failed userId={_loadingUserId} username='{_loadingUsername}'");
            return;
        }

        if (hideOtherInfoPanelsBeforeShowing)
        {
            var infoPanel = Object.FindObjectOfType<InformationPanelController>(includeInactive: true);
            if (infoPanel != null && infoPanel.IsAnyPanelVisible)
            {
                Debug.Log("[PlayerProfileBridge] Hiding existing character/inventory panel before showing friend profile.");
                infoPanel.HideAll();
            }
            else
            {
                Object.FindObjectOfType<InventoryUI>(includeInactive: true)?.HideInventory();
            }
        }

        string displayName = string.IsNullOrWhiteSpace(dto.character_name)
            ? _loadingUsername
            : dto.character_name;

        Debug.Log(
            $"[PlayerProfileBridge] Profile loaded userId={dto.user_id} playerId={dto.player_id} displayName='{displayName}' " +
            $"skills={dto.skills?.Length ?? 0} potential={dto.potential_stats?.Length ?? 0} hasEquipment={dto.equipment != null}");

        characterPanel.ShowFriendProfile(dto, displayName);
    }

    private void ResolveReferences()
    {
        if (characterPanel != null)
            return;

        characterPanel = GetComponent<CharacterPanelController>();
        if (characterPanel == null)
            characterPanel = Object.FindObjectOfType<CharacterPanelController>(includeInactive: true);

        if (characterPanel != null)
            Debug.Log($"[PlayerProfileBridge] Resolved CharacterPanelController on '{characterPanel.gameObject.name}'.");
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

[System.Serializable]
public class PlayerProfileDto
{
    public int    player_id;
    public int    user_id;
    public string character_name;
    public string element_type;
    public string gender;
    public int    level;
    public int    gold;
    public int    gene_tier;
    public bool   is_hybrid;
    public PlayerEquipmentDto equipment;
    public PlayerSkillInfo[]  skills;
    public PotentialStatInfo[] potential_stats;
    public ProfileFinalStatsDto final_stats;
}

[System.Serializable]
public class ProfileFinalStatsDto
{
    public int hp;
    public int max_hp;
    public int mp;
    public int max_mp;
    public int attack;
    public int defense;
    public float move_speed;
}
