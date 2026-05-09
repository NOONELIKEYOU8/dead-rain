using UnityEngine;

public enum EraId
{
    Bronze,
    QinHanThreeKingdoms,
    TangSong,
    MingQing,
    ModernFounding
}

public enum ContentTier
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Boss
}

public enum EnemyAttackPattern
{
    PatrolMelee,
    RangedCaster,
    ChargeBruiser,
    ShieldBlock,
    Summoner,
    BossPhase
}

public enum ItemEffectType
{
    MeleeDamagePercent,
    DashCooldownPercent,
    HealOnKill,
    BonusProjectileChance,
    EraAdvanceGrowth
}

public enum RuntimeEnemyRole
{
    Melee,
    Ranged,
    Charger,
    Shield,
    Summoner
}
