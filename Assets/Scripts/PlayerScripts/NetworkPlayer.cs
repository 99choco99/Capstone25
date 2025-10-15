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
    }

    void Update()
    {
        if (player.IsLocalPlayer) return;

        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 10f);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 15f);
        player.Anim.SetFloat("Horizontal", horizontal);
        player.Anim.SetFloat("Vertical", vertical);
    }

    // SocketManager가 호출해줄 함수
    public void UpdatePosition(Vector3 newPosition)
    {
        targetPosition = newPosition;
    }

    public void UpdateRotation(Quaternion newRotation)
    {
        targetRotation = newRotation;
    }

    public void UpdateMoveAnimation(float newHorizontal, float newVertical)
    {
        horizontal = newHorizontal;
        vertical = newVertical;
    }
}
