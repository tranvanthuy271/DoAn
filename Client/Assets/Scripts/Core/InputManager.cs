using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    private static InputManager _instance;
    public static InputManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Auto-create nếu chưa có trong scene
                var go = new GameObject("InputManager [Auto]");
                _instance = go.AddComponent<InputManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [Header("Input Settings")]
    public bool inputEnabled = true;

    private readonly HashSet<string> _gameplayBlockSources = new HashSet<string>();

    public bool IsGameplayInputBlocked => !inputEnabled || _gameplayBlockSources.Count > 0;
    public bool IsGameplayInputAllowed => !IsGameplayInputBlocked;

    // Mobile virtual input (set by MobileLeftButton / MobileRightButton / MobileJumpButton)
    private float _mobileHorizontal;
    private float _mobileVertical;
    private bool _mobileJumpPressed;  // one-frame flag
    private bool _mobileJumpHeld;
    private bool _mobileAttackPressed; // one-frame flag
    private bool _mobileFallThroughPressed; // one-frame flag (nút rơi xuống platform)

    // Auto-move injection (set bởi PlayerSkillManager khi auto-move đến mục tiêu)
    private float _autoMoveHorizontal;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void LateUpdate()
    {
        // Clear one-frame mobile flags after each frame
        _mobileJumpPressed = false;
        _mobileAttackPressed = false;
        _mobileFallThroughPressed = false;
    }

    // ── Mobile setters (called by UI components) ────────────────────────────

    public void SetMobileAxis(float horizontal, float vertical)
    {
        _mobileHorizontal = horizontal;
        _mobileVertical   = vertical;
    }

    public void SetMobileJump(bool pressed, bool held)
    {
        if (pressed) _mobileJumpPressed = true;
        _mobileJumpHeld = held;
    }

    public void SetMobileAttack()
    {
        _mobileAttackPressed = true;
    }

    /// <summary>Gọi từ MobileFallThroughButton khi người chơi nhấn nút rơi xuống platform.</summary>
    public void SetMobileFallThrough()
    {
        _mobileFallThroughPressed = true;
    }

    /// <summary>Inject hướng di chuyển tự động (PlayerSkillManager gọi khi auto-move đến target).</summary>
    public void SetAutoMoveInput(float horizontal)
    {
        _autoMoveHorizontal = horizontal;
    }

    /// <summary>Hủy auto-move injection.</summary>
    public void CancelAutoMove()
    {
        _autoMoveHorizontal = 0f;
    }

    // ── Input queries (keyboard OR mobile) ──────────────────────────────────

    public float GetHorizontalInput()
    {
        if (IsGameplayInputBlocked) return 0f;
        float keyboard = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(keyboard) > 0.01f)
        {
            _autoMoveHorizontal = 0f; // input thủ công hủy auto-move
            return keyboard;
        }
        if (Mathf.Abs(_mobileHorizontal) > 0.01f)
        {
            _autoMoveHorizontal = 0f;
            return _mobileHorizontal;
        }
        return _autoMoveHorizontal;
    }

    public float GetVerticalInput()
    {
        if (IsGameplayInputBlocked) return 0f;
        float keyboard = Input.GetAxisRaw("Vertical");
        return Mathf.Abs(keyboard) > 0.01f ? keyboard : _mobileVertical;
    }

    public bool GetJumpPressed()
    {
        if (IsGameplayInputBlocked) return false;
        return Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)
            || Input.GetKeyDown(KeyCode.Space) || _mobileJumpPressed;
    }

    public bool GetJumpHeld()
    {
        if (IsGameplayInputBlocked) return false;
        return Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)
            || Input.GetKey(KeyCode.Space) || _mobileJumpHeld;
    }

    public bool GetAttackPressed()
    {
        if (IsGameplayInputBlocked) return false;
        return Input.GetKeyDown(KeyCode.Z) || Input.GetMouseButtonDown(0) || _mobileAttackPressed;
    }

    /// <summary>Trả về true trong ĐÚNG 1 frame khi người chơi nhấn S/DownArrow hoặc nút ↓ mobile.</summary>
    public bool GetFallThroughPressed()
    {
        if (IsGameplayInputBlocked) return false;
        return Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) || _mobileFallThroughPressed;
    }

    public void SetGameplayInputBlocked(string source, bool blocked)
    {
        if (string.IsNullOrWhiteSpace(source))
            source = "Unknown";

        bool changed = blocked
            ? _gameplayBlockSources.Add(source)
            : _gameplayBlockSources.Remove(source);

        if (!changed) return;

        Debug.Log($"[InputManager] Gameplay input {(blocked ? "blocked" : "unblocked")} by '{source}'. activeBlocks={_gameplayBlockSources.Count} inputEnabled={inputEnabled}");
    }

    public void EnableInput(bool enable)
    {
        inputEnabled = enable;
    }

    /// <summary>
    /// Backwards-compatible wrapper for older callers named SetInputEnabled.
    /// </summary>
    public void SetInputEnabled(bool enable)
    {
        EnableInput(enable);
    }
}

