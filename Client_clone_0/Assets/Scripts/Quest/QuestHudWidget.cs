using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// QuestHudWidget — Widget HUD nhỏ luôn hiện khi có quest đang active.
/// Hiển thị bước hiện tại và tiến trình.
///
/// Attach vào một GameObject trên HUD Canvas.
/// Tự cập nhật khi QuestManager.OnQuestListChanged phát.
/// </summary>
public class QuestHudWidget : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject rootWidget;
    [SerializeField] private TMP_Text   questNameText;
    [SerializeField] private TMP_Text   questStepText;
    [SerializeField] private Button     btnOpenQuest; // tuỳ chọn: mở lại QuestNpcPanel

    private void Awake()
    {
        AutoWire();
    }

    private void OnEnable()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestListChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestListChanged -= Refresh;
    }

    private void Start()
    {
        // Đảm bảo widget đồng bộ ngay sau khi scene load
        if (QuestManager.Instance != null)
            QuestManager.Instance.RefreshFromServer(0, _ => Refresh());
        else
            Refresh();
    }

    // ─── Refresh ─────────────────────────────────────────────────────────────

    public void Refresh()
    {
        var quest = QuestManager.Instance?.ActiveQuest;

        bool hasActive = quest != null;
        if (rootWidget) rootWidget.SetActive(hasActive);

        if (!hasActive) return;

        if (questNameText) questNameText.text = quest.name ?? "";
        if (questStepText) questStepText.text = BuildStepLine(quest);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static string BuildStepLine(QuestManager.QuestStatusDto q)
    {
        if (string.IsNullOrEmpty(q.steps_json)) return "Đang thực hiện...";

        try
        {
            var wrapped  = $"{{\"items\":{q.steps_json}}}";
            var stepRoot = JsonUtility.FromJson<StepArrayWrapper>(wrapped);
            if (stepRoot?.items == null || q.current_step_index >= stepRoot.items.Count)
                return "Hoàn thành! Quay lại NPC";

            var step = stepRoot.items[q.current_step_index];

            // Parse progress for current step
            int done = 0;
            if (!string.IsNullOrEmpty(q.progress_json))
            {
                try
                {
                    string json = q.progress_json.Trim('{', '}');
                    foreach (var pair in json.Split(','))
                    {
                        var kv = pair.Split(':');
                        if (kv.Length == 2 && kv[0].Trim('"', ' ') == q.current_step_index.ToString())
                            int.TryParse(kv[1].Trim(), out done);
                    }
                }
                catch { }
            }

            bool isLast = q.current_step_index >= stepRoot.items.Count - 1;
            string suffix = (done >= step.required_count && isLast) ? " — Hoàn thành! Quay lại NPC" : "";
            return $"{step.target_name}: {done}/{step.required_count}{suffix}";
        }
        catch
        {
            return "Đang thực hiện...";
        }
    }

    private void AutoWire()
    {
        if (rootWidget   == null) rootWidget   = gameObject;
        if (questNameText == null) questNameText = transform.Find("QuestName")?.GetComponent<TMP_Text>();
        if (questStepText == null) questStepText = transform.Find("QuestStep")?.GetComponent<TMP_Text>();
        if (btnOpenQuest  == null) btnOpenQuest  = transform.Find("BtnOpen")?.GetComponent<Button>();

        if (btnOpenQuest != null)
            btnOpenQuest.onClick.AddListener(OnOpenQuestClicked);
    }

    private void OnOpenQuestClicked()
    {
        // Nếu biết NPC receiver, có thể lưu lại npc_receiver_id từ ActiveQuest
        // Hiện tại chỉ refresh QuestNpcPanel nếu đang open
        var panel = QuestNpcPanel.Instance;
        if (panel != null && QuestManager.Instance?.ActiveQuest != null)
        {
            // Panel cần NpcData — không có sẵn ở đây nên chỉ log
            Debug.Log("[QuestHudWidget] Mở lại QuestNpcPanel không được hỗ trợ trực tiếp từ HUD.");
        }
    }

    // ─── DTO helpers ─────────────────────────────────────────────────────────
    [System.Serializable]
    private class StepDto { public string target_name; public int required_count; }

    [System.Serializable]
    private class StepArrayWrapper { public System.Collections.Generic.List<StepDto> items; }
}
