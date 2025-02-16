using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    PlayerInputs input;
    public Transform objectToFollow;
    public float followSpeed = 10f;  // 카메라 이동속도
    public float sensitivity = 100f;  // 카메라 감도
    public float clampAngle = 70f;  // 각도 제한

    private float rotX;
    private float rotY;

    public Transform realCamera;
    public Vector3 dirNormalized;  //실제 카메라가 있는 곳의 방향
    public Vector3 finalDir;

    public float minDistnace;
    public float maxDistnace;
    public float finalDistance;

    public float smoothness = 10f;


    private void Awake()
    {
        input = GetComponentInParent<PlayerInputs>();
        rotX = transform.localRotation.eulerAngles.x;
        rotY = transform.localRotation.eulerAngles.y;

        dirNormalized = realCamera.localPosition.normalized;
        finalDistance = realCamera.localPosition.magnitude;
    }

    private void Update()
    {
        rotX += -(input.look.y) * sensitivity * Time.deltaTime;
        rotY += input.look.x * sensitivity * Time.deltaTime;

        rotX = Mathf.Clamp(rotX, -clampAngle, clampAngle);
        Quaternion rot = Quaternion.Euler(rotX, rotY, 0);
        transform.rotation = rot;
    }

    private void LateUpdate()
    {
        transform.position = Vector3.MoveTowards(transform.position, objectToFollow.position, followSpeed * Time.deltaTime);

        finalDir = transform.TransformPoint(dirNormalized * maxDistnace);

        RaycastHit hit;

        if (Physics.Linecast(transform.position, finalDir, out hit))
        {
            finalDistance = Mathf.Clamp(hit.distance, minDistnace, maxDistnace);
        }
        else
        {
            finalDistance = maxDistnace;
        }
        realCamera.localPosition = Vector3.Lerp(realCamera.localPosition, dirNormalized* finalDistance, Time.deltaTime * smoothness);
    }
}
