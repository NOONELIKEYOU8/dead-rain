using System;
using UnityEngine;

public delegate void BeforeDamageAppliedHandler(ref CombatContext ctx);

public static class BattleEvents
{
    public static event Action<CombatContext> OnAttackStarted;
    public static event Action<CombatContext> OnAttackResolved;

    public static event BeforeDamageAppliedHandler OnBeforeDamageApplied;
    public static event Action<CombatContext, HealthSnapshot> OnAfterDamageApplied;

    public static event Action<GameObject, GameObject, CombatContext> OnParrySuccess;
    public static event Action<GameObject, CombatContext> OnEnemyDied;

    public static event Action<string, int> OnItemPicked;

    public static void RaiseAttackStarted(CombatContext ctx)
    {
        OnAttackStarted?.Invoke(ctx);
    }

    public static void RaiseAttackResolved(CombatContext ctx)
    {
        OnAttackResolved?.Invoke(ctx);
    }

    public static void RaiseBeforeDamageApplied(ref CombatContext ctx)
    {
        OnBeforeDamageApplied?.Invoke(ref ctx);
    }

    public static void RaiseAfterDamageApplied(CombatContext ctx, HealthSnapshot snapshot)
    {
        OnAfterDamageApplied?.Invoke(ctx, snapshot);
    }

    public static void RaiseParrySuccess(GameObject defender, GameObject attacker, CombatContext ctx)
    {
        OnParrySuccess?.Invoke(defender, attacker, ctx);
    }

    public static void RaiseEnemyDied(GameObject enemy, CombatContext killingBlow)
    {
        OnEnemyDied?.Invoke(enemy, killingBlow);
    }

    public static void RaiseItemPicked(string itemId, int newStack)
    {
        OnItemPicked?.Invoke(itemId, newStack);
    }
}
