/// <summary>
/// 可被处决接口（IExecutable）
/// 实现此接口的对象可以被 ExecutionSystem 判定为"可被处决"。
/// 敌人失衡状态时实现此接口。
/// </summary>
public interface IExecutable
{
    /// <summary>
    /// 检查当前是否处于可被处决状态
    /// </summary>
    /// <returns>true 表示可被处决</returns>
    bool CanBeExecuted();

    /// <summary>
    /// 执行处决
    /// 由 ExecutionSystem 调用，触发处决逻辑。
    /// </summary>
    void Execute();
}
