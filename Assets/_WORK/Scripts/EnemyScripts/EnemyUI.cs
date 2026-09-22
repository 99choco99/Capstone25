using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyUI : MonoBehaviour
{
    [SerializeField] private EnemyStats enemyStats;

    [Header("적 상태 UI")]
    [SerializeField] private Slider postureGauge;
    [SerializeField] private Slider healthGauge;
    [SerializeField] private Transform lifeContainer;
    [SerializeField] private Image lifeIconPrefab;

    private List<Image> lifeIcons = new();

    private Transform mainCameraTransform;

    private void Start()
    {
        EnemyStats stats = GetComponentInParent<EnemyStats>();
        Bind(stats);

        UnityEngine.Camera mainCamera = UnityEngine.Camera.main;
        mainCameraTransform = mainCamera != null ? mainCamera.transform : null;

        for (int i = 0; i < enemyStats.MaxLife; i++)
        {
            Image icon = Instantiate(lifeIconPrefab, lifeContainer);
            lifeIcons.Add(icon);
        }
    }

    public void Bind(EnemyStats stats)
    {
        Unsubscribe();
        enemyStats = stats;

        if (enemyStats == null) return;

        enemyStats.OnHpChanged += UpdateHealth;
        enemyStats.OnPostureChanged += UpdatePosture;
        enemyStats.OnLifeChanged += UpdateLife;
        enemyStats.OnDeath += Hide;

        UpdateHealth(enemyStats.CurrentHp, enemyStats.MaxHp.GetValue());
        UpdatePosture(enemyStats.CurrentPosture, enemyStats.MaxPosture.GetValue());
        UpdateLife(enemyStats.CurrentLife, enemyStats.MaxLife);
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

    private void UpdateLife(int currentLife, int maxLife)
    {
        for (int i = 0; i < lifeIcons.Count; i++)
        {
            Color color = lifeIcons[i].color;
            color.a = i < currentLife ? 1f : 0.2f;
            lifeIcons[i].color = color;
        }
    }

    /// <summary>사망 시 적의 상태 UI를 숨기기</summary>
    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Unsubscribe()
    {
        if (enemyStats == null) return;

        enemyStats.OnHpChanged -= UpdateHealth;
        enemyStats.OnPostureChanged -= UpdatePosture;
        enemyStats.OnLifeChanged -= UpdateLife;
        enemyStats.OnDeath -= Hide;
    }
}
