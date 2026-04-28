using UnityEngine;

/// <summary>
/// 伤害接口示例（已迁移为继承 Damageable 基类）
/// 演示如何通过 Damageable 统一伤害系统接收伤害
/// </summary>
public class DamageInterfaceExample : Damageable
{
    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Die()
    {
        Debug.Log($"{name} 死亡");
        base.Die();
    }
}
