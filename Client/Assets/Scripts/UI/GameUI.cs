using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject hudPanel;

    [Header("HUD Elements")]
    [SerializeField] private Text debugText;
    [SerializeField] private Text modStatusText;

    [Header("References")]
    private PlayerController player;
    private GameManager gameManager;

    private void Start()
    {
        player = FindObjectOfType<PlayerController>();
        gameManager = GameManager.Instance;

        // Hide panels initially
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);
    }

    private void Update()
    {
        UpdateDebugInfo();
        UpdateModStatus();
        UpdatePanels();
    }

    private void UpdateDebugInfo()
    {
        if (debugText == null || player == null) return;

        var movement = player.GetMovement();
        if (movement == null) return;

        string info = $"FPS: {(int)(1f / Time.unscaledDeltaTime)}\n";
        info += $"Grounded: {movement.IsGrounded()}\n";
        info += $"Flying: {movement.IsFlying()}\n";
        info += $"Horizontal: {movement.GetHorizontalInput():F2}\n";

        debugText.text = info;
    }

    private void UpdateModStatus()
    {
        if (modStatusText == null || player == null) return;

        string status = "";
        if (player.godMode)
        {
            status += "[GOD MODE] ";
        }
        if (player.unlimitedFlight)
        {
            status += "[UNLIMITED FLIGHT] ";
        }

        modStatusText.text = status;
        modStatusText.gameObject.SetActive(!string.IsNullOrEmpty(status));
    }

    private void UpdatePanels()
    {
        if (gameManager == null) return;

        // Show/hide pause panel
        if (pausePanel != null)
        {
            pausePanel.SetActive(gameManager.isPaused);
        }

        // Show/hide game over panel
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(gameManager.isGameOver);
        }
    }

    // UI Button callbacks
    public void OnResumeButton()
    {
        if (gameManager != null)
        {
            gameManager.TogglePause();
        }
    }

    public void OnRestartButton()
    {
        if (gameManager != null)
        {
            gameManager.RestartGame();
        }
    }

    public void OnQuitButton()
    {
        Debug.Log("Quit game");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}

