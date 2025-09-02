using TMPro;
using UnityEngine;

public class MarketManager : MonoBehaviour
{
    public static MarketManager Instance;
    SaleSlot saleSlot;   //판매 슬롯

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        saleSlot = GetComponentInChildren<SaleSlot>();
    }

    private void OnEnable()
    {
        // 아이템 등록
        APIEvents.OnItemRegister += OnItemRegisterFromAPI;

        //아이템 판매목록 가져오기
        APIEvents.OnGetSellingListSuccess += OnGetSellingListSuccessFromAPI;
        APIEvents.OnGetMySellingListSuccess += OnGetMySellingListSuccessFromAPI;

        //아이템 구매
        APIEvents.OnBuyItem += OnBuyItemFromAPI;

        //아이템 취소
        APIEvents.OnCancelItem += OnCancelRegisterFromAPI;
    }
    private void OnDisable()
    {
        // 구독 해제
        APIEvents.OnItemRegister -= OnItemRegisterFromAPI;
        APIEvents.OnGetSellingListSuccess -= OnGetSellingListSuccessFromAPI;
        APIEvents.OnGetMySellingListSuccess -= OnGetMySellingListSuccessFromAPI;
        APIEvents.OnBuyItem -= OnBuyItemFromAPI;
        APIEvents.OnCancelItem -= OnCancelRegisterFromAPI;
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

    //아이템 구매 성공/실패
    void OnBuyItemFromAPI(BuyItemResponse response)
    {
        if (!response.success)
        {
            MarketManagerEvents.OnItemPurchaseFailed?.Invoke(response.message);
        }
        else
        {
            InventoryManager.instance.AddPurchasedItem(response);
            MarketManagerEvents.OnItemPurchaseComplete?.Invoke(response);
        }

    }

    //아이템 등록
    void OnItemRegisterFromAPI(ItemRegistResponse response)
    {
        if (!response.success)
        {
            Debug.LogError("아이템 등록 실패: " + response.message);
            MarketManagerEvents.OnItemRegistFailed?.Invoke(response.message);
        }
        else
        {
            // 인벤토리에서 해당 아이템 개수 차감.
            UpdateInventoryAfterSale(response);

            MarketManagerEvents.OnItemRegistComplete?.Invoke(response);
        }
    }

    public void OnCancelRegisterFromAPI(CancelRegistResponse response)
    {
        if (!response.success)
        {
            Debug.LogError("아이템 등록 취소 실패: " + response.message);
            MarketManagerEvents.OnCancelRegistFailed?.Invoke(response.message);
        }
        else
        {
            MarketManagerEvents.OnCancelRegistComplete?.Invoke(response);
        }
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

    //아이템 판매
    public void SellItem(int Itemid, ItemSpec itemspec, string price, string count)
    {
        APIManager.Instance.Market.RequestToSellItem(Itemid, itemspec, price, count);
    }

    //아이템 취소
    public void CancelMyItem(int marketId)
    {
        APIManager.Instance.Market.RequestToCancelItem(marketId);
    }
}
