using UnityEngine;

/// <summary>
/// 死亡状态（DeadState）
/// 敌人生命值归零或被处决后进入的最终状态。
/// 播放死亡动画，禁用所有碰撞体和组件。
/// 此状态不可退出。
/// </summary>
public class DeadState : IState
{
    private readonly EnemyBase _enemy;

    public DeadState(EnemyBase enemy)
    {
        _enemy = enemy;
    }

    public void Enter()
    {
        Debug.Log($"[LOG] [{_enemy.name}] 进入 Dead 状态");
        // 播放死亡动画
        // [ANIM_DISABLED] _enemy.Animator.SetBool("IsMoving", false);
        // [ANIM_DISABLED] _enemy.Animator.SetBool("IsAttacking", false);
        // [ANIM_DISABLED] _enemy.Animator.SetBool("IsBlocking", false);
        // [ANIM_DISABLED] _enemy.Animator.SetTrigger("Dead");
        _enemy.SetVelocityX(0f);

        // 关闭所有判定盒
        _enemy.Hitbox.DisableHitbox();
        _enemy.BlockBox.DisableBlock();
        _enemy.Hurtbox.SetInvincible(true);

        // 禁用碰撞体（防止死亡后仍被检测）
        Collider2D[] colliders = _enemy.GetComponentsInChildren<Collider2D>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }

        // 禁用 Rigidbody（防止物理干扰）
        Rigidbody2D rb = _enemy.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
        }

        Debug.Log($"[DeadState] {_enemy.name} 已死亡。");
    }

    public void Update(float deltaTime)
    {
        // 死亡状态无逻辑，可在此添加淡出/销毁计时
    }

    public void FixedUpdate()
    {
        // 死亡状态无物理逻辑
    }

    public void Exit()
    {
        // 死亡状态不可退出
        Debug.LogWarning($"[DeadState] 尝试从死亡状态退出，这是不允许的。");
    }

    public EnemyState GetStateType() => EnemyState.Dead;
}
