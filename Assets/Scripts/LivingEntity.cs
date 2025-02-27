using UnityEngine;

public class LivingEntity : MonoBehaviour,IDamageable
{
    [SerializeField]
    float maxHp;   // �ִ� ü��
    float currentHp;  // ���� ü��
    float damage;  // ���ݷ�
    public virtual void OnDamage(float damage, Vector3 hitPoint, Vector3 hitNormal) { }  //������ �Ծ��� ��

    //�׾��� ��
    public virtual void Die() { 
        
    }

    // ��ȸ��
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

    // ����ü Ȱ��ȭ �� ���� ����
    protected virtual void OnEnable() {
        currentHp = maxHp;
    }
}
