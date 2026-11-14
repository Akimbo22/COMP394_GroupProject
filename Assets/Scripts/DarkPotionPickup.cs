using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class DarkPotionPickup : MonoBehaviour
{
    [Header("Dark Potion Settings")]
    [Min(1)]
    [SerializeField] private int damageAmount = 25;

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

    private void OnTriggerEnter(Collider other)
    {
        if (collected)
            return;

        PlayerHealth playerHealth =
            other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null || playerHealth.IsDead)
            return;

        // Prevent any additional damage immediately.
        collected = true;

        int healthBefore = playerHealth.CurrentHealth;

        playerHealth.TakeDamage(damageAmount);

        Debug.Log(
            "Dark Potion: HP before = " + healthBefore +
            ", HP after = " + playerHealth.CurrentHealth);

        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(
                pickupSound,
                transform.position);
        }

        if (pickupEffectPrefab != null)
        {
        GameObject effect = Instantiate(
                pickupEffectPrefab,
                transform.position,
                Quaternion.identity);

            Destroy(effect, 3f);
        }

        Destroy(gameObject);
    }
}
