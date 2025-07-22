using System.Collections;
using UnityEngine;

public class PlayerWeapon : Weapon
{

    //private void OnTriggerEnter(Collider other)
    //{
    //    if(other.CompareTag("Enemy"))
    //    {
    //        if (!canTrigger) { return; }
    //        Enemy enemy = other.GetComponent<Enemy>();

    //        Vector3 hitPoint = other.ClosestPoint(transform.position);
    //        Vector3 hitnormal = transform.position - other.transform.position;

    //        playerSetting.playerUI.ShowEnemyInfoUI();
    //        enemy.OnDamage(playerSetting.damage, hitPoint, hitnormal);
    //        playerSetting.playerUI.EnemyName.text = "" + enemy.gameObject.name;
    //        playerSetting.playerUI.EnemyHpUI.value = enemy.currentHp / enemy.maxHp;
    //        canTrigger = false;
    //        StartCoroutine(ResetTrigger());
    //    }
    //}


}
