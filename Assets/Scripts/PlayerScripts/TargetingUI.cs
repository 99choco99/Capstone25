using UnityEngine;
using UnityEngine.UI;

public class TargetingUI : MonoBehaviour
{
    [SerializeField] private Image targetMarker; // 타겟 위에 표시될 마커 이미지
    [SerializeField] private Image executeMarker; // 암살 가능 시 표시될 마커 이미지

    private TargetingSystem targetingSystem;
    private UnityEngine.Camera mainCamera;


    public void Init(TargetingSystem system)
    {
        targetingSystem = system;
        mainCamera = UnityEngine.Camera.main;

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

            targetMarker.enabled = (screenPos.z > 0);

            bool shouldBeActive = (screenPos.z > 0);
            if (targetMarker.gameObject.activeSelf != shouldBeActive)
            {
                targetMarker.gameObject.SetActive(shouldBeActive);
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


