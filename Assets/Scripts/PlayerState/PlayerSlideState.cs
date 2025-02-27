using UnityEngine;
public class PlayerSlideState : IState
{
    PlayerController player;

    public PlayerSlideState(PlayerController player)
    {
        this.player = player;
    }

    public void Enter()
    {
        player.anim.SetTrigger("Slide");

        //�����̵� �� �ݶ��̴��� �Բ� ��������
        player.transform.GetChild(0).localPosition = new Vector3(0, 0.2f, 1);
        Vector3 currentRotation = player.transform.GetChild(0).transform.eulerAngles;
        currentRotation.x -= 80;
        player.transform.GetChild(0).eulerAngles = currentRotation;
    }

    public void Update()
    {
        if (player.anim.GetCurrentAnimatorStateInfo(0).IsName("Slide")){
            player.rb.MovePosition(player.rb.position + (player.transform.forward * player.slideSpeed * Time.deltaTime));
        }
        else
        {
            player.playerStateMachine.TransitionTo(player.playerStateMachine.preState);
        }
    }

    public void Exit()
    {
        //�ݶ��̴� ����
        player.transform.GetChild(0).localPosition = new Vector3(0, 0, 0);
        Vector3 currentRotation = player.transform.GetChild(0).transform.eulerAngles;
        currentRotation.x += 80;
        player.transform.GetChild(0).eulerAngles = currentRotation;

        player.anim.ResetTrigger("Slide");
        player.sprint = false;
    }
}


