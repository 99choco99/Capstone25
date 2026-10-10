using UnityEngine;
using System.Collections.Generic;

public class PlayerInteractUI : MonoBehaviour
{
    private PlayerInteraction playerInteraction; // 플레이어의 interact 참조

    [SerializeField] private GameObject uiContainer; //전체 UI
    [SerializeField] private PromptUIItem promptPrefab;  // 각 상호작용 대상들의 prompt
    [SerializeField] private Transform promptParent;  // prompt 부모

    private List<PromptUIItem> promptPool = new();

    private int currentActivePromptCount = 0;

    [SerializeField] private Color selectedColor = Color.green;
    [SerializeField] private Color defaultColor = Color.white;

    private void Awake()
    {
        Player.OnLocalPlayerSpawned += Init;
        uiContainer.SetActive(false);
    }

    private void Start()
    {
        if (playerInteraction == null && Player.LocalPlayer != null)
            Init(Player.LocalPlayer);
    }

    private void OnDestroy()
    {
        Player.OnLocalPlayerSpawned -= Init;
        Unsubscribe();
    }

    public void Init(Player localPlayer)
    {
        Unsubscribe();
        playerInteraction = localPlayer.Interaction;

        playerInteraction.OnInteractableChanged += UpdateInteractablesList;
        playerInteraction.OnSelectionChanged += UpdateSelection;

        UpdateInteractablesList(playerInteraction.interactablesInRange);
        UpdateSelection(playerInteraction.CurrentSelection, playerInteraction.selectionIndex);
    }

    private void Unsubscribe()
    {
        if (playerInteraction == null) return;

        playerInteraction.OnInteractableChanged -= UpdateInteractablesList;
        playerInteraction.OnSelectionChanged -= UpdateSelection;
    }

    void UpdateInteractablesList(List<IInteractable> interactables)
    {
        currentActivePromptCount = interactables.Count;
        uiContainer.SetActive(currentActivePromptCount > 0);

        for (int i = 0; i < currentActivePromptCount; i++)
        {
            if (i == promptPool.Count)
                promptPool.Add(Instantiate(promptPrefab, promptParent));

            PromptUIItem promptInstance = promptPool[i];

            promptInstance.gameObject.SetActive(true);

            promptInstance.SetText(interactables[i].InteractionPrompt);
        }

        for (int i = currentActivePromptCount; i < promptPool.Count; i++)
        {
            promptPool[i].gameObject.SetActive(false);
        }
    }

    void UpdateSelection(IInteractable newSelection, int selectedIndex)
    {
        for (int i = 0; i < currentActivePromptCount; i++)
        {
            Color colorToSet = (i == selectedIndex) ? selectedColor : defaultColor;
            promptPool[i].SetColor(colorToSet);
        }
    }
}
