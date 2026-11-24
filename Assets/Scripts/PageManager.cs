using TMPro;
using UnityEngine;

public class PageManager : MonoBehaviour
{
    public static PageManager Instance { get; private set; }

    [Header("Page Collection")]
    [Min(1)]
    [SerializeField] private int requiredPages = 10;

    [Header("Page Collection UI")]
    [SerializeField] private TMP_Text pageCountText;
    [SerializeField] private TMP_Text requiredPagesText;
    [SerializeField] private TMP_Text bookStatusText;

    // public page information
    public int PagesCollected { get; private set; }
    public int TotalPages { get; private set; }

    public int RequiredPages => requiredPages;

    public bool HasEnoughPages =>
        PagesCollected >= requiredPages;

    public bool AllPagesCollected =>
        PagesCollected >= TotalPages && TotalPages > 0;

    // initialization

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        TotalPages = FindObjectsByType<PagePickup>(
            FindObjectsSortMode.None
        ).Length;

        UpdatePageUI();

        if (requiredPages > TotalPages)
        {
            Debug.LogWarning(
                "Required pages exceeds total pages in the level!"
            );
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // collect page

    public void CollectPage()
    {
        PagesCollected++;

        UpdatePageUI();

        Debug.Log(
            "Pages collected: " +
            PagesCollected + " / " + TotalPages
        );

        if (PagesCollected == requiredPages)
        {
            Debug.Log("Book unlocked!");
        }

        if (AllPagesCollected)
        {
            Debug.Log("All pages collected!");
        }
    }

    // update page ui

    public void UpdatePageUI()
    {
        // Display total collection progress
        if (pageCountText != null)
        {
            pageCountText.text =
                "Pages: " +
                PagesCollected + " / " + TotalPages;
        }

        // Display book unlock progress
        if (requiredPagesText != null)
        {
            requiredPagesText.text =
                "Required: " +
                Mathf.Min(PagesCollected, requiredPages)
                + " / " + requiredPages;
        }

        // Display book unlock status
        if (bookStatusText != null)
        {
            if (HasEnoughPages)
            {
                bookStatusText.text = "Book Unlocked!";
                bookStatusText.color = Color.green;
            }
            else
            {
                bookStatusText.text = "Book Locked";
                bookStatusText.color = Color.white;
            }
        }
    }
}
