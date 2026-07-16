using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public enum AIBrainState { Idle, Chasing, CombatReady, Attacking }

public class EnemyAIController : MonoBehaviour
{
    private Enemy enemy;
    public AIBrainState currentBrainState = AIBrainState.Idle;

    [Header("거리 판단 기준")]
    public float combatRange = 5.5f; // 이 거리 안에 들어오면 공격 가능
    public float chaseRange = 15.0f; // 이 거리 안에 플레이어가 있으면 인식하고 쫓아감

    [Header("행동 타이머 (눈치보는 시간)")]
    public float minActionDelay = 0.5f;
    public float maxActionDelay = 2.0f;
    private float actionTimer = 0f;

    [Header("공격 패턴")]
    public List<EnemyAttackData> pattern = new();
    private List<EnemyAttackData> validAttacks = new();
    private Dictionary<EnemyAttackData, float> coolDown = new();
    public void Init(Enemy owner)
    {
        this.enemy = owner;
    }

    public void Tick()
    {

        if (enemy.Stats.IsDead || enemy.Sense.CurrentTarget == null)
        {
            currentBrainState = AIBrainState.Idle;
            return;
        }

        float distanceToPlayer = enemy.Sense.DistanceToTarget;

        switch (currentBrainState)
        {
            case AIBrainState.Idle:
                if (distanceToPlayer <= chaseRange)
                {
                    currentBrainState = AIBrainState.Chasing;
                }
                break;

            case AIBrainState.Chasing:
                actionTimer -= Time.deltaTime;
                if(actionTimer < 0f)
                {
                    if (TryAttack(distanceToPlayer)) return;
                    actionTimer = 0.2f;
                }

                if (distanceToPlayer <= combatRange)
                {
                    currentBrainState = AIBrainState.CombatReady;
                    ResetActionTimer(); // 돌입하면 타이머 시작
                    enemy.Motor.Stop();
                }
                else
                {
                    enemy.Motor.Chase(enemy.Sense.CurrentTarget.position);
                }
                break;

            case AIBrainState.CombatReady:
                if (distanceToPlayer > combatRange * 1.2f)
                {
                    currentBrainState = AIBrainState.Chasing;
                    break;
                }
                enemy.Motor.CombatStrafe(enemy.Sense.CurrentTarget.position, combatRange);

                actionTimer -= Time.deltaTime;
                if (actionTimer <= 0f)
                {
                    enemy.Motor.Stop();
                    if (!TryAttack(distanceToPlayer))
                    {
                        currentBrainState = AIBrainState.CombatReady;
                        ResetActionTimer();
                    }
                }
                break;

            case AIBrainState.Attacking:
                break;
        }
    }

    private bool TryAttack(float distance)
    {
        float totalWeight = 0f;
        validAttacks.Clear();

        foreach (var atk in pattern)
        {
            if (coolDown.ContainsKey(atk) && coolDown[atk] > Time.time) { continue; }

            if(atk.minDistance > distance || atk.maxDistance < distance) { continue; }
            totalWeight += atk.weight;
            validAttacks.Add(atk);
        }

        EnemyAttackData chosenAttack = null;

        if (validAttacks.Count > 0)
        {
            float randomVal = Random.Range(0, totalWeight);
            foreach (var attack in validAttacks)
            {
                randomVal -= attack.weight;
                if (randomVal <= 0)
                {
                    chosenAttack = attack;
                    break;
                }
            }
        }

        if (chosenAttack != null)
        {
            currentBrainState = AIBrainState.Attacking;
            coolDown[chosenAttack] = Time.time + Random.Range(chosenAttack.minAttackCooldown,chosenAttack.maxAttackCooldown);
            enemy.StateMachine.RequestedAttackData = chosenAttack;
            enemy.StateMachine.TransitionTo(enemy.StateMachine.EnemyAttackState);
            return true;
        }
        return false;
    }


    // 공격끝남
    public void OnAttackFinished()
    {
        currentBrainState = AIBrainState.CombatReady;
        ResetActionTimer();
    }

    private void ResetActionTimer()
    {
        actionTimer = Random.Range(minActionDelay, maxActionDelay);
    }






    private void OnDrawGizmosSelected()
    {
        // 1. 추적 거리 (노란색) & 기본 교전 거리 (빨간색)
        Handles.color = new Color(1f, 1f, 0f, 0.1f); // 반투명 노란색
        Handles.DrawSolidDisc(transform.position, Vector3.up, chaseRange);

        Handles.color = new Color(1f, 0f, 0f, 0.2f); // 반투명 빨간색
        Handles.DrawSolidDisc(transform.position, Vector3.up, combatRange); // attackRange에서 이름 바꾸셨다면 combatRange로!

        // 2. 도넛 모양으로 패턴 사거리 그리기!
        if (pattern != null)
        {
            foreach (var atk in pattern)
            {
                if (atk == null) continue;

                // 공격 패턴의 최대 사거리를 파란색 선으로 그림
                Handles.color = Color.cyan;
                Handles.DrawWireDisc(transform.position, Vector3.up, atk.maxDistance);

                // 텍스트로 무슨 공격인지 공중에 띄워줌 (대박 꿀팁!)
                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.cyan;
                Handles.Label(transform.position + transform.forward * atk.maxDistance, atk.name, style);
            }
        }
    }

}