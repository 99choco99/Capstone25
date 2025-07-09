using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MarketSell : MonoBehaviour
{
    [SerializeField] GameObject SellContainer;
    [SerializeField] GameObject SellingList;
    [SerializeField] GameObject notice;
    TextMeshProUGUI notice_text;
    [SerializeField] GameObject check;
    [SerializeField] TMP_InputField price;
    [SerializeField] TMP_InputField count;
    [SerializeField] Button SellBtn;
    SaleSlot saleSlot;

    int price_result;
    int count_result;

    private void Start()
    {
        saleSlot = GetComponentInChildren<SaleSlot>();
        notice_text = notice.GetComponentInChildren<TextMeshProUGUI>(true);
        notice.GetComponentInChildren<Button>().onClick.AddListener(() => { notice.SetActive(false); });
        
        SellBtn.onClick.AddListener(() => {
            //°Ë»ç
            if (CheckState())
            {
                check.SetActive(true);
            }
            else
            {
                notice.SetActive(true);
            }
        });
        check.GetComponentInChildren<Button>().onClick.AddListener(() => {
            ListForSale();
            check.SetActive(false);
            });
    }

    private bool CheckState()
    {
        if (saleSlot.currentItem == null)
        {
            notice_text.text = "Slot Error";
        }
        else if (!int.TryParse(price.text, out price_result) && price_result <= 0)
        {
            notice_text.text = "price Error";
        }
        else if(!int.TryParse(count.text, out count_result) && count_result <= 0) {
            notice_text.text = "count Error";
        }
        else
        {
            return true;
        }
        return false;
    }

    void ListForSale()
    {
        GameObject item = Instantiate(SellContainer,SellingList.transform);
        BuySlot buySlot = item.GetComponentInChildren<BuySlot>();

        saleSlot.currentItem.transform.SetParent(item.transform);
        saleSlot.currentItem.rect.position = buySlot.rect.position;
        buySlot.currentItem = saleSlot.currentItem;
        buySlot.hasItem = true;

        TextMeshProUGUI[] ItemInfo = item.GetComponentsInChildren<TextMeshProUGUI>();
        ItemInfo[0].text = "Name: " + saleSlot.currentItem.name + "\n" + "Count: " + count.text;
        ItemInfo[1].text = price.text + " Gold" ;


        saleSlot.currentItem = null;
    }

}
