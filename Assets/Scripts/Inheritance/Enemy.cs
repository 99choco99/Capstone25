//using System;
//using System.Collections;
//using Unity.Behavior;
//using UnityEngine;
//using UnityEngine.AI;

//public class Enemy : LivingEntity
//{
//    //protected NavMeshAgent agent;
//    //protected Animator anim;
//    //protected BehaviorGraphAgent BehaviourAgent;
//    //[SerializeField] protected EnemyData enemyData;

//    //private void Awake()
//    //{
//    //    agent = GetComponent<NavMeshAgent>();
//    //    anim = GetComponent<Animator>();
//    //    BehaviourAgent = GetComponent<BehaviorGraphAgent>();
//    //}
//    //private void Start()
//    //{
//    //    SetUp(enemyData);
//    //}

//    //public void SetUp(EnemyData enemyData)
//    //{
//    //    maxHp = enemyData.hp;
//    //    damage = enemyData.damage;
//    //    agent.speed = enemyData.speed;
//    //    base.OnEnable();
//    //    OnDeath += Dead;
//    //}

//    //public override void OnDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
//    //{
//    //    base.OnDamage(damage, hitPoint, hitNormal);
//    //}

//    //void Dead()
//    //{
//    //    anim.SetTrigger("Die");
//    //    BehaviourAgent.BlackboardReference.SetVariableValue<Boolean>("Dead", true);
//    //    StartCoroutine("Disappear");
//    //}

//    ////Á×Àº ÈÄ 2.5ÃÊµÚ ½ÃÃ¼ ¾ø¾îÁü.
//    //IEnumerator Disappear()
//    //{
//    //    yield return new WaitForSeconds(2.5f);
//    //    Destroy(gameObject);
//    //}
//}
