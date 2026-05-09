using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    #region State Variables
    public PlayerStateMachine StateMachine { get; private set; }
    public PlayerIdleState IdleState { get; private set; }
    public PlayerMoveState MoveState { get; private set; }
    public PlayerJumpState JumpState { get; private set; }
    public PlayerInAirState InAirState { get; private set; }
    public PlayerLandState LandState { get; private set; }
    public PlayerWallSlideState WallSlideState { get; private set; }
    public PlayerWallGrabState WallGrabState { get; private set; }
    public PlayerWallClimbState WallClimbState { get; private set; }
    public PlayerWallJumpState WallJumpState { get; private set; }
    public PlayerLedgeClimbState LedgeClimbState { get; private set; }
    public PlayerDashState DashState { get; private set; }
    public PlayerCrouchIdleState CrouchIdleState { get; private set; }
    public PlayerCrouchMoveState CrouchMoveState { get; private set; }
    public PlayerAttackState PrimaryAttackState { get; private set; }
    public PlayerAttackState SecondaryAttackState { get; private set; }

    [SerializeField]
    private PlayerData playerData;
    #endregion

    #region Components
    public Core Core { get; private set; }
    public Animator Anim { get; private set; }
    public PlayerInputHandler InputHandler { get; private set; }
    public Rigidbody2D RB { get; private set; }
    public Transform DashDirectionIndicator { get; private set; }
    public BoxCollider2D MovementCollider { get; private set; }
    public PlayerInventory Inventory { get; private set; }

    #endregion

    #region Other Variables

    private Vector2 workspace;
    private int inventoryVersion = -1;
    private bool wasShieldHeld;
    private SpriteRenderer[] bodyRenderers;
    #endregion

    #region Unity Callback Functions
    private void Awake()
    {
        Core = GetComponentInChildren<Core>();

        StateMachine = new PlayerStateMachine();

        IdleState = new PlayerIdleState(this, StateMachine, playerData, "idle");
        MoveState = new PlayerMoveState(this, StateMachine, playerData, "move");
        JumpState = new PlayerJumpState(this, StateMachine, playerData, "inAir");
        InAirState = new PlayerInAirState(this, StateMachine, playerData, "inAir");
        LandState = new PlayerLandState(this, StateMachine, playerData, "land");
        WallSlideState = new PlayerWallSlideState(this, StateMachine, playerData, "wallSlide");
        WallGrabState = new PlayerWallGrabState(this, StateMachine, playerData, "wallGrab");
        WallClimbState = new PlayerWallClimbState(this, StateMachine, playerData, "wallClimb");
        WallJumpState = new PlayerWallJumpState(this, StateMachine, playerData, "inAir");
        LedgeClimbState = new PlayerLedgeClimbState(this, StateMachine, playerData, "ledgeClimbState");
        DashState = new PlayerDashState(this, StateMachine, playerData, "inAir");
        CrouchIdleState = new PlayerCrouchIdleState(this, StateMachine, playerData, "crouchIdle");
        CrouchMoveState = new PlayerCrouchMoveState(this, StateMachine, playerData, "crouchMove");
        PrimaryAttackState = new PlayerAttackState(this, StateMachine, playerData, "attack");
        SecondaryAttackState = new PlayerAttackState(this, StateMachine, playerData, "attack");
    }

    private void Start()
    {
        Anim = GetComponent<Animator>();
        InputHandler = GetComponent<PlayerInputHandler>();
        RB = GetComponent<Rigidbody2D>();
        DashDirectionIndicator = transform.Find("DashDirectionIndicator");
        MovementCollider = GetComponent<BoxCollider2D>();
        Inventory = GetComponent<PlayerInventory>();
        bodyRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        RefreshEquippedWeapons();

        StateMachine.Initialize(IdleState);

    }

    private void Update()
    {
        RefreshEquippedWeapons();
        UpdateShieldBlock();
        Core.LogicUpdate();
        StateMachine.CurrentState.LogicUpdate();
    }

    private void FixedUpdate()
    {
        StateMachine.CurrentState.PhysicsUpdate();
    }
    #endregion

    #region Other Functions

    public void SetColliderHeight(float height)
    {
        Vector2 center = MovementCollider.offset;
        workspace.Set(MovementCollider.size.x, height);

        center.y += (height - MovementCollider.size.y) / 2;

        MovementCollider.size = workspace;
        MovementCollider.offset = center;
    }



    private void AnimationTrigger() => StateMachine.CurrentState.AnimationTrigger();

    private void AnimationFinishTrigger() => StateMachine.CurrentState.AnimationFinishTrigger();

    private void RefreshEquippedWeapons()
    {
        if (Inventory == null || inventoryVersion == Inventory.Version)
        {
            return;
        }

        if (StateMachine != null && (StateMachine.CurrentState == PrimaryAttackState || StateMachine.CurrentState == SecondaryAttackState))
        {
            return;
        }

        PrimaryAttackState.SetWeapon(Inventory.GetWeapon(CombatInputs.primary));
        SecondaryAttackState.SetWeapon(Inventory.GetWeapon(CombatInputs.sencondary));
        inventoryVersion = Inventory.Version;
    }

    private void UpdateShieldBlock()
    {
        if (Inventory == null || Inventory.GetShield() == null)
        {
            return;
        }

        bool isShieldHeld = InputHandler != null && InputHandler.SecondaryAttackHeld;
        if (isShieldHeld == wasShieldHeld)
        {
            return;
        }

        Inventory.GetShield().SetBlocking(isShieldHeld);
        SetBodyVisible(!isShieldHeld && !IsInAttackState());
        wasShieldHeld = isShieldHeld;
    }

    public void SetBodyVisible(bool value)
    {
        if (bodyRenderers == null)
        {
            return;
        }

        foreach (SpriteRenderer spriteRenderer in bodyRenderers)
        {
            if (spriteRenderer == null || spriteRenderer.GetComponentInParent<Weapon>() != null)
            {
                continue;
            }

            spriteRenderer.enabled = value;
        }
    }

    public bool IsShieldHeld()
    {
        return wasShieldHeld;
    }

    public bool IsInAttackState()
    {
        return StateMachine != null && (StateMachine.CurrentState == PrimaryAttackState || StateMachine.CurrentState == SecondaryAttackState);
    }

    #endregion

}
