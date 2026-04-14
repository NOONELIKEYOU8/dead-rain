using UnityEngine;
using System.Collections;

/// <summary>
/// 爆炸武器类 - 继承自Weapon，提供爆炸范围伤害的武器实现
/// </summary>
public class ExplosionWeapon : Weapon
{
    /// <summary>
    /// 爆炸的范围（米），在此范围内的敌人都会受到伤害
    /// </summary>
    [SerializeField]
    protected float explosionRadius = 5f;

    /// <summary>
    /// 爆炸延迟时间（秒），扔出后需要等待多久才爆炸
    /// </summary>
    [SerializeField]
    protected float explosionDelay = 2f;

    /// <summary>
    /// 爆炸造成的伤害加成倍率，最终伤害 = damage * explosionDamageMultiplier
    /// </summary>
    [SerializeField]
    protected float explosionDamageMultiplier = 1.5f;


    /// <summary>
    /// 爆炸视觉效果的预制体
    /// </summary>
    [SerializeField]
    protected GameObject explosionEffectPrefab;

    /// <summary>
    /// 投掷力度
    /// </summary>
    [SerializeField]
    protected float throwForce = 10f;


    // 公共访问属性
    public float ExplosionRadius => explosionRadius;
    public float ExplosionDelay => explosionDelay;
    public int ExplosionDamage => Mathf.RoundToInt(damage * explosionDamageMultiplier);


    /// <summary>
    /// 投掷爆炸武器的方法
    /// </summary>
    /// <param name="direction">投掷方向</param>
    public virtual void ThrowBomb(Vector2 direction)
    {
        // 这里可以实现投掷逻辑，例如创建一个炸弹对象并应用力
        Debug.Log($"投掷爆炸武器！方向: {direction}");
    }   

    public virtual void Explode(Vector3 explosionPosition)
    {
        // 这里可以实现爆炸逻辑，例如检测范围内的敌人并造成伤害，播放爆炸效果等
        Debug.Log($"爆炸发生！位置: {explosionPosition}，范围: {explosionRadius} 米，伤害: {ExplosionDamage}");
    }



    /// <summary>
    /// 执行攻击的抽象方法实现
    /// </summary>
    /// <param name="attacker">发起攻击的玩家对象</param>
    /// <returns>攻击是否成功</returns>
    protected override bool Attack(GameObject attacker)
    {
        // 对于爆炸武器，攻击就是投掷炸弹
        Vector2 direction = attacker.transform.right; // 假设朝向右边
        ThrowBomb(direction);
        return true;
    }
}
