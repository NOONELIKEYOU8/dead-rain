using UnityEngine;

/// <summary>
/// 攻击状态（AttackState）
/// 敌人执行攻击动作，包含前摇、活跃、后摇三个阶段。
/// 前摇：准备攻击，Hitbox 未激活。
/// 活跃：Hitbox 激活，可对玩家造成伤害。
/// 后摇：攻击结束，无法操作。
/// 攻击完成后切换回追击状态。
/// </summary>
public class AttackState : IState
{
    private readonly EnemyBase _enemy;

    /// <summary>攻击阶段</summary>
    private enum AttackPhase
    {
        Startup,   // 前摇
        Active,    // 活跃（Hitbox 激活）
        Recovery   // 后摇
    }

    private AttackPhase _currentPhase;
    private float _phaseTimer;
    private AttackDataSO _currentAttackData;

    public AttackState(EnemyBase enemy)
    {
        _enemy = enemy;
    }

    public void Enter()
    {
        // 获取当前攻击数据
        _currentAttackData = _enemy.GetCurrentAttackData();
        if (_currentAttackData == null)
        {
            Debug.LogError($"[AttackState] {_enemy.name} 未配置 AttackDataSO！");
            _enemy.StateMachine.ForceChangeState(EnemyState.Chase);
            return;
        }

        Debug.Log($"[LOG] [{_enemy.name}] 进入 Attack 状态，伤害: {_currentAttackData.damage}");

        // 设置动画参数
        // [ANIM_DISABLED] _enemy.Animator.SetBool("IsMoving", false);
        // [ANIM_DISABLED] _enemy.Animator.SetBool("IsAttacking", true);
        // [ANIM_DISABLED] _enemy.Animator.SetTrigger("Attack");

        // 配置 Hitbox 参数
        _enemy.Hitbox.SetDamageParams(
            _currentAttackData.damage,
            _currentAttackData.knockbackForce,
            _currentAttackData.canBeBlocked
        );

        // 进入前摇阶段
        _currentPhase = AttackPhase.Startup;
        _phaseTimer = _currentAttackData.startupTime;

        _enemy.SetVelocityX(0f);
    }

    public void Update(float deltaTime)
    {
        if (_currentAttackData == null) return;

        _phaseTimer -= deltaTime;

        switch (_currentPhase)
        {
            case AttackPhase.Startup:
                if (_phaseTimer <= 0f)
                {
                    // 进入活跃阶段，激活 Hitbox
                    _currentPhase = AttackPhase.Active;
                    _phaseTimer = _currentAttackData.activeTime;
                    _enemy.Hitbox.EnableHitbox();
                }
                break;

            case AttackPhase.Active:
                if (_phaseTimer <= 0f)
                {
                    // 进入后摇阶段，关闭 Hitbox
                    _currentPhase = AttackPhase.Recovery;
                    _phaseTimer = _currentAttackData.recoveryTime;
                    _enemy.Hitbox.DisableHitbox();
                }
                break;

            case AttackPhase.Recovery:
                if (_phaseTimer <= 0f)
                {
                    // 攻击完成，重置冷却并切换到追击
                    _enemy.ResetAttackCooldown();
                    _enemy.StateMachine.ForceChangeState(EnemyState.Chase);
                }
                break;
        }
    }

    public void FixedUpdate()
    {
        // 攻击状态下不移动
    }

    public void Exit()
    {
        // 确保 Hitbox 关闭
        _enemy.Hitbox.DisableHitbox();
        // [ANIM_DISABLED] _enemy.Animator.SetBool("IsAttacking", false);
    }

    public EnemyState GetStateType() => EnemyState.Attack;
}
