using UnityEngine;

public class ConsumptionItem : OwnedItem
{

    public void consume(PlayerData playerData)
    {

        playerData.currentHp += data.hp;
        playerData.damage += data.damage;
        playerData.defense += data.defense;
        playerData.speed += data.speed;
        if(playerData.maxHp < playerData.currentHp)
        {
            playerData.currentHp = playerData.maxHp;
        }

    }

}
