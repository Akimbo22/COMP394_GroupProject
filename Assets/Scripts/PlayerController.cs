using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Third-person character controller driving an Animator with:
/// Idle / Walk / Run (blend tree), Jump, Fall, Roll, Attack1, Attack2, Hit, Death.
///
/// File name MUST be HeroController.cs (same as the class name).
/// Works with the new Input System, the old Input Manager, or both.
///
/// Controls: WASD move | Shift run | Space jump | Left Ctrl roll
///           Left click attack 1 | Right click attack 2
///           Debug: H = take damage, K = die
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class HeroController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [Tooltip("Leave empty to use Camera.main")]
    [SerializeField] private Transform cameraTransform;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 5f;
    [SerializeField] private float turnSmoothTime = 0.08f;
    [Tooltip("How smoothly the Speed animator value changes")]
    [SerializeField] private float speedDampTime = 0.1f;

    [Header("Jump & Gravity")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f;
    [Tooltip("Time after leaving the ground before we count as falling")]
    [SerializeField] private float groundedGraceTime = 0.1f;

    [Header("Roll")]
    [SerializeField] private float rollSpeed = 7f;
    [Tooltip("Set to roughly the length of your roll clip")]
    [SerializeField] private float rollDuration = 0.6f;

    [Header("Combat")]
    [Tooltip("Set to roughly the length of your attack clips")]
    [SerializeField] private float attack1Duration = 0.8f;
    [SerializeField] private float attack2Duration = 1.0f;
    [SerializeField] private float hitStunDuration = 0.5f;
    [SerializeField] private int maxHealth = 100;

    [Header("Debug")]
    [SerializeField] private bool enableDebugKeys = true;

    // ---- Animator parameter hashes ----
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int YVelocityHash = Animator.StringToHash("YVelocity");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int RollHash = Animator.StringToHash("Roll");
    private static readonly int Attack1Hash = Animator.StringToHash("Attack1");
    private static readonly int Attack2Hash = Animator.StringToHash("Attack2");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DieHash = Animator.StringToHash("Die");

    // ---- State ----
    private CharacterController controller;
    private int currentHealth;
    private float verticalVelocity;
    private float lastGroundedTime;
    private float turnVelocity;

    private bool isDead;
    private float rollTimer;
    private float attackTimer;
    private float stunTimer;
    private Vector3 rollDirection;

    public bool IsDead => isDead;
    public bool IsRolling => rollTimer > 0f;
    public int CurrentHealth => currentHealth;

    private bool IsGroundedForAnim => Time.time - lastGroundedTime <= groundedGraceTime;
    private bool CanAct => !isDead && rollTimer <= 0f && attackTimer <= 0f && stunTimer <= 0f;

    // --------------------------------------------------------------------
    // Setup
    // --------------------------------------------------------------------
    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator == null)
            Debug.LogError("HeroController: no Animator found. Assign one in the Inspector.", this);

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        currentHealth = maxHealth;
        lastGroundedTime = Time.time;
    }

    // --------------------------------------------------------------------
    // Main loop
    // --------------------------------------------------------------------
    private void Update()
    {
        if (animator == null) return;

        TickTimers();

        if (controller.isGrounded) lastGroundedTime = Time.time;

        Vector3 horizontalVelocity = Vector3.zero;
        float animSpeed = 0f;

        if (!isDead)
        {
            if (CanAct) HandleActionInput();

            if (rollTimer > 0f)
            {
                horizontalVelocity = rollDirection * rollSpeed;
            }
            else if (CanAct)
            {
                horizontalVelocity = HandleLocomotion(out animSpeed);
            }

            if (enableDebugKeys)
            {
                if (DebugHitPressed()) TakeDamage(25);
                if (DebugDiePressed()) Die();
            }
        }

        ApplyGravity();

        Vector3 move = horizontalVelocity + Vector3.up * verticalVelocity;
        controller.Move(move * Time.deltaTime);

        UpdateAnimator(animSpeed);
    }

    // --------------------------------------------------------------------
    // Locomotion
    // --------------------------------------------------------------------
    private Vector3 HandleLocomotion(out float animSpeed)
    {
        Vector2 input = Vector2.ClampMagnitude(ReadMoveInput(), 1f);
        bool running = RunHeld();

        Vector3 moveDir = Vector3.zero;

        if (input.sqrMagnitude > 0.01f)
        {
            float targetAngle = GetTargetAngle(input);
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        }

        float targetSpeed = running ? runSpeed : walkSpeed;

        // Blend tree thresholds: 0 = idle, 0.5 = walk, 1 = run
        animSpeed = input.magnitude * (running ? 1f : 0.5f);

        return moveDir * (targetSpeed * input.magnitude);
    }

    private float GetTargetAngle(Vector2 input)
    {
        float camYaw = cameraTransform != null ? cameraTransform.eulerAngles.y : 0f;
        return Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg + camYaw;
    }

    // --------------------------------------------------------------------
    // Jump / gravity
    // --------------------------------------------------------------------
    private void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f; // keeps the controller stuck to the ground

        verticalVelocity += gravity * Time.deltaTime;
    }

    private void StartJump()
    {
        verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        lastGroundedTime = float.NegativeInfinity; // airborne immediately
        animator.SetTrigger(JumpHash);
    }

    // --------------------------------------------------------------------
    // Actions: jump, roll, attacks
    // --------------------------------------------------------------------
    private void HandleActionInput()
    {
        bool grounded = IsGroundedForAnim;

        if (grounded && JumpPressed())
        {
            StartJump();
            return;
        }

        if (!grounded) return; // roll/attack only on the ground

        if (RollPressed())
            StartRoll();
        else if (Attack1Pressed())
            StartAttack(Attack1Hash, attack1Duration);
        else if (Attack2Pressed())
            StartAttack(Attack2Hash, attack2Duration);
    }

    private void StartRoll()
    {
        Vector2 input = ReadMoveInput();

        if (input.sqrMagnitude > 0.01f)
        {
            float targetAngle = GetTargetAngle(input);
            rollDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);
        }
        else
        {
            rollDirection = transform.forward;
        }

        rollTimer = rollDuration;
        animator.SetTrigger(RollHash);
    }

    private void StartAttack(int triggerHash, float duration)
    {
        attackTimer = duration;
        animator.SetTrigger(triggerHash);
    }

    // --------------------------------------------------------------------
    // Damage / death
    // --------------------------------------------------------------------

    public void TakeDamage(int amount)
    {
        if (isDead) return;
        if (IsRolling) return; // i-frames while rolling (delete this line to disable)

        currentHealth = Mathf.Max(0, currentHealth - amount);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        rollTimer = 0f;
        attackTimer = 0f;
        stunTimer = hitStunDuration;
        ResetActionTriggers();
        animator.SetTrigger(HitHash);
    }

    public void Die()
    {
        if (isDead) return;

        isDead = true;
        rollTimer = attackTimer = stunTimer = 0f;
        ResetActionTriggers();
        animator.ResetTrigger(HitHash);
        animator.SetBool(IsDeadHash, true);
        animator.SetTrigger(DieHash);
    }

    private void ResetActionTriggers()
    {
        animator.ResetTrigger(JumpHash);
        animator.ResetTrigger(RollHash);
        animator.ResetTrigger(Attack1Hash);
        animator.ResetTrigger(Attack2Hash);
    }

    // --------------------------------------------------------------------
    // Helpers
    // --------------------------------------------------------------------
    private void TickTimers()
    {
        float dt = Time.deltaTime;
        if (rollTimer > 0f) rollTimer -= dt;
        if (attackTimer > 0f) attackTimer -= dt;
        if (stunTimer > 0f) stunTimer -= dt;
    }

    private void UpdateAnimator(float animSpeed)
    {
        animator.SetFloat(SpeedHash, animSpeed, speedDampTime, Time.deltaTime);
        animator.SetFloat(YVelocityHash, verticalVelocity);
        animator.SetBool(IsGroundedHash, IsGroundedForAnim);
    }

    // --------------------------------------------------------------------
    // Input (new Input System when available, otherwise legacy Input Manager)
    // --------------------------------------------------------------------
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
        (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed) ||
        (Gamepad.current != null && Gamepad.current.leftTrigger.isPressed);

    private static bool JumpPressed() =>
        (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
        (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

    private static bool RollPressed() =>
        (Keyboard.current != null && Keyboard.current.leftCtrlKey.wasPressedThisFrame) ||
        (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);

    private static bool Attack1Pressed() =>
        (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
        (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame);

    private static bool Attack2Pressed() =>
        (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) ||
        (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame);

    private static bool DebugHitPressed() =>
        Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame;

    private static bool DebugDiePressed() =>
        Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame;
#else
    private static Vector2 ReadMoveInput() =>
        new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
 
    private static bool RunHeld() => Input.GetKey(KeyCode.LeftShift);
    private static bool JumpPressed() => Input.GetKeyDown(KeyCode.Space);
    private static bool RollPressed() => Input.GetKeyDown(KeyCode.LeftControl);
    private static bool Attack1Pressed() => Input.GetMouseButtonDown(0);
    private static bool Attack2Pressed() => Input.GetMouseButtonDown(1);
    private static bool DebugHitPressed() => Input.GetKeyDown(KeyCode.H);
    private static bool DebugDiePressed() => Input.GetKeyDown(KeyCode.K);
#endif
}