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
        // 공격력 성장은 PlayerStats가 담당하고 숫자형 방어력은 사용하지 않으므로,
        // 장비 생성 시 두 전투 수치를 무작위로 부여하지 않습니다.
        return new EquipmentInstance(id);
    }
}
