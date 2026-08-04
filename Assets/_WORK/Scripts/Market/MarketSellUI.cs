using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static APIManager;

public class MarketSellUI : MonoBehaviour
{

    [SerializeField] GameObject notice;         //결과 통지
    TextMeshProUGUI notice_text;                //결과 통지 텍스트
    [SerializeField] GameObject check;          //확인창
    [SerializeField] TMP_InputField price;      //사용자가 적은 가격
    [SerializeField] TMP_InputField count;      //사용자가 적은 아이템 개수
    [SerializeField] Button SellBtn;            //판매 버튼
    SaleSlot saleSlot;                          //판매 슬롯

    int price_result;
    int count_result;

    private void Awake()
    {
        saleSlot = GetComponentInChildren<SaleSlot>();
        notice_text = notice.GetComponentInChildren<TextMeshProUGUI>(true);
        Button noticeBtn = notice.GetComponentInChildren<Button>();
        noticeBtn.onClick.RemoveAllListeners(); // 중복 방지
        noticeBtn.onClick.AddListener(() => { notice.SetActive(false); });


        SellBtn.onClick.RemoveAllListeners();
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

        Button confirmBtn = check.GetComponentInChildren<Button>();
        confirmBtn.onClick.RemoveAllListeners(); //여기서 중복 등록을 막아야 두 번 판매되지 않음
        confirmBtn.onClick.AddListener(() => {

            MarketManager.Instance.SellItem(
                //saleSlot.slotData.itemData.id,
                //saleSlot.slotData.itemSpec,
                //price.choiceText,
                //currentAmount.choiceText,
                //saleSlot.slotData.slotType,
                //saleSlot.slotData.slotIndex
            );
            check.SetActive(false);
        });


    }


    private void Start()
    {

    }

    private void OnDisable()
    {
        ClearSaleSlot();
    }

    private void OnDestroy()
    {
        SellBtn.onClick.RemoveAllListeners();
        notice.GetComponentInChildren<Button>().onClick.RemoveAllListeners();
        check.GetComponentInChildren<Button>().onClick.RemoveAllListeners();

    }

    //등록 가능한지 검사
    private bool CheckState()
    {
        //슬롯에 아이템이 없을 시
        if (saleSlot.slotData.itemData == null)
        {
            notice_text.text = "Slot Error";
        }
        //가격이 적혀있지 않을 시
        else if (!int.TryParse(price.text, out price_result) || price_result <= 0)
        {
            notice_text.text = "price Error";
        }
        //개수가 적혀있지 않을 시
        else if((!int.TryParse(count.text, out count_result) || count_result <= 0) || count_result > saleSlot.slotData.itemCount) {
            notice_text.text = "currentAmount Error";
        }
        else
        {
            return true;
        }
        return false;
    }


    //등록 성공시 UI 업데이트
    void ItemRegistComplete(ItemRegistResponse response)
    {
        SlotData saleSlotData = saleSlot.slotData;
        if (saleSlotData == null)
        {
            Debug.LogWarning("판매할 아이템이 없습니다.");
            return;
        }
        //DataManager.Instance.SlotDict.RegisterItemToMarket(saleSlotData, response.ItemCount);
        //ShowNotice(response.message);
        ClearSaleSlot();
    }



    // 판매 슬롯을 초기화하는 전용 함수
    private void ClearSaleSlot()
    {
        saleSlot.slotData = new SlotData();
        saleSlot.itemImage.sprite = null;
        saleSlot.slotData.itemCount = 0;
        price.text = string.Empty;
        count.text = string.Empty;
    }


    // 결과 메시지를 표시하는 전용 함수
    public void ShowNotice(string message)
    {
        notice.SetActive(true);
        notice_text.text = message;
        notice.transform.SetAsLastSibling();
    }


}
