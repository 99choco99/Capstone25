using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TitleScreenManager : MonoBehaviour
{
    [SerializeField] Button StartButton;
    [SerializeField] Button ContinueButton;
    [SerializeField] Button OptionsButton;
    [SerializeField] Button QuitButton;
    [SerializeField] CanvasGroup MenuGroup;
    [SerializeField] Canvas TitleCanvas;
    [SerializeField] CanvasGroup ContinueButtonGroup;
    [SerializeField] GameObject ErrorPopupObj;
    [SerializeField] TextMeshProUGUI StatusText;
    [SerializeField] GameObject NewGameConfirmationObj;
    [SerializeField] Button CancelNewGameButton;

    private bool isStarting;
    private MainUIManager uiManager;

    private void Start()
    {
        uiManager = MainUIManager.Instance;
        uiManager.SetTitleCanvas(TitleCanvas);
        uiManager.PanelsChanged += RefreshMenu;
        RefreshMenu();
        SelectButton(ContinueButton.interactable ? ContinueButton : StartButton);
        ShowCursor();
    }

    private void OnDestroy()
    {
        if (uiManager != null)
        {
            uiManager.PanelsChanged -= RefreshMenu;
            uiManager.ClearTitleCanvas(TitleCanvas);
        }
    }

    private void Update()
    {
        if (isStarting)
            return;

        bool optionsOpen = MainUIManager.Instance.IsUIOpen(UIPanelType.Setting);

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            if (NewGameConfirmationObj.activeSelf)
                OnCancelNewGameClick();
            else if (ErrorPopupObj.activeSelf)
                OnDismissErrorClick();
            else if (optionsOpen)
                OnCloseOptionsButtonClick();
        }
        else if (keyboard.oKey.wasPressedThisFrame)
        {
            if (optionsOpen)
                OnCloseOptionsButtonClick();
            else if (MenuGroup.interactable)
                OnOptionsButtonClick();
        }
    }

    private void OnApplicationFocus(bool focused)
    {
        if (focused && !isStarting)
            RefreshMenu();
    }

    /// <summary>기존 기록이 있으면 초기화 경고를 표시하고, 없으면 바로 새 게임을 시작합니다.</summary>
    public void OnStartButtonClick()
    {
        if (!MenuGroup.interactable || isStarting)
            return;

        if (DataManager.Instance.HasLocalSave)
        {
            NewGameConfirmationObj.SetActive(true);
            RefreshMenu();
            SelectButton(CancelNewGameButton);
            return;
        }

        BeginGame(true);
    }

    public void OnConfirmNewGameClick()
    {
        if (!NewGameConfirmationObj.activeSelf || isStarting)
            return;
        BeginGame(true);
    }

    public void OnCancelNewGameClick()
    {
        if (isStarting)
            return;
        NewGameConfirmationObj.SetActive(false);
        RefreshMenu();
        SelectButton(StartButton);
    }

    public void OnContinueButtonClick()
    {
        if (!MenuGroup.interactable || !ContinueButton.interactable || isStarting)
            return;
        BeginGame(false);
    }

    public void OnOptionsButtonClick()
    {
        if (!MenuGroup.interactable || isStarting)
            return;
        MainUIManager.Instance.OpenUI(UIPanelType.Setting);
        RefreshMenu();
        ShowCursor();
    }

    public void OnCloseOptionsButtonClick()
    {
        if (isStarting)
            return;
        MainUIManager.Instance.CloseUI(UIPanelType.Setting);

        RefreshMenu();
        SelectButton(OptionsButton);
        ShowCursor();
    }

    public void OnQuitButtonClick()
    {
        if (!MenuGroup.interactable || isStarting)
            return;
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private async void BeginGame(bool newGame)
    {
        if (isStarting)
            return;
        isStarting = true;
        NewGameConfirmationObj.SetActive(false);
        MainUIManager.Instance.CloseUI(UIPanelType.Setting);

        RefreshMenu();

        try
        {
            if (newGame)
                await GameManager.Instance.StartNewGame();
            else
                await GameManager.Instance.ContinueGame();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            isStarting = false;
            SetStatus($"게임 시작 실패: {exception.Message}");
            RefreshMenu();
            ShowCursor();
        }
    }

    public void OnDismissErrorClick()
    {
        if (isStarting)
            return;
        ErrorPopupObj.SetActive(false);
        RefreshMenu();
        SelectButton(ContinueButton.interactable ? ContinueButton : StartButton);
    }

    private void RefreshMenu()
    {
        bool hasSavedGame = DataManager.Instance.HasSavedGame;
        StartButton.interactable = !isStarting;
        ContinueButton.interactable = !isStarting && hasSavedGame;
        OptionsButton.interactable = !isStarting;
        QuitButton.interactable = !isStarting;
        ContinueButtonGroup.alpha = hasSavedGame ? 1f : 0.45f;
        ContinueButtonGroup.interactable = ContinueButton.interactable;

        bool optionsOpen = uiManager != null && uiManager.IsUIOpen(UIPanelType.Setting);
        bool menuAvailable = !isStarting && !optionsOpen && !NewGameConfirmationObj.activeSelf
            && !ErrorPopupObj.activeSelf;
        MenuGroup.interactable = menuAvailable;
        MenuGroup.blocksRaycasts = menuAvailable;
    }

    private static void SelectButton(Button button)
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    private static void ShowCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    //메세지 표시
    void SetStatus(string message)
    {
        StatusText.text = message;
        ErrorPopupObj.SetActive(true);
        SelectButton(ErrorPopupObj.GetComponentInChildren<Button>());
    }
}