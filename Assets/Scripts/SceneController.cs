using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class SceneController : MonoBehaviour
{
    //Singleton
    public static SceneController Instance;

    //Scene
    [SerializeField] private CanvasGroup screenDarkener;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        if (screenDarkener != null)
            screenDarkener.DOFade(0f, 0.5f);
    }

    public void StartGameScene()
    {
        StartCoroutine(FadeOutGameScene());
    }

    public void LoadSceneWithFade(string sceneName)
    {
        StartCoroutine(FadeOutToScene(sceneName));
    }

    public void BackToMainMenu()
    {
        LoadSceneWithFade("Main_Menu");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    IEnumerator FadeOutGameScene()
    {
        yield return FadeOutToScene("MovementTest");
    }

    IEnumerator FadeOutToScene(string sceneName)
    {
        Time.timeScale = 0f;

        if (screenDarkener != null)
            screenDarkener.DOFade(1f, 1.5f).SetUpdate(true);

        yield return new WaitForSecondsRealtime(1.5f);

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}
