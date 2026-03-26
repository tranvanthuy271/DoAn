using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class PlayerController : MonoBehaviour
{
    [Header("Components")]
    private PlayerMovement movement;
    private PlayerAnimator playerAnimator;
    private Rigidbody2D rb;
    private NetworkObject networkObject;

    [Header("Settings")]
    public PlayerStats stats;
    
    [Header("Mod Mode")]
    public bool godMode = false;
    public bool unlimitedFlight = false;

    // Gene item debug state
    private bool _geneItemsBusy;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        playerAnimator = GetComponent<PlayerAnimator>();
        rb = GetComponent<Rigidbody2D>();
        networkObject = GetComponent<NetworkObject>();
    }

    private void Start()
    {
        if (stats == null)
        {
            Debug.LogError("PlayerStats is not assigned!");
        }

        // Ngăn player và player va chạm vật lý với nhau (damage được xử lý bằng code, không qua physics contact)
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0)
            Physics2D.IgnoreLayerCollision(playerLayer, playerLayer, true);

        // Setup Rigidbody2D cho non-owner (để NetworkTransform hoạt động tốt)
        if (networkObject != null && NetworkManager.Singleton != null && !networkObject.IsOwner)
        {
            // Non-owner: để NetworkTransform điều khiển, không dùng physics local
            if (rb != null)
            {
                rb.interpolation = RigidbodyInterpolation2D.Interpolate; // Mượt hơn khi sync
                rb.simulated = true; // Vẫn cần physics cho collision
            }
        }

        // Nếu KHÔNG có NetworkPlayerController (ví dụ Fusion prefab F_Phong, F_Kim...)
        // thì tự đăng ký camera ở đây (tránh trùng với NetworkPlayerController.OnNetworkSpawn)
        bool hasNetworkController = GetComponent<NetworkPlayerController>() != null;
        if (!hasNetworkController)
        {
            // Chỉ gán nếu là owner hoặc không có network (standalone / single player)
            bool isLocalPlayer = (networkObject == null)
                || (NetworkManager.Singleton == null)
                || networkObject.IsOwner;

            if (isLocalPlayer)
            {
                CameraFollow cam = FindObjectOfType<CameraFollow>();
                if (cam != null)
                {
                    cam.SetTarget(transform);
                    Debug.Log($"[PlayerController] Camera gán target: {gameObject.name}");
                }
            }
        }
    }

    private void Update()
    {
        // HandleInput chạy cho TẤT CẢ players (ground check cần thiết cho animation)
        if (movement != null)
        {
            movement.HandleInput();
        }

        // Chỉ owner mới xử lý input / toggle
        if (networkObject != null && NetworkManager.Singleton != null && !networkObject.IsOwner)
        {
            return;
        }

        // Toggle God Mode with G key
        if (Input.GetKeyDown(KeyCode.G))
        {
            ToggleGodMode();
        }

        // Toggle Unlimited Flight with F key
        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleUnlimitedFlight();
        }

        // Thêm x10 item đột biến mỗi hệ với phím M
        if (Input.GetKeyDown(KeyCode.M) && !_geneItemsBusy)
        {
            Debug.Log("[PlayerController] Phím M được nhấn — bắt đầu thêm item đột biến...");
            StartCoroutine(AddGeneItemsCoroutine());
        }
    }

    private void FixedUpdate()
    {
        // Chỉ owner mới xử lý movement
        // QUAN TRỌNG: Chỉ check IsOwner, không check IsClient (vì có thể gây lỗi timing)
        if (networkObject != null && NetworkManager.Singleton != null && !networkObject.IsOwner)
        {
            return; // Remote player không xử lý movement, để NetworkTransform tự sync
        }

        if (movement != null)
        {
            movement.HandleMovement();
        }
    }

    public void ToggleGodMode()
    {
        godMode = !godMode;
        Debug.Log($"God Mode: {(godMode ? "ON" : "OFF")}");
    }

    public void ToggleUnlimitedFlight()
    {
        unlimitedFlight = !unlimitedFlight;
        Debug.Log($"Unlimited Flight: {(unlimitedFlight ? "ON" : "OFF")}");
    }

    public PlayerMovement GetMovement() => movement;
    public PlayerAnimator GetPlayerAnimator() => playerAnimator;
    public Rigidbody2D GetRigidbody() => rb;

    // ── Debug: Thêm x10 Lõi Đột Biến (fusion cores) mỗi hệ ──────────────
    private IEnumerator AddGeneItemsCoroutine()
    {
        _geneItemsBusy = true;
        Debug.Log("[PlayerController] === Đang thêm x10 Lõi Đột Biến vào túi... ===");

        int playerId = 0;
        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
            playerId = GameManager.Instance.GetPlayerData().player_id;
        if (playerId == 0)
            playerId = PlayerPrefs.GetInt("USER_ID", 0);

        if (playerId <= 0)
        {
            Debug.LogWarning("[PlayerController] playerId = 0, chưa đăng nhập!");
            _geneItemsBusy = false;
            yield break;
        }

        string url = $"{APIClient.BASE_URL}/api/item/debug/add-fusion-cores?playerId={playerId}";
        Debug.Log($"[PlayerController] POST {url}");

        using var req = UnityEngine.Networking.UnityWebRequest.PostWwwForm(url, "");
        yield return req.SendWebRequest();

        if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            Debug.Log($"[PlayerController] ✅ +10 Lõi Đột Biến đã thêm vào túi! Server: {req.downloadHandler.text}");
        }
        else
        {
            Debug.LogError($"[PlayerController] ❌ Thêm Lõi Đột Biến thất bại: {req.downloadHandler?.text ?? req.error}");
        }

        _geneItemsBusy = false;
    }
}

