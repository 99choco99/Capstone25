using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MarketBuy : MonoBehaviour
{
    [SerializeField] GameObject ConsumptionSlots;
    Button BuyBtn;
    [SerializeField] TextMeshProUGUI spec;
    [SerializeField] TextMeshProUGUI price;
    [SerializeField] GameObject check;
    [SerializeField] BuySlot buySlot;

    private void Start()
    {
        BuyBtn = GetComponentInChildren<Button>();
        buySlot = GetComponentInChildren<BuySlot>();
        ConsumptionSlots = GameObject.Find("Consumption1");
        BuyBtn.onClick.AddListener(() => {
            GameObject _ = Instantiate(check, transform.parent.parent.parent.parent);
            _.GetComponentInChildren<TextMeshProUGUI>().text = "Buy " + buySlot.currentItem.name;
            _.SetActive(true);
            _.GetComponentInChildren<Button>().onClick.AddListener(() =>
            {
                BuyItem();
                Destroy(_);
            });
        });
    }


    void BuyItem()
    {
        //Slot slot = InventoryManager.instance.FindEmptySlot(buySlot.currentItem.data.type);
        //slot.currentItem = Instantiate(buySlot.currentItem.data.OwnedStatePrefab, slot.transform).GetComponent<OwnedItem>();
        InventoryManager.instance.GetItemSlot(SlotType.Consumption, ConsumptionSlots.transform);
        Destroy(gameObject);
        //InventoryManager.instance.GetSingleItemSlot(buySlot.currentItem.data.type,buySlot, slot);
        //Debug.Log("±¸¸Å");
    }
}
