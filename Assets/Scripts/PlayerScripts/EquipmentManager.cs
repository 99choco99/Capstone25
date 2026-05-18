
using System;
using System.Collections.Generic;
using UnityEngine;



public class EquipmentManager : MonoBehaviour
{
    // 장착된 아이템들을 관리 (어떤 부위에 어떤 아이템?)
    public Dictionary<EquipmentType, ItemSpec> EquippedItems = new Dictionary<EquipmentType, ItemSpec>();
    [SerializeField] private PlayerStats playerStats; // 스탯을 적용할 대상

    public event Action OnChangedEquipmentItem;

}