using UnityEngine;


//API들을 관리하는 클래스.
public class APIManager
{
    public string userId { get; private set; }

    public LoginAPI Login { get; private set; }  
    public MarketAPI Market{ get; private set; }
    public PlayerDataAPI PlayerData{ get; private set; } //플레이어 데이터를 관리하는 클래스.
    public QuestAPI Quest{ get; private set; }
    public InventoryAPI Inventory{  get; private set; }

    public APIManager()
    {
        Login = new LoginAPI();
    }


    public void SetUserId(string userId)
    {
        this.userId = userId;

        Quest = new QuestAPI(userId);
        PlayerData = new PlayerDataAPI(userId);
        Inventory = new InventoryAPI(userId);

        Market = new MarketAPI(userId);
    }

}