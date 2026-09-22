using System.Linq;
using UnityEngine;



public enum UIPanelType { 
    Quest = 0,
    Profile = 3,
    Setting = 4,
    Dialogue = 5
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

        if(panels != null)
        {
            panels.InitPanels();
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

    private void SetUp(Player player)
    {
        if (currentInput != null) UnsubscribeInputEvents();

        currentInput = player.InputHandler;

        if (panels != null) {
            panels.CloseAllPanels();
            panels.SetUpPanels(player);
        }

        currentInput.OnProfilePressed += OnProfileInput;
        currentInput.OnSettingPressed += OnSettingInput;
        currentInput.OnQuestPressed += OnQuestInput;

        currentInput.OnEscapePressed += CloseLastUI;

        UpdateCursorState();
    }


    private void OnProfileInput() => ToggleUI(UIPanelType.Profile);
    private void OnSettingInput() => ToggleUI(UIPanelType.Setting);
    private void OnQuestInput() => ToggleUI(UIPanelType.Quest);

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

        currentInput.OnProfilePressed -= OnProfileInput;
        currentInput.OnSettingPressed -= OnSettingInput;
        currentInput.OnQuestPressed -= OnQuestInput;
        currentInput.OnEscapePressed -= CloseLastUI;

    }



}



