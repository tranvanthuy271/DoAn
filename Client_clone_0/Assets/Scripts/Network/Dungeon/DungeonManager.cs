using System;
using UnityEngine;

/// <summary>
/// DungeonManager - Singleton quản lý TRẠNG THÁI phó bản phía CLIENT.
///
/// KIẾN TRÚC MỚI (Zone-based):
///   Client KHÔNG BAO GIỜ gọi Shutdown/StartHost/StartClient.
///   Mọi chuyển cảnh phó bản đều thông qua ZoneTransitionController ServerRpc
///   (giống chuyển map thường — in-process, instant, không disconnect).
///
/// SOLO:  Client → RequestDungeonEntryServerRpc(mapId, configId)
///        Server tạo custom room → transfer client vào dungeon scene
///
/// PARTY: Leader → RequestPartyDungeonEntryServerRpc(mapId, configId, memberIds)
///        Server tạo 1 custom room → transfer tất cả party members
///
/// EXIT:  Client → RequestDungeonExitServerRpc(returnMapId)
///        Server transfer client về overworld map
/// </summary>
public class DungeonManager : MonoBehaviour
{
    private static DungeonManager _instance;
    public static DungeonManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<DungeonManager>();
                if (_instance == null)
                {
                    var go = new GameObject("DungeonManager");
                    _instance = go.AddComponent<DungeonManager>();
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("Dungeon State")]
    [SerializeField] private bool _isInDungeon;
    [SerializeField] private int  _activeDungeonId = -1;
    [SerializeField] private int  _activeDungeonMapId = -1;
    [SerializeField] private int  _activeDungeonZoneId;

    public bool IsInDungeon        => _isInDungeon;
    public int  ActiveDungeonId    => _activeDungeonId;
    public int  ActiveDungeonMapId => _activeDungeonMapId;
    public int  ActiveDungeonZoneId => _activeDungeonZoneId;

    public event Action<string> OnDungeonStatusMessage;
    public event Action         OnDungeonEntered;
    public event Action         OnDungeonExited;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ========================================================================
    //  ENTRY — Gửi yêu cầu vào phó bản qua ZoneTransitionController
    // ========================================================================

    /// <summary>
    /// Yêu cầu vào phó bản solo. Gọi từ UI (DungeonNpcMenuUI).
    /// </summary>
    public void EnterDungeon(DungeonConfigData config)
    {
        if (config == null) return;

        Debug.Log($"[DungeonManager] EnterDungeon | dungeonId={config.dungeon_id} name='{config.dungeon_name}' type='{config.dungeon_type}' mapId={config.map_id}", this);

        var ztc = FindAnyObjectByType<ZoneTransitionController>();
        if (ztc == null)
        {
            Debug.LogError("[DungeonManager] ZoneTransitionController không tìm thấy!");
            return;
        }

        Notify("Đang vào phó bản " + config.dungeon_name + "...");
        ztc.RequestDungeonEntryServerRpc(config.map_id, config.dungeon_id);
    }

    /// <summary>
    /// Yêu cầu cả tổ đội vào phó bản. Gọi từ DungeonNpcMenuUI (leader only).
    /// partyMemberUserIds: danh sách userId của thành viên party (bao gồm cả leader).
    /// </summary>
    public void EnterPartyDungeon(DungeonConfigData config, string[] partyMemberUserIds)
    {
        if (config == null) return;

        Debug.Log($"[DungeonManager] EnterPartyDungeon | dungeonId={config.dungeon_id} members={partyMemberUserIds?.Length ?? 0}", this);

        var ztc = FindAnyObjectByType<ZoneTransitionController>();
        if (ztc == null)
        {
            Debug.LogError("[DungeonManager] ZoneTransitionController không tìm thấy!");
            return;
        }

        string csv = partyMemberUserIds != null ? string.Join(",", partyMemberUserIds) : "";
        Notify("Đang vào phó bản " + config.dungeon_name + " cùng tổ đội...");
        ztc.RequestPartyDungeonEntryServerRpc(config.map_id, config.dungeon_id, csv);
    }

    // ========================================================================
    //  EXIT — Rời phó bản qua ZoneTransitionController
    // ========================================================================

    /// <summary>Rời phó bản và quay về overworld.</summary>
    public void ExitDungeon(int returnMapId = 0)
    {
        Debug.Log($"[DungeonManager] ExitDungeon | returnMapId={returnMapId}", this);

        var ztc = FindAnyObjectByType<ZoneTransitionController>();
        if (ztc == null)
        {
            Debug.LogError("[DungeonManager] ZoneTransitionController không tìm thấy!");
            return;
        }

        Notify("Đang rời phó bản...");
        ztc.RequestDungeonExitServerRpc(returnMapId);
    }

    // ========================================================================
    //  CALLBACKS TỪ ZoneTransitionController (ClientRpc)
    // ========================================================================

    /// <summary>Gọi bởi NotifyDungeonEnteredClientRpc — cập nhật trạng thái đã vào dungeon.</summary>
    public void OnZoneDungeonEntered(int dungeonConfigId, int mapId, int zoneId)
    {
        _isInDungeon        = true;
        _activeDungeonId    = dungeonConfigId;
        _activeDungeonMapId = mapId;
        _activeDungeonZoneId = zoneId;

        Debug.Log($"[DungeonManager] Entered dungeon | configId={dungeonConfigId} map={mapId} zone={zoneId}", this);
        OnDungeonEntered?.Invoke();
        Notify("Đã vào phó bản!");
    }

    /// <summary>Gọi bởi NotifyDungeonExitedClientRpc — cập nhật trạng thái đã rời dungeon.</summary>
    public void OnZoneDungeonExited()
    {
        _isInDungeon        = false;
        _activeDungeonId    = -1;
        _activeDungeonMapId = -1;
        _activeDungeonZoneId = 0;

        Debug.Log("[DungeonManager] Exited dungeon", this);
        OnDungeonExited?.Invoke();
        Notify("Đã rời phó bản!");
    }

    // ========================================================================
    //  UTILITIES
    // ========================================================================

    private void Notify(string msg)
    {
        Debug.Log("[DungeonManager] " + msg);
        OnDungeonStatusMessage?.Invoke(msg);
    }
}
