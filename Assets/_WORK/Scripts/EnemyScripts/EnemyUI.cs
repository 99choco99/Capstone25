using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyUI : MonoBehaviour
{
    private EnemyStats enemyStats;
    private Transform mainCameraTransform;

    [Header("적 상태 UI")]
    [SerializeField] Slider PostureGauge;
    [SerializeField] Slider EnemyHpUI;

    [Header("강공격 알림")]
    [SerializeField] private CanvasGroup heavyAttackIndicator;


    public void Init(EnemyStats stats)
    {
        if(stats != null)
        {
            enemyStats.OnHpChanged -= UpdateHp;
            enemyStats.OnPostureChanged -= UpdatePostureGauge;
        }
        enemyStats = stats;

        if (enemyStats != null)
        {
            enemyStats.OnHpChanged += UpdateHp;
            enemyStats.OnPostureChanged += UpdatePostureGauge;

            UpdateHp(enemyStats.CurrentHp, enemyStats.MaxHp.GetValue());
            UpdatePostureGauge(enemyStats.CurrentPosture, enemyStats.MaxPosture.GetValue());
        }

        HideHeavyAttackIndicator();

        if (UnityEngine.Camera.main != null) mainCameraTransform = UnityEngine.Camera.main.transform;
    }
    

    private void OnDestroy()
    {
        if(enemyStats != null)
        {
            enemyStats.OnHpChanged -= UpdateHp;
            enemyStats.OnPostureChanged -= UpdatePostureGauge;
        }
    }
    void LateUpdate()
    {
        if (mainCameraTransform != null) transform.forward = mainCameraTransform.forward;
    }

    public void UpdatePostureGauge(float currentPosture, float maxPosture)
    {

        if(PostureGauge == null) { return; }

        if (currentPosture <= 0f)
        {
            PostureGauge.value = 0f;
            return;
        }

        PostureGauge.maxValue = maxPosture;
        PostureGauge.value = currentPosture;
    }

    public void UpdateHp(float currenthp, float maxHp)
    {
        if (EnemyHpUI == null) return;

        if (maxHp <= 0f)
        {
            EnemyHpUI.value = 0f;
            return;
        }
        EnemyHpUI.value = currenthp / maxHp;
    }

    public void ShowHeavyAttackIndicator()
    {
        if (heavyAttackIndicator != null) heavyAttackIndicator.alpha = 1f;
    }
    public void HideHeavyAttackIndicator()
    {
        if (heavyAttackIndicator != null) heavyAttackIndicator.alpha = 0f;
    }
}
