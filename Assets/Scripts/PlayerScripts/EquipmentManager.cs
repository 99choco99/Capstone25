
using System;
using System.Collections.Generic;
using UnityEngine;


public enum EquipmentType { helmet, top, bottom, shoes, gloves, accessory,None }
public class EquipmentManager : MonoBehaviour
{
    // 장착된 아이템들을 관리 (어떤 부위에 어떤 아이템?)
    public Dictionary<EquipmentType, ItemSpec> equippedItems = new Dictionary<EquipmentType, ItemSpec>();

    private PlayerStats playerStats; // 스탯을 적용할 대상
    public event Action OnChangedEquipmentItem;


    // 아이템 장착 함수
    public void Equip(EquipmentType slotType, ItemSpec spec)
    {
        if (playerStats == null)
        {
            playerStats = GetComponentInParent<PlayerStats>();
        }

        if (equippedItems.ContainsKey(slotType))
        {
            Unequip(slotType);
        }

        equippedItems[slotType] = spec;


        playerStats.ApplyStatChanges(spec);
        SoundManager.Instance.PlaySFX("Equip");
        OnChangedEquipmentItem?.Invoke();
    }

    public void Unequip(EquipmentType slotType)
    {
        if (equippedItems.ContainsKey(slotType))
        {
            playerStats.ApplyStatChanges(equippedItems[slotType], false);
            equippedItems.Remove(slotType);
        }
    }
}