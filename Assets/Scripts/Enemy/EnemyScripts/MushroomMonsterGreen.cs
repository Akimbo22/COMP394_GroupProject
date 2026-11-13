using UnityEngine;

[RequireComponent(typeof(Animation))]
public class MushroomMonsterGreen : Enemy
{
    private enum MushroomState
    {
        Idle,
        Chase,
        Attacking,
        Damaged,
        Dead
    }

    [Header("Mushroom Stats")]
    [SerializeField] private float attackRange = 0.3f;
    [SerializeField] private float attackSpeedDelay = 3f;
    [SerializeField] private float attackSpeedDelayTick = 0;
    [SerializeField] private uint attackDamage = 1;
    [SerializeField] private MushroomState state;
    [SerializeField] private MushroomState priorState;

    [Header("Mushroom Animations")]
    [SerializeField] private AnimationClip idle;
    [SerializeField] private AnimationClip chase;
    [SerializeField] private AnimationClip attack;
    [SerializeField] private AnimationClip damage;
    [SerializeField] private AnimationClip death;

    [SerializeField] private AnimationClip currentAnim;
    [SerializeField] private AnimationClip nextAnim;

    private Animation animation;

    private void Awake()
    {
        state = MushroomState.Idle;
        animation = GetComponent<Animation>();

        animation.clip = idle;
        animation.Play();
    }

    public override void EnemyUpdate()
    {
        base.EnemyUpdate();

        currentAnim = animation.clip;
        priorState = state;

        float distToPlyr = (
            Mathf.Abs(player.transform.position.x - gameObject.transform.position.x) +
            Mathf.Abs(player.transform.position.z - gameObject.transform.position.z));

        if (attackSpeedDelayTick > 0) attackSpeedDelayTick -= Time.fixedDeltaTime;

        if (distToPlyr <= awarenessRange && distToPlyr > attackRange)
        {
            state = MushroomState.Chase;
            gameObject.transform.rotation = Quaternion.LookRotation(new Vector3(player.transform.position.x, 0, player.transform.position.z), Vector3.up);
        }
        else if (distToPlyr <= attackRange)
        {
            if (attackSpeedDelayTick <= 0) Attack();
            gameObject.transform.rotation = Quaternion.LookRotation(new Vector3(player.transform.position.x, 0, player.transform.position.z), Vector3.up);
        }
        else
        {
            state = MushroomState.Idle;
        }

        if (state != priorState)
        {
            SetAnimation(state);
        }

        SetAnimation(state);

        if (nextAnim != null && currentAnim != nextAnim)
        {
            if (!(currentAnim != idle || currentAnim != chase)) animation.PlayQueued(nextAnim.name);
            else if (nextAnim == idle && currentAnim != chase || nextAnim == chase && currentAnim != idle) animation.PlayQueued(nextAnim.name);
            else
            {
                animation.clip = nextAnim;
                animation.Play(nextAnim.name);
            }
        }
        else if (currentAnim == nextAnim && (currentAnim == idle || currentAnim == chase) && (animation.clip != idle || animation.clip != chase))
        {
            animation.clip = nextAnim;
            animation.PlayQueued(nextAnim.name);
        }
    }

    public void Attack()
    {
        state = MushroomState.Attacking;
        SetAnimation(MushroomState.Attacking);
        Debug.Log("Attacking");
        attackSpeedDelayTick = attackSpeedDelay;
    }

    public override void OnDeath()
    {
        SetAnimation(MushroomState.Dead);
        base.OnDeath();
    }

    private void SetAnimation(MushroomState _state)
    {
        switch (state)
        {
            case MushroomState.Chase:
                nextAnim = chase;
                break;
            case MushroomState.Attacking:
                nextAnim = attack;
                break;
            case MushroomState.Damaged:
                nextAnim = damage;
                break;
            case MushroomState.Dead:
                nextAnim = death;
                break;
            default:
                nextAnim = idle;
                break;
        }
    }
}
