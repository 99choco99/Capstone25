using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnNPC : NPC
{
    // [하드코딩 1] 돌아갈 씬 이름
    private string targetSceneName = "Main";

    // [하드코딩 2] 돌아갈 씬의 위치
    private Vector3 targetPosition = new Vector3(-15.76f, 3.866f, 49.78f);

    public override void Interact(Player player)
    {
        if (NetworkManager.instance.socket != null)
        {
            //NetworkManager.instance.socket.EmitJoinScene(targetSceneName);
        }
        else
        {
            Debug.LogError("SocketManager 인스턴스를 찾을 수 없어 씬을 변경할 수 없습니다.");
        }
    }
}
