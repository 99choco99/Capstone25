using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Recipe : MonoBehaviour
{
    public string marketId;
    [SerializeField] Button recipe_yesButton;
    [SerializeField] TMP_InputField recipe_Input;
    [SerializeField] GameObject check;
    GameObject checkBox;
    string count;

    private void Start()
    {
        SocketManager.Instance.OnBuyItemSuccess += (_ =>
        {
            SuccessBuyItem();
        });
    }

    public void OnRecipeYesButtonClick()
    {
        count = recipe_Input.text;
        if (int.Parse(count) < 0)
        {
            Debug.Log("1 이상의 값을 넣어주세요.");
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

    public void SuccessBuyItem()
    {
        Destroy(checkBox);
        Destroy(gameObject);
    }


}
