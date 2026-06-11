using System.Collections.Generic;

// Data class mang thông tin hiển thị của một enemy instance.
// Xây dựng bởi EnemyClickHandler và truyền sang EnemyInfoPanel.Show().
[System.Serializable]
public class EnemyStats
{
    // Tên hiển thị của enemy.
    public string enemyName;

    // HP hiện tại (sync cho tất cả clients qua NetworkEnemyHealth).
    public int currentHp;

    // HP tối đa.
    public int maxHp;

    // Hệ nguyên tố: Fire / Water / Earth / Metal / Wood / Wind / None.
    public string elementType;

    // Level của enemy (từ spawn_json.level).
    public int level;

    // EXP thưởng khi giết enemy này.
    public int expReward;
}

