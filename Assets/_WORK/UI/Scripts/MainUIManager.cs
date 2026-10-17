using UnityEngine;

public enum UIPanelType
{
    Profile = 3,
    Setting = 4,
    Dialogue = 5,
    GameMenu = 6
}

public class MainUIManager : MonoBehaviour
{
    public static MainUIManager Instance { get; private set; }

    [Header("UI 계층 레이어")]
    [SerializeField] private PanelUI panels;

    private PlayerInputHandler currentInput;
    private Canvas panelCanvas;
    private Canvas titleCanvas;
    private int panelSortingOrder;
    private DialogueUI dialogueUI;
    public event System.Action PanelsChanged;

    //============ 초기화와 정리 ============

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

        if (panels != null)
        {
            panels.InitPanels();
            panelCanvas = panels.GetComponent<Canvas>();
            if (panelCanvas != null)
                panelSortingOrder = panelCanvas.sortingOrder;
            dialogueUI = panels.GetComponentInChildren<DialogueUI>(true);
        }
    }

    private void Start()
    {
        Player.OnLocalPlayerSpawned += SetUp;
        if (Player.LocalPlayer != null)
            SetUp(Player.LocalPlayer);
    }

    private void OnDestroy()
    {
        Player.OnLocalPlayerSpawned -= SetUp;
        UnsubscribeInputEvents();
    }

    //============ 플레이어 연결과 해제 ============

    private void SetUp(Player player)
    {
        UnsubscribeInputEvents();

        currentInput = player.InputHandler;

        if (panels != null)
        {
            panels.CloseAllPanels();
            panels.SetUpPanels(player);
        }

        currentInput.OnProfilePressed += OnProfileInput;
        currentInput.OnSettingPressed += OnSettingInput;

        currentInput.OnEscapePressed += OnEscapeInput;

        UpdateCursorState();
    }

    public void ReleasePlayerUI()
    {
        panels?.CloseAllPanels();
        UnsubscribeInputEvents();
        currentInput = null;
        UpdateCursorState();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void UnsubscribeInputEvents()
    {
        if (currentInput == null)
            return;

        currentInput.OnProfilePressed -= OnProfileInput;
        currentInput.OnSettingPressed -= OnSettingInput;
        currentInput.OnEscapePressed -= OnEscapeInput;
    }

    public void SetTitleCanvas(Canvas canvas)
    {
        titleCanvas = canvas;
        UpdatePanelSortingOrder();
    }

    public void ClearTitleCanvas(Canvas canvas)
    {
        if (titleCanvas != canvas)
            return;
        titleCanvas = null;
        UpdatePanelSortingOrder();
    }

    //============ 패널 열기와 닫기 ============

    // UI를 열고 닫을 때 사용하는 통로
    public void ToggleUI(UIPanelType type)
    {
        panels?.ToggleUI(type);
        UpdateCursorState();
    }

    public void OpenUI(UIPanelType type)
    {
        panels?.OpenUI(type);
        UpdateCursorState();
    }

    public void CloseUI(UIPanelType type)
    {
        panels?.CloseUI(type);
        UpdateCursorState();
    }

    public void CloseLastUI()
    {
        if (panels != null && panels.TryGetLastOpenPanel(out UIPanelType lastPanelType))
        {
            if (lastPanelType == UIPanelType.Dialogue)
            {
                return;
            }
            panels.CloseLastUI();
        }
        UpdateCursorState();
    }

    public bool IsUIOpen(UIPanelType type)
    {
        return panels != null && panels.IsPanelOpen(type);
    }

    //============ 대화 시작 ============

    public bool StartDialogue(UniversalGraph.DialogueEntryPoint entryPoint, UniversalGraph.DialogueExecutionContext context)
    {
        return dialogueUI != null && dialogueUI.StartConversation(entryPoint, context);
    }

    //============ UI 입력 처리 ============

    private void OnProfileInput() => ToggleUI(UIPanelType.Profile);

    private void OnSettingInput() => ToggleUI(UIPanelType.Setting);

    private void OnEscapeInput()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == SceneName.Login || sceneName == SceneName.Loading
            || GameManager.Instance != null && GameManager.Instance.IsChangingScene)
            return;

        if (panels != null && panels.IsAnyPanelOpen())
            CloseLastUI();
        else
            OpenUI(UIPanelType.GameMenu);
    }

    //============ 커서와 입력 모드 ============

    private void UpdatePanelSortingOrder()
    {
        if (panelCanvas == null)
            return;

        bool titleOptionsOpen = titleCanvas != null
            && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == SceneName.Login
            && IsUIOpen(UIPanelType.Setting);
        panelCanvas.sortingOrder = titleOptionsOpen
            ? System.Math.Max(panelSortingOrder, titleCanvas.sortingOrder + 1) : panelSortingOrder;
    }

    private void UpdateCursorState()
    {
        UpdatePanelSortingOrder();

        if (currentInput != null && panels != null)
        {
            bool shouldShowCursor = panels.IsAnyPanelOpen();

            Cursor.visible = shouldShowCursor;
            Cursor.lockState = shouldShowCursor ? CursorLockMode.Confined : CursorLockMode.Locked;

            if (shouldShowCursor)
                currentInput.SwitchToUIMode();
            else
                currentInput.SwitchToGameplayMode();
        }
        PanelsChanged?.Invoke();
    }
}
