using System;
using UnityEngine;

public static class MarketManagerEvents
{
    // 아이템 등록 로직 완료
    public static Action<ItemRegistResponse> OnItemRegistComplete;
    public static Action<string> OnItemRegistFailed;

    // 아이템 구매 로직 완료
    public static Action<BuyItemResponse> OnItemPurchaseComplete;
    public static Action<string> OnItemPurchaseFailed;

    // 아이템 등록 취소 완료
    public static Action<CancelRegistResponse> OnCancelRegistComplete;
    public static Action<string> OnCancelRegistFailed;

    // 아이템 목록 불러오기 완료
    public static Action<IMarketItemResponse> OnGetSellingListComplete;
    public static Action<IMarketItemResponse> OnGetMySellingListComplete;
    public static Action<bool> OnSetCancelButtonUI;
    
}
