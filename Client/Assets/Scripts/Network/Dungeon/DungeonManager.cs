using System;
using UnityEngine;


// DungeonManager - Singleton quản lý TRẠNG THÁI phó bản phía CLIENT.
// KIẾN TRÚC MỚI (Zone-based):
// Client KHÔNG BAO GIỜ gọi Shutdown/StartHost/StartClient.
// Mọi chuyển cảnh phó bản đều thông qua ZoneTransitionController ServerRpc
// (giống chuyển map thường — in-process, instant, không disconnect).
// SOLO:  Client → RequestDungeonEntryServerRpc(mapId, configId)
// Server tạo custom room → transfer client vào dungeon scene
// PARTY: Leader → RequestPartyDungeonEntryServerRpc(mapId, configId, memberIds)
// Server tạo 1 custom room → transfer tất cả party members
// EXIT:  Client → RequestDungeonExitServerRpc(returnMapId)
// Server transfer client về overworld map
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
    [SerializeField] private int _currentWaveRound;
    [SerializeField] private int _currentWaveMaxRounds;
    [SerializeField] private int _currentWaveRemainingSeconds;
    [SerializeField] private string _currentDungeonStatusMessage = string.Empty;

    private float _waveCountdownEndRealtime;
    private bool _waveCountdownActive;
    private WaveHUD _waveHUD; // auto-created canvas HUD for wave dungeons

    public bool IsInDungeon        => _isInDungeon;
    public int  ActiveDungeonId    => _activeDungeonId;
    public int  ActiveDungeonMapId => _activeDungeonMapId;
    public int  ActiveDungeonZoneId => _activeDungeonZoneId;
    public int CurrentWaveRound => _currentWaveRound;
    public int CurrentWaveMaxRounds => _currentWaveMaxRounds;
    public int CurrentWaveRemainingSeconds => _currentWaveRemainingSeconds;
    public string CurrentDungeonStatusMessage => _currentDungeonStatusMessage;

    public event Action<string> OnDungeonStatusMessage;
    public event Action         OnDungeonEntered;
    public event Action         OnDungeonExited;
    public event Action<int, int, int> OnWaveStateChanged;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (!_isInDungeon || !_waveCountdownActive)
            return;

        int localRemaining = Mathf.Max(0, Mathf.CeilToInt(_waveCountdownEndRealtime - Time.realtimeSinceStartup));
        if (localRemaining == _currentWaveRemainingSeconds)
            return;

        _currentWaveRemainingSeconds = localRemaining;
        if (_currentWaveRemainingSeconds <= 0)
            _waveCountdownActive = false;

        OnWaveStateChanged?.Invoke(_currentWaveRound, _currentWaveMaxRounds, _currentWaveRemainingSeconds);
    }

    // ========================================================================
    //  ENTRY — Gửi yêu cầu vào phó bản qua ZoneTransitionController
    // ========================================================================

    // Yêu cầu vào phó bản solo. Gọi từ UI (DungeonNpcMenuUI).
    public void EnterDungeon(DungeonConfigData config)
    {
        if (config == null) return;

        { /* EnterDungeon | dungeonId={config.dungeon_id} name='{config.dungeon_name}' type='{config.dungeon_type}' mapId={config.map_id} */ }

        var ztc = FindAnyObjectByType<ZoneTransitionController>();
        if (ztc == null)
        {
            { /* Lỗi: ZoneTransitionController không tìm thấy */ }
            return;
        }

        Notify("Đang vào phó bản " + config.dungeon_name + "...");
        ztc.RequestDungeonEntryServerRpc(config.map_id, config.dungeon_id);
    }

    // Yêu cầu cả tổ đội vào phó bản. Gọi từ DungeonNpcMenuUI (leader only).
    // partyMemberUserIds: danh sách userId của thành viên party (bao gồm cả leader).
    public void EnterPartyDungeon(DungeonConfigData config, string[] partyMemberUserIds)
    {
        if (config == null) return;

        { /* EnterPartyDungeon | dungeonId={config.dungeon_id} members={partyMemberUserIds?.Length ?? 0} */ }

        var ztc = FindAnyObjectByType<ZoneTransitionController>();
        if (ztc == null)
        {
            { /* Lỗi: ZoneTransitionController không tìm thấy */ }
            return;
        }

        string csv = partyMemberUserIds != null ? string.Join(",", partyMemberUserIds) : "";
        Notify("Đang vào phó bản " + config.dungeon_name + " cùng tổ đội...");
        ztc.RequestPartyDungeonEntryServerRpc(config.map_id, config.dungeon_id, csv);
    }

    // ========================================================================
    //  EXIT — Rời phó bản qua ZoneTransitionController
    // ========================================================================

    // Rời phó bản và quay về overworld.
    public void ExitDungeon(int returnMapId = 0)
    {
        { /* ExitDungeon | returnMapId={returnMapId} */ }

        var ztc = FindAnyObjectByType<ZoneTransitionController>();
        if (ztc == null)
        {
            { /* Lỗi: ZoneTransitionController không tìm thấy */ }
            return;
        }

        Notify("Đang rời phó bản...");
        ztc.RequestDungeonExitServerRpc(returnMapId);
    }

    // ========================================================================
    //  CALLBACKS TỪ ZoneTransitionController (ClientRpc)
    // ========================================================================

    // Gọi bởi NotifyDungeonEnteredClientRpc — cập nhật trạng thái đã vào dungeon.
    public void OnZoneDungeonEntered(int dungeonConfigId, int mapId, int zoneId)
    {
        _isInDungeon        = true;
        _activeDungeonId    = dungeonConfigId;
        _activeDungeonMapId = mapId;
        _activeDungeonZoneId = zoneId;
        _currentWaveRound = 0;
        _currentWaveMaxRounds = 0;
        _currentWaveRemainingSeconds = 0;
        _currentDungeonStatusMessage = string.Empty;
        _waveCountdownEndRealtime = 0f;
        _waveCountdownActive = false;

        { /* Entered dungeon | configId={dungeonConfigId} map={mapId} zone={zoneId} */ }
        OnDungeonEntered?.Invoke();
        Notify("Đã vào phó bản!");
    }

    // Gọi bởi NotifyDungeonExitedClientRpc — cập nhật trạng thái đã rời dungeon.
    public void OnZoneDungeonExited()
    {
        _isInDungeon        = false;
        _activeDungeonId    = -1;
        _activeDungeonMapId = -1;
        _activeDungeonZoneId = 0;
        _currentWaveRound = 0;
        _currentWaveMaxRounds = 0;
        _currentWaveRemainingSeconds = 0;
        _currentDungeonStatusMessage = string.Empty;
        _waveCountdownEndRealtime = 0f;
        _waveCountdownActive = false;
        // Reset WaveHUD ref so it gets re-validated on next entry
        _waveHUD = null;

        { /* Exited dungeon */ }
        OnDungeonExited?.Invoke();
        Notify("Đã rời phó bản!");
    }

    public void OnWaveStateUpdated(int currentRound, int maxRounds, int remainingSeconds)
    {
        _currentWaveRound = Mathf.Max(0, currentRound);
        _currentWaveMaxRounds = Mathf.Max(0, maxRounds);
        _currentWaveRemainingSeconds = Mathf.Max(0, remainingSeconds);

        if (_currentWaveRemainingSeconds > 0)
        {
            _waveCountdownEndRealtime = Time.realtimeSinceStartup + _currentWaveRemainingSeconds;
            _waveCountdownActive = true;
        }
        else
        {
            _waveCountdownEndRealtime = 0f;
            _waveCountdownActive = false;
        }

        if (_currentWaveRound > 0)
            EnsureWaveHUD();

        { /* Wave state updated | round={_currentWaveRound}/{_currentWaveMaxRounds} remaining={_currentWaveRemainingSeconds}s */ }
        OnWaveStateChanged?.Invoke(_currentWaveRound, _currentWaveMaxRounds, _currentWaveRemainingSeconds);
    }

    // Auto-creates a persistent WaveHUD canvas if no WaveHUD exists in the scene.
    // Called when the server sends the first wave state with round > 0.
    private void EnsureWaveHUD()
    {
        if (_waveHUD != null) return;
        _waveHUD = FindObjectOfType<WaveHUD>();
        if (_waveHUD != null) return;

        // Create Canvas (DDOL — persists across scene loads with DungeonManager)
        var canvasGo = new GameObject("WaveHUD_Canvas");
        DontDestroyOnLoad(canvasGo);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode  = UnityEngine.RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // WaveHUD panel (child of canvas)
        var hudGo = new GameObject("WaveHUD");
        hudGo.transform.SetParent(canvasGo.transform, false);
        _waveHUD = hudGo.AddComponent<WaveHUD>();
        // WaveHUD.Start() -> AutoCreateUI() will create the RectTransform + TMP labels
        { /* Auto-created WaveHUD canvas for wave dungeon */ }
    }

    public void OnDungeonRuntimeStatusUpdated(string message)
    {
        _currentDungeonStatusMessage = message ?? string.Empty;
        { /* Runtime status updated | message='{_currentDungeonStatusMessage}' */ }
        OnDungeonStatusMessage?.Invoke(_currentDungeonStatusMessage);
    }

    // ========================================================================
    //  UTILITIES
    // ========================================================================

    private void Notify(string msg)
    {
        { /* Thực hiện ghi log */ }
        OnDungeonStatusMessage?.Invoke(msg);
    }
}
