using UnityEngine;

public interface IState
{
    public void Enter(); // 상태 진입
    public void Update();  // 프레임당 로직
    public void Exit();  // 상태 탈출
}
