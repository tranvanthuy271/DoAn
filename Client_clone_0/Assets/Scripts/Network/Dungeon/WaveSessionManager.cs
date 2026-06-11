using System;
using System.Collections.Generic;
using UnityEngine;

// WaveSessionManager — Quản lý session phó bản wave per-player, hoàn toàn in-memory (không DB).
// Trách nhiệm:
// 1. Theo dõi số lượt phó bản wave theo ngày.
// - Mỗi user có 1 lượt free/ngày.
// - Dùng item 409/410 sẽ cộng thêm lượt cho CHÍNH ngày đó.
// - Reset lúc 00:00 theo múi giờ server/VN.
// 2. Lưu session hoạt động của từng người chơi (keyed by userId):
// - Khi người chơi ngắt kết nối (disconnect), session được GIỮ NGUYÊN.
// - Timer vẫn chạy trong WaveDungeonRuntime trên server.
// - Khi reconnect và gọi lại RequestDungeonEntryServerRpc, server phục hồi họ
// về đúng zone cũ (thời gian tiếp tục, không reset).
// Gắn vào: ServerBootstrap (cùng GameObject với ZoneTransitionController,
// ZonePlayerSessionManager, WaveDungeonRuntime…)
[DisallowMultipleComponent]
public class WaveSessionManager : MonoBehaviour
{
    public static WaveSessionManager Instance { get; private set; }

    public static WaveSessionManager GetOrCreateInstance(GameObject host = null)
    {
        if (Instance != null)
            return Instance;

        Instance = FindAnyObjectByType<WaveSessionManager>();
        if (Instance != null)
            return Instance;

        GameObject owner = host;
        if (owner == null)
        {
            owner = GameObject.Find("ServerBootstrap");
            if (owner == null)
                owner = new GameObject("WaveSessionManager");
        }

        Instance = owner.GetComponent<WaveSessionManager>();
        if (Instance == null)
        {
            Instance = owner.AddComponent<WaveSessionManager>();
            { /* Cảnh báo: Instance bị thiếu ở runtime. Auto-created trên GameObject '{owner.name}' */ }
        }

        return Instance;
    }

    [Header("Giới hạn tham gia mỗi ngày")]
    [Tooltip("Số lượt tham gia tối đa mỗi ngày (mỗi userId / mỗi dungeonId). " +
             "-1 = không giới hạn. Ghi đè bởi config từ API nếu có.")]
    [SerializeField] private int _defaultDailyLimit = 1;

    private static readonly string[] DailyResetTimeZoneIds =
    {
        "SE Asia Standard Time",
        "Asia/Ho_Chi_Minh"
    };

    private static readonly TimeZoneInfo DailyResetTimeZone = ResolveDailyResetTimeZone();

    // Internal models

    private sealed class DailyEntry
    {
        public string DateKey;
        public int    UsedCount;
        public int    BonusCount;
    }

    public sealed class PlayerWaveSession
    {
        public string   UserId;
        public int      DungeonId;
        public int      MapId;
        public int      ZoneId;
        public int      CurrentRound;
        public int      MaxRounds;
        public int      RemainingSeconds;
        public bool     IsActive;
        // Reference đến ZoneRoom để server có thể ExecuteTransferToRoom khi reconnect.
        // Null = zone đã bị giải phóng → session không còn hợp lệ.
        public ZoneRoom ZoneRoom;
        public DateTime SessionStartTime;
    }

    // key: userId
    private readonly Dictionary<string, DailyEntry>        _dailyEntries   = new();
    // key: userId
    private readonly Dictionary<string, PlayerWaveSession> _activeSessions = new();

    // Unity lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            { /* Cảnh báo: Duplicate instance on '{gameObject.name}' (existing='{Instance.gameObject.name}')  destroying duplicate COMPONENT only */ }
            Destroy(this);
            return;
        }
        Instance = this;
        { /* Initialized (in-memory, no DB) */ }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Daily Limit API

    // Kiểm tra người chơi còn lượt phó bản wave hôm nay không.
    public bool CheckDailyLimit(string userId, int dungeonId, int? configuredLimit = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return false;

        int limit = configuredLimit ?? _defaultDailyLimit;
        if (limit < 0)
        {
            { /* CheckDailyLimit userId={userId} dungeonId={dungeonId} limit=unlimited → OK */ }
            return true;
        }

        DailyEntry entry = GetOrCreateDailyEntry(userId);
        int allowed = Mathf.Max(0, limit) + Mathf.Max(0, entry.BonusCount);
        bool ok = entry.UsedCount < allowed;

        { /* CheckDailyLimit userId={userId} dungeonId={dungeonId} used={entry.UsedCount}/{allowed} base={limit} bonus={entry.BonusCount} date={entry.DateKey} → {(ok ? */ }
        return ok;
    }

    // Số lượt đã dùng hôm nay.
    public int GetDailyUsedCount(string userId, int dungeonId)
    {
        return string.IsNullOrWhiteSpace(userId) ? 0 : GetOrCreateDailyEntry(userId).UsedCount;
    }

    public int GetDailyBonusCount(string userId)
    {
        return string.IsNullOrWhiteSpace(userId) ? 0 : GetOrCreateDailyEntry(userId).BonusCount;
    }

    public int GetDailyAllowedCount(string userId, int? configuredLimit = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return 0;

        int limit = configuredLimit ?? _defaultDailyLimit;
        if (limit < 0)
            return int.MaxValue;

        DailyEntry entry = GetOrCreateDailyEntry(userId);
        return Mathf.Max(0, limit) + Mathf.Max(0, entry.BonusCount);
    }

    public int GetDailyRemainingCount(string userId, int dungeonId, int? configuredLimit = null)
    {
        int allowed = GetDailyAllowedCount(userId, configuredLimit);
        if (allowed == int.MaxValue)
            return int.MaxValue;

        return Mathf.Max(0, allowed - GetDailyUsedCount(userId, dungeonId));
    }

    public void AddBonusEntries(string userId, int amount)
    {
        if (string.IsNullOrWhiteSpace(userId) || amount <= 0)
            return;

        DailyEntry entry = GetOrCreateDailyEntry(userId);
        entry.BonusCount += amount;

        { /* AddBonusEntries userId={userId} add={amount} bonusToday={entry.BonusCount} usedToday={entry.UsedCount} remaining={GetDailyRemainingCount(userId, 0)} */ }
    }

    // Tăng bộ đếm lượt đã dùng lên 1.
    // Phải gọi SAU khi xác nhận cho phép vào dungeon và ZoneRoom đã được tạo.
    public void ConsumeEntry(string userId, int dungeonId)
    {
        if (string.IsNullOrEmpty(userId)) return;

        DailyEntry entry = GetOrCreateDailyEntry(userId);
        entry.UsedCount++;

        { /* ConsumeEntry userId={userId} dungeonId={dungeonId} usedToday={entry.UsedCount} bonusToday={entry.BonusCount} remaining={GetDailyRemainingCount(userId, dungeonId)} */ }
    }

    // Session API

    // Người chơi có session wave đang hoạt động không?
    public bool HasActiveSession(string userId)
        => !string.IsNullOrEmpty(userId)
           && _activeSessions.TryGetValue(userId, out var s)
           && s != null
           && s.IsActive;

    // Lấy session hiện tại của người chơi. Null nếu không có.
    public PlayerWaveSession GetSession(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return null;
        _activeSessions.TryGetValue(userId, out var s);
        return s;
    }

    public bool HasActiveSessionInZone(int mapId, int zoneId)
    {
        foreach (var kv in _activeSessions)
        {
            PlayerWaveSession session = kv.Value;
            if (session != null && session.IsActive && session.MapId == mapId && session.ZoneId == zoneId)
                return true;
        }

        return false;
    }

    public bool HasActiveSessionRoom(ZoneRoom room)
    {
        if (room == null)
            return false;

        foreach (var kv in _activeSessions)
        {
            PlayerWaveSession session = kv.Value;
            if (session == null || !session.IsActive)
                continue;

            bool sameRoom = ReferenceEquals(session.ZoneRoom, room)
                || (session.MapId == room.MapId && session.ZoneId == room.ZoneId);

            if (!sameRoom)
                continue;

            if (session.ZoneRoom == null)
                session.ZoneRoom = room;

            return true;
        }

        return false;
    }

    // Đăng ký session mới cho người chơi vừa vào dungeon wave.
    // Gọi sau khi ZoneRoom đã được tạo thành công.
    public void BeginSession(string userId, int dungeonId, int mapId, int zoneId, ZoneRoom zoneRoom)
    {
        if (string.IsNullOrEmpty(userId))
        {
            { /* Cảnh báo: BeginSession gọi với userId rỗng  bỏ qua */ }
            return;
        }

        var session = new PlayerWaveSession
        {
            UserId           = userId,
            DungeonId        = dungeonId,
            MapId            = mapId,
            ZoneId           = zoneId,
            CurrentRound     = 0,
            MaxRounds        = 0,
            RemainingSeconds = 0,
            IsActive         = true,
            ZoneRoom         = zoneRoom,
            SessionStartTime = DateTime.UtcNow,
        };
        _activeSessions[userId] = session;
        { /* BeginSession userId={userId} dungeonId={dungeonId} mapId={mapId} zoneId={zoneId} room={(zoneRoom != null ? zoneRoom.ZoneId.ToString() */ }
    }

    // Cập nhật trạng thái wave (round, timer) cho tất cả session thuộc zoneId.
    // Gọi từ WaveDungeonRuntime mỗi giây / mỗi khi round thay đổi.
    public void UpdateSessionStateByZone(int zoneId, int currentRound, int maxRounds, int remainingSeconds)
    {
        foreach (var kv in _activeSessions)
        {
            var s = kv.Value;
            if (s != null && s.IsActive && s.ZoneId == zoneId)
            {
                s.CurrentRound     = currentRound;
                s.MaxRounds        = maxRounds;
                s.RemainingSeconds = remainingSeconds;
            }
        }
    }

    // Khi người chơi ngắt kết nối — session được GIỮ NGUYÊN.
    // Timer vẫn chạy ở WaveDungeonRuntime trên server.
    // Khi reconnect, client gọi RequestDungeonEntryServerRpc → server phục hồi họ về zone cũ.
    public void OnPlayerDisconnect(string userId)
    {
        // [RECONNECT-DEBUG] Bước 2: log chi tiết trước khi preserve
        PlayerWaveSession dbgSession = null;
        bool found   = !string.IsNullOrEmpty(userId) && _activeSessions.TryGetValue(userId, out dbgSession);
        bool active  = found && dbgSession != null && dbgSession.IsActive;
        { /* [2-WaveDisconnect] userId={userId} */ }

        if (HasActiveSession(userId))
        {
            var s = _activeSessions[userId];
            if (s.ZoneRoom != null)
            {
                ZoneRoom restoredRoom = ZoneRoomRegistry.Instance?.EnsureRoomRegistered(s.ZoneRoom);
                { /* [2a-EnsureRegistered] userId={userId} zoneKey={s.ZoneRoom.ZoneKey} */ }
                if (restoredRoom != null)
                    s.ZoneRoom = restoredRoom;
            }
            else
            {
                s.ZoneRoom = ZoneRoomRegistry.Instance?.EnsureCustomRoomRegistered(s.MapId, s.ZoneId);
                { /* [2b-EnsureCustomRegistered] userId={userId} map={s.MapId} zone={s.ZoneId} */ }
            }

            ZoneRoomRegistry.Instance?.MarkRoomPreserved(s.ZoneRoom, $"disconnect userId={userId}");
            { /* [2c-MarkPreserved] userId={userId} ZoneRoom={(s.ZoneRoom != null ? s.ZoneRoom.ZoneKey */ }
            { /* OnPlayerDisconnect userId={userId} dungeonId={s.DungeonId} */ }
            // Không xóa session — IsActive vẫn = true
        }
        else
        {
            { /* Cảnh báo: [2-WaveDisconnect] userId={userId}  HasActiveSession=false */ }
            { /* OnPlayerDisconnect userId={userId}  không có active session */ }
        }
    }

    // Kết thúc session của người chơi (exit bình thường hoặc encounter kết thúc).
    public void EndSession(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return;
        if (_activeSessions.TryGetValue(userId, out var s) && s != null)
        {
            s.IsActive = false;
            ZoneRoomRegistry.Instance?.ReleasePreservedRoom(s.MapId, s.ZoneId);
            { /* EndSession userId={userId} dungeonId={s.DungeonId} finalRound={s.CurrentRound} */ }
        }
    }

    // Kết thúc tất cả session thuộc zone (gọi khi encounter kết thúc — boss đã diệt hoặc hết giờ).
    public void EndSessionsByZone(int zoneId)
    {
        bool releasedRoom = false;
        foreach (var kv in _activeSessions)
        {
            var s = kv.Value;
            if (s != null && s.IsActive && s.ZoneId == zoneId)
            {
                s.IsActive = false;
                if (!releasedRoom)
                {
                    ZoneRoomRegistry.Instance?.ReleasePreservedRoom(s.MapId, s.ZoneId);
                    releasedRoom = true;
                }
                { /* EndSessionsByZone userId={kv.Key} zoneId={zoneId} finalRound={s.CurrentRound} */ }
            }
        }
    }

    // Hàm hỗ trợ dùng nội bộ để tách nhỏ xử lý chính.

    private DailyEntry GetOrCreateDailyEntry(string userId)
    {
        string key = BuildEntryKey(userId);
        string today = GetTodayKey();

        if (_dailyEntries.TryGetValue(key, out var entry))
        {
            if (!string.Equals(entry.DateKey, today, StringComparison.Ordinal))
            {
                { /* Daily reset userId={userId} prevDate={entry.DateKey} newDate={today} used={entry.UsedCount} bonus={entry.BonusCount} */ }
                entry.DateKey = today;
                entry.UsedCount = 0;
                entry.BonusCount = 0;
            }

            return entry;
        }

        entry = new DailyEntry
        {
            DateKey = today,
            UsedCount = 0,
            BonusCount = 0
        };
        _dailyEntries[key] = entry;
        return entry;
    }

    private static string BuildEntryKey(string userId) => userId;

    private static string GetTodayKey()
    {
        DateTime localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, DailyResetTimeZone);
        return localNow.ToString("yyyy-MM-dd");
    }

    private static TimeZoneInfo ResolveDailyResetTimeZone()
    {
        for (int i = 0; i < DailyResetTimeZoneIds.Length; i++)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(DailyResetTimeZoneIds[i]);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        { /* Cảnh báo: Không resolve được múi giờ VN. Fallback về TimeZoneInfo.Local */ }
        return TimeZoneInfo.Local;
    }
}
