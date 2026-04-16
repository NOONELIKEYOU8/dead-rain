using UnityEngine;

public interface IDifficultyProvider
{
    DifficultySnapshot GetSnapshot();
    float GetThreatLevel();
}

public interface IItemEffectService
{
    void ApplyItem(string itemId, int stackDelta);
    void RemoveItem(string itemId, int stackDelta);
    void EvaluateDamageModifiers(ref CombatContext ctx);
}

public interface IDropService
{
    string RollDrop(string enemyTypeId, float threatLevel);
    void SpawnDrop(string dropId, Vector3 worldPos);
}

public interface IEnemyTypeProvider
{
    string GetEnemyTypeId();
}
