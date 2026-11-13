using Unity.VisualScripting;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [HideInInspector] public GameObject player;

    [Header("Enemy Stats")]
    [SerializeField] private string enemyName;
    [SerializeField] private uint maxHealth;
    [SerializeField] private uint health;
    [SerializeField] private bool isDead = false;
    [SerializeField] protected float awarenessRange;
    [SerializeField] private bool delayDespawnByAnim = true;

    //[Header("Death VFX")]
    //[SerializeField] private GameObject deathExplosionPrefab;
    //[SerializeField] private float deathExplosionSizeModifier = 1.0f;

    private void OnValidate() { health = maxHealth; }

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

    public virtual void OnDeath()
    {
        //Instantiate(deathExplosionPrefab, gameObject.transform.position, Quaternion.identity);
        isDead = true;
    }
}
