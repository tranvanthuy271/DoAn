using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerClickHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject selectionIndicator;

    [Header("Auto Hide")]
    [SerializeField] private float panelHideDistance = 4f;

    private static PlayerClickHandler _currentSelected;

    private NetworkPlayerDataSync _dataSync;
    private NetworkPlayerHealth _health;

    private void Awake()
    {
        _dataSync = GetComponent<NetworkPlayerDataSync>() ?? GetComponentInParent<NetworkPlayerDataSync>();
        _health = GetComponent<NetworkPlayerHealth>() ?? GetComponentInParent<NetworkPlayerHealth>();

        if (selectionIndicator == null)
        {
            Transform found = transform.Find("SelectionIndicator");
            if (found != null)
                selectionIndicator = found.gameObject;
        }

        if (selectionIndicator != null)
            selectionIndicator.SetActive(false);
    }

    private void OnEnable()
    {
        if (_dataSync != null)
        {
            _dataSync.networkHp.OnValueChanged += OnHpChanged;
            _dataSync.networkMaxHp.OnValueChanged += OnHpChanged;
        }
    }

    private void OnDisable()
    {
        if (_dataSync != null)
        {
            _dataSync.networkHp.OnValueChanged -= OnHpChanged;
            _dataSync.networkMaxHp.OnValueChanged -= OnHpChanged;
        }
    }

    private void Update()
    {
        if (_currentSelected != this)
            return;

        Transform localPlayer = FindLocalPlayerTransform();
        if (localPlayer == null || panelHideDistance <= 0f)
            return;

        float sqrDistance = (localPlayer.position - transform.position).sqrMagnitude;
        if (sqrDistance > panelHideDistance * panelHideDistance)
            DeselectCurrent();
    }

    private void OnMouseDown()
    {
        Select();
    }

    public void Select()
    {
        if (InputManager.Instance != null && InputManager.Instance.IsGameplayInputBlocked)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (IsLocalPlayer())
        {
            DeselectCurrent();
            return;
        }

        EnemyClickHandler.DeselectCurrent();
        NpcInteraction.DeselectCurrent();

        if (_currentSelected != null && _currentSelected != this)
            _currentSelected.Deselect();

        _currentSelected = this;

        if (selectionIndicator != null)
            selectionIndicator.SetActive(true);

        TargetSelector.SetTarget(transform);
        EnemyInfoPanel.Instance?.Show(BuildStats());
    }

    public void Deselect()
    {
        if (selectionIndicator != null)
            selectionIndicator.SetActive(false);

        TargetSelector.ClearTarget(transform);
    }

    public static void DeselectCurrent()
    {
        if (_currentSelected == null)
            return;

        _currentSelected.Deselect();
        EnemyInfoPanel.Instance?.Hide();
        _currentSelected = null;
    }

    private void OnHpChanged(int oldValue, int newValue)
    {
        if (_currentSelected != this)
            return;

        EnemyInfoPanel.Instance?.UpdateHP(GetCurrentHp(), GetMaxHp());
    }

    private EnemyStats BuildStats()
    {
        string playerName = gameObject.name.Replace("(Clone)", string.Empty).Trim();
        string element = "None";
        int level = 1;

        if (_dataSync != null)
        {
            string syncedName = _dataSync.networkCharacterName.Value.ToString();
            if (!string.IsNullOrWhiteSpace(syncedName))
                playerName = syncedName;

            string syncedElement = _dataSync.networkElementType.Value.ToString();
            if (!string.IsNullOrWhiteSpace(syncedElement))
                element = syncedElement;

            level = Mathf.Max(1, _dataSync.networkLevel.Value);
        }

        return new EnemyStats
        {
            enemyName = playerName,
            currentHp = GetCurrentHp(),
            maxHp = GetMaxHp(),
            elementType = element,
            level = level,
            expReward = 0
        };
    }

    private int GetCurrentHp()
    {
        if (_dataSync != null)
            return _dataSync.networkHp.Value;

        return _health != null ? _health.GetCurrentHealth() : 0;
    }

    private int GetMaxHp()
    {
        if (_dataSync != null)
            return _dataSync.networkMaxHp.Value;

        return _health != null ? _health.GetMaxHealth() : 0;
    }

    private bool IsLocalPlayer()
    {
        NetworkObject networkObject = GetComponent<NetworkObject>() ?? GetComponentInParent<NetworkObject>();
        return networkObject != null && networkObject.IsOwner;
    }

    private void OnDestroy()
    {
        if (_currentSelected == this)
        {
            _currentSelected = null;
            EnemyInfoPanel.Instance?.Hide();
            TargetSelector.ClearTarget(transform);
        }
    }

    private static Transform FindLocalPlayerTransform()
    {
        var players = FindObjectsOfType<NetworkPlayerController>();
        for (int i = 0; i < players.Length; i++)
        {
            var player = players[i];
            if (player != null && player.IsOwner)
                return player.transform;
        }

        return null;
    }
}
