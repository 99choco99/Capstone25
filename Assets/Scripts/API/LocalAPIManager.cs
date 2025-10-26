using Unity.VisualScripting;
using UnityEngine;
using static PublicAPIManager;

public class LocalAPIManager : MonoBehaviour
{


    public InventoryAPI Inventory;
    public QuestAPI Quest;


    public void Awake()
    {
        LoginData loginData = PublicAPIManager.Instance.loginData;


        Inventory = new InventoryAPI(this, loginData.user_id);
        Quest = new QuestAPI(this, loginData.user_id);
    }

}
