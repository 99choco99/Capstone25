using System;
using UnityEngine;

public static class InventoryEvents
{
    // 특정 슬롯의 데이터가 변경되었음
    public static Action<SlotType, int> OnSlotDataChanged;
    // 인벤토리 데이터 초기화가 완료되었음
    public static Action<SlotType, int> OnInventoryDataInitialized;
    // 소유 골드가 변경됨.
    public static Action<int> OnChangedGold;
    // 퀵슬롯 아이템 사용 완료
    public static Action<ItemSpec> OnQuickSlotUsed;
}
