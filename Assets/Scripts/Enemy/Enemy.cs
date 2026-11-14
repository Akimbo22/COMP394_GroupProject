using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [HideInInspector] public GameObject player;

    [Header("Enemy Stats")]
    [SerializeField] private string enemyName;
    [SerializeField] private uint maxHealth;
    [SerializeField] private uint health;
    public bool isDead = false;
    [SerializeField] protected float awarenessRange;
    [SerializeField] private bool delayDespawnByAnim = true;

    //[Header("Death VFX")]
    //[SerializeField] private GameObject deathExplosionPrefab;
    //[SerializeField] private float deathExplosionSizeModifier = 1.0f;

    private void OnValidate() { health = maxHealth; }

    [SerializeField] private List<bool> damageConditions;

    private void Awake()
    {
        health = maxHealth;
    }

    private void Start()
    {
        player = FindAnyObjectByType<HeroController>().GameObject();
    }

    private void FixedUpdate()
    {
        if (health > 0) EnemyUpdate();
        else OnDeath();
    }

    public virtual void EnemyUpdate() { }

    public bool TriggerConditions()
    {
        for (int i = 0; i < damageConditions.Count; i++)
        {
            if (!damageConditions[i]) return false;
        }
        return true;
    }

    public void AddTriggerCondition(bool boolMethod)
    {
        damageConditions.Add(boolMethod);
    }

    public void AddTriggerCondition(bool[] boolMethods)
    {
        for (int i = 0;i < damageConditions.Count;i++)
        {
            AddTriggerCondition(boolMethods[i]);
        }
    }

    public virtual void OnDeath()
    {
        //Instantiate(deathExplosionPrefab, gameObject.transform.position, Quaternion.identity);
        isDead = true;
    }
}
