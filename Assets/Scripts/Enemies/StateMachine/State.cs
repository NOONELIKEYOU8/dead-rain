using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class State
{
    protected Movement Movement { get => movement ??= core.GetCoreComponent<Movement>(); }
    private Movement movement;

    protected CollisionSenses CollisionSenses { get => collisionSenses ??= core.GetCoreComponent<CollisionSenses>(); }
    private CollisionSenses collisionSenses;

    protected FiniteStateMachine stateMachine;
    protected Entity entity;
    protected Core core;

    public float startTime { get; protected set; }

    protected string animBoolName;

    public State(Entity entity, FiniteStateMachine stateMachine, string animBoolName)
    {
        this.entity = entity;
        this.stateMachine = stateMachine;
        this.animBoolName = animBoolName;
        core = entity.Core;
    }

    public virtual void Enter()
    {
        startTime = Time.time;
        entity.anim.SetBool(animBoolName, true);
        DoChecks();
    }

    public virtual void Exit()
    {
        entity.anim.SetBool(animBoolName, false);
    }

    public virtual void LogicUpdate()
    {

    }

    public virtual void PhysicsUpdate()
    {
        DoChecks();
    }

    public virtual void DoChecks()
    {

    }
}
