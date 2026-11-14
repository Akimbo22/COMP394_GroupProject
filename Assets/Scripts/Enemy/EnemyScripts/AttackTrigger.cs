using System.Collections.Generic;
using UnityEngine;

public class AttackTrigger : MonoBehaviour
{
    [SerializeField] private Enemy enemy;
    [SerializeField] private uint attackDamage;
    [SerializeField] private float coolDownTime = 1.5f;
    [SerializeField] private float coolDownTick;
    [SerializeField] bool attackFlag;

    private void OnValidate()
    {
        enemy = GetComponentInParent<Enemy>();
        coolDownTick = coolDownTime;
    }

    private void FixedUpdate()
    {
        if (enemy.isDead) return;
        if (coolDownTick > 0) coolDownTick -= Time.fixedDeltaTime;
    }

    public void SetAttackDamage(uint _attackDamage) { attackDamage = _attackDamage; }
    public void TriggerAttackFlag() { attackFlag = true; }

    public void OnTriggerStay(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))// && enemy.TriggerConditions())
        {
            //Debug.Log("Player Check 1");
            PlayerHealth player = other.gameObject.GetComponent<PlayerHealth>();

            //if (player != null) Debug.Log("Player Check")


            if (player != null && attackDamage > 0 && attackFlag) //coolDownTick <= 0)
            {
                //Debug.Log("Damage Player");
                player.TakeDamage((int)attackDamage);
                coolDownTick = coolDownTime;
                attackFlag = false;
            }
        }
    }
}
