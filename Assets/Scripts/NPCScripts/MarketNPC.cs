using UnityEngine;

public class MarketNPC : NPC
{
    [SerializeField] GameObject MarketUI;


    public override void Interact(Player player)
    {
        PlayerUIManager.instnace.OpenUI(UIPanelType.Market);
    }
}
