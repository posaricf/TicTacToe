using UnityEngine;
using UnityEngine.SceneManagement;

public class EscapeMenu : MonoBehaviour
{
    public bool paused;
    public GameObject background;
    public GameObject exitButton;
    public GameObject resumeButton;

    void Start()
    {
        paused = false;
        background.SetActive(paused);
        exitButton.SetActive(paused);
        resumeButton.SetActive(paused);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            paused = !paused;
            background.SetActive(paused);
            exitButton.SetActive(paused);
            resumeButton.SetActive(paused);
        }
    }

    public void Resume()
    {
        paused = false;
        background.SetActive(false);
        exitButton.SetActive(false);
        resumeButton.SetActive(false);
    }

    public void Exit()
    {
        GameManager mng = FindObjectOfType<GameManager>();
        mng.AccessDispose();
        SceneManager.LoadScene(0); 
    }
}
