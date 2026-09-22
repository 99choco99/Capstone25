using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class PromptUIItem : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private Button button;
    [SerializeField] private GameObject keyHint;

    // 텍스트를 설정
    public void SetText(string text)
    {
        promptText.text = text;
    }

    // 색상을 설정
    public void SetColor(Color color)
    {
        backgroundImage.color = color;
    }

    public void SetClickAction(UnityAction action)
    {
        button.onClick.RemoveAllListeners();
        button.enabled = action != null;
        keyHint.SetActive(action == null);
        if (action != null) button.onClick.AddListener(action);
    }
}
