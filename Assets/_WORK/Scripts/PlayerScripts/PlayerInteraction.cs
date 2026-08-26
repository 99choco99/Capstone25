using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("?곹샇?묒슜 踰붿쐞")]
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private LayerMask layerMask;

    public bool IsDetectionPaused { get; set; } = false;

    //Event
    public event Action<List<IInteractable>> OnInteractableChanged;
    public event Action<IInteractable, int> OnSelectionChanged;


    private IInteractable currentSelection;
    public IInteractable CurrentSelection 
    { 
        get => IsNull(currentSelection) ? null : currentSelection;
        private set => currentSelection = value;
    }

    public int selectionIndex = 0;
    private float detectTimer = 0f;
    private const float DetectInterval = 0.1f;

    public List<IInteractable> interactablesInRange = new();
    private readonly Collider[] hitColliders = new Collider[15];
    private readonly List<IInteractable> currentHits = new();


    /// <summary>
    /// ?곹샇?묒슜 媛?ν븳 紐⑸줉??珥덇린??
    /// </summary>
    public void ClearInteraction()
    {
        interactablesInRange.Clear();
        CurrentSelection = null;
        selectionIndex = 0;
        OnInteractableChanged?.Invoke(interactablesInRange);
        OnSelectionChanged?.Invoke(null, -1);
    }

    /// <summary>
    /// ?곹샇?묒슜
    /// </summary>
    public void ExecuteInteraction() => CurrentSelection?.Interact(gameObject);


    /// <summary>
    /// 留??꾨젅?꾨쭏?ㅺ? ?꾨땲??0.1珥덉뿉 ?쒕쾲??寃?ы븯?꾨줉
    /// </summary>
    void Update()
    {
        detectTimer += Time.deltaTime;

        if(detectTimer >= DetectInterval)
        {
            DetectInteractables();
            detectTimer -= DetectInterval;
        }

    }

    /// <summary>
    /// 媛?ν븳 ?곹샇?묒슜 ?붿냼???먯깋
    /// </summary>
    private void DetectInteractables()
    {
        if (IsDetectionPaused) return;
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, interactRange,hitColliders,layerMask);

        bool isChanged = false;
        currentHits.Clear();

        for (int i  = 0; i < hitCount; i++)
        {
            if (hitColliders[i].TryGetComponent(out IInteractable interactable))
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

    /// <summary>
    /// ?좏깮???붿냼 蹂寃?
    /// </summary>
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

    /// <summary>
    /// null?몄? ?꾨땶吏
    /// </summary>
    private static bool IsNull(IInteractable interactable) => (interactable as UnityEngine.Object) == null;
}

