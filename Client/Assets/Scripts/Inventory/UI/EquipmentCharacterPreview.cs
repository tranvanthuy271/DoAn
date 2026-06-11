using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Hiển thị prefab nhân vật idle ở giữa Equipment Panel.
// Nhân vật chỉ để xem, không điều khiển được.
// Auto-resolve prefab (KHUYẾN NGHỊ)
// - Để trống characterPrefab.
// - Tạo asset PlayerPreviewPrefabConfig tại
// Assets/Resources/ScriptableObjects/PlayerPreviewPrefabConfig
// - Điền đủ entry cho từng hệ/giới tính → script tự lookup từ GameManager.
// Manual prefab (override)
// - Kéo trực tiếp 1 prefab cụ thể vào characterPrefab.
// - Script dùng prefab đó bất kể hệ nhân vật.
// MODE A – RenderTexture (KHUYẾN NGHỊ cho Screen Space Canvas)
// 1. Tạo Layer mới tên "UICharacter" (Edit → Project Settings → Tags and Layers)
// 2. Tạo Camera con tên "PreviewCamera" bên ngoài Canvas:
// - Clear Flags: Solid Color, Background: alpha=0
// - Culling Mask: chỉ tick "UICharacter"
// - Depth > main camera
// 3. Tạo RawImage trong Equipment Panel để hiển thị nhân vật
// 4. Gắn script EquipmentCharacterPreview lên 1 GameObject trống trong Panel
// 5. Kéo PreviewCamera → previewCamera, RawImage → renderTargetImage
// 6. Đặt overrideLayer = index của layer "UICharacter"
// MODE B – World Space Canvas (đơn giản hơn)
// - Không gán previewCamera / renderTargetImage
// - Canvas phải là World Space
// - Nhân vật spawn là con của transform này
public class EquipmentCharacterPreview : MonoBehaviour
{
    [Header("Prefab (để trống = tự động tra theo hệ nhân vật)")]
    [Tooltip("Kéo prefab cụ thể vào đây để override. Để trống = dùng PlayerPreviewPrefabConfig.")]
    [SerializeField] private GameObject characterPrefab;

    [Tooltip("(Tuỳ chọn) Kéo PlayerPreviewPrefabConfig asset vào đây.\n" +
             "Nếu để trống, script tự Resources.Load từ ScriptableObjects/PlayerPreviewPrefabConfig.")]
    [SerializeField] private PlayerPreviewPrefabConfig previewPrefabConfig;

    [Header("Mode A – RenderTexture (Screen Space Canvas)")]
    [Tooltip("Camera riêng chỉ render layer UICharacter → RenderTexture")]
    [SerializeField] private Camera previewCamera;

    [Tooltip("RawImage trong UI nhận output từ previewCamera")]
    [SerializeField] private RawImage renderTargetImage;

    [Tooltip("Vị trí thế giới spawn nhân vật preview (đặt xa scene chính)")]
    [SerializeField] private Vector3 previewWorldPosition = new Vector3(1000f, 0f, 1000f);

    [Tooltip("Kích thước RenderTexture (pixels). 0 = tự lấy từ RawImage size, fallback 256×512.")]
    [SerializeField] private Vector2Int renderTextureSize = new Vector2Int(256, 512);

    [Header("Mode B – World Space Canvas")]
    [Tooltip("Vị trí spawn local so với transform này (chỉ dùng Mode B)")]
    [SerializeField] private Vector3 localOffset = Vector3.zero;

    [Header("Shared Settings")]
    [Tooltip("Scale nhân vật preview")]
    [SerializeField] private Vector3 previewScale = Vector3.one;

    [Tooltip("Rotation Y ban đầu (độ)")]
    [SerializeField] private float initialRotationY = 180f;

    [Tooltip("Layer override cho nhân vật preview (index layer UICharacter). -1 = giữ nguyên.")]
    [SerializeField] private int overrideLayer = -1;

    [Tooltip("Dịch camera theo trục Y để điều chỉnh vị trí nhân vật trong frame. Âm = character lên cao.")]
    [SerializeField] private float cameraVerticalOffset = -3f;

    [Tooltip("Danh sách tên child object sẽ bị ẩn trong preview (SkillEffect, platform, shadow...).")]
    [SerializeField] private string[] hideChildrenNamed = { "SkillEffect" };

    // Tên state Idle phổ biến – thử lần lượt cho đến khi tìm được
    private static readonly string[] IdleStateNames =
    {
        "Idle", "idle", "ide", "Ide",
        "Idle_01", "Idle_Loop", "IdleNormal",
        "Base Layer.Idle", "locomotion"
    };

    private GameObject _previewInstance;
    private RenderTexture _renderTexture;
    private bool _usingRenderTexture;
    private Animator _previewAnimator;
    private Coroutine _retryCoroutine;
    private bool _subscribedToPlayerData;
    private string _lastResolvedKey;

    // Unity lifecycle

    private void Awake()
    {
        { /* Awake trên '{gameObject.name}' | previewCamera={(previewCamera != null ? previewCamera.name */ }
        // Camera phải luôn disabled khi start.
        // Chỉ được enable lại sau khi SpawnPreview() tạo xong RenderTexture.
        if (previewCamera != null)
        {
            previewCamera.enabled = false;
            { /* Awake: previewCamera disabled (chờ spawn) */ }
        }
    }

    private void OnEnable()
    {
        { /* OnEnable | _previewInstance={((_previewInstance != null) ? _previewInstance.name */ }
        SubscribePlayerData();
        SpawnPreview();
        if (_usingRenderTexture && previewCamera != null)
            previewCamera.enabled = true;
    }

    private void OnDisable()
    {
        { /* OnDisable */ }
        UnsubscribePlayerData();
        StopRetry();
        // Luôn tắt camera — không để nó render to screen khi panel ẩn
        if (previewCamera != null)
            previewCamera.enabled = false;
        DestroyPreview();
    }

    // Hàm public để script hoặc hệ thống khác gọi vào.

    // Đổi prefab nhân vật (VD: khi đổi nhân vật / trang phục).
    public void SetCharacterPrefab(GameObject newPrefab)
    {
        characterPrefab = newPrefab;
        DestroyPreview();
        SpawnPreview();
    }

    // Gọi khi player data thay đổi (VD: sau khi login xong lần đầu).
    // Re-spawn preview với đúng prefab theo hệ mới.
    public void RefreshForLocalPlayer()
    {
        StopRetry();
        DestroyPreview();
        SpawnPreview();
    }

    private void OnPlayerDataSet(PlayerDataResponse data)
    {
        if (!isActiveAndEnabled)
            return;

        string newKey = BuildPlayerDataKey(data);
        if (_previewInstance != null && string.Equals(_lastResolvedKey, newKey, System.StringComparison.Ordinal))
            return;

        { /* PlayerDataSet -> refresh preview key '{_lastResolvedKey}' -> '{newKey}' */ }
        RefreshForLocalPlayer();
    }

    private void SubscribePlayerData()
    {
        if (_subscribedToPlayerData)
            return;

        GameManager.OnPlayerDataSet += OnPlayerDataSet;
        _subscribedToPlayerData = true;
    }

    private void UnsubscribePlayerData()
    {
        if (!_subscribedToPlayerData)
            return;

        GameManager.OnPlayerDataSet -= OnPlayerDataSet;
        _subscribedToPlayerData = false;
    }

    // Xử lý nội bộ phục vụ các hàm public.

    // Tra cứu prefab phù hợp:
    // 1. characterPrefab gán tay (override)
    // 2. PlayerPreviewPrefabConfig.Resolve(GameManager.playerData)
    // 3. null → cảnh báo
    private GameObject ResolveCharacterPrefab()
    {
        // 1. Manual override
        if (characterPrefab != null)
        {
            { /* Resolve: dùng characterPrefab (manual) = '{characterPrefab.name}' */ }
            return characterPrefab;
        }

        { /* Resolve: characterPrefab=null, thử PlayerPreviewPrefabConfig */ }

        // 2. Tra config
        var config = previewPrefabConfig;
        if (config == null)
        {
            { /* Resolve: previewPrefabConfig chưa gán, thử Resources.Load */ }
            config = PlayerPreviewPrefabConfig.Load();
        }

        if (config != null)
        {
            { /* Resolve: có config '{config.name}' */ }
            PlayerDataResponse playerData = null;
            if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
            {
                playerData = GameManager.Instance.GetPlayerData();
                { /* Resolve: playerData element_type='{playerData?.element_type}', gender='{playerData?.gender}', is_hybrid={playerData?.is_hybrid} */ }
            }
            else
                { /* Cảnh báo: Resolve: GameManager.Instance=null hoặc HasPlayerData()=false. Prefab sẽ là fallback */ }

            var resolved = config.Resolve(playerData);
            if (resolved != null)
            {
                { /* Resolve: ✓ resolved = '{resolved.name}' */ }
                return resolved;
            }
            { /* Cảnh báo: Resolve: config.Resolve() trả về null (không có entry khớp) */ }
        }
        else
        {
            { /* Cảnh báo: Resolve: Không tìm thấy PlayerPreviewPrefabConfig tại Resources/ScriptableObjects/PlayerPreviewPrefabConfig */ }
        }

        { /* Cảnh báo: Không có characterPrefab và không resolve được từ PlayerPreviewPrefabConfig */ }
        return null;
    }

    private void SpawnPreview()
    {
        if (_previewInstance != null)
        {
            { /* SpawnPreview: đã có _previewInstance, bỏ qua */ }
            return;
        }

        SetRenderTargetVisible(false);

        var prefabToSpawn = ResolveCharacterPrefab();
        if (prefabToSpawn == null)
        {
            { /* Cảnh báo: SpawnPreview: prefabToSpawn = null, dừng lại */ }
            ScheduleRetry();
            return;
        }

        StopRetry();
        _lastResolvedKey = BuildCurrentPlayerDataKey();
        { /* SpawnPreview: dùng prefab '{prefabToSpawn.name}' */ }

        _usingRenderTexture = (previewCamera != null && renderTargetImage != null);
        { /* _usingRenderTexture={_usingRenderTexture} | previewCamera={(previewCamera != null ? previewCamera.name */ }

        if (_usingRenderTexture)
        {
            // MODE A: RenderTexture
            _previewInstance = Instantiate(prefabToSpawn);
            _previewInstance.transform.position  = previewWorldPosition;
            _previewInstance.transform.localScale = previewScale;
            _previewInstance.transform.rotation   = Quaternion.Euler(0f, initialRotationY, 0f);
            { /* MODE A: Spawn tại {previewWorldPosition}, scale={previewScale} */ }

            // Tính kích thước RT
            int rtW = renderTextureSize.x > 0 ? renderTextureSize.x : 256;
            int rtH = renderTextureSize.y > 0 ? renderTextureSize.y : 512;
            var rect = renderTargetImage.rectTransform.rect;
            if (rect.width  > 1) rtW = (int)rect.width;
            if (rect.height > 1) rtH = (int)rect.height;
            { /* RenderTexture size={rtW}x{rtH} */ }

            _renderTexture = new RenderTexture(rtW, rtH, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 2,
                name         = "EquipPreviewRT"
            };
            previewCamera.targetTexture   = _renderTexture;
            renderTargetImage.texture     = _renderTexture;
            // Hiện RawImage (đã transparent by default từ setup)
            SetRenderTargetVisible(true);
            // Đảm bảo RawImage render trên cùng (trên mọi sibling như BeDa)
            int siblingBefore = renderTargetImage.transform.GetSiblingIndex();
            renderTargetImage.transform.SetAsLastSibling();
            int siblingAfter = renderTargetImage.transform.GetSiblingIndex();
            { /* RawImage sibling: {siblingBefore} → {siblingAfter} (parent='{renderTargetImage.transform.parent?.name}', total children={renderTargetImage.transform.parent?.childCount}) */ }
            // Log tên các siblings để xác nhận thứ tự
            if (renderTargetImage.transform.parent != null)
            {
                var sb = new System.Text.StringBuilder("[EquipPreview] Children order: ");
                for (int i = 0; i < renderTargetImage.transform.parent.childCount; i++)
                    sb.Append($"[{i}]{renderTargetImage.transform.parent.GetChild(i).name} ");
                { /* Ghi nhận: sb.ToString() */ }
            }
            previewCamera.enabled = true;
            { /* Camera enabled, RenderTexture gán xong */ }
        }
        else
        {
            // MODE B: World Space Canvas
            _previewInstance = Instantiate(prefabToSpawn, transform);
            _previewInstance.transform.localPosition = localOffset;
            _previewInstance.transform.localScale    = previewScale;
            _previewInstance.transform.localRotation = Quaternion.Euler(0f, initialRotationY, 0f);
            { /* MODE B: Spawn làm con của {transform.name} */ }
        }

        // Tắt mọi script gameplay + physics trước khi Unity simulate frame
        DisableAllControlScripts(_previewInstance);

        // Ẩn các child object không cần thiết (SkillEffect, platform...)
        HidePreviewChildren(_previewInstance);

        // Log toàn bộ children sau khi ẩn
        {
            var sb = new System.Text.StringBuilder("[EquipPreview] Children sau HidePreviewChildren:\n");
            foreach (Transform ch in _previewInstance.transform)
                sb.AppendLine($"  - '{ch.name}' active={ch.gameObject.activeSelf}");
            { /* Ghi nhận: sb.ToString() */ }
        }

        // Ghim lại vị trí (MODE A: fix tại previewWorldPosition)
        if (_usingRenderTexture)
            _previewInstance.transform.position = previewWorldPosition;

        // Bật & play Idle
        ForceIdleAnimation(_previewInstance);
        // Cache Animator của character (chỉ lấy active, bỏ qua SkillEffect đã ẩn)
        _previewAnimator = _previewInstance.GetComponentInChildren<Animator>(false);
        if (_previewAnimator == null)
            _previewAnimator = _previewInstance.GetComponentInChildren<Animator>(true);
        { /* _previewAnimator cached = '{(_previewAnimator != null ? _previewAnimator.gameObject.name */ }

        // Đặt layer
        if (overrideLayer >= 0)
            SetLayerRecursive(_previewInstance, overrideLayer);

        // Auto-set camera cullingMask theo layer thực tế của prefab
        if (_usingRenderTexture && previewCamera != null)
        {
            int targetLayer = (overrideLayer >= 0) ? overrideLayer : _previewInstance.layer;
            previewCamera.cullingMask = 1 << targetLayer;
            { /* Camera cullingMask = layer {targetLayer} ('{LayerMask.LayerToName(targetLayer)}') */ }

            // Auto-center camera dựa trên bounds thực của nhân vật
            AutoCenterCamera();
        }
    }

    private void HidePreviewChildren(GameObject root)
    {
        { /* HidePreviewChildren: hideChildrenNamed={hideChildrenNamed?.Length ?? 0} entries, root='{root.name}' childCount={root.transform.childCount} */ }
        if (hideChildrenNamed == null || hideChildrenNamed.Length == 0) return;
        foreach (var childName in hideChildrenNamed)
        {
            if (string.IsNullOrEmpty(childName)) continue;
            var t = root.transform.Find(childName);
            if (t != null)
            {
                t.gameObject.SetActive(false);
                { /* Ẩn child '{childName}' ✓ */ }
            }
            else
            {
                // Tìm trong toàn bộ hierarchy
                var found = root.transform.GetComponentsInChildren<Transform>(true);
                { /* Cảnh báo: Không tìm thấy direct child '{childName}'. Các children hiện có: {string.Join( */ }
            }
        }
    }

    // Tự động căn chỉnh camera để nhân vật nằm ở giữa frame và đủ kích thước.
    private void AutoCenterCamera()
    {
        if (previewCamera == null || _previewInstance == null) return;

        // Chỉ lấy renderer của các active children (SkillEffect đã bị ẩn)
        var renderers = _previewInstance.GetComponentsInChildren<Renderer>(false);
        { /* AutoCenter: tìm được {renderers.Length} active renderer(s) */ }
        if (renderers.Length == 0)
        {
            renderers = _previewInstance.GetComponentsInChildren<Renderer>(true);
            { /* Cảnh báo: AutoCenter: fallback include-inactive, {renderers.Length} renderer(s) */ }
        }
        if (renderers.Length == 0) { { /* Cảnh báo: AutoCenter: 0 renderers, bỏ qua */ } return; }

        var sbR = new System.Text.StringBuilder("[EquipPreview] AutoCenter renderers:\n");
        foreach (var r in renderers)
            sbR.AppendLine($"  - '{r.gameObject.name}' bounds={r.bounds.center} size={r.bounds.size} active={r.gameObject.activeSelf}");
        { /* Ghi nhận: sbR.ToString() */ }

        // Tính bounding box chỉ của character
        var allBounds = renderers[0].bounds;
        foreach (var r in renderers) allBounds.Encapsulate(r.bounds);

        // Đặt camera Y = center của nhân vật + offset, giữ nguyên X và Z
        var camT = previewCamera.transform;
        float newY = allBounds.center.y + cameraVerticalOffset;
        camT.position = new Vector3(camT.position.x, newY, camT.position.z);

        // OrthoSize: 1.5x lớn hơn (3.6 / 1.5 = 2.4)
        previewCamera.orthographicSize = allBounds.extents.y * 2.4f;

        { /* AutoCenter: center={allBounds.center} extents={allBounds.extents} camY={newY:F3} orthoSize={previewCamera.orthographicSize:F3} (offset={cameraVerticalOffset}) */ }
    }

    private void LateUpdate()
    {
        if (_previewAnimator == null || !_previewAnimator.enabled) return;
        LockIdleParameters(_previewAnimator);
    }

    // Reset Animator parameters mỗi frame để tránh transition sang jump/run/die.
    private static void LockIdleParameters(Animator animator)
    {
        foreach (var param in animator.parameters)
        {
            switch (param.type)
            {
                case AnimatorControllerParameterType.Bool:
                    var pn = param.name.ToLower();
                    // Parameter có "ground"/"land"/"floor" → true; còn lại → false
                    bool bval = pn.Contains("ground") || pn.Contains("land") || pn.Contains("floor");
                    animator.SetBool(param.nameHash, bval);
                    break;
                case AnimatorControllerParameterType.Float:
                    animator.SetFloat(param.nameHash, 0f);
                    break;
                case AnimatorControllerParameterType.Int:
                    animator.SetInteger(param.nameHash, 0);
                    break;
                case AnimatorControllerParameterType.Trigger:
                    animator.ResetTrigger(param.nameHash);
                    break;
            }
        }
    }

    private void DestroyPreview()
    {
        _previewAnimator = null;
        _lastResolvedKey = null;
        if (_previewInstance != null)
        {
            Destroy(_previewInstance);
            _previewInstance = null;
        }

        if (_renderTexture != null)
        {
            if (previewCamera      != null) previewCamera.targetTexture  = null;
            if (renderTargetImage  != null) renderTargetImage.texture    = null;
            _renderTexture.Release();
            Destroy(_renderTexture);
            _renderTexture = null;
        }

        SetRenderTargetVisible(false);
    }

    private void SetRenderTargetVisible(bool visible)
    {
        if (renderTargetImage == null)
            return;

        var c = renderTargetImage.color;
        renderTargetImage.color = new Color(c.r, c.g, c.b, visible ? 1f : 0f);

        if (!visible)
            renderTargetImage.texture = null;
    }

    private void ScheduleRetry()
    {
        if (!isActiveAndEnabled || _retryCoroutine != null)
            return;

        _retryCoroutine = StartCoroutine(RetrySpawnPreview());
    }

    private IEnumerator RetrySpawnPreview()
    {
        for (int attempt = 1; attempt <= 20 && _previewInstance == null; attempt++)
        {
            yield return new WaitForSeconds(0.1f);

            if (!isActiveAndEnabled || _previewInstance != null)
                break;

            if (GameManager.Instance == null || !GameManager.Instance.HasPlayerData())
                continue;

            { /* Retry spawn preview attempt={attempt} */ }
            SpawnPreview();
        }

        _retryCoroutine = null;
    }

    private void StopRetry()
    {
        if (_retryCoroutine == null)
            return;

        StopCoroutine(_retryCoroutine);
        _retryCoroutine = null;
    }

    private static string BuildPlayerDataKey(PlayerDataResponse data)
    {
        if (data == null)
            return string.Empty;

        return $"{data.player_id}|{data.element_type}|{data.gender}|{data.is_hybrid}|{data.hybrid_prefab_path}";
    }

    private static string BuildCurrentPlayerDataKey()
    {
        return GameManager.Instance != null && GameManager.Instance.HasPlayerData()
            ? BuildPlayerDataKey(GameManager.Instance.GetPlayerData())
            : string.Empty;
    }

    private static void ForceIdleAnimation(GameObject root)
    {
        // Ƭu tiên active animator trước (bỏ qua SkillEffect đã bị ẩn)
        var animator = root.GetComponentInChildren<Animator>(false);
        if (animator == null)
        {
            { /* Cảnh báo: ForceIdle: Không tìm được active Animator, thử include-inactive */ }
            animator = root.GetComponentInChildren<Animator>(true);
        }
        if (animator == null)
        {
            { /* Cảnh báo: ForceIdle: Không tìm thấy Animator trên '{root.name}' hoặc children */ }
            return;
        }

        { /* ForceIdle: Animator trên '{animator.gameObject.name}' | controller={(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name */ }

        if (animator.runtimeAnimatorController == null)
        {
            { /* Cảnh báo: ForceIdle: Animator không có Controller, không thể play animation */ }
            return;
        }

        animator.enabled = true;
        animator.speed   = 1f;

        // Reset toàn bộ parameters ngay lập tức để tránh transition sang jump/run
        LockIdleParameters(animator);

        // In ra danh sách tất cả clip names để debug
        if (animator.runtimeAnimatorController != null)
        {
            var clips = animator.runtimeAnimatorController.animationClips;
            var names = new System.Text.StringBuilder("[EquipPreview] ForceIdle: Clips trong controller: ");
            foreach (var clip in clips) names.Append(clip.name).Append(", ");
            { /* Ghi nhận: names.ToString() */ }
        }

        // Thử từng tên state phổ biến
        foreach (var stateName in IdleStateNames)
        {
            int hash = Animator.StringToHash(stateName);
            if (animator.HasState(0, hash))
            {
                animator.Play(hash, 0, 0f);
                { /* ForceIdle: ✓ Play state '{stateName}' */ }
                return;
            }
        }

        // Fallback: thử đúng tên clip (state name thường = clip name)
        var clips2 = animator.runtimeAnimatorController.animationClips;
        foreach (var clip in clips2)
        {
            // Thử chính xác, lowercase, capitalize
            string[] candidates = { clip.name, clip.name.ToLower(),
                char.ToUpper(clip.name[0]) + clip.name.Substring(1).ToLower() };
            foreach (var c in candidates)
            {
                int h = Animator.StringToHash(c);
                if (animator.HasState(0, h))
                {
                    animator.Play(h, 0, 0f);
                    { /* ForceIdle: ✓ Play state '{c}' (từ clip '{clip.name}') */ }
                    return;
                }
            }
        }

        // Fallback cuối: Rebind → entry state mặc định
        { /* ForceIdle: Dùng Rebind() → entry state */ }
        animator.Rebind();
        animator.Update(0f);
    }

    // Tắt mọi MonoBehaviour gameplay để nhân vật chỉ hiển thị,
    // không di chuyển và không nhận input.
    // Animator là Behaviour (không phải MonoBehaviour) nên không bị tắt.
    private void DisableAllControlScripts(GameObject root)
    {
        int disabledCount = 0;
        foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == this) continue;
            if (!mb.enabled) continue;
            { /* Disable script: {mb.GetType().Name} trên '{mb.gameObject.name}' */ }
            mb.enabled = false;
            disabledCount++;
        }
        { /* DisableAllControlScripts: đã disable {disabledCount} MonoBehaviour(s) */ }

        foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic = true;
            rb.useGravity  = false;
            rb.velocity     = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        foreach (var rb2d in root.GetComponentsInChildren<Rigidbody2D>(true))
        {
            rb2d.bodyType = RigidbodyType2D.Kinematic;
            rb2d.gravityScale = 0f;
            rb2d.velocity = Vector2.zero;
        }

        foreach (var col  in root.GetComponentsInChildren<Collider>(true))  col.enabled  = false;
        foreach (var col2 in root.GetComponentsInChildren<Collider2D>(true)) col2.enabled = false;

        // Ghim vị trí sau khi tắt physics để không bị drift
        var rigidbodies = root.GetComponentsInChildren<Rigidbody>(true);
        if (rigidbodies.Length > 0)
            root.transform.position = root.transform.position; // force sync
    }

    private static void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}
