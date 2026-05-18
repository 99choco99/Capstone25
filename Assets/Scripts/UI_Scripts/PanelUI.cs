using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PanelUI : MonoBehaviour
{
    [Header("패널 UI")]
    [SerializeField] private List<UIBase> uiPanels;


    private Dictionary<UIPanelType, UIBase> panelDictionary;
    public List<UIPanelType> currentOpenUI = new List<UIPanelType>();


    public void Init()
    {
        // 딕셔너리에 UI 패널들을 등록
        panelDictionary = new Dictionary<UIPanelType, UIBase>();
        foreach (var panel in uiPanels)
        {
            if (!panelDictionary.ContainsKey(panel.panelType))
            {
                panelDictionary.Add(panel.panelType, panel);
                panel.Init();
                panel.gameObject.SetActive(false);
            }
        }
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
        if (panelDictionary.TryGetValue(type, out UIBase panel))
        {
            panel.Open();
            currentOpenUI.Add(type);
        }

    }

    public void CloseUI(UIPanelType type)
    {
        if (panelDictionary.TryGetValue(type, out UIBase panel))
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


    public bool IsAnyPanelOpen()
    {
        return currentOpenUI.Count > 0;
    }
}
