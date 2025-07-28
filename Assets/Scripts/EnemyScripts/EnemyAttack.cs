using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [SerializeField] Collider [] weapons;
    [SerializeField] public Attack[] attacks;
    public Attack currentPattern;
    public Enemy enemy;
    public int currentAnimationIndex;

    private void Start()
    {
        enemy = GetComponent<Enemy>();
    }

    public void AE_EnemyAttackStart()
    {
        foreach (Collider weapon in weapons)
        {
            weapon.enabled = true;
        }
    }

    public void AE_EnemyAttackEnd()
    {
        foreach (Collider weapon in weapons)
        {
            weapon.enabled = false;
        }
    }

    public void AE_EnemyParryAndAttack()
    {
        enemy.anim.SetInteger("pattern", (int)Random.value);
    }
}
