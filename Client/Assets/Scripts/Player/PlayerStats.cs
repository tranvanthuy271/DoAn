using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStats", menuName = "Game/Player Stats", order = 1)]
public class PlayerStats : ScriptableObject
{
    [Header("Movement")]
    [Tooltip("Tốc độ di chuyển ngang")]
    public float moveSpeed = 5f;

    [Header("Jump")]
    [Tooltip("Lực nhảy")]
    public float jumpForce = 10f;
    
    [Tooltip("Thời gian có thể giữ nhảy để nhảy cao hơn")]
    public float jumpTime = 0.35f;

    [Header("Flight")]
    [Tooltip("Tốc độ bay lên")]
    public float flySpeed = 5f;
    
    [Tooltip("Thời gian bay tối đa (giây)")]
    public float maxFlightTime = 3f;
    
    [Tooltip("Thời gian chờ để bay lại (giây)")]
    public float flightCooldown = 2f;

    [Header("Physics")]
    [Tooltip("Trọng lực")]
    public float gravity = 3f;

    [Header("Combat")]
    [Tooltip("Máu tối đa")]
    public int maxHealth = 100;
    
    [Tooltip("Sát thương cơ bản")]
    public int baseDamage = 10;
    
    [Tooltip("Tốc độ đánh")]
    public float attackSpeed = 1f;
}

