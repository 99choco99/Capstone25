using TMPro;
using UnityEngine;

public class Market : MonoBehaviour
{
    [SerializeField] GameObject notice;         //결과 통지
    [SerializeField] TextMeshProUGUI notice_text;                //결과 통지 텍스트

    private void Start()
    {
        SocketManager.Instance.OnBuyItemFailed += ShowNotice;
        SocketManager.Instance.OnBuyItemSuccess += (response) => {
            ShowNotice(response.message);
        };
    }

    void ShowNotice(string message)
    {
        notice.SetActive(true);
        notice_text.text = message;
        notice.transform.SetAsLastSibling();
    }

}
