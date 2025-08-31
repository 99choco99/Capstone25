using TMPro;
using UnityEngine;

public class MarketManager : MonoBehaviour
{
    SaleSlot saleSlot;   //판매 슬롯

    private void Awake()
    {
        saleSlot = GetComponentInChildren<SaleSlot>();
    }

    private void OnEnable()
    {
        // 아이템 등록
        APIEvents.OnItemRegisterSuccess += OnItemRegisterSuccessFromAPI;
        APIEvents.OnItemRegisterFailed += OnItemRegisterFailedFromAPI;

        //아이템 판매목록 가져오기
        APIEvents.OnGetSellingListSuccess += OnGetSellingListSuccessFromAPI;
        APIEvents.OnGetMySellingListSuccess += OnGetMySellingListSuccessFromAPI;

        APIEvents.OnBuyItemSuccess += OnBuyItemSuccessFromAPI;
    }
    private void OnDisable()
    {
        // 구독 해제
        APIEvents.OnItemRegisterSuccess -= OnItemRegisterSuccessFromAPI;
        APIEvents.OnItemRegisterFailed -= OnItemRegisterFailedFromAPI;
        APIEvents.OnGetSellingListSuccess -= OnGetSellingListSuccessFromAPI;
        APIEvents.OnGetMySellingListSuccess -= OnGetMySellingListSuccessFromAPI;
    }


    //아이템 판매 목록 가져오기 성공
    void OnGetSellingListSuccessFromAPI(IMarketItemResponse response)
    {
        MarketManagerEvents.OnGetSellingListComplete?.Invoke(response);
    }

    //내 아이템 판매 목록 가져오기 성공
    void OnGetMySellingListSuccessFromAPI(IMarketItemResponse response)
    {
        MarketManagerEvents.OnGetMySellingListComplete?.Invoke(response);
    }

    //아이템 구매 성공
    void OnBuyItemSuccessFromAPI(BuyItemResponse response)
    {



        MarketManagerEvents.OnItemPurchaseComplete?.Invoke(response);
    }

    //아이템 등록 성공
    void OnItemRegisterSuccessFromAPI(ItemRegistResponse response)
    {
        // 인벤토리에서 해당 아이템 개수 차감.
        UpdateInventoryAfterSale(response);

        MarketManagerEvents.OnItemRegistComplete?.Invoke(response);
    }

    // API 통신 실패 시 호출되는 핸들러
    private void OnItemRegisterFailedFromAPI(string message)
    {
        Debug.LogError("아이템 등록 실패: " + message);
        MarketManagerEvents.OnItemRegistFailed?.Invoke(message);
    }

    void UpdateInventoryAfterSale(ItemRegistResponse response)
    {
        SlotData saleSlotData = saleSlot.slotData;
        if (saleSlotData == null)
        {
            Debug.LogWarning("판매할 아이템이 없습니다.");
            return;
        }
        InventoryManager.instance.RegisterItemToMarket(saleSlotData, response.ItemCount);
    }
}
