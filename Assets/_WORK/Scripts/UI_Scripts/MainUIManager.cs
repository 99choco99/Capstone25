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

    [Header("UI 계층 레이어")]
    [SerializeField] private PanelUI panels;

    private PlayerInputHandler currentInput;


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
        Player.OnLocalPlayerSpawned += SetUp;
    }
    private void OnDestroy()
    {
        Player.OnLocalPlayerSpawned -= SetUp;
        UnsubscribeInputEvents();
    }

    private void SetUp(Player localPlayer)
    {
        currentInput = localPlayer.InputHandler;

        if (panels != null) { 
            panels.Init();
            panels.SetUpPanels(localPlayer);
        }

        if (currentInput != null) UnsubscribeInputEvents();

        currentInput.OnInventoryPressed += OnInventoryInput;
        currentInput.OnProfilePressed += OnProfileInput;
        currentInput.OnSettingPressed += OnSettingInput;
        currentInput.OnQuestPressed += OnQuestInput;

        currentInput.OnEscapePressed += CloseLastUI;

        if (DialogueManager.Instance != null)
        {
            // 중복 구독 방지
            DialogueManager.Instance.OnConversationStart -= OnConversationStart;
            DialogueManager.Instance.OnConversationEnd -= OnConversationEnd;

            DialogueManager.Instance.OnConversationStart += OnConversationStart;
            DialogueManager.Instance.OnConversationEnd += OnConversationEnd;
        }
        currentInput.OnDialogueNextPressed += OnDialogueNextInput;


        UpdateCursorState();
    }


    private void OnInventoryInput() => ToggleUI(UIPanelType.Inventory);
    private void OnProfileInput() => ToggleUI(UIPanelType.Profile);
    private void OnSettingInput() => ToggleUI(UIPanelType.Setting);
    private void OnQuestInput() => ToggleUI(UIPanelType.Quest);

    private void OnConversationStart()
    {
        if (panels != null)
        {
            panels.CloseAllPanels();
        }
        OpenUI(UIPanelType.Dialogue);
    }
    private void OnConversationEnd() => CloseUI(UIPanelType.Dialogue);
    private void OnDialogueNextInput()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.ShowNextLine();
        }
    }



    // UI를 열고 닫을 때 사용하는 통로
    public void ToggleUI(UIPanelType type){panels?.ToggleUI(type);UpdateCursorState();}
    public void OpenUI(UIPanelType type) { panels?.OpenUI(type); UpdateCursorState(); }
    public void CloseUI(UIPanelType type) { panels?.CloseUI(type); UpdateCursorState(); }
    public void CloseLastUI() {
        if (panels != null && panels.IsAnyPanelOpen())
        {
            UIPanelType lastPanelType = panels.currentOpenUI.Last();
            if (lastPanelType == UIPanelType.Dialogue) { return; }
            panels.CloseLastUI();
        }
        UpdateCursorState();
    }

    private void UpdateCursorState()
    {
        if (currentInput != null && panels != null)
        {
            bool isUIOpen = panels.IsAnyPanelOpen();
            bool shouldShowCursor = isUIOpen;

            Cursor.visible = shouldShowCursor;
            Cursor.lockState = shouldShowCursor ? CursorLockMode.Confined : CursorLockMode.Locked;

            if (shouldShowCursor)
                currentInput.SwitchToUIMode();
            else
                currentInput.SwitchToGameplayMode();
        }
    }


    private void UnsubscribeInputEvents()
    {
        if (currentInput == null) return;

        currentInput.OnInventoryPressed -= OnInventoryInput;
        currentInput.OnProfilePressed -= OnProfileInput;
        currentInput.OnSettingPressed -= OnSettingInput;
        currentInput.OnQuestPressed -= OnQuestInput;
        currentInput.OnEscapePressed -= CloseLastUI;

        currentInput.OnDialogueNextPressed -= OnDialogueNextInput;

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnConversationStart -= OnConversationStart;
            DialogueManager.Instance.OnConversationEnd -= OnConversationEnd;
        }
    }



}

