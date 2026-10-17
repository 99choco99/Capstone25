using System.Collections.Generic;
using UnityEngine;

public class PanelUI : MonoBehaviour
{
    [Header("패널 UI")]
    [SerializeField] private List<UIBase> uiPanels;

    private Dictionary<UIPanelType, UIBase> panelDictionary;
    [SerializeField] private List<UIPanelType> currentOpenUI = new();

    //============ 패널 초기화와 연결 ============

    public void InitPanels()
    {
        // 딕셔너리에 UI 패널들을 등록
        panelDictionary = new Dictionary<UIPanelType, UIBase>();

        foreach (var panel in uiPanels)
        {
            if (panelDictionary.TryAdd(panel.panelType, panel))
            {
                panel.Init();
                panel.Close();
            }
            else
            {
                Debug.LogWarning($"[PanelUI] 중복된 패널 타입이 있습니다: {panel.panelType}");
            }
        }
    }

    /// <summary>
    /// 모든 패널들의 Setup을 실행
    /// </summary>
    public void SetUpPanels(Player player)
    {
        foreach (var panel in panelDictionary.Values)
        {
            panel.SetUp(player);
        }
    }

    //============ 패널 열기와 닫기 ============

    public void ToggleUI(UIPanelType type)
    {
        if (!panelDictionary.TryGetValue(type, out UIBase panel))
            return;

        if (panel.IsOpen)
            CloseUI(type);
        else
            OpenUI(type);
    }

    public void OpenUI(UIPanelType type)
    {
        if (!panelDictionary.TryGetValue(type, out UIBase panel))
            return;

        panel.Open();
        panel.transform.SetAsLastSibling();
        if (!currentOpenUI.Contains(type))
            currentOpenUI.Add(type);
    }

    public void CloseUI(UIPanelType type)
    {
        if (!panelDictionary.TryGetValue(type, out UIBase panel))
            return;

        panel.Close();
        currentOpenUI.Remove(type);
        BringLastOpenPanelToFront();
    }

    public void CloseLastUI()
    {
        if (TryGetLastOpenPanel(out UIPanelType type))
            CloseUI(type);
    }

    public void CloseAllPanels()
    {
        foreach (var type in currentOpenUI)
        {
            if (panelDictionary.TryGetValue(type, out UIBase panel))
            {
                panel.Close();
            }
        }

        currentOpenUI.Clear();
    }

    //============ 패널 표시 순서 ============

    private void BringLastOpenPanelToFront()
    {
        if (TryGetLastOpenPanel(out UIPanelType type)
            && panelDictionary.TryGetValue(type, out UIBase panel))
            panel.transform.SetAsLastSibling();
    }

    //============ 패널 상태 조회 ============

    public bool IsPanelOpen(UIPanelType type)
    {
        return currentOpenUI.Contains(type);
    }

    public bool IsAnyPanelOpen()
    {
        return currentOpenUI.Count > 0;
    }

    public bool TryGetLastOpenPanel(out UIPanelType type)
    {
        type = default;
        if (currentOpenUI.Count == 0)
            return false;

        type = currentOpenUI[^1];
        return true;
    }
}
