using UnityEngine;
using System;

public enum EraId
{
    Bronze = 0,
    QinHanThreeKingdoms = 1,
    TangSong = 2,
    MingQing = 3,
    ModernFounding = 4,
    Republic = 5,
    FoundingEnding = 6
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
    EraAdvanceGrowth,
    DefensePercent,
    MoveSpeedPercent,
    CritChancePercent,
    SkillCooldownPercent,
    BleedOnHit,
    FireBurstOnDamageTaken
}

public enum RuntimeEnemyRole
{
    Melee,
    Ranged,
    Charger,
    Shield,
    Summoner
}

public enum RoomType
{
    Start,
    Combat,
    Treasure,
    Shop,
    Rest,
    Challenge,
    Key,
    Locked,
    BossAnte,
    Boss,
    Exit,
    Secret
}

[Flags]
public enum RoomConnectionDirection
{
    None = 0,
    Left = 1 << 0,
    Right = 1 << 1,
    Up = 1 << 2,
    Down = 1 << 3
}

[Flags]
public enum ItemTriggerEvent
{
    None = 0,
    Passive = 1 << 0,
    OnHit = 1 << 1,
    OnKill = 1 << 2,
    OnDash = 1 << 3,
    OnDamageTaken = 1 << 4,
    OnEraChanged = 1 << 5
}

public enum BossAttackType
{
    Melee,
    Charge,
    Projectile,
    AreaWarning,
    Summon,
    PhaseShift
}

public enum BossTriggerCondition
{
    RoomEntered,
    KillCount,
    ElapsedTime,
    KeyCollected,
    Manual
}
