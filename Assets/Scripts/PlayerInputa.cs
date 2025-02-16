using UnityEngine;


public class PlayerInputa : MonoBehaviour
{

    public string verticalInputName = "Vertical"; 
    public string horizontalInputName = "Horizontal";
    public string slideInputName = "Slide";
    public string jumpInputName = "Jump";
    public string attackInputName = "Attack";
    public string guardInputName = "Guard";

    public float VerticalInput { get; private set; }  // 앞뒤 움직임 입력
    public float HorizontalInput{ get; private set; }  // 옆으로 움직임 입력
    public bool SlideInput { get; private set; }  // 슬라이딩 입력
    public bool JumpInput { get; private set; }   // 점프 입력
    public bool AttackInput{ get; private set; }  // 일반 공격 입력
    public bool GuardInput{ get; private set; }  // 가드 입력




    void Update()
    {
        // 사용자의 입력을 변수에 저장함.
        VerticalInput = Input.GetAxisRaw(verticalInputName);
        HorizontalInput = Input.GetAxisRaw(horizontalInputName);
        SlideInput = Input.GetKeyDown(KeyCode.LeftShift);
        JumpInput = Input.GetKeyDown(KeyCode.Space);
        AttackInput = Input.GetMouseButtonDown(0);
        GuardInput = Input.GetMouseButtonDown(2);

    }
}
