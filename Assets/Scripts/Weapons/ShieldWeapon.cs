using UnityEngine;

public class ShieldWeapon : AggressiveWeapon
{
    [SerializeField] private Sprite baseHoldSprite;
    [SerializeField] private Sprite weaponHoldSprite;
    [SerializeField] private int firstParryAttackIndex = 1;
    [SerializeField] private int parryAttackCount = 2;

    private SpriteRenderer baseRenderer;
    private SpriteRenderer weaponRenderer;
    private int parryCounter;
    private bool isBlocking;
    private bool isParrying;

    protected override void Awake()
    {
        base.Awake();

        Transform baseTransform = transform.Find("Base");
        Transform weaponTransform = transform.Find("Weapon");

        if (baseTransform != null)
        {
            baseRenderer = baseTransform.GetComponent<SpriteRenderer>();
        }

        if (weaponTransform != null)
        {
            weaponRenderer = weaponTransform.GetComponent<SpriteRenderer>();
        }
    }

    public override void EnterWeapon()
    {
        isParrying = true;
        SetAnimatorsEnabled(true);

        attackCounter = firstParryAttackIndex + parryCounter;
        parryCounter = (parryCounter + 1) % Mathf.Max(1, parryAttackCount);

        base.EnterWeapon();
    }

    public override void ExitWeapon()
    {
        baseAnimator.SetBool("attack", false);
        weaponAnimator.SetBool("attack", false);
        isParrying = false;

        if (isBlocking)
        {
            SetAnimatorsEnabled(false);
            ShowHoldSprites();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void SetBlocking(bool value)
    {
        isBlocking = value;

        if (isBlocking)
        {
            gameObject.SetActive(true);
            SetAnimatorsEnabled(false);
            baseAnimator.SetBool("attack", false);
            weaponAnimator.SetBool("attack", false);
            ShowHoldSprites();
        }
        else if (!isParrying)
        {
            gameObject.SetActive(false);
        }
    }

    private void SetAnimatorsEnabled(bool value)
    {
        if (baseAnimator != null)
        {
            baseAnimator.enabled = value;
        }

        if (weaponAnimator != null)
        {
            weaponAnimator.enabled = value;
        }
    }

    private void ShowHoldSprites()
    {
        if (baseRenderer != null)
        {
            baseRenderer.sprite = baseHoldSprite;
        }

        if (weaponRenderer != null)
        {
            weaponRenderer.sprite = weaponHoldSprite;
        }
    }
}
