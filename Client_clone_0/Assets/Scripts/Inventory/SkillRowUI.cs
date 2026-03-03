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
    public void SetData(PlayerSkillInfo info, int playerId, Action onUpgraded)
    {
        _info       = info;
        _playerId   = playerId;
        _onUpgraded = onUpgraded;

        RefreshUI();

        // Wire upgrade button
        if (btnUpgrade == null)
        {
            Debug.LogError($"[SkillRowUI] btnUpgrade là NULL trên prefab '{gameObject.name}'! " +
                            "Chưa kéo BtnUpgrade vào slot trong Prefab Inspector.");
            return;
        }

        btnUpgrade.onClick.RemoveAllListeners();
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
            if (maxed)
                txtRequire.text = "<color=#FFD700>Đã đạt tối đa</color>";
            else
                txtRequire.text = _info.can_upgrade
                    ? $"<color=#00FF88>Nâng: {_info.next_level_sp_cost} SP • cần lv.{_info.next_level_player_req}</color>"
                    : $"<color=#FF8888>Khoá đến lv.{_info.next_level_player_req} • cần {_info.next_level_sp_cost} SP</color>";
        }

        if (txtDesc != null)
            txtDesc.text = maxed ? "(Max)" : _info.next_level_desc;

        // Nút "+" chỉ active khi có thể nâng
        if (btnUpgrade != null)
            btnUpgrade.interactable = _info.can_upgrade && !maxed;
    }

    private void OnClickUpgrade()
    {
        Debug.Log($"[SkillRowUI][OnClick] CALLED – skill='{_info?.skill_name}' playerId={_playerId} can_upgrade={_info?.can_upgrade} cur={_info?.current_level}/{_info?.max_level} APIClient={(APIClient.Instance != null ? "OK" : "NULL!")}");

        if (_info == null)              { Debug.LogError("[SkillRowUI] _info NULL"); return; }
        if (APIClient.Instance == null) { Debug.LogError("[SkillRowUI] APIClient.Instance NULL"); return; }
        if (!_info.can_upgrade)         { Debug.LogWarning("[SkillRowUI] can_upgrade=false"); return; }
        if (_info.current_level >= _info.max_level) { Debug.LogWarning("[SkillRowUI] Đã max level"); return; }

        Debug.Log($"[SkillRowUI] Gọi UpgradeSkill playerId={_playerId} skillId={_info.skill_id}");

        // Tắt nút trong lúc chờ
        if (btnUpgrade != null) btnUpgrade.interactable = false;

        APIClient.Instance.UpgradeSkill(
            _playerId,
            _info.skill_id,
            onSuccess: _ =>
            {
                Debug.Log($"[SkillRowUI] Đã nâng {_info.skill_name} lên Lv.{_info.current_level + 1}");
                _onUpgraded?.Invoke();   // SkillTabUI sẽ reload toàn bộ list
            },
            onError: err =>
            {
                Debug.LogError($"[SkillRowUI] Lỗi nâng skill: {err}");
                // Bật lại nút nếu lỗi
                if (btnUpgrade != null) btnUpgrade.interactable = _info.can_upgrade;
            }
        );
    }

    #endregion
}
