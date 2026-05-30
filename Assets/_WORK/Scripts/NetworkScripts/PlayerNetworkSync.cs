using System;
using UnityEngine;

public class PlayerNetworkSync : MonoBehaviour
{
    Player player;
    Animator Anim;
    [SerializeField] private float sendInterval = 0.1f; // 전송 간격
    private float lastSendTime = 0f;
    

    //위치 및 회전
    private Vector3 lastSentPosition;
    private Quaternion lastSentRotation;

    //애니메이션
    private float lastSentVertical = -99f;
    private float lastSentHorizontal = -99f;

    public event Action<Vector3, Quaternion, float, float> OnNetworkStateChanged;


    private void Awake()
    {
        player = GetComponent<Player>();
    }

    private void Update()
    {
        if (player != null && player.IsLocalPlayer)
        {
            HandleNetworkSync();
        }
    }


    //움직임 동기화
    public void HandleNetworkSync()
    {
        if (Time.time - lastSendTime > sendInterval)
        {
            float currentVertical = Anim.GetFloat("Vertical");
            float currentHorizontal = Anim.GetFloat("Horizontal");

            bool isPositionChanged = Vector3.Distance(transform.position, lastSentPosition) > 0.01f;
            bool isRotationChanged = Quaternion.Angle(transform.rotation, lastSentRotation) > 0.1f;
            bool isAnimChanged = Mathf.Abs(currentVertical - lastSentVertical) > 0.01f || Mathf.Abs(currentHorizontal - lastSentHorizontal) > 0.01f;

            if (isAnimChanged || isPositionChanged || isRotationChanged ) {

                OnNetworkStateChanged?.Invoke(transform.position, transform.rotation, currentVertical, currentHorizontal);

                lastSendTime = Time.time;
                lastSentPosition = transform.position;
                lastSentRotation = transform.rotation;
                lastSentVertical = currentVertical;
                lastSentHorizontal = currentHorizontal;
            }
        }
    }
}
