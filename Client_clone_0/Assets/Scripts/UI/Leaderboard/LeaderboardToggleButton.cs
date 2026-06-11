using UnityEngine;
using UnityEngine.UI;

// Nút HUD mở / đóng LeaderboardPanel (Bảng Xếp Hạng).
// Gắn script này lên một Button trong Canvas HUD.
// Panel sẽ tự tìm trong scene; nếu không có sẽ load từ Resources.
[RequireComponent(typeof(Button))]
public class LeaderboardToggleButton : MonoBehaviour
{
    [Header("Panel (tự tìm trong scene nếu để trống)")]
    [SerializeField] private LeaderboardPanelUI leaderboardPanel;


    private Button _btn;

    #region Unity lifecycle

    private void Awake()
    {
        _btn = GetComponent<Button>();
        _btn.onClick.AddListener(OnClicked);
    }

    private void Start()
    {
        ResolvePanel();
        if (leaderboardPanel == null)
            { /* Cảnh báo: Chưa tìm thấy LeaderboardPanelUI. Hãy gán thủ công trong Inspector */ }
    }

    private void OnDestroy()
    {
        _btn?.onClick.RemoveListener(OnClicked);
    }

    #endregion

    #region Click

    private void OnClicked()
    {
        ResolvePanel();
        if (leaderboardPanel == null)
        {
            { /* Lỗi: Không tìm thấy LeaderboardPanelUI */ }
            return;
        }

        bool isOpen = leaderboardPanel.gameObject.activeSelf;
        if (isOpen)
            leaderboardPanel.Close();
        else
            leaderboardPanel.Open();
    }

    private void ResolvePanel()
    {
        // Nếu đã gán và còn hợp lệ thì thôi
        if (leaderboardPanel != null
            && leaderboardPanel.gameObject.scene.IsValid()
            && leaderboardPanel.gameObject.scene.isLoaded)
            return;

        // Tìm trong scene
        leaderboardPanel = FindObjectOfType<LeaderboardPanelUI>(includeInactive: true);
        if (leaderboardPanel != null) return;

        // Không tìm thấy trong scene — KHÔNG tự Instantiate để tránh tạo bản sao mới
        // User cần:
        //   1. Kéo LeaderboardPanel vào Canvas trong scene, HOẶC
        //   2. Gán thủ công trường 'leaderboardPanel' trong Inspector của nút này
        { /* Cảnh báo: Không tìm thấy LeaderboardPanelUI trong scene.\n */ }
    }

    #endregion
}
