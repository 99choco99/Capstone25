using UnityEngine;
using static UnityEditor.SceneView;


public enum CameraMode { Gameplay, Cinematic }

public class Legacy_PlayerCamera : MonoBehaviour
{
    public static Legacy_PlayerCamera Instance;
    public Player player;
    public Camera realCamera;                   // 실제 카메라
    public Transform cameraPivotTransform;      //pivot Transform


    [Header("Camera Setting")]
    private Vector3 cameraVelocity;
    public float followSpeed = 10f;             // 카메라 이동속도
    public float baseSensitivity = 50f;         // 카메라 감도
    public float minimumclampAngle = -30f;      // 최소 각도 제한
    public float maximumclampAngle = 60f;       // 최대 각도 제한
    public float MaximumLockAnlge = 20f;
    public float MinimumLockAngle = -20f;
    [SerializeField] LayerMask collideLayer;


    private float rotX;                         // 카메라 X축 회전
    public float rotY;                          // 카메라 Y축 회전

    [SerializeField] private float defaultCameraZPosition;
    public float cameraZPosition;
    private float targetCameraZPosition;
    private float cameraCollisionOffset = 0.2f;

    public float smoothness;                    //카메라 이동속도

    public CameraMode currentMode = CameraMode.Gameplay;
    private Transform cinematicTarget;



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

        if (currentMode == CameraMode.Cinematic)
        {
            if (cinematicTarget != null)
            {
                // 타겟 위치와 회전값으로 부드럽게 이동
                transform.position = Vector3.SmoothDamp(transform.position, cinematicTarget.position, ref cameraVelocity, Time.deltaTime * smoothness);
                transform.rotation = Quaternion.Slerp(transform.rotation, cinematicTarget.rotation, Time.deltaTime * smoothness);
            }
            return;
        }

        //플레이어 추적
        FollowPlayer();

        //카메라 회전
        if (player.IsLockOn)
        {
            UpdateLockOnRotation();
        }
        else
        {
            UpdateFreeRotation();
        }

        //충돌 처리
        HandleCameraCollisions();
    }

    //플레이어 추적
    public void FollowPlayer()
    {
        Vector3 targetPosition = Vector3.SmoothDamp(transform.position, player.transform.position, ref cameraVelocity, Time.deltaTime * smoothness);
        transform.position = targetPosition;
    }

    //자유시점 시 카메라 움직임
    public void UpdateFreeRotation()
    {
        float currentSensitivityMultiplier = SettingManager.instance.MouseSensitivity;

        rotX -= player.InputHandler.LookInput.y * baseSensitivity * currentSensitivityMultiplier * Time.deltaTime;
        rotY += player.InputHandler.LookInput.x * baseSensitivity * currentSensitivityMultiplier * Time.deltaTime;

        //상하 회전
        rotX = Mathf.Clamp(rotX, minimumclampAngle, maximumclampAngle);

        transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        cameraPivotTransform.localRotation = Quaternion.Euler(rotX, 0f, 0f);
    }

    //락온 시 카메라 움직임
    public void UpdateLockOnRotation()
    {
        Vector3 targetDirection = player.TargetingSystem.CurrentTarget.TargetTransform.position - transform.position;
        targetDirection.y = 0;
        targetDirection.Normalize();
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothness);

        targetDirection = player.TargetingSystem.CurrentTarget.TargetTransform.position - cameraPivotTransform.position;

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


    //카메라 충돌 처리
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

    // 연출 시작(특정 대상을 바라봄)
    public void StartCinematicFocus(Transform target)
    {
        currentMode = CameraMode.Cinematic;
        cinematicTarget = target;
    }

    // 연출 끝 (다시 플레이어에게 돌아옴)
    public void EndCinematicFocus()
    {
        currentMode = CameraMode.Gameplay;
        cinematicTarget = null;
        cameraVelocity = Vector3.zero; // 플레이어에게 돌아갈 때 부드럽게 복귀
    }

    public void ResetCameraZPostion()
    {
        cameraZPosition = defaultCameraZPosition;
    }


}
