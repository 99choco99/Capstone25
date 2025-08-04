using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


public enum UIPanelType { 
    Market,
    Inventory,
    Profile,
    Setting
}
public class PlayerUIManager : MonoBehaviour
{
    public Slider PlayerHpUI;
    public Slider EnemyHpUI;
    public Slider ExpUI;
    public Image dialogUI;
    public TextMeshProUGUI EnemyName;
    public TextMeshProUGUI NpcName;
    public TextMeshProUGUI NpcText;


    private Dictionary<UIPanelType, GameObject> panelDictionary;
    public Stack<UIPanelType> currentOpenUI = new Stack<UIPanelType>();

    [Header("UI_Panel")]
    public GameObject Market;
    public GameObject Inventory;
    public GameObject PlayerProfile;
    public GameObject Setting;


    public static PlayerUIManager instnace;

    private void Start()
    {
        if(instnace == null)
        {
            instnace = this;
        }
        else
        {
            Destroy(instnace);
        }

        // 딕셔너리에 UI 패널들을 등록
        panelDictionary = new Dictionary<UIPanelType, GameObject>()
        {
            {UIPanelType.Market, Market },
            { UIPanelType.Inventory, Inventory },
            { UIPanelType.Profile, PlayerProfile },
            { UIPanelType.Setting, Setting }
        };
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

    public void ShowDialogUI()
    {
        dialogUI.gameObject.SetActive(true);
    }
    public void HideDialogUI()
    {
        dialogUI.gameObject.SetActive(false);
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
        panelDictionary[type].SetActive(true);
        currentOpenUI.Push(type);
    }

    public void CloseUI(UIPanelType type)
    {
        panelDictionary[type].SetActive(false);
        if (currentOpenUI.Count > 0 && currentOpenUI.Peek() == type)
        {
            currentOpenUI.Pop();
        }
    }

    public void CloseLastUI()
    {
        if (currentOpenUI.Count > 0)
        {
            UIPanelType LastUIType = currentOpenUI.Pop();
            while (!panelDictionary[LastUIType].activeSelf && currentOpenUI.Count > 0)
            {
                LastUIType = currentOpenUI.Pop();
            }
            panelDictionary[LastUIType].SetActive(false);
        }
    }
}

