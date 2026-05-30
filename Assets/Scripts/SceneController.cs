using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(this);
        }
    }

    public void StartGameScene()
    {
        SceneManager.LoadScene("MovementTest");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
