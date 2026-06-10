using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel danh sach nhiem vu theo phong cach menu NPC.
///
/// Flow:
///   1. NpcMenuUI.Open(npc) voi npc_type="quest" goi QuestNpcPanel.GetOrCreate().Open(npc).
///   2. Panel hien thi danh sach nhiem vu duoi dang button.
///   3. Click quest mo QuestDialogueUI de nhan, xem tien do hoac nop nhiem vu.
///   4. Sau thao tac nhan/nop, panel refresh hoac dong theo ket qua API.
///
/// Hierarchy de xuat:
///   QuestNpcPanelCanvas [Canvas sortOrder=50]
///   QuestNpcPanelRoot [Image wood background]
///   Header, BtnClose, QuestListScroll/Viewport/Content, BtnCaoTu
/// </summary>
public class QuestNpcPanel : MonoBehaviour
{
    private const string LogPrefix      = "[QuestNpcPanel]";
    private const string ResourcesPath  = "UI/QuestNpcPanel";

    public static QuestNpcPanel Instance { get; private set; }

    // ─── Inspector references ────────────────────────────────────────────────
    [Header("Root")]
    [SerializeField] private GameObject rootPanel;

    [Header("Header")]
    [SerializeField] private TMP_Text   headerText;         // "Xin chào ..."

    [Header("Quest List")]
    [SerializeField] private Transform  questListContent;   // VerticalLayoutGroup parent
    [SerializeField] private GameObject questItemPrefab;    // prefab: Button + TMP_Text

    [Header("Buttons")]
    [SerializeField] private Button     btnClose;           // nút X
    [SerializeField] private Button     btnCaoTu;           // "Cáo từ" ở dưới

    // ─── Runtime ─────────────────────────────────────────────────────────────
    private NpcData _currentNpc;
    private bool    _initialized;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // Tách khỏi parent (nếu user kéo prefab vào bên trong ScreenSpaceCanvas/Panel).
        // QuestNpcPanel là root Canvas độc lập — phải là root GO để:
        //   1. Canvas sortOrder=50 (ScreenSpaceOverlay) hoạt động đúng
        //   2. Không bị các Canvas khác (EnemyInfoPanel, BuffHudPanel…) che khuất
        if (transform.parent != null)
        {
            Debug.Log($"{LogPrefix} Awake: đang ở trong '{transform.parent.name}' → tách ra root để Canvas hoạt động đúng.");
            transform.SetParent(null, false);
        }
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (Instance == null) Instance = this;
        EnsureInit();
    }

    private void EnsureInit()
    {
        if (_initialized) return;
        _initialized = true;
        Debug.Log($"{LogPrefix} EnsureInit() running | childCount={transform.childCount}");

        // AutoWire by name convention
        if (rootPanel        == null) rootPanel        = transform.Find("QuestNpcPanelRoot")?.gameObject
                                                        ?? gameObject;
        if (headerText       == null) headerText       = rootPanel.transform.Find("Header")?.GetComponent<TMP_Text>();
        if (questListContent == null)
        {
            var scroll = rootPanel.transform.Find("QuestListScroll");
            questListContent = scroll?.Find("Viewport/Content") ?? scroll?.Find("Content");
        }
        // Auto-load QuestListItem prefab nếu chưa được assign trong Inspector
        if (questItemPrefab == null)
            questItemPrefab = Resources.Load<GameObject>("UI/Quest/QuestListItem");

        // Runtime fix: Viewport dùng Mask + Color.clear → TMP text invisible.
        // Thay bằng RectMask2D để TMP không cần stencil.
        if (questListContent != null)
        {
            var viewport = questListContent.parent;
            if (viewport != null)
            {
                var oldMask = viewport.GetComponent<Mask>();
                if (oldMask != null)
                {
                    oldMask.enabled = false;   // disable ngay → NotifyStencilStateChanged → TMP revert material
                    var oldImg = viewport.GetComponent<Image>();
                    if (oldImg != null) oldImg.enabled = false;
                    if (viewport.GetComponent<RectMask2D>() == null)
                        viewport.gameObject.AddComponent<RectMask2D>();
                    Debug.Log($"{LogPrefix} Viewport Mask→RectMask2D fixed.");
                }
            }
        }

        Debug.Log($"{LogPrefix} EnsureInit() done | rootPanel={(rootPanel == null ? "null" : rootPanel.name)} headerText={(headerText == null ? "null" : "ok")} questListContent={(questListContent == null ? "null" : "ok")} btnClose={(btnClose == null ? "null" : "ok")} questItemPrefab={(questItemPrefab == null ? "null (fallback)" : questItemPrefab.name)}");
        if (btnClose  == null) btnClose  = rootPanel.transform.Find("BtnClose")?.GetComponent<Button>();
        if (btnCaoTu  == null) btnCaoTu  = rootPanel.transform.Find("BtnCaoTu")?.GetComponent<Button>();

        btnClose?.onClick.AddListener(Close);
        btnCaoTu?.onClick.AddListener(Close);

        if (rootPanel) rootPanel.SetActive(false);
        // Đăng ký rootPanel (panel con hiển thị) vào hệ thống mutual-exclusion
        UIPanelManager.Register(rootPanel != null ? rootPanel : gameObject, Close);
    }

    // ─── Public API ──────────────────────────────────────────────────────────

    public static QuestNpcPanel GetOrCreate()
    {
        if (Instance != null) return Instance;
        Instance = FindObjectOfType<QuestNpcPanel>(true);

        if (Instance == null)
        {
            var prefab = Resources.Load<GameObject>(ResourcesPath);
            if (prefab != null)
                Instance = Instantiate(prefab).GetComponent<QuestNpcPanel>();
        }

        if (Instance == null)
            Debug.LogWarning($"{LogPrefix} Không tìm thấy QuestNpcPanel trong scene hoặc Resources/{ResourcesPath}.");

        return Instance;
    }

    public void Open(NpcData npc)
    {
        _currentNpc = npc;
        Debug.Log($"{LogPrefix} Open() called | npc={(npc == null ? "null" : $"id={npc.npc_id} name={npc.npc_name} type={npc.npc_type}")} | GO.active={gameObject.activeSelf} parent={(transform.parent == null ? "none" : $"{transform.parent.name}.active={transform.parent.gameObject.activeSelf}")}");
        EnsureInit();
        // Kích hoạt root GO trước — nếu prefab bắt đầu với SetActive(false),
        // rootPanel.SetActive(true) trên child sẽ không có tác dụng khi parent vẫn inactive.
        var _registeredGo = rootPanel != null ? rootPanel : gameObject;
        UIPanelManager.CloseOthers(_registeredGo);
        gameObject.SetActive(true);
        Debug.Log($"{LogPrefix} After gameObject.SetActive(true) | GO.active={gameObject.activeSelf} rootPanel={(rootPanel == null ? "null" : $"{rootPanel.name}.active={rootPanel.activeSelf}")}");
        if (rootPanel) rootPanel.SetActive(true);
        Debug.Log($"{LogPrefix} After rootPanel.SetActive(true) | rootPanel.active={rootPanel?.activeSelf} rootPanel.activeInHierarchy={rootPanel?.activeInHierarchy}");

        // --- DIAGNOSTIC + AUTO-FIX: Canvas info ---
        var _c = GetComponent<Canvas>();
        if (_c != null)
        {
            Debug.Log($"{LogPrefix} Canvas | renderMode={_c.renderMode} sortOrder={_c.sortingOrder} enabled={_c.enabled} isActiveEnabled={_c.isActiveAndEnabled}");
            // Force đúng mode nếu bị đặt sai trong Inspector
            if (_c.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                Debug.LogWarning($"{LogPrefix} Canvas renderMode SAI ({_c.renderMode})! Đang sửa → ScreenSpaceOverlay.");
                _c.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            if (!_c.enabled)
            {
                Debug.LogWarning($"{LogPrefix} Canvas bị disabled! Đang bật lại.");
                _c.enabled = true;
            }
        }
        else
            Debug.LogError($"{LogPrefix} KHÔNG CÓ Canvas component! Prefab bị sai — cần chạy lại DoAn > Quest > Create Quest NPC Panel.");
        if (rootPanel != null)
        {
            var _rt = rootPanel.GetComponent<RectTransform>();
            if (_rt != null) Debug.Log($"{LogPrefix} rootPanel RectTransform | worldPos={_rt.position} anchoredPos={_rt.anchoredPosition} sizeDelta={_rt.sizeDelta}");
        }
        UIPanelManager.NotifyOpened(rootPanel != null ? rootPanel : gameObject);

        // Header
        string playerName = PlayerPrefs.GetString("PLAYER_NAME", "Dũng sĩ");
        if (headerText) headerText.text = $"Xin chào {playerName}";

        LoadAndBuildList();
    }

    public void Close()
    {
        _currentNpc = null;
        if (rootPanel) rootPanel.SetActive(false);
        UIPanelManager.NotifyClosed(rootPanel != null ? rootPanel : gameObject);
    }

    // ─── Internal ────────────────────────────────────────────────────────────

    private void LoadAndBuildList()
    {
        Debug.Log($"{LogPrefix} LoadAndBuildList() | QuestManager.Instance={(QuestManager.Instance == null ? "null" : "ok")} npcId={_currentNpc?.npc_id ?? 0}");
        if (QuestManager.Instance == null)
        {
            Debug.LogWarning($"{LogPrefix} QuestManager.Instance is null! Đảm bảo QuestManager có trong scene.");
            return;
        }
        QuestManager.Instance.RefreshFromServer(_currentNpc?.npc_id ?? 0, BuildQuestList);
    }

    private void BuildQuestList(List<QuestManager.QuestStatusDto> quests)
    {
        Debug.Log($"{LogPrefix} BuildQuestList() | quests={(quests == null ? "null" : quests.Count.ToString())} questListContent={(questListContent == null ? "null" : "ok")} rootPanel.activeInHierarchy={rootPanel?.activeInHierarchy}");
        if (questListContent == null)
        {
            Debug.LogWarning($"{LogPrefix} questListContent is null — không thể build danh sách. Kiểm tra hierarchy: QuestNpcPanelRoot > QuestListScroll > Viewport > Content");
            return;
        }

        // Clear old items — dùng SetParent(null) trước để xóa khỏi hierarchy ngay lập tức
        // (Destroy() chỉ xóa cuối frame → childCount vẫn còn khi layout rebuild)
        for (int i = questListContent.childCount - 1; i >= 0; i--)
        {
            var child = questListContent.GetChild(i);
            child.SetParent(null);
            Destroy(child.gameObject);
        }

        if (quests == null || quests.Count == 0)
        {
            Debug.LogWarning($"{LogPrefix} Không có nhiệm vụ nào từ server cho npcId={_currentNpc?.npc_id}. Kiểm tra DB: npc_type='quest' và quest_config có npc_id phù hợp.");
            var empty = CreateListItem("NPC này hiện không có nhiệm vụ.");
            empty.GetComponent<Button>().interactable = false;
            return;
        }

        var visibleQuests = BuildVisibleQuestList(quests);
        if (visibleQuests.Count == 0)
        {
            var empty = CreateListItem("NPC nay hien khong co nhiem vu phu hop.");
            empty.GetComponent<Button>().interactable = false;
            return;
        }

        foreach (var q in visibleQuests)
        {
            // Chỉ hiện: available, active, completed-but-submittable
            // Locked: hiện mờ, không bấm được
            bool submittable = q.status == "active" && IsAllDone(q);

            string icon = q.status switch
            {
                "available"  => "?",
                "active"     => submittable ? "(*)" : "(>)",
                "completed"  => "[v]",
                _            => "?"
            };
            string label = $"  {icon}  {q.name}";

            var item = CreateListItem(label);
            Debug.Log($"{LogPrefix} item '{q.name}' created | active={item.activeSelf} parent={item.transform.parent?.name}");
            var btn  = item.GetComponent<Button>();

            var captured = q;
            btn.onClick.AddListener(() => OnQuestItemClicked(captured));
        }

        // Force layout rebuild để ContentSizeFitter cập nhật ngay lập tức
        Debug.Log($"{LogPrefix} BuildQuestList done | Content.childCount={questListContent.childCount}");
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(
            questListContent.GetComponent<RectTransform>());

        // Reset scroll về đầu danh sách — Content.localPos có thể bị lệch rất lớn
        // (ví dụ y=84522) do ScrollRect giữ vị trí scroll từ lần trước → items bị clip hoàn toàn.
        var scrollRect = questListContent.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;  // 1 = top
            Debug.Log($"{LogPrefix} ScrollRect reset to top | Content.localPos={questListContent.localPosition}");
        }
        else
            Debug.LogWarning($"{LogPrefix} Không tìm thấy ScrollRect cha của Content!");
    }

    private void OnQuestItemClicked(QuestManager.QuestStatusDto quest)
    {
        // Tắt panel quest list trước khi mở dialogue
        if (rootPanel) rootPanel.SetActive(false);

        var dialogueUI = QuestDialogueUI.GetOrCreate();
        if (dialogueUI == null)
        {
            Debug.LogWarning($"{LogPrefix} QuestDialogueUI không tồn tại!");
            if (rootPanel) rootPanel.SetActive(true);
            return;
        }

        string npcName = _currentNpc?.npc_name ?? "NPC";
        bool canSubmit = quest.status == "active" && IsAllDone(quest);

        if (canSubmit)
        {
            // Hiện hội thoại nộp quest
            dialogueUI.ShowComplete(quest, npcName, accepted =>
            {
                if (accepted)
                {
                    QuestManager.Instance?.CompleteQuest(quest.quest_id, (ok, msg) =>
                    {
                        if (!ok)
                        {
                            Debug.LogWarning($"{LogPrefix} Nộp quest thất bại: {msg}");
                            GlobalNotificationUI.Show(msg, "Nộp nhiệm vụ thất bại", 3f, "Đóng");
                            Open(_currentNpc);
                        }
                    });
                }
                else
                {
                    // Ngưá»i chơi đóng → mở lại danh sách
                    Open(_currentNpc);
                }
            });
        }
        else if (quest.status == "available")
        {
            // Hiện hội thoại nhận quest
            dialogueUI.ShowAccept(quest, npcName, accepted =>
            {
                if (accepted)
                {
                    QuestManager.Instance?.AcceptQuest(quest.quest_id, (ok, msg) =>
                    {
                        if (ok)
                            Debug.Log($"{LogPrefix} Đã nhận nhiệm vụ '{quest.name}'.");
                        else
                        {
                            Debug.LogWarning($"{LogPrefix} Nhận quest thất bại: {msg}");
                            GlobalNotificationUI.Show(msg, "Nhận nhiệm vụ thất bại", 3f, "Đóng");
                            Open(_currentNpc);
                        }
                    });
                }
                else
                {
                    // Từ chối → mở lại danh sách
                    Open(_currentNpc);
                }
            });
        }
        else
        {
            // Quest đang làm dở — hiện str3 (gợi ý) dưới dạng hội thoại giản lược
            string hint = !string.IsNullOrEmpty(quest.str3) ? quest.str3 : BuildProgressHint(quest);
            var tempDto = new QuestManager.QuestStatusDto
            {
                str1 = hint, str2 = "", name = quest.name,
                npc_id = quest.npc_id, npc_name = npcName
            };
            dialogueUI.ShowAccept(tempDto, npcName, _ => Open(_currentNpc));
        }
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private GameObject CreateListItem(string label)
    {
        GameObject item;
        if (questItemPrefab != null)
        {
            item = Instantiate(questItemPrefab, questListContent);
            item.SetActive(true);
            // Đảm bảo VLG có đủ info để tính height (mặc định Image.preferredHeight=0 → item bị height=0)
            var le = item.GetComponent<UnityEngine.UI.LayoutElement>()
                  ?? item.AddComponent<UnityEngine.UI.LayoutElement>();
            le.minHeight       = 46;
            le.preferredHeight = 46;
        }
        else
        {
            // Fallback: tạo nút đơn giản
            item = new GameObject("QuestItem", typeof(RectTransform), typeof(Button), typeof(Image));
            item.transform.SetParent(questListContent, false);
            var rt = item.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 48);

            var img = item.GetComponent<Image>();
            img.color = new Color(1, 1, 1, 0.05f);

            var textGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(item.transform, false);
            var txtRect = textGO.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = new Vector2(16, 4);
            txtRect.offsetMax = new Vector2(-8, -4);
        }

        var txt = item.GetComponentInChildren<TMP_Text>();
        Debug.Log($"[QuestNpcPanel] CreateListItem | txt={(txt == null ? "NULL" : txt.name)} " +
                  $"enabled={txt?.enabled} activeInH={txt?.gameObject.activeInHierarchy} " +
                  $"text_before='{txt?.text}' color={txt?.color} fontSize={txt?.fontSize}");
        if (txt)
        {
            txt.text      = label;
            txt.fontSize  = 18;
            txt.color     = Color.white;
            txt.alignment = TextAlignmentOptions.MidlineLeft;
            Debug.Log($"[QuestNpcPanel] CreateListItem SET | text='{txt.text}' h={item.GetComponent<RectTransform>()?.sizeDelta.y}");
        }
        else
        {
            Debug.LogWarning($"[QuestNpcPanel] CreateListItem: KHONG TIM THAY TMP_Text trong '{item.name}' (childCount={item.transform.childCount})");
        }
        return item;
    }

    private static List<QuestManager.QuestStatusDto> BuildVisibleQuestList(List<QuestManager.QuestStatusDto> quests)
    {
        var active = new List<QuestManager.QuestStatusDto>();
        QuestManager.QuestStatusDto nextAvailable = null;

        foreach (var q in quests)
        {
            if (q == null) continue;

            switch (q.status)
            {
                case "active":
                    active.Add(q);
                    break;
                case "available":
                    if (nextAvailable == null)
                        nextAvailable = q;
                    break;
            }
        }

        if (active.Count > 0)
            return active;

        return nextAvailable != null
            ? new List<QuestManager.QuestStatusDto> { nextAvailable }
            : new List<QuestManager.QuestStatusDto>();
    }

    private static string BuildProgressHint(QuestManager.QuestStatusDto q)
    {
        if (string.IsNullOrEmpty(q.steps_json)) return "Đang thực hiện...";
        try
        {
            var wrapped = $"{{\"items\":{q.steps_json}}}";
            var root    = JsonUtility.FromJson<StepArrayWrapper>(wrapped);
            if (root?.items == null || root.items.Count == 0) return "Đang thực hiện...";

            var sb = new System.Text.StringBuilder();
            var prog = ParseProgress(q.quest_progress_json);
            for (int i = 0; i < root.items.Count; i++)
            {
                var s = root.items[i];
                prog.TryGetValue(i.ToString(), out int done);
                string mark = done >= s.require ? "[v]" : (i == q.current_step_index ? "(>)" : " ");
                sb.AppendLine($"{mark} {s.name}: {done}/{s.require}");
            }
            return sb.ToString().TrimEnd();
        }
        catch { return "Đang thực hiện..."; }
    }

    private static bool IsAllDone(QuestManager.QuestStatusDto q)
    {
        if (string.IsNullOrEmpty(q.steps_json)) return false;
        try
        {
            var wrapped = $"{{\"items\":{q.steps_json}}}";
            var root    = JsonUtility.FromJson<StepArrayWrapper>(wrapped);
            if (root?.items == null) return false;
            var prog = ParseProgress(q.quest_progress_json);
            for (int i = 0; i < root.items.Count; i++)
            {
                prog.TryGetValue(i.ToString(), out int done);
                if (done < root.items[i].require) return false;
            }
            return true;
        }
        catch { return false; }
    }

    private static Dictionary<string, int> ParseProgress(string json)
    {
        var dict = new Dictionary<string, int>();
        if (string.IsNullOrEmpty(json) || json == "{}") return dict;
        try
        {
            foreach (var pair in json.Trim('{', '}').Split(','))
            {
                var kv = pair.Split(':');
                if (kv.Length == 2 && int.TryParse(kv[1].Trim(), out int v))
                    dict[kv[0].Trim('"', ' ')] = v;
            }
        }
        catch { }
        return dict;
    }

    private static string LocalizeStatus(string s) => s switch
    {
        "available" => "Co the nhan", "active" => "Dang lam",
        "completed" => "Da xong",      "locked" => "Khoa",
        _           => s
    };

    [Serializable]
    private class StepDto { public string name; public int require; }
    [Serializable]
    private class StepArrayWrapper { public List<StepDto> items; }
}
