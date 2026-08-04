using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    /// 상호작용 가능한 목록들 초기화
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
    /// 상호작용
    /// </summary>
    public void ExecuteInteraction() => CurrentSelection?.Interact(gameObject);


    /// <summary>
    /// 매 프레임마다가 아니라 0.1초에 한번씩 검사하도록
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
    /// 가능한 상호작용 요소들 탐색
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
    /// 선택한 요소 변경
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
    /// null인지 아닌지
    /// </summary>
    private static bool IsNull(IInteractable interactable) => (interactable as UnityEngine.Object) == null;
}
