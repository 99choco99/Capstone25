using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public static PlayerCamera Instance;
    public Player player;
    public Camera realCamera;   // 실제 카메라
    [SerializeField] Transform cameraPivotTransform; //pivot Transform

    [Header("Camera Setting")]
    private Vector3 cameraVelocity;
    public float followSpeed = 10f;  // 카메라 이동속도
    public float sensitivity = 50f;  // 카메라 감도
    public float minimumclampAngle = -30f;  // 각도 제한
    public float maximumclampAngle = 60f;  // 각도 제한

    private float rotX;   // 카메라 X축 회전
    public float rotY;  // 카메라 Y축 회전

    private float cameraZPosition;
    private float targetCameraZPosition;
    private float cameraCollisionOffset = 0.2f;

    public float smoothness; //카메라 이동속도



    public bool isLockOn;
    private Transform currentTarget;



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

        cameraZPosition = realCamera.transform.localPosition.z;
    }

    private void Start()
    {
        player.TargetingSystem.OnChangedTarget += HandleTargetChanged;
        player.TargetingSystem.OnTargetDeselected += HandleTargetDeselected;
    }

    private void OnDisable()
    {
        player.TargetingSystem.OnChangedTarget -= HandleTargetChanged;
        player.TargetingSystem.OnTargetDeselected -= HandleTargetDeselected;
    }

    private void Update()
    {
        if (!isLockOn)
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

    }

    private void LateUpdate()
    {
        Vector3 targetCameraPoistion = Vector3.SmoothDamp(transform.position, player.transform.position, ref cameraVelocity, Time.deltaTime * smoothness);
        transform.position = targetCameraPoistion;


        if (isLockOn && currentTarget != null)
        {
            Vector3 directionToTarget = (currentTarget.transform.position - transform.position).normalized;
            directionToTarget.y = 0;
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothness * Time.deltaTime);


            rotY = transform.eulerAngles.y;
            rotX = transform.eulerAngles.x;

        }
        HandleCameraCollisions();
    }

    private void HandleCameraCollisions()
    {

        targetCameraZPosition = cameraZPosition;
        RaycastHit hit;
        Vector3 direction = realCamera.transform.position - cameraPivotTransform.position;
        direction.Normalize();

        if (Physics.SphereCast(cameraPivotTransform.position, cameraCollisionOffset, direction, out hit, Mathf.Abs(targetCameraZPosition)))
        {
            float distanceFromHit = Vector3.Distance(cameraPivotTransform.position, hit.transform.position);
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

    private void HandleTargetChanged(IDamageable target)
    {
        isLockOn = true;
        currentTarget = target.transform;
    }

    private void HandleTargetDeselected()
    {
        isLockOn = false;
        currentTarget = null;
    }

}
