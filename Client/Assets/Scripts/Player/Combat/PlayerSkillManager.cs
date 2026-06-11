using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

// Quản lý tất cả skill projectile của player
// Chỉ cần thêm SkillData vào list và skill sẽ tự động hoạt động
public class PlayerSkillManager : NetworkBehaviour
{
    [Header("Components")]
    private PlayerController controller;
    private NetworkObject networkObject;
    
    [Header("Skills List")]
    [Tooltip("Danh sách các skill projectile. Thêm skill mới vào đây để tự động hoạt động")]
    public List<SkillData> skills = new List<SkillData>();
    
    [Header("Skill Effect (Optional)")]
    [Tooltip("Object SkillEffect chung để tìm nếu skill không có playerSkillEffectObject riêng. Nếu để trống sẽ tự tìm child có tên 'SkillEffect'")]
    [SerializeField] private GameObject defaultSkillEffectObject;
    
    private Dictionary<KeyCode, SkillData> skillByKey = new Dictionary<KeyCode, SkillData>();
    private Dictionary<string, Animator> skillEffectAnimators = new Dictionary<string, Animator>();
    private Dictionary<string, Unity.Netcode.Components.NetworkAnimator> skillEffectNetworkAnimators = new Dictionary<string, Unity.Netcode.Components.NetworkAnimator>();

    // Teleport skill (auto-detected)
    private TeleportSkill teleportSkillComponent;
    private SkillData teleportSkillData;

    // Wind Step skill (auto-detected)
    private WindStepSkill windStepComponent;
    private SkillData windStepSkillData;

    // Metal Shield skill (auto-detected)
    private MetalShieldSkill metalShieldComponent;
    private SkillData metalShieldSkillData;
    // Water Pillar skill (auto-detected)
    private WaterPillarSkill waterPillarComponent;
    private SkillData waterPillarSkillData;

    // Water Armor Buff skill (auto-detected)
    private WaterArmorBuffSkill waterArmorBuffComponent;
    private SkillData waterArmorBuffSkillData;
    // Fire Rain skill (auto-detected)
    private FireRainSkill fireRainComponent;
    private SkillData fireRainSkillData;
    // Earth Attack Buff skill (auto-detected)
    private EarthAttackBuffSkill earthAuraComponent;
    private SkillData earthAuraSkillData;
    // Earth Boomerang skill (auto-detected)
    private EarthBoomerangSkill earthBoomerangComponent;
    private SkillData earthBoomerangSkillData;
    // Earth Blink Strike skill (auto-detected)
    private EarthBlinkStrikeSkill earthBlinkStrikeComponent;
    private SkillData earthBlinkStrikeSkillData;
    private HybridMetalWindBarrageSkill hybridMetalWindBarrageComponent;
    private SkillData hybridMetalWindBarrageSkillData;
    // Hybrid Fire Earth Lava Aura skill (auto-detected)
    private HybridFireEarthLavaAuraSkill hybridLavaAuraComponent;
    private SkillData hybridLavaAuraSkillData;
    // Hybrid Water Wood Venom skill (auto-detected)
    private HybridWaterWoodVenomSkill hybridVenomComponent;
    private SkillData hybridVenomSkillData;
    // Dash skill (auto-detected)
    private PlayerDash playerDashComponent;
    private SkillData dashSkillData;
    // Auto-move toward selected target
    private Transform _autoMoveTarget;
    private bool _autoMoving;
    private const float AUTO_MOVE_ATTACK_RANGE = 1.5f; // dừng và đánh khi cách target ≤ khoảng này

    // Normal Attack skill (auto-detected via PlayerCombat)
    private PlayerCombat playerCombatComponent;
    private SkillData normalAttackSkillData;
    // Player animator (main character sprite)
    private PlayerAnimator playerAnimator;

    // MP System
    private NetworkPlayerDataSync dataSync;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        InitializeSkills();

        // Nếu là owner (local player), thông báo SkillHotbarUI rebind ngay
        if (IsOwner)
        {
            var hotbar = FindObjectOfType<SkillHotbarUI>();
            if (hotbar != null)
                hotbar.ForceRebind();
        }
    }
    
    private void Start()
    {
        if (!IsSpawned)
        {
            InitializeSkills();
        }
    }
    
    private void InitializeSkills()
    {
        // Tìm PlayerController
        controller = GetComponent<PlayerController>();
        if (controller == null)
        {
            controller = GetComponentInParent<PlayerController>();
        }

        // Tìm PlayerAnimator (để trigger attack animation khi dùng skill)
        playerAnimator = GetComponent<PlayerAnimator>() ?? GetComponentInParent<PlayerAnimator>();

        networkObject = GetComponent<NetworkObject>();
        dataSync = GetComponent<NetworkPlayerDataSync>();
        
        // Tìm default SkillEffect nếu chưa gán
        if (defaultSkillEffectObject == null)
        {
            defaultSkillEffectObject = transform.Find("SkillEffect")?.gameObject;
        }

        // Xóa sprite mặc định của SkillEffect để tránh hiện frame đầu animation khi spawn
        if (defaultSkillEffectObject != null)
        {
            SpriteRenderer sr = defaultSkillEffectObject.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = null;
        }

        // Auto-detect TeleportSkill — chỉ dùng skillData đã có trong prefab, KHÔNG tự tạo thêm
        teleportSkillComponent = GetComponent<TeleportSkill>() ?? GetComponentInParent<TeleportSkill>();
        if (teleportSkillComponent != null)
        {
            teleportSkillData = skills.Find(s => s != null && s.skillType == SkillType.Teleport);
            Debug.Log("[PlayerSkillManager] Detected TeleportSkill component.");
        }

        // Auto-detect WindStepSkill và đồng bộ cooldown vào SkillData có type WindStep
        windStepComponent = GetComponent<WindStepSkill>() ?? GetComponentInParent<WindStepSkill>();
        if (windStepComponent != null)
        {
            windStepSkillData = skills.Find(s => s != null && s.skillType == SkillType.WindStep);
            // Đồng bộ cooldown từ component vào SkillData để Hotbar UI hiển thị đúng
            if (windStepSkillData != null)
            {
                windStepSkillData.cooldown = windStepComponent.cooldown;
            }
            Debug.Log("[PlayerSkillManager] Đã phát hiện WindStepSkill component.");
        }

        // Auto-detect MetalShieldSkill và đồng bộ cooldown
        metalShieldComponent = GetComponent<MetalShieldSkill>() ?? GetComponentInParent<MetalShieldSkill>();
        if (metalShieldComponent != null)
        {
            metalShieldSkillData = skills.Find(s => s != null && s.skillType == SkillType.MetalShield);
            if (metalShieldSkillData != null)
                metalShieldSkillData.cooldown = metalShieldComponent.cooldown;
            Debug.Log("[PlayerSkillManager] Đã phát hiện MetalShieldSkill component.");
        }

        // Auto-detect WaterPillarSkill và đồng bộ cooldown
        waterPillarComponent = GetComponent<WaterPillarSkill>() ?? GetComponentInParent<WaterPillarSkill>();
        if (waterPillarComponent != null)
        {
            waterPillarSkillData = skills.Find(s => s != null && s.skillType == SkillType.WaterPillar);
            if (waterPillarSkillData != null)
                waterPillarSkillData.cooldown = waterPillarComponent.cooldown;
            Debug.Log("[PlayerSkillManager] Đã phát hiện WaterPillarSkill component.");
        }

        // Auto-detect WaterArmorBuffSkill và đồng bộ cooldown
        waterArmorBuffComponent = GetComponent<WaterArmorBuffSkill>() ?? GetComponentInParent<WaterArmorBuffSkill>();
        if (waterArmorBuffComponent != null)
        {
            waterArmorBuffSkillData = skills.Find(s => s != null && s.skillType == SkillType.WaterArmorBuff);
            if (waterArmorBuffSkillData != null)
                waterArmorBuffSkillData.cooldown = waterArmorBuffComponent.cooldown;
            Debug.Log("[PlayerSkillManager] Đã phát hiện WaterArmorBuffSkill component.");
        }

        // Auto-detect FireRainSkill
        fireRainComponent = GetComponent<FireRainSkill>() ?? GetComponentInParent<FireRainSkill>();
        if (fireRainComponent != null)
        {
            fireRainSkillData = skills.Find(s => s != null && s.skillType == SkillType.FireRain);
            if (fireRainSkillData != null)
                fireRainSkillData.cooldown = fireRainComponent.cooldown;
            Debug.Log("[PlayerSkillManager] Đã phát hiện FireRainSkill component.");
        }

        // Auto-detect EarthAttackBuffSkill
        earthAuraComponent = GetComponent<EarthAttackBuffSkill>() ?? GetComponentInParent<EarthAttackBuffSkill>();
        if (earthAuraComponent != null)
        {
            earthAuraSkillData = skills.Find(s => s != null && s.skillType == SkillType.EarthAura);
            if (earthAuraSkillData != null)
                earthAuraSkillData.cooldown = earthAuraComponent.cooldown;
            Debug.Log("[PlayerSkillManager] Đã phát hiện EarthAttackBuffSkill component.");
        }

        // Auto-detect EarthBoomerangSkill
        earthBoomerangComponent = GetComponent<EarthBoomerangSkill>() ?? GetComponentInParent<EarthBoomerangSkill>();
        if (earthBoomerangComponent != null)
        {
            earthBoomerangSkillData = skills.Find(s => s != null && s.skillType == SkillType.EarthBoomerang);
            if (earthBoomerangSkillData != null)
                earthBoomerangSkillData.cooldown = earthBoomerangComponent.cooldown;
            Debug.Log("[PlayerSkillManager] Đã phát hiện EarthBoomerangSkill component.");
        }

        // Auto-detect EarthBlinkStrikeSkill
        earthBlinkStrikeComponent = GetComponent<EarthBlinkStrikeSkill>() ?? GetComponentInParent<EarthBlinkStrikeSkill>();
        if (earthBlinkStrikeComponent != null)
        {
            earthBlinkStrikeSkillData = skills.Find(s => s != null && s.skillType == SkillType.EarthBlinkStrike);
            if (earthBlinkStrikeSkillData != null)
                earthBlinkStrikeSkillData.cooldown = earthBlinkStrikeComponent.cooldown;
            Debug.Log("[PlayerSkillManager] Đã phát hiện EarthBlinkStrikeSkill component.");
        }

        // Auto-detect HybridMetalWindBarrageSkill
        hybridMetalWindBarrageComponent = GetComponent<HybridMetalWindBarrageSkill>() ?? GetComponentInParent<HybridMetalWindBarrageSkill>();
        if (hybridMetalWindBarrageComponent != null)
        {
            hybridMetalWindBarrageSkillData = skills.Find(s => s != null && s.skillType == SkillType.HybridBarrage);
            if (hybridMetalWindBarrageSkillData != null)
                hybridMetalWindBarrageSkillData.cooldown = hybridMetalWindBarrageComponent.cooldown;
            Debug.Log("[PlayerSkillManager] Detected HybridMetalWindBarrageSkill component.");
        }

        // Auto-detect HybridFireEarthLavaAuraSkill
        hybridLavaAuraComponent = GetComponent<HybridFireEarthLavaAuraSkill>()
            ?? GetComponentInParent<HybridFireEarthLavaAuraSkill>()
            ?? GetComponentInChildren<HybridFireEarthLavaAuraSkill>();
        if (hybridLavaAuraComponent != null)
        {
            hybridLavaAuraSkillData = skills.Find(s => s != null && s.skillType == SkillType.HybridLavaAura);
            if (hybridLavaAuraSkillData != null)
                hybridLavaAuraSkillData.cooldown = hybridLavaAuraComponent.cooldown;
            Debug.Log("[PlayerSkillManager] Detected HybridFireEarthLavaAuraSkill component.");
        }

        // Auto-detect HybridWaterWoodVenomSkill
        hybridVenomComponent = GetComponent<HybridWaterWoodVenomSkill>() ?? GetComponentInParent<HybridWaterWoodVenomSkill>();
        if (hybridVenomComponent != null)
        {
            hybridVenomSkillData = skills.Find(s => s != null && s.skillType == SkillType.HybridVenom);
            if (hybridVenomSkillData != null)
                hybridVenomSkillData.cooldown = hybridVenomComponent.cooldown;
            Debug.Log("[PlayerSkillManager] Detected HybridWaterWoodVenomSkill component.");
        }

        // Auto-detect PlayerDash và đồng bộ cooldown
        playerDashComponent = GetComponent<PlayerDash>() ?? GetComponentInParent<PlayerDash>();
        if (playerDashComponent != null)
        {
            dashSkillData = skills.Find(s => s != null && s.skillType == SkillType.Dash);
            if (dashSkillData != null)
                dashSkillData.cooldown = 1f; // đồng bộ với PlayerDash.dashCooldown
            Debug.Log("[PlayerSkillManager] Detected PlayerDash component.");
        }

        // Auto-detect PlayerCombat (đánh thường)
        playerCombatComponent = GetComponent<PlayerCombat>() ?? GetComponentInParent<PlayerCombat>();
        if (playerCombatComponent != null)
        {
            normalAttackSkillData = skills.Find(s => s != null && s.skillType == SkillType.NormalAttack);
            Debug.Log("[PlayerSkillManager] Detected PlayerCombat component (NormalAttack).");
        }

        SortSkillsForHotbar();

        // Initialize skill dictionary
        skillByKey.Clear();
        foreach (var skill in skills)
        {
            if (skill == null) continue;
            
            // Reset skill state
            skill.Reset();
            
            // Check for duplicate keys
            if (skillByKey.ContainsKey(skill.activationKey))
            {
                Debug.LogWarning($"[PlayerSkillManager] Cảnh báo: Skill '{skill.skillName}' và skill khác đều dùng phím '{skill.activationKey}'!");
            }
            else
            {
                skillByKey[skill.activationKey] = skill;
            }
            
            // Initialize skill effect animator cho tất cả skill types
            InitializeSkillEffect(skill);
        }
        
        Debug.Log($"[PlayerSkillManager] Đã khởi tạo {skillByKey.Count} skill(s) (bao gồm Teleport nếu có)");
    }

    public void SortSkillsForHotbar()
    {
        skills.Sort((a, b) => GetSkillHotbarOrder(a).CompareTo(GetSkillHotbarOrder(b)));
    }

    private static int GetSkillHotbarOrder(SkillData skill)
    {
        if (skill == null) return int.MaxValue;
        if (skill.skillType == SkillType.NormalAttack || string.Equals(skill.skillCode, "NORMAL_ATTACK", System.StringComparison.OrdinalIgnoreCase))
            return 0;
        if (skill.skillType == SkillType.Dash || string.Equals(skill.skillCode, "DASH", System.StringComparison.OrdinalIgnoreCase))
            return 9000;
        if (skill.skillType.ToString().StartsWith("Hybrid"))
            return 5000 + Mathf.Max(0, skill.requiredPlayerLevel);
        return 100 + Mathf.Max(0, skill.requiredPlayerLevel);
    }
    
    private void InitializeSkillEffect(SkillData skill)
    {
        GameObject skillEffectObj = skill.playerSkillEffectObject ?? defaultSkillEffectObject;
        if (skillEffectObj == null) return;
        
        string key = skill.skillName;
        
        // Tìm Animator
        Animator animator = skillEffectObj.GetComponent<Animator>();
        if (animator != null)
        {
            skillEffectAnimators[key] = animator;
        }
        
        // Tìm NetworkAnimator
        Unity.Netcode.Components.NetworkAnimator networkAnimator = skillEffectObj.GetComponent<Unity.Netcode.Components.NetworkAnimator>();
        if (networkAnimator != null)
        {
            skillEffectNetworkAnimators[key] = networkAnimator;
            // Disable nếu SkillEffect inactive
            if (!skillEffectObj.activeSelf)
            {
                networkAnimator.enabled = false;
            }
        }
    }
    
    private void Update()
    {
        if (!IsOwner) return;
        
        // Update cooldowns
        foreach (var skill in skills)
        {
            if (skill != null)
            {
                skill.UpdateCooldown(Time.deltaTime);
            }
        }
        
        // Handle input
        HandleSkillInput();

        // Đồng bộ cooldown khi PlayerDash được kích hoạt bằng phím tắt (LeftShift)
        // để hotbar UI hiển thị cooldown đúng
        if (dashSkillData != null && playerDashComponent != null
            && !playerDashComponent.CanUseNow && dashSkillData.CanUse())
        {
            dashSkillData.StartUsing();
            dashSkillData.StopUsing();
        }
    }
    
    private void HandleSkillInput()
    {
        // Xử lý auto-move mỗi frame (phải chạy trước xử lý key input)
        HandleAutoMove();

        // Không xử lý skill input khi đang gõ trong ô chat
        if (InputManager.Instance != null && InputManager.Instance.IsGameplayInputBlocked) return;

        bool mainSkillUsed = false;
        foreach (var kvp in skillByKey)
        {
            KeyCode key = kvp.Key;
            SkillData skill = kvp.Value;

            if (Input.GetKeyDown(key) && skill.CanUse() && !skill.IsUsing())
            {
                CancelAutoMoveInternal(); // hủy auto-move khi dùng skill khác
                UseSkill(skill);
                mainSkillUsed = true;
            }
        }

        // Xử lý NormalAttack bằng phím Z hoặc Enter
        // (LMB không còn tự động kích hoạt đánh thường nữa)
        if (!mainSkillUsed
            && normalAttackSkillData != null
            && normalAttackSkillData.CanUse() && !normalAttackSkillData.IsUsing())
        {
            bool zPressed     = Input.GetKeyDown(KeyCode.Z);
            bool enterPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
            if (zPressed || enterPressed)
                TryAttackOrAutoMove();
        }
    }

    // Nếu có mục tiêu được chọn và còn xa → bắt đầu auto-move.
    // Nếu trong tầm → đánh ngay.
    private void TryAttackOrAutoMove()
    {
        if (TargetSelector.HasTarget)
        {
            float dist = Vector2.Distance(transform.position, TargetSelector.CurrentTarget.position);
            if (dist > AUTO_MOVE_ATTACK_RANGE)
            {
                _autoMoveTarget = TargetSelector.CurrentTarget;
                _autoMoving     = true;
                return; // chờ đến gần rồi tự đánh
            }
        }
        UseSkill(normalAttackSkillData);
    }

    // Xử lý auto-move mỗi frame: inject hướng di chuyển vào InputManager cho đến khi đến nơi.
    private void HandleAutoMove()
    {
        if (!_autoMoving) return;

        if (InputManager.Instance != null && InputManager.Instance.IsGameplayInputBlocked)
        {
            CancelAutoMoveInternal();
            return;
        }

        // Hủy nếu Escape hoặc input thủ công
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelAutoMoveInternal();
            return;
        }

        if (_autoMoveTarget == null || !_autoMoveTarget.gameObject.activeInHierarchy)
        {
            CancelAutoMoveInternal();
            return;
        }

        float dist = Vector2.Distance(transform.position, _autoMoveTarget.position);
        if (dist <= AUTO_MOVE_ATTACK_RANGE)
        {
            // Đến nơi → dừng, đánh
            CancelAutoMoveInternal();
            if (normalAttackSkillData != null && normalAttackSkillData.CanUse() && !normalAttackSkillData.IsUsing())
                UseSkill(normalAttackSkillData);
            return;
        }

        float dir = _autoMoveTarget.position.x > transform.position.x ? 1f : -1f;
        InputManager.Instance?.SetAutoMoveInput(dir);
    }

    private void CancelAutoMoveInternal()
    {
        _autoMoving     = false;
        _autoMoveTarget = null;
        InputManager.Instance?.CancelAutoMove();
    }
    
    private void UseSkill(SkillData skill)
    {
        if (skill == null) return;
        if (!skill.CanUse() || skill.IsUsing()) return;

        Debug.Log("[PlayerSkillManager] UseSkill: " + skill.skillName + " | IsOwner=" + IsOwner + " | IsServer=" + IsServer + " | MP=" + dataSync?.networkMp.Value + "/" + dataSync?.networkMaxMp.Value + " | Cost=" + skill.currentMpCost);

        // Kiểm tra và trừ MP
        if (!TryConsumeMP(skill.currentMpCost)) return;
        EnemyClickHandler.NotifySkillUsedOnCurrentTarget();

        // Xử lý Teleport skill
        if (skill.skillType == SkillType.Teleport)
        {
            if (teleportSkillComponent != null && teleportSkillComponent.CanUseNow)
            {
                teleportSkillComponent.UseTeleport();
                skill.StartUsing(); // sync cooldown hiển thị trên hotbar
            }
            return;
        }

        // Xử lý Dash skill (delegate sang PlayerDash component)
        if (skill.skillType == SkillType.Dash)
        {
            if (playerDashComponent != null && playerDashComponent.CanUseNow)
            {
                playerDashComponent.Dash();
                skill.StartUsing();
                skill.StopUsing();
            }
            return;
        }

        // Xử lý NormalAttack (đánh thường — delegate sang PlayerCombat + đồng bộ animation)
        if (skill.skillType == SkillType.NormalAttack)
        {
            if (playerCombatComponent == null || !playerCombatComponent.CanAttackNow)
                return;

            if (IsServer)
            {
                UseNormalAttackLocal(skill);
            }
            else if (IsOwner)
            {
                // Pre-trigger locally để tránh delay round-trip ServerRpc
                if (!skill.disablePlayerSkillEffectAnimation && !string.IsNullOrEmpty(skill.animationTriggerName))
                    playerAnimator?.TriggerAttack();
                skill.StartUsing();
                skill.StopUsing();
                UseNormalAttackServerRpc(skill.skillName, transform.localScale.x >= 0f, skill.currentEffectValue);
            }
            return;
        }

        // Xử lý Melee skill (chỉ trigger animation tại vị trí player, không spawn projectile)
        if (skill.skillType == SkillType.Melee)
        {
            if (IsServer)
            {
                UseMeleeLocal(skill, transform.localScale.x >= 0f);
            }
            else if (IsOwner)
            {
                // Pre-trigger locally để tránh delay round-trip ServerRpc
                if (!skill.disablePlayerSkillEffectAnimation && !string.IsNullOrEmpty(skill.animationTriggerName))
                    playerAnimator?.TriggerAttack();
                bool facingRightMelee = transform.localScale.x >= 0f;
                // Bắt đầu cooldown trên client ngay lập tức để UI cập nhật đúng
                skill.StartUsing();
                skill.StopUsing();
                UseMeleeServerRpc(skill.skillName, facingRightMelee, skill.currentEffectValue);
            }
            return;
        }

        // Xử lý WindStep skill (ẩn player + animation + dash)
        if (skill.skillType == SkillType.WindStep)
        {
            if (windStepComponent != null && windStepComponent.CanUseNow)
            {
                windStepComponent.UseWindStep();
                skill.StartUsing();  // bắt đầu cooldown timer cho hotbar
                skill.StopUsing();   // xóa isUsing ngay — WindStepSkill.CanUseNow đã guard re-entry
            }
            return;
        }

        // Xử lý MetalShield skill (bất tử + xóa projectile chạm vào)
        if (skill.skillType == SkillType.MetalShield)
        {
            if (metalShieldComponent != null && metalShieldComponent.CanUseNow)
            {
                metalShieldComponent.UseMetalShield();
                skill.StartUsing();
                skill.StopUsing();
            }
            return;
        }

        // Xử lý WaterPillar skill (cây thánh rơi từ trên xuống)
        if (skill.skillType == SkillType.WaterPillar)
        {
            if (waterPillarComponent != null && waterPillarComponent.CanUseNow)
            {
                waterPillarComponent.UseWaterPillar();
                skill.StartUsing();
                skill.StopUsing();
            }
            return;
        }

        // Xử lý WaterArmorBuff skill (buff giáp cho đồng đội xung quanh)
        if (skill.skillType == SkillType.WaterArmorBuff)
        {
            if (waterArmorBuffComponent != null && waterArmorBuffComponent.CanUseNow)
            {
                waterArmorBuffComponent.UseWaterArmorBuff();
                skill.StartUsing();
                skill.StopUsing();
            }
            return;
        }

        // Xử lý FireRain skill (mưa lửa từ trên trời rơi xuống)
        if (skill.skillType == SkillType.FireRain)
        {
            if (fireRainComponent != null && fireRainComponent.CanUseNow)
            {
                fireRainComponent.UseFireRain();
                skill.StartUsing();
                skill.StopUsing();
            }
            return;
        }

        // Xử lý EarthAura skill (buff tấn công cho đồng đội xung quanh)
        if (skill.skillType == SkillType.EarthAura)
        {
            if (earthAuraComponent != null && earthAuraComponent.CanUseNow)
            {
                earthAuraComponent.UseEarthAura();
                skill.StartUsing();
                skill.StopUsing();
            }
            return;
        }

        // Xử lý EarthBoomerang skill (đạn boomerang quay về)
        if (skill.skillType == SkillType.EarthBoomerang)
        {
            if (earthBoomerangComponent != null && earthBoomerangComponent.CanUseNow)
            {
                earthBoomerangComponent.UseEarthBoomerang(skill.currentEffectValue);
                skill.StartUsing();
                skill.StopUsing();
            }
            return;
        }

        // Xử lý EarthBlinkStrike skill (dịch chuyển + DoT)
        if (skill.skillType == SkillType.EarthBlinkStrike)
        {
            if (earthBlinkStrikeComponent != null && earthBlinkStrikeComponent.CanUseNow)
            {
                earthBlinkStrikeComponent.UseEarthBlinkStrike();
                skill.StartUsing();
                skill.StopUsing();
            }
            return;
        }

        // Xử lý HybridBarrage skill (Kim Phong Liên Tiễn)
        if (skill.skillType == SkillType.HybridBarrage)
        {
            if (hybridMetalWindBarrageComponent != null && hybridMetalWindBarrageComponent.CanUseNow)
            {
                Vector2 dir = transform.localScale.x >= 0f ? Vector2.right : Vector2.left;
                if (hybridMetalWindBarrageComponent.TryUse(dir))
                {
                    skill.StartUsing();
                    skill.StopUsing();
                }
            }
            return;
        }

        // Xử lý HybridLavaAura skill (Hỏa Thổ Dung Nham)
        if (skill.skillType == SkillType.HybridLavaAura)
        {
            if (hybridLavaAuraComponent != null && hybridLavaAuraComponent.CanUseNow)
            {
                Vector2 dir = transform.localScale.x >= 0f ? Vector2.right : Vector2.left;
                if (hybridLavaAuraComponent.TryUse(dir))
                {
                    skill.StartUsing();
                    skill.StopUsing();
                }
            }
            return;
        }

        // Xử lý HybridVenom skill (Băng Độc Vĩnh Cửu)
        if (skill.skillType == SkillType.HybridVenom)
        {
            if (hybridVenomComponent != null && hybridVenomComponent.CanUseNow)
            {
                Vector2 dir = transform.localScale.x >= 0f ? Vector2.right : Vector2.left;
                if (hybridVenomComponent.TryUse(dir))
                {
                    skill.StartUsing();
                    skill.StopUsing();
                }
            }
            return;
        }

        if (skill.projectilePrefab == null)
        {
            Debug.LogWarning($"[PlayerSkillManager] Skill '{skill.skillName}' không có projectile prefab!");
            return;
        }
        
        // Chỉ owner mới spawn projectile
        // Spawn trên server để đồng bộ cho tất cả client
        if (IsOwner)
        {
            if (IsServer)
            {
                // Nếu là server và owner, spawn trực tiếp
                UseSkillLocal(skill);
            }
            else
            {
                // Nếu là client owner, gọi server để spawn với hướng hiện tại
                // Pre-trigger locally để tránh delay round-trip ServerRpc
                if (!skill.disablePlayerSkillEffectAnimation && !string.IsNullOrEmpty(skill.animationTriggerName))
                    playerAnimator?.TriggerAttack();
                bool facingRight = transform.localScale.x >= 0f;
                // Bắt đầu cooldown trên client ngay lập tức để UI cập nhật đúng
                skill.StartUsing();
                skill.StopUsing();
                UseSkillServerRpc(skill.skillName, facingRight, skill.currentEffectValue);
            }
        }
    }
    
    // Server RPC để client owner yêu cầu server spawn projectile
    [ServerRpc]
    private void UseSkillServerRpc(string skillName, bool facingRight, float effectValue = 0f)
    {
        // Tìm skill theo tên
        SkillData skill = skills.Find(s => s != null && s.skillName == skillName);
        if (skill != null)
        {
            // Lưu hướng player vào skill tạm thời để spawn đúng
            // Client truyền effectValue lên để server dùng đúng stats từ DB
            if (effectValue > 0f) skill.currentEffectValue = effectValue;
            UseSkillLocalWithDirection(skill, facingRight);
        }
    }

    // Server RPC để client owner yêu cầu server kích hoạt NormalAttack (hitbox + animation sync)
    [ServerRpc]
    private void UseNormalAttackServerRpc(string skillName, bool facingRight, float effectValue = 0f)
    {
        SkillData skill = skills.Find(s => s != null && s.skillName == skillName);
        if (skill != null)
        {
            if (effectValue > 0f) skill.currentEffectValue = effectValue;
            UseNormalAttackLocal(skill);
        }
    }

    // Kích hoạt NormalAttack trên server: hitbox qua PlayerCombat + animation sync tới tất cả client
    private void UseNormalAttackLocal(SkillData skill)
    {
        skill.StartUsing();

        // Trigger hitbox thông qua PlayerCombat
        int dmg = skill.currentEffectValue > 0f ? (int)skill.currentEffectValue : -1;
        playerCombatComponent?.TriggerAttack(dmg);

        if (!skill.disablePlayerSkillEffectAnimation && !string.IsNullOrEmpty(skill.animationTriggerName))
        {
            TriggerPlayerAttackClientRpc();
            // NormalAttack sprite nhìn PHẢI → spriteFacesLeft=false
            TriggerSkillEffectAnimationClientRpc(skill.animationTriggerName, spriteFacesLeft: false);
            float animLen = GetAnimationLength(skill.animationTriggerName, skill);
            pendingClearDelay = animLen > 0 ? animLen : 0.5f;
            Invoke(nameof(InvokeClearSprite), pendingClearDelay);
        }

        Invoke(nameof(ResetSkillState), 0.1f);
    }

    // Server RPC để client owner yêu cầu server kích hoạt skill Melee (animation, không projectile)
    [ServerRpc]
    private void UseMeleeServerRpc(string skillName, bool facingRight, float effectValue = 0f)
    {
        SkillData skill = skills.Find(s => s != null && s.skillName == skillName);
        if (skill != null)
        {
            // Client truyền effectValue từ DB để server dùng đúng sát thương
            if (effectValue > 0f) skill.currentEffectValue = effectValue;
            UseMeleeLocal(skill, facingRight); // truyền hướng nhìn từ client, tránh sai khi scale chưa sync
        }
    }

    // Kích hoạt Melee skill: chỉ trigger animation, không spawn projectile
    private void UseMeleeLocal(SkillData skill, bool facingRight)
    {
        skill.StartUsing();

        if (!skill.disablePlayerSkillEffectAnimation && !string.IsNullOrEmpty(skill.animationTriggerName))
        {
            // Phát animation attack của nhân vật trên TẤT CẢ client
            TriggerPlayerAttackClientRpc();

            // Phát animation trên TẤT CẢ client qua ClientRpc
            TriggerSkillEffectAnimationClientRpc(skill.animationTriggerName);

            // Sau khi animation xong, xóa sprite
            float animLen = GetAnimationLength(skill.animationTriggerName, skill);
            pendingClearDelay = animLen > 0 ? animLen : 0.8f;
            Invoke(nameof(InvokeClearSprite), pendingClearDelay);
        }

        // Gây damage melee sau delay nhỏ để khớp với hit-frame animation (chỉ server)
        if (IsServer)
            StartCoroutine(ApplyMeleeDamageDelayed(skill, facingRight));

        Invoke(nameof(ResetSkillState), 0.1f);
    }

    private IEnumerator ApplyMeleeDamageDelayed(SkillData skill, bool facingRight, float delay = 0.3f)
    {
        yield return new WaitForSeconds(delay);
        ApplyMeleeDamage(skill, facingRight);
    }

    private void ApplyMeleeDamage(SkillData skill, bool facingRight)
    {
        float range   = Mathf.Max(skill.spawnOffset * 2f, 1.5f);
        Vector2 center = (Vector2)transform.position
                        + new Vector2(facingRight ? range * 0.5f : -range * 0.5f, 0f);
        
        int dmg = (int)skill.currentEffectValue;
        if (dmg <= 0)
        {
            var stats = GetComponent<PlayerController>()?.stats;
            dmg = stats != null ? stats.baseDamage : 10;
        }

        Debug.Log($"[PlayerSkillManager] ApplyMeleeDamage | center={center} range={range} dmg={dmg}");

        // Không lọc LayerMask ở đây vì collider của enemy có thể nằm ở bất kỳ layer nào
        Collider2D[] hits = MapPhysicsQuery2D.OverlapCircleAll(gameObject, center, range);
        System.Collections.Generic.HashSet<int> damaged = new System.Collections.Generic.HashSet<int>();
        foreach (var hit in hits)
        {
            if (hit == null) continue;

            // Bỏ qua collider của chính mình (so sánh qua NetworkObject trên root)
            var selfNetObj = GetComponent<NetworkObject>();
            var hitRootNetObj = hit.GetComponentInParent<NetworkObject>();
            if (selfNetObj != null && hitRootNetObj != null && hitRootNetObj.NetworkObjectId == selfNetObj.NetworkObjectId) continue;

            // Tránh damage cùng một enemy 2 lần (nếu có nhiều collider)
            int goId = hit.gameObject.GetInstanceID();
            int rootId = hit.transform.root.gameObject.GetInstanceID();
            if (damaged.Contains(rootId)) continue;

            // Gây damage cho enemy — dùng GetComponentInParent để tìm cả khi collider là child
            var netEnemy = hit.GetComponentInParent<NetworkEnemyHealth>();
            if (netEnemy != null)
            {
                Debug.Log($"[PlayerSkillManager] Melee hit NetworkEnemyHealth: {hit.transform.root.name} for {dmg}");
                netEnemy.TakeDamage(dmg, OwnerClientId);
                damaged.Add(rootId);
                continue;
            }
            var localEnemy = hit.GetComponentInParent<EnemyHealth>();
            if (localEnemy != null)
            {
                Debug.Log($"[PlayerSkillManager] Melee hit EnemyHealth: {hit.transform.root.name} for {dmg}");
                localEnemy.TakeDamage(dmg);
                damaged.Add(rootId);
                continue;
            }

            // PvP: gây damage cho player khác
            if (hit.CompareTag("Player") || hit.transform.root.CompareTag("Player"))
            {
                var netPlayer = hit.GetComponentInParent<NetworkPlayerHealth>();
                if (netPlayer != null && !damaged.Contains(rootId))
                {
                    netPlayer.TakeDamage(dmg);
                    damaged.Add(rootId);
                }
            }
        }

        Debug.Log($"[PlayerSkillManager] ApplyMeleeDamage done | {damaged.Count} targets hit.");
    }

    private float pendingClearDelay;
    private void InvokeClearSprite()
    {
        ClearSkillEffectSpriteClientRpc();
    }

    // Kích hoạt animation attack của nhân vật (phong.controller) trên TẤT CẢ client.
    // Dùng khi player dùng skill để sprite nhân vật cũng thể hiện đòn chém.
    [ClientRpc]
    private void TriggerPlayerAttackClientRpc()
    {
        // Owner đã trigger locally rồi — chỉ trigger cho các client khác
        if (!IsServer && IsOwner) return;
        if (playerAnimator == null)
            playerAnimator = GetComponent<PlayerAnimator>() ?? GetComponentInParent<PlayerAnimator>();
        playerAnimator?.TriggerAttack();
    }

    // Phát Animator trigger trên SkillEffect của player cho TẤT CẢ client.
    // Chỉ gọi từ server.
    // spriteFacesLeft=true : sprite gốc nhìn TRÁI (convention cũ — flip sang phải bằng flipX)
    // spriteFacesLeft=false: sprite gốc nhìn PHẢI (NormalAttack) — parent scale xử lý flip
    [ClientRpc]
    private void TriggerSkillEffectAnimationClientRpc(string triggerName, bool spriteFacesLeft = true)
    {
        GameObject skillEffectObj = defaultSkillEffectObject;
        if (skillEffectObj == null)
            skillEffectObj = transform.Find("SkillEffect")?.gameObject;
        if (skillEffectObj == null) return;

        if (!skillEffectObj.activeSelf)
            skillEffectObj.SetActive(true);

        SpriteRenderer sr = skillEffectObj.GetComponent<SpriteRenderer>();
        if (sr != null) sr.flipX = spriteFacesLeft;

        Animator animator = skillEffectObj.GetComponent<Animator>();
        if (animator == null || animator.runtimeAnimatorController == null) return;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == triggerName && param.type == AnimatorControllerParameterType.Trigger)
            {
                animator.SetTrigger(triggerName);
                break;
            }
        }
    }

    // Xóa sprite trên SkillEffect của player trên tất cả client.
    // Gọi sau khi animation kết thúc.
    [ClientRpc]
    public void ClearSkillEffectSpriteClientRpc()
    {
        GameObject skillEffectObj = defaultSkillEffectObject;
        if (skillEffectObj == null)
            skillEffectObj = transform.Find("SkillEffect")?.gameObject;
        if (skillEffectObj == null) return;

        SpriteRenderer sr = skillEffectObj.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = null;
    }
    
    // Spawn skill với hướng cụ thể (để sync đúng trên network)
    private void UseSkillLocalWithDirection(SkillData skill, bool facingRight)
    {
        skill.StartUsing();

        if (!string.IsNullOrEmpty(skill.animationTriggerName))
        {
            TriggerPlayerAttackClientRpc();
        }
        
        // Trigger animation trên player SkillEffect cho TẤT CẢ client (không chỉ server)
        if (!skill.disablePlayerSkillEffectAnimation && !string.IsNullOrEmpty(skill.animationTriggerName))
        {
            TriggerSkillEffectAnimationClientRpc(skill.animationTriggerName);
        }
        
        // Spawn projectile với hướng cụ thể
        SpawnProjectileWithDirection(skill, facingRight);
        
        // Reset skill state sau một khoảng thời gian ngắn
        Invoke(nameof(ResetSkillState), 0.1f);
        
        // Ẩn SkillEffect sau khi animation kết thúc (nếu cần)
        if (!string.IsNullOrEmpty(skill.animationTriggerName))
        {
            float animationLength = GetAnimationLength(skill.animationTriggerName, skill);
            if (animationLength > 0)
            {
                Invoke(nameof(HideSkillEffect), animationLength);
            }
        }
    }
    
    private void UseSkillLocal(SkillData skill)
    {
        skill.StartUsing();

        if (!string.IsNullOrEmpty(skill.animationTriggerName))
        {
            TriggerPlayerAttackClientRpc();
        }
        
        // Trigger animation trên player SkillEffect cho TẤT CẢ client
        if (!skill.disablePlayerSkillEffectAnimation && !string.IsNullOrEmpty(skill.animationTriggerName))
        {
            TriggerSkillEffectAnimationClientRpc(skill.animationTriggerName);
        }
        
        // Spawn projectile
        SpawnProjectile(skill);
        
        // Reset skill state sau một khoảng thời gian ngắn
        Invoke(nameof(ResetSkillState), 0.1f);
        
        // Ẩn SkillEffect sau khi animation kết thúc (nếu cần)
        if (!string.IsNullOrEmpty(skill.animationTriggerName))
        {
            float animationLength = GetAnimationLength(skill.animationTriggerName, skill);
            if (animationLength > 0)
            {
                Invoke(nameof(HideSkillEffect), animationLength);
            }
        }
    }
    
    private void TriggerPlayerSkillEffectAnimation(SkillData skill)
    {
        if (string.IsNullOrEmpty(skill.animationTriggerName)) return;
        
        GameObject skillEffectObj = skill.playerSkillEffectObject ?? defaultSkillEffectObject;
        if (skillEffectObj == null) return;
        
        string key = skill.skillName;
        
        if (!skillEffectAnimators.ContainsKey(key))
        {
            InitializeSkillEffect(skill);
        }
        
        if (skillEffectAnimators.TryGetValue(key, out Animator animator) && animator != null)
        {
            // Đảm bảo SkillEffect active
            if (!skillEffectObj.activeSelf)
            {
                skillEffectObj.SetActive(true);
            }

            // Flip sprite để khớp hướng nhân vật
            // Sprite gốc hướng TRÁI. Parent scale.x điều khiển flip trái/phải.
            // flipX=true: khi scale.x=1 (phải) → flip TRÁI→PHẢI ✓
            //             khi scale.x=-1 (trái) → double flip = về TRÁI gốc ✓
            SpriteRenderer skillEffectSr = skillEffectObj.GetComponent<SpriteRenderer>();
            if (skillEffectSr != null)
                skillEffectSr.flipX = true;
            
            // Enable NetworkAnimator nếu có
            if (skillEffectNetworkAnimators.TryGetValue(key, out var networkAnimator) && networkAnimator != null)
            {
                if (!networkAnimator.enabled)
                {
                    networkAnimator.enabled = true;
                }
            }
            
            // Trigger animation
            if (animator.runtimeAnimatorController != null)
            {
                bool hasParameter = false;
                foreach (AnimatorControllerParameter param in animator.parameters)
                {
                    if (param.name == skill.animationTriggerName && param.type == AnimatorControllerParameterType.Trigger)
                    {
                        hasParameter = true;
                        break;
                    }
                }
                
                if (hasParameter)
                {
                    animator.SetTrigger(skill.animationTriggerName);
                }
            }
        }
    }
    
    private void SpawnProjectile(SkillData skill)
    {
        // Xác định hướng player đang nhìn
        bool facingRight = transform.localScale.x >= 0f;
        SpawnProjectileWithDirection(skill, facingRight);
    }
    
    private void SpawnProjectileWithDirection(SkillData skill, bool facingRight)
    {
        Vector2 direction = facingRight ? Vector2.right : Vector2.left;
        
        // Tính vị trí spawn
        Vector3 spawnPosition = transform.position + new Vector3(
            facingRight ? skill.spawnOffset : -skill.spawnOffset,
            0f,
            0f
        );
        
        // Spawn projectile với NetworkObject để đồng bộ cho tất cả client
        GameObject projectile = Instantiate(skill.projectilePrefab, spawnPosition, Quaternion.identity);

        ApplyProjectileFacing(projectile, facingRight, skill.projectileSpriteFacesLeft);
        
        // Đảm bảo projectile có NetworkObject để đồng bộ network
        NetworkObject projectileNetworkObject = projectile.GetComponent<NetworkObject>();
        if (projectileNetworkObject == null)
        {
            projectileNetworkObject = projectile.AddComponent<NetworkObject>();
            Debug.LogWarning($"[PlayerSkillManager] Projectile '{skill.skillName}' không có NetworkObject, đã tự động thêm vào. Nên thêm NetworkObject vào Prefab!");
        }

        int projectileMapId = ResolveProjectileMapId();
        if (projectileMapId >= 0)
        {
            MapSceneManager.Instance?.MoveToMapScene(projectile, projectileMapId);
            ApplyProjectileMapVisibility(projectile, projectileMapId);
            Debug.Log($"[PlayerSkillManager] SpawnProjectile '{skill.skillName}' -> mapId={projectileMapId}, pos={spawnPosition}, facingRight={facingRight}");
        }
        else
        {
            Debug.LogWarning($"[PlayerSkillManager] Không resolve được mapId cho projectile '{skill.skillName}'. Projectile sẽ dùng physics scene mặc định.");
        }
        
        // Spawn projectile trên network (chỉ server mới spawn được)
        if (IsServer)
        {
            projectileNetworkObject.Spawn();
            projectile.GetComponent<NetworkVisibilityZoneFilter>()?.RefreshVisibility();

            // Gán owner để projectile không tự gây damage cho người bắn
            ulong ownerId = NetworkObjectId;
            var fireballDmg = projectile.GetComponent<FireballDamage>();
            if (fireballDmg != null)
            {
                fireballDmg.SetOwner(ownerId);
                // Apply effectValue từ DB (nếu có) thay vì dùng Inspector default
                if (skill.currentEffectValue > 0f) fireballDmg.SetDamage((int)skill.currentEffectValue);
                // Apply AttackBuff của owner vào projectile (giống PlayerCombat.PerformAttack)
                if (ActiveBuffManager.Instance != null)
                {
                    int atkBonusPct = Mathf.RoundToInt(ActiveBuffManager.Instance.GetBonusPct("AttackBuff") * 100f);
                    if (atkBonusPct > 0) fireballDmg.SetAttackBonus(atkBonusPct);
                }
            }
            var dotDmg = projectile.GetComponent<DotDamage>();
            if (dotDmg != null)
            {
                dotDmg.SetOwner(ownerId);
                if (skill.effectConfig != null) dotDmg.SetDebuffConfig(skill.effectConfig);
            }

            // Truyền debuff config từ SkillData vào projectile (tự động, không cần set thủ công)
            if (skill.effectConfig != null)
            {
                fireballDmg?.SetDebuffConfig(skill.effectConfig);
            }
        }
        else
        {
            // Nếu không phải server, chỉ spawn local (hoặc gọi RPC để server spawn)
            Debug.LogWarning("[PlayerSkillManager] Chỉ server mới spawn được projectile trên network!");
        }
        
        // Setup Animator cho projectile (tắt Apply Root Motion)
        Animator projectileAnimator = projectile.GetComponent<Animator>();
        if (projectileAnimator != null)
        {
            projectileAnimator.applyRootMotion = false;
            
            if (!string.IsNullOrEmpty(skill.animationTriggerName))
            {
                StartCoroutine(TriggerProjectileAnimationDelayed(projectileAnimator, skill.animationTriggerName));
            }
        }
        
        // Sync animation trigger to all clients; server already handled its own instance above.
        if (IsServer && projectileNetworkObject != null && projectileNetworkObject.IsSpawned)
            StartCoroutine(SyncProjectileAnimationToClientsDelayed(
                projectileNetworkObject.NetworkObjectId, skill.animationTriggerName));
        
        // Spawn SkillEffect instance gắn vào projectile (nếu có)
        if (skill.projectileSkillEffectPrefab != null)
        {
            GameObject projectileSkillEffect = Instantiate(skill.projectileSkillEffectPrefab, projectile.transform);
            projectileSkillEffect.transform.localPosition = Vector3.zero;
            
            Animator projectileSkillEffectAnimator = projectileSkillEffect.GetComponent<Animator>();
            if (projectileSkillEffectAnimator != null && !string.IsNullOrEmpty(skill.animationTriggerName))
            {
                if (!projectileSkillEffect.activeSelf)
                {
                    projectileSkillEffect.SetActive(true);
                }
                
                if (projectileSkillEffectAnimator.runtimeAnimatorController != null)
                {
                    bool hasParameter = false;
                    foreach (AnimatorControllerParameter param in projectileSkillEffectAnimator.parameters)
                    {
                        if (param.name == skill.animationTriggerName && param.type == AnimatorControllerParameterType.Trigger)
                        {
                            hasParameter = true;
                            break;
                        }
                    }
                    
                    if (hasParameter)
                    {
                        projectileSkillEffectAnimator.SetTrigger(skill.animationTriggerName);
                    }
                }
            }
        }
        
        // Setup Rigidbody2D
        Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();
        if (projectileRb == null)
        {
            projectileRb = projectile.AddComponent<Rigidbody2D>();
        }
        
        if (projectileRb != null)
        {
            if (projectileRb.constraints.HasFlag(RigidbodyConstraints2D.FreezePositionX))
            {
                projectileRb.constraints &= ~RigidbodyConstraints2D.FreezePositionX;
            }
            
            if (projectileRb.bodyType == RigidbodyType2D.Static)
            {
                projectileRb.bodyType = RigidbodyType2D.Dynamic;
            }
            
            projectileRb.gravityScale = 0f;
        }
        
        // Setup ProjectileMovement
        ProjectileMovement projectileMovement = projectile.GetComponent<ProjectileMovement>();
        if (projectileMovement == null)
        {
            projectileMovement = projectile.AddComponent<ProjectileMovement>();
        }
        
        if (projectileMovement != null)
        {
            projectileMovement.SetMovement(skill.projectileSpeed, direction.x);
            if (skill.projectileLifetime > 0f)
            {
                projectileMovement.SetLifetime(skill.projectileLifetime);
            }
        }
        else if (projectileRb != null)
        {
            projectileRb.velocity = direction * skill.projectileSpeed;
            if (skill.projectileLifetime > 0f)
            {
                Destroy(projectile, skill.projectileLifetime);
            }
        }
        
        // Hướng sprite đã được set ngay sau khi Instantiate.
    }

    private void ApplyProjectileFacing(GameObject projectile, bool facingRight, bool spriteFacesLeft)
    {
        if (projectile == null) return;

        // Dùng localScale.x thay vì sr.flipX để NetworkTransform đồng bộ hướng sang client.
        // SyncScaleX = 1 trên NetworkTransform → client nhận được scale đúng ngay khi spawn.
        Vector3 scale = projectile.transform.localScale;
        float scaleSign = spriteFacesLeft ? (facingRight ? -1f : 1f) : (facingRight ? 1f : -1f);
        projectile.transform.localScale = new Vector3(
            Mathf.Abs(scale.x) * scaleSign,
            scale.y,
            scale.z
        );
    }

    private int ResolveProjectileMapId()
    {
        int registryMapId = ZoneRoomRegistry.Instance?.GetClientRoom(OwnerClientId)?.MapId ?? -1;
        if (registryMapId >= 0)
            return registryMapId;

        if (DungeonManager.Instance != null && DungeonManager.Instance.ActiveDungeonMapId >= 0)
            return DungeonManager.Instance.ActiveDungeonMapId;

        if (ClientSceneController.Instance != null && ClientSceneController.Instance.CurrentMapId >= 0)
            return ClientSceneController.Instance.CurrentMapId;

        if (MapManager.Instance != null && MapManager.Instance.GetMapId() >= 0)
            return MapManager.Instance.GetMapId();

        return -1;
    }

    private static void ApplyProjectileMapVisibility(GameObject projectile, int mapId)
    {
        if (projectile == null || mapId < 0)
            return;

        var zoneTag = projectile.GetComponent<ZoneOwnerTag>() ?? projectile.AddComponent<ZoneOwnerTag>();
        zoneTag.SetZone(mapId, 0);

        var filter = projectile.GetComponent<NetworkVisibilityZoneFilter>() ?? projectile.AddComponent<NetworkVisibilityZoneFilter>();
        filter.InitializeForServer();
    }
    
    private IEnumerator TriggerProjectileAnimationDelayed(Animator animator, string triggerName)
    {
        yield return null;
        
        if (animator != null && animator.enabled && animator.runtimeAnimatorController != null)
        {
            try
            {
                animator.SetTrigger(triggerName);
                Debug.Log($"[PlayerSkillManager] Đã trigger animation '{triggerName}' trên projectile!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PlayerSkillManager] Lỗi khi trigger animation trên projectile: {e.Message}");
            }
        }
    }
    
    // Chờ 1 frame rồi gửi ClientRpc để client trigger animation trên projectile đã spawn.
    private System.Collections.IEnumerator SyncProjectileAnimationToClientsDelayed(
        ulong netObjId, string triggerName)
    {
        yield return null;
        SyncProjectileAnimationClientRpc(netObjId, triggerName);
    }
    
    [Unity.Netcode.ClientRpc]
    private void SyncProjectileAnimationClientRpc(ulong netObjId, string triggerName)
    {
        if (IsServer) return; // server đã trigger trực tiếp
        StartCoroutine(FindAndTriggerProjectileAnimation(netObjId, triggerName));
    }
    
    private System.Collections.IEnumerator FindAndTriggerProjectileAnimation(
        ulong netObjId, string triggerName)
    {
        float elapsed = 0f;
        while (elapsed < 2f)
        {
            if (NetworkManager.Singleton?.SpawnManager?.SpawnedObjects
                    .TryGetValue(netObjId, out var netObj) == true)
            {
                Animator anim = netObj.GetComponent<Animator>();
                if (anim != null && anim.runtimeAnimatorController != null)
                {
                    anim.Rebind();
                    anim.Update(0f);
                    if (!string.IsNullOrEmpty(triggerName))
                    {
                        foreach (var p in anim.parameters)
                        {
                            if (p.name == triggerName &&
                                p.type == UnityEngine.AnimatorControllerParameterType.Trigger)
                            {
                                anim.SetTrigger(triggerName);
                                break;
                            }
                        }
                    }
                }
                yield break;
            }
            elapsed += UnityEngine.Time.deltaTime;
            yield return null;
        }
    }
    
    private float GetAnimationLength(string triggerName, SkillData skill)
    {
        GameObject skillEffectObj = skill.playerSkillEffectObject ?? defaultSkillEffectObject;
        if (skillEffectObj == null) return 0.5f;
        
        string key = skill.skillName;
        if (skillEffectAnimators.TryGetValue(key, out Animator animator) && animator != null)
        {
            if (animator.runtimeAnimatorController != null)
            {
                RuntimeAnimatorController ac = animator.runtimeAnimatorController;
                foreach (AnimationClip clip in ac.animationClips)
                {
                    // So sánh tên clip và trigger sau khi xóa khoảng trắng
                    // VD: "skill 1" vs "Skill1" → cả hai thành "skill1" → match
                    string normalizedClip = System.Text.RegularExpressions.Regex.Replace(clip.name, @"\s+", "").ToLower();
                    string normalizedTrigger = System.Text.RegularExpressions.Regex.Replace(triggerName, @"\s+", "").ToLower();
                    if (normalizedClip.Contains(normalizedTrigger) || normalizedTrigger.Contains(normalizedClip))
                    {
                        return clip.length;
                    }
                }
            }
        }
        
        return 0.5f; // Default
    }
    
    private void ResetSkillState()
    {
        // Reset tất cả skill đang sử dụng
        foreach (var skill in skills)
        {
            if (skill != null && skill.IsUsing())
            {
                skill.StopUsing();
            }
        }
    }
    
    private void HideSkillEffect()
    {
        ClearSkillEffectSpriteClientRpc();
    }
    
    // Hàm public để script hoặc hệ thống khác gọi vào.
    public bool IsUsingSkill(string skillName)
    {
        foreach (var skill in skills)
        {
            if (skill != null && skill.skillName == skillName)
            {
                return skill.IsUsing();
            }
        }
        return false;
    }
    
    public bool CanUseSkill(string skillName)
    {
        foreach (var skill in skills)
        {
            if (skill != null && skill.skillName == skillName)
            {
                return skill.CanUse();
            }
        }
        return false;
    }
    
    public float GetSkillCooldownPercent(string skillName)
    {
        foreach (var skill in skills)
        {
            if (skill != null && skill.skillName == skillName)
            {
                return skill.GetCooldownPercent();
            }
        }
        return 0f;
    }

    // Lấy SkillData theo index (dùng cho SkillHotbarUI binding)
    public SkillData GetSkill(int index)
    {
        if (index < 0 || index >= skills.Count) return null;
        return skills[index];
    }

    // Lấy tổng số skill hiện tại
    public int GetSkillCount() => skills.Count;

    // Kích hoạt skill theo index — dùng khi nhấn nút UI hotbar thay thế phím tắt
    public void TryUseSkillByIndex(int index)
    {
        if (!IsOwner) return;
        if (index < 0 || index >= skills.Count) return;

        SkillData skill = skills[index];
        if (skill == null || !skill.CanUse() || skill.IsUsing()) return;

        // Nếu bấm nút đánh thường → dùng TryAttackOrAutoMove để hỗ trợ auto-move
        if (skill.skillType == SkillType.NormalAttack)
        {
            TryAttackOrAutoMove();
            return;
        }

        CancelAutoMoveInternal(); // skill khác hủy auto-move
        UseSkill(skill);
    }

    //  MP Consumption Helper

    // Kiểm tra đủ MP và trừ MP khi dùng skill.
    // Trả về false nếu không đủ MP (skill bị chặn).
    private bool TryConsumeMP(int cost)
    {
        if (cost <= 0) return true; // Skill không tốn MP

        if (dataSync == null)
        {
            dataSync = GetComponent<NetworkPlayerDataSync>();
            if (dataSync == null) return true; // Không có hệ thống MP → cho phép
        }

        if (dataSync.networkMp.Value < cost)
        {
            Debug.Log($"[PlayerSkillManager] Không đủ MP! Cần {cost}, hiện có {dataSync.networkMp.Value}");
            return false;
        }

        // Trừ MP
        if (IsServer)
        {
            dataSync.networkMp.Value = Mathf.Max(0, dataSync.networkMp.Value - cost);
        }
        else
        {
            // Client owner gọi ServerRpc để server trừ MP
            dataSync.ConsumeMpServerRpc(cost);
            // Cập nhật UI ngay lập tức (optimistic) — sẽ bị ghi đè khi server phản hồi
            // Không sửa trực tiếp vì NetworkVariable chỉ server mới ghi được
        }

        Debug.Log($"[PlayerSkillManager] Trừ {cost} MP. Còn lại: {dataSync.networkMp.Value}");
        return true;
    }
}
