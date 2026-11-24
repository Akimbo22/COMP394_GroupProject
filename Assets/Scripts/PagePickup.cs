using UnityEngine;

public class PagePickup : MonoBehaviour
{
    private bool collected = false;

    private void OnTriggerEnter(Collider other)
    {
        if (collected)
        {
            return;
        }

        if (other.CompareTag("Player"))
        {
            if (PageManager.Instance == null)
            {
                Debug.LogWarning("PageManager is missing!");
                return;
            }

            collected = true;

            PageManager.Instance.CollectPage();

            Destroy(gameObject);
        }
    }
}
