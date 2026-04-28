using UnityEngine;

/// <summary>
/// 失衡状态（StaggeredState）
/// 当架势条满时触发，敌人被打断当前动作，进入可被处决状态。
/// 敌人在此状态下无法行动，持续一段时间后恢复（Boss）或保持可处决（小怪）。
/// 玩家在可处决范围内可触发处决。
/// </summary>
public class StaggeredState : IState
{
    private readonly EnemyBase _enemy;

    /// <summary>失衡持续时间计时器</summary>
    private float _staggerTimer;

    /// <summary>失衡持续时间（秒）</summary>
    private float _staggerDuration;

    public StaggeredState(EnemyBase enemy)
    {
        _enemy = enemy;
    }

    public void Enter()
    {
        Debug.Log($"[LOG] [{_enemy.name}] 进入 Staggered 状态（可被处决）");
        // [ANIM_DISABLED] _enemy.Animator.SetBool("IsMoving", false);
        // [ANIM_DISABLED] _enemy.Animator.SetBool("IsAttacking", false);
        // [ANIM_DISABLED] _enemy.Animator.SetBool("IsBlocking", false);
        // [ANIM_DISABLED] _enemy.Animator.SetTrigger("Staggered");
        _enemy.SetVelocityX(0f);

        // 确保 BlockBox 关闭
        _enemy.BlockBox.DisableBlock();

        // Boss 可能在失衡后自动恢复
        if (_enemy.Data.enemyType == EnemyType.Boss && _enemy.StanceData.bossAutoRecover)
        {
            _staggerDuration = _enemy.StanceData.bossRecoverTime;
        }
        else
        {
            // 小怪：持续失衡直到被处决或超时恢复
            _staggerDuration = 5f;
        }

        _staggerTimer = _staggerDuration;

        Debug.Log($"[StaggeredState] {_enemy.name} 进入失衡状态（可被处决）");
    }

    public void Update(float deltaTime)
    {
        // 检查是否被处决（由 ExecutionSystem 触发状态切换，此处无需额外处理）

        _staggerTimer -= deltaTime;
        if (_staggerTimer <= 0f)
        {
            // 超时恢复
            Debug.Log($"[LOG] [{_enemy.name}] 失衡超时，自动恢复");
            _enemy.StanceBar.ResetStance();
            // [ANIM_DISABLED] _enemy.Animator.SetTrigger("Recover");
            _enemy.StateMachine.ForceChangeState(EnemyState.Chase);
        }
    }

    public void FixedUpdate()
    {
        // 失衡状态下不移动
    }

    public void Exit()
    {
        // [ANIM_DISABLED] _enemy.Animator.ResetTrigger("Staggered");
        // [ANIM_DISABLED] _enemy.Animator.ResetTrigger("Recover");
    }

    public EnemyState GetStateType() => EnemyState.Staggered;
}
