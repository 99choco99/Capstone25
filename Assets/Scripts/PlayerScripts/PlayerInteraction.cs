using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    private Player player;
    [SerializeField] DialogueManager dialogueManager;


    [Header("상호작용 범위")]
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private LayerMask layerMask;

    //Event
    public event Action<List<IInteractable>> OnInteractableChanged;
    public event Action<IInteractable> OnSelectionChanged;


    public IInteractable CurrentSelection { get; private set; }
    public int selectionIndex = 0;


    public List<IInteractable> interactablesInRange = new List<IInteractable>();
    private Collider[] hitColliders = new Collider[15];
    private List<IInteractable> currentHits = new List<IInteractable>(15);

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    private void Start()
    {
        if (!player.IsLocalPlayer) { return; }
        dialogueManager.OnConversationStart += HandleConversationStart;
        dialogueManager.OnConversationEnd += HandleConversationEnd;
    }

    private void OnDestroy()
    {
        if (player != null && dialogueManager != null)
        {
            if (!player.IsLocalPlayer) { return; }
            dialogueManager.OnConversationStart -= HandleConversationStart;
            dialogueManager.OnConversationEnd -= HandleConversationEnd;
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
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, interactRange,hitColliders,layerMask);

        currentHits.Clear();
        for(int i  = 0; i < hitCount; i++)
        {
            if (hitColliders[i].GetComponent<Collider>().TryGetComponent<IInteractable>(out IInteractable interactable))
            {
                currentHits.Add(interactable);
            }
        }

        bool IsListEqual = new HashSet<IInteractable>(interactablesInRange).SetEquals(currentHits);

        if (!IsListEqual)
        {
            interactablesInRange = currentHits;
            OnInteractableChanged?.Invoke(currentHits);

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
        CurrentSelection = interactablesInRange.Count > 0 ? interactablesInRange[selectionIndex] : null;
        OnSelectionChanged?.Invoke(CurrentSelection);
    }

    void CheckForInteraction()
    {
        if (player.InputHandler.InteractionInput)
        {
            player.InputHandler.UseInteractionInput();
            CurrentSelection?.Interact(player);
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
