using System.Collections;
using UnityEngine;

public class PlayerWeapon : Weapon
{
    [SerializeField] PlayerSetting player;
    

    //private void OnTriggerEnter(Collider other)
    //{
    //    if(other.CompareTag("Enemy"))
    //    {
    //        if (!canTrigger) { return; }
    //        Enemy enemy = other.GetComponent<Enemy>();

    //        Vector3 hitPoint = other.ClosestPoint(transform.position);
    //        Vector3 hitnormal = transform.position - other.transform.position;

    //        player.playerUI.ShowEnemyInfoUI();
    //        enemy.OnDamage(player.damage, hitPoint, hitnormal);
    //        player.playerUI.EnemyName.text = "" + enemy.gameObject.name;
    //        player.playerUI.EnemyHpUI.value = enemy.currentHp / enemy.maxHp;
    //        canTrigger = false;
    //        StartCoroutine(ResetTrigger());
    //    }
    //}


}
