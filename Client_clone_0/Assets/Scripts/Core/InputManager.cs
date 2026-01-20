using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("Input Settings")]
    public bool inputEnabled = true;

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

    public float GetHorizontalInput()
    {
        if (!inputEnabled) return 0f;
        return Input.GetAxisRaw("Horizontal");
    }

    public float GetVerticalInput()
    {
        if (!inputEnabled) return 0f;
        return Input.GetAxisRaw("Vertical");
    }

    public bool GetJumpPressed()
    {
        if (!inputEnabled) return false;
        return Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space);
    }

    public bool GetJumpHeld()
    {
        if (!inputEnabled) return false;
        return Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Space);
    }

    public bool GetAttackPressed()
    {
        if (!inputEnabled) return false;
        return Input.GetKeyDown(KeyCode.Z) || Input.GetMouseButtonDown(0);
    }

    public void EnableInput(bool enable)
    {
        inputEnabled = enable;
    }
}

