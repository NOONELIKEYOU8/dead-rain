using UnityEngine;

/// <summary>
/// 敌人 AI 行为控制器（EnemyAIController）
/// 基于距离和视野的简单决策树，控制敌人的战斗行为决策。
///
/// 决策逻辑：
/// - 若玩家攻击且距离近 → 概率触发格挡
/// - 若玩家距离适中 → 触发攻击
/// - 若架势条接近满值 → 触发"后撤"或"使用道具"
/// </summary>
public class EnemyAIController : MonoBehaviour
{
    /// <summary>敌人基类引用</summary>
    private EnemyBase _enemy;

    /// <summary>攻击冷却计时器</summary>
    private float _attackCooldownTimer;

    private void Awake()
    {
        _enemy = GetComponent<EnemyBase>();
    }

    private void Update()
    {
        if (_enemy == null) return;

        // 更新攻击冷却
        if (_attackCooldownTimer > 0f)
        {
            _attackCooldownTimer -= Time.deltaTime;
        }

        // 更新后撤冷却
        if (_retreatCooldown > 0f)
        {
            _retreatCooldown -= Time.deltaTime;
        }
    }

    /// <summary>
    /// 战斗决策
    /// 在 ChaseState 中到达攻击距离时调用。
    /// 根据当前情况决定攻击或格挡。
    /// </summary>
    public void MakeCombatDecision()
    {
        if (_enemy == null) return;

        // 检查攻击冷却
        if (_attackCooldownTimer > 0f)
        {
            // 冷却中，尝试格挡
            TryBlock();
            return;
        }

        // 随机决定攻击或格挡
        float random = Random.value;

        if (random < _enemy.Data.blockChance)
        {
            // 触发格挡
            Debug.Log($"[LOG] [{_enemy.name}] AI 决策：格挡");
            TryBlock();
        }
        else
        {
            // 触发攻击
            Debug.Log($"[LOG] [{_enemy.name}] AI 决策：攻击");
            TryAttack();
        }
    }

    /// <summary>
    /// 处理高架势条情况
    /// 当架势条接近满值时，优先采取防御性行为。
    /// </summary>
    public void HandleHighStance()
    {
        if (_enemy == null) return;

        // 优先尝试使用道具
        if (_enemy.ItemSlot != null && _enemy.ItemSlot.CanUseItem())
        {
            _enemy.ItemSlot.UseItem();
            Debug.Log($"[LOG] [{_enemy.name}] AI 决策：使用道具（架势条过高 {_enemy.StanceBar.NormalizedValue:P0}）");
            return;
        }

        // 没有道具可用，后撤
        RetreatFromPlayer();
    }

    /// <summary>后撤冲击冷却计时器</summary>
    private float _retreatCooldown;

    /// <summary>后撤冷却时间（秒），避免连续冲击</summary>
    private const float RETREAT_COOLDOWN = 1.5f;

    /// <summary>后撤冲击力度</summary>
    private const float RETREAT_FORCE = 12f;

    /// <summary>
    /// 尝试攻击
    /// </summary>
    private void TryAttack()
    {
        if (_enemy == null) return;

        _enemy.StateMachine.ForceChangeState(EnemyState.Attack);
        _attackCooldownTimer = _enemy.Data.attackCooldown;
    }

    /// <summary>
    /// 尝试格挡
    /// </summary>
    private void TryBlock()
    {
        if (_enemy == null) return;

        _enemy.StateMachine.ForceChangeState(EnemyState.Block);
    }

    /// <summary>
    /// 后撤：一次性反向冲击力远离玩家，附带冷却间隔。
    /// </summary>
    private void RetreatFromPlayer()
    {
        if (_enemy == null || !_enemy.IsPlayerAlive()) return;
        if (_retreatCooldown > 0f) return;

        float retreatDir = -_enemy.DirectionToPlayer; // 背对玩家
        _enemy.SetVelocityX(retreatDir * RETREAT_FORCE);
        _enemy.FlipDirection(retreatDir);
        _retreatCooldown = RETREAT_COOLDOWN;
        Debug.Log($"[AIController] {_enemy.name} 执行后撤冲击，方向: {retreatDir}");
    }

    /// <summary>
    /// 重置后撤冷却（当架势条降低或切换到其他行为时调用）
    /// </summary>
    public void ResetRetreat()
    {
        _retreatCooldown = 0f;
    }

    /// <summary>
    /// 重置攻击冷却（由 AttackState 在攻击完成时调用）
    /// </summary>
    public void ResetCooldown()
    {
        _attackCooldownTimer = 0f;
    }

    /// <summary>获取当前攻击是否在冷却中</summary>
    public bool IsOnCooldown => _attackCooldownTimer > 0f;
}
