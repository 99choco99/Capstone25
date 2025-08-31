using System;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public static class APIEvents
{




    //아이템 등록 성공/실패
    public static Action<ItemRegistResponse> OnItemRegisterSuccess;
    public static Action<string> OnItemRegisterFailed;

    //아이템 구매 성공/실패
    public static Action<BuyItemResponse> OnBuyItemSuccess;
    public static Action<string> OnBuyItemFailed;

    //아이템 판매 목록 가져오기
    public static Action<IMarketItemResponse> OnGetSellingListSuccess;  //아이템 판매 목록 가져오기 성공 이벤트
    public static Action<IMarketItemResponse> OnGetMySellingListSuccess; //내 판매 목록 가져오기 성공 이벤트

}
