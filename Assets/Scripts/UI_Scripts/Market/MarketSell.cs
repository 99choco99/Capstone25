using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static SocketManager;

public class MarketSell : MonoBehaviour
{

    [SerializeField] GameObject SellingList;    //판매목록
    [SerializeField] GameObject SellContainer;  //판매목록에 등록될 양식
    [SerializeField] GameObject notice;         //결과 통지
    TextMeshProUGUI notice_text;                //결과 통지 텍스트
    [SerializeField] GameObject check;          //확인창
    [SerializeField] TMP_InputField price;      //사용자가 적은 가격
    [SerializeField] TMP_InputField count;      //사용자가 적은 아이템 개수
    [SerializeField] Button SellBtn;            //판매 버튼
    SaleSlot saleSlot;                          //판매 슬롯

    int price_result;
    int count_result;

    private void Start()
    {
        saleSlot = GetComponentInChildren<SaleSlot>();
        notice_text = notice.GetComponentInChildren<TextMeshProUGUI>(true);
        notice.GetComponentInChildren<Button>().onClick.AddListener(() => { notice.SetActive(false); });


        SocketManager.Instance.OnItemRegisterSuccess += RegistForSale;


        SellBtn.onClick.AddListener(() => {
            //판매 가능한 상태인지 검사
            if (CheckState())
            {
                check.SetActive(true);
            }
            else
            {
                notice.SetActive(true);
            }
        });

        //판매버튼 클릭시 서버에 판매 목록 등록 요청
        check.GetComponentInChildren<Button>().onClick.AddListener(() => {
            SocketManager.Instance.RequestToSellItem(saleSlot.currentItem.data.id, saleSlot.currentItem.data.spec, price.text ,count.text);
            check.SetActive(false);
        });
    }


    private void OnDestroy()
    {
        SellBtn.onClick.RemoveAllListeners();
        notice.GetComponentInChildren<Button>().onClick.RemoveAllListeners();
        check.GetComponentInChildren<Button>().onClick.RemoveAllListeners();
        SocketManager.Instance.OnItemRegisterSuccess -= RegistForSale;
    }
    private bool CheckState()
    {
        //슬롯에 아이템이 없을 시
        if (saleSlot.currentItem == null)
        {
            notice_text.text = "Slot Error";
        }
        //가격이 적혀있지 않을 시
        else if (!int.TryParse(price.text, out price_result) && price_result <= 0)
        {
            notice_text.text = "price Error";
        }
        //개수가 적혀있지 않을 시
        else if((!int.TryParse(count.text, out count_result) && count_result <= 0) || count_result > saleSlot.itemCount) {
            notice_text.text = "count Error";
        }
        else
        {
            return true;
        }
        return false;
    }


    void RegistForSale(ItemRegistResponse response)
    {
        // 1. 판매 컨테이너 생성 및 부모 설정
        GameObject itemContainer = Instantiate(SellContainer, SellingList.transform);

        // 2. 아이템 정보 업데이트
        UpdateSaleItemInfo(itemContainer, saleSlot.currentItem, response);

        // 3. 인벤토리에서 해당 아이템 개수 차감.
        UpdateInventoryAfterSale(response);
        

        // 3. 현재 판매 슬롯 초기화
        ClearSaleSlot();
    }

    // 판매 목록 아이템의 정보를 업데이트
    private void UpdateSaleItemInfo(GameObject itemContainer, OwnedItem itemToSell, ItemRegistResponse response)
    {
        // 아이템 아이콘 변경
        MarketBuy marketBuyComponent = itemContainer.GetComponent<MarketBuy>();
        marketBuyComponent.marketId = response.marketId;
        marketBuyComponent.icon.sprite = itemToSell.data.icon;



        // Text 정보 업데이트
        TextMeshProUGUI[] itemInfoText = itemContainer.GetComponentsInChildren<TextMeshProUGUI>();
        if (itemInfoText.Length >= 3)
        {
            // 첫 번째 텍스트: 아이템 이름
            itemInfoText[0].text = $"Name: {itemToSell.data.name}\n";
            // 두 번쨰 텍스트: 아이템 개수
            itemInfoText[1].text = $"Count: {response.ItemCount}";
            // 세 번째 텍스트: 가격 정보
            itemInfoText[2].text = $"{response.price} Gold";
        }

    }

    void UpdateInventoryAfterSale(ItemRegistResponse response)
    {
        Slot slot = saleSlot.currentItem.currentSlot;
        if(slot == null) { Debug.Log("슬롯없음"); }
        InventoryManager.instance.UpdateSlot(slot, -response.ItemCount);
        slot.itemCount -= response.ItemCount;
        if(slot.itemCount > 0)
        {
            slot.UpdateUI();
        }
        else
        {

            Destroy(slot.currentItem.gameObject);
            slot.Clear();
        }
    }


    // 판매 슬롯을 초기화하는 전용 함수
    private void ClearSaleSlot()
    {

        // 슬롯에 있던 아이템을 제거하고 null로 설정
        if (saleSlot.currentItem != null)
        {
            saleSlot.itemImage.sprite = null;
            saleSlot.currentItem = null;
            saleSlot.hasItem = false;
            saleSlot.itemCount = 0;
        }
        price.text = string.Empty;
        count.text = string.Empty;
    }

}
