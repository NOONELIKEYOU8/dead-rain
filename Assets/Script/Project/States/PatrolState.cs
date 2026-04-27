using UnityEngine;

/// <summary>
/// 巡逻状态（PatrolState）
/// 敌人在配置的路径点之间循环移动。
/// 到达路径点后原地等待一段时间，然后自动前进到下一个路径点。
/// 巡逻过程中持续检测玩家，发现后切换到追击状态。
///
/// 注意：等待逻辑完全在状态内部处理，不经过 EnemyBase 的巡逻等待系统，
/// 因为 FSM 不允许切换到与当前相同的状态（ChangeState 会直接 return）。
/// </summary>
public class PatrolState : IState
{
    private readonly EnemyBase _enemy;
    private int _currentPatrolIndex;
    private bool _hasInitialized;

    /// <summary>等待计时器</summary>
    private float _waitTimer;

    /// <summary>是否正在等待</summary>
    private bool _isWaiting;

    /// <summary>是否已离开上一个巡逻点（防止到达后立刻再次触发到达）</summary>
    private bool _hasLeftPoint;

    /// <summary>离场判定距离：离开巡逻点超过此距离才允许再次触发到达</summary>
    private const float LEAVE_DISTANCE = 0.8f;

    public PatrolState(EnemyBase enemy)
    {
        _enemy = enemy;
    }

    public void Enter()
    {
        Debug.Log($"[LOG] [{_enemy.name}] 进入 Patrol 状态，目标路径点: {_currentPatrolIndex}");
        _isWaiting = false;
        _hasLeftPoint = true; // 进入巡逻时默认允许到达判定

        // 如果没有配置巡逻点，直接进入空闲
        if (_enemy.PatrolPoints == null || _enemy.PatrolPoints.Length == 0)
        {
            _enemy.StateMachine.ForceChangeState(EnemyState.Idle);
            return;
        }

        // 首次进入时，找到最近的巡逻点
        if (!_hasInitialized)
        {
            _hasInitialized = true;
            float minDist = float.MaxValue;
            for (int i = 0; i < _enemy.PatrolPoints.Length; i++)
            {
                float dist = Vector2.Distance(_enemy.Position, _enemy.PatrolPoints[i]);
                if (dist < minDist)
                {
                    minDist = dist;
                    _currentPatrolIndex = i;
                }
            }
        }

       // _enemy.Animator.SetBool("IsMoving", true);
    }

    public void Update(float deltaTime)
    {
        // 检测玩家
        if (_enemy.DetectPlayer())
        {
            _enemy.StateMachine.ForceChangeState(EnemyState.Chase);
            return;
        }

        // 等待阶段：计时结束后前进到下一个路径点
        if (_isWaiting)
        {
            _waitTimer -= deltaTime;
            if (_waitTimer <= 0f)
            {
                _isWaiting = false;
                _hasLeftPoint = false; // 等待结束，要求先离开当前点

                // 前进到下一个巡逻点（循环）
                _currentPatrolIndex = (_currentPatrolIndex + 1) % _enemy.PatrolPoints.Length;

                // [ANIM_DISABLED] _enemy.Animator.SetBool("IsMoving", true);
            }
            return;
        }

        // 移动阶段
        Vector2 targetPoint = _enemy.PatrolPoints[_currentPatrolIndex];
        float distance = Vector2.Distance(_enemy.Position, targetPoint);

        // 检查是否已离开上一个巡逻点
        if (!_hasLeftPoint && distance > LEAVE_DISTANCE)
        {
            _hasLeftPoint = true;
        }

        if (distance < 0.5f && _hasLeftPoint)
        {
            // 到达巡逻点，进入等待
            _isWaiting = true;
            _waitTimer = _enemy.Data.patrolWaitTime;
            _enemy.SetVelocityX(0f);
            // [ANIM_DISABLED] _enemy.Animator.SetBool("IsMoving", false);
        }
        else
        {
            // 朝巡逻点移动
            _enemy.MoveToward(targetPoint, _enemy.Data.moveSpeed);
        }
    }

    public void FixedUpdate()
    {
        // 物理移动由 EnemyBase 的 Rigidbody 处理
    }

    public void Exit()
    {
        _isWaiting = false;
    }

    public EnemyState GetStateType() => EnemyState.Patrol;
}
