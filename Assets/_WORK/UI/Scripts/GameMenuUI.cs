using System;
using UnityEngine;

public class GameMenuUI : UIBase
{
    [SerializeField] private CanvasGroup menuGroup;
    private bool isLeaving;
    private MainUIManager manager;

    public override void Init()
    {
        manager = MainUIManager.Instance;
        manager.PanelsChanged += RefreshInteraction;
    }

    private void OnDestroy()
    {
        if (manager != null)
            manager.PanelsChanged -= RefreshInteraction;
    }

    private void RefreshInteraction()
    {
        menuGroup.interactable = IsOpen && !isLeaving
            && !manager.IsUIOpen(UIPanelType.Setting)
            && !manager.IsUIOpen(UIPanelType.Profile);
    }

    public void OnSettingsButtonClick()
    {
        if (isLeaving)
            return;
        menuGroup.interactable = false;
        MainUIManager.Instance.OpenUI(UIPanelType.Setting);
    }

    public async void OnReturnToTitleButtonClick()
    {
        if (isLeaving)
            return;
        isLeaving = true;
        menuGroup.interactable = false;
        try
        {
            await GameManager.Instance.ReturnToTitle();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            isLeaving = false;
            RefreshInteraction();
        }
    }

    public void OnQuitButtonClick()
    {
        if (isLeaving)
            return;
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
