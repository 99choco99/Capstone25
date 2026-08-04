using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PromptUIItem : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI promptText;

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
}