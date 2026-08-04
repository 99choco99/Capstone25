using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Linq;


public class PlayerInteractUI : MonoBehaviour
{
    private PlayerInteraction playerInteraction; // 플레이어의 interact 참조

    [SerializeField] private GameObject uiContainer; //전체 UI
    [SerializeField] private PromptUIItem promptPrefab;  // 각 상호작용 대상들의 prompt
    [SerializeField] private Transform promptParent;  // prompt 부모

    private List<PromptUIItem> promptPool = new List<PromptUIItem>();

    private int currentActivePromptCount = 0;

    [SerializeField] private Color selectedColor = Color.green;
    [SerializeField] private Color defaultColor = Color.white;

    private void Awake()
    {
        Player.OnLocalPlayerSpawned += Init;
        uiContainer.SetActive(false);
    }

    private void OnDestroy()
    {
        Player.OnLocalPlayerSpawned -= Init;
        if (playerInteraction != null)
        {
            playerInteraction.OnInteractableChanged -= UpdateInteractablesList;
            playerInteraction.OnSelectionChanged -= UpdateSelection;
        }
    }

    public void Init(Player localPlayer)
    {
        playerInteraction = localPlayer.Interaction;
        if (playerInteraction != null)
        {
            playerInteraction.OnInteractableChanged -= UpdateInteractablesList;
            playerInteraction.OnSelectionChanged -= UpdateSelection;
        }

        playerInteraction.OnInteractableChanged += UpdateInteractablesList;
        playerInteraction.OnSelectionChanged += UpdateSelection;

    }

    void UpdateInteractablesList(List<IInteractable> interactables)
    {
        currentActivePromptCount = interactables.Count;
        uiContainer.SetActive(currentActivePromptCount > 0);

        for (int i = 0; i < currentActivePromptCount; i++)
        {
            PromptUIItem promptInstance;
            if (i < promptPool.Count)
            {
                promptInstance = promptPool[i];
            }
            else
            {
                promptInstance = Instantiate(promptPrefab, promptParent);
                promptPool.Add(promptInstance);
            }

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
        for (int i = 0; i < promptPool.Count; i++)
        {
            if (i < currentActivePromptCount)
            {
                Color colorToSet = (i == selectedIndex) ? selectedColor : defaultColor;
                promptPool[i].SetColor(colorToSet);
            }
            else
            {
                break;
            }
        }
    }
}
