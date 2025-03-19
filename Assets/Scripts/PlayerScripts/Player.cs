using UnityEngine;
using UnityEngine.UI;

public class Player : LivingEntity
{
    public PlayerUI playerUI;
    [SerializeField] PlayerData playerData;
    public bool Ishit; // 데미지를 입었는가?
    private void Awake()
    {
        OnEnable();
        playerUI.PlayerHp.maxValue = maxHp;
        playerUI.PlayerHp.value = currentHp;
    }
    //데미지를 입었을 때
    public override void OnDamage(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        base.OnDamage(damage, hitPoint, hitDirection);
        playerUI.PlayerHp.value = currentHp;
        Ishit = true;
    }

    protected override void OnEnable()
    {
        maxHp = playerData.Hp;
        damage = playerData.Damage;
        base.OnEnable();
    }
}
