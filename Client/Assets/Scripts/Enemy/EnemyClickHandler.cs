using UnityEngine;
using UnityEngine.EventSystems;

// EnemyClickHandler — Xử lý click chọn enemy trên client.
// Gắn vào root GameObject của enemy prefab.
// Yêu cầu:
// - Collider2D (non-trigger) trên root enemy để OnMouseDown hoạt động.
// - Child GameObject "SelectionIndicator" (sprite mũi tên, mặc định ẩn).
// - EnemyInfoPanel tồn tại trong scene (singleton).
// Khi click:
// 1. Bỏ chọn enemy đang được chọn trước đó.
// 2. Hiển thị SelectionIndicator (mũi tên bên dưới chân quái).
// 3. Mở EnemyInfoPanel với thông số của enemy này.
// Tự động bỏ chọn và đóng panel khi enemy bị destroy.
public class EnemyClickHandler : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Child GameObject mũi tên/indicator, mặc định ẩn trong prefab")]
    public GameObject selectionIndicator;

    [Header("Auto Hide")]
    [SerializeField] private float panelHideDistance = 12f;
    [SerializeField] private float panelIdleHideSeconds = 5f;

    // Static: chỉ một enemy được chọn tại một thời điểm
    private static EnemyClickHandler _currentSelected;

    // Cached components
    private NetworkEnemyHealth _netHealth;
    private EnemySkillSet _skillSet;          // Chỉ có trên server/host
    private EnemyStatOverride _statOverride;  // Chỉ có trên server/host
    private float _lastSelectedSkillTime;

    private void Awake()
    {
        _netHealth    = GetComponent<NetworkEnemyHealth>();
        _skillSet     = GetComponent<EnemySkillSet>();
        // _statOverride được AddComponent tại runtime bởi HostSpawnConfigLoader.
        // KHÔNG cache trong Awake() vì lúc đó chưa tồn tại — tự tìm lại khi cần.

        if (selectionIndicator != null)
            selectionIndicator.SetActive(false);

        // Real-time HP sync: cập nhật panel mỗi khi HP thay đổi
        if (_netHealth != null)
            _netHealth.OnHealthChanged.AddListener(OnHealthChangedRefresh);
    }

    private void OnHealthChangedRefresh(int cur, int max) => RefreshPanelIfSelected();

    private void Update()
    {
        if (_currentSelected != this)
            return;

        if (panelIdleHideSeconds > 0f && Time.time - _lastSelectedSkillTime >= panelIdleHideSeconds)
        {
            DeselectCurrent();
            return;
        }

        Transform localPlayer = FindLocalPlayerTransform();
        if (localPlayer == null || panelHideDistance <= 0f)
            return;

        float sqrDistance = (localPlayer.position - transform.position).sqrMagnitude;
        if (sqrDistance > panelHideDistance * panelHideDistance)
            DeselectCurrent();
    }

    // Unity gọi khi click chuột trên Collider2D (cần Collider2D non-trigger trên root)
    private void OnMouseDown()
    {
        Select();
    }


    // Chọn enemy này, bỏ chọn enemy trước đó.
    public void Select()
    {
        if (InputManager.Instance != null && InputManager.Instance.IsGameplayInputBlocked)
        {
            Debug.Log("[EnemyClickHandler] Select ignored because gameplay input is blocked by UI.");
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log("[EnemyClickHandler] Select ignored because pointer is over UI.");
            return;
        }

        // Bỏ chọn NPC đang được chọn (nếu có)
        NpcInteraction.DeselectCurrent();
        PlayerClickHandler.DeselectCurrent();

        // Bỏ chọn enemy cũ
        if (_currentSelected != null && _currentSelected != this)
            _currentSelected.Deselect();

        _currentSelected = this;
        _lastSelectedSkillTime = Time.time;

        if (selectionIndicator != null)
            selectionIndicator.SetActive(true);

        // Đặt làm mục tiêu cho hệ thống auto-move
        TargetSelector.SetTarget(transform);

        EnemyInfoPanel.Instance?.Show(BuildStats());
    }

    // Bỏ chọn enemy này (ẩn indicator).
    public void Deselect()
    {
        if (selectionIndicator != null)
            selectionIndicator.SetActive(false);

        TargetSelector.ClearTarget(transform);
    }

    // Bỏ chọn enemy đang được chọn (gọi từ NpcInteraction khi NPC được chọn).
    public static void DeselectCurrent()
    {
        if (_currentSelected != null)
        {
            _currentSelected.Deselect();
            EnemyInfoPanel.Instance?.Hide();
            _currentSelected = null;
        }
    }

    public static void NotifySkillUsedOnCurrentTarget()
    {
        if (_currentSelected != null)
            _currentSelected._lastSelectedSkillTime = Time.time;
    }

    // Cập nhật lại HP trên panel nếu enemy này đang được chọn.
    // Gọi từ NetworkEnemyHealth.OnHealthChanged nếu cần real-time update.
    public void RefreshPanelIfSelected()
    {
        if (_currentSelected == this)
            EnemyInfoPanel.Instance?.UpdateHP(
                _netHealth != null ? _netHealth.GetCurrentHealth() : 0,
                _netHealth != null ? _netHealth.GetMaxHealth()     : 0);
    }


        private EnemyStats BuildStats()
    {
        // Lazy-load vì _statOverride được AddComponent runtime sau khi NetworkObject.Spawn()
        if (_statOverride == null)
            _statOverride = GetComponent<EnemyStatOverride>();

        // Tên quái: ưu tiên NetworkEnemyHealth (sync từ server), fallback EnemyStatOverride, rồi tên GameObject
        string name = gameObject.name.Replace("(Clone)", "").Trim();
        if (_netHealth != null && !string.IsNullOrEmpty(_netHealth.EnemyName))
            name = _netHealth.EnemyName;
        else if (_statOverride != null && !string.IsNullOrEmpty(_statOverride.EnemyName))
            name = _statOverride.EnemyName;

        // Hệ: ưu tiên NetworkEnemyHealth (sync từ server), fallback EnemySkillSet
        string element = "None";
        if (_netHealth != null && !string.IsNullOrEmpty(_netHealth.ElementType) && _netHealth.ElementType != "None")
            element = _netHealth.ElementType;
        else if (_skillSet != null && !string.IsNullOrEmpty(_skillSet.ElementType))
            element = _skillSet.ElementType;

        // Level: ưu tiên NetworkEnemyHealth, fallback EnemyStatOverride
        int level = 1;
        if (_netHealth != null && _netHealth.EnemyLevel > 0)
            level = _netHealth.EnemyLevel;
        else if (_statOverride != null && _statOverride.Level > 0)
            level = _statOverride.Level;

        // EXP: chỉ có trên EnemyStatOverride (server)
        int exp = _statOverride != null ? _statOverride.OverrideExp : 0;

        return new EnemyStats
        {
            enemyName   = name,
            currentHp   = _netHealth    != null ? _netHealth.GetCurrentHealth() : 0,
            maxHp       = _netHealth    != null ? _netHealth.GetMaxHealth()     : 0,
            elementType = element,
            level       = level,
            expReward   = exp
        };
    }

    private void OnDestroy()
    {
        if (_netHealth != null)
            _netHealth.OnHealthChanged.RemoveListener(OnHealthChangedRefresh);

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

        return Camera.main != null ? Camera.main.transform : null;
    }
}
