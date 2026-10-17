using System.Collections.Generic;
using UnityEngine;

public static class AnimHash
{
    public static readonly int Locomotion = Animator.StringToHash("Locomotion");
    public static readonly int Horizontal = Animator.StringToHash("Horizontal");
    public static readonly int Vertical = Animator.StringToHash("Vertical");

    public static readonly int Jump = Animator.StringToHash("Jump");
    public static readonly int Parry = Animator.StringToHash("Parry");
    public static readonly int ParryHeavy = Animator.StringToHash("ParryHeavy");
    public static readonly int Knockdown = Animator.StringToHash("Knockdown");
    public static readonly int Dodge = Animator.StringToHash("Dodge");
    public static readonly int Guard = Animator.StringToHash("Base Layer.Guard.GuardStart");
    public static readonly int PlayerGuardEnter = Animator.StringToHash("Base Layer.Guard.GuardEnter");
    public static readonly int PlayerGuardRelease = Animator.StringToHash("Base Layer.Guard.GuardRelease");

    // GroggyEnter는 붕괴 진입, Groggy는 인살 대기 자세, GroggyRecover는 체간 회복 동작
    public static readonly int GroggyEnter = Animator.StringToHash("GroggyEnter");
    public static readonly int Groggy = Animator.StringToHash("Groggy");
    public static readonly int GroggyRecover = Animator.StringToHash("GroggyRecover");
    public static readonly int Stun = Animator.StringToHash("Stun");
    public static readonly int Death = Animator.StringToHash("Death");

    public static readonly int GuardHit = Animator.StringToHash("GuardHit");
    public static readonly int GuardHit2 = Animator.StringToHash("GuardHit2");
    public static readonly int GuardHit3 = Animator.StringToHash("GuardHit3");
    public static readonly int AttackRebound = Animator.StringToHash("AttackRebound");
    public static readonly int HitFront = Animator.StringToHash("HitFront");
    public static readonly int HitLeft = Animator.StringToHash("HitLeft");
    public static readonly int HitRight = Animator.StringToHash("HitRight");
    public static readonly int BackHit = Animator.StringToHash("BackHit");
    public static readonly int BackHit1 = Animator.StringToHash("HitBack1");
    public static readonly int BackHit2 = Animator.StringToHash("HitBack2");
}

[RequireComponent(typeof(Animator))]
public class AnimationController : MonoBehaviour
{
    public const float AttackTransitionDuration = 0.08f;
    public const float GuardTransitionDuration = 0.03f;

    [field: SerializeField] private Animator Anim;
    [SerializeField, Min(0f)] private float locomotionDampTime = 0.1f;
    private readonly List<AnimatorClipInfo> attackClips = new(1);
    private int reactionAnimationHash;

    public Animator Animator => Anim;
    public int ReactionHash => reactionAnimationHash;
    public bool IsPlayingReaction => reactionAnimationHash != AnimHash.Guard && IsReactionPlaying();
    public bool IsPlayingGuardRelease => TryGetAttackProgress(AnimHash.PlayerGuardRelease, out _, out float progress)
        && progress < 1f;
    public bool IsPlayingGuardReaction => (reactionAnimationHash == AnimHash.GuardHit || reactionAnimationHash == AnimHash.GuardHit2
        || reactionAnimationHash == AnimHash.GuardHit3 || reactionAnimationHash == AnimHash.Parry
        || reactionAnimationHash == AnimHash.ParryHeavy) && IsReactionPlaying();

    public static int GetGuardReactionHash(in DamageResult result)
    {
        AttackImpactProfile profile = result.Request.ImpactProfile;
        if (profile != null)
        {
            if (profile.UseHeavyDefenseFeedback)
                return AnimHash.GuardHit3;
            switch (profile.GuardMotion)
            {
                case GuardReactionMotion.Light:
                    return AnimHash.GuardHit;
                case GuardReactionMotion.Medium:
                    return AnimHash.GuardHit2;
                case GuardReactionMotion.Heavy:
                    return AnimHash.GuardHit3;
            }
        }

        return result.Request.KnockBackLevel switch
        {
            KnockBackLevel.Medium => AnimHash.GuardHit2,
            KnockBackLevel.Heavy => AnimHash.GuardHit3,
            _ => AnimHash.GuardHit
        };
    }

    public static int GetHitReactionHash(Vector3 forward, in DamageResult result)
    {
        if (result.DefenseType == DefenseType.Parry)
            return AnimHash.Parry;
        if (result.DefenseType == DefenseType.NormalGuard)
            return AnimHash.GuardHit;

        float hitAngle = Vector3.SignedAngle(forward, result.HitDirection, Vector3.up);
        if (Mathf.Abs(hitAngle) <= 45f)
            return AnimHash.BackHit;
        if (hitAngle > 45f && hitAngle <= 135f)
            return AnimHash.HitLeft;
        if (hitAngle >= -135f && hitAngle < -45f)
            return AnimHash.HitRight;
        return AnimHash.HitFront;
    }

    private bool IsReactionPlaying() => reactionAnimationHash != 0
        && TryGetAttackProgress(
        reactionAnimationHash,
        out _,
        out float progress)
        && progress < 1f;

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

        Anim.SetFloat(AnimHash.Horizontal, horizontalInput, locomotionDampTime, Time.deltaTime);
        Anim.SetFloat(AnimHash.Vertical, verticalInput, locomotionDampTime, Time.deltaTime);
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
    public void PlayAction(int targetAnimHash, float normalizedTransitionDuration = 0.2f, float playbackSpeed = 1f)
    {
        if (!ValidateAnimationState(targetAnimHash))
            return;
        reactionAnimationHash = 0;
        Anim.speed = Mathf.Max(0.01f, playbackSpeed);
        Anim.CrossFade(targetAnimHash, normalizedTransitionDuration, 0, 0f);
    }

    /// <summary>
    /// 공격 클립을 AttackData의 속도로 재생
    /// </summary>
    public void PlayAttack(int targetAnimHash, float playbackSpeed, float transitionSeconds = AttackTransitionDuration)
    {
        PlayFixedTimeMotion(targetAnimHash, playbackSpeed, transitionSeconds);
    }

    public void PlayActionInFixedTime(int targetAnimHash, float transitionSeconds, float playbackSpeed = 1f)
    {
        PlayFixedTimeMotion(targetAnimHash, playbackSpeed, transitionSeconds);
    }

    public void PlayGuardMotion(int targetAnimHash)
    {
        PlayFixedTimeMotion(targetAnimHash, 1f, GuardTransitionDuration);
    }

    private void PlayFixedTimeMotion(int targetAnimHash, float playbackSpeed, float transitionSeconds)
    {
        if (!ValidateAnimationState(targetAnimHash))
            return;

        reactionAnimationHash = 0;
        Anim.speed = Mathf.Max(0.01f, playbackSpeed);
        Anim.CrossFadeInFixedTime(targetAnimHash, transitionSeconds, 0, 0f);
    }

    public bool TryGetAttackProgress(int animationHash, out float frame, out float progress)
    {
        frame = 0f;
        progress = 0f;
        bool isTransitioning = Anim.IsInTransition(0);
        AnimatorStateInfo state = isTransitioning
            ? Anim.GetNextAnimatorStateInfo(0)
            : Anim.GetCurrentAnimatorStateInfo(0);
        if (state.shortNameHash != animationHash && state.fullPathHash != animationHash)
            return false;

        if (isTransitioning)
            Anim.GetNextAnimatorClipInfo(0, attackClips);
        else
            Anim.GetCurrentAnimatorClipInfo(0, attackClips);

        if (attackClips.Count == 0)
            return false;

        progress = Mathf.Max(0f, state.normalizedTime);
        float clipLength = attackClips[0].clip.length;
        frame = progress * clipLength * AttackData.FrameRate;
        return true;
    }

    /// <summary>
    /// 피격, 가드, 패링
    /// </summary>
    public void PlayReaction(int targetAnimHash, float transitionSeconds = 0.05f)
    {
        reactionAnimationHash = 0;
        Anim.speed = 1f;

        if (!ValidateAnimationState(targetAnimHash))
            return;

        reactionAnimationHash = targetAnimHash;
        Anim.CrossFadeInFixedTime(targetAnimHash, transitionSeconds, 0, 0f);
    }

    private bool ValidateAnimationState(int targetAnimHash)
    {
        if (Anim.HasState(0, targetAnimHash))
            return true;

        Debug.LogError($"Animator에 해당 State가 없습니다: {targetAnimHash}", this);
        return false;
    }
}
