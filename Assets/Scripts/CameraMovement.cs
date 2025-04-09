using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    PlayerController playerController;  // 사용자 입력
    public Transform objectToFollow;  // 카메라가 따라갈 대상
    public float followSpeed = 10f;  // 카메라 이동속도
    public float sensitivity = 100f;  // 카메라 감도
    public float clampAngle = 70f;  // 각도 제한

    private float rotX;   // 카메라 X축 회전
    private float rotY;  // 카메라 Y축 회전

    public Transform realCamera;   // 실제 카메라
    public Vector3 dirNormalized;  //실제 카메라가 있는 곳의 방향
    public Vector3 finalDir;      //최종 카메라 방향

    public float minDistance;    //카메라의 최소 거리
    public float maxDistance;   //카메라의 최대 거리
    public float RevertDistance; // 대화완료 시 카메라 거리 복구
    public float finalDistance; //카메라가 접근할 최종 거리

    public float smoothness; //카메라 이동속도


    private void Awake()
    {
        playerController = GetComponentInParent<PlayerController>();
        rotX = transform.localRotation.eulerAngles.x;
        rotY = transform.localRotation.eulerAngles.y;


        dirNormalized = realCamera.localPosition.normalized;
        finalDistance = realCamera.localPosition.magnitude;
        RevertDistance = maxDistance;
    }

    private void Update()
    {
        rotX += -(playerController.look.y) * sensitivity * Time.deltaTime;
        rotY += playerController.look.x * sensitivity * Time.deltaTime;

        rotX = Mathf.Clamp(rotX, -clampAngle, clampAngle);
        Quaternion rot = Quaternion.Euler(rotX, rotY, 0);
        transform.rotation = rot;
    }

    private void LateUpdate()
    {
        finalDir = transform.TransformPoint(dirNormalized * maxDistance);

        if (Physics.Linecast(transform.position, finalDir, out RaycastHit hit))
        {
            finalDistance = Mathf.Clamp(hit.distance, minDistance, maxDistance);
        }
        else
        {
            finalDistance = maxDistance;
        }
        realCamera.localPosition = Vector3.Lerp(realCamera.localPosition, dirNormalized* finalDistance, Time.deltaTime * smoothness);
    }
}
