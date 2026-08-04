using UnityEngine;
using UnityEngine.Timeline;

// 이 트랙은 GameObject(Player)를 제어하며, MoveToTargetAsset 클립을 사용합니다.
[TrackBindingType(typeof(GameObject))]
[TrackClipType(typeof(MoveToTargetAsset))]
public class MoveToTargetTrack : TrackAsset
{
}