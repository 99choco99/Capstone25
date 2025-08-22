using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MarketBuy : MonoBehaviour
{
    public int marketId;

    public Image icon;
    [SerializeField] GameObject Recipe;
    [SerializeField] GameObject check;
    [SerializeField] GameObject cancelButton;
    [SerializeField] TextMeshProUGUI count_text;


    private void Start()
    {
        SocketManager.Instance.OnBuyItemSuccess += OnBuyItemSuccessHandler;
        SocketManager.Instance.OnGetMySellingListSuccess += SetActiveItemCancelButton;
    }

    private void OnDisable()
    {
        SocketManager.Instance.OnBuyItemSuccess -= OnBuyItemSuccessHandler;
        SocketManager.Instance.OnGetMySellingListSuccess -= SetActiveItemCancelButton;
    }
    public void CreateRecipe()
    {
        GameObject recipe = Instantiate(Recipe, transform.root);
        recipe.GetComponent<Recipe>().marketId = marketId;
    }

    public void SuccessBuyItem(int remainingItemCount)
    {
        if (remainingItemCount > 0)
        {
            count_text.text = $"Count: {remainingItemCount}";
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnBuyItemSuccessHandler(SocketManager.BuyItemResponse response)
    {
        if (this == null)
        {
            return;
        }
        if (response.marketId == marketId)
        {
            SuccessBuyItem(response.remainingItemCount);
        }
    }

    void SetActiveItemCancelButton(SocketManager.GetSellingListResponse response)
    {
        cancelButton.SetActive(true);
    }

    //아이템 등록 취소
    void CancelRegistMyItem()
    {

    }
}
