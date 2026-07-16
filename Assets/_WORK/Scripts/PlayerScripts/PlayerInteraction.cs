using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("상호작용 범위")]
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private LayerMask layerMask;

    public bool IsDetectionPaused { get; set; } = false;

    //Event
    public event Action<List<IInteractable>> OnInteractableChanged;
    public event Action<IInteractable, int> OnSelectionChanged;


    public IInteractable CurrentSelection { get; private set; }
    public int selectionIndex = 0;


    public List<IInteractable> interactablesInRange = new List<IInteractable>();
    private Collider[] hitColliders = new Collider[15];

    public void ClearInteraction()
    {
        interactablesInRange.Clear();
        CurrentSelection = null;
        selectionIndex = 0;
        OnInteractableChanged?.Invoke(interactablesInRange);
        OnSelectionChanged?.Invoke(null, -1);
    }

    public void ExecuteInteraction() => CurrentSelection?.Interact(gameObject);
    private void OnDestroy() => CancelInvoke(nameof(DetectInteractables));

    void Start()
    {
        InvokeRepeating(nameof(DetectInteractables), 0f, 0.1f);
    }




    //가능한 상호작용 요소들 탐색
    private void DetectInteractables()
    {
        if (IsDetectionPaused) return;
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, interactRange,hitColliders,layerMask);

        bool isChanged = false;
        List<IInteractable> currentHits = new List<IInteractable>();

        for (int i  = 0; i < hitCount; i++)
        {
            if (hitColliders[i].TryGetComponent<IInteractable>(out IInteractable interactable))
            {
                currentHits.Add(interactable);
                if (!interactablesInRange.Contains(interactable)) isChanged = true;
            }
        }

        if (currentHits.Count != interactablesInRange.Count) isChanged = true;
        if (isChanged)
        {
            interactablesInRange.Clear();
            interactablesInRange.AddRange(currentHits);

            OnInteractableChanged?.Invoke(interactablesInRange);
            UpdateSelection();
        }
    }

    //선택 요소 변경?
    void UpdateSelection()
    {
        if (interactablesInRange.Count == 0)
        {
            CurrentSelection = null;
            selectionIndex = 0;
        }
        else
        {
            selectionIndex = Mathf.Clamp(selectionIndex, 0, interactablesInRange.Count - 1);
            CurrentSelection = interactablesInRange[selectionIndex];
        }
        OnSelectionChanged?.Invoke(CurrentSelection, selectionIndex);
    }

}
