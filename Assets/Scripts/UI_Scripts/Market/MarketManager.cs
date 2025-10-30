using System;
using TMPro;
using UnityEngine;

public class MarketManager : MonoBehaviour
{
    public static MarketManager Instance;
    public GameObject MarketUI;

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
    }

    //API 호출 함수
    public void SellItem(int Itemid, ItemSpec itemspec, string price, string count)
    {
        string myUserId = PublicAPIManager.Instance.loginData.user_id;
        PublicAPIManager.Instance.Market.RequestToSellItem(myUserId, Itemid, itemspec, price, count);
    }

    public void CancelMyItem(int marketId)
    {
        string myUserId = PublicAPIManager.Instance.loginData.user_id;
        PublicAPIManager.Instance.Market.RequestToCancelItem(myUserId, marketId);
    }

    public void BuyItem(int marketId, string count)
    {
        string myUserId = PublicAPIManager.Instance.loginData.user_id;
        PublicAPIManager.Instance.Market.RequestToBuyItem(myUserId, marketId, count);
    }

    public void GetMyList()
    {
        string myUserId = PublicAPIManager.Instance.loginData.user_id;
        PublicAPIManager.Instance.Market.RequestToGetMyList(myUserId);
    }

    public void GetAllList()
    {
        PublicAPIManager.Instance.Market.RequestToGetSellingList();
    }
}
