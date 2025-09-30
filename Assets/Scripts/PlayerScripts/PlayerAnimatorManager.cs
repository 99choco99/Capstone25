using UnityEngine;
using UnityEngine.InputSystem.XR;

public class PlayerAnimatorManager : MonoBehaviour
{
    Player player;

    public bool isPerformingAction = false;


    // [추가] 네트워크 전송 최적화를 위한 변수들
    private float lastAnimSendTime = 0f;
    private float animSendInterval = 0.1f; // 0.1초에 한 번씩만 체크
    private float lastVertical = -99f;
    private float lastHorizontal = -99f;

    private void Awake()
    {
        player = GetComponent<Player>();
    }


    private void LateUpdate()
    {
        if (player.isLockOn)
        {
            UpdateAnimMoveParameter(player.InputHandler.horizontalInput, player.InputHandler.verticalInput);
        }
        else
        {
            UpdateAnimMoveParameter(0, player.InputHandler.moveAmount);
        }

    }

    public void UpdateAnimMoveParameter(float horizontalInput, float verticalInput)
    {

        if (player.InputHandler.SprintInput)
        {
            verticalInput = 2;
        }
        player.Anim.SetFloat("Horizontal", horizontalInput, 0.1f, Time.deltaTime);
        player.Anim.SetFloat("Vertical", verticalInput, 0.1f, Time.deltaTime);

        if (Time.time - lastAnimSendTime > animSendInterval)
        {
            if (Mathf.Abs(verticalInput - lastVertical) > 0.01f ||
                Mathf.Abs(horizontalInput - lastHorizontal) > 0.01f)
            {
                SocketManager.instance.EmitPlayerMoveAnimation(verticalInput, horizontalInput);

                lastAnimSendTime = Time.time;
                lastVertical = verticalInput;
                lastHorizontal = horizontalInput;
            }
        }
    }

    // 구르기 등 특정 액션을 재생
    public void PlayTargetActionAnimation(string targetAnim, bool isPerformingAction = true)
    {
        player.Anim.CrossFade(targetAnim, 0.2f);
        this.isPerformingAction = isPerformingAction;

    }


    private void OnAnimatorMove()
    {
        if (player.StateMachine.CurrentState != null && player.StateMachine.CurrentState.UseRootMotion)
        {
            player.Motor.controller.Move(player.Anim.deltaPosition);
            transform.rotation *= player.Anim.deltaRotation;
        }
    }
}
