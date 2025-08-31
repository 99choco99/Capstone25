using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static APIManager;

public class SellingList : MonoBehaviour
{
    [SerializeField] GameObject SellContainer;

    private void Awake()
    {
        MarketManagerEvents.OnGetSellingListComplete += UpdateSellingList;
        MarketManagerEvents.OnGetMySellingListComplete += UpdateSellingList;
        MarketManagerEvents.OnItemRegistComplete += UpdateSellingList;
    }
    private void OnDestroy()
    {
        MarketManagerEvents.OnGetSellingListComplete -= UpdateSellingList;
        MarketManagerEvents.OnGetMySellingListComplete -= UpdateSellingList;
    }


    private void OnEnable()
    {
        Clear();
        RequestMarketListUpdate();
    }


    // 마켓 목록 갱신을 요청
    public void RequestMarketListUpdate()
    {
        Instance.Market.RequestToGetSellingList();
    }

    //판매목록 업데이트
    private void UpdateSellingList(IMarketItemResponse response)
    {

        GameObject container = Instantiate(SellContainer,transform);
        ItemData itemData = ItemManager.Instance.GetItem(response.ItemId);

        UpdateSaleItemInfo(container, itemData, response);
    }


    // 아이템의 정보를 업데이트
    private void UpdateSaleItemInfo(GameObject itemContainer, ItemData itemData, IMarketItemResponse response)
    {
        // 아이템 아이콘 변경
        MarketBuy marketBuyComponent = itemContainer.GetComponent<MarketBuy>();
        marketBuyComponent.marketId = response.marketId;
        marketBuyComponent.icon.sprite = itemData.icon;



        // Text 정보 업데이트
        TextMeshProUGUI[] itemInfoText = itemContainer.GetComponentsInChildren<TextMeshProUGUI>();
        if (itemInfoText.Length >= 3)
        {
            // 첫 번째 텍스트: 아이템 이름
            itemInfoText[0].text = $"Name: {itemData.name}\n";
            // 두 번쨰 텍스트: 아이템 개수
            itemInfoText[1].text = $"Count: {response.ItemCount}";
            // 세 번째 텍스트: 가격 정보
            itemInfoText[2].text = $"{response.price} Gold";
        }

    }

    void Clear()
    {
        int childCount = transform.childCount;

        for (int i = transform.childCount -1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }
}
