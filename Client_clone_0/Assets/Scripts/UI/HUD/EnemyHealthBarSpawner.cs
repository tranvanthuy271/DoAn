using System.Collections;
using Unity.Netcode;
using UnityEngine;

// EnemyHealthBarSpawner - Tự động bind health bar local sau khi enemy root được spawn.
// Health bar là UI local, không phải NetworkObject.
public class EnemyHealthBarSpawner : MonoBehaviour
{
    [Header("Health Bar Prefab")]
    [Tooltip("Prefab của Enemy Health Bar (Canvas World Space với EnemyHealthBar component)")]
    [SerializeField] private GameObject healthBarPrefab;

    [Header("Components")]
    private NetworkEnemyHealth enemyHealth; // Dùng NetworkEnemyHealth thay vì EnemyHealth
    private GameObject healthBarInstance;
    private bool hasSpawned = false; // Flag để tránh spawn nhiều lần
    private Coroutine spawnRoutine;

    private void Awake()
    {
        ResolveEnemyHealth();
    }

    private void OnEnable()
    {
        BeginSpawnRoutine();
    }

    private void OnDisable()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private void OnDestroy()
    {
        if (healthBarInstance != null)
        {
            Destroy(healthBarInstance);
            healthBarInstance = null;
        }

        hasSpawned = false;
    }

    private void ResolveEnemyHealth()
    {
        enemyHealth = transform.root.GetComponent<NetworkEnemyHealth>();
        if (enemyHealth == null)
            enemyHealth = GetComponentInParent<NetworkEnemyHealth>();
    }

    private NetworkObject GetRootNetworkObject()
    {
        return transform.root.GetComponent<NetworkObject>() ?? GetComponentInParent<NetworkObject>();
    }

    private void BeginSpawnRoutine()
    {
        if (hasSpawned || spawnRoutine != null)
            return;

        spawnRoutine = StartCoroutine(WaitForSpawnReady());
    }

    private IEnumerator WaitForSpawnReady()
    {
        while (isActiveAndEnabled && !hasSpawned)
        {
            ResolveEnemyHealth();

            NetworkObject rootNetworkObject = GetRootNetworkObject();
            bool rootReady = rootNetworkObject == null || rootNetworkObject.IsSpawned;

            if (enemyHealth != null && rootReady)
            {
                SpawnHealthBar();
                hasSpawned = true;
                spawnRoutine = null;
                yield break;
            }

            yield return null;
        }

        spawnRoutine = null;
    }

    // Spawn health bar local trên client này
    private void SpawnHealthBar()
    {
        // Kiểm tra đã spawn chưa
        if (healthBarInstance != null)
        {
            // Debug.LogWarning($"[EnemyHealthBarSpawner] Health bar already exists for {gameObject.name}!");
            return;
        }

        // ⭐ Nếu đã có EnemyHealthBar trên cùng object (HP bar canvas baked-in vào enemy prefab),
        // tái sử dụng thay vì yêu cầu một prefab khác.
        EnemyHealthBar existingBar = GetComponent<EnemyHealthBar>();
        if (existingBar != null)
        {
            existingBar.Setup(enemyHealth, transform.root);
            return;
        }

        if (healthBarPrefab == null)
        {
            // Debug.LogWarning($"[EnemyHealthBarSpawner] Health bar prefab not assigned on {gameObject.name}!");
            return;
        }

        if (enemyHealth == null)
        {
            // Debug.LogWarning($"[EnemyHealthBarSpawner] NetworkEnemyHealth is null on {gameObject.name}!");
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
                // Debug.Log($"[EnemyHealthBarSpawner] Preserved size: {prefabSize} for {gameObject.name}");
            }
        }

        // Setup EnemyHealthBar componentff
        EnemyHealthBar healthBarComponent = healthBarInstance.GetComponent<EnemyHealthBar>();
        if (healthBarComponent != null)
        {
            healthBarComponent.Setup(enemyHealth, transform.root);
            // Debug.Log($"[EnemyHealthBarSpawner] Health bar spawned for {gameObject.name}");
        }
        else
        {
            // Debug.LogWarning($"[EnemyHealthBarSpawner] EnemyHealthBar component not found on health bar prefab!");
        }
    }

    // Public method để spawn health bar từ script khác (nếu cần)
    public void SpawnHealthBarManually()
    {
        if (!hasSpawned && healthBarInstance == null)
        {
            ResolveEnemyHealth();
            SpawnHealthBar();
            hasSpawned = true;
        }
    }
}
