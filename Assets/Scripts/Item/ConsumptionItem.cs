using System.Collections;
using UnityEngine;

public class ConsumptionItem : OwnedItem
{

    public void consume(PlayerStats playerData)
    {
        playerData.ApplyStatChanges();
    }

    IEnumerator buff_duration(float duration)
    {
        yield break;
    }

}
