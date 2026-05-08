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
    public GameObject Inventory;
    public GameObject PlayerProfile;
    public GameObject Setting;
    public GameObject Quest;
    public GameObject dialogUI;
    private Dictionary<UIPanelType, GameObject> panelDictionary;
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
        PlayerStats.OnLocalPlayerHpChanged += UpdateHp;
        PlayerStats.OnLocalPlayerExpChanged += UpdateExp;
        PlayerStats.OnLocalPlayerPostureChanged += UpdatePostureGauge;
        if (MarketManager.Instance != null)
        {
            Market = MarketManager.Instance.MarketUI;
        }

        // 딕셔너리에 UI 패널들을 등록
        panelDictionary = new Dictionary<UIPanelType, GameObject>()
        {
            {UIPanelType.Market,Market},
            { UIPanelType.Inventory, Inventory },
            { UIPanelType.Profile, PlayerProfile },
            { UIPanelType.Setting, Setting },
            {UIPanelType.Quest, Quest },
            {UIPanelType.Dialogue, dialogUI }
        };

        HideInGameUI();
    }

    private void OnDestroy()
    {
        PlayerStats.OnLocalPlayerHpChanged -= UpdateHp;
        PlayerStats.OnLocalPlayerExpChanged -= UpdateExp;
        PlayerStats.OnLocalPlayerPostureChanged -= UpdatePostureGauge;
    }


    public void ShowInGameUI()
    {
        InGameUIGroup.SetActive(true);
    }
    void HideInGameUI()
    {
        InGameUIGroup.SetActive(false);
    }
    public void UpdateHp(float currenthp, float maxHp)
    {
        PlayerHpUI.value = currenthp / maxHp;
    }

    public void UpdateExp(int exp, int level)
    {
        ExpUI.maxValue = DataManager.Instance.GetMaxExpForLevel(level);
        ExpUI.value = exp;
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
        if (panelDictionary[type].activeSelf)
        {
            CloseUI(type);
        }
        else
        {
            OpenUI(type);
        }
    }


    public void OpenUI(UIPanelType type)
    {
        if (currentOpenUI.Count > 0 && currentOpenUI.Last() == UIPanelType.Market) { return; } //마켓있을땐 UI못열게
        panelDictionary[type].SetActive(true);
        currentOpenUI.Add(type);
    }

    public void CloseUI(UIPanelType type)
    {
        panelDictionary[type].SetActive(false);
        TooltipManager.Instance.HideTooltip();
        currentOpenUI.Remove(type);   
    }

    public void CloseLastUI()
    {
        if (currentOpenUI.Count > 0)
        {
            UIPanelType lastPanel = currentOpenUI.Last();
            CloseUI(lastPanel);
        }
    }

    public bool IsPanelOpen(UIPanelType type)
    {
        return panelDictionary[type].activeSelf;
    }


}

