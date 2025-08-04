using UnityEngine;

public class MarketNPC : NPC
{
    [SerializeField] GameObject MarketUI;


    public override void Interact(Transform Player)
    {
        Player.GetComponent<PlayerController>().OpenUI(UIPanelType.Market);
    }
}
