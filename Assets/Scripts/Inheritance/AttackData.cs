using UnityEngine;


[CreateAssetMenu(fileName = "AttackData", menuName = "Scriptable Objects/AttackData")]
public class AttackData : ScriptableObject
{
    [Header("타격 정보")]
    public AttackType type;
    public float damage;
    public float postureDamage;
    public float knockbackPower;
    public float knockbackDuration;

    [Header("애니메이션")]
    public string animationStateName;
    public int AnimationHash => Animator.StringToHash(animationStateName);
}
