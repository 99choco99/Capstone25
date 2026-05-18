using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using static UnityEngine.EventSystems.EventTrigger;

public class PlayerExecution : MonoBehaviour
{
    private Player player;
    public PlayableDirector DeathblowDirector{ get; private set; }
    public bool IsPlayingDirector { get; private set; }
    public event Action<Player> OnExecuteEnd;

    private void Awake()
    {
        player = GetComponent<Player>();
        DeathblowDirector = GetComponent<PlayableDirector>();
    }

    private void Start()
    {
        if (DeathblowDirector != null)
        {
            DeathblowDirector.stopped += OnDeathblowTimelineFinished;
        }
    }

    private void OnDestroy()
    {
        if (DeathblowDirector != null)
        {
            DeathblowDirector.stopped -= OnDeathblowTimelineFinished;
        }
    }


    //인살 실행
    public void AttemptDeathblow(Enemy enemy)
    {
        player.Combat.CurrentWeapon.DisableWeaponCollider();
        player.TargetingSystem.DeselectTarget();

        bool isFront = Vector3.Dot(enemy.transform.forward, transform.forward) < 0;

        PlayableAsset targetTimeline = enemy.GetExecutionTimeline(isFront);

        if (targetTimeline == null)
        {
            Debug.LogWarning($"{enemy.gameObject.name}에게 인살 타임라인이 없습니다!");
            player.StateMachine.TransitionTo(player.StateMachine.PlayerGroundedState);
            return;
        }

        //타임라인 실행
        PlayDeathblowTimeline(enemy, targetTimeline, isFront);
    }




    // 실제 인살
    private void PlayDeathblowTimeline(Enemy enemy, PlayableAsset timelineAsset, bool isFront)
    {
        IsPlayingDirector = true;
        DeathblowDirector.playableAsset = timelineAsset;

        enemy.Motor.Stop();

        float offset = isFront ? 0.9f : -0.9f;
        transform.position = enemy.transform.position + enemy.transform.forward * offset;
        transform.rotation = Quaternion.LookRotation((enemy.transform.position - transform.position).normalized);

        if (isFront)
        {
            enemy.transform.rotation = Quaternion.LookRotation((transform.position - enemy.transform.position).normalized);
            enemy.Stats.isDeflecting = false;
        }

        // 타임라인 트랙 동적 바인딩
        var outputs = timelineAsset.outputs;
        DeathblowDirector.SetGenericBinding(outputs.ElementAt(1).sourceObject, isFront ? enemy.gameObject : player.gameObject);
        DeathblowDirector.SetGenericBinding(outputs.ElementAt(2).sourceObject, isFront ? player.gameObject : enemy.gameObject);

        // 상태 및 이벤트 연결 후 실행
        enemy.Stats.IsPlayingDeathBlow = true;
        //OnExecuteEnd += enemy.Stats.DeathBlowProcess;

        DeathblowDirector.Play();
        SoundManager.Instance.PlaySFX("ExecuteBGM");
    }

    public void OnDeathblowTimelineFinished(PlayableDirector director)
    {
        OnExecuteEnd?.Invoke(player);
        OnExecuteEnd = null; // 구독해제

        player.StateMachine.TransitionTo(player.StateMachine.PlayerGroundedState);
        IsPlayingDirector = false;

        player.Stats.IsInvincible = false;
        player.InputHandler.enabled = true;
    }
}
