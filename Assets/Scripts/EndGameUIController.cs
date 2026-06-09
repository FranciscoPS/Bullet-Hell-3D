using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndGameUIController : MonoBehaviour
{
    [Header("End Game Panels")]
    [SerializeField] private CanvasGroup playerDefeatPanel;
    [SerializeField] private CanvasGroup bossVictoryPanel;

    [Header("Buttons")]
    [SerializeField] private Button playerDefeatRepeatButton;
    [SerializeField] private Button playerDefeatMainMenuButton;
    [SerializeField] private Button bossVictoryRepeatButton;
    [SerializeField] private Button bossVictoryMainMenuButton;

    private void Awake()
    {
        ResolveButtons();

        if (playerDefeatRepeatButton != null)
            playerDefeatRepeatButton.onClick.AddListener(RepeatLevel);

        if (playerDefeatMainMenuButton != null)
            playerDefeatMainMenuButton.onClick.AddListener(GoToMainMenu);

        if (bossVictoryRepeatButton != null)
            bossVictoryRepeatButton.onClick.AddListener(RepeatLevel);

        if (bossVictoryMainMenuButton != null)
            bossVictoryMainMenuButton.onClick.AddListener(GoToMainMenu);

        SetPanelVisible(playerDefeatPanel, false);
        SetPanelVisible(bossVictoryPanel, false);
    }

    public void ShowPlayerDefeat()
    {
        SetPanelVisible(playerDefeatPanel, true);
        SetPanelVisible(bossVictoryPanel, false);
        Time.timeScale = 0f;
    }

    public void ShowBossVictory()
    {
        SetPanelVisible(playerDefeatPanel, false);
        SetPanelVisible(bossVictoryPanel, true);
        Time.timeScale = 0f;
    }

    private void ResolveButtons()
    {
        if (playerDefeatPanel != null)
        {
            Button[] defeatButtons = playerDefeatPanel.GetComponentsInChildren<Button>(true);
            if (playerDefeatRepeatButton == null && defeatButtons.Length > 0)
                playerDefeatRepeatButton = defeatButtons[0];
            if (playerDefeatMainMenuButton == null && defeatButtons.Length > 1)
                playerDefeatMainMenuButton = defeatButtons[1];
        }

        if (bossVictoryPanel != null)
        {
            Button[] victoryButtons = bossVictoryPanel.GetComponentsInChildren<Button>(true);
            if (bossVictoryRepeatButton == null && victoryButtons.Length > 0)
                bossVictoryRepeatButton = victoryButtons[0];
            if (bossVictoryMainMenuButton == null && victoryButtons.Length > 1)
                bossVictoryMainMenuButton = victoryButtons[1];
        }
    }

    private void RepeatLevel()
    {
        if (SceneController.Instance != null)
            SceneController.Instance.LoadSceneWithFade(SceneManager.GetActiveScene().name);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void GoToMainMenu()
    {
        if (SceneController.Instance != null)
            SceneController.Instance.LoadSceneWithFade("Main_Menu");
        else
            SceneManager.LoadScene("Main_Menu");
    }

    private void SetPanelVisible(CanvasGroup panel, bool visible)
    {
        if (panel == null)
            return;

        panel.alpha = visible ? 1f : 0f;
        panel.interactable = visible;
        panel.blocksRaycasts = visible;
    }
}
