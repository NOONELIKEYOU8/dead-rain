using UnityEngine;

/// <summary>
/// 道具使用接口（IUsable）
/// 所有可使用的道具（药水、炸弹等）均需实现此接口。
/// 敌人通过 ItemSlot 组件调用此接口来使用道具。
/// </summary>
public interface IUsable
{
    /// <summary>
    /// 使用道具
    /// </summary>
    /// <param name="user">使用者的 GameObject</param>
    void Use(GameObject user);

    /// <summary>
    /// 检查道具是否可以使用
    /// </summary>
    /// <param name="user">使用者的 GameObject</param>
    /// <returns>true 表示可以使用</returns>
    bool CanUse(GameObject user);

    /// <summary>
    /// 获取道具显示名称
    /// </summary>
    string ItemName { get; }

    /// <summary>
    /// 获取道具描述
    /// </summary>
    string ItemDescription { get; }
}
