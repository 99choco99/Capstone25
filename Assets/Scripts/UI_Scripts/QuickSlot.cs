using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class QuickSlot : Slot
{
    Player player;
    [SerializeField] Slider slider;
    [SerializeField] InventoryManager Inventory;
    bool isCoolingDown = false;

    private void Awake()
    {
        player = GetComponentInParent<Player>();
        Inventory.OnQuickSlotUsed += StartCooldownVisual;
    }

    private void Start()
    {
        slider = GetComponentInChildren<Slider>();
    }

    private void OnDestroy()
    {
        Inventory.OnQuickSlotUsed -= StartCooldownVisual;
    }


    public override void OnDrop(PointerEventData eventData)
    {
        if (isCoolingDown) { return; }
        if (eventData.pointerDrag.TryGetComponent<OwnedItem>(out OwnedItem newItem) && newItem.data.type == SlotType.Consumption)
        {
            base.OnDrop(eventData);
        }
    }

    public void RequestUseItem()
    {
        if (isCoolingDown)
        {
            Debug.Log("쿨타임 중입니다.");
            return;
        }

        // 실제 사용 로직은 InventoryManager에게
        Inventory.RequestUseQuickSlotItem();
    }

    private void StartCooldownVisual(ItemSpec spec)
    {
        if (slotData == null || !slotData.hasItem)
        {
            return;
        }
        StartCoroutine(CooldownCoroutine(spec.coolTime));
    }

    IEnumerator CooldownCoroutine(float time)
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
