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

    // Mobile virtual input (set by MobileLeftButton / MobileRightButton / MobileJumpButton)
    private float _mobileHorizontal;
    private float _mobileVertical;
    private bool _mobileJumpPressed;  // one-frame flag
    private bool _mobileJumpHeld;
    private bool _mobileAttackPressed; // one-frame flag

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

    // ── Input queries (keyboard OR mobile) ──────────────────────────────────

    public float GetHorizontalInput()
    {
        if (!inputEnabled) return 0f;
        float keyboard = Input.GetAxisRaw("Horizontal");
        return Mathf.Abs(keyboard) > 0.01f ? keyboard : _mobileHorizontal;
    }

    public float GetVerticalInput()
    {
        if (!inputEnabled) return 0f;
        float keyboard = Input.GetAxisRaw("Vertical");
        return Mathf.Abs(keyboard) > 0.01f ? keyboard : _mobileVertical;
    }

    public bool GetJumpPressed()
    {
        if (!inputEnabled) return false;
        return Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)
            || Input.GetKeyDown(KeyCode.Space) || _mobileJumpPressed;
    }

    public bool GetJumpHeld()
    {
        if (!inputEnabled) return false;
        return Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)
            || Input.GetKey(KeyCode.Space) || _mobileJumpHeld;
    }

    public bool GetAttackPressed()
    {
        if (!inputEnabled) return false;
        return Input.GetKeyDown(KeyCode.Z) || Input.GetMouseButtonDown(0) || _mobileAttackPressed;
    }

    public void EnableInput(bool enable)
    {
        inputEnabled = enable;
    }
}

