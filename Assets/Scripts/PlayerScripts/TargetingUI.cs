using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(TargetingSystem))]
public class TargetingUI : MonoBehaviour
{
    [SerializeField] private Image targetMarker; // 타겟 위에 표시될 마커 이미지
    [SerializeField] private Image executeMarker; // 암살 가능 시 표시될 마커 이미지

    [SerializeField] private TargetingSystem targetingSystem;

    private Camera mainCamera;


    private void Awake()
    {
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
            Vector3 targetTopPosition = targetingSystem.CurrentTarget.TargetTransform.position;
            targetTopPosition += new Vector3(0, 0.1f, 0);
            Vector3 screenPos = mainCamera.WorldToScreenPoint(targetTopPosition);

            if(screenPos.z > 0)
            {
                targetMarker.rectTransform.position = screenPos;
                targetMarker.gameObject.SetActive(true);
            }
            else
            {
                targetMarker.gameObject.SetActive(false);
            }

            bool canExecute = targetingSystem.IsCurrentTargetExecutable();
            executeMarker.gameObject.SetActive(canExecute);
            targetMarker.gameObject.SetActive(!canExecute);
            executeMarker.rectTransform.position = screenPos;
        }
    }

    private void HandleTargetSelected(ITargetable target)
    {
        targetMarker.gameObject.SetActive(true);
    }

    private void HandleTargetDeselected()
    {
        targetMarker.gameObject.SetActive(false);
        executeMarker.gameObject.SetActive(false);

    }
}


