using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// SelectGeneController: Scene controller cho màn chọn hệ gene (SelectGene).
// Xuất hiện sau login khi gene 2 đã được mở khoá.
// Người chơi chọn chơi với nhân vật Hệ Gene 1 hoặc Hệ Gene 2.
public class SelectGeneController : MonoBehaviour
{
    [Header("Slot UI")]
    public GeneSlotUI slot1UI;
    public GeneSlotUI slot2UI;

    [Header("UI References")]
    public TMP_Text  titleText;
    public Button    exitButton;
    public GameObject loadingOverlay;
    public TMP_Text  loadingText;
    public TMP_Text  errorText;

    [Header("Tạo nhân vật Gene 2 — Panel")]
    public GameObject   createGene2Panel;
    public TMP_InputField createNameInput;
    public Button       confirmCreateButton;
    public Button       cancelCreateButton;
    public TMP_Text     createErrorText;

    // Trạng thái runtime được cập nhật khi game đang chạy.
    private int          _userId;
    private APIClient    _api;
    private GeneSlotsResponse _slotsData;
    private string       _pendingCreateElement; // element_type khi tạo gene2 mới

    // PlayerPrefs keys
    private const string KeyActiveGeneSlot      = "ACTIVE_GENE_SLOT";
    private const string KeyPostGeneSelectScene = "POST_GENE_SELECT_SCENE";

    // Unity Lifecycle
    private void Start()
    {
        _userId = PlayerPrefs.GetInt("USER_ID", 0);
        if (_userId == 0)
        {
            SceneManager.LoadScene("Login");
            return;
        }

        _api = APIClient.Instance;
        if (_api == null)
            _api = new GameObject("APIClient").AddComponent<APIClient>();

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitClicked);

        // Ẩn panel tạo nhân vật gene 2
        SetCreatePanelVisible(false);

        // Fallback: tìm buttons trong ButtonRow nếu chưa được gán qua Inspector
        if (createGene2Panel != null)
        {
            if (confirmCreateButton == null)
                confirmCreateButton = createGene2Panel.transform.Find("ButtonRow/ConfirmCreate")?.GetComponent<Button>();
            if (cancelCreateButton == null)
                cancelCreateButton = createGene2Panel.transform.Find("ButtonRow/CancelCreate")?.GetComponent<Button>();
            if (createNameInput == null)
                createNameInput = createGene2Panel.GetComponentInChildren<TMP_InputField>();
            if (createErrorText == null)
                createErrorText = createGene2Panel.transform.Find("CreateError")?.GetComponent<TMP_Text>();
        }

        if (confirmCreateButton != null)
            confirmCreateButton.onClick.AddListener(OnConfirmCreate);
        if (cancelCreateButton != null)
            cancelCreateButton.onClick.AddListener(() => SetCreatePanelVisible(false));

        // Wire slot events
        if (slot1UI != null)
        {
            slot1UI.slotIndex = 1;
            slot1UI.OnPlayClicked   += OnSlotPlayClicked;
        }

        if (slot2UI != null)
        {
            slot2UI.slotIndex = 2;
            slot2UI.OnPlayClicked   += OnSlotPlayClicked;
            slot2UI.OnCreateClicked += OnSlotCreateClicked;
        }

        LoadGeneSlots();
    }

    // Data Loading
    private void LoadGeneSlots()
    {
        SetLoading(true, "Đang tải dữ liệu nhân vật...");
        if (errorText != null) errorText.gameObject.SetActive(false);

        _api.LoadGeneSlots(
            _userId,
            onSuccess: data =>
            {
                _slotsData = data;
                SetLoading(false);

                // Nếu gene2 chưa mở → chỉ có 1 slot → skip màn này và đi thẳng vào game
                if (!data.gene2_unlocked)
                {
                    PlayerPrefs.SetInt(KeyActiveGeneSlot, 1);
                    PlayerPrefs.Save();
                    LoadTargetScene();
                    return;
                }

                RenderSlots(data);
            },
            onError: err =>
            {
                SetLoading(false);
                ShowError($"Không thể tải dữ liệu: {err}");
            });
    }

    private void RenderSlots(GeneSlotsResponse data)
    {
        // Slot 1 — luôn tồn tại
        if (slot1UI != null)
        {
            if (data.slot1 != null && data.slot1.exists)
                slot1UI.SetupExistingCharacter(data.slot1);
            else
                slot1UI.SetupEmpty(isUnlocked: true, slot: 1);
        }

        // Slot 2
        if (slot2UI != null)
        {
            if (data.slot2 != null && data.slot2.exists)
                slot2UI.SetupExistingCharacter(data.slot2);
            else
                slot2UI.SetupEmpty(
                    isUnlocked: data.gene2_unlocked,
                    slot: 2,
                    defaultElement: data.slot2?.element_type);
        }
    }

    // Slot Events
    private void OnSlotPlayClicked(int slot)
    {
        PlayerPrefs.SetInt(KeyActiveGeneSlot, slot);
        PlayerPrefs.Save();
        { /* ==== [GENE2_DEBUG] SelectGene: OnSlotPlayClicked slot= */ }

        if (slot == 1)
        {
            // Dữ liệu gene 1 đã được load bởi LoginLoadingManager — đi thẳng vào game
            LoadTargetScene();
        }
        else
        {
            // Load dữ liệu gene 2 rồi mới vào game
            SetLoading(true, "Đang tải dữ liệu nhân vật hệ gene 2...");
            _api.LoadPlayer2Data(
                _userId,
                onSuccess: data2 =>
                {
                    // Lưu dữ liệu gene 2 vào GameManager (override gene 1)
                    GameManager.Instance?.SetPlayerData(data2);
                    SetLoading(false);
                    LoadTargetScene();
                },
                onError: err =>
                {
                    SetLoading(false);
                    ShowError($"Không thể tải nhân vật gene 2: {err}");
                });
        }
    }

    private void OnSlotCreateClicked(int slot)
    {
        if (slot != 2) return;

        // Lấy element_type mặc định cho gene2 từ secondary_element của gene1
        string defaultElement = _slotsData?.slot2?.element_type ?? "";
        _pendingCreateElement = defaultElement;

        if (createNameInput != null) createNameInput.text = "";
        if (createErrorText != null) createErrorText.gameObject.SetActive(false);
        SetCreatePanelVisible(true);
    }

    // Create Gene 2 Character
    private void OnConfirmCreate()
    {
        string name = createNameInput != null ? createNameInput.text.Trim() : "";
        if (string.IsNullOrEmpty(name) || name.Length < 3 || name.Length > 20)
        {
            if (createErrorText != null)
            {
                createErrorText.text = "Tên nhân vật phải từ 3-20 ký tự.";
                createErrorText.gameObject.SetActive(true);
            }
            return;
        }

        SetCreatePanelVisible(false);
        SetLoading(true, "Đang tạo nhân vật hệ gene 2...");

        _api.CreatePlayer2(
            _pendingCreateElement,
            name,
            onSuccess: data2 =>
            {
                SetLoading(false);
                // Reload slot info để cập nhật UI
                LoadGeneSlots();
            },
            onError: err =>
            {
                SetLoading(false);
                ShowError($"Tạo nhân vật thất bại: {err}");
                SetCreatePanelVisible(true);
            });
    }

    // Navigation
    private void OnExitClicked()
    {
        // Quay lại Login và xoá token
        PlayerPrefs.DeleteKey("JWT_TOKEN");
        PlayerPrefs.DeleteKey("USER_ID");
        PlayerPrefs.Save();
        SceneManager.LoadScene("Login");
    }

    private void LoadTargetScene()
    {
        string targetScene = PlayerPrefs.GetString(KeyPostGeneSelectScene, "GameScene");
        SceneManager.LoadScene(targetScene);
    }

    // UI Helpers
    private void SetLoading(bool show, string message = "")
    {
        if (loadingOverlay != null) loadingOverlay.SetActive(show);
        if (loadingText != null && show) loadingText.text = message;
    }

    private void ShowError(string msg)
    {
        if (errorText != null)
        {
            errorText.text = msg;
            errorText.gameObject.SetActive(true);
        }
        else
        {
            { /* Cảnh báo: {msg} */ }
        }
    }

    private void SetCreatePanelVisible(bool show)
    {
        if (createGene2Panel != null) createGene2Panel.SetActive(show);
    }
}
