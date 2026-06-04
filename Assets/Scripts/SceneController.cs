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
            Destroy(this);
        }
    }

    void Start()
    {
        screenDarkener.alpha = 1f;
        screenDarkener.DOFade(0f, 0.5f);
    }

    public void StartGameScene()
    {
        StartCoroutine(FadeOutGameScene());
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene("Main_Menu");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    IEnumerator FadeOutGameScene()
    {
        screenDarkener.DOFade(1f, 1.5f);
        yield return new WaitForSeconds(1.5f);
        SceneManager.LoadScene("MovementTest");
    }
}
