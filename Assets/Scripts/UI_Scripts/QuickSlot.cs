using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class QuickSlot : Slot
{
    Slider slider;
    ConsumptionItem quickItem;
    bool isCoolingDown = false;

    private void Start()
    {
        slider = GetComponentInChildren<Slider>();
    }
    public override void OnDrop(PointerEventData eventData)
    {
        if (isCoolingDown) { return; }
        if (eventData.pointerDrag.TryGetComponent<OwnedItem>(out newItem) && newItem.data.type == SlotType.Consumption)
        {
            base.OnDrop(eventData);
        }
    }

    public void Use()
    {
        if (isCoolingDown)
        {
            return;
        }
        if (quickItem = GetComponentInChildren<ConsumptionItem>())
        {
            ItemCount -= 1;
            if(ItemCount <= 0)
            {
                Destroy(quickItem.gameObject);
                hasItem = false;
            }
            quickItem.consume(playerData);
            StartCoroutine("CoolTime", 3);
        }
    }


    IEnumerator CoolTime(float time)
    {
        isCoolingDown = true;
        slider.value = 1;
        float ElapsedTime = 0;
        while(ElapsedTime <= time)
        {
            ElapsedTime += Time.deltaTime;
            slider.value =  1 - ElapsedTime/time;
            yield return null;
        }
        slider.value = 0;
        isCoolingDown = false;
    }
}
