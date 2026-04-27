using UnityEngine;

/// <summary>
/// 格挡状态（BlockState）
/// 敌人举起盾牌/武器进行格挡，激活 BlockBox 判定。
/// 格挡期间架势条随时间增长。
/// 持续格挡一段时间后自动解除，或由 AI 决策主动解除。
/// 如果架势条满，强制退出格挡进入失衡状态。
/// </summary>
public class BlockState : IState
{
    private readonly EnemyBase _enemy;

    /// <summary>最大格挡持续时间（秒）</summary>
    private const float MAX_BLOCK_DURATION = 3f;

    /// <summary>格挡计时器</summary>
    private float _blockTimer;

    public BlockState(EnemyBase enemy)
    {
        _enemy = enemy;
    }

    public void Enter()
    {
        Debug.Log($"[LOG] [{_enemy.name}] 进入 Block 状态");
        _blockTimer = MAX_BLOCK_DURATION;
        // [ANIM_DISABLED] _enemy.Animator.SetBool("IsMoving", false);
        // [ANIM_DISABLED] _enemy.Animator.SetBool("IsBlocking", true);
        _enemy.SetVelocityX(0f);

        // 激活格挡判定盒
        _enemy.BlockBox.EnableBlock();
    }

    public void Update(float deltaTime)
    {
        // 架势条满时强制退出
        if (_enemy.StanceBar.IsBroken)
        {
            ExitBlock();
            _enemy.StateMachine.ForceChangeState(EnemyState.Staggered);
            return;
        }

        // 格挡期间架势条随时间增长
        _enemy.StanceBar.AddBlockStance(deltaTime);

        // 格挡计时
        _blockTimer -= deltaTime;
        if (_blockTimer <= 0f)
        {
            ExitBlock();
            _enemy.StateMachine.ForceChangeState(EnemyState.Chase);
        }
    }

    public void FixedUpdate()
    {
        // 格挡状态下不移动
    }

    public void Exit()
    {
        ExitBlock();
    }

    public EnemyState GetStateType() => EnemyState.Block;

    /// <summary>
    /// 退出格挡，清理相关状态
    /// </summary>
    private void ExitBlock()
    {
        // [ANIM_DISABLED] _enemy.Animator.SetBool("IsBlocking", false);
        _enemy.BlockBox.DisableBlock();
    }
}
