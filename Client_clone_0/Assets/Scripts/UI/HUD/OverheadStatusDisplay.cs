using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

// OverheadStatusDisplay – Hiển thị icon buff/debuff trên đầu player/enemy trong World Space Canvas.
// Setup:
// • Thêm component này vào PlayerHpBarCanvas (World Space canvas con của Player prefab).
// • Thêm vào enemy health bar canvas của Enemy prefab.
// • Gán statusIconPrefab (StatusIconEntry prefab).
// • Script tự subscribe DebuffManager.OnDebuffsChanged và PlayerBuffSync.OnBuffStateChanged.
// Layout:
// • Icon xếp ngang (HorizontalLayoutGroup).
// • Icon debuff (bất lợi) hiện bình thường.
// • Icon buff (có lợi) hiện với viền vàng nhạt.
// • Mỗi icon update countdown riêng (kể cả giây đối với buff lẫn debuff).
// Countdown được thể hiện qua CountdownRing (Radial360 fill) bên trong mỗi StatusIconEntry.
public class OverheadStatusDisplay : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("Prefab StatusIconEntry (32×32) – tạo qua GameTools → Skill Effects → Create Status Icon Prefab")]
    [SerializeField] private StatusIconEntry statusIconPrefab;

    [Header("Layout")]
    [Tooltip("Container chứa các icon. Nên có HorizontalLayoutGroup.")]
    [SerializeField] private RectTransform iconContainer;

    [Header("Buff Colors")]
    [SerializeField] private Color buffRingColor   = new Color(1f, 0.9f, 0.2f, 1f);   // vàng
    [SerializeField] private Color debuffRingColor = new Color(1f, 0.2f, 0.2f, 1f);   // đỏ

    // Pool
    private readonly List<StatusIconEntry> _pool = new List<StatusIconEntry>();

    // Refs
    private DebuffManager   _debuffManager;
    private PlayerBuffSync  _buffSync;      // null nếu là enemy

    // Dùng để tính remaining seconds từ DebuffEntry
    private float _serverTimeOffset; // client correction — ít dùng vì chỉ hiển thị

    // Hàm vòng đời của Unity hoặc ASP.NET được gọi tự động.

    private void Awake()
    {
        // Container fallback: dùng chính transform này
        if (iconContainer == null)
            iconContainer = GetComponent<RectTransform>();
    }

    private void Start()
    {
        // Tìm DebuffManager trên root object
        _debuffManager = GetComponentInParent<DebuffManager>();
        _buffSync      = GetComponentInParent<PlayerBuffSync>();

        if (_debuffManager != null)
            _debuffManager.OnDebuffsChanged += RefreshAll;

        if (_buffSync != null)
            _buffSync.OnBuffStateChanged += RefreshAll;

        RefreshAll();
    }

    private void OnDestroy()
    {
        if (_debuffManager != null)
            _debuffManager.OnDebuffsChanged -= RefreshAll;

        if (_buffSync != null)
            _buffSync.OnBuffStateChanged -= RefreshAll;
    }

    private void Update()
    {
        // Poll remaining seconds cho mỗi entry active
        if (_debuffManager == null) return;

        float now = GetServerTime();
        int poolIndex = 0;

        // Debuffs
        var debuffs = _debuffManager.ActiveDebuffs;
        for (int i = 0; i < debuffs.Count && poolIndex < _pool.Count; i++, poolIndex++)
        {
            float remaining = Mathf.Max(0f, debuffs[i].ExpireServerTime - now);
            _pool[poolIndex].UpdateCountdown(remaining);
        }

        // Buff: ArmorBuff
        if (_buffSync != null)
        {
            float armorRemaining = _buffSync.GetArmorBuffRemaining();
            if (armorRemaining > 0f && poolIndex < _pool.Count)
            {
                _pool[poolIndex].UpdateCountdown(armorRemaining);
                poolIndex++;
            }
            float attackRemaining = _buffSync.GetAttackBuffRemaining();
            if (attackRemaining > 0f && poolIndex < _pool.Count)
            {
                _pool[poolIndex].UpdateCountdown(attackRemaining);
                poolIndex++;
            }
        }
    }

    // Refresh

    private void RefreshAll()
    {
        ReturnAllToPool();
        if (statusIconPrefab == null) return;

        float now = GetServerTime();

        // Debuffs
        if (_debuffManager != null)
        {
            var debuffs = _debuffManager.ActiveDebuffs;

            for (int i = 0; i < debuffs.Count; i++)
            {
                var entry    = debuffs[i];
                float remain = Mathf.Max(0f, entry.ExpireServerTime - now);
                if (remain <= 0f) continue;

                var icon = GetFromPool();
                icon.Bind(entry.IconId, entry.TotalDuration);
                icon.UpdateCountdown(remain);
                SetRingColor(icon, debuffRingColor);

            }

        }

        // Buffs (PlayerBuffSync, chỉ trên player)
        if (_buffSync != null)
        {
            float armorRemain = _buffSync.GetArmorBuffRemaining();
            if (armorRemain > 0f)
            {
                var icon = GetFromPool();
                icon.Bind(_buffSync.armorIconId.Value, _buffSync.armorBuffValue.Value > 0 ? armorRemain : armorRemain);
                icon.UpdateCountdown(armorRemain);
                SetRingColor(icon, buffRingColor);
            }

            float attackRemain = _buffSync.GetAttackBuffRemaining();
            if (attackRemain > 0f)
            {
                var icon = GetFromPool();
                icon.Bind(_buffSync.attackIconId.Value, attackRemain);
                icon.UpdateCountdown(attackRemain);
                SetRingColor(icon, buffRingColor);
            }
        }
    }

    // Pool

    private StatusIconEntry GetFromPool()
    {
        // Tìm entry inactive trong pool
        foreach (var entry in _pool)
        {
            if (!entry.gameObject.activeSelf)
            {
                entry.gameObject.SetActive(true);
                return entry;
            }
        }

        // Tạo mới
        var newEntry = Instantiate(statusIconPrefab, iconContainer);
        _pool.Add(newEntry);
        return newEntry;
    }

    private void ReturnAllToPool()
    {
        foreach (var entry in _pool)
            entry.gameObject.SetActive(false);
    }

    private static void SetRingColor(StatusIconEntry entry, Color color)
    {
        // Truy cập countdownRing Image qua child — tìm Image có fillMethod
        var images = entry.GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            if (img.type == Image.Type.Filled)
            {
                img.color = color;
                break;
            }
        }
    }

    private float GetServerTime()
    {
        if (NetworkManager.Singleton != null)
            return (float)NetworkManager.Singleton.ServerTime.TimeAsFloat;
        return Time.time;
    }
}
