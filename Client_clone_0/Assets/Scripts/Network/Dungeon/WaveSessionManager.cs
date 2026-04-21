using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WaveSessionManager — Quản lý session phó bản wave per-player, hoàn toàn in-memory (không DB).
///
/// Trách nhiệm:
///   1. Theo dõi số lượt phó bản wave theo ngày.
///      - Mỗi user có 1 lượt free/ngày.
///      - Dùng item 409/410 sẽ cộng thêm lượt cho CHÍNH ngày đó.
///      - Reset lúc 00:00 theo múi giờ server/VN.
///
///   2. Lưu session hoạt động của từng người chơi (keyed by userId):
///      - Khi người chơi ngắt kết nối (disconnect), session được GIỮ NGUYÊN.
///      - Timer vẫn chạy trong WaveDungeonRuntime trên server.
///      - Khi reconnect và gọi lại RequestDungeonEntryServerRpc, server phục hồi họ
///        về đúng zone cũ (thời gian tiếp tục, không reset).
///
/// Gắn vào: ServerBootstrap (cùng GameObject với ZoneTransitionController,
///           ZonePlayerSessionManager, WaveDungeonRuntime…)
/// </summary>
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
            Debug.LogWarning($"[WaveSessionManager] Instance bị thiếu ở runtime. Auto-created trên GameObject '{owner.name}'.");
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

    // ─── Internal models ──────────────────────────────────────────────────────

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
        /// <summary>
        /// Reference đến ZoneRoom để server có thể ExecuteTransferToRoom khi reconnect.
        /// Null = zone đã bị giải phóng → session không còn hợp lệ.
        /// </summary>
        public ZoneRoom ZoneRoom;
        public DateTime SessionStartTime;
    }

    // key: userId
    private readonly Dictionary<string, DailyEntry>        _dailyEntries   = new();
    // key: userId
    private readonly Dictionary<string, PlayerWaveSession> _activeSessions = new();

    // ─── Unity lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[WaveSessionManager] Duplicate instance on '{gameObject.name}' (existing='{Instance.gameObject.name}') — destroying duplicate COMPONENT only.");
            Destroy(this);
            return;
        }
        Instance = this;
        Debug.Log("[WaveSessionManager] Initialized (in-memory, no DB).");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ─── Daily Limit API ─────────────────────────────────────────────────────

    /// <summary>Kiểm tra người chơi còn lượt phó bản wave hôm nay không.</summary>
    public bool CheckDailyLimit(string userId, int dungeonId, int? configuredLimit = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return false;

        int limit = configuredLimit ?? _defaultDailyLimit;
        if (limit < 0)
        {
            Debug.Log($"[WaveSessionManager] CheckDailyLimit userId={userId} dungeonId={dungeonId} limit=unlimited → OK");
            return true;
        }

        DailyEntry entry = GetOrCreateDailyEntry(userId);
        int allowed = Mathf.Max(0, limit) + Mathf.Max(0, entry.BonusCount);
        bool ok = entry.UsedCount < allowed;

        Debug.Log($"[WaveSessionManager] CheckDailyLimit userId={userId} dungeonId={dungeonId} used={entry.UsedCount}/{allowed} base={limit} bonus={entry.BonusCount} date={entry.DateKey} → {(ok ? "OK" : "DENIED")}");
        return ok;
    }

    /// <summary>Số lượt đã dùng hôm nay.</summary>
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

        Debug.Log($"[WaveSessionManager] AddBonusEntries userId={userId} add={amount} bonusToday={entry.BonusCount} usedToday={entry.UsedCount} remaining={GetDailyRemainingCount(userId, 0)}");
    }

    /// <summary>
    /// Tăng bộ đếm lượt đã dùng lên 1.
    /// Phải gọi SAU khi xác nhận cho phép vào dungeon và ZoneRoom đã được tạo.
    /// </summary>
    public void ConsumeEntry(string userId, int dungeonId)
    {
        if (string.IsNullOrEmpty(userId)) return;

        DailyEntry entry = GetOrCreateDailyEntry(userId);
        entry.UsedCount++;

        Debug.Log($"[WaveSessionManager] ConsumeEntry userId={userId} dungeonId={dungeonId} usedToday={entry.UsedCount} bonusToday={entry.BonusCount} remaining={GetDailyRemainingCount(userId, dungeonId)}");
    }

    // ─── Session API ─────────────────────────────────────────────────────────

    /// <summary>Người chơi có session wave đang hoạt động không?</summary>
    public bool HasActiveSession(string userId)
        => !string.IsNullOrEmpty(userId)
           && _activeSessions.TryGetValue(userId, out var s)
           && s != null
           && s.IsActive;

    /// <summary>Lấy session hiện tại của người chơi. Null nếu không có.</summary>
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

    /// <summary>
    /// Đăng ký session mới cho người chơi vừa vào dungeon wave.
    /// Gọi sau khi ZoneRoom đã được tạo thành công.
    /// </summary>
    public void BeginSession(string userId, int dungeonId, int mapId, int zoneId, ZoneRoom zoneRoom)
    {
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogWarning("[WaveSessionManager] BeginSession gọi với userId rỗng — bỏ qua.");
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
        Debug.Log($"[WaveSessionManager] BeginSession userId={userId} dungeonId={dungeonId} mapId={mapId} zoneId={zoneId} room={(zoneRoom != null ? zoneRoom.ZoneId.ToString() : "null")}");
    }

    /// <summary>
    /// Cập nhật trạng thái wave (round, timer) cho tất cả session thuộc zoneId.
    /// Gọi từ WaveDungeonRuntime mỗi giây / mỗi khi round thay đổi.
    /// </summary>
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

    /// <summary>
    /// Khi người chơi ngắt kết nối — session được GIỮ NGUYÊN.
    /// Timer vẫn chạy ở WaveDungeonRuntime trên server.
    /// Khi reconnect, client gọi RequestDungeonEntryServerRpc → server phục hồi họ về zone cũ.
    /// </summary>
    public void OnPlayerDisconnect(string userId)
    {
        // ── [RECONNECT-DEBUG] Bước 2: log chi tiết trước khi preserve ────────
        PlayerWaveSession dbgSession = null;
        bool found   = !string.IsNullOrEmpty(userId) && _activeSessions.TryGetValue(userId, out dbgSession);
        bool active  = found && dbgSession != null && dbgSession.IsActive;
        Debug.Log($"[RECONNECT-DEBUG][2-WaveDisconnect] userId={userId} " +
                  $"foundInDict={found} IsActive={active} " +
                  $"ZoneRoom={(found && dbgSession?.ZoneRoom != null ? dbgSession.ZoneRoom.ZoneKey : "null")} " +
                  $"ZoneRoom.IsCustom={(found && dbgSession?.ZoneRoom != null ? dbgSession.ZoneRoom.IsCustom.ToString() : "n/a")} " +
                  $"mapId={(found ? dbgSession?.MapId.ToString() : "n/a")} zoneId={(found ? dbgSession?.ZoneId.ToString() : "n/a")} " +
                  $"ZoneRoomRegistry.Instance={(ZoneRoomRegistry.Instance != null ? "OK" : "NULL!")}");
        // ──────────────────────────────────────────────────────────────────────

        if (HasActiveSession(userId))
        {
            var s = _activeSessions[userId];
            if (s.ZoneRoom != null)
            {
                ZoneRoom restoredRoom = ZoneRoomRegistry.Instance?.EnsureRoomRegistered(s.ZoneRoom);
                Debug.Log($"[RECONNECT-DEBUG][2a-EnsureRegistered] userId={userId} zoneKey={s.ZoneRoom.ZoneKey} " +
                          $"restoredRoom={(restoredRoom != null ? restoredRoom.ZoneKey : "NULL!")}");
                if (restoredRoom != null)
                    s.ZoneRoom = restoredRoom;
            }
            else
            {
                s.ZoneRoom = ZoneRoomRegistry.Instance?.EnsureCustomRoomRegistered(s.MapId, s.ZoneId);
                Debug.Log($"[RECONNECT-DEBUG][2b-EnsureCustomRegistered] userId={userId} map={s.MapId} zone={s.ZoneId} " +
                          $"result={(s.ZoneRoom != null ? s.ZoneRoom.ZoneKey : "NULL! → preserve sẽ thất bại")}");
            }

            ZoneRoomRegistry.Instance?.MarkRoomPreserved(s.ZoneRoom, $"disconnect userId={userId}");
            Debug.Log($"[RECONNECT-DEBUG][2c-MarkPreserved] userId={userId} ZoneRoom={(s.ZoneRoom != null ? s.ZoneRoom.ZoneKey : "NULL → KHÔNG preserved!")}");
            Debug.Log($"[WaveSessionManager] OnPlayerDisconnect userId={userId} dungeonId={s.DungeonId} " +
                      $"zoneId={s.ZoneId} round={s.CurrentRound}/{s.MaxRounds} remaining={s.RemainingSeconds}s " +
                      $"— session PRESERVED. Timer tiếp tục chạy trên server.");
            // Không xóa session — IsActive vẫn = true
        }
        else
        {
            Debug.LogWarning($"[RECONNECT-DEBUG][2-WaveDisconnect] userId={userId} — HasActiveSession=false " +
                             $"→ KHÔNG preserve session! Lý do: found={found} active={active}");
            Debug.Log($"[WaveSessionManager] OnPlayerDisconnect userId={userId} — không có active session.");
        }
    }

    /// <summary>
    /// Kết thúc session của người chơi (exit bình thường hoặc encounter kết thúc).
    /// </summary>
    public void EndSession(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return;
        if (_activeSessions.TryGetValue(userId, out var s) && s != null)
        {
            s.IsActive = false;
            ZoneRoomRegistry.Instance?.ReleasePreservedRoom(s.MapId, s.ZoneId);
            Debug.Log($"[WaveSessionManager] EndSession userId={userId} dungeonId={s.DungeonId} finalRound={s.CurrentRound}");
        }
    }

    /// <summary>
    /// Kết thúc tất cả session thuộc zone (gọi khi encounter kết thúc — boss đã diệt hoặc hết giờ).
    /// </summary>
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
                Debug.Log($"[WaveSessionManager] EndSessionsByZone userId={kv.Key} zoneId={zoneId} finalRound={s.CurrentRound}");
            }
        }
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private DailyEntry GetOrCreateDailyEntry(string userId)
    {
        string key = BuildEntryKey(userId);
        string today = GetTodayKey();

        if (_dailyEntries.TryGetValue(key, out var entry))
        {
            if (!string.Equals(entry.DateKey, today, StringComparison.Ordinal))
            {
                Debug.Log($"[WaveSessionManager] Daily reset userId={userId} prevDate={entry.DateKey} newDate={today} used={entry.UsedCount} bonus={entry.BonusCount}");
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

        Debug.LogWarning("[WaveSessionManager] Không resolve được múi giờ VN. Fallback về TimeZoneInfo.Local.");
        return TimeZoneInfo.Local;
    }
}
