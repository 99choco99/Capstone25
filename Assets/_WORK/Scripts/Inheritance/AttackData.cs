using UnityEngine;


[CreateAssetMenu(fileName = "AttackData", menuName = "Scriptable Objects/AttackData")]
public class AttackData : ScriptableObject
{
    [Header("타격 정보")]
    public float damage;
    public float postureDamage;
    public float knockbackPower;
    public virtual bool CanGuard => true;

    [Header("Combo Rules")]
    public AttackData NextComboAttack;


    [Header("애니메이션")]
    public string animationStateName;
    public int AnimationHash => Animator.StringToHash(animationStateName);



    [Header("타이밍 설정 (프레임 단위 입력)")]
    [Tooltip("애니메이션의 전체 프레임 수")]
    public float totalFrames = 60f;

    [Tooltip("타격 판정이 켜지는 프레임")]
    public float activeStartFrame = 15f;
    [Tooltip("후딜레이가 시작되어 캔슬 가능한 프레임")]
    public float recoveryStartFrame = 45f;
    public float ActiveStartTime => totalFrames > 0 ? Mathf.Clamp01(activeStartFrame / totalFrames) : 0f;
    public float RecoveryStartTime => totalFrames > 0 ? Mathf.Clamp01(recoveryStartFrame / totalFrames) : 0f;

    public float DurationInSeconds => totalFrames / 60f;
}
