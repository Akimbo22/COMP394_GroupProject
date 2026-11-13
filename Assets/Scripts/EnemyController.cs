
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class EnemyController : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Animator animator;
    public AudioSource audioSource;

    [Header("Enemy Visual Model")]
    [Tooltip("Assign the enemy model GameObject, not a Renderer.")]
    public GameObject enemyModelForFlash;

    [Header("Audio Clips")]
    public AudioClip attackSound;
    public AudioClip deathSound;
    public AudioClip hitSound;

    [Header("Health")]
    public int maxHealth = 100;

    protected int currentHealth;
    protected bool isDead = false;

    [Header("Combat")]
    public int attackDamage = 25;
    public float attackCooldown = 1.5f;
    public float attackWindup = 0.4f;
    public float attackRangeTolerance = 0.3f;
    public float verticalAttackTolerance = 2f;

    protected bool canAttack = true;

    [Header("Movement")]
    public float followSpeed = 3.5f;
    public float followDistance = 8f;
    public float attackDistance = 1.5f;
    public float stopDistance = 1.2f;

    [Header("Patrol")]
    public Transform[] patrolPoints;
    protected int patrolIndex = 0;
    public float patrolSpeed = 2f;
    public float waypointReachDistance = 0.5f;

    private bool patrolForward = true;

    [Header("Rotation")]
    public float rotationSpeed = 10f;

    [Header("Gravity")]
    public float gravity = -30f;
    public float groundedForce = -2f;

    private float yVelocity;

    [Header("Damage Flash")]
    public Color hitColor = Color.red;
    public float flashDuration = 0.15f;

    [Header("Death")]
    public float deathDespawnDelay = 2f;

    protected CharacterController controller;

    private PlayerHealth playerHealth;
    private Coroutine flashRoutine;

    private static readonly int BaseColorID =
        Shader.PropertyToID("_BaseColor");

    private static readonly int ColorID =
        Shader.PropertyToID("_Color");

    private class FlashPart
    {
        public Renderer renderer;
        public int materialIndex;
        public int colorProperty;
        public MaterialPropertyBlock originalBlock;
    }

    private readonly List<FlashPart> flashParts =
        new List<FlashPart>();

    public int CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    // INITIALIZATION
    protected virtual void Start()
    {
        currentHealth = Mathf.Max(1, maxHealth);

        controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        FindPlayer();
        SetupFlash();

        yVelocity = 0f;
    }

    private void FindPlayer()
    {
        if (playerHealth == null && player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();

            if (playerHealth == null)
                playerHealth =
                    player.GetComponentInParent<PlayerHealth>();
        }

        if (playerHealth == null)
            playerHealth = FindAnyObjectByType<PlayerHealth>();

        if (playerHealth != null)
            player = playerHealth.transform;
    }

    // MAIN LOOP
    protected virtual void Update()
    {
        if (isDead)
            return;

        if (controller == null || !controller.enabled)
            return;

        ApplyGravity();

        if (player == null)
        {
            FindPlayer();

            if (player == null)
            {
                Patrol();
                return;
            }
        }

        if (playerHealth != null && playerHealth.IsDead)
        {
            StopRunning();
            return;
        }

        float distance = FlatDistance(
            transform.position, player.position);

        float verticalDifference = Mathf.Abs(
            player.position.y - transform.position.y);

        // Enemy attacks only when reasonably level.
        if (distance <= attackDistance &&
            verticalDifference <= verticalAttackTolerance)
        {
            StopRunning();
            FaceTargetSmooth(player.position);

            if (canAttack)
                StartCoroutine(AttackPlayer());
        }
        else if (distance <= followDistance &&
                 verticalDifference <= verticalAttackTolerance)
        {
            if (canAttack)
            {
                if (distance > stopDistance)
                    FollowPlayer();
                else
                {
                    StopRunning();
                    FaceTargetSmooth(player.position);
                }
            }
            else
            {
                StopRunning();
            }
        }
        else
        {
            if (canAttack)
                Patrol();
            else
                StopRunning();
        }
    }

    private float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    // GRAVITY
    private void ApplyGravity()
    {
        if (controller.isGrounded)
        {
            if (yVelocity < 0f)
                yVelocity = groundedForce;
        }
        else
        {
            yVelocity += gravity * Time.deltaTime;
        }

        controller.Move(
            Vector3.up * yVelocity * Time.deltaTime);
    }

    // FOLLOW PLAYER
    protected virtual void FollowPlayer()
    {
        if (player == null || isDead)
            return;

        SetRunning(true);

        Vector3 direction =
            player.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
            FaceTargetSmooth(player.position);

        if (direction.magnitude > stopDistance)
        {
            controller.Move(
                direction.normalized *
                followSpeed * Time.deltaTime);
        }
    }

    // PATROL
    protected virtual void Patrol()
    {
        if (patrolPoints == null ||
            patrolPoints.Length == 0)
        {
            StopRunning();
            return;
        }

        patrolIndex = Mathf.Clamp(
            patrolIndex, 0, patrolPoints.Length - 1);

        Transform target = patrolPoints[patrolIndex];

        if (target == null)
        {
            StopRunning();
            return;
        }

        Vector3 direction =
            target.position - transform.position;

        direction.y = 0f;

        float distance = direction.magnitude;

        if (distance > waypointReachDistance)
        {
            SetRunning(true);
            FaceTargetSmooth(target.position);

            controller.Move(
                direction.normalized *
                patrolSpeed * Time.deltaTime);
        }
        else
        {
            StopRunning();

            if (patrolPoints.Length <= 1)
                return;

            if (patrolForward)
            {
                patrolIndex++;

                if (patrolIndex >= patrolPoints.Length)
                {
                    patrolIndex = patrolPoints.Length - 2;
                    patrolForward = false;
                }
            }
            else
            {
                patrolIndex--;

                if (patrolIndex < 0)
                {
                    patrolIndex = 1;
                    patrolForward = true;
                }
            }
        }
    }

    // FACE TARGET
    protected void FaceTargetSmooth(Vector3 target)
    {
        if (isDead)
            return;

        Vector3 direction =
            target - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime);
    }

    // ATTACK PLAYER
    protected virtual IEnumerator AttackPlayer()
    {
        canAttack = false;

        StopRunning();

        SetTriggerSafe("Attack");

        if (attackSound != null && audioSource != null)
            audioSource.PlayOneShot(attackSound);

        // Wait for the attack animation's impact moment.
        yield return new WaitForSeconds(
            Mathf.Max(0f, attackWindup));

        // Never damage after the enemy has died.
        if (!isDead && playerHealth != null &&
            !playerHealth.IsDead && player != null)
        {
            float distance = FlatDistance(
                transform.position, player.position);

            float heightDifference = Mathf.Abs(
                player.position.y - transform.position.y);

            // Recheck range at the actual hit moment.
            if (distance <=
                    attackDistance + attackRangeTolerance &&
                heightDifference <= verticalAttackTolerance)
            {
                // Uses the existing 100 HP system.
                playerHealth.TakeDamage(attackDamage);
            }
        }

        float remainingCooldown = Mathf.Max(
            0f, attackCooldown - attackWindup);

        yield return new WaitForSeconds(remainingCooldown);

        canAttack = true;
    }

    // ENEMY TAKES DAMAGE
    public virtual void TakeDamage(int amount)
    {
        if (isDead || amount <= 0)
            return;

        currentHealth = Mathf.Max(
            0, currentHealth - amount);

        if (hitSound != null && audioSource != null)
            audioSource.PlayOneShot(hitSound);

        StartFlash();

        if (currentHealth <= 0)
        {
            isDead = true;
            StartCoroutine(Die());
        }
        else
        {
            SetTriggerSafe("Hit");
        }
    }

    // DAMAGE FLASH SETUP
    private void SetupFlash()
    {
        flashParts.Clear();

        GameObject visualRoot = enemyModelForFlash;

        if (visualRoot == null ||
            !visualRoot.scene.IsValid() ||
            (visualRoot != gameObject &&
             !visualRoot.transform.IsChildOf(transform)))
        {
            visualRoot = gameObject;
        }

        Renderer[] renderers =
            visualRoot.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (!(renderer is MeshRenderer) &&
                !(renderer is SkinnedMeshRenderer))
                continue;

            Material[] materials = renderer.sharedMaterials;

            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];

                if (material == null)
                    continue;

                int colorProperty;

                if (material.HasProperty(BaseColorID))
                    colorProperty = BaseColorID;
                else if (material.HasProperty(ColorID))
                    colorProperty = ColorID;
                else
                    continue;

                MaterialPropertyBlock original =
                    new MaterialPropertyBlock();

                renderer.GetPropertyBlock(original, i);

                FlashPart part = new FlashPart();
                part.renderer = renderer;
                part.materialIndex = i;
                part.colorProperty = colorProperty;
                part.originalBlock = original;

                flashParts.Add(part);
            }
        }
    }

    private void StartFlash()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            RestoreFlash();
        }

        if (flashParts.Count > 0)
            flashRoutine = StartCoroutine(FlashRed());
    }

    protected IEnumerator FlashRed()
    {
        foreach (FlashPart part in flashParts)
        {
            if (part.renderer == null)
                continue;

            MaterialPropertyBlock block =
                new MaterialPropertyBlock();

            part.renderer.GetPropertyBlock(
                block, part.materialIndex);

            block.SetColor(
                part.colorProperty, hitColor);

            part.renderer.SetPropertyBlock(
                block, part.materialIndex);
        }

        yield return new WaitForSecondsRealtime(
            flashDuration);

        RestoreFlash();
        flashRoutine = null;
    }

    private void RestoreFlash()
    {
        foreach (FlashPart part in flashParts)
        {
            if (part.renderer == null)
                continue;

            if (part.originalBlock.isEmpty)
                part.renderer.SetPropertyBlock(
                    null, part.materialIndex);
            else
                part.renderer.SetPropertyBlock(
                    part.originalBlock, part.materialIndex);
        }
    }

    // ENEMY DEATH
    protected virtual IEnumerator Die()
    {
        StopRunning();

        if (controller != null)
            controller.enabled = false;

        if (deathSound != null && audioSource != null)
            audioSource.PlayOneShot(deathSound);

        SetTriggerSafe("Die");

        yield return new WaitForSeconds(
            Mathf.Max(0f, deathDespawnDelay));

        gameObject.SetActive(false);
    }

    // ANIMATOR HELPERS
    private bool HasParameter(
        string name,
        AnimatorControllerParameterType type)
    {
        if (animator == null)
            return false;

        foreach (AnimatorControllerParameter parameter
                 in animator.parameters)
        {
            if (parameter.name == name &&
                parameter.type == type)
                return true;
        }

        return false;
    }

    private void SetTriggerSafe(string name)
    {
        if (HasParameter(
            name, AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger(name);
        }
    }

    private void SetRunning(bool running)
    {
        if (HasParameter(
            "IsRunning",
            AnimatorControllerParameterType.Bool))
        {
            animator.SetBool("IsRunning", running);
        }
    }

    private void StopRunning()
    {
        SetRunning(false);
    }

    private void OnDisable()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        RestoreFlash();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position, followDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            transform.position, attackDistance);
    }
}
