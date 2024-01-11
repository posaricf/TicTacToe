using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public static PlayerType playerType;

    public void Create()
    {
        playerType = PlayerType.HOST;
        SceneManager.LoadScene(1);
    }

    public void Join()
    {
        playerType = PlayerType.CLIENT;
        SceneManager.LoadScene(1);
    }

    public void Exit()
    {
        Application.Quit();
    }
}
