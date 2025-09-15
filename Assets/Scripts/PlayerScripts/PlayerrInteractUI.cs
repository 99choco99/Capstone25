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
    private List<GameObject> uiInteractables = new List<GameObject>();


    private void Awake()
    {
        playerInteraction = GetComponentInParent<PlayerInteraction>();
        playerInteraction.OnInteractableChanged += UpdateInteractablesList;
        playerInteraction.OnSelectionChanged += UpdateSelection;

        uiContainer.SetActive(false);
    }



    void UpdateInteractablesList(List<IInteractable> interactables)
    {
        foreach (Transform child in promptParent) { Destroy(child.gameObject); }
        uiInteractables.Clear();

        if (interactables.Count == 0) { 
            uiContainer.SetActive(false); 
            return; 
        }
        uiContainer.SetActive(true);
        
        foreach(var interactable in interactables)
        {
            var promptInstance = Instantiate(promptPrefab, promptParent);
            var promptText = promptInstance.GetComponentInChildren<TextMeshProUGUI>();
            promptText.text = interactable.InteractionPrompt;
            uiInteractables.Add(promptInstance);
        }

        UpdateSelection(playerInteraction.currentSelection);
    }

    void UpdateSelection(IInteractable newSelection)
    {
        for(int i = 0; i < uiInteractables.Count; i++)
        {
            uiInteractables[i].GetComponent<Image>().color = (playerInteraction.interactablesInRange[i] == playerInteraction.currentSelection) ? Color.green : Color.red;
        }
    }
}
