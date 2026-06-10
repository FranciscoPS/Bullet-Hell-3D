using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [Header("Pause UI")]
    [SerializeField] private CanvasGroup pauseMenu;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button mainMenuButton;

    private bool isPaused;
    private InputAction pauseAction;

    private void Awake()
    {
        ResolveButtons();

        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);
    }

    private void Start()
    {
        if (pauseMenu != null)
            SetPauseMenuVisible(false);

        TryBindPauseAction();
    }

    private void OnEnable()
    {
        TryBindPauseAction();

        if (pauseAction != null)
            pauseAction.Enable();
    }

    private void OnDisable()
    {
        if (pauseAction != null)
            pauseAction.Disable();
    }

    private void Update()
    {
        TryBindPauseAction();

        if (pauseAction != null && pauseAction.WasPressedThisFrame())
            TogglePause();
    }

    private void TryBindPauseAction()
    {
        if (pauseAction != null)
            return;

        PlayerInput playerInput = FindAnyObjectByType<PlayerInput>();
        if (playerInput != null)
            pauseAction = playerInput.actions["Pause"];
    }

    private void ResolveButtons()
    {
        if (pauseMenu == null)
            return;

        Button[] buttons = pauseMenu.GetComponentsInChildren<Button>(true);

        if (resumeButton == null && buttons.Length > 0)
            resumeButton = buttons[0];

        if (mainMenuButton == null && buttons.Length > 1)
            mainMenuButton = buttons[1];
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        SetPauseMenuVisible(isPaused);
    }

    public void ResumeGame()
    {
        if (isPaused)
            TogglePause();
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;

        if (SceneController.Instance != null)
            SceneController.Instance.BackToMainMenu();
        else
            SceneManager.LoadScene("Main_Menu");
    }

    private void SetPauseMenuVisible(bool visible)
    {
        if (pauseMenu == null)
            return;

        pauseMenu.alpha = visible ? 1f : 0f;
        pauseMenu.interactable = visible;
        pauseMenu.blocksRaycasts = visible;
    }
}
