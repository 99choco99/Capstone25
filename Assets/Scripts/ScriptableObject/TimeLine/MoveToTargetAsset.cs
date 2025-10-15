using UnityEngine;
using UnityEngine.Playables;

public class MoveToTargetAsset : PlayableAsset
{
    // Inspector에서 Enemy 오브젝트를 할당할 수 있도록 합니다.
    public ExposedReference<Transform> enemy;
    public float offset = 0.9f;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<MoveToTargetBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();

        // Player는 트랙에 바인딩 된 오브젝트로 설정합니다.
        behaviour.player = owner.transform;

        // Inspector에서 설정한 Enemy와 Offset 값을 Behaviour에 넘겨줍니다.
        behaviour.enemy = enemy.Resolve(graph.GetResolver());
        behaviour.targetOffset = offset;

        return playable;
    }
}