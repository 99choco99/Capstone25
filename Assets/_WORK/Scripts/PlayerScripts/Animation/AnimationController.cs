using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AnimHash
{
    public static readonly int Locomotion = Animator.StringToHash("Locomotion");
    public static readonly int Horizontal = Animator.StringToHash("Horizontal");
    public static readonly int Vertical = Animator.StringToHash("Vertical");

    public static readonly int Jump = Animator.StringToHash("Jump");
    public static readonly int Parry = Animator.StringToHash("Parry");
    public static readonly int Dodge = Animator.StringToHash("Dodge");
    public static readonly int Guard = Animator.StringToHash("Base Layer.Guard.GuardStart");

    // GroggyEnter는 붕괴 진입, Groggy는 인살 대기 자세, GroggyRecover는 체간 회복 동작
    public static readonly int GroggyEnter = Animator.StringToHash("GroggyEnter");
    public static readonly int Groggy = Animator.StringToHash("Groggy");
    public static readonly int GroggyRecover = Animator.StringToHash("GroggyRecover");
    public static readonly int Stun = Animator.StringToHash("Stun");
    public static readonly int Death = Animator.StringToHash("Death");

    public static readonly int GuardHit = Animator.StringToHash("GuardHit");
    public static readonly int AttackRebound = Animator.StringToHash("AttackRebound");
    public static readonly int HitFront = Animator.StringToHash("HitFront");
    public static readonly int HitLeft = Animator.StringToHash("HitLeft");
    public static readonly int HitRight = Animator.StringToHash("HitRight");
    public static readonly int BackHit1 = Animator.StringToHash("HitBack1");
    public static readonly int BackHit2 = Animator.StringToHash("HitBack2");


}

[RequireComponent(typeof(Animator))]
public class AnimationController : MonoBehaviour
{
    [field: SerializeField] private Animator Anim;

    public Animator Animator => Anim;

    private void Awake()
    {
        Anim = GetComponent<Animator>();
    }

    /// <summary>
    /// 이동 관련 애니메이션
    /// </summary>
    public void UpdateLocomotion(float horizontalInput, float verticalInput, bool isSprinting = false)
    {
        verticalInput = isSprinting ? 2f : verticalInput;

        Anim.SetFloat(AnimHash.Horizontal, horizontalInput, 0.1f, Time.deltaTime);
        Anim.SetFloat(AnimHash.Vertical, verticalInput, 0.1f, Time.deltaTime);
    }

    /// <summary>
    /// locomotion 0으로 만들기 
    /// </summary>
    public void ForceStopLocomotion() => SetLocomotion(0f, 0f);

    /// <summary>
    /// 사용자 wasd 입력에 따라 이동 방향 설정
    /// </summary>
    public void SetLocomotion(float horizontal, float vertical)
    {
        Anim.SetFloat(AnimHash.Horizontal, horizontal);
        Anim.SetFloat(AnimHash.Vertical, vertical);
    }


    /// <summary>
    /// 애니메이션 재생
    /// </summary>
    public void PlayAction(int targetAnimHash, float transitionDuration = 0.2f, float playbackSpeed = 1f)
    {
        if (!Anim.HasState(0, targetAnimHash))
        {
            Debug.LogError($"Animator에 해당 State가 없습니다: {targetAnimHash}", this);
            return;
        }
        Anim.speed = Mathf.Max(0.01f, playbackSpeed);
        Anim.CrossFade(targetAnimHash, transitionDuration, 0, 0f);
    }

    /// <summary>
    /// 공격 클립을 AttackData의 속도로 재생
    /// </summary>
    public void PlayAttack(int targetAnimHash, float playbackSpeed, float transitionDuration = 0.08f)
    {
        if (!Anim.HasState(0, targetAnimHash))
        {
            Debug.LogError($"Animator에 해당 State가 없습니다: {targetAnimHash}", this);
            return;
        }

        Anim.speed = Mathf.Max(0.01f, playbackSpeed);
        Anim.CrossFadeInFixedTime(targetAnimHash, transitionDuration, 0, 0f);
    }

    /// <summary>
    /// 피격, 가드, 패링
    /// </summary>
    public void PlayReaction(int targetAnimHash, float transitionDuration = 0.05f)
    {
        Anim.speed = 1f;

        if (!Anim.HasState(0, targetAnimHash))
        {
            Debug.LogError($"Animator에 해당 State가 없습니다: {targetAnimHash}", this);
            return;
        }

        Anim.CrossFadeInFixedTime(targetAnimHash, transitionDuration, 0, 0f);
    }
}
