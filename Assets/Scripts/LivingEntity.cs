using UnityEngine;

public class LivingEntity : MonoBehaviour,IDamageable
{
    [SerializeField]
    float maxHp;   // 최대 체력
    float currentHp;  // 현재 체력
    float damage;  // 공격력
    public virtual void OnDamage(float damage, Vector3 hitPoint, Vector3 hitNormal) { }  //데미지 입었을 때

    //죽었을 때
    public virtual void Die() { 
        
    }

    // 피회복
    public virtual void RestoreHealth(float heal)
    {
        if (currentHp + heal >= maxHp)
        {
            currentHp = maxHp;
        }
        else
        {
            currentHp += heal;
        }
    }

    // 생명체 활성화 시 상태 리셋
    protected virtual void OnEnable() {
        currentHp = maxHp;
    }
}
