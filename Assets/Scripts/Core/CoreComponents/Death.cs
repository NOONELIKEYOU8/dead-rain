using System;
using Unity.VisualScripting;
using UnityEngine;

public class Death : CoreComponent {
    [SerializeField] private GameObject[] deathParticles;

    private ParticleManager ParticleManager => particleManager ??= core.GetCoreComponent<ParticleManager>();
    private ParticleManager particleManager;

    private Stats Stats => stats ??= core.GetCoreComponent<Stats>();
    private Stats stats;
    
    public void Die() {
        foreach (var particle in deathParticles) {
            ParticleManager.StartParticles(particle);
        }
        
        core.transform.parent.gameObject.SetActive(false);
    }

    private void OnEnable() {
        Stats.OnHealthZero += Die;
    }

    private void OnDisable() {
        Stats.OnHealthZero -= Die;
    }
}
