using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;

// ActiveBuffManager – Singleton quản lý danh sách buff đang active trên client local.
// Luồng dữ liệu:
// 1. UseInventoryItem API trả về active_buffs → ItemUseHandler gọi OnBuffsReceived()
// 2. Khi vào game / scene change → gọi LoadFromServer() để đồng bộ từ DB
// 3. Mỗi giây: coroutine TrimExpiredBuffs() xóa buff hết hạn + cập nhật HUD
// Tích hợp buff vào combat:
// - GetBonusPct("GeneExpBuff") → trả về % cộng thêm vào gene EXP
// - GetBonusPct("ExpBuff")     → cộng vào EXP kill
// - GetBonusPct("PhucBuff")    → cộng vào vàng + EXP
// - GetBonusPct("AttackBuff")  → cộng vào damage deal
// - GetBonusPct("DefenseBuff") → cộng vào damage reduce
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

    // Fired mỗi giây với tổng HP/s và MP/s cần hồi từ HpRestoreOverTime / MpRestoreOverTime buff.
    // InventoryNetworkBridge subscribe để gửi HealTickServerRpc lên NGO.
    public static event System.Action<int, int> OnHealTick;

    // Hàm vòng đời của Unity hoặc ASP.NET được gọi tự động.

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (transform.parent != null)
            transform.SetParent(null, true);
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

    // Hàm public để script hoặc hệ thống khác gọi vào.

    // Thay thế toàn bộ danh sách buff bằng dữ liệu từ server.
    // Gọi sau UseInventoryItem hoặc khi load game.
    public void OnBuffsReceived(ActiveBuffDto[] buffs)
    {
        _activeBuffs.Clear();
        if (buffs != null)
            _activeBuffs.AddRange(buffs.Where(b => !b.IsExpired()));
        FireChanged();
        { /* Nhận {_activeBuffs.Count} buff(s) từ server */ }
    }

    // Thêm buff mới vào danh sách (từ new_buffs trong UseItemResponse).
    // Stacking: cùng effectType thì cập nhật expiry.
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

    // Load danh sách buff từ server (GET /active-buffs).
    // Gọi khi vào game / sau scene transition.
    public void LoadFromServer()
    {
        if (GameplayCommandService.Instance == null || !GameplayCommandService.Instance.IsSpawned)
        {
            StartCoroutine(LoadFromServerDirectCoroutine());
            return;
        }

        GameplayCommandService.OnActiveBuffsReceived -= HandleBuffsReceived;
        GameplayCommandService.OnActiveBuffsReceived += HandleBuffsReceived;
        GameplayCommandService.Instance.GetActiveBuffsServerRpc();

        void HandleBuffsReceived(string json)
        {
            GameplayCommandService.OnActiveBuffsReceived -= HandleBuffsReceived;
            if (!json.Contains("\"error\""))
            {
                var wrapper = JsonUtility.FromJson<ActiveBuffsResponse>(json);
                if (wrapper?.active_buffs != null) OnBuffsReceived(wrapper.active_buffs);
            }
            else
            {
                { /* Cảnh báo: Không load được buff: {json} */ }
                StartCoroutine(LoadFromServerDirectCoroutine());
            }
        }
    }

    private IEnumerator LoadFromServerDirectCoroutine()
    {
        int playerId = GetPlayerId();
        if (playerId <= 0)
            yield break;

        int geneSlot = PlayerPrefs.GetInt("ACTIVE_GENE_SLOT", 1) == 2 ? 2 : 1;
        string url = $"{APIClient.BASE_URL}/api/player/{playerId}/active-buffs?geneSlot={geneSlot}";
        using var req = UnityWebRequest.Get(url);
        req.timeout = 10;
        AuthHelper.AddAuthHeader(req);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            string error = !string.IsNullOrWhiteSpace(req.downloadHandler?.text)
                ? req.downloadHandler.text
                : $"HTTP {(long)req.responseCode}: {req.error}";
            { /* Cảnh báo: Direct buff load failed: {error} */ }
            yield break;
        }

        var wrapper = JsonUtility.FromJson<ActiveBuffsResponse>(req.downloadHandler.text);
        if (wrapper?.active_buffs != null)
            OnBuffsReceived(wrapper.active_buffs);
    }

    // Xử lý nội bộ phục vụ các hàm public.

    // Callback khi GameManager.OnPlayerDataSet fire — reload buff từ server.
    // Xử lý trường hợp player data load xong sau Start() (async).
    private void OnPlayerDataReady(PlayerDataResponse data)
    {
        LoadFromServer();
    }

    // Trả về tổng % bonus của effectType (e.g. GeneExpBuff → 0.20 nếu value=20).
    public float GetBonusPct(string effectType)
    {
        float total = 0;
        foreach (var b in _activeBuffs)
            if (b.effectType == effectType && !b.IsExpired())
                total += b.value / 100f;
        return total;
    }

    // Kiểm tra xem có buff loại này đang active không.
    public bool HasBuff(string effectType)
        => _activeBuffs.Any(b => b.effectType == effectType && !b.IsExpired());

    // Lấy snapshot bất biến để HUD render.
    public List<ActiveBuffDto> GetActiveBuffs() => new List<ActiveBuffDto>(_activeBuffs);

    // Push buff từ skill (WaterArmor, EarthAura) vào HUD.
    // Gọi từ PlayerBuffSync khi local player nhận buff từ skill.
    // Không persist lên server — chỉ là visual UI.
    public void PushSkillBuff(ActiveBuffDto dto)
    {
        if (dto == null || dto.IsExpired()) return;

        var existing = _activeBuffs.FirstOrDefault(x => x.effectType == dto.effectType);
        if (existing != null)
        {
            // Refresh expiry nếu mới dài hơn
            double newExpiry   = ParseExpireAt(dto.expireAt);
            double curExpiry   = ParseExpireAt(existing.expireAt);
            if (newExpiry > curExpiry)
            {
                existing.expireAt = dto.expireAt;
                existing.value    = dto.value;
                existing.iconId   = dto.iconId;
                existing.name     = dto.name;
                existing.detail   = dto.detail;
                FireChanged();
            }
        }
        else
        {
            _activeBuffs.Add(dto);
            FireChanged();
        }
    }

    private static double ParseExpireAt(string expireAt)
    {
        if (string.IsNullOrEmpty(expireAt)) return double.MaxValue;
        if (System.DateTime.TryParse(expireAt, null,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out var dt))
            return dt.ToUniversalTime().Subtract(System.DateTime.UnixEpoch).TotalSeconds;
        return 0;
    }

    // Xử lý nội bộ phục vụ các hàm public.

    private IEnumerator TrimExpiredBuffsLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(TrimInterval);

            // Heal-over-time tick
            int hpTick = 0, mpTick = 0;
            foreach (var b in _activeBuffs)
            {
                if (b.IsExpired()) continue;
                if (b.effectType == "HpRestoreOverTime") hpTick += b.value;
                else if (b.effectType == "MpRestoreOverTime") mpTick += b.value;
            }
            if (hpTick > 0 || mpTick > 0)
                OnHealTick?.Invoke(hpTick, mpTick);

            // Trim expired
            int before = _activeBuffs.Count;
            _activeBuffs.RemoveAll(b => b.IsExpired());
            if (_activeBuffs.Count != before)
            {
                { /* Xóa {before - _activeBuffs.Count} buff hết hạn */ }
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
