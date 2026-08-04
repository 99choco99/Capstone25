using Newtonsoft.Json;
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
public class MarketAPI
{
    string userId;


    public MarketAPI(string userId){
        this.userId = userId;
    }

    //판매 목록 가져오기
    public async Awaitable<IMarketItemResponse[]> GetSellingList()
    {
        string url = $"{APIConstants.BASE_API_URL}/market/items";
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            await webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    return JsonConvert.DeserializeObject<IMarketItemResponse[]>(webRequest.downloadHandler.text);
                }
                catch (JsonException ex)
                {
                    Debug.LogError("JSON 역직렬화 오류: " + ex.Message);
                    return null;
                }
            }
            else
            {
                Debug.LogError(webRequest.downloadHandler);
                return null;
            }
        }
    }


    public async Awaitable<IMarketItemResponse[]> GetSellingList(string id)
    {
        string url = $"{APIConstants.BASE_API_URL}/market/items/{id}";

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            await webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    return JsonConvert.DeserializeObject<IMarketItemResponse[]>(webRequest.downloadHandler.text);
                }
                catch (JsonException ex)
                {
                    Debug.LogError("JSON 역직렬화 오류: " + ex.Message);
                    return null;
                }
            }
            return null;
        }
    }

    //판매 요청
    public async Awaitable<ItemRegistResponse> RequestToSell()
    {
        string json = " ";
        string url = $"{APIConstants.BASE_API_URL}/market/items";  // 아이템 경매 등록 url설정해야됨.

        using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            await webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("서버 응답 성공!");
                string responseJson = webRequest.downloadHandler.text;
                try
                {
                    return JsonConvert.DeserializeObject<ItemRegistResponse>(responseJson);
                }
                catch(JsonException ex) { Debug.LogError("JSON 역직렬화 오류: " + ex.Message); return null; }
            }
            else
            {
                Debug.LogError("아이템 등록 실패: " + webRequest.error);
                return null;
            }
        }

    }

    // 구매 요청
    public async Awaitable<BuyItemResponse> RequestToBuy(int marketId, string count)
    {
        string url = $"{APIConstants.BASE_API_URL}/market/buy?userId={userId}&marketId={marketId}&currentAmount={count}";

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            webRequest.downloadHandler = new DownloadHandlerBuffer();

            await webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("서버 응답 성공!");
                string responseJson = webRequest.downloadHandler.text;
                try
                {
                    return JsonConvert.DeserializeObject<BuyItemResponse>(responseJson);

                }
                catch (JsonException ex)
                {
                    Debug.LogError("JSON 역직렬화 오류" + ex.Message);
                    return null;
                }
            }
            else
            {
                Debug.LogError("서버 통신 장애");
                return null;
            }
        }
    }

    //아이템 등록 취소
    public async Awaitable<CancelRegistResponse> CancelRegistItem(string userId, int marketId)
    {
        string url = $"{APIConstants.BASE_API_URL}/market/items/{userId}/{marketId}";

        using (UnityWebRequest webRequest = UnityWebRequest.Delete(url))
        {
            webRequest.downloadHandler = new DownloadHandlerBuffer();

            await webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("아이템 삭제 요청 성공");
                string responseJson = webRequest.downloadHandler.text;
                return JsonConvert.DeserializeObject<CancelRegistResponse>(responseJson);
            }
            else
            {
                Debug.LogError($"아이템 삭제 실패: {webRequest.result}");
                return null;
            }
        }
    }
}






