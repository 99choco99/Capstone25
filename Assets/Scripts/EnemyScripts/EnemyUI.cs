using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyUI : MonoBehaviour
{
    EnemyStats enemyStats;
    Collider col;
    Camera mainCamera;


    [SerializeField] Slider PostureGauge;
    [SerializeField] Slider EnemyHpUI;

    [Header("강공격 알림")]
    [SerializeField] private CanvasGroup heavyAttackIndicator;
    [SerializeField] private float heavyIndicatorDuration = 1.0f;
    private Coroutine indicatorCoroutine;

    private void Awake()
    {
        enemyStats = GetComponentInParent<EnemyStats>();
        col = GetComponentInParent<Collider>();

        enemyStats.OnHpChanged += UpdateHp;
        enemyStats.OnPostureChanged += UpdatePostureGauge;
    }

    void OnEnable()
    {
        transform.position = col.bounds.center + new Vector3(0, col.bounds.extents.y, 0);
    }

    private void Start()
    {
        if (enemyStats != null)
        {
            //UpdateHp(enemyStats.CurrentHp, enemyStats.MaxHp);
            //UpdatePostureGauge(enemyStats.MaxPosture, enemyStats.CurrentPosture);
        }
        if (heavyAttackIndicator != null)
        {
            heavyAttackIndicator.alpha = 0f;
        }
    }

    private void OnDestroy()
    {
        enemyStats.OnHpChanged -= UpdateHp;
        enemyStats.OnPostureChanged -= UpdatePostureGauge;
    }

    void LateUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        else
        {
            transform.LookAt(mainCamera.transform);
            transform.rotation = mainCamera.transform.rotation;
        }

    }

    public void UpdatePostureGauge(float currentPosture, float maxPosture)
    {
        PostureGauge.maxValue = maxPosture;
        PostureGauge.value = currentPosture;
    }

    public void UpdateHp(float currenthp, float maxHp)
    {
        EnemyHpUI.value = currenthp / maxHp;
    }

    public void ShowHeavyAttackIndicator()
    {
        if (heavyAttackIndicator == null)
        {
            Debug.LogWarning("강공격 표시기가 EnemyUI에 할당되지 않음");
            return;
        }

        if (indicatorCoroutine != null)
        {
            StopCoroutine(indicatorCoroutine);
        }

        indicatorCoroutine = StartCoroutine(ShowIndicatorCoroutine());
    }


    private IEnumerator ShowIndicatorCoroutine()
    {
        heavyAttackIndicator.alpha = 1f;

        yield return new WaitForSeconds(heavyIndicatorDuration);

        heavyAttackIndicator.alpha = 0f;
        indicatorCoroutine = null;
    }
}
