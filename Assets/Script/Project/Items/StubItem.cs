using UnityEngine;

/// <summary>
/// 占位道具（StubItem）
/// IUsable 接口的空实现，用于开发阶段占位。
/// 确保道具系统可运行不报错，未来替换为具体道具实现时无需修改敌人代码。
///
/// 使用方式：
/// 1. 将此脚本挂载到一个 GameObject 上
/// 2. 将该 GameObject 拖入敌人预制体的 ItemSlot 组件中
/// 3. 未来替换为具体道具（如 PotionItem）时，直接更换即可
/// </summary>
public class StubItem : MonoBehaviour, IUsable
{
    [Header("占位道具配置")]
    [Tooltip("道具名称")]
    public string itemName = "占位道具";

    [Tooltip("道具描述")]
    public string itemDescription = "这是一个占位道具，暂无实际效果。";

    public string ItemName => itemName;
    public string ItemDescription => itemDescription;

    /// <summary>
    /// 使用道具（空实现，仅打印日志）
    /// </summary>
    /// <param name="user">使用者</param>
    public void Use(GameObject user)
    {
        Debug.Log($"[StubItem] {user.name} 使用了占位道具 \"{itemName}\"（无实际效果）。");
    }

    /// <summary>
    /// 检查是否可以使用（始终返回 true）
    /// </summary>
    /// <param name="user">使用者</param>
    /// <returns>始终为 true</returns>
    public bool CanUse(GameObject user)
    {
        return true;
    }
}
