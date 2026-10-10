using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TitleScreenManager : MonoBehaviour
{
    [SerializeField] Button StartButton;
    [SerializeField] GameObject ErrorPopupObj;
    [SerializeField] TextMeshProUGUI StatusText;

    /// <summary>서버에 접속하지 않고 로컬 저장 데이터로 게임을 시작합니다.</summary>
    public async void OnStartButtonClick()
    {
        if (!StartButton.interactable) return;
        StartButton.interactable = false;

        try
        {
            await GameManager.Instance.StartGame();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            SetStatus($"게임 시작 실패: {exception.Message}");
            StartButton.interactable = true;
        }
    }

    //메세지 표시
    void SetStatus(string message)
    {
        StatusText.text = message;
        ErrorPopupObj.SetActive(true);
    }
}
