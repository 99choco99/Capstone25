using Newtonsoft.Json;
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static PublicAPIManager;

public class MarketAPI
{
    private MonoBehaviour coroutineRunner;

    // 아이템 등록 로직 완료
    public event Action<ItemRegistResponse> OnItemRegistComplete;
    public event Action<string> OnItemRegistFailed;

    //아이템 구매
    public event Action<BuyItemResponse> OnItemPurchaseComplete;
    public event Action<string> OnItemPurchaseFailed;

    //아이템 취소
    public event Action<CancelRegistResponse> OnCancelRegistComplete;
    public event Action<string> OnCancelRegistFailed;

    // 아이템 목록 불러오기 완료
    public event Action<IMarketItemResponse> OnGetSellingListComplete;
    public event Action<IMarketItemResponse> OnGetMySellingListComplete;



    // 생성자를 통해 MonoBehaviour 인스턴스를 주입
    public MarketAPI(MonoBehaviour runner)
    {
        this.coroutineRunner = runner;
    }

    //판매 목록 가져오기
    IEnumerator GetSellingList()
    {
        string url = $"{APIConstants.BASE_API_URL}/market/items";
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    IMarketItemResponse[] responseList = JsonConvert.DeserializeObject<IMarketItemResponse[]>(webRequest.downloadHandler.text);
                    foreach (var response in responseList)
                    {
                        if(response.userId != PublicAPIManager.Instance.loginData.user_id)
                        {
                            OnGetSellingListComplete?.Invoke(response);
                        }
                    }
                }
                catch (JsonException ex)
                {
                    Debug.LogError("JSON 역직렬화 오류: " + ex.Message);
                }
            }
            else
            {
                Debug.LogError(webRequest.downloadHandler);
            }
        }
    }


    IEnumerator GetSellingList(string id)
    {
        string url = $"{APIConstants.BASE_API_URL}/market/items/{id}";

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    IMarketItemResponse[] responseList = JsonConvert.DeserializeObject<IMarketItemResponse[]>(webRequest.downloadHandler.text);
                    foreach (var response in responseList)
                    {
                        OnGetMySellingListComplete?.Invoke(response);
                    }
                }
                catch (JsonException ex)
                {
                    Debug.LogError("JSON 역직렬화 오류: " + ex.Message);
                }
            }
        }
    }

    //판매 요청
    IEnumerator RequestToSell(string userId, int ItemId, ItemSpec spec, string price, string count, SlotType slotType, int slotIndex)
    {
        var itemData = new
        {
            userId = userId,
            ItemId = ItemId,
            itemSpec = spec,
            price = price,
            itemCount = count,
            slotType = slotType,
            slotIndex = slotIndex

        };
        string json = JsonConvert.SerializeObject(itemData);


        string url = $"{APIConstants.BASE_API_URL}/market/items";  // 아이템 경매 등록 url설정해야됨.

        using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("서버 응답 성공!");
                string responseJson = webRequest.downloadHandler.text;
                try
                {
                    ItemRegistResponse response = JsonConvert.DeserializeObject<ItemRegistResponse>(responseJson);

                    if (response.success)
                    {
                        OnItemRegistComplete?.Invoke(response);
                    }
                    else
                    {
                        Debug.LogError("아이템 등록 실패: " + response.message);
                        OnItemRegistFailed?.Invoke(response.message);
                    }
                }
                catch { }
            }
            else
            {
                Debug.LogError("아이템 등록 실패: " + webRequest.error);
            }
        }

    }

    // 구매 요청
    IEnumerator RequestToBuy(string userId, int marketId, string count)
    {
        string url = $"{APIConstants.BASE_API_URL}/market/buy?userId={userId}&marketId={marketId}&count={count}";

        using (UnityWebRequest webRequest = new UnityWebRequest(url, "GET"))
        {
            webRequest.downloadHandler = new DownloadHandlerBuffer();

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("서버 응답 성공!");
                string responseJson = webRequest.downloadHandler.text;
                try
                {
                    BuyItemResponse response = JsonConvert.DeserializeObject<BuyItemResponse>(responseJson);

                    if (response.success)
                    {
                        OnItemPurchaseComplete?.Invoke(response);
                    }
                    else
                    {
                        OnItemPurchaseFailed?.Invoke(response.message);
                    }
                }
                catch (JsonException ex)
                {
                    Debug.LogError("JSON 역직렬화 오류" + ex.Message);
                }
            }
            else
            {
                Debug.LogError("서버 통신 장애");
            }
        }
    }

    //아이템 등록 취소
    IEnumerator CancelRegistItem(string userId, int marketId)
    {
        string url = $"{APIConstants.BASE_API_URL}/market/items/{userId}/{marketId}";

        using (UnityWebRequest webRequest = UnityWebRequest.Delete(url))
        {
            webRequest.downloadHandler = new DownloadHandlerBuffer();

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("아이템 삭제 요청 성공");
                string responseJson = webRequest.downloadHandler.text;
                try
                {
                    CancelRegistResponse response = JsonConvert.DeserializeObject<CancelRegistResponse>(responseJson);
                    if (response.success)
                    {
                        OnCancelRegistComplete?.Invoke(response);
                    }
                    else
                    {
                        Debug.LogError("아이템 등록 취소 실패: " + response.message);
                        OnCancelRegistFailed?.Invoke(response.message);
                    }
                }
                catch (JsonException ex)
                {
                    Debug.LogError($"역직렬화 오류 : {ex.Message}");
                }
            }
            else
            {
                Debug.LogError($"아이템 삭제 실패: {webRequest.result}");
            }
        }
    }


    public void RequestToSellItem(string userId, int Itemid, ItemSpec itemspec, string price, string count, SlotType slotType, int slotIndex)
    {
        coroutineRunner.StartCoroutine(RequestToSell(userId, Itemid, itemspec, price, count, slotType, slotIndex));
    }

    public void RequestToBuyItem(string userId, int marketId, string count)
    {
        coroutineRunner.StartCoroutine(RequestToBuy(userId, marketId, count));
    }

    public void RequestToCancelItem(string userId, int marketId)
    {
        coroutineRunner.StartCoroutine(CancelRegistItem(userId, marketId));
    }

    public void RequestToGetMyList(string userId)
    {
        coroutineRunner.StartCoroutine(GetSellingList(userId));
    }

    // "공용" 함수는 userId가 필요 없음
    public void RequestToGetSellingList()
    {
        coroutineRunner.StartCoroutine(GetSellingList());
    }
}




public class IMarketItemResponse
{
    public string userId { get; set; } //등록한 유저 아이디
    public int marketId { get; set; }  //마켓 id
    public int ItemId { get; set; }     //아이템 id
    public int ItemCount { get; set; }  // 등록된 아이템 개수
    public int price { get; set; }   //등록한 가격
}


public class ItemRegistResponse : IMarketItemResponse
{
    public bool success;  //등록 성공 여부
    public string message { get; set; }  // 성공 or 실패 메세지
}

public class CancelRegistResponse : IMarketItemResponse
{
    public bool success;
    public string message { get; set; }
    public ItemSpec spec { get; set; }
}


public class BuyItemResponse
{
    public bool success;
    public string message { get; set; }
    public int marketId { get; set; }
    public int ItemId { get; set; }
    public ItemSpec spec { get; set; }
    public int purchasedItemCount { get; set; }
    public int remainingItemCount { get; set; }
    public int gold { get; set; }
}




