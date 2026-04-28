/// <summary>
/// 状态接口（IState）
/// 所有敌人状态（Idle、Patrol、Chase 等）均实现此接口。
/// 状态机通过此接口统一管理状态的进入、更新和退出。
/// </summary>
public interface IState
{
    /// <summary>
    /// 进入状态时调用（仅调用一次）
    /// 用于初始化状态相关的逻辑。
    /// </summary>
    void Enter();

    /// <summary>
    /// 每帧更新时调用
    /// 用于执行状态内的持续逻辑（如移动、计时器等）。
    /// </summary>
    /// <param name="deltaTime">帧间隔时间</param>
    void Update(float deltaTime);

    /// <summary>
    /// 物理更新时调用（固定时间步长）
    /// 用于处理物理相关的逻辑（如移动、碰撞响应等）。
    /// </summary>
    void FixedUpdate();

    /// <summary>
    /// 退出状态时调用（仅调用一次）
    /// 用于清理状态相关的逻辑。
    /// </summary>
    void Exit();

    /// <summary>
    /// 获取当前状态的枚举标识
    /// 用于状态机查询和调试。
    /// </summary>
    EnemyState GetStateType();
}
