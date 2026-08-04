using UnityEngine;

[RequireComponent(typeof(EnemySense))]
public class EnemyAttackObserver : MonoBehaviour
{

    Player player;
    [SerializeField] private EnemySense sense;

    [Header("위협 판정")]
    [SerializeField,Min(0.1f)] private float reactionRange = 2.5f;
    [SerializeField, Range(0f, 180f)] private float angle = 100f;

    /// <summary>
    /// player가 공격중인지 아닌지
    /// </summary>
    public bool IsPlayerAttacking { get; private set; }

    /// <summary>
    /// 현재 몇번째 공격인지 구분하는 변수
    /// </summary>
    public int curAttackVersion { get; private set; }

    /// <summary>
    /// 예상 타격 시간
    /// </summary>
    public float ExpectedActiveTime { get; private set; }

    private float minimumThreatDot;


    private void Awake()
    {
        sense = GetComponent<EnemySense>();
        minimumThreatDot = Mathf.Cos(angle * 0.5f *  Mathf.Deg2Rad);
    }


    private void OnEnable()
    {
        Player.OnLocalPlayerSpawned += BindPlayer;

        if (Player.LocalPlayer != null)
            BindPlayer(Player.LocalPlayer);
    }

    private void OnDisable()
    {
        Player.OnLocalPlayerSpawned -= BindPlayer;
        UnbindPlayer();
    }

    private void BindPlayer(Player localPlayer)
    {
        if (localPlayer == null || !localPlayer.IsLocalPlayer) return;
        if (player == localPlayer) return;

        player = localPlayer;

        UnbindPlayer();

        player = localPlayer;
        player.Combat.AttackStarted += HandleAttackStart;
        player.Combat.AttackEnded += HandleAttackEnd;
    }

    private void UnbindPlayer()
    {
        if (player.Combat != null)
        {
            player.Combat.AttackStarted -= HandleAttackStart;
            player.Combat.AttackEnded -= HandleAttackEnd;
        }

        player = null;
        IsPlayerAttacking = false;
        ExpectedActiveTime = 0f;
    }


    private void HandleAttackStart(float expectedActiveAt)
    {
        IsPlayerAttacking = true;
        ExpectedActiveTime = expectedActiveAt;
        curAttackVersion++;
    }

    private void HandleAttackEnd()
    {
        IsPlayerAttacking = false;
        ExpectedActiveTime = 0f;
    }


    /// <summary>
    /// player공격이 닿을만한가?
    /// </summary>
    public bool IsAttackInRange()
    {
        if (!IsPlayerAttacking || player == null) return false;

        if (!sense.CanSeeTarget) return false;

        Vector3 playerToEnemy = transform.position - player.transform.position;
        playerToEnemy.y = 0f;

        float sqrDistance = playerToEnemy.sqrMagnitude;
        if (sqrDistance > reactionRange * reactionRange) return false;
        if (sqrDistance < 0.0001f) return true;

        Vector3 attackForward = player.transform.forward;
        attackForward.y = 0f;

        return Vector3.Dot(attackForward.normalized, playerToEnemy.normalized) >= minimumThreatDot;
    }
}

