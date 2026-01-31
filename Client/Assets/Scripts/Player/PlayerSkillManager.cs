using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Quản lý tất cả skill projectile của player
/// Chỉ cần thêm SkillData vào list và skill sẽ tự động hoạt động
/// </summary>
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
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        InitializeSkills();
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
        
        networkObject = GetComponent<NetworkObject>();
        
        // Tìm default SkillEffect nếu chưa gán
        if (defaultSkillEffectObject == null)
        {
            defaultSkillEffectObject = transform.Find("SkillEffect")?.gameObject;
        }
        
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
            
            // Initialize skill effect animator nếu có
            InitializeSkillEffect(skill);
        }
        
        Debug.Log($"[PlayerSkillManager] Đã khởi tạo {skillByKey.Count} skill(s)");
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
    }
    
    private void HandleSkillInput()
    {
        foreach (var kvp in skillByKey)
        {
            KeyCode key = kvp.Key;
            SkillData skill = kvp.Value;
            
            if (Input.GetKeyDown(key) && skill.CanUse() && !skill.IsUsing())
            {
                UseSkill(skill);
            }
        }
    }
    
    private void UseSkill(SkillData skill)
    {
        if (skill == null || skill.projectilePrefab == null)
        {
            Debug.LogWarning($"[PlayerSkillManager] Skill '{skill?.skillName}' không có projectile prefab!");
            return;
        }
        
        // Chỉ owner mới spawn projectile
        if (IsOwner)
        {
            UseSkillLocal(skill);
        }
    }
    
    private void UseSkillLocal(SkillData skill)
    {
        skill.StartUsing();
        
        // Trigger animation trên player SkillEffect (nếu có)
        if (!skill.disablePlayerSkillEffectAnimation)
        {
            TriggerPlayerSkillEffectAnimation(skill);
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
        Vector2 direction = facingRight ? Vector2.right : Vector2.left;
        
        // Tính vị trí spawn
        Vector3 spawnPosition = transform.position + new Vector3(
            facingRight ? skill.spawnOffset : -skill.spawnOffset,
            0f,
            0f
        );
        
        // Spawn projectile
        GameObject projectile = Instantiate(skill.projectilePrefab, spawnPosition, Quaternion.identity);
        
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
        
        // Flip sprite nếu cần
        SpriteRenderer projectileSprite = projectile.GetComponent<SpriteRenderer>();
        if (projectileSprite != null && !facingRight)
        {
            projectileSprite.flipX = true;
        }
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
                    // Tìm animation clip có tên tương tự trigger
                    if (clip.name.ToLower().Contains(triggerName.ToLower()))
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
        // Hide skill effect nếu cần
        // Có thể implement logic riêng cho từng skill nếu cần
    }
    
    // Public API
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
}
