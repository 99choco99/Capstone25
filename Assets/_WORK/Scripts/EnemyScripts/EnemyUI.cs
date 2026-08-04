using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class EnemyUI : MonoBehaviour
{
    [SerializeField] private EnemyStats enemyStats;

    [Header("적 상태 UI")]
    [SerializeField] private Slider postureGauge;
    [SerializeField] private Slider healthGauge;

    private Transform mainCameraTransform;

    private void Start()
    {
        EnemyStats stats = GetComponentInParent<EnemyStats>();
        Bind(stats);

        UnityEngine.Camera mainCamera = UnityEngine.Camera.main;
        mainCameraTransform = mainCamera != null ? mainCamera.transform : null;
    }

    public void Bind(EnemyStats stats)
    {
        Unsubscribe();
        enemyStats = stats;

        if (enemyStats == null) return;

        enemyStats.OnHpChanged += UpdateHealth;
        enemyStats.OnPostureChanged += UpdatePosture;

        UpdateHealth(enemyStats.CurrentHp, enemyStats.MaxHp.GetValue());
        UpdatePosture(enemyStats.CurrentPosture, enemyStats.MaxPosture.GetValue());
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void LateUpdate()
    {
        if (mainCameraTransform != null)
            transform.forward = mainCameraTransform.forward;
    }

    private void UpdatePosture(float currentPosture, float maxPosture)
    {
        if (postureGauge == null) return;

        postureGauge.maxValue = Mathf.Max(1f, maxPosture);
        postureGauge.value = Mathf.Max(0f, currentPosture);
    }

    private void UpdateHealth(float currentHealth, float maxHealth)
    {
        if (healthGauge == null) return;

        healthGauge.value = maxHealth > 0f? Mathf.Clamp01(currentHealth / maxHealth): 0f;
    }

    private void Unsubscribe()
    {
        if (enemyStats == null) return;

        enemyStats.OnHpChanged -= UpdateHealth;
        enemyStats.OnPostureChanged -= UpdatePosture;
    }
}
