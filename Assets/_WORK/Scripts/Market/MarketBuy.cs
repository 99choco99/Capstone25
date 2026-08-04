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


    public void CreateRecipe()
    {
        GameObject recipe = Instantiate(Recipe, transform.root);
        recipe.GetComponent<Recipe>().marketId = marketId;
    }

    private void OnBuyItemSuccessHandler(BuyItemResponse response)
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

    //취소된 아이템을 등록현황에서 제거
    public void RemoveRegistedItem(CancelRegistResponse response)
    {

    }

    // 내 판매목록 가져올 때 취소버튼 활성화
    public void SetActiveItemCancelButton(bool value)
    {
        cancelButton.SetActive(value);
        gameObject.GetComponent<Button>().enabled = !value;
    }

    //아이템 등록 취소 요청
    public void CancelRegistMyItem()
    {

    }
}
