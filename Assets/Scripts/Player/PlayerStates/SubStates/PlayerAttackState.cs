using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackState : PlayerAbilityState
{
    private Weapon weapon;
    public bool HasWeapon => weapon != null;

    private int xInput;

    private float velocityToSet;

    private bool setVelocity;
    private bool shouldCheckFlip;

    public PlayerAttackState(Player player, PlayerStateMachine stateMachine, PlayerData playerData, string animBoolName) : base(player, stateMachine, playerData, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        setVelocity = false;

        if (weapon == null)
        {
            isAbilityDone = true;
            return;
        }

        weapon.EnterWeapon();
        player.SetBodyVisible(!weapon.HasVisibleBase());
    }

    public override void Exit()
    {
        base.Exit();

        if (weapon != null)
        {
            weapon.ExitWeapon();
        }

        player.SetBodyVisible(!player.IsShieldHeld());
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (weapon != null)
        {
            player.SetBodyVisible(!weapon.HasVisibleBase());
        }

        xInput = player.InputHandler.NormInputX;

        if (shouldCheckFlip)
        {
            Movement?.CheckIfShouldFlip(xInput);

        }

        if (setVelocity)
        {
            Movement?.SetVelocityX(velocityToSet * Movement.FacingDirection);
        }
    }

    public void SetWeapon(Weapon weapon)
    {
        this.weapon = weapon;
        if (weapon != null)
        {
            weapon.InitializeWeapon(this, core);
        }
    }

    public void SetPlayerVelocity(float velocity)
    {
        Movement?.SetVelocityX(velocity * Movement.FacingDirection);

        velocityToSet = velocity;
        setVelocity = true;
    }

    public void SetFlipCheck(bool value)
    {
        shouldCheckFlip = value;
    }

    #region Animation Triggers

    public override void AnimationFinishTrigger()
    {
        base.AnimationFinishTrigger();

        isAbilityDone = true;
    }

    #endregion
}
