using UnityEngine;

public class NetworkPlayer : MonoBehaviour
{

    private Vector3 targetPosition;
    private Player player;

    void Awake()
    {
        player = GetComponent<Player>();
        targetPosition = transform.position;
    }

    void Update()
    {
        if (player.IsLocalPlayer) return;

        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 10f);
    }

    // SocketManager가 호출해줄 함수
    public void UpdatePosition(Vector3 newPosition)
    {
        targetPosition = newPosition;
    }
}
