
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class DeathUIManager : MonoBehaviour
{
    [Header("Death Screen")]
    [Tooltip("Drag your DeathScreen panel here.")]
    public GameObject deathScreen;

    [Header("UI Buttons")]
    [Tooltip("Drag the Restart button here.")]
    public Button restartButton;

    [Tooltip("Drag the Title Screen button here.")]
    public Button titleScreenButton;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip deathClip;

    [Header("Settings")]
    public bool pauseGameOnDeath = true;
    public bool unlockCursorOnDeath = true;
    public bool showDebugMessages = true;

    [Header("Scene Settings")]
    public string titleSceneName = "TitleScreen 1";

    private bool deathScreenShowing = false;
    private bool loadingScene = false;

    // INITIALIZATION
    private void Awake()
    {
        Time.timeScale = 1f;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        // Never put this manager directly on
        // the panel that is being deactivated.
        if (deathScreen == gameObject)
        {
            Debug.LogError(
                "DeathUIManager: The manager must be " +
                "on a separate active GameObject, " +
                "not on DeathScreen itself!",
                this);
        }
        else if (deathScreen != null)
        {
            deathScreen.SetActive(false);
        }
        else
        {
            Debug.LogWarning(
                "DeathUIManager: DeathScreen panel " +
                "has not been assigned.",
                this);
        }

        // Automatically connect UI buttons.
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartScene);
        }

        if (titleScreenButton != null)
        {
            titleScreenButton.onClick.AddListener(
                LoadTitleScreen);
        }

        deathScreenShowing = false;
        loadingScene = false;
    }

    private void OnDestroy()
    {
        if (restartButton != null)
            restartButton.onClick.RemoveListener(RestartScene);

        if (titleScreenButton != null)
            titleScreenButton.onClick.RemoveListener(
                LoadTitleScreen);
    }

    // CALLED BY PLAYERHEALTH AT ZERO HP
    public void ShowDeathScreen()
    {
        if (deathScreenShowing)
            return;

        if (deathScreen == null)
        {
            Debug.LogError(
                "DeathUIManager: Cannot show death screen. " +
                "Assign DeathScreen in the Inspector!",
                this);
            return;
        }

        if (deathScreen == gameObject)
        {
            Debug.LogError(
                "DeathUIManager: DeathScreen cannot " +
                "be the same object as DeathUIManager.",
                this);
            return;
        }

        deathScreenShowing = true;

        // Make the panel visible.
        deathScreen.SetActive(true);

        if (!deathScreen.activeInHierarchy)
        {
            Debug.LogError(
                "DeathUIManager: DeathScreen was enabled " +
                "but its parent is inactive. " +
                "Make sure the Canvas and all parents " +
                "are active.",
                this);
        }

        // Play death sound.
        if (audioSource != null && deathClip != null)
        {
            audioSource.PlayOneShot(deathClip);
        }

        // Unlock cursor for UI interaction.
        if (unlockCursorOnDeath)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Pause gameplay after showing UI.
        if (pauseGameOnDeath)
            Time.timeScale = 0f;

        if (showDebugMessages)
        {
            Debug.Log(
                "DeathUIManager: Death screen shown.",
                this);
        }
    }

    // RESTART CURRENT SCENE
    public void RestartScene()
    {
        if (loadingScene)
            return;

        loadingScene = true;

        if (showDebugMessages)
        {
            Debug.Log(
                "DeathUIManager: Restarting current scene.",
                this);
        }

        PrepareForSceneChange();

        Scene currentScene =
            SceneManager.GetActiveScene();

        // Prefer the build index when available.
        if (currentScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(
                currentScene.buildIndex);
        }
        else
        {
            SceneManager.LoadScene(
                currentScene.name);
        }
    }

    // LOAD TITLE SCREEN
    public void LoadTitleScreen()
    {
        if (loadingScene)
            return;

        loadingScene = true;

        PrepareForSceneChange();

        SceneManager.LoadScene(titleSceneName);
    }

    // COMPATIBILITY WITH OLD BUTTON METHODS
    public void RestartGameButton()
    {
        RestartScene();
    }

    public void TitleScreenButton()
    {
        LoadTitleScreen();
    }

    // RESET GAME STATE BEFORE SCENE CHANGES
    private void PrepareForSceneChange()
    {
        Time.timeScale = 1f;
        deathScreenShowing = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
