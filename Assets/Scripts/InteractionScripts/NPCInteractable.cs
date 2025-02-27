using UnityEngine;

public class NPCInteractable : MonoBehaviour, IInteractable
{
    Animator anim;
    private void Awake()
    {
        anim = GetComponent<Animator>();
    }
    public void Interact()
    {
        anim.SetTrigger("Talk");
        Debug.Log("³ª´Â NPC");
    }

}
