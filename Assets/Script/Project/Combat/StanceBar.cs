using UnityEngine;
using System;

/// <summary>
/// 格挡架势条（Stance Bar）
/// 独立于生命值的数值槽（0~maxValue），控制敌人失衡状态的触发。
///
/// 增长条件：
///   - 敌人主动格挡时，随时间增加。
///   - 敌人攻击被玩家格挡时，瞬间大幅增加。
///   - 敌人被玩家命中时，少量增加。
///
/// 衰减逻辑：
///   - 非战斗状态下缓慢衰减。
///   - 受击/格挡时暂停衰减，延迟一段时间后恢复衰减。
///
/// 阈值触发：
///   - 当架势条达到阈值（默认满值）时，触发失衡事件。
/// </summary>
public class StanceBar : MonoBehaviour
{
    /// <summary>
    /// 架势条满值/失衡事件委托
    /// </summary>
    public event Action OnStanceBroken;

    /// <summary>
    /// 架势条值变化事件委托，参数为当前值和最大值的比例
    /// 可用于实时响应架势条变化，如动画状态更新,UI更新。
    /// </summary>
    public event Action<float> OnValueChanged;

    [Header("架势条数据配置")]
    [Tooltip("架势条数据 ScriptableObject")]
    public StanceBarDataSO stanceData;

    /// <summary>当前架势条值</summary>
    private float _currentValue;

    /// <summary>上次受击/格挡的时间（用于衰减延迟计算）</summary>
    private float _lastCombatTime;

    /// <summary>是否处于失衡状态</summary>
    private bool _isBroken = false;

    /// <summary>当前架势条值</summary>
    public float CurrentValue => _currentValue;

    /// <summary>架势条最大值</summary>
    public float MaxValue => stanceData != null ? stanceData.maxValue : 100f;

    /// <summary>架势条比例（0~1）</summary>
    public float NormalizedValue => MaxValue > 0 ? _currentValue / MaxValue : 0f;

    /// <summary>是否处于失衡状态</summary>
    public bool IsBroken => _isBroken;

    private void Awake()
    {
        // 初始化为 0，等待外部通过 SetStanceData() 进行完整初始化
        _currentValue = 0f;
    }

    private void Update()
    {
        if (_isBroken || stanceData == null) return;

        // 检查是否在衰减延迟内
        float timeSinceCombat = Time.time - _lastCombatTime;
        if (timeSinceCombat < stanceData.decayDelay) return;

        // 缓慢衰减
        if (_currentValue > 0)
        {
            _currentValue = Mathf.Max(0f, _currentValue - stanceData.decayPerSecond * Time.deltaTime);
            OnValueChanged?.Invoke(NormalizedValue);
        }
    }

    /// <summary>
    /// 主动格挡时随时间增加架势条（由 BlockState 每帧调用）
    /// </summary>
    public void AddBlockStance(float deltaTime)
    {
        if (_isBroken) return;
        AddStance(stanceData.blockGainPerSecond * deltaTime);
    }

    /// <summary>
    /// 攻击被玩家格挡时，瞬间大幅增加架势条
    /// </summary>
    public void AddBlockedAttackStance()
    {
        if (_isBroken) return;
        AddStance(stanceData.blockedAttackGain);
    }

    /// <summary>
    /// 被玩家命中时，增加架势条
    /// </summary>
    public void AddHitStance()
    {
        if (_isBroken) return;
        AddStance(stanceData.hitGain);
    }

    /// <summary>
    /// 增加架势条（内部方法）
    /// </summary>
    private void AddStance(float amount)
    {
        _currentValue = Mathf.Min(MaxValue, _currentValue + amount);
        _lastCombatTime = Time.time; // 重置衰减计时
        OnValueChanged?.Invoke(NormalizedValue);

        // 检查是否达到失衡阈值
        if (_currentValue >= MaxValue * stanceData.staggerThreshold && !_isBroken)
        {
            BreakStance();
        }
    }

    /// <summary>
    /// 触发失衡状态
    /// </summary>
    private void BreakStance()
    {
        _isBroken = true;
        _currentValue = MaxValue;
        Debug.Log($"[StanceBar] {gameObject.name} 架势条已满，进入失衡状态！");
        OnStanceBroken?.Invoke();
    }

    /// <summary>
    /// 重置架势条（敌人死亡或失衡恢复时调用）
    /// </summary>
    public void ResetStance()
    {
        _isBroken = false;
        _currentValue = stanceData != null ? stanceData.initialValue : 0f;
        _lastCombatTime = 0f;
        OnValueChanged?.Invoke(NormalizedValue);
    }

    /// <summary>
    /// 设置架势条数据（运行时动态切换配置）
    /// </summary>
    public void SetStanceData(StanceBarDataSO data)
    {
        stanceData = data;
        ResetStance();
    }
}
