using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Recipe : MonoBehaviour
{
    public int marketId;
    [SerializeField] GameObject notice;         //결과 통지
    TextMeshProUGUI notice_text;                //결과 통지 텍스트
    [SerializeField] Button recipe_yesButton;
    [SerializeField] TMP_InputField recipe_Input;
    [SerializeField] GameObject check;
    GameObject checkBox;

    string count;

    private void Awake()
    {
        notice_text = notice.GetComponentInChildren<TextMeshProUGUI>(true);
        notice.GetComponentInChildren<Button>().onClick.AddListener(() => { notice.SetActive(false); });
    }

    private void OnEnable()
    {

        MarketManagerEvents.OnItemPurchaseComplete += SuccessBuyItem;
        MarketManagerEvents.OnItemPurchaseFailed += ShowNotice;
    }

    private void OnDisable()
    {
        MarketManagerEvents.OnItemPurchaseComplete -= SuccessBuyItem;
        MarketManagerEvents.OnItemPurchaseFailed -= ShowNotice;
        notice.GetComponentInChildren<Button>().onClick.RemoveAllListeners();
        recipe_yesButton.onClick.RemoveAllListeners();
    }

    public void OnRecipeYesButtonClick()
    {
        count = recipe_Input.text;
        if (int.Parse(count) <= 0)
        {
            ShowNotice("1이상의 값을 넣어주세요.");
            recipe_Input.text = "";
            return;
        }
        checkBox = Instantiate(check,transform.root);
        checkBox.GetComponentInChildren<Button>().onClick.AddListener(OnCheckYesButtonClick);
        checkBox.SetActive(true);
    }

    public void OnCheckYesButtonClick()
    {
        checkBox.SetActive(false);
        APIManager.Instance.Market.RequestToBuyItem(marketId, count);
        Debug.Log("구매시도");
    }

    public void SuccessBuyItem(BuyItemResponse response)
    {
        Destroy(checkBox);
        Destroy(gameObject);
    }

    // 결과 메시지를 표시하는 전용 함수
    public void ShowNotice(string message)
    {
        notice.SetActive(true);
        notice_text.text = message;
        notice.transform.SetAsLastSibling();

        Destroy(checkBox);
        Destroy(gameObject);
    }


}
