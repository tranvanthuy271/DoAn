using System.Collections.Generic;

/// <summary>
/// Data class mang thông tin hiển thị của một enemy instance.
/// Xây dựng bởi EnemyClickHandler và truyền sang EnemyInfoPanel.Show().
/// </summary>
[System.Serializable]
public class EnemyStats
{
    /// <summary>Tên hiển thị của enemy.</summary>
    public string enemyName;

    /// <summary>HP hiện tại (sync cho tất cả clients qua NetworkEnemyHealth).</summary>
    public int currentHp;

    /// <summary>HP tối đa.</summary>
    public int maxHp;

    /// <summary>Hệ nguyên tố: Fire / Water / Earth / Metal / Wood / Wind / None.</summary>
    public string elementType;

    /// <summary>Level của enemy (từ spawn_json.level).</summary>
    public int level;

    /// <summary>EXP thưởng khi giết enemy này.</summary>
    public int expReward;
}

