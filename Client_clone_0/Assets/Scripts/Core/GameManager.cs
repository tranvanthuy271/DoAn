using System;
using UnityEngine;

// GameManager trung tâm: quản lý state game + giữ PlayerData (login từ API).
public class GameManager : MonoBehaviour
{
    // Fired bất cứ khi nào player data được set (login, reconnect, scene load).
    // ActiveBuffManager subscribe để reload buff sau khi player ID đã sẵn sàng.
    public static event Action<PlayerDataResponse> OnPlayerDataSet;

    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public bool isPaused = false;
    public bool isGameOver = false;

    [Header("References")]
    public PlayerController player;

    [Header("Player Data (từ Backend API)")]
    public PlayerDataResponse currentPlayerData; // Được set sau khi login/load data

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent != null)
                transform.SetParent(null, true);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            PromotePersistentChildren();
            Destroy(gameObject);
        }
    }

    private void PromotePersistentChildren()
    {
        for (int index = transform.childCount - 1; index >= 0; index--)
        {
            var child = transform.GetChild(index);
            if (child == null) continue;

            bool shouldPromote = child.GetComponent<ChatManager>() != null
                              || child.GetComponent<FriendManager>() != null
                              || child.GetComponent<PartyManager>() != null;

            if (shouldPromote)
                child.SetParent(null, true);
        }
    }

    private void Start()
    {
        // Find player if not assigned
        if (player == null)
        {
            player = FindObjectOfType<PlayerController>();
        }
    }

    private void Update()
    {
        if (InputManager.Instance != null && InputManager.Instance.IsGameplayInputBlocked) return;
        // Pause game with ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    #region Game State

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        Debug.Log($"Game {(isPaused ? "Paused" : "Resumed")}");
    }

    public void GameOver()
    {
        isGameOver = true;
        Time.timeScale = 0f;
        Debug.Log("Game Over!");
    }

    public void RestartGame()
    {
        isGameOver = false;
        isPaused = false;
        Time.timeScale = 1f;
        // Reload scene or reset game state
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    #endregion

    #region Player Data (API)

    // Được gọi sau khi login & load player data từ API.
    public void SetPlayerData(PlayerDataResponse data)
    {
        currentPlayerData = data;
        Debug.Log($"[GameManager] Player data set: Level {data.level}, Map {data.map_id}");
        OnPlayerDataSet?.Invoke(data);
    }

    public PlayerDataResponse GetPlayerData()
    {
        return currentPlayerData;
    }

    public bool HasPlayerData()
    {
        return currentPlayerData != null;
    }

    public void ClearPlayerData()
    {
        currentPlayerData = null;
        player = null;
    }

    #endregion
}

