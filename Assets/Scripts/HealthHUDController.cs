using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthHUDController : MonoBehaviour
{
    [Header("Player HUD")]
    [SerializeField] private Slider playerHealthSlider;
    [SerializeField] private TextMeshProUGUI playerHealthText;

    [Header("Boss HUD")]
    [SerializeField] private Slider bossHealthSlider;
    [SerializeField] private TextMeshProUGUI bossHealthText;

    private PlayerHealth playerHealth;
    private BossHealth bossHealth;

    private void Start()
    {
        playerHealth = FindAnyObjectByType<PlayerHealth>();
        bossHealth = FindAnyObjectByType<BossHealth>();

        if (playerHealthSlider != null)
            playerHealthSlider.gameObject.SetActive(playerHealth != null);

        if (playerHealthText != null)
            playerHealthText.gameObject.SetActive(playerHealth != null);

        if (bossHealthSlider != null)
            bossHealthSlider.gameObject.SetActive(bossHealth != null);

        if (bossHealthText != null)
            bossHealthText.gameObject.SetActive(bossHealth != null);
    }

    private void Update()
    {
        if (playerHealth != null)
        {
            if (playerHealthSlider != null)
            {
                float ratio = playerHealth.MaxHealth > 0f
                    ? playerHealth.CurrentHealth / playerHealth.MaxHealth
                    : 0f;

                playerHealthSlider.value = ratio;
            }

            if (playerHealthText != null)
                playerHealthText.text = $"HP: {Mathf.CeilToInt(playerHealth.CurrentHealth)} / {Mathf.CeilToInt(playerHealth.MaxHealth)}";
        }

        if (bossHealth != null)
        {
            if (bossHealthSlider != null)
                bossHealthSlider.value = bossHealth.HealthRatio;

            if (bossHealthText != null)
                bossHealthText.text = $"BOSS: {Mathf.CeilToInt(bossHealth.HealthRatio * 100f)}%";
        }
    }
}
