using UnityEngine;
using UnityEngine.Playables;

public class MoveToTargetBehaviour : PlayableBehaviour
{
    public Transform player;
    public Transform enemy;
    public float targetOffset = 0.9f;

    // 시작 상태와 최종 목표 상태를 저장할 변수
    private Vector3 startPlayerPos, endPlayerPos;
    private Quaternion startPlayerRot, endPlayerRot;
    private Quaternion startEnemyRot, endEnemyRot;

    // 클립이 시작될 때 '목표'를 한 번만 계산하고 저장합니다.
    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (player == null || enemy == null) return;

        // 1. 시작 상태 저장
        startPlayerPos = player.position;
        startPlayerRot = player.rotation;
        startEnemyRot = enemy.rotation;

        // 2. 최종 목표 상태 계산
        // 플레이어의 최종 위치
        endPlayerPos = enemy.position + enemy.forward * targetOffset;

        // 플레이어의 최종 회전 (적을 바라봄)
        Vector3 playerDirToEnemy = (enemy.position - endPlayerPos).normalized;
        endPlayerRot = Quaternion.LookRotation(playerDirToEnemy);

        // 적의 최종 회전 (플레이어를 바라봄)
        Vector3 enemyDirToPlayer = (endPlayerPos - enemy.position).normalized;
        endEnemyRot = Quaternion.LookRotation(enemyDirToPlayer);
    }

    // 매 프레임 '과정'을 수행합니다.
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (player == null || enemy == null) return;

        // 클립의 진행도 (0.0 ~ 1.0)
        float progress = (float)(playable.GetTime() / playable.GetDuration());

        // 저장해둔 시작값과 목표값을 기준으로 부드럽게 보간
        player.position = Vector3.Lerp(startPlayerPos, endPlayerPos, progress);
        player.rotation = Quaternion.Slerp(startPlayerRot, endPlayerRot, progress);
        enemy.transform.rotation = Quaternion.Slerp(startEnemyRot, endEnemyRot, progress);
    }
}