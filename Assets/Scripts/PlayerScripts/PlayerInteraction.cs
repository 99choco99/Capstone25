using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    private Player player;
    [SerializeField] DialogueManager Dialogue;


    [Header("설정")]
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private LayerMask layerMask;

    //Event
    public event Action<List<IInteractable>> OnInteractableChanged;
    public event Action<IInteractable> OnSelectionChanged;


    public IInteractable currentSelection { get; private set; }


    public int selectionIndex = 0;
    public List<IInteractable> interactablesInRange = new List<IInteractable>();

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    private void Start()
    {
        if (!player.IsLocalPlayer) { return; }
        Dialogue.OnConversationStart += HandleConversationStart;
        Dialogue.OnConversationEnd += HandleConversationEnd;
    }

    private void OnDestroy()
    {
        if (player != null && Dialogue != null)
        {
            if (!player.IsLocalPlayer) { return; }
            Dialogue.OnConversationStart -= HandleConversationStart;
            Dialogue.OnConversationEnd -= HandleConversationEnd;
        }

    }

    void Update()
    {
        if(player.StateMachine.CurrentState != player.StateMachine.ConversationState)
        {
            DetectInteractables();
            if (interactablesInRange.Count > 0)
            {
                HandleSelection();
                CheckForInteraction();
            }
        }

    }

    private void DetectInteractables()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, interactRange,layerMask);

        List<IInteractable> newInteractables = new List<IInteractable>();
        foreach (Collider collider in colliders)
        {
            if (collider.TryGetComponent<IInteractable>(out IInteractable interactable))
            {
                newInteractables.Add(interactable);
            }
        }

        bool IsListEqual = new HashSet<IInteractable>(interactablesInRange).SetEquals(newInteractables);

        if (!IsListEqual)
        {
            interactablesInRange = newInteractables;
            OnInteractableChanged?.Invoke(newInteractables);

            UpdateSelection();
        }
    }

    private void HandleSelection()
    {
        float scroll = player.InputHandler.Scroll;
        if(scroll != 0)
        {
            selectionIndex += (scroll < 0) ? -1 : 1;
            UpdateSelection();
        }
    }

    void UpdateSelection()
    {
        if (selectionIndex < 0) selectionIndex = 0;
        if (selectionIndex >= interactablesInRange.Count) selectionIndex = interactablesInRange.Count - 1;
        currentSelection = interactablesInRange.Count > 0 ? interactablesInRange[selectionIndex] : null;
        OnSelectionChanged?.Invoke(currentSelection);
    }

    void CheckForInteraction()
    {
        if (player.InputHandler.InteractionInput)
        {
            player.InputHandler.UseInteractionInput(); // 입력 소비
            currentSelection?.Interact(player);
        }
    }

    private void HandleConversationStart()
    {
        player.StateMachine.TransitionTo(player.StateMachine.ConversationState);
    }

    private void HandleConversationEnd()
    {
        if (player.StateMachine.CurrentState is ConversationState)
        {
            player.StateMachine.TransitionTo(player.StateMachine.PlayerIdleState);
        }
    }
}
