using Unity.VisualScripting;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    PlayerController player;
    IInteractable interactObject;
    Collider [] hits;

    public float interactRange;
    void Start()
    {
        player = GetComponent<PlayerController>();
    }

    void Update()
    {
        DetectInteractable();
        if (player.interaction)
        {
            foreach(Collider collider in hits)
            {
                if(collider.gameObject.TryGetComponent(out interactObject))
                {
                    interactObject.Interact();
                }
            }
        }
    }

    void DetectInteractable()
    {
        hits = Physics.OverlapSphere(transform.position, interactRange);
    }

}
