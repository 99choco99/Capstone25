
using System;
using System.Collections.Generic;
using UnityEngine;


public enum EquipmentType { Helmet, Top, Bottom, Shoes, Gloves, Accessory, None }
public class EquipmentManager : MonoBehaviour
{
    // 장착된 아이템들을 관리 (어떤 부위에 어떤 아이템?)
    public Dictionary<EquipmentType, ItemSpec> EquippedItems = new Dictionary<EquipmentType, ItemSpec>();
    [SerializeField] private PlayerStats playerStats; // 스탯을 적용할 대상

    public event Action OnChangedEquipmentItem;


    // 아이템 장착 함수
    public void Equip(EquipmentType slotType, ItemSpec spec)
    {
        if (EquippedItems.ContainsKey(slotType)) { Unequip(slotType); }

        EquippedItems[slotType] = spec;
        playerStats.AddStatsModifier(spec);

        SoundManager.Instance.PlaySFX("Equip");
        OnChangedEquipmentItem?.Invoke();
    }

    public void Unequip(EquipmentType slotType)
    {
        if (EquippedItems.ContainsKey(slotType))
        {
            playerStats.RemoveStatsModifier(EquippedItems[slotType]);
            EquippedItems.Remove(slotType);
            OnChangedEquipmentItem?.Invoke();
        }
    }
}