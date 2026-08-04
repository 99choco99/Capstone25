using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using static UnityEngine.EventSystems.EventTrigger;

public class PlayerExecution : MonoBehaviour
{
    public PlayableDirector DeathblowDirector{ get; private set; }
    public event Action OnExecuteEnd;
    public event Action<Transform, Transform> OnExecuteStart; // (공격자, 피격자) — 카메라/연출 훅

    private void Awake() => DeathblowDirector = GetComponent<PlayableDirector>();

    private void OnEnable() => DeathblowDirector.stopped += OnDeathblowTimelineFinished;
    private void OnDisable() => DeathblowDirector.stopped -= OnDeathblowTimelineFinished;


    //�λ� ����
    public bool AttemptDeathblow(Enemy enemy)
    {
        bool isFront = Vector3.Dot(enemy.transform.forward, transform.forward) < 0;
        PlayableAsset targetTimeline = enemy.GetExecutionTimeline(isFront);

        if (targetTimeline == null) return false;

        //Ÿ�Ӷ��� ����
        PlayDeathblowTimeline(enemy, targetTimeline, isFront);
        return true;
    }




    // ���� �λ�
    private void PlayDeathblowTimeline(Enemy enemy, PlayableAsset timelineAsset, bool isFront)
    {
        DeathblowDirector.playableAsset = timelineAsset;

        //enemy.HandleBeingExecuted(this.transform, isFront); �� ��ġ ����

        var outputs = timelineAsset.outputs;
        DeathblowDirector.SetGenericBinding(outputs.ElementAt(1).sourceObject, isFront ? enemy.gameObject : gameObject);
        DeathblowDirector.SetGenericBinding(outputs.ElementAt(2).sourceObject, isFront ? gameObject : enemy.gameObject);

        // 카메라/연출이 이 프레임부터 붙도록, 재생 직전에 알린다. (공격자=플레이어, 피격자=적)
        OnExecuteStart?.Invoke(transform, enemy.transform);

        DeathblowDirector.Play();
        SoundManager.Instance.PlaySFX("ExecuteBGM");
    }

    public void OnDeathblowTimelineFinished(PlayableDirector director)
    {
        OnExecuteEnd?.Invoke();
    }
}
