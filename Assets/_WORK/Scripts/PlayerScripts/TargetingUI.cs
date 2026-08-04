using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TargetingUI : MonoBehaviour
{
    [SerializeField] private Image targetMarker;
    [SerializeField] private Image executeMarker;

    [Header("위험 공격 경고")]
    [Tooltip("비워두면 런타임에 붉은 危(폰트 미지원 시 !) 텍스트를 만들어 사용합니다.")]
    [SerializeField] private Graphic specialAttackMarker;

    private TargetingSystem targetingSystem;
    private UnityEngine.Camera mainCamera;

    private void Awake()
    {
        CreateFallbackSpecialAttackMarker();

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
        UpdateSpecialAttackMarker();
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

        Transform point = currentTarget.LockOnPoint != null? currentTarget.LockOnPoint: currentTarget.TargetTransform;
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(point.position);
        bool visible = screenPosition.z > 0f;

        SetMarkerVisible(targetMarker, visible);
        if (visible) targetMarker.rectTransform.position = screenPosition;
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

        Transform point = deathblowTarget.LockOnPoint != null? deathblowTarget.LockOnPoint: deathblowTarget.transform;
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(point.position);
        bool visible = screenPosition.z > 0f;

        SetMarkerVisible(executeMarker, visible);
        if (visible) executeMarker.rectTransform.position = screenPosition;
    }

    /// <summary>
    /// 가드 불가 기술 표시
    /// </summary>
    private void UpdateSpecialAttackMarker()
    {
        if (specialAttackMarker == null)
            return;

        if (targetingSystem.CurrentTarget is not Enemy enemy || enemy.Combat == null || !enemy.Combat.IsSpecialAttack)
        {
            SetMarkerVisible(specialAttackMarker, false);
            specialAttackMarker.rectTransform.localScale = Vector3.one;
            return;
        }

        Transform point = enemy.LockOnPoint != null? enemy.LockOnPoint: enemy.transform;
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(point.position + Vector3.up * 0.65f);
        bool visible = screenPosition.z > 0f;

        SetMarkerVisible(specialAttackMarker, visible);
        if (visible)
        {
            specialAttackMarker.rectTransform.position = screenPosition;

            // 경고가 화면에 묻히지 않도록 시간 배율과 무관한 작은 맥박을 줍니다.
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 11f) * 0.12f;
            specialAttackMarker.rectTransform.localScale = Vector3.one * pulse;
        }
    }

    /// <summary>
    /// 씬의 specialAttackMarker 슬롯이 비어 있어도 위험 공격 기능 자체를 시험할 수 있게
    /// 같은 Canvas 아래에 최소한의 텍스트 표식을 만듭니다. 최종 아트가 연결되면 실행되지 않습니다.
    /// </summary>
    private void CreateFallbackSpecialAttackMarker()
    {
        if (specialAttackMarker != null)
            return;

        GameObject markerObject = new(
            "SpecialAttackMarker_Runtime",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        markerObject.transform.SetParent(transform, worldPositionStays: false);

        TextMeshProUGUI text = markerObject.GetComponent<TextMeshProUGUI>();
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        bool supportsDangerCharacter =
            font != null && font.HasCharacter('危', searchFallbacks: true, tryAddCharacter: false);

        text.font = font;
        text.text = supportsDangerCharacter ? "危" : "!";
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.fontSize = 72f;
        text.color = new Color(0.95f, 0.08f, 0.04f, 1f);
        text.outlineColor = new Color32(30, 0, 0, 255);
        text.outlineWidth = 0.18f;
        text.raycastTarget = false;

        RectTransform rect = text.rectTransform;
        rect.sizeDelta = new Vector2(100f, 100f);
        rect.localScale = Vector3.one;

        specialAttackMarker = text;
        markerObject.SetActive(false);
    }

    private void HideMarkers()
    {
        SetMarkerVisible(targetMarker, false);
        SetMarkerVisible(executeMarker, false);
        SetMarkerVisible(specialAttackMarker, false);
    }

    private static void SetMarkerVisible(Graphic marker, bool visible)
    {
        if (marker != null && marker.gameObject.activeSelf != visible)
            marker.gameObject.SetActive(visible);
    }
}
