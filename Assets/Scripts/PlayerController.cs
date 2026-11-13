
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerHealth))]
public class HeroController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Tooltip("Leave empty to use Camera.main")]
    [SerializeField] private Transform cameraTransform;

    [SerializeField] private PlayerHealth playerHealth;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 5f;
    [SerializeField] private float turnSmoothTime = 0.08f;
    [SerializeField] private float speedDampTime = 0.1f;

    [Header("Jump & Gravity")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedGraceTime = 0.1f;

    [Header("Roll")]
    [SerializeField] private float rollSpeed = 7f;
    [SerializeField] private float rollDuration = 0.6f;

    [Header("Combat / Animations")]
    [SerializeField] private float attack1Duration = 0.8f;
    [SerializeField] private float attack2Duration = 1.0f;
    [SerializeField] private float hitStunDuration = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugKeys = true;

    // ANIMATOR PARAMETERS
    private static readonly int SpeedHash =
        Animator.StringToHash("Speed");

    private static readonly int YVelocityHash =
        Animator.StringToHash("YVelocity");

    private static readonly int IsGroundedHash =
        Animator.StringToHash("IsGrounded");

    private static readonly int IsDeadHash =
        Animator.StringToHash("IsDead");

    private static readonly int JumpHash =
        Animator.StringToHash("Jump");

    private static readonly int RollHash =
        Animator.StringToHash("Roll");

    private static readonly int Attack1Hash =
        Animator.StringToHash("Attack1");

    private static readonly int Attack2Hash =
        Animator.StringToHash("Attack2");

    private static readonly int HitHash =
        Animator.StringToHash("Hit");

    private static readonly int DieHash =
        Animator.StringToHash("Die");

    // STATE
    private CharacterController controller;

    private float verticalVelocity;
    private float lastGroundedTime;
    private float turnVelocity;

    private bool isDead;

    private float rollTimer;
    private float attackTimer;
    private float stunTimer;

    private Vector3 rollDirection;

    public bool IsDead =>
        playerHealth != null ? playerHealth.IsDead : isDead;

    public bool IsRolling => rollTimer > 0f;

    public int CurrentHealth =>
        playerHealth != null ? playerHealth.CurrentHealth : 0;

    private bool IsGroundedForAnim =>
        Time.time - lastGroundedTime <= groundedGraceTime;

    private bool CanAct =>
        !isDead &&
        rollTimer <= 0f &&
        attackTimer <= 0f &&
        stunTimer <= 0f;

    // SETUP
    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogError(
                "HeroController: no Animator found.", this);
        }

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        lastGroundedTime = Time.time;
    }

    private void OnEnable()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.Damaged += OnHealthDamaged;
            playerHealth.Died += OnHealthDied;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.Damaged -= OnHealthDamaged;
            playerHealth.Died -= OnHealthDied;
        }
    }

    // MAIN LOOP
    private void Update()
    {
        if (animator == null ||
            controller == null ||
            !controller.enabled)
            return;

        TickTimers();

        if (controller.isGrounded)
            lastGroundedTime = Time.time;

        Vector3 horizontalVelocity = Vector3.zero;
        float animSpeed = 0f;

        if (!isDead)
        {
            if (CanAct)
                HandleActionInput();

            if (rollTimer > 0f)
            {
                horizontalVelocity =
                    rollDirection * rollSpeed;
            }
            else if (CanAct)
            {
                horizontalVelocity =
                    HandleLocomotion(out animSpeed);
            }

            if (enableDebugKeys)
            {
                if (DebugHitPressed())
                    TakeDamage(25);

                if (DebugDiePressed())
                    Die();
            }
        }

        ApplyGravity();

        Vector3 move =
            horizontalVelocity + Vector3.up * verticalVelocity;

        controller.Move(move * Time.deltaTime);

        UpdateAnimator(animSpeed);
    }

    // MOVEMENT
    private Vector3 HandleLocomotion(out float animSpeed)
    {
        Vector2 input =
            Vector2.ClampMagnitude(ReadMoveInput(), 1f);

        bool running = RunHeld();

        Vector3 moveDir = Vector3.zero;

        if (input.sqrMagnitude > 0.01f)
        {
            float targetAngle = GetTargetAngle(input);

            float angle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetAngle,
                ref turnVelocity,
                turnSmoothTime);

            transform.rotation =
                Quaternion.Euler(0f, angle, 0f);

            moveDir =
                Quaternion.Euler(0f, targetAngle, 0f)
                * Vector3.forward;
        }

        float targetSpeed =
            running ? runSpeed : walkSpeed;

        // 0 = Idle, 0.5 = Walk, 1 = Run
        animSpeed =
            input.magnitude * (running ? 1f : 0.5f);

        return moveDir * (targetSpeed * input.magnitude);
    }

    private float GetTargetAngle(Vector2 input)
    {
        float camYaw = cameraTransform != null
            ? cameraTransform.eulerAngles.y
            : 0f;

        return Mathf.Atan2(input.x, input.y)
            * Mathf.Rad2Deg + camYaw;
    }

    // GRAVITY
    private void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;
    }

    // JUMP
    private void StartJump()
    {
        verticalVelocity =
            Mathf.Sqrt(jumpHeight * -2f * gravity);

        lastGroundedTime = float.NegativeInfinity;

        animator.SetTrigger(JumpHash);
    }

    // PLAYER ACTIONS
    private void HandleActionInput()
    {
        bool grounded = IsGroundedForAnim;

        if (grounded && JumpPressed())
        {
            StartJump();
            return;
        }

        if (!grounded)
            return;

        if (RollPressed())
        {
            StartRoll();
        }
        else if (Attack1Pressed())
        {
            StartAttack(Attack1Hash, attack1Duration);
        }
        else if (Attack2Pressed())
        {
            StartAttack(Attack2Hash, attack2Duration);
        }
    }

    // ROLL
    private void StartRoll()
    {
        Vector2 input = ReadMoveInput();

        if (input.sqrMagnitude > 0.01f)
        {
            float targetAngle = GetTargetAngle(input);

            rollDirection =
                Quaternion.Euler(0f, targetAngle, 0f)
                * Vector3.forward;

            transform.rotation =
                Quaternion.Euler(0f, targetAngle, 0f);
        }
        else
        {
            rollDirection = transform.forward;
        }

        rollTimer = rollDuration;

        animator.SetTrigger(RollHash);
    }

    // ATTACKS
    private void StartAttack(int triggerHash, float duration)
    {
        attackTimer = duration;
        animator.SetTrigger(triggerHash);
    }

    // DAMAGE - CONNECTED TO PLAYER HEALTH
    public void TakeDamage(int amount)
    {
        if (playerHealth != null)
            playerHealth.TakeDamage(amount);
    }

    // INSTANT DEATH
    public void Die()
    {
        if (playerHealth != null)
            playerHealth.Kill();
    }

    // HIT ANIMATION
    private void OnHealthDamaged()
    {
        if (isDead)
            return;

        rollTimer = 0f;
        attackTimer = 0f;
        stunTimer = hitStunDuration;

        ResetActionTriggers();

        if (animator != null)
            animator.SetTrigger(HitHash);
    }

    // DEATH ANIMATION
    private void OnHealthDied()
    {
        if (isDead)
            return;

        isDead = true;

        rollTimer = 0f;
        attackTimer = 0f;
        stunTimer = 0f;

        ResetActionTriggers();

        if (animator != null)
        {
            animator.ResetTrigger(HitHash);
            animator.SetBool(IsDeadHash, true);
            animator.SetTrigger(DieHash);
        }
    }

    private void ResetActionTriggers()
    {
        if (animator == null)
            return;

        animator.ResetTrigger(JumpHash);
        animator.ResetTrigger(RollHash);
        animator.ResetTrigger(Attack1Hash);
        animator.ResetTrigger(Attack2Hash);
    }

    // RESET GRAVITY AFTER RESPAWNING
    public void ResetVerticalVelocity()
    {
        verticalVelocity = -2f;
        lastGroundedTime = Time.time;
    }

    // TIMERS
    private void TickTimers()
    {
        float dt = Time.deltaTime;

        if (rollTimer > 0f)
            rollTimer -= dt;

        if (attackTimer > 0f)
            attackTimer -= dt;

        if (stunTimer > 0f)
            stunTimer -= dt;
    }

    // ANIMATOR
    private void UpdateAnimator(float animSpeed)
    {
        animator.SetFloat(
            SpeedHash, animSpeed, speedDampTime, Time.deltaTime);

        animator.SetFloat(YVelocityHash, verticalVelocity);

        animator.SetBool(
            IsGroundedHash, IsGroundedForAnim);
    }

    // NEW INPUT SYSTEM
#if ENABLE_INPUT_SYSTEM

    private static Vector2 ReadMoveInput()
    {
        Vector2 v = Vector2.zero;

        var kb = Keyboard.current;

        if (kb != null)
        {
            if (kb.dKey.isPressed) v.x += 1f;
            if (kb.aKey.isPressed) v.x -= 1f;
            if (kb.wKey.isPressed) v.y += 1f;
            if (kb.sKey.isPressed) v.y -= 1f;
        }

        var pad = Gamepad.current;

        if (pad != null && v == Vector2.zero)
            v = pad.leftStick.ReadValue();

        return v;
    }

    private static bool RunHeld() =>
        (Keyboard.current != null &&
         Keyboard.current.leftShiftKey.isPressed) ||
        (Gamepad.current != null &&
         Gamepad.current.leftTrigger.isPressed);

    private static bool JumpPressed() =>
        (Keyboard.current != null &&
         Keyboard.current.spaceKey.wasPressedThisFrame) ||
        (Gamepad.current != null &&
         Gamepad.current.buttonSouth.wasPressedThisFrame);

    private static bool RollPressed() =>
        (Keyboard.current != null &&
         Keyboard.current.leftCtrlKey.wasPressedThisFrame) ||
        (Gamepad.current != null &&
         Gamepad.current.buttonEast.wasPressedThisFrame);

    private static bool Attack1Pressed() =>
        (Mouse.current != null &&
         Mouse.current.leftButton.wasPressedThisFrame) ||
        (Gamepad.current != null &&
         Gamepad.current.buttonWest.wasPressedThisFrame);

    private static bool Attack2Pressed() =>
        (Mouse.current != null &&
         Mouse.current.rightButton.wasPressedThisFrame) ||
        (Gamepad.current != null &&
         Gamepad.current.buttonNorth.wasPressedThisFrame);

    private static bool DebugHitPressed() =>
        Keyboard.current != null &&
        Keyboard.current.hKey.wasPressedThisFrame;

    private static bool DebugDiePressed() =>
        Keyboard.current != null &&
        Keyboard.current.kKey.wasPressedThisFrame;

#else

    // LEGACY INPUT SYSTEM
    private static Vector2 ReadMoveInput() =>
        new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"));

    private static bool RunHeld() =>
        Input.GetKey(KeyCode.LeftShift);

    private static bool JumpPressed() =>
        Input.GetKeyDown(KeyCode.Space);

    private static bool RollPressed() =>
        Input.GetKeyDown(KeyCode.LeftControl);

    private static bool Attack1Pressed() =>
        Input.GetMouseButtonDown(0);

    private static bool Attack2Pressed() =>
        Input.GetMouseButtonDown(1);

    private static bool DebugHitPressed() =>
        Input.GetKeyDown(KeyCode.H);

    private static bool DebugDiePressed() =>
        Input.GetKeyDown(KeyCode.K);

#endif
}
