using TMPro;
using UnityEngine;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance; // 싱글톤

    [SerializeField] private GameObject itemDescriptionObject;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // 툴팁은 처음에 숨겨져 있어야 함
        itemDescriptionObject.SetActive(false);
    }

    public void ShowTooltip(string text, Vector3 position)
    {
        if (itemDescriptionObject == null) return;
        itemDescriptionText.text = text;
        itemDescriptionObject.transform.position = position + Vector3.down * 10;
        itemDescriptionObject.SetActive(true);
    }

    public void HideTooltip()
    {
        if (itemDescriptionObject == null) return;
        itemDescriptionObject.SetActive(false);
    }
}
