using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SkillRowUI – Một dòng hiển thị thông tin 1 skill trong tab Kỹ Năng.
///
/// Cấu trúc GameObject gợi ý:
/// ┌─ SkillRow
/// │   ├─ IconImage           [Image]   – icon skill (tuỳ chọn)
/// │   ├─ TxtSkillName        [TMP_Text] – tên skill + element
/// │   ├─ TxtLevel            [TMP_Text] – "Lv.2 / 5"
/// │   ├─ TxtRequire          [TMP_Text] – "Cần lv.X" hoặc "Đã tối đa"
/// │   ├─ TxtDesc             [TMP_Text] – mô tả efect ở level hiện tại
/// │   └─ BtnUpgrade          [Button]  – nút "+"
///
/// Lưu ý: Prefab này được SkillTabUI instantiate tự động.
/// </summary>
public class SkillRowUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text txtSkillName;
    [SerializeField] private TMP_Text txtLevel;
    [SerializeField] private TMP_Text txtRequire;
    [SerializeField] private TMP_Text txtDesc;
    [SerializeField] private Button   btnUpgrade;
    [SerializeField] private Image    iconImage;

    // ── Internal state ─────────────────────────────────────
    private PlayerSkillInfo _info;
    private int             _playerId;
    private Action          _onUpgraded;        // callback để SkillTabUI refresh
    private bool            _isReadOnlyView;

    // ───────────────────────────────────────────────────────
    private void Awake()
    {
        // Tắt Raycast Target trên mọi Image không phải target graphic của Button,
        // để tránh các Image trang trí (Background, root Image...) chặn click xuống BtnUpgrade.
        foreach (var img in GetComponentsInChildren<Image>(includeInactive: true))
        {
            // Giữ lại raycast chỉ cho graphic của Button
            bool isButtonTarget = img.GetComponent<Button>() != null
                               || (img.transform.parent != null &&
                                   img.transform.parent.GetComponent<Button>() != null &&
                                   img.transform.parent.GetComponent<Button>().targetGraphic == img);
            Debug.Log($"[SkillRowUI][Awake] Image '{img.gameObject.name}' raycastTarget={img.raycastTarget} → isButtonTarget={isButtonTarget}");
            if (!isButtonTarget)
                img.raycastTarget = false;
        }
    }

    // ───────────────────────────────────────────────────────
    #region Public API

    /// <summary>
    /// Khởi tạo dòng skill với dữ liệu từ API.
    /// </summary>
    /// <param name="info">Thông tin skill trả từ server</param>
    /// <param name="playerId">ID của player</param>
    /// <param name="onUpgraded">Callback sau khi nâng cấp thành công</param>
    public void SetData(PlayerSkillInfo info, int playerId, Action onUpgraded, bool readOnly = false)
    {
        _info       = info;
        _playerId   = playerId;
        _onUpgraded = onUpgraded;
        _isReadOnlyView = readOnly;

        RefreshUI();

        // Wire upgrade button
        if (btnUpgrade == null)
        {
            Debug.LogError($"[SkillRowUI] btnUpgrade là NULL trên prefab '{gameObject.name}'! " +
                            "Chưa kéo BtnUpgrade vào slot trong Prefab Inspector.");
            return;
        }

        btnUpgrade.onClick.RemoveAllListeners();
        if (_isReadOnlyView)
        {
            btnUpgrade.gameObject.SetActive(false);
            Debug.Log($"[SkillRowUI][SetData] '{info.skill_name}' in read-only mode.");
            return;
        }

        // Log thẳng trên sự kiện onClick – cái này kích hoạt TRƯỚC OnClickUpgrade
        btnUpgrade.onClick.AddListener(() =>
            Debug.Log($"[BtnUpgrade] ===== CLICK NHẬN ĐƯỢC ===== skill='{_info?.skill_name}' interactable={btnUpgrade.interactable}")
        );
        btnUpgrade.onClick.AddListener(OnClickUpgrade);
        Debug.Log($"[SkillRowUI][SetData] '{info.skill_name}' – btnUpgrade.interactable={btnUpgrade.interactable} | btnUpgrade.enabled={btnUpgrade.enabled} | GO.activeInHierarchy={btnUpgrade.gameObject.activeInHierarchy}");
    }

    #endregion

    // ───────────────────────────────────────────────────────
    #region Private helpers

    private void RefreshUI()
    {
        if (_info == null) return;

        // Tên skill + hệ (nếu có)
        string elementTag = string.IsNullOrEmpty(_info.element_type)
            ? "[Universal]"
            : $"[{_info.element_type}]";
        if (txtSkillName != null)
            txtSkillName.text = $"{elementTag} {_info.skill_name}";

        // Level hiện tại / max
        if (txtLevel != null)
            txtLevel.text = $"Lv.{_info.current_level} / {_info.max_level}";

        // Mô tả efect & yêu cầu nâng cấp
        bool maxed = _info.current_level >= _info.max_level;
        if (txtRequire != null)
        {
            if (_isReadOnlyView)
            {
                txtRequire.text = "<color=#8FD3FF>Chế độ xem hồ sơ bạn bè</color>";
            }
            else if (maxed)
            {
                txtRequire.text = "<color=#FFD700>Đã đạt tối đa</color>";
            }
            else
            {
                // Kiểm tra gene_tier_required
                int playerGeneTier = GameManager.Instance?.GetPlayerData()?.gene_tier ?? 0;
                bool geneTierOk = playerGeneTier >= _info.gene_tier_required;

                if (_info.gene_tier_required > 0 && !geneTierOk)
                {
                    txtRequire.text = $"<color=#FF8888>Cần Gene Tier {_info.gene_tier_required} • bạn có Tier {playerGeneTier}</color>";
                }
                else
                {
                    txtRequire.text = _info.can_upgrade
                        ? $"<color=#00FF88>Nâng: {_info.next_level_sp_cost} SP • cần lv.{_info.next_level_player_req}</color>"
                        : $"<color=#FF8888>Khoá đến lv.{_info.next_level_player_req} • cần {_info.next_level_sp_cost} SP</color>";
                }
            }
        }

        if (txtDesc != null)
            txtDesc.text = _isReadOnlyView
                ? (string.IsNullOrWhiteSpace(_info.description) ? "Không có mô tả." : _info.description)
                : (maxed ? "(Max)" : _info.next_level_desc);

        // Nút "+" chỉ active khi có thể nâng
        if (btnUpgrade != null)
        {
            btnUpgrade.gameObject.SetActive(!_isReadOnlyView);
            btnUpgrade.interactable = !_isReadOnlyView && _info.can_upgrade && !maxed;
        }

        // Icon skill (ưu tiên icon_id từ server, fallback sang skill_code)
        if (iconImage != null)
        {
            string iconKey = !string.IsNullOrEmpty(_info.icon_id)
                ? _info.icon_id
                : _info.skill_code;
            Sprite icon = SkillIconDatabase.Instance != null
                ? SkillIconDatabase.Instance.GetIcon(iconKey)
                : null;
            iconImage.sprite  = icon;
            iconImage.enabled = icon != null;
        }
    }

    private void OnClickUpgrade()
    {
        if (_isReadOnlyView)
        {
            Debug.LogWarning($"[SkillRowUI] Ignored upgrade click in read-only mode for skill='{_info?.skill_name}'.");
            return;
        }

        Debug.Log($"[SkillRowUI][OnClick] CALLED – skill='{_info?.skill_name}' can_upgrade={_info?.can_upgrade}");

        if (_info == null)                                  { Debug.LogError("[SkillRowUI] _info NULL"); return; }
        if (GameplayCommandService.Instance == null)        { Debug.LogError("[SkillRowUI] GameplayCommandService NULL"); return; }
        if (!_info.can_upgrade)                             { Debug.LogWarning("[SkillRowUI] can_upgrade=false"); return; }
        if (_info.current_level >= _info.max_level)         { Debug.LogWarning("[SkillRowUI] Đã max level"); return; }

        if (btnUpgrade != null) btnUpgrade.interactable = false;

        GameplayCommandService.OnSkillUpgraded -= HandleSkillUpgraded;
        GameplayCommandService.OnSkillUpgraded += HandleSkillUpgraded;
        GameplayCommandService.Instance.UpgradeSkillServerRpc(_info.skill_id);
    }

    private void HandleSkillUpgraded(string json)
    {
        GameplayCommandService.OnSkillUpgraded -= HandleSkillUpgraded;

        if (json.Contains("\"error\""))
        {
            Debug.LogError($"[SkillRowUI] Lỗi nâng skill: {json}");
            if (btnUpgrade != null) btnUpgrade.interactable = _info?.can_upgrade ?? false;
            return;
        }

        int newLevel = (_info?.current_level ?? 0) + 1;
        Debug.Log($"[SkillRowUI] Đã nâng {_info?.skill_name} lên Lv.{newLevel}");

        var pd = GameManager.Instance?.GetPlayerData();
        if (pd?.skills != null)
        {
            foreach (var s in pd.skills)
            {
                if (s.skill_id == _info?.skill_id) { s.level = newLevel; break; }
            }
        }

        _onUpgraded?.Invoke();
    }

    #endregion
}
