using System.Collections;
using UnityEngine;

/// <summary>
/// EnemyEffectSystem — Centralized runtime particle effect system for enemies.
/// 
/// Responsibilities:
///   - Play hit (on-hit), attack (on-attack), and death (on-death) particle bursts.
///   - All particle systems are created procedurally at runtime via code; NO external
///     asset files are required. Replace with real SpriteSheet textures later if desired.
/// 
/// Usage:
///   Place this component on the same GameObject as MinionEnemy / BossEnemy,
///   OR let EnemyBase.Awake() add it automatically via RequireComponent / AddComponent.
/// 
/// Particle visual overview:
///   Hit     — small orange sparks bursting outward (6 particles, 0.3 s lifetime)
///   Attack  — white/yellow flash arc in front of the enemy (8 particles, 0.25 s)
///   Death   — large multi-colored explosion with debris scatter (20 particles, 0.6 s)
///   Block   — grey sparks ricocheting at steep angles (5 particles, 0.2 s)
/// </summary>
public class EnemyEffectSystem : MonoBehaviour
{
    // ─── Inspector tunables ─────────────────────────────────────────────────
    [Header("Hit Effect")]
    [Tooltip("Color of the spark particles spawned on hit")]
    public Color hitSparkColor = new Color(1f, 0.55f, 0f, 1f);   // orange

    [Header("Attack Effect")]
    [Tooltip("Color of the slash particles spawned on attack")]
    public Color attackSlashColor = new Color(1f, 0.95f, 0.4f, 1f); // yellow-white

    [Header("Death Effect")]
    [Tooltip("Primary color of the death explosion")]
    public Color deathColorA = new Color(1f, 0.2f, 0.1f, 1f);    // red
    [Tooltip("Secondary color of the death explosion debris")]
    public Color deathColorB = new Color(1f, 0.7f, 0f, 1f);       // orange-gold

    [Header("Block Effect")]
    [Tooltip("Color of the guard/block spark particles")]
    public Color blockSparkColor = new Color(0.8f, 0.8f, 0.8f, 1f); // grey

    // ─── Cached particle systems (lazily created) ────────────────────────
    private ParticleSystem _hitPS;
    private ParticleSystem _attackPS;
    private ParticleSystem _deathPS;
    private ParticleSystem _blockPS;

    // ─── Unity lifecycle ─────────────────────────────────────────────────
    private void Awake()
    {
        BuildAllSystems();
    }

    // ─── Public trigger API ──────────────────────────────────────────────

    /// <summary>Play the on-hit spark burst at a world position.</summary>
    public void PlayHit(Vector3 worldPos)
    {
        PlayBurst(_hitPS, worldPos);
    }

    /// <summary>Play the attack slash effect in the facing direction.</summary>
    /// <param name="facingRight">True if the enemy is facing right.</param>
    public void PlayAttack(Vector3 worldPos, bool facingRight)
    {
        if (_attackPS == null) return;
        // Rotate attack particles toward the attack direction
        float angle = facingRight ? 0f : 180f;
        _attackPS.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        PlayBurst(_attackPS, worldPos);
    }

    /// <summary>Play the death explosion. Detaches from parent so it outlives the enemy.</summary>
    public void PlayDeath(Vector3 worldPos)
    {
        if (_deathPS == null) return;
        // Detach so the effect finishes even after the enemy GameObject is destroyed
        _deathPS.transform.SetParent(null);
        _deathPS.transform.position = worldPos;
        _deathPS.Play();
        // Auto-destroy the detached particle system after its duration
        Destroy(_deathPS.gameObject, _deathPS.main.duration + _deathPS.main.startLifetime.constantMax + 0.5f);
        _deathPS = null; // nullify so we don't reuse the detached instance
    }

    /// <summary>Play the guard/block spark effect.</summary>
    public void PlayBlock(Vector3 worldPos)
    {
        PlayBurst(_blockPS, worldPos);
    }

    // ─── Internal helpers ────────────────────────────────────────────────

    private void PlayBurst(ParticleSystem ps, Vector3 worldPos)
    {
        if (ps == null) return;
        ps.transform.position = worldPos;
        ps.Play();
    }

    /// <summary>Build all four particle sub-systems as children of this GameObject.</summary>
    private void BuildAllSystems()
    {
        _hitPS    = CreateHitSystem();
        _attackPS = CreateAttackSystem();
        _deathPS  = CreateDeathSystem();
        _blockPS  = CreateBlockSystem();
    }

    // ─── Particle system factories ───────────────────────────────────────

    /// <summary>
    /// Hit sparks: 6 small orange particles shot outward in all directions.
    /// Simulates sparks flying off when the enemy takes a hit.
    /// </summary>
    private ParticleSystem CreateHitSystem()
    {
        var go = new GameObject("FX_Hit");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;

        var ps = go.AddComponent<ParticleSystem>();

        // ── Main module ──
        var main = ps.main;
        main.loop               = false;
        main.playOnAwake        = false;
        main.startLifetime      = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
        main.startSpeed         = new ParticleSystem.MinMaxCurve(2.5f, 5f);
        main.startSize          = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
        main.startColor         = new ParticleSystem.MinMaxGradient(hitSparkColor,
                                     new Color(hitSparkColor.r, hitSparkColor.g * 0.5f, 0f));
        main.gravityModifier    = 1.5f;           // sparks fall
        main.simulationSpace    = ParticleSystemSimulationSpace.World;
        main.maxParticles       = 30;

        // ── Emission: one burst ──
        var emission = ps.emission;
        emission.enabled = true;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 6) });

        // ── Shape: hemisphere pointing up (scatter) ──
        var shape = ps.shape;
        shape.enabled    = true;
        shape.shapeType  = ParticleSystemShapeType.Hemisphere;
        shape.radius     = 0.05f;
        shape.rotation   = new Vector3(-90f, 0f, 0f); // open upward

        // ── Color over lifetime: fade to transparent ──
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(hitSparkColor, 0f), new GradientColorKey(Color.yellow, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        // ── Size over lifetime: shrink ──
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        var sizeCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
        sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // ── Renderer: use built-in dot sprite ──
        ConfigureRenderer(ps, Color.white);

        return ps;
    }

    /// <summary>
    /// Attack slash: 8 elongated yellow particles fanning out in front of the enemy.
    /// Represents the slash arc of a melee attack.
    /// </summary>
    private ParticleSystem CreateAttackSystem()
    {
        var go = new GameObject("FX_Attack");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;

        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop            = false;
        main.playOnAwake     = false;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.15f, 0.28f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        main.startColor      = new ParticleSystem.MinMaxGradient(attackSlashColor);
        main.gravityModifier = 0f;          // slash particles are weightless
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles    = 40;

        var emission = ps.emission;
        emission.enabled = true;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });

        // Cone pointing forward (right by default; rotated per facing direction in PlayAttack)
        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle     = 35f;
        shape.radius    = 0.1f;
        shape.rotation  = new Vector3(0f, -90f, 0f); // shoot rightward

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(attackSlashColor, 0.5f), new GradientColorKey(Color.yellow, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        var sizeCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.5f, 0.6f), new Keyframe(1f, 0f));
        sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        ConfigureRenderer(ps, Color.white);

        return ps;
    }

    /// <summary>
    /// Death explosion: 20 particles in two color bands, high gravity, wide spread.
    /// Produces a dramatic burst that lingers on screen after the enemy is destroyed.
    /// </summary>
    private ParticleSystem CreateDeathSystem()
    {
        var go = new GameObject("FX_Death");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;

        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop            = false;
        main.playOnAwake     = false;
        main.duration        = 0.1f;    // burst is near-instant
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(2f, 7f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.08f, 0.25f);
        // Randomize between two death colors
        main.startColor      = new ParticleSystem.MinMaxGradient(deathColorA, deathColorB);
        main.gravityModifier = 2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles    = 60;

        var emission = ps.emission;
        emission.enabled = true;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20) });

        // Full sphere: fragments fly in every direction
        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.2f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(deathColorA, 0.3f), new GradientColorKey(new Color(0.2f, 0.2f, 0.2f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        var sizeCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.7f, 0.5f), new Keyframe(1f, 0f));
        sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Slightly larger renderer for the death burst
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode      = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder    = 15;
        renderer.material        = CreateDefaultParticleMaterial(Color.white);

        return ps;
    }

    /// <summary>
    /// Block sparks: 5 grey particles bouncing up-and-back.
    /// Represents armor deflecting / the weapon being blocked.
    /// </summary>
    private ParticleSystem CreateBlockSystem()
    {
        var go = new GameObject("FX_Block");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;

        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop            = false;
        main.playOnAwake     = false;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.15f, 0.25f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
        main.startColor      = new ParticleSystem.MinMaxGradient(blockSparkColor);
        main.gravityModifier = 1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles    = 20;

        var emission = ps.emission;
        emission.enabled = true;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 5) });

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius    = 0.03f;
        shape.rotation  = new Vector3(-90f, 0f, 0f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(blockSparkColor, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        ConfigureRenderer(ps, Color.white);

        return ps;
    }

    // ─── Renderer / material helpers ─────────────────────────────────────

    private void ConfigureRenderer(ParticleSystem ps, Color tint)
    {
        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode   = ParticleSystemRenderMode.Billboard;
        r.sortingOrder = 15;
        r.material     = CreateDefaultParticleMaterial(tint);
    }

    /// <summary>
    /// Creates a simple unlit material tinted with the provided color.
    /// Uses the built-in Sprites/Default shader which works in both Built-in RP
    /// and URP in Unity 2022.x.
    /// </summary>
    private Material CreateDefaultParticleMaterial(Color tint)
    {
        // "Sprites/Default" is always available in Unity 2022 (both URP and Built-in)
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = tint;
        // Enable alpha blending so transparent regions are invisible
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        return mat;
    }
}
