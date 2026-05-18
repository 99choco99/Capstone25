using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    private Player player;

    [Header("상호작용 범위")]
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private LayerMask layerMask;

    //Event
    public event Action<List<IInteractable>> OnInteractableChanged;
    public event Action<IInteractable, int> OnSelectionChanged;


    public IInteractable CurrentSelection { get; private set; }
    public int selectionIndex = 0;


    public List<IInteractable> interactablesInRange = new List<IInteractable>();
    private Collider[] hitColliders = new Collider[15];
    private List<IInteractable> currentHits = new List<IInteractable>(15);

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    void Update()
    {
        DetectInteractables();
        if (interactablesInRange.Count > 0)
        {
            HandleSelection();
            CheckForInteraction();
        }
    }


    private void OnDisable()
    {
        interactablesInRange.Clear();
        currentHits.Clear();
        CurrentSelection = null;
        selectionIndex = 0;

        OnInteractableChanged?.Invoke(interactablesInRange);
        OnSelectionChanged?.Invoke(CurrentSelection, selectionIndex);
    }

    //가능한 상호작용 요소들 탐색
    private void DetectInteractables()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, interactRange,hitColliders,layerMask);

        currentHits.Clear();
        for(int i  = 0; i < hitCount; i++)
        {
            if (hitColliders[i].TryGetComponent<IInteractable>(out IInteractable interactable))
            {
                currentHits.Add(interactable);
            }
        }

        bool isListEqual = CheckListEquals(interactablesInRange, currentHits);

        if (!isListEqual)
        {
            interactablesInRange.Clear();
            interactablesInRange.AddRange(currentHits);

            OnInteractableChanged?.Invoke(currentHits);
            UpdateSelection();
        }
    }

    //선택 요소 변경?
    void UpdateSelection()
    {
        if (selectionIndex < 0) selectionIndex = 0;
        if (selectionIndex >= interactablesInRange.Count) selectionIndex = interactablesInRange.Count - 1;
        CurrentSelection = interactablesInRange.Count > 0 ? interactablesInRange[selectionIndex] : null;
        OnSelectionChanged?.Invoke(CurrentSelection, selectionIndex);
    }


    //스크롤 조절
    private void HandleSelection()
    {
        float scroll = player.InputHandler.Scroll;
        if(scroll != 0)
        {
            selectionIndex += (scroll < 0) ? -1 : 1;
            UpdateSelection();
        }
    }


    //최종 선택
    void CheckForInteraction()
    {
        if (player.InputHandler.InteractionInput)
        {
            player.InputHandler.UseInteractionInput();
            CurrentSelection?.Interact(player);
        }
    }


    //상호작용 리스트에 변화가 있었는지 체크
    private bool CheckListEquals(List<IInteractable> a, List<IInteractable> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
        {
            if (!b.Contains(a[i])) return false;
        }
        return true;
    }

}
