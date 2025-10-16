using UnityEngine;
using UnityEngine.UI;

public class EnemyUI : MonoBehaviour
{
    EnemyStats enemyStats;
    Collider col;
    Camera mainCamera;


    [SerializeField] Slider PostureGauge;
    [SerializeField] Slider EnemyHpUI;

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
            UpdateHp(enemyStats.currentHp);
            UpdatePostureGauge(enemyStats.maxPosture, enemyStats.currentPosture);
        }
    }

    private void OnDestroy()
    {
        enemyStats.OnHpChanged -= UpdateHp;
        enemyStats.OnPostureChanged -= UpdatePostureGauge;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if(mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        transform.LookAt(mainCamera.transform);
    }

    public void UpdatePostureGauge(float currentPosture, float maxPosture)
    {
        PostureGauge.maxValue = maxPosture;
        PostureGauge.value = currentPosture;
    }

    public void UpdateHp(float currenthp)
    {
        EnemyHpUI.value = currenthp / enemyStats.maxHp;
    }

}
