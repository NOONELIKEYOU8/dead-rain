using UnityEngine;

/// <summary>
/// 攻击数据配置（ScriptableObject）
/// 每种攻击动作对应一个 AttackDataSO，支持多段攻击配置。
/// </summary>
[CreateAssetMenu(fileName = "NewAttackData", menuName = "Project/Enemy/AttackData")]
public class AttackDataSO : ScriptableObject
{
    [Header("攻击基础信息")]
    [Tooltip("攻击动作名称（对应 Animator 中的动画状态名）")]
    public string animationName = "Attack";

    [Tooltip("攻击伤害值")]
    public float damage = 10f;

    [Tooltip("攻击前摇时间（秒），在此期间不造成伤害")]
    public float startupTime = 0.3f;

    [Tooltip("攻击持续/活跃时间（秒），在此期间 Hitbox 激活")]
    public float activeTime = 0.2f;

    [Tooltip("攻击后摇时间（秒），在此期间无法操作")]
    public float recoveryTime = 0.4f;

    [Header("Hitbox 配置")]
    [Tooltip("Hitbox 偏移量（相对于挂载点）")]
    public Vector2 hitboxOffset = new Vector2(0.5f, 0f);

    [Tooltip("Hitbox 尺寸")]
    public Vector2 hitboxSize = new Vector2(1f, 1f);

    [Header("特殊效果")]
    [Tooltip("该攻击是否可被玩家格挡")]
    public bool canBeBlocked = true;

    [Tooltip("命中时给予玩家的架势条增长量（若玩家有架势条系统）")]
    public float stanceDamageOnHit = 15f;

    [Tooltip("命中时击退力度")]
    public float knockbackForce = 8f;
}
