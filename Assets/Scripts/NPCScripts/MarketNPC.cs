using UnityEngine;

public class MarketNPC : NPC
{
    [SerializeField] GameObject MarketUI;


    public override void Interact(Player player)
    {
        PlayerUIManager.instance.OpenUI(UIPanelType.Market);
    }
}
