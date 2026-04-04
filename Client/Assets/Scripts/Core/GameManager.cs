using System;
using UnityEngine;

/// <summary>
/// GameManager trung tâm: quản lý state game + giữ PlayerData (login từ API).
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>
    /// Fired bất cứ khi nào player data được set (login, reconnect, scene load).
    /// ActiveBuffManager subscribe để reload buff sau khi player ID đã sẵn sàng.
    /// </summary>
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
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
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

    /// <summary>
    /// Được gọi sau khi login & load player data từ API.
    /// </summary>
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

    #endregion
}

