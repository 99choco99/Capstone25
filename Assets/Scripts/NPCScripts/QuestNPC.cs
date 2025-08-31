using UnityEngine;

public class QuestNPC : NPC
{
    public override void Interact(PlayerController player) {
        base.Interact(player);
        anim.SetBool("Talk", true);
    }



}
