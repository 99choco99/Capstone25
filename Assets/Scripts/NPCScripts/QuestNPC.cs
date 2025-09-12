using UnityEngine;

public class QuestNPC : NPC
{
    public override void Interact(Player player) {
        base.Interact(player);
        anim.SetBool("Talk", true);
    }



}
