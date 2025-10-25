using NUnit.Framework;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static PublicAPIManager;

public class SellingList : MonoBehaviour
{
    [SerializeField] private bool isMySellingList;
    [SerializeField] GameObject SellContainer;


    private void Awake()
    {
        PublicAPIManager.Instance.Market.OnGetSellingListComplete += UpdateSellingList;
        PublicAPIManager.Instance.Market.OnGetMySellingListComplete += UpdateSellingList;
        PublicAPIManager.Instance.Market.OnItemRegistComplete += UpdateSellingList;
    }
    private void OnDestroy()
    {
        PublicAPIManager.Instance.Market.OnGetSellingListComplete -= UpdateSellingList;
        PublicAPIManager.Instance.Market.OnGetMySellingListComplete -= UpdateSellingList;
        PublicAPIManager.Instance.Market.OnItemRegistComplete -= UpdateSellingList;
    }


    private void OnEnable()
    {
        Clear();
        RequestMarketListUpdate();
    }

    // 마켓 목록 갱신을 요청
    public void RequestMarketListUpdate()
    {
        if (MarketManager.Instance == null || PublicAPIManager.Instance.Market == null)
        {
            return;
        }
        if (isMySellingList)
        {
            // 내 판매 목록만 가져오기
            MarketManager.Instance.GetMyList();
        }
        else
        {
            // 전체 마켓 목록 가져오기
            MarketManager.Instance.GetAllList();
        }
    }

    //판매목록 업데이트
    private void UpdateSellingList(IMarketItemResponse response)
    {
        if (!gameObject.activeSelf) { return; }
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

        if (isMySellingList) { itemContainer.GetComponent<MarketBuy>().SetActiveItemCancelButton(isMySellingList); }
    }

    void Clear()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }
}
