using NUnit.Framework;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor.Overlays;
using UnityEngine;

public class SellingList : MonoBehaviour
{
    [SerializeField] private bool isMySellingList;
    [SerializeField] GameObject SellContainer;


    private void OnEnable()
    {
        Clear();
        RequestMarketListUpdate();
    }

    // 마켓 목록 갱신을 요청
    public void RequestMarketListUpdate()
    {
        if (MarketManager.Instance == null)
        {
            return;
        }
        if (isMySellingList)
        {
            // 내 판매 목록만 가져오기

        }
        else
        {
            // 전체 마켓 목록 가져오기

        }
    }

    //판매목록 업데이트
    private void UpdateSellingList(IMarketItemResponse response)
    {
        GameObject container = Instantiate(SellContainer,transform);
        ItemBase itemData = ItemManager.Instance.GetItem(response.ItemId);

        UpdateSaleItemInfo(container, itemData, response);
    }


    // 아이템의 정보를 업데이트
    private void UpdateSaleItemInfo(GameObject itemContainer, ItemBase itemData, IMarketItemResponse response)
    {
        // 아이템 아이콘 변경
        MarketBuy marketBuyComponent = itemContainer.GetComponent<MarketBuy>();
        marketBuyComponent.marketId = response.marketId;
        //marketBuyComponent.icon.sprite = Resources.Load<Sprite>(itemData.iconPath);



        // Text 정보 업데이트
        TextMeshProUGUI[] itemInfoText = itemContainer.GetComponentsInChildren<TextMeshProUGUI>();
        if (itemInfoText.Length >= 3)
        {
            // 첫 번째 텍스트: 아이템 이름
            itemInfoText[0].text = $"Name: {itemData.itemName}\n";
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
