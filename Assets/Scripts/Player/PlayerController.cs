using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : Damageable
{
    public enum PlayerState
    {
        Idle,
        Run,
        Jump,
        Fall,
        Roll,
        Parry,
        Attack // 可留作扩展
    }

    [Header("States (Debug)")]
    public PlayerState currentState = PlayerState.Idle;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;

    [Header("Jump Assist")]
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.15f;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;

    [Header("Roll / Dodge")]
    public float rollSpeed = 10f;
    public float rollDuration = 0.3f;
    public float rollCooldown = 0.5f;
    private float rollCooldownTimer;

    [Header("Parry")]
    public float parryDuration = 0.2f;
    public float parryCooldown = 0.5f;
    private float parryCooldownTimer;

    [Header("Checks")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;

    [Header("Input Keys")]
    public KeyCode primaryAttackKey = KeyCode.J;
    public KeyCode secondaryAttackKey = KeyCode.K;
    public KeyCode skill1Key = KeyCode.L;
    public KeyCode skill2Key = KeyCode.I; // 示意的按键
    public KeyCode rollKey = KeyCode.LeftShift;

    // --- 给模块 C 开放的委托/事件 ---
    public event System.Action OnPrimaryAttackInput;
    public event System.Action OnSecondaryAttackInput;
    public event System.Action OnSkill1Input;
    public event System.Action OnSkill2Input;

    Rigidbody2D rb;
    private float hInput;
    private float facingDirection = 1f;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (groundCheck == null)
        {
            var go = new GameObject("GroundCheck");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0, -0.5f, 0);
            groundCheck = go.transform;
        }
        
        // 绑定招架成功时的反馈
        OnParrySuccess += HandleParrySuccess;
    }

    void Update()
    {
        UpdateTimers();
        HandleInputs();
        UpdateState();
    }

    void FixedUpdate()
    {
        if (currentState == PlayerState.Roll || currentState == PlayerState.Parry)
            return; // 暂不接受普通移动控制

        ApplyMovement();
    }

    void UpdateTimers()
    {
        // 冷却计时器
        if (rollCooldownTimer > 0) rollCooldownTimer -= Time.deltaTime;
        if (parryCooldownTimer > 0) parryCooldownTimer -= Time.deltaTime;

        // 土狼时间机制
        bool isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // 跳跃缓冲机制
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }

    void HandleInputs()
    {
        if (currentState == PlayerState.Roll || currentState == PlayerState.Parry)
            return; // 控制硬直

        hInput = Input.GetAxisRaw("Horizontal");
        if (hInput > 0.1f) facingDirection = 1f;
        else if (hInput < -0.1f) facingDirection = -1f;

        // 面向翻转
        if (facingDirection > 0) transform.localScale = new Vector3(1, 1, 1);
        else transform.localScale = new Vector3(-1, 1, 1);

        // 处理跳跃
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            jumpBufferCounter = 0f;
        }

        // 处理短跳机制 (提前松开跳跃键，减轻上升速度)
        if (Input.GetButtonUp("Jump") && rb.velocity.y > 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);
        }

        // 闪避输入
        if (Input.GetKeyDown(rollKey) && rollCooldownTimer <= 0f)
        {
            StartCoroutine(RollRoutine());
        }

        // --- 提取原本硬编码的攻击输入，向外抛出事件 ---
        if (Input.GetKeyDown(primaryAttackKey)) OnPrimaryAttackInput?.Invoke();
        if (Input.GetKeyDown(secondaryAttackKey))
        {
            // 防反示例：如果没有另外的盾牌系统接管，且可以直接按键防反，则此处演示
            // 稍后可以通过订阅 OnSecondaryAttackInput 来解耦招架逻辑
            OnSecondaryAttackInput?.Invoke();
            
            // 为了快速测试模块A的手感，暂时直接触发Parry机制
            if (parryCooldownTimer <= 0f)
            {
                StartCoroutine(ParryRoutine());
            }
        }
        if (Input.GetKeyDown(skill1Key)) OnSkill1Input?.Invoke();
        if (Input.GetKeyDown(skill2Key)) OnSkill2Input?.Invoke();
    }

    void ApplyMovement()
    {
        Vector2 vel = rb.velocity;
        vel.x = hInput * moveSpeed;
        rb.velocity = vel;
    }

    void UpdateState()
    {
        if (currentState == PlayerState.Roll || currentState == PlayerState.Parry) return;

        if (coyoteTimeCounter <= 0f) // Not grounded effectively
        {
            if (rb.velocity.y > 0) currentState = PlayerState.Jump;
            else currentState = PlayerState.Fall;
        }
        else
        {
            if (Mathf.Abs(rb.velocity.x) > 0.1f) currentState = PlayerState.Run;
            else currentState = PlayerState.Idle;
        }
    }

    // --- 翻滚逻辑 ---
    private System.Collections.IEnumerator RollRoutine()
    {
        currentState = PlayerState.Roll;
        invulnerable = true; // 复用基础类的无敌标识
        
        // 可选：将玩家层级改变甚至使其为Trigger穿透敌人
        
        rb.velocity = new Vector2(facingDirection * rollSpeed, rb.velocity.y);
        
        yield return new WaitForSeconds(rollDuration);

        // 翻滚结束
        currentState = PlayerState.Idle;
        invulnerable = false;
        rollCooldownTimer = rollCooldown;
    }

    // --- 招架/防反逻辑 ---
    private System.Collections.IEnumerator ParryRoutine()
    {
        currentState = PlayerState.Parry;
        isParrying = true;  // Damageable 中的属性
        
        rb.velocity = Vector2.zero; // 原地防反不动

        // 举盾过程/防反判定窗口
        yield return new WaitForSeconds(parryDuration);

        // 结束
        isParrying = false;
        currentState = PlayerState.Idle;
        parryCooldownTimer = parryCooldown;
    }

    // --- 当判定组件传来成功招架时的反馈 ---
    private void HandleParrySuccess(GameObject attacker, int damage)
    {
        Debug.Log("【盾反成功！】防御了来自 " + (attacker != null ? attacker.name : "未知来源") + " 的 " + damage + " 点伤害！");
        
        // 可以在这里触发反弹伤害或者击退 attacker，比如：
        // if (attacker != null) {
        //     var e = attacker.GetComponent<Damageable>();
        //     if (e) e.TakeDamage(damage * 2); // 招架反伤
        // }
    }

    protected override void Die()
    {
        base.Die();
        var gm = GameManager.Instance;
        if (gm != null) gm.OnPlayerDead();
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
