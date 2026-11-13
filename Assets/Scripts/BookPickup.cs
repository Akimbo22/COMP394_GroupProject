
using UnityEngine;

public enum BookColor
{
    Red,
    Blue,
    Green,
    Purple,
    Pink
}

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class BookPickup : MonoBehaviour
{
    [Header("Book Settings")]
    public BookColor bookColor = BookColor.Red;

    [Header("Pickup Settings")]
    public bool showDebugMessages = false;

    private bool collected = false;

    private void Reset()
    {
        ConfigurePhysics();
    }

    private void Awake()
    {
        ConfigurePhysics();
    }

    private void ConfigurePhysics()
    {
        SphereCollider sphere =
            GetComponent<SphereCollider>();

        if (sphere != null)
            sphere.isTrigger = true;

        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryCollect(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryCollect(other);
    }

    private void TryCollect(Collider other)
    {
        if (collected)
            return;

        // Find BookCollection on player's root or parent.
        BookCollection collection =
            other.GetComponentInParent<BookCollection>();

        if (collection == null)
            return;

        bool success = collection.CollectBook(bookColor);

        if (!success)
            return;

        collected = true;

        if (showDebugMessages)
        {
            Debug.Log(
                "Picked up " + bookColor + " book.",
                this);
        }

        Destroy(gameObject);
    }
}

