using UnityEngine;

public class NPCInteractable : MonoBehaviour, IInteractable
{
    Animator anim;
    [SerializeField] private string InteractName;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }
    public void Interact(Transform player)
    {
        anim.SetTrigger("Talk");
        transform.LookAt(player.position);
    }

    public string GetInteractName()
    {
        return InteractName;
    }

}
