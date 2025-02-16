using UnityEngine;


public class PlayerInputa : MonoBehaviour
{

    public string verticalInputName = "Vertical"; 
    public string horizontalInputName = "Horizontal";
    public string slideInputName = "Slide";
    public string jumpInputName = "Jump";
    public string attackInputName = "Attack";
    public string guardInputName = "Guard";

    public float VerticalInput { get; private set; }  // �յ� ������ �Է�
    public float HorizontalInput{ get; private set; }  // ������ ������ �Է�
    public bool SlideInput { get; private set; }  // �����̵� �Է�
    public bool JumpInput { get; private set; }   // ���� �Է�
    public bool AttackInput{ get; private set; }  // �Ϲ� ���� �Է�
    public bool GuardInput{ get; private set; }  // ���� �Է�




    void Update()
    {
        // ������� �Է��� ������ ������.
        VerticalInput = Input.GetAxisRaw(verticalInputName);
        HorizontalInput = Input.GetAxisRaw(horizontalInputName);
        SlideInput = Input.GetKeyDown(KeyCode.LeftShift);
        JumpInput = Input.GetKeyDown(KeyCode.Space);
        AttackInput = Input.GetMouseButtonDown(0);
        GuardInput = Input.GetMouseButtonDown(2);

    }
}
