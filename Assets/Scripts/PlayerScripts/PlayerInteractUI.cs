using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Linq;


public class PlayerInteractUI : MonoBehaviour
{
    [SerializeField] private GameObject uiContainer; //전체 UI
    [SerializeField] private PromptUIItem promptPrefab;  // 각 상호작용 대상들의 prompt
    [SerializeField] private Transform promptParent;  // prompt 부모


    private PlayerInteraction playerInteraction; // 플레이어의 interact 참조
    private List<PromptUIItem> promptPool = new List<PromptUIItem>();

    [SerializeField] private Color selectedColor = Color.green;
    [SerializeField] private Color defaultColor = Color.white;

    private void Awake()
    {
        uiContainer.SetActive(false);
    }

    public void Init(PlayerInteraction interaction)
    {
        if (playerInteraction != null)
        {
            playerInteraction.OnInteractableChanged -= UpdateInteractablesList;
            playerInteraction.OnSelectionChanged -= UpdateSelection;
        }

        playerInteraction = interaction;
        playerInteraction.OnInteractableChanged += UpdateInteractablesList;
        playerInteraction.OnSelectionChanged += UpdateSelection;

    }

    private void OnDestroy()
    {
        if (playerInteraction != null)
        {
            playerInteraction.OnInteractableChanged -= UpdateInteractablesList;
            playerInteraction.OnSelectionChanged -= UpdateSelection;
        }
    }

    void UpdateInteractablesList(List<IInteractable> interactables)
    {
        int count = interactables.Count;
        uiContainer.SetActive(count > 0);

        for (int i = 0; i < count; i++)
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

        for (int i = count; i < promptPool.Count; i++)
        {
            promptPool[i].gameObject.SetActive(false);
        }
    }

    void UpdateSelection(IInteractable newSelection, int selectedIndex)
    {
        int activePromptCount = playerInteraction.interactablesInRange.Count;

        for (int i = 0; i < promptPool.Count; i++)
        {
            if (i < activePromptCount)
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
