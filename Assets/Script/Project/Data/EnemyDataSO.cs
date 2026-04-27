using UnityEngine;

/// <summary>
/// 敌人基础数据配置（ScriptableObject）
/// 所有数值均从此配置读取，禁止硬编码。
/// </summary>
[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Project/Enemy/EnemyData")]
public class EnemyDataSO : ScriptableObject
{
    [Header("基础属性")]
    [Tooltip("敌人最大生命值")]
    public float maxHealth = 100f;

    [Tooltip("敌人移动速度")]
    public float moveSpeed = 3f;

    [Tooltip("敌人追击速度（通常高于移动速度）")]
    public float chaseSpeed = 4.5f;

    [Header("检测参数")]
    [Tooltip("视野检测距离")]
    public float detectionRange = 8f;

    [Tooltip("攻击触发距离")]
    public float attackRange = 1.5f;

    [Tooltip("可处决距离")]
    public float executionRange = 1.2f;

    [Header("AI 行为参数")]
    [Tooltip("巡逻到达路径点后的等待时间（秒）")]
    public float patrolWaitTime = 1.5f;

    [Tooltip("玩家攻击时触发格挡的概率（0~1）")]
    [Range(0f, 1f)]
    public float blockChance = 0.5f;

    [Tooltip("攻击冷却时间（秒）")]
    public float attackCooldown = 2f;

    [Header("受击参数")]
    [Tooltip("受击后无敌时间（秒）")]
    public float invincibleDuration = 0.5f;

    [Tooltip("受击击退力度")]
    public float knockbackForce = 5f;

    [Header("类型标识")]
    [Tooltip("敌人类型：普通小怪 / Boss")]
    public EnemyType enemyType = EnemyType.Normal;

    [Tooltip("敌人显示名称")]
    public string displayName = "未命名敌人";
}

/// <summary>
/// 敌人类型枚举
/// </summary>
public enum EnemyType
{
    Normal,  // 普通小怪
    Boss     // Boss
}
