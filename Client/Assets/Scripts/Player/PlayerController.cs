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
    }

    private void Update()
    {
        // Chỉ owner mới xử lý input
        if (networkObject != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && !networkObject.IsOwner)
        {
            return; // Remote player không xử lý input
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

        if (movement != null)
        {
            movement.HandleInput();
        }
    }

    private void FixedUpdate()
    {
        // Chỉ owner mới xử lý movement
        if (networkObject != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && !networkObject.IsOwner)
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
}

