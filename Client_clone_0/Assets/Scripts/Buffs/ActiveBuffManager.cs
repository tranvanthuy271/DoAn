using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// ActiveBuffManager – Singleton quản lý danh sách buff đang active trên client local.
///
/// Luồng dữ liệu:
///   1. UseInventoryItem API trả về active_buffs → ItemUseHandler gọi OnBuffsReceived()
///   2. Khi vào game / scene change → gọi LoadFromServer() để đồng bộ từ DB
///   3. Mỗi giây: coroutine TrimExpiredBuffs() xóa buff hết hạn + cập nhật HUD
///
/// Tích hợp buff vào combat:
///   - GetBonusPct("GeneExpBuff") → trả về % cộng thêm vào gene EXP
///   - GetBonusPct("ExpBuff")     → cộng vào EXP kill
///   - GetBonusPct("PhucBuff")    → cộng vào vàng + EXP
///   - GetBonusPct("AttackBuff")  → cộng vào damage deal
///   - GetBonusPct("DefenseBuff") → cộng vào damage reduce
/// </summary>
public class ActiveBuffManager : MonoBehaviour
{
    public static ActiveBuffManager Instance { get; private set; }

    // Danh sách buff đang active (chỉ của local player)
    private readonly List<ActiveBuffDto> _activeBuffs = new List<ActiveBuffDto>();

    // Event: HUD subscribe để tự cập nhật
    private event Action<List<ActiveBuffDto>> _onBuffListChanged;
    public event Action<List<ActiveBuffDto>> OnBuffListChanged
    {
        add
        {
            _onBuffListChanged += value;
            // Ngay khi có subscriber đầu tiên, gửi snapshot hiện tại để HUD không bị bỏ lỡ
            // trường hợp LoadFromServer đã hoàn thành trước khi panel subscribe.
            if (_activeBuffs.Count > 0)
                value?.Invoke(GetActiveBuffs());
        }
        remove { _onBuffListChanged -= value; }
    }

    // Interval kiểm tra buff hết hạn (giây)
    private const float TrimInterval = 1f;

    /// <summary>
    /// Fired mỗi giây với tổng HP/s và MP/s cần hồi từ HpRestoreOverTime / MpRestoreOverTime buff.
    /// InventoryNetworkBridge subscribe để gửi HealTickServerRpc lên NGO.
    /// </summary>
    public static event System.Action<int, int> OnHealTick;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartCoroutine(TrimExpiredBuffsLoop());

        // Nếu player data đã có sẵn khi start (vd: login xong rồi mới load scene)
        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
            LoadFromServer();
    }

    private void OnEnable()
    {
        // Subscribe để load buff ngay khi player data được set (xử lý async login)
        GameManager.OnPlayerDataSet += OnPlayerDataReady;
    }

    private void OnDisable()
    {
        GameManager.OnPlayerDataSet -= OnPlayerDataReady;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Thay thế toàn bộ danh sách buff bằng dữ liệu từ server.
    /// Gọi sau UseInventoryItem hoặc khi load game.
    /// </summary>
    public void OnBuffsReceived(ActiveBuffDto[] buffs)
    {
        _activeBuffs.Clear();
        if (buffs != null)
            _activeBuffs.AddRange(buffs.Where(b => !b.IsExpired()));
        FireChanged();
        Debug.Log($"[ActiveBuffManager] Nhận {_activeBuffs.Count} buff(s) từ server.");
    }

    /// <summary>
    /// Thêm buff mới vào danh sách (từ new_buffs trong UseItemResponse).
    /// Stacking: cùng effectType thì cập nhật expiry.
    /// </summary>
    public void OnBuffsAdded(ActiveBuffDto[] newBuffs)
    {
        if (newBuffs == null) return;
        foreach (var b in newBuffs)
        {
            if (b.IsExpired()) continue;
            var existing = _activeBuffs.FirstOrDefault(x => x.effectType == b.effectType);
            if (existing != null)
            {
                existing.expireAt = b.expireAt;
                existing.value    = b.value;
            }
            else
            {
                _activeBuffs.Add(b);
            }
        }
        FireChanged();
    }

    /// <summary>
    /// Load danh sách buff từ server (GET /active-buffs).
    /// Gọi khi vào game / sau scene transition.
    /// </summary>
    public void LoadFromServer()
    {
        if (APIClient.Instance == null) return;
        int playerId = GetPlayerId();
        if (playerId == 0) return;

        APIClient.Instance.GetActiveBuffs(playerId,
            buffs => OnBuffsReceived(buffs),
            err  => Debug.LogWarning($"[ActiveBuffManager] Không load được buff: {err}"));
    }

    // ── Internal ──────────────────────────────────────────────────────────

    /// <summary>
    /// Callback khi GameManager.OnPlayerDataSet fire — reload buff từ server.
    /// Xử lý trường hợp player data load xong sau Start() (async).
    /// </summary>
    private void OnPlayerDataReady(PlayerDataResponse data)
    {
        LoadFromServer();
    }

    /// <summary>Trả về tổng % bonus của effectType (e.g. GeneExpBuff → 0.20 nếu value=20).</summary>
    public float GetBonusPct(string effectType)
    {
        float total = 0;
        foreach (var b in _activeBuffs)
            if (b.effectType == effectType && !b.IsExpired())
                total += b.value / 100f;
        return total;
    }

    /// <summary>Kiểm tra xem có buff loại này đang active không.</summary>
    public bool HasBuff(string effectType)
        => _activeBuffs.Any(b => b.effectType == effectType && !b.IsExpired());

    /// <summary>Lấy snapshot bất biến để HUD render.</summary>
    public List<ActiveBuffDto> GetActiveBuffs() => new List<ActiveBuffDto>(_activeBuffs);

    // ── Internal ──────────────────────────────────────────────────────────

    private IEnumerator TrimExpiredBuffsLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(TrimInterval);

            // ── Heal-over-time tick ───────────────────────────────────────
            int hpTick = 0, mpTick = 0;
            foreach (var b in _activeBuffs)
            {
                if (b.IsExpired()) continue;
                if (b.effectType == "HpRestoreOverTime") hpTick += b.value;
                else if (b.effectType == "MpRestoreOverTime") mpTick += b.value;
            }
            if (hpTick > 0 || mpTick > 0)
                OnHealTick?.Invoke(hpTick, mpTick);

            // ── Trim expired ──────────────────────────────────────────────
            int before = _activeBuffs.Count;
            _activeBuffs.RemoveAll(b => b.IsExpired());
            if (_activeBuffs.Count != before)
            {
                Debug.Log($"[ActiveBuffManager] Xóa {before - _activeBuffs.Count} buff hết hạn.");
                FireChanged();
            }
        }
    }

    private void FireChanged() => _onBuffListChanged?.Invoke(GetActiveBuffs());

    private static int GetPlayerId()
    {
        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
            return GameManager.Instance.GetPlayerData().user_id;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            var serverDataMgr = ServerPlayerDataManager.Instance;
            if (serverDataMgr != null)
            {
                var pd = serverDataMgr.GetPlayerDataForClient(NetworkManager.Singleton.LocalClientId);
                if (pd != null) return pd.user_id;
            }
        }

        return PlayerPrefs.GetInt("USER_ID", 0);
    }
}
