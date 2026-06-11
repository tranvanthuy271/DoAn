using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// SecondaryGeneSelectPanel — Popup XÁC NHẬN hệ phụ CỐ ĐỊNH (không cho tự chọn).
// Cặp hệ cố định theo cấu hình hybrid của game:
// Hỏa ↔ Thổ  |  Thủy ↔ Mộc  |  Kim ↔ Phong
// INSPECTOR SETUP:
// Header:
// 1. TitleText          → TMP "Hệ Phụ Cố Định"
// 2. WarningText        → TMP cảnh báo 1 lần
// Pair Display:
// 3. PrimaryIcon        → Image icon hệ chính
// 4. PrimaryNameText    → TMP tên + tier hệ chính
// 5. SecondaryIcon      → Image icon hệ phụ
// 6. SecondaryNameText  → TMP tên hệ phụ
// Preview:
// 7. PreviewPanel       → GameObject (ẩn cho đến khi load xong)
// 8. HybridNameText     → TMP tên hybrid
// 9. StatBonusText      → TMP chỉ số bonus
// 10. BonusTargetsText   → TMP hệ bị +50% sát thương
// 11. ImmuneText         → TMP hệ miễn nhiễm
// Controls:
// 12. StatusText         → TMP
// 13. LoadingOverlay     → GameObject
// 14. ConfirmButton      → Button
// 15. CloseButton        → Button
// Sprites:
// 16. ElementSprites     → Sprite[6] theo thứ tự ElementHelper.EnglishKeys
public class SecondaryGeneSelectPanel : MonoBehaviour
{
    public static SecondaryGeneSelectPanel Instance { get; private set; }

    [Header("Header")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text warningText;

    [Header("Pair Display")]
    [SerializeField] private Image    primaryIcon;
    [SerializeField] private TMP_Text primaryNameText;
    [SerializeField] private Image    secondaryIcon;
    [SerializeField] private TMP_Text secondaryNameText;

    [Header("Preview Panel")]
    [SerializeField] private GameObject previewPanel;
    [SerializeField] private TMP_Text   hybridNameText;
    [SerializeField] private TMP_Text   statBonusText;
    [SerializeField] private TMP_Text   bonusTargetsText;
    [SerializeField] private TMP_Text   immuneText;

    [Header("Controls")]
    [SerializeField] private TMP_Text   statusText;
    [SerializeField] private GameObject loadingOverlay;
    [SerializeField] private Button     confirmButton;
    [SerializeField] private Button     closeButton;

    [Header("Shared Element Visuals")]
    [SerializeField] private ElementIconConfig elementIconConfig;

    // Runtime
    private string _fixedSecondary;
    private PlayerDataResponse _playerData;

    // Hàm vòng đời của Unity hoặc ASP.NET được gọi tự động.
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        gameObject.SetActive(false);
    }

    private void Start()
    {
        confirmButton.onClick.AddListener(OnConfirmClicked);
        closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        if (previewPanel != null) previewPanel.SetActive(false);
        confirmButton.interactable = false;
    }

    // Hàm public để script hoặc hệ thống khác gọi vào.

    public void Open()
    {
        // Bật cả canvas cha nếu đang bị tắt (HideOtherBlacksmithFlows dùng root.SetActive(false))
        var root = transform.root.gameObject;
        if (!root.activeSelf) root.SetActive(true);
        gameObject.SetActive(true);
        // Awake() may have fired on first activation and called SetActive(false); re-ensure visible.
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        confirmButton.interactable = false;
        SetStatus("", Color.white);
        if (previewPanel != null) previewPanel.SetActive(false);

        _playerData = GameManager.Instance?.GetPlayerData();
        if (_playerData == null)
        {
            SetStatus("Không tải được dữ liệu nhân vật.", Color.red);
            return;
        }

        // Xác định hệ phụ CỐ ĐỊNH từ hệ chính theo cặp hybrid của game
        _fixedSecondary = ElementHelper.GetFixedSecondary(_playerData.element_type);
        if (string.IsNullOrEmpty(_fixedSecondary))
        {
            SetStatus("Hệ chính không hợp lệ.", Color.red);
            return;
        }

        // Cập nhật icons & tên
        int primaryIdx   = ElementHelper.ToId(_playerData.element_type);
        int secondaryIdx = ElementHelper.ToId(_fixedSecondary);

        ApplyElementIcon(primaryIcon, primaryIdx, "primary element");
        ApplyElementIcon(secondaryIcon, secondaryIdx, "secondary element");

        if (primaryNameText   != null)
            primaryNameText.text   = $"Hệ chính\n{ElementHelper.ToVietnamese(_playerData.element_type)} Tier {_playerData.gene_tier}";
        if (secondaryNameText != null)
            secondaryNameText.text = $"Hệ phụ\n{ElementHelper.ToVietnamese(_fixedSecondary)}";
        if (warningText != null)
            warningText.text = "[!] Hệ phụ được xác định theo cặp hybrid cố định. Xác nhận KHÔNG THỂ hoàn tác!";

        StartCoroutine(LoadHybridPreview());
    }

    // Load preview

    private IEnumerator LoadHybridPreview()
    {
        SetLoading(true);

        string url = $"{APIClient.BASE_URL}/api/gene/hybrid/all-configs";
        using var req = UnityEngine.Networking.UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        SetLoading(false);

        if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            try
            {
                var list = JsonUtility.FromJson<HybridConfigListWrapper>(req.downloadHandler.text);
                if (list?.configs != null)
                {
                    foreach (var cfg in list.configs)
                    {
                        if (string.Equals(cfg.secondaryElement, _fixedSecondary,
                                System.StringComparison.OrdinalIgnoreCase))
                        {
                            ShowPreview(cfg);
                            break;
                        }
                    }
                }
            }
            catch { /* Preview trống — vẫn cho phép confirm */ }
        }

        // Luôn enable confirm kể cả khi preview API lỗi
        confirmButton.interactable = true;
    }

    private void ShowPreview(HybridPreviewData cfg)
    {
        if (previewPanel     != null) previewPanel.SetActive(true);
        if (hybridNameText   != null) hybridNameText.text   = cfg.hybridName;
        if (statBonusText    != null) statBonusText.text    = $"+{cfg.statBonusHp} HP  +{cfg.statBonusMp} MP  +{cfg.statBonusAtk} ATK  +{cfg.statBonusDef} DEF";
        if (bonusTargetsText != null) bonusTargetsText.text = $"Sát thương +50% lên: {FriendlyElements(cfg.bonusTargets)}";
        if (immuneText       != null) immuneText.text       = $"Miễn nhiễm: {FriendlyElements(cfg.immuneElements)}";
    }

    private void ApplyElementIcon(Image targetImage, int elementId, string logContext)
    {
        if (targetImage == null)
            return;

        var config = ResolveElementIconConfig();
        if (config == null)
            return;

        var sprite = config.GetSpriteOrLog(elementId, ElementIconConfig.SpriteKind.Icon, this, nameof(SecondaryGeneSelectPanel));
        if (sprite == null)
        {
            Debug.LogWarning($"[SecondaryGeneSelectPanel] Không apply được icon cho {logContext}.", this);
            return;
        }

        targetImage.sprite = sprite;
        targetImage.color = Color.white;
    }

    private ElementIconConfig ResolveElementIconConfig()
    {
        if (elementIconConfig == null)
            elementIconConfig = ElementIconConfig.Resolve(elementIconConfig, this, nameof(SecondaryGeneSelectPanel));

        return elementIconConfig;
    }

    // Confirm

    private void OnConfirmClicked()
    {
        if (string.IsNullOrEmpty(_fixedSecondary)) return;
        StartCoroutine(ConfirmSelectCoroutine());
    }

    private IEnumerator ConfirmSelectCoroutine()
    {
        SetLoading(true);
        confirmButton.interactable = false;

        string body = JsonUtility.ToJson(new SecondarySelectRequest
        {
            playerId         = _playerData?.player_id ?? 0,
            secondaryElement = _fixedSecondary,
        });

        using var req = new UnityEngine.Networking.UnityWebRequest(
            $"{APIClient.BASE_URL}/api/gene/secondary/select", "POST");
        req.uploadHandler   = new UnityEngine.Networking.UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
        req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        AuthHelper.AddAuthHeader(req);
        yield return req.SendWebRequest();

        SetLoading(false);

        if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            SetStatus($"✅ Đã chọn hệ phụ: {ElementHelper.ToVietnamese(_fixedSecondary)}!", Color.green);

            // Cập nhật local player data
            if (_playerData != null)
            {
                _playerData.secondary_element       = _fixedSecondary;
                _playerData.secondary_gene_tier     = 1;
                _playerData.secondary_gene_exp      = 0;
                GameManager.Instance?.SetPlayerData(_playerData);
            }

            yield return new WaitForSeconds(1.5f);
            gameObject.SetActive(false);

            // Mở SecondaryGeneUpgradePanel ngay sau đó
            SecondaryGeneUpgradePanel.Instance?.Open();
        }
        else
        {
            SetStatus($"Lỗi: {req.downloadHandler.text}", Color.red);
            confirmButton.interactable = true;
        }
    }

    // Hàm hỗ trợ dùng nội bộ để tách nhỏ xử lý chính.

    private string FriendlyElements(string csv)
    {
        if (string.IsNullOrEmpty(csv)) return "—";
        var parts = csv.Split(',', System.StringSplitOptions.RemoveEmptyEntries);
        var names = new List<string>();
        foreach (var p in parts)
            names.Add(ElementHelper.ToVietnamese(p.Trim()));
        return string.Join(", ", names);
    }

    private void SetStatus(string msg, Color color)
    {
        if (statusText == null) return;
        statusText.text  = msg;
        statusText.color = color;
    }

    private void SetLoading(bool show)
    {
        if (loadingOverlay != null) loadingOverlay.SetActive(show);
    }

    // Helper DTOs (chỉ dùng để parse JSON)

    [System.Serializable]
    private class SecondarySelectRequest
    {
        public int    playerId;
        public string secondaryElement;
    }

    [System.Serializable]
    private class HybridPreviewData
    {
        public string secondaryElement;
        public string hybridName;
        public string bonusTargets;
        public string immuneElements;
        public int    statBonusHp;
        public int    statBonusMp;
        public int    statBonusAtk;
        public int    statBonusDef;
    }

    [System.Serializable]
    private class HybridConfigListWrapper
    {
        public HybridPreviewData[] configs;
    }
}
