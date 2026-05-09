using System;
using UnityEngine;

public class EraStageSystem : MonoBehaviour
{
    [SerializeField] private EraStageData[] eras;
    [SerializeField] private int currentIndex;

    public event Action<EraStageData> OnEraEntered;
    public event Action<EraStageData> OnEraUnlocked;

    public EraStageData CurrentEra => eras != null && currentIndex >= 0 && currentIndex < eras.Length ? eras[currentIndex] : null;

    private void Start()
    {
        EnterEra(currentIndex);
    }

    public void EnterEra(int index)
    {
        if (eras == null || eras.Length == 0)
        {
            return;
        }

        currentIndex = Mathf.Clamp(index, 0, eras.Length - 1);
        GameRunManager.Instance?.SetCurrentEra(CurrentEra);
        OnEraEntered?.Invoke(CurrentEra);
    }

    public void UnlockAndEnterNextEra()
    {
        if (eras == null || eras.Length == 0)
        {
            return;
        }

        int next = Mathf.Min(currentIndex + 1, eras.Length - 1);
        OnEraUnlocked?.Invoke(eras[next]);
        EnterEra(next);
    }
}
