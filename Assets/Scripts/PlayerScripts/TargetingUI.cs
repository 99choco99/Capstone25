using UnityEngine;
using UnityEngine.UI;

public class TargetingUI : MonoBehaviour
{
    [SerializeField] private SpriteRenderer targetMarker; // 타겟 위에 표시될 마커 이미지
    [SerializeField] private SpriteRenderer executeMarker; // 암살 가능 시 표시될 마커 이미지

    private TargetingSystem targetingSystem;
    private Collider targetCollider;
    private Camera mainCamera;


    private void Awake()
    {
        targetingSystem = GetComponentInParent<TargetingSystem>();
        mainCamera = Camera.main;

        targetingSystem.OnChangedTarget += HandleTargetSelected;
        targetingSystem.OnTargetDeselected += HandleTargetDeselected;

        targetMarker.gameObject.SetActive(false);
        executeMarker.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (targetingSystem != null)
        {
            targetingSystem.OnChangedTarget -= HandleTargetSelected;
            targetingSystem.OnTargetDeselected -= HandleTargetDeselected;
        }
    }
    private void LateUpdate()
    {

        if (targetingSystem.CurrentTarget != null)
        {
            Vector3 targetTopPosition = targetCollider.bounds.center;
            targetTopPosition += new Vector3(0, 0.2f, 0);

            targetMarker.transform.position = targetTopPosition;
            executeMarker.transform.position = targetTopPosition + new Vector3(0, 0.5f, 0);

            targetMarker.transform.LookAt(mainCamera.transform);
            executeMarker.transform.LookAt(mainCamera.transform);

            bool canExecute = targetingSystem.IsCurrentTargetExecutable();
            executeMarker.gameObject.SetActive(canExecute);
        }
    }

    private void HandleTargetSelected(IDamageable target)
    {
        targetMarker.gameObject.SetActive(true);

        if (targetingSystem.CurrentTarget != null)
        {
            targetCollider = targetingSystem.CurrentTarget.gameObject.GetComponent<Collider>();
        }
    }

    private void HandleTargetDeselected()
    {
        targetMarker.gameObject.SetActive(false);
        executeMarker.gameObject.SetActive(false);
        targetCollider = null;
    }
}


