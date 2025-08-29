using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Recipe : MonoBehaviour
{
    public int marketId;
    [SerializeField] Button recipe_yesButton;
    [SerializeField] TMP_InputField recipe_Input;
    [SerializeField] GameObject check;
    GameObject checkBox;
    Market market;
    string count;

    private void Start()
    {
        market = GetComponent<Market>();
        SocketManager.Instance.OnBuyItemSuccess += SuccessBuyItem;
    }

    private void OnDestroy()
    {
        SocketManager.Instance.OnBuyItemSuccess -= SuccessBuyItem;
    }

    public void OnRecipeYesButtonClick()
    {
        count = recipe_Input.text;
        if (int.Parse(count) <= 0 && market != null)
        {
            market.ShowNotice("1이상의 값을 넣어주세요.");
            recipe_Input.text = "";
            return;
        }
        checkBox = Instantiate(check, transform.root);
        checkBox.GetComponentInChildren<Button>().onClick.AddListener(OnCheckYesButtonClick);
    }

    public void OnCheckYesButtonClick()
    {
        checkBox.SetActive(false);
        SocketManager.Instance.RequestToBuyItem(marketId, count);
        Debug.Log("구매시도");
    }

    public void SuccessBuyItem(SocketManager.BuyItemResponse response)
    {
        Destroy(checkBox);
        Destroy(gameObject);
    }

    

}
