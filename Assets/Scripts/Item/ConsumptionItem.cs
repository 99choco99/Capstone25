using System.Collections;
using UnityEngine;

public class ConsumptionItem : OwnedItem
{

    public void consume(PlayerSetting playerData)
    {
        playerData.ApplyStatChanges(data.spec.damage, data.spec.hp, data.spec.defense, data.spec.speed);
    }

    IEnumerator buff_duration(float duration)
    {
        yield break;
    }

}
