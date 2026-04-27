using UnityEngine;

/// <summary>
/// 空闲状态（IdleState）
/// 敌人原地待命，持续检测玩家。
/// 如果发现玩家则切换到追击状态，否则在等待一段时间后切换到巡逻状态。
/// </summary>
public class IdleState : IState
{
    private readonly EnemyBase _enemy;
    private float _waitTimer;

    public IdleState(EnemyBase enemy)
    {
        _enemy = enemy;
    }

    public void Enter()
    {
        Debug.Log($"[LOG] [{_enemy.name}] 进入 Idle 状态");
        _waitTimer = _enemy.Data.patrolWaitTime;
        // [ANIM_DISABLED] _enemy.Animator.SetBool("IsMoving", false);
        _enemy.SetVelocityX(0f);
    }

    public void Update(float deltaTime)
    {
        // 检测玩家
        if (_enemy.DetectPlayer())
        {
            _enemy.StateMachine.ForceChangeState(EnemyState.Chase);
            return;
        }

        // 等待计时
        _waitTimer -= deltaTime;
        if (_waitTimer <= 0f)
        {
            _enemy.StateMachine.ForceChangeState(EnemyState.Patrol);
        }
    }

    public void FixedUpdate()
    {
        // 空闲状态无物理逻辑
    }

    public void Exit()
    {
        // 清理
    }

    public EnemyState GetStateType() => EnemyState.Idle;
}
