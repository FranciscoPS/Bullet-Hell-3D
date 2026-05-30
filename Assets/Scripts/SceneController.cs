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

    public void StartGameScene()
    {
        StartCoroutine(FadeOutGameScene());
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
