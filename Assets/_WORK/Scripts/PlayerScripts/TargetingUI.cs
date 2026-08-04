using UnityEngine;
using UnityEngine.UI;

public class TargetingUI : MonoBehaviour
{
    [SerializeField] private Image targetMarker; // 타겟 위에 표시될 마커 이미지
    [SerializeField] private Image executeMarker; // 암살 가능 시 표시될 마커 이미지

    private TargetingSystem targetingSystem;
    private UnityEngine.Camera mainCamera;

    private void Awake()
    {
        Player.OnLocalPlayerSpawned -= Init;
        Player.OnLocalPlayerSpawned += Init;
    }

    private void OnDestroy()
    {
        Player.OnLocalPlayerSpawned -= Init;
        if (targetingSystem != null)
        {
            targetingSystem.OnTargetDeselected -= HideMarkers;
        }
    }

    public void Init(Player localPlayer)
    {
        if (localPlayer == null) return;

        targetingSystem = localPlayer.TargetingSystem;
        mainCamera = UnityEngine.Camera.main;

        if (targetingSystem != null)
        {
            targetingSystem.OnTargetDeselected -= HideMarkers;
            targetingSystem.OnTargetDeselected += HideMarkers;
        }
        HideMarkers();
    }


    private void LateUpdate()
    {

        if (targetingSystem == null || targetingSystem.CurrentTarget == null || mainCamera == null)
        {
            HideMarkers();
            return;
        }

        Vector3 targetPos = targetingSystem.CurrentTarget.LockOnPoint.position;
        Vector3 screenPos = mainCamera.WorldToScreenPoint(targetPos);

        bool isBehindCamera = screenPos.z <= 0;
        bool canExecute = targetingSystem.IsCurrentTargetExecutable();
        bool showTargetMarker = !isBehindCamera && !canExecute;
        bool showExecuteMarker = !isBehindCamera && canExecute;

        if (targetMarker.gameObject.activeSelf != showTargetMarker)
            targetMarker.gameObject.SetActive(showTargetMarker);

        if (executeMarker.gameObject.activeSelf != showExecuteMarker)
            executeMarker.gameObject.SetActive(showExecuteMarker);


        if (showTargetMarker) targetMarker.rectTransform.position = screenPos;
        if (showExecuteMarker) executeMarker.rectTransform.position = screenPos;
    }

    private void HideMarkers()
    {
        if (targetMarker != null && targetMarker.gameObject.activeSelf)
            targetMarker.gameObject.SetActive(false);
        if (executeMarker != null && executeMarker.gameObject.activeSelf)
            executeMarker.gameObject.SetActive(false);
    }
}


