using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : Damageable
{
    public enum State { Idle, Run, Jump, Fall, Roll, Attack, Slam }

    [Header("Current State")]
    public State currentState = State.Idle;

    [Header("Movement Physics")]
    public float maxMoveSpeed = 8f;
    public float acceleration = 50f;
    public float deceleration = 50f;

    [Header("Jump Physics")]
    public float jumpForce = 15f;
    public float fallGravityMultiplier = 2.5f;
    public float maxFallSpeed = 25f;
    public float jumpCutMultiplier = 0.5f;

    [Header("Assists")]
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.15f;

    [Header("Dodge Roll (Utility Skill)")]
    public float rollSpeed = 16f;
    public float rollDuration = 0.3f;
    public float rollCooldown = 1f;

    [Header("Slam / Dive")]
    public float slamFallSpeed = 30f;
    public float slamRadius = 2.5f;
    public int slamDamage = 5;

    [Header("Attack (Primary Skill)")]
    public Transform attackPoint;
    public float attackRange = 0.8f;
    public LayerMask enemyLayers;
    public int primaryDamage = 1;
    public float primaryCooldown = 0.4f;
    public float attackDuration = 0.2f;

    [Header("Checks")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;

    [Header("Animation")]
    public Animator anim;

    // --- Inputs ---
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction jumpDownAction;
    private InputAction primaryAction;
    private InputAction utilityAction; // Roll

    // --- Internal State ---
    private Rigidbody2D rb;
    private float defaultGravityScale;
    
    private float moveInput;
    private bool isGrounded;
    private int facingDirection = 1;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    
    private float rollTimeCounter;
    private float rollCooldownCounter;
    
    private float primaryCooldownCounter;
    private float attackTimeCounter;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        defaultGravityScale = rb.gravityScale;

        SetupInputs();
        SetupChecks();
    }

    private void SetupInputs()
    {
        // Horizontal Movement
        moveAction = new InputAction("Move", binding: "<Gamepad>/leftStick/x");
        moveAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/a")
            .With("Negative", "<Keyboard>/leftArrow")
            .With("Positive", "<Keyboard>/d")
            .With("Positive", "<Keyboard>/rightArrow");

        // Jump
        jumpAction = new InputAction("Jump", binding: "<Keyboard>/space");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");

        // Down/Slam Input Check
        jumpDownAction = new InputAction("JumpDown", binding: "<Keyboard>/s");
        jumpDownAction.AddBinding("<Gamepad>/leftStick/down");
        jumpDownAction.AddBinding("<Keyboard>/downArrow");

        // Primary Attack
        primaryAction = new InputAction("Primary", binding: "<Keyboard>/j");
        primaryAction.AddBinding("<Gamepad>/buttonWest");
        primaryAction.AddBinding("<Mouse>/leftButton");

        // Utility (Roll)
        utilityAction = new InputAction("Utility", binding: "<Keyboard>/leftShift");
        utilityAction.AddBinding("<Gamepad>/rightTrigger");
        utilityAction.AddBinding("<Mouse>/rightButton");

        // Variable Jump Height (Release Jump)
        jumpAction.canceled += ctx => OnJumpCanceled();
    }

    private void SetupChecks()
    {
        if (groundCheck == null)
        {
            var go = new GameObject("GroundCheck");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0, -0.5f, 0);
            groundCheck = go.transform;
        }
        if (attackPoint == null)
        {
            var go = new GameObject("AttackPoint");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0.6f, 0f, 0);
            attackPoint = go.transform;
        }
    }

    void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        jumpDownAction.Enable();
        primaryAction.Enable();
        utilityAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        jumpDownAction.Disable();
        primaryAction.Disable();
        utilityAction.Disable();
    }

    void Update()
    {
        TimersUpdate();
        ReadInputs();

        // 状态转移动理
        switch (currentState)
        {
            case State.Idle:
            case State.Run:
            case State.Jump:
            case State.Fall:
                if (CheckUtilitySkill()) return;
                if (CheckPrimarySkill()) return;
                if (CheckSlam()) return;
                CheckJump();
                UpdateNormalStates();
                break;
            case State.Attack:
                if (attackTimeCounter <= 0)
                {
                    currentState = State.Idle;
                }
                break;
            case State.Roll:
                if (rollTimeCounter <= 0)
                {
                    EndRoll();
                }
                break;
            case State.Slam:
                if (isGrounded)
                {
                    ExecuteSlamImpact();
                }
                break;
        }
    }

    void FixedUpdate()
    {
        CheckGrounded();

        switch (currentState)
        {
            case State.Idle:
            case State.Run:
            case State.Jump:
            case State.Fall:
                HandleHorizontalMovement();
                ApplyGravityAndFallClamp();
                break;
            case State.Attack:
                // 攻击时减速滑行
                DecelerateRapidly();
                ApplyGravityAndFallClamp();
                break;
            case State.Roll:
                // 翻滚期间保持高速
                rb.velocity = new Vector2(facingDirection * rollSpeed, 0);
                rb.gravityScale = 0;
                break;
            case State.Slam:
                rb.velocity = new Vector2(0, -slamFallSpeed);
                break;
        }
    }

    // 更新各个技能的冷却和判定计时器
    private void TimersUpdate()
    {
        if (rollCooldownCounter > 0) rollCooldownCounter -= Time.deltaTime;
        if (primaryCooldownCounter > 0) primaryCooldownCounter -= Time.deltaTime;
        if (attackTimeCounter > 0) attackTimeCounter -= Time.deltaTime;
        if (rollTimeCounter > 0) rollTimeCounter -= Time.deltaTime;

        if (isGrounded)
            coyoteTimeCounter = coyoteTime;
        else
            coyoteTimeCounter -= Time.deltaTime;

        if (jumpBufferCounter > 0)
            jumpBufferCounter -= Time.deltaTime;

        // 如果在非无敌状态且不再翻滚中，确保invulnerable复位（继承自Damageable）
        if (currentState != State.Roll && invulnerable && rollTimeCounter <= 0)
        {
            // 注意这里不覆盖被击闪烁带来的无敌，所以最好用专属的翻滚标记。
            // 为了兼顾 Damageable 逻辑，暂时简单使用 invulnerable。
            invulnerable = false; 
        }
    }

    private void ReadInputs()
    {
        moveInput = moveAction.ReadValue<float>();
        if (jumpAction.WasPressedThisFrame())
        {
            jumpBufferCounter = jumpBufferTime;
        }
    }

    private void UpdateNormalStates()
    {
        if (isGrounded)
        {
            if (Mathf.Abs(moveInput) > 0.01f)
            {
                currentState = State.Run;
                if (anim != null) anim.Play("Run");
            }
            else
            {
                currentState = State.Idle;
                if (anim != null) anim.Play("Idle");
            }
        }
        else
        {
            if (rb.velocity.y > 0)
            {
                currentState = State.Jump;
                if (anim != null) anim.Play("Jump");
            }
            else
            {
                currentState = State.Fall;
                if (anim != null) anim.Play("Fall");
            }
        }

        // 翻转图片
        if (moveInput > 0.01f && facingDirection != 1) Flip();
        else if (moveInput < -0.01f && facingDirection != -1) Flip();
    }

    private void CheckGrounded()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    // 核心物理移动
    private void HandleHorizontalMovement()
    {
        float targetSpeed = moveInput * maxMoveSpeed;
        float speedDiff = targetSpeed - rb.velocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        float movement = Mathf.Pow(Mathf.Abs(speedDiff) * accelRate, 0.9f) * Mathf.Sign(speedDiff);
        
        rb.AddForce(movement * Vector2.right);
    }

    private void DecelerateRapidly()
    {
        float speedDiff = 0 - rb.velocity.x;
        float movement = Mathf.Pow(Mathf.Abs(speedDiff) * deceleration, 0.9f) * Mathf.Sign(speedDiff);
        rb.AddForce(movement * Vector2.right);
    }

    // 重力增强与最大下落速度 (死亡细胞手感)
    private void ApplyGravityAndFallClamp()
    {
        if (rb.velocity.y < 0)
            rb.gravityScale = defaultGravityScale * fallGravityMultiplier;
        else
            rb.gravityScale = defaultGravityScale;

        // Clamp Fall
        if (rb.velocity.y < -maxFallSpeed)
            rb.velocity = new Vector2(rb.velocity.x, -maxFallSpeed);
    }

    // 跳跃判定
    private void CheckJump()
    {
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
            currentState = State.Jump;
        }
    }

    // 变高跳削减
    private void OnJumpCanceled()
    {
        if (rb.velocity.y > 0 && currentState == State.Jump)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * jumpCutMultiplier);
            coyoteTimeCounter = 0f;
        }
    }

    // ------ 雨中冒险/死亡细胞 框架技能 ------

    // 【Utility Skill: Dodge Roll】
    private bool CheckUtilitySkill()
    {
        if (utilityAction.WasPressedThisFrame() && rollCooldownCounter <= 0)
        {
            // 发动翻滚
            currentState = State.Roll;
            rollTimeCounter = rollDuration;
            rollCooldownCounter = rollCooldown;
            invulnerable = true; // 无敌帧
            return true;
        }
        return false;
    }

    private void EndRoll()
    {
        rb.gravityScale = defaultGravityScale;
        invulnerable = false;
        currentState = State.Idle;
    }

    // 【Primary Skill: Attack】
    private bool CheckPrimarySkill()
    {
        if (primaryAction.WasPressedThisFrame() && primaryCooldownCounter <= 0)
        {
            currentState = State.Attack;
            primaryCooldownCounter = primaryCooldown;
            attackTimeCounter = attackDuration;
            
            if (anim != null) anim.Play("Attack");
            
            // 简单检测伤害
            Vector2 center = attackPoint != null ? (Vector2)attackPoint.position : (Vector2)transform.position;
            Collider2D[] hits = Physics2D.OverlapCircleAll(center, attackRange, enemyLayers);
            foreach(var hit in hits)
            {
                var d = hit.GetComponent<Damageable>();
                if (d != null && d != this) d.TakeDamage(primaryDamage);
            }
            return true;
        }
        return false;
    }

    // 【Slam Mechanism (下砸)】
    private bool CheckSlam()
    {
        // 必须在空中，按下+跳跃键触发
        if (!isGrounded && jumpDownAction.ReadValue<float>() > 0.5f && jumpAction.WasPressedThisFrame())
        {
            currentState = State.Slam;
            rb.velocity = new Vector2(0, -slamFallSpeed);
            jumpBufferCounter = 0; // 清除可能因为按了跳跃产生的缓冲
            return true;
        }
        return false;
    }

    private void ExecuteSlamImpact()
    {
        currentState = State.Idle;
        // 范围伤害
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, slamRadius, enemyLayers);
        foreach(var hit in hits)
        {
            var d = hit.GetComponent<Damageable>();
            if (d != null && d != this) d.TakeDamage(slamDamage);
        }
        // 可以播放震屏或粒子特效
        Debug.Log("Slam Impact!");
    }

    private void Flip()
    {
        facingDirection *= -1;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    protected override void Die()
    {
        base.Die();
        var gm = GameManager.Instance;
        if (gm != null) gm.OnPlayerDead();
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, slamRadius);
    }
}
