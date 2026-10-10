using UnityEngine;
using UnityEngine.UI;

public class TargetingUI : MonoBehaviour
{
    [SerializeField] private Image targetMarker;
    [SerializeField] private Image executeMarker;

    private TargetingSystem targetingSystem;
    private UnityEngine.Camera mainCamera;

    private void Awake()
    {
        Player.OnLocalPlayerSpawned -= Init;
        Player.OnLocalPlayerSpawned += Init;
    }

    private void Start()
    {
        // UI가 플레이어보다 늦게 생성돼 스폰 이벤트를 놓친 경우에도 연결합니다.
        if (Player.LocalPlayer != null)
            Init(Player.LocalPlayer);
    }

    private void OnDestroy()
    {
        Player.OnLocalPlayerSpawned -= Init;
        if (targetingSystem != null)
            targetingSystem.TargetChanged -= HandleTargetChanged;
    }

    public void Init(Player localPlayer)
    {
        if (localPlayer == null) return;

        if (targetingSystem != null)
            targetingSystem.TargetChanged -= HandleTargetChanged;

        targetingSystem = localPlayer.TargetingSystem;
        mainCamera = UnityEngine.Camera.main;

        if (targetingSystem != null)
            targetingSystem.TargetChanged += HandleTargetChanged;

        HideMarkers();
    }

    private void HandleTargetChanged(ITargetable target)
    {
        if (target == null)
            HideMarkers();
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
            mainCamera = UnityEngine.Camera.main;

        if (targetingSystem == null || mainCamera == null)
        {
            HideMarkers();
            return;
        }

        bool hasDeathblow = targetingSystem.GetDeathblowPlanForUI(out DeathblowPlan plan);

        UpdateTargetMarker(hasDeathblow ? plan.Target : null);
        UpdateExecuteMarker(hasDeathblow ? plan.Target : null);
    }

    /// <summary>
    /// 타겟팅 표시
    /// </summary>
    /// <param name="deathblowTarget"></param>
    private void UpdateTargetMarker(Enemy deathblowTarget)
    {
        ITargetable currentTarget = targetingSystem.CurrentTarget;
        bool replacedByDeathblowMarker = deathblowTarget != null && currentTarget is Enemy currentEnemy && currentEnemy == deathblowTarget;

        if (currentTarget == null || replacedByDeathblowMarker)
        {
            SetMarkerVisible(targetMarker, false);
            return;
        }

        Transform point = currentTarget.LockOnPoint != null ? currentTarget.LockOnPoint : currentTarget.TargetTransform;
        UpdateMarker(targetMarker, point);
    }

    /// <summary>
    /// 인살 마크
    /// </summary>
    private void UpdateExecuteMarker(Enemy deathblowTarget)
    {
        if (deathblowTarget == null)
        {
            SetMarkerVisible(executeMarker, false);
            return;
        }

        Transform point = deathblowTarget.LockOnPoint != null ? deathblowTarget.LockOnPoint : deathblowTarget.transform;
        UpdateMarker(executeMarker, point);
    }

    private void UpdateMarker(Image marker, Transform point)
    {
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(point.position);
        bool visible = screenPosition.z > 0f;

        SetMarkerVisible(marker, visible);
        if (visible) marker.rectTransform.position = screenPosition;
    }

    private void HideMarkers()
    {
        SetMarkerVisible(targetMarker, false);
        SetMarkerVisible(executeMarker, false);
    }

    private static void SetMarkerVisible(Graphic marker, bool visible)
    {
        if (marker != null && marker.gameObject.activeSelf != visible)
            marker.gameObject.SetActive(visible);
    }
}
