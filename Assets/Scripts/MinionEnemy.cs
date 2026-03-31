using UnityEngine;

/// <summary>
/// 小怪敌人：继承自 EnemyBase。
/// 体型较小（Scale 1x），血量和伤害低，速度较快。
/// 
/// 占位动画说明：
///   此脚本会在 Awake 时尝试从 Animator 读取动画状态。
///   使用配套的编辑器工具（菜单 工具 → 生成占位动画）可自动创建
///   placeholder AnimatorController（含 Idle / Walk / Attack / Hit 四个状态）。
/// </summary>
public class MinionEnemy : EnemyBase
{
    [Header("Minion Config")]
    [Tooltip("小怪基础移动速度，略快于Boss")]
    public float minionPatrolSpeed = 2.0f;

    protected override void Awake()
    {
        // 先让基类初始化（rb、anim、healthBar 等）
        base.Awake();

        // 应用小怪专属默认值（若 Inspector 未手动调整）
        patrolSpeed   = minionPatrolSpeed;
        maxHealth     = Mathf.Max(maxHealth, 1);   // 保留 Inspector 设定，否则默认 3
        contactDamage = Mathf.Max(contactDamage, 1);

        // 小怪体型：保持 1×1 比例（不修改 localScale，让 Prefab 决定）
        // 也可在此强制：transform.localScale = new Vector3(1f, 1f, 1f);
    }

    protected override void Start()
    {
        base.Start();
        // 如需小怪专属 Start 逻辑，在此添加
    }

    protected override void Update()
    {
        base.Update();
        // 如需小怪专属 Update 逻辑，在此添加
    }

    // ─── 受伤：可覆写以添加小怪专属效果 ────────────────────────────────
    public override void TakeDamage(int amount)
    {
        base.TakeDamage(amount);
        // 例如：播放受击音效 / 特效（留给后续扩展）
    }

    // ─── 死亡：可覆写以添加小怪掉落 / 爆炸特效等 ───────────────────────
    protected override void Die()
    {
        // 后续可在此添加掉落物、经验值、特效等
        base.Die();
    }
}
