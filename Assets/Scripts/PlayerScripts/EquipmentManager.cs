// EquipmentManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager instance;

    // 장착된 아이템들을 관리 (어떤 부위에 어떤 아이템?)
    public Dictionary<EquipmentType, ItemSpec> equippedItems = new Dictionary<EquipmentType, ItemSpec>();

    private PlayerStats playerStats; // 스탯을 적용할 대상
    public event Action OnChangedEquipmentItem;

    void Awake() {
        if (instance == null)
        {
            instance = this;
        }
    }
    void Start() { playerStats = GetComponentInParent<PlayerStats>(); }

    // 아이템 장착 함수
    public void Equip(EquipmentType slotType, ItemSpec spec)
    {
        if (equippedItems.ContainsKey(slotType))
        {
            Unequip(slotType);
        }

        equippedItems[slotType] = spec;

        playerStats.ApplyStatChanges(spec);

        OnChangedEquipmentItem?.Invoke();
    }

    public void Unequip(EquipmentType slotType)
    {
        if (equippedItems.ContainsKey(slotType))
        {
            // 인벤토리로 아이템을 돌려주는 로직 (InventoryManager.instance.AddItem(...))

            equippedItems.Remove(slotType);
            playerStats.ApplyStatChanges(null);
        }
    }
}