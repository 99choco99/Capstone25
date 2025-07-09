using System.Collections;
using UnityEngine;

public class EnemyWeapon : Weapon
{
    LayerMask playerLayer = 1 << 6;
    [SerializeField] Enemy self;
    PlayerController target;
    Animator anim;
    Collider col;


    Vector3 hitPoint;
    Vector3 hitDirection;

    private void Awake()
    {
        self = GetComponentInParent<Enemy>();
        anim = GetComponentInParent<Animator>();
        col = GetComponent<Collider>();
    }

    private void Update()
    {
        if (!anim.GetCurrentAnimatorStateInfo(0).IsName("Start"))
        {
            col.enabled = true;
        }
        else
        {
            col.enabled = false;
        }
        Debug.DrawLine(hitPoint, hitDirection * 10, Color.red);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!self.canTrigger) { return; }
        if(target == null)
        {
           target = other.transform.parent.GetComponent<PlayerController>();
        }
        else if ((1 << other.gameObject.layer) == playerLayer)
        {
            hitPoint = other.ClosestPoint(transform.position);
            hitDirection = (target.transform.position - self.transform.position).normalized;
            hitDirection.y = 0;
            hitDirection.Normalize();
            target.player.OnDamage(self.damage, hitPoint, hitDirection);
            self.canTrigger = false;
            StartCoroutine(self.ResetTrigger());
            Debug.Log("ÇÇ°Ý");
        }

        //if (other.CompareTag("GuardState"))
        //{
        //    PlayerSetting player = other.transform.parent.GetComponent<PlayerSetting>();
        //    player.Ishit = true;
        //    canTrigger = false;
        //    StartCoroutine(ResetTrigger());
        //}
    }

}
