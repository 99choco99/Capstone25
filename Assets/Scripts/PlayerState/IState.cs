using UnityEngine;

public interface IState
{
    public void Enter(); // ���� ����
    public void Update();  // �����Ӵ� ����
    public void Exit();  // ���� Ż��
}
