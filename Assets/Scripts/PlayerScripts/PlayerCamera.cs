using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public static PlayerCamera Instance;
    public Player player;
    public Camera realCamera;   // 실제 카메라
    public Transform cameraPivotTransform; //pivot Transform


    [Header("Camera Setting")]
    private Vector3 cameraVelocity;
    public float followSpeed = 10f;  // 카메라 이동속도
    public float sensitivity = 50f;  // 카메라 감도
    public float minimumclampAngle = -30f;  // 각도 제한
    public float maximumclampAngle = 60f;  // 각도 제한
    public float MaximumLockAnlge = 20f;
    public float MinimumLockAngle = -20f;
    [SerializeField] LayerMask collideLayer;


    private float rotX;   // 카메라 X축 회전
    public float rotY;  // 카메라 Y축 회전

    [SerializeField] private float defaultCameraZPosition;
    public float cameraZPosition;
    private float targetCameraZPosition;
    private float cameraCollisionOffset = 0.2f;

    public float smoothness; //카메라 이동속도





    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(Instance);
        }
        else
        {
            Destroy(this);
        }


        rotX = transform.rotation.eulerAngles.x;
        rotY = transform.rotation.eulerAngles.y;


        cameraZPosition = defaultCameraZPosition;
    }


    private void LateUpdate()
    {
        if (player == null) { return; }
        if (player.isLockOn)
        {
            Vector3 targetDirection = player.TargetingSystem.CurrentTarget.transform.position - transform.position;
            targetDirection.y = 0;
            targetDirection.Normalize();
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothness);

            targetDirection = player.TargetingSystem.CurrentTarget.transform.position - cameraPivotTransform.position;
            
            targetDirection.Normalize();
            targetRotation = Quaternion.LookRotation(targetDirection);


            Vector3 eulerAngles = targetRotation.eulerAngles;

            if (eulerAngles.x > 180)
            {
                eulerAngles.x -= 360;
            }

            eulerAngles.x = Mathf.Clamp(eulerAngles.x, MinimumLockAngle, MaximumLockAnlge);
            Quaternion clampedRotation = Quaternion.Euler(eulerAngles);

            cameraPivotTransform.rotation = Quaternion.Slerp(cameraPivotTransform.rotation, clampedRotation, Time.deltaTime * smoothness);

            rotY = transform.eulerAngles.y;
            rotX = cameraPivotTransform.localEulerAngles.x;
            if (rotX > 180) rotX -= 360;
        }
        else
        {
            rotX -= player.InputHandler.LookInput.y * sensitivity * Time.deltaTime;
            rotY += player.InputHandler.LookInput.x * sensitivity * Time.deltaTime;
            rotX = Mathf.Clamp(rotX, minimumclampAngle, maximumclampAngle);

            Vector3 cameraRotation = Vector3.zero;
            Quaternion targetRotation;

            cameraRotation.y = rotY;
            targetRotation = Quaternion.Euler(cameraRotation);
            transform.rotation = targetRotation;

            cameraRotation = Vector3.zero;
            cameraRotation.x = rotX;
            targetRotation = Quaternion.Euler(cameraRotation);
            cameraPivotTransform.localRotation = targetRotation;
        }

        Vector3 targetCameraPoistion = Vector3.SmoothDamp(transform.position, player.transform.position, ref cameraVelocity, Time.deltaTime * smoothness);
        transform.position = targetCameraPoistion;


        HandleCameraCollisions();
    }

    private void HandleCameraCollisions()
    {

        targetCameraZPosition = cameraZPosition;
        Vector3 direction = realCamera.transform.position - cameraPivotTransform.position;
        direction.Normalize();

        if (Physics.SphereCast(cameraPivotTransform.position, cameraCollisionOffset, direction, out var hit, Mathf.Abs(targetCameraZPosition), collideLayer))
        {
            float distanceFromHit = Vector3.Distance(cameraPivotTransform.position, hit.point);
            targetCameraZPosition = -(distanceFromHit - cameraCollisionOffset);
        }

        if (Mathf.Abs(targetCameraZPosition) < cameraCollisionOffset)
        {
            targetCameraZPosition = -cameraCollisionOffset;
        }

        Vector3 newCameraLocalPosition = realCamera.transform.localPosition;
        newCameraLocalPosition.z = Mathf.Lerp(realCamera.transform.localPosition.z, targetCameraZPosition, 0.2f); // Lerp의 세 번째 인자는 시간보다 보간 계수로 사용하는 것이 더 직관적일 수 있습니다.
        realCamera.transform.localPosition = newCameraLocalPosition;

    }


    public void ResetCameraZPostion()
    {
        cameraZPosition = defaultCameraZPosition;
    }
}
