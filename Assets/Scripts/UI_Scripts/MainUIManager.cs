using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;



public enum UIPanelType { 
    Quest,
    Market,
    Inventory,
    Profile,
    Setting,
    Dialogue
}
public class MainUIManager : MonoBehaviour
{
    public static MainUIManager Instance { get; private set; }

    [SerializeField] private GameObject InGameUIGroup;
    private PlayerStats playerStats;

    [Header("상태 UI")]
    public Slider PlayerHpUI;
    public Slider PostureGauge;
    public Slider ExpUI;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI ExpText;


    [Header("적 UI")]
    public Slider EnemyHpUI;
    public TextMeshProUGUI EnemyName;

    [Header("패널 UI")]
    [SerializeField] GameObject Market;
    [SerializeField] private List<UIBase> uiPanels;
    private Dictionary<UIPanelType, UIBase> panelDictionary;
    public List<UIPanelType> currentOpenUI = new List<UIPanelType>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        Player.OnLocalPlayerSpawned += ConnectLocalPlayerUI;

        if (MarketManager.Instance != null)
        {
            Market = MarketManager.Instance.MarketUI;
        }

        // 딕셔너리에 UI 패널들을 등록
        panelDictionary = new Dictionary<UIPanelType, UIBase>();
        foreach(var panel in uiPanels)
        {
            if (!panelDictionary.ContainsKey(panel.panelType)){
                panelDictionary.Add(panel.panelType, panel);
                panel.Init();
                panel.gameObject.SetActive(false);
            }
        }

        HideInGameUI();
    }

    private void OnDestroy()
    {
        Player.OnLocalPlayerSpawned -= ConnectLocalPlayerUI;
        UnsubscribeFromStats();
    }

    private void ConnectLocalPlayerUI(Transform playerTransform)
    {
        UnsubscribeFromStats();

        playerStats = playerTransform.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.OnHpChanged += UpdateHp;
            playerStats.OnPostureChanged += UpdatePostureGauge;
            playerStats.OnExpChanged += UpdateExp;

            // 초기값 세팅
            UpdateHp(playerStats.CurrentHp, playerStats.MaxHp.GetValue());
            UpdatePostureGauge(playerStats.CurrentPosture, playerStats.MaxPosture.GetValue());
            UpdateExp(playerStats.Exp, playerStats.Level);
        }

        ShowInGameUI();
    }


    private void UnsubscribeFromStats()
    {
        if (playerStats != null)
        {
            playerStats.OnHpChanged -= UpdateHp;
            playerStats.OnPostureChanged -= UpdatePostureGauge;
            playerStats.OnExpChanged -= UpdateExp;
        }
    }

    public void ShowInGameUI() => InGameUIGroup.SetActive(true);
    public void HideInGameUI() => InGameUIGroup.SetActive(false);
    public void UpdateHp(float currenthp, float maxHp) => PlayerHpUI.value = currenthp / maxHp;


    public void UpdateExp(int exp, int level)
    {
        float maxExp = DataManager.Instance.GetMaxExpForLevel(level);
        ExpUI.value = exp / maxExp;
        levelText.text = $"Lv. {level}";
        ExpText.text = $"{(ExpUI.value / ExpUI.maxValue) * 100}%";
    }

    public void UpdatePostureGauge(float currentPosture, float maxPosture)
    {
        PostureGauge.maxValue = maxPosture;
        PostureGauge.value = currentPosture;
    }

    public void ShowEnemyInfoUI()
    {
        EnemyHpUI.gameObject.SetActive(true);
        EnemyName.gameObject.SetActive(true);
        StartCoroutine(HideEnemyInfoUI());
    }
    IEnumerator HideEnemyInfoUI()
    {
        yield return new WaitForSeconds(4f);
        EnemyHpUI.gameObject.SetActive(false);
        EnemyName.gameObject.SetActive(false);
    }

    public void ToggleUI(UIPanelType type)
    {
        if (panelDictionary.TryGetValue(type, out UIBase panel))
        {
            if (panel.IsOpen)
            {
                CloseUI(type);
            }
            else
            {
                OpenUI(type);
            }
        }
    }


    public void OpenUI(UIPanelType type)
    {
        if(panelDictionary.TryGetValue(type, out UIBase panel)){
            panel.Open();
            currentOpenUI.Add(type);
        }

    }

    public void CloseUI(UIPanelType type)
    {
        if(panelDictionary.TryGetValue(type,out UIBase panel))
        {
            panel.Close();
            TooltipManager.Instance.HideTooltip();
            currentOpenUI.Remove(type);
        }
    }

    public void CloseLastUI()
    {
        if (currentOpenUI.Count > 0)
        {
            UIPanelType lastPanel = currentOpenUI.Last();
            CloseUI(lastPanel);
        }
    }

}

