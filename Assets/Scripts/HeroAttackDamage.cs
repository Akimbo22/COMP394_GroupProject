
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class HeroAttackDamage : MonoBehaviour
{
    [Header("Player Reference")]
    public HeroController heroController;

    [Header("Attack Damage")]
    public int attack1Damage = 25;
    public int attack2Damage = 40;

    [Header("Attack Detection")]
    [Tooltip("Optional attack point. Leave empty to use player forward.")]
    public Transform attackOrigin;

    public float forwardOffset = 1.4f;
    public float hitHeight = 1.0f;
    public float hitRadius = 1.5f;

    [Tooltip("Everything is fine for initial testing.")]
    public LayerMask enemyLayers = ~0;

    [Header("Automatic Attack Damage")]
    [Tooltip("Enable to detect mouse/controller attack buttons.")]
    public bool autoDetectAttackButtons = true;

    [Tooltip("Disable for automatic timed hits. Enable after setting up Animation Events.")]
    public bool useAnimationEventTiming = false;

    [Header("Attack Timing")]
    public float attack1HitDelay = 0.25f;
    public float attack2HitDelay = 0.35f;

    public float attack1Cooldown = 0.8f;
    public float attack2Cooldown = 1.0f;

    [Header("Debug")]
    public bool enableDebugKey = true;
    public bool showDebugMessages = true;

    private CharacterController playerCharacterController;

    private float nextAttackTime = 0f;
    private int currentAttackType = 0;
    private bool attackAlreadyHit = false;

    private void Awake()
    {
        if (heroController == null)
        {
            heroController =
                GetComponentInParent<HeroController>();
        }

        if (heroController != null)
        {
            playerCharacterController =
                heroController.GetComponent<CharacterController>();
        }
        else
        {
            Debug.LogError(
                "HeroAttackDamage: HeroController not found! " +
                "Assign the Player in the Inspector.",
                this);
        }
    }

    private void Update()
    {
        if (heroController == null)
            return;

        if (heroController.IsDead)
            return;

        // DEBUG: J deals Attack1 damage.
        if (enableDebugKey && DebugAttackPressed())
        {
            DealDamage(attack1Damage);
            return;
        }

        if (!autoDetectAttackButtons)
            return;

        if (heroController.IsRolling)
            return;

        if (Time.time < nextAttackTime)
            return;

        // Match the existing HeroController attack controls.
        if (playerCharacterController != null &&
            !playerCharacterController.isGrounded)
        {
            return;
        }

        if (Attack1Pressed())
        {
            BeginAttack(
                1,
                attack1HitDelay,
                attack1Cooldown);
        }
        else if (Attack2Pressed())
        {
            BeginAttack(
                2,
                attack2HitDelay,
                attack2Cooldown);
        }
    }

    // --------------------------------------------------
    // START AN ATTACK
    // --------------------------------------------------

    private void BeginAttack(
        int attackType,
        float hitDelay,
        float cooldown)
    {
        currentAttackType = attackType;
        attackAlreadyHit = false;

        nextAttackTime =
            Time.time + Mathf.Max(0.01f, cooldown);

        if (showDebugMessages)
        {
            Debug.Log(
                "HeroAttackDamage: Started Attack " +
                attackType,
                this);
        }

        // Until Animation Events are configured,
        // apply damage after the attack's windup.
        if (!useAnimationEventTiming)
        {
            StartCoroutine(
                DelayedAttackHit(attackType, hitDelay));
        }
    }

    private IEnumerator DelayedAttackHit(
        int attackType,
        float delay)
    {
        yield return new WaitForSeconds(
            Mathf.Max(0f, delay));

        TryApplyAttackHit(attackType);
    }

    // --------------------------------------------------
    // OPTIONAL ANIMATION EVENTS
    // --------------------------------------------------

    public void Attack1Hit()
    {
        TryApplyAttackHit(1);
    }

    public void Attack2Hit()
    {
        TryApplyAttackHit(2);
    }

    // Prevent duplicate hits from one attack.
    private void TryApplyAttackHit(int attackType)
    {
        if (heroController == null)
            return;

        if (heroController.IsDead ||
            heroController.IsRolling)
            return;

        if (currentAttackType != attackType)
            return;

        if (attackAlreadyHit)
            return;

        attackAlreadyHit = true;

        int damage =
            attackType == 1
                ? attack1Damage
                : attack2Damage;

        DealDamage(damage);
    }

    // --------------------------------------------------
    // DETECT ENEMIES AND APPLY DAMAGE
    // --------------------------------------------------

    private void DealDamage(int damage)
    {
        if (heroController == null || heroController.IsDead)
            return;

        Transform playerRoot = heroController.transform;

        Vector3 hitCenter =
            GetHitCenter(playerRoot);

        Collider[] hits = Physics.OverlapSphere(
            hitCenter,
            hitRadius,
            enemyLayers,
            QueryTriggerInteraction.Collide);

        HashSet<EnemyController> damagedEnemies =
            new HashSet<EnemyController>();

        int enemiesHit = 0;

        foreach (Collider hit in hits)
        {
            EnemyController enemy =
                hit.GetComponentInParent<EnemyController>();

            if (enemy == null || enemy.IsDead)
                continue;

            if (!damagedEnemies.Add(enemy))
                continue;

            // Keep attacks in front of the player.
            Vector3 toEnemy =
                enemy.transform.position -
                playerRoot.position;

            toEnemy.y = 0f;

            if (toEnemy.sqrMagnitude > 0.001f)
            {
                float facing = Vector3.Dot(
                    playerRoot.forward,
                    toEnemy.normalized);

                if (facing < 0f)
                    continue;
            }

            enemy.TakeDamage(damage);
            enemiesHit++;

            if (showDebugMessages)
            {
                Debug.Log(
                    "Hero hit " + enemy.name +
                    " for " + damage + " damage. " +
                    "Enemy HP: " + enemy.CurrentHealth,
                    enemy);
            }
        }

        if (enemiesHit == 0 && showDebugMessages)
        {
            Debug.Log(
                "HeroAttackDamage: Attack missed. " +
                "Check range, facing, enemy collider " +
                "and EnemyController component.",
                this);
        }
    }

    // --------------------------------------------------
    // HIT AREA
    // --------------------------------------------------

    private Vector3 GetHitCenter(Transform playerRoot)
    {
        if (attackOrigin != null)
            return attackOrigin.position;

        return playerRoot.position +
               Vector3.up * hitHeight +
               playerRoot.forward * forwardOffset;
    }

    // Show the hit sphere in Scene view.
    private void OnDrawGizmosSelected()
    {
        Transform root =
            heroController != null
                ? heroController.transform
                : transform.root;

        Vector3 center = GetHitCenter(root);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, hitRadius);
    }

    // --------------------------------------------------
    // INPUT
    // --------------------------------------------------

#if ENABLE_INPUT_SYSTEM

    private static bool Attack1Pressed()
    {
        return
            (Mouse.current != null &&
             Mouse.current.leftButton.wasPressedThisFrame) ||
            (Gamepad.current != null &&
             Gamepad.current.buttonWest.wasPressedThisFrame);
    }

    private static bool Attack2Pressed()
    {
        return
            (Mouse.current != null &&
             Mouse.current.rightButton.wasPressedThisFrame) ||
            (Gamepad.current != null &&
             Gamepad.current.buttonNorth.wasPressedThisFrame);
    }

    private static bool DebugAttackPressed()
    {
        return Keyboard.current != null &&
               Keyboard.current.jKey.wasPressedThisFrame;
    }

#else

    private static bool Attack1Pressed()
    {
        return Input.GetMouseButtonDown(0);
    }

    private static bool Attack2Pressed()
    {
        return Input.GetMouseButtonDown(1);
    }

    private static bool DebugAttackPressed()
    {
        return Input.GetKeyDown(KeyCode.J);
    }

#endif
}

