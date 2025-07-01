using UnityEngine;
using UnityEngine.UI;

public class PlayerDetectEnemy : MonoBehaviour
{
    PlayerController player;
    LayerMask targetMask = 1 << 7;


    Collider[] targets; // 주변에 있는 몬스터들
    public Collider currentTarget; // 목표 타겟
    public Enemy currentEnemy; // 현재 타겟의 Enemy 컴포넌트
    int currentTargetIndex = 0;
    int numTargets = 0;

    [SerializeField] Canvas markPosition;
    [SerializeField] Image executeMark;
    [SerializeField] Image currentTargetMark;


    void Start()
    {
        player = GetComponent<PlayerController>();
        targets = new Collider[10];
    }

    void Update()
    {
        numTargets = Physics.OverlapSphereNonAlloc(transform.position, player.interactRange, targets, targetMask);


        if (numTargets == 0)
        {
            currentTarget = null; // 타겟이 없으면 null
            currentEnemy = null;
        }

        if (currentTarget != null)
        {
            markPosition.transform.position = currentTarget.transform.position;
            currentTargetMark.transform.position = currentTarget.transform.position + Vector3.up * 2;
            currentTargetMark.gameObject.SetActive(true);
            if (Vector3.Distance(currentTarget.gameObject.transform.position, transform.position) > player.interactRange)
            {
                ChangeTarget();
            }
        }
        else
        {
            currentTargetMark.gameObject.SetActive(false);
        }

        // currentTarget이 null이 아닐 때만 접근
        if (currentTarget != null && currentEnemy.isVulnerable)
        {
            player.canExecute = true;
            executeMark.transform.position = currentTarget.transform.position + new Vector3(0,3,0);
            executeMark.gameObject.SetActive(true);
        }
        else
        {
            player.canExecute = false;
            executeMark.gameObject.SetActive(false);
        }
    }

    public void ChangeTarget()
    {
        if (numTargets == 0) { return; }
        currentTargetIndex = (currentTargetIndex + 1) % numTargets;
        currentTarget = targets[currentTargetIndex];
        currentEnemy = targets[currentTargetIndex].GetComponent<Enemy>();
        currentTargetMark.transform.position = currentEnemy.transform.position;
    }
}
