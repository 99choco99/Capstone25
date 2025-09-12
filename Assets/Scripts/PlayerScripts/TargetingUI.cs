using UnityEngine;
using UnityEngine.UI;

public class TargetingUI : MonoBehaviour
{
    [SerializeField] private Image targetMarker; // 타겟 위에 표시될 마커 이미지
    [SerializeField] private Image executeMarker; // 암살 가능 시 표시될 마커 이미지

    private TargetingSystem targetingSystem;
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

    private void Update()
    {
        if (targetingSystem.CurrentTarget != null)
        {
            Vector3 screenPos = mainCamera.WorldToScreenPoint(targetingSystem.CurrentTarget.transform.position);
            targetMarker.transform.position = screenPos;
            executeMarker.transform.position = screenPos + new Vector3(0,2,0);

        }
    }

    private void HandleTargetSelected(IDamageable target)
    {
        targetMarker.gameObject.SetActive(true);
    }

    private void HandleTargetDeselected()
    {
        targetMarker.gameObject.SetActive(false);
        executeMarker.gameObject.SetActive(false);
    }
}


