using System.Collections;
using UnityEngine;

public class ConsumptionItem : OwnedItem
{

    public void consume(PlayerSetting playerData)
    {
        playerData.ApplyStatChanges(data.damage, data.hp, data.defense, data.speed);
    }

    IEnumerator buff_duration(float duration)
    {
        yield break;
    }

}
