using UnityEngine;

/// <summary>
/// 道具槽组件（ItemSlot）
/// 挂载到敌人预制体上，用于持有和调用道具。
/// 支持单个道具槽位，未来可扩展为多槽位。
/// </summary>
public class ItemSlot : MonoBehaviour
{
    [Header("道具槽配置")]
    [Tooltip("当前装备的道具（实现 IUsable 接口的 MonoBehaviour）")]
    public MonoBehaviour item;

    /// <summary>
    /// 使用当前道具
    /// </summary>
    /// <returns>true 表示使用成功</returns>
    public bool UseItem()
    {
        if (item == null) return false;

        IUsable usable = item as IUsable;
        if (usable == null)
        {
            Debug.LogError($"[ItemSlot] {item.name} 未实现 IUsable 接口！");
            return false;
        }

        if (usable.CanUse(gameObject))
        {
            usable.Use(gameObject);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 检查当前道具是否可以使用
    /// </summary>
    public bool CanUseItem()
    {
        if (item == null) return false;

        IUsable usable = item as IUsable;
        return usable != null && usable.CanUse(gameObject);
    }

    /// <summary>
    /// 设置/更换道具
    /// </summary>
    /// <param name="newItem">新道具（MonoBehaviour）</param>
    public void SetItem(MonoBehaviour newItem)
    {
        item = newItem;
    }

    /// <summary>
    /// 清空道具槽
    /// </summary>
    public void ClearItem()
    {
        item = null;
    }

    /// <summary>获取当前道具名称</summary>
    public string GetItemName()
    {
        if (item == null) return "空";
        IUsable usable = item as IUsable;
        return usable?.ItemName ?? item.name;
    }
}
