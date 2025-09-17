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
public class PlayerUIManager : MonoBehaviour
{
    public PlayerStats playerStats;


    public Slider PlayerHpUI;
    public Slider PostureGauge;
    public Slider EnemyHpUI;
    public Slider ExpUI;

    public TextMeshProUGUI EnemyName;
    public TextMeshProUGUI NpcName;
    public TextMeshProUGUI NpcText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI ExpText;


    private Dictionary<UIPanelType, GameObject> panelDictionary;
    public List<UIPanelType> currentOpenUI = new List<UIPanelType>();

    [Header("UI_Panel")]
    public GameObject Market;
    public GameObject Inventory;
    public GameObject PlayerProfile;
    public GameObject Setting;
    public GameObject Quest;
    public GameObject dialogUI;


    public static PlayerUIManager instnace;


    private void Awake()
    {
        if (instnace == null)
        {
            instnace = this;
        }
        else
        {
            Destroy(instnace);
        }
        playerStats = GetComponentInParent<PlayerStats>();
    }

    private void Start()
    {


        // 딕셔너리에 UI 패널들을 등록
        panelDictionary = new Dictionary<UIPanelType, GameObject>()
        {
            {UIPanelType.Market, Market },
            { UIPanelType.Inventory, Inventory },
            { UIPanelType.Profile, PlayerProfile },
            { UIPanelType.Setting, Setting },
            {UIPanelType.Quest, Quest },
            {UIPanelType.Dialogue, dialogUI }
        };

        playerStats.OnHpChanged += UpdateHp;
        playerStats.OnExpChanged += UpdateExp;
        playerStats.OnPostureChanged += UpdatePostureGauge;

        foreach(var panel in panelDictionary.Values)
        {
            panel.SetActive(false);
        }
    }

    public void UpdateHp(float currenthp)
    {
        PlayerHpUI.value = currenthp / playerStats.maxHp;
    }

    public void UpdateExp(int exp, int level)
    {
        if(exp / playerStats.maxExp[level] < 1)
        {
            ExpUI.value = exp / playerStats.maxExp[level];
        }
        levelText.text = $"Lv. {level}";
        ExpText.text = $"{ExpUI.value}%";
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

    public void SetNpcText(string text)
    {
        NpcText.text = text;
    }

    public void SetNpcName(string name)
    {
        NpcName.text = name;
    }

    public void SetMaxExp(float maxExp)
    {
        ExpUI.maxValue = maxExp;
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
        GameManager.instance.ChangeState(GameState.UIMode);
        panelDictionary[type].SetActive(true);
        currentOpenUI.Add(type);
    }

    public void CloseUI(UIPanelType type)
    {
        panelDictionary[type].SetActive(false);
        currentOpenUI.Remove(type);
        if (currentOpenUI.Count == 0)
        {
            GameManager.instance.ChangeState(GameState.Gameplay);
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

