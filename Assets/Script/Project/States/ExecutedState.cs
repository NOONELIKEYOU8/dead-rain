using UnityEngine;

/// <summary>
/// 被处决状态（ExecutedState）
/// 当玩家在可处决范围内对失衡状态的敌人触发处决时进入。
/// 播放处决动画，期间敌人和玩家均无敌。
/// 动画结束后敌人直接死亡。
/// </summary>
public class ExecutedState : IState
{
    private readonly EnemyBase _enemy;

    /// <summary>处决动画持续时间（秒）</summary>
    private const float EXECUTION_DURATION = 1.5f;

    private float _executionTimer;

    public ExecutedState(EnemyBase enemy)
    {
        _enemy = enemy;
    }

    public void Enter()
    {
        Debug.Log($"[LOG] [{_enemy.name}] 进入 Executed 状态，处决动画播放中");
        _executionTimer = EXECUTION_DURATION;

        // 敌人无敌（处决动画期间不可被打断）
        _enemy.Hurtbox.SetInvincible(true);

        // 播放处决动画
        // [ANIM_DISABLED] _enemy.Animator.SetBool("IsMoving", false);
        // [ANIM_DISABLED] _enemy.Animator.SetBool("IsAttacking", false);
        // [ANIM_DISABLED] _enemy.Animator.SetBool("IsBlocking", false);
        // [ANIM_DISABLED] _enemy.Animator.SetTrigger("Executed");
        _enemy.SetVelocityX(0f);

        // 确保 BlockBox 关闭
        _enemy.BlockBox.DisableBlock();

        // 通知 ExecutionSystem 播放玩家处决动画（双方无敌）
        ExecutionSystem.Instance?.OnExecutionStarted(_enemy.gameObject);

        Debug.Log($"[ExecutedState] {_enemy.name} 正在被处决...");
    }

    public void Update(float deltaTime)
    {
        _executionTimer -= deltaTime;
        if (_executionTimer <= 0f)
        {
            // 处决动画结束，敌人死亡
            _enemy.Die();
        }
    }

    public void FixedUpdate()
    {
        // 处决期间不移动
    }

    public void Exit()
    {
        // 清理无敌状态（虽然即将死亡，但保持接口一致性）
        _enemy.Hurtbox.SetInvincible(false);
        // [ANIM_DISABLED] _enemy.Animator.ResetTrigger("Executed");
    }

    public EnemyState GetStateType() => EnemyState.Executed;
}
