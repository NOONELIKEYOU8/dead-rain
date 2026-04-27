using System.Collections.Generic;

/// <summary>
/// 敌人状态枚举
/// 定义敌人所有可能的状态类型。
/// </summary>
public enum EnemyState
{
    /// <summary>空闲/待机</summary>
    Idle,

    /// <summary>巡逻</summary>
    Patrol,

    /// <summary>追击玩家</summary>
    Chase,

    /// <summary>攻击</summary>
    Attack,

    /// <summary>格挡</summary>
    Block,

    /// <summary>失衡（架势条满后触发）</summary>
    Staggered,

    /// <summary>被处决中</summary>
    Executed,

    /// <summary>死亡</summary>
    Dead
}

/// <summary>
/// 有限状态机（FSM）
/// 通用状态管理器，负责状态的注册、切换和更新。
/// </summary>
public class FSM
{
    /// <summary>当前状态</summary>
    private IState _currentState;

    /// <summary>状态字典（状态类型 → 状态实例）</summary>
    private readonly Dictionary<EnemyState, IState> _states = new Dictionary<EnemyState, IState>();

    /// <summary>当前状态类型</summary>
    public EnemyState CurrentStateType { get; private set; } = EnemyState.Idle;

    /// <summary>当前状态实例</summary>
    public IState CurrentState => _currentState;

    /// <summary>
    /// 注册状态到状态机
    /// </summary>
    /// <param name="stateType">状态类型</param>
    /// <param name="state">状态实例</param>
    public void RegisterState(EnemyState stateType, IState state)
    {
        if (_states.ContainsKey(stateType))
        {
            _states[stateType] = state;
        }
        else
        {
            _states.Add(stateType, state);
        }
    }

    /// <summary>
    /// 切换到指定状态
    /// 如果目标状态与当前状态相同，则不执行切换。
    /// </summary>
    /// <param name="stateType">目标状态类型</param>
    public void ChangeState(EnemyState stateType)
    {
        if (CurrentStateType == stateType) return;

        // 退出当前状态,防止为空时调用
        _currentState?.Exit();
 
        // 切换到新状态
        if (_states.TryGetValue(stateType, out IState newState))
        {
            _currentState = newState;
            CurrentStateType = stateType;
            _currentState.Enter();
        }
        else
        {
            UnityEngine.Debug.LogError($"[FSM] 未注册状态: {stateType}，无法切换！");
        }
    }

    /// <summary>
    /// 每帧更新当前状态
    /// </summary>
    /// <param name="deltaTime">帧间隔时间</param>
    public void Update(float deltaTime)
    {
        _currentState?.Update(deltaTime);
    }

    /// <summary>
    /// 物理更新当前状态
    /// </summary>
    public void FixedUpdate()
    {
        _currentState?.FixedUpdate();
    }

    /// <summary>
    /// 获取指定类型的已注册状态
    /// </summary>
    /// <param name="stateType">状态类型</param>
    /// <returns>状态实例，未注册则返回 null</returns>
    public IState GetState(EnemyState stateType)
    {
        _states.TryGetValue(stateType, out IState state);
        return state;
    }

    /// <summary>
    /// 检查当前状态是否为指定类型
    /// </summary>
    public bool IsInState(EnemyState stateType)
    {
        return CurrentStateType == stateType;
    }
}
