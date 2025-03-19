using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] Player player;
    

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();

            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Vector3 hitnormal = transform.position - other.transform.position;
            player.playerUI.ShowEnemyInfoUI();
            enemy.OnDamage(player.damage, hitPoint, hitnormal);
            player.playerUI.EnemyName.text = "" + enemy.gameObject.name;
            player.playerUI.EnemyHp.value = enemy.currentHp / enemy.maxHp;
        }
    }

}
