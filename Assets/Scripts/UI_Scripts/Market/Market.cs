using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Market : MonoBehaviour
{
    [SerializeField] GameObject notice;                 //결과 통지
    [SerializeField] TextMeshProUGUI notice_text;       //결과 통지 텍스트

    private void Start()
    {
        SocketManager.Instance.OnItemRegisterSuccess += OnItemRegisterSuccessHandler;
        SocketManager.Instance.OnItemRegisterFailed += ShowNotice;


        //아이템 구매 성공 실패시 안내문 뜨게 함.
        SocketManager.Instance.OnBuyItemSuccess += OnBuyItemSuccessHandler;
        SocketManager.Instance.OnBuyItemFailed += ShowNotice;

    }

    private void OnDestroy()
    {

        if (SocketManager.Instance != null)
        {
            SocketManager.Instance.OnItemRegisterFailed -= ShowNotice;
            SocketManager.Instance.OnBuyItemFailed -= ShowNotice;

            SocketManager.Instance.OnItemRegisterSuccess -= OnItemRegisterSuccessHandler;
            SocketManager.Instance.OnBuyItemSuccess -= OnBuyItemSuccessHandler;
        }
    }

    // 아이템 등록 성공 시 호출될 메서드
    private void OnItemRegisterSuccessHandler(SocketManager.ItemRegistResponse response)
    {
        ShowNotice(response.message);
    }

    // 아이템 구매 성공 시 호출될 메서드
    private void OnBuyItemSuccessHandler(SocketManager.BuyItemResponse response)
    {
        ShowNotice(response.message);
    }


    // 결과 메시지를 표시하는 전용 함수
    public void ShowNotice(string message)
    {
        notice.SetActive(true);
        notice_text.text = message;
        notice.transform.SetAsLastSibling();
    }
}
