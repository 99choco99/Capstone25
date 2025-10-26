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
    [SerializeField] private GameObject promptPrefab;  // 각 상호작용 대상들의 prompt
    [SerializeField] private Transform promptParent;  // prompt 부모


    private PlayerInteraction playerInteraction; // 플레이어의 interact 참조
    private List<PromptUIItem> promptPool = new List<PromptUIItem>();
    [SerializeField] private Color selectedColor = Color.green;
    [SerializeField] private Color defaultColor = Color.white;

    private void Awake()
    {
        playerInteraction = GetComponentInParent<PlayerInteraction>();
        playerInteraction.OnInteractableChanged += UpdateInteractablesList;
        playerInteraction.OnSelectionChanged += UpdateSelection;

        uiContainer.SetActive(false);
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
                GameObject newPromptObject = Instantiate(promptPrefab, promptParent);
                promptInstance = newPromptObject.GetComponent<PromptUIItem>();
                promptPool.Add(promptInstance);
            }

            promptInstance.gameObject.SetActive(true);

            promptInstance.SetText(interactables[i].InteractionPrompt);
        }

        for (int i = count; i < promptPool.Count; i++)
        {
            promptPool[i].gameObject.SetActive(false);
        }

        UpdateSelection(playerInteraction.currentSelection);
    }

    void UpdateSelection(IInteractable newSelection)
    {
        // 현재 선택된 항목의 인덱스를 찾음
        int selectedIndex = -1;
        if (newSelection != null)
        {
            selectedIndex = playerInteraction.interactablesInRange.IndexOf(newSelection);
        }

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
