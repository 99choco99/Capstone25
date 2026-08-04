using System;
using System.Collections.Generic;
using UnityEngine;

public static class ItemFactory
{
    public static Dictionary<SlotType, Func<int, ItemInstance>> creator = new(){
        {SlotType.Equipment, (id) => CreateEquipmentInstance(id) },
        {SlotType.Consumption,(id) => new ConsumptionInstance(id) },
        {SlotType.Other,(id) => new OtherInstance(id) },
    };


    public static ItemInstance CreateInstance(ItemBase baseData)
    {
        if(baseData == null) return null;


        if(creator.TryGetValue(baseData.type, out var func))
        {
            return func.Invoke(baseData.id);
        }
        return null;
    }

    private static EquipmentInstance CreateEquipmentInstance(int id)
    {
        var instance = new EquipmentInstance(id);
        instance.bonusStat.attackPower = Mathf.Round(UnityEngine.Random.Range(-2f, 2f));
        instance.bonusStat.defense = Mathf.Round(UnityEngine.Random.Range(-2f, 2f));
        return instance;
    }
}
