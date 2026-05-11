using UnityEngine;

public class BossAltar : MonoBehaviour
{
    [SerializeField] private RunFlowController flowController;
    [SerializeField] private Transform bossSpawnPoint;
    [SerializeField] private SpriteRenderer altarRenderer;
    [SerializeField] private Color idleColor = new Color(0.55f, 0.34f, 0.11f, 1f);
    [SerializeField] private Color activatedColor = new Color(0.95f, 0.28f, 0.08f, 1f);

    private bool activated;

    public Transform BossSpawnPoint => bossSpawnPoint != null ? bossSpawnPoint : transform;
    public bool IsActivated => activated;

    public void Initialize(RunFlowController controller, Transform spawnPoint, SpriteRenderer renderer)
    {
        flowController = controller;
        bossSpawnPoint = spawnPoint;
        altarRenderer = renderer;
        SetColor(activated ? activatedColor : idleColor);
    }

    private void Awake()
    {
        if (flowController == null)
        {
            flowController = FindObjectOfType<RunFlowController>();
        }

        if (altarRenderer == null)
        {
            altarRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        SetColor(idleColor);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activated || !other.CompareTag("Player"))
        {
            return;
        }

        Activate();
    }

    public void Activate()
    {
        if (activated)
        {
            return;
        }

        activated = true;
        SetColor(activatedColor);

        if (flowController == null)
        {
            flowController = FindObjectOfType<RunFlowController>();
        }

        flowController?.TrySpawnBossFromAltar(this);
    }

    private void SetColor(Color color)
    {
        if (altarRenderer != null)
        {
            altarRenderer.color = color;
        }
    }
}
