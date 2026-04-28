using UnityEngine;
using System.Linq;

/// <summary>
/// 处决系统（ExecutionSystem）
/// 全局单例，负责检测玩家对失衡敌人的处决输入。
/// 当玩家处于"可处决"范围内且敌人处于"可被处决"状态时触发处决。
/// 处决动画播放期间，敌人和玩家均无敌。
///
/// 使用方式：
/// 1. 在场景中创建一个空 GameObject，命名为 "ExecutionSystem"
/// 2. 挂载此脚本
/// 3. player 字段会自动查找 Tag 为 "Player" 的对象（也可手动拖拽指定）
/// </summary>
public class ExecutionSystem : MonoBehaviour
{
    /// <summary>全局单例</summary>
    public static ExecutionSystem Instance { get; private set; }

    [Header("处决系统配置")]
    [Tooltip("默认处决检测范围（当目标敌人未配置 executionRange 时使用）")]
    public float defaultExecutionRange = 1.5f;

    [Tooltip("玩家引用（用于检测处决输入和设置无敌），留空则自动查找 Tag=Player 的对象")]
    public GameObject player;

    [Tooltip("处决动画持续时间（秒）")]
    public float executionDuration = 1.5f;

    [Tooltip("玩家 Tag（用于自动查找玩家）")]
    public string playerTag = "Player";

    /// <summary>当前是否正在执行处决动画</summary>
    private bool _isExecuting = false;

    /// <summary>处决动画计时器</summary>
    private float _executionTimer;

    /// <summary>当前被处决的敌人</summary>
    private GameObject _currentTarget;

    /// <summary>玩家身上的 Damageable 组件缓存（用于设置无敌）</summary>
    private Damageable _playerDamageable;

    /// <summary>玩家控制器引用（用于订阅处决输入事件）</summary>
    private PlayerController _playerController;

    private void Awake()
    {
        // 单例模式
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[ExecutionSystem] 已存在实例，销毁重复对象。");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log("[LOG] [ExecutionSystem] 初始化完成。");
    }

    private void Start()
    {
        // 自动查找玩家
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                Debug.Log($"[LOG] [ExecutionSystem] 自动找到玩家: {player.name}");
            }
            else
            {
                Debug.LogWarning($"[ExecutionSystem] 未找到 Tag='{playerTag}' 的玩家对象，处决系统将无法工作。请在 Inspector 中手动指定 player 字段。");
            }
        }

        // 缓存玩家组件
        if (player != null)
        {
            _playerDamageable = player.GetComponent<Damageable>();
            _playerController = player.GetComponent<PlayerController>();

            if (_playerDamageable == null)
            {
                Debug.LogWarning("[ExecutionSystem] 玩家对象上没有 Damageable 组件，处决期间无法设置无敌。");
            }
            if (_playerController == null)
            {
                Debug.LogWarning("[ExecutionSystem] 玩家对象上没有 PlayerController 组件，无法订阅处决输入事件。");
            }
            else
            {
                _playerController.OnExecuteInput += TryExecute;
                Debug.Log("[LOG] [ExecutionSystem] 已订阅 PlayerController.OnExecuteInput 事件。");
            }
        }
    }

    private void OnDestroy()
    {
        // 取消订阅，防止内存泄漏
        if (_playerController != null)
        {
            _playerController.OnExecuteInput -= TryExecute;
        }
    }

    private void Update()
    {
        if (_isExecuting)
        {
            UpdateExecution();
        }
    }

    /// <summary>
    /// 尝试对附近的失衡敌人执行处决
    /// </summary>
    private void TryExecute()
    {
        if (player == null)
        {
            Debug.LogWarning("[ExecutionSystem] 未设置玩家引用，无法执行处决。");
            return;
        }

        // 查找所有可处决的对象
        IExecutable[] executables = FindObjectsOfType<MonoBehaviour>(true)
            .OfType<IExecutable>()
            .Where(e => e.CanBeExecuted())
            .ToArray();

        if (executables.Length == 0)
        {
            Debug.Log("[LOG] [ExecutionSystem] 附近没有可处决的目标。");
            return;
        }

        Debug.Log($"[LOG] [ExecutionSystem] 发现 {executables.Length} 个可处决目标，正在筛选最近目标...");

        IExecutable nearestExecutable = null;
        float nearestDistance = float.MaxValue;

        foreach (var executable in executables)
        {
            GameObject target = (executable as MonoBehaviour)?.gameObject;
            if (target == null) continue;

            float distance = Vector2.Distance(player.transform.position, target.transform.position);
            // 优先使用目标敌人的 executionRange 配置，否则使用默认值
            float range = GetExecutionRange(target);
            Debug.Log($"[LOG] [ExecutionSystem]   - 候选: {target.name}, 距离: {distance:F2}, 范围: {range:F2}");

            if (distance <= range && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestExecutable = executable;
            }
        }

        if (nearestExecutable != null)
        {
            GameObject targetObj = (nearestExecutable as MonoBehaviour)?.gameObject;
            Debug.Log($"[LOG] [ExecutionSystem] >>> 处决 {targetObj.name}！距离: {nearestDistance:F2}");
            nearestExecutable.Execute();
        }
        else
        {
            Debug.Log("[LOG] [ExecutionSystem] 可处决目标均不在范围内。");
        }
    }

    /// <summary>
    /// 处决开始时调用（由 ExecutedState 调用）
    /// 设置双方无敌状态
    /// </summary>
    /// <param name="target">被处决的敌人</param>
    public void OnExecutionStarted(GameObject target)
    {
        Debug.Log($"[LOG] [ExecutionSystem] 处决开始！目标: {target.name}，持续: {executionDuration}秒");
        _isExecuting = true;
        _currentTarget = target;
        _executionTimer = executionDuration;

        // 设置玩家无敌（处决期间）
        if (_playerDamageable != null)
        {
            _playerDamageable.invulnerable = true;
            Debug.Log("[LOG] [ExecutionSystem] 玩家已设为无敌（处决期间）。");
        }
    }

    /// <summary>
    /// 更新处决动画
    /// </summary>
    private void UpdateExecution()
    {
        _executionTimer -= Time.deltaTime;

        // 处决期间锁定玩家位置
        if (player != null)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.velocity = Vector2.zero;
        }

        if (_executionTimer <= 0f)
        {
            EndExecution();
        }
    }

    /// <summary>
    /// 处决结束，恢复正常状态
    /// </summary>
    private void EndExecution()
    {
        Debug.Log("[LOG] [ExecutionSystem] 处决结束，恢复正常状态。");
        _isExecuting = false;
        _currentTarget = null;

        // 恢复玩家无敌状态
        if (_playerDamageable != null)
        {
            _playerDamageable.invulnerable = false;
            Debug.Log("[LOG] [ExecutionSystem] 玩家无敌已解除。");
        }
    }

    /// <summary>
    /// 获取当前是否有可处决目标（供 UI 提示使用）
    /// </summary>
    public bool HasExecutableTarget()
    {
        if (player == null) return false;

        return FindObjectsOfType<MonoBehaviour>(true)
            .OfType<IExecutable>()
            .Any(e =>
            {
                var go = (e as MonoBehaviour)?.gameObject;
                return go != null && e.CanBeExecuted() &&
                       Vector2.Distance(player.transform.position, go.transform.position) <= GetExecutionRange(go);
            });
    }

    /// <summary>
    /// 获取目标敌人的处决范围（优先使用敌人自身配置，否则使用默认值）
    /// </summary>
    private float GetExecutionRange(GameObject target)
    {
        var enemyBase = target.GetComponent<EnemyBase>();
        if (enemyBase != null && enemyBase.Data != null)
        {
            return enemyBase.Data.executionRange;
        }
        return defaultExecutionRange;
    }
}
