using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// FusionCoreDebugAdder — Nút debug thêm x10 Lõi Đột Biến theo hệ phụ.
// INSPECTOR SETUP:
// 1. addButton  → Button "➕ x10 Lõi" (có thể ẩn trong build thật)
// 2. statusText → TMP_Text hiển thị kết quả (optional)
// PHÍM TẮT: Nhấn Q trong runtime để kích hoạt (chỉ khi panel debug mở).
// Gọi: POST /api/item/debug/add-fusion-cores?playerId=X
public class FusionCoreDebugAdder : MonoBehaviour
{
    [Header("Button & Feedback")]
    [SerializeField] private Button         addButton;
    [SerializeField] private TMP_Text       statusText;

    [Header("Hotkey (phím tắt debug)")]
    [SerializeField] private KeyCode        hotkey = KeyCode.Q;

    private bool _isBusy;

    private void Start()
    {
        if (addButton != null)
            addButton.onClick.AddListener(OnAddClicked);
    }

    private void Update()
    {
        if (InputManager.Instance != null && InputManager.Instance.IsGameplayInputBlocked) return;

        if (Input.GetKeyDown(hotkey) && !_isBusy)
            OnAddClicked();
    }

    private void OnAddClicked()
    {
        if (_isBusy) return;
        StartCoroutine(AddCoresCoroutine());
    }

    private IEnumerator AddCoresCoroutine()
    {
        _isBusy = true;
        if (addButton != null) addButton.interactable = false;
        SetStatus("Đang thêm lõi...", Color.yellow);

        int playerId = GameManager.Instance?.GetPlayerData()?.player_id ?? 0;
        if (playerId <= 0)
        {
            SetStatus("❌ Chưa đăng nhập.", Color.red);
            Done();
            yield break;
        }

        string url = $"{APIClient.BASE_URL}/api/item/debug/add-fusion-cores?playerId={playerId}";
        using var req = UnityEngine.Networking.UnityWebRequest.PostWwwForm(url, "");
        yield return req.SendWebRequest();

        if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            SetStatus("✅ +10 Lõi Đột Biến đã thêm vào túi!", Color.green);
            { /* {req.downloadHandler.text} */ }
        }
        else
        {
            SetStatus($"❌ {req.downloadHandler.text}", Color.red);
        }

        Done();
    }

    private void Done()
    {
        _isBusy = false;
        if (addButton != null) addButton.interactable = true;
    }

    private void SetStatus(string msg, Color color)
    {
        if (statusText == null) return;
        statusText.text  = msg;
        statusText.color = color;
    }
}
