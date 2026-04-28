using UnityEngine;

/// <summary>
/// 追击状态（ChaseState）
/// 发现玩家后，敌人以追击速度移动至攻击距离。
/// 到达攻击距离后，根据 AI 决策决定攻击或格挡。
/// 如果玩家脱离视野，则切换回巡逻状态。
/// </summary>
public class ChaseState : IState
{
    private readonly EnemyBase _enemy;

    public ChaseState(EnemyBase enemy)
    {
        _enemy = enemy;
    }

    public void Enter()
    {
        Debug.Log($"[LOG] [{_enemy.name}] 进入 Chase 状态，玩家距离: {_enemy.DistanceToPlayer:F1}");
        // [ANIM_DISABLED] _enemy.Animator.SetBool("IsMoving", true);
    }

    public void Update(float deltaTime)
    {
        // 检查玩家是否仍在视野内
        if (!_enemy.IsPlayerAlive() || !_enemy.DetectPlayer())
        {
            Debug.Log($"[LOG] [{_enemy.name}] 玩家脱离视野，返回 Patrol");
            _enemy.StateMachine.ForceChangeState(EnemyState.Patrol);
            return;
        }

        float distanceToPlayer = _enemy.DistanceToPlayer;

        // 到达攻击距离，交给 AI 决策
        if (distanceToPlayer <= _enemy.Data.attackRange)
        {
            _enemy.AIController.MakeCombatDecision();
            return;
        }

        // 架势条接近满值时，优先后撤或使用道具
        if (_enemy.StanceBar.NormalizedValue >= 0.7f)
        {
            _enemy.AIController.HandleHighStance();
            return;
        }

        // 架势条恢复正常，重置后撤状态
        _enemy.AIController.ResetRetreat();

        // 继续追击
        _enemy.MoveTowardPlayer(_enemy.Data.chaseSpeed);
    }

    public void FixedUpdate()
    {
        // 物理移动由 EnemyBase 的 Rigidbody 处理
    }

    public void Exit()
    {
        // [ANIM_DISABLED] _enemy.Animator.SetBool("IsMoving", false);
        _enemy.SetVelocityX(0f);
        _enemy.AIController.ResetRetreat();
    }

    public EnemyState GetStateType() => EnemyState.Chase;
}
