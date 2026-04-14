using UnityEngine;

/// <summary>
/// 道具基类 - 所有游戏道具的父类，提供基础属性和行为
/// </summary>
public abstract class Item : MonoBehaviour
{
    /// <summary>
    /// 道具的唯一标识符，用于数据库查询和保存
    /// </summary>
    [SerializeField]
    protected string itemId;
    
    /// <summary>
    /// 道具的显示名称，在UI中展示给玩家
    /// </summary>
    [SerializeField]
    protected string displayName;
    
    /// <summary>
    /// 道具的详细描述信息，支持多行文本编辑
    /// </summary>
    [SerializeField]
    [TextArea(1, 4)]
    protected string description;
    
    // /// <summary>
    // /// 道具的图标，用于背包和UI显示
    // /// </summary>
    // [SerializeField]
    // protected Sprite icon;
    
    // /// <summary>
    // /// 道具的最大堆叠数量，1表示不可堆叠
    // /// </summary>
    // [SerializeField]
    // protected int maxStack = 999;
    
    // /// <summary>
    // /// 道具的稀有度等级，0-1的浮点数，用于品质分类
    // /// </summary>
    // [SerializeField]
    // protected float rarity = 0.1f;

    // 公共访问属性 - 只读属性，防止外部直接修改
    public string ItemId => itemId;
    public string DisplayName => displayName;
    public string Description => description;
    // public Sprite Icon => icon;
    // public int MaxStack => maxStack;
    // public float Rarity => rarity;



    /// <summary>
    /// 当玩家使用道具时调用的虚方法
    /// 子类可以重写此方法以实现特定的使用效果
    /// </summary>
    /// <param name="player">使用道具的玩家对象</param>
    public virtual void OnUse(GameObject player)
    {
        Debug.Log($"使用道具: {displayName}");
    }

    // // / <summary>
    // // / 当玩家丢弃道具时调用的虚方法
    // // / 子类可以重写此方法以实现特定的丢弃逻辑
    // // / </summary>
    // public virtual void OnDrop()
    // {
    //     Debug.Log($"丢弃道具: {displayName}");
    // }

}