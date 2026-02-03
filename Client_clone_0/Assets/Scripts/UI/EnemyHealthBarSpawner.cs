using UnityEngine;
using Unity.Netcode;

/// <summary>
/// EnemyHealthBarSpawner - Tự động spawn health bar khi enemy spawn trên network
/// Health bar được tạo local trên mỗi client (không cần network sync)
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class EnemyHealthBarSpawner : NetworkBehaviour
{
    [Header("Health Bar Prefab")]
    [Tooltip("Prefab của Enemy Health Bar (Canvas World Space với EnemyHealthBar component)")]
    [SerializeField] private GameObject healthBarPrefab;

    [Header("Components")]
    private NetworkEnemyHealth enemyHealth; // Dùng NetworkEnemyHealth thay vì EnemyHealth
    private GameObject healthBarInstance;
    private bool hasSpawned = false; // Flag để tránh spawn nhiều lần

    private void Awake()
    {
        enemyHealth = GetComponent<NetworkEnemyHealth>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Tránh spawn nhiều lần
        if (hasSpawned)
        {
            Debug.LogWarning($"[EnemyHealthBarSpawner] Health bar already spawned for {gameObject.name}!");
            return;
        }
        
        if (enemyHealth == null)
        {
            enemyHealth = GetComponent<NetworkEnemyHealth>();
        }
        
        if (enemyHealth == null)
        {
            Debug.LogWarning($"[EnemyHealthBarSpawner] NetworkEnemyHealth not found on {gameObject.name}!");
            return;
        }

        // Spawn health bar trên TẤT CẢ clients (local, không cần network sync)
        SpawnHealthBar();
        hasSpawned = true;
    }

    public override void OnNetworkDespawn()
    {
        // Cleanup health bar khi enemy despawn
        if (healthBarInstance != null)
        {
            Destroy(healthBarInstance);
            healthBarInstance = null;
        }
        
        hasSpawned = false;
        base.OnNetworkDespawn();
    }

    /// <summary>
    /// Spawn health bar local trên client này
    /// </summary>
    private void SpawnHealthBar()
    {
        // Kiểm tra đã spawn chưa
        if (healthBarInstance != null)
        {
            Debug.LogWarning($"[EnemyHealthBarSpawner] Health bar already exists for {gameObject.name}!");
            return;
        }

        if (healthBarPrefab == null)
        {
            Debug.LogWarning($"[EnemyHealthBarSpawner] Health bar prefab not assigned on {gameObject.name}!");
            return;
        }

        if (enemyHealth == null)
        {
            Debug.LogWarning($"[EnemyHealthBarSpawner] NetworkEnemyHealth is null on {gameObject.name}!");
            return;
        }

        // Spawn health bar như child của enemy
        // Lưu scale và size từ prefab TRƯỚC KHI spawn (để preserve)
        Vector3 prefabScale = healthBarPrefab.transform.localScale;
        
        Canvas prefabCanvas = healthBarPrefab.GetComponent<Canvas>();
        RectTransform prefabRect = prefabCanvas != null ? prefabCanvas.GetComponent<RectTransform>() : healthBarPrefab.GetComponent<RectTransform>();
        Vector2 prefabSize = prefabRect != null ? prefabRect.sizeDelta : Vector2.zero;
        
        healthBarInstance = Instantiate(healthBarPrefab, transform);
        
        // ⭐ QUAN TRỌNG: Preserve local scale và size từ prefab ngay sau khi spawn
        // Điều này đảm bảo scale và size không bị ảnh hưởng bởi parent hoặc Unity tự động thay đổi
        if (healthBarInstance != null)
        {
            // Preserve scale
            healthBarInstance.transform.localScale = prefabScale;
            
            // Preserve size (width/height)
            Canvas canvas = healthBarInstance.GetComponent<Canvas>();
            RectTransform rect = canvas != null ? canvas.GetComponent<RectTransform>() : healthBarInstance.GetComponent<RectTransform>();
            if (rect != null && prefabSize != Vector2.zero)
            {
                rect.sizeDelta = prefabSize;
                Debug.Log($"[EnemyHealthBarSpawner] Preserved size: {prefabSize} for {gameObject.name}");
            }
        }

        // Setup EnemyHealthBar componentff
        EnemyHealthBar healthBarComponent = healthBarInstance.GetComponent<EnemyHealthBar>();
        if (healthBarComponent != null)
        {
            healthBarComponent.Setup(enemyHealth, transform);
            Debug.Log($"[EnemyHealthBarSpawner] Health bar spawned for {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"[EnemyHealthBarSpawner] EnemyHealthBar component not found on health bar prefab!");
        }
    }

    /// <summary>
    /// Public method để spawn health bar từ script khác (nếu cần)
    /// </summary>
    public void SpawnHealthBarManually()
    {
        // Chỉ spawn nếu chưa spawn
        if (!hasSpawned && healthBarInstance == null)
        {
            SpawnHealthBar();
            hasSpawned = true;
        }
    }
}
