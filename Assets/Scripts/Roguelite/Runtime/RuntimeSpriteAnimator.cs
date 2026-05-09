using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class RuntimeSpriteAnimator : MonoBehaviour
{
    [SerializeField] private EnemyActionSpriteSet spriteSet;
    [SerializeField] private float actionHoldTime = 0.28f;

    private SpriteRenderer spriteRenderer;
    private Coroutine actionRoutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        ApplyIdle();
    }

    public void SetSpriteSet(EnemyActionSpriteSet value)
    {
        spriteSet = value;
        ApplyIdle();
    }

    public void ApplyIdle()
    {
        Apply(spriteSet != null ? spriteSet.idle : null);
    }

    public void ApplyMove()
    {
        Apply(spriteSet != null ? spriteSet.move : null);
    }

    public void PlayAttack()
    {
        PlayAction(spriteSet != null ? spriteSet.attack : null);
    }

    public void PlayCast()
    {
        PlayAction(spriteSet != null ? spriteSet.cast : null);
    }

    public void PlayCharge()
    {
        PlayAction(spriteSet != null ? spriteSet.charge : null);
    }

    public void PlayHurt()
    {
        PlayAction(spriteSet != null ? spriteSet.hurt : null);
    }

    private void PlayAction(Sprite sprite)
    {
        if (actionRoutine != null)
        {
            StopCoroutine(actionRoutine);
        }

        actionRoutine = StartCoroutine(ActionRoutine(sprite));
    }

    private IEnumerator ActionRoutine(Sprite sprite)
    {
        Apply(sprite);
        yield return new WaitForSeconds(actionHoldTime);
        ApplyIdle();
        actionRoutine = null;
    }

    private void Apply(Sprite sprite)
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null && sprite != null)
        {
            spriteRenderer.sprite = sprite;
        }
    }
}
