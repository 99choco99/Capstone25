using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
            SocketManager.Instance.RequestToSellItem(saleSlot.currentItem.data.id, price.text ,count.text);
            check.SetActive(false);
        });
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
        else if(!int.TryParse(count.text, out count_result) && count_result <= 0) {
            notice_text.text = "count Error";
        }
        else
        {
            return true;
        }
        return false;
    }


    void RegistForSale(string successMessage)
    {
        // 1. 판매 컨테이너 생성 및 부모 설정
        GameObject newSaleItem = Instantiate(SellContainer, SellingList.transform);

        // 2. 아이템 정보 업데이트
        UpdateSaleItemInfo(newSaleItem, saleSlot.currentItem);

        // 3. 현재 판매 슬롯 초기화
        ClearSaleSlot();

        // 4. 성공 메시지 표시
        ShowNotice(successMessage);
    }

    // 판매 목록 아이템의 정보를 업데이트
    private void UpdateSaleItemInfo(GameObject saleItem, OwnedItem itemToSell)
    {
        // 아이템 아이콘 변경
        Image itemImage = saleItem.GetComponentInChildren<Image>();
        if (itemImage != null && itemToSell.icon != null)
        {
            itemImage.sprite = itemToSell.icon.sprite;
        }

        // 모든 TextMeshProUGUI 컴포넌트를 찾아 정보 업데이트
        TextMeshProUGUI[] itemInfos = saleItem.GetComponentsInChildren<TextMeshProUGUI>();
        if (itemInfos.Length >= 2)
        {
            // 첫 번째 텍스트: 아이템 이름과 개수
            itemInfos[0].text = $"Name: {itemToSell.data.name}\n" +
                                $"Count: {count.text}";

            // 두 번째 텍스트: 가격 정보
            itemInfos[1].text = $"{price.text} Gold";
        }
    }

    // 판매 슬롯을 초기화하는 전용 함수
    private void ClearSaleSlot()
    {
        // 슬롯에 있던 아이템을 제거하고 null로 설정
        if (saleSlot.currentItem != null)
        {
            Destroy(saleSlot.currentItem.gameObject);
            saleSlot.currentItem = null;
        }
        price.text = string.Empty;
        count.text = string.Empty;
    }

    // 결과 메시지를 표시하는 전용 함수
    private void ShowNotice(string message)
    {
        notice.SetActive(true);
        notice_text.text = message;
    }

}
