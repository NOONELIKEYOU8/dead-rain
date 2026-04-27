using UnityEngine;

/// <summary>
/// 格挡架势条数据配置（ScriptableObject）
/// 控制架势条的增长、衰减和阈值行为。
/// </summary>
[CreateAssetMenu(fileName = "NewStanceBarData", menuName = "Project/Enemy/StanceBarData")]
public class StanceBarDataSO : ScriptableObject
{
    [Header("架势条基础参数")]
    [Tooltip("架势条最大值")]
    [Range(0f, 200f)]
    public float maxValue = 100f;

    [Tooltip("架势条初始值")]
    [Range(0f, 200f)]
    public float initialValue = 0f;

    [Header("增长参数")]
    [Tooltip("敌人主动格挡时，每秒架势条增长量")]
    public float blockGainPerSecond = 5f;

    [Tooltip("敌人攻击被玩家格挡时，瞬间增长量")]
    public float blockedAttackGain = 25f;

    [Tooltip("敌人被玩家普通命中时，架势条增长量")]
    public float hitGain = 10f;

    [Header("衰减参数")]
    [Tooltip("非战斗状态下，每秒架势条衰减量")]
    public float decayPerSecond = 8f;

    [Tooltip("衰减延迟时间（秒），最后一次受击/格挡后多久开始衰减）")]
    public float decayDelay = 3f;

    [Header("阈值触发")]
    [Tooltip("架势条达到此比例（0~1）时触发失衡状态，1.0 = 满值触发")]
    [Range(0.5f, 1f)]
    public float staggerThreshold = 1f;

    [Header("Boss 特殊参数")]
    [Tooltip("Boss 是否在失衡后自动恢复（而非直接进入可处决状态）")]
    public bool bossAutoRecover = false;

    [Tooltip("Boss 自动恢复时间（秒），仅 bossAutoRecover 为 true 时有效")]
    public float bossRecoverTime = 3f;
}
