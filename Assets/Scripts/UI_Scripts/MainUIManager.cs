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
    [SerializeField] private PlayerHUD hud;
    [SerializeField] private WorldUI worldUI;
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
        Player.OnLocalPlayerSpawned += ConnectLocalPlayerUI;
        
    }

    private void ConnectLocalPlayerUI(Transform playerTransform)
    {
        Player localPlayer = playerTransform.GetComponent<Player>();
        if (localPlayer == null) return;

        // 의존성 주입
        if (hud != null) hud.Init(localPlayer.Stats);
        if (worldUI != null) worldUI.Init(localPlayer);
        if (panels != null) { 
            panels.Init(); 
            
            IPlayerUI[] allPlayerUIs = panels.gameObject.GetComponentsInChildren<IPlayerUI>(true);
            foreach (var ui in allPlayerUIs)
            {
                ui.SetUp(localPlayer);
            }
        }

        if (currentInput != null) UnsubscribeInputEvents();

        currentInput = localPlayer.InputHandler;

        currentInput.OnInventoryPressed += () => ToggleUI(UIPanelType.Inventory);
        currentInput.OnProfilePressed += () => ToggleUI(UIPanelType.Profile);
        currentInput.OnSettingPressed += () => ToggleUI(UIPanelType.Setting);
        currentInput.OnQuestPressed += () => ToggleUI(UIPanelType.Quest);

        currentInput.OnEscapePressed += CloseLastUI;

        currentInput.SetCursorState(false);
    }



    // UI를 열고 닫을 때 사용하는 통로
    public void ToggleUI(UIPanelType type){panels?.ToggleUI(type);UpdateCursorState();}
    public void OpenUI(UIPanelType type) { panels?.OpenUI(type); UpdateCursorState(); }
    public void CloseUI(UIPanelType type) { panels?.CloseUI(type); UpdateCursorState(); }
    public void CloseLastUI() { panels?.CloseLastUI(); UpdateCursorState(); }

    private void UpdateCursorState()
    {
        if (currentInput != null && panels != null)
        {
            bool isUIOpen = panels.IsAnyPanelOpen();
            currentInput.SetCursorState(isUIOpen);
        }
    }


    private void UnsubscribeInputEvents()
    {
        if (currentInput == null) return;

        currentInput.OnInventoryPressed -= () => ToggleUI(UIPanelType.Inventory);
        currentInput.OnProfilePressed -= () => ToggleUI(UIPanelType.Profile);
        currentInput.OnSettingPressed -= () => ToggleUI(UIPanelType.Setting);
        currentInput.OnQuestPressed -= () => ToggleUI(UIPanelType.Quest);
        currentInput.OnEscapePressed -= CloseLastUI;
    }

    private void OnDestroy()
    {
        Player.OnLocalPlayerSpawned -= ConnectLocalPlayerUI;
        UnsubscribeInputEvents();
    }

}

