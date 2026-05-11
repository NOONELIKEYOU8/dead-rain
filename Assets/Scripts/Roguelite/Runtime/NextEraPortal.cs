using UnityEngine;

public class NextEraPortal : MonoBehaviour
{
    [SerializeField] private RunFlowController flowController;

    private void Awake()
    {
        if (flowController == null)
        {
            flowController = FindObjectOfType<RunFlowController>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        flowController?.EnterNextEra();
    }

    public void Initialize(RunFlowController controller)
    {
        flowController = controller;
    }
}
