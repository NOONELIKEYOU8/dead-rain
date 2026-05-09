using UnityEngine;

public class Death : CoreComponent {
    [SerializeField] private GameObject[] deathParticles;
    [SerializeField] private bool deactivateOnDeath = true;
    [SerializeField] private float deactivateDelay = 0f;
    [SerializeField] private bool disableCollidersOnDeath = true;
    [SerializeField] private bool stopRigidbodyOnDeath = true;

    private ParticleManager ParticleManager => particleManager ??= core.GetCoreComponent<ParticleManager>();
    private ParticleManager particleManager;

    private Stats Stats => stats ??= ResolveStats();
    private Stats stats;
    private bool hasDied;
    
    public void Die() {
        if (hasDied)
        {
            return;
        }

        hasDied = true;
        GameObject owner = core.transform.parent.gameObject;

        foreach (var particle in deathParticles)
        {
            ParticleManager?.StartParticles(particle);
        }

        if (disableCollidersOnDeath)
        {
            foreach (Collider2D collider in owner.GetComponentsInChildren<Collider2D>())
            {
                collider.enabled = false;
            }
        }

        if (stopRigidbodyOnDeath && owner.TryGetComponent(out Rigidbody2D rb))
        {
            rb.velocity = Vector2.zero;
            rb.simulated = false;
        }

        SetAnimatorDead(owner);
        
        if (owner.CompareTag("Player"))
        {
            DisablePlayerControl(owner);
            return;
        }

        if (deactivateOnDeath)
        {
            if (deactivateDelay > 0f)
            {
                Invoke(nameof(DeactivateOwner), deactivateDelay);
            }
            else
            {
                owner.SetActive(false);
            }
        }
    }

    private void DeactivateOwner()
    {
        if (core != null && core.transform.parent != null)
        {
            core.transform.parent.gameObject.SetActive(false);
        }
    }

    private void OnEnable() {
        if (Stats != null)
        {
            Stats.OnHealthZero += Die;
        }
    }

    private void OnDisable() {
        if (stats != null)
        {
            stats.OnHealthZero -= Die;
        }
    }

    private Stats ResolveStats()
    {
        if (core == null)
        {
            core = GetComponentInParent<Core>();
        }

        return core != null ? core.GetCoreComponent<Stats>() : null;
    }

    private static void SetAnimatorDead(GameObject owner)
    {
        Animator animator = owner.GetComponent<Animator>();
        if (animator == null)
        {
            return;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == "dead" && parameter.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool("dead", true);
                return;
            }
        }
    }

    private static void DisablePlayerControl(GameObject owner)
    {
        PlayerInputHandler inputHandler = owner.GetComponent<PlayerInputHandler>();
        if (inputHandler != null)
        {
            inputHandler.enabled = false;
        }

        Player player = owner.GetComponent<Player>();
        if (player != null)
        {
            player.SetBodyVisible(true);
            player.enabled = false;
        }
    }
}
