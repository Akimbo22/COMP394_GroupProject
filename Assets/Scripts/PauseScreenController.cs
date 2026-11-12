using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseScreenController : MonoBehaviour
{
    [SerializeField] private GameObject pauseScreenUI;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pauseScreenUI.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if(Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame)
        {
            PauseScreen();
        }
    }

    private void PauseScreen()
    {
        if(pauseScreenUI.activeInHierarchy)
        {
            pauseScreenUI.SetActive(false);
            Time.timeScale = 1.0f;
        }
        else if(!pauseScreenUI.activeInHierarchy)
        {
            pauseScreenUI.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    public void OnContinueClick()
    {
        pauseScreenUI.SetActive(false);
        Time.timeScale = 1.0f;
    }

    public void OnQuitClick()
    {
        SceneManager.LoadScene("Title");
    }
}
