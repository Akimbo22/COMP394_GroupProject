using UnityEngine;
using UnityEngine.AI;

public class MushroomCyclops : Enemy
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
    [SerializeField] private float attackSpeedDelay = 5f;
    [SerializeField] private float attackSpeedDelayTick = 0;
    [SerializeField] public uint attackDamage = 1;
    [SerializeField] private MushroomState state;
    [SerializeField] private MushroomState priorState;
    private AttackTrigger attackTrigger;
    private bool secondAttackFlag = false;

    private Animator anim;
    private MushMonAnimTrigHandler animTrigHandler;

    [Header("Navigation")]
    private NavMeshAgent agent;
    [SerializeField] private Vector3 targetPos;
    [SerializeField] private Vector3 lookTargetPos;
    [SerializeField] private Vector3 home;
    [SerializeField] private float maxWanderDist = 10.0f;
    [SerializeField] private float homeRestDist = 1.5f;
    [SerializeField] private bool isReturningHome = false;

    private void Awake()
    {
        state = MushroomState.Idle;
        anim = GetComponent<Animator>();
        animTrigHandler = anim.GetBehaviour<MushMonAnimTrigHandler>();
        agent = GetComponent<NavMeshAgent>();
        //agent.updateRotation = false;
        home = gameObject.transform.position;

        attackTrigger = GetComponentInChildren<AttackTrigger>();
        attackTrigger.SetAttackDamage(attackDamage);

        //AddTriggerCondition(CanHit());
        AddTriggerCondition(secondAttackFlag);
    }

    public override void EnemyUpdate()
    {
        //Debug.Log($"{secondAttackFlag}, {CanHit()} : {TriggerConditions()}");
        if (secondAttackFlag) attackTrigger.TriggerAttackFlag();
        secondAttackFlag = false;
        base.EnemyUpdate();

        priorState = state;

        float distToPlyr = (
            Mathf.Abs(player.transform.position.x - gameObject.transform.position.x) +
            Mathf.Abs(player.transform.position.z - gameObject.transform.position.z));

        if (attackSpeedDelayTick > 0) attackSpeedDelayTick -= Time.fixedDeltaTime;

        if (distToPlyr <= awarenessRange && distToPlyr > attackRange)
        {
            targetPos = player.transform.position;
            isReturningHome = false;
            state = MushroomState.Chase;
        }
        else if (distToPlyr <= attackRange)
        {
            targetPos = gameObject.transform.position;
            if (attackSpeedDelayTick <= 0) Attack();
        }
        else
        {
            state = MushroomState.Idle;
        }

        if (Mathf.Abs(gameObject.transform.position.x - home.x) +
            Mathf.Abs(gameObject.transform.position.z - home.z) > maxWanderDist)
        {
            targetPos = new Vector3(home.x, 0, home.z);
            isReturningHome = true;
            state = MushroomState.Chase;
        }
        if (isReturningHome && Mathf.Abs(gameObject.transform.position.x - home.x) +
            Mathf.Abs(gameObject.transform.position.z - home.z) <= homeRestDist)
        {
            isReturningHome = false;
            targetPos = gameObject.transform.position;
            state = MushroomState.Idle;
        }

        if (state != priorState)
        {
            SetAnimation(state);
        }

        if (state == MushroomState.Chase && !animTrigHandler.isActiveJumping)
        {
            agent.SetDestination(targetPos);
        }

        if (!animTrigHandler.isActiveJumping && agent.updateRotation == false)
        {
            lookTargetPos = targetPos;
            if (distToPlyr <= attackRange)
            {
                lookTargetPos = new Vector3(player.transform.position.x, 0, player.transform.position.z);
            }
            gameObject.transform.rotation = Quaternion.LookRotation(lookTargetPos, Vector3.up);
        }
    }

    public void Attack()
    {
        state = MushroomState.Attacking;
        SetAnimation(MushroomState.Attacking);
        attackSpeedDelayTick = attackSpeedDelay;
        secondAttackFlag = true;
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
                anim.SetBool("IsRunning", true);
                break;
            case MushroomState.Attacking:
                anim.SetBool("IsRunning", false);
                anim.SetTrigger("Attack");
                break;
            case MushroomState.Damaged:
                anim.SetBool("IsRunning", false);
                break;
            case MushroomState.Dead:
                agent.SetDestination(gameObject.transform.position);
                anim.SetBool("IsRunning", false);
                anim.SetTrigger("Die");
                break;
            default:
                anim.SetBool("IsRunning", false);
                break;
        }
    }

    public bool CanHit()
    {
        if (animTrigHandler.attackTrigger)
        {
            animTrigHandler.attackTrigger = false;
            return true;
        }
        return false;
    }
}
