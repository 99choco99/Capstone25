using UnityEngine;

public class MarketNPC : NPC
{
    [SerializeField] GameObject MarketUI;


    public override void Interact(Player player)
    {
        //player.OpenUI(UIPanelType.Market);
    }
}
