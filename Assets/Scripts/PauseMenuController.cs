using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenuController : MonoBehaviour
{
    [Header("Pause UI")]
    [SerializeField] private CanvasGroup pauseMenu;

    private bool isPaused;
    private InputAction pauseAction;

    private void Start()
    {
        if (pauseMenu != null)
            SetPauseMenuVisible(false);

        PlayerInput playerInput = FindObjectOfType<PlayerInput>();
        if (playerInput != null)
            pauseAction = playerInput.actions["Pause"];
    }

    private void OnEnable()
    {
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
        if (pauseAction != null && pauseAction.WasPressedThisFrame())
            TogglePause();
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        SetPauseMenuVisible(isPaused);
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
