using UnityEngine;

/// <summary>
/// 敌人状态机（EnemyStateMachine）
/// 继承 MonoBehaviour，挂载到敌人预制体上。
/// 负责创建和注册所有状态实例，并在每帧驱动状态机更新。
/// </summary>
public class EnemyStateMachine : MonoBehaviour
{
    /// <summary>底层有限状态机</summary>
    private FSM _fsm;

    /// <summary>敌人基类引用（各状态通过此引用访问敌人数据）</summary>
    private EnemyBase _enemy;

    /// <summary>FSM 实例</summary>
    public FSM Machine => _fsm;

    /// <summary>当前状态类型</summary>
    public EnemyState CurrentState => _fsm.CurrentStateType;

    private void Awake()
    {
        _enemy = GetComponent<EnemyBase>();
        _fsm = new FSM();
    }

    private void Start()
    {
        // 注册所有状态
        RegisterAllStates();

        // 初始进入巡逻状态
        _fsm.ChangeState(EnemyState.Patrol);
    }

    private void Update()
    {
        _fsm.Update(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        _fsm.FixedUpdate();
    }

    /// <summary>
    /// 注册所有敌人状态
    /// 状态实例通过传入 EnemyBase 引用来访问敌人数据和方法。
    /// </summary>
    private void RegisterAllStates()
    {
        _fsm.RegisterState(EnemyState.Idle, new IdleState(_enemy));
        _fsm.RegisterState(EnemyState.Patrol, new PatrolState(_enemy));
        _fsm.RegisterState(EnemyState.Chase, new ChaseState(_enemy));
        _fsm.RegisterState(EnemyState.Attack, new AttackState(_enemy));
        _fsm.RegisterState(EnemyState.Block, new BlockState(_enemy));
        _fsm.RegisterState(EnemyState.Staggered, new StaggeredState(_enemy));
        _fsm.RegisterState(EnemyState.Executed, new ExecutedState(_enemy));
        _fsm.RegisterState(EnemyState.Dead, new DeadState(_enemy));
    }

    /// <summary>
    /// 外部强制切换状态（如架势条满时强制进入失衡状态）
    /// </summary>
    /// <param name="stateType">目标状态</param>
    public void ForceChangeState(EnemyState stateType)
    {
        _fsm.ChangeState(stateType);
    }

    /// <summary>
    /// 检查当前是否处于可被处决状态（失衡状态）
    /// </summary>
    public bool IsInStaggeredState()
    {
        return _fsm.IsInState(EnemyState.Staggered);
    }
}
