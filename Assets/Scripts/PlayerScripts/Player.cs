using UnityEngine;
using UnityEngine.UI;

public class Player : LivingEntity
{
    [SerializeField] Slider HpSlider;
    [SerializeField] Animator anim;

    private void Start()
    {
        HpSlider.maxValue = maxHp;
        HpSlider.value = currentHp;
    }

    //데미지를 입었을 때
    public override void OnDamage(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        base.OnDamage(damage, hitPoint, hitDirection);
        HpSlider.value = currentHp;
        if (dead) { anim.SetTrigger("Die"); return; }
        anim.SetTrigger("Hit");
    }

    protected override void OnEnable()
    {
        maxHp = 100;
        base.OnEnable();
    }
}
