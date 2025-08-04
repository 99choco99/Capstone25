using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MarketBuy : MonoBehaviour
{
    Button BuyButton;
    [SerializeField] TextMeshProUGUI spec;
    [SerializeField] TextMeshProUGUI price;
    [SerializeField] GameObject check;


    private void Start()
    {
        BuyButton = GetComponentInChildren<Button>();

        BuyButton.onClick.AddListener(() => {
            GameObject checkBox = Instantiate(check, transform.root);

            checkBox.SetActive(true);
            checkBox.GetComponentInChildren<Button>().onClick.AddListener(() =>
            {
                BuyItem();
                Destroy(checkBox);
            });
        });
    }


    void BuyItem()
    {
        //Slot slot = InventoryManager.instance.FindEmptySlot(buySlot.currentItem.data.type);
        //slot.currentItem = Instantiate(buySlot.currentItem.data.OwnedStatePrefab, slot.transform).GetComponent<OwnedItem>();
        Destroy(gameObject);
        //InventoryManager.instance.GetSingleItemSlot(buySlot.currentItem.data.type,buySlot, slot);
        //Debug.Log("±¸¸Å");
    }
}
