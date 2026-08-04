using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using static UnityEngine.EventSystems.EventTrigger;

public class PlayerExecution : MonoBehaviour
{
    public PlayableDirector DeathblowDirector{ get; private set; }
    public event Action OnExecuteEnd;

    private void Awake() => DeathblowDirector = GetComponent<PlayableDirector>();

    private void OnEnable() => DeathblowDirector.stopped += OnDeathblowTimelineFinished;
    private void OnDisable() => DeathblowDirector.stopped -= OnDeathblowTimelineFinished;


    //인살 실행
    public bool AttemptDeathblow(Enemy enemy)
    {
        bool isFront = Vector3.Dot(enemy.transform.forward, transform.forward) < 0;
        PlayableAsset targetTimeline = enemy.GetExecutionTimeline(isFront);

        if (targetTimeline == null) return false;

        //타임라인 실행
        PlayDeathblowTimeline(enemy, targetTimeline, isFront);
        return true;
    }




    // 실제 인살
    private void PlayDeathblowTimeline(Enemy enemy, PlayableAsset timelineAsset, bool isFront)
    {
        DeathblowDirector.playableAsset = timelineAsset;

        //enemy.HandleBeingExecuted(this.transform, isFront); 적 위치 조정

        var outputs = timelineAsset.outputs;
        DeathblowDirector.SetGenericBinding(outputs.ElementAt(1).sourceObject, isFront ? enemy.gameObject : gameObject);
        DeathblowDirector.SetGenericBinding(outputs.ElementAt(2).sourceObject, isFront ? gameObject : enemy.gameObject);

        DeathblowDirector.Play();
        SoundManager.Instance.PlaySFX("ExecuteBGM");
    }

    public void OnDeathblowTimelineFinished(PlayableDirector director)
    {
        OnExecuteEnd?.Invoke();
    }
}
