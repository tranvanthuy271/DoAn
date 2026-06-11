using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Mỗi row trong panel danh sách phó bản của NPC (DungeonNpcMenuUI).
// Prefab: DungeonNpcMenuEntryPrefab  →  Assets/Prefabs/UI/DungeonNpcMenuEntryPrefab.prefab
public class DungeonNpcMenuEntryUI : MonoBehaviour
{
    private const string LogPrefix = "[DungeonNpcMenuEntryUI]";

    [SerializeField] private Image        chatBubbleIcon;   // icon chat bubble (sprite)
    [SerializeField] private TMP_Text     dungeonNameText;  // tên phó bản
    [SerializeField] private Button       selectButton;     // toàn bộ row là button

    private DungeonConfigData _config;
    private DungeonNpcMenuUI  _owner;

    private void Awake()
    {
        EnsureReferences();
        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(OnClicked);
            selectButton.onClick.AddListener(OnClicked);
        }
    }

    // Khởi tạo dữ liệu row — gọi bởi DungeonNpcMenuUI.
    public void Setup(DungeonConfigData config, DungeonNpcMenuUI owner)
    {
        EnsureReferences();
        _config = config;
        _owner  = owner;

        if (dungeonNameText != null)
            dungeonNameText.text = config?.dungeon_name ?? "";

        if (selectButton != null)
            selectButton.interactable = config != null;
    }

    private void OnClicked()
    {
        if (_config == null || _owner == null) return;
        Debug.Log($"{LogPrefix} Click | dungeonId={_config.dungeon_id} name='{_config.dungeon_name}' type='{_config.dungeon_type}'", this);
        _owner.ShowConfirm(_config);
    }

    private void EnsureReferences()
    {
        if (chatBubbleIcon == null)
            chatBubbleIcon = FindChildComponent<Image>("ChatBubbleIcon");

        if (dungeonNameText == null)
            dungeonNameText = FindChildComponent<TMP_Text>("DungeonNameText");

        if (selectButton == null)
            selectButton = GetComponent<Button>() ?? GetComponentInChildren<Button>(true);

        EnsureButtonGraphic();

        if (dungeonNameText != null)
            UIRuntimeAssetHelper.ApplyNotoSans(dungeonNameText);
    }

    private void EnsureButtonGraphic()
    {
        if (selectButton == null)
            return;

        Image targetGraphic = selectButton.targetGraphic as Image;
        if (targetGraphic == null)
        {
            targetGraphic = GetComponent<Image>();
            if (targetGraphic == null)
                targetGraphic = gameObject.AddComponent<Image>();

            if (targetGraphic.sprite == null)
                targetGraphic.color = new Color(1f, 1f, 1f, 0f);

            targetGraphic.raycastTarget = true;
            selectButton.targetGraphic = targetGraphic;
        }
    }

    private T FindChildComponent<T>(string childName) where T : Component
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
                return child.GetComponent<T>();
        }

        return null;
    }
}
