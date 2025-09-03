using UnityEngine;

public class MarketNPC : NPC
{
    [SerializeField] GameObject MarketUI;


    public override void Interact(PlayerController player)
    {
        player.OpenUI(UIPanelType.Market);
    }
}
