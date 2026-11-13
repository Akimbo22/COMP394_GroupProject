
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Min(1)]
    [SerializeField] private int maxHealth = 100;

    [SerializeField] private bool startAtFullHealth = true;

    [Min(0)]
    [SerializeField] private int startingHealth = 100;

    [Header("Health Bar UI")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Image healthFillImage;

    [Tooltip("Hide the entire health slider at zero HP.")]
    [SerializeField] private bool hideHealthBarAtZero = true;

    [Header("Health Bar Colors")]
    [SerializeField]
    private Color fullHealthColor =
        new Color(0.86f, 0.19f, 0.23f, 1f);

    [SerializeField]
    private Color lowHealthColor =
        new Color(0.55f, 0.07f, 0.11f, 1f);

    [Header("Audio")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip healSound;

    [Header("Respawn Settings")]
    [SerializeField] private Transform respawnPoint;

    [SerializeField]
    private Vector3 spawnPosition =
        new Vector3(-4.15f, 13.47f, -13.22f);

    [Header("Out Of Bounds")]
    [SerializeField] private float minY = -340f;

    [Min(0)]
    [SerializeField] private int fallDamage = 25;

    [HideInInspector]
    public bool suppressFallDamage = false;

    [Header("Death UI")]
    [SerializeField] private DeathUIManager deathUIManager;

    [Min(0f)]
    [SerializeField] private float deathScreenDelay = 1.5f;

    [Header("Damage Flash - Character Model")]
    [Tooltip(
        "Drag your character GameObject here. " +
        "The script finds all mesh renderers automatically. " +
        "Leave empty to use the Player's children.")]
    [SerializeField] private GameObject characterModelForFlash;

    [SerializeField] private Color flashColor = Color.red;

    [Min(0f)]
    [SerializeField] private float flashDuration = 0.15f;

    // PRIVATE VARIABLES
    private int currentHealth;

    private AudioSource audioSource;
    private HeroController heroController;

    private Coroutine flashRoutine;

    private bool outOfBoundsCooldown = false;
    private bool deathTriggered = false;

    // Shader color properties
    private static readonly int BaseColorID =
        Shader.PropertyToID("_BaseColor");

    private static readonly int ColorID =
        Shader.PropertyToID("_Color");

    // Store each character material slot.
    private class FlashSlot
    {
        public Renderer renderer;
        public int materialIndex;
        public int colorPropertyID;
        public MaterialPropertyBlock originalBlock;
    }

    private readonly List<FlashSlot> flashSlots =
        new List<FlashSlot>();

    // PUBLIC HEALTH INFORMATION
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0;

    // Events used by HeroController
    public event Action Damaged;
    public event Action Died;

    // --------------------------------------------------
    // INITIALIZATION
    // --------------------------------------------------

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);

        currentHealth = startAtFullHealth
            ? maxHealth
            : Mathf.Clamp(startingHealth, 0, maxHealth);

        audioSource = GetComponent<AudioSource>();

        heroController = GetComponent<HeroController>();

        FindDeathUIManager();

        SetupHealthBar();

        SetupDamageFlash();

        UpdateHealthUI();
    }

    private void Start()
    {
        UpdateHealthUI();

        if (deathUIManager == null)
            FindDeathUIManager();

        if (IsDead)
            HandleDeath();
    }

    private void Update()
    {
        if (IsDead)
            return;

        CheckOutOfBoundsY();
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);

        startingHealth = Mathf.Clamp(
            startingHealth, 0, maxHealth);

        fallDamage = Mathf.Max(0, fallDamage);

        flashDuration = Mathf.Max(0f, flashDuration);

        deathScreenDelay = Mathf.Max(0f, deathScreenDelay);
    }

    // --------------------------------------------------
    // FIND DEATH UI MANAGER
    // --------------------------------------------------

    private void FindDeathUIManager()
    {
        if (deathUIManager != null)
            return;

        deathUIManager =
            FindAnyObjectByType<DeathUIManager>(
                FindObjectsInactive.Include);
    }

    // --------------------------------------------------
    // HEALTH BAR INITIALIZATION
    // --------------------------------------------------

    private void SetupHealthBar()
    {
        if (healthBar == null)
            return;

        // Automatically find the slider's Fill image.
        if (healthBar.fillRect != null)
        {
            Image fill =
                healthBar.fillRect.GetComponent<Image>();

            if (fill != null)
                healthFillImage = fill;
        }

        healthBar.minValue = 0f;
        healthBar.maxValue = maxHealth;
        healthBar.wholeNumbers = true;
        healthBar.interactable = false;

        healthBar.transition =
            Selectable.Transition.None;
    }

    // --------------------------------------------------
    // UPDATE HEALTH UI
    // --------------------------------------------------

    public void UpdateHealthUI()
    {
        // Update the actual health slider.
        if (healthBar != null)
        {
            healthBar.minValue = 0f;
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }

        // Display health numbers.
        if (healthText != null)
        {
            healthText.text =
                currentHealth + " / " + maxHealth;
        }

        // Automatically color the fill red.
        if (healthFillImage != null)
        {
            float percentage =
                currentHealth / (float)maxHealth;

            healthFillImage.color = Color.Lerp(
                lowHealthColor,
                fullHealthColor,
                percentage);

            // Completely disable fill at zero HP.
            healthFillImage.enabled = currentHealth > 0;
        }

        // FIX FOR THE BLACK BAR AT ZERO HP.
        // Hide the entire slider, including any leftover
        // fill, background, or slider graphics.
        if (healthBar != null && hideHealthBarAtZero)
        {
            healthBar.gameObject.SetActive(
                currentHealth > 0);
        }
    }

    // --------------------------------------------------
    // TAKE DAMAGE
    // --------------------------------------------------

    public void TakeDamage(int amount)
    {
        ApplyDamage(amount, false);
    }

    private void ApplyDamage(
        int amount,
        bool ignoreRollInvulnerability)
    {
        if (amount <= 0 || IsDead)
            return;

        // Preserve invulnerability while rolling.
        if (!ignoreRollInvulnerability &&
            heroController != null &&
            heroController.IsRolling)
        {
            return;
        }

        currentHealth = Mathf.Max(
            0, currentHealth - amount);

        UpdateHealthUI();

        // Play hit sound.
        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound);
        }

        // Flash character model.
        StartDamageFlash();

        // Death or hit animation.
        if (IsDead)
        {
            HandleDeath();
        }
        else
        {
            Damaged?.Invoke();
        }
    }

    // --------------------------------------------------
    // HEAL PLAYER
    // --------------------------------------------------

    public bool TryHeal(int amount)
    {
        if (amount <= 0)
            return false;

        if (IsDead)
            return false;

        if (currentHealth >= maxHealth)
            return false;

        currentHealth = Mathf.Min(
            maxHealth, currentHealth + amount);

        UpdateHealthUI();

        // Play healing sound.
        if (healSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(healSound);
        }

        return true;
    }

    // Compatibility with old potion scripts.
    public void AddHealth(int amount)
    {
        TryHeal(amount);
    }

    // --------------------------------------------------
    // INSTANT DEATH
    // --------------------------------------------------

    public void Kill()
    {
        if (IsDead)
            return;

        currentHealth = 0;

        UpdateHealthUI();

        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound);
        }

        StartDamageFlash();

        HandleDeath();
    }

    // --------------------------------------------------
    // SETUP DAMAGE FLASH
    // --------------------------------------------------

    private void SetupDamageFlash()
    {
        flashSlots.Clear();

        GameObject target = characterModelForFlash;

        // A prefab asset in the Project window is not
        // the live model in the scene.
        // If one was assigned, use the live Player instead.
        if (target == null ||
            !target.scene.IsValid() ||
            (target != gameObject &&
             !target.transform.IsChildOf(transform)))
        {
            target = gameObject;
        }

        // This includes SkinnedMeshRenderers used
        // by animated 3D characters.
        Renderer[] renderers =
            target.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer r in renderers)
        {
            // Only flash character mesh renderers.
            if (!(r is SkinnedMeshRenderer) &&
                !(r is MeshRenderer))
            {
                continue;
            }

            Material[] materials = r.sharedMaterials;

            for (int i = 0; i < materials.Length; i++)
            {
                Material mat = materials[i];

                if (mat == null)
                    continue;

                int propertyID;

                // URP / HDRP / compatible shaders
                if (mat.HasProperty(BaseColorID))
                {
                    propertyID = BaseColorID;
                }
                // Built-in Standard / compatible shaders
                else if (mat.HasProperty(ColorID))
                {
                    propertyID = ColorID;
                }
                else
                {
                    continue;
                }

                // Save existing material overrides.
                MaterialPropertyBlock original =
                    new MaterialPropertyBlock();

                r.GetPropertyBlock(original, i);

                FlashSlot slot = new FlashSlot();

                slot.renderer = r;
                slot.materialIndex = i;
                slot.colorPropertyID = propertyID;
                slot.originalBlock = original;

                flashSlots.Add(slot);
            }
        }

        if (flashSlots.Count == 0)
        {
            Debug.LogWarning(
                "PlayerHealth: No compatible character " +
                "mesh materials found for damage flash. " +
                "The health system will still work.",
                this);
        }
    }

    // --------------------------------------------------
    // START DAMAGE FLASH
    // --------------------------------------------------

    private void StartDamageFlash()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            RestoreDamageFlash();
            flashRoutine = null;
        }

        // Try again if the model was added later.
        if (flashSlots.Count == 0)
            SetupDamageFlash();

        if (flashSlots.Count == 0)
            return;

        flashRoutine = StartCoroutine(FlashDamage());
    }

    // --------------------------------------------------
    // FLASH CHARACTER RED
    // --------------------------------------------------

    private IEnumerator FlashDamage()
    {
        foreach (FlashSlot slot in flashSlots)
        {
            if (slot.renderer == null)
                continue;

            MaterialPropertyBlock block =
                new MaterialPropertyBlock();

            slot.renderer.GetPropertyBlock(
                block, slot.materialIndex);

            block.SetColor(
                slot.colorPropertyID,
                flashColor);

            slot.renderer.SetPropertyBlock(
                block, slot.materialIndex);
        }

        yield return new WaitForSecondsRealtime(
            flashDuration);

        RestoreDamageFlash();

        flashRoutine = null;
    }

    // --------------------------------------------------
    // RESTORE ORIGINAL CHARACTER APPEARANCE
    // --------------------------------------------------

    private void RestoreDamageFlash()
    {
        foreach (FlashSlot slot in flashSlots)
        {
            if (slot.renderer == null)
                continue;

            // Restore previous material properties.
            if (slot.originalBlock.isEmpty)
            {
                slot.renderer.SetPropertyBlock(
                    null, slot.materialIndex);
            }
            else
            {
                slot.renderer.SetPropertyBlock(
                    slot.originalBlock,
                    slot.materialIndex);
            }
        }
    }

    private void OnDisable()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        RestoreDamageFlash();
    }

    // --------------------------------------------------
    // OUT OF BOUNDS
    // --------------------------------------------------

    private void CheckOutOfBoundsY()
    {
        if (outOfBoundsCooldown)
            return;

        if (transform.position.y < minY)
            OutOfBoundsDamage();
    }

    public void OutOfBoundsDamage()
    {
        if (IsDead || outOfBoundsCooldown)
            return;

        StartCoroutine(HandleOutOfBounds());
    }

    // --------------------------------------------------
    // RESPAWN AFTER FALLING
    // --------------------------------------------------

    private IEnumerator HandleOutOfBounds()
    {
        outOfBoundsCooldown = true;

        suppressFallDamage = true;

        // Fall damage ignores roll invulnerability.
        ApplyDamage(fallDamage, true);

        if (IsDead)
        {
            suppressFallDamage = false;
            outOfBoundsCooldown = false;
            yield break;
        }

        CharacterController cc =
            GetComponent<CharacterController>();

        if (cc != null)
            cc.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
        }

        Vector3 destination =
            respawnPoint != null
                ? respawnPoint.position
                : spawnPosition;

        transform.position =
            destination + Vector3.up * 1f;

        // Reset gravity on HeroController.
        if (heroController != null)
            heroController.ResetVerticalVelocity();

        yield return null;

        if (cc != null)
            cc.enabled = true;

        yield return new WaitForSeconds(0.2f);

        suppressFallDamage = false;
        outOfBoundsCooldown = false;
    }

    // --------------------------------------------------
    // DEATH
    // --------------------------------------------------

    private void HandleDeath()
    {
        if (deathTriggered)
            return;

        deathTriggered = true;

        // Tell HeroController to play death animation.
        Died?.Invoke();

        StartCoroutine(ShowDeathScreenAfterDelay());
    }

    private IEnumerator ShowDeathScreenAfterDelay()
    {
        yield return new WaitForSecondsRealtime(
            deathScreenDelay);

        FindDeathUIManager();

        if (deathUIManager != null)
        {
            deathUIManager.ShowDeathScreen();
        }
        else
        {
            Debug.LogWarning(
                "PlayerHealth: DeathUIManager not found. " +
                "Assign your DeathUIManager in the Inspector.",
                this);
        }
    }

    // --------------------------------------------------
    // RESTART BUTTON
    // --------------------------------------------------

    public void RestartGameButton()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name);
    }

    // --------------------------------------------------
    // TITLE SCREEN BUTTON
    // --------------------------------------------------

    public void TitleScreenButton()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene("TitleScreen 1");
    }
}
