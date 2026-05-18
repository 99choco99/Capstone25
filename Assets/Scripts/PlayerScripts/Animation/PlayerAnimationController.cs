using System;
using System.Collections;
using UnityEngine;

public static class AnimHash
{
    public static readonly int HeavyHit = Animator.StringToHash("HeavyHit");
    public static readonly int Parry = Animator.StringToHash("Parry");
    public static readonly int GuardHit = Animator.StringToHash("GuardHit");
    public static readonly int BackHit = Animator.StringToHash("BackHit");
    public static readonly int Hit = Animator.StringToHash("Hit");
    public static readonly int Jump = Animator.StringToHash("Jump");
    public static readonly int Locomotion = Animator.StringToHash("Locomotion");
    public static readonly int Roll = Animator.StringToHash("Roll");
    public static readonly int BackStep = Animator.StringToHash("BackStep");
    public static readonly int Guard = Animator.StringToHash("Guard");
    public static readonly int GuardBreak = Animator.StringToHash("GuardBreak");
}

public class PlayerAnimationController : MonoBehaviour
{
    Player player;
    public event Action OnDespawnRequested;

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    public void UpdateLocomotion(float horizontalInput, float verticalInput)
    {

        horizontalInput = Mathf.Round(horizontalInput);
        verticalInput = Mathf.Round(verticalInput);

        if (player.InputHandler.SprintInput && player.StateMachine.CurrentState == player.StateMachine.PlayerSprintState)
        {
            verticalInput = 2f;
        }

        player.Anim.SetFloat("Horizontal", horizontalInput, 0.1f, Time.deltaTime);
        player.Anim.SetFloat("Vertical", verticalInput, 0.1f, Time.deltaTime);
    }

    // 구르기 등 특정 액션을 재생
    public void PlayAction(int targetAnimHash)
    {
        player.Anim.CrossFade(targetAnimHash, 0.2f,0,0f);
    }


    public void OnPlaySFX(string name) => SoundManager.Instance.PlaySFX(name);
    public void OnPlayLoopingSFX(string name) => SoundManager.Instance.PlayLoopingSFX(name);
}
