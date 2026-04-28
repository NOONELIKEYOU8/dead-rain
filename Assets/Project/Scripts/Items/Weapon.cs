using UnityEngine;

/// <summary>
/// 武器基类 - 所有武器的父类，提供攻击相关的基础属性和行为
/// </summary>
public abstract class Weapon : Item
{
    /// <summary>
    /// 武器造成的伤害值
    /// </summary>
    [SerializeField]
    protected int damage = 10;

    /// <summary>
    /// 武器的攻击范围，单位为米
    /// </summary>
    [SerializeField]
    protected float attackRange = 1.5f;


    // 公共访问属性
    public int Damage => damage;
    public float AttackRange => attackRange;

    /// <summary>
    /// 当玩家使用武器时调用的虚方法
    /// 子类必须实现此方法以实现具体的攻击逻辑
    /// </summary>
    /// <param name="player">使用武器的玩家对象</param>
    public override void OnUse(GameObject player)
    {
        base.OnUse(player);
    }

    /// <summary>
    /// 执行攻击的抽象方法，子类必须实现具体的攻击逻辑
    /// </summary>
    /// <param name="attacker">发起攻击的玩家对象</param>
    /// <returns>攻击是否成功</returns>
    public abstract bool Attack(GameObject attacker);




    /// <summary>
    /// 在编辑器中可视化武器的攻击范围
    /// </summary>
    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
<<<<<<< HEAD

/// <summary>
/// 可伤害接口 - 实现此接口的对象可以受到伤害
/// </summary>
public interface IDamageable
{
    void TakeDamage(int damage);
}
=======
>>>>>>> dead-rain/qgh
