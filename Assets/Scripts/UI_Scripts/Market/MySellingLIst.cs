using TMPro;

using UnityEngine;
using UnityEngine.UI;
using static SocketManager;

public class MySellingLIst : MonoBehaviour
{

    [SerializeField] GameObject SellContainer;
    private void OnEnable()
    {
        Clear();
        SocketManager.Instance.RequestToGetMyList();
        SocketManager.Instance.OnGetMySellingListSuccess += UpdateSellingList;
    }

    private void OnDisable()
    {
        SocketManager.Instance.OnGetMySellingListSuccess -= UpdateSellingList;
    }

    private void UpdateSellingList(GetSellingListResponse response)
    {

        GameObject container = Instantiate(SellContainer, transform);
        ItemData itemData = ItemManager.Instance.GetItem(response.ItemId);
        container.GetComponent<Button>().interactable = false;

        UpdateSaleItemInfo(container, itemData, response);
    }


    // 판매 목록 아이템의 정보를 업데이트
    private void UpdateSaleItemInfo(GameObject itemContainer, ItemData itemData, GetSellingListResponse response)
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

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }
}
