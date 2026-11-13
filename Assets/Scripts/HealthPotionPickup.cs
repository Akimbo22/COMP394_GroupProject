
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class HealthPotionPickup : MonoBehaviour
{
    [Header("Potion Settings")]
    [Min(1)]
    [SerializeField] private int healAmount = 25;

    [Header("Pickup Audio")]
    [SerializeField] private AudioClip pickupSound;

    [Header("Pickup Effects")]
    [SerializeField] private GameObject pickupEffectPrefab;

    [Header("Debug")]
    [SerializeField] private bool showDebugMessages = true;

    private bool collected = false;

    private void Reset()
    {
        SetupPhysics();
    }

    private void Awake()
    {
        SetupPhysics();
    }

    // Automatically configure potion physics.
    private void SetupPhysics()
    {
        SphereCollider sphere = GetComponent<SphereCollider>();

        if (sphere != null)
        {
            sphere.isTrigger = true;
        }

        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    // Detect when the player enters.
    private void OnTriggerEnter(Collider other)
    {
        TryPickup(other, true);
    }

    // Also detects a player already inside the trigger.
    private void OnTriggerStay(Collider other)
    {
        TryPickup(other, false);
    }

    private void TryPickup(Collider other, bool firstContact)
    {
        if (collected)
            return;

        // Find PlayerHealth on the player or its parent.
        PlayerHealth playerHealth =
            other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
            return;

        if (showDebugMessages && firstContact)
        {
            Debug.Log(
                "Health Potion: Player detected!",
                this);
        }

        // Cannot collect potion when dead.
        if (playerHealth.IsDead)
            return;

        // Do not waste potions when health is full.
        if (playerHealth.CurrentHealth >= playerHealth.MaxHealth)
        {
            if (showDebugMessages && firstContact)
            {
                Debug.Log(
                    "Health Potion: Player already has full HP.",
                    this);
            }

            return;
        }

        // Heal using the shared PlayerHealth system.
        bool healed = playerHealth.TryHeal(healAmount);

        if (!healed)
            return;

        collected = true;

        if (showDebugMessages)
        {
            Debug.Log(
                "Health Potion collected! Current HP: " +
                playerHealth.CurrentHealth,
                this);
        }

        // Optional additional pickup sound.
        // PlayerHealth already plays the heal sound.
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(
                pickupSound,
                transform.position);
        }

        // Optional particle effect.
        if (pickupEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                pickupEffectPrefab,
                transform.position,
                Quaternion.identity);

            Destroy(effect, 3f);
        }

        // Remove the collected potion.
        Destroy(gameObject);
    }
}
