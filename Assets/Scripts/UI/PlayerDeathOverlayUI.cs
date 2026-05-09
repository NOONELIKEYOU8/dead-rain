using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerDeathOverlayUI : MonoBehaviour
{
    [SerializeField] private Text messageText;
    [SerializeField] private KeyCode restartKey = KeyCode.R;

    private Stats playerStats;

    public void Bind(Stats stats)
    {
        if (playerStats != null)
        {
            playerStats.OnHealthZero -= Show;
        }

        playerStats = stats;

        if (playerStats != null)
        {
            playerStats.OnHealthZero += Show;
        }

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnHealthZero -= Show;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(restartKey))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void Show()
    {
        gameObject.SetActive(true);

        if (messageText != null)
        {
            messageText.text = "YOU DIED\nPress R to restart";
        }
    }
}
