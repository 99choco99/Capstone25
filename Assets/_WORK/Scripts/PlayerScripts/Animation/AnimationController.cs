using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AnimHash
{
    public static readonly int Locomotion = Animator.StringToHash("Locomotion");
    public static readonly int Jump = Animator.StringToHash("Jump");
    public static readonly int Parry = Animator.StringToHash("Parry");
    public static readonly int Roll = Animator.StringToHash("Roll");
    public static readonly int BackStep = Animator.StringToHash("BackStep");
    public static readonly int Guard = Animator.StringToHash("Guard");
    public static readonly int HardLand = Animator.StringToHash("HardLand");
    public static readonly int SoftLand = Animator.StringToHash("SoftLand");


    public static readonly int GuardBreak = Animator.StringToHash("GuardBreak");
    public static readonly int Stun = Animator.StringToHash("Stun");
    public static readonly int Death = Animator.StringToHash("Death");

    public static readonly int GuardHit = Animator.StringToHash("GuardHit");
    public static readonly int HitFront = Animator.StringToHash("HitFront");
    public static readonly int HitLeft = Animator.StringToHash("HitLeft");
    public static readonly int HitRight = Animator.StringToHash("HitRight");
    public static readonly int BackHit1 = Animator.StringToHash("HitBack1");
    public static readonly int BackHit2 = Animator.StringToHash("HitBack2");


}

public class AnimationController : MonoBehaviour
{
    [field: SerializeField] private Animator Anim;

    public void UpdateLocomotion(float horizontalInput, float verticalInput, bool isSprinting = false)
    {
        verticalInput = isSprinting ? 2f : verticalInput;

        Anim.SetFloat("Horizontal", horizontalInput, 0.1f, Time.deltaTime);
        Anim.SetFloat("Vertical", verticalInput, 0.1f, Time.deltaTime);
    }


    public void ForceStopLocomotion()
    {
        Anim.SetFloat("Horizontal", 0);
        Anim.SetFloat("Vertical", 0);
    }


    // 구르기 등 특정 액션을 재생
    public void PlayAction(int targetAnimHash, float transitionDuration = 0.2f)
    {
        Anim.CrossFade(targetAnimHash, transitionDuration, 0, 0f);
    }

    public void OnPlaySFX(string name) => SoundManager.Instance.PlaySFX(name);
    public void OnPlayLoopingSFX(string name) => SoundManager.Instance.PlayLoopingSFX(name);
}
