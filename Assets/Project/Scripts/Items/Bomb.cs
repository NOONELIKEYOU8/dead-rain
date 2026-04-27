using UnityEngine;

/// <summary>
/// 炸弹类
/// 继承自 Weapon，负责实现：
/// 1. 闲置状态（Idle）
/// 2. 被投掷后的飞行动画（Throw）
/// 3. 爆炸动画（Explosion）
/// 4. 爆炸伤害判定
/// 5. 爆炸结束后的销毁
/// 
/// 当前逻辑流程：
/// 默认状态：Idle
/// 调用 ThrowBomb() 后：
///     -> 进入 Throw 动画
///     -> Throw 动画播完（由动画事件通知）
///     -> 触发爆炸逻辑，造成范围伤害
///     -> 播放 Explosion 动画
///     -> Explosion 动画播完（由动画事件通知）
///     -> 销毁炸弹对象
/// </summary>
public class Bomb : Weapon
{
    /// <summary>
    /// 爆炸的范围（米），在此范围内的敌人都会受到伤害
    /// </summary>
    [SerializeField]
    protected float explosionRadius = 2f;

    /// <summary>
    /// 投掷力度
    /// </summary>
    [SerializeField]
    protected float throwForce = 10f;

    /// <summary>
    /// 炸弹的 2D 刚体组件
    /// 用于控制飞行速度、重力、旋转等物理行为
    /// </summary>
    protected Rigidbody2D bombRigidbody;

    /// <summary>
    /// 炸弹的碰撞体
    /// 爆炸后通常会关闭，避免继续参与碰撞
    /// </summary>
    protected Collider2D bombCollider;

    /// <summary>
    /// Animator 组件
    /// 用于控制 Idle / Throw / Explosion 三段动画切换
    /// </summary>
    protected Animator animator;

    /// <summary>
    /// 炸弹的初始位置
    /// 目前主要用于记录投掷起点，便于调试
    /// </summary>
    protected Vector3 initialPosition;

    /// <summary>
    /// 炸弹的投掷方向
    /// 在 ThrowBomb 时记录
    /// </summary>
    protected Vector2 throwDirection;

    /// <summary>
    /// Animator 中“是否已被投掷”的参数名
    /// 建议你 Animator 里真的就创建一个 bool 参数叫 isThrow
    /// </summary>
    [Header("Animator 参数名")]
     [SerializeField] private string isFlyingParam = "isFlying";

    /// <summary>
    /// Animator 中“是否进入爆炸状态”的参数名
    /// 建议你 Animator 里创建一个 bool 参数叫 isExplode
    /// </summary>
    [SerializeField] private string explodeTriggerParam = "Explode";

    /// <summary>
    /// 标记炸弹是否已经被投掷
    /// 用来防止同一个炸弹重复执行 ThrowBomb
    /// </summary>
    private bool hasBeenThrown = false;

    /// <summary>
    /// 标记炸弹是否已经爆炸
    /// 用来防止爆炸逻辑被重复触发
    /// </summary>
    private bool hasExploded = false;

    public int ExplosionDamage => damage;

    /// <summary>
    /// 初始化组件
    /// 在对象创建时自动执行一次
    /// </summary>
    protected virtual void Awake()
    {
        // 获取现有组件
        bombRigidbody = GetComponent<Rigidbody2D>();
        bombCollider = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();


        // 设置基础物理参数
        // 这些参数决定炸弹飞行时的手感
        bombRigidbody.gravityScale = 1f;     // 正常重力
        bombRigidbody.drag = 0.1f;           // 线性阻力，越大飞得越慢
        bombRigidbody.angularDrag = 0.1f;    // 角阻力，影响旋转减速

        // 初始化 Animator 参数
        // 确保对象一开始处于 Idle 逻辑
        if (animator != null)
        {
            animator.SetBool(isFlyingParam, false);
            animator.ResetTrigger(explodeTriggerParam);
        }
    }

    /// <summary>
    /// 投掷炸弹
    /// 这是炸弹被真正“使用”的核心入口
    /// 
    /// 执行内容：
    /// 1. 记录当前位置与方向
    /// 2. 给予炸弹飞行速度
    /// 3. 给予随机旋转速度
    /// 4. 切换到 Throw 动画
    /// 
    /// 注意：
    /// 这里只负责“进入投掷状态”
    /// 不在这里直接爆炸
    /// 爆炸时机由 Throw 动画最后一帧的 Animation Event 决定
    /// </summary>
    /// <param name="direction">投掷方向</param>
    public void ThrowBomb(Vector2 direction)
    {
        // 防止重复投掷
        if (hasBeenThrown)
        {
            return;
        }

        if (bombRigidbody != null)
        {
            bombRigidbody.simulated = true;
        }

        if (bombCollider != null)
        {
            bombCollider.enabled = true;
        }
        
        hasBeenThrown = true;

        // 记录起始位置
        initialPosition = transform.position;

        // 归一化方向，防止不同长度的 direction 导致速度忽大忽小
        throwDirection = direction.normalized;

        Debug.Log($"投掷炸弹！位置: {initialPosition}，方向: {direction}");

        // 给炸弹一个初始飞行速度
        bombRigidbody.velocity = throwDirection * throwForce;

        // 让炸弹在飞行过程中有一点随机旋转感，更像“被扔出去”
        bombRigidbody.angularVelocity = Random.Range(-180f, 180f);

        // 通知 Animator：炸弹已经被投掷
        // 状态机应该从 Idle 切换到 Throw
        if (animator != null)
        {
            animator.SetBool(isFlyingParam, true);
        }
    }

    /// <summary>
    /// 切换炸弹是否处于手持状态。
    /// 手持状态下禁用物理与碰撞，让炸弹稳定跟随挂点。
    /// </summary>
    public void SetHeldState(bool isHeld, Transform holder)
    {
        if (isHeld)
        {
            if (holder != null)
            {
                transform.SetParent(holder);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }

            if (bombRigidbody != null)
            {
                bombRigidbody.velocity = Vector2.zero;
                bombRigidbody.angularVelocity = 0f;
                bombRigidbody.simulated = false;
            }

            if (bombCollider != null)
            {
                bombCollider.enabled = false;
            }

            if (animator != null)
            {
                animator.SetBool(isFlyingParam, false);
                animator.ResetTrigger(explodeTriggerParam);
            }

            return;
        }

        transform.SetParent(null);

        if (bombRigidbody != null)
        {
            bombRigidbody.simulated = true;
        }

        if (bombCollider != null)
        {
            bombCollider.enabled = true;
        }
    }

    /// <summary>
    /// 爆炸逻辑
    /// 
    /// 这个函数不应该由外部随便乱调，
    /// 正常情况下应该由 Throw 动画最后一帧的动画事件触发。
    /// 
    /// 执行内容：
    /// 1. 防止重复爆炸
    /// 2. 停止物理运动
    /// 3. 关闭碰撞
    /// 4. 造成范围伤害
    /// 5. 播放独立爆炸特效（如果配置了）
    /// 6. 通知 Animator 播放 Explosion 动画
    /// </summary>
    /// <param name="explosionPosition">爆炸发生的位置</param>
    public void Explode(Vector3 explosionPosition)
    {
        // 防止同一颗炸弹重复爆炸
        if (hasExploded)
        {
            return;
        }

        hasExploded = true;

        Debug.Log($"炸弹爆炸！位置: {explosionPosition}，范围: {explosionRadius}m");

        // 让炸弹停止一切物理运动
        // 因为接下来要进入爆炸状态，不再继续飞行
        if (bombRigidbody != null)
        {
            bombRigidbody.velocity = Vector2.zero;
            bombRigidbody.angularVelocity = 0f;
            bombRigidbody.simulated = false;
        }

        // 关闭碰撞，避免爆炸状态下继续撞来撞去
        if (bombCollider != null)
        {
            bombCollider.enabled = false;
        }

        // 立刻造成范围伤害
        // 这符合你说的：“Throw 动画播放完之后就立刻造成伤害”
        DealExplosionDamage(explosionPosition);

        // 通知 Animator 进入 Explosion 动画状态
        if (animator != null)
        {
            Debug.Log($"here, trigger explosion animation");
            animator.SetTrigger(explodeTriggerParam);
        }
    }

    /// <summary>
    /// 处理爆炸范围伤害
    /// 优先检测 Hurtbox（新敌人系统），其次兼容 Damageable（旧系统）
    /// </summary>
    /// <param name="explosionPosition">爆炸中心点</param>
    private void DealExplosionDamage(Vector3 explosionPosition)
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(explosionPosition, explosionRadius);

        foreach (Collider2D hit in hitColliders)
        {
            if (hit.gameObject == gameObject) continue;

            // 优先检测 Hurtbox（新敌人系统）
            var hurtbox = hit.GetComponent<Hurtbox>();
            if (hurtbox != null && hurtbox.owner != null && hurtbox.owner != gameObject)
            {
                Vector2 knockbackDir = ((Vector2)hit.transform.position - (Vector2)explosionPosition).normalized;
                DamageInfo dmgInfo = new DamageInfo(ExplosionDamage, knockbackDir, 10f, gameObject, true);
                hurtbox.OnHit(dmgInfo);
                continue;
            }

            // 兼容旧系统：检测 Damageable 组件
            var damageable = hit.GetComponentInParent<Damageable>();
            if (damageable != null && damageable.gameObject != gameObject)
            {
                damageable.TakeDamage(ExplosionDamage, gameObject);
            }
        }
    }

    /// <summary>
    /// 这个函数给 Throw 动画最后一帧的 Animation Event 调用
    /// 
    /// 使用方式：
    /// 你要在 Throw 动画的最后一帧加一个动画事件，
    /// 函数名填：OnThrowAnimationFinished
    /// 
    /// 作用：
    /// Throw 动画播完后，立刻触发爆炸
    /// </summary>
    public void OnThrowAnimationFinished()
    {
        // 如果已经爆炸，就不重复执行
        if (hasExploded)
        {
            return;
        }
        Debug.Log($"Throw 动画结束，触发爆炸逻辑");
        Explode(transform.position);
    }

    /// <summary>
    /// 这个函数给 Explosion 动画最后一帧的 Animation Event 调用
    /// 
    /// 使用方式：
    /// 你要在 Explosion 动画最后一帧加一个动画事件，
    /// 函数名填：OnExplosionAnimationFinished
    /// 
    /// 作用：
    /// 爆炸动画播完后，销毁炸弹对象
    /// </summary>
    public void OnExplosionAnimationFinished()
    {
        Destroy(gameObject);
    }

    /// <summary>
    /// 执行攻击：根据玩家面向执行一次投掷
    /// </summary>
    public override bool Attack(GameObject attacker)
    {
        // 使用 lossyScale 兼容角色翻转发生在父节点时的朝向读取。
        float facing = attacker.transform.lossyScale.x >= 0f ? 1f : -1f;
        Vector2 direction = new Vector2(facing, 0.5f).normalized;
        ThrowBomb(direction);
        return true;
    }

    /// <summary>
    /// 在 Scene 视图里绘制爆炸范围，方便调试
    /// 选中炸弹对象时，可以看到一个表示爆炸半径的圆
    /// </summary>
    protected override void OnDrawGizmosSelected()
    {
        // base.OnDrawGizmosSelected();

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}