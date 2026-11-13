
using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class BookCollection : MonoBehaviour
{
    [Serializable]
    public class LevelBookSettings
    {
        public string sceneName;
        [Min(1)] public int totalBooks = 50;
    }

    [Header("Level Book Targets")]
    [Tooltip("Default target when the scene is not listed below.")]
    [Min(1)]
    public int targetBookCount = 50;

    [Tooltip("Enter each Unity scene name and its book target.")]
    public LevelBookSettings[] levelBookTargets;

    [Header("Current Level Progress")]
    public int booksCollected = 0;
    public int currentScore = 0;

    [Header("Individual Book Counts")]
    public int redBooks = 0;
    public int blueBooks = 0;
    public int greenBooks = 0;
    public int purpleBooks = 0;
    public int pinkBooks = 0;

    [Header("Book Point Values")]
    public int redPoints = 10;
    public int bluePoints = 2;
    public int greenPoints = 5;
    public int purplePoints = 25;
    public int pinkPoints = 50;

    [Header("Main UI")]
    public TextMeshProUGUI bookText;
    public TextMeshProUGUI scoreText;

    [Header("Optional Individual Book UI")]
    public TextMeshProUGUI redBookText;
    public TextMeshProUGUI blueBookText;
    public TextMeshProUGUI greenBookText;
    public TextMeshProUGUI purpleBookText;
    public TextMeshProUGUI pinkBookText;

    [Header("UI Settings")]
    public bool showLevelName = false;

    [Header("Audio")]
    public AudioSource audioSource;

    public AudioClip redBookSound;
    public AudioClip blueBookSound;
    public AudioClip greenBookSound;
    public AudioClip purpleBookSound;
    public AudioClip pinkBookSound;

    [Header("Optional Level Completion")]
    public bool enableBookCompletion = false;
    public GameObject endGamePanel;
    public TextMeshProUGUI endMessage;
    public string nextSceneName = "Level2";

    private bool gameEnded = false;
    private string currentSceneName;

    public int CurrentLevelTarget => GetLevelTarget();

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        currentSceneName =
            SceneManager.GetActiveScene().name;

        ResetLevelProgress();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        UpdateBookUI();
    }

    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        // Reset counts when moving to a different level.
        if (currentSceneName != scene.name)
        {
            currentSceneName = scene.name;
            ResetLevelProgress();
        }

        UpdateBookUI();
    }

    // GET TARGET FOR CURRENT LEVEL
    public int GetLevelTarget()
    {
        if (levelBookTargets != null)
        {
            foreach (LevelBookSettings level
                     in levelBookTargets)
            {
                if (level != null &&
                    level.sceneName == currentSceneName)
                {
                    return Mathf.Max(1, level.totalBooks);
                }
            }
        }

        return Mathf.Max(1, targetBookCount);
    }

    // COLLECT BOOK
    public bool CollectBook(BookColor bookColor)
    {
        if (gameEnded)
            return false;

        PlayerHealth health =
            GetComponent<PlayerHealth>();

        if (health != null && health.IsDead)
            return false;

        booksCollected++;

        AudioClip pickupSound = null;

        switch (bookColor)
        {
            case BookColor.Red:
                redBooks++;
                currentScore += redPoints;
                pickupSound = redBookSound;
                break;

            case BookColor.Blue:
                blueBooks++;
                currentScore += bluePoints;
                pickupSound = blueBookSound;
                break;

            case BookColor.Green:
                greenBooks++;
                currentScore += greenPoints;
                pickupSound = greenBookSound;
                break;

            case BookColor.Purple:
                purpleBooks++;
                currentScore += purplePoints;
                pickupSound = purpleBookSound;
                break;

            case BookColor.Pink:
                pinkBooks++;
                currentScore += pinkPoints;
                pickupSound = pinkBookSound;
                break;
        }

        if (pickupSound != null && audioSource != null)
            audioSource.PlayOneShot(pickupSound);

        UpdateBookUI();

        Debug.Log(
            currentSceneName + ": Collected " +
            bookColor + " book. " +
            booksCollected + "/" + GetLevelTarget());

        if (enableBookCompletion &&
            booksCollected >= GetLevelTarget())
        {
            CompleteLevel();
        }

        return true;
    }

    // UPDATE THE UI
    public void UpdateBookUI()
    {
        int target = GetLevelTarget();

        if (bookText != null)
        {
            string prefix = showLevelName
                ? currentSceneName + " - Books: "
                : "Books: ";

            bookText.text =
                prefix + booksCollected + "/" + target;
        }

        if (scoreText != null)
            scoreText.text = "Score: " + currentScore;

        if (redBookText != null)
            redBookText.text = "Red: " + redBooks;

        if (blueBookText != null)
            blueBookText.text = "Blue: " + blueBooks;

        if (greenBookText != null)
            greenBookText.text = "Green: " + greenBooks;

        if (purpleBookText != null)
            purpleBookText.text = "Purple: " + purpleBooks;

        if (pinkBookText != null)
            pinkBookText.text = "Pink: " + pinkBooks;
    }

    // RESET COUNTS FOR NEW LEVEL
    private void ResetLevelProgress()
    {
        booksCollected = 0;
        currentScore = 0;

        redBooks = 0;
        blueBooks = 0;
        greenBooks = 0;
        purpleBooks = 0;
        pinkBooks = 0;

        gameEnded = false;

        if (endGamePanel != null)
            endGamePanel.SetActive(false);

        UpdateBookUI();
    }

    // OPTIONAL LEVEL COMPLETION
    private void CompleteLevel()
    {
        if (gameEnded)
            return;

        gameEnded = true;

        if (endMessage != null)
        {
            endMessage.text =
                "Congratulations! All books collected!";
        }

        if (endGamePanel != null)
        {
            endGamePanel.SetActive(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Time.timeScale = 0f;
        }
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name);
    }

    public void GoToNextLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(nextSceneName);
    }

    public void GoToLevel2()
    {
        GoToNextLevel();
    }
}


