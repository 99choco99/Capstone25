using UnityEngine;


public class IMarketItemResponse
{
    public string userId { get; set; } //등록한 유저 아이디
    public int marketId { get; set; }  //마켓 id
    public int ItemId { get; set; }     //아이템 id
    public int ItemCount { get; set; }  // 등록된 아이템 개수
    public int price { get; set; }   //등록한 가격
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


public class SellItemData
{

}

public class CancelRegistResponse
{

}

public class ItemRegistResponse { }

