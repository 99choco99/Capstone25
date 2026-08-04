using UnityEngine;

public class NetworkPlayer : MonoBehaviour
{

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float horizontal;
    private float vertical;
    private Player player;

    void Awake()
    {
        player = GetComponent<Player>();
        targetPosition = transform.position;
        targetRotation = transform.rotation;
    }

    void Update()
    {
        if (player.IsLocalPlayer) return;

        transform.SetPositionAndRotation(Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 10f), Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 15f));
        //player.Anim.SetFloat("Horizontal", horizontal);
        //player.Anim.SetFloat("Vertical", vertical);
    }

    // SocketManager가 호출해줄 함수
    public void UpdatePosition(Vector3 newPosition)
    {
        if (player.IsLocalPlayer) return;
        targetPosition = newPosition;
    }

    //회전
    public void UpdateRotation(Quaternion newRotation)
    {
        if (player.IsLocalPlayer) return;
        targetRotation = newRotation;
    }

    public void UpdateMoveAnimation(float newHorizontal, float newVertical)
    {
        if (player.IsLocalPlayer) return;
        horizontal = newHorizontal;
        vertical = newVertical;
    }
}
