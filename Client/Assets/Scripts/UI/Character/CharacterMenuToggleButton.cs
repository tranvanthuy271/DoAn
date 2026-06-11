using UnityEngine;
using UnityEngine.UI;

// Nút HUD mở / đóng CharacterMenuPanel.
// Panel sẽ được neo ở cạnh trái màn hình.
// Gắn script này lên một Button trong Canvas HUD.
[RequireComponent(typeof(Button))]
public class CharacterMenuToggleButton : MonoBehaviour
{
    [Header("Panel (tự tìm trong scene nếu để trống)")]
    [SerializeField] private CharacterMenuPanelUI characterMenuPanel;

    private Button _btn;

    #region Unity lifecycle

    private void Awake()
    {
        _btn = GetComponent<Button>();
        _btn.onClick.AddListener(OnClicked);
    }

    private void Start()
    {
        ResolvePanel();

        if (characterMenuPanel == null)
            Debug.LogWarning("[CharacterMenuToggleButton] Chưa tìm thấy CharacterMenuPanelUI. Hãy gán thủ công trong Inspector.");
    }

    private void OnDestroy()
    {
        if (_btn != null)
            _btn.onClick.RemoveListener(OnClicked);
    }

    #endregion

    #region Click

    private void OnClicked()
    {
        ResolvePanel();

        if (characterMenuPanel == null)
        {
            Debug.LogError("[CharacterMenuToggleButton] Không tìm thấy CharacterMenuPanelUI trong scene.");
            return;
        }

        bool isOpen = characterMenuPanel.gameObject.activeSelf;
        if (isOpen)
            characterMenuPanel.Close();
        else
            characterMenuPanel.Open();
    }

    private void ResolvePanel()
    {
        if (characterMenuPanel != null
            && characterMenuPanel.gameObject.scene.IsValid()
            && characterMenuPanel.gameObject.scene.isLoaded)
            return;

        characterMenuPanel = FindObjectOfType<CharacterMenuPanelUI>(includeInactive: true);
    }

    #endregion
}
