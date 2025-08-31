using System;
using UnityEngine;

[Serializable] // 인스펙터에서 보려면 추가
public class SlotData
{
    public SlotType slotType;
    public ItemData currentItemData; // 아이템의 데이터
    public int slotIndex;          // 슬롯 번호
    public int itemCount;                 // 아이템 개수
    public bool hasItem => currentItemData != null; // 아이템 유무

}
