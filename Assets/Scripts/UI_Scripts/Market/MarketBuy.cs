using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MarketBuy : MonoBehaviour
{
    public string marketId;

    [SerializeField] GameObject Recipe;
    [SerializeField] TextMeshProUGUI count;


    private void Start()
    {
        SocketManager.Instance.OnBuyItemSuccess += (response =>
        {
            SuccessBuyItem(response.purchasedItemCount);
        });
    }

    public void CreateRecipe()
    {
        GameObject recipe = Instantiate(Recipe, transform.root);
        recipe.GetComponent<Recipe>().marketId = marketId;
    }

    public void SuccessBuyItem(int purchasedItemCount)
    {
        // 텍스트를 파싱하여 숫자 부분만 추출
        string[] parts = count.text.Split(':');

        // 파싱된 문자열을 안전하게 숫자로 변환
        if (parts.Length < 2 || !int.TryParse(parts[1].Trim(), out int currentCount))
        {
            // 텍스트 형식이 잘못되었을 경우 오류 로그 출력 후 함수 종료
            Debug.LogError("수량 텍스트 형식이 잘못되었습니다.");
            return;
        }
        Debug.Log(parts[1].Trim());
        // 남은 수량 계산 및 처리
        int remainingCount = currentCount - purchasedItemCount;
        Debug.Log(remainingCount);
        if (remainingCount > 0)
        {
            count.text = $"Count: {2}";
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
