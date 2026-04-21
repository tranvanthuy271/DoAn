using TMPro;
using UnityEngine;

/// <summary>
/// WaveHUD — Hiển thị thông tin vòng hiện tại và thời gian còn lại trên client.
///
/// Cách dùng:
///   1. Tạo một GameObject trong scene UI (ví dụ MainHUD hoặc GameHUD).
///   2. Gắn component WaveHUD vào GameObject đó.
///   3. Kéo 2 TMP_Text vào roundText và timerText trong Inspector.
///   4. Script tự động tìm WaveDungeonRuntime khi dungeon được load.
///
/// Script này đọc giá trị trực tiếp từ NetworkVariable của WaveDungeonRuntime
/// thông qua các public property CurrentRound / RemainingSeconds / MaxRounds.
/// </summary>
public class WaveHUD : MonoBehaviour
{
    [Header("UI Labels (gán trong Inspector hoặc để trống để tự tạo)")]
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text timerText;

    [Header("Hiển thị")]
    [SerializeField] private GameObject hudRoot;     // root panel — ẩn khi không ở dungeon
    [SerializeField] private float pollInterval = 0.5f; // bao lâu tìm lại runtime một lần (giây)

    private DungeonManager _dungeonManager;
    private float _pollTimer;
    private int _lastRound = -1;
    private int _lastRemaining = -1;
    private int _lastMaxRounds = -1;

    // ── Unity lifecycle ──────────────────────────────────────────────────────

    private void Start()
    {
        // CẢNH BÁO: nếu hudRoot trỏ vào chính GameObject này, SetActive(false)
        // sẽ tắt luôn Update() → không bao giờ tìm được runtime.
        // Chúng ta ẩn labels thay vì ẩn toàn bộ root.
        _dungeonManager = DungeonManager.Instance;
        Debug.Log($"[WaveHUD] Start — hudRoot={(hudRoot != null ? hudRoot.name : "null")} " +
                  $"selfName={gameObject.name} " +
                  $"roundText={(roundText != null ? roundText.name : "null")} " +
                  $"timerText={(timerText != null ? timerText.name : "null")}");

        SetHudVisible(false);
    }

    private void Update()
    {
        // Tìm DungeonManager định kỳ (singleton persistent phía client)
        _pollTimer -= Time.deltaTime;
        if (_pollTimer <= 0f)
        {
            _pollTimer = pollInterval;
            if (_dungeonManager == null)
            {
                _dungeonManager = DungeonManager.Instance;
                if (_dungeonManager != null)
                    Debug.Log("[WaveHUD] DungeonManager found.");
                else
                    Debug.Log("[WaveHUD] DungeonManager NOT found.");
            }
        }

        if (_dungeonManager == null)
        {
            SetLabelsVisible(false);
            return;
        }

        int round = _dungeonManager.CurrentWaveRound;
        int maxRounds = _dungeonManager.CurrentWaveMaxRounds;
        int remaining = _dungeonManager.CurrentWaveRemainingSeconds;

        if (!_dungeonManager.IsInDungeon)
        {
            SetHudVisible(false);
            return;
        }

        // Chỉ update text khi giá trị thay đổi
        if (round != _lastRound || remaining != _lastRemaining || maxRounds != _lastMaxRounds)
        {
            _lastRound     = round;
            _lastRemaining = remaining;
            _lastMaxRounds = maxRounds;

            bool show = round > 0;
            SetHudVisible(show);

            if (roundText != null)
                roundText.text = $"Vòng {round} / {maxRounds}";

            if (timerText != null)
            {
                int sec = Mathf.Max(0, remaining);
                timerText.text = $"{sec / 60:00}:{sec % 60:00}";
                timerText.color = sec < 30 ? Color.red : Color.white;
            }
        }
    }

    private void OnDestroy()
    {
        _dungeonManager = null;
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    // BUG FIX: Không gọi SetActive trên hudRoot nếu nó là chính GameObject này,
    // vì sẽ tắt script → Update() không chạy → không bao giờ tìm được runtime.
    private void SetHudVisible(bool visible)
    {
        // Nếu hudRoot hợp lệ VÀ không phải chính mình → an toàn để SetActive
        if (hudRoot != null && hudRoot != gameObject)
        {
            hudRoot.SetActive(visible);
        }
        else
        {
            // Chỉ ẩn/hiện các label con, giữ script GameObject luôn active
            SetLabelsVisible(visible);
        }
    }

    private void SetLabelsVisible(bool visible)
    {
        if (roundText != null)  roundText.gameObject.SetActive(visible);
        if (timerText != null)  timerText.gameObject.SetActive(visible);
    }
}
