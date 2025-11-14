using UnityEngine;

public class MarketNPC : NPC
{
    [SerializeField] GameObject MarketUI;


    public override void Interact(Player player)
    {
        player.InputHandler.UseInteractionInput();
        player.PlayerUIManager.OpenUI(UIPanelType.Market);
    }
}
