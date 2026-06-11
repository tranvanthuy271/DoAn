using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// QuestDialogueUI — Hộp thoại nhiệm vụ kiểu màn hình tối + hộp chat dưới cùng.
// Flow:
// 1. ShowAccept(quest, npcName, callback)  — hiện str1, cuối có Nhận / Hủy
// 2. ShowComplete(quest, npcName, callback) — hiện str2, cuối có Nhận thưởng / Đóng
// Canvas hierarchy (tạo bằng menu DoAn > Quest > Create Quest Dialogue UI):
// QuestDialogueCanvas [Canvas sortOrder=100, QuestDialogueUI component]
// └── Overlay [Image – full-screen dark]
// └── DialoguePanel [Image – bottom strip]
// ├── NpcPortrait  [Image – circle avatar]
// ├── NpcName      [TMP_Text]
// ├── DialogueText [TMP_Text]
// ├── ContinueHint [TMP_Text – "▼ Nhấn để tiếp"]
// └── ActionButtons [GameObject]
// ├── BtnAccept  [Button + TMP_Text]
// └── BtnDecline [Button + TMP_Text]
public class QuestDialogueUI : MonoBehaviour
{
    public static QuestDialogueUI Instance { get; private set; }

    private const string ResourcesPath = "UI/QuestDialogueUI";

    [Header("Root")]
    [SerializeField] private Canvas    rootCanvas;
    [SerializeField] private Image     overlay;

    [Header("Dialogue Panel")]
    [SerializeField] private Image     npcPortrait;
    [SerializeField] private TMP_Text  npcNameText;
    [SerializeField] private TMP_Text  dialogueText;
    [SerializeField] private TMP_Text  continueHint;   // "▼ Nhấn để tiếp"

    [Header("Action Buttons")]
    [SerializeField] private GameObject actionButtons;
    [SerializeField] private Button     btnAccept;
    [SerializeField] private TMP_Text   btnAcceptLabel;
    [SerializeField] private Button     btnDecline;
    [SerializeField] private TMP_Text   btnDeclineLabel;

    // Runtime
    private readonly List<string> _lines = new();
    private int          _currentLine;
    private Action<bool> _callback;
    private Coroutine    _typewriterCo;
    private bool         _blockNextInput; // block click on the same frame as Show()

    private const float TYPEWRITER_SPEED = 0.025f; // seconds per char

    // Hàm vòng đời của Unity hoặc ASP.NET được gọi tự động.

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // Tách khỏi parent nếu user kéo prefab vào bên trong Canvas khác.
        // QuestDialogueUI là root Canvas sortOrder=100 — phải là root GO.
        if (transform.parent != null)
        {
            { /* Awake: đang nested → tách ra root */ }
            transform.SetParent(null, false);
        }
        DontDestroyOnLoad(gameObject); // hoạt động đúng sau khi đã là root
        // AutoWire phải chạy trong Awake (không phải Start) vì:
        // - Prefab được lưu với root.SetActive(false)
        // - Start() KHÔNG chạy cho inactive GameObjects
        // - Nếu AutoWire chỉ ở Start, rootCanvas = null → SetCanvasActive là no-op → dialogue không bao giờ hiện
        AutoWire();
        btnAccept?.onClick.AddListener(OnAcceptClicked);
        btnDecline?.onClick.AddListener(OnDeclineClicked);
        SetCanvasActive(false);
    }

    private void Start() { /* AutoWire đã chạy trong Awake */ }

    private void Update()
    {
        if (!IsVisible()) return;
        if (_blockNextInput) { _blockNextInput = false; return; }

        bool clicked = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);
        if (!clicked) return;

        // Skip typewriter first
        if (_typewriterCo != null)
        {
            StopCoroutine(_typewriterCo);
            _typewriterCo = null;
            if (dialogueText && _currentLine < _lines.Count)
                dialogueText.text = _lines[_currentLine];
            RefreshButtonsAndHint();
            return;
        }

        // Advance line if not on last
        if (_currentLine < _lines.Count - 1)
        {
            _currentLine++;
            PlayLine();
        }
    }

    // Hàm public để script hoặc hệ thống khác gọi vào.

    public static QuestDialogueUI GetOrCreate()
    {
        if (Instance != null) return Instance;
        Instance = FindObjectOfType<QuestDialogueUI>(true);
        if (Instance == null)
        {
            var prefab = Resources.Load<GameObject>(ResourcesPath);
            if (prefab != null)
                Instance = Instantiate(prefab).GetComponent<QuestDialogueUI>();
        }
        return Instance;
    }

    // Hiện hội thoại nhận nhiệm vụ (str1). callback(true) = đã nhận.
    public void ShowAccept(QuestManager.QuestStatusDto quest, string npcName, Action<bool> callback)
    {
        Show(quest.str1, npcName, "Nhận", "Hủy", callback);
    }

    // Hiện hội thoại hoàn thành nhiệm vụ (str2). callback(true) = xác nhận nộp.
    public void ShowComplete(QuestManager.QuestStatusDto quest, string npcName, Action<bool> callback)
    {
        Show(quest.str2, npcName, "Nhận thưởng", "Đóng", callback);
    }

    public void Hide()
    {
        if (_typewriterCo != null) { StopCoroutine(_typewriterCo); _typewriterCo = null; }
        SetCanvasActive(false);
    }

    // Xử lý nội bộ phục vụ các hàm public.

    private void Show(string text, string npcName,
                      string acceptLabel, string declineLabel, Action<bool> callback)
    {
        { /* Show() | npcName='{npcName}' acceptLabel='{acceptLabel}' textLen={text?.Length ?? 0} rootCanvas={(rootCanvas == null ? */ }
        _callback = callback;
        _lines.Clear();

        if (!string.IsNullOrEmpty(text))
        {
            foreach (var raw in text.Split('\n'))
            {
                var t = raw.Trim();
                if (t.Length > 0) _lines.Add(t);
            }
        }
        if (_lines.Count == 0) _lines.Add("...");

        if (npcNameText)     npcNameText.text     = npcName      ?? "";
        if (btnAcceptLabel)  btnAcceptLabel.text  = acceptLabel  ?? "Nhận";
        if (btnDeclineLabel) btnDeclineLabel.text = declineLabel ?? "Hủy";

        _currentLine    = 0;
        _blockNextInput = true;

        SetCanvasActive(true);
        if (actionButtons) actionButtons.SetActive(false);
        if (continueHint)  continueHint.gameObject.SetActive(false);

        PlayLine();
    }

    private void PlayLine()
    {
        if (_typewriterCo != null) { StopCoroutine(_typewriterCo); _typewriterCo = null; }
        _typewriterCo = StartCoroutine(TypewriterCoroutine(_lines[_currentLine]));
    }

    private IEnumerator TypewriterCoroutine(string text)
    {
        if (actionButtons) actionButtons.SetActive(false);
        if (continueHint)  continueHint.gameObject.SetActive(false);
        if (dialogueText)  dialogueText.text = "";

        foreach (char c in text)
        {
            if (dialogueText) dialogueText.text += c;
            yield return new WaitForSecondsRealtime(TYPEWRITER_SPEED);
        }

        _typewriterCo = null;
        RefreshButtonsAndHint();
    }

    private void RefreshButtonsAndHint()
    {
        bool isLast = _currentLine >= _lines.Count - 1;
        if (continueHint)  continueHint.gameObject.SetActive(!isLast);
        if (actionButtons) actionButtons.SetActive(isLast);
    }

    private void OnAcceptClicked()
    {
        _blockNextInput = true;
        Hide();
        _callback?.Invoke(true);
    }

    private void OnDeclineClicked()
    {
        _blockNextInput = true;
        Hide();
        _callback?.Invoke(false);
    }

    private bool IsVisible() => rootCanvas != null && rootCanvas.gameObject.activeSelf;

    private void SetCanvasActive(bool active)
    {
        // Dùng gameObject trực tiếp để đảm bảo hoạt động ngay cả khi rootCanvas chưa được gán
        if (rootCanvas != null)
        {
            { /* SetCanvasActive({active}) via rootCanvas={rootCanvas.name} */ }
            rootCanvas.gameObject.SetActive(active);
        }
        else
        {
            { /* SetCanvasActive({active}) via gameObject (rootCanvas=null) */ }
            gameObject.SetActive(active);
        }
    }

    // AutoWire

    private void AutoWire()
    {
        if (rootCanvas == null)
            rootCanvas = GetComponent<Canvas>() ?? GetComponentInChildren<Canvas>();

        // Tìm Overlay
        var overlayT = transform.Find("Overlay");
        if (overlay == null && overlayT != null) overlay = overlayT.GetComponent<Image>();

        // Tìm DialoguePanel (con của Overlay hoặc trực tiếp)
        var panelT = (overlayT != null ? overlayT.Find("DialoguePanel") : null)
                  ?? transform.Find("DialoguePanel");

        if (panelT == null)
        {
            { /* Cảnh báo: AutoWire: KHÔNG tìm thấy DialoguePanel! overlayT={(overlayT == null ? */ }
            return;
        }

        if (npcPortrait  == null) npcPortrait  = panelT.Find("NpcPortrait")?.GetComponent<Image>();
        if (npcNameText  == null) npcNameText  = panelT.Find("NpcName")?.GetComponent<TMP_Text>();
        if (dialogueText == null) dialogueText = panelT.Find("DialogueText")?.GetComponent<TMP_Text>();
        if (continueHint == null) continueHint = panelT.Find("ContinueHint")?.GetComponent<TMP_Text>();

        if (actionButtons == null)
            actionButtons = panelT.Find("ActionButtons")?.gameObject;

        if (actionButtons != null)
        {
            var ab = actionButtons.transform;
            if (btnAccept == null)
            {
                var t = ab.Find("BtnAccept");
                if (t != null) { btnAccept = t.GetComponent<Button>(); btnAcceptLabel = t.GetComponentInChildren<TMP_Text>(); }
            }
            if (btnDecline == null)
            {
                var t = ab.Find("BtnDecline");
                if (t != null) { btnDecline = t.GetComponent<Button>(); btnDeclineLabel = t.GetComponentInChildren<TMP_Text>(); }
            }
        }

        { /* AutoWire done | rootCanvas={(rootCanvas == null ? */ }
    }
}
