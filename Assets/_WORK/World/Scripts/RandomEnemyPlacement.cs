using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>지정된 후보 중 배치 지점과 일반 적의 종류를 섞어 배치합니다.</summary>
public class RandomEnemyPlacement : MonoBehaviour
{
    [Tooltip("보스를 제외한 일반 적 목록. 씬에서는 비활성 상태로 준비합니다.")]
    [SerializeField] private Transform[] enemies;
    [Tooltip("일반 적을 배치할 후보 지점. 회전은 주 진입 방향을 바라보도록 설정합니다.")]
    [SerializeField] private Transform[] spawnPoints;
    [Tooltip("코너와 갈림길의 우선 후보. 유효한 지점은 다른 후보보다 먼저 배치합니다.")]
    [SerializeField] private Transform[] prioritySpawnPoints;
    [Tooltip("스폰 수의 하한/상한. 상한이 0이면 준비한 적을 모두 배치합니다.")]
    [SerializeField, Min(0)] private int minimumEnemyCount;
    [SerializeField, Min(0)] private int maximumEnemyCount;
    [Tooltip("선택된 지점끼리 확보할 최소 간격")]
    [SerializeField, Min(0f)] private float enemySpacing = 8f;
    [SerializeField, Min(0f)] private float playerClearance = 12f;
    [Tooltip("시작 정면에서 보이는 첫 적을 우선 배치할 최대 거리. 0이면 우선 배치하지 않습니다.")]
    [SerializeField, Min(0f)] private float firstEncounterDistance = 20f;
    [SerializeField, Min(0.1f)] private float navMeshSampleDistance = 3f;
    [SerializeField] private LayerMask obstacleLayers = 1;

    [Header("배치 공간 검사")]
    [Tooltip("스폰 위치에서 검사 캡슐의 아래 구 중심까지의 높이입니다. 캡슐 전체의 바닥 높이와 구분합니다.")]
    [SerializeField, Min(0f)] private float placementProbeLowerHeight = 0.7f;
    [Tooltip("스폰 위치에서 검사 캡슐의 위 구 중심까지의 높이입니다.")]
    [SerializeField, Min(0f)] private float placementProbeUpperHeight = 1.5f;
    [SerializeField, Min(0.01f)] private float placementProbeRadius = 0.6f;

    [Header("일반 적의 활동 범위")]
    [SerializeField] private bool returnToSpawn;
    [SerializeField, Min(0.1f)] private float idleDetectionRadius = 16f;

    private readonly List<Pose> candidatePoints = new();
    private readonly List<Transform> availableEnemies = new();
    private readonly List<Vector3> occupiedPositions = new();
    private bool placed;
    private int desiredEnemyCount;

    private void Awake()
    {
        if (enemies == null)
            return;

        // 보스와 NPC는 목록에 포함하지 않습니다.
        foreach (Transform enemy in enemies)
        {
            if (enemy == null || availableEnemies.Contains(enemy))
                continue;
            availableEnemies.Add(enemy);
            enemy.gameObject.SetActive(false);

        }

        if (spawnPoints == null)
            return;
        foreach (Transform point in spawnPoints)
        {
            if (point != null)
                candidatePoints.Add(new Pose(point.position, point.rotation));
        }
    }

    private IEnumerator Start()
    {
        while (Player.LocalPlayer == null)
            yield return null;

        PlaceEnemies(Player.LocalPlayer.transform.position, Player.LocalPlayer.transform.forward);
    }

    private void PlaceEnemies(Vector3 playerPosition, Vector3 playerForward)
    {
        if (placed)
            return;
        placed = true;

        if (!NavMesh.SamplePosition(playerPosition, out NavMeshHit playerHit, navMeshSampleDistance, NavMesh.AllAreas))
        {
            Debug.LogWarning("일반 적 배치: 플레이어 시작점에서 이동 가능한 바닥을 찾지 못했습니다.", this);
            return;
        }

        if (availableEnemies.Count == 0)
            return;

        int maximum = maximumEnemyCount > 0
            ? Mathf.Min(maximumEnemyCount, availableEnemies.Count)
            : availableEnemies.Count;
        int minimum = maximumEnemyCount > 0 ? Mathf.Clamp(minimumEnemyCount, 1, maximum) : maximum;
        desiredEnemyCount = minimum == maximum ? maximum : Random.Range(minimum, maximum + 1);

        List<Pose> validPoints = FindValidPoints(playerPosition, playerHit);
        Shuffle(validPoints);
        Shuffle(availableEnemies);

        PlaceFirstEncounter(validPoints, playerPosition, playerForward);
        PlacePriorityEnemies(validPoints);
        PlaceRemainingEnemies(validPoints);

        if (occupiedPositions.Count < desiredEnemyCount)
            Debug.LogWarning(
                $"일반 적 배치: 선택한 적 {desiredEnemyCount}명 중 {occupiedPositions.Count}명 배치. 후보 지점의 바닥·장애물·이동 경로와 간격을 확인하세요.",
                this);
    }

    private List<Pose> FindValidPoints(Vector3 playerPosition, NavMeshHit playerHit)
    {
        List<Pose> validPoints = new();
        NavMeshPath path = new();

        foreach (Pose point in candidatePoints)
        {
            if (!NavMesh.SamplePosition(point.position, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas))
                continue;

            Vector3 position = hit.position;
            if (HorizontalDistanceSquared(position, playerPosition) < playerClearance * playerClearance)
                continue;
            if (Physics.CheckCapsule(position + Vector3.up * placementProbeLowerHeight, position + Vector3.up * placementProbeUpperHeight,
                placementProbeRadius, obstacleLayers, QueryTriggerInteraction.Ignore))
                continue;
            if (!NavMesh.CalculatePath(playerHit.position, position, NavMesh.AllAreas, path)
                || path.status != NavMeshPathStatus.PathComplete)
                continue;

            validPoints.Add(new Pose(position, point.rotation));
        }

        return validPoints;
    }

    private void PlaceFirstEncounter(List<Pose> validPoints, Vector3 playerPosition, Vector3 playerForward)
    {
        playerForward.y = 0f;
        playerForward.Normalize();
        for (int index = 0; firstEncounterDistance > 0f && index < validPoints.Count; index++)
        {
            Pose point = validPoints[index];
            Vector3 direction = point.position - playerPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude > firstEncounterDistance * firstEncounterDistance
                || Vector3.Dot(direction.normalized, playerForward) < 0.9f)
                continue;
            if (Physics.Linecast(playerPosition + Vector3.up * 1.3f, point.position + Vector3.up * 1.3f,
                obstacleLayers, QueryTriggerInteraction.Ignore))
                continue;

            PlaceEnemy(point);
            validPoints.RemoveAt(index);
            break;
        }

        if (firstEncounterDistance > 0f && occupiedPositions.Count == 0)
            Debug.LogWarning("일반 적 배치: 시작 정면에서 보이는 첫 조우 후보가 없습니다.", this);
    }

    private void PlacePriorityEnemies(List<Pose> validPoints)
    {
        if (prioritySpawnPoints != null)
        {
            foreach (Transform priority in prioritySpawnPoints)
            {
                if (occupiedPositions.Count >= desiredEnemyCount)
                    break;
                if (priority == null)
                    continue;
                if (!NavMesh.SamplePosition(
                    priority.position,
                    out NavMeshHit hit,
                    navMeshSampleDistance,
                    NavMesh.AllAreas))
                    continue;
                int index = validPoints.FindIndex(point => HorizontalDistanceSquared(point.position, hit.position) < 0.01f);
                if (index < 0 || !HasEnemyClearance(validPoints[index].position))
                    continue;
                PlaceEnemy(validPoints[index]);
                validPoints.RemoveAt(index);
            }
        }
    }

    private void PlaceRemainingEnemies(List<Pose> validPoints)
    {
        foreach (Pose point in validPoints)
        {
            if (occupiedPositions.Count >= desiredEnemyCount)
                break;
            if (!HasEnemyClearance(point.position))
                continue;
            PlaceEnemy(point);
        }
    }

    private void PlaceEnemy(Pose point)
    {
        Transform enemy = availableEnemies[occupiedPositions.Count];
        enemy.SetPositionAndRotation(point.position, point.rotation);
        if (returnToSpawn)
        {
            EnemyTerritory territory = enemy.GetComponent<EnemyTerritory>();
            if (territory == null)
                territory = enemy.gameObject.AddComponent<EnemyTerritory>();
            territory.Configure(point, idleDetectionRadius);
        }
        enemy.gameObject.SetActive(true);
        occupiedPositions.Add(point.position);
    }

    private bool HasEnemyClearance(Vector3 position)
    {
        foreach (Vector3 occupied in occupiedPositions)
        {
            if (HorizontalDistanceSquared(position, occupied) < enemySpacing * enemySpacing)
                return false;
        }

        return true;
    }

    private static float HorizontalDistanceSquared(Vector3 first, Vector3 second)
    {
        Vector3 difference = first - second;
        difference.y = 0f;
        return difference.sqrMagnitude;
    }

    private static void Shuffle<T>(List<T> items)
    {
        for (int index = items.Count - 1; index > 0; index--)
        {
            int other = Random.Range(0, index + 1);
            (items[index], items[other]) = (items[other], items[index]);
        }
    }
}
