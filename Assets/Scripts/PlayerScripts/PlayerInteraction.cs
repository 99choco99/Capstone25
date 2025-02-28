using Unity.VisualScripting;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    PlayerController player;
    IInteractable interactObject;


    public float interactRange;
    private int layerMask = 1 << 8;
    void Start()
    {
        player = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (player.interaction)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, interactRange, layerMask);
            if (hits[0].gameObject.TryGetComponent(out interactObject))
            {
                interactObject.Interact(player.transform);
            }
        }
    }

    public Collider[] GetInteractObject()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interactRange, layerMask);
        if (hits != null)
        {
            return hits;
        }
        return null;
    }


}
